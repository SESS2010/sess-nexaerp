using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// R10 (26 Sep): the selected company's legal identity. Any signed-in employee of the company reads
/// it; only the Technical Director changes it (checked in the service, with version, reason and audit).
/// </summary>
public static class CompanyProfileEndpoints
{
    public static IEndpointRouteBuilder MapCompanyProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/company").WithTags("Company profile").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/profile", (ICompanyProfileService service, HttpContext h, CancellationToken ct) => Run(() => service.GetAsync(ct), h));
        group.MapPut("/profile", (SaveCompanyProfileRequest request, ICompanyProfileService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.SaveAsync(request, ct), h));
        group.MapGet("/warehouse-state-codes", (ICompanyProfileService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ListWarehouseStatesAsync(ct), h));
        group.MapPut("/warehouse-state-codes/{warehouseCode}", (string warehouseCode, SaveWarehouseStateCodeRequest request, ICompanyProfileService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.SaveWarehouseStateAsync(warehouseCode, request, ct), h));
        return endpoints;
    }

    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h)
    {
        try { return Results.Ok(await action()); }
        catch (StoresValidationException e) when (e.Errors is { Count: > 0 }) { return Results.BadRequest(new { message = e.Message, errors = e.Errors }); }
        catch (StoresValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
        catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException e) { return Results.Conflict(new { message = e.Message }); }
    }
}
