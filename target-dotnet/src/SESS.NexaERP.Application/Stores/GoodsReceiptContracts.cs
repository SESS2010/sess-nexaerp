namespace SESS.NexaERP.Application.Stores;

public sealed record GoodsReceiptLotRequest(
    int LotOrdinal,
    decimal Quantity,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate);

public sealed record GoodsReceiptSerialRequest(
    int SerialOrdinal,
    int LotOrdinal,
    string EnteredSerialNumber,
    string StoredSerialNumber,
    bool DuplicateWarningAcknowledged,
    string? DisambiguationReason);

public sealed record GoodsReceiptLineRequest(
    Guid GateEntryLineId,
    IReadOnlyList<GoodsReceiptLotRequest> Lots,
    IReadOnlyList<GoodsReceiptSerialRequest> Serials);

public sealed record CreateGoodsReceiptRequest(
    string GateEntryNumber,
    string VendorBillNumber,
    DateOnly VendorBillDate,
    DateTimeOffset ReceivedAt,
    string IsoReceiptVerificationJson,
    IReadOnlyList<GoodsReceiptLineRequest> Lines);

public sealed record UpdateGoodsReceiptRequest(
    string VendorBillNumber,
    DateOnly VendorBillDate,
    DateTimeOffset ReceivedAt,
    string IsoReceiptVerificationJson,
    IReadOnlyList<GoodsReceiptLineRequest> Lines,
    uint Version);

public sealed record FinalizeGoodsReceiptRequest(uint Version, string IdempotencyKey);

public sealed record ReverseGoodsReceiptRequest(uint Version, string Reason, string IdempotencyKey);

public sealed record GoodsReceiptLotResult(
    Guid Id,
    Guid InventoryLotId,
    int LotOrdinal,
    decimal Quantity,
    string? SupplierLotNumber,
    string? ManufacturerLotNumber,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate);

public sealed record GoodsReceiptSerialResult(
    Guid Id,
    Guid? InventorySerialId,
    int SerialOrdinal,
    int LotOrdinal,
    string EnteredSerialNumber,
    string StoredSerialNumber,
    bool DuplicateWarningAcknowledged,
    string? DisambiguationReason);

public sealed record GoodsReceiptLineResult(
    Guid Id,
    int LineNumber,
    Guid GateEntryLineId,
    Guid PurchaseOrderLineId,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string ItemCategoryCode,
    string HsnSacCode,
    decimal GstPercentage,
    string? Model,
    string? ManufacturerPartNumber,
    string Uom,
    decimal ReceivedQuantity,
    decimal UnitRate,
    string SerialCaptureMode,
    DateOnly WarrantyExpiryDate,
    Guid QcHoldConditionLocationId,
    IReadOnlyList<GoodsReceiptLotResult> Lots,
    IReadOnlyList<GoodsReceiptSerialResult> Serials);

public sealed record GoodsReceiptHistoryResult(
    string? FromStatus,
    string ToStatus,
    string Action,
    Guid ActorEmployeeId,
    string ActorRoleCode,
    DateTimeOffset OccurredAt);

public sealed record GoodsReceiptResult(
    Guid Id,
    string GrnNumber,
    string DocumentKind,
    Guid? ReversesGoodsReceiptId,
    string? ReversalReason,
    string GateEntryNumber,
    Guid GateEntryId,
    string PurchaseOrderNumber,
    Guid PurchaseOrderId,
    Guid VendorId,
    string VendorName,
    string VendorBillNumber,
    DateOnly VendorBillDate,
    string VendorDcNumber,
    string ModeOfTransport,
    DateTimeOffset ReceivedAt,
    string IsoReceiptVerificationJson,
    string Status,
    uint Version,
    Guid? StockPostingBatchId,
    bool Replayed,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<GoodsReceiptLineResult> Lines,
    IReadOnlyList<GoodsReceiptHistoryResult> History);

public sealed record GoodsReceiptListResult(int TotalCount, int PageNumber, int PageSize, IReadOnlyList<GoodsReceiptResult> Items);

public interface IGoodsReceiptService
{
    Task<GoodsReceiptResult> CreateAsync(CreateGoodsReceiptRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<GoodsReceiptResult> UpdateAsync(Guid id, UpdateGoodsReceiptRequest request, CancellationToken cancellationToken);
    Task<GoodsReceiptResult> FinalizeAsync(Guid id, FinalizeGoodsReceiptRequest request, CancellationToken cancellationToken);
    Task<GoodsReceiptResult> ReverseAsync(Guid id, ReverseGoodsReceiptRequest request, CancellationToken cancellationToken);
    Task<GoodsReceiptResult?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<GoodsReceiptListResult> ListAsync(string? grnNumber, string? gateEntryNumber, Guid? vendorId, string? status, int page, int pageSize, CancellationToken cancellationToken);
}
