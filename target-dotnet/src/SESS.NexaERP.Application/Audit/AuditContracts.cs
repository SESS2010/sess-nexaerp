namespace SESS.NexaERP.Application.Audit;

public sealed record AuditLogSummary(
    Guid Id,
    string Module,
    string Action,
    string EntityName,
    string EntityId,
    string UserLoginId,
    string Result,
    string CorrelationId,
    DateTimeOffset CreatedAt);