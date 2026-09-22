namespace SESS.NexaERP.Domain.Stores;

public static class StockAdjustmentStatuses
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Approved = "APPROVED";
    public const string Posted = "POSTED";
    public const string Rejected = "REJECTED";
}

public static class StockAdjustmentReasonKinds
{
    public const string CountVariance = "COUNT_VARIANCE";
    public const string DamageLoss = "DAMAGE_LOSS";
    public const string Correction = "CORRECTION";
    public static readonly string[] All = [CountVariance, DamageLoss, Correction];
}

/// <summary>
/// A stock adjustment: count variance, damage/loss write-off or correction, recorded by Stores,
/// decided by the roles the approval snapshot names, posted to the physical and FIFO ledgers by
/// the decision that completes the review. Lines and decisions are append-only; a posted
/// adjustment is immutable and corrected only by a reversing adjustment.
/// </summary>
public sealed class StockAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string ReasonKind { get; set; } = StockAdjustmentReasonKinds.CountVariance;
    public DateOnly EffectiveDate { get; set; }
    public Guid InventoryPeriodId { get; set; }
    public string Status { get; set; } = StockAdjustmentStatuses.Draft;
    public int CurrentRevisionNumber { get; set; } = 1;
    public string Remarks { get; set; } = string.Empty;
    public Guid RecordedByEmployeeId { get; set; }
    public Guid RecordedRoleAssignmentId { get; set; }
    public string CounterEmployeeIdsJson { get; set; } = "[]";
    public string? BackdateReason { get; set; }
    public Guid? BackdateEvidenceId { get; set; }
    public int DaysBackdated { get; set; }
    public string? ApprovalSnapshotJson { get; set; }
    public Guid? ReversesStockAdjustmentId { get; set; }
    public Guid? StockPostingBatchId { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public Guid? PostedByEmployeeId { get; set; }
    public string? PostingIdempotencyKey { get; set; }
    public string? PostingRequestFingerprint { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public long Version { get; set; }
}

public sealed class StockAdjustmentLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid StockAdjustmentId { get; set; }
    public int RevisionNumber { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseConditionLocationId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal? UnitValue { get; set; }
    public decimal AcceptedLineValue { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class StockAdjustmentDecision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid StockAdjustmentId { get; set; }
    public int RevisionNumber { get; set; }
    public string Decision { get; set; } = "APPROVE";
    public Guid EmployeeId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public Guid RoleAssignmentId { get; set; }
    public string RoleAssignmentType { get; set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "system";
}
