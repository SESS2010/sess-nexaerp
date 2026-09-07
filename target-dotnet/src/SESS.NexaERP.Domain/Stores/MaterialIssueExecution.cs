using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;

namespace SESS.NexaERP.Domain.Stores;

public sealed class MaterialIssue : CompanyScopedAuditableEntity
{
    public string IssueNumber { get; set; } = string.Empty;
    public Guid MaterialIssueRequestId { get; set; }
    public MaterialIssueRequest? MaterialIssueRequest { get; set; }
    public Guid? JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid IssuedToEmployeeId { get; set; }
    public Employee? IssuedToEmployee { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ReturnDueAt { get; set; }
    public string Status { get; set; } = "ISSUED";
    public Guid? StockPostingBatchId { get; set; }
    public StockPostingBatch? StockPostingBatch { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public Guid IssuedByEmployeeId { get; set; }
    public Employee? IssuedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public List<MaterialIssueLine> Lines { get; set; } = [];
}

public sealed class MaterialIssueLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaterialIssueId { get; set; }
    public MaterialIssue? MaterialIssue { get; set; }
    public Guid MaterialIssueRequestLineId { get; set; }
    public MaterialIssueRequestLine? MaterialIssueRequestLine { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal QuantityBase { get; set; }
    public Guid OwnershipAccountId { get; set; }
    public Guid FromCustodyAssignmentId { get; set; }
    public Guid ToCustodyAssignmentId { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public Guid? CustodyCaseLineId { get; set; }
    public Guid? InventoryLotId { get; set; }
    public Guid? InventorySerialId { get; set; }
    public Guid? OriginGoodsReceiptLineId { get; set; }
    public Guid? GoodsReceiptLineLotAllocationId { get; set; }
    public Guid? QcInspectionLotDispositionId { get; set; }
    public Guid WarehouseConditionLocationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class MaterialIssueExcessDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaterialIssueRequestLineId { get; set; }
    public MaterialIssueRequestLine? MaterialIssueRequestLine { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid DecidedByEmployeeId { get; set; }
    public Employee? DecidedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class MaterialIssueHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? MaterialIssueRequestId { get; set; }
    public Guid? MaterialIssueId { get; set; }
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
