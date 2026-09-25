using System.Net;
using System.Text.Json;

namespace SESS.NexaERP.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status400BadRequest)
        {
            var jsonException = FindJsonException(ex);
            var field = NormalizeJsonPath(jsonException?.Path);
            var detail = $"The value supplied for {field} is invalid.";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                message = detail,
                errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [detail] }
            });
        }
        catch (SESS.NexaERP.Api.Endpoints.EmployeeRoleOperationConflictException ex)
        {
            logger.LogWarning(ex, "Employee role operation conflict");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (SESS.NexaERP.Application.Stores.StoresValidationException ex)
        {
            logger.LogWarning(ex, "Stores request validation failed");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            if (ex.Errors is { Count: > 0 } errors)
                await context.Response.WriteAsJsonAsync(new { message = ex.Message, errors });
            else
                await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = context.User.Identity?.IsAuthenticated == true
                ? (int)HttpStatusCode.Forbidden : (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (SESS.NexaERP.Application.Stores.StoresConflictException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            context.Items[StandardErrorEnvelopeMiddleware.ConcurrencyFailureKey] = true;
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }

    private static JsonException? FindJsonException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is JsonException jsonException) return jsonException;
        return null;
    }

    private static string NormalizeJsonPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Request";
        var normalized = path.Trim();
        if (normalized.StartsWith("$.", StringComparison.Ordinal)) normalized = normalized[2..];
        else if (normalized.StartsWith('$')) normalized = normalized[1..].TrimStart('.');
        return string.IsNullOrWhiteSpace(normalized) ? "Request" : normalized;
    }
}
