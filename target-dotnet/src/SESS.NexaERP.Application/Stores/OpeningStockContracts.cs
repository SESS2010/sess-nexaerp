namespace SESS.NexaERP.Application.Stores;

public sealed record CreateOpeningStockFromImportRequest(
    Guid ImportBatchId, DateOnly PeriodStart, DateOnly PeriodEnd,
    string Reason, string IdempotencyKey);
public sealed record OpeningStockTransitionRequest(
    long Version, string Reason, string IdempotencyKey);
public sealed record OpeningStockLineView(
    Guid Id, int LineNumber, string LineReference, Guid ItemId,
    string ItemCode, string ItemName, Guid WarehouseId, string WarehouseCode,
    Guid RackBinId, string RackBinCode, Guid WarehouseConditionLocationId,
    string? LotNumber, string? SerialNumber, decimal Quantity,
    decimal UnitRate, decimal LineValue, Guid? InventoryLotId,
    Guid? InventorySerialId, Guid? FifoInventoryCostLayerId,
    string? VendorName = null, string? VendorBillNumber = null, DateOnly? BillDate = null, DateOnly? PurchaseDate = null,
    string? Make = null, string? Model = null, string? PartNumber = null, string? Remarks = null);
public sealed record OpeningStockActorView(
    Guid EmployeeId, string EmployeeCode, string EmployeeName,
    string RoleCode, Guid RoleAssignmentId, string AssignmentType,
    DateTimeOffset At, string Reason);
public sealed record OpeningStockView(
    Guid Id, Guid ImportBatchId, DateOnly PeriodStart, DateOnly PeriodEnd,
    string Status, long Version, decimal TotalQuantity, decimal TotalValue,
    OpeningStockActorView CountedBy, OpeningStockActorView? ValuedBy,
    OpeningStockActorView? AuthorizedBy, Guid? StockPostingBatchId,
    bool Replayed, IReadOnlyList<OpeningStockLineView> Lines);
public sealed record OpeningStockPage(
    int Total, int Page, int PageSize, IReadOnlyList<OpeningStockView> Items);

public interface IOpeningStockService
{
    Task<OpeningStockPage> ListAsync(string? status, int page, int pageSize, CancellationToken ct);
    Task<OpeningStockView?> GetAsync(Guid id, CancellationToken ct);
    Task<OpeningStockView> RecordCountAsync(CreateOpeningStockFromImportRequest request, CancellationToken ct);
    Task<OpeningStockView> ConfirmValueAsync(Guid id, OpeningStockTransitionRequest request, CancellationToken ct);
    Task<OpeningStockView> AuthorizeAsync(Guid id, OpeningStockTransitionRequest request, CancellationToken ct);
}
