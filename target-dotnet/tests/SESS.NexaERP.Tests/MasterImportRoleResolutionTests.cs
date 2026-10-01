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

    [Theory]
    [InlineData("vendors", "PURCHASE_MANAGER", true)]
    [InlineData("vendors", "STORES_MANAGER", false)]
    [InlineData("vendors", "IT_MANAGER", false)]
    [InlineData("items", "STORES_MANAGER", true)]
    [InlineData("items", "PURCHASE_MANAGER", false)]
    [InlineData("uoms", "STORES_MANAGER", true)]
    [InlineData("uoms", "PURCHASE_MANAGER", false)]
    [InlineData("manufacturers", "STORES_MANAGER", true)]
    [InlineData("manufacturers", "IT_MANAGER", false)]
    [InlineData("vendors", "TECHNICAL_DIRECTOR", true)]
    [InlineData("items", "MANAGING_DIRECTOR", true)]
    [InlineData("uoms", "MANAGING_DIRECTOR", true)]
    [InlineData("manufacturers", "TECHNICAL_DIRECTOR", true)]
    public async Task R1_imports_require_the_approved_owner_even_when_another_role_has_page_grants(string master, string role, bool allowed)
    {
        IMasterDataDefinition definition = master switch
        {
            "vendors" => new VendorMasterDataDefinition(),
            "items" => new ItemImportDefinition(),
            "uoms" => new UomMasterDataDefinition(),
            _ => new ManufacturerImportDefinition()
        };
        var service = Service(User(role), new Grants(role, true, true));
        if (allowed) Assert.Equal(role, await service.ResolveOperationalRoleAsync(definition, CancellationToken.None));
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResolveOperationalRoleAsync(definition, CancellationToken.None));
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