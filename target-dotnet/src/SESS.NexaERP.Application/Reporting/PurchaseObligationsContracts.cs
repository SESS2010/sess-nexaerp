namespace SESS.NexaERP.Application.Reporting;

public sealed record PurchaseObligationsRequest(string? Queue = null, Guid? VendorId = null,
    string? Currency = null, Guid? DocumentId = null, int Page = 1, int PageSize = 100);
public sealed record PurchaseObligationAmount(string Currency, long DocumentCount, long LineCount,
    decimal Value, int? OldestAgeDays);
public sealed record PurchaseObligationTile(string Key, string Title, string Basis, long Count,
    int? OldestAgeDays, IReadOnlyList<PurchaseObligationAmount> Amounts);
public sealed record PurchaseObligationVendor(string Queue, Guid VendorId, string VendorCode,
    string VendorName, string Currency, long DocumentCount, decimal Value, int? OldestAgeDays);
public sealed record PurchaseObligationRow(string Queue, Guid DocumentId, string DocumentNumber,
    Guid? LineId, Guid PurchaseOrderId, Guid RootPurchaseOrderId, string PoNumber, Guid VendorId,
    string VendorCode, string VendorName, Guid? ItemId, string? ItemCode, string? ItemName,
    string? Uom, DateOnly SourceDate, int AgeDays, decimal? Quantity, string Currency,
    decimal Value, decimal? UnitRate, decimal? OriginalAmount, decimal? AdjustedAmount);
public sealed record PurchaseObligationsPage(string CompanyCode, DateTimeOffset GeneratedAt,
    string TimeZone, IReadOnlyList<PurchaseObligationTile> Tiles,
    IReadOnlyList<PurchaseObligationVendor> Vendors, PurchaseObligationsRequest Filters,
    long TotalRows, IReadOnlyList<PurchaseObligationRow> Rows);
public sealed class PurchaseObligationSourceException() : Exception(
    "Receipt or advance balances are inconsistent. Ask the administrator to reconcile the source documents.");
public interface IPurchaseObligationsService
{
    Task<PurchaseObligationsPage> GetAsync(PurchaseObligationsRequest request, CancellationToken ct);
}
