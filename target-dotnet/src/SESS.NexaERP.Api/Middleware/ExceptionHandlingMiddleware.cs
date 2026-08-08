using System.Diagnostics;
using System.Net;

namespace SESS.NexaERP.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://sess-nexaerp.local/problems/unhandled-error",
                title = "Unexpected server error",
                status = context.Response.StatusCode,
                traceId = Activity.Current?.Id ?? context.TraceIdentifier
            });
        }
    }
}
