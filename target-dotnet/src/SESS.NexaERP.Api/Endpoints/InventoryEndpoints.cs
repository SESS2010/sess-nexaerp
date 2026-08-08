using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/items", async (NexaErpDbContext db, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            var paging = Paging.Normalize(page, pageSize);
            var items = await db.Items
                .AsNoTracking()
                .OrderBy(item => item.ItemCode)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .Select(item => new ItemSummary(item.Id, item.ItemCode, item.Name, item.Uom, item.Barcode, item.ImageStorageKey, item.MinimumStock, item.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(items);
        });

        group.MapPost("/items", async (CreateItemRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var code = NormalizeCode(request.ItemCode);
            var barcode = NormalizeOptional(request.Barcode);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Uom))
            {
                return Results.BadRequest(new { message = "Item code, name and UOM are required." });
            }

            if (request.MinimumStock < 0)
            {
                return Results.BadRequest(new { message = "Minimum stock cannot be negative." });
            }

            if (await db.Items.AnyAsync(item => item.ItemCode == code || (barcode != null && item.Barcode == barcode), cancellationToken))
            {
                return Results.Conflict(new { message = "Duplicate item code/barcode blocked." });
            }

            var item = new Item
            {
                ItemCode = code,
                Name = request.Name.Trim(),
                Uom = request.Uom.Trim().ToUpperInvariant(),
                Barcode = barcode,
                ImageStorageKey = NormalizeOptionalKeepCase(request.ImageStorageKey),
                MinimumStock = request.MinimumStock
            };

            db.Items.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Inventory", "Create", nameof(Item), item.Id.ToString(), null, item, cancellationToken);

            return Results.Created($"/api/v1/inventory/items/{item.Id}", new ItemSummary(item.Id, item.ItemCode, item.Name, item.Uom, item.Barcode, item.ImageStorageKey, item.MinimumStock, item.IsActive));
        });

        group.MapGet("/warehouses", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var warehouses = await db.Warehouses
                .AsNoTracking()
                .OrderBy(warehouse => warehouse.WarehouseCode)
                .Select(warehouse => new WarehouseSummary(warehouse.Id, warehouse.WarehouseCode, warehouse.Name, warehouse.Location, warehouse.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(warehouses);
        });

        group.MapPost("/warehouses", async (CreateWarehouseRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var code = NormalizeCode(request.WarehouseCode);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Warehouse code and name are required." });
            }

            if (await db.Warehouses.AnyAsync(warehouse => warehouse.WarehouseCode == code, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate warehouse code blocked: {code}" });
            }

            var warehouse = new Warehouse
            {
                WarehouseCode = code,
                Name = request.Name.Trim(),
                Location = NormalizeOptionalKeepCase(request.Location)
            };

            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Inventory", "Create", nameof(Warehouse), warehouse.Id.ToString(), null, warehouse, cancellationToken);

            return Results.Created($"/api/v1/inventory/warehouses/{warehouse.Id}", new WarehouseSummary(warehouse.Id, warehouse.WarehouseCode, warehouse.Name, warehouse.Location, warehouse.IsActive));
        });

        group.MapPost("/rack-bins", async (CreateRackBinRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var warehouseCode = NormalizeCode(request.WarehouseCode);
            var binCode = NormalizeCode(request.BinCode);
            if (string.IsNullOrWhiteSpace(warehouseCode) || string.IsNullOrWhiteSpace(binCode))
            {
                return Results.BadRequest(new { message = "Warehouse code and bin code are required." });
            }

            var warehouse = await db.Warehouses.SingleOrDefaultAsync(existing => existing.WarehouseCode == warehouseCode && existing.IsActive, cancellationToken);
            if (warehouse is null)
            {
                return Results.BadRequest(new { message = $"Active warehouse not found: {warehouseCode}" });
            }

            if (await db.RackBins.AnyAsync(bin => bin.WarehouseId == warehouse.Id && bin.BinCode == binCode, cancellationToken))
            {
                return Results.Conflict(new { message = $"Duplicate rack/bin blocked for warehouse {warehouseCode}: {binCode}" });
            }

            var rackBin = new RackBin
            {
                WarehouseId = warehouse.Id,
                BinCode = binCode,
                Description = NormalizeOptionalKeepCase(request.Description)
            };

            db.RackBins.Add(rackBin);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Inventory", "Create", nameof(RackBin), rackBin.Id.ToString(), null, rackBin, cancellationToken);

            return Results.Created($"/api/v1/inventory/rack-bins/{rackBin.Id}", new RackBinSummary(rackBin.Id, warehouse.Id, warehouse.WarehouseCode, rackBin.BinCode, rackBin.Description, rackBin.IsActive));
        });

        return endpoints;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeOptionalKeepCase(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
