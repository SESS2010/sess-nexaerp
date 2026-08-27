using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Domain.Stores;

public sealed class GoodsReceipt : CompanyScopedAuditableEntity
{
    public string GrnNumber { get; set; } = string.Empty;
    public string DocumentKind { get; set; } = "NORMAL";
    public Guid? ReversesGoodsReceiptId { get; set; }
    public GoodsReceipt? ReversesGoodsReceipt { get; set; }
    public string? ReversalReason { get; set; }
    public Guid GateEntryId { get; set; }
    public GateEntry? GateEntry { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string VendorNameSnapshot { get; set; } = string.Empty;
    public string VendorBillNumber { get; set; } = string.Empty;
    public DateOnly VendorBillDate { get; set; }
    public string VendorDcNumberSnapshot { get; set; } = string.Empty;
    public string ModeOfTransportSnapshot { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public Guid ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public string IsoReceiptVerificationJson { get; set; } = "{}";
    public string ConfigurationSnapshotJson { get; set; } = "{}";
    public string ConfigurationSnapshotHash { get; set; } = string.Empty;
    public Guid QcCompletionDaysConfigVersionId { get; set; }
    public BusinessRuleConfigurationVersion? QcCompletionDaysConfigVersion { get; set; }
    public int QcCompletionDaysSnapshot { get; set; }
    public DateTimeOffset QcDueAt { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateTimeOffset? FinalizedAt { get; set; }
    public Guid? FinalizedByEmployeeId { get; set; }
    public Employee? FinalizedByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<GoodsReceiptLine> Lines { get; set; } = [];
}

public sealed class GoodsReceiptLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid GateEntryLineId { get; set; }
    public GateEntryLine? GateEntryLine { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public Guid ItemCategoryIdSnapshot { get; set; }
    public ItemCategory? ItemCategorySnapshot { get; set; }
    public string ItemCategoryCodeSnapshot { get; set; } = string.Empty;
    public string HsnSacCodeSnapshot { get; set; } = string.Empty;
    public decimal GstPercentageSnapshot { get; set; }
    public string? ModelSnapshot { get; set; }
    public string? ManufacturerPartNumberSnapshot { get; set; }
    public string? ManufacturerMakeSnapshot { get; set; }
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal PoOrderedQuantitySnapshot { get; set; }
    public decimal PriorEffectiveReceivedQuantitySnapshot { get; set; }
    public decimal RemainingPoQuantitySnapshot { get; set; }
    public decimal DeliveredQuantitySnapshot { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal ExcessRejectedQuantity { get; set; }
    public string? ExcessDisposition { get; set; }
    public decimal LineValueSnapshot { get; set; }
    public decimal UnitRateSnapshot { get; set; }
    public Guid SerialThresholdConfigVersionId { get; set; }
    public BusinessRuleConfigurationVersion? SerialThresholdConfigVersion { get; set; }
    public decimal SerialThresholdValueSnapshot { get; set; }
    public string SerialCaptureModeSnapshot { get; set; } = string.Empty;
    public Guid? SerialOverrideSettingId { get; set; }
    public ItemCompanyInventorySetting? SerialOverrideSetting { get; set; }
    public Guid QcRouteIdSnapshot { get; set; }
    public StoreCategoryRoute? QcRouteSnapshot { get; set; }
    public Guid QcHoldConditionLocationIdSnapshot { get; set; }
    public WarehouseConditionLocation? QcHoldConditionLocationSnapshot { get; set; }
    public DateOnly BillWarrantyLimitDate { get; set; }
    public DateOnly InitialWarrantyExpiryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public List<GoodsReceiptLineSerial> Serials { get; set; } = [];
}

public sealed class InventorySerial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string StoredSerialNumber { get; set; } = string.Empty;
    public string NormalizedStoredSerialNumber { get; set; } = string.Empty;
    public DateTimeOffset FirstCapturedAt { get; set; }
    public Guid FirstCapturedByEmployeeId { get; set; }
    public Employee? FirstCapturedByEmployee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class GoodsReceiptLineSerial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public int SerialOrdinal { get; set; }
    public string EnteredSerialNumber { get; set; } = string.Empty;
    public string StoredSerialNumberSnapshot { get; set; } = string.Empty;
    public string ReceiptDisposition { get; set; } = string.Empty;
    public bool DisambiguationApplied { get; set; }
    public bool DuplicateWarningAcknowledged { get; set; }
    public string? DisambiguationReason { get; set; }
    public Guid CapturedByEmployeeId { get; set; }
    public Employee? CapturedByEmployee { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
