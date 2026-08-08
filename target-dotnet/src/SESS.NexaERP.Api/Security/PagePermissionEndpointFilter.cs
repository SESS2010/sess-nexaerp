using System.Security.Claims;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;

namespace SESS.NexaERP.Api.Security;

public static class PagePermissionEndpointFilter
{
    public static RouteHandlerBuilder RequirePagePermission(this RouteHandlerBuilder builder, string pageKey, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var roleCode = httpContext.User.Claims
                .Where(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
                .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(roleCode))
            {
                return Results.Forbid();
            }

            var permissions = httpContext.RequestServices.GetRequiredService<IPagePermissionService>();
            var allowed = await permissions.HasPermissionAsync(roleCode, pageKey, permission, httpContext.RequestAborted);
            if (!allowed)
            {
                var audit = httpContext.RequestServices.GetRequiredService<IAuditWriter>();
                await audit.WriteAsync("Security", "Denied", pageKey, permission, null, new
                {
                    roleCode,
                    pageKey,
                    permission,
                    path = httpContext.Request.Path.Value,
                    method = httpContext.Request.Method,
                    correlationId = httpContext.TraceIdentifier
                }, httpContext.RequestAborted);
                return Results.Forbid();
            }

            return await next(context);
        });
    }
}
