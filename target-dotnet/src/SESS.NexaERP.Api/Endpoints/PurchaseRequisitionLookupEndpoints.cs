using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class PurchaseRequisitionEndpoints
{
    private static async Task<IResult> DepartmentLookups(
        NexaErpDbContext db,
        ICurrentUser user,
        CancellationToken ct)
    {
        var scopes = EffectiveCreateScopes(db, user);
        var crossScope = user.RoleCodes.Any(Rev869ARoleCodes.IsExplicitCrossScopeRole) &&
            await scopes.AnyAsync(x => x.AllowsPrivilegedCrossScope, ct);
        var hasAllDepartments = crossScope || await scopes.AnyAsync(x => !x.DepartmentId.HasValue, ct);
        var departmentIds = scopes.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value);
        var query = db.Departments.AsNoTracking().Where(x => x.IsActive);
        if (!hasAllDepartments) query = query.Where(x => departmentIds.Contains(x.Id));
        var rows = await query.OrderBy(x => x.Name)
            .Select(x => new PurchaseRequisitionLookupOption(x.Code, x.Name)).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> WarehouseLookups(
        NexaErpDbContext db,
        ICurrentUser user,
        CancellationToken ct)
    {
        var companyId = await CurrentCompanyId(db, user, ct);
        var scopes = EffectiveCreateScopes(db, user);
        var crossScope = user.RoleCodes.Any(Rev869ARoleCodes.IsExplicitCrossScopeRole) &&
            await scopes.AnyAsync(x => x.AllowsPrivilegedCrossScope, ct);
        var hasAllWarehouses = crossScope || await scopes.AnyAsync(x => !x.WarehouseId.HasValue, ct);
        var warehouseIds = scopes.Where(x => x.WarehouseId.HasValue).Select(x => x.WarehouseId!.Value);
        var query = db.Warehouses.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
        if (!hasAllWarehouses) query = query.Where(x => warehouseIds.Contains(x.Id));
        var rows = await query.OrderBy(x => x.Name)
            .Select(x => new PurchaseRequisitionLookupOption(x.WarehouseCode, x.Name)).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static async Task<IResult> ItemLookups(
        string? search,
        NexaErpDbContext db,
        CancellationToken ct)
    {
        // Item is an organization-wide reference master; company ownership is applied
        // to inventory settings and stock, not to the item definition itself.
        var query = db.Items.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.ItemCode.ToUpper().Contains(normalized) || x.Name.ToUpper().Contains(normalized));
        }
        var rows = await query.OrderBy(x => x.ItemCode).Take(25)
            .Select(x => new PurchaseRequisitionLookupOption(x.ItemCode, x.Name)).ToListAsync(ct);
        return Results.Ok(rows);
    }

    private static IQueryable<EmployeeOperationalScope> EffectiveCreateScopes(
        NexaErpDbContext db,
        ICurrentUser user)
    {
        if (!user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            return db.EmployeeOperationalScopes.Where(_ => false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return db.EmployeeOperationalScopes.Where(x =>
            x.OrganizationId == user.OrganizationId &&
            x.EmployeeId == user.EmployeeId.Value &&
            x.IsActive &&
            x.EffectiveFrom <= today &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
    }

    private static async Task<Guid> CurrentCompanyId(
        NexaErpDbContext db,
        ICurrentUser user,
        CancellationToken ct) =>
        await db.Companies.Where(x => x.Code == user.OrganizationId && x.IsActive)
            .Select(x => x.Id).SingleAsync(ct);
}
