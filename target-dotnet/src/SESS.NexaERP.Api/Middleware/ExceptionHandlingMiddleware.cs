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
        catch (SESS.NexaERP.Api.Endpoints.EmployeeRoleOperationConflictException ex)
        {
            logger.LogWarning(ex, "Employee role operation conflict");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
