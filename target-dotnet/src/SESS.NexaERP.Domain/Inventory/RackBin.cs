using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class RackBin : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string BinCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
