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
            context.Items[ResolutionItemKey] = resolution;
        }

        await next(context);
    }
}