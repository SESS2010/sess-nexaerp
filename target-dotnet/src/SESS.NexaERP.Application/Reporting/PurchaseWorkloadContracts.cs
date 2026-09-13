namespace SESS.NexaERP.Application.Reporting;

public sealed record PurchaseWorkloadRequest(string? Queue = null, string? ApprovalRoute = null,
    int Page = 1, int PageSize = 100);
public sealed record DashboardCurrencyAmount(string Currency, decimal Amount);
public sealed record PurchaseWorkloadBand(string ApprovalRoute, long Count, int? OldestAgeDays,
    IReadOnlyList<DashboardCurrencyAmount> Amounts);
public sealed record PurchaseWorkloadTile(string Key, string Title, string State,
    long? Count, int? OldestAgeDays, bool CommercialValuesVisible,
    IReadOnlyList<DashboardCurrencyAmount> Amounts, long? UnvaluedDocumentCount,
    IReadOnlyList<PurchaseWorkloadBand> ApprovalBands, string? Coverage);
public sealed record PurchaseWorkloadRow(string Queue, Guid DocumentId, string DocumentType,
    string DocumentNumber, string Status, DateTimeOffset WaitingSince, int AgeDays,
    string? ApprovalRoute, Guid? NextApproverEmployeeId, string? NextApproverEmployeeCode,
    string? NextApproverRole, string? ResponsibilityIssue, string? Currency,
    decimal? Value, IReadOnlyList<string> Vendors, long? PendingLineCount, string DetailPath);
public sealed record PurchaseWorkloadPage(string CompanyCode, DateTimeOffset GeneratedAt,
    string TimeZone, IReadOnlyList<PurchaseWorkloadTile> Tiles,
    string? Queue, string? ApprovalRoute, int Page, int PageSize,
    long TotalRows, IReadOnlyList<PurchaseWorkloadRow> Rows);
public interface IPurchaseWorkloadService
{
    Task<PurchaseWorkloadPage> GetAsync(PurchaseWorkloadRequest request, CancellationToken ct);
}
