using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev866SeedTests
{
    [Fact]
    public void Rev866_employee_seed_contains_exactly_39_unique_employee_codes()
    {
        var codes = Rev866SeedData.Employees.Select(employee => employee.EmployeeCode).ToList();

        Assert.Equal(39, codes.Count);
        Assert.Equal(39, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("SESS-001", codes);
        Assert.Contains("SESS-039", codes);
    }

    [Fact]
    public void Rev866_employee_import_history_retains_original_and_normalized_names()
    {
        var employee = Rev866SeedData.Employees.Single(item => item.EmployeeCode == "SESS-001");
        var import = Rev866SeedData.EmployeeImportHistories.Single(item => item.EmployeeId == employee.Id);

        Assert.Equal("A. PARAMANANTHAM", employee.EmployeeName);
        Assert.Equal(employee.EmployeeName, employee.OriginalImportedName);
        Assert.Equal(employee.OriginalImportedName, import.SourceEmployeeName);
        Assert.Equal(employee.EmployeeName, import.NormalizedEmployeeName);
    }

    [Fact]
    public void Rev866_role_mapping_supports_multiple_roles_without_comma_separated_storage()
    {
        var employee = Rev866SeedData.Employees.Single(item => item.EmployeeCode == "SESS-012");
        var roleIds = Rev866SeedData.EmployeeRoleAssignments
            .Where(assignment => assignment.EmployeeId == employee.Id)
            .Select(assignment => assignment.RoleId)
            .ToList();

        Assert.Equal(2, roleIds.Count);
        Assert.Equal(2, roleIds.Distinct().Count());
    }

    [Fact]
    public void Rev866_td_and_md_roles_are_limited_to_approved_employees()
    {
        var td = Rev866SeedData.AdditionalEmployeeRoles.Single(role => role.Code == "technical_director");
        var md = Rev866SeedData.AdditionalEmployeeRoles.Single(role => role.Code == "managing_director");
        var assignments = Rev866SeedData.EmployeeRoleAssignments.ToLookup(assignment => assignment.RoleId);
        var tdEmployees = assignments[td.Id].Select(assignment => Rev866SeedData.Employees.Single(employee => employee.Id == assignment.EmployeeId).EmployeeCode).ToList();
        var mdEmployees = assignments[md.Id].Select(assignment => Rev866SeedData.Employees.Single(employee => employee.Id == assignment.EmployeeId).EmployeeCode).ToList();

        Assert.Equal(new[] { "SESS-001" }, tdEmployees);
        Assert.Equal(new[] { "SESS-002" }, mdEmployees);
    }

    [Fact]
    public void Rev866_permission_matrix_covers_all_active_roles_and_pages_with_distinct_permission_flags()
    {
        var roleCount = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Count();
        var pageCount = FoundationSeedData.Pages.Count();

        Assert.Equal(roleCount * pageCount, Rev866SeedData.RolePagePermissions.Count());
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanRequestClarification);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanViewCommercialValues);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanViewAuditHistory);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.HasFullControl);
    }

    [Fact]
    public void Rev866_corrective_status_history_has_one_initial_active_row_per_employee()
    {
        var statusRows = Rev866SeedData.EmployeeStatusHistories;
        var employeeIds = Rev866SeedData.Employees.Select(employee => employee.Id).ToHashSet();

        Assert.Equal(39, statusRows.Count);
        Assert.Equal(39, statusRows.Select(row => row.EmployeeId).Distinct().Count());
        Assert.Equal(39, statusRows.Select(row => row.Id).Distinct().Count());
        Assert.All(statusRows, row =>
        {
            Assert.Contains(row.EmployeeId, employeeIds);
            Assert.Equal("Not Created", row.OldStatus);
            Assert.Equal("Active", row.NewStatus);
            Assert.Contains("REV866C1", row.Reason, StringComparison.Ordinal);
            Assert.Equal("system-migration-rev866c1", row.CreatedBy);
        });
    }

    [Fact]
    public void Rev866_corrective_operational_roles_have_explicit_deny_rows_without_commercial_or_approval_power()
    {
        var pages = FoundationSeedData.Pages;
        var operationalRoleCodes = new[]
        {
            "technical_engineer", "electrical_engineer", "plc_engineer", "design_engineer",
            "junior_engineer", "production_operator", "software_engineer", "accounts_assistant",
            "software_developer", "admin_executive", "production_coordinator"
        };

        foreach (var roleCode in operationalRoleCodes)
        {
            var role = Rev866SeedData.AdditionalEmployeeRoles.Single(role => role.Code == roleCode);
            var rows = Rev866SeedData.RolePagePermissions.Where(permission => permission.RoleId == role.Id).ToList();
            Assert.Equal(pages.Length, rows.Count);
            Assert.All(rows, row =>
            {
                Assert.False(row.CanApprove);
                Assert.False(row.CanViewCommercialValues);
                Assert.False(row.CanExport);
                Assert.False(row.HasFullControl);
            });
        }
    }

    [Fact]
    public void Rev866_corrective_purchase_and_stores_entry_roles_do_not_receive_approval_or_financial_power()
    {
        foreach (var roleCode in new[] { "purchase_executive", "stores_executive", "stores_assistant" })
        {
            var role = Rev866SeedData.AdditionalEmployeeRoles.Single(role => role.Code == roleCode);
            var rows = Rev866SeedData.RolePagePermissions.Where(permission => permission.RoleId == role.Id).ToList();
            Assert.Equal(FoundationSeedData.Pages.Length, rows.Count);
            Assert.Contains(rows, row => row.CanView || row.CanCreate);
            Assert.All(rows, row =>
            {
                Assert.False(row.CanApprove);
                Assert.False(row.CanViewCommercialValues);
                Assert.False(row.CanExport);
                Assert.False(row.HasFullControl);
            });
        }
    }

    [Fact]
    public void Rev866_model_contains_employee_expanded_permission_and_audit_tables()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test")
            .Options;

        using var dbContext = new NexaErpDbContext(options);
        var tables = dbContext.Model.GetEntityTypes().Select(entity => entity.GetTableName()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionProperties = dbContext.Model.FindEntityType(typeof(SESS.NexaERP.Domain.Authorization.RolePagePermission))!
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var auditProperties = dbContext.Model.FindEntityType(typeof(SESS.NexaERP.Domain.Audit.AuditLog))!
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("employees", tables);
        Assert.Contains("departments", tables);
        Assert.Contains("skills", tables);
        Assert.Contains("designations", tables);
        Assert.Contains("employee_role_assignments", tables);
        Assert.Contains("employee_import_history", tables);
        Assert.Contains("employee_status_history", tables);
        Assert.Contains("CanRequestRevision", permissionProperties);
        Assert.Contains("CanReplaceAttachment", permissionProperties);
        Assert.Contains("HasFullControl", permissionProperties);
        Assert.Contains("Result", auditProperties);
        Assert.Contains("CorrelationId", auditProperties);
    }
}