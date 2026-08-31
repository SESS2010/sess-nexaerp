using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/items", async (NexaErpDbContext db, int? page, int? pageSize, string? search, string? status, string? category, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
            var q = db.Items.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToUpperInvariant(); q = q.Where(x => x.ItemCode.ToUpper().Contains(s) || x.Name.ToUpper().Contains(s) || (x.PartNumber != null && x.PartNumber.ToUpper().Contains(s)) || (x.Barcode != null && x.Barcode.ToUpper().Contains(s))); }
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim());
            if (!string.IsNullOrWhiteSpace(category)) { var categoryCode = MasterEndpointHelpers.NormalizeCode(category); q = q.Where(x => (x.Category != null && x.Category.Code == categoryCode) || x.MaterialType == category.Trim()); }
            var total = await q.CountAsync(ct);
            q = Sort(q, sortBy, sortDirection, x => x.ItemCode, x => x.Name, x => x.Status);
            var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => new ItemSummary(x.Id, x.ItemCode, x.Name, x.CategoryId, x.Category != null ? x.Category.Code : null, x.Category != null ? x.Category.Name : null, x.SubcategoryId, x.Subcategory != null ? x.Subcategory.Code : null, x.Subcategory != null ? x.Subcategory.Name : null, x.Uom, x.MaterialType, x.ItemType, x.IsReturnable, x.ManufacturerMake, x.Model, x.PartNumber, x.MinimumStock, x.MaximumStock, x.ReorderLevel, x.Status, x.ApprovalStatus, x.IsActive, x.Version)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<ItemSummary>(total, p.PageNumber, p.PageSize, rows));
        }).RequirePagePermission("masters.items", PagePermissionActions.View);

        group.MapGet("/items/{code}", async (string code, NexaErpDbContext db, CancellationToken ct) =>
        {
            var item = await db.Items.AsNoTracking().Include(x => x.Category).Include(x => x.Subcategory).Include(x => x.PreferredVendor).SingleOrDefaultAsync(x => x.ItemCode == MasterEndpointHelpers.NormalizeCode(code), ct);
            return item is null ? Results.NotFound(new { message = "Item not found." }) : Results.Ok(ToDetail(item));
        }).RequirePagePermission("masters.items", PagePermissionActions.View);

        group.MapPost("/items", async (UpsertItemRequest r, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var validation = await ValidateItem(r, db, null, ct); if (validation is not null) return validation;
            var item = new Item(); Apply(item, r, user.LoginId, true); await ApplyItemRelationships(item, r, db, ct); db.Items.Add(item); AddInitialStatus(db, nameof(Item), item.Id, item.ItemCode, item.Status, user.LoginId);
            await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "CreateDraft", nameof(Item), item.Id.ToString(), null, item, ct);
            return Results.Created($"/api/v1/inventory/items/{item.ItemCode}", ToDetail(item));
        }).RequirePagePermission("masters.items", PagePermissionActions.Create);

        group.MapPut("/items/{code}", async (string code, UpsertItemRequest r, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var item = await db.Items.Include(x => x.Category).Include(x => x.Subcategory).Include(x => x.PreferredVendor).SingleOrDefaultAsync(x => x.ItemCode == MasterEndpointHelpers.NormalizeCode(code), ct); if (item is null) return Results.NotFound(new { message = "Item not found." });
            if (item.IsItemCodeLocked && MasterEndpointHelpers.NormalizeCode(r.ItemCode) != item.ItemCode) return Results.BadRequest(new { message = "Item code is immutable after approval." });
            if (r.Version is null || r.Version.Value != item.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
            var validation = await ValidateItem(r, db, item.Id, ct); if (validation is not null) return validation; var before = ToDetail(item); Apply(item, r, user.LoginId, false); await ApplyItemRelationships(item, r, db, ct);
            await db.SaveChangesAsync(ct); await audit.WriteAsync("Masters", "UpdateDraft", nameof(Item), item.Id.ToString(), before, item, ct); return Results.Ok(ToDetail(item));
        }).RequirePagePermission("masters.items", PagePermissionActions.Update);

        MapItemAction(group, "submit", "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Submit);
        MapItemAction(group, "approve", "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Approve);
        MapItemAction(group, "reject", "Reject", MasterStatuses.Rejected, MasterApprovalStatuses.Rejected, PagePermissionActions.Reject);
        MapItemAction(group, "request-clarification", "RequestClarification", MasterStatuses.PendingApproval, MasterApprovalStatuses.ClarificationRequested, PagePermissionActions.RequestClarification);
        MapItemAction(group, "request-revision", "RequestRevision", MasterStatuses.Draft, MasterApprovalStatuses.RevisionRequested, PagePermissionActions.RequestRevision);
        MapItemAction(group, "resubmit", "Resubmit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Resubmit);
        MapItemAction(group, "hold", "Hold", MasterStatuses.OnHold, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapItemAction(group, "reactivate", "Reactivate", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Update);
        MapItemAction(group, "deactivate", "Deactivate", MasterStatuses.Inactive, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        group.MapGet("/items/{code}/status-history", (string code, NexaErpDbContext db, CancellationToken ct) => MasterEndpointHelpers.GetStatusHistoryAsync(db, nameof(Item), MasterEndpointHelpers.NormalizeCode(code), ct)).RequirePagePermission("masters.items", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/items/{code}/approval-history", (string code, NexaErpDbContext db, CancellationToken ct) => MasterEndpointHelpers.GetApprovalHistoryAsync(db, nameof(Item), MasterEndpointHelpers.NormalizeCode(code), ct)).RequirePagePermission("masters.items", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/items/{code}/audit-history", async (string code, NexaErpDbContext db, CancellationToken ct) => { var id = await db.Items.AsNoTracking().Where(x => x.ItemCode == MasterEndpointHelpers.NormalizeCode(code)).Select(x => x.Id.ToString()).SingleOrDefaultAsync(ct); return id is null ? Results.NotFound(new { message = "Item not found." }) : await MasterEndpointHelpers.GetAuditHistoryAsync(db, nameof(Item), id, ct); }).RequirePagePermission("masters.items", PagePermissionActions.ViewAuditHistory);

        MapWarehouseEndpoints(group);
        MapRackBinEndpoints(group);
        return endpoints;
    }

    private static void MapWarehouseEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/warehouses", async (NexaErpDbContext db, ICurrentUser user, int? page, int? pageSize, string? search, string? status, string? type, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var p = MasterEndpointHelpers.NormalizePaging(page, pageSize); var q = db.Warehouses.AsNoTracking().Where(x=>x.CompanyId==companyId);
            if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToUpperInvariant(); q = q.Where(x => x.WarehouseCode.ToUpper().Contains(s) || x.Name.ToUpper().Contains(s)); }
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim()); if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.WarehouseType == type.Trim());
            var total = await q.CountAsync(ct); q = Sort(q, sortBy, sortDirection, x => x.WarehouseCode, x => x.Name, x => x.Status);
            var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => new WarehouseSummary(x.Id, x.WarehouseCode, x.Name, x.WarehouseType, x.Location, x.Status, x.ApprovalStatus, x.IsActive, x.Version)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<WarehouseSummary>(total, p.PageNumber, p.PageSize, rows));
        }).RequirePagePermission("masters.warehouses", PagePermissionActions.View);

        group.MapGet("/warehouses/{code}", async (string code, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var w = await db.Warehouses.AsNoTracking().Include(x => x.ResponsibleEmployee).Include(x => x.Department).SingleOrDefaultAsync(x => x.CompanyId==companyId&&x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(code), ct);
            return w is null ? Results.NotFound(new { message = "Warehouse not found." }) : Results.Ok(ToDetail(w));
        }).RequirePagePermission("masters.warehouses", PagePermissionActions.View);

        group.MapPost("/warehouses", async (UpsertWarehouseRequest r, IWarehouseMasterDataService service, CancellationToken ct) =>
        {
            try{var result=await service.CreateAsync(r,ct);return Results.Created($"/api/v1/inventory/warehouses/{MasterEndpointHelpers.NormalizeCode(r.WarehouseCode)}",result);}catch(Exception ex){return MasterDataFailure(ex);}
        }).RequirePagePermission("masters.warehouses", PagePermissionActions.Create);

        group.MapPut("/warehouses/{code}", async (string code, UpsertWarehouseRequest r, NexaErpDbContext db, ICurrentUser user, IWarehouseMasterDataService service, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var normalized=MasterEndpointHelpers.NormalizeCode(code);var w=await db.Warehouses.AsNoTracking().SingleOrDefaultAsync(x=>x.CompanyId==companyId&&x.WarehouseCode==normalized,ct);if(w is null)return Results.NotFound(new{message="Warehouse not found."});if(normalized!=MasterEndpointHelpers.NormalizeCode(r.WarehouseCode))return Results.BadRequest(new{message="Warehouse Code is immutable through this endpoint."});try{var result=await service.UpdateAsync(new MasterDataExistingRecord(w.Id,w.WarehouseCode,w.WarehouseCode,w.Version,new Dictionary<string,string?>()),r,ct);return Results.Ok(result);}catch(Exception ex){return MasterDataFailure(ex);}
        }).RequirePagePermission("masters.warehouses", PagePermissionActions.Update);

        MapWarehouseAction(group, "submit", "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Submit);
        MapWarehouseAction(group, "approve", "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Approve);
        MapWarehouseAction(group, "reject", "Reject", MasterStatuses.Rejected, MasterApprovalStatuses.Rejected, PagePermissionActions.Reject);
        MapWarehouseAction(group, "hold", "Hold", MasterStatuses.OnHold, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapWarehouseAction(group, "reactivate", "Reactivate", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Update);
        MapWarehouseAction(group, "deactivate", "Deactivate", MasterStatuses.Inactive, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        group.MapGet("/warehouses/{code}/status-history", async(string code,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)=>{var id=await WarehouseId(db,user,code,ct);return id is null?Results.NotFound(new{message="Warehouse not found."}):await MasterEndpointHelpers.GetStatusHistoryByIdAsync(db,nameof(Warehouse),id.Value,ct);}).RequirePagePermission("masters.warehouses", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/warehouses/{code}/approval-history", async(string code,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)=>{var id=await WarehouseId(db,user,code,ct);return id is null?Results.NotFound(new{message="Warehouse not found."}):await MasterEndpointHelpers.GetApprovalHistoryByIdAsync(db,nameof(Warehouse),id.Value,ct);}).RequirePagePermission("masters.warehouses", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/warehouses/{code}/audit-history", async (string code, NexaErpDbContext db,ICurrentUser user, CancellationToken ct) => { var id=await WarehouseId(db,user,code,ct); return id is null ? Results.NotFound(new { message = "Warehouse not found." }) : await MasterEndpointHelpers.GetAuditHistoryAsync(db, nameof(Warehouse), id.Value.ToString(), ct); }).RequirePagePermission("masters.warehouses", PagePermissionActions.ViewAuditHistory);
    }

    private static void MapRackBinEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/rack-bins", async (NexaErpDbContext db, ICurrentUser user, int? page, int? pageSize, string? search, string? status, string? type, string? warehouseCode, string? sortBy, string? sortDirection, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var p = MasterEndpointHelpers.NormalizePaging(page, pageSize); var q = db.RackBins.AsNoTracking().Include(x => x.Warehouse).Where(x => x.CompanyId==companyId&&x.Warehouse != null).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) { var s = search.Trim().ToUpperInvariant(); q = q.Where(x => x.BinCode.ToUpper().Contains(s) || x.RackName.ToUpper().Contains(s)); }
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim()); if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.LocationType == type.Trim()); if (!string.IsNullOrWhiteSpace(warehouseCode)) { var wh = MasterEndpointHelpers.NormalizeCode(warehouseCode); q = q.Where(x => x.Warehouse!.WarehouseCode == wh); }
            var total = await q.CountAsync(ct); q = Sort(q, sortBy, sortDirection, x => x.BinCode, x => x.RackName, x => x.Status);
            var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => new RackBinSummary(x.Id, x.WarehouseId, x.Warehouse!.WarehouseCode, x.BinCode, x.RackName, x.BinNameNumber, x.Zone, x.LocationType, x.MaterialCondition, x.Status, x.ApprovalStatus, x.IsActive, x.Version)).ToListAsync(ct);
            return Results.Ok(new PagedResponse<RackBinSummary>(total, p.PageNumber, p.PageSize, rows));
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.View);

        group.MapPost("/rack-bins", async (UpsertRackBinRequest r, IRackBinMasterDataService service, CancellationToken ct) =>
        {
            try{var result=await service.CreateAsync(r,ct);return Results.Created($"/api/v1/inventory/rack-bins/{result.RecordId}",result);}catch(Exception ex){return MasterDataFailure(ex);}
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.Create);

        group.MapGet("/rack-bins/{id:guid}", async (Guid id, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var b = await db.RackBins.AsNoTracking().Include(x => x.Warehouse).SingleOrDefaultAsync(x => x.CompanyId==companyId&&x.Id == id, ct);
            return b is null ? Results.NotFound(new { message = "Rack/bin not found." }) : Results.Ok(ToDetail(b));
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.View);

        group.MapPut("/rack-bins/{id:guid}", async (Guid id, UpsertRackBinRequest r, NexaErpDbContext db, ICurrentUser user, IRackBinMasterDataService service, CancellationToken ct) =>
        {
            var companyId=await SelectedCompanyId(db,user,ct);var b=await db.RackBins.AsNoTracking().Include(x=>x.Warehouse).SingleOrDefaultAsync(x=>x.CompanyId==companyId&&x.Id==id,ct);if(b is null)return Results.NotFound(new{message="Rack/bin not found."});if(MasterEndpointHelpers.NormalizeCode(r.BinCode)!=b.BinCode)return Results.BadRequest(new{message="Bin Code is immutable through this endpoint; Rack Name and Bin / Partition Number may be renamed without changing historical IDs."});if(MasterEndpointHelpers.NormalizeCode(r.WarehouseCode)!=b.Warehouse!.WarehouseCode)return Results.Conflict(new{message="Warehouse assignment is immutable. Deactivate this rack/bin and create it in the other warehouse."});try{var result=await service.UpdateAsync(new MasterDataExistingRecord(b.Id,b.BinCode,b.BinCode,b.Version,new Dictionary<string,string?>()),r,ct);return Results.Ok(result);}catch(Exception ex){return MasterDataFailure(ex);}
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.Update);
        MapRackBinAction(group, "submit", "Submit", MasterStatuses.PendingApproval, MasterApprovalStatuses.PendingApproval, PagePermissionActions.Submit);
        MapRackBinAction(group, "approve", "Approve", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Approve);
        MapRackBinAction(group, "reject", "Reject", MasterStatuses.Rejected, MasterApprovalStatuses.Rejected, PagePermissionActions.Reject);
        MapRackBinAction(group, "hold", "Hold", MasterStatuses.OnHold, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        MapRackBinAction(group, "reactivate", "Reactivate", MasterStatuses.Active, MasterApprovalStatuses.Approved, PagePermissionActions.Update);
        MapRackBinAction(group, "deactivate", "Deactivate", MasterStatuses.Inactive, MasterApprovalStatuses.Approved, PagePermissionActions.Deactivate);
        group.MapGet("/rack-bins/{id:guid}/status-history", async (Guid id, NexaErpDbContext db,ICurrentUser user, CancellationToken ct) =>
        {
            return !await OwnsRack(db,user,id,ct)?Results.NotFound(new{message="Rack/bin not found."}):await MasterEndpointHelpers.GetStatusHistoryByIdAsync(db,nameof(RackBin),id,ct);
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/rack-bins/{id:guid}/approval-history", async (Guid id, NexaErpDbContext db,ICurrentUser user, CancellationToken ct) =>
        {
            return !await OwnsRack(db,user,id,ct)?Results.NotFound(new{message="Rack/bin not found."}):await MasterEndpointHelpers.GetApprovalHistoryByIdAsync(db,nameof(RackBin),id,ct);
        }).RequirePagePermission("masters.rack-bins", PagePermissionActions.ViewAuditHistory);
        group.MapGet("/rack-bins/{id:guid}/audit-history", async (Guid id,NexaErpDbContext db,ICurrentUser user,CancellationToken ct)=>!await OwnsRack(db,user,id,ct)?Results.NotFound(new{message="Rack/bin not found."}):await MasterEndpointHelpers.GetAuditHistoryAsync(db,nameof(RackBin),id.ToString(),ct)).RequirePagePermission("masters.rack-bins", PagePermissionActions.ViewAuditHistory);
    }

    private static void MapItemAction(RouteGroupBuilder g, string route, string action, string status, string approval, string permission) => g.MapPost($"/items/{{code}}/{route}", async (string code, MasterActionRequest r, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
    { var e = await db.Items.SingleOrDefaultAsync(x => x.ItemCode == MasterEndpointHelpers.NormalizeCode(code), ct); if (e is null) return Results.NotFound(new { message = "Item not found." }); return await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, user, e, nameof(Item), e.ItemCode, action, status, approval, r.Remarks, r.Version, (x, s, actor) => { x.Status = s; x.IsActive = s != MasterStatuses.Inactive; if (s == MasterStatuses.Active) { x.IsItemCodeLocked = true; x.ApprovedBy = actor; x.ApprovedAt = DateTimeOffset.UtcNow; } }, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, ct); }).RequirePagePermission("masters.items", permission);

    private static void MapWarehouseAction(RouteGroupBuilder g, string route, string action, string status, string approval, string permission) => g.MapPost($"/warehouses/{{code}}/{route}", async (string code, MasterActionRequest r, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
    { var companyId=await SelectedCompanyId(db,user,ct);var e = await db.Warehouses.SingleOrDefaultAsync(x => x.CompanyId==companyId&&x.WarehouseCode == MasterEndpointHelpers.NormalizeCode(code), ct); if (e is null) return Results.NotFound(new { message = "Warehouse not found." });if(route=="deactivate"){var balance=await db.StockMovements.Where(x=>x.CompanyId==companyId&&x.WarehouseId==e.Id).SumAsync(x=>(decimal?)(x.QuantityIn-x.QuantityOut),ct)??0m;if(balance!=0m)return Results.Conflict(new{message=$"Warehouse cannot be deactivated while its current stock balance is {balance}. Transfer or issue the stock first."});if(await db.RackBins.AnyAsync(x=>x.CompanyId==companyId&&x.WarehouseId==e.Id&&x.IsActive,ct))return Results.Conflict(new{message="Warehouse cannot be deactivated while it has active rack/bins. Deactivate its empty rack/bins first."});}return await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, user, e, nameof(Warehouse), e.WarehouseCode, action, status, approval, r.Remarks, r.Version, (x, s, actor) => { x.Status = s; x.IsActive = s != MasterStatuses.Inactive; if (s == MasterStatuses.Active) { x.IsWarehouseCodeLocked = true; x.ApprovedBy = actor; x.ApprovedAt = DateTimeOffset.UtcNow; } }, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, ct); }).RequirePagePermission("masters.warehouses", permission);

    private static void MapRackBinAction(RouteGroupBuilder g, string route, string action, string status, string approval, string permission) => g.MapPost($"/rack-bins/{{id:guid}}/{route}", async (Guid id, MasterActionRequest r, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct) =>
    { var companyId=await SelectedCompanyId(db,user,ct);var e = await db.RackBins.SingleOrDefaultAsync(x => x.CompanyId==companyId&&x.Id == id, ct); if (e is null) return Results.NotFound(new { message = "Rack/bin not found." });if(route=="deactivate"){var balance=await db.StockMovements.Where(x=>x.CompanyId==companyId&&x.RackBinId==e.Id).SumAsync(x=>(decimal?)(x.QuantityIn-x.QuantityOut),ct)??0m;if(balance!=0m)return Results.Conflict(new{message=$"Rack/bin cannot be deactivated while its current stock balance is {balance}. Transfer or issue the stock first."});if(await db.WarehouseConditionLocations.AnyAsync(x=>x.CompanyId==companyId&&x.RackBinId==e.Id&&x.IsActive&&x.EffectiveTo==null,ct))return Results.Conflict(new{message="Rack/bin cannot be deactivated while an open condition-location version uses it. Close that mapping first."});}return await MasterEndpointHelpers.ChangeLifecycleAsync(db, audit, user, e, nameof(RackBin), e.BinCode, action, status, approval, r.Remarks, r.Version, (x, s, actor) => { x.Status = s; x.IsActive = s != MasterStatuses.Inactive; if (s == MasterStatuses.Active) { x.ApprovedBy = actor; x.ApprovedAt = DateTimeOffset.UtcNow; } }, x => x.Status, x => x.ApprovalStatus, (x, s) => x.ApprovalStatus = s, ct); }).RequirePagePermission("masters.rack-bins", permission);

    private static async Task<Guid> SelectedCompanyId(NexaErpDbContext db,ICurrentUser user,CancellationToken ct)=>await db.Companies.Where(x=>x.Code==user.OrganizationId&&x.IsActive&&x.Status=="ACTIVE").Select(x=>x.Id).SingleAsync(ct);
    private static async Task<Guid?> WarehouseId(NexaErpDbContext db,ICurrentUser user,string code,CancellationToken ct){var companyId=await SelectedCompanyId(db,user,ct);return await db.Warehouses.AsNoTracking().Where(x=>x.CompanyId==companyId&&x.WarehouseCode==MasterEndpointHelpers.NormalizeCode(code)).Select(x=>(Guid?)x.Id).SingleOrDefaultAsync(ct);}
    private static async Task<bool> OwnsRack(NexaErpDbContext db,ICurrentUser user,Guid id,CancellationToken ct){var companyId=await SelectedCompanyId(db,user,ct);return await db.RackBins.AsNoTracking().AnyAsync(x=>x.CompanyId==companyId&&x.Id==id,ct);}
    private static IResult MasterDataFailure(Exception ex)=>ex switch{MasterDataNotFoundException=>Results.NotFound(new{message=ex.Message}),MasterDataConflictException=>Results.Conflict(new{message=ex.Message}),MasterDataValidationException=>Results.BadRequest(new{message=ex.Message}),_=>throw ex};

    private static async Task<IResult?> ValidateItem(UpsertItemRequest r, NexaErpDbContext db, Guid? id, CancellationToken ct)
    {
        var code = MasterEndpointHelpers.NormalizeCode(r.ItemCode); var barcode = MasterEndpointHelpers.NormalizeUpperOptional(r.Barcode);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Uom) || string.IsNullOrWhiteSpace(r.DetailedDescription) || string.IsNullOrWhiteSpace(r.MaterialType) || string.IsNullOrWhiteSpace(r.ItemType)) return Results.BadRequest(new { message = "Item code, name, description, material type, item type and UOM are required." });
        var itemType = r.ItemType.Trim().ToUpperInvariant();
        if (!ItemTypes.All.Contains(itemType)) return Results.BadRequest(new { message = "Item type is not supported." });
        if (r.IsReturnable != (itemType == ItemTypes.Tool)) return Results.BadRequest(new { message = "Only TOOL items must be returnable." });
        if (r.MinimumStock < 0) return Results.BadRequest(new { message = "Minimum stock cannot be negative." });
        if (r.MaximumStock < r.MinimumStock) return Results.BadRequest(new { message = "Maximum stock must not be below minimum stock." });
        if (r.ReorderLevel < 0 || r.ReorderLevel > r.MaximumStock) return Results.BadRequest(new { message = "Reorder level must be within valid range." });
        if (r.GstPercentage < 0 || r.GstPercentage > 100) return Results.BadRequest(new { message = "Legacy GST display percentage must be a valid percentage; effective tax is configuration-resolved." });
        if (r.CategoryId == Guid.Empty || !await db.ItemCategories.AnyAsync(x => x.Id == r.CategoryId && x.IsActive, ct)) return Results.BadRequest(new { message = "An active item category is required." });
        if (r.SubcategoryId.HasValue && !await db.ItemSubcategories.AnyAsync(x => x.Id == r.SubcategoryId.Value && x.CategoryId == r.CategoryId && x.IsActive, ct)) return Results.BadRequest(new { message = "Item subcategory must be active and belong to the selected category." });
        var uomCode = MasterEndpointHelpers.NormalizeCode(r.Uom);
        if (!await db.Uoms.AnyAsync(x => x.Code == uomCode && x.IsActive && x.QuantityPrecision == 6 && x.MeasurementDimension != string.Empty, ct)) return Results.BadRequest(new { message = "Active canonical Base UOM with measurement dimension and six-decimal precision is required." });
        var make = MasterEndpointHelpers.NormalizeOptional(r.ManufacturerMake); var model = MasterEndpointHelpers.NormalizeOptional(r.Model); var part = MasterEndpointHelpers.NormalizeOptional(r.PartNumber);
        if (await db.Items.AnyAsync(x => x.Id != id && (x.ItemCode == code || (barcode != null && x.Barcode == barcode) || (x.Name == r.Name.Trim() && x.ManufacturerMake == make && x.Model == model && x.PartNumber == part)), ct)) return Results.Conflict(new { message = "Duplicate item identity blocked." });
        if (!string.IsNullOrWhiteSpace(r.PreferredVendorCode))
        {
            var vendorCode = MasterEndpointHelpers.NormalizeCode(r.PreferredVendorCode);
            if (!await db.Vendors.AnyAsync(x => x.VendorCode == vendorCode && x.IsActive && x.VendorStatus == MasterStatuses.Active, ct)) return Results.BadRequest(new { message = "Preferred vendor must identify one active vendor." });
        }
        return null;
    }

    private static async Task ApplyItemRelationships(Item item, UpsertItemRequest request, NexaErpDbContext db, CancellationToken ct)
    {
        var baseUom = await db.Uoms.SingleAsync(x => x.Code == item.Uom && x.IsActive, ct);
        var category = await db.ItemCategories.SingleAsync(x => x.Id == request.CategoryId && x.IsActive, ct);
        var subcategory = request.SubcategoryId.HasValue
            ? await db.ItemSubcategories.SingleAsync(x => x.Id == request.SubcategoryId.Value && x.CategoryId == request.CategoryId && x.IsActive, ct)
            : null;
        var preferredVendor = !string.IsNullOrWhiteSpace(request.PreferredVendorCode)
            ? await db.Vendors.SingleAsync(x => x.VendorCode == MasterEndpointHelpers.NormalizeCode(request.PreferredVendorCode) && x.IsActive && x.VendorStatus == MasterStatuses.Active, ct)
            : null;

        item.UomId = baseUom.Id; item.UomMaster = baseUom; item.BaseUomId = baseUom.Id; item.BaseUom = baseUom;
        item.CategoryId = category.Id; item.Category = category;
        item.SubcategoryId = subcategory?.Id; item.Subcategory = subcategory;
        item.PreferredVendorId = preferredVendor?.Id; item.PreferredVendor = preferredVendor;
    }

    private static async Task<IResult?> ValidateWarehouse(UpsertWarehouseRequest r, NexaErpDbContext db, Guid? id, CancellationToken ct)
    { var code = MasterEndpointHelpers.NormalizeCode(r.WarehouseCode); if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.WarehouseType)) return Results.BadRequest(new { message = "Warehouse code, name and type are required." }); if (await db.Warehouses.AnyAsync(x => x.Id != id && x.WarehouseCode == code, ct)) return Results.Conflict(new { message = "Duplicate warehouse code blocked." }); return null; }

    private static async Task<IResult?> ValidateRackBin(UpsertRackBinRequest r, NexaErpDbContext db, Guid? id, CancellationToken ct)
    { var wh = MasterEndpointHelpers.NormalizeCode(r.WarehouseCode); var bin = MasterEndpointHelpers.NormalizeCode(r.BinCode); if (string.IsNullOrWhiteSpace(wh) || string.IsNullOrWhiteSpace(bin) || string.IsNullOrWhiteSpace(r.RackName) || string.IsNullOrWhiteSpace(r.BinNameNumber) || string.IsNullOrWhiteSpace(r.LocationType) || string.IsNullOrWhiteSpace(r.MaterialCondition)) return Results.BadRequest(new { message = "Warehouse, bin code, rack, bin number, location type and condition are required." }); var w = await db.Warehouses.SingleOrDefaultAsync(x => x.WarehouseCode == wh && x.IsActive, ct); if (w is null) return Results.BadRequest(new { message = "Active warehouse not found." }); if (!InventoryConditionCodes.All.Contains(MasterEndpointHelpers.NormalizeCode(r.MaterialCondition), StringComparer.OrdinalIgnoreCase)) return Results.BadRequest(new { message = "Rack/Bin material condition must use an approved inventory condition code." }); if (await db.RackBins.AnyAsync(x => x.Id != id && x.WarehouseId == w.Id && x.BinCode == bin, ct)) return Results.Conflict(new { message = "Duplicate rack/bin within warehouse blocked." }); return null; }

    private static void Apply(Item x, UpsertItemRequest r, string login, bool create)
    { x.ItemCode = MasterEndpointHelpers.NormalizeCode(r.ItemCode); x.Name = r.Name.Trim(); x.DetailedDescription = r.DetailedDescription.Trim(); x.MaterialType = r.MaterialType.Trim(); x.ItemType = r.ItemType.Trim().ToUpperInvariant(); x.IsReturnable = r.IsReturnable; x.Uom = r.Uom.Trim().ToUpperInvariant(); x.ManufacturerMake = MasterEndpointHelpers.NormalizeOptional(r.ManufacturerMake); x.Model = MasterEndpointHelpers.NormalizeOptional(r.Model); x.PartNumber = MasterEndpointHelpers.NormalizeOptional(r.PartNumber); x.HsnSacCode = MasterEndpointHelpers.NormalizeUpperOptional(r.HsnSacCode); x.GstPercentage = r.GstPercentage; x.TechnicalSpecification = MasterEndpointHelpers.NormalizeOptional(r.TechnicalSpecification); x.DrawingDocumentReference = MasterEndpointHelpers.NormalizeOptional(r.DrawingDocumentReference); x.QcRequired = r.QcRequired; x.SerialNumberTracking = r.SerialNumberTracking; x.BatchTracking = r.BatchTracking; x.ShelfLifeTracking = r.ShelfLifeTracking; x.MinimumStock = r.MinimumStock; x.MaximumStock = r.MaximumStock; x.ReorderLevel = r.ReorderLevel; x.StandardEstimatedPrice = r.StandardEstimatedPrice; x.Barcode = MasterEndpointHelpers.NormalizeUpperOptional(r.Barcode); x.BarcodeSymbology = MasterEndpointHelpers.NormalizeOptional(r.BarcodeSymbology); x.ImageStorageKey = MasterEndpointHelpers.NormalizeOptional(r.ImageStorageKey); x.ImageFileName = MasterEndpointHelpers.NormalizeOptional(r.ImageFileName); x.ImageContentType = MasterEndpointHelpers.NormalizeOptional(r.ImageContentType); if (create) x.CreatedBy = login; else { x.UpdatedBy = login; x.UpdatedAt = DateTimeOffset.UtcNow; } }

    private static async Task Apply(Warehouse x, UpsertWarehouseRequest r, NexaErpDbContext db, string login, bool create, CancellationToken ct)
    { x.WarehouseCode = MasterEndpointHelpers.NormalizeCode(r.WarehouseCode); x.Name = r.Name.Trim(); x.WarehouseType = r.WarehouseType.Trim(); x.Location = MasterEndpointHelpers.NormalizeOptional(r.Location); x.DefaultReceivingLocationId = r.DefaultReceivingLocationId; x.DefaultAcceptedLocationId = r.DefaultAcceptedLocationId; x.DefaultQcHoldLocationId = r.DefaultQcHoldLocationId; x.DefaultRejectedLocationId = r.DefaultRejectedLocationId; x.DefaultRepairableLocationId = r.DefaultRepairableLocationId; x.DefaultScrapLocationId = r.DefaultScrapLocationId; if (!string.IsNullOrWhiteSpace(r.ResponsibleEmployeeCode)) x.ResponsibleEmployeeId = await db.Employees.Where(e => e.EmployeeCode == MasterEndpointHelpers.NormalizeCode(r.ResponsibleEmployeeCode)).Select(e => e.Id).SingleOrDefaultAsync(ct); if (!string.IsNullOrWhiteSpace(r.DepartmentCode)) x.DepartmentId = await db.Departments.Where(d => d.Code == MasterEndpointHelpers.NormalizeCode(r.DepartmentCode)).Select(d => d.Id).SingleOrDefaultAsync(ct); if (create) x.CreatedBy = login; else { x.UpdatedBy = login; x.UpdatedAt = DateTimeOffset.UtcNow; } }

    private static void Apply(RackBin x, UpsertRackBinRequest r, string login, bool create)
    { x.BinCode = MasterEndpointHelpers.NormalizeCode(r.BinCode); x.RackName = r.RackName.Trim(); x.BinNameNumber = r.BinNameNumber.Trim(); x.Zone = MasterEndpointHelpers.NormalizeOptional(r.Zone); x.LocationType = r.LocationType.Trim(); x.MaterialCondition = MasterEndpointHelpers.NormalizeCode(r.MaterialCondition); x.CapacityQuantity = r.CapacityQuantity; x.CapacityUom = MasterEndpointHelpers.NormalizeOptional(r.CapacityUom); x.Barcode = MasterEndpointHelpers.NormalizeUpperOptional(r.Barcode); x.Description = MasterEndpointHelpers.NormalizeOptional(r.Description); if (create) x.CreatedBy = login; else { x.UpdatedBy = login; x.UpdatedAt = DateTimeOffset.UtcNow; } }

    private static void AddInitialStatus(NexaErpDbContext db, string type, Guid id, string code, string status, string user) => db.MasterStatusHistories.Add(new MasterStatusHistory { MasterType = type, MasterId = id, MasterCode = code, PreviousStatus = null, NewStatus = status, Reason = "REV867 draft created", SourceRevision = "REV867", CorrelationId = $"REV867_{type.ToUpperInvariant()}_CREATE_{Guid.NewGuid():N}", CreatedBy = user });
    private static IQueryable<T> Sort<T>(IQueryable<T> q, string? sortBy, string? dir, System.Linq.Expressions.Expression<Func<T, string>> code, System.Linq.Expressions.Expression<Func<T, string>> name, System.Linq.Expressions.Expression<Func<T, string>> status) => (sortBy?.Trim().ToLowerInvariant(), dir?.Trim().ToLowerInvariant()) switch { ("name", "desc") => q.OrderByDescending(name), ("name", _) => q.OrderBy(name), ("status", "desc") => q.OrderByDescending(status), ("status", _) => q.OrderBy(status), ("code", "desc") => q.OrderByDescending(code), _ => q.OrderBy(code) };
    private static ItemDetail ToDetail(Item x) => new(x.Id, x.ItemCode, x.Name, x.DetailedDescription, x.CategoryId, x.Category?.Code, x.Category?.Name, x.SubcategoryId, x.Subcategory?.Code, x.Subcategory?.Name, x.MaterialType, x.ItemType, x.IsReturnable, x.Uom, x.ManufacturerMake, x.Model, x.PartNumber, x.HsnSacCode, x.GstPercentage, x.TechnicalSpecification, x.DrawingDocumentReference, x.QcRequired, x.SerialNumberTracking, x.BatchTracking, x.ShelfLifeTracking, x.MinimumStock, x.MaximumStock, x.ReorderLevel, x.PreferredVendor?.VendorCode, x.StandardEstimatedPrice, x.Barcode, x.BarcodeSymbology, x.ImageStorageKey, x.ImageFileName, x.ImageContentType, x.Status, x.ApprovalStatus, x.IsActive, x.Version);
    private static WarehouseDetail ToDetail(Warehouse x) => new(x.Id, x.WarehouseCode, x.Name, x.WarehouseType, x.Location, x.ResponsibleEmployee?.EmployeeCode, x.Department?.Name, x.DefaultReceivingLocationId, x.DefaultAcceptedLocationId, x.DefaultQcHoldLocationId, x.DefaultRejectedLocationId, x.DefaultRepairableLocationId, x.DefaultScrapLocationId, x.Status, x.ApprovalStatus, x.IsActive, x.Version);
    private static RackBinDetail ToDetail(RackBin x) => new(x.Id, x.WarehouseId, x.Warehouse?.WarehouseCode ?? string.Empty, x.BinCode, x.RackName, x.BinNameNumber, x.Zone, x.LocationType, x.MaterialCondition, x.CapacityQuantity, x.CapacityUom, x.Barcode, x.Description, x.Status, x.ApprovalStatus, x.IsActive, x.Version);
}
