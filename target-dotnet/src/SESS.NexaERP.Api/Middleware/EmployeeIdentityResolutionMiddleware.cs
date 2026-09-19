using System.Security.Claims;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Api.Security;

namespace SESS.NexaERP.Api.Middleware;

public sealed class EmployeeIdentityResolutionMiddleware(RequestDelegate next)
{
    public const string ResolutionItemKey = "SESS.Rev869A.ResolvedEmployeeIdentity";

    public async Task InvokeAsync(HttpContext context, IEmployeeIdentityResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var validated = context.Items[OidcAccessTokenConfiguration.ValidatedIdentityKey] as ValidatedOidcIdentity;
            var issuer = validated?.Issuer ?? context.User.FindFirstValue("iss");
            var subject = validated?.Subject ?? context.User.FindFirstValue("sub");
            var organization = validated is not null ? validated.Organization : context.User.FindFirstValue("organization_id") ?? context.User.FindFirstValue("org_id");
#if DEBUG
            var developmentEmployeeCode = context.User.FindFirstValue(DevelopmentTokenService.ImpersonatedEmployeeCodeClaim);
            var resolution = context.RequestServices.GetService<DevelopmentTokenService>() is not null && !string.IsNullOrWhiteSpace(developmentEmployeeCode)
                ? await resolver.ResolveDevelopmentEmployeeAsync(developmentEmployeeCode, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted)
                : string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)
                    ? ResolvedEmployeeIdentity.Failed("Exact OIDC issuer and subject are required.")
                    : await resolver.ResolveAsync(issuer, subject, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted);
#else
            var resolution = string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)
                ? ResolvedEmployeeIdentity.Failed("Exact OIDC issuer and subject are required.")
                : await resolver.ResolveAsync(issuer, subject, organization, DateOnly.FromDateTime(DateTime.UtcNow), context.RequestAborted);
#endif
            if (resolution.Success &&
                resolution.RoleCodes.Any(role => role is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "ACCOUNTS_MANAGER" or "CHIEF_FINANCIAL_OFFICER") &&
                validated?.MfaAssured != true
#if DEBUG
                && context.RequestServices.GetService<DevelopmentTokenService>() is null
#endif
                )
            {
                context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey] = "MFA_REQUIRED";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your ERP role requires MFA. Sign in through the mandatory-MFA provider. Contact your administrator if your account has not been enrolled."
                }, context.RequestAborted);
                return;
            }
            context.Items[ResolutionItemKey] = resolution;
            if (!resolution.Success)
            {
                context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey] = "EMPLOYEE_ACCESS_NOT_CONFIGURED";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your sign-in is valid, but ERP access is not configured or is inactive for this company. Contact your administrator to check your employee identity mapping, company assignment and login status."
                }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}