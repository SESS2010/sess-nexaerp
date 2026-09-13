using System.Text.Json.Serialization;

namespace SESS.NexaERP.Application.Stores;

public sealed record RecordVendorAdvanceRequest(
    Guid PurchaseOrderId, DateOnly PaidDate, decimal Amount, string CurrencyCode,
    string PaymentReference, string EvidenceObjectKey, string IdempotencyKey);

public sealed record ReverseVendorAdvanceRequest(string Reason, string IdempotencyKey);

public sealed record VendorAdvancePurchaseOrderOption(
    Guid PurchaseOrderId, string PurchaseOrderNumber, Guid VendorId,
    string VendorCode, string VendorName, string CurrencyCode, decimal PurchaseOrderValue,
    decimal ActiveAdvanceAmount, decimal AvailableAdvanceAmount,
    [property: JsonRequired] decimal BillPaymentAmount);

public sealed record VendorAdvanceView(
    Guid Id, string AdvanceNumber, Guid PurchaseOrderId, string PurchaseOrderNumber,
    Guid VendorId, string VendorCode, string VendorName, DateOnly PaidDate, decimal Amount,
    string CurrencyCode, string PaymentReference, string EvidenceObjectKey,
    decimal AdjustedAmount, decimal OutstandingAmount, bool IsReversed,
    bool PurchaseOrderCancelled, DateTimeOffset CreatedAt, bool Replayed);

public sealed record VendorAdvancePage(
    int Total, int Page, int PageSize, IReadOnlyList<VendorAdvanceView> Items);

public sealed record VendorPaymentAllocationInput(Guid VendorBillId, decimal Amount);

public sealed record RecordVendorPaymentRequest(
    Guid VendorId, DateOnly PaidDate, decimal Amount, string CurrencyCode,
    string PaymentReference, string EvidenceObjectKey,
    IReadOnlyList<VendorPaymentAllocationInput> Allocations, string IdempotencyKey);

public sealed record VendorPaymentAllocationView(
    Guid VendorBillId, string BillNumber, decimal AcceptedValue,
    decimal AdvanceAdjustedValue, decimal PreviouslyPaidValue, decimal Amount);

public sealed record VendorPaymentView(
    Guid Id, string PaymentNumber, Guid VendorId, string VendorCode, string VendorName,
    DateOnly PaidDate, decimal Amount, string CurrencyCode, string PaymentReference,
    string EvidenceObjectKey, DateTimeOffset CreatedAt, bool Replayed,
    IReadOnlyList<VendorPaymentAllocationView> Allocations);

public sealed record VendorPaymentPage(
    int Total, int Page, int PageSize, IReadOnlyList<VendorPaymentView> Items);

public sealed record VendorPayableView(
    Guid VendorBillId, string BillNumber, Guid PurchaseOrderId, string PurchaseOrderNumber,
    Guid VendorId, string VendorCode, string VendorName, DateOnly BillDate,
    DateTimeOffset AcceptedAt, string PaymentTerms, DateOnly? DueDate,
    decimal AcceptedValue, decimal AdvanceAdjustedValue, decimal PaidValue,
    decimal OutstandingValue, bool IsOverdue,
    [property: JsonRequired] string CurrencyCode);

public sealed record VendorPositionView(
    Guid VendorId, string VendorCode, string VendorName,
    decimal OutstandingAdvance, decimal OutstandingBills, decimal NetPayable,
    int CancelledPurchaseOrdersWithOutstandingAdvance,
    [property: JsonRequired] string CurrencyCode);

public interface IVendorFinancialEvidenceService
{
    Task<IReadOnlyList<VendorAdvancePurchaseOrderOption>> ListAdvancePurchaseOrdersAsync(
        Guid? vendorId, CancellationToken ct);
    Task<VendorAdvancePage> ListAdvancesAsync(Guid? vendorId, Guid? purchaseOrderId,
        bool? outstandingOnly, int page, int pageSize, CancellationToken ct);
    Task<VendorAdvanceView> RecordAdvanceAsync(RecordVendorAdvanceRequest request, CancellationToken ct);
    Task<VendorAdvanceView> ReverseAdvanceAsync(Guid id, ReverseVendorAdvanceRequest request, CancellationToken ct);
    Task<VendorPaymentPage> ListPaymentsAsync(Guid? vendorId, int page, int pageSize, CancellationToken ct);
    Task<VendorPaymentView> RecordPaymentAsync(RecordVendorPaymentRequest request, CancellationToken ct);
    Task<IReadOnlyList<VendorPayableView>> ListPayablesAsync(Guid? vendorId, bool overdueOnly, CancellationToken ct);
    Task<IReadOnlyList<VendorPositionView>> ListVendorPositionsAsync(CancellationToken ct);
}