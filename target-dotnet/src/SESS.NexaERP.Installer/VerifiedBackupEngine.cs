using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;

internal static class VerifiedBackupEngine
{
    internal static async Task<string> RunAsync(VerifiedBackupConfiguration config)
    {
        Validate(config);
        var source=Source(config);
        var rootId=BackupFiles.OpenRoot(config.BackupRoot);
        BackupFiles.OpenRoot(config.WorkingRoot);
        await using var rootLock=new FileStream(BackupFiles.Child(config.BackupRoot,"backup.lock"),
            FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var run=Guid.NewGuid();
        var bundle=BackupFiles.Child(config.BackupRoot,"run-"+run.ToString("N"));
        BackupFiles.CreatePrivateDirectory(bundle);
        var started=DateTimeOffset.UtcNow;
        BackupFiles.AtomicJson(Path.Combine(bundle,"started.json"),new { RootId=rootId,RunId=run,StartedUtc=started });
        var toolsLog=Path.Combine(bundle,"tools.log");
        await using var configuration=BackupConfigurationSnapshot.Open(config);
        await using var connection=new NpgsqlConnection(source.ConnectionString);
        await connection.OpenAsync();
        await RequireSource(connection,config);
        await BackupDatabaseSnapshot.Scalar(connection,null,"SET search_path=pg_catalog; SET timezone='UTC'; SELECT 'ready'");
        BackupDatabaseEvidence evidence;
        await using(var transaction=await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead))
        {
            await BackupDatabaseSnapshot.Scalar(connection,transaction,"SET TRANSACTION READ ONLY; SELECT 'ready'");
            var snapshot=await BackupDatabaseSnapshot.Scalar(connection,transaction,"SELECT pg_export_snapshot()");
            evidence=await BackupDatabaseSnapshot.ReadAsync(connection,transaction);
            await BackupProcess.Run(config.PostgreSqlBin,"pg_dump",toolsLog,source,
                Path.Combine(bundle,"absent-passfile"),"--format=custom","--snapshot",snapshot,
                "--file",Path.Combine(bundle,"database.dump"),"--dbname",config.ExpectedDatabase);
            await BackupProcess.Run(config.PostgreSqlBin,"pg_dumpall",toolsLog,source,
                Path.Combine(bundle,"absent-passfile"),"--globals-only","--no-role-passwords","--no-tablespaces",
                "--quote-all-identifiers","--database",config.ExpectedDatabase,"--file",Path.Combine(bundle,"globals.sql"));
            await transaction.CommitAsync();
        }
        // Globals have no shared database snapshot. Refuse observable role changes.
        var rolesAfter=await BackupDatabaseSnapshot.Scalar(connection,null,BackupDatabaseSnapshot.RolesSql);
        if(evidence.Roles!=rolesAfter) throw new InvalidOperationException("Role definitions changed during backup; retry during a stable administration window.");
        var supplemental=await configuration.CopyAsync(bundle);
        BackupFileEvidence[] files=[await BackupFiles.Evidence(bundle,"database.dump"),await BackupFiles.Evidence(bundle,"globals.sql"),..supplemental];
        await File.WriteAllTextAsync(Path.Combine(bundle,"source-evidence.json"),JsonSerializer.Serialize(evidence,BackupFiles.Json));
        await RestorePrivate(config,bundle,evidence,null,files);
        foreach(var file in files)
            if(file!=await BackupFiles.Evidence(bundle,file.Name))
                throw new InvalidOperationException("Backup artifacts changed during restore verification.");
        await configuration.RequireUnchanged(supplemental);
        var manifest=new VerifiedBackupManifest(supplemental.Length==0 ? 1 : 2,"VERIFIED",rootId,run,started,DateTimeOffset.UtcNow,
            started.DayOfWeek==DayOfWeek.Sunday,config.ExpectedDatabase,config.ExpectedSystemIdentifier,evidence,files);
        BackupFiles.AtomicJson(Path.Combine(bundle,"manifest.json"),manifest);
        await Retain(config.BackupRoot,rootId,DateTimeOffset.UtcNow);
        return bundle;
    }

