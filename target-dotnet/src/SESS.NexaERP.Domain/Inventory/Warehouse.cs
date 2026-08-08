using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class Warehouse : AuditableEntity
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
}
