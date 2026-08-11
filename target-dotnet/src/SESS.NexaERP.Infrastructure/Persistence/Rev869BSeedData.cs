using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev869BSeedData
{
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    private static readonly DateOnly EffectiveFrom = new(2026, 8, 11);

    public static readonly PageDefinition[] Pages =
    [
        Page("purchase.vendor-quotations", "Vendor Quotations", "/purchase/vendor-quotations"),
        Page("purchase.technical-verification", "Technical Verification", "/purchase/technical-verification"),
        Page("purchase.commercial-comparisons", "Commercial Comparisons", "/purchase/commercial-comparisons"),
        Page("purchase.material-followup", "Material Follow-up", "/purchase/material-followup")
    ];

    public static readonly PurchaseTransactionApprovalPolicy[] ApprovalPolicies =
    [
        Policy(Rev869BApprovalRoutes.Manager, 0m, 50000m, Rev869ARoleCodes.PurchaseManager),
        Policy(Rev869BApprovalRoutes.TechnicalDirector, 50000.000001m, 500000m, Rev869ARoleCodes.TechnicalDirector),
        Policy(Rev869BApprovalRoutes.ManagingDirector, 500000.000001m, Rev869BCommercialCalculator.MaximumSupportedValue, Rev869ARoleCodes.ManagingDirector)
    ];

    public static IReadOnlyList<RolePagePermission> RolePagePermissions
    {
        get
        {
            var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
                .GroupBy(x => Rev869ARoleCodes.Normalize(x.Code)).ToDictionary(x => x.Key, x => x.First());
            var pages = new[]
            {
                FoundationSeedData.Pages.Single(x => x.PageKey == "purchase.rfq"),
                Pages.Single(x => x.PageKey == "purchase.vendor-quotations"),
                Pages.Single(x => x.PageKey == "purchase.technical-verification"),
                Pages.Single(x => x.PageKey == "purchase.commercial-comparisons"),
                FoundationSeedData.Pages.Single(x => x.PageKey == "purchase.po"),
                Pages.Single(x => x.PageKey == "purchase.material-followup")
            };
            var roleCodes = new[]
            {
                Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.PurchaseExecutive, "TECHNICAL_ENGINEER",
                Rev869ARoleCodes.TechnicalDirector, Rev869ARoleCodes.ManagingDirector,
                Rev869ARoleCodes.StoresManager, Rev869ARoleCodes.StoresExecutive, "ACCOUNTS_HEAD"
            };
            return roleCodes.SelectMany(code => pages.Select(page => Permission(roles[code], page))).Where(HasAnyGrant).ToList();
        }
    }

    public static Guid PermissionId(string logicalRoleCode, string pageKey) => Id("rev869b-permission", Rev869ARoleCodes.Normalize(logicalRoleCode), pageKey);

    private static RolePagePermission Permission(Role role, PageDefinition page)
    {
        var roleCode = Rev869ARoleCodes.Normalize(role.Code);
        var rfq = page.PageKey == "purchase.rfq";
        var quote = page.PageKey == "purchase.vendor-quotations";
        var technical = page.PageKey == "purchase.technical-verification";
        var comparison = page.PageKey == "purchase.commercial-comparisons";
        var po = page.PageKey == "purchase.po";
        var followUp = page.PageKey == "purchase.material-followup";
        var purchaseExecutive = roleCode == Rev869ARoleCodes.PurchaseExecutive;
        var purchaseManager = roleCode == Rev869ARoleCodes.PurchaseManager;
        var technicalVerifier = roleCode == "TECHNICAL_ENGINEER";
        var director = roleCode is Rev869ARoleCodes.TechnicalDirector or Rev869ARoleCodes.ManagingDirector;
        var stores = roleCode is Rev869ARoleCodes.StoresManager or Rev869ARoleCodes.StoresExecutive;
        var accounts = roleCode == "ACCOUNTS_HEAD";
        var canView = director || purchaseManager || purchaseExecutive && (rfq || quote) ||
            technicalVerifier && (rfq || quote || technical) || stores && (po || followUp) || accounts && (comparison || po);
        var canCreate = purchaseExecutive && (rfq || quote) || purchaseManager && (rfq || quote || comparison || po) || technicalVerifier && technical;
        var canUpdate = canCreate;
        var canSubmit = purchaseExecutive && (rfq || quote) || purchaseManager && (rfq || quote || comparison) || technicalVerifier && technical;
        var canVerify = purchaseManager && (rfq || quote || comparison) || technicalVerifier && technical || director && (technical || comparison);
        var canApprove = (purchaseManager || director) && (comparison || po);
        var canCancel = purchaseManager && (rfq || quote || po) || director && (rfq || quote || po);
        return new RolePagePermission
        {
            Id = PermissionId(roleCode, page.PageKey), RoleId = role.Id, PageDefinitionId = page.Id,
            CanView = canView, CanCreate = canCreate, CanUpdate = canUpdate, CanSubmit = canSubmit,
            CanVerify = canVerify, CanApprove = canApprove, CanReject = canApprove,
            CanRequestClarification = canView && (rfq || technical || comparison), CanRequestRevision = canApprove,
            CanResubmit = purchaseManager && comparison, CanCancel = canCancel, CanDeactivate = false,
            CanPrint = canView, CanDownload = canView, CanExport = canView && (accounts || director || purchaseManager),
            CanUploadAttachment = canCreate, CanReplaceAttachment = false,
            CanViewCommercialValues = canView && (purchaseExecutive || purchaseManager || director || accounts),
            CanViewAuditHistory = canView && (purchaseManager || director || accounts), HasFullControl = roleCode == Rev869ARoleCodes.ManagingDirector,
            CreatedAt = SeedTime, CreatedBy = "migration-rev869b"
        };
    }

    private static bool HasAnyGrant(RolePagePermission x) => x.CanView || x.CanCreate || x.CanUpdate || x.CanSubmit || x.CanVerify || x.CanApprove || x.CanReject || x.CanRequestClarification || x.CanRequestRevision || x.CanResubmit || x.CanCancel || x.CanDeactivate || x.CanPrint || x.CanDownload || x.CanExport || x.CanUploadAttachment || x.CanReplaceAttachment || x.CanViewCommercialValues || x.CanViewAuditHistory || x.HasFullControl;

    private static PurchaseTransactionApprovalPolicy Policy(string route, decimal min, decimal? max, string role) => new()
    {
        Id = Id("rev869b-approval-policy", "SESS", route), OrganizationId = "SESS", RouteCode = route,
        MinimumAmount = min, MaximumAmount = max, ApproverRoleCode = role, EffectiveFrom = EffectiveFrom,
        IsActive = true, CreatedAt = SeedTime, CreatedBy = "migration-rev869b"
    };

    private static PageDefinition Page(string key, string title, string route) => new()
    {
        Id = Id("rev869b-page", key), PageKey = key, Module = "Purchase", Title = title, Route = route,
        IsActive = true, CreatedAt = SeedTime, CreatedBy = "migration-rev869b"
    };

    private static Guid Id(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts)));
        return new Guid(bytes[..16]);
    }
}
