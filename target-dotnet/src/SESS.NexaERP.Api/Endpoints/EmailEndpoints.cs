namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// OWNED BY THE TD (branch feature/email-lite). Claude created this empty hook on 27 Sep so that Program.cs
/// maps it once. Map /api/v1/email/* here, with RequirePagePermission("admin.email", …) (page created by
/// migration 20260927100000_EmailOutbox: View for TD and IT Manager, Update for TD).
/// </summary>
public static class EmailEndpoints
{
    public static IEndpointRouteBuilder MapEmailLiteEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
