#if DEBUG
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// Development-only login endpoints. Compiled exclusively into Debug builds and
/// mapped only when development authentication is active. Tokens identify an
/// existing employee and active company assignment; the normal permission and
/// audit paths then run under that resolved employee identity.
/// </summary>
public static class DevelopmentAuthEndpoints
{
    public sealed record DevelopmentTokenRequest(string? EmployeeCode, string? LoginId, string? OrganizationId);

    public static IEndpointRouteBuilder MapDevelopmentAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/dev")
            .WithTags("Development")
            .AllowAnonymous();

        group.MapGet("/identities", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var identities = await (from assignment in db.EmployeeCompanyAssignments.AsNoTracking()
                join employee in db.Employees.AsNoTracking() on assignment.EmployeeId equals employee.Id
                join company in db.Companies.AsNoTracking() on assignment.CompanyId equals company.Id
                where assignment.IsActive && assignment.Status == "ACTIVE"
                    && assignment.EffectiveFrom <= today && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo >= today)
                    && company.IsActive && company.Status == "ACTIVE"
                select new
                {
                    employeeCode = employee.EmployeeCode,
                    employeeName = employee.EmployeeName,
                    organizationId = company.Code,
                })
                .AsNoTracking()
                .Distinct()
                .OrderBy(identity => identity.employeeCode)
                .ThenBy(identity => identity.organizationId)
                .ToListAsync(cancellationToken);
            return Results.Ok(identities);
        });

        group.MapPost("/token", async (DevelopmentTokenRequest request, NexaErpDbContext db, DevelopmentTokenService tokens, CancellationToken cancellationToken) =>
        {
            var requestedEmployeeCode = (request.EmployeeCode ?? request.LoginId)?.Trim();
            if (string.IsNullOrWhiteSpace(requestedEmployeeCode))
            {
                return Results.BadRequest(new { message = "EmployeeCode is required." });
            }

            var code = requestedEmployeeCode.ToUpperInvariant();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = from assignment in db.EmployeeCompanyAssignments.AsNoTracking()
                join employee in db.Employees.AsNoTracking() on assignment.EmployeeId equals employee.Id
                join company in db.Companies.AsNoTracking() on assignment.CompanyId equals company.Id
                where employee.EmployeeCode == code
                    && assignment.IsActive && assignment.Status == "ACTIVE"
                    && assignment.EffectiveFrom <= today && (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo >= today)
                    && company.IsActive && company.Status == "ACTIVE"
                select new { Employee = employee, Company = company };

            if (!string.IsNullOrWhiteSpace(request.OrganizationId))
            {
                var organization = request.OrganizationId.Trim().ToUpperInvariant();
                query = query.Where(x => x.Company.Code == organization);
            }

            var mapping = await query
                .OrderBy(x => x.Company.Code)
                .Select(mapping => new
                {
                    EmployeeId = mapping.Employee.Id,
                    mapping.Employee.EmployeeCode,
                    CompanyId = mapping.Company.Id,
                    OrganizationId = mapping.Company.Code,
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (mapping is null)
            {
                return Results.NotFound(new { message = $"No active company assignment exists for employee '{code}'." });
            }

            var token = tokens.IssueToken("urn:nexaerp:development", mapping.EmployeeCode, mapping.OrganizationId, mapping.EmployeeCode, TimeSpan.FromHours(12));
            db.AuditLogs.Add(new AuditLog
            {
                CompanyId = mapping.CompanyId,
                Scope = "COMPANY",
                Module = "Authentication",
                Action = "DevelopmentEmployeeImpersonation",
                EntityName = "Employee",
                EntityId = mapping.EmployeeId.ToString(),
                UserLoginId = "DEVELOPMENT_AUTHENTICATION",
                ActorRoleCode = "DEVELOPMENT_AUTHENTICATION",
                Result = "Success",
                CorrelationId = Guid.NewGuid().ToString("N"),
                AfterJson = JsonSerializer.Serialize(new { requestedEmployeeCode = code, mapping.OrganizationId, expiresInHours = 12 }),
                CreatedBy = "DEVELOPMENT_AUTHENTICATION",
            });
            await db.SaveChangesAsync(cancellationToken);
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
