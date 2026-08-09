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
        Assert.DoesNotContain("from nexa.purchase_approval_workflow_steps", preflight, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("department_history_relation_count = 0", preflight);
        Assert.Contains("workflow_step_relation_count = 0", preflight);
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
        Assert.Contains("insert into nexa.audit_logs (\"Id\", \"UserLoginId\", \"UserRole\", \"Module\", \"EntityName\", \"EntityId\", \"Action\", \"OldValue\", \"NewValue\", \"Reason\", \"Result\", \"CorrelationId\", \"IpAddress\", \"CreatedAt\", \"CreatedBy\", \"UpdatedAt\", \"UpdatedBy\", \"Version\")", migration);
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
