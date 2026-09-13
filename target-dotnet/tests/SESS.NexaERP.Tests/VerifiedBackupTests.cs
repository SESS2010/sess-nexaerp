using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task AutomatedBackupRestoresThreeBandDataAndRefusesCorruptedBundle()
    {
        var observed=false;
        await RunCompletePurchaseFlow(mixedRun:async context=>
        {
            observed=true;
            await using var db=new SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext(context.Options);
            var source=new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
            Assert.Equal("127.0.0.1",source.Host);
            Assert.Equal("advance_parser",source.Database);
            Assert.NotEqual(5432,source.Port);
            Assert.False(source.Pooling);
            await using var admin=new NpgsqlConnection(source.ConnectionString);
            await admin.OpenAsync();
            var system=await BackupDatabaseSnapshot.Scalar(admin,null,"SELECT system_identifier::text FROM pg_control_system()");
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item26","automated-backup-"+Guid.NewGuid().ToString("N"));
            BackupFiles.CreatePrivateDirectory(evidence);
            var variable="SESS_BACKUP_TEST_"+Guid.NewGuid().ToString("N");
            var config=new VerifiedBackupConfiguration(variable,source.Host!,source.Port,source.Database!,system,
                FindPostgreSqlBin(),Path.Combine(evidence,"backups"),Path.Combine(evidence,"verifiers"));
            var oldRecovery=Environment.GetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword");
            Environment.SetEnvironmentVariable(variable,source.ConnectionString);
            try
            {
                await File.WriteAllTextAsync(Path.Combine(evidence,"config.json"),JsonSerializer.Serialize(config,BackupFiles.Json));
                var timer=System.Diagnostics.Stopwatch.StartNew();
                var bundle=await VerifiedBackupEngine.RunAsync(config);
                var backupSeconds=timer.Elapsed.TotalSeconds;
                var rootId=BackupFiles.OpenRoot(config.BackupRoot);
                var manifest=VerifiedBackupEngine.ReadManifest(config.BackupRoot,rootId,bundle);
                Assert.Equal("VERIFIED",manifest.State);
                Assert.Equal(3,manifest.DatabaseEvidence.TableCounts["\"advance\".\"purchase_orders\""]);
                Assert.True(manifest.DatabaseEvidence.TableCounts["\"advance\".\"stock_movements\""]>0);
                Assert.True(manifest.DatabaseEvidence.TableCounts["\"advance\".\"fifo_cost_consumptions\""]>0);
                Assert.Equal(manifest.DatabaseEvidence.TableCounts["\"advance\".\"command_requests\""],
                    manifest.DatabaseEvidence.TableCounts["\"advance\".\"command_receipts\""]);
                Assert.Empty(Directory.EnumerateDirectories(config.WorkingRoot));
                var globals=Path.Combine(bundle,"globals.sql");
                var original=await File.ReadAllBytesAsync(globals);
                await File.AppendAllTextAsync(globals,"\n-- deliberate disposable backup corruption\n");
                try
                {
                    var failure=await Assert.ThrowsAsync<InvalidOperationException>(()=>VerifiedBackupEngine.VerifyAsync(config,bundle));
                    Assert.Contains("hash or size",failure.Message,StringComparison.Ordinal);
                    Assert.Empty(Directory.EnumerateDirectories(config.WorkingRoot));
                }
                finally { await File.WriteAllBytesAsync(globals,original); }
                var recovered=Path.Combine(evidence,"recovered");
                var password=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
                Environment.SetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword",password);
                timer.Restart();
                await VerifiedBackupEngine.VerifyAsync(config,bundle,recovered);
                var recoverySeconds=timer.Elapsed.TotalSeconds;
                Assert.True(File.Exists(Path.Combine(recovered,"RECOVERED.json")));
                Assert.False(File.Exists(Path.Combine(recovered,"data","postmaster.pid")));
                var port=BackupProcess.ReservePort();
                var connection=new NpgsqlConnectionStringBuilder { Host="127.0.0.1",Port=port,Database="advance_parser",
                    Username=manifest.DatabaseEvidence.BootstrapRole,Password=password,Pooling=false,SslMode=SslMode.Disable };
                var started=false;
                try
                {
                    started=true;
                    await BackupProcess.Run(config.PostgreSqlBin,"pg_ctl",Path.Combine(evidence,"recovery-tools.log"),null,
                        Path.Combine(evidence,"absent-passfile"),"-D",Path.Combine(recovered,"data"),
                        "-l",Path.Combine(recovered,"postgres.log"),"-o",$"-h 127.0.0.1 -p {port} -c fsync=on -c synchronous_commit=on","-w","start");
                    await using var restored=new NpgsqlConnection(connection.ConnectionString);
                    await restored.OpenAsync();
                    Assert.NotEqual(system,await BackupDatabaseSnapshot.Scalar(restored,null,"SELECT system_identifier::text FROM pg_control_system()"));
                    await using(var check=new NpgsqlCommand(DatabasePrincipalProvisioningSql.Verify,restored))
                        await check.ExecuteNonQueryAsync();
                    var runtimePassword=Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
                    await using(var reset=new NpgsqlCommand("ALTER ROLE nexa_erp_runtime PASSWORD '"+runtimePassword+"'",restored))
                        await reset.ExecuteNonQueryAsync();
                    var runtime=new NpgsqlConnectionStringBuilder(connection.ConnectionString) {
                        Username="nexa_erp_runtime",Password=runtimePassword };
                    await using var restricted=new NpgsqlConnection(runtime.ConnectionString);
                    await restricted.OpenAsync();
                    var count=long.Parse(await BackupDatabaseSnapshot.Scalar(restricted,null,"SELECT count(*)::text FROM advance.employees"));
                    Assert.Equal(manifest.DatabaseEvidence.TableCounts["\"advance\".\"employees\""],count);
                    var denied=await Assert.ThrowsAsync<PostgresException>(()=>BackupDatabaseSnapshot.Scalar(restricted,null,"SELECT count(*)::text FROM advance.command_receipts"));
                    Assert.Equal("42501",denied.SqlState);
                }
                finally
                {
                    if(started) await BackupProcess.Run(config.PostgreSqlBin,"pg_ctl",Path.Combine(evidence,"recovery-tools.log"),null,
                        Path.Combine(evidence,"absent-passfile"),"-D",Path.Combine(recovered,"data"),"-m","fast","-w","stop");
                }

                var installer=Path.Combine(FindRepositoryRoot(),"src","SESS.NexaERP.Installer","bin",
#if DEBUG
                    "Debug",
#else
                    "Release",
#endif
                    "net10.0","SESS.NexaERP.Installer.exe");
                var schedule=new System.Diagnostics.ProcessStartInfo("powershell.exe") {
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,
                    WorkingDirectory=FindRepositoryRoot() };
                foreach(var arg in new[]{"-NoProfile","-NonInteractive","-File",
                    Path.Combine(FindRepositoryRoot(),"tools","Invoke-VerifiedDatabaseBackupScheduleWitness.ps1"),
                    "-RepositoryRoot",FindRepositoryRoot(),"-InstallerPath",installer,
                    "-ConfigPath",Path.Combine(evidence,"config.json"),"-EvidenceDirectory",evidence})
                    schedule.ArgumentList.Add(arg);
                using(var process=System.Diagnostics.Process.Start(schedule)!)
                {
                    var stdout=process.StandardOutput.ReadToEndAsync();
                    var stderr=process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    var output=await stdout+await stderr;
                    await File.WriteAllTextAsync(Path.Combine(evidence,"scheduled-witness.log"),output);
                    Assert.True(process.ExitCode==0,output);
                }
                Assert.Equal(2,Directory.EnumerateDirectories(config.BackupRoot,"run-*")
                    .Count(x=>File.Exists(Path.Combine(x,"manifest.json"))));
                Assert.True(File.Exists(Path.Combine(evidence,"scheduled-cleanup.json")));
                await File.WriteAllTextAsync(Path.Combine(evidence,"result.json"),JsonSerializer.Serialize(new {
                    BackupSeconds=backupSeconds,RecoverySeconds=recoverySeconds,
                    TableCounts=manifest.DatabaseEvidence.TableCounts,CorruptedBundleRefused=true,
                    RecoveryStopped=true,RuntimeLoginAfterCredentialReset=true,DirectLedgerRead="42501"
                },BackupFiles.Json));
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable,null);
                Environment.SetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword",oldRecovery);
            }
        },durableDatabase:true);
        Assert.True(observed);
    }
}
