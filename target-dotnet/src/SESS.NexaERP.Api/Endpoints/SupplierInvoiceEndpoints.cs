using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class SupplierInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapSupplierInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string pageKey = "accounts.supplier-invoices";
        var group = endpoints.MapGroup("/api/v1/accounts/supplier-invoices")
            .WithTags("Accounts - Supplier Invoice Intake").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/purchase-order-options", (string? search, int? page, int? pageSize,
            ISupplierInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ListPurchaseOrdersAsync(search, page ?? 1, pageSize ?? 50, ct), h))
            .RequirePagePermission(pageKey, PagePermissionActions.View)
            .RequirePagePermission(pageKey, PagePermissionActions.ViewCommercialValues);
        group.MapPost("/", (RecordSupplierInvoiceRequest request, ISupplierInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordAsync(request, ct), h))
            .RequirePagePermission(pageKey, PagePermissionActions.Create)
            .RequirePagePermission(pageKey, PagePermissionActions.UploadAttachment);
        group.MapGet("/{id:guid}", (Guid id, ISupplierInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(async () => await service.GetAsync(id, ct) ?? throw new KeyNotFoundException("Supplier invoice not found in this company."), h))
            .RequirePagePermission(pageKey, PagePermissionActions.View)
            .RequirePagePermission(pageKey, PagePermissionActions.ViewCommercialValues);
        group.MapGet("/{id:guid}/evidence", async (Guid id, ISupplierInvoiceService service, HttpContext h, CancellationToken ct) =>
        {
            try
            {
                var file = await service.DownloadAsync(id, ct);
                return Results.File(file.Content, file.ContentType, file.FileName);
            }
            catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
            catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
            catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
        }).RequirePagePermission(pageKey, PagePermissionActions.Download)
          .RequirePagePermission(pageKey, PagePermissionActions.ViewCommercialValues);
        group.MapPost("/{id:guid}/cancel", (Guid id, CancelSupplierInvoiceRequest request,
            ISupplierInvoiceService service, HttpContext h, CancellationToken ct) => Run(() => service.CancelAsync(id, request, ct), h))
            .RequirePagePermission(pageKey, PagePermissionActions.Cancel);
        group.MapPost("/{id:guid}/link-accepted-bill", (Guid id, LinkSupplierInvoiceAcceptedBillRequest request,
            ISupplierInvoiceService service, HttpContext h, CancellationToken ct) => Run(() => service.LinkAcceptedBillAsync(id, request, ct), h))
            .RequirePagePermission(pageKey, PagePermissionActions.Approve);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h)
    {
        try { return Results.Ok(await action()); }
        catch (StoresValidationException e) { return Results.BadRequest(new { message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
        catch (StoresConflictException e) { return Results.Conflict(new { message = e.Message }); }
        catch (DbUpdateConcurrencyException e) { return Results.Conflict(new { message = e.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
    }
}
