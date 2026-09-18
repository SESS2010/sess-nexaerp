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
    string DepartmentCode, Guid RequestedByEmployeeId, string EmployeeCode, string EmployeeName,
    DateOnly RequiredDate, string Status, long Version,
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
    Guid? InventorySerialId, Guid WarehouseConditionLocationId, string? StoredSerialNumber, decimal FittedQuantityBase);
public sealed record MaterialIssueView(Guid Id, string IssueNumber, Guid MaterialIssueRequestId,
    Guid? JobOrderId, Guid IssuedToEmployeeId, DateTimeOffset IssuedAt, DateTimeOffset ReturnDueAt,
    string Status, Guid? StockPostingBatchId,
    string ActorRoleCode, Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    long Version, bool Replayed, IReadOnlyList<MaterialIssueLineView> Lines);
public sealed record OutstandingEngineerCustodyView(Guid MaterialIssueId, string IssueNumber,
    Guid? JobOrderId, Guid EmployeeId, string EmployeeCode, DateTimeOffset IssuedAt,
    DateTimeOffset ReturnDueAt, bool ReturnNotificationDue, decimal QuantityBase);

public sealed record MaterialIssueRecipientView(Guid EmployeeId, string EmployeeCode,
    string EmployeeName, string DepartmentCode);
public sealed record AvailableMaterialIssueSerialView(Guid InventorySerialId,
    string StoredSerialNumber, Guid ItemId, string ItemCode, Guid? InventoryLotId,
    string? SupplierLotNumber, Guid WarehouseConditionLocationId, Guid WarehouseId,
    string WarehouseCode, Guid RackBinId, string RackBinCode, decimal AvailableQuantity);
public sealed record MaterialReturnLineInput(Guid MaterialIssueLineId, string ScanCode,
    decimal ReturnedQuantity, decimal ReportedConsumedQuantity, decimal ReportedStillHeldQuantity);
public sealed record CreateMaterialReturn(DateTimeOffset DeclaredAt,
    IReadOnlyList<MaterialReturnLineInput> Lines, string IdempotencyKey);
public sealed record AcceptMaterialReturn(long Version, DateTimeOffset AcceptedAt,
    string Reason, string IdempotencyKey);
public sealed record MaterialReturnLineView(Guid Id, Guid MaterialIssueLineId, int LineNumber,
    Guid ItemId, decimal ReturnedQuantityBase, decimal ReportedConsumedQuantityBase,
    decimal ReportedStillHeldQuantityBase, string ScanCode, Guid? InventorySerialId);
public sealed record MaterialReturnView(Guid Id, string ReturnNumber, Guid MaterialIssueId,
    Guid ReturnedByEmployeeId, DateTimeOffset DeclaredAt, string Status, DateTimeOffset? AcceptedAt,
    Guid? AcceptedByEmployeeId, Guid? StockPostingBatchId, long Version, bool Replayed,
    string ActorRoleCode, Guid ResolvedRoleAssignmentId, string ResolvedRoleAssignmentType,
    string? AcceptedActorRoleCode, Guid? AcceptedRoleAssignmentId,
    string? AcceptedRoleAssignmentType, IReadOnlyList<MaterialReturnLineView> Lines);
public sealed record MaterialReturnPage(int Total, int Page, int PageSize,
    IReadOnlyList<MaterialReturnView> Items);

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
    Task<IReadOnlyList<MaterialIssueRecipientView>> ListIssueRecipientsAsync(CancellationToken ct);
    Task<IReadOnlyList<AvailableMaterialIssueSerialView>> AvailableSerialsAsync(
        Guid materialIssueRequestLineId, CancellationToken ct);
    Task<MaterialReturnPage> ListReturnsAsync(Guid? materialIssueId, string? status,
        int page, int pageSize, CancellationToken ct);
    Task<MaterialReturnView?> GetReturnAsync(Guid id, CancellationToken ct);
    Task<MaterialReturnView> CreateReturnAsync(Guid materialIssueId,
        CreateMaterialReturn request, CancellationToken ct);
    Task<MaterialReturnView> AcceptReturnAsync(Guid id,
        AcceptMaterialReturn request, CancellationToken ct);
}
