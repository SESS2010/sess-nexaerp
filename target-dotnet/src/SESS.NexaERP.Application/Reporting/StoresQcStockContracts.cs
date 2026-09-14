namespace SESS.NexaERP.Application.Reporting;

public sealed record StoresQcStockRequest(string? Queue=null,Guid? DocumentId=null,int Page=1,int PageSize=100);
public sealed record StoresQcStockValue(string Currency,decimal ReceiptProvisionalValue);
public sealed record StoresQcStockTile(string Key,long LineCount,long OverdueLineCount,int? OldestReceiptAgeDays,
    IReadOnlyList<StoresQcStockValue>? Values);
public sealed record StoresQcStockRow(string Queue,Guid DocumentId,string DocumentNumber,Guid LineId,Guid AllocationId,
    Guid ItemId,string ItemCode,string ItemName,string Uom,decimal Quantity,Guid? WarehouseId,Guid? RackBinId,
    Guid OwnershipAccountId,Guid CustodyAssignmentId,Guid ProvenanceLayerId,Guid? SerialId,
    DateTimeOffset ReceivedAt,DateTimeOffset QcDueAt,bool IsOverdue,int ReceiptAgeDays,
    string Currency,decimal? ReceiptProvisionalValue,string DetailPath);
public sealed record StoresQcStockPage(string CompanyCode,DateTimeOffset GeneratedAt,string TimeZone,
    bool CanViewCommercialValues,string ValueBasis,IReadOnlyList<StoresQcStockTile> Tiles,
    StoresQcStockRequest Filters,long TotalRows,IReadOnlyList<StoresQcStockRow> Rows);
public interface IStoresQcStockService
{
    Task<StoresQcStockPage> GetAsync(StoresQcStockRequest request,CancellationToken ct);
}
