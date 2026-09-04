namespace SESS.NexaERP.Application.Identity;

public sealed record RoleSummary(
    Guid Id,
    string Code,
    string Name,
    bool IsPrivileged,
    bool IsActive,
    string Audience = "INTERNAL_EMPLOYEE",
    string BusinessArea = "GENERAL",
    bool IsEmployeeAssignable = true,
    string? ReplacementRoleCode = null,
    uint Version = 0);

public sealed record CreateRoleRequest(string Code, string Name, bool IsPrivileged);

public sealed record UpdateRoleGovernanceRequest(
    string Name,
    bool IsPrivileged,
    bool IsActive,
    string Audience,
    string BusinessArea,
    bool IsEmployeeAssignable,
    string? ReplacementRoleCode,
    uint Version);

public sealed record CompanyRoleActivationSummary(
    Guid RoleId,
    string RoleCode,
    string RoleName,
    string Audience,
    string BusinessArea,
    bool IsEmployeeAssignable,
    Guid? ActivationId,
    bool IsEnabled,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Remarks,
    uint? Version);

public sealed record UpdateCompanyRoleActivationRequest(
    bool IsEnabled,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Remarks,
    uint? Version);

public sealed record UserAccountSummary(Guid Id, string LoginId, string DisplayName, string Email, string UserType, string RoleCode, bool MfaRequired, bool IsActive);

public sealed record CreateUserAccountRequest(string LoginId, string DisplayName, string Email, string UserType, string RoleCode, bool MfaRequired);

public sealed record SessionMe(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid CompanyId,
    string OrganizationId,
    Guid DepartmentId,
    string DepartmentCode,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> Permissions,
    string IdentityIssuer,
    string IdentitySubject);

public interface ISessionService
{
    Task<SessionMe> GetCurrentAsync(CancellationToken cancellationToken);
}
