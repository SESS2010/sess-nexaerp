using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Identity;

public sealed class EfEmployeeIdentityResolver(NexaErpDbContext db) : IEmployeeIdentityResolver
{
    public async Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
            return ResolvedEmployeeIdentity.Failed("OIDC issuer and subject are required; alternate identity linking is prohibited.");

        var normalizedIssuer = EmployeeIdentityMapping.NormalizeIssuer(issuer);
        var normalizedSubject = EmployeeIdentityMapping.NormalizeSubject(subject);
        var mappings = await db.EmployeeIdentityMappings.AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.Issuer == normalizedIssuer && x.Subject == normalizedSubject && x.IsActive)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => string.IsNullOrWhiteSpace(organizationId) || x.OrganizationId == organizationId)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (mappings.Count != 1) return ResolvedEmployeeIdentity.Failed(mappings.Count == 0 ? "No active employee identity mapping exists." : "Identity mapping is ambiguous.");
        var mapping = mappings[0];
        var employee = mapping.Employee;
        if (employee is null || !employee.LoginEnabled || !string.Equals(employee.Status, MasterStatuses.Active, StringComparison.OrdinalIgnoreCase))
            return new(false, mapping.EmployeeId, employee?.DepartmentId, mapping.OrganizationId, null, [], "Mapped employee is inactive or login disabled.");

        var roles = await db.EmployeeRoleAssignments.AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.EmployeeId == employee.Id && x.CompanyId == mapping.CompanyId)
            .Where(x => x.ApprovalStatus == "SeedApproved" || x.ApprovalStatus == "Approved")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => x.Role != null && x.Role.IsActive)
            .Select(x => x.Role!.Code)
            .ToListAsync(cancellationToken);
        var effectiveRoleCodes = roles
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new(true, employee.Id, employee.DepartmentId, mapping.OrganizationId, employee.EmployeeCode, effectiveRoleCodes, "Employee identity resolved.");
    }
}
