using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public sealed class ComponentFitment : CompanyScopedAuditableEntity
{
    public string FitmentNumber { get; set; } = string.Empty;
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public Guid? ReverifiesFitmentId { get; set; }
    public ComponentFitment? ReverifiesFitment { get; set; }
    public decimal QuantityBase { get; set; }
    public DateTimeOffset FittedAt { get; set; }
    public Guid ConfirmedByEmployeeId { get; set; }
    public Employee? ConfirmedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string ConfirmationNote { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
}

public sealed class ComponentFitmentReversal : CompanyScopedAuditableEntity
{
    public Guid ComponentFitmentId { get; set; }
    public ComponentFitment? ComponentFitment { get; set; }
    public DateTimeOffset ReversedAt { get; set; }
    public Guid ReversedByEmployeeId { get; set; }
    public Employee? ReversedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsSelfReversal { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
}

public sealed class ActualBom : CompanyScopedAuditableEntity
{
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public List<ActualBomEntry> Entries { get; set; } = [];
}

public sealed class ActualBomEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ActualBomId { get; set; }
    public ActualBom? ActualBom { get; set; }
    public Guid? ComponentFitmentId { get; set; }
    public ComponentFitment? ComponentFitment { get; set; }
    public Guid? ComponentFitmentReversalId { get; set; }
    public ComponentFitmentReversal? ComponentFitmentReversal { get; set; }
    public string EntryKind { get; set; } = string.Empty;
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public decimal QuantityBase { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public Guid? InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public string GrnNumberSnapshot { get; set; } = string.Empty;
    public Guid VendorBillLineId { get; set; }
    public VendorBillLine? VendorBillLine { get; set; }
    public string VendorBillNumberSnapshot { get; set; } = string.Empty;
    public decimal AcceptedMaterialValue { get; set; }
    public decimal AllocatedChargeValue { get; set; }
    public decimal TotalAcceptedValue { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string CreatedBy { get; set; } = "system";
}