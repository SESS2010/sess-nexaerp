using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Api.Middleware;

namespace SESS.NexaERP.Api.Endpoints;

public static class PurchaseSpendingEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseSpendingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/dashboards/purchase/spending",
            async (string? period, DateOnly? month, Guid? vendorId, Guid? categoryId, string? currency,
                Guid? billId, int? page, int? pageSize, IPurchaseSpendingService service,
                HttpContext context, CancellationToken ct) =>
            {
                try
                {
                    return Results.Ok(await service.GetAsync(new(period ?? "financial-year", month,
                        vendorId, categoryId, currency, billId, page ?? 1, pageSize ?? 100), ct));
                }
                catch (ReportAccessDeniedException)
                {
                    context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] =
                        new StandardErrorEnvelopeMiddleware.ReportFailure("DASHBOARD_ACCESS_DENIED", false);
                    return Results.Json(new { code = "DASHBOARD_ACCESS_DENIED",
                        message = "Purchase spending is not permitted for your employee and selected company." }, statusCode: 403);
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
