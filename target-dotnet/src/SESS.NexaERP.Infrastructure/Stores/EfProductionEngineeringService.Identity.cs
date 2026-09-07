using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    private async Task<Company> CompanyAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("Company scope is required.");
        var code = user.OrganizationId.Trim().ToUpperInvariant();
        return await db.Companies.SingleAsync(x => x.Code == code && x.IsActive && x.Status == "ACTIVE", ct);
    }
    private Guid Actor()
    {
        if (!user.EmployeeId.HasValue)
            throw new UnauthorizedAccessException("Resolved employee identity is required.");
        return user.EmployeeId.Value;
    }
    private static string Code(string value) => !string.IsNullOrWhiteSpace(value)
        ? value.Trim().ToUpperInvariant() : throw new StoresValidationException("Code is required.");
    private static string Required(string value, string field) => !string.IsNullOrWhiteSpace(value)
        ? value.Trim() : throw new StoresValidationException(field + " is required.");
    private async Task RequireDesignPreparerAsync(CancellationToken ct)
    {
        var employeeCode = await db.Employees.AsNoTracking().Where(x => x.Id == Actor())
            .Select(x => x.EmployeeCode).SingleAsync(ct);
        var roles = employeeCode is "SESS-04" or "SESS-05"
            ? new[] { "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR",
                "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER" }
            : new[] { "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR" };
        _ = user.RequireRole("create", roles);
    }
}
