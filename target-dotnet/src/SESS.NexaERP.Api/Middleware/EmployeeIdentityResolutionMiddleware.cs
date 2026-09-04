using System.Security.Claims;
using SESS.NexaERP.Application.Identity;
#if DEBUG
using SESS.NexaERP.Api.Security;
#endif

namespace SESS.NexaERP.Api.Middleware;

public sealed class EmployeeIdentityResolutionMiddleware(RequestDelegate next)
{
    public const string ResolutionItemKey = "SESS.Rev869A.ResolvedEmployeeIdentity";

    public async Task InvokeAsync(HttpContext context, IEmployeeIdentityResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var issuer = context.User.FindFirstValue("iss");
            var subject = context.User.FindFirstValue("sub");
            var organization = context.User.FindFirstValue("organization_id") ?? context.User.FindFirstValue("org_id");
#if DEBUG
            var developmentEmployeeCode = context.User.FindFirstValue(DevelopmentTokenService.ImpersonatedEmployeeCodeClaim);
            var resolution = !string.IsNullOrWhiteSpace(developmentEmployeeCode)
                ? await resolver.ResolveDevelopmentEmployeeAsync(developmentEmployeeCode, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted)
                : string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)
                    ? ResolvedEmployeeIdentity.Failed("Exact OIDC issuer and subject are required.")
                    : await resolver.ResolveAsync(issuer, subject, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted);
#else
            var resolution = string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)
                ? ResolvedEmployeeIdentity.Failed("Exact OIDC issuer and subject are required.")
                : await resolver.ResolveAsync(issuer, subject, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted);
#endif
            if (resolution.Success)
            {
                var requestedRole = context.Request.Headers["X-SESS-Acting-Role"].FirstOrDefault()?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(requestedRole) &&
                    !resolution.RoleCodes.Contains(requestedRole, StringComparer.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "The requested acting role is not currently held by this employee in this company.",
                        requestedRole
                    }, context.RequestAborted);
                    return;
                }

                resolution = resolution with { ActingRoleCode = requestedRole ?? resolution.PrimaryRoleCode };
            }
            context.Items[ResolutionItemKey] = resolution;
        }

        await next(context);
    }
}
