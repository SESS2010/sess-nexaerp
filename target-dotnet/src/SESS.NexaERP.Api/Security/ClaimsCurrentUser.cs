using System.Security.Claims;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Api.Security;

public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private HttpContext? Context => httpContextAccessor.HttpContext;
    private ResolvedEmployeeIdentity? Resolution => Context?.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey] as ResolvedEmployeeIdentity;

    public string LoginId => Resolution?.Success == true && !string.IsNullOrWhiteSpace(IdentitySubject) ? IdentitySubject! : "unauthenticated";
    public string RoleCode
    {
        get
        {
            if (Resolution?.Success != true) return "none";
            var claimed = Context?.User.Claims
                .Where(x => x.Type is ClaimTypes.Role or "role" or "roles")
                .SelectMany(x => x.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .FirstOrDefault(x => Resolution.RoleCodes.Contains(x, StringComparer.OrdinalIgnoreCase));
            return claimed ?? (Resolution.RoleCodes.Count == 1 ? Resolution.RoleCodes[0] : "none");
        }
    }
    public string? OrganizationId => Resolution?.Success == true ? Resolution.OrganizationId : null;
    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true && Resolution?.Success == true;
    public string? IdentityIssuer => ClaimValue("iss");
    public string? IdentitySubject => ClaimValue("sub");
    public Guid? EmployeeId => Resolution?.Success == true ? Resolution.EmployeeId : null;
    public Guid? DepartmentId => Resolution?.Success == true ? Resolution.DepartmentId : null;

    private string? ClaimValue(string claimType) => Context?.User.FindFirstValue(claimType);
}