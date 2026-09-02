using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public static class InventoryProvenanceLayerTypes
{
    public const string Receipt = "RECEIPT";
    public const string QcAccepted = "QC_ACCEPTED";
    public const string QcRejected = "QC_REJECTED";
    public const string ConcessionAccepted = "CONCESSION_ACCEPTED";
    public const string Custody = "CUSTODY";
    public const string TransformationOutput = "TRANSFORMATION_OUTPUT";
    public const string Return = "RETURN";
    public const string Adjustment = "ADJUSTMENT";
}

public sealed class InventoryLotAttributeRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? SupersedesRevisionId { get; set; }
    public InventoryLotAttributeRevision? SupersedesRevision { get; set; }
    public string AttributesJson { get; set; } = "{}";
    public string ChangeReason { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public Guid RecordedByEmployeeId { get; set; }
    public Employee? RecordedByEmployee { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class InventoryProvenanceLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public Guid? InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public string LayerType { get; set; } = string.Empty;
    public decimal QuantityCreated { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string IdentityHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public abstract class InventoryProvenanceOrigin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public string OriginRole { get; set; } = "PRIMARY";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryProvenanceGoodsReceiptLotOrigin : InventoryProvenanceOrigin
{
    public Guid GoodsReceiptLineLotAllocationId { get; set; }
    public GoodsReceiptLineLotAllocation? GoodsReceiptLineLotAllocation { get; set; }
}

public sealed class InventoryProvenanceCustodyCaseLineOrigin : InventoryProvenanceOrigin
{
    public Guid CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
}

public sealed class InventoryTransformation : CompanyScopedAuditableEntity
{
    public string TransformationNumber { get; set; } = string.Empty;
    public string TransformationType { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? PostedAt { get; set; }
    public Guid? PostedByEmployeeId { get; set; }
    public Employee? PostedByEmployee { get; set; }
    public Guid? ReversesTransformationId { get; set; }
    public InventoryTransformation? ReversesTransformation { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<InventoryTransformationInput> Inputs { get; set; } = [];
    public List<InventoryTransformationOutput> Outputs { get; set; } = [];
}

public sealed class InventoryTransformationInput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryTransformationId { get; set; }
    public InventoryTransformation? InventoryTransformation { get; set; }
    public int LineNumber { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryTransformationOutput
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryTransformationId { get; set; }
    public InventoryTransformation? InventoryTransformation { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public decimal Quantity { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public Guid OutputProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? OutputProvenanceLayer { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryProvenanceTransformationOutputOrigin : InventoryProvenanceOrigin
{
    public Guid InventoryTransformationOutputId { get; set; }
    public InventoryTransformationOutput? InventoryTransformationOutput { get; set; }
}

public sealed class InventoryProvenanceEdge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? InventoryTransformationId { get; set; }
    public InventoryTransformation? InventoryTransformation { get; set; }
    public Guid FromProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? FromProvenanceLayer { get; set; }
    public Guid ToProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? ToProvenanceLayer { get; set; }
    public string EdgeType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string AllocationBasis { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventorySerialIdentityRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? SupersedesRevisionId { get; set; }
    public InventorySerialIdentityRevision? SupersedesRevision { get; set; }
    public string StoredSerialNumberSnapshot { get; set; } = string.Empty;
    public string NormalizedSerialNumberSnapshot { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public Guid RecordedByEmployeeId { get; set; }
    public Employee? RecordedByEmployee { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class InventorySerialGenealogyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid? ReversesEventId { get; set; }
    public InventorySerialGenealogyEvent? ReversesEvent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class InventorySerialGenealogyLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventorySerialGenealogyEventId { get; set; }
    public InventorySerialGenealogyEvent? InventorySerialGenealogyEvent { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public Guid? FromInventorySerialId { get; set; }
    public InventorySerial? FromInventorySerial { get; set; }
    public Guid? ToInventorySerialId { get; set; }
    public InventorySerial? ToInventorySerial { get; set; }
    public Guid? FromProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? FromProvenanceLayer { get; set; }
    public Guid? ToProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? ToProvenanceLayer { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class QcInspectionLotDisposition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid GoodsReceiptLineLotAllocationId { get; set; }
    public GoodsReceiptLineLotAllocation? GoodsReceiptLineLotAllocation { get; set; }
    public Guid InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public decimal InspectedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal DiscrepancyPendingQuantity { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public Guid DestinationConditionLocationId { get; set; }
    public WarehouseConditionLocation? DestinationConditionLocation { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryProvenanceQcDispositionOrigin : InventoryProvenanceOrigin
{
    public Guid QcInspectionLotDispositionId { get; set; }
    public QcInspectionLotDisposition? QcInspectionLotDisposition { get; set; }
}

public sealed class InventoryConcession : CompanyScopedAuditableEntity
{
    public string ConcessionNumber { get; set; } = string.Empty;
    public Guid QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid QcInspectionLotDispositionId { get; set; }
    public QcInspectionLotDisposition? QcInspectionLotDisposition { get; set; }
    public Guid QcInspectionParameterResultId { get; set; }
    public QcInspectionParameterResult? QcInspectionParameterResult { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string FailedParameterSnapshot { get; set; } = string.Empty;
    public string MeasuredValueSnapshot { get; set; } = string.Empty;
    public string TechnicalAcceptanceReason { get; set; } = string.Empty;
    public string IntendedUse { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public Guid CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public string? DecidedRoleCode { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionReason { get; set; }
    public Guid? ReversesConcessionId { get; set; }
    public InventoryConcession? ReversesConcession { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<InventoryConcessionAllocation> Allocations { get; set; } = [];
}

public sealed class InventoryConcessionAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryConcessionId { get; set; }
    public InventoryConcession? InventoryConcession { get; set; }
    public Guid GoodsReceiptLineLotAllocationId { get; set; }
    public GoodsReceiptLineLotAllocation? GoodsReceiptLineLotAllocation { get; set; }
    public Guid InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public Guid RejectedProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? RejectedProvenanceLayer { get; set; }
    public Guid? AcceptedProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? AcceptedProvenanceLayer { get; set; }
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public List<InventoryConcessionAllocationSerial> Serials { get; set; } = [];
}

public sealed class InventoryConcessionAllocationSerial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryConcessionAllocationId { get; set; }
    public InventoryConcessionAllocation? InventoryConcessionAllocation { get; set; }
    public Guid InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryProvenanceAnnotation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public string AnnotationType { get; set; } = string.Empty;
    public string AnnotationCode { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public Guid? InventoryConcessionId { get; set; }
    public InventoryConcession? InventoryConcession { get; set; }
    public Guid? InheritedFromAnnotationId { get; set; }
    public InventoryProvenanceAnnotation? InheritedFromAnnotation { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class InventoryProvenanceConcessionAllocationOrigin : InventoryProvenanceOrigin
{
    public Guid InventoryConcessionAllocationId { get; set; }
    public InventoryConcessionAllocation? InventoryConcessionAllocation { get; set; }
}
