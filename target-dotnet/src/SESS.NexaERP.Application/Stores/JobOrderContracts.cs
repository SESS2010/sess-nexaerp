using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record CreateJobOrderRequest(Guid CustomerPurchaseOrderLineId, int MachineOrdinal,
    string MachineSerial, DateOnly JobOrderDate, DateOnly? PlannedCompletionDate, string IdempotencyKey);
public sealed record ConfirmJobOrderRequest(uint ExpectedVersion, string Reason, string IdempotencyKey);
public sealed record JobOrderSummary(Guid Id, string JobOrderNumber, Guid CustomerPurchaseOrderId,
    Guid CustomerPurchaseOrderLineId, string CustomerPoNumber, int MachineOrdinal, string MachineModel,
    string MachineSerial, string CustomerName, string Status, DateOnly JobOrderDate,
    DateOnly? PlannedCompletionDate, uint Version);
public sealed record JobOrderView(Guid Id, string JobOrderNumber, Guid CustomerPurchaseOrderId,
    Guid CustomerPurchaseOrderLineId, string CustomerPoRecordNumber, string CustomerPoNumber,
    int CustomerPoRevisionNumber, int CustomerPoLineNumber, Guid MachineItemId, string MachineItemCode,
    int MachineOrdinal, string MachineModel, string MachineSerial, string CustomerName, string Status,
    DateOnly JobOrderDate, DateOnly? PlannedCompletionDate, Guid InitiatedByEmployeeId,
    string InitiatedActorRoleCode, Guid InitiatedRoleAssignmentId, string InitiatedRoleAssignmentType,
    DateTimeOffset? AccountsConfirmedAt, Guid? AccountsConfirmedByEmployeeId,
    string? AccountsConfirmationActorRoleCode, Guid? AccountsConfirmationRoleAssignmentId,
    string? AccountsConfirmationRoleAssignmentType, string? AccountsConfirmationReason, string FatReadinessStatus,
    DateTimeOffset? FatReconciledAt, Guid? FatReconciledByEmployeeId, Guid? LatestFatReconciliationId, uint Version);
public sealed record JobOrderHistoryView(Guid Id, string Action, string? FromStatus, string ToStatus,
    Guid ActorEmployeeId, string ActorRoleCode, Guid ResolvedRoleAssignmentId,
    string ResolvedRoleAssignmentType, string CorrelationId, string Remarks, DateTimeOffset CreatedAt);

public interface IJobOrderService
{
    Task<PagedResponse<JobOrderSummary>> ListAsync(int? page, int? pageSize, string? search, string? status, CancellationToken ct);
    Task<JobOrderView?> GetAsync(Guid id, CancellationToken ct);
    Task<JobOrderView> CreateAsync(CreateJobOrderRequest request, CancellationToken ct);
    Task<JobOrderView> ConfirmAccountsAsync(Guid id, ConfirmJobOrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<JobOrderHistoryView>> HistoryAsync(Guid id, CancellationToken ct);
}