using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

public static class EmployeeScopeEndpointFilter
{
    public static async ValueTask<object?> RequireResolvedEmployeeAndScope(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var user = services.GetRequiredService<ICurrentUser>();
        var audit = services.GetRequiredService<IAuditWriter>();
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue)
        {
            await audit.WriteAsync("Security", "Denied", "Identity", context.HttpContext.Request.Path, null, new { reason = "Authenticated employee identity is unresolved", method = context.HttpContext.Request.Method }, context.HttpContext.RequestAborted);
            return Results.Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(user.OrganizationId))
        {
            await audit.WriteAsync("Security", "Denied", "OrganizationScope", context.HttpContext.Request.Path, null, new { reason = "Organization scope is unresolved", user.EmployeeId, user.RoleCode }, context.HttpContext.RequestAborted);
            return Results.Forbid();
        }

        var authorizer = services.GetRequiredService<IRecordScopeAuthorizer>();
        var decision = await authorizer.AuthorizeAnyAsync(user.EmployeeId.Value, user.RoleCode, user.OrganizationId, DateOnly.FromDateTime(DateTime.UtcNow), context.HttpContext.RequestAborted);
        if (!decision.Allowed)
        {
            await audit.WriteAsync("Security", "RecordScopeDenied", "Endpoint", context.HttpContext.Request.Path, null,
                new { decision.Reason, user.EmployeeId, user.RoleCode, user.OrganizationId, method = context.HttpContext.Request.Method }, context.HttpContext.RequestAborted);
            return Results.Forbid();
        }
        return await next(context);
    }
}
