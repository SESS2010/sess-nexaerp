#if KEYCLOAK_WITNESS
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    [Trait("Witness", "KeycloakRecovery")]
    public async Task KeycloakPostgreSqlBackupRestoresPasswordTotpSubjectsAndSigningKeys()
    {
        var control = Environment.GetEnvironmentVariable("SESS_KEYCLOAK_RECOVERY_CONTROL")
            ?? throw new InvalidOperationException("Set the disposable Keycloak recovery controller script.");
        if (!Path.IsPathFullyQualified(control) || !File.Exists(control))
            throw new InvalidOperationException("Recovery controller must be an existing absolute script path.");
        var origin = Environment.GetEnvironmentVariable("SESS_KEYCLOAK_WITNESS_URL")?.TrimEnd('/')
            ?? throw new InvalidOperationException("Set the disposable HTTPS origin.");
        var pin = Environment.GetEnvironmentVariable("SESS_KEYCLOAK_CERT_SHA256")
            ?? throw new InvalidOperationException("Set the disposable certificate pin.");
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != "https" || pin.Length != 64)
            throw new InvalidOperationException("Recovery witness requires loopback HTTPS and a certificate pin.");
        using var sourceServer = DisposablePostgreSql.Start(FindPostgreSqlBin(), durable: true);
        var source = new NpgsqlConnectionStringBuilder(sourceServer.ConnectionString) { Database = "keycloak_witness" };
        Assert.NotEqual(5432, source.Port);
        sourceServer.Execute("identity-database.sql", """
            CREATE ROLE keycloak_fixture LOGIN PASSWORD 'Keycloak-Database-Witness-Only!26';
            CREATE DATABASE keycloak_witness OWNER keycloak_fixture;
            """);
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item16", "recovery-" + Guid.NewGuid().ToString("N"));
        BackupFiles.CreatePrivateDirectory(evidence);
        var restoredDirectory = Path.Combine(evidence, "recovered");
        var restoredStarted = false;
        var timer = Stopwatch.StartNew();
        var variable = "SESS_KEYCLOAK_BACKUP_" + Guid.NewGuid().ToString("N");
        var oldRecovery = Environment.GetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword");
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        try
        {
            await Control("start", source.Port);
            await Ready();
            var staffBefore = await KeycloakPkceLogin(origin, "sess-staff", "SESS-04", false, pin);
            var approverBefore = await KeycloakPkceLogin(origin, "sess-approvers", "SESS-01", true, pin);
            var keysBefore = await Keys();
            await LocalKeycloakContainerAuthenticatesThroughProductionOidcAndRealEmployeeMappings();
            // Quiesce identity writes so the recovery point is exact, including credential state.
            await Control("stop", source.Port);
            await using var admin = new NpgsqlConnection(source.ConnectionString);
            await admin.OpenAsync();
            var system = await BackupDatabaseSnapshot.Scalar(admin, null, "SELECT system_identifier::text FROM pg_control_system()");
            var credentialsBefore = await BackupDatabaseSnapshot.Scalar(admin, null, "SELECT count(*)::text FROM public.credential");
            var providerConfig = Path.Combine(evidence, "keycloak.conf");
            await File.WriteAllTextAsync(providerConfig, "hostname=" + origin + "\n# disposable recovery fixture\n");
            var config = new VerifiedBackupConfiguration(variable, "127.0.0.1", source.Port, "keycloak_witness", system,
                FindPostgreSqlBin(), Path.Combine(evidence, "backups"), Path.Combine(evidence, "verifiers"), new() { ["keycloak.conf"] = providerConfig });
            Environment.SetEnvironmentVariable(variable, source.ConnectionString);
            var configPath = Path.Combine(evidence, "config.json");
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config, BackupFiles.Json));
            var installer = Path.Combine(FindRepositoryRoot(), "src", "SESS.NexaERP.Installer", "bin",
#if DEBUG
                "Debug",
#else
                "Release",
#endif
                "net10.0", "SESS.NexaERP.Installer.exe");
            var scheduled = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            foreach(var argument in new[] { "-NoProfile", "-NonInteractive", "-File",
                Path.Combine(FindRepositoryRoot(), "tools", "Invoke-VerifiedDatabaseBackupScheduleWitness.ps1"),
                "-RepositoryRoot", FindRepositoryRoot(), "-InstallerPath", installer,
                "-ConfigPath", configPath, "-EvidenceDirectory", evidence }) scheduled.ArgumentList.Add(argument);
            using(var process = Process.Start(scheduled)!)
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var errors = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                await File.WriteAllTextAsync(Path.Combine(evidence,"schedule.log"),await output + await errors);
                Assert.True(process.ExitCode == 0, "Scheduled identity backup failed; see private witness log.");
            }
            var bundle = Assert.Single(Directory.GetDirectories(config.BackupRoot,"run-*"));
            var manifest = VerifiedBackupEngine.ReadManifest(config.BackupRoot, BackupFiles.OpenRoot(config.BackupRoot), bundle);
            Assert.Equal("VERIFIED", manifest.State);
            Assert.True(manifest.DatabaseEvidence.TableCounts["\"public\".\"credential\""] > 0);
            Environment.SetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword", password);
            await VerifiedBackupEngine.VerifyAsync(config, bundle, restoredDirectory);
            Assert.Equal(await File.ReadAllTextAsync(providerConfig), await File.ReadAllTextAsync(Path.Combine(restoredDirectory, "configuration-keycloak.conf")));
            var restoredPort = BackupProcess.ReservePort();
            restoredStarted = true;
            await BackupProcess.Run(config.PostgreSqlBin, "pg_ctl", Path.Combine(evidence, "recovery-tools.log"), null,
                Path.Combine(evidence, "absent-passfile"), "-D", Path.Combine(restoredDirectory, "data"),
                "-l", Path.Combine(restoredDirectory, "postgres.log"), "-o", $"-h 127.0.0.1 -p {restoredPort} -c fsync=on -c synchronous_commit=on", "-w", "start");
            var restoredConnection = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Port = restoredPort, Password = password };
            await using (var restored = new NpgsqlConnection(restoredConnection.ConnectionString))
            {
                await restored.OpenAsync();
                Assert.NotEqual(system, await BackupDatabaseSnapshot.Scalar(restored, null, "SELECT system_identifier::text FROM pg_control_system()"));
                Assert.Equal(credentialsBefore, await BackupDatabaseSnapshot.Scalar(restored, null, "SELECT count(*)::text FROM public.credential"));
                // Only the PostgreSQL service credential is reset; realm credentials are retained unchanged.
                await BackupDatabaseSnapshot.Scalar(restored, null,
                    "ALTER ROLE keycloak_fixture PASSWORD 'Keycloak-Database-Witness-Only!26'; SELECT 'reset'");
            }
            // Make the original identity database unavailable: a wrong restart connection
            // must fail instead of accidentally proving login against the source.
            await admin.CloseAsync();
            sourceServer.Dispose();
            await Control("start", restoredPort);
            await Ready();
            var staffAfter = await KeycloakPkceLogin(origin, "sess-staff", "SESS-04", false, pin);
            var approverAfter = await KeycloakPkceLogin(origin, "sess-approvers", "SESS-01", true, pin);
            Assert.Equal(staffBefore.Subject, staffAfter.Subject);
            Assert.Equal(approverBefore.Subject, approverAfter.Subject);
            Assert.Equal(keysBefore, await Keys());
            await LocalKeycloakContainerAuthenticatesThroughProductionOidcAndRealEmployeeMappings();
            await Control("stop", restoredPort);
            await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new {
                State = "VERIFIED", Seconds = timer.Elapsed.TotalSeconds,
                Credentials = credentialsBefore, SameStaffSubject = true, SameApproverSubject = true,
                SameSigningKeys = true, PasswordAndTotpLoginAfterRestore = true, OriginalIdentityDatabaseUnavailable = true,
                AutomatedScheduledBackup = true, DeploymentConfigurationRestored = true,
                ProductionOidcAndMappingsBeforeAndAfterRestore = true,
                TableCounts = manifest.DatabaseEvidence.TableCounts
            }, BackupFiles.Json));
        }
        finally
        {
            try { await Control("stop", source.Port); }
            finally
            {
                if (restoredStarted)
                    await BackupProcess.Run(FindPostgreSqlBin(), "pg_ctl", Path.Combine(evidence, "recovery-tools.log"), null,
                        Path.Combine(evidence, "absent-passfile"), "-D", Path.Combine(restoredDirectory, "data"), "-m", "fast", "-w", "stop");
                Environment.SetEnvironmentVariable(variable, null);
                Environment.SetEnvironmentVariable("NexaErp__RecoveryBootstrapPassword", oldRecovery);
            }
        }

        async Task Control(string action, int port)
        {
            var processInfo = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-File", control, "-Action", action, "-DatabasePort", port.ToString() })
                processInfo.ArgumentList.Add(argument);
            using var process = Process.Start(processInfo)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await stdout + await stderr;
            await File.AppendAllTextAsync(Path.Combine(evidence, "container-control.log"), output);
            Assert.True(process.ExitCode == 0, "Disposable container control failed; see private witness log.");
        }
        async Task Ready()
        {
            using var client = KeycloakHttpClient(pin);
            var deadline = DateTimeOffset.UtcNow.AddMinutes(40);
            while (DateTimeOffset.UtcNow < deadline)
            {
                try
                {
                    using var response = await client.GetAsync(origin + "/realms/sess-approvers/.well-known/openid-configuration");
                    if (response.IsSuccessStatusCode) return;
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            throw new TimeoutException("PostgreSQL-backed Keycloak did not become ready.");
        }
        async Task<string> Keys()
        {
            using var client = KeycloakHttpClient(pin);
            var keys = new List<string>();
            foreach (var realm in new[] { "sess-staff", "sess-approvers" })
            {
                using var json = JsonDocument.Parse(await client.GetStringAsync(origin + "/realms/" + realm + "/protocol/openid-connect/certs"));
                keys.AddRange(json.RootElement.GetProperty("keys").EnumerateArray().Select(k => realm + ":" + k.GetRawText()));
            }
            return string.Join("\n", keys.Order(StringComparer.Ordinal));
        }
    }
}
#endif