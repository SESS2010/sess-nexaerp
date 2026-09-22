using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class VendorRatingEvidenceEndpoints
{
    public static IEndpointRouteBuilder MapVendorRatingEvidenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/quality/vendor-rating-evidence/{goodsReceiptId:guid}",
            async (Guid goodsReceiptId, IVendorRatingEvidenceService service, HttpContext h, CancellationToken ct) =>
            {
                try
                {
                    var value = await service.GetAsync(goodsReceiptId, ct);
                    return value is null ? Results.NotFound() : Results.Ok(value);
                }
                catch (StoresConflictException error) { return Results.Conflict(new { message = error.Message }); }
                catch (DbUpdateConcurrencyException error) { return Results.Conflict(new { message = error.Message }); }
                catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
            }).WithTags("Quality - Vendor rating evidence").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope)
            .RequirePagePermission("qc.inspection-policies", PagePermissionActions.View);
        endpoints.MapGet("/api/v1/quality/vendor-rating-evidence/receipts",
            async (int? page, int? pageSize, IVendorRatingEvidenceService service, HttpContext h, CancellationToken ct) =>
            {
                try { return Results.Ok(await service.ListReceiptsAsync(page ?? 1, pageSize ?? 50, ct)); }
                catch (UnauthorizedAccessException) { return h.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized(); }
            }).WithTags("Quality - Vendor rating evidence").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope)
            .RequirePagePermission("qc.inspection-policies", PagePermissionActions.View);
        return endpoints;
    }
}
