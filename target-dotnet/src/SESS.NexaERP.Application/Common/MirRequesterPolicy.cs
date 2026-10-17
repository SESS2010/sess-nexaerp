namespace SESS.NexaERP.Application.Common;

/// <summary>The requester role grants portal actions only, never a general employee/master exemption.</summary>
public static class MirRequesterPolicy
{
    public const string Role = "MIR_REQUESTER";
    public const string ApproverRole = "MIR_APPROVER";
    public const string DecisionPage = "stores.material-issue-requests";
    public static bool IsMirOnly(IReadOnlyCollection<string> roles) => roles.Count > 0 && roles.All(role => role.Trim().ToUpperInvariant() is Role or ApproverRole);
    public static bool AllowsMirAction(IReadOnlyCollection<string> roles,string page,string action) =>
        roles.Contains(Role,StringComparer.OrdinalIgnoreCase) && AllowsPortalAction(page,action) ||
        roles.Contains(ApproverRole,StringComparer.OrdinalIgnoreCase) && string.Equals(page,DecisionPage,StringComparison.OrdinalIgnoreCase) && action.Trim().ToLowerInvariant() is "view" or "approve" or "reject";
    public static bool AllowsMirApiPath(IReadOnlyCollection<string> roles,string path) =>
        AllowsApiPath(path) || roles.Contains(ApproverRole,StringComparer.OrdinalIgnoreCase) && !path.Split('/').Any(part=>part is "." or "..") &&
        (path.Equals("/api/v1/stores/material-issue-requests",StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/stores/material-issue-requests/",StringComparison.OrdinalIgnoreCase));
    public const string Page = "employee.my-mirs";
    public static bool IsRequesterOnly(ICurrentUser user) => IsRequesterOnly(user.RoleCodes);
    public static bool IsRequesterOnly(IReadOnlyCollection<string> roles) => roles.Count > 0 &&
        roles.All(role => string.Equals(role.Trim(), Role, StringComparison.OrdinalIgnoreCase));
    public static bool AllowsApiPath(string path) =>
        string.Equals(path, "/api/v1/session/me", StringComparison.OrdinalIgnoreCase) ||
        !path.Split('/').Any(segment => segment is "." or "..") &&
        (string.Equals(path, "/api/v1/employee/my-mirs", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("/api/v1/employee/my-mirs/", StringComparison.OrdinalIgnoreCase));
    public static bool AllowsPortalAction(string page, string action) =>
        string.Equals(page.Trim(), Page, StringComparison.OrdinalIgnoreCase) &&
        action.Trim().ToLowerInvariant() is "view" or "create" or "update" or "submit";
    public static bool IsOwn(ICurrentUser user, Guid owner) => user.EmployeeId is { } actor && actor != Guid.Empty && actor == owner;
}
