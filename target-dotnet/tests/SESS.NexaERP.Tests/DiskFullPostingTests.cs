#if DISK_FULL_WITNESS
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string DiskFullMarker = "SESS_ITEM26_BOUNDED_FS:b82de8ad-a832-400b-be74-9768dea6af91:33554432";
    private const string DiskFullLog = "/mnt/sess-item26-postgresql/postgres.log";

    [Fact]
    public async Task DiskFullDuringIssueReceiptRollsBackAndRetriesOnce()
    {
        var observed=false;
        await RunCompletePurchaseFlow(issueRace:async context =>
        {
            observed=true;
            return await RunDiskFullIssue(context);
        },durableDatabase:true);
        Assert.True(observed);
    }

    private static async Task<MaterialIssueView> RunDiskFullIssue(SerialIssueRaceContext context)
    {
        var raw=Environment.GetEnvironmentVariable("SESS_DISKFULL_WITNESS_CONNECTION")
            ?? throw new InvalidOperationException("The bounded VM PostgreSQL connection is required for DiskFullWitness.");
        var expectedSystem=Environment.GetEnvironmentVariable("SESS_DISKFULL_WITNESS_SYSTEM_ID")
            ?? throw new InvalidOperationException("The owned VM PostgreSQL system identifier is required.");
        var native=new NpgsqlConnectionStringBuilder(raw) { Pooling=false };
        Assert.Equal("127.0.0.1",native.Host);
        Assert.Equal(18444,native.Port);
        Assert.Equal("postgres",native.Database);
        Assert.Equal("sess_diskfull_bootstrap",native.Username);
        await using var sourceDb=new NexaErpDbContext(context.Options);
        var source=new NpgsqlConnectionStringBuilder(sourceDb.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);
        Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);
        Assert.NotEqual(5432,source.Port);
        Assert.NotEqual(native.Port,source.Port);
        Assert.False(source.Pooling);
        await using var sourceAdmin=new NpgsqlConnection(source.ConnectionString);
        await sourceAdmin.OpenAsync();
        await using var admin=new NpgsqlConnection(native.ConnectionString);
        await admin.OpenAsync();
        var identity=await DiskFullScalar(admin,"""
            SELECT jsonb_build_object('system',system_identifier::text,'directory',current_setting('data_directory'),
              'port',inet_server_port(),'fsync',current_setting('fsync'),'sync',current_setting('synchronous_commit'),
              'marker',btrim(pg_read_file('/mnt/sess-item26-full/.sess-item26-bounded'),E'\r\n'))::text
            FROM pg_control_system()
            """);
        using(var verified=JsonDocument.Parse(identity))
        {
            var root=verified.RootElement;
            Assert.Equal(expectedSystem,root.GetProperty("system").GetString());
            Assert.Equal("/mnt/sess-item26-postgresql",root.GetProperty("directory").GetString());
            Assert.Equal(5433,root.GetProperty("port").GetInt32());
            Assert.Equal("on",root.GetProperty("fsync").GetString());
            Assert.Equal("on",root.GetProperty("sync").GetString());
            Assert.Equal(DiskFullMarker,root.GetProperty("marker").GetString());
        }
        Assert.NotEqual(expectedSystem,await DiskFullScalar(sourceAdmin,"SELECT system_identifier::text FROM pg_control_system()"));
        Assert.Equal("false",await DiskFullScalar(admin,"SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname='advance_parser')::text"));
        var sourceBefore=await DiskFullSnapshot(sourceAdmin,context.Draft.Id);
        var sourceRoles=await DiskFullScalar(sourceAdmin,DiskFullRolesSql);
        var temp=Path.Combine(Path.GetTempPath(),"sess-disk-full-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var dump=Path.Combine(temp,"fixture.dump");
        var globals=Path.Combine(temp,"globals.sql");
        var bin=FindPostgreSqlBin();
        var nonce=Guid.NewGuid().ToString("N");
        var tablespace="item26_full_"+nonce;
        var space="/mnt/sess-item26-full/space-"+nonce;
        var filler="/mnt/sess-item26-full/filler-"+nonce;
        var databaseAttempted=false;
        var spaceCreated=false;
        var directoryCreated=false;
        try
        {
            await DiskFullTool(bin,"pg_dump",source,temp,"--format=custom","--file",dump,"--dbname","advance_parser");
            await DiskFullTool(bin,"pg_dumpall",source,temp,"--globals-only","--no-role-passwords","--no-tablespaces","--database","advance_parser","--file",globals);
            var roleCount=await DiskFullScalar(admin,"SELECT count(*)::text FROM pg_roles WHERE rolname !~ '^pg_' AND rolname<>'sess_diskfull_bootstrap'");
            Assert.Equal("postgres",await DiskFullScalar(sourceAdmin,"SELECT rolname::text FROM pg_roles WHERE oid=10"));
            Assert.Equal("postgres",await DiskFullScalar(admin,"SELECT rolname::text FROM pg_roles WHERE oid=10"));
            if(roleCount=="1")
            {
                // PostgreSQL 17 requires original bootstrap grantor identity.
                // Keep every ALTER/GRANT, omitting only creation of that existing role.
                var lines=await File.ReadAllLinesAsync(globals);
                Assert.Single(lines,x=>x=="CREATE ROLE postgres;");
                await File.WriteAllLinesAsync(globals,lines.Where(x=>x!="CREATE ROLE postgres;"));
                await DiskFullTool(bin,"psql",native,temp,"-X","--set","ON_ERROR_STOP=1","--dbname","postgres","--file",globals);
            }
            Assert.Equal(sourceRoles,await DiskFullScalar(admin,DiskFullRolesSql));
            databaseAttempted=true;
            await DiskFullTool(bin,"pg_restore",native,temp,"--create","--exit-on-error","--no-tablespaces","--dbname","postgres",dump);
            await using(var mark=new NpgsqlCommand($"COMMENT ON DATABASE advance_parser IS 'SESS_ITEM26_DISPOSABLE:{nonce}'",admin))
                await mark.ExecuteNonQueryAsync();
            var target=new NpgsqlConnectionStringBuilder(native.ConnectionString) { Database="advance_parser" };
            var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(target.ConnectionString).Options;
            await using(var db=new NpgsqlConnection(target.ConnectionString))
            {
                await db.OpenAsync();
                Assert.Equal(sourceBefore,await DiskFullSnapshot(db,context.Draft.Id));
                // Only the owned, marked loop filesystem may receive the filler.
                await using(var df=new NpgsqlCommand("""
                    CREATE TEMP TABLE witness_filesystem(line text);
                    COPY witness_filesystem FROM PROGRAM '/bin/df -kP /mnt/sess-item26-full';
                    """,db)) await df.ExecuteNonQueryAsync();
                var dfLine=await DiskFullScalar(db,"SELECT line FROM witness_filesystem WHERE line LIKE '/dev/loop%'");
                var fields=Regex.Split(dfLine.Trim(),@"\s+");
                Assert.Equal("/mnt/sess-item26-full",fields[^1]);
                Assert.InRange(long.Parse(fields[1]),16384,32768);
                Assert.InRange(long.Parse(fields[3]),22550,32768);
                await using(var mkdir=new NpgsqlCommand($"COPY (SELECT '' WHERE false) TO PROGRAM '/bin/mkdir -m 700 {space}'",db))
                    await mkdir.ExecuteNonQueryAsync();
                directoryCreated=true;
                await using(var createSpace=new NpgsqlCommand($"CREATE TABLESPACE {tablespace} OWNER nexa_erp_owner LOCATION '{space}'",db))
                    await createSpace.ExecuteNonQueryAsync();
                spaceCreated=true;
                await using(var probe=new NpgsqlCommand($$"""
                    CREATE TABLE advance.witness_disk_full_probe("Payload" bytea) TABLESPACE {{tablespace}};
                    ALTER TABLE advance.witness_disk_full_probe ALTER COLUMN "Payload" SET STORAGE EXTERNAL;
                    ALTER TABLE advance.witness_disk_full_probe OWNER TO nexa_erp_owner;
                    GRANT INSERT ON advance.witness_disk_full_probe TO nexa_erp_runtime;
                    CREATE FUNCTION advance.witness_disk_full_receipt() RETURNS trigger LANGUAGE plpgsql AS $f$
                    BEGIN
                      INSERT INTO advance.witness_disk_full_probe VALUES(convert_to(repeat('x',8388608),'UTF8'));
                      RETURN NEW;
                    END $f$;
                    CREATE TRIGGER witness_disk_full_receipt BEFORE INSERT ON advance.command_receipts
                      FOR EACH ROW EXECUTE FUNCTION advance.witness_disk_full_receipt();
                    """,db)) await probe.ExecuteNonQueryAsync();
                var company=Guid.Parse("70000000-0000-0000-0000-000000000001");
                var assignments=await Query(options,async state =>
                    (await state.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                        .Where(x=>x.CompanyId==company && x.EffectiveTo==null).ToListAsync())
                    .ToDictionary(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                        x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
                var subject=await Query(options,state=>state.EmployeeIdentityMappings
                    .Where(x=>x.CompanyId==company && x.EmployeeId==context.FirstOperatorId && x.IsActive)
                    .Select(x=>x.Subject).SingleAsync());
                var actor=new TaxWorkflowUser(context.FirstOperatorId,subject,"STORES_EXECUTIVE",assignments);
                var runtime=new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
                    { Host=native.Host,Port=native.Port,Database="advance_parser",Pooling=false }.ConnectionString;
                await using var host=await PurchaseFlowHost.StartAsync(runtime,actor,true,true);
                var before=await DiskFullSnapshot(db,context.Draft.Id);
                RaceHttpResult failed;
                string afterFailure;
                var logStart=long.Parse(await DiskFullScalar(db,$"SELECT (pg_stat_file('{DiskFullLog}')).size::text"));
                try
                {
                    await using(var fill=new NpgsqlCommand($"COPY (SELECT repeat('f',23068672)) TO '{filler}'",db))
                        await fill.ExecuteNonQueryAsync();
                    Assert.Equal("23068673",await DiskFullScalar(db,$"SELECT (pg_stat_file('{filler}')).size::text"));
                    failed=await TimedRacePost(host.Client,$"/api/v1/stores/material-issues/from-request/{context.Draft.Id}",context.Command);
                    afterFailure=await DiskFullSnapshot(db,context.Draft.Id);
                    Assert.Equal("0",await DiskFullScalar(db,"SELECT count(*)::text FROM advance.witness_disk_full_probe"));
                }
                finally
                {
                    await using var remove=new NpgsqlCommand($"COPY (SELECT '' WHERE false) TO PROGRAM '/bin/rm -f -- {filler}'",db);
                    await remove.ExecuteNonQueryAsync();
                }
                var retry=await TimedRacePost(host.Client,$"/api/v1/stores/material-issues/from-request/{context.Draft.Id}",context.Command);
                var replay=await TimedRacePost(host.Client,$"/api/v1/stores/material-issues/from-request/{context.Draft.Id}",context.Command);
                var afterRetry=await DiskFullSnapshot(db,context.Draft.Id);
                var log=await DiskFullScalar(db,$"SELECT pg_read_file('{DiskFullLog}',{logStart},16777216,true)");
                var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item26");
                Directory.CreateDirectory(evidence);
                await File.WriteAllTextAsync(Path.Combine(evidence,"disk-full-issue.json"),JsonSerializer.Serialize(new {
                    NativeIdentity=identity,Filesystem=dfLine,SourceBefore=sourceBefore,Before=before,
                    Failed=failed,AfterFailure=afterFailure,Retry=retry,Replay=replay,AfterRetry=afterRetry,
                    Limit="Actual ENOSPC in a bounded receipt-trigger probe table after issue/FIFO/audit writes; not WAL or whole-server disk exhaustion."
                },new JsonSerializerOptions { WriteIndented=true }));
                await File.WriteAllTextAsync(Path.Combine(evidence,"disk-full-issue-postgresql.log"),log);
                Assert.Equal(before,afterFailure);
                Assert.Equal(HttpStatusCode.InternalServerError,failed.Status);
                Assert.Contains("53100",log,StringComparison.Ordinal);
                Assert.Contains("No space left on device",log,StringComparison.Ordinal);
                Assert.DoesNotContain("40P01",log,StringComparison.Ordinal);
                Assert.Equal(HttpStatusCode.Created,retry.Status);
                Assert.Equal(HttpStatusCode.Created,replay.Status);
                var result=JsonSerializer.Deserialize<MaterialIssueView>(retry.Body)!;
                var repeated=JsonSerializer.Deserialize<MaterialIssueView>(replay.Body)!;
                Assert.False(result.Replayed);
                Assert.True(repeated.Replayed);
                Assert.Equal(result.Id,repeated.Id);
                Assert.Equal(1m,Assert.Single(result.Lines).QuantityBase);
                Assert.Equal("1",await DiskFullScalar(db,"SELECT count(*)::text FROM advance.witness_disk_full_probe"));
                using var b=JsonDocument.Parse(before);
                using var a=JsonDocument.Parse(afterRetry);
                foreach(var name in new[]{"issues","lines","history","batches","fifo","audit","requests","receipts"})
                    Assert.Equal(b.RootElement.GetProperty(name).GetInt64()+1,a.RootElement.GetProperty(name).GetInt64());
                Assert.Equal(b.RootElement.GetProperty("movements").GetInt64()+2,a.RootElement.GetProperty("movements").GetInt64());
                Assert.Equal(b.RootElement.GetProperty("requestVersion").GetInt64()+1,a.RootElement.GetProperty("requestVersion").GetInt64());
                Assert.Equal("FULFILLED",a.RootElement.GetProperty("requestStatus").GetString());
            }
        }
        finally
        {
            // This exact native cluster and initially absent fixture database
            // were verified before the restore attempt. Never target the source.
            Assert.Equal(expectedSystem,await DiskFullScalar(admin,"SELECT system_identifier::text FROM pg_control_system()"));
            if(databaseAttempted)
            {
                await using var drop=new NpgsqlCommand("DROP DATABASE IF EXISTS advance_parser",admin);
                await drop.ExecuteNonQueryAsync();
            }
            if(spaceCreated)
            {
                await using var drop=new NpgsqlCommand($"DROP TABLESPACE {tablespace}",admin);
                await drop.ExecuteNonQueryAsync();
            }
            if(directoryCreated)
            {
                await using var remove=new NpgsqlCommand($"COPY (SELECT '' WHERE false) TO PROGRAM '/bin/rmdir -- {space}'",admin);
                await remove.ExecuteNonQueryAsync();
            }
            // Only the two known files in our new temporary directory are removed.
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()),Path.GetFullPath(temp),StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("sess-disk-full-",Path.GetFileName(temp),StringComparison.Ordinal);
            File.Delete(dump);
            File.Delete(globals);
            Directory.Delete(temp,false);
        }
        // The VM test used a restored fixture. Complete the original Windows
        // command once so the parent still witnesses its full three-band flow.
        var originalAssignments=await Query(context.Options,async state =>
            (await state.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001") && x.EffectiveTo==null).ToListAsync())
            .ToDictionary(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
        var originalSubject=await Query(context.Options,state=>state.EmployeeIdentityMappings
            .Where(x=>x.EmployeeId==context.FirstOperatorId && x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001") && x.IsActive)
            .Select(x=>x.Subject).SingleAsync());
        await using var originalHost=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,
            new TaxWorkflowUser(context.FirstOperatorId,originalSubject,"STORES_EXECUTIVE",originalAssignments),true,true);
        return await Post<MaterialIssueView>(originalHost.Client,
            $"/api/v1/stores/material-issues/from-request/{context.Draft.Id}",context.Command);
    }

    private static async Task<string> DiskFullScalar(NpgsqlConnection connection,string sql)
    {
        await using var command=new NpgsqlCommand(sql,connection) { CommandTimeout=120 };
        return await command.ExecuteScalarAsync() is string value ? value : throw new InvalidOperationException("Missing disk-full witness value.");
    }

    private static async Task<string> DiskFullSnapshot(NpgsqlConnection connection,Guid request)
    {
        await using var command=new NpgsqlCommand("""
            SELECT jsonb_build_object(
              'issues',(SELECT count(*) FROM advance.material_issues),
              'lines',(SELECT count(*) FROM advance.material_issue_lines),
              'history',(SELECT count(*) FROM advance.material_issue_history),
              'batches',(SELECT count(*) FROM advance.stock_posting_batches),
              'movements',(SELECT count(*) FROM advance.stock_movements),
              'fifo',(SELECT count(*) FROM advance.fifo_cost_consumptions),
              'audit',(SELECT count(*) FROM advance.audit_logs),
              'requests',(SELECT count(*) FROM advance.command_requests),
              'receipts',(SELECT count(*) FROM advance.command_receipts),
              'requestStatus',(SELECT "Status" FROM advance.material_issue_requests WHERE "Id"=@request),
              'requestVersion',(SELECT "Version" FROM advance.material_issue_requests WHERE "Id"=@request))::text
            """,connection);
        command.Parameters.AddWithValue("request",request);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DiskFullTool(string bin,string tool,NpgsqlConnectionStringBuilder connection,string temp,params string[] args)
    {
        var info=new ProcessStartInfo(Path.Combine(bin,tool+(OperatingSystem.IsWindows()?".exe":"")))
        { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        foreach(var key in info.Environment.Keys.Where(x=>x.StartsWith("PG",StringComparison.Ordinal)).ToArray())
            info.Environment.Remove(key);
        info.Environment["PGPASSFILE"]=Path.Combine(temp,"absent-passfile");
        info.Environment["PGCONNECT_TIMEOUT"]="10";
        if(!string.IsNullOrEmpty(connection.Password)) info.Environment["PGPASSWORD"]=connection.Password;
        foreach(var value in new[]{"--host",connection.Host ?? throw new InvalidOperationException("Fixture host is required."),"--port",connection.Port.ToString(),"--username",connection.Username ?? throw new InvalidOperationException("Fixture username is required."),"--no-password"})
            info.ArgumentList.Add(value);
        foreach(var value in args) info.ArgumentList.Add(value);
        using var process=Process.Start(info)??throw new InvalidOperationException("Cannot start PostgreSQL fixture tool.");
        var stdout=process.StandardOutput.ReadToEndAsync();
        var stderr=process.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(8));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); throw; }
        var output=await stdout+await stderr;
        Assert.True(process.ExitCode==0,$"{tool} failed ({process.ExitCode}): {output}");
    }

    private const string DiskFullRolesSql="""
        SELECT jsonb_build_object(
          'roles',coalesce((SELECT jsonb_agg(jsonb_build_object('name',rolname,'super',rolsuper,'inherit',rolinherit,
            'createRole',rolcreaterole,'createDb',rolcreatedb,'login',rolcanlogin,'replication',rolreplication,
            'bypass',rolbypassrls,'limit',rolconnlimit,'config',rolconfig) ORDER BY rolname)
            FROM pg_roles WHERE rolname !~ '^pg_' AND rolname<>'sess_diskfull_bootstrap'),'[]'::jsonb),
          'memberships',coalesce((SELECT jsonb_agg(jsonb_build_object('role',r.rolname,'member',m.rolname,
            'grantor',g.rolname,'admin',a.admin_option,'inherit',a.inherit_option,'set',a.set_option)
            ORDER BY r.rolname,m.rolname,g.rolname)
            FROM pg_auth_members a JOIN pg_roles r ON r.oid=a.roleid JOIN pg_roles m ON m.oid=a.member
            JOIN pg_roles g ON g.oid=a.grantor
            WHERE r.rolname !~ '^pg_' AND m.rolname !~ '^pg_' AND m.rolname<>'sess_diskfull_bootstrap'),'[]'::jsonb))::text
        """;
}
#endif
