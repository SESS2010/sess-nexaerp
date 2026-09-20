using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

// Stores category routes decide where a receipt of each item category is held for QC,
// where its rejected quantity waits for return, and where accepted stock lands by
// default. The GRN resolves exactly one effective route per company and category, so a
// fresh company cannot receive goods until Stores has created them. Until now only the
// development-only trial script wrote this table.
public static partial class Rev869AConfigurationEndpoints
{
    private const string StoreCategoryRoutesPage = "masters.store-category-routes";

    private static async Task<IResult> CreateStoreCategoryRoute(CreateStoreCategoryRouteRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var organization = request.OrganizationId.Trim().ToUpperInvariant();
        if (!string.Equals(organization, user.OrganizationId, StringComparison.Ordinal)) return Results.Forbid();
        var companyId = await db.Companies.Where(x => x.Code == organization && x.IsActive && x.Status == "ACTIVE").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!companyId.HasValue) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Route preparation remarks are required." });
        if (request.EffectiveTo < request.EffectiveFrom) return Results.BadRequest(new { message = "Invalid Stores route effective range." });
        var categoryCode = MasterEndpointHelpers.NormalizeCode(request.ItemCategoryCode);
        var category = await db.ItemCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Code == categoryCode && x.IsActive, ct);
        if (category is null) return Results.Conflict(new { message = "Active item category was not found." });
        var ids = new[] { request.QcHoldConditionLocationId, request.PendingReturnConditionLocationId, request.DefaultAcceptedConditionLocationId };
        var locations = await db.WarehouseConditionLocations.AsNoTracking().Include(x => x.Warehouse).Include(x => x.RackBin)
            .Where(x => x.CompanyId == companyId.Value && ids.Contains(x.Id)).ToListAsync(ct);
        WarehouseConditionLocation? Effective(Guid id, string condition) => locations.SingleOrDefault(x => x.Id == id
            && string.Equals(x.ConditionCode, condition, StringComparison.Ordinal) && x.IsEffective(request.EffectiveFrom)
            && x.Warehouse is { IsActive: true } && x.RackBin is { IsActive: true });
        var qcHold = Effective(request.QcHoldConditionLocationId, InventoryConditionCodes.QcHold);
        var pendingReturn = Effective(request.PendingReturnConditionLocationId, InventoryConditionCodes.PendingReturnableDc);
        var accepted = Effective(request.DefaultAcceptedConditionLocationId, InventoryConditionCodes.Available);
        if (qcHold is null || pendingReturn is null || accepted is null)
            return Results.Conflict(new { message = "Route requires effective same-company QC_HOLD, PENDING_RETURNABLE_DC and AVAILABLE condition locations." });
        if (qcHold.WarehouseId != pendingReturn.WarehouseId || qcHold.WarehouseId != accepted.WarehouseId)
            return Results.Conflict(new { message = "All three route locations must belong to one warehouse." });
        // The GRN resolves exactly one effective route per company and category.
        var overlap = await db.StoreCategoryRoutes.AnyAsync(x => x.CompanyId == companyId.Value && x.ItemCategoryId == category.Id && x.IsActive
            && x.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue) && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= request.EffectiveFrom), ct);
        if (overlap) return Results.Conflict(new { message = "An overlapping Stores route exists for this item category." });
        var entity = new StoreCategoryRoute
        {
            CompanyId = companyId.Value, ItemCategoryId = category.Id, QcHoldConditionLocationId = qcHold.Id,
            PendingReturnConditionLocationId = pendingReturn.Id, DefaultAcceptedConditionLocationId = accepted.Id,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo, CreatedBy = user.LoginId
        };
        db.StoreCategoryRoutes.Add(entity);
        AddHistory(db, organization, nameof(StoreCategoryRoute), entity.Id, "Create", null, entity, request.Remarks, user, companyId.Value);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Stores", "CreateStoreCategoryRoute", nameof(StoreCategoryRoute), entity.Id.ToString(), null, entity, ct);
        return Results.Created($"/api/v1/rev869a/configuration/store-category-routes/{entity.Id}", Summary(entity, category.Code, qcHold, pendingReturn, accepted));
    }

    private static async Task<IResult> ListStoreCategoryRoutes(string? itemCategoryCode, bool? effectiveOnly, NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var companyId = await db.Companies.Where(x => x.Code == user.OrganizationId && x.IsActive && x.Status == "ACTIVE").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!companyId.HasValue) return Results.Forbid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.StoreCategoryRoutes.AsNoTracking().Include(x => x.ItemCategory)
            .Include(x => x.QcHoldConditionLocation!).ThenInclude(x => x.RackBin)
            .Include(x => x.QcHoldConditionLocation!).ThenInclude(x => x.Warehouse)
            .Include(x => x.PendingReturnConditionLocation!).ThenInclude(x => x.RackBin)
            .Include(x => x.DefaultAcceptedConditionLocation!).ThenInclude(x => x.RackBin)
            .Where(x => x.CompanyId == companyId.Value);
        if (!string.IsNullOrWhiteSpace(itemCategoryCode)) { var code = MasterEndpointHelpers.NormalizeCode(itemCategoryCode); query = query.Where(x => x.ItemCategory!.Code == code); }
        if (effectiveOnly == true) query = query.Where(x => x.IsActive && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today));
        var rows = await query.OrderBy(x => x.ItemCategory!.Code).ThenByDescending(x => x.EffectiveFrom).ToListAsync(ct);
        return Results.Ok(rows.Select(x => Summary(x, x.ItemCategory!.Code, x.QcHoldConditionLocation!, x.PendingReturnConditionLocation!, x.DefaultAcceptedConditionLocation!)).ToList());
    }

    private static async Task<IResult> CloseStoreCategoryRoute(Guid routeId, CloseStoreCategoryRouteRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var companyId = await db.Companies.Where(x => x.Code == user.OrganizationId && x.IsActive && x.Status == "ACTIVE").Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!companyId.HasValue) return Results.Forbid();
        var row = await db.StoreCategoryRoutes.SingleOrDefaultAsync(x => x.Id == routeId && x.CompanyId == companyId.Value, ct);
        if (row is null) return Results.NotFound(new { message = "Stores category route not found." });
        if (row.Version != request.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        if (row.EffectiveTo.HasValue) return Results.Conflict(new { message = "This route version is already closed. Create a new version instead." });
        if (request.EffectiveTo < row.EffectiveFrom) return Results.BadRequest(new { message = "Effective To must be on or after Effective From." });
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Route closure remarks are required." });
        var before = new { row.EffectiveTo, row.IsActive, row.Version };
        row.EffectiveTo = request.EffectiveTo; row.Version = checked(row.Version + 1); row.UpdatedAt = DateTimeOffset.UtcNow; row.UpdatedBy = user.LoginId;
        AddHistory(db, user.OrganizationId!, nameof(StoreCategoryRoute), row.Id, "CloseVersion", before, new { row.EffectiveTo, row.IsActive, row.Version }, request.Remarks, user, row.CompanyId);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Stores", "CloseStoreCategoryRoute", nameof(StoreCategoryRoute), row.Id.ToString(), before, row, ct);
        return Results.Ok(new { row.Id, row.EffectiveTo, row.Version });
    }

    private static StoreCategoryRouteSummary Summary(StoreCategoryRoute route, string categoryCode,
        WarehouseConditionLocation qcHold, WarehouseConditionLocation pendingReturn, WarehouseConditionLocation accepted) =>
        new(route.Id, route.ItemCategoryId, categoryCode, qcHold.Id, qcHold.RackBin!.BinCode, pendingReturn.Id, pendingReturn.RackBin!.BinCode,
            accepted.Id, accepted.RackBin!.BinCode, qcHold.Warehouse!.WarehouseCode, route.EffectiveFrom, route.EffectiveTo, route.IsActive, route.Version);
}
