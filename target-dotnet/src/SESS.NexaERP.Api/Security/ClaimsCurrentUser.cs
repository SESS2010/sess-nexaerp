using System.Security.Claims;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

public sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string LoginId => IdentitySubject
        ?? ClaimValue(ClaimTypes.Email)
        ?? ClaimValue(ClaimTypes.NameIdentifier)
        ?? ClaimValue("preferred_username")
        ?? "unauthenticated";

    public string RoleCode => ClaimValue(ClaimTypes.Role) ?? ClaimValue("role") ?? "none";

    public string? OrganizationId => ClaimValue("organization_id") ?? ClaimValue("org_id") ?? ClaimValue("portal_organization_id");

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public string? IdentityIssuer => ClaimValue("iss");
    public string? IdentitySubject => ClaimValue("sub");
    public Guid? EmployeeId => ParseGuid(ClaimValue("sess_employee_id"));
    public Guid? DepartmentId => ParseGuid(ClaimValue("sess_department_id"));

    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var parsed) ? parsed : null;

    private string? ClaimValue(string claimType)
    {
        return httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
    }
}

