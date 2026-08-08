using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Inventory;

public sealed record ItemSummary(Guid Id, string ItemCode, string Name, string Uom, string? Category, string? ManufacturerMake, string? Model, string? PartNumber, decimal MinimumStock, decimal MaximumStock, decimal ReorderLevel, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record ItemDetail(Guid Id, string ItemCode, string Name, string DetailedDescription, string MaterialType, string Uom, string? ManufacturerMake, string? Model, string? PartNumber, string? HsnSacCode, decimal GstPercentage, string? TechnicalSpecification, string? DrawingDocumentReference, bool QcRequired, bool SerialNumberTracking, bool BatchTracking, bool ShelfLifeTracking, decimal MinimumStock, decimal MaximumStock, decimal ReorderLevel, string? PreferredVendorCode, decimal? StandardEstimatedPrice, string? Barcode, string? BarcodeSymbology, string? ImageStorageKey, string? ImageFileName, string? ImageContentType, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record UpsertItemRequest(string ItemCode, string Name, string DetailedDescription, string MaterialType, string Uom, string? ManufacturerMake, string? Model, string? PartNumber, string? HsnSacCode, decimal GstPercentage, string? TechnicalSpecification, string? DrawingDocumentReference, bool QcRequired, bool SerialNumberTracking, bool BatchTracking, bool ShelfLifeTracking, decimal MinimumStock, decimal MaximumStock, decimal ReorderLevel, string? PreferredVendorCode, decimal? StandardEstimatedPrice, string? Barcode, string? BarcodeSymbology, string? ImageStorageKey, string? ImageFileName, string? ImageContentType, uint? Version);

public sealed record WarehouseSummary(Guid Id, string WarehouseCode, string Name, string WarehouseType, string? Location, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record WarehouseDetail(Guid Id, string WarehouseCode, string Name, string WarehouseType, string? Location, string? ResponsibleEmployeeCode, string? Department, Guid? DefaultReceivingLocationId, Guid? DefaultAcceptedLocationId, Guid? DefaultQcHoldLocationId, Guid? DefaultRejectedLocationId, Guid? DefaultRepairableLocationId, Guid? DefaultScrapLocationId, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record UpsertWarehouseRequest(string WarehouseCode, string Name, string WarehouseType, string? Location, string? ResponsibleEmployeeCode, string? DepartmentCode, Guid? DefaultReceivingLocationId, Guid? DefaultAcceptedLocationId, Guid? DefaultQcHoldLocationId, Guid? DefaultRejectedLocationId, Guid? DefaultRepairableLocationId, Guid? DefaultScrapLocationId, uint? Version);

public sealed record RackBinSummary(Guid Id, Guid WarehouseId, string WarehouseCode, string BinCode, string RackName, string BinNameNumber, string? Zone, string LocationType, string MaterialCondition, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record RackBinDetail(Guid Id, Guid WarehouseId, string WarehouseCode, string BinCode, string RackName, string BinNameNumber, string? Zone, string LocationType, string MaterialCondition, decimal? CapacityQuantity, string? CapacityUom, string? Barcode, string? Description, string Status, string ApprovalStatus, bool IsActive, uint Version);

public sealed record UpsertRackBinRequest(string WarehouseCode, string BinCode, string RackName, string BinNameNumber, string? Zone, string LocationType, string MaterialCondition, decimal? CapacityQuantity, string? CapacityUom, string? Barcode, string? Description, uint? Version);
