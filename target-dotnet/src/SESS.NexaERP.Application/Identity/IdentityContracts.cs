namespace SESS.NexaERP.Application.Identity;

public sealed record RoleSummary(Guid Id, string Code, string Name, bool IsPrivileged, bool IsActive);

public sealed record CreateRoleRequest(string Code, string Name, bool IsPrivileged);

public sealed record UserAccountSummary(Guid Id, string LoginId, string DisplayName, string Email, string UserType, string RoleCode, bool MfaRequired, bool IsActive);

public sealed record CreateUserAccountRequest(string LoginId, string DisplayName, string Email, string UserType, string RoleCode, bool MfaRequired);