    internal static async Task VerifyAsync(VerifiedBackupConfiguration config,string bundle,string? recoveryDestination=null)
    {
        Validate(config);
        var rootId=BackupFiles.OpenRoot(config.BackupRoot);
        BackupFiles.OpenRoot(config.WorkingRoot);
        await using var rootLock=new FileStream(BackupFiles.Child(config.BackupRoot,"backup.lock"),
            FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var manifest=ReadManifest(config.BackupRoot,rootId,bundle);
        if(manifest.Database!=config.ExpectedDatabase || manifest.SourceSystemIdentifier!=config.ExpectedSystemIdentifier)
            throw new InvalidOperationException("Backup does not match the configured source identity.");
        await BackupFiles.RequireFiles(bundle,manifest);
        await RestorePrivate(config,bundle,manifest.DatabaseEvidence,recoveryDestination,manifest.Files);
    }

    internal static VerifiedBackupManifest ReadManifest(string root,Guid rootId,string bundle)
    {
        var full=BackupFiles.Child(root,Path.GetFileName(bundle));
        if(!string.Equals(full,BackupFiles.Canonical(bundle),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup bundle is outside the configured root.");
        var value=JsonSerializer.Deserialize<VerifiedBackupManifest>(File.ReadAllText(BackupFiles.Child(full,"manifest.json")),BackupFiles.Json)
            ?? throw new InvalidOperationException("Missing backup manifest.");
        if(value.RootId!=rootId || Path.GetFileName(full)!="run-"+value.RunId.ToString("N")
            || value.RunId==Guid.Empty || value.Format is not (1 or 2) || value.State!="VERIFIED")
            throw new InvalidOperationException("Backup bundle ownership mismatch.");
        return value;
    }

    private static async Task RestorePrivate(VerifiedBackupConfiguration config,string bundle,
        BackupDatabaseEvidence expected,string? recoveryDestination,BackupFileEvidence[] expectedFiles)
    {
        var scratchId=Guid.NewGuid();
        var scratch=recoveryDestination is null
            ? BackupFiles.Child(config.WorkingRoot,"restore-"+scratchId.ToString("N"))
            : BackupFiles.Canonical(recoveryDestination);
        if(Directory.Exists(scratch) || File.Exists(scratch))
            throw new InvalidOperationException("Restore destination must be a new, absent directory.");
        if(recoveryDestination is not null
            && (IsWithin(scratch,config.BackupRoot) || IsWithin(config.BackupRoot,scratch)
                || IsWithin(scratch,config.WorkingRoot) || IsWithin(config.WorkingRoot,scratch)))
            throw new InvalidOperationException("Recovery destination must be separate from backup and verifier roots.");
        var password=recoveryDestination is null ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            : Environment.GetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword");
        if(password is null || password.Length<14 || password.Contains('\n') || password.Contains('\r'))
            throw new InvalidOperationException("Set a recovery bootstrap password of at least 14 characters in NexaErp__RecoveryBootstrapPassword.");
        BackupFiles.CreatePrivateDirectory(scratch);
        var marker=scratchId.ToString("N");
        await File.WriteAllTextAsync(Path.Combine(scratch,".sess-restore-owned"),marker);
        var data=Path.Combine(scratch,"data");
        var pwfile=Path.Combine(scratch,"init-password");
        var restoreGlobals=Path.Combine(scratch,"restore-globals.sql");
        var toolsLog=Path.Combine(bundle,"tools.log");
        var serverLog=Path.Combine(scratch,"postgres.log");
        var port=BackupProcess.ReservePort();
        var target=new NpgsqlConnectionStringBuilder {
            Host="127.0.0.1",Port=port,Database="postgres",Username=expected.BootstrapRole,
            Password=password,Pooling=false,SslMode=SslMode.Disable,Timeout=15 };
        var startAttempted=false;
        var stopped=false;
        var verified=false;
        try
        {
            await File.WriteAllTextAsync(pwfile,password+Environment.NewLine);
            await BackupProcess.Run(config.PostgreSqlBin,"initdb",toolsLog,null,Path.Combine(scratch,"absent-passfile"),
                "-D",data,"--username",expected.BootstrapRole,"--auth=scram-sha-256","--pwfile",pwfile,"--encoding=UTF8","--no-locale","--data-checksums");
            File.Delete(pwfile);
            startAttempted=true;
            await BackupProcess.Run(config.PostgreSqlBin,"pg_ctl",toolsLog,null,Path.Combine(scratch,"absent-passfile"),
                "-D",data,"-l",serverLog,"-o",$"-h 127.0.0.1 -p {port} -c fsync=on -c synchronous_commit=on","-w","start");
            await using(var control=new NpgsqlConnection(target.ConnectionString))
            {
                await control.OpenAsync();
                var identity=await BackupDatabaseSnapshot.Scalar(control,null,
                    "SELECT system_identifier::text FROM pg_control_system()");
                if(identity==config.ExpectedSystemIdentifier)
                    throw new InvalidOperationException("Restore must use a different private cluster.");
                var actualDirectory=await BackupDatabaseSnapshot.Scalar(control,null,"SHOW data_directory");
                if(!string.Equals(Path.GetFullPath(actualDirectory),Path.GetFullPath(data),StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Verifier data directory mismatch.");
                if(await BackupDatabaseSnapshot.Scalar(control,null,"SHOW fsync")!="on"
                    || await BackupDatabaseSnapshot.Scalar(control,null,"SHOW synchronous_commit")!="on"
                    || await BackupDatabaseSnapshot.Scalar(control,null,"SELECT count(*)::text FROM pg_database WHERE datname NOT IN ('postgres','template0','template1')")!="0")
                    throw new InvalidOperationException("Verifier cluster is not fresh and durable.");
            }
            var lines=await File.ReadAllLinesAsync(Path.Combine(bundle,"globals.sql"));
            var create="CREATE ROLE "+BackupDatabaseSnapshot.Quote(expected.BootstrapRole)+";";
            if(lines.Count(x=>x==create)!=1) throw new InvalidOperationException("Globals must contain exactly one bootstrap-role creation statement.");
            await File.WriteAllLinesAsync(restoreGlobals,lines.Where(x=>x!=create));
            await BackupProcess.Run(config.PostgreSqlBin,"psql",toolsLog,target,Path.Combine(scratch,"absent-passfile"),
                "-X","--set","ON_ERROR_STOP=1","--dbname","postgres","--file",restoreGlobals);
            await BackupProcess.Run(config.PostgreSqlBin,"pg_restore",toolsLog,target,Path.Combine(scratch,"absent-passfile"),
                "--create","--exit-on-error","--no-tablespaces","--dbname","postgres",Path.Combine(bundle,"database.dump"));
            target.Database=config.ExpectedDatabase;
            await using(var restored=new NpgsqlConnection(target.ConnectionString))
            {
                await restored.OpenAsync();
                await BackupDatabaseSnapshot.Scalar(restored,null,"SET search_path=pg_catalog; SET timezone='UTC'; SELECT 'ready'");
                await using var transaction=await restored.BeginTransactionAsync(IsolationLevel.RepeatableRead);
                await BackupDatabaseSnapshot.Scalar(restored,transaction,"SET TRANSACTION READ ONLY; SELECT 'ready'");
                var actual=await BackupDatabaseSnapshot.ReadAsync(restored,transaction);
                // Preserve diagnostic metadata inside the protected backup, without passwords.
                await File.WriteAllTextAsync(Path.Combine(bundle,"last-restored-evidence.json"),JsonSerializer.Serialize(actual,BackupFiles.Json));
                BackupDatabaseSnapshot.RequireEqual(expected,actual);
                await transaction.CommitAsync();
            }
            foreach(var expectedFile in expectedFiles)
                if(expectedFile!=await BackupFiles.Evidence(bundle,expectedFile.Name))
                    throw new InvalidOperationException("Backup artifacts changed during restore verification.");
            if(recoveryDestination is not null)
                foreach(var file in expectedFiles.Where(f=>BackupConfigurationSnapshot.IsArtifact(f.Name)))
                {
                    File.Copy(BackupFiles.Child(bundle,file.Name),BackupFiles.Child(scratch,file.Name),false);
                    if(file!=await BackupFiles.Evidence(scratch,file.Name))
                        throw new InvalidOperationException("Recovered configuration hash or size differs.");
                }
            verified=true;
        }
        finally
        {
            File.Delete(pwfile);
            if(startAttempted)
            {
                await BackupProcess.Run(config.PostgreSqlBin,"pg_ctl",toolsLog,null,Path.Combine(scratch,"absent-passfile"),
                    "-D",data,"-m","fast","-w","stop");
                stopped=true;
            }
            else stopped=true;
            if(File.Exists(serverLog))
                File.Copy(serverLog,Path.Combine(bundle,"last-restore-postgresql.log"),true);
            if(stopped && recoveryDestination is null)
                BackupFiles.DeleteTree(config.WorkingRoot,scratch,".sess-restore-owned",marker);
        }
        if(verified && stopped && recoveryDestination is not null)
            BackupFiles.AtomicJson(Path.Combine(scratch,"RECOVERED.json"),new {
                State="RESTORED_VERIFIED_STOPPED",Database=config.ExpectedDatabase,
                BootstrapRole=expected.BootstrapRole,DataDirectory=data,VerifiedUtc=DateTimeOffset.UtcNow,
                Next="Re-establish database service credentials, restore reviewed application configuration and validate application access before enabling users." });
    }

    internal static async Task Retain(string root,Guid rootId,DateTimeOffset now)
    {
        var manifests=new List<(string Path,VerifiedBackupManifest Manifest)>();
        foreach(var directory in Directory.EnumerateDirectories(root,"run-*"))
        {
            BackupFiles.Canonical(directory);
            if(!File.Exists(Path.Combine(directory,"manifest.json"))) continue;
            manifests.Add((directory,ReadManifest(root,rootId,directory)));
        }
        var ordered=manifests.OrderByDescending(x=>x.Manifest.StartedUtc).ToArray();
        foreach(var candidate in ordered.Skip(2))
        {
            if(candidate.Manifest.StartedUtc>=now.AddDays(candidate.Manifest.Weekly?-84:-30)) continue;
            await BackupFiles.RequireFiles(candidate.Path,candidate.Manifest);
            var allowed=new HashSet<string>(StringComparer.Ordinal) {
                "database.dump","globals.sql","manifest.json","started.json","tools.log",
                "last-restored-evidence.json","last-restore-postgresql.log","source-evidence.json" };
            allowed.UnionWith(candidate.Manifest.Files.Select(f=>f.Name));
            if(Directory.EnumerateFileSystemEntries(candidate.Path).Any(x=>!allowed.Contains(Path.GetFileName(x)) || Directory.Exists(x)))
                throw new InvalidOperationException("Retention refuses an unexpected backup entry.");
            foreach(var file in Directory.EnumerateFiles(candidate.Path)) BackupFiles.Canonical(file);
            foreach(var file in Directory.EnumerateFiles(candidate.Path)) File.Delete(file);
            Directory.Delete(candidate.Path,false);
        }
    }

    private static NpgsqlConnectionStringBuilder Source(VerifiedBackupConfiguration config)
    {
        var raw=Environment.GetEnvironmentVariable(config.ConnectionEnvironment);
        if(string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("The configured backup connection environment variable is missing.");
        var source=new NpgsqlConnectionStringBuilder(raw) { Pooling=false,IncludeErrorDetail=false,ApplicationName="SESS.VerifiedBackup" };
        if(source.Host!=config.ExpectedHost || source.Port!=config.ExpectedPort || source.Database!=config.ExpectedDatabase
            || string.IsNullOrWhiteSpace(source.Username))
            throw new InvalidOperationException("Backup connection does not match the configured endpoint/database.");
        return source;
    }
    private static async Task RequireSource(NpgsqlConnection connection,VerifiedBackupConfiguration config)
    {
        if(await BackupDatabaseSnapshot.Scalar(connection,null,"SELECT system_identifier::text FROM pg_control_system()")!=config.ExpectedSystemIdentifier)
            throw new InvalidOperationException("Backup source cluster identifier mismatch.");
        var version=int.Parse(await BackupDatabaseSnapshot.Scalar(connection,null,"SHOW server_version_num"));
        if(version/10000!=17) throw new InvalidOperationException("This backup implementation requires PostgreSQL 17.");
        if(await BackupDatabaseSnapshot.Scalar(connection,null,"SELECT (rolsuper AND rolcanlogin)::text FROM pg_roles WHERE oid=10")!="true")
            throw new InvalidOperationException("Automatic restore requires the original bootstrap role to retain superuser/login.");
    }
    internal static void Validate(VerifiedBackupConfiguration config)
    {
        if(string.IsNullOrWhiteSpace(config.ConnectionEnvironment) || string.IsNullOrWhiteSpace(config.ExpectedHost)
            || config.ExpectedPort is <1 or >65535 || string.IsNullOrWhiteSpace(config.ExpectedDatabase)
            || config.ExpectedDatabase is "postgres" or "template0" or "template1"
            || !System.Text.RegularExpressions.Regex.IsMatch(config.ExpectedDatabase,@"\A[A-Za-z_][A-Za-z0-9_]{0,62}\z")
            || !ulong.TryParse(config.ExpectedSystemIdentifier,out _))
            throw new InvalidOperationException("Explicit backup source identity is required.");
        BackupFiles.Canonical(config.PostgreSqlBin);
        BackupFiles.Canonical(config.BackupRoot);
        BackupFiles.Canonical(config.WorkingRoot);
        if(IsWithin(config.BackupRoot,config.WorkingRoot) || IsWithin(config.WorkingRoot,config.BackupRoot))
            throw new InvalidOperationException("Backup and verification working roots must be separate.");
    }
    private static bool IsWithin(string child,string parent)
    {
        var c=BackupFiles.Canonical(child);
        var p=BackupFiles.Canonical(parent);
        return c.Equals(p,StringComparison.OrdinalIgnoreCase) || c.StartsWith(p+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    }
}
