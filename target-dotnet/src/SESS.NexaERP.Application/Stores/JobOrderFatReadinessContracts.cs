using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record CreateFatCustodyExplanationRequest(Guid MaterialIssueLineId, decimal QuantityBase,
    string Disposition, string Reason, string IdempotencyKey);
public sealed record ReconcileJobOrderFatRequest(string Reason, string IdempotencyKey);
public sealed record FatCustodyExplanationView(Guid Id, Guid JobOrderId, Guid MaterialIssueLineId,
    decimal QuantityBase, string Disposition, string Reason, Guid ExplainedByEmployeeId,
    string ActorRoleCode, Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    DateTimeOffset CreatedAt, bool Replayed);
public sealed record FatReconciliationLineView(Guid Id, Guid MaterialIssueLineId, Guid ItemId,
    string ItemCode, Guid CustodianEmployeeId, string CustodianEmployeeCode,
    decimal IssuedQuantityBase, decimal FittedQuantityBase, decimal ReturnedQuantityBase,
    decimal ReturnedLateQuantityBase, decimal ExplainedLostQuantityBase,
    decimal ExplainedScrappedQuantityBase, decimal UnexplainedQuantityBase, string Classification);
public sealed record FatReconciliationView(Guid Id, Guid JobOrderId, int AttemptNumber, string Result,
    decimal IssuedQuantityBase, decimal FittedQuantityBase, decimal ReturnedQuantityBase,
    decimal ExplainedQuantityBase, decimal UnexplainedQuantityBase, DateTimeOffset ReconciledAt,
    Guid ReconciledByEmployeeId, string ActorRoleCode, Guid ResolvedRoleAssignmentId,
    string ResolvedRoleAssignmentType, string Reason, IReadOnlyList<FatReconciliationLineView> Lines,
    bool Replayed);
public sealed record JobOrderFatReadinessView(Guid JobOrderId, string JobOrderNumber,
    string FatReadinessStatus, DateTimeOffset? FatReconciledAt, Guid? FatReconciledByEmployeeId,
    Guid? LatestFatReconciliationId, FatReconciliationView? LatestReconciliation);

public interface IJobOrderFatReadinessService
{
    Task<JobOrderFatReadinessView?> GetAsync(Guid jobOrderId, CancellationToken ct);
    Task<FatCustodyExplanationView> ExplainAsync(Guid jobOrderId,
        CreateFatCustodyExplanationRequest request, CancellationToken ct);
    Task<FatReconciliationView> ReconcileAsync(Guid jobOrderId,
        ReconcileJobOrderFatRequest request, CancellationToken ct);
}