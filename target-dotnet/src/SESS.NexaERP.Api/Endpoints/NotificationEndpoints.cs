using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications")
            .WithTags("Notifications").RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/", (bool? unreadOnly, int? page, int? pageSize,
            IInAppNotificationService service, CancellationToken ct) =>
            service.ListAsync(unreadOnly ?? false, page ?? 1, pageSize ?? 50, ct));
        group.MapGet("/unread-count", async (IInAppNotificationService service, CancellationToken ct) =>
            Results.Ok(new { count = await service.UnreadCountAsync(ct) }));
        group.MapPost("/{recipientId:guid}/read", async (Guid recipientId,
            IInAppNotificationService service, HttpContext context, CancellationToken ct) =>
        {
            await service.MarkReadAsync(recipientId, context.TraceIdentifier, ct);
            return Results.NoContent();
        });
        return endpoints;
    }
}
