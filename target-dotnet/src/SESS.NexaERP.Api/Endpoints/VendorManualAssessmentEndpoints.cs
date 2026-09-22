using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class VendorManualAssessmentEndpoints
{
    public static IEndpointRouteBuilder MapVendorManualAssessmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string pageKey = "quality.vendor-manual-assessments";
        var group = endpoints.MapGroup("/api/v1/quality/vendor-manual-assessments")
            .WithTags("Quality - Vendor manual assessments").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/receipts", (int? page, int? pageSize, IVendorManualAssessmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.ListReceiptsAsync(page ?? 1, pageSize ?? 50, ct), h))
            .RequirePagePermission(pageKey, PagePermissionActions.View);
        group.MapGet("/for-receipt/{goodsReceiptId:guid}", (Guid goodsReceiptId, IVendorManualAssessmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.HistoryAsync(goodsReceiptId, ct), h)).RequirePagePermission(pageKey, PagePermissionActions.View);
        group.MapPost("/", (RecordVendorManualAssessmentRequest request, IVendorManualAssessmentService service, HttpContext h, CancellationToken ct) =>
            Run(() => service.RecordAsync(request, ct), h)).RequirePagePermission(pageKey, PagePermissionActions.Create);
        return endpoints;
    }
    private static async Task<IResult> Run<T>(Func<Task<T>> action, HttpContext h)
    {
        try { return Results.Ok(await action()); }
        catch (StoresValidationException error) { return Results.BadRequest(new { message = error.Message }); }
        catch (StoresConflictException error) { return Results.Conflict(new { message = error.Message }); }
        catch (DbUpdateConcurrencyException error) { return Results.Conflict(new { message = error.Message }); }
        catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
    }
}
