using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Tracking;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>Tracking-lite (R1): pending lists, home tiles and one document's timeline. Access is decided in the database.</summary>
public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tracking").WithTags("Tracking").RequireAuthorization();
        group.MapGet("/pending", (string? docType, string? queue, bool? overdueOnly, bool? mine, int? page, int? pageSize,
                ITrackingService service, HttpContext context, CancellationToken ct) =>
            Run(context, async () => Results.Ok(await service.PendingAsync(
                new(docType, queue, overdueOnly ?? false, mine ?? false, page ?? 1, pageSize ?? 50), ct))));
        group.MapGet("/summary", (ITrackingService service, HttpContext context, CancellationToken ct) =>
            Run(context, async () => Results.Ok(await service.SummaryAsync(ct))));
        group.MapGet("/{docType}/{documentId:guid}/history", (string docType, Guid documentId, ITrackingService service,
                HttpContext context, CancellationToken ct) =>
            Run(context, async () => await service.HistoryAsync(docType, documentId, ct) is { } history
                ? Results.Ok(history)
                : Results.NotFound(new { code = "NOT_FOUND", message = "Document not found." })));
        return endpoints;
    }

    private static async Task<IResult> Run(HttpContext context, Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ReportAccessDeniedException)
        {
            context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] =
                new StandardErrorEnvelopeMiddleware.ReportFailure("TRACKING_ACCESS_DENIED", false);
            return Results.Json(new { code = "TRACKING_ACCESS_DENIED",
                message = "Pending documents are not permitted for your employee and selected company." }, statusCode: 403);
        }
        catch (ReportRequestException error)
        {
            context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] =
                new StandardErrorEnvelopeMiddleware.ReportFailure("TRACKING_REQUEST_INVALID", false);
            return Results.BadRequest(new { code = "TRACKING_REQUEST_INVALID", message = error.Message });
        }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }
}
