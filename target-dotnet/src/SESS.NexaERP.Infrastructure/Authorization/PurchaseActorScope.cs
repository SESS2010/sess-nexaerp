using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

// Operation-specific scope. Never use workflow read access to authorize an update/issue.
public static class PurchaseActorScope
{
    public static IQueryable<EmployeeOperationalScope> ActiveScopes(NexaErpDbContext db, ICurrentUser user, DateOnly today)
    {
        var employee = user.EmployeeId;
        var organization = user.OrganizationId;
        if (!user.IsAuthenticated || !employee.HasValue || string.IsNullOrWhiteSpace(organization))
            return db.EmployeeOperationalScopes.Where(_ => false);
        var companies = db.Companies.Where(c => c.Code == organization && c.IsActive && c.Status == "ACTIVE").Select(c => c.Id);
        var memberships = db.EmployeeCompanyAssignments.Where(m => companies.Contains(m.CompanyId) &&
            m.EmployeeId == employee.Value && m.IsActive && m.Status == "ACTIVE" && m.EffectiveFrom <= today &&
            (!m.EffectiveTo.HasValue || m.EffectiveTo.Value >= today));
        var departments = db.EmployeeDepartmentAssignments.Where(d => d.IsActive && d.Status == "ACTIVE" &&
            d.EffectiveFrom <= today && (!d.EffectiveTo.HasValue || d.EffectiveTo.Value >= today) &&
            memberships.Any(m => m.Id == d.EmployeeCompanyAssignmentId && m.CompanyId == d.CompanyId) &&
            db.Departments.Any(p => p.Id == d.DepartmentId && p.IsActive));
        var role = user.RoleCode;
        var assignment = user.ResolvedRoleAssignmentId;
        var roles = db.EmployeeRoleAssignments.Where(r => companies.Contains(r.CompanyId) && r.EmployeeId == employee.Value &&
            r.EffectiveFrom <= today && (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= today) &&
            (!assignment.HasValue || r.Id == assignment.Value) && r.Role != null && r.Role.IsActive && r.Role.Code == role);
        var issuer = user.IdentityIssuer;
        var subject = user.IdentitySubject;
        return db.EmployeeOperationalScopes.Where(s => s.OrganizationId == organization && companies.Contains(s.CompanyId) &&
            s.EmployeeId == employee.Value && s.IsActive && s.EffectiveFrom <= today &&
            (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= today) &&
            db.Employees.Any(e => e.Id == employee.Value && e.Status == "Active" && e.LoginEnabled) &&
            roles.Any(r => r.CompanyId == s.CompanyId) &&
            db.EmployeeIdentityMappings.Any(i => i.CompanyId == s.CompanyId && i.OrganizationId == organization &&
                i.EmployeeId == employee.Value && i.IsActive && i.EffectiveFrom <= today &&
                (!i.EffectiveTo.HasValue || i.EffectiveTo.Value >= today) && i.Issuer == issuer && i.Subject == subject) &&
            departments.Any(d => d.CompanyId == s.CompanyId && (!s.DepartmentId.HasValue || d.DepartmentId == s.DepartmentId)));
    }

    public static string SnapshotActor(ICurrentUser user) =>
        System.Text.Json.JsonSerializer.Serialize(new { steps = new[] { new { employeeId = user.EmployeeId, roleCode = user.RoleCode } } });

    public static IQueryable<PurchaseRequisition> ReadRequisitions(IQueryable<PurchaseRequisition> query, NexaErpDbContext db, ICurrentUser user)
    {
        var ordinaryIds = SESS.NexaERP.Infrastructure.Purchase.PurchaseRequisitionVisibility.Apply(query, user, db, includeAssignedApprover: false).Select(p => p.Id);
        var scopes = ActiveScopes(db, user, DateOnly.FromDateTime(DateTime.UtcNow));
        var actor = user.EmployeeId;
        var role = user.RoleCode;
        var snapshotActor = SnapshotActor(user);
        return query.Where(p => ordinaryIds.Contains(p.Id) || p.OrganizationId == user.OrganizationId &&
            scopes.Any(s => s.CompanyId == p.CompanyId && (!s.WarehouseId.HasValue || s.WarehouseId == p.DeliveryWarehouseId) &&
                !s.RackBinId.HasValue && (!s.OwnRecordsOnly || p.RequesterEmployeeId == actor)) &&
            (p.ApprovalCycle > 0 && EF.Functions.JsonContains(p.ApprovalWorkflowSnapshotJson, snapshotActor) ||
             db.PurchaseRequisitionApprovalHistories.Any(h => h.CompanyId == p.CompanyId && h.PurchaseRequisitionId == p.Id &&
                h.ResolvedEmployeeId == actor && h.ResolvedRoleCode == role && h.StepNumber > 0)));
    }

    public static IQueryable<PurchaseRequisition> StockRequisitions(IQueryable<PurchaseRequisition> query, NexaErpDbContext db, ICurrentUser user)
    {
        if (user.RoleCode is not ("STORES_EXECUTIVE" or "STORES_ASSISTANT" or "STORES_MANAGER"))
            return query.Where(_ => false);
        var scopes = ActiveScopes(db, user, DateOnly.FromDateTime(DateTime.UtcNow));
        var actor = user.EmployeeId;
        return query.Where(p => p.OrganizationId == user.OrganizationId &&
            scopes.Any(s => s.CompanyId == p.CompanyId && (!s.WarehouseId.HasValue || s.WarehouseId == p.DeliveryWarehouseId) &&
                !s.RackBinId.HasValue && (!s.OwnRecordsOnly || p.RequesterEmployeeId == actor)));
    }

    // Caller must first validate the immutable snapshot's EXACT next employee/role and SoD.
    public static Task<bool> AllowsAssignedDecisionAsync(NexaErpDbContext db, ICurrentUser user, Guid companyId,
        Guid? warehouseId, Guid? ownerId, CancellationToken ct) =>
        ActiveScopes(db, user, DateOnly.FromDateTime(DateTime.UtcNow)).AnyAsync(s => s.CompanyId == companyId &&
            (!s.WarehouseId.HasValue || s.WarehouseId == warehouseId) && !s.RackBinId.HasValue &&
            (!s.OwnRecordsOnly || ownerId == user.EmployeeId), ct);
}
