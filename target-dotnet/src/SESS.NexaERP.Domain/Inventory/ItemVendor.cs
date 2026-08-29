using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Inventory;

/// <summary>
/// Many-to-many link: which vendors supply an item. Maintained from the item
/// master (vendor multi-select); the vendor master shows the reverse view.
/// </summary>
public sealed class ItemVendor : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
}

/// <summary>Stored item image content; Item.ImageStorageKey references the row id.</summary>
public sealed class ItemImage : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = [];
}
