using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Authorization;

public sealed class EfRecordScopeAuthorizer(NexaErpDbContext db) : IRecordScopeAuthorizer
{
    public async Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId)) return new(false, "Organization scope is required.");
        var exists = await db.EmployeeOperationalScopes.AsNoTracking().AnyAsync(x => x.EmployeeId == employeeId && x.OrganizationId == organizationId && x.IsActive && x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate), cancellationToken);
        return exists ? new(true, "At least one effective operational scope exists.") : new(false, "No active operational scope is configured.");
    }
    public async Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken cancellationToken)
    {
        var scopes = await db.EmployeeOperationalScopes.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.OrganizationId == target.OrganizationId && x.IsActive)
            .Where(x => x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .ToListAsync(cancellationToken);
        if (scopes.Count == 0) return new(false, "No active operational scope is configured.");

        if (Rev869ARoleCodes.IsExplicitCrossScopeRole(roleCode) && scopes.Any(x => x.AllowsPrivilegedCrossScope))
            return new(true, "Explicit audited privileged cross-scope grant applies.");

        var allowed = scopes.Any(x => x.Matches(target.DepartmentId, target.WarehouseId, target.RackBinId, target.OwnerEmployeeId, onDate));
        return allowed
            ? new(true, "Role and most-restrictive operational scope intersect.")
            : new(false, "Record is outside department, warehouse or Rack/Bin scope.");
    }
}
