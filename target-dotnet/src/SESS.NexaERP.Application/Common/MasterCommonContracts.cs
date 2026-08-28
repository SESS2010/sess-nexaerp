namespace SESS.NexaERP.Application.Common;

public sealed record PagedResponse<T>(int TotalCount, int PageNumber, int PageSize, IReadOnlyList<T> Items);

public sealed record MasterActionRequest(string Remarks, uint Version);

public sealed record MasterHistorySummary(Guid Id, string Action, string FromStatus, string ToStatus, string Remarks, string ActorLoginId, string ActorRoleCode, DateTimeOffset CreatedAt, string CorrelationId);

public sealed record MasterStatusHistorySummary(Guid Id, string? PreviousStatus, string NewStatus, string Reason, DateTimeOffset CreatedAt, string CorrelationId);

public sealed record MasterAuditHistorySummary(Guid Id, string Module, string Action, string UserLoginId, string Result, string CorrelationId, string? BeforeJson, string? AfterJson, DateTimeOffset CreatedAt);
