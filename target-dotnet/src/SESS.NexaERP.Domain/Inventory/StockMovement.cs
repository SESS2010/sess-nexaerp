using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class StockMovement : CompanyScopedAuditableEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public DateOnly PostingDate { get; set; }
}
