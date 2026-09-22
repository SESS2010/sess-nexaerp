using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class InventoryPeriodEndpoints
{
    public static IEndpointRouteBuilder MapInventoryPeriodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string page = "accounts.inventory-periods";
        var group = endpoints.MapGroup("/api/v1/accounts/inventory-periods").WithTags("Accounts - Inventory periods")
            .RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (IInventoryPeriodService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ListAsync(ct), h)).RequirePagePermission(page, PagePermissionActions.View);
        group.MapGet("/{id:guid}", (Guid id, IInventoryPeriodService service, HttpContext h, CancellationToken ct) =>
            Run(async () => await service.GetAsync(id, ct)
                ?? throw new KeyNotFoundException("Inventory period not found in this company."), h))
            .RequirePagePermission(page, PagePermissionActions.View);
        group.MapPost("/", (OpenInventoryPeriodRequest request, IInventoryPeriodService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.OpenAsync(request, ct), h)).RequirePagePermission(page, PagePermissionActions.Approve);
        group.MapPost("/{id:guid}/close", (Guid id, CloseInventoryPeriodRequest request, IInventoryPeriodService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.CloseAsync(id, request, ct), h)).RequirePagePermission(page, PagePermissionActions.Approve);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h)
    {
        try { return Results.Ok(await action()); }
        catch (StoresValidationException error) { return Results.BadRequest(new { message = error.Message }); }
        catch (KeyNotFoundException error) { return Results.NotFound(new { message = error.Message }); }
        catch (StoresConflictException error) { return Results.Conflict(new { message = error.Message }); }
        catch (DbUpdateConcurrencyException error) { return Results.Conflict(new { message = error.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
    }
}
