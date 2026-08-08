namespace SESS.NexaERP.Application.Purchase;

public sealed record PurchaseRequisitionLineRequest(string ItemCode, decimal RequestedQuantity, decimal EstimatedUnitPrice, DateOnly RequiredDate, string? PreferredWarehouseCode, string? ProjectReference, string? MachineReference, string? ServiceReference);

public sealed record CreatePurchaseRequisitionRequest(string OrganizationId, string RequestingDepartmentCode, string RequesterEmployeeCode, DateOnly RequiredByDate, string Priority, string PurposeJustification, string DeliveryWarehouseCode, string? CostCentre, string? ProjectReference, string? ServiceReference, string? WorkOrderReference, string? CustomerReference, IReadOnlyList<PurchaseRequisitionLineRequest> Lines);

public sealed record UpdatePurchaseRequisitionRequest(DateOnly RequiredByDate, string Priority, string PurposeJustification, string DeliveryWarehouseCode, string? CostCentre, string? ProjectReference, string? ServiceReference, string? WorkOrderReference, string? CustomerReference, IReadOnlyList<PurchaseRequisitionLineRequest> Lines, uint Version);

public sealed record PurchaseRequisitionActionRequest(string Remarks, uint Version, string? IdempotencyKey = null);

public sealed record StockCheckLocationRequest(int LineNumber, string WarehouseCode, string? RackBinCode = null);

public sealed record StockCheckRequest(string Remarks, uint Version, string? IdempotencyKey = null, IReadOnlyList<StockCheckLocationRequest>? Locations = null);

public sealed record PurchaseRequisitionLineSummary(Guid Id, int LineNumber, string ItemCode, string ItemName, string Uom, decimal RequestedQuantity, decimal EstimatedUnitPrice, decimal EstimatedLineTotal, decimal OnHand, decimal ActiveReserved, decimal Available, decimal ReservedQuantity, decimal ShortageQuantity, decimal HandoffQuantity, string LineStatus);

public sealed record PurchaseRequisitionSummary(Guid Id, string PrNumber, string OrganizationId, string RequestingDepartment, string RequesterEmployeeCode, DateOnly RequestDate, DateOnly RequiredByDate, string Priority, string Status, string ApprovalRoute, decimal EstimatedTotal, uint Version);

public sealed record PurchaseRequisitionDetail(Guid Id, string PrNumber, string OrganizationId, string RequestingDepartment, string RequesterEmployeeCode, DateOnly RequestDate, DateOnly RequiredByDate, string Priority, string PurposeJustification, string DeliveryWarehouseCode, string? CostCentre, string? ProjectReference, string? ServiceReference, string? WorkOrderReference, string? CustomerReference, string Status, string ApprovalRoute, decimal EstimatedTotal, uint Version, IReadOnlyList<PurchaseRequisitionLineSummary> Lines);

public sealed record PurchaseRequisitionHistorySummary(Guid Id, string Action, string? PreviousStatus, string NewStatus, string Remarks, string ActorLoginId, string ActorRoleCode, DateTimeOffset CreatedAt, string CorrelationId);

public sealed record StockAvailabilityCheckSummary(Guid Id, string CheckNumber, string ResultStatus, string CheckedBy, DateTimeOffset CheckedAt, string Remarks);

public sealed record StockReservationSummary(Guid Id, string ReservationNumber, string PrNumber, int LineNumber, string ItemCode, string WarehouseCode, string? RackBinCode, decimal ReservedQuantity, string Status);

public sealed record PurchaseRequirementHandoffSummary(Guid Id, string HandoffNumber, string PrNumber, int LineNumber, string ItemCode, string WarehouseCode, string? RackBinCode, decimal HandoffQuantity, string Status);
