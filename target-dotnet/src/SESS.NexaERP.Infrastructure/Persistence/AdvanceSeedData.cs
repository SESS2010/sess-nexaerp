using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class AdvanceSeedData
{
    public static readonly Role DepartmentManagerRole = new()
    {
        Id = Guid.Parse("30000000-0000-0000-0000-000000000005"),
        Code = Rev869ARoleCodes.DepartmentManager,
        Name = "Department Manager",
        IsPrivileged = true,
        IsActive = true,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = "advance-baseline"
    };

    public static IReadOnlyList<RolePagePermission> RolePagePermissions { get; } = BuildRolePagePermissions();

    private static IReadOnlyList<RolePagePermission> BuildRolePagePermissions()
    {
        var ordered = Rev866SeedData.RolePagePermissions
            .Concat(Rev869ASeedData.RolePagePermissions)
            .Concat(Rev869ADepartmentManagerPermissions())
            .Concat(Rev869BSeedData.RolePagePermissions)
            .Concat(Rev869BDepartmentManagerPermissions())
            .ToArray();

        return ordered
            .GroupBy(row => (row.RoleId, row.PageDefinitionId))
            .Select(group => group.Last())
            .ToArray();
    }

    private static IEnumerable<RolePagePermission> Rev869ADepartmentManagerPermissions()
    {
        var ids = new[]
        {
            "aea2e8a1-18a6-72d2-a954-6f5513b80eeb", "f8e7d0a6-f056-175a-e604-14c1f9f6ad83",
            "a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12", "15ee5b19-d532-c28c-b755-de4152769a7a",
            "5794f740-90b1-5a70-413a-d59bbc97ce78", "42e2a253-d767-6191-caf9-e1f79652c44f",
            "38371df3-5a46-5137-8204-4c5391633180", "680f7358-4b7c-0733-be42-f9d52e746d1b"
        };

        return Rev869ASeedData.Pages.Select((page, index) => Permission(
            ids[index], page.Id, "migration-rev869a",
            canView: true, canRequestClarification: false, canApprove: false,
            canRequestRevision: false, canViewAuditHistory: true));
    }

    private static IEnumerable<RolePagePermission> Rev869BDepartmentManagerPermissions()
    {
        var pages = new[]
        {
            (Id: "918d0634-ff61-c756-98f4-a17290d04110", PageKey: "purchase.rfq"),
            (Id: "062fd69d-1356-6930-ecdb-1c13ae5a01d5", PageKey: "purchase.commercial-comparisons"),
            (Id: "c9ec48cd-024d-128e-697f-389458e12c97", PageKey: "purchase.po")
        };
        var allPages = FoundationSeedData.Pages.Concat(Rev869BSeedData.Pages).ToDictionary(page => page.PageKey);
        return pages.Select(item => Permission(
            item.Id, allPages[item.PageKey].Id, "migration-rev869b",
            canView: true, canRequestClarification: true, canApprove: item.PageKey == "purchase.po",
            canRequestRevision: item.PageKey == "purchase.po", canViewAuditHistory: true));
    }

    private static RolePagePermission Permission(
        string id,
        Guid pageDefinitionId,
        string createdBy,
        bool canView,
        bool canRequestClarification,
        bool canApprove,
        bool canRequestRevision,
        bool canViewAuditHistory) => new()
    {
        Id = Guid.Parse(id),
        RoleId = DepartmentManagerRole.Id,
        PageDefinitionId = pageDefinitionId,
        CanView = canView,
        CanApprove = canApprove,
        CanReject = canApprove,
        CanRequestClarification = canRequestClarification,
        CanRequestRevision = canRequestRevision,
        CanPrint = canView,
        CanDownload = canView,
        CanViewAuditHistory = canViewAuditHistory,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = createdBy
    };
}