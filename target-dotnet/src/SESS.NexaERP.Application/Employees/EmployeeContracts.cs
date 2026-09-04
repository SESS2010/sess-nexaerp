namespace SESS.NexaERP.Application.Employees;

public sealed record EmployeeSummary(
    Guid Id,
    string EmployeeCode,
    string EmployeeName,
    string EmployeeType,
    string Grade,
    string Department,
    string SkillCategory,
    string JobDesignation,
    string Status,
    bool LoginEnabled,
    string ApprovalStatus,
    uint Version);

public sealed record EmployeeDetail(
    Guid Id,
    string EmployeeCode,
    string EmployeeName,
    string OriginalImportedName,
    string EmployeeType,
    string Grade,
    string Department,
    IReadOnlyList<string> SkillCategories,
    string JobDesignation,
    string Status,
    DateOnly? DateOfJoining,
    string? OfficialEmail,
    string? MobileNumber,
    bool LoginEnabled,
    string ApprovalStatus,
    IReadOnlyList<EmployeeRoleSummary> Roles,
    uint Version);

public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string EmployeeName,
    string EmployeeType,
    string Grade,
    string DepartmentCode,
    string SkillCode,
    string DesignationCode,
    DateOnly? DateOfJoining,
    string? OfficialEmail,
    string? MobileNumber,
    string Remarks);

public sealed record UpdateEmployeeRequest(
    string EmployeeName,
    string EmployeeType,
    string Grade,
    string DepartmentCode,
    string SkillCode,
    string DesignationCode,
    DateOnly? DateOfJoining,
    string? OfficialEmail,
    string? MobileNumber,
    string Reason,
    uint Version);

public sealed record EmployeeApprovalRequest(string Remarks, uint Version);

public sealed record LoginStatusRequest(string Reason, uint Version);

public sealed record AssignEmployeeRoleRequest(
    string RoleCode,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Remarks,
    string AssignmentType = "PERMANENT",
    bool IsPrimary = false,
    uint? ProfileVersion = null);

public sealed record PromoteEmployeeRoleRequest(
    string NewRoleCode,
    DateOnly EffectiveOn,
    bool KeepPreviousRoleAsSecondary,
    string Remarks,
    uint ProfileVersion);

public sealed record TransferEmployeeRoleRequest(
    string NewRoleCode,
    DateOnly EffectiveOn,
    bool KeepPreviousRoleAsSecondary,
    string Remarks,
    uint ProfileVersion);

public sealed record TemporaryRoleCoverRequest(
    string RoleCode,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    string Remarks);

public sealed record ChangePrimaryRoleRequest(
    Guid AssignmentId,
    DateOnly EffectiveOn,
    bool KeepPreviousRoleAsSecondary,
    string Remarks,
    uint ProfileVersion);

public sealed record EndEmployeeRoleAssignmentRequest(
    DateOnly EffectiveTo,
    string Reason,
    uint Version);

public sealed record EmployeeRoleSummary(
    Guid Id,
    string RoleCode,
    string RoleName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string ApprovalStatus,
    string Remarks,
    string AssignmentType = "PERMANENT",
    bool IsPrimary = false,
    string? EndReason = null,
    DateTimeOffset? EndedAt = null,
    string? EndedBy = null,
    uint Version = 0);

public sealed record EmployeeRoleProfileSummary(
    string EmployeeCode,
    string CompanyCode,
    string ConfigurationStatus,
    Guid? PrimaryRoleAssignmentId,
    string? PrimaryRoleCode,
    uint Version,
    IReadOnlyList<EmployeeRoleSummary> Assignments);

public sealed record EmployeeRoleAssignmentEventSummary(
    Guid Id,
    string Operation,
    string? FromRoleCode,
    string? ToRoleCode,
    bool? PreviousRoleRetained,
    DateOnly EffectiveOn,
    string Reason,
    string ActorLoginId,
    string ActorRoleCode,
    DateTimeOffset CreatedAt);
public sealed record MasterLookupItem(string Code, string Name);

public sealed record EmployeeMasterLookups(
    IReadOnlyList<MasterLookupItem> Departments,
    IReadOnlyList<MasterLookupItem> Skills,
    IReadOnlyList<MasterLookupItem> Designations);

public sealed record EmployeeHistorySummary(Guid Id, string Action, string FromStatus, string ToStatus, string Remarks, DateTimeOffset CreatedAt, string CreatedBy);
