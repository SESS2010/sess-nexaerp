using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed class MirRequesterPolicyTests
{
    [Theory]
    [InlineData("MIR_REQUESTER", true)]
    [InlineData("mir_requester", true)]
    [InlineData("MIR_REQUESTER,STORES_MANAGER", false)]
    [InlineData("MIR_REQUESTER,ACCOUNTS_ASSISTANT", false)]
    [InlineData("DESIGN_ENGINEER,SERVICE_ENGINEER", false)]
    [InlineData("", false)]
    public void RequesterOnlyNeverIncludesSeparatelyApprovedBusinessRoles(string roles, bool only)
    {
        var actor = new Actor(Guid.NewGuid(), roles.Split(',', StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(only, MirRequesterPolicy.IsRequesterOnly(actor));
    }
    [Theory]
    [InlineData("employee.my-mirs", "view", true)]
    [InlineData("employee.my-mirs", "create", true)]
    [InlineData("employee.my-mirs", "update", true)]
    [InlineData("employee.my-mirs", "submit", true)]
    [InlineData("employee.my-mirs", "approve", false)]
    [InlineData("employee.my-mirs", "issue", false)]
    [InlineData("employee.my-mirs", "full-control", false)]
    [InlineData("masters.items", "create", false)]
    [InlineData("stores.material-issue-requests", "view", false)]
    public void PortalActionsCannotBecomeGeneralMasterStoresOrDecisionRights(string page, string action, bool allowed) =>
        Assert.Equal(allowed, MirRequesterPolicy.AllowsPortalAction(page, action));

    [Theory]
    [InlineData("/api/v1/session/me", true)]
    [InlineData("/api/v1/employee/my-mirs", true)]
    [InlineData("/api/v1/employee/my-mirs/returns", true)]
    [InlineData("/api/v1/employee/my-mirs-other", false)]
    [InlineData("/api/v1/session/me-other", false)]
    [InlineData("/api/v1/employee/my-mirs/../company/profile", false)]
    [InlineData("/api/v1/company/profile", false)]
    [InlineData("/api/v1/stores/material-issues", false)]
    [InlineData("/api/v1/inventory/items", false)]
    public void RequesterBoundaryDoesNotDependOnEveryLegacyEndpointHavingAPageFilter(string path, bool allowed) =>
        Assert.Equal(allowed, MirRequesterPolicy.AllowsApiPath(path));

    [Fact]
    public void OwnershipUsesCanonicalEmployeeNotRoleOrLoginName()
    {
        var id = Guid.NewGuid();
        Assert.True(MirRequesterPolicy.IsOwn(new Actor(id,[MirRequesterPolicy.Role]),id));
        Assert.False(MirRequesterPolicy.IsOwn(new Actor(Guid.NewGuid(),[MirRequesterPolicy.Role]),id));
        Assert.False(MirRequesterPolicy.IsOwn(new Actor(null,[MirRequesterPolicy.Role]),id));
        Assert.False(MirRequesterPolicy.IsOwn(new Actor(Guid.Empty,[MirRequesterPolicy.Role]),Guid.Empty));
    }
    private sealed record Actor(Guid? EmployeeId, IReadOnlyList<string> RoleCodes) : ICurrentUser
    {
        public string LoginId => "test-employee";
        public string RoleCode => RoleCodes.FirstOrDefault() ?? "none";
        public string OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
    }
    [Theory]
    [InlineData("employee.my-mirs","approve",false)]
    [InlineData("masters.items","create",false)]
    [InlineData("stores.material-issues","issue",false)]
    [InlineData("stores.material-issue-requests","create",false)]
    [InlineData("stores.material-issue-requests","approve",true)]
    [InlineData("stores.material-issue-requests","reject",true)]
    [InlineData("stores.material-issue-requests","view",true)]
    public void DecisionOnlyRoleHasNoRequesterMasterOrStoresOperationAuthority(string page,string action,bool expected)
    {
        Assert.True(MirRequesterPolicy.IsMirOnly([MirRequesterPolicy.ApproverRole]));
        Assert.Equal(expected,MirRequesterPolicy.AllowsMirAction([MirRequesterPolicy.ApproverRole],page,action));
        Assert.False(MirRequesterPolicy.AllowsMirAction([MirRequesterPolicy.Role],MirRequesterPolicy.DecisionPage,"approve"));
        Assert.False(MirRequesterPolicy.AllowsMirApiPath([MirRequesterPolicy.ApproverRole],"/api/v1/inventory/items"));
        Assert.False(MirRequesterPolicy.AllowsMirApiPath([MirRequesterPolicy.ApproverRole],"/api/v1/stores/material-issue-requests/../material-issues"));
        Assert.False(MirRequesterPolicy.IsMirOnly([MirRequesterPolicy.ApproverRole,"TECHNICAL_DIRECTOR"]));
    }

}
