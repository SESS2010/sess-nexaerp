using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class RackBin : CompanyScopedAuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string BinCode { get; set; } = string.Empty;
    public string RackName { get; set; } = string.Empty;
    public string BinNameNumber { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public string LocationType { get; set; } = string.Empty;
    public string MaterialCondition { get; set; } = "Accepted";
    public decimal? CapacityQuantity { get; set; }
    public string? CapacityUom { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = MasterStatuses.Draft;
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
