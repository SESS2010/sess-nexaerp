using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class EmployeeRoleGovernancePhase2Tests
{
    [Fact]
    public void Model_has_company_profile_assignment_metadata_and_immutable_events()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var assignment = model.FindEntityType(typeof(EmployeeRoleAssignment))!;
        Assert.Contains(assignment.GetProperties(), x => x.Name == "AssignmentType");
        Assert.Contains(assignment.GetProperties(), x => x.Name == "IsPrimary");
        Assert.Contains(assignment.GetCheckConstraints(), x => x.Name == "CK_employee_role_assignment_dates");
        var profile = model.FindEntityType(typeof(EmployeeCompanyRoleProfile))!;
        Assert.Contains(profile.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual(["CompanyId", "EmployeeId"]));
        Assert.NotNull(model.FindEntityType(typeof(EmployeeRoleAssignmentEvent)));
    }

    [Fact]
    public void Request_contracts_support_every_explicit_assignment_operation()
    {
        AssertMandatory(typeof(AssignEmployeeRoleRequest), "RoleCode", "EffectiveFrom", "Remarks", "AssignmentType", "IsPrimary");
        AssertMandatory(typeof(PromoteEmployeeRoleRequest), "NewRoleCode", "EffectiveOn", "KeepPreviousRoleAsSecondary", "Remarks", "ProfileVersion");
        AssertMandatory(typeof(TransferEmployeeRoleRequest), "NewRoleCode", "EffectiveOn", "KeepPreviousRoleAsSecondary", "Remarks", "ProfileVersion");
        AssertMandatory(typeof(TemporaryRoleCoverRequest), "RoleCode", "EffectiveFrom", "EffectiveTo", "Remarks");
        AssertMandatory(typeof(ChangePrimaryRoleRequest), "AssignmentId", "EffectiveOn", "KeepPreviousRoleAsSecondary", "Remarks", "ProfileVersion");
        AssertMandatory(typeof(EndEmployeeRoleAssignmentRequest), "EffectiveTo", "Reason", "Version");
    }

    [Fact]
    public void Read_models_return_every_version_and_assignment_input_identifier()
    {
        AssertMandatory(typeof(EmployeeRoleSummary), "Id", "RoleCode", "EffectiveFrom", "EffectiveTo",
            "AssignmentType", "IsPrimary", "Version");
        AssertMandatory(typeof(EmployeeRoleProfileSummary), "EmployeeCode", "CompanyCode",
            "ConfigurationStatus", "PrimaryRoleAssignmentId", "PrimaryRoleCode", "Version", "Assignments");
        AssertMandatory(typeof(AuditLogSummary), "ActorRoleCode");
        AssertMandatory(typeof(SessionMe), "PrimaryRoleCode", "ActingRoleCode");
    }

    [Fact]
    public void Endpoint_surface_has_runtime_operations_and_no_migration_dependency()
    {
        var source = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Endpoints", "EmployeeEndpoints.cs")) +
                     File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Endpoints", "EmployeeRoleGovernanceEndpoints.cs"));
        foreach (var route in new[] { "temporary-cover", "promote", "transfer", "change-primary", "/end", "role-profile", "role-events" })
            Assert.Contains(route, source, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync(IsolationLevel.Serializable", source, StringComparison.Ordinal);
        Assert.Contains("Use promotion, transfer or change-primary", source, StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Acting_role_defaults_to_primary_and_invalid_override_is_refused()
    {
        var claims = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Security", "ClaimsCurrentUser.cs"));
        var middleware = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Middleware", "EmployeeIdentityResolutionMiddleware.cs"));
        Assert.Contains("Resolution.PrimaryRoleCode", claims, StringComparison.Ordinal);
        Assert.Contains("X-SESS-Acting-Role", middleware, StringComparison.Ordinal);
        Assert.Contains("Status403Forbidden", middleware, StringComparison.Ordinal);
        Assert.Contains("resolution.RoleCodes.Contains", middleware, StringComparison.Ordinal);
        Assert.Contains("ActingRoleCode = requestedRole ?? resolution.PrimaryRoleCode", middleware, StringComparison.Ordinal);
        var permissions = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Security", "PagePermissionEndpointFilter.cs"));
        Assert.Contains("HasPermissionAsync([currentUser.ActingRoleCode]", permissions, StringComparison.Ordinal);
        Assert.DoesNotContain("HasPermissionAsync(currentUser.RoleCodes", permissions, StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_contains_only_confirmed_primary_people_and_resolves_vengat_to_sess_28()
    {
        var sql = EmployeeRoleGovernancePhase2Sql.Up;
        foreach (var employee in new[] { "SESS-01", "SESS-02", "SESS-12", "SESS-14", "SESS-15",
                     "SESS-16", "SESS-25", "SESS-33", "SESS-35", "SESS-41" })
            Assert.Contains($"('{employee}',", sql, StringComparison.Ordinal);
        Assert.Contains("e.\"EmployeeCode\"='SESS-28'", sql, StringComparison.Ordinal);
        Assert.Contains("upper(e.\"EmployeeName\") LIKE 'VENKAT RAV%'", sql, StringComparison.Ordinal);
        Assert.Contains("'SERVICE_COORDINATOR'", sql, StringComparison.Ordinal);
        Assert.Contains("'PENDING'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_has_both_cluster_guards_and_database_enforced_overlap_rules()
    {
        var directory = Source("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations");
        var migration = Directory.GetFiles(directory, "*_EmployeeRoleGovernancePhase2.cs").Single();
        var code = File.ReadAllText(migration);
        Assert.Equal(2, Count(code, "PostgreSqlClusterGuard.Require(migrationBuilder)"));
        Assert.Contains("EmployeeRoleGovernancePhase2Sql.DownBeforeTables", code, StringComparison.Ordinal);
        Assert.Contains("EmployeeRoleGovernancePhase2Sql.DownAfterTables", code, StringComparison.Ordinal);
        Assert.Contains("EX_employee_role_assignment_no_overlap", EmployeeRoleGovernancePhase2Sql.Up, StringComparison.Ordinal);
        Assert.Contains("EX_employee_role_assignment_one_primary", EmployeeRoleGovernancePhase2Sql.Up, StringComparison.Ordinal);
        Assert.Contains("DEFERRABLE INITIALLY DEFERRED", EmployeeRoleGovernancePhase2Sql.Up, StringComparison.Ordinal);
        Assert.Contains("validate_employee_primary_role", EmployeeRoleGovernancePhase2Sql.Up, StringComparison.Ordinal);
    }

    private static void AssertMandatory(Type type, params string[] names)
    {
        var actual = type.GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(names, name => Assert.Contains(name, actual));
    }

    private static int Count(string value, string term) =>
        value.Split(term, StringSplitOptions.None).Length - 1;

    private static string Source(params string[] parts) =>
        Path.Combine([FindRoot(), .. parts]);

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "SESS.NexaERP.slnx")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}