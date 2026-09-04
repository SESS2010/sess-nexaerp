namespace SESS.NexaERP.Application.Common;

public interface ICurrentUser
{
    string LoginId { get; }
    string RoleCode { get; }
    IReadOnlyList<string> RoleCodes =>
        string.IsNullOrWhiteSpace(RoleCode) || string.Equals(RoleCode, "none", StringComparison.OrdinalIgnoreCase)
            ? []
            : [RoleCode];
    string? PrimaryRoleCode => null;
    string ActingRoleCode => RoleCode;
    string? OrganizationId { get; }
    bool IsAuthenticated { get; }
    string? IdentityIssuer => null;
    string? IdentitySubject => null;
    Guid? EmployeeId => null;
    Guid? DepartmentId => null;
}
