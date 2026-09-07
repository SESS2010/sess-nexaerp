using SESS.NexaERP.Domain.Authorization;

namespace SESS.NexaERP.Infrastructure.Persistence;

internal static class ApprovalConfigurationPart2SeedData
{
    private static readonly Guid ProductionManagerRoleId = Guid.Parse("83000000-0000-0000-0000-000000000001");
    private static readonly Guid AccountsManagerRoleId = Guid.Parse("83000000-0000-0000-0000-000000000002");

    internal static readonly RolePagePermission[] RolePagePermissions =
    [
        Permission("84000000-0000-0000-0000-000000000001", ProductionManagerRoleId, "purchase.requisition-approvals"),
        Permission("84000000-0000-0000-0000-000000000002", ProductionManagerRoleId, "purchase.commercial-comparisons"),
        Permission("84000000-0000-0000-0000-000000000003", ProductionManagerRoleId, "purchase.po"),
        Permission("84000000-0000-0000-0000-000000000004", AccountsManagerRoleId, "purchase.requisition-approvals"),
        Permission("84000000-0000-0000-0000-000000000005", AccountsManagerRoleId, "purchase.commercial-comparisons"),
        Permission("84000000-0000-0000-0000-000000000006", AccountsManagerRoleId, "purchase.po"),
        Permission("84000000-0000-0000-0000-000000000007", ProductionManagerRoleId, "purchase.requisitions", canVerify: true),
        Permission("84000000-0000-0000-0000-000000000008", AccountsManagerRoleId, "purchase.requisitions", canVerify: true)
    ];

    private static RolePagePermission Permission(string id, Guid roleId, string pageKey, bool canVerify = false) => new()
    {
        Id = Guid.Parse(id),
        RoleId = roleId,
        PageDefinitionId = FoundationSeedData.Pages.Concat(Rev869BSeedData.Pages).Single(x => x.PageKey == pageKey).Id,
        CanView = true,
        CanVerify = canVerify,
        CanApprove = true,
        CanReject = true,
        CanRequestRevision = true,
        CanViewCommercialValues = true,
        CanViewAuditHistory = true,
        CreatedAt = DateTimeOffset.UnixEpoch,
        CreatedBy = "migration-approval-configuration-part2"
    };
}
