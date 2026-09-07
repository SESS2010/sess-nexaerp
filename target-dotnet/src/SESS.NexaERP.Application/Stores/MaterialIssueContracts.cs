namespace SESS.NexaERP.Application.Stores;

public sealed record MaterialIssueRequestLineInput(Guid ItemId, Guid UomId, decimal Quantity,
    Guid? CustomerPurchaseOrderLineId, string? Remarks);
public sealed record CreateMaterialIssueRequest(string Purpose, string Situation, string DestinationType, Guid? JobOrderId,
    Guid? CustomerId, Guid? VendorId, Guid? DestinationDepartmentId, string DestinationName,
    Guid RequestingDepartmentId, DateOnly RequiredDate, IReadOnlyList<MaterialIssueRequestLineInput> Lines,
    string IdempotencyKey);
public sealed record UpdateMaterialIssueRequest(long Version, string Purpose, string Situation, string DestinationType,
    Guid? JobOrderId, Guid? CustomerId, Guid? VendorId, Guid? DestinationDepartmentId,
    string DestinationName, Guid RequestingDepartmentId, DateOnly RequiredDate,
    IReadOnlyList<MaterialIssueRequestLineInput> Lines, string IdempotencyKey);
public sealed record MaterialIssueTransitionRequest(long Version, string Reason, string IdempotencyKey);
public sealed record MaterialIssueExcessDecisionRequest(string Decision, string Reason, string IdempotencyKey);
public sealed record MaterialIssueRequestLineView(Guid Id, int LineNumber, Guid ItemId, string ItemCode,
    string ItemName, Guid UomId, string UomCode, decimal RequestedQuantity, decimal RequestedBaseQuantity,
    Guid? CustomerPurchaseOrderLineId, decimal EstimatedBomBaseQuantity, decimal ProductionBomBaseQuantity,
    decimal CustomerPoBaseQuantity,
    decimal ExcessBaseQuantity, string ExcessClassification, bool TdDecisionPresent, string? Remarks);
public sealed record MaterialIssueRequestView(Guid Id, string RequestNumber, string Purpose,
    string Situation, string DestinationType, Guid? JobOrderId, Guid? CustomerId, Guid? VendorId,
    Guid? DestinationDepartmentId, string DestinationName, Guid RequestingDepartmentId,
    Guid RequestedByEmployeeId, DateOnly RequiredDate, string Status, long Version,
    IReadOnlyList<MaterialIssueRequestLineView> Lines);
public sealed record MaterialIssueRequestPage(int Total, int Page, int PageSize,
    IReadOnlyList<MaterialIssueRequestView> Items);

public sealed record MaterialIssueScan(Guid MaterialIssueRequestLineId, string ScanCode,
    Guid? InventorySerialId, decimal Quantity = 1);
public sealed record CreateMaterialIssue(string IdempotencyKey, Guid IssuedToEmployeeId,
    DateTimeOffset IssuedAt, IReadOnlyList<MaterialIssueScan> Scans);
public sealed record MaterialIssueLineView(Guid Id, Guid MaterialIssueRequestLineId, int LineNumber,
    Guid ItemId, decimal QuantityBase, Guid OwnershipAccountId, Guid FromCustodyAssignmentId,
    Guid ToCustodyAssignmentId, Guid InventoryProvenanceLayerId, Guid? InventoryLotId,
    Guid? InventorySerialId, Guid WarehouseConditionLocationId);
public sealed record MaterialIssueView(Guid Id, string IssueNumber, Guid MaterialIssueRequestId,
    Guid? JobOrderId, Guid IssuedToEmployeeId, DateTimeOffset IssuedAt, DateTimeOffset ReturnDueAt,
    string Status, Guid? StockPostingBatchId,
    string ActorRoleCode, Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    long Version, bool Replayed, IReadOnlyList<MaterialIssueLineView> Lines);
public sealed record OutstandingEngineerCustodyView(Guid MaterialIssueId, string IssueNumber,
    Guid? JobOrderId, Guid EmployeeId, string EmployeeCode, DateTimeOffset IssuedAt,
    DateTimeOffset ReturnDueAt, bool ReturnNotificationDue, decimal QuantityBase);

public interface IMaterialIssueService
{
    Task<MaterialIssueRequestPage> ListRequestsAsync(string? number, string? status, int page, int pageSize, CancellationToken ct);
    Task<MaterialIssueRequestView?> GetRequestAsync(Guid id, CancellationToken ct);
    Task<MaterialIssueRequestView> CreateRequestAsync(CreateMaterialIssueRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> UpdateRequestAsync(Guid id, UpdateMaterialIssueRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> SubmitAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> ApproveAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> RejectAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> CancelAsync(Guid id, MaterialIssueTransitionRequest request, CancellationToken ct);
    Task<MaterialIssueRequestView> DecideExcessAsync(Guid lineId, MaterialIssueExcessDecisionRequest request, CancellationToken ct);
    Task<MaterialIssueView?> GetIssueAsync(Guid id, CancellationToken ct);
    Task<MaterialIssueView> IssueAsync(Guid requestId, CreateMaterialIssue request, CancellationToken ct);
    Task<IReadOnlyList<OutstandingEngineerCustodyView>> OutstandingCustodyAsync(
        Guid? employeeId, bool? notificationDue, CancellationToken ct);
}
