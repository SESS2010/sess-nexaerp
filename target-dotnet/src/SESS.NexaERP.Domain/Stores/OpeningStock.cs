using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Stores;

public static class OpeningStockStatuses
{
    public const string Counted = "COUNTED";
    public const string Valued = "VALUED";
    public const string Posted = "POSTED";
}

public sealed class OpeningStockImportStagingLine : CompanyScopedAuditableEntity
{
    public string LineReference { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public string? VendorName { get; set; }
    public string? VendorBillNumber { get; set; }
    public DateOnly? BillDate { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? Remarks { get; set; }
}

public sealed class OpeningStock : CompanyScopedAuditableEntity
{
    public Guid ImportBatchId { get; set; }
    public MasterImportBatch? ImportBatch { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Status { get; set; } = OpeningStockStatuses.Counted;
    public Guid CountedByEmployeeId { get; set; }
    public Employee? CountedByEmployee { get; set; }
    public string CountActorRoleCode { get; set; } = string.Empty;
    public Guid CountRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? CountRoleAssignment { get; set; }
    public string CountRoleAssignmentType { get; set; } = string.Empty;
    public DateTimeOffset CountedAt { get; set; }
    public string CountReason { get; set; } = string.Empty;
    public string CountIdempotencyKey { get; set; } = string.Empty;
    public string CountRequestFingerprint { get; set; } = string.Empty;
    public Guid? ValuedByEmployeeId { get; set; }
    public Employee? ValuedByEmployee { get; set; }
    public string? ValueActorRoleCode { get; set; }
    public Guid? ValueRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ValueRoleAssignment { get; set; }
    public string? ValueRoleAssignmentType { get; set; }
    public DateTimeOffset? ValuedAt { get; set; }
    public string? ValueReason { get; set; }
    public string? ValueIdempotencyKey { get; set; }
    public string? ValueRequestFingerprint { get; set; }
    public Guid? AuthorizedByEmployeeId { get; set; }
    public Employee? AuthorizedByEmployee { get; set; }
    public string? AuthorizationActorRoleCode { get; set; }
    public Guid? AuthorizationRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? AuthorizationRoleAssignment { get; set; }
    public string? AuthorizationRoleAssignmentType { get; set; }
    public DateTimeOffset? AuthorizedAt { get; set; }
    public string? AuthorizationReason { get; set; }
    public string? AuthorizationIdempotencyKey { get; set; }
    public string? AuthorizationRequestFingerprint { get; set; }
    public Guid? StockPostingBatchId { get; set; }
    public StockPostingBatch? StockPostingBatch { get; set; }
    public List<OpeningStockLine> Lines { get; set; } = [];
}

public sealed class OpeningStockLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid OpeningStockId { get; set; }
    public OpeningStock? OpeningStock { get; set; }
    public Guid ImportStagingLineId { get; set; }
    public OpeningStockImportStagingLine? ImportStagingLine { get; set; }
    public int LineNumber { get; set; }
    public string LineReference { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public Guid WarehouseConditionLocationId { get; set; }
    public WarehouseConditionLocation? WarehouseConditionLocation { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal LineValue { get; set; }
    public string? VendorName { get; set; }
    public string? VendorBillNumber { get; set; }
    public DateOnly? BillDate { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? PartNumber { get; set; }
    public string? Remarks { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public Guid? InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public Guid? InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public Guid? FifoInventoryCostLayerId { get; set; }
    public FifoInventoryCostLayer? FifoInventoryCostLayer { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; } = "system";
}

public sealed class OpeningStockEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid OpeningStockId { get; set; }
    public OpeningStock? OpeningStock { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public Guid ResolvedRoleAssignmentId { get; set; }
    public EmployeeRoleAssignment? ResolvedRoleAssignment { get; set; }
    public string ResolvedRoleAssignmentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
