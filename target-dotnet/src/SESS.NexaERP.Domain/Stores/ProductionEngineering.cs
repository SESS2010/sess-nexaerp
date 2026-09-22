using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public sealed class ProductionBom : CompanyScopedAuditableEntity
{
    public string BomNumber { get; set; } = string.Empty;
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public int CurrentRevisionNumber { get; set; }
    public string Status { get; set; } = "DRAFT";
    public List<ProductionBomRevision> Revisions { get; set; } = [];
}

public sealed class ProductionBomRevision : CompanyScopedAuditableEntity
{
    public Guid ProductionBomId { get; set; }
    public ProductionBom? ProductionBom { get; set; }
    public int RevisionNumber { get; set; }
    public Guid SourceEstimatedBomRevisionId { get; set; }
    public EstimatedBomRevision? SourceEstimatedBomRevision { get; set; }
    public Guid? SupersedesRevisionId { get; set; }
    public ProductionBomRevision? SupersedesRevision { get; set; }
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
    public List<ProductionBomLine> Lines { get; set; } = [];
}

public sealed class ProductionBomLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid ProductionBomRevisionId { get; set; }
    public ProductionBomRevision? ProductionBomRevision { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PlannedUnitValue { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class ProductionEngineeringHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? ProductionBomId { get; set; }
    public ProductionBom? ProductionBom { get; set; }
    public Guid? ProductionBomRevisionId { get; set; }
    public ProductionBomRevision? ProductionBomRevision { get; set; }
    public Guid? EngineeringDocumentId { get; set; }
    public EngineeringDocument? EngineeringDocument { get; set; }
    public Guid? EngineeringDocumentRevisionId { get; set; }
    public EngineeringDocumentRevision? EngineeringDocumentRevision { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class EngineeringDocument : CompanyScopedAuditableEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? CurrentRevisionId { get; set; }
    public EngineeringDocumentRevision? CurrentRevision { get; set; }
    public string Status { get; set; } = "DRAFT";
    public List<EngineeringDocumentRevision> Revisions { get; set; } = [];
}

public sealed class EngineeringDocumentRevision : CompanyScopedAuditableEntity
{
    public Guid EngineeringDocumentId { get; set; }
    public EngineeringDocument? EngineeringDocument { get; set; }
    public int RevisionNumber { get; set; }
    public string RevisionCode { get; set; } = string.Empty;
    public Guid? SupersedesRevisionId { get; set; }
    public EngineeringDocumentRevision? SupersedesRevision { get; set; }
    public Guid DrawnByEmployeeId { get; set; }
    public Employee? DrawnByEmployee { get; set; }
    public Guid CheckedByEmployeeId { get; set; }
    public Employee? CheckedByEmployee { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public string RevisionNote { get; set; } = string.Empty;
    public DateOnly DocumentDate { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ContentFingerprint { get; set; } = string.Empty;
}
