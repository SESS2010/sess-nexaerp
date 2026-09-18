using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Tests;

public sealed class MasterImportRoleResolutionTests
{
    [Theory]
    [InlineData("uoms", "TECHNICAL_DIRECTOR")]
    [InlineData("opening-stock", "STORES_MANAGER")]
    public async Task Import_resolves_effective_role_before_any_operation_has_selected_authority(string master, string role)
    {
        var user = User(role);
        Assert.Equal("none", user.RoleCode);
        IMasterDataDefinition definition = master == "uoms" ? new UomMasterDataDefinition() : new OpeningStockImportDefinition();
        var service = Service(user, new Grants(role, true, true));
        Assert.Equal(role, await service.ResolveOperationalRoleAsync(definition, CancellationToken.None));
        Assert.Equal(role, user.RoleCode);
        Assert.NotNull(user.ResolvedRoleAssignmentId);
        Assert.Equal("FULL", user.ResolvedRoleAssignmentType);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Import_requires_both_permissions_on_the_effective_role(bool create, bool update)
    {
        var service = Service(User("TECHNICAL_DIRECTOR"), new Grants("TECHNICAL_DIRECTOR", create, update));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResolveOperationalRoleAsync(new UomMasterDataDefinition(), CancellationToken.None));
    }

    [Fact]
    public async Task Permission_grants_do_not_make_an_unassigned_role_effective()
    {
        var service = Service(User("ACCOUNTS_EXECUTIVE"), new Grants("TECHNICAL_DIRECTOR", true, true));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResolveOperationalRoleAsync(new UomMasterDataDefinition(), CancellationToken.None));
    }

    private static EfMasterDataTransferService Service(ICurrentUser user, IPagePermissionService grants) =>
        new(null!, new MasterDataRegistry([]), user, grants, new SystemClock(), Options.Create(new MasterDataTransferOptions()));

    private static ClaimsCurrentUser User(string role)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "import-test")], "test"))
        };
        context.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey] = new ResolvedEmployeeIdentity(
            true, Guid.NewGuid(), Guid.NewGuid(), "SESS_PVT_LTD", "SESS-01", [role], "resolved", [role],
            [new EffectiveRoleAssignment(Guid.NewGuid(), role, "FULL")]);
        return new ClaimsCurrentUser(new HttpContextAccessor { HttpContext = context });
    }

    private sealed class Grants(string role, bool create, bool update) : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roles, string page, string permission, CancellationToken ct) =>
            Task.FromResult(roles.Count == 1 && roles.Contains(role) && (permission switch
            {
                PagePermissionActions.Create => create,
                PagePermissionActions.Update => update,
                _ => false
            }));
    }
}