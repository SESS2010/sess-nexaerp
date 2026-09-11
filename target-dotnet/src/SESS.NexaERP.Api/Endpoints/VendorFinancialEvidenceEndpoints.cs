using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class VendorFinancialEvidenceEndpoints
{
    public static IEndpointRouteBuilder MapVendorFinancialEvidenceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/accounts/vendor-financial-evidence")
            .WithTags("Accounts - Vendor Advances and Payments")
            .RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);

        group.MapGet("/advance-purchase-orders", (
            Guid? vendorId, IVendorFinancialEvidenceService service, CancellationToken ct) =>
            service.ListAdvancePurchaseOrdersAsync(vendorId, ct))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.View);

        group.MapGet("/advances", (
            Guid? vendorId, Guid? purchaseOrderId, bool? outstandingOnly,
            int? page, int? pageSize, IVendorFinancialEvidenceService service,
            CancellationToken ct) =>
            service.ListAdvancesAsync(vendorId, purchaseOrderId, outstandingOnly,
                page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.View);

        group.MapPost("/advances", (
            RecordVendorAdvanceRequest request, IVendorFinancialEvidenceService service,
            HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordAdvanceAsync(request, ct), h, true))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.Approve);

        group.MapPost("/advances/{id:guid}/reverse", (
            Guid id, ReverseVendorAdvanceRequest request,
            IVendorFinancialEvidenceService service, HttpContext h,
            CancellationToken ct) =>
            Run(() => service.ReverseAdvanceAsync(id, request, ct), h, false))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.Cancel);

        group.MapGet("/payments", (
            Guid? vendorId, int? page, int? pageSize,
            IVendorFinancialEvidenceService service, CancellationToken ct) =>
            service.ListPaymentsAsync(vendorId, page ?? 1, pageSize ?? 50, ct))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.View);

        group.MapPost("/payments", (
            RecordVendorPaymentRequest request, IVendorFinancialEvidenceService service,
            HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordPaymentAsync(request, ct), h, true))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.Approve);

        group.MapGet("/payables", (
            Guid? vendorId, bool? overdueOnly,
            IVendorFinancialEvidenceService service, CancellationToken ct) =>
            service.ListPayablesAsync(vendorId, overdueOnly ?? false, ct))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.View);

        group.MapGet("/vendor-positions", (
            IVendorFinancialEvidenceService service, CancellationToken ct) =>
            service.ListVendorPositionsAsync(ct))
            .RequirePagePermission("accounts.vendor-financial-evidence",
                PagePermissionActions.View);

        return endpoints;
    }

    private static async Task<IResult> Run<T>(
        Func<Task<T>> action, HttpContext context, bool created)
    {
        try
        {
            var value = await action();
            return created ? Results.Created(string.Empty, value) : Results.Ok(value);
        }
        catch (StoresValidationException e)
        {
            return Results.BadRequest(new { message = e.Message });
        }
        catch (KeyNotFoundException e)
        {
            return Results.NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return context.User.Identity?.IsAuthenticated == true
                ? Results.Forbid() : Results.Unauthorized();
        }
        catch (StoresConflictException e)
        {
            return Results.Conflict(new { message = e.Message });
        }
        catch (DbUpdateConcurrencyException e)
        {
            return Results.Conflict(new { message = e.Message });
        }
    }
}