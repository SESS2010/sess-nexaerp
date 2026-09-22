using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Http_witness_users_start_unresolved_on_every_request_and_reject_unassigned_authority()
    {
        var employee = Guid.NewGuid();
        var assignment = new EffectiveRoleAssignment(Guid.NewGuid(), "STORES_MANAGER", "FULL");
        var selection = new TaxWorkflowUser(employee, "witness", assignment.RoleCode,
            new Dictionary<string, EffectiveRoleAssignment>
            {
                [TaxWorkflowUser.AssignmentKey(employee, assignment.RoleCode)] = assignment
            });
        selection.SetOrganization("SESS_PROPRIETORSHIP");
        var first = selection.ForRequest();
        Assert.Equal("none", first.RoleCode);
        Assert.Equal("STORES_MANAGER", first.RequireRole("create", "STORES_MANAGER"));
        var second = selection.ForRequest();
        Assert.Equal("none", selection.RoleCode);
        Assert.Equal("none", second.RoleCode);
        Assert.Null(second.ResolvedRoleAssignmentId);
        Assert.Equal("SESS_PROPRIETORSHIP", second.OrganizationId);
        Assert.Equal(selection.EffectiveRoleAssignments, second.EffectiveRoleAssignments);
        Assert.Throws<UnauthorizedAccessException>(() => second.SetResolvedRoleAuthority(
            new(Guid.NewGuid(), "STORES_MANAGER", "FULL")));
    }
}