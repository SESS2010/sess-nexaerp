namespace SESS.NexaERP.Application.Reporting;

public sealed record StoresWorkloadRequest(string? Queue=null,Guid? DocumentId=null,int Page=1,int PageSize=100);
public sealed record StoresWorkloadTile(string Key,string Title,string State,long? Count,int? OldestAgeDays,string Coverage);
public sealed record StoresWorkloadRow(string Queue,Guid DocumentId,string DocumentType,string DocumentNumber,
    string Status,DateTimeOffset WaitingSince,int AgeDays,long PendingLineCount,Guid? VendorId,string? VendorName,
    IReadOnlyList<string> EligibleApprovalRoles,Guid? AssignedApproverEmployeeId,string? ResponsibilityIssue,string DetailPath);
public sealed record StoresWorkloadPage(string CompanyCode,DateTimeOffset GeneratedAt,string TimeZone,
    IReadOnlyList<StoresWorkloadTile> Tiles,StoresWorkloadRequest Filters,long TotalRows,IReadOnlyList<StoresWorkloadRow> Rows);
public interface IStoresWorkloadService
{
    Task<StoresWorkloadPage> GetAsync(StoresWorkloadRequest request,CancellationToken ct);
}
