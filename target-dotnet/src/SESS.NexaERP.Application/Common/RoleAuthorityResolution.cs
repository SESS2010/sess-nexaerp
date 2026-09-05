namespace SESS.NexaERP.Application.Common;

public static class RoleAuthorityResolution
{
    private static readonly HashSet<string> SupportDeniedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "approve", "reject", "cancel", "reverse", "deactivate",
        "permission-configuration", "role-administration"
    };

    public static string RequireRole(this ICurrentUser user, string operation, params string[] sufficientRoles)
    {
        if (sufficientRoles.Length == 0) throw new ArgumentException("At least one sufficient role is required.", nameof(sufficientRoles));
        var normalized = sufficientRoles.Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        var deniedToSupport = IsSupportDenied(operation);
        var candidate = user.EffectiveRoleAssignments
            .Where(x => x.AssignmentId != Guid.Empty &&
                normalized.Contains(x.RoleCode.Trim().ToUpperInvariant(), StringComparer.Ordinal) &&
                (!deniedToSupport || !string.Equals(x.AssignmentType, "SUPPORT", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => PrivilegeRank(x.RoleCode))
            .ThenBy(x => AssignmentRank(x.AssignmentType))
            .ThenBy(x => x.AssignmentId)
            .FirstOrDefault();
        if (candidate is null)
            throw new UnauthorizedAccessException($"Required role: {string.Join(" or ", normalized)}.");
        var authority = new ResolvedRoleAuthority(candidate.AssignmentId,
            candidate.RoleCode.Trim().ToUpperInvariant(), candidate.AssignmentType.Trim().ToUpperInvariant());
        user.SetResolvedRoleAuthority(authority);
        return authority.RoleCode;
    }

    public static bool IsSupportDenied(string operation)
    {
        var normalized = operation.Trim().ToLowerInvariant();
        if (normalized.Contains("permission", StringComparison.Ordinal) ||
            normalized.Contains("employee-role", StringComparison.Ordinal) ||
            normalized.Contains("role-administration", StringComparison.Ordinal)) return true;
        return SupportDeniedActions.Any(x => normalized.Equals(x, StringComparison.Ordinal) ||
            normalized.StartsWith(x, StringComparison.Ordinal) ||
            normalized.EndsWith(":" + x, StringComparison.Ordinal));
    }

    private static int AssignmentRank(string type) => type.Trim().ToUpperInvariant() switch
    {
        "SUPPORT" => 0,
        "TEMPORARY" => 1,
        "FULL" => 2,
        _ => int.MaxValue
    };

    private static int PrivilegeRank(string role)
    {
        var code = role.Trim().ToUpperInvariant();
        if (code.Contains("ASSISTANT", StringComparison.Ordinal) || code.Contains("OPERATOR", StringComparison.Ordinal) ||
            code.Contains("JUNIOR", StringComparison.Ordinal)) return 10;
        if (code.Contains("EXECUTIVE", StringComparison.Ordinal) || code.Contains("ENGINEER", StringComparison.Ordinal) ||
            code.Contains("COORDINATOR", StringComparison.Ordinal) || code.Contains("DEVELOPER", StringComparison.Ordinal)) return 20;
        if (code.Contains("MANAGER", StringComparison.Ordinal)) return 30;
        if (code.Contains("DIRECTOR", StringComparison.Ordinal)) return 40;
        return 25;
    }
}
