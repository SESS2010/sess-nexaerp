using System.Text.Json;

namespace SESS.NexaERP.Application.Reporting;

public sealed record ReportColumn(string Key, string Label, string Type = "text", string? Metric = null);
public sealed record ReportDescriptor(string Key, string Title, bool UsesPeriod, bool ContainsCommercialValues, string? Coverage = null, bool CurrentOnly = false);
public sealed record CompanyReportRequest(
    DateOnly? FromDate = null, DateOnly? ToDate = null, string Mode = "summary",
    string? Group = null, string? Metric = null, int Page = 1, int PageSize = 100);

public sealed record CompanyReportPage(
    string Key, string Title, string CompanyCode, DateTimeOffset GeneratedAt,
    DateOnly? FromDate, DateOnly ToDate, string Mode, int Page, int PageSize,
    long TotalRows, long TotalSourceRows, IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<JsonElement> Rows, IReadOnlyList<JsonElement> Totals, string? Coverage = null, string TimeZone = "UTC");

public sealed record CompanyReportDownload(byte[] Content, string FileName);

public interface ICompanyReportService
{
    Task<IReadOnlyList<ReportDescriptor>> ListAsync(CancellationToken cancellationToken);
    Task<CompanyReportPage> GetAsync(string key, CompanyReportRequest request, CancellationToken cancellationToken);
    Task<CompanyReportDownload> ExportAsync(string key, CompanyReportRequest request, CancellationToken cancellationToken);
}

public sealed class ReportAccessDeniedException : Exception
{
    public ReportAccessDeniedException() : base("This report is not permitted for your employee and selected company.") { }
}
public sealed class ReportSourceUnavailableException(string code) : Exception(code switch
{
    "FIFO_RETURN_CREDITS_REQUIRED" => "FIFO valuation is unavailable because accepted returns have no FIFO cost credits. Ask the ERP administrator to resolve the accounting workflow.",
    _ => "FIFO source ownership, currency or balances need reconciliation. Ask the ERP administrator to reconcile the source ledger."
})
{
    public string Code { get; } = code;
}

public sealed class ReportRequestException(string message) : Exception(message) { }
