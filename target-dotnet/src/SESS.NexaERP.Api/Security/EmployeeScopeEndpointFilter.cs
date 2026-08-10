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
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(user.OrganizationId)) return Results.Forbid();

        var authorizer = services.GetRequiredService<IRecordScopeAuthorizer>();
        var decision = await authorizer.AuthorizeAnyAsync(user.EmployeeId.Value, user.RoleCode, user.OrganizationId, DateOnly.FromDateTime(DateTime.UtcNow), context.HttpContext.RequestAborted);
        if (!decision.Allowed)
        {
            var audit = services.GetRequiredService<IAuditWriter>();
            await audit.WriteAsync("Security", "RecordScopeDenied", "Endpoint", context.HttpContext.Request.Path, null,
                new { decision.Reason, user.EmployeeId, user.RoleCode, user.OrganizationId, method = context.HttpContext.Request.Method }, context.HttpContext.RequestAborted);
            return Results.Forbid();
        }
        return await next(context);
    }
}
