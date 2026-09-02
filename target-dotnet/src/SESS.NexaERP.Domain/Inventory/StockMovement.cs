using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class StockMovement : CompanyScopedAuditableEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public DateOnly PostingDate { get; set; }
    public short LedgerSchemaVersion { get; set; } = 2;
    public Guid? WarehouseConditionLocationId { get; set; }
    public WarehouseConditionLocation? WarehouseConditionLocation { get; set; }
    public string? ConditionCode { get; set; }
    public Guid? StockPostingBatchId { get; set; }
    public StockPostingBatch? StockPostingBatch { get; set; }
    public int? BatchLineOrdinal { get; set; }
    public string? MovementLeg { get; set; }
    public Guid? GoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? GoodsReceiptLine { get; set; }
    public Guid? QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid? MaterialIssueRequestLineId { get; set; }
    public MaterialIssueRequestLine? MaterialIssueRequestLine { get; set; }
    public Guid? DeliveryChallanLineId { get; set; }
    public DeliveryChallanLine? DeliveryChallanLine { get; set; }
    public Guid? OriginGoodsReceiptLineId { get; set; }
    public GoodsReceiptLine? OriginGoodsReceiptLine { get; set; }
    public Guid OwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? OwnershipAccount { get; set; }
    public Guid CustodyAssignmentId { get; set; }
    public InventoryCustodyAssignment? CustodyAssignment { get; set; }
    public Guid InventoryProvenanceLayerId { get; set; }
    public InventoryProvenanceLayer? InventoryProvenanceLayer { get; set; }
    public Guid? CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }
    public Guid? InventorySerialId { get; set; }
    public InventorySerial? InventorySerial { get; set; }
    public Guid? GoodsReceiptLineLotAllocationId { get; set; }
    public GoodsReceiptLineLotAllocation? GoodsReceiptLineLotAllocation { get; set; }
    public Guid? QcInspectionLotDispositionId { get; set; }
    public QcInspectionLotDisposition? QcInspectionLotDisposition { get; set; }
    public Guid? InventoryCustodyHandoffLineId { get; set; }
    public InventoryCustodyHandoffLine? InventoryCustodyHandoffLine { get; set; }
    public Guid? InventoryOwnershipTransferLineId { get; set; }
    public InventoryOwnershipTransferLine? InventoryOwnershipTransferLine { get; set; }
    public Guid? InventoryTransformationInputId { get; set; }
    public InventoryTransformationInput? InventoryTransformationInput { get; set; }
    public Guid? InventoryTransformationOutputId { get; set; }
    public InventoryTransformationOutput? InventoryTransformationOutput { get; set; }
    public Guid? InventoryConcessionAllocationId { get; set; }
    public InventoryConcessionAllocation? InventoryConcessionAllocation { get; set; }
    public Guid? ReversesStockMovementId { get; set; }
    public StockMovement? ReversesStockMovement { get; set; }
    public string? PostingIdentity { get; set; }
}
