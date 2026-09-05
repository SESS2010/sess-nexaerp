namespace SESS.NexaERP.Application.Common;

public sealed record EffectiveRoleAssignment(Guid AssignmentId, string RoleCode, string AssignmentType);
public sealed record ResolvedRoleAuthority(Guid AssignmentId, string RoleCode, string AssignmentType);

public interface ICurrentUser
{
    string LoginId { get; }
    string RoleCode { get; }
    IReadOnlyList<string> RoleCodes =>
        string.IsNullOrWhiteSpace(RoleCode) || string.Equals(RoleCode, "none", StringComparison.OrdinalIgnoreCase)
            ? []
            : [RoleCode];
    IReadOnlyList<string> FullAuthorityRoleCodes => [];
    IReadOnlyList<EffectiveRoleAssignment> EffectiveRoleAssignments => [];
    Guid? ResolvedRoleAssignmentId => null;
    string? ResolvedRoleAssignmentType => null;
    void SetResolvedRoleAuthority(ResolvedRoleAuthority authority) =>
        throw new NotSupportedException("This current-user implementation cannot record resolved role authority.");
    string? OrganizationId { get; }
    bool IsAuthenticated { get; }
    string? IdentityIssuer => null;
    string? IdentitySubject => null;
    Guid? EmployeeId => null;
    Guid? DepartmentId => null;
}
