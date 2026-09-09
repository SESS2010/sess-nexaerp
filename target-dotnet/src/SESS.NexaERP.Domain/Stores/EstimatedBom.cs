using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public sealed class EstimatedBom : CompanyScopedAuditableEntity
{
    public string BomNumber { get; set; } = string.Empty;
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public int CurrentRevisionNumber { get; set; }
    public Guid? ApprovedRevisionId { get; set; }
    public Guid? CommercialBaselineRevisionId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public List<EstimatedBomRevision> Revisions { get; set; } = [];
}

public sealed class EstimatedBomHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid EstimatedBomId { get; set; }
    public Guid EstimatedBomRevisionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class EstimatedBomRevision : CompanyScopedAuditableEntity
{
    public Guid EstimatedBomId { get; set; }
    public EstimatedBom? EstimatedBom { get; set; }
    public int RevisionNumber { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string RevisionReason { get; set; } = string.Empty;
    public Guid PreparedByEmployeeId { get; set; }
    public Employee? PreparedByEmployee { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public string? ApprovalReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ContentFingerprint { get; set; } = string.Empty;
    public List<EstimatedBomLine> Lines { get; set; } = [];
}

public sealed class EstimatedBomLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid EstimatedBomRevisionId { get; set; }
    public EstimatedBomRevision? EstimatedBomRevision { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal? EstimatedUnitValue { get; set; }
    public bool EstimatedUnitValueOverridden { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}
