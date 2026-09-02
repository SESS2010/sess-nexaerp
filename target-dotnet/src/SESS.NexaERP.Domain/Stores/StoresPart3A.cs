using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public sealed class QcInspection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid? GoodsReceiptLineLotAllocationId { get; set; }
    public GoodsReceiptLineLotAllocation? GoodsReceiptLineLotAllocation { get; set; }
    public Guid? DeliveryChallanLineId { get; set; }
    public DeliveryChallanLine? DeliveryChallanLine { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class QcInspectionRevision : CompanyScopedAuditableEntity
{
    public Guid QcInspectionId { get; set; }
    public QcInspection? QcInspection { get; set; }
    public int RevisionNumber { get; set; }
    public string RevisionKind { get; set; } = "INITIAL";
    public Guid? RevisesRevisionId { get; set; }
    public QcInspectionRevision? RevisesRevision { get; set; }
    public string? CorrectionReason { get; set; }
    public Guid InspectorEmployeeId { get; set; }
    public Employee? InspectorEmployee { get; set; }
    public string InspectorBasis { get; set; } = string.Empty;
    public string? FallbackReason { get; set; }
    public DateTimeOffset InspectionStartedAt { get; set; }
    public DateTimeOffset? InspectionCompletedAt { get; set; }
    public decimal InspectedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal DiscrepancyPendingQuantity { get; set; }
    public string Decision { get; set; } = string.Empty;
    public Guid? AcceptedConditionLocationId { get; set; }
    public WarehouseConditionLocation? AcceptedConditionLocation { get; set; }
    public Guid QcHoldConditionLocationIdSnapshot { get; set; }
    public WarehouseConditionLocation? QcHoldConditionLocationSnapshot { get; set; }
    public Guid PendingReturnConditionLocationIdSnapshot { get; set; }
    public WarehouseConditionLocation? PendingReturnConditionLocationSnapshot { get; set; }
    public DateTimeOffset PolicyResolvedAt { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateTimeOffset? FinalizedAt { get; set; }
    public Guid? FinalizedByEmployeeId { get; set; }
    public Employee? FinalizedByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
}

public sealed class QcInspectionParameterResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid QcInspectionPolicyId { get; set; }
    public QcInspectionPolicy? QcInspectionPolicy { get; set; }
    public string ParameterCodeSnapshot { get; set; } = string.Empty;
    public Guid MeasurementUomIdSnapshot { get; set; }
    public Uom? MeasurementUomSnapshot { get; set; }
    public string MeasurementUomCodeSnapshot { get; set; } = string.Empty;
    public decimal? LowerLimitSnapshot { get; set; }
    public decimal? UpperLimitSnapshot { get; set; }
    public string InspectionMethodSnapshot { get; set; } = string.Empty;
    public int RequiredSampleSizeSnapshot { get; set; }
    public int SampleOrdinal { get; set; }
    public decimal? ObservedNumericValue { get; set; }
    public string? ObservedTextValue { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public Guid ObservedByEmployeeId { get; set; }
    public Employee? ObservedByEmployee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class QcInspectionSerialDisposition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class JobOrder : CompanyScopedAuditableEntity
{
    public string JobOrderNumber { get; set; } = string.Empty;
    public string MachineModel { get; set; } = string.Empty;
    public string MachineSerial { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public DateOnly JobOrderDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
}

public sealed class MaterialIssueRequest : CompanyScopedAuditableEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string DestinationType { get; set; } = string.Empty;
    public Guid? JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? DestinationDepartmentId { get; set; }
    public Department? DestinationDepartment { get; set; }
    public string DestinationNameSnapshot { get; set; } = string.Empty;
    public Guid RequestingDepartmentId { get; set; }
    public Department? RequestingDepartment { get; set; }
    public Guid RequestedByEmployeeId { get; set; }
    public Employee? RequestedByEmployee { get; set; }
    public DateOnly RequiredDate { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string ApprovalRouteSnapshotJson { get; set; } = "{}";
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<MaterialIssueRequestLine> Lines { get; set; } = [];
}

public sealed class MaterialIssueRequestLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaterialIssueRequestId { get; set; }
    public MaterialIssueRequest? MaterialIssueRequest { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class StoresApprovalHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? MaterialIssueRequestId { get; set; }
    public MaterialIssueRequest? MaterialIssueRequest { get; set; }
    public Guid? DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }
    public int ApprovalCycle { get; set; }
    public int StepNumber { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ResolvedEmployeeId { get; set; }
    public Employee? ResolvedEmployee { get; set; }
    public string ResolvedRoleCode { get; set; } = string.Empty;
    public string SnapshotIdentity { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class DeliveryChallan : CompanyScopedAuditableEntity
{
    public string DcNumber { get; set; } = string.Empty;
    public string Direction { get; set; } = "OUTBOUND";
    public Guid? ParentDeliveryChallanId { get; set; }
    public DeliveryChallan? ParentDeliveryChallan { get; set; }
    public string DcType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public Guid? MaterialIssueRequestId { get; set; }
    public MaterialIssueRequest? MaterialIssueRequest { get; set; }
    public Guid? JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string DestinationNameSnapshot { get; set; } = string.Empty;
    public string? ExternalReferenceNumber { get; set; }
    public string DispatchEvidenceJson { get; set; } = "{}";
    public DateOnly? ExpectedReturnDate { get; set; }
    public DateOnly DocumentDate { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? ApprovalRouteSnapshotJson { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public Guid HandledByEmployeeId { get; set; }
    public Employee? HandledByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<DeliveryChallanLine> Lines { get; set; } = [];
}

public sealed class DeliveryChallanLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }
    public int LineNumber { get; set; }
    public Guid? ParentDeliveryChallanLineId { get; set; }
    public DeliveryChallanLine? ParentDeliveryChallanLine { get; set; }
    public Guid? MaterialIssueRequestLineId { get; set; }
    public MaterialIssueRequestLine? MaterialIssueRequestLine { get; set; }
    public Guid? QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? WeightUomId { get; set; }
    public Uom? WeightUom { get; set; }
    public decimal? DispatchedWeight { get; set; }
    public decimal? ReturnedWeight { get; set; }
    public decimal? CalculatedScrapWeight { get; set; }
    public string? VendorWeightExplanation { get; set; }
    public bool RequiresQcSnapshot { get; set; }
    public Guid? ReplacementGoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? ReplacementGoodsReceiptLine { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}
