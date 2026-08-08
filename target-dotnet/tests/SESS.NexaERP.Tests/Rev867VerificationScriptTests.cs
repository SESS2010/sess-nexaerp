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

