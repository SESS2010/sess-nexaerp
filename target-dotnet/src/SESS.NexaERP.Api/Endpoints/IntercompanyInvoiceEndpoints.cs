using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class IntercompanyInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapIntercompanyInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string page = "accounts.intercompany-invoices";
        var group = endpoints.MapGroup("/api/v1/accounts/intercompany-invoices")
            .WithTags("Accounts - Intercompany invoices").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/for-purchase/{correlationId:guid}", (Guid correlationId, IIntercompanyInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ListForPurchaseAsync(correlationId, ct), h))
            .RequirePagePermission(page, PagePermissionActions.View)
            .RequirePagePermission(page, PagePermissionActions.ViewCommercialValues);
        group.MapGet("/{id:guid}", (Guid id, IIntercompanyInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(async () => await service.GetAsync(id, ct) ?? throw new KeyNotFoundException("Intercompany invoice not found in this company."), h))
            .RequirePagePermission(page, PagePermissionActions.View)
            .RequirePagePermission(page, PagePermissionActions.ViewCommercialValues);
        group.MapPost("/", (RecordIntercompanyInvoiceRequest request, IIntercompanyInvoiceService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordAsync(request, ct), h))
            .RequirePagePermission(page, PagePermissionActions.Create)
            .RequirePagePermission(page, PagePermissionActions.UploadAttachment)
            .RequirePagePermission(page, PagePermissionActions.ViewCommercialValues);
        group.MapGet("/{id:guid}/evidence", async (Guid id, IIntercompanyInvoiceService service, HttpContext h, CancellationToken ct) =>
        {
            try
            {
                var file = await service.DownloadAsync(id, ct);
                return Results.File(file.Content, file.ContentType, file.FileName);
            }
            catch (KeyNotFoundException error) { return Results.NotFound(new { message = error.Message }); }
            catch (StoresConflictException error) { return Results.Conflict(new { message = error.Message }); }
            catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
        }).RequirePagePermission(page, PagePermissionActions.Download)
          .RequirePagePermission(page, PagePermissionActions.ViewCommercialValues);
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
