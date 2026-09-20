namespace SESS.NexaERP.Application.Stores;

/// <summary>One adjustment line: a signed quantity change at an AVAILABLE location; additions carry the stated ex-tax unit value.</summary>
public sealed record StockAdjustmentLineInput(
    Guid ItemId, Guid WarehouseConditionLocationId, decimal QuantityChange,
    decimal? UnitValue = null, string? LotNumber = null, string? SerialNumber = null, string? Remarks = null);
public sealed record CreateStockAdjustmentRequest(
    Guid WarehouseId, string ReasonKind, DateOnly EffectiveDate, Guid InventoryPeriodId,
    string Remarks, IReadOnlyList<StockAdjustmentLineInput> Lines, string IdempotencyKey,
    IReadOnlyList<Guid>? CounterEmployeeIds = null, string? BackdateReason = null,
    Guid? BackdateEvidenceId = null, Guid? ReversesStockAdjustmentId = null);
/// <summary>Replaces the lines of a DRAFT or SUBMITTED adjustment as a new revision; decisions already taken bind to the old revision.</summary>
public sealed record ReviseStockAdjustmentRequest(
    long Version, string Remarks, IReadOnlyList<StockAdjustmentLineInput> Lines, string Reason, string IdempotencyKey,
    string? BackdateReason = null, Guid? BackdateEvidenceId = null);
public sealed record StockAdjustmentTransitionRequest(long Version, string Reason, string IdempotencyKey);
/// <summary>An approval; RoleCode names which of the outstanding required roles the approver acts in (default: the least privileged one held).</summary>
public sealed record StockAdjustmentDecisionRequest(long Version, string Reason, string IdempotencyKey, string? RoleCode = null);

public sealed record StockAdjustmentLineView(
    Guid Id, int LineNumber, Guid ItemId, string ItemCode, string ItemName,
    Guid WarehouseConditionLocationId, Guid RackBinId, string RackBinCode,
    string? LotNumber, string? SerialNumber, decimal QuantityChange, decimal? UnitValue,
    decimal AcceptedLineValue, string? Remarks);
public sealed record StockAdjustmentDecisionView(
    Guid Id, int RevisionNumber, string Decision, Guid EmployeeId, string EmployeeCode, string EmployeeName,
    string RoleCode, Guid RoleAssignmentId, string RoleAssignmentType, DateTimeOffset DecidedAt, string Reason);
public sealed record StockAdjustmentView(
    Guid Id, string AdjustmentNumber, Guid WarehouseId, string WarehouseCode, string ReasonKind,
    DateOnly EffectiveDate, Guid InventoryPeriodId, string InventoryPeriodCode, string Status,
    int CurrentRevisionNumber, string Remarks, Guid RecordedByEmployeeId, string RecordedByEmployeeCode,
    IReadOnlyList<Guid> CounterEmployeeIds, string? BackdateReason, Guid? BackdateEvidenceId, int DaysBackdated,
    decimal AbsoluteValue, IReadOnlyList<string> RequiredRoleCodes, IReadOnlyList<string> OutstandingRoleCodes,
    IReadOnlyList<Guid> ExcludedEmployeeIds, Guid? ReversesStockAdjustmentId, Guid? StockPostingBatchId,
    DateTimeOffset? PostedAt, long Version, bool Replayed,
    IReadOnlyList<StockAdjustmentLineView> Lines, IReadOnlyList<StockAdjustmentDecisionView> Decisions);
public sealed record StockAdjustmentPage(int Total, int Page, int PageSize, IReadOnlyList<StockAdjustmentView> Items);
/// <summary>An inventory period a Stores user may date an adjustment into (the CFO list is CFO-only).</summary>
public sealed record StockAdjustmentPeriodView(Guid Id, string Code, string Name, DateOnly StartDate, DateOnly EndDate, string Status);

public interface IStockAdjustmentService
{
    Task<IReadOnlyList<StockAdjustmentPeriodView>> ListOpenPeriodsAsync(CancellationToken ct);
    Task<StockAdjustmentPage> ListAsync(string? status, int page, int pageSize, CancellationToken ct);
    Task<StockAdjustmentView?> GetAsync(Guid id, CancellationToken ct);
    Task<StockAdjustmentView> CreateAsync(CreateStockAdjustmentRequest request, CancellationToken ct);
    Task<StockAdjustmentView> ReviseAsync(Guid id, ReviseStockAdjustmentRequest request, CancellationToken ct);
    Task<StockAdjustmentView> SubmitAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct);
    Task<StockAdjustmentView> ApproveAsync(Guid id, StockAdjustmentDecisionRequest request, CancellationToken ct);
    Task<StockAdjustmentView> RejectAsync(Guid id, StockAdjustmentTransitionRequest request, CancellationToken ct);
}
