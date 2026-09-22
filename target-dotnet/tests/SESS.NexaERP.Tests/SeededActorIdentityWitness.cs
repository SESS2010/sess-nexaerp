using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task AssertResolvedSeedRole(DbContextOptions<NexaErpDbContext> options,
        TaxWorkflowUser user, string role)
    {
        await using var db = new NexaErpDbContext(options);
        var identity = await new EfEmployeeIdentityResolver(db).ResolveAsync(user.IdentityIssuer!, user.IdentitySubject!,
            user.OrganizationId, DateOnly.FromDateTime(DateTime.UtcNow), CancellationToken.None);
        Assert.True(identity.Success, identity.Message);
        Assert.Equal(user.EmployeeId, identity.EmployeeId);
        Assert.Contains(role, identity.RoleCodes);
        var selected = Assert.Single(user.EffectiveRoleAssignments, x => x.RoleCode == role);
        Assert.Contains(identity.EffectiveRoleAssignments!, x => x.AssignmentId == selected.AssignmentId && x.RoleCode == role);
    }
}
