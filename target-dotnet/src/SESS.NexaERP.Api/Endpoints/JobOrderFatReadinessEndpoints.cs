using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class JobOrderFatReadinessEndpoints
{
    public static IEndpointRouteBuilder MapJobOrderFatReadinessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/production/job-orders/{jobOrderId:guid}/fat-readiness")
            .WithTags("Job Order FAT Readiness").RequireAuthorization();
        group.MapGet("/", async (Guid jobOrderId, IJobOrderFatReadinessService service, CancellationToken ct) =>
            await service.GetAsync(jobOrderId, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.fat-readiness", PagePermissionActions.View);
        group.MapPost("/custody-explanations", (Guid jobOrderId, CreateFatCustodyExplanationRequest request,
            IJobOrderFatReadinessService service, CancellationToken ct) => service.ExplainAsync(jobOrderId, request, ct))
            .RequirePagePermission("production.fat-readiness", PagePermissionActions.Create);
        group.MapPost("/reconcile", (Guid jobOrderId, ReconcileJobOrderFatRequest request,
            IJobOrderFatReadinessService service, CancellationToken ct) => service.ReconcileAsync(jobOrderId, request, ct))
            .RequirePagePermission("production.fat-readiness", PagePermissionActions.Verify);
        return endpoints;
    }
}