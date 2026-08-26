using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/session/me", async (ISessionService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetCurrentAsync(ct)); }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
        }).WithTags("Session").RequireAuthorization();
        return endpoints;
    }
}
