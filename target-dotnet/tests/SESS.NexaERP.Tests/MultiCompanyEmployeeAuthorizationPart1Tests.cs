using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class MultiCompanyEmployeeAuthorizationPart1Tests
{
    [Fact]
    public void Settled_role_manifest_contains_44_assignments_for_all_42_employees()
    {
        var rows = ReadEmployeeRoles();

        Assert.Equal(44, rows.Length);
        Assert.Equal(42, rows.Select(x => x.EmployeeCode).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            ["PURCHASE_EXECUTIVE", "PURCHASE_MANAGER", "STORES_EXECUTIVE"],
            rows.Where(x => x.EmployeeCode == "SESS-15").Select(x => x.RoleCode).Order(StringComparer.Ordinal).ToArray());
        Assert.Contains(rows, x => x.EmployeeCode == "SESS-14" && x.RoleCode == "ACCOUNTS_MANAGER");
        Assert.Contains(rows, x => x.EmployeeCode == "SESS-25" && x.RoleCode == "PRODUCTION_MANAGER");

        var newEmployees = rows.Where(x => string.CompareOrdinal(x.EmployeeCode, "SESS-13") >= 0)
            .Where(x => x.EmployeeCode == "SESS-13" || string.CompareOrdinal(x.EmployeeCode, "SESS-32") >= 0)
            .ToDictionary(x => x.EmployeeCode, x => x.RoleCode, StringComparer.Ordinal);
        Assert.Equal(12, newEmployees.Count);
        Assert.Equal("PRODUCTION_OPERATOR", newEmployees["SESS-13"]);
        Assert.Equal("SOFTWARE_DEVELOPER", newEmployees["SESS-32"]);
        Assert.Equal("QC_MANAGER", newEmployees["SESS-33"]);
        Assert.Equal("TECHNICAL_ENGINEER", newEmployees["SESS-34"]);
        Assert.Equal("STORES_EXECUTIVE", newEmployees["SESS-35"]);
        Assert.Equal("ELECTRICAL_ENGINEER", newEmployees["SESS-36"]);
        Assert.Equal("ELECTRICAL_ENGINEER", newEmployees["SESS-37"]);
        Assert.Equal("TECHNICAL_ENGINEER", newEmployees["SESS-38"]);
        Assert.Equal("PRODUCTION_OPERATOR", newEmployees["SESS-39"]);
        Assert.Equal("SOFTWARE_DEVELOPER", newEmployees["SESS-40"]);
        Assert.Equal("ACCOUNTS_ASSISTANT", newEmployees["SESS-41"]);
        Assert.Equal("TECHNICAL_ENGINEER", newEmployees["SESS-42"]);
    }

    [Fact]
    public void New_manager_roles_are_uppercase_stable_and_permissionless_in_part1()
    {
        var roles = ReadNewRoles();
        Assert.Equal(2, roles.Length);
        Assert.Equal(["ACCOUNTS_MANAGER", "PRODUCTION_MANAGER"], roles.Select(x => x.Code).Order(StringComparer.Ordinal).ToArray());
        Assert.All(roles, role => Assert.True(role.IsPrivileged));

        var permissionRoleIds = Rev866SeedData.RolePagePermissions.Concat(Rev869ASeedData.RolePagePermissions)
            .Select(x => x.RoleId).ToHashSet();
        Assert.All(roles, role => Assert.DoesNotContain(role.Id, permissionRoleIds));
    }

    [Fact]
    public void Qc_manager_has_no_page_approval_grant()
    {
        var qcRole = Assert.Single(Rev869ASeedData.Roles, x => x.Code == Rev869ARoleCodes.QcManager);
        var permissions = Rev869ASeedData.RolePagePermissions.Where(x => x.RoleId == qcRole.Id).ToArray();

        Assert.NotEmpty(permissions);
        Assert.All(permissions, permission => Assert.False(permission.CanApprove));
        Assert.All(permissions, permission => Assert.False(permission.CanDeactivate));
    }

    [Fact]
    public void Company_aware_unique_indexes_include_company_id_first()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);

        AssertIndex(db, "EmployeeRoleAssignment", true, "CompanyId", "EmployeeId", "RoleId", "EffectiveFrom");
        AssertIndex(db, "EmployeeIdentityMapping", true, "CompanyId", "Issuer", "Subject", "IsActive");
        AssertIndex(db, "PurchaseApprovalRouteSetting", true, "CompanyId", "RouteCode");
        AssertIndex(db, "PurchaseApprovalWorkflowStep", true, "CompanyId", "RouteCode", "StepNumber", "EffectiveFrom");
        AssertIndex(db, "DepartmentApprovalMapping", true, "CompanyId", "DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom");
    }

    private static void AssertIndex(NexaErpDbContext db, string entityName, bool unique, params string[] properties)
    {
        var entity = db.Model.GetEntityTypes().Single(x => x.ClrType.Name == entityName);
        var index = Assert.Single(entity.GetIndexes(), x => x.Properties.Select(p => p.Name).SequenceEqual(properties));
        Assert.Equal(unique, index.IsUnique);
    }

    private static (string EmployeeCode, string RoleCode)[] ReadEmployeeRoles()
    {
        var type = typeof(NexaErpDbContext).Assembly.GetType(
            "SESS.NexaERP.Infrastructure.Persistence.Migrations.MultiCompanyEmployeeAuthorizationPart1Data", true)!;
        var values = (Array)type.GetField("EmployeeRoles", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        return values.Cast<object>().Select(value =>
        {
            var valueType = value.GetType();
            return (
                (string)valueType.GetProperty("EmployeeCode")!.GetValue(value)!,
                (string)valueType.GetProperty("RoleCode")!.GetValue(value)!);
        }).ToArray();
    }

    private static Role[] ReadNewRoles()
    {
        var type = typeof(NexaErpDbContext).Assembly.GetType(
            "SESS.NexaERP.Infrastructure.Persistence.MultiCompanyEmployeeAuthorizationPart1SeedData", true)!;
        return (Role[])type.GetField("Roles", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    }
}
