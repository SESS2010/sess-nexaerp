using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

public static class PagePermissionEndpointFilter
{
    public static RouteHandlerBuilder RequirePagePermission(this RouteHandlerBuilder builder, string pageKey, string permission)
    {
        return builder.AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope).AddEndpointFilter(async (context, next) =>
        {
            var httpContext = context.HttpContext;
            var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();
            if (!currentUser.IsAuthenticated || !currentUser.EmployeeId.HasValue) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(currentUser.RoleCode) || currentUser.RoleCode == "none") return Results.Forbid();

            var permissions = httpContext.RequestServices.GetRequiredService<IPagePermissionService>();
            var allowed = await permissions.HasPermissionAsync(currentUser.RoleCode, pageKey, permission, httpContext.RequestAborted);
            if (!allowed)
            {
                var audit = httpContext.RequestServices.GetRequiredService<IAuditWriter>();
                await audit.WriteAsync("Security", "Denied", pageKey, permission, null, new
                {
                    roleCode = currentUser.RoleCode,
                    currentUser.EmployeeId,
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