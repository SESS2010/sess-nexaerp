using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Api.Security;

namespace SESS.NexaERP.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/audit")
            .WithTags("Audit")
            .RequireAuthorization();

        group.MapGet("/history", async (IAuditHistoryService service, string? module, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            var safePage = Math.Max(page ?? 1, 1);
            var safePageSize = Math.Clamp(pageSize ?? 50, 1, 200);
            try { return Results.Ok(await service.GetCompanyHistoryAsync(module, safePage, safePageSize, cancellationToken)); }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
        }).RequirePagePermission("audit.history", PagePermissionActions.ViewAuditHistory);

        return endpoints;
    }
}
