#if DEBUG
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// Development-only login endpoints. Compiled exclusively into Debug builds and
/// mapped only when development authentication is active. Tokens carry the
/// exact issuer/subject/organization of a real employee identity mapping so the
/// normal resolution middleware, permission checks and audit trail run unchanged.
/// </summary>
public static class DevelopmentAuthEndpoints
{
    public sealed record DevelopmentTokenRequest(string? EmployeeCode, string? LoginId, string? Password, string? OrganizationId);

    public static IEndpointRouteBuilder MapDevelopmentAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/dev")
            .WithTags("Development")
            .AllowAnonymous();

        group.MapGet("/identities", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var identities = await db.EmployeeIdentityMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive && mapping.EffectiveFrom <= today && (!mapping.EffectiveTo.HasValue || mapping.EffectiveTo >= today))
                .Where(mapping => mapping.Employee != null && mapping.Employee.LoginEnabled)
                .Select(mapping => new
                {
                    employeeCode = mapping.Employee!.EmployeeCode,
                    employeeName = mapping.Employee.EmployeeName,
                    organizationId = mapping.OrganizationId,
                })
                .Distinct()
                .OrderBy(identity => identity.employeeCode)
                .ThenBy(identity => identity.organizationId)
                .ToListAsync(cancellationToken);
            return Results.Ok(identities);
        });

        group.MapPost("/token", async (DevelopmentTokenRequest request, NexaErpDbContext db, DevelopmentTokenService tokens, IConfiguration configuration, CancellationToken cancellationToken) =>
        {
            var login = (request.LoginId ?? request.EmployeeCode)?.Trim();
            if (string.IsNullOrWhiteSpace(login))
            {
                return Results.BadRequest(new { message = "Login ID (employee ID or official email) is required." });
            }

            // Development password gate: when NexaErp:DevelopmentAuthenticationPassword
            // is configured, sign-in requires it. This is NOT production authentication
            // (REV866: production is OIDC-only); it only makes the development login
            // realistic. Compiled out of Release builds with the rest of this file.
            var requiredPassword = configuration["NexaErp:DevelopmentAuthenticationPassword"];
            if (!string.IsNullOrEmpty(requiredPassword) && !string.Equals(request.Password, requiredPassword, StringComparison.Ordinal))
            {
                return Results.BadRequest(new { message = "Invalid login ID or password." });
            }

            var code = login.ToUpperInvariant();
            var email = login.ToLowerInvariant();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = db.EmployeeIdentityMappings
                .AsNoTracking()
                .Where(mapping => mapping.IsActive && mapping.EffectiveFrom <= today && (!mapping.EffectiveTo.HasValue || mapping.EffectiveTo >= today))
                .Where(mapping => mapping.Employee != null
                    && (mapping.Employee.EmployeeCode == code
                        || (mapping.Employee.OfficialEmail != null && mapping.Employee.OfficialEmail.ToLower() == email)));

            if (!string.IsNullOrWhiteSpace(request.OrganizationId))
            {
                var organization = request.OrganizationId.Trim().ToUpperInvariant();
                query = query.Where(mapping => mapping.OrganizationId == organization);
            }

            var mapping = await query
                .OrderBy(mapping => mapping.OrganizationId)
                .Select(mapping => new
                {
                    mapping.Issuer,
                    mapping.Subject,
                    mapping.OrganizationId,
                    mapping.Employee!.EmployeeCode,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (mapping is null)
            {
                return Results.NotFound(new { message = $"No active identity mapping exists for login '{login}'." });
            }

            var token = tokens.IssueToken(mapping.Issuer, mapping.Subject, mapping.OrganizationId, TimeSpan.FromHours(12));
            return Results.Ok(new
            {
                token,
                employeeCode = mapping.EmployeeCode,
                organizationId = mapping.OrganizationId,
                expiresInHours = 12,
            });
        });

        return endpoints;
    }
}
#endif
