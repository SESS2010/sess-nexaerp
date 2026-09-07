using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

public static class PurchaseRequisitionVisibility
{
    public static IQueryable<PurchaseRequisition> Apply(
        IQueryable<PurchaseRequisition> query,
        ICurrentUser user,
        NexaErpDbContext db)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return query.Where(_ => false);

        query = query.Where(x => x.OrganizationId == user.OrganizationId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employeeId = user.EmployeeId.Value;
        var scopes = EffectiveScopes(db, user.OrganizationId, employeeId, today);
        if (user.RoleCodes.Any(Rev869ARoleCodes.IsExplicitCrossScopeRole) &&
            scopes.Any(x => x.AllowsPrivilegedCrossScope))
            return query;

        var employeeText = employeeId.ToString();
        var step1 = $$"""{"steps":[{"stepNumber":1,"employeeId":"{{employeeText}}"}]}""";
        var step2 = $$"""{"steps":[{"stepNumber":2,"employeeId":"{{employeeText}}"}]}""";

        return query.Where(pr =>
            pr.RequesterEmployeeId == employeeId ||
            pr.CreatorEmployeeId == employeeId ||
            pr.ApprovalCycle > 0 && pr.CompletedApprovalStepCount < pr.RequiredApprovalStepCount &&
                (pr.CompletedApprovalStepCount == 0 && EF.Functions.JsonContains(pr.ApprovalWorkflowSnapshotJson, step1) ||
                 pr.CompletedApprovalStepCount == 1 && EF.Functions.JsonContains(pr.ApprovalWorkflowSnapshotJson, step2)) ||
            scopes.Any(scope =>
                (!scope.DepartmentId.HasValue || scope.DepartmentId == pr.RequestingDepartmentId) &&
                (!scope.WarehouseId.HasValue || scope.WarehouseId == pr.DeliveryWarehouseId) &&
                !scope.RackBinId.HasValue &&
                (!scope.OwnRecordsOnly || pr.RequesterEmployeeId == employeeId)));
    }

    public static async Task<bool> CanCreateAsync(
        ICurrentUser user,
        NexaErpDbContext db,
        Guid departmentId,
        Guid warehouseId,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scopes = EffectiveScopes(db, user.OrganizationId, user.EmployeeId.Value, today);
        if (user.RoleCodes.Any(Rev869ARoleCodes.IsExplicitCrossScopeRole) &&
            await scopes.AnyAsync(x => x.AllowsPrivilegedCrossScope, ct))
            return true;

        return await scopes.AnyAsync(scope =>
            (!scope.DepartmentId.HasValue || scope.DepartmentId == departmentId) &&
            (!scope.WarehouseId.HasValue || scope.WarehouseId == warehouseId) &&
            !scope.RackBinId.HasValue, ct);
    }

    private static IQueryable<EmployeeOperationalScope> EffectiveScopes(
        NexaErpDbContext db,
        string organizationId,
        Guid employeeId,
        DateOnly today) =>
        db.EmployeeOperationalScopes.Where(x =>
            x.OrganizationId == organizationId &&
            x.EmployeeId == employeeId &&
            x.IsActive &&
            x.EffectiveFrom <= today &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
}
