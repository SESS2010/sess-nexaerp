using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class ReferenceMasterEndpoints
{
    private const string CategoriesPage = "masters.item-categories";
    private const string SubcategoriesPage = "masters.item-subcategories";
    private const string UomsPage = "masters.uoms";
    private const string ManufacturersPage = "masters.manufacturers";

    public static IEndpointRouteBuilder MapReferenceMasterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/masters").WithTags("Reference Masters").RequireAuthorization();
        MapCategories(group);
        MapSubcategories(group);
        MapUoms(group);
        MapManufacturers(group);
        return endpoints;
    }

    private static void MapCategories(RouteGroupBuilder group)
    {
        group.MapGet("/item-categories", async (NexaErpDbContext db, int? page, int? pageSize, string? search, bool? isActive, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var query = db.ItemCategories.AsNoTracking().AsQueryable();
            query = Filter(query, search, isActive);
            var total = await query.CountAsync(ct);
            var rows = await Sort(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize)
                .Select(x => new ReferenceMasterSummary(x.Id, x.Code, x.Name, x.IsActive, x.Version)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<ReferenceMasterSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission(CategoriesPage, PagePermissionActions.View);

        group.MapGet("/item-categories/{id:guid}", async (Guid id, NexaErpDbContext db, CancellationToken ct) =>
        {
            var row = await db.ItemCategories.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new ReferenceMasterSummary(x.Id, x.Code, x.Name, x.IsActive, x.Version)).SingleOrDefaultAsync(ct);
            return row is null ? Results.NotFound(new { message = "Item category not found." }) : Results.Ok(row);
        }).RequirePagePermission(CategoriesPage, PagePermissionActions.View);

        group.MapPost("/item-categories", async (UpsertReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(request.Code);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Category code and name are required." });
            if (await db.ItemCategories.AnyAsync(x => x.Code == code, ct)) return Results.Conflict(new { message = "Item category code already exists." });
            var row = new ItemCategory { Code = code, Name = request.Name.Trim(), CreatedBy = user.LoginId };
            db.ItemCategories.Add(row); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Create", nameof(ItemCategory), row.Id.ToString(), null, row, ct);
            return Results.Created($"/api/v1/masters/item-categories/{row.Id}", Summary(row));
        }).RequirePagePermission(CategoriesPage, PagePermissionActions.Create);

        group.MapPut("/item-categories/{id:guid}", async (Guid id, UpsertReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.ItemCategories.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Item category not found." });
            var conflict = ValidateVersion(request.Version, row.Version); if (conflict is not null) return conflict;
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Category code and name are required." });
            if (await db.ItemCategories.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Results.Conflict(new { message = "Item category code already exists." });
            var before = Summary(row); Apply(row, code, request.Name, user.LoginId); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Update", nameof(ItemCategory), row.Id.ToString(), before, row, ct); return Results.Ok(Summary(row));
        }).RequirePagePermission(CategoriesPage, PagePermissionActions.Update);

        group.MapPost("/item-categories/{id:guid}/deactivate", async (Guid id, DeactivateReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.ItemCategories.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Item category not found." });
            var validation = ValidateDeactivate(request, row); if (validation is not null) return validation;
            if (await db.Items.AnyAsync(x => x.CategoryId == id && x.IsActive, ct) || await db.ItemSubcategories.AnyAsync(x => x.CategoryId == id && x.IsActive, ct) || await db.QcInspectionPolicies.AnyAsync(x => x.ItemCategoryId == id && x.IsActive, ct) || await db.StoreCategoryRoutes.AnyAsync(x => x.ItemCategoryId == id && x.IsActive, ct)) return Results.Conflict(new { message = "Item category is referenced by active items, subcategories, QC policies or Stores routes." });
            return await Deactivate(row, request, db, audit, user, nameof(ItemCategory), Summary(row), ct);
        }).RequirePagePermission(CategoriesPage, PagePermissionActions.Deactivate);
    }

    private static void MapSubcategories(RouteGroupBuilder group)
    {
        group.MapGet("/item-subcategories", async (NexaErpDbContext db, int? page, int? pageSize, string? search, bool? isActive, Guid? categoryId, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize); var query = db.ItemSubcategories.AsNoTracking().Include(x => x.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToUpperInvariant(); query = query.Where(x => x.Code.ToUpper().Contains(term) || x.Name.ToUpper().Contains(term)); }
            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value); if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
            var total = await query.CountAsync(ct); query = SortSubcategories(query, sortBy, sortDirection);
            var rows = await query.Skip(paging.Skip).Take(paging.PageSize).Select(x => new ItemSubcategorySummary(x.Id, x.CategoryId, x.Category!.Code, x.Category.Name, x.Code, x.Name, x.IsActive, x.Version)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<ItemSubcategorySummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission(SubcategoriesPage, PagePermissionActions.View);

        group.MapGet("/item-subcategories/{id:guid}", async (Guid id, NexaErpDbContext db, CancellationToken ct) =>
        {
            var row = await db.ItemSubcategories.AsNoTracking().Where(x => x.Id == id).Select(x => new ItemSubcategorySummary(x.Id, x.CategoryId, x.Category!.Code, x.Category.Name, x.Code, x.Name, x.IsActive, x.Version)).SingleOrDefaultAsync(ct);
            return row is null ? Results.NotFound(new { message = "Item subcategory not found." }) : Results.Ok(row);
        }).RequirePagePermission(SubcategoriesPage, PagePermissionActions.View);

        group.MapPost("/item-subcategories", async (UpsertItemSubcategoryRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); var category = await db.ItemCategories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.IsActive, ct);
            if (category is null || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Active category, subcategory code and name are required." });
            if (await db.ItemSubcategories.AnyAsync(x => x.CategoryId == request.CategoryId && x.Code == code, ct)) return Results.Conflict(new { message = "Subcategory code already exists in this category." });
            var row = new ItemSubcategory { CategoryId = category.Id, Category = category, Code = code, Name = request.Name.Trim(), CreatedBy = user.LoginId }; db.ItemSubcategories.Add(row);
            await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Create", nameof(ItemSubcategory), row.Id.ToString(), null, row, ct); return Results.Created($"/api/v1/masters/item-subcategories/{row.Id}", SubcategorySummary(row));
        }).RequirePagePermission(SubcategoriesPage, PagePermissionActions.Create);

        group.MapPut("/item-subcategories/{id:guid}", async (Guid id, UpsertItemSubcategoryRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.ItemSubcategories.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Item subcategory not found." });
            var conflict = ValidateVersion(request.Version, row.Version); if (conflict is not null) return conflict;
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); var category = await db.ItemCategories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.IsActive, ct);
            if (category is null || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Active category, subcategory code and name are required." });
            if (await db.ItemSubcategories.AnyAsync(x => x.Id != id && x.CategoryId == request.CategoryId && x.Code == code, ct)) return Results.Conflict(new { message = "Subcategory code already exists in this category." });
            var before = SubcategorySummary(row); row.CategoryId = category.Id; row.Category = category; Apply(row, code, request.Name, user.LoginId); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Update", nameof(ItemSubcategory), row.Id.ToString(), before, row, ct); return Results.Ok(SubcategorySummary(row));
        }).RequirePagePermission(SubcategoriesPage, PagePermissionActions.Update);

        group.MapPost("/item-subcategories/{id:guid}/deactivate", async (Guid id, DeactivateReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.ItemSubcategories.Include(x => x.Category).SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Item subcategory not found." });
            var validation = ValidateDeactivate(request, row); if (validation is not null) return validation;
            if (await db.Items.AnyAsync(x => x.SubcategoryId == id && x.IsActive, ct)) return Results.Conflict(new { message = "Item subcategory is referenced by active items." });
            return await Deactivate(row, request, db, audit, user, nameof(ItemSubcategory), SubcategorySummary(row), ct);
        }).RequirePagePermission(SubcategoriesPage, PagePermissionActions.Deactivate);
    }

    private static void MapUoms(RouteGroupBuilder group)
    {
        group.MapGet("/uoms", async (NexaErpDbContext db, int? page, int? pageSize, string? search, bool? isActive, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize); var query = db.Uoms.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToUpperInvariant(); query = query.Where(x => x.Code.ToUpper().Contains(term) || x.Name.ToUpper().Contains(term) || x.MeasurementDimension.ToUpper().Contains(term)); }
            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value); var total = await query.CountAsync(ct); query = SortUoms(query, sortBy, sortDirection);
            var rows = await query.Skip(paging.Skip).Take(paging.PageSize)
                .Select(x => new UomSummary(x.Id, x.Code, x.Name, x.MeasurementDimension, x.QuantityPrecision, x.IsActive, x.Version)).ToListAsync(ct); return Results.Ok(new PagedResponse<UomSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission(UomsPage, PagePermissionActions.View);

        group.MapGet("/uoms/{id:guid}", async (Guid id, NexaErpDbContext db, CancellationToken ct) => { var row = await db.Uoms.AsNoTracking().Where(x => x.Id == id).Select(x => new UomSummary(x.Id, x.Code, x.Name, x.MeasurementDimension, x.QuantityPrecision, x.IsActive, x.Version)).SingleOrDefaultAsync(ct); return row is null ? Results.NotFound(new { message = "UOM not found." }) : Results.Ok(row); }).RequirePagePermission(UomsPage, PagePermissionActions.View);

        group.MapPost("/uoms", async (UpsertUomMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); var dimension = MasterEndpointHelpers.NormalizeCode(request.MeasurementDimension);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(dimension)) return Results.BadRequest(new { message = "UOM code, name and measurement dimension are required." });
            if (await db.Uoms.AnyAsync(x => x.Code == code, ct)) return Results.Conflict(new { message = "UOM code already exists." });
            var row = new Uom { Code = code, Name = request.Name.Trim(), MeasurementDimension = dimension, QuantityPrecision = 6, CreatedBy = user.LoginId }; db.Uoms.Add(row); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Create", nameof(Uom), row.Id.ToString(), null, row, ct); return Results.Created($"/api/v1/masters/uoms/{row.Id}", UomSummary(row));
        }).RequirePagePermission(UomsPage, PagePermissionActions.Create);

        group.MapPut("/uoms/{id:guid}", async (Guid id, UpsertUomMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.Uoms.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "UOM not found." }); var conflict = ValidateVersion(request.Version, row.Version); if (conflict is not null) return conflict;
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); var dimension = MasterEndpointHelpers.NormalizeCode(request.MeasurementDimension); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(dimension)) return Results.BadRequest(new { message = "UOM code, name and measurement dimension are required." });
            if (await db.Uoms.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Results.Conflict(new { message = "UOM code already exists." });
            var before = UomSummary(row); Apply(row, code, request.Name, user.LoginId); row.MeasurementDimension = dimension; row.QuantityPrecision = 6; await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Update", nameof(Uom), row.Id.ToString(), before, row, ct); return Results.Ok(UomSummary(row));
        }).RequirePagePermission(UomsPage, PagePermissionActions.Update);

        group.MapPost("/uoms/{id:guid}/deactivate", async (Guid id, DeactivateReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.Uoms.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "UOM not found." }); var validation = ValidateDeactivate(request, row); if (validation is not null) return validation;
            if (await db.Items.AnyAsync(x => x.IsActive && (x.UomId == id || x.BaseUomId == id), ct) || await db.UomConversions.AnyAsync(x => x.IsActive && (x.FromUomId == id || x.ToUomId == id), ct) || await db.QcInspectionPolicies.AnyAsync(x => x.IsActive && x.MeasurementUomId == id, ct) || await db.DeliveryChallanLines.AnyAsync(x => x.WeightUomId == id, ct) || await db.QcInspectionParameterResults.AnyAsync(x => x.MeasurementUomIdSnapshot == id, ct)) return Results.Conflict(new { message = "UOM is referenced by active or historical inventory, conversion, QC or delivery records." });
            return await Deactivate(row, request, db, audit, user, nameof(Uom), UomSummary(row), ct);
        }).RequirePagePermission(UomsPage, PagePermissionActions.Deactivate);
    }

    private static void MapManufacturers(RouteGroupBuilder group)
    {
        group.MapGet("/manufacturers", async (NexaErpDbContext db, int? page, int? pageSize, string? search, bool? isActive, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var paging = MasterEndpointHelpers.NormalizePaging(page, pageSize); var query = Filter(db.Manufacturers.AsNoTracking(), search, isActive); var total = await query.CountAsync(ct); var rows = await Sort(query, sortBy, sortDirection).Skip(paging.Skip).Take(paging.PageSize).Select(x => new ReferenceMasterSummary(x.Id, x.Code, x.Name, x.IsActive, x.Version)).ToListAsync(ct); return Results.Ok(new PagedResponse<ReferenceMasterSummary>(total, paging.PageNumber, paging.PageSize, rows));
        }).RequirePagePermission(ManufacturersPage, PagePermissionActions.View);
        group.MapGet("/manufacturers/{id:guid}", async (Guid id, NexaErpDbContext db, CancellationToken ct) => { var row = await db.Manufacturers.AsNoTracking().Where(x => x.Id == id).Select(x => new ReferenceMasterSummary(x.Id, x.Code, x.Name, x.IsActive, x.Version)).SingleOrDefaultAsync(ct); return row is null ? Results.NotFound(new { message = "Manufacturer not found." }) : Results.Ok(row); }).RequirePagePermission(ManufacturersPage, PagePermissionActions.View);
        group.MapPost("/manufacturers", async (UpsertReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Manufacturer code and name are required." }); if (await db.Manufacturers.AnyAsync(x => x.Code == code, ct)) return Results.Conflict(new { message = "Manufacturer code already exists." });
            var row = new Manufacturer { Code = code, Name = request.Name.Trim(), CreatedBy = user.LoginId }; db.Manufacturers.Add(row); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Create", nameof(Manufacturer), row.Id.ToString(), null, row, ct); return Results.Created($"/api/v1/masters/manufacturers/{row.Id}", Summary(row));
        }).RequirePagePermission(ManufacturersPage, PagePermissionActions.Create);
        group.MapPut("/manufacturers/{id:guid}", async (Guid id, UpsertReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.Manufacturers.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Manufacturer not found." }); var conflict = ValidateVersion(request.Version, row.Version); if (conflict is not null) return conflict;
            var code = MasterEndpointHelpers.NormalizeCode(request.Code); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Manufacturer code and name are required." }); if (await db.Manufacturers.AnyAsync(x => x.Id != id && x.Code == code, ct)) return Results.Conflict(new { message = "Manufacturer code already exists." });
            var before = Summary(row); Apply(row, code, request.Name, user.LoginId); await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Update", nameof(Manufacturer), row.Id.ToString(), before, row, ct); return Results.Ok(Summary(row));
        }).RequirePagePermission(ManufacturersPage, PagePermissionActions.Update);
        group.MapPost("/manufacturers/{id:guid}/deactivate", async (Guid id, DeactivateReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var row = await db.Manufacturers.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null) return Results.NotFound(new { message = "Manufacturer not found." }); var validation = ValidateDeactivate(request, row); if (validation is not null) return validation; if (await db.Items.AnyAsync(x => x.ManufacturerId == id && x.IsActive, ct)) return Results.Conflict(new { message = "Manufacturer is referenced by active items." }); return await Deactivate(row, request, db, audit, user, nameof(Manufacturer), Summary(row), ct);
        }).RequirePagePermission(ManufacturersPage, PagePermissionActions.Deactivate);
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> query, string? search, bool? isActive) where T : AuditableEntity
    {
        if (typeof(T) == typeof(ItemCategory)) { var typed = (IQueryable<ItemCategory>)query; if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToUpperInvariant(); typed = typed.Where(x => x.Code.ToUpper().Contains(term) || x.Name.ToUpper().Contains(term)); } if (isActive.HasValue) typed = typed.Where(x => x.IsActive == isActive.Value); return (IQueryable<T>)typed; }
        var manufacturers = (IQueryable<Manufacturer>)query; if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim().ToUpperInvariant(); manufacturers = manufacturers.Where(x => x.Code.ToUpper().Contains(term) || x.Name.ToUpper().Contains(term)); } if (isActive.HasValue) manufacturers = manufacturers.Where(x => x.IsActive == isActive.Value); return (IQueryable<T>)manufacturers;
    }

    private static IQueryable<T> Sort<T>(IQueryable<T> query, string? sortBy, string? direction) where T : AuditableEntity
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        if (typeof(T) == typeof(ItemCategory)) { var typed = (IQueryable<ItemCategory>)query; typed = string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase) ? (descending ? typed.OrderByDescending(x => x.Name) : typed.OrderBy(x => x.Name)) : (descending ? typed.OrderByDescending(x => x.Code) : typed.OrderBy(x => x.Code)); return (IQueryable<T>)typed; }
        var manufacturers = (IQueryable<Manufacturer>)query; manufacturers = string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase) ? (descending ? manufacturers.OrderByDescending(x => x.Name) : manufacturers.OrderBy(x => x.Name)) : (descending ? manufacturers.OrderByDescending(x => x.Code) : manufacturers.OrderBy(x => x.Code)); return (IQueryable<T>)manufacturers;
    }

    private static IQueryable<ItemSubcategory> SortSubcategories(IQueryable<ItemSubcategory> query, string? sortBy, string? direction) => string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase) ? (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)) : (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code));
    private static IQueryable<Uom> SortUoms(IQueryable<Uom> query, string? sortBy, string? direction) => string.Equals(sortBy, "name", StringComparison.OrdinalIgnoreCase) ? (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name)) : (string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase) ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code));
    private static ReferenceMasterSummary Summary(ItemCategory x) => new(x.Id, x.Code, x.Name, x.IsActive, x.Version);
    private static ReferenceMasterSummary Summary(Manufacturer x) => new(x.Id, x.Code, x.Name, x.IsActive, x.Version);
    private static ItemSubcategorySummary SubcategorySummary(ItemSubcategory x) => new(x.Id, x.CategoryId, x.Category!.Code, x.Category.Name, x.Code, x.Name, x.IsActive, x.Version);
    private static UomSummary UomSummary(Uom x) => new(x.Id, x.Code, x.Name, x.MeasurementDimension, x.QuantityPrecision, x.IsActive, x.Version);
    private static IResult? ValidateVersion(uint? expected, uint actual) => !expected.HasValue || expected.Value != actual ? Results.Conflict(new { message = "Stale record version. Refresh and retry." }) : null;
    private static IResult? ValidateDeactivate(DeactivateReferenceMasterRequest request, AuditableEntity row) => string.IsNullOrWhiteSpace(request.Reason) ? Results.BadRequest(new { message = "Deactivation reason is required." }) : request.Version != row.Version ? Results.Conflict(new { message = "Stale record version. Refresh and retry." }) : null;

    private static void Apply(AuditableEntity row, string code, string name, string login)
    {
        switch (row)
        {
            case ItemCategory category: category.Code = code; category.Name = name.Trim(); break;
            case ItemSubcategory subcategory: subcategory.Code = code; subcategory.Name = name.Trim(); break;
            case Uom uom: uom.Code = code; uom.Name = name.Trim(); break;
            case Manufacturer manufacturer: manufacturer.Code = code; manufacturer.Name = name.Trim(); break;
            default: throw new InvalidOperationException($"Unsupported reference master type {row.GetType().Name}.");
        }
        row.Version = checked(row.Version + 1); row.UpdatedBy = login; row.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static async Task<IResult> Deactivate(AuditableEntity row, DeactivateReferenceMasterRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, string entityName, object before, CancellationToken ct)
    {
        var code = row switch
        {
            ItemCategory x => Deactivate(x),
            ItemSubcategory x => Deactivate(x),
            Uom x => Deactivate(x),
            Manufacturer x => Deactivate(x),
            _ => throw new InvalidOperationException($"Unsupported reference master type {row.GetType().Name}.")
        };
        row.Version = checked(row.Version + 1); row.UpdatedBy = user.LoginId; row.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "Deactivate", entityName, row.Id.ToString(), before, new { row.Id, Code = code, IsActive = false, request.Reason, row.Version }, ct); return Results.Ok(new { row.Id, Code = code, IsActive = false, row.Version });
    }

    private static string Deactivate(ItemCategory row) { row.IsActive = false; return row.Code; }
    private static string Deactivate(ItemSubcategory row) { row.IsActive = false; return row.Code; }
    private static string Deactivate(Uom row) { row.IsActive = false; return row.Code; }
    private static string Deactivate(Manufacturer row) { row.IsActive = false; return row.Code; }
}
