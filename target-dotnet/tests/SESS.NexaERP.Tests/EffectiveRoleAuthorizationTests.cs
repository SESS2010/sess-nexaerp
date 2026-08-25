using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Tests;

public sealed class EffectiveRoleAuthorizationTests
{
    [Fact]
    public void Current_user_uses_database_role_set_and_never_oidc_role_claims()
    {
        var context = ContextWithResolution(
            ["IT_MANAGER", "TECHNICAL_DIRECTOR"],
            new Claim(ClaimTypes.Role, "MANAGING_DIRECTOR"));
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(["IT_MANAGER", "TECHNICAL_DIRECTOR"], currentUser.RoleCodes);
        Assert.Equal("none", currentUser.RoleCode);
        Assert.DoesNotContain("MANAGING_DIRECTOR", currentUser.RoleCodes);
    }

    [Fact]
    public void Exactly_one_effective_database_role_remains_the_legacy_scalar_acting_role()
    {
        var context = ContextWithResolution(["IT_MANAGER"], new Claim("roles", "MANAGING_DIRECTOR"));
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(["IT_MANAGER"], currentUser.RoleCodes);
        Assert.Equal("IT_MANAGER", currentUser.RoleCode);
    }

    [Fact]
    public void Raw_token_policies_are_removed_and_identity_endpoints_have_database_page_requirements()
    {
        var root = FindRoot();
        var securityFile = Path.Combine(root, "src", "SESS.NexaERP.Api", "Security", "AuthorizationPolicies.cs");
        var identity = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "IdentityEndpoints.cs"));
        var program = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Program.cs"));

        Assert.False(File.Exists(securityFile));
        Assert.DoesNotContain("AdminOnly", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("MasterDataWrite", program, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryWrite", program, StringComparison.Ordinal);
        Assert.Equal(2, identity.Split("identity.roles", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, identity.Split("identity.users", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, identity.Split("PagePermissionActions.View", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, identity.Split("PagePermissionActions.Create", StringSplitOptions.None).Length - 1);
    }

    private static DefaultHttpContext ContextWithResolution(
        IReadOnlyList<string> roleCodes,
        params Claim[] extraClaims)
    {
        var claims = new[] { new Claim("iss", "https://issuer.example"), new Claim("sub", "subject-1") }
            .Concat(extraClaims);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        context.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey] = new ResolvedEmployeeIdentity(
            true,
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            Guid.Parse("90000000-0000-0000-0000-000000000002"),
            "SESS",
            "SESS-01",
            roleCodes,
            "resolved");
        return context;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
