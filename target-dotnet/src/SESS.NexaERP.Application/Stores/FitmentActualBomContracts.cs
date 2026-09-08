using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record ConfirmComponentFitmentRequest(Guid JobOrderId, Guid MaterialIssueLineId,
    decimal QuantityBase, DateTimeOffset FittedAt, string ConfirmationNote,
    Guid? ReverifiesFitmentId, string IdempotencyKey);

public sealed record ReverseComponentFitmentRequest(string Reason, string IdempotencyKey);

public sealed record ComponentFitmentSummary(Guid Id, string FitmentNumber, Guid JobOrderId,
    string JobOrderNumber, Guid MaterialIssueLineId, Guid ItemId, string ItemCode,
    decimal QuantityBase, DateTimeOffset FittedAt, Guid ConfirmedByEmployeeId,
    string ActorRoleCode, Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    bool IsReversed, bool IsSelfReversal, DateTimeOffset? ReversedAt, string? ReversalReason,
    Guid? ReverifiesFitmentId, bool Replayed);

public sealed record ActualBomEntryView(Guid Id, string EntryKind, Guid? ComponentFitmentId,
    Guid? ComponentFitmentReversalId, Guid MaterialIssueLineId, Guid ItemId, string ItemCode,
    string ItemName, Guid UomId, string UomCode, decimal QuantityBase,
    Guid InventoryProvenanceLayerId, Guid? InventoryLotId, Guid? InventorySerialId,
    string? SerialNumber, Guid GoodsReceiptLineId, string GrnNumber, Guid VendorBillLineId,
    string BillNumber, decimal AcceptedMaterialValue, decimal AllocatedChargeValue,
    decimal TotalAcceptedValue, DateTimeOffset OccurredAt);

public sealed record ActualBomView(Guid Id, Guid JobOrderId, string JobOrderNumber,
    DateTimeOffset GeneratedAt, decimal TotalAcceptedMaterialValue,
    decimal TotalAllocatedChargeValue, decimal TotalAcceptedValue,
    IReadOnlyList<ActualBomEntryView> Entries);

public interface IFitmentActualBomService
{
    Task<PagedResponse<ComponentFitmentSummary>> ListAsync(int? page, int? pageSize,
        Guid? jobOrderId, bool? activeOnly, CancellationToken ct);
    Task<ComponentFitmentSummary?> GetAsync(Guid id, CancellationToken ct);
    Task<ComponentFitmentSummary> ConfirmAsync(ConfirmComponentFitmentRequest request, CancellationToken ct);
    Task<ComponentFitmentSummary> ReverseAsync(Guid id, ReverseComponentFitmentRequest request, CancellationToken ct);
    Task<ActualBomView?> GetActualBomAsync(Guid jobOrderId, CancellationToken ct);
}