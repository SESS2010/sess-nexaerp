namespace SESS.NexaERP.Tests;

public sealed class Rev867VerificationScriptTests
{
    [Theory]
    [InlineData("apply-rev867-secure.ps1")]
    [InlineData("resume-rev867-verification-secure.ps1")]
    public void Rev867_verification_scripts_initialize_report_variables(string scriptName)
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", scriptName));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("$testOutput = @()", script);
        Assert.Contains("$buildOutput = @()", script);
        Assert.Contains("$secretScanOutput", script);
        Assert.Contains("$databaseEvidence", script);
        Assert.Contains("$backupHash", script);
        Assert.Contains("Write-FailureReport", script);
    }

    [Fact]
    public void Rev867_resume_verifier_never_contains_migration_apply_command()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "resume-rev867-verification-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef database", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REV867 migration is missing. This resume verifier will not apply migrations.", script);
    }

    [Fact]
    public void Rev867_failure_report_simulation_succeeds_before_test_output_assignment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "rev867-report-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var reportFile = Path.Combine(tempDir, "failure.md");
            var testOutput = Array.Empty<string>();
            var buildOutput = Array.Empty<string>();
            var secretScanOutput = string.Empty;
            var backupFile = string.Empty;
            var backupHash = string.Empty;

            void AddReport(string text) => File.AppendAllText(reportFile, text + Environment.NewLine);
            void WriteFailureReport(string message)
            {
                AddReport("# REV867 Resume Verification Failed");
                AddReport("- Error: " + message);
                AddReport("- Backup path: " + backupFile);
                AddReport("- Backup SHA-256: " + backupHash);
                AddReport("- Build lines captured: " + buildOutput.Length);
                AddReport("- Test lines captured: " + testOutput.Length);
                AddReport("- Secret scan: " + secretScanOutput);
            }

            try
            {
                throw new InvalidOperationException("forced before tests");
            }
            catch (InvalidOperationException ex)
            {
                WriteFailureReport(ex.Message);
            }

            var report = File.ReadAllText(reportFile);
            Assert.Contains("forced before tests", report);
            Assert.Contains("Test lines captured: 0", report);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }


    [Fact]
    public void Rev867C1_readonly_diagnostic_never_contains_migration_or_destructive_commands()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "diagnose-rev867c1-readonly-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdb", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev867C1_readonly_diagnostic_contains_required_identity_and_history_probes()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "diagnose-rev867c1-readonly-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("sess_nexaerp_rev867c1_verify", script);
        Assert.Contains("current_database()", script);
        Assert.Contains("current_user", script);
        Assert.Contains("inet_server_addr()", script);
        Assert.Contains("inet_server_port()", script);
        Assert.Contains("pg_namespace", script);
        Assert.Contains("pg_class", script);
        Assert.Contains("c.relkind::text", script);
        Assert.Contains("Get-DiagnosticSql", script);
        Assert.DoesNotContain("to_regclass('", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("__EFMigrationsHistory", script);
        Assert.Contains("MigrationId", script);
        Assert.DoesNotContain("ConnectionStrings__NexaErp", script, StringComparison.OrdinalIgnoreCase);
    }



    [Fact]
    public void Rev867C1_generate_sql_only_outputs_complete_balanced_readonly_sql()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "diagnose-rev867c1-readonly-secure.ps1"));
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("-GenerateSqlOnly");

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(15000), "GenerateSqlOnly did not exit within 15 seconds.");
        Assert.Equal(0, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), error);

        Assert.DoesNotContain("Enter PostgreSQL password", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-- Session identity", output);
        Assert.Contains("select 'database=' || current_database()", output);
        Assert.Contains("current_user", output);
        Assert.Contains("inet_server_addr()", output);
        Assert.Contains("inet_server_port()", output);
        Assert.Contains("from pg_catalog.pg_namespace", output);
        Assert.Contains("from pg_catalog.pg_class c", output);
        Assert.Contains("c.relkind::text", output);
        Assert.Contains("where c.relname = '__EFMigrationsHistory'", output);
        Assert.Contains("where n.nspname = 'public' and c.relname = '__EFMigrationsHistory'", output);
        Assert.Contains("select \"MigrationId\"", output);
        Assert.Contains("from \"public\".\"__EFMigrationsHistory\"", output);
        Assert.DoesNotContain("to_regclass", output, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("select coalesce(to_regclass('", output, StringComparison.OrdinalIgnoreCase);

        var sqlLines = output.Split(Environment.NewLine).Where(line => !line.StartsWith("REV867C1 generated", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("-- ", StringComparison.Ordinal)).ToArray();
        var sqlText = string.Join(Environment.NewLine, sqlLines);
        Assert.Equal(0, sqlText.Count(ch => ch == '\'') % 2);
        Assert.True(output.Split(';').Length >= 6, "Expected multiple complete semicolon-terminated statements.");
    }

    [Fact]
    public void Design_time_factory_uses_secure_environment_connection_string()
    {
        var factoryPath = FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDesignTimeDbContextFactory.cs"));
        var source = File.ReadAllText(factoryPath);

        Assert.Contains("Environment.GetEnvironmentVariable(\"ConnectionStrings__NexaErp\")", source);
        Assert.Contains("NexaErp__ExpectedDatabase", source);
        Assert.Contains("throw new InvalidOperationException", source);
        Assert.Contains("NpgsqlConnectionStringBuilder", source);
        Assert.DoesNotContain("Host=localhost;Database=sess_nexaerp;Username=postgres", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Database=sess_nexaerp", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Design_time_factory_fails_closed_when_connection_string_is_absent()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp");
        var previousExpected = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", null);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", null);
            var factory = new SESS.NexaERP.Infrastructure.Persistence.NexaErpDesignTimeDbContextFactory();

            var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("ConnectionStrings__NexaErp", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", previousConnection);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", previousExpected);
        }
    }

    [Fact]
    public void Design_time_factory_accepts_only_environment_supplied_connection_string()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp");
        var previousExpected = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", "Host=localhost;Database=design_time_source_only;Username=postgres");
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", "design_time_source_only");
            var factory = new SESS.NexaERP.Infrastructure.Persistence.NexaErpDesignTimeDbContextFactory();

            using var db = factory.CreateDbContext([]);
            Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", previousConnection);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", previousExpected);
        }
    }



    [Fact]
    public void Design_time_factory_rejects_unexpected_database()
    {
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp");
        var previousExpected = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", "Host=localhost;Database=sess_nexaerp;Username=postgres");
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", "sess_nexaerp_rev867c1_verify");
            var factory = new SESS.NexaERP.Infrastructure.Persistence.NexaErpDesignTimeDbContextFactory();

            var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateDbContext([]));
            Assert.Contains("does not match", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__NexaErp", previousConnection);
            Environment.SetEnvironmentVariable("NexaErp__ExpectedDatabase", previousExpected);
        }
    }

    [Fact]
    public void Rev867C1_isolated_verification_helper_is_restricted_to_verification_database()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "apply-rev867c1-isolated-verification-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("sess_nexaerp_rev867c1_verify", script);
        Assert.Contains("NexaErp__ExpectedDatabase", script);
        Assert.Contains("This helper is permanently restricted to sess_nexaerp_rev867c1_verify on localhost:5432.", script);
        Assert.Contains("Refusing to run against main development database sess_nexaerp.", script);
        Assert.Contains("empty_and_safe", script);
        Assert.Contains("not_empty_or_wrong_target", script);
        Assert.DoesNotContain("Database=sess_nexaerp;", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE nexa", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev867C1_isolated_verification_helper_generate_sql_only_documents_preflight_and_apply_plan()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "apply-rev867c1-isolated-verification-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("GenerateSqlOnly", script);
        Assert.Contains("PreflightOnly", script);
        Assert.Contains("No password requested and no PostgreSQL connection attempted in this mode.", script);
        Assert.Contains("dotnet ef database update 20260808160435_Rev867C1Corrections", script);
        Assert.Contains("select 'database=' || current_database()", script);
        Assert.Contains("current_user", script);
        Assert.Contains("inet_server_addr()", script);
        Assert.Contains("inet_server_port()", script);
        Assert.Contains("current_database() = 'sess_nexaerp_rev867c1_verify'", script);
        Assert.Contains("where c.relname = '__EFMigrationsHistory'", script);
        Assert.Contains("select \"MigrationId\"", script);
        Assert.Contains("where \"MigrationId\" = '20260808160435_Rev867C1Corrections'", script);
        Assert.Contains("master_status_history", script);
        Assert.Contains("master_approval_history", script);
        Assert.Contains("audit_logs", script);
    }

    [Fact]
    public void Rev867C1_main_db_readonly_diagnostic_never_contains_migration_or_destructive_commands()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "diagnose-rev867c1-main-db-readonly-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdb", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev867C1_main_db_generate_sql_only_outputs_complete_balanced_readonly_sql()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "diagnose-rev867c1-main-db-readonly-secure.ps1"));
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        process.StartInfo.ArgumentList.Add("-NoProfile");
        process.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        process.StartInfo.ArgumentList.Add("Bypass");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("-GenerateSqlOnly");

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(15000), "Main-db GenerateSqlOnly did not exit within 15 seconds.");
        Assert.Equal(0, process.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), error);

        Assert.DoesNotContain("Enter PostgreSQL password", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-- Session identity", output);
        Assert.Contains("select 'database=' || current_database()", output);
        Assert.Contains("current_user", output);
        Assert.Contains("inet_server_addr()", output);
        Assert.Contains("inet_server_port()", output);
        Assert.Contains("from pg_catalog.pg_namespace", output);
        Assert.Contains("from pg_catalog.pg_class c", output);
        Assert.Contains("where c.relname = '__EFMigrationsHistory'", output);
        Assert.Contains("select \"MigrationId\"", output);
        Assert.Contains("where \"MigrationId\" = '20260808160435_Rev867C1Corrections'", output);
        Assert.DoesNotContain("to_regclass", output, StringComparison.OrdinalIgnoreCase);

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("sess_nexaerp", script);
        Assert.Contains("This diagnostic is restricted to sess_nexaerp on localhost:5432.", script);

        var sqlLines = output.Split(Environment.NewLine).Where(line => !line.StartsWith("REV867C1 main-db generated", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("-- ", StringComparison.Ordinal)).ToArray();
        var sqlText = string.Join(Environment.NewLine, sqlLines);
        Assert.Equal(0, sqlText.Count(ch => ch == '\'') % 2);
        Assert.True(output.Split(';').Length >= 6, "Expected multiple complete semicolon-terminated statements.");
    }

    [Fact]
    public void Rev867C1_isolated_resume_helper_never_applies_migrations_or_destructive_operations()
    {
        var scriptPath = FindTargetDotnetFile(Path.Combine("tools", "resume-rev867c1-isolated-verification-secure.ps1"));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("sess_nexaerp_rev867c1_verify", script);
        Assert.Contains("This resume helper is permanently restricted to sess_nexaerp_rev867c1_verify on localhost:5432.", script);
        Assert.Contains("Expected migration ID missing before resume verification", script);
        Assert.Contains("Rev867C1PostgresVerificationTests|Rev867MasterFoundationTests", script);
        Assert.Contains("Test stdout/stderr", script);
        Assert.DoesNotContain("database update", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef database", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdb", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER DATABASE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE nexa", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Database=sess_nexaerp;", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev867C1_postgres_scope_test_uses_deterministic_resume_safe_records()
    {
        var testPath = FindTargetDotnetFile(Path.Combine("tests", "SESS.NexaERP.Tests", "Rev867C1PostgresVerificationTests.cs"));
        var source = File.ReadAllText(testPath);

        Assert.Contains("REV867C1-SCOPE-CUST-A", source);
        Assert.Contains("REV867C1-SCOPE-CUST-B", source);
        Assert.Contains("REV867C1-SCOPE-VEND-A", source);
        Assert.Contains("REV867C1-SCOPE-VEND-B", source);
        Assert.Contains("UpsertCustomerAsync", source);
        Assert.Contains("UpsertVendorAsync", source);
        Assert.Contains("crossCustomerCount", source);
        Assert.Contains("crossVendorCount", source);
        Assert.DoesNotContain("StartsWith(\"C1-CUST-\" + run)", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StartsWith(\"C1-VEND-\" + run)", source, StringComparison.OrdinalIgnoreCase);
    }
    private static string FindTargetDotnetFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (directory.Name.Equals("target-dotnet", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }
}
