using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class ItemMergeAlias : CompanyScopedAuditableEntity
{
    public Guid SourceItemId { get; set; }
    public Item? SourceItem { get; set; }
    public Guid SurvivorItemId { get; set; }
    public Item? SurvivorItem { get; set; }
    public Guid ActorEmployeeId { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
