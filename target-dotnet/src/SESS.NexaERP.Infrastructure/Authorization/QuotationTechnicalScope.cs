using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

// Technical competence is anchored to an active employee department assignment.
// Item categories have no governed technical-department mapping in the schema.
public static class QuotationTechnicalScope
{
    public static IQueryable<RequestForQuotation> AccessibleRfqs(NexaErpDbContext db,
        ICurrentUser user, DateOnly onDate)
    {
        var actor = user.EmployeeId;
        var organization = user.OrganizationId;
        var technicalRole = user.EffectiveRoleAssignments.Any(x => x.AssignmentId != Guid.Empty &&
            OperationRoleContracts.TechnicalVerification.Contains(x.RoleCode, StringComparer.Ordinal) &&
            RoleAuthorityResolution.CanAssignmentExercise(x.AssignmentType, "TechnicalVerification"));
        var rfqs = db.RequestForQuotations.AsNoTracking();
        if (!user.IsAuthenticated || !actor.HasValue || string.IsNullOrWhiteSpace(organization) || !technicalRole)
            return rfqs.Where(_ => false);

        var companies = db.Companies.Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => x.Id);
        var memberships = db.EmployeeCompanyAssignments.Where(x => x.EmployeeId == actor.Value &&
            companies.Contains(x.CompanyId) && x.IsActive && x.Status == "ACTIVE" && x.EffectiveFrom <= onDate &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate));
        var departments = db.EmployeeDepartmentAssignments.Where(x => companies.Contains(x.CompanyId) &&
            x.IsActive && x.Status == "ACTIVE" && x.EffectiveFrom <= onDate &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate) &&
            memberships.Any(m => m.Id == x.EmployeeCompanyAssignmentId && m.CompanyId == x.CompanyId) &&
            db.Departments.Any(d => d.Id == x.DepartmentId && d.IsActive));
        var scopes = db.EmployeeOperationalScopes.Where(x => companies.Contains(x.CompanyId) &&
            x.EmployeeId == actor.Value && x.OrganizationId == organization && x.IsActive &&
            x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate) &&
            departments.Any(d => d.CompanyId == x.CompanyId && (!x.DepartmentId.HasValue || d.DepartmentId == x.DepartmentId)));
        // Preserve warehouse/rack/ownership restrictions. A privileged cross-scope flag
        // does not replace the required intersection with an active own department.
        return rfqs.Where(r => r.OrganizationId == organization && companies.Contains(r.CompanyId) &&
            scopes.Any(s => s.CompanyId == r.CompanyId &&
                (!s.WarehouseId.HasValue || s.WarehouseId == r.DeliveryWarehouseId) && !s.RackBinId.HasValue &&
                (!s.OwnRecordsOnly || r.OwnerEmployeeId == actor.Value)));
    }
}
