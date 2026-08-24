using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Inventory;

public static class ItemTypes
{
    public const string RawMaterial = "RAW_MATERIAL";
    public const string Component = "COMPONENT";
    public const string Consumable = "CONSUMABLE";
    public const string Spare = "SPARE";
    public const string FinishedMachine = "FINISHED_MACHINE";
    public const string Tool = "TOOL";
    public const string ServiceItem = "SERVICE_ITEM";
    public const string NonStock = "NON_STOCK";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        RawMaterial, Component, Consumable, Spare, FinishedMachine, Tool, ServiceItem, NonStock
    };
}

public sealed class Item : AuditableEntity
{
    public string ItemCode { get; set; } = string.Empty;
    public bool IsItemCodeLocked { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public Guid? SubcategoryId { get; set; }
    public ItemSubcategory? Subcategory { get; set; }
    public string MaterialType { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public bool IsReturnable { get; set; }
    public string Uom { get; set; } = string.Empty;
    public Guid? UomId { get; set; }
    public Uom? UomMaster { get; set; }
    public Guid BaseUomId { get; set; }
    public Uom? BaseUom { get; set; }
    public string? ManufacturerMake { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? HsnSacCode { get; set; }
    public decimal GstPercentage { get; set; }
    public string? TechnicalSpecification { get; set; }
    public string? DrawingDocumentReference { get; set; }
    public bool QcRequired { get; set; }
    public bool SerialNumberTracking { get; set; }
    public bool BatchTracking { get; set; }
    public bool ShelfLifeTracking { get; set; }
    public string? Barcode { get; set; }
    public string? BarcodeSymbology { get; set; }
    public string? ImageStorageKey { get; set; }
    public string? ImageFileName { get; set; }
    public string? ImageContentType { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public Guid? PreferredVendorId { get; set; }
    public Vendor? PreferredVendor { get; set; }
    public decimal? StandardEstimatedPrice { get; set; }
    public string Status { get; set; } = MasterStatuses.Draft;
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
