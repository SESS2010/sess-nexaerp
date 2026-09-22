using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
namespace SESS.NexaERP.Application.Stores;

/// <summary>Pairs actual role authority with its page permission before selecting receipt scope.</summary>
public static class VendorManualAssessmentAuthority
{
    public static async Task<string> RequireRoleAsync(ICurrentUser user, IPagePermissionService permissions,
        string operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permissions);
        if (operation is not ("view" or "create")) throw new ArgumentOutOfRangeException(nameof(operation));
        const string page = "quality.vendor-manual-assessments";
        var employeeGrant = user.EmployeeId is Guid employee && !string.IsNullOrWhiteSpace(user.OrganizationId)
            && await permissions.HasEmployeePermissionAsync(user.OrganizationId, employee, page, operation, ct);
        foreach (var role in new[] { "QC_MANAGER", "PRODUCTION_MANAGER" })
        {
            var eligible = user.EffectiveRoleAssignments.Any(x => x.AssignmentId != Guid.Empty
                && string.Equals(x.RoleCode.Trim(), role, StringComparison.OrdinalIgnoreCase)
                && RoleAuthorityResolution.CanAssignmentExercise(x.AssignmentType, operation));
            if (eligible && (employeeGrant || await permissions.HasPermissionAsync([role], page, operation, ct)))
                return user.RequireRole(operation, role);
        }
        throw new UnauthorizedAccessException("A permitted QC or receiving Production Manager assignment is required.");
    }
}
