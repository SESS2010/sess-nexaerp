namespace SESS.NexaERP.Application.Authorization;

public sealed record PageDefinitionSummary(Guid Id, string PageKey, string Module, string Title, string Route, bool IsActive);

public sealed record CreatePageDefinitionRequest(string PageKey, string Module, string Title, string Route);

public sealed record RolePagePermissionSummary(
    Guid Id,
    string RoleCode,
    string PageKey,
    bool CanView,
    bool CanCreate,
    bool CanUpdate,
    bool CanApprove,
    bool CanExport);

public sealed record UpsertRolePagePermissionRequest(
    string RoleCode,
    string PageKey,
    bool CanView,
    bool CanCreate,
    bool CanUpdate,
    bool CanApprove,
    bool CanExport);
