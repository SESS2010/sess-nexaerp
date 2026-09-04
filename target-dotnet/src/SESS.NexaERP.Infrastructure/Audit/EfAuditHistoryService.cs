using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Audit;

public sealed class EfAuditHistoryService(NexaErpDbContext db, ICurrentUser currentUser) : IAuditHistoryService
{
    public async Task<PagedResponse<AuditLogSummary>> GetCompanyHistoryAsync(
        string? module,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.OrganizationId))
            throw new UnauthorizedAccessException("A resolved company session is required.");

        var organization = currentUser.OrganizationId.Trim().ToUpperInvariant();
        var companyId = await db.Companies.AsNoTracking()
            .Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved company is inactive or unavailable.");

        var query = db.AuditLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Scope == "COMPANY");
        if (!string.IsNullOrWhiteSpace(module))
        {
            var normalizedModule = module.Trim();
            query = query.Where(log => log.Module == normalizedModule);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogSummary(log.Id, log.Module, log.Action, log.EntityName, log.EntityId,
                log.UserLoginId, log.Result, log.CorrelationId, log.CreatedAt, log.ActorRoleCode))
            .ToListAsync(cancellationToken);
        return new PagedResponse<AuditLogSummary>(total, page, pageSize, rows);
    }
}
