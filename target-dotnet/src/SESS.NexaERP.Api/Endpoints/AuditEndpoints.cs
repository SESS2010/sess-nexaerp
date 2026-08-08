using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/audit")
            .WithTags("Audit")
            .RequireAuthorization();

        group.MapGet("/history", async (NexaErpDbContext db, string? module, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            var safePage = Math.Max(page ?? 1, 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(module))
            {
                var normalizedModule = module.Trim();
                query = query.Where(log => log.Module == normalizedModule);
            }

            var rows = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(log => new AuditLogSummary(log.Id, log.Module, log.Action, log.EntityName, log.EntityId, log.UserLoginId, log.Result, log.CorrelationId, log.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(rows);
        });

        return endpoints;
    }
}
