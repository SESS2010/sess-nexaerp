using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SESS.NexaERP.Api.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string MasterDataWrite = "MasterDataWrite";
    public const string InventoryWrite = "InventoryWrite";

    private static readonly string[] AdminRoles = ["admin", "it_admin", "md"];
    private static readonly string[] MasterRoles = ["admin", "it_admin", "md", "purchase_head", "store_head", "sales_head", "PURCHASE_MANAGER", "STORES_MANAGER"];
    private static readonly string[] InventoryRoles = ["admin", "it_admin", "md", "store_head", "purchase_head", "STORES_MANAGER", "PURCHASE_MANAGER"];

    public static bool HasAnyRole(ClaimsPrincipal user, params string[] allowedRoles)
    {
        var actualRoles = user.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(role => role.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allowedRoles.Any(actualRoles.Contains);
    }

    public static void AddSessPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => HasAnyRole(context.User, AdminRoles)));

        options.AddPolicy(MasterDataWrite, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => HasAnyRole(context.User, MasterRoles)));

        options.AddPolicy(InventoryWrite, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => HasAnyRole(context.User, InventoryRoles)));
    }
}
