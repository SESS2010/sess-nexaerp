using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class EmployeeRoleGovernancePhase2Tests
{
    [Fact]
    public void Model_has_typed_dated_assignments_and_immutable_events_without_primary_role()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var assignment = model.FindEntityType(typeof(EmployeeRoleAssignment))!;
        Assert.Contains(assignment.GetProperties(), x => x.Name == "AssignmentType");
        Assert.DoesNotContain(assignment.GetProperties(), x => x.Name == "IsPrimary");
        Assert.Contains(assignment.GetCheckConstraints(), x => x.Name == "CK_employee_role_assignment_dates");
        Assert.NotNull(model.FindEntityType(typeof(EmployeeRoleAssignmentEvent)));
    }

    [Fact]
    public void Request_contracts_cover_explicit_assignment_operations_without_primary_role()
    {
        AssertMandatory(typeof(AssignEmployeeRoleRequest), "RoleCode", "EffectiveFrom", "Remarks", "AssignmentType");
        AssertMandatory(typeof(PromoteEmployeeRoleRequest), "PreviousAssignmentId", "NewRoleCode", "NewAssignmentType", "EffectiveOn", "KeepPreviousAssignment", "Remarks", "PreviousAssignmentVersion");
        AssertMandatory(typeof(TransferEmployeeRoleRequest), "PreviousAssignmentId", "NewRoleCode", "NewAssignmentType", "EffectiveOn", "KeepPreviousAssignment", "Remarks", "PreviousAssignmentVersion");
        AssertMandatory(typeof(TemporaryRoleCoverRequest), "RoleCode", "EffectiveFrom", "EffectiveTo", "Remarks");
        AssertMandatory(typeof(EndEmployeeRoleAssignmentRequest), "EffectiveTo", "Reason", "Version");
    }

    [Fact]
    public void Read_models_expose_assignment_ids_types_dates_versions_and_audit_provenance()
    {
        AssertMandatory(typeof(EmployeeRoleSummary), "Id", "RoleCode", "EffectiveFrom", "EffectiveTo", "AssignmentType", "Version");
        AssertMandatory(typeof(EmployeeRolePortfolioSummary), "EmployeeCode", "CompanyCode", "Assignments");
        AssertMandatory(typeof(AuditLogSummary), "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType");
        AssertMandatory(typeof(SessionMe), "RoleCodes", "FullAuthorityRoleCodes");
    }

    [Fact]
    public void Endpoint_surface_has_runtime_operations_and_no_role_switcher()
    {
        var source = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Endpoints", "EmployeeEndpoints.cs")) +
                     File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Endpoints", "EmployeeRoleGovernanceEndpoints.cs"));
        foreach (var route in new[] { "temporary-cover", "promote", "transfer", "/end", "role-portfolio", "role-events" })
            Assert.Contains(route, source, StringComparison.Ordinal);
        Assert.DoesNotContain("change-primary", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("db.EmployeeCompanyAssignments.Add", source, StringComparison.Ordinal);
        Assert.Contains("db.EmployeeDepartmentAssignments.Add", source, StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_selected_acting_role_is_removed_and_resolution_is_fail_closed()
    {
        var claims = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Security", "ClaimsCurrentUser.cs"));
        var middleware = File.ReadAllText(Source("src", "SESS.NexaERP.Api", "Middleware", "EmployeeIdentityResolutionMiddleware.cs"));
        Assert.DoesNotContain("X-SESS-Acting-Role", middleware, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EffectiveRoleAssignments", claims, StringComparison.Ordinal);
        Assert.Contains("ResolvedAuthorityItemKey", claims, StringComparison.Ordinal);
        Assert.Contains("?? \"none\"", claims, StringComparison.Ordinal);
        Assert.Contains("Required role:", File.ReadAllText(Source("src", "SESS.NexaERP.Application", "Common", "RoleAuthorityResolution.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Least_privileged_sufficient_assignment_is_selected_and_support_is_denied_for_sensitive_verbs()
    {
        var user = new ResolverUser([
            new(Guid.Parse("10000000-0000-0000-0000-000000000001"), "PURCHASE_MANAGER", "FULL"),
            new(Guid.Parse("10000000-0000-0000-0000-000000000002"), "PURCHASE_EXECUTIVE", "FULL"),
            new(Guid.Parse("10000000-0000-0000-0000-000000000003"), "TECHNICAL_DIRECTOR", "SUPPORT")
        ]);
        Assert.Equal("PURCHASE_EXECUTIVE", user.RequireRole("create", "PURCHASE_MANAGER", "PURCHASE_EXECUTIVE"));
        Assert.Equal(Guid.Parse("10000000-0000-0000-0000-000000000002"), user.ResolvedRoleAssignmentId);
        var denied = Assert.Throws<UnauthorizedAccessException>(() => user.RequireRole("approve", "TECHNICAL_DIRECTOR"));
        Assert.Contains("Required role: TECHNICAL_DIRECTOR", denied.Message, StringComparison.Ordinal);
        foreach (var verb in new[] { "approve", "reject", "cancel", "reverse", "deactivate", "permission-configuration", "role-administration" })
            Assert.True(RoleAuthorityResolution.IsSupportDenied(verb));
    }

    [Fact]
    public void Migration_has_full_baseline_database_boundary_and_guards_both_directions()
    {
        var directory = Source("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations");
        var migration = Directory.GetFiles(directory, "*_RevisedEmployeeRoleGovernancePhase2.cs").Single();
        var code = File.ReadAllText(migration);
        var sql = RevisedEmployeeRoleGovernancePhase2Sql.Up;
        Assert.Equal(2, Count(code, "PostgreSqlClusterGuard.Require(migrationBuilder)"));
        Assert.Contains("EX_employee_role_assignment_no_overlap", sql, StringComparison.Ordinal);
        Assert.Contains("resolve_employee_role_authority", sql, StringComparison.Ordinal);
        Assert.Contains("Self role assignment is prohibited", sql, StringComparison.Ordinal);
        Assert.Contains("ActorRoleAssignmentId", sql, StringComparison.Ordinal);
        Assert.Contains("TR_sensitive_audit_assignment_guard", sql, StringComparison.Ordinal);
        Assert.Contains("('SESS-05','TECHNICAL_SUPPORT_MANAGER','SUPPORT')", sql, StringComparison.Ordinal);
        Assert.Contains("purchase.technical-verification", sql, StringComparison.Ordinal);
        Assert.Contains("CanVerify", sql, StringComparison.Ordinal);
        Assert.Contains("('SESS-28','SERVICE_COORDINATOR','SUPPORT')", sql, StringComparison.Ordinal);
        Assert.Contains("('SESS-41','ACCOUNTS_ASSISTANT','SUPPORT')", sql, StringComparison.Ordinal);
        Assert.Equal(42, Enumerable.Range(1, 42).Count(i => sql.Contains($"SESS-{i:00}", StringComparison.Ordinal)));
    }

    private sealed class ResolverUser(IReadOnlyList<EffectiveRoleAssignment> assignments) : ICurrentUser
    {
        private ResolvedRoleAuthority? authority;
        public string LoginId => "test"; public string RoleCode => authority?.RoleCode ?? "none";
        public IReadOnlyList<string> RoleCodes => assignments.Select(x => x.RoleCode).ToArray();
        public IReadOnlyList<string> FullAuthorityRoleCodes => assignments.Where(x => x.AssignmentType != "SUPPORT").Select(x => x.RoleCode).ToArray();
        public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => assignments;
        public Guid? ResolvedRoleAssignmentId => authority?.AssignmentId;
        public string? ResolvedRoleAssignmentType => authority?.AssignmentType;
        public string? OrganizationId => "SESS_PVT_LTD"; public bool IsAuthenticated => true;
        public void SetResolvedRoleAuthority(ResolvedRoleAuthority value) => authority = value;
    }

    private static void AssertMandatory(Type type, params string[] names)
    {
        var actual = type.GetProperties().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(names, name => Assert.Contains(name, actual));
    }
    private static int Count(string value, string term) => value.Split(term, StringSplitOptions.None).Length - 1;
    private static string Source(params string[] parts) => Path.Combine([FindRoot(), .. parts]);
    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "SESS.NexaERP.slnx"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
