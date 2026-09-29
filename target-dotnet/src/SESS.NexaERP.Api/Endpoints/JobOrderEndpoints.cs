using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class JobOrderEndpoints
{
    public static IEndpointRouteBuilder MapJobOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/production/job-orders").WithTags("Job Orders").RequireAuthorization();
        group.MapGet("/", (int? page, int? pageSize, string? search, string? status, IJobOrderService service, CancellationToken ct) =>
            service.ListAsync(page, pageSize, search, status, ct)).RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/customer-po-lines", (IJobOrderService service, CancellationToken ct) =>
            service.CustomerPoLinesAsync(ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IJobOrderService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/{id:guid}/history", (Guid id, IJobOrderService service, CancellationToken ct) =>
            service.HistoryAsync(id, ct)).RequirePagePermission("production.job-orders", PagePermissionActions.ViewAuditHistory);
        group.MapPost("/", async (CreateJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            Results.Created("", await service.CreateAsync(request, ct)))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Create);
        group.MapPost("/{id:guid}/accounts-confirm", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ConfirmAccountsAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Verify);
        group.MapPost("/{id:guid}/return-to-draft", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ReturnToDraftAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Reject);
        group.MapPut("/{id:guid}/draft", (Guid id, ReviseDraftJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ReviseDraftAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Update);
        group.MapPost("/{id:guid}/resubmit", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ResubmitAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Submit);
        return endpoints;
    }
}