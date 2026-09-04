using Microsoft.EntityFrameworkCore;
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
        var mappings = await db.EmployeeIdentityMappings.AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.Issuer == normalizedIssuer && x.Subject == normalizedSubject && x.IsActive)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => x.OrganizationId == normalizedOrganization)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (mappings.Count != 1) return ResolvedEmployeeIdentity.Failed(mappings.Count == 0 ? "No active employee identity mapping exists." : "Identity mapping is ambiguous.");
        var mapping = mappings[0];
        var employee = mapping.Employee;
        if (employee is null || !employee.LoginEnabled || !string.Equals(employee.Status, MasterStatuses.Active, StringComparison.OrdinalIgnoreCase))
            return new(false, mapping.EmployeeId, employee?.DepartmentId, mapping.OrganizationId, null, [], "Mapped employee is inactive or login disabled.");

        var companyAssignments = await db.EmployeeCompanyAssignments.AsNoTracking()
            .Where(x => x.CompanyId == mapping.CompanyId && x.EmployeeId == employee.Id && x.IsActive && x.Status == "ACTIVE")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Take(2)
            .ToListAsync(cancellationToken);
        if (companyAssignments.Count != 1)
            return new(false, employee.Id, null, mapping.OrganizationId, employee.EmployeeCode, [], companyAssignments.Count == 0
                ? "Employee has no active assignment in the requested company."
                : "Employee company assignment is ambiguous.");

        var companyAssignment = companyAssignments[0];
        var primaryDepartments = await db.EmployeeDepartmentAssignments.AsNoTracking()
            .Where(x => x.CompanyId == mapping.CompanyId && x.EmployeeCompanyAssignmentId == companyAssignment.Id)
            .Where(x => x.IsActive && x.Status == "ACTIVE" && x.IsPrimary)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Select(x => x.DepartmentId)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (primaryDepartments.Count != 1)
            return new(false, employee.Id, null, mapping.OrganizationId, employee.EmployeeCode, [], "Employee must have exactly one active primary department in the requested company.");

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
        var primaryRoleCode = await ResolvePrimaryRoleCodeAsync(mapping.CompanyId, employee.Id, onDate, cancellationToken);
        return new(true, employee.Id, primaryDepartments[0], mapping.OrganizationId, employee.EmployeeCode, effectiveRoleCodes, "Employee identity resolved.", primaryRoleCode);
    }

    private async Task<string?> ResolvePrimaryRoleCodeAsync(Guid companyId, Guid employeeId, DateOnly onDate, CancellationToken cancellationToken)
    {
        var primaryRoles = await db.EmployeeCompanyRoleProfiles.AsNoTracking()
            .Where(profile => profile.CompanyId == companyId && profile.EmployeeId == employeeId &&
                profile.ConfigurationStatus == EmployeeRoleProfileStatuses.Configured)
            .Where(profile => profile.PrimaryRoleAssignment != null &&
                profile.PrimaryRoleAssignment.IsPrimary &&
                (profile.PrimaryRoleAssignment.ApprovalStatus == "SeedApproved" || profile.PrimaryRoleAssignment.ApprovalStatus == "Approved") &&
                profile.PrimaryRoleAssignment.EffectiveFrom <= onDate &&
                (!profile.PrimaryRoleAssignment.EffectiveTo.HasValue || profile.PrimaryRoleAssignment.EffectiveTo.Value >= onDate) &&
                profile.PrimaryRoleAssignment.Role != null && profile.PrimaryRoleAssignment.Role.IsActive)
            .Select(profile => profile.PrimaryRoleAssignment!.Role!.Code)
            .Take(2)
            .ToListAsync(cancellationToken);
        return primaryRoles.Count == 1 ? primaryRoles[0].Trim().ToUpperInvariant() : null;
    }

#if DEBUG
    public async Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode, string? organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(organizationId))
            return ResolvedEmployeeIdentity.Failed("Development employee code and organization are required.");

        var normalizedCode = employeeCode.Trim().ToUpperInvariant();
        var normalizedOrganization = organizationId.Trim().ToUpperInvariant();
        var company = await db.Companies.AsNoTracking()
            .Where(x => x.Code == normalizedOrganization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => new { x.Id, x.Code })
            .SingleOrDefaultAsync(cancellationToken);
        if (company is null) return ResolvedEmployeeIdentity.Failed("Development organization is not active.");

        var employees = await db.Employees.AsNoTracking()
            .Where(x => x.EmployeeCode == normalizedCode)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (employees.Count != 1)
            return ResolvedEmployeeIdentity.Failed(employees.Count == 0 ? "Development employee does not exist." : "Development employee code is ambiguous.");

        var employee = employees[0];
        var companyAssignments = await db.EmployeeCompanyAssignments.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id && x.IsActive && x.Status == "ACTIVE")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Take(2)
            .ToListAsync(cancellationToken);
        if (companyAssignments.Count != 1)
            return new(false, employee.Id, null, company.Code, employee.EmployeeCode, [], companyAssignments.Count == 0
                ? "Employee has no active assignment in the requested development company."
                : "Employee company assignment is ambiguous.");

        var companyAssignment = companyAssignments[0];
        var primaryDepartments = await db.EmployeeDepartmentAssignments.AsNoTracking()
            .Where(x => x.CompanyId == company.Id && x.EmployeeCompanyAssignmentId == companyAssignment.Id)
            .Where(x => x.IsActive && x.Status == "ACTIVE" && x.IsPrimary)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Select(x => x.DepartmentId)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (primaryDepartments.Count != 1)
            return new(false, employee.Id, null, company.Code, employee.EmployeeCode, [], "Employee must have exactly one active primary department in the requested development company.");

        var roles = await db.EmployeeRoleAssignments.AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.EmployeeId == employee.Id && x.CompanyId == company.Id)
            .Where(x => x.ApprovalStatus == "SeedApproved" || x.ApprovalStatus == "Approved")
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => x.Role != null && x.Role.IsActive)
            .Select(x => x.Role!.Code)
            .ToListAsync(cancellationToken);
        var effectiveRoleCodes = roles.Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var primaryRoleCode = await ResolvePrimaryRoleCodeAsync(company.Id, employee.Id, onDate, cancellationToken);
        return new(true, employee.Id, primaryDepartments[0], company.Code, employee.EmployeeCode, effectiveRoleCodes, "Development employee identity resolved.", primaryRoleCode);
    }
#endif
}
