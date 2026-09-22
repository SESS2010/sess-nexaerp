namespace SESS.NexaERP.Application.Reporting;

public sealed record PurchaseOpenOrdersRequest(Guid? VendorId = null, string? Currency = null,
    Guid? RootPurchaseOrderId = null, bool OverdueOnly = false, int Page = 1, int PageSize = 100);
public sealed record PurchaseOpenOrderAmount(string Currency, long PoCount, decimal Value,
    long? OverduePoCount, decimal? OverdueValue);
public sealed record PurchaseOpenOrderIssue(Guid RootPurchaseOrderId, string PoNumber, string Code);
public sealed record PurchaseOpenOrderRow(Guid PurchaseOrderId, Guid RootPurchaseOrderId,
    string PoNumber, int RevisionNumber, int CurrentRevisionNumber, string CurrentStatus,
    DateTimeOffset FirstIssuedAt, DateTimeOffset IssuedAt, int AgeDays,
    Guid VendorId, string VendorCode, string VendorName, Guid LineId, Guid ItemId,
    string ItemCode, string ItemName, string Uom, decimal OrderedQuantity,
    decimal ReceivedQuantity, decimal RemainingQuantity, string Currency, decimal Value,
    DateOnly QuotedDeliveryDate, DateOnly? CommittedDeliveryDate, string DeliveryTerms,
    int? DaysLate, string DeliveryState);
public sealed record PurchaseOpenOrdersPage(string CompanyCode, DateTimeOffset GeneratedAt,
    string TimeZone, string Basis, bool Complete, long? OpenPoCount, int? OldestAgeDays,
    bool DeliveryComplete, long? OverduePoCount, long DeliveryDateUnconfirmedPoCount,
    IReadOnlyList<PurchaseOpenOrderIssue> SourceIssues, IReadOnlyList<PurchaseOpenOrderAmount>? Amounts,
    PurchaseOpenOrdersRequest Filters, long TotalRows, IReadOnlyList<PurchaseOpenOrderRow> Rows);
public interface IPurchaseOpenOrdersService
{
    Task<PurchaseOpenOrdersPage> GetAsync(PurchaseOpenOrdersRequest request, CancellationToken ct);
}
