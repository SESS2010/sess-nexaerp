using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Identity;

public sealed class EfSessionService(NexaErpDbContext db, ICurrentUser currentUser) : ISessionService
{
    public async Task<SessionMe> GetCurrentAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.EmployeeId.HasValue || !currentUser.DepartmentId.HasValue ||
            string.IsNullOrWhiteSpace(currentUser.OrganizationId) || string.IsNullOrWhiteSpace(currentUser.IdentityIssuer) ||
            string.IsNullOrWhiteSpace(currentUser.IdentitySubject))
            throw new UnauthorizedAccessException("A resolved employee OIDC session is required.");

        var organization = currentUser.OrganizationId.Trim().ToUpperInvariant();
        var company = await db.Companies.AsNoTracking().SingleOrDefaultAsync(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE", cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved company is inactive or unavailable.");
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == currentUser.EmployeeId.Value, cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved employee is unavailable.");
        var departmentCode = await db.Departments.AsNoTracking().Where(x => x.Id == currentUser.DepartmentId.Value && x.IsActive)
            .Select(x => x.Code).SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("The resolved primary department is unavailable.");

        return new SessionMe(employee.Id, employee.EmployeeCode, employee.EmployeeName, company.Id, company.Code,
            currentUser.DepartmentId.Value, departmentCode, currentUser.RoleCodes.Order(StringComparer.Ordinal).ToArray(),
            currentUser.IdentityIssuer!, currentUser.IdentitySubject!);
    }
}
