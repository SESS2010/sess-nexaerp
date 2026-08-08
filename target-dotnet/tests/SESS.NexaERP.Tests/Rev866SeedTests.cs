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
    public void Rev866_permission_matrix_covers_all_seeded_roles_and_pages_with_distinct_permission_flags()
    {
        var roleCount = FoundationSeedData.Roles.Count();
        var pageCount = FoundationSeedData.Pages.Count();

        Assert.Equal(roleCount * pageCount, Rev866SeedData.RolePagePermissions.Count());
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanRequestClarification);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanViewCommercialValues);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.CanViewAuditHistory);
        Assert.Contains(Rev866SeedData.RolePagePermissions, permission => permission.HasFullControl);
    }

    [Fact]
    public void Rev866_model_contains_employee_and_expanded_permission_tables()
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

        Assert.Contains("employees", tables);
        Assert.Contains("departments", tables);
        Assert.Contains("skills", tables);
        Assert.Contains("designations", tables);
        Assert.Contains("employee_role_assignments", tables);
        Assert.Contains("employee_import_history", tables);
        Assert.Contains("CanRequestRevision", permissionProperties);
        Assert.Contains("CanReplaceAttachment", permissionProperties);
        Assert.Contains("HasFullControl", permissionProperties);
    }
}


