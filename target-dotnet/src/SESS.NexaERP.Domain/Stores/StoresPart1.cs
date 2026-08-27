using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Domain.Stores;

public static class StoresConfigurationRoleCodes
{
    public const string TechnicalDirector = "TECHNICAL_DIRECTOR";
    public const string ManagingDirector = "MANAGING_DIRECTOR";
    public const string ItManager = "IT_MANAGER";
}

public static class StoresConfigurationRuleKeys
{
    public const string SerialCaptureThreshold = "SERIAL_CAPTURE_THRESHOLD";
    public const string QcCompletionDays = "QC_COMPLETION_DAYS";
    public const string EmergencyPurchaseCountPerMonth = "EMERGENCY_PURCHASE_COUNT_PER_MONTH";
    public const string EmergencyPurchaseValueLimit = "EMERGENCY_PURCHASE_VALUE_LIMIT";
    public const string ExpenseFoodPerPersonPerDay = "EXPENSE_FOOD_PER_PERSON_PER_DAY";
    public const string ExpenseLodgingSinglePerDay = "EXPENSE_LODGING_SINGLE_PER_DAY";
    public const string ExpenseLodgingDoublePerDay = "EXPENSE_LODGING_DOUBLE_PER_DAY";
    public const string ExpenseDailyApprovalCap = "EXPENSE_DAILY_APPROVAL_CAP";
    public const string ExpenseTravelDistanceThresholdKm = "EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM";
}

public sealed class BusinessRuleConfigurationVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string? OldValueJson { get; set; }
    public string NewValueJson { get; set; } = string.Empty;
    public string? UnitCode { get; set; }
    public int VersionNumber { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public BusinessRuleConfigurationVersion? PreviousVersion { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public Guid ChangedByEmployeeId { get; set; }
    public Employee? ChangedByEmployee { get; set; }
    public string ChangedByRoleCode { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class ItemCompanyInventorySetting : CompanyScopedAuditableEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string ErpBarcode { get; set; } = string.Empty;
    public string BarcodeCategoryCode { get; set; } = string.Empty;
    public long BarcodeSequenceNumber { get; set; }
    public string BarcodeSymbology { get; set; } = "CODE128";
    public string SerialCaptureMode { get; set; } = "INHERIT";
    public bool IsActive { get; set; } = true;
}

public sealed class StoreCategoryRoute : CompanyScopedAuditableEntity
{
    public Guid ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }
    public Guid QcHoldConditionLocationId { get; set; }
    public WarehouseConditionLocation? QcHoldConditionLocation { get; set; }
    public Guid PendingReturnConditionLocationId { get; set; }
    public WarehouseConditionLocation? PendingReturnConditionLocation { get; set; }
    public Guid DefaultAcceptedConditionLocationId { get; set; }
    public WarehouseConditionLocation? DefaultAcceptedConditionLocation { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GateEntry : CompanyScopedAuditableEntity
{
    public string GateEntryNumber { get; set; } = string.Empty;
    public string DocumentKind { get; set; } = "NORMAL";
    public Guid? ReversesGateEntryId { get; set; }
    public GateEntry? ReversesGateEntry { get; set; }
    public string? ReversalReason { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string VendorNameSnapshot { get; set; } = string.Empty;
    public string VendorDcNumber { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public string ModeOfTransport { get; set; } = string.Empty;
    public DateTimeOffset ArrivedAt { get; set; }
    public Guid ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public string IsoReceiptVerificationJson { get; set; } = "{}";
    public string Status { get; set; } = "DRAFT";
    public DateTimeOffset? FinalizedAt { get; set; }
    public Guid? FinalizedByEmployeeId { get; set; }
    public Employee? FinalizedByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<GateEntryLine> Lines { get; set; } = [];
}

public sealed class GateEntryLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid GateEntryId { get; set; }
    public GateEntry? GateEntry { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal DeliveredQuantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class StoresDocumentStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? GateEntryId { get; set; }
    public GateEntry? GateEntry { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class NotificationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public Guid SourceEntityId { get; set; }
    public string SourceReferenceSnapshot { get; set; } = string.Empty;
    public string[] RecipientRoleCodes { get; set; } = [];
    public string TitleSnapshot { get; set; } = string.Empty;
    public string BodySnapshot { get; set; } = string.Empty;
    public string DeepLinkSnapshot { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset NotBeforeAt { get; set; }
    public string? CancellationKey { get; set; }
    public string Status { get; set; } = "SCHEDULED";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public List<NotificationRecipient> Recipients { get; set; } = [];
}

public sealed class NotificationRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid NotificationEventId { get; set; }
    public NotificationEvent? NotificationEvent { get; set; }
    public Guid RecipientEmployeeId { get; set; }
    public Employee? RecipientEmployee { get; set; }
    public string[] ResolvedRoleCodes { get; set; } = [];
    public DateTimeOffset ResolvedAt { get; set; }
    public DateTimeOffset InAppAvailableAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public Guid? ReadByEmployeeId { get; set; }
    public Employee? ReadByEmployee { get; set; }
    public string? ReadCorrelationId { get; set; }
    public List<NotificationDeliveryAttempt> DeliveryAttempts { get; set; } = [];
}

public sealed class NotificationDeliveryAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid NotificationRecipientId { get; set; }
    public NotificationRecipient? NotificationRecipient { get; set; }
    public string Channel { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDetail { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
