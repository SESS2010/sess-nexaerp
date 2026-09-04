using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record QcQueueItem(
    Guid GoodsReceiptLineLotAllocationId,
    string GrnNumber,
    Guid GoodsReceiptLineId,
    int LineNumber,
    int LotOrdinal,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    Guid InventoryLotId,
    string? SupplierLotNumber,
    decimal Quantity,
    IReadOnlyList<Guid> InventorySerialIds,
    DateTimeOffset ReceivedAt,
    int AgeDays,
    int CompletionLimitDays,
    bool IsOverdue,
    bool HasEffectivePolicy,
    string PolicyResolution);

public sealed record QcParameterResultRequest(
    Guid QcInspectionPolicyId,
    int SampleOrdinal,
    decimal? ObservedNumericValue,
    string? ObservedTextValue,
    string Result,
    string? Remarks);

public sealed record QcSerialDispositionRequest(Guid InventorySerialId, string Disposition, string? Reason);

public sealed record FinalizeQcInspectionRequest(
    Guid GoodsReceiptLineLotAllocationId,
    DateTimeOffset InspectionStartedAt,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    decimal DiscrepancyPendingQuantity,
    Guid? AcceptedConditionLocationId,
    IReadOnlyList<QcParameterResultRequest> ParameterResults,
    IReadOnlyList<QcSerialDispositionRequest> SerialDispositions);

public sealed record CorrectQcInspectionRequest(
    Guid RevisesRevisionId,
    string CorrectionReason,
    DateTimeOffset InspectionStartedAt,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    decimal DiscrepancyPendingQuantity,
    Guid? AcceptedConditionLocationId,
    IReadOnlyList<QcParameterResultRequest> ParameterResults,
    IReadOnlyList<QcSerialDispositionRequest> SerialDispositions);

public sealed record QcParameterResultView(Guid Id, string ParameterCode, string MeasuredValue, string Result);
public sealed record QcSerialDispositionView(Guid InventorySerialId, string SerialNumber, string Disposition);

public sealed record QcInspectionResult(
    Guid InspectionId,
    string InspectionNumber,
    Guid RevisionId,
    Guid QcInspectionLotDispositionId,
    int RevisionNumber,
    Guid GoodsReceiptLineLotAllocationId,
    string GrnNumber,
    string ItemCode,
    int LotOrdinal,
    decimal InspectedQuantity,
    decimal AcceptedQuantity,
    decimal RejectedQuantity,
    decimal DiscrepancyPendingQuantity,
    string Decision,
    string Status,
    string InspectorBasis,
    Guid InspectorEmployeeId,
    Guid? StockPostingBatchId,
    bool Replayed,
    IReadOnlyList<QcParameterResultView> ParameterResults,
    IReadOnlyList<QcSerialDispositionView> SerialDispositions);

public sealed record CreateInventoryConcessionRequest(
    Guid QcInspectionLotDispositionId,
    Guid FailedParameterResultId,
    decimal Quantity,
    string FailedParameter,
    string MeasuredValue,
    string TechnicalJustification,
    string IntendedUse,
    IReadOnlyList<Guid> InventorySerialIds);

public sealed record ApproveInventoryConcessionRequest(uint Version, Guid AvailableConditionLocationId, string DecisionReason);
public sealed record RejectInventoryConcessionRequest(uint Version, string DecisionReason);
public sealed record ReverseInventoryConcessionRequest(uint Version, string Reason);

public sealed record InventoryConcessionResult(
    Guid Id,
    string ConcessionNumber,
    string Status,
    Guid QcInspectionRevisionId,
    Guid QcInspectionLotDispositionId,
    decimal Quantity,
    Guid GoodsReceiptLineLotAllocationId,
    IReadOnlyList<Guid> InventorySerialIds,
    string FailedParameter,
    string MeasuredValue,
    string TechnicalJustification,
    string IntendedUse,
    Guid CreatedByEmployeeId,
    Guid? DecidedByEmployeeId,
    string? DecidedRoleCode,
    Guid? StockPostingBatchId,
    Guid? AvailableProvenanceLayerId,
    string? ProvenanceAnnotationJson,
    uint Version,
    bool Replayed);

public interface IQcWorkflowService
{
    Task<PagedResponse<QcQueueItem>> QueueAsync(Guid? allocationId, string? grnNumber, bool overdueOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<QcInspectionResult> FinalizeAsync(FinalizeQcInspectionRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<QcInspectionResult> CorrectAsync(string inspectionNumber, CorrectQcInspectionRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<QcInspectionResult?> GetAsync(string inspectionNumber, CancellationToken cancellationToken);
    Task<InventoryConcessionResult> CreateConcessionAsync(CreateInventoryConcessionRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<InventoryConcessionResult> ApproveConcessionAsync(string concessionNumber, ApproveInventoryConcessionRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<InventoryConcessionResult> RejectConcessionAsync(string concessionNumber, RejectInventoryConcessionRequest request, CancellationToken cancellationToken);
    Task<InventoryConcessionResult> ReverseConcessionAsync(string concessionNumber, ReverseInventoryConcessionRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<InventoryConcessionResult?> GetConcessionAsync(string concessionNumber, CancellationToken cancellationToken);
}
