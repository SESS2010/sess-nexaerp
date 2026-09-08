using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Domain.Stores;

public sealed class VendorBill : CompanyScopedAuditableEntity
{
    public string BillNumber { get; set; } = string.Empty;
    public DateOnly BillDate { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string MatchStatus { get; set; } = "MATCHED";
    public decimal TotalPayableValue { get; set; }
    public Guid CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string CreateIdempotencyKey { get; set; } = string.Empty;
    public string CreateRequestFingerprint { get; set; } = string.Empty;
    public DateTimeOffset? DecidedAt { get; set; }
    public Guid? DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public string? DecisionActorRoleCode { get; set; }
    public Guid? DecisionRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? DecisionRoleAssignment { get; set; }
    public string? DecisionRoleAssignmentType { get; set; }
    public string? DecisionReason { get; set; }
    public string? DecisionIdempotencyKey { get; set; }
    public string? DecisionRequestFingerprint { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public Guid? ReversedByEmployeeId { get; set; }
    public Employee? ReversedByEmployee { get; set; }
    public string? ReversalActorRoleCode { get; set; }
    public Guid? ReversalRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ReversalRoleAssignment { get; set; }
    public string? ReversalRoleAssignmentType { get; set; }
    public string? ReversalReason { get; set; }
    public string? ReversalIdempotencyKey { get; set; }
    public string? ReversalRequestFingerprint { get; set; }
    public List<VendorBillLine> Lines { get; set; } = [];
}

public sealed class VendorBillLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid VendorBillId { get; set; }
    public VendorBill? VendorBill { get; set; }
    public int LineNumber { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal BilledQuantity { get; set; }
    public decimal ExpectedUnitRate { get; set; }
    public decimal BilledUnitRate { get; set; }
    public decimal ExpectedPayableValue { get; set; }
    public decimal BilledPayableValue { get; set; }
    public string MatchStatus { get; set; } = "MATCHED";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class FifoInventoryCostLayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LayerValue { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string CostBasis { get; set; } = "PO_PROVISIONAL_IDENTICAL";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class FifoCostConsumption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid FifoInventoryCostLayerId { get; set; }
    public FifoInventoryCostLayer? FifoInventoryCostLayer { get; set; }
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ConsumedValue { get; set; }
    public DateTimeOffset ConsumedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
}

public sealed class VendorBillCostAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid VendorBillLineId { get; set; }
    public VendorBillLine? VendorBillLine { get; set; }
    public Guid FifoInventoryCostLayerId { get; set; }
    public FifoInventoryCostLayer? FifoInventoryCostLayer { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal AcceptedValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class VendorBillHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid VendorBillId { get; set; }
    public VendorBill? VendorBill { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}