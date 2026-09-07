using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Stores;

public sealed class MaterialReturn : CompanyScopedAuditableEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid MaterialIssueId { get; set; }
    public MaterialIssue? MaterialIssue { get; set; }
    public Guid ReturnedByEmployeeId { get; set; }
    public Employee? ReturnedByEmployee { get; set; }
    public DateTimeOffset DeclaredAt { get; set; }
    public string Status { get; set; } = "SUBMITTED";
    public Guid CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string CreateIdempotencyKey { get; set; } = string.Empty;
    public string CreateRequestFingerprint { get; set; } = string.Empty;
    public DateTimeOffset? AcceptedAt { get; set; }
    public Guid? AcceptedByEmployeeId { get; set; }
    public Employee? AcceptedByEmployee { get; set; }
    public string? AcceptedActorRoleCode { get; set; }
    public Guid? AcceptedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? AcceptedRoleAssignment { get; set; }
    public string? AcceptedRoleAssignmentType { get; set; }
    public string? AcceptanceReason { get; set; }
    public string? AcceptanceIdempotencyKey { get; set; }
    public string? AcceptanceRequestFingerprint { get; set; }
    public Guid? StockPostingBatchId { get; set; }
    public StockPostingBatch? StockPostingBatch { get; set; }
    public List<MaterialReturnLine> Lines { get; set; } = [];
}

public sealed class MaterialReturnLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaterialReturnId { get; set; }
    public MaterialReturn? MaterialReturn { get; set; }
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public decimal ReturnedQuantityBase { get; set; }
    public decimal ReportedConsumedQuantityBase { get; set; }
    public decimal ReportedStillHeldQuantityBase { get; set; }
    public string ScanCode { get; set; } = string.Empty;
    public Guid? InventorySerialId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class MaterialReturnHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid MaterialReturnId { get; set; }
    public MaterialReturn? MaterialReturn { get; set; }
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
