using System.Security.Claims;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string LoginId => ClaimValue(ClaimTypes.Email)
        ?? ClaimValue(ClaimTypes.NameIdentifier)
        ?? ClaimValue("preferred_username")
        ?? "unauthenticated";

    public string RoleCode => ClaimValue(ClaimTypes.Role) ?? ClaimValue("role") ?? "none";

    public string? OrganizationId => ClaimValue("organization_id") ?? ClaimValue("org_id") ?? ClaimValue("portal_organization_id");

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    private string? ClaimValue(string claimType)
    {
        return httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}

