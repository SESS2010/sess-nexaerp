using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
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
        Assert.Contains("DotnetEfPath", helper);
        Assert.Contains("Resolve-DotnetEfInvocation", helper);
        Assert.Contains("Test-DotnetEfTool", helper);
        Assert.Contains("Invoke-DotnetEfTool", helper);
        Assert.Contains("Assert-SdkStyleProject", helper);
        Assert.Contains("Test-EfProjectMetadata", helper);
        Assert.Contains("Get-EfProjectArgs", helper);
        Assert.Contains(MigrationId, helper);
        Assert.Contains("safe_retry_state", helper);
        Assert.Contains("active_employee_codes_expected", helper);
        Assert.Contains("relieved_employee_codes_expected", helper);
        Assert.Contains("backup_relation_count", helper);
        Assert.Contains("department_history_relation_count", helper);
        Assert.Contains("role_assignment_partial_count", helper);
        Assert.Contains("role_page_permission_partial_count", helper);
        Assert.Contains("manager_mapping_rows", helper);
        Assert.Contains("missing_mapping_count", helper);
        Assert.Contains("unexpected_mapping_count", helper);
        Assert.Contains("duplicate_mapping_count", helper);
        Assert.Contains("mapping_acceptance_state", helper);
        Assert.Contains("purchase_approval_workflow_steps", helper);
        Assert.Contains("workflow_missing_count", helper);
        Assert.Contains("workflow_unexpected_count", helper);
        Assert.Contains("workflow_duplicate_count", helper);
        Assert.Contains("workflow_sequence_violation_count", helper);
        Assert.Contains("workflow_acceptance_state", helper);
        Assert.Contains("login_enabled_mismatch_count", helper);
        Assert.Contains("approval_status_mismatch_count", helper);
        Assert.Contains("Get-TestResultSummary", helper);
        Assert.Contains("Assert-RequiredTargetedTestsPassed", helper);
        Assert.Contains("Rev868c3_unauthenticated_request_returns_401", helper);
        Assert.Contains("Rev868c3_unauthorized_role_returns_403", helper);
        Assert.Contains("Rev868c3_creator_self_approval_returns_403", helper);
        Assert.Contains("Rev868c3_duplicate_approver_is_prevented", helper);
        Assert.Contains("Rev868c3_missing_department_manager_fails_closed", helper);
        Assert.Contains("Rev868c3_manager_md_td_approval_sequence_is_enforced", helper);
        Assert.Contains("database_acceptance_state", helper);
        Assert.Contains("test_acceptance_state", helper);
        Assert.Contains("overall_acceptance_state=PASS", helper);
        Assert.Contains("Full execution final report requires database_acceptance_state=PASS, test_acceptance_state=PASS, and overall_acceptance_state=PASS", helper);
        Assert.DoesNotContain("C:\\Users\\User\\Documents\\Codex\\2026-07-03\\see\\target-dotnet\\local-evidence\\rev868c3\\SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx", helper);
    }

    [Fact]
    public void Rev868c3_helper_post_verification_uses_full_data_sets_and_no_static_test_evidence()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");
        var postStart = helper.IndexOf("function Get-PostMigrationSql", StringComparison.Ordinal);
        var postEnd = helper.IndexOf("function Get-TestResultSummary", postStart, StringComparison.Ordinal);
        var post = helper[postStart..postEnd];

        Assert.Contains("DESIGN:PROJECT:SESS-019:SESS-015", helper);
        Assert.Contains("DESIGN:REGULAR_PRODUCT:SESS-015:SESS-019", helper);
        Assert.Contains("LegacyMixedDepartmentCodes", helper);
        Assert.Contains("legacy_mixed_department_active_count", post);
        Assert.Contains("actual(code) as (select \"Code\" from nexa.departments where \"IsActive\" = true)", post);
        Assert.Contains("actual(code) as (select \"EmployeeCode\" from nexa.employees where \"EmployeeCode\" like 'SESS-%' and lower(\"Status\")", post);
        Assert.Contains("where m.\"ApprovalRouteCode\" = 'MANAGER' and m.\"IsActive\" = true", post);
        Assert.DoesNotContain("m.\"CreatedBy\" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'", post);
        Assert.Contains("where \"IsActive\" = true and \"RouteCode\" in ('MANAGER_ONLY','MANAGER_MD','MANAGER_MD_TD')", post);
        Assert.DoesNotContain("select 'targeted_test|", post);
        Assert.Contains("status_history_missing_employee_count", post);
        Assert.Contains("department_transfer_history_missing_employee_count", post);
        Assert.Contains("manager_role_missing_count", post);
        Assert.Contains("manager_permission_rows=", post);
        Assert.Contains("manager_permission_rows_expected", post);
        Assert.Contains("manager_permission_missing_count", post);
        Assert.Contains("manager_permission_unexpected_count", post);
        Assert.Contains("manager_permission_duplicate_count", post);
        Assert.Contains("manager_permission_acceptance_state", post);
        Assert.DoesNotContain("manager_permission_missing_count=' || case when count(*) > 0", post);
        Assert.DoesNotContain("manager_permission_required_count", post);
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
    public void Rev868c3_helper_resolves_dotnet_ef_before_password_and_backup()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        var projectCheckIndex = helper.IndexOf("Assert-SdkStyleProject $InfrastructureProject", StringComparison.Ordinal);
        var startupCheckIndex = helper.IndexOf("Assert-SdkStyleProject $StartupProject", StringComparison.Ordinal);
        var resolveIndex = helper.IndexOf("Resolve-DotnetEfInvocation $script:dotnetExe $DotnetEfPath", StringComparison.Ordinal);
        var versionCheckIndex = helper.IndexOf("Test-DotnetEfTool", resolveIndex, StringComparison.Ordinal);
        var metadataIndex = helper.IndexOf("Test-EfProjectMetadata", versionCheckIndex, StringComparison.Ordinal);
        var passwordIndex = helper.IndexOf("Read-Host -AsSecureString", StringComparison.Ordinal);
        var backupIndex = helper.IndexOf("Find-ExistingValidPreC3Backup", passwordIndex, StringComparison.Ordinal);
        var updateIndex = helper.IndexOf("    Invoke-EfDatabaseUpdateSanitized", backupIndex, StringComparison.Ordinal);

        Assert.True(projectCheckIndex > 0);
        Assert.True(startupCheckIndex > projectCheckIndex);
        Assert.True(resolveIndex > startupCheckIndex);
        Assert.True(versionCheckIndex > resolveIndex);
        Assert.True(metadataIndex > versionCheckIndex);
        Assert.True(passwordIndex > metadataIndex);
        Assert.True(backupIndex > passwordIndex);
        Assert.True(updateIndex > backupIndex);
        Assert.Contains("dotnet-ef tooling is unavailable", helper);
        Assert.Contains("No password was requested and no backup/migration was attempted", helper);
        Assert.Contains("EF project metadata/migration discovery failed before password prompt", helper);
        Assert.Contains("migrations','list','--no-connect", helper);
        Assert.Contains("SESS.NexaERP.Infrastructure.csproj", helper);
        Assert.Contains("SESS.NexaERP.Api.csproj", helper);
        Assert.Contains("--framework", helper);
        Assert.Contains("net10.0", helper);
        Assert.Contains("--configuration", helper);
        Assert.Contains("Release", helper);
        Assert.Contains("NexaErpDbContext", helper);
        Assert.Contains(".nuget\\packages\\dotnet-ef", helper);
        Assert.DoesNotContain("$script:dotnetExe ef database update", helper);
        Assert.Contains("Invoke-DotnetEfTool (@('database','update',$MigrationName) + (Get-EfProjectArgs))", helper);
        Assert.DoesNotContain("--project','.\\SESS.NexaERP.slnx", helper);
    }

    [Fact]
    public void Rev868c3_helper_accepts_nuget_dotnet_ef_dll_only_as_dotnet_exec()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("if ($leaf -eq 'dotnet-ef.dll')", helper);
        Assert.Contains(".nuget\\packages\\dotnet-ef", helper);
        Assert.Contains("dotnet-ef.dll must be under approved NuGet package root", helper);
        Assert.Contains("return [pscustomobject]@{ Mode = 'DotnetExec'; Command = $resolved }", helper);
        Assert.Contains("if ($script:dotnetEfInvocation.Mode -eq 'DotnetExec') { & $script:dotnetExe exec $script:dotnetEfInvocation.Command @EfArgs; return }", helper);
        Assert.Contains("C:\\Users\\User\\.nuget\\", CorrectedPreflightCommand());
    }

    [Fact]
    public void Rev868c3_helper_rejects_unapproved_dotnet_ef_paths_and_keeps_exe_mode()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("if ($leaf -eq 'dotnet-ef.exe') { return [pscustomobject]@{ Mode = 'Executable'; Command = $resolved } }", helper);
        Assert.Contains("throw \"Invalid dotnet-ef executable name: $leaf\"", helper);
        Assert.Contains("path traversal is rejected", helper);
        Assert.DoesNotContain("dotnet-ef.cmd", helper);
        Assert.DoesNotContain("unrelated.dll", helper);
    }

    [Fact]
    public void Rev868c3_helper_validates_ef10_and_discovers_migration_before_password_backup()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        var resolveIndex = helper.IndexOf("Resolve-DotnetEfInvocation $script:dotnetExe $DotnetEfPath", StringComparison.Ordinal);
        var versionIndex = helper.IndexOf("Test-DotnetEfTool", resolveIndex, StringComparison.Ordinal);
        var metadataIndex = helper.IndexOf("Test-EfProjectMetadata", versionIndex, StringComparison.Ordinal);
        var passwordIndex = helper.IndexOf("Read-Host -AsSecureString", StringComparison.Ordinal);
        var backupIndex = helper.IndexOf("Find-ExistingValidPreC3Backup", passwordIndex, StringComparison.Ordinal);

        Assert.True(resolveIndex > 0);
        Assert.True(versionIndex > resolveIndex);
        Assert.True(metadataIndex > versionIndex);
        Assert.True(passwordIndex > metadataIndex);
        Assert.True(backupIndex > passwordIndex);
        Assert.Contains("dotnet-ef version is not compatible with EF Core 10", helper);
        Assert.Contains("\\b10\\.\\d+\\.\\d+\\b", helper);
        Assert.Contains("migrations','list','--no-connect", helper);
        Assert.Contains(MigrationId, helper);
    }

    private static string CorrectedPreflightCommand() => "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"C:\\Users\\User\\Documents\\Codex\\2026-07-03\\see\\target-dotnet\\tools\\apply-rev868c3-employee-reconciliation-secure.ps1\" -GitPath \"C:\\Users\\User\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\native\\git\\cmd\\git.exe\" -DotnetEfPath \"C:\\Users\\User\\.nuget\\packages\\dotnet-ef\\10.0.10\\tools\\net8.0\\any\\dotnet-ef.dll\" -PreflightOnly";
    [Fact]
    public void Rev868c3_helper_preflight_safe_retry_requires_all_partial_artifacts_zero()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("rev868c3\\_%\\_backup", helper);
        Assert.Contains("status_history_partial_count", helper);
        Assert.Contains("department_history_relation_count", helper);
        Assert.Contains("workflow_step_relation_count", helper);
        Assert.Contains("audit_partial_count", helper);
        Assert.Contains("deterministic_employee_partial_count", helper);
        Assert.Contains("deterministic_department_partial_count", helper);
        Assert.Contains("deterministic_designation_partial_count", helper);
        Assert.Contains("safe_retry_state=' || case when prerequisite_history_count = 9", helper);
        Assert.Contains("workflow_step_relation_count = 0", helper);
        Assert.Contains("rev868c3_owned_index_artifact_count = 0", helper);
        Assert.Contains("conflict_duplicate_total_count = 0", helper);
        Assert.Contains("mapping_nullable_conflict_column_state = 'PASS'", helper);
        Assert.Contains("workflow_nullable_conflict_column_state = 'PASS'", helper);
        Assert.Contains("backup_relation_count", helper);
        Assert.Contains("Backup SHA-256", helper);
        Assert.Contains("Find-ExistingValidPreC3Backup", helper);
        Assert.Contains("Existing non-zero pre-C3 backup reused", helper);
    }

    [Fact]
    public void Rev868c3_helper_preflight_does_not_query_missing_department_history_table()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");
        var preflightStart = helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal);
        var preflightEnd = helper.IndexOf("function Get-PostMigrationSql", preflightStart, StringComparison.Ordinal);
        var preflight = helper[preflightStart..preflightEnd];

        Assert.Contains("department_history_relation_count", preflight);
        Assert.Contains("workflow_step_relation_count", preflight);
        Assert.DoesNotContain("nexa.employee_department_history", preflight, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("case when ac.workflow_step_relation_count = 0 then 0 else", preflight);
        Assert.Contains("query_to_xml('select count(*) as c from (select \"RouteCode\", \"StepNumber\", \"EffectiveFrom\"", preflight);
        Assert.Contains("department_history_relation_count = 0", preflight);
        Assert.Contains("workflow_step_relation_count = 0", preflight);
    }

    [Fact]
    public void Rev868c3_helper_preflight_reports_conflict_duplicates_and_owned_index_artifacts()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");
        var preflightStart = helper.IndexOf("function Get-PreflightSql", StringComparison.Ordinal);
        var preflightEnd = helper.IndexOf("function Get-PostMigrationSql", preflightStart, StringComparison.Ordinal);
        var preflight = helper[preflightStart..preflightEnd];

        var duplicateKeys = new[]
        {
            "departments_code_duplicate_count",
            "designations_code_duplicate_count",
            "employees_employee_code_duplicate_count",
            "roles_code_duplicate_count",
            "role_page_permissions_duplicate_count",
            "employee_role_assignments_duplicate_count",
            "department_approval_mappings_duplicate_count",
            "purchase_approval_workflow_steps_duplicate_count"
        };
        foreach (var key in duplicateKeys)
        {
            Assert.Contains(key, preflight);
        }

        var ownedIndexes = new[]
        {
            "ux_rev868c3_conflict_departments_code_count",
            "ux_rev868c3_conflict_designations_code_count",
            "ux_rev868c3_conflict_employees_employee_code_count",
            "ux_rev868c3_conflict_roles_code_count",
            "ux_rev868c3_conflict_role_page_permissions_count",
            "ux_rev868c3_conflict_employee_role_assignments_count",
            "ux_rev868c3_conflict_department_approval_mappings_count",
            "ux_rev868c3_conflict_purchase_approval_workflow_steps_count"
        };
        foreach (var key in ownedIndexes)
        {
            Assert.Contains(key, preflight);
        }

        Assert.Contains("rev868c3_owned_index_artifact_count", preflight);
        Assert.Contains("conflict_duplicate_total_count", preflight);
        Assert.Contains("conflict_duplicate_state=' || case when conflict_duplicate_total_count = 0 then 'PASS' else 'FAIL' end", preflight);
        Assert.Contains("query_to_xml('select count(*) as c from (select \"DepartmentId\", \"ApprovalRouteCode\", \"Scope\", \"EffectiveFrom\"", preflight);
        Assert.Contains("query_to_xml('select count(*) as c from (select \"RouteCode\", \"StepNumber\", \"EffectiveFrom\"", preflight);
        Assert.Contains("mapping_nullable_conflict_column_state", preflight);
        Assert.Contains("workflow_nullable_conflict_column_state", preflight);
    }

    [Fact]
    public void Rev868c3_down_removes_only_migration_owned_conflict_indexes()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var start = migration.IndexOf("private static string DropConflictIndexesSql", StringComparison.Ordinal);
        var end = migration.IndexOf("private static string BuildUpsertSql", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var dropBlock = migration[start..end];

        var ownedIndexes = new[]
        {
            "UX_rev868c3_conflict_departments_code",
            "UX_rev868c3_conflict_designations_code",
            "UX_rev868c3_conflict_employees_employee_code",
            "UX_rev868c3_conflict_roles_code",
            "UX_rev868c3_conflict_role_page_permissions",
            "UX_rev868c3_conflict_employee_role_assignments",
            "UX_rev868c3_conflict_department_approval_mappings",
            "UX_rev868c3_conflict_purchase_approval_workflow_steps"
        };
        foreach (var index in ownedIndexes)
        {
            Assert.Contains($"drop index if exists nexa.\"{index}\"", dropBlock);
        }

        Assert.DoesNotContain("IX_", dropBlock, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop index if exists nexa.\"PK_", dropBlock, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_migration_persists_approval_workflow_steps()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("purchase_approval_workflow_steps", migration);
        Assert.Contains("MANAGER_ONLY", migration);
        Assert.Contains("MANAGER_MD", migration);
        Assert.Contains("MANAGER_MD_TD", migration);
        Assert.Contains("SESS-002", migration);
        Assert.Contains("SESS-001", migration);
        Assert.Contains("FIXED_EMPLOYEE_ROLE", migration);
    }

    [Fact]
    public void Rev868c3_migration_seeds_exact_department_manager_page_permissions()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3_DEPARTMENT_MANAGER_PERMISSION", migration);
        Assert.Contains("insert into nexa.role_page_permissions", migration);
        Assert.Contains("p.\"PageKey\" = 'purchase.requisitions'", migration);
        Assert.Contains("p.\"PageKey\" = 'purchase.requisition-approvals'", migration);
        Assert.Contains("on conflict (\"RoleId\", \"PageDefinitionId\") do update", migration);
        Assert.Contains("CanApprove", migration);
        Assert.Contains("CanReject", migration);
        Assert.Contains("CanRequestClarification", migration);
        Assert.Contains("CanRequestRevision", migration);
        Assert.Contains("CanViewAuditHistory", migration);
    }


    [Fact]
    public void Rev868c3_designation_insert_satisfies_not_null_is_active_contract()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("{Sql(designation)}, true, TIMESTAMPTZ", migration);
        Assert.Contains("\"IsActive\" = true", migration);
        Assert.DoesNotContain("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"CreatedAt\"", migration);
    }

    [Fact]
    public void Rev868c3_migration_insert_schema_contracts_include_required_columns()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("insert into nexa.departments (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.designations (\"Id\", \"Code\", \"Name\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employees (\"Id\", \"EmployeeCode\", \"PayrollEmployeeId\", \"EmployeeName\", \"OriginalImportedName\", \"Gender\", \"Qualification\", \"DateOfBirth\", \"EmployeeType\", \"Grade\", \"DepartmentId\", \"DesignationId\", \"Status\", \"DateOfJoining\", \"DateOfJoiningAccuracy\", \"IsDateOfJoiningApproximate\", \"ApproximateDateNote\", \"FunctionalResponsibility\", \"WorkLocation\", \"ManagerScope\", \"LegacyDepartment\", \"OfficialEmail\", \"MobileNumber\", \"LoginEnabled\", \"ApprovalStatus\", \"IsEmployeeCodeLocked\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.roles (\"Id\", \"Code\", \"Name\", \"IsPrivileged\", \"IsActive\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.role_page_permissions (\"Id\", \"RoleId\", \"PageDefinitionId\", \"CanView\", \"CanCreate\", \"CanUpdate\", \"CanSubmit\", \"CanVerify\", \"CanApprove\", \"CanReject\", \"CanRequestClarification\", \"CanRequestRevision\", \"CanResubmit\", \"CanCancel\", \"CanDeactivate\", \"CanPrint\", \"CanDownload\", \"CanExport\", \"CanUploadAttachment\", \"CanReplaceAttachment\", \"CanViewCommercialValues\", \"CanViewAuditHistory\", \"HasFullControl\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_role_assignments (\"Id\", \"EmployeeId\", \"RoleId\", \"EffectiveFrom\", \"EffectiveTo\", \"ApprovalStatus\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.department_approval_mappings (\"Id\", \"DepartmentId\", \"ApprovalRouteCode\", \"Scope\", \"PrimaryApproverEmployeeId\", \"AlternateApproverEmployeeId\", \"EffectiveFrom\", \"EffectiveTo\", \"IsActive\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.purchase_approval_workflow_steps (\"Id\", \"RouteCode\", \"MinimumAmount\", \"MaximumAmount\", \"StepNumber\", \"ApproverResolutionType\", \"ApproverEmployeeCode\", \"ApproverRoleCode\", \"IsActive\", \"EffectiveFrom\", \"EffectiveTo\", \"Remarks\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_status_history (\"Id\", \"EmployeeId\", \"OldStatus\", \"NewStatus\", \"Reason\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.employee_department_history (\"Id\", \"EmployeeId\", \"PreviousDepartmentId\", \"NewDepartmentId\", \"Reason\", \"SourceRevision\", \"CorrelationId\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
        Assert.Contains("insert into nexa.audit_logs (\"Id\", \"Action\", \"AfterJson\", \"BeforeJson\", \"CorrelationId\", \"CreatedAt\", \"CreatedBy\", \"EntityId\", \"EntityName\", \"IpAddress\", \"Module\", \"Result\", \"UpdatedAt\", \"UpdatedBy\", \"UserLoginId\", \"Version\")", migration);
        Assert.DoesNotContain("\"UserRole\"", migration);
        Assert.DoesNotContain("\"OldValue\"", migration);
        Assert.DoesNotContain("\"NewValue\"", migration);
    }

    [Fact]
    public void Rev868c3_raw_sql_inserts_supply_all_required_model_columns()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var suppliedColumnsByTable = ParseRawInsertColumns(migration);
        var targetTables = new[]
        {
            "departments",
            "designations",
            "employees",
            "roles",
            "role_page_permissions",
            "employee_role_assignments",
            "department_approval_mappings",
            "purchase_approval_workflow_steps",
            "employee_status_history",
            "employee_department_history",
            "audit_logs"
        };

        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=sess_nexaerp_rev868_design_only;Username=design_only")
            .Options;
        using var db = new NexaErpDbContext(options);
        var reportRows = new List<string>();

        foreach (var table in targetTables)
        {
            Assert.True(suppliedColumnsByTable.TryGetValue(table, out var suppliedColumns), $"No REV868C3 raw INSERT columns found for nexa.{table}");
            var requiredColumns = RequiredRawInsertColumns(db, table);
            var missing = requiredColumns.Except(suppliedColumns, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            reportRows.Add($"nexa.{table} | {string.Join(',', requiredColumns.Order(StringComparer.Ordinal))} | {string.Join(',', suppliedColumns.Order(StringComparer.Ordinal))} | {string.Join(',', missing)}");
            Assert.Empty(missing);
        }

        Assert.Contains(reportRows, row => row.StartsWith("nexa.roles |", StringComparison.Ordinal) && row.Contains("IsPrivileged", StringComparison.Ordinal));
    }

    [Fact]
    public void Rev868c3_department_manager_role_is_least_privilege_and_post_verified()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("'DEPARTMENT_MANAGER', 'Department Manager', false, true", migration);
        Assert.Contains("\"IsPrivileged\" = false", migration);
        Assert.Contains("department_manager_role_state", helper);
        Assert.Contains("manager_role_state_ok", helper);
        Assert.Contains("HasFullControl", helper);
        Assert.Contains("FC=F", helper);
    }


    [Fact]
    public void Rev868c3_employee_reconciliation_uses_actual_master_ids_from_natural_key_lookups()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3 missing department lookup for employee reconciliation", migration);
        Assert.Contains("REV868C3 missing designation lookup for employee reconciliation", migration);
        Assert.Contains("from (values {Values(Rev868C3EmployeeWorkbookData.Departments.Select(x => x.Code))})", migration);
        Assert.Contains("from (values {Values(designations.Select(Code))})", migration);
        Assert.Contains("select '{Id(\"employee\", employee.EmployeeCode)}'", migration);
        Assert.Contains("from nexa.departments d", migration);
        Assert.Contains("join nexa.designations g on g.\"Code\" = {Sql(Code(employee.HrDesignation))}", migration);
        Assert.Contains("where d.\"Code\" = {Sql(employee.FinalDepartmentCode)}", migration);
        Assert.Contains("d.\"Id\", g.\"Id\", 'Active'", migration);
        Assert.DoesNotContain("'{Id(\"department\", employee.FinalDepartmentCode)}'", migration);
        Assert.DoesNotContain("'{Id(\"designation\", employee.HrDesignation)}'", migration);
    }

    [Fact]
    public void Rev868c3_role_and_permission_reconciliation_uses_actual_role_and_page_ids()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("REV868C3 missing DEPARTMENT_MANAGER role lookup", migration);
        Assert.Contains("REV868C3 missing page lookup for department manager permissions", migration);
        Assert.Contains("from nexa.roles r join nexa.page_definitions p on p.\"PageKey\" = 'purchase.requisitions'", migration);
        Assert.Contains("from nexa.roles r join nexa.page_definitions p on p.\"PageKey\" = 'purchase.requisition-approvals'", migration);
        Assert.Contains("select '{Id(\"rev868c3-department-manager-role\", employeeCode)}', e.\"Id\", r.\"Id\"", migration);
        Assert.Contains("join nexa.roles r on r.\"Code\" = 'DEPARTMENT_MANAGER'", migration);
        Assert.DoesNotContain("e.\"Id\", '{Id(\"role\", \"department_manager\")}'", migration);
    }

    [Fact]
    public void Rev868c3_fk_sources_are_audited_for_natural_key_or_persisted_row_lookup()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.Contains("join nexa.employees p on p.\"EmployeeCode\" = {Sql(mapping.PrimaryManagerCode)}", migration);
        Assert.Contains("join nexa.employees a on a.\"EmployeeCode\" = {Sql(mapping.AlternateManagerCode)}", migration);
        Assert.Contains("where d.\"Code\" = {Sql(mapping.DepartmentCode)}", migration);
        Assert.Contains("select gen_random_uuid(), e.\"Id\", b.\"DepartmentId\", e.\"DepartmentId\"", migration);
        Assert.Contains("left join nexa.rev868c3_employee_backup b on b.\"EmployeeId\" = e.\"Id\"", migration);
        Assert.Contains("Values(IEnumerable<string> values)", migration);
    }
    [Fact]
    public void Rev868c3_upsert_sql_runs_after_scope_aware_mapping_index_creation()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        var addScope = migration.IndexOf("AddColumn<string>(name: \"Scope\"", StringComparison.Ordinal);
        var dropOldIndex = migration.IndexOf("DropIndex(name: \"IX_department_approval_mappings_DepartmentId_ApprovalRouteCod\"", StringComparison.Ordinal);
        var createScopeUniqueIndex = migration.IndexOf("IX_department_approval_mappings_DepartmentId_Route_Scope_From", StringComparison.Ordinal);
        var createScopeActiveIndex = migration.IndexOf("IX_department_approval_mappings_DepartmentId_Route_Scope_Active", StringComparison.Ordinal);
        var upsert = migration.IndexOf("migrationBuilder.Sql(BuildUpsertSql());", StringComparison.Ordinal);

        Assert.True(addScope > 0);
        Assert.True(dropOldIndex > addScope);
        Assert.True(createScopeUniqueIndex > dropOldIndex);
        Assert.True(createScopeActiveIndex > createScopeUniqueIndex);
        Assert.True(upsert > createScopeActiveIndex);
    }

    [Fact]
    public void Rev868c3_offline_sql_places_every_on_conflict_after_matching_unique_support()
    {
        var sql = Read("outputs", "rev868c3_employee_reconciliation_idempotent.sql");

        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_departments_Code\"", "on conflict (\"Code\") do update set \"Name\" = excluded.\"Name\", \"IsActive\" = true");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_designations_Code\"", "on conflict (\"Code\") do update set \"Name\" = excluded.\"Name\", \"IsActive\" = true");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_employees_EmployeeCode\"", "on conflict (\"EmployeeCode\") do update");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_roles_Code\"", "on conflict (\"Code\") do update set \"Name\" = excluded.\"Name\", \"IsPrivileged\" = false");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_role_page_permissions_RoleId_PageDefinitionId\"", "on conflict (\"RoleId\", \"PageDefinitionId\") do update");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_employee_role_assignments_EmployeeId_RoleId_EffectiveFrom\"", "on conflict (\"EmployeeId\", \"RoleId\", \"EffectiveFrom\") do nothing");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_department_approval_mappings_DepartmentId_Route_Scope_From\"", "on conflict (\"DepartmentId\", \"ApprovalRouteCode\", \"Scope\", \"EffectiveFrom\") do update");
        AssertOrdered(sql, "CREATE UNIQUE INDEX \"IX_purchase_approval_workflow_steps_RouteCode_StepNumber_EffectiveFrom\"", "on conflict (\"RouteCode\", \"StepNumber\", \"EffectiveFrom\") do update");
    }

    [Fact]
    public void Rev868c3_offline_sql_every_targeted_on_conflict_has_prior_matching_arbiter()
    {
        var sql = Read("outputs", "rev868c3_employee_reconciliation_idempotent.sql");
        var lines = sql.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var uniqueIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var requiredTables = new HashSet<string>(new[]
        {
            "departments",
            "designations",
            "employees",
            "roles",
            "role_page_permissions",
            "employee_role_assignments",
            "department_approval_mappings",
            "purchase_approval_workflow_steps"
        }, StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();
        string? currentInsertTable = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var unique = System.Text.RegularExpressions.Regex.Match(line, @"CREATE UNIQUE INDEX ""[^""]+"" ON nexa\.([a-z_]+) \(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (unique.Success)
            {
                var table = unique.Groups[1].Value;
                var columns = ExtractQuotedColumns(unique.Groups[2].Value);
                uniqueIndexes[$"{table}|{columns}"] = i + 1;
            }

            var insert = System.Text.RegularExpressions.Regex.Match(line, @"insert into nexa\.([a-z_]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (insert.Success)
            {
                currentInsertTable = insert.Groups[1].Value;
            }

            var conflict = System.Text.RegularExpressions.Regex.Match(line, @"on conflict \(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!conflict.Success || currentInsertTable is null || !requiredTables.Contains(currentInsertTable))
            {
                continue;
            }

            var conflictColumns = ExtractQuotedColumns(conflict.Groups[1].Value);
            var key = $"{currentInsertTable}|{conflictColumns}";
            if (!uniqueIndexes.TryGetValue(key, out var indexLine) || indexLine >= i + 1)
            {
                failures.Add($"line {i + 1}: {currentInsertTable} ({conflictColumns}) missing prior unique arbiter");
            }
        }

        Assert.Empty(failures);
        Assert.Contains("UX_rev868c3_conflict_departments_code", sql);
        Assert.Contains("UX_rev868c3_conflict_designations_code", sql);
        Assert.Contains("UX_rev868c3_conflict_employees_employee_code", sql);
        Assert.Contains("UX_rev868c3_conflict_roles_code", sql);
        Assert.Contains("UX_rev868c3_conflict_role_page_permissions", sql);
        Assert.Contains("UX_rev868c3_conflict_employee_role_assignments", sql);
        Assert.Contains("UX_rev868c3_conflict_department_approval_mappings", sql);
        Assert.Contains("UX_rev868c3_conflict_purchase_approval_workflow_steps", sql);
    }
    [Fact]
    public void Rev868c3_migration_version_columns_and_values_are_bigint_compatible()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation.cs");

        Assert.DoesNotContain("table.Column<uint>(type: \"xid\"", migration);
        Assert.Contains("Version = table.Column<long>(type: \"bigint\", nullable: false)", migration);
        Assert.Contains(", 0::bigint)", migration);
        Assert.Contains(", 0::bigint", migration);
        Assert.DoesNotContain("null, null, 0)", migration);
        Assert.DoesNotContain("null, null, 0\r\n", migration);
        Assert.DoesNotContain("null, null, 0\n", migration);
    }

    [Fact]
    public void Rev868c3_offline_sql_version_values_are_explicit_bigint_casts()
    {
        var sql = Read("outputs", "rev868c3_employee_reconciliation_idempotent.sql");
        var rev868c3Start = sql.IndexOf("20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation", StringComparison.Ordinal);
        Assert.True(rev868c3Start >= 0);
        var rev868c3Sql = sql[rev868c3Start..];

        Assert.Contains("0::bigint", rev868c3Sql);
        Assert.DoesNotContain("null, null, 0)", rev868c3Sql);
        Assert.DoesNotContain("null, NULL, 0)", rev868c3Sql);
        Assert.DoesNotContain("xid", rev868c3Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_sanitized_ef_failure_metadata_handles_common_postgres_and_ef_failures()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");
        var sanitizer = helper[helper.IndexOf("function New-Sha256Fingerprint", StringComparison.Ordinal)..helper.IndexOf("function Test-EfProjectMetadata", StringComparison.Ordinal)];

        Assert.Contains("42P10", helper);
        Assert.Contains("42804", helper);
        Assert.Contains("42703", helper);
        Assert.Contains("datatype_mismatch", helper);
        Assert.Contains("undefined_column", helper);
        Assert.Contains("SqlState", helper);
        Assert.Contains("on_conflict_index_mismatch", helper);
        Assert.Contains("23502", helper);
        Assert.Contains("23503", helper);
        Assert.Contains("ef_project_metadata", helper);
        Assert.Contains("exception_type=", helper);
        Assert.Contains("constraint=", helper);
        Assert.Contains("message_category=", helper);
        Assert.Contains("phase=", helper);
        Assert.Contains("raw_output_sha256=", helper);
        Assert.Contains("New-Sha256Fingerprint", helper);
        Assert.Contains("SHA256]::Create", helper);
        Assert.Contains("ComputeHash", helper);
        Assert.Contains("BitConverter]::ToString", helper);
        Assert.Contains("raw_output_sha256=unavailable", helper);
        Assert.Contains("sanitizer_failure", helper);
        Assert.DoesNotContain("HashData", sanitizer);
        Assert.DoesNotContain("ToHexString", sanitizer);
        Assert.DoesNotContain("CommandText", sanitizer);
        Assert.DoesNotContain("ArgumentList", helper);
        Assert.DoesNotContain("GetRelativePath", helper);
        Assert.DoesNotContain("OperatingSystem.IsWindows", helper);
    }

    [Fact]
    public void Rev868c3_sanitizer_runs_under_windows_powershell_51_without_raw_private_output()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");
        var start = helper.IndexOf("function New-Sha256Fingerprint", StringComparison.Ordinal);
        var end = helper.IndexOf("function Invoke-EfDatabaseUpdateSanitized", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var functions = helper[start..end];
        var tempFile = Path.Combine(Path.GetTempPath(), "rev868c3_sanitizer_" + Guid.NewGuid().ToString("N") + ".ps1");
        var script = functions + """
$cases = @(
    @('Npgsql.PostgresException: SqlState: 42P10 there is no unique or exclusion constraint matching the ON CONFLICT specification. SQL: INSERT INTO private_table VALUES (''PRIVATE_EMPLOYEE_NAME'')', 'on_conflict_index_mismatch'),
    @('Npgsql.PostgresException: SqlState: 42804 column "Version" is of type bigint but expression is of type text. SQL: INSERT INTO private_table VALUES (''PRIVATE_EMPLOYEE_NAME'')', 'datatype_mismatch'),
    @('Npgsql.PostgresException: SqlState: 42804 column "Version" is of type bigint but expression is of type text. SQL: INSERT INTO private_table VALUES (''PRIVATE_EMPLOYEE_NAME'')', 'datatype_mismatch'),
    @('Npgsql.PostgresException: SqlState: 42703 column "UserRole" does not exist. SQL: INSERT INTO private_table VALUES (''PRIVATE_EMPLOYEE_NAME'')', 'undefined_column'),
    @('PostgresException (23502): null value in column "IsPrivileged" of relation "roles" violates not-null constraint. DOB 1990-01-01 payroll PAYROLL-SECRET PRIVATE_EMPLOYEE_NAME', 'not_null_violation'),
    @('PostgresException (23503): insert or update on table "employees" violates foreign key constraint "FK_employees_departments_DepartmentId". SQL: SELECT * FROM secret_employee', 'foreign_key_violation'),
    @('System.InvalidOperationException: Unable to retrieve project metadata. PRIVATE_EMPLOYEE_NAME payroll PAYROLL-SECRET', 'ef_project_metadata')
)
foreach ($case in $cases) {
    $result = Get-SanitizedEfFailure -Output @($case[0]) -ExitCode 1 -Phase 'unit_test'
    if ($result -notmatch $case[1]) { throw "Missing category $($case[1]): $result" }
    if ($result -match 'PRIVATE_EMPLOYEE_NAME|PAYROLL-SECRET|1990-01-01|INSERT INTO|SELECT \* FROM|SQL:') { throw "Sanitized result leaked private output: $result" }
}
$hash = New-Sha256Fingerprint 'abc'
if ($hash -ne 'BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD') { throw "Unexpected SHA-256 fingerprint $hash" }
'OK'
""";
        try
        {
            File.WriteAllText(tempFile, script);
            var startInfo = new System.Diagnostics.ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + tempFile + "\"";
            using var process = System.Diagnostics.Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, output + error);
            Assert.Contains("OK", output);
            Assert.DoesNotContain("PRIVATE_EMPLOYEE_NAME", output + error);
            Assert.DoesNotContain("PAYROLL-SECRET", output + error);
            Assert.DoesNotContain("1990-01-01", output + error);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    [Fact]
    public void Rev868c3_helper_sanitizes_ef_failure_output_and_blocks_raw_pii_leakage()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("Get-SanitizedEfFailure", helper);
        Assert.Contains("Invoke-EfDatabaseUpdateSanitized", helper);
        Assert.Contains("sqlstate=", helper);
        Assert.Contains("schema=", helper);
        Assert.Contains("table=", helper);
        Assert.Contains("column=", helper);
        Assert.Contains("category=", helper);
        Assert.Contains("not_null_violation", helper);
        Assert.DoesNotContain("EF database update failed with exit code", helper);
        Assert.DoesNotContain("$output -join", helper[helper.IndexOf("function Invoke-EfDatabaseUpdateSanitized", StringComparison.Ordinal)..helper.IndexOf("function Test-EfProjectMetadata", StringComparison.Ordinal)]);
        Assert.DoesNotContain("Include Error Detail", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EmployeeName", helper[helper.IndexOf("function Get-SanitizedEfFailure", StringComparison.Ordinal)..helper.IndexOf("function Test-EfProjectMetadata", StringComparison.Ordinal)]);
        Assert.Contains("REV868C3 PostgreSQL tests failed. exit_code=", helper);
    }

    private static string ExtractQuotedColumns(string text) => string.Join(",", System.Text.RegularExpressions.Regex.Matches(text, "\"([^\"]+)\"").Select(x => x.Groups[1].Value));
    [Fact]
    public void Rev868c3_full_helper_reports_sanitized_trx_path_on_postgresql_test_failure()
    {
        var helper = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        Assert.Contains("Write-PostgresTestFailureReport", helper);
        Assert.Contains("trx_path=$trxPath", helper);
        Assert.Contains("sanitized_report=$testFailureReport", helper);
        Assert.Contains("ConvertTo-SanitizedTestOutput", helper);
        Assert.Contains("passwordKey + '=<redacted>'", helper);
        Assert.Contains("TRX not created or unavailable", helper);
    }
    [Fact]
    public void Rev868c3_sanitizer_preserves_safe_test_evidence_and_removes_only_sensitive_values()
    {
        var resume = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");
        var full = Read("tools", "apply-rev868c3-employee-reconciliation-secure.ps1");

        foreach (var helper in new[] { resume, full })
        {
            Assert.DoesNotContain("redacted-uppercase-text", helper);
            Assert.Contains("<redacted-path>", helper);
            Assert.Contains("<redacted-password>", helper);
            Assert.Contains("targeted_test|$required|Missing", helper);
            Assert.Contains("passed=$", helper);
            Assert.Contains("Rev868c3_manager_md_td_approval_sequence_is_enforced", helper);
        }
    }

    [Fact]
    public void Rev868c3_postrun_readonly_verifier_is_select_only_and_covers_missing_final_evidence()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("begin transaction read only;", helper);
        Assert.Contains("migration_acceptance_state", helper);
        Assert.Contains("employee_acceptance_state", helper);
        Assert.Contains("department_acceptance_state", helper);
        Assert.Contains("manager_mapping_acceptance_state", helper);
        Assert.Contains("workflow_acceptance_state", helper);
        Assert.Contains("permission_acceptance_state", helper);
        Assert.Contains("history_audit_acceptance_state", helper);
        Assert.Contains("duplicate_conflict_acceptance_state", helper);
        Assert.DoesNotContain("'active_employee_acceptance_state='", helper);
        Assert.DoesNotContain("'relieved_employee_acceptance_state='", helper);
        Assert.DoesNotContain("'mapping_acceptance_state='", helper);
        Assert.DoesNotContain("'manager_permission_acceptance_state='", helper);
        Assert.Contains("audit_evidence_count", helper);
        Assert.Contains("duplicate_employee_codes", helper);
        Assert.Contains("duplicate_payroll_ids", helper);
        Assert.Contains("-f $script:tempSqlFile", helper);
        Assert.DoesNotContain("database update", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", helper, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Rev868c3_resume_postgresql_test_helper_is_migration_free_and_isolated()
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("REV868C3 resume requires all", helper);
        Assert.Contains("Rev868C3PostgreSqlWorkflowVerificationTests", helper);
        Assert.Contains("trx_path=$trxPath", helper);
        Assert.Contains("sanitized_report=$report", helper);
        Assert.Contains("sess_nexaerp", helper);
        Assert.Contains("template0", helper);
        Assert.Contains("template1", helper);
        Assert.DoesNotContain("database update", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("migrations remove", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet ef", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_dump", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", helper, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868c3_resume_sql_generation_is_read_only_and_uses_safe_mixed_case_identifier_quoting()
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("function Get-ResumeSql", helper);
        Assert.Contains("[switch]$GenerateSqlOnly", helper);
        Assert.Contains("begin transaction read only;", helper);
        Assert.Contains("select \"MigrationId\"", helper);
        Assert.Contains("from \"public\".\"__EFMigrationsHistory\"", helper);
        Assert.Contains("commit;", helper);
        Assert.Contains("-f $script:tempSqlFile", helper);
        Assert.DoesNotContain("\"\"MigrationId\"\"", helper);
        Assert.DoesNotContain("\"\"__EFMigrationsHistory\"\"", helper);
        Assert.DoesNotContain("-c $Sql", helper);
        Assert.DoesNotContain("unterminated quoted identifier", helper, StringComparison.OrdinalIgnoreCase);

        var expectedMigrationIds = new[]
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
            "20260809143000_Rev868C3EmployeeDepartmentManagerReconciliation"
        };
        foreach (var migrationId in expectedMigrationIds) Assert.Contains(migrationId, helper);
    }
    [Theory]
    [InlineData(10, 10, 0, 0, 0, true)]
    [InlineData(10, 9, 0, 1, 0, false)]
    [InlineData(10, 10, 1, 0, 0, false)]
    [InlineData(10, 10, 0, 0, 1, false)]
    public void Rev868c3_resume_migration_acceptance_requires_no_unexpected_migrations(int expectedCount, int matchedCount, int duplicateCount, int missingCount, int unexpectedCount, bool expectedPass)
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("unexpected_count = 0 then 'PASS'", helper);
        var actualPass = expectedCount == 10 && matchedCount == 10 && duplicateCount == 0 && missingCount == 0 && unexpectedCount == 0;
        Assert.Equal(expectedPass, actualPass);
    }

    [Fact]
    public void Rev868c3_resume_runtime_guard_requires_exact_database_and_exact_ten_migration_ids_once()
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("Assert-TargetDatabaseName", helper);
        Assert.Contains("sess_nexaerp_rev868_verify", helper);
        Assert.Contains("$returnedMigrations.Count -ne $ExpectedMigrations.Count", helper);
        Assert.Contains("$unexpectedMigrations.Count -ne 0", helper);
        Assert.Contains("$missingMigrations.Count -ne 0", helper);
        Assert.Contains("$duplicateMigrations.Count -ne 0", helper);
        Assert.Contains("REV868C3 resume migration ID evidence did not contain exactly the ten expected migrations once", helper);
    }

    [Fact]
    public void Rev868c3_resume_helper_rejects_malformed_quoting_and_database_modification_commands()
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("Resume SQL contains doubled quoted identifier output", helper);
        Assert.Contains("Resume SQL has unbalanced double quotes", helper);
        Assert.Contains("begin transaction read only;", helper);
        Assert.Contains("-f $script:tempSqlFile", helper);
        Assert.DoesNotContain("-c $Sql", helper);
        Assert.DoesNotContain("database update", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE DATABASE", helper, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP DATABASE", helper, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void Rev868c3_resume_helper_cleans_temp_sql_and_secret_environment_values()
    {
        var helper = Read("tools", "resume-rev868c3-postgresql-tests-secure.ps1");

        Assert.Contains("$script:tempSqlFile", helper);
        Assert.Contains("Remove-Item -LiteralPath $script:tempSqlFile", helper);
        Assert.Contains("Remove-Item Env:\\PGPASSWORD", helper);
        Assert.Contains("Remove-Item Env:\\ConnectionStrings__NexaErp", helper);
        Assert.Contains("Remove-Item Env:\\NexaErp__ExpectedDatabase", helper);
        Assert.Contains("Test-Path -LiteralPath $script:tempSqlFile", helper);
    }


    [Fact]
    public void Rev868c3_postrun_verifier_uses_independent_fail_closed_sections()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");

        foreach (var functionName in new[]
        {
            "Get-MigrationEvidenceSql",
            "Get-EmployeeEvidenceSql",
            "Get-DepartmentEvidenceSql",
            "Get-ManagerMappingEvidenceSql",
            "Get-WorkflowEvidenceSql",
            "Get-PermissionEvidenceSql",
            "Get-HistoryAuditEvidenceSql",
            "Get-DuplicateConflictEvidenceSql"
        })
        {
            Assert.Contains($"function {functionName}", helper);
        }

        Assert.Contains("begin transaction read only;", helper);
        Assert.Contains("-v ON_ERROR_STOP=1", helper);
        Assert.Contains("-f $script:tempSqlFile", helper);
        Assert.DoesNotContain(" -c ", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("migration_unexpected_count", helper);
        Assert.Contains("migration_acceptance_state='||case when expected_count=10 and actual_matched_count=10 and missing_count=0 and unexpected_count=0 and duplicate_count=0", helper);
        Assert.Contains("except all select * from sa", helper);
        Assert.Contains("status_history_duplicate_count", helper);
        Assert.Contains("department_history_duplicate_count", helper);
        Assert.Contains("history_audit_acceptance_state='||case when", helper);
        Assert.Contains("Get-DatabaseAcceptanceEvidence", helper);
        Assert.Contains("if($matches.Count -ne 1){return 'FAIL'}", helper);
        Assert.DoesNotContain("database_acceptance_state_requires_all_previous_labels", helper);
    }

    [Fact]
    public void Rev868c3_postrun_verifier_preserves_exact_controlled_sets_and_trx_acceptance()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");

        Assert.Contains("SESS-051", helper);
        Assert.Contains("SESS-016", helper);
        Assert.Contains("QUALITY_QC", helper);
        Assert.Contains("ENGINEER_TECHNICAL", helper);
        Assert.Contains("DESIGN|PROJECT|SESS-019|SESS-015", helper);
        Assert.Contains("DESIGN|REGULAR_PRODUCT|SESS-015|SESS-019", helper);
        Assert.Contains("purchase.requisition-approvals|CanView=T|CanCreate=F|CanUpdate=F", helper);
        Assert.Contains("purchase.requisitions|CanView=T|CanCreate=F|CanUpdate=F", helper);
        Assert.Contains("rev868c3_resume_20260809_210202.trx", helper);
        Assert.Contains("test_acceptance_state=PASS", helper);
        Assert.Contains("Get-OverallAcceptanceEvidence", helper);
        Assert.Contains("database_acceptance_blocker=database_sql_not_executed_in_generate_sql_only_mode", helper);
        Assert.Contains("if($overallEvidence -ne 'overall_acceptance_state=PASS')", helper);
        Assert.Contains("Rev868c3_unauthenticated_request_returns_401", helper);
        Assert.Contains("Rev868c3_unauthorized_role_returns_403", helper);
        Assert.Contains("Rev868c3_creator_self_approval_returns_403", helper);
        Assert.Contains("Rev868c3_duplicate_approver_is_prevented", helper);
        Assert.Contains("Rev868c3_missing_department_manager_fails_closed", helper);
        Assert.Contains("Rev868c3_manager_md_td_approval_sequence_is_enforced", helper);
    }

    [Theory]
    [InlineData("PASS,PASS,PASS,PASS,PASS,PASS,PASS,PASS", "PASS")]
    [InlineData("PASS,PASS,PASS,PASS,PASS,PASS,FAIL,PASS", "FAIL")]
    [InlineData("PASS,PASS,PASS,PASS,PASS,PASS,PASS", "FAIL")]
    [InlineData("PASS,PASS,PASS,PASS,PASS,PASS,PASS,PASS,PASS", "FAIL")]
    public void Rev868c3_database_acceptance_formula_requires_exact_eight_section_states(string statesCsv, string expected)
    {
        var states = statesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var actual = states.Length == 8 && states.All(x => x == "PASS") ? "PASS" : "FAIL";
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Rev868c3_postrun_verifier_proves_history_and_audit_exactly_and_rejects_ambiguous_evidence()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");

        Assert.Contains("nexa.rev868c3_employee_backup", helper);
        Assert.Contains("except all select * from sa", helper);
        Assert.Contains("except all select * from da", helper);
        Assert.Contains("SourceWorkbook=SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx", helper);
        Assert.Contains("audit_scoped_count", helper);
        Assert.Contains("ascoped=1 and aexact=1", helper);
        Assert.Contains("$units.Count -eq 6", helper);
        Assert.Contains("$results.Count -eq 6", helper);
        Assert.Contains("$notPassed -eq 0", helper);
        Assert.Contains("Database identity evidence missing or duplicated.", helper);
    }

    [Fact]
    public void Rev868c3_postrun_verifier_requires_employee_department_and_complete_permission_conditions()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");
        var employee = FunctionBody(helper, "Get-EmployeeEvidenceSql", "Get-DepartmentEvidenceSql");
        var department = FunctionBody(helper, "Get-DepartmentEvidenceSql", "Get-ManagerMappingEvidenceSql");
        var permission = FunctionBody(helper, "Get-PermissionEvidenceSql", "Get-HistoryAuditEvidenceSql");

        Assert.Contains("e.\"LoginEnabled\" is distinct from b.\"LoginEnabled\")=0", employee);
        Assert.Contains("e.\"ApprovalStatus\" is distinct from b.\"ApprovalStatus\")=0", employee);
        Assert.Contains("group by \"Code\" having count(*)>1) d)=0", department);
        foreach (var flag in PermissionFlags)
        {
            Assert.Contains($"rpp.\"{flag}\"", permission);
            Assert.Contains($"|{flag}=", permission);
        }

        Assert.Contains("where r.\"Code\"='DEPARTMENT_MANAGER'", permission);
        Assert.DoesNotContain("p.\"PageKey\" in", permission, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dupes as (select page_key from actual group by page_key", permission);
    }

    [Fact]
    public void Rev868c3_postrun_verifier_requires_exactly_eight_unique_canonical_section_labels()
    {
        var helper = Read("tools", "verify-rev868c3-postrun-readonly-secure.ps1");

        Assert.Contains("$acceptanceLines.Count -eq $SectionAcceptanceLabels.Count", helper);
        Assert.Contains("$matches.Count -eq 1", helper);
        Assert.DoesNotContain("'active_employee_acceptance_state='", helper);
        Assert.DoesNotContain("'relieved_employee_acceptance_state='", helper);
        Assert.DoesNotContain("'mapping_acceptance_state='", helper);
        Assert.DoesNotContain("'manager_permission_acceptance_state='", helper);
    }

    [Theory]
    [InlineData("employee_login_mismatch")]
    [InlineData("employee_approval_mismatch")]
    [InlineData("department_duplicate")]
    [InlineData("permission_flag_mismatch")]
    [InlineData("unexpected_department_manager_page")]
    [InlineData("missing_canonical_label")]
    [InlineData("duplicate_canonical_label")]
    [InlineData("extra_noncanonical_label")]
    public void Rev868c3_postrun_negative_defects_force_database_and_overall_fail(string defect)
    {
        var evidence = CanonicalSectionLabels.Select(x => $"{x}=PASS").ToList();
        switch (defect)
        {
            case "employee_login_mismatch":
            case "employee_approval_mismatch":
                ReplaceState(evidence, "employee_acceptance_state", "FAIL");
                break;
            case "department_duplicate":
                ReplaceState(evidence, "department_acceptance_state", "FAIL");
                break;
            case "permission_flag_mismatch":
            case "unexpected_department_manager_page":
                ReplaceState(evidence, "permission_acceptance_state", "FAIL");
                break;
            case "missing_canonical_label":
                evidence.Remove("history_audit_acceptance_state=PASS");
                break;
            case "duplicate_canonical_label":
                evidence.Add("migration_acceptance_state=PASS");
                break;
            case "extra_noncanonical_label":
                evidence.Add("mapping_acceptance_state=PASS");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defect));
        }

        var databasePass = CanonicalDatabasePass(evidence);
        var testAcceptancePass = true;
        var overallPass = databasePass && testAcceptancePass;
        Assert.False(databasePass);
        Assert.False(overallPass);
    }

    private static readonly string[] CanonicalSectionLabels =
    [
        "migration_acceptance_state",
        "employee_acceptance_state",
        "department_acceptance_state",
        "manager_mapping_acceptance_state",
        "workflow_acceptance_state",
        "permission_acceptance_state",
        "history_audit_acceptance_state",
        "duplicate_conflict_acceptance_state"
    ];

    private static readonly string[] PermissionFlags =
    [
        "CanView", "CanCreate", "CanUpdate", "CanSubmit", "CanVerify", "CanApprove", "CanReject",
        "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanCancel", "CanDeactivate",
        "CanPrint", "CanDownload", "CanExport", "CanUploadAttachment", "CanReplaceAttachment",
        "CanViewCommercialValues", "CanViewAuditHistory", "HasFullControl"
    ];

    private static string FunctionBody(string source, string functionName, string nextFunctionName)
    {
        var start = source.IndexOf($"function {functionName}", StringComparison.Ordinal);
        var end = source.IndexOf($"function {nextFunctionName}", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, $"Unable to isolate {functionName}.");
        return source[start..end];
    }

    private static bool CanonicalDatabasePass(IReadOnlyList<string> evidence)
    {
        var acceptanceLines = evidence
            .Where(x => System.Text.RegularExpressions.Regex.IsMatch(x, "^[a-z0-9_]+_acceptance_state=(PASS|FAIL)$"))
            .ToArray();
        return acceptanceLines.Length == CanonicalSectionLabels.Length
            && CanonicalSectionLabels.All(label => acceptanceLines.Count(x => x == $"{label}=PASS") == 1);
    }

    private static void ReplaceState(List<string> evidence, string label, string state)
    {
        var index = evidence.FindIndex(x => x.StartsWith(label + "=", StringComparison.Ordinal));
        Assert.True(index >= 0, $"Missing synthetic label {label}.");
        evidence[index] = $"{label}={state}";
    }

    private static void AssertOrdered(string text, string before, string after)
    {
        var beforeIndex = text.IndexOf(before, StringComparison.OrdinalIgnoreCase);
        var afterIndex = text.IndexOf(after, StringComparison.OrdinalIgnoreCase);
        Assert.True(beforeIndex >= 0, $"Missing before marker: {before}");
        Assert.True(afterIndex >= 0, $"Missing after marker: {after}");
        Assert.True(beforeIndex < afterIndex, $"Expected marker before ON CONFLICT: {before}");
    }


    private static Dictionary<string, HashSet<string>> ParseRawInsertColumns(string migration)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(migration, @"insert into nexa\.([a-z_]+) \(([^)]*)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            var table = match.Groups[1].Value;
            if (!result.TryGetValue(table, out var columns))
            {
                columns = new HashSet<string>(StringComparer.Ordinal);
                result[table] = columns;
            }

            foreach (System.Text.RegularExpressions.Match column in System.Text.RegularExpressions.Regex.Matches(match.Groups[2].Value, "\\\"([^\\\"]+)\\\""))
            {
                columns.Add(column.Groups[1].Value);
            }
        }

        return result;
    }

    private static IReadOnlyCollection<string> RequiredRawInsertColumns(NexaErpDbContext db, string table)
    {
        var entity = Assert.Single(db.Model.GetEntityTypes(), e => e.GetSchema() == "nexa" && e.GetTableName() == table);
        var storeObject = StoreObjectIdentifier.Table(table, "nexa");
        return entity.GetProperties()
            .Where(property => !property.IsNullable && property.ValueGenerated == ValueGenerated.Never)
            .Select(property => property.GetColumnName(storeObject))
            .Where(column => column is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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
