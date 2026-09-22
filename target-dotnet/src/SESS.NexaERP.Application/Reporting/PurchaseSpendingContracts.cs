namespace SESS.NexaERP.Application.Reporting;

public sealed record PurchaseSpendingRequest(string Period = "financial-year", DateOnly? Month = null,
    Guid? VendorId = null, Guid? CategoryId = null, string? Currency = null, Guid? BillId = null,
    int Page = 1, int PageSize = 100);
public sealed record PurchaseSpendingAmount(string Currency, decimal MaterialValue,
    decimal AllocatedCharges, decimal Amount, long PoCount, long BillCount);
public sealed record PurchaseSpendingPeriod(string Key, DateOnly FromDate, DateOnly ToDate,
    IReadOnlyList<PurchaseSpendingAmount> Amounts);
public sealed record PurchaseSpendingGroup(Guid Id, string Code, string Name,
    string Currency, decimal MaterialValue, decimal AllocatedCharges, decimal Amount,
    long PoCount, long BillCount);
public sealed record PurchaseSpendingRow(Guid BillId, Guid BillLineId, string BillNumber,
    string Event, DateOnly EventDate, Guid PurchaseOrderId, Guid RootPurchaseOrderId,
    string PoNumber, Guid VendorId, string VendorCode, string VendorName,
    Guid CategoryId, string CategoryCode, Guid ItemId, string ItemCode, string ItemName,
    string Uom, decimal Quantity, string Currency, decimal MaterialValue,
    decimal AllocatedCharges, decimal Amount);
public sealed record PurchaseSpendingPage(string CompanyCode, DateTimeOffset GeneratedAt,
    string TimeZone, string Basis, IReadOnlyList<PurchaseSpendingPeriod> Periods,
    IReadOnlyList<PurchaseSpendingGroup> TopVendors, IReadOnlyList<PurchaseSpendingGroup> Categories,
    IReadOnlyList<PurchaseSpendingPeriod> MonthlyTrend, DateOnly FromDate, DateOnly ToDate,
    PurchaseSpendingRequest Filters, long TotalRows, IReadOnlyList<PurchaseSpendingRow> Rows);
public interface IPurchaseSpendingService
{
    Task<PurchaseSpendingPage> GetAsync(PurchaseSpendingRequest request, CancellationToken ct);
}