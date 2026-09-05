using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Identity;

public sealed class EfEmployeeIdentityResolver(NexaErpDbContext db) : IEmployeeIdentityResolver
{
    public async Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(organizationId))
            return ResolvedEmployeeIdentity.Failed("OIDC issuer, subject and organization are required; alternate identity linking is prohibited.");
        var normalizedIssuer = EmployeeIdentityMapping.NormalizeIssuer(issuer);
        var normalizedSubject = EmployeeIdentityMapping.NormalizeSubject(subject);
        var normalizedOrganization = organizationId.Trim().ToUpperInvariant();
        var mappings = await db.EmployeeIdentityMappings.AsNoTracking().Include(x => x.Employee)
            .Where(x => x.Issuer == normalizedIssuer && x.Subject == normalizedSubject && x.IsActive)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => x.OrganizationId == normalizedOrganization).Take(2).ToListAsync(cancellationToken);
        if (mappings.Count != 1) return ResolvedEmployeeIdentity.Failed(mappings.Count == 0 ? "No active employee identity mapping exists." : "Identity mapping is ambiguous.");
        return await ResolveMappedEmployeeAsync(mappings[0].Employee, mappings[0].EmployeeId, mappings[0].CompanyId,
            mappings[0].OrganizationId, onDate, false, cancellationToken);
    }

    private async Task<ResolvedEmployeeIdentity> ResolveMappedEmployeeAsync(Employee? employee, Guid employeeId, Guid companyId,
        string organization, DateOnly onDate, bool development, CancellationToken ct)
    {
        if (employee is null || !employee.LoginEnabled || !string.Equals(employee.Status, MasterStatuses.Active, StringComparison.OrdinalIgnoreCase))
            return new(false, employeeId, employee?.DepartmentId, organization, null, [], "Mapped employee is inactive or login disabled.");
        var companyAssignments = await db.EmployeeCompanyAssignments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employee.Id && x.IsActive && x.Status == "ACTIVE")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate)).Take(2).ToListAsync(ct);
        if (companyAssignments.Count != 1)
            return new(false, employee.Id, null, organization, employee.EmployeeCode, [], companyAssignments.Count == 0
                ? $"Employee has no active assignment in the requested {(development ? "development " : string.Empty)}company."
                : "Employee company assignment is ambiguous.");
        var companyAssignment = companyAssignments[0];
        var primaryDepartments = await db.EmployeeDepartmentAssignments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.EmployeeCompanyAssignmentId == companyAssignment.Id)
            .Where(x => x.IsActive && x.Status == "ACTIVE" && x.IsPrimary)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Select(x => x.DepartmentId).Take(2).ToListAsync(ct);
        if (primaryDepartments.Count != 1)
            return new(false, employee.Id, null, organization, employee.EmployeeCode, [], "Employee must have exactly one active primary department in the requested company.");

        var roles = await db.EmployeeRoleAssignments.AsNoTracking()
            .Where(x => x.EmployeeId == employee.Id && x.CompanyId == companyId)
            .Where(x => x.ApprovalStatus == "SeedApproved" || x.ApprovalStatus == "Approved")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => x.Role != null && x.Role.IsActive)
            .Where(x => db.CompanyRoleActivations.Any(ca => ca.CompanyId == companyId && ca.RoleId == x.RoleId &&
                ca.IsEnabled && ca.EffectiveFrom <= onDate && (!ca.EffectiveTo.HasValue || ca.EffectiveTo.Value >= onDate)))
            .Select(x => new { x.Id, Code = x.Role!.Code, x.AssignmentType }).ToListAsync(ct);
        var assignments = roles.Select(x => new EffectiveRoleAssignment(x.Id, x.Code.Trim().ToUpperInvariant(),
            x.AssignmentType.Trim().ToUpperInvariant())).OrderBy(x => x.RoleCode, StringComparer.Ordinal).ThenBy(x => x.AssignmentId).ToArray();
        var roleCodes = assignments.Select(x => x.RoleCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var full = assignments.Where(x => x.AssignmentType is EmployeeRoleAssignmentTypes.Full or EmployeeRoleAssignmentTypes.Temporary)
            .Select(x => x.RoleCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new(true, employee.Id, primaryDepartments[0], organization, employee.EmployeeCode, roleCodes,
            development ? "Development employee identity resolved." : "Employee identity resolved.", full, assignments);
    }

#if DEBUG
    public async Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode, string? organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(organizationId))
            return ResolvedEmployeeIdentity.Failed("Development employee code and organization are required.");
        var normalizedCode = employeeCode.Trim().ToUpperInvariant();
        var normalizedOrganization = organizationId.Trim().ToUpperInvariant();
        var company = await db.Companies.AsNoTracking().Where(x => x.Code == normalizedOrganization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => new { x.Id, x.Code }).SingleOrDefaultAsync(cancellationToken);
        if (company is null) return ResolvedEmployeeIdentity.Failed("Development organization is not active.");
        var employees = await db.Employees.AsNoTracking().Where(x => x.EmployeeCode == normalizedCode).Take(2).ToListAsync(cancellationToken);
        if (employees.Count != 1)
            return ResolvedEmployeeIdentity.Failed(employees.Count == 0 ? "Development employee does not exist." : "Development employee code is ambiguous.");
        return await ResolveMappedEmployeeAsync(employees[0], employees[0].Id, company.Id, company.Code, onDate, true, cancellationToken);
    }
#endif
}
