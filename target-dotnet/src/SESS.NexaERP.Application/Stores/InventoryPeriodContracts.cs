namespace SESS.NexaERP.Application.Stores;

public sealed record OpenInventoryPeriodRequest(string Code, string Name, DateOnly StartDate,
    DateOnly EndDate, string Reason, string IdempotencyKey);
public sealed record CloseInventoryPeriodRequest(long Version, string Reason, string IdempotencyKey);
public sealed record InventoryPeriodDecisionView(Guid Id, string Action, long PeriodVersion,
    Guid ActorEmployeeId, Guid RoleAssignmentId, string RoleAssignmentType,
    string Reason, DateTimeOffset RecordedAt, string RecordedBy);
public sealed record InventoryPeriodView(Guid Id, string Code, string Name, DateOnly StartDate,
    DateOnly EndDate, string Status, long Version, DateTimeOffset CreatedAt,
    string CreatedBy, DateTimeOffset? ClosedAt, IReadOnlyList<InventoryPeriodDecisionView> Decisions,
    bool Replayed = false);
public interface IInventoryPeriodService
{
    Task<IReadOnlyList<InventoryPeriodView>> ListAsync(CancellationToken ct);
    Task<InventoryPeriodView?> GetAsync(Guid id, CancellationToken ct);
    Task<InventoryPeriodView> OpenAsync(OpenInventoryPeriodRequest request, CancellationToken ct);
    Task<InventoryPeriodView> CloseAsync(Guid id, CloseInventoryPeriodRequest request, CancellationToken ct);
}
