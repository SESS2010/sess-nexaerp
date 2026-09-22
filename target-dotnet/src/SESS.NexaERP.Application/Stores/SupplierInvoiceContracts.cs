using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record SupplierInvoicePurchaseOrderLineOption(
    Guid Id, int LineNumber, Guid ItemId, string ItemCode, string ItemName, string Uom,
    decimal OrderedQuantity, decimal UnitRate, decimal TotalPayableValue);
public sealed record SupplierInvoicePurchaseOrderOption(
    Guid Id, string PoNumber, int RevisionNumber, Guid VendorId, string VendorCode,
    string VendorName, string CurrencyCode, IReadOnlyList<SupplierInvoicePurchaseOrderLineOption> Lines);

public sealed record SupplierInvoiceLineInput(
    Guid PurchaseOrderLineId, decimal Quantity, decimal UnitRate, decimal PayableValue);
public sealed record SupplierInvoiceEvidenceInput(string FileName, string ContentType, byte[] Content);
public sealed record RecordSupplierInvoiceRequest(
    Guid PurchaseOrderId, string InvoiceNumber, DateOnly InvoiceDate, string CurrencyCode,
    IReadOnlyList<SupplierInvoiceLineInput> Lines, SupplierInvoiceEvidenceInput Evidence, string IdempotencyKey);
public sealed record CancelSupplierInvoiceRequest(long Version, string Reason, string IdempotencyKey);
public sealed record LinkSupplierInvoiceAcceptedBillRequest(
    long Version, Guid VendorBillId, string IdempotencyKey);
public sealed record SupplierInvoiceEvidence(
    string FileName, string ContentType, int SizeBytes, string Sha256,
    DateTimeOffset RecordedAt, Guid RecordedByEmployeeId);
public sealed record SupplierInvoiceContent(string FileName, string ContentType, byte[] Content, string Sha256);
public sealed record SupplierInvoiceLineView(
    Guid Id, int LineNumber, Guid PurchaseOrderLineId, Guid ItemId, string ItemCode, string ItemName, string Uom,
    decimal Quantity, decimal UnitRate, decimal PayableValue, decimal ReceivedQuantity, decimal OutstandingQuantity);
public sealed record SupplierInvoiceReceiptMatchView(
    Guid Id, Guid SupplierInvoiceLineId, Guid GoodsReceiptLineId, Guid ReceiptEventId,
    decimal Quantity, DateTimeOffset EffectiveAt, DateTimeOffset RecordedAt,
    Guid RecordedByEmployeeId, Guid? ReversesMatchId);
public sealed record SupplierInvoiceView(
    Guid Id, string InvoiceNumber, DateOnly InvoiceDate, Guid PurchaseOrderId, string PurchaseOrderNumber,
    Guid VendorId, string VendorCode, string VendorName, string CurrencyCode, string Status,
    long Version, bool Replayed, SupplierInvoiceEvidence Evidence,
    IReadOnlyList<SupplierInvoiceLineView> Lines, IReadOnlyList<SupplierInvoiceReceiptMatchView> ReceiptMatches,
    IReadOnlyList<Guid> AcceptedBillIds, string? CancellationReason);
public interface ISupplierInvoiceService
{
    Task<PagedResponse<SupplierInvoicePurchaseOrderOption>> ListPurchaseOrdersAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<SupplierInvoiceView> RecordAsync(RecordSupplierInvoiceRequest request, CancellationToken ct);
    Task<SupplierInvoiceView?> GetAsync(Guid id, CancellationToken ct);
    Task<SupplierInvoiceContent> DownloadAsync(Guid id, CancellationToken ct);
    Task<SupplierInvoiceView> LinkAcceptedBillAsync(Guid id, LinkSupplierInvoiceAcceptedBillRequest request, CancellationToken ct);
    Task<SupplierInvoiceView> CancelAsync(Guid id, CancelSupplierInvoiceRequest request, CancellationToken ct);
}
