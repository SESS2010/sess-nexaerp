using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class IntercompanyEndpoints
{
    public static IEndpointRouteBuilder MapIntercompanyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var routes = endpoints.MapGroup("/api/v1/stores/intercompany/routes")
            .WithTags("Intercompany routes").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        routes.MapGet("/options", (IIntercompanyService service, CancellationToken ct) => service.RouteOptionsAsync(ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.View);
        routes.MapGet("/", (int? page, int? pageSize, IIntercompanyService service, CancellationToken ct) => service.RoutesAsync(page, pageSize, ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.View);
        routes.MapGet("/{id:guid}", async (Guid id, IIntercompanyService service, CancellationToken ct) =>
            await service.RouteAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.View);
        routes.MapPost("/", (ProposeIntercompanyRouteRequest request, IIntercompanyService service, CancellationToken ct) =>
            service.ProposeRouteAsync(request, ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.Create);
        routes.MapPost("/{id:guid}/approve", (Guid id, DecideIntercompanyRouteRequest request, IIntercompanyService service, CancellationToken ct) =>
            service.DecideRouteAsync(id, "APPROVED", request, ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.Approve);
        routes.MapPost("/{id:guid}/reject", (Guid id, DecideIntercompanyRouteRequest request, IIntercompanyService service, CancellationToken ct) =>
            service.DecideRouteAsync(id, "REJECTED", request, ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.Reject);
        routes.MapPost("/{id:guid}/revoke", (Guid id, DecideIntercompanyRouteRequest request, IIntercompanyService service, CancellationToken ct) =>
            service.DecideRouteAsync(id, "REVOKED", request, ct))
            .RequirePagePermission("stores.intercompany-routes", PagePermissionActions.Deactivate);
        var purchases = endpoints.MapGroup("/api/v1/stores/intercompany/purchases")
            .WithTags("Intercompany purchase handover").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        purchases.MapGet("/options", (IIntercompanyService service, CancellationToken ct) => service.PurchaseOptionsAsync(ct))
            .RequirePagePermission("purchase.intercompany-orders", PagePermissionActions.Issue);
        purchases.MapGet("/", (int? page, int? pageSize, IIntercompanyService service, CancellationToken ct) => service.PurchasesAsync(page, pageSize, ct))
            .RequirePagePermission("purchase.intercompany-orders", PagePermissionActions.View);
        purchases.MapGet("/{id:guid}", async (Guid id, IIntercompanyService service, CancellationToken ct) =>
            await service.PurchaseAsync(id, ct) is { } result ? Results.Ok(result) : Results.NotFound())
            .RequirePagePermission("purchase.intercompany-orders", PagePermissionActions.View);
        purchases.MapPost("/", (PublishIntercompanyPurchaseRequest request, IIntercompanyService service, CancellationToken ct) =>
            service.PublishPurchaseAsync(request, ct))
            .RequirePagePermission("purchase.intercompany-orders", PagePermissionActions.Issue);
        return endpoints;
    }
}
