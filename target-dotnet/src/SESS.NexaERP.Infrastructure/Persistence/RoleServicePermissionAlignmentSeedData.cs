using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

internal static class RoleServicePermissionAlignmentSeedData
{
    internal static readonly RolePagePermission[] RolePagePermissions =
    [
        .. TaxCreateOvergrantCorrections(),
        TechnicalVerificationCorrection(),
        AccountsManagerTaxCreatorPermission()
    ];

    private static IEnumerable<RolePagePermission> TaxCreateOvergrantCorrections()
    {
        var taxPage = Rev869ASeedData.Pages.Single(x => x.PageKey == "settings.tax-gst").Id;
        var correctedRoles = new[] { Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.TechnicalDirector, Rev869ARoleCodes.ManagingDirector };
        return correctedRoles.Select(roleCode => WithoutAdvertisedCreate(
            Rev869ASeedData.RolePagePermissions.Single(x => x.PageDefinitionId == taxPage && RoleCode(x.RoleId) == roleCode)));
    }

    private static RolePagePermission TechnicalVerificationCorrection()
    {
        var page = Rev869BSeedData.Pages.Single(x => x.PageKey == "purchase.technical-verification").Id;
        var source = Rev869BSeedData.RolePagePermissions.Single(x => x.PageDefinitionId == page && RoleCode(x.RoleId) == Rev869ARoleCodes.ManagingDirector);
        return Copy(source, canVerify: false, hasFullControl: false);
    }

    private static RolePagePermission WithoutAdvertisedCreate(RolePagePermission source) => Copy(
        source, canCreate: false, canUpdate: false, canSubmit: false, canResubmit: false,
        canCancel: false, canUploadAttachment: false,
        hasFullControl: RoleCode(source.RoleId) == Rev869ARoleCodes.ManagingDirector ? false : source.HasFullControl);

    private static RolePagePermission AccountsManagerTaxCreatorPermission() => new()
    {
        Id = Guid.Parse("84000000-0000-0000-0000-000000000009"),
        RoleId = MultiCompanyEmployeeAuthorizationPart1SeedData.Roles.Single(x => x.Code == Rev869ARoleCodes.AccountsManager).Id,
        PageDefinitionId = Rev869ASeedData.Pages.Single(x => x.PageKey == "settings.tax-gst").Id,
        CanView = true, CanCreate = true, CanPrint = true, CanDownload = true,
        CanViewCommercialValues = true, CanViewAuditHistory = true,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = "migration-role-service-permission-alignment"
    };

    private static string RoleCode(Guid roleId) => FoundationSeedData.Roles
        .Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
        .Single(x => x.Id == roleId).Code.ToUpperInvariant();

    private static RolePagePermission Copy(RolePagePermission x,
        bool? canCreate = null, bool? canUpdate = null, bool? canSubmit = null,
        bool? canVerify = null, bool? canResubmit = null, bool? canCancel = null,
        bool? canUploadAttachment = null, bool? hasFullControl = null) => new()
    {
        Id=x.Id, RoleId=x.RoleId, PageDefinitionId=x.PageDefinitionId,
        CanView=x.CanView, CanCreate=canCreate??x.CanCreate, CanUpdate=canUpdate??x.CanUpdate,
        CanSubmit=canSubmit??x.CanSubmit, CanIssue=x.CanIssue, CanVerify=canVerify??x.CanVerify,
        CanApprove=x.CanApprove, CanReject=x.CanReject, CanRequestClarification=x.CanRequestClarification,
        CanRequestRevision=x.CanRequestRevision, CanResubmit=canResubmit??x.CanResubmit,
        CanCancel=canCancel??x.CanCancel, CanDeactivate=x.CanDeactivate, CanPrint=x.CanPrint,
        CanDownload=x.CanDownload, CanExport=x.CanExport, CanUploadAttachment=canUploadAttachment??x.CanUploadAttachment,
        CanReplaceAttachment=x.CanReplaceAttachment, CanViewCommercialValues=x.CanViewCommercialValues,
        CanViewAuditHistory=x.CanViewAuditHistory, HasFullControl=hasFullControl??x.HasFullControl,
        CreatedAt=x.CreatedAt, CreatedBy=x.CreatedBy, UpdatedAt=x.UpdatedAt, UpdatedBy=x.UpdatedBy, Version=x.Version
    };
}