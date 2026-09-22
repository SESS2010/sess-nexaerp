using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Api.Middleware;

namespace SESS.NexaERP.Api.Endpoints;

public static class PurchaseOpenOrdersEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOpenOrdersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/dashboards/purchase/open-orders",
            async (Guid? vendorId, string? currency, Guid? rootPurchaseOrderId, bool? overdueOnly,
                int? page, int? pageSize, IPurchaseOpenOrdersService service, HttpContext context, CancellationToken ct) =>
            {
                try
                {
                    return Results.Ok(await service.GetAsync(new(vendorId, currency,
                        rootPurchaseOrderId, overdueOnly ?? false, page ?? 1, pageSize ?? 100), ct));
                }
                catch (ReportAccessDeniedException)
                {
                    context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] =
                        new StandardErrorEnvelopeMiddleware.ReportFailure("DASHBOARD_ACCESS_DENIED", false);
                    return Results.Json(new { code = "DASHBOARD_ACCESS_DENIED",
                        message = "Open purchase orders are not permitted for your employee and selected company." }, statusCode: 403);
                }
                catch (ReportRequestException error)
                {
                    context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] =
                        new StandardErrorEnvelopeMiddleware.ReportFailure("DASHBOARD_REQUEST_INVALID", false);
                    return Results.BadRequest(new { code = "DASHBOARD_REQUEST_INVALID", message = error.Message });
                }
                catch (UnauthorizedAccessException) { return Results.Forbid(); }
            }).WithTags("Purchase dashboard").RequireAuthorization();
        return endpoints;
    }
}
