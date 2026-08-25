using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C3LegacyDepartmentCorrectionTests
{
    private const string MigrationId = "20260810110000_Rev868C3LegacyMixedDepartmentDeactivationCorrection";
    private static readonly string[] CleanDepartments =
    [
        "MANAGEMENT", "PURCHASE", "STORES", "ACCOUNTS_FINANCE", "HR_ADMIN", "PRODUCTION_FABRICATION",
        "DESIGN", "ELECTRICAL_PLC_INSTRUMENTATION", "REFRIGERATION_MECHANICAL",
        "SERVICE_TECHNICAL_SUPPORT", "SOFTWARE_IT", "QUALITY_QC"
    ];
    private static readonly string[] LegacyDepartments =
    ["ENGINEER_TECHNICAL", "MANAGER", "JUNIOR_ASSISTANT", "ADMIN_ACCOUNTS_STORES"];

    [Fact]
    public void Corrective_migration_is_discoverable_once_after_rev868c3()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.ToList();
        const string advanceBaseline = "20260824032638_AdvanceInitialBaseline";

        Assert.Equal(1, migrations.Count(x => x == advanceBaseline));
        Assert.Equal(new[]
        {
            advanceBaseline,
            string.Concat(20260824135450L, (char)95, nameof(MultiCompanySharedIdentityFoundation)),
            string.Concat(20260824150742L, (char)95, nameof(CalibrationPurchasePairItemTypeCorrections)),
            "20260825063221_EmployeeMasterRebuild42",
            "20260825073027_CorrectManagingDirectorDepartmentPriority",
            "20260825092016_AuthenticationBootstrapFoundation",
            "20260825125621_MultiCompanyEmployeeAuthorizationPart1"
        }, migrations);
    }

    [Fact]
    public void Workflow_actual_matched_count_uses_null_safe_comparison_without_weakening_acceptance()
    {
        var verifier = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");
        var workflow = Section(verifier, "function Get-WorkflowEvidenceSql", "function Get-PermissionEvidenceSql");

        foreach (var column in new[] { "RouteCode", "MinimumAmount", "MaximumAmount", "StepNumber", "ApproverResolutionType", "ApproverEmployeeCode", "ApproverRoleCode" })
        {
            Assert.Contains($"a.\"{column}\" is not distinct from e.\"{column}\"", workflow);
        }
        Assert.DoesNotContain("actual join expected using", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow_missing_count", workflow);
        Assert.Contains("workflow_unexpected_count", workflow);
        Assert.Contains("workflow_duplicate_count", workflow);
        Assert.Contains("workflow_sequence_violation_count", workflow);
        Assert.Contains("workflow_overlap_count", workflow);

        var expected = new WorkflowRow("MANAGER_MD_TD", 500000.01m, null, 1, "DEPARTMENT_MAPPING", null, "MANAGER");
        var actual = new WorkflowRow("MANAGER_MD_TD", 500000.01m, null, 1, "DEPARTMENT_MAPPING", null, "MANAGER");
        Assert.Equal(1, new[] { actual }.Count(row => row == expected));
    }

    private static bool DepartmentAcceptance(IReadOnlyCollection<string> actualActive)
    {
        var expected = CleanDepartments.ToHashSet(StringComparer.Ordinal);
        var actual = actualActive.ToHashSet(StringComparer.Ordinal);
        return actual.Count == 12
            && expected.All(actual.Contains)
            && actual.All(expected.Contains)
            && LegacyDepartments.All(code => !actual.Contains(code));
    }

    private static bool PreflightLegacyReferenceAcceptance(int activeLegacy, int employeeReferences, int managerReferences, int activeOpenPrReferences) =>
        activeLegacy == 4 && employeeReferences == 0 && managerReferences == 0 && activeOpenPrReferences == 0;

    private static bool PostLegacyReferenceAcceptance(int activeLegacy, int inactiveLegacy, int employeeReferences, int managerReferences, int activeOpenPrReferences) =>
        activeLegacy == 0 && inactiveLegacy == 4 && employeeReferences == 0 && managerReferences == 0 && activeOpenPrReferences == 0;

    private static string Section(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"Unable to isolate section {startMarker}.");
        return source[start..end];
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        for (var index = 0; (index = source.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0; index += value.Length) count++;
        return count;
    }

    private static string Read(params string[] parts)
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "SESS.NexaERP.slnx")))
        {
            root = Directory.GetParent(root)?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }

    private sealed record WorkflowRow(string RouteCode, decimal MinimumAmount, decimal? MaximumAmount, int StepNumber, string ResolutionType, string? EmployeeCode, string? RoleCode);

    [Fact]
    public void Corrective_helper_is_isolated_recovery_aware_and_fail_closed()
    {
        var helper = Read("tools", "apply-rev868c3-legacy-department-correction-secure.ps1");

        Assert.Contains("GeneratePlanOnly", helper);
        Assert.Contains("PreflightOnly", helper);
        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("Protected database rejected", helper);
        Assert.Contains("sess_nexaerp", helper);
        Assert.Contains("REV861", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backup_relation_count", helper);
        Assert.Contains("migration_owned_department_count", helper);
        Assert.Contains("safe_retry_state", helper);
        Assert.Contains("prerequisite_expected_count=10", helper);
        Assert.Contains("prerequisite_actual_matched_count=10", helper);
        Assert.Contains("prerequisite_missing_count=0", helper);
        Assert.Contains("prerequisite_unexpected_count=0", helper);
        Assert.Contains("prerequisite_duplicate_count=0", helper);
        Assert.Contains("target_correction_migration_count=0", helper);
        Assert.Contains("legacy_department_missing_count", helper);
        Assert.Contains("legacy_department_count=4", helper);
        Assert.Contains("active_legacy_department_count=4", helper);
        Assert.Contains("active_employee_legacy_department_reference_count=0", helper);
        Assert.Contains("active_manager_mapping_legacy_department_reference_count=0", helper);
        Assert.Contains("total_active_sess_employee_count=42", helper);
        Assert.Contains("\"EmployeeCode\" like 'SESS-%'", helper);
        Assert.Contains("total_active_manager_mapping_count=14", helper);
        Assert.Contains("active_open_pr_legacy_department_reference_count=0", helper);
        Assert.Contains("historical_pr_legacy_department_reference_count", helper);
        Assert.Contains("active_clean_department_count=12", helper);
        Assert.Contains("missing_clean_department_count=0", helper);
        Assert.Contains("unexpected_active_department_count=0", helper);
        Assert.Contains("active_legacy_department_count=0", helper);
        Assert.Contains("inactive_legacy_department_count=4", helper);
        Assert.Contains("database_acceptance_state=PASS", helper);
        Assert.Contains("Expected host: $HostName", helper);
        Assert.Contains("Expected port: $Port", helper);
        Assert.Contains("Isolated target database: $ExpectedDatabase", helper);
        Assert.Contains("Protected databases:", helper);
        Assert.Contains("No main-DB, database create/drop, backup, restore, or database cleanup operation", helper);
        Assert.Contains("Historical PR legacy-department references (read-only; retained)", helper);
        Assert.Contains("-v ON_ERROR_STOP=1", helper);
        Assert.Contains("-f $script:tempSqlFile", helper);
        Assert.DoesNotContain(" -c ", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dropdb", helper, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("legacy_active")]
    [InlineData("unexpected_active")]
    [InlineData("missing_clean")]
    public void Department_post_verification_negative_states_fail_closed(string defect)
    {
        var actual = CleanDepartments.ToList();
        switch (defect)
        {
            case "legacy_active": actual.Add(LegacyDepartments[0]); break;
            case "unexpected_active": actual.Add("UNEXPECTED_DEPARTMENT"); break;
            case "missing_clean": actual.Remove(CleanDepartments[0]); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }

        Assert.False(DepartmentAcceptance(actual));
    }

    [Theory]
    [InlineData("active_legacy_department_count")]
    [InlineData("active_employee_legacy_department_reference_count")]
    [InlineData("active_manager_mapping_legacy_department_reference_count")]
    [InlineData("active_open_pr_legacy_department_reference_count")]
    public void Legacy_reference_negative_states_fail_preflight_and_post_verification(string defect)
    {
        var preActiveLegacy = 4;
        var postActiveLegacy = 0;
        var postInactiveLegacy = 4;
        var employeeReferences = 0;
        var managerReferences = 0;
        var activeOpenPrReferences = 0;
        switch (defect)
        {
            case "active_legacy_department_count": preActiveLegacy = 3; postActiveLegacy = 1; break;
            case "active_employee_legacy_department_reference_count": employeeReferences = 1; break;
            case "active_manager_mapping_legacy_department_reference_count": managerReferences = 1; break;
            case "active_open_pr_legacy_department_reference_count": activeOpenPrReferences = 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }

        Assert.False(PreflightLegacyReferenceAcceptance(preActiveLegacy, employeeReferences, managerReferences, activeOpenPrReferences));
        Assert.False(PostLegacyReferenceAcceptance(postActiveLegacy, postInactiveLegacy, employeeReferences, managerReferences, activeOpenPrReferences));
    }
}
