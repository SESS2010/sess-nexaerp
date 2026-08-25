using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public sealed class EfPagePermissionService(NexaErpDbContext db) : IPagePermissionService
{
    public async Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleCodes, string pageKey, string permission, CancellationToken cancellationToken)
    {
        var normalizedRoles = roleCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedRoles.Length == 0) return false;
        var normalizedPage = pageKey.Trim().ToLowerInvariant();
        var normalizedPermission = permission.Trim().ToLowerInvariant();

        var grants = await db.RolePagePermissions
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.PageDefinition)
            .Where(item => item.Role != null
                && item.PageDefinition != null
                && normalizedRoles.Contains(item.Role.Code)
                && item.PageDefinition.PageKey == normalizedPage
                && item.Role.IsActive
                && item.PageDefinition.IsActive)
            .ToListAsync(cancellationToken);

        if (grants.Count == 0) return false;

        var requiresExplicitRev869BGrant = normalizedPage is "purchase.rfq" or "purchase.vendor-quotations" or
            "purchase.technical-verification" or "purchase.commercial-comparisons" or "purchase.po" or "purchase.material-followup" or "purchase.requisition-approvals";
        return grants.Any(grant =>
        {
            var explicitlyGranted = normalizedPermission switch
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
            return explicitlyGranted || grant.HasFullControl && !requiresExplicitRev869BGrant;
        });
    }
}
