using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class FitmentActualBomEndpoints
{
    public static IEndpointRouteBuilder MapFitmentActualBomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/production/component-fitments")
            .WithTags("Production - Component Fitment").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (int? page, int? pageSize, Guid? jobOrderId, bool? activeOnly,
            IFitmentActualBomService service, CancellationToken ct) =>
            service.ListAsync(page, pageSize, jobOrderId, activeOnly, ct))
            .RequirePagePermission("production.component-fitments", PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IFitmentActualBomService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.component-fitments", PagePermissionActions.View);
        group.MapPost("/", (ConfirmComponentFitmentRequest request, IFitmentActualBomService service,
            HttpContext context, CancellationToken ct) => Run(() => service.ConfirmAsync(request, ct), context, true))
            .RequirePagePermission("production.component-fitments", PagePermissionActions.Create);
        group.MapPost("/{id:guid}/reverse", (Guid id, ReverseComponentFitmentRequest request,
            IFitmentActualBomService service, HttpContext context, CancellationToken ct) =>
            Run(() => service.ReverseAsync(id, request, ct), context, false))
            .RequirePagePermission("production.component-fitments", PagePermissionActions.Cancel);
        group.MapGet("/job-orders/{jobOrderId:guid}/actual-bom", async (Guid jobOrderId,
            IFitmentActualBomService service, CancellationToken ct) =>
            await service.GetActualBomAsync(jobOrderId, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.component-fitments", PagePermissionActions.View);
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext context, bool created)
    {
        try { var value = await action(); return created ? Results.Created(string.Empty, value) : Results.Ok(value); }
        catch (StoresValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException e) { return context.User.Identity?.IsAuthenticated == true
            ? Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Fitment authority refused", detail: e.Message)
            : Results.Unauthorized(); }
        catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException e) { return Results.Conflict(new { message = e.Message }); }
    }
}