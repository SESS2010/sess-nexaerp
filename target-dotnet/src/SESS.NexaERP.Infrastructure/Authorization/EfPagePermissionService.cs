using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public sealed class EfPagePermissionService(NexaErpDbContext db) : IPagePermissionService
{
    public async Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken cancellationToken)
    {
        var normalizedRole = roleCode.Trim().ToLowerInvariant();
        var normalizedPage = pageKey.Trim().ToLowerInvariant();
        var normalizedPermission = permission.Trim().ToLowerInvariant();

        var grant = await db.RolePagePermissions
            .AsNoTracking()
            .Include(item => item.Role)
            .Include(item => item.PageDefinition)
            .SingleOrDefaultAsync(item => item.Role != null
                && item.PageDefinition != null
                && item.Role.Code == normalizedRole
                && item.PageDefinition.PageKey == normalizedPage
                && item.Role.IsActive
                && item.PageDefinition.IsActive,
                cancellationToken);

        if (grant is null)
        {
            return false;
        }

        return grant.HasFullControl || normalizedPermission switch
        {
            PagePermissionActions.View => grant.CanView,
            PagePermissionActions.Create => grant.CanCreate,
            PagePermissionActions.Update => grant.CanUpdate,
            PagePermissionActions.Submit => grant.CanSubmit,
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
    }
}
