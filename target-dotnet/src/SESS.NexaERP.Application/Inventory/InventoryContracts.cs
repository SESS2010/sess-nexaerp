namespace SESS.NexaERP.Application.Inventory;

public sealed record ItemSummary(Guid Id, string ItemCode, string Name, string Uom, string? Barcode, string? ImageStorageKey, decimal MinimumStock, bool IsActive);

public sealed record CreateItemRequest(string ItemCode, string Name, string Uom, string? Barcode, string? ImageStorageKey, decimal MinimumStock);

public sealed record WarehouseSummary(Guid Id, string WarehouseCode, string Name, string? Location, bool IsActive);

public sealed record CreateWarehouseRequest(string WarehouseCode, string Name, string? Location);

public sealed record RackBinSummary(Guid Id, Guid WarehouseId, string WarehouseCode, string BinCode, string? Description, bool IsActive);

public sealed record CreateRackBinRequest(string WarehouseCode, string BinCode, string? Description);
