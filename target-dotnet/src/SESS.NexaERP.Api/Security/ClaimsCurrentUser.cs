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

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    private string? ClaimValue(string claimType)
    {
        return httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}
