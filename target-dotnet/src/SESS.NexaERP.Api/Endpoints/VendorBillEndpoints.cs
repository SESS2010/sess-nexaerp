using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class VendorBillEndpoints
{
    public static IEndpointRouteBuilder MapVendorBillEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/accounts/vendor-bills").WithTags("Accounts - Vendor Bills").RequireAuthorization().AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (string? billNumber, string? status, Guid? vendorId, int? page, int? pageSize, IVendorBillService service, CancellationToken ct) => service.ListAsync(billNumber, status, vendorId, page ?? 1, pageSize ?? 50, ct)).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IVendorBillService service, CancellationToken ct) => await service.GetAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound()).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.View);
        group.MapPost("/from-grn/{goodsReceiptId:guid}", (Guid goodsReceiptId, CreateVendorBillRequest request, IVendorBillService service, HttpContext h, CancellationToken ct) => Run(() => service.CreateAsync(goodsReceiptId, request, ct), h, true)).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.Create);
        group.MapPost("/{id:guid}/accept", (Guid id, VendorBillDecisionRequest request, IVendorBillService service, HttpContext h, CancellationToken ct) => Run(() => service.AcceptAsync(id, request, ct), h, false)).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.Approve);
        group.MapPost("/{id:guid}/reject", (Guid id, VendorBillDecisionRequest request, IVendorBillService service, HttpContext h, CancellationToken ct) => Run(() => service.RejectAsync(id, request, ct), h, false)).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.Reject);
        group.MapPost("/{id:guid}/reverse", (Guid id, VendorBillDecisionRequest request, IVendorBillService service, HttpContext h, CancellationToken ct) => Run(() => service.ReverseAsync(id, request, ct), h, false)).RequirePagePermission("accounts.vendor-bills", PagePermissionActions.Cancel);
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