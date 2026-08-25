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
    public IReadOnlyList<string> RoleCodes => Resolution?.Success == true ? Resolution.RoleCodes : [];
    public string RoleCode => RoleCodes.Count == 1 ? RoleCodes[0] : "none";
    public string? OrganizationId => Resolution?.Success == true ? Resolution.OrganizationId : null;
    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true && Resolution?.Success == true;
    public string? IdentityIssuer => ClaimValue("iss");
    public string? IdentitySubject => ClaimValue("sub");
    public Guid? EmployeeId => Resolution?.Success == true ? Resolution.EmployeeId : null;
    public Guid? DepartmentId => Resolution?.Success == true ? Resolution.DepartmentId : null;

    private string? ClaimValue(string claimType) => Context?.User.FindFirstValue(claimType);
}
