using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class OpeningStockEndpoints
{
    public static IEndpointRouteBuilder MapOpeningStockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/stores/opening-stock")
            .WithTags("Stores - Opening Stock").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (string? status, int? page, int? pageSize,
            IOpeningStockService service, CancellationToken ct) =>
            service.ListAsync(status, page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission("stores.opening-stock", PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IOpeningStockService service,
            CancellationToken ct) => await service.GetAsync(id, ct) is { } value
                ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("stores.opening-stock", PagePermissionActions.View);
        group.MapPost("/from-import", (CreateOpeningStockFromImportRequest request,
            IOpeningStockService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordCountAsync(request, ct), h, true))
            .RequirePagePermission("stores.opening-stock", PagePermissionActions.Create);
        group.MapPost("/{id:guid}/confirm-value", (Guid id,
            OpeningStockTransitionRequest request, IOpeningStockService service,
            HttpContext h, CancellationToken ct) =>
            Run(() => service.ConfirmValueAsync(id, request, ct), h, false))
            .RequirePagePermission("stores.opening-stock", PagePermissionActions.Verify);
        group.MapPost("/{id:guid}/authorize", (Guid id,
            OpeningStockTransitionRequest request, IOpeningStockService service,
            HttpContext h, CancellationToken ct) =>
            Run(() => service.AuthorizeAsync(id, request, ct), h, false))
            .RequirePagePermission("stores.opening-stock", PagePermissionActions.Approve);
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
