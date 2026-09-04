using SESS.NexaERP.Application.Common;

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
    DateTimeOffset CreatedAt,
    string ActorRoleCode = "");

public interface IAuditHistoryService
{
    Task<PagedResponse<AuditLogSummary>> GetCompanyHistoryAsync(
        string? module,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
