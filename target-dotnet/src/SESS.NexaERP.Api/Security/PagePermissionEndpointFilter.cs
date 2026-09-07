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
            var audit = httpContext.RequestServices.GetRequiredService<IAuditWriter>();
            if (!currentUser.IsAuthenticated || !currentUser.EmployeeId.HasValue)
            {
                await audit.WriteAsync("Security", "Denied", "Identity", pageKey, null,
                    new { reason = "Authenticated employee identity is unresolved", permission }, httpContext.RequestAborted);
                return Results.Unauthorized();
            }
            var permissions = httpContext.RequestServices.GetRequiredService<IPagePermissionService>();
            var qualifyingRoles = new List<string>();
            foreach (var assignment in currentUser.EffectiveRoleAssignments)
            {
                if (RoleAuthorityResolution.IsSupportDenied($"{pageKey}:{permission}") &&
                    string.Equals(assignment.AssignmentType, "SUPPORT", StringComparison.OrdinalIgnoreCase)) continue;
                if (await permissions.HasPermissionAsync([assignment.RoleCode], pageKey, permission, httpContext.RequestAborted))
                    qualifyingRoles.Add(assignment.RoleCode);
            }
            try
            {
                currentUser.RequireRole($"{pageKey}:{permission}", qualifyingRoles.Distinct(StringComparer.Ordinal).ToArray());
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException)
            {
                var required = qualifyingRoles.Count == 0 ? $"a role granted {pageKey}:{permission}" : string.Join(" or ", qualifyingRoles);
                await audit.WriteAsync("Security", "Denied", pageKey, permission, null, new
                {
                    reason = $"Required role: {required}.", currentUser.EmployeeId, pageKey, permission,
                    path = httpContext.Request.Path.Value, method = httpContext.Request.Method,
                    correlationId = httpContext.TraceIdentifier
                }, httpContext.RequestAborted);
                return Results.Forbid();
            }
            return await next(context);
        });
    }
}
