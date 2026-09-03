using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev869ASeedData
{
    public static readonly Guid ItManagerEmployeeIdentitiesPermissionId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    public static readonly Guid ItManagerOperationalScopesPermissionId = Guid.Parse("82000000-0000-0000-0000-000000000002");
    public static readonly Guid ApprovedEaUomId = Guid.Parse("f71a4725-bb15-e7bf-e97b-991985e96328");
    public static readonly Guid ApprovedEaHistoryId = Guid.Parse("0007efa3-4888-a87d-45ef-72cc55f4dd45");
    public static readonly Guid ApprovedEaItemId = Guid.Parse("8c428e59-db05-471d-a7e7-4f7dc1c13b54");
    public const string ApprovedEaUomCode = "EA";
    public const string ApprovedEaUomName = "Each";
    public const string ApprovedEaMeasurementDimension = "COUNT";
    public const string ApprovedEaConversionPolicy = "IDENTITY_ONLY";
    public const string ApprovedEaManagementReference = "MGMT-REV869A-UOM-20260810-001";
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    private static readonly DateOnly EffectiveFrom = new(2026, 8, 10);

    public static readonly Role[] Roles =
    [
        Role("30000000-0000-0000-0000-000000000001", Rev869ARoleCodes.PurchaseManager, "Purchase Manager", true),
        Role("30000000-0000-0000-0000-000000000002", Rev869ARoleCodes.StoresManager, "Stores Manager", true),
        Role("30000000-0000-0000-0000-000000000003", Rev869ARoleCodes.QcManager, "QC Manager", true),
        Role("30000000-0000-0000-0000-000000000004", Rev869ARoleCodes.QcInspector, "QC Inspector", false)
    ];

    public static readonly PageDefinition[] Pages =
    [
        Page("40000000-0000-0000-0000-000000000001", "security.employee-identities", "Security", "Employee Identities", "/security/employee-identities"),
        Page("40000000-0000-0000-0000-000000000002", "security.operational-scopes", "Security", "Operational Scopes", "/security/operational-scopes"),
        Page("40000000-0000-0000-0000-000000000003", "masters.uoms", "Masters", "UOM Master", "/masters/uoms"),
        Page("40000000-0000-0000-0000-000000000004", "masters.uom-conversions", "Masters", "UOM Conversion Master", "/masters/uom-conversions"),
        Page("40000000-0000-0000-0000-000000000005", "settings.tax-gst", "Settings", "Tax/GST Settings", "/settings/tax-gst"),
        Page("40000000-0000-0000-0000-000000000006", "masters.vendor-qualifications", "Masters", "Vendor Qualifications", "/masters/vendor-qualifications"),
        Page("40000000-0000-0000-0000-000000000007", "masters.warehouse-condition-locations", "Masters", "Warehouse Condition Locations", "/masters/warehouse-condition-locations"),
        Page("40000000-0000-0000-0000-000000000008", "qc.inspection-policies", "QC", "QC Inspection Policies", "/qc/inspection-policies")
    ];

    public static readonly OrganizationPolicy[] OrganizationPolicies =
    [
        Policy("50000000-0000-0000-0000-000000000001", "SESS_PVT_LTD", Rev869APolicyCodes.VendorFinalApprover, Rev869ARoleCodes.ManagingDirector),
        Policy("50000000-0000-0000-0000-000000000002", "SESS_PVT_LTD", Rev869APolicyCodes.InventoryValuationMethod, InventoryValuationMethods.WeightedAverage)
    ];

    public static IReadOnlyList<RolePagePermission> RolePagePermissions
    {
        get
        {
            var existingRoles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Roles)
                .GroupBy(x => Rev869ARoleCodes.Normalize(x.Code)).ToDictionary(x => x.Key, x => x.First());
            var rows = new List<RolePagePermission>();
            foreach (var roleCode in Rev869ARoleCodes.All)
            {
                if (roleCode == Rev869ARoleCodes.DepartmentManager) continue;
                if (!existingRoles.TryGetValue(roleCode, out var role)) throw new InvalidOperationException($"REV869A role {roleCode} is not seeded.");
                foreach (var page in Pages) rows.Add(Permission(role, page));
            }
            var accounts = FoundationSeedData.Roles.Single(x => x.Code == "ACCOUNTS_HEAD");
            rows.Add(Permission(accounts, Pages.Single(x => x.PageKey == "masters.vendor-qualifications")));
            rows.Add(Permission(accounts, Pages.Single(x => x.PageKey == "settings.tax-gst")));
            var itManager = FoundationSeedData.Roles.Single(x => x.Code == "IT_MANAGER");
            rows.Add(ItAdministrationPermission(itManager, Pages.Single(x => x.PageKey == "security.employee-identities")));
            rows.Add(ItAdministrationPermission(itManager, Pages.Single(x => x.PageKey == "security.operational-scopes")));
            return rows;
        }
    }

    private static RolePagePermission Permission(Role role, PageDefinition page)
    {
        var code = Rev869ARoleCodes.Normalize(role.Code);
        var director = code is Rev869ARoleCodes.TechnicalDirector or Rev869ARoleCodes.ManagingDirector;
        var purchaseManager = code == Rev869ARoleCodes.PurchaseManager;
        var storesManager = code == Rev869ARoleCodes.StoresManager;
        var qcManager = code == Rev869ARoleCodes.QcManager;
        var executive = code is Rev869ARoleCodes.PurchaseExecutive or Rev869ARoleCodes.StoresExecutive or Rev869ARoleCodes.QcInspector;
        var accounts = string.Equals(role.Code, "ACCOUNTS_HEAD", StringComparison.Ordinal);
        var identity = page.PageKey.StartsWith("security.", StringComparison.Ordinal);
        var qc = page.PageKey.StartsWith("qc.", StringComparison.Ordinal);
        var tax = page.PageKey == "settings.tax-gst";
        var vendor = page.PageKey == "masters.vendor-qualifications";
        var stores = page.PageKey == "masters.warehouse-condition-locations";
        var uom = page.PageKey is "masters.uoms" or "masters.uom-conversions";
        var canView = director || purchaseManager || storesManager || qcManager || executive || accounts;
        var canCreate = director || (purchaseManager && (uom || tax || vendor)) || (storesManager && (uom || stores)) || (qcManager && qc);
        var canVerify = director || accounts && (tax || vendor) || storesManager && stores || qcManager && qc;
        // QC_MANAGER owns inspection control; concession decisions are reserved to
        // TECHNICAL_DIRECTOR alone. QC_MANAGER and MANAGING_DIRECTOR must not receive QC approval.
        var canApprove = qc
            ? code == Rev869ARoleCodes.TechnicalDirector
            : code == Rev869ARoleCodes.ManagingDirector;
        if (identity) { canCreate = code == Rev869ARoleCodes.ManagingDirector; canVerify = director; canApprove = code == Rev869ARoleCodes.ManagingDirector; }
        return new RolePagePermission
        {
            Id = Id("rev869a-permission", LegacyPermissionIdentityRoleCode(role), page.PageKey), RoleId = role.Id, PageDefinitionId = page.Id,
            CanView = canView, CanCreate = canCreate, CanUpdate = canCreate, CanSubmit = canCreate,
            CanVerify = canVerify, CanApprove = canApprove, CanReject = canVerify && !qcManager || canApprove,
            CanRequestClarification = canVerify || canApprove, CanRequestRevision = canVerify && !qcManager || canApprove,
            CanResubmit = canCreate, CanCancel = qc ? code == Rev869ARoleCodes.TechnicalDirector : canCreate, CanDeactivate = qc ? false : canApprove,
            CanPrint = canView, CanDownload = canView, CanExport = director || accounts,
            CanUploadAttachment = canCreate, CanReplaceAttachment = false,
            CanViewCommercialValues = director || purchaseManager || accounts,
            CanViewAuditHistory = director || canVerify || canApprove, HasFullControl = code == Rev869ARoleCodes.ManagingDirector,
            CreatedAt = SeedTime, CreatedBy = "migration-rev869a"
        };
    }

    private static RolePagePermission ItAdministrationPermission(Role role, PageDefinition page) => new()
    {
        Id = page.PageKey switch
        {
            "security.employee-identities" => ItManagerEmployeeIdentitiesPermissionId,
            "security.operational-scopes" => ItManagerOperationalScopesPermissionId,
            _ => throw new InvalidOperationException($"Unsupported IT administration page {page.PageKey}.")
        },
        RoleId = role.Id,
        PageDefinitionId = page.Id,
        CanView = true,
        CanCreate = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-authentication-bootstrap"
    };

    // REV869A originally generated permission IDs from the role code. Preserve the
    // exact historical casing used by that migration so canonicalizing Code never
    // changes the identity of any of the 1,086 existing permission rows.
    private static string LegacyPermissionIdentityRoleCode(Role role) =>
        Roles.Any(x => x.Id == role.Id) ? role.Code : role.Code.ToLowerInvariant();

    private static Role Role(string id, string code, string name, bool privileged) => new() { Id = Guid.Parse(id), Code = code, Name = name, IsPrivileged = privileged, IsActive = true, CreatedAt = SeedTime, CreatedBy = "migration-rev869a" };
    private static PageDefinition Page(string id, string key, string module, string title, string route) => new() { Id = Guid.Parse(id), PageKey = key, Module = module, Title = title, Route = route, IsActive = true, CreatedAt = SeedTime, CreatedBy = "migration-rev869a" };
    private static OrganizationPolicy Policy(string id, string organization, string code, string value) => new() { Id = Guid.Parse(id), CompanyId = MultiCompanyFoundationSeedData.SessPvtLtdId, OrganizationId = organization, PolicyCode = code, PolicyValue = value, EffectiveFrom = EffectiveFrom, IsActive = true, CreatedAt = SeedTime, CreatedBy = "migration-rev869a" };
    private static Guid Id(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts)));
        return new Guid(bytes[..16]);
    }
}
