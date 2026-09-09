using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Stores;

public sealed class JobOrderFatCustodyExplanation : CompanyScopedAuditableEntity
{
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public decimal QuantityBase { get; set; }
    public string Disposition { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ExplainedByEmployeeId { get; set; }
    public Employee? ExplainedByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
}

public sealed class JobOrderFatReconciliation : CompanyScopedAuditableEntity
{
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public int AttemptNumber { get; set; }
    public string Result { get; set; } = string.Empty;
    public decimal IssuedQuantityBase { get; set; }
    public decimal FittedQuantityBase { get; set; }
    public decimal ReturnedQuantityBase { get; set; }
    public decimal ExplainedQuantityBase { get; set; }
    public decimal UnexplainedQuantityBase { get; set; }
    public DateTimeOffset ReconciledAt { get; set; }
    public Guid ReconciledByEmployeeId { get; set; }
    public Employee? ReconciledByEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<JobOrderFatReconciliationLine> Lines { get; set; } = [];
}

public sealed class JobOrderFatReconciliationLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid JobOrderFatReconciliationId { get; set; }
    public JobOrderFatReconciliation? JobOrderFatReconciliation { get; set; }
    public Guid MaterialIssueLineId { get; set; }
    public MaterialIssueLine? MaterialIssueLine { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public Guid CustodianEmployeeId { get; set; }
    public string CustodianEmployeeCodeSnapshot { get; set; } = string.Empty;
    public decimal IssuedQuantityBase { get; set; }
    public decimal FittedQuantityBase { get; set; }
    public decimal ReturnedQuantityBase { get; set; }
    public decimal ReturnedLateQuantityBase { get; set; }
    public decimal ExplainedLostQuantityBase { get; set; }
    public decimal ExplainedScrappedQuantityBase { get; set; }
    public decimal UnexplainedQuantityBase { get; set; }
    public string Classification { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}