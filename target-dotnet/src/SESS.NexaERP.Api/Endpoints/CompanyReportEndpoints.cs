using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Api.Middleware;

namespace SESS.NexaERP.Api.Endpoints;

public static class CompanyReportEndpoints
{
    public static IEndpointRouteBuilder MapCompanyReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reports").WithTags("Company reports").RequireAuthorization();
        group.MapGet("/", (ICompanyReportService service, HttpContext context, CancellationToken ct) => Run(context,async () => Results.Ok(await service.ListAsync(ct))));
        group.MapGet("/{key}", (string key, DateOnly? fromDate, DateOnly? toDate, string? mode,
            string? selection, string? metric, int? page, int? pageSize, ICompanyReportService service, HttpContext context, CancellationToken ct) =>
            Run(context,async () => Results.Ok(await service.GetAsync(key,
                new(fromDate,toDate,mode ?? "summary",selection,metric,page ?? 1,pageSize ?? 100),ct))));
        group.MapGet("/{key}/excel", (string key, DateOnly? fromDate, DateOnly? toDate,
            string? selection, ICompanyReportService service, HttpContext context, CancellationToken ct) => Run(context,async () =>
            {
                var file = await service.ExportAsync(key,new(fromDate,toDate,Group:selection),ct);
                return Results.File(file.Content,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",file.FileName);
            }));
        return endpoints;
    }

    private static async Task<IResult> Run(HttpContext context, Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (ReportAccessDeniedException e) { context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] = new StandardErrorEnvelopeMiddleware.ReportFailure("REPORT_ACCESS_DENIED",false); return Results.Json(new { code = "REPORT_ACCESS_DENIED", message = e.Message },statusCode:403); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (ReportSourceUnavailableException e) { context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] = new StandardErrorEnvelopeMiddleware.ReportFailure(e.Code,true); return Results.Conflict(new { code = e.Code, message = e.Message, administratorActionRequired = true }); }
        catch (ReportRequestException e) { context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey] = new StandardErrorEnvelopeMiddleware.ReportFailure("REPORT_REQUEST_INVALID",false); return Results.BadRequest(new { code = "REPORT_REQUEST_INVALID", message = e.Message }); }
        catch (KeyNotFoundException e) { return Results.NotFound(new { message = e.Message }); }
    }
}
