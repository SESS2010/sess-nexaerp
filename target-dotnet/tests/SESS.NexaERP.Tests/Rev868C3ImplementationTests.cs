using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C3ImplementationTests
{
    private const string MigrationId = "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation";

    [Fact]
    public void Rev868c3_migration_is_discoverable_after_rev868c2()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.ToList();

        Assert.Contains("20260809123000_Rev868C2DepartmentManagerApprovalMapping", migrations);
        Assert.Contains(MigrationId, migrations);
        Assert.True(migrations.IndexOf("20260809123000_Rev868C2DepartmentManagerApprovalMapping") < migrations.IndexOf(MigrationId));
        Assert.Equal(1, migrations.Count(x => x == MigrationId));
    }

    [Fact]
    public void Rev868c3_migration_contains_backup_tables_and_exact_rollback_guards()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var down = migration[migration.IndexOf("protected override void Down", StringComparison.Ordinal)..];

        Assert.Contains("rev868c3_employee_backup", migration);
        Assert.Contains("rev868c3_department_backup", migration);
        Assert.Contains("rev868c3_department_mapping_backup", migration);
        Assert.Contains("on conflict (\"EmployeeCode\") do update", migration);
        Assert.Contains("IX_employees_PayrollEmployeeId", migration);
        Assert.Contains("IsDateOfJoiningApproximate", migration);
        Assert.Contains(Rev868C3EmployeeWorkbookData.ActiveEmployees, x => x.EmployeeCode == "SESS-040" && x.EmployeeName == "NARREN VALENTINO" && x.DateOfJoining == new DateOnly(2026, 2, 1) && !x.DateOfJoiningAccuracy.StartsWith("Approximate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(Rev868C3EmployeeWorkbookData.ActiveEmployees, x => x.EmployeeCode == "SESS-049" && x.Gender == "Female" && x.PayrollEmployeeId == "1072");
        Assert.Contains("Gender\" = 'Female'", Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1"));
        Assert.Contains("rollback blocked: employee code integrity failure", down);
        Assert.DoesNotContain("Confidential statutory identifier", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sensitive identifier", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_helper_is_isolated_and_contains_recovery_aware_modes()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("sess_nexaerp", helper);
        Assert.Contains("template0", helper);
        Assert.Contains("template1", helper);
        Assert.Contains("REV861", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GeneratePlanOnly", helper);
        Assert.Contains("PreflightOnly", helper);
        Assert.Contains("ResumeVerifyOnly", helper);
        Assert.Contains(MigrationId, helper);
        Assert.Contains("safe_retry_state", helper);
        Assert.Contains("active_employee_codes_expected", helper);
        Assert.Contains("relieved_employee_codes_expected", helper);
        Assert.Contains("backup_relation_count", helper);
        Assert.Contains("department_history_partial_count", helper);
        Assert.Contains("role_assignment_partial_count", helper);
        Assert.Contains("role_page_permission_partial_count", helper);
        Assert.Contains("manager_mapping_rows", helper);
        Assert.Contains("workflow_step|range=500000.01-unbounded|sequence=3", helper);
        Assert.Contains("login_enabled_mismatch_count", helper);
        Assert.Contains("approval_status_mismatch_count", helper);
        Assert.DoesNotContain("C:\\Users\\User\\Documents\\Codex\\2026-07-03\\see\\target-dotnet\\local-evidence\\rev868c3\\SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx", helper);
    }

    [Theory]
    [InlineData(0, "MANAGER_ONLY", 1, null, null)]
    [InlineData(50000, "MANAGER_ONLY", 1, null, null)]
    [InlineData(50000.01, "MANAGER_MD", 2, "SESS-002", PurchaseRequisitionApprovalRoutes.ManagingDirector)]
    [InlineData(500000, "MANAGER_MD", 2, "SESS-002", PurchaseRequisitionApprovalRoutes.ManagingDirector)]
    [InlineData(500000.01, "MANAGER_MD_TD", 3, "SESS-001", PurchaseRequisitionApprovalRoutes.TechnicalDirector)]
    [InlineData(500001, "MANAGER_MD_TD", 3, "SESS-001", PurchaseRequisitionApprovalRoutes.TechnicalDirector)]
    public void Rev868c3_approval_workflow_boundaries_follow_manager_md_td_chain(decimal amount, string routeCode, int stepCount, string? finalApproverEmployee, string? finalRole)
    {
        var steps = PurchaseRequisitionEndpoints.ApprovalWorkflowFor(amount);

        Assert.All(steps, step => Assert.Equal(routeCode, step.RouteCode));
        Assert.Equal(stepCount, steps.Count);
        Assert.Equal(PurchaseApproverResolutionTypes.DepartmentMapping, steps[0].ApproverResolutionType);
        Assert.Equal(finalApproverEmployee, steps[^1].ApproverEmployeeCode);
        Assert.Equal(finalRole, steps[^1].ApproverRoleCode);
    }

    [Fact]
    public void Rev868c3_workbook_data_distinguishes_manikandan_records_and_unique_payroll_ids()
    {
        var employees = Rev868C3EmployeeWorkbookData.ActiveEmployees;
        var manikandan009 = Assert.Single(employees, x => x.EmployeeCode == "SESS-009");
        var manikandan030 = Assert.Single(employees, x => x.EmployeeCode == "SESS-030");

        Assert.Equal("1010", manikandan009.PayrollEmployeeId);
        Assert.Equal("1050", manikandan030.PayrollEmployeeId);
        Assert.NotEqual(manikandan009.EmployeeName, manikandan030.EmployeeName);

        var duplicatePayrollIds = employees
            .Where(x => !string.Equals(x.PayrollEmployeeId, "NA", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.PayrollEmployeeId, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();
        Assert.Empty(duplicatePayrollIds);
    }

    [Fact]
    public void Rev868c3_helper_preflight_safe_retry_requires_all_partial_artifacts_zero()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("rev868c3\\_%\\_backup", helper);
        Assert.Contains("status_history_partial_count", helper);
        Assert.Contains("department_history_partial_count", helper);
        Assert.Contains("audit_partial_count", helper);
        Assert.Contains("deterministic_employee_partial_count", helper);
        Assert.Contains("deterministic_department_partial_count", helper);
        Assert.Contains("deterministic_designation_partial_count", helper);
        Assert.Contains("safe_retry_state=' || case when prerequisite_history_count = 9", helper);
        Assert.Contains("backup_relation_count", helper);
        Assert.Contains("Backup SHA-256", helper);
    }

    private static string Read(params string[] relativeParts) => File.ReadAllText(Find(relativeParts));

    private static string Find(params string[] relativeParts)
    {
        var relativePath = Path.Combine(relativeParts);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(relativePath);
    }
}
