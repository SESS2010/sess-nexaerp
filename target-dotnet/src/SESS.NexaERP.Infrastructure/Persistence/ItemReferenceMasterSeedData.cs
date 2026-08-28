using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Authorization;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class ItemReferenceMasterSeedData
{
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    private static readonly Guid DepartmentManagerRoleId = Guid.Parse("30000000-0000-0000-0000-000000000005");

    public static readonly PageDefinition[] Pages =
    [
        Page("41000000-0000-0000-0000-000000000001", "masters.item-categories", "Item Category Master", "/masters/item-categories"),
        Page("41000000-0000-0000-0000-000000000002", "masters.item-subcategories", "Item Subcategory Master", "/masters/item-subcategories"),
        Page("41000000-0000-0000-0000-000000000003", "masters.manufacturers", "Manufacturer Master", "/masters/manufacturers")
    ];

    public static IReadOnlyList<RolePagePermission> RolePagePermissions { get; } = BuildPermissions();

    private static IReadOnlyList<RolePagePermission> BuildPermissions()
    {
        var itemPageId = FoundationSeedData.Pages.Single(x => x.PageKey == "masters.items").Id;
        var uomPageId = Rev869ASeedData.Pages.Single(x => x.PageKey == "masters.uoms").Id;
        var sourceRows = Rev866SeedData.RolePagePermissions.Concat(Rev869ASeedData.RolePagePermissions).ToArray();
        var roleIds = sourceRows.Where(x => x.PageDefinitionId == itemPageId || x.PageDefinitionId == uomPageId)
            .Select(x => x.RoleId).Append(DepartmentManagerRoleId).Distinct().OrderBy(x => x).ToArray();
        var rows = new List<RolePagePermission>();

        foreach (var roleId in roleIds)
        {
            var source = sourceRows.LastOrDefault(x => x.RoleId == roleId && x.PageDefinitionId == itemPageId)
                ?? sourceRows.LastOrDefault(x => x.RoleId == roleId && x.PageDefinitionId == uomPageId);
            foreach (var page in Pages)
            {
                rows.Add(source is null
                    ? DepartmentManagerPermission(page)
                    : Clone(source, page));
            }
        }

        return rows;
    }

    private static RolePagePermission Clone(RolePagePermission source, PageDefinition page) => new()
    {
        Id = Id("item-reference-master-permission", source.RoleId.ToString(), page.PageKey),
        RoleId = source.RoleId,
        PageDefinitionId = page.Id,
        CanView = source.CanView,
        CanCreate = source.CanCreate,
        CanUpdate = source.CanUpdate,
        CanSubmit = source.CanSubmit,
        CanIssue = source.CanIssue,
        CanVerify = source.CanVerify,
        CanApprove = source.CanApprove,
        CanReject = source.CanReject,
        CanRequestClarification = source.CanRequestClarification,
        CanRequestRevision = source.CanRequestRevision,
        CanResubmit = source.CanResubmit,
        CanCancel = source.CanCancel,
        CanDeactivate = source.CanDeactivate,
        CanPrint = source.CanPrint,
        CanDownload = source.CanDownload,
        CanExport = source.CanExport,
        CanUploadAttachment = source.CanUploadAttachment,
        CanReplaceAttachment = source.CanReplaceAttachment,
        CanViewCommercialValues = source.CanViewCommercialValues,
        CanViewAuditHistory = source.CanViewAuditHistory,
        HasFullControl = source.HasFullControl,
        CreatedAt = SeedTime,
        CreatedBy = "migration-item-reference-masters"
    };

    private static RolePagePermission DepartmentManagerPermission(PageDefinition page) => new()
    {
        Id = Id("item-reference-master-permission", DepartmentManagerRoleId.ToString(), page.PageKey),
        RoleId = DepartmentManagerRoleId,
        PageDefinitionId = page.Id,
        CanView = true,
        CanPrint = true,
        CanDownload = true,
        CanViewAuditHistory = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-item-reference-masters"
    };

    private static PageDefinition Page(string id, string key, string title, string route) => new()
    {
        Id = Guid.Parse(id),
        PageKey = key,
        Module = "Masters",
        Title = title,
        Route = route,
        IsActive = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-item-reference-masters"
    };

    private static Guid Id(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts)));
        return new Guid(bytes[..16]);
    }
}
