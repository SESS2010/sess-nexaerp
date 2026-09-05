using System.Security.Claims;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Api.Security;

public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string ResolvedAuthorityItemKey = "SESS.ResolvedRoleAuthority";
    private HttpContext? Context => httpContextAccessor.HttpContext;
    private ResolvedEmployeeIdentity? Resolution => Context?.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey] as ResolvedEmployeeIdentity;
    private ResolvedRoleAuthority? Authority => Context?.Items[ResolvedAuthorityItemKey] as ResolvedRoleAuthority;

    public string LoginId => Resolution?.Success == true && !string.IsNullOrWhiteSpace(IdentitySubject) ? IdentitySubject! : "unauthenticated";
    public IReadOnlyList<string> RoleCodes => Resolution?.Success == true ? Resolution.RoleCodes : [];
    public IReadOnlyList<string> FullAuthorityRoleCodes => Resolution?.Success == true ? Resolution.FullAuthorityRoleCodes ?? [] : [];
    public IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => Resolution?.Success == true ? Resolution.EffectiveRoleAssignments ?? [] : [];
    public string RoleCode => Authority?.RoleCode ?? "none";
    public Guid? ResolvedRoleAssignmentId => Authority?.AssignmentId;
    public string? ResolvedRoleAssignmentType => Authority?.AssignmentType;
    public string? OrganizationId => Resolution?.Success == true ? Resolution.OrganizationId : null;
    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true && Resolution?.Success == true;
    public string? IdentityIssuer => ClaimValue("iss");
    public string? IdentitySubject => ClaimValue("sub");
    public Guid? EmployeeId => Resolution?.Success == true ? Resolution.EmployeeId : null;
    public Guid? DepartmentId => Resolution?.Success == true ? Resolution.DepartmentId : null;

    public void SetResolvedRoleAuthority(ResolvedRoleAuthority authority)
    {
        if (Context is null || !IsAuthenticated || authority.AssignmentId == Guid.Empty ||
            !EffectiveRoleAssignments.Any(x => x.AssignmentId == authority.AssignmentId &&
                string.Equals(x.RoleCode, authority.RoleCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.AssignmentType, authority.AssignmentType, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Resolved role authority is not an effective assignment for this employee and company.");
        Context.Items[ResolvedAuthorityItemKey] = authority;
    }

    private string? ClaimValue(string claimType) => Context?.User.FindFirstValue(claimType);
}
