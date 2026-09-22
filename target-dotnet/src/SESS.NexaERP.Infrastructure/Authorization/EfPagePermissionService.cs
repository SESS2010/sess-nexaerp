using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public sealed class EfPagePermissionService(NexaErpDbContext db) : IPagePermissionService
{
    public static readonly IReadOnlyList<string> SupportedActions =
    [
        PagePermissionActions.View, PagePermissionActions.Create, PagePermissionActions.Update,
        PagePermissionActions.Submit, PagePermissionActions.Issue, PagePermissionActions.Verify,
        PagePermissionActions.Approve, PagePermissionActions.Reject, PagePermissionActions.RequestClarification,
        PagePermissionActions.RequestRevision, PagePermissionActions.Resubmit, PagePermissionActions.Cancel,
        PagePermissionActions.Deactivate, PagePermissionActions.Print, PagePermissionActions.Download,
        PagePermissionActions.Export, PagePermissionActions.UploadAttachment, PagePermissionActions.ReplaceAttachment,
        PagePermissionActions.ViewCommercialValues, PagePermissionActions.ViewAuditHistory
    ];

    public async Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes, string pageKey, string permission, CancellationToken cancellationToken)
    {
        var normalizedRoles = NormalizeRoles(roleCodes);
        if (normalizedRoles.Length == 0) return false;
        var normalizedPage = NormalizePage(pageKey);
        var normalizedPermission = NormalizePermission(permission);
        var grants = await RoleGrants(normalizedRoles, normalizedPage).ToListAsync(cancellationToken);
        return grants.Any(grant => RoleGrantAllows(grant, normalizedPage, normalizedPermission));
    }

    public async Task<IReadOnlyList<string>> ResolveEffectivePermissionsAsync(
        IReadOnlyCollection<EffectiveRoleAssignment> assignments,
        string organizationCode,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var roles = NormalizeRoles(assignments.Select(x => x.RoleCode).ToArray());
        var result = new HashSet<string>(RoleAuthorityResolution.UniversalEmployeePermissions, StringComparer.Ordinal);
        if (roles.Length > 0)
        {
            var grants = await RoleGrants(roles).ToListAsync(cancellationToken);
            foreach (var grant in grants)
            {
                var assignmentTypes = assignments
                    .Where(x => string.Equals(x.RoleCode.Trim(), grant.Role!.Code, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.AssignmentType)
                    .ToArray();
                foreach (var action in SupportedActions)
                {
                    var permission = $"{grant.PageDefinition!.PageKey}:{action}";
                    if (RoleGrantAllows(grant, grant.PageDefinition.PageKey, action) &&
                        assignmentTypes.Any(type => RoleAuthorityResolution.CanAssignmentExercise(type, permission)))
                        result.Add(permission);
                }
            }
        }

        var employeeGrants = await EmployeeGrantsAsync(organizationCode, employeeId, cancellationToken);
        foreach (var grant in employeeGrants)
        foreach (var action in SupportedActions)
            if (EmployeeGrantAllows(grant.Permission, action)) result.Add($"{grant.PageKey}:{action}");

        return result.Order(StringComparer.Ordinal).ToArray();
    }

    public async Task<bool> HasEmployeePermissionAsync(string organizationCode, Guid employeeId, string pageKey, string permission, CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(pageKey);
        var normalizedPermission = NormalizePermission(permission);
        var grants = await EmployeeGrantsAsync(organizationCode, employeeId, cancellationToken);
        var grant = grants.SingleOrDefault(x => x.PageKey == normalizedPage);
        return grant is not null && EmployeeGrantAllows(grant.Permission, normalizedPermission);
    }

    public static bool RoleGrantAllows(RolePagePermission grant, string pageKey, string permission)
    {
        var explicitlyGranted = permission switch
        {
            PagePermissionActions.View => grant.CanView,
            PagePermissionActions.Create => grant.CanCreate,
            PagePermissionActions.Update => grant.CanUpdate,
            PagePermissionActions.Submit => grant.CanSubmit,
            PagePermissionActions.Issue => grant.CanIssue,
            PagePermissionActions.Verify => grant.CanVerify,
            PagePermissionActions.Approve => grant.CanApprove,
            PagePermissionActions.Reject => grant.CanReject,
            PagePermissionActions.RequestClarification => grant.CanRequestClarification,
            PagePermissionActions.RequestRevision => grant.CanRequestRevision,
            PagePermissionActions.Resubmit => grant.CanResubmit,
            PagePermissionActions.Cancel => grant.CanCancel,
            PagePermissionActions.Deactivate => grant.CanDeactivate,
            PagePermissionActions.Print => grant.CanPrint,
            PagePermissionActions.Download => grant.CanDownload,
            PagePermissionActions.Export => grant.CanExport,
            PagePermissionActions.UploadAttachment => grant.CanUploadAttachment,
            PagePermissionActions.ReplaceAttachment => grant.CanReplaceAttachment,
            PagePermissionActions.ViewCommercialValues => grant.CanViewCommercialValues,
            PagePermissionActions.ViewAuditHistory => grant.CanViewAuditHistory,
            PagePermissionActions.FullControl => grant.HasFullControl,
            _ => false
        };
        return explicitlyGranted || grant.HasFullControl && !RequiresExplicitGrant(NormalizePage(pageKey));
    }

    private static bool EmployeeGrantAllows(EmployeePermission permission, string action) => action switch
    {
        PagePermissionActions.View => permission.CanView,
        PagePermissionActions.Create => permission.CanCreate,
        PagePermissionActions.Update => permission.CanUpdate,
        PagePermissionActions.Submit => permission.CanSubmit,
        PagePermissionActions.Download => permission.CanDownload,
        PagePermissionActions.ViewAuditHistory => permission.CanViewAuditHistory,
        _ => false
    };

    private IQueryable<RolePagePermission> RoleGrants(string[] roles, string? page = null) => db.RolePagePermissions
        .AsNoTracking().Include(x => x.Role).Include(x => x.PageDefinition)
        .Where(x => x.Role != null && x.PageDefinition != null && roles.Contains(x.Role.Code) &&
            x.Role.IsActive && x.PageDefinition.IsActive && (page == null || x.PageDefinition.PageKey == page));

    private async Task<IReadOnlyList<EmployeeGrantProjection>> EmployeeGrantsAsync(
        string organizationCode, Guid employeeId, CancellationToken cancellationToken)
    {
        var organization = organizationCode.Trim().ToUpperInvariant();
        var companyId = await db.Companies.AsNoTracking()
            .Where(x => x.Code == organization && x.IsActive)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (!companyId.HasValue) return [];
        return await db.EmployeePagePermissions.AsNoTracking()
            .Where(x => x.CompanyId == companyId.Value && x.EmployeeId == employeeId &&
                x.PageDefinition != null && x.PageDefinition.IsActive)
            .Select(x => new EmployeeGrantProjection(x.PageDefinition!.PageKey,
                new EmployeePermission(x.CanView, x.CanCreate, x.CanUpdate, x.CanSubmit, x.CanDownload, x.CanViewAuditHistory)))
            .ToListAsync(cancellationToken);
    }

    private static bool RequiresExplicitGrant(string page) => page is "purchase.rfq" or "purchase.vendor-quotations" or
        "purchase.technical-verification" or "purchase.commercial-comparisons" or "purchase.po" or
        "purchase.material-followup" or "purchase.requisition-approvals" or "inventory.grn";
    private static string[] NormalizeRoles(IReadOnlyCollection<string> roles) => roles.Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
    private static string NormalizePage(string value) => value.Trim().ToLowerInvariant();
    private static string NormalizePermission(string value) => value.Trim().ToLowerInvariant();
    private sealed record EmployeePermission(bool CanView, bool CanCreate, bool CanUpdate, bool CanSubmit, bool CanDownload, bool CanViewAuditHistory);
    private sealed record EmployeeGrantProjection(string PageKey, EmployeePermission Permission);
}