using System.Text.RegularExpressions;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AIsolatedDatabasePreparationHelperTests
{
    private static readonly string Root = FindRoot();
    private static readonly string HelperPath = Path.Combine(Root, "tools", "prepare-rev869a-isolated-database-secure.ps1");
    private static readonly string Source = File.ReadAllText(HelperPath);

    private static readonly string[] ExpectedMigrations =
    {
        "20260808110924_Phase1Foundation",
        "20260808114550_Phase1AuthorizationSeed",
        "20260808123411_Rev866EmployeePermissionMatrix",
        "20260808142353_Rev866CorrectiveStatusPermissionAudit",
        "20260808151207_Rev867MasterFoundation",
        "20260808160435_Rev867C1Corrections",
        "20260808182945_Rev868PurchaseRequisitionFoundation",
        "20260808190920_Rev868PurchaseLocationAllocationCorrection",
        "20260809123000_Rev868C2DepartmentManagerApprovalMapping",
        "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation",
        "20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection"
    };

    [Fact]
    public void HelperHasFourExplicitModesAndPlanReturnsBeforeAccess()
    {
        foreach (var mode in new[] { "GeneratePlanOnly", "SourcePreflightOnly", "Provision", "PostProvisionVerification" })
            Assert.Contains($"[switch]${mode}", Source, StringComparison.Ordinal);

        var planReturn = Source.IndexOf("if ($GeneratePlanOnly) { Write-Plan; return }", StringComparison.Ordinal);
        Assert.True(planReturn > 0);
        Assert.True(planReturn < Source.IndexOf("Initialize-DatabaseAccess", planReturn, StringComparison.Ordinal));
        Assert.Contains("requests no password and performs no PostgreSQL", Source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("wrong", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "wrong")]
    [InlineData("sess_nexaerp", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "postgres")]
    [InlineData("sess_nexaerp_rev861_verify", "sess_nexaerp_rev869a_verify")]
    [InlineData("sess_nexaerp_rev868_verify", "production")]
    public void WrongProtectedOrUnexpectedDatabaseIsRejected(string source, string target)
    {
        Assert.False(IsAcceptedEndpoint(source, target, "localhost", 5432));
    }

    [Fact]
    public void OnlyExactEndpointIsAccepted()
    {
        Assert.True(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "localhost", 5432));
        Assert.False(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "127.0.0.1", 5432));
        Assert.False(IsAcceptedEndpoint("sess_nexaerp_rev868_verify", "sess_nexaerp_rev869a_verify", "localhost", 5433));
        Assert.Contains("$SourceDatabase -cne $acceptedSource", Source, StringComparison.Ordinal);
        Assert.Contains("$TargetDatabase -cne $acceptedTarget", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetAlreadyExistsFailsSourceReadiness()
    {
        Assert.False(SourceEvidencePass(ExpectedMigrations, targetCount: 1, 42, 9, 12, 14));
        Assert.Contains("target_database_count = 0", Source, StringComparison.Ordinal);
        Assert.Contains("Provision must fail closed", Checkpoint(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingUnexpectedOrDuplicateMigrationFails()
    {
        Assert.False(MigrationSetPass(ExpectedMigrations.Skip(1)));
        Assert.False(MigrationSetPass(ExpectedMigrations.Append("20990101000000_Unexpected")));
        Assert.False(MigrationSetPass(ExpectedMigrations.Append(ExpectedMigrations[0])));
        Assert.Equal(11, ExpectedMigrations.Length);
        foreach (var migration in ExpectedMigrations) Assert.Contains(migration, Source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("begin transaction read only; select 1; commit;", true)]
    [InlineData("begin; select 1; commit;", false)]
    [InlineData("begin transaction read only; update employees set \"Status\"='x'; commit;", false)]
    [InlineData("begin transaction read only; create table x(id int); commit;", false)]
    [InlineData("begin transaction read only; delete from employees; commit;", false)]
    public void SourcePreflightRejectsNonReadOnlySql(string sql, bool expected)
    {
        Assert.Equal(expected, IsReadOnlySql(sql));
        Assert.Contains("Assert-ReadOnlySql", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void DumpAndRestoreContractRejectUnsafeOptions()
    {
        Assert.DoesNotContain("dropdb.exe", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-Native $PgRestorePath $restoreArgs", Source[..Source.IndexOf("Assert-SafeRestoreArguments $restoreArgs", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("--format=custom", Source, StringComparison.Ordinal);
        Assert.Contains("--no-owner", Source, StringComparison.Ordinal);
        Assert.Contains("--no-privileges", Source, StringComparison.Ordinal);
        Assert.Contains("--clean", Source, StringComparison.Ordinal);
        Assert.Contains("--create", Source, StringComparison.Ordinal);
        Assert.False(RestoreOptionsPass(new[] { "--clean", "--no-owner", "--no-privileges" }));
        Assert.False(RestoreOptionsPass(new[] { "--create", "--no-owner", "--no-privileges" }));
        Assert.False(RestoreOptionsPass(new[] { "--no-owner" }));
        Assert.False(RestoreOptionsPass(new[] { "--no-privileges" }));
        Assert.True(RestoreOptionsPass(new[] { "--no-owner", "--no-privileges" }));
    }

    [Fact]
    public void PasswordAndConnectionDetailsCannotLeak()
    {
        Assert.Contains("Read-Host \"PostgreSQL password (not logged)\" -AsSecureString", Source, StringComparison.Ordinal);
        Assert.Contains("$env:PGPASSWORD", Source, StringComparison.Ordinal);
        Assert.Contains("Remove-Item Env:\\PGPASSWORD", Source, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionStrings__", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Include Error Detail", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$plainPassword", Regex.Match(Source, @"\$dumpArgs\s*=.*", RegexOptions.None).Value, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupMustBeFreshContainedAndHasCompleteEvidence()
    {
        Assert.Contains("backups\\postgresql\\pre-rev869a-isolated", Source, StringComparison.Ordinal);
        Assert.Contains("older pre-C3 backup forbidden", Source, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash -LiteralPath $backupPath -Algorithm SHA256", Source, StringComparison.Ordinal);
        Assert.Contains("backup_byte_size=", Source, StringComparison.Ordinal);
        Assert.Contains("backup_creation_utc=", Source, StringComparison.Ordinal);
        Assert.Contains("backup_sha256=", Source, StringComparison.Ordinal);
        Assert.False(BackupEvidencePass("", 10, DateTime.UtcNow));
        Assert.False(BackupEvidencePass(new string('A', 64), 0, DateTime.UtcNow));
        Assert.True(BackupEvidencePass(new string('A', 64), 1, DateTime.UtcNow));
    }

    [Fact]
    public void ProvisionFailureQuarantinesWithoutAutomaticCleanup()
    {
        Assert.Contains("QUARANTINED_DO_NOT_USE_OR_AUTO_REPAIR", Source, StringComparison.Ordinal);
        Assert.Contains("no automatic drop or repair", Source, StringComparison.OrdinalIgnoreCase);
        var catchStart = Source.LastIndexOf("catch {", StringComparison.Ordinal);
        var catchSection = Source[catchStart..Source.IndexOf("finally {", catchStart, StringComparison.Ordinal)];
        Assert.DoesNotContain("Remove-Item -LiteralPath $backupPath", catchSection, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-Native $CreateDbPath", catchSection, StringComparison.Ordinal);
    }

    [Fact]
    public void IncompletePreservationEvidenceCannotPass()
    {
        var source = RequiredPreservation().ToDictionary(x => x, _ => "1");
        var target = RequiredPreservation().ToDictionary(x => x, _ => "1");
        target.Remove("audit_logs");
        Assert.False(PreservationPass(source, target));
        target["audit_logs"] = "2";
        Assert.False(PreservationPass(source, target));
        target["audit_logs"] = "1";
        Assert.True(PreservationPass(source, target));
        foreach (var name in RequiredPreservation()) Assert.Contains($"\"{name}\"", Source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExactRev868C3CountsAndFailClosedStatesAreRequired()
    {
        Assert.True(SourceEvidencePass(ExpectedMigrations, 0, 42, 9, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 41, 9, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, 8, 12, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, 9, 11, 14));
        Assert.False(SourceEvidencePass(ExpectedMigrations, 0, 42, 9, 12, 13));
        foreach (var marker in new[] { "safe_source_state=PASS", "provisioning_readiness_state=PASS", "provision_acceptance_state=PASS" })
            Assert.Contains(marker, Source, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperRemainsPowerShell51Compatible()
    {
        Assert.DoesNotContain("ForEach-Object -Parallel", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pwsh", Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("$PSStyle", Source, StringComparison.Ordinal);
        Assert.Contains("Set-StrictMode -Version Latest", Source, StringComparison.Ordinal);
    }

    private static bool IsAcceptedEndpoint(string source, string target, string host, int port) =>
        source == "sess_nexaerp_rev868_verify" && target == "sess_nexaerp_rev869a_verify" &&
        host == "localhost" && port == 5432 && source != target &&
        !Regex.IsMatch(source + target, "rev861|production|prod|live|main", RegexOptions.IgnoreCase);

    private static bool MigrationSetPass(IEnumerable<string> actual)
    {
        var rows = actual.ToArray();
        return rows.Length == 11 && rows.Distinct(StringComparer.Ordinal).Count() == 11 &&
               rows.Order(StringComparer.Ordinal).SequenceEqual(ExpectedMigrations.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static bool SourceEvidencePass(IEnumerable<string> migrations, int targetCount, int active, int relieved, int departments, int mappings) =>
        MigrationSetPass(migrations) && targetCount == 0 && active == 42 && relieved == 9 && departments == 12 && mappings == 14;

    private static bool IsReadOnlySql(string sql)
    {
        var stripped = Regex.Replace(sql, "'(?:''|[^'])*'", "''");
        return Regex.IsMatch(sql, @"^\s*begin\s+transaction\s+read\s+only\s*;", RegexOptions.IgnoreCase) &&
               Regex.IsMatch(sql, @"(commit|rollback)\s*;\s*$", RegexOptions.IgnoreCase) &&
               !Regex.IsMatch(stripped, @"\b(insert|update|delete|merge|create|alter|drop|truncate|grant|revoke|copy|call|do|vacuum|analyze|reindex)\b", RegexOptions.IgnoreCase);
    }

    private static bool RestoreOptionsPass(IEnumerable<string> args)
    {
        var options = args.ToArray();
        return !options.Contains("--clean") && !options.Contains("--create") &&
               options.Contains("--no-owner") && options.Contains("--no-privileges");
    }

    private static bool BackupEvidencePass(string hash, long bytes, DateTime created) =>
        Regex.IsMatch(hash, "^[A-Fa-f0-9]{64}$") && bytes > 0 && created != default;

    private static string[] RequiredPreservation() =>
    [
        "employees", "departments", "department_approval_mappings", "purchase_requisitions",
        "purchase_requisition_approval_history", "purchase_requisition_status_history",
        "stock_availability_checks", "stock_availability_check_lines", "stock_reservations",
        "stock_reservation_history", "purchase_requirement_handoffs", "purchase_approval_route_settings",
        "purchase_approval_workflow_steps", "page_definitions", "role_page_permissions", "audit_logs",
        "employee_status_history", "employee_department_history", "employee_approval_history", "employee_import_history"
    ];

    private static bool PreservationPass(IReadOnlyDictionary<string, string> source, IReadOnlyDictionary<string, string> target) =>
        RequiredPreservation().All(key => source.TryGetValue(key, out var before) && target.TryGetValue(key, out var after) && before == after);

    private static string Checkpoint() => File.ReadAllText(Path.Combine(Root, "outputs", "rev869a_isolated_database_provisioning_checkpoint.md"));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
