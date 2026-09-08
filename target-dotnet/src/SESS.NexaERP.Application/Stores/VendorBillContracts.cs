namespace SESS.NexaERP.Application.Stores;

public sealed record VendorBillLineInput(Guid GoodsReceiptLineId, decimal Quantity,
    decimal UnitRate, decimal TotalPayableValue);
public sealed record CreateVendorBillRequest(string BillNumber, DateOnly BillDate,
    IReadOnlyList<VendorBillLineInput> Lines, string IdempotencyKey);
public sealed record VendorBillDecisionRequest(long Version, string Reason, string IdempotencyKey);
public sealed record VendorBillLineView(Guid Id, int LineNumber, Guid GoodsReceiptLineId,
    Guid PurchaseOrderLineId, Guid ItemId, decimal BilledQuantity, decimal ExpectedUnitRate,
    decimal BilledUnitRate, decimal ExpectedPayableValue, decimal BilledPayableValue,
    string MatchStatus);
public sealed record VendorBillView(Guid Id, string BillNumber, DateOnly BillDate,
    Guid GoodsReceiptId, Guid PurchaseOrderId, Guid VendorId, string Status, string MatchStatus,
    decimal TotalPayableValue, long Version, bool Replayed, string ActorRoleCode,
    Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    DateTimeOffset? DecidedAt, Guid? DecidedByEmployeeId, string? DecisionReason,
    DateTimeOffset? ReversedAt, Guid? ReversedByEmployeeId, string? ReversalReason,
    IReadOnlyList<VendorBillLineView> Lines);
public sealed record VendorBillPage(int Total, int Page, int PageSize, IReadOnlyList<VendorBillView> Items);

public interface IVendorBillService
{
    Task<VendorBillPage> ListAsync(string? billNumber, string? status, Guid? vendorId,
        int page, int pageSize, CancellationToken ct);
    Task<VendorBillView?> GetAsync(Guid id, CancellationToken ct);
    Task<VendorBillView> CreateAsync(Guid goodsReceiptId, CreateVendorBillRequest request, CancellationToken ct);
    Task<VendorBillView> AcceptAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct);
    Task<VendorBillView> RejectAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct);
    Task<VendorBillView> ReverseAsync(Guid id, VendorBillDecisionRequest request, CancellationToken ct);
}