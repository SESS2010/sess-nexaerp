using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class Item : AuditableEntity
{
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? ImageStorageKey { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
}
