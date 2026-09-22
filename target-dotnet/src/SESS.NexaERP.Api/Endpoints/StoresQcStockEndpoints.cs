using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Api.Middleware;

namespace SESS.NexaERP.Api.Endpoints;

public static class StoresQcStockEndpoints
{
    public static IEndpointRouteBuilder MapStoresQcStockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/dashboards/stores/qc-stock",
            async(string? queue,Guid? documentId,int? page,int? pageSize,IStoresQcStockService service,
                HttpContext context,CancellationToken ct)=>
            {
                try{return Results.Ok(await service.GetAsync(new(queue,documentId,page??1,pageSize??100),ct));}
                catch(ReportAccessDeniedException)
                {
                    context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey]=
                        new StandardErrorEnvelopeMiddleware.ReportFailure("DASHBOARD_ACCESS_DENIED",false);
                    return Results.Json(new{code="DASHBOARD_ACCESS_DENIED",
                        message="Stores QC stock is not permitted for your employee and selected company."},statusCode:403);
                }
                catch(ReportRequestException error)
                {
                    context.Items[StandardErrorEnvelopeMiddleware.ReportFailureKey]=
                        new StandardErrorEnvelopeMiddleware.ReportFailure("DASHBOARD_REQUEST_INVALID",false);
                    return Results.BadRequest(new{code="DASHBOARD_REQUEST_INVALID",message=error.Message});
                }
                catch(UnauthorizedAccessException){return Results.Forbid();}
            }).WithTags("Stores dashboard").RequireAuthorization();
        return endpoints;
    }
}
