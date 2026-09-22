using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class StockAdjustmentEndpoints
{
    public static IEndpointRouteBuilder MapStockAdjustmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string page = "stores.stock-adjustments";
        var group = endpoints.MapGroup("/api/v1/stores/stock-adjustments")
            .WithTags("Stores - Stock Adjustments").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (string? status, int? page, int? pageSize, IStockAdjustmentService service, CancellationToken ct) =>
            service.ListAsync(status, page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission(page, PagePermissionActions.View);
        group.MapGet("/inventory-periods", (IStockAdjustmentService service, CancellationToken ct) => service.ListOpenPeriodsAsync(ct))
            .RequirePagePermission(page, PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IStockAdjustmentService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission(page, PagePermissionActions.View);
        group.MapPost("/", (CreateStockAdjustmentRequest request, IStockAdjustmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.CreateAsync(request, ct), h, true))
            .RequirePagePermission(page, PagePermissionActions.Create);
        group.MapPut("/{id:guid}", (Guid id, ReviseStockAdjustmentRequest request, IStockAdjustmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ReviseAsync(id, request, ct), h, false))
            .RequirePagePermission(page, PagePermissionActions.Update);
        group.MapPost("/{id:guid}/submit", (Guid id, StockAdjustmentTransitionRequest request, IStockAdjustmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.SubmitAsync(id, request, ct), h, false))
            .RequirePagePermission(page, PagePermissionActions.Submit);
        group.MapPost("/{id:guid}/approve", (Guid id, StockAdjustmentDecisionRequest request, IStockAdjustmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ApproveAsync(id, request, ct), h, false))
            .RequirePagePermission(page, PagePermissionActions.Approve);
        group.MapPost("/{id:guid}/reject", (Guid id, StockAdjustmentTransitionRequest request, IStockAdjustmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.RejectAsync(id, request, ct), h, false))
            .RequirePagePermission(page, PagePermissionActions.Reject);
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h, bool created)
    {
        try { var value = await action(); return created ? Results.Created(string.Empty, value) : Results.Ok(value); }
        catch (StoresValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
        catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException e) { return Results.Conflict(new { message = e.Message }); }
    }
}
