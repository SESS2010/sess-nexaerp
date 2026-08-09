using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;

namespace SESS.NexaERP.Domain.Purchase;

public static class PurchaseRequisitionStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string DepartmentVerified = "DepartmentVerified";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string StockCheckPending = "StockCheckPending";
    public const string FullyAvailable = "FullyAvailable";
    public const string PartiallyAvailable = "PartiallyAvailable";
    public const string NotAvailable = "NotAvailable";
    public const string Reserved = "Reserved";
    public const string PurchaseHandoffCreated = "PurchaseHandoffCreated";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
    public const string Cancelled = "Cancelled";
    public const string Held = "Held";
}

public static class PurchaseRequisitionLineStatuses
{
    public const string Draft = "Draft";
    public const string PendingStockCheck = "PendingStockCheck";
    public const string FullyReserved = "FullyReserved";
    public const string PartiallyReserved = "PartiallyReserved";
    public const string PurchaseRequired = "PurchaseRequired";
    public const string NewItemRequestRequired = "NewItemRequestRequired";
    public const string Cancelled = "Cancelled";
}

public static class PurchaseApproverResolutionTypes
{
    public const string DepartmentMapping = "DEPARTMENT_MAPPING";
    public const string FixedRole = "FIXED_ROLE";
}

public static class PurchaseRequisitionApprovalRoutes
{
    public const string Manager = "MANAGER";
    public const string TechnicalDirector = "TECHNICAL_DIRECTOR";
    public const string ManagingDirector = "MANAGING_DIRECTOR";

    public static string Normalize(string? routeCode) => routeCode?.Trim().ToUpperInvariant() switch
    {
        "MANAGER" or "MANAGER_APPROVAL" or "BRANCH_MANAGER" => Manager,
        "TD" or "TECHNICALDIRECTOR" or "TECHNICAL_DIRECTOR" => TechnicalDirector,
        "MD" or "MANAGINGDIRECTOR" or "MANAGING_DIRECTOR" => ManagingDirector,
        _ => routeCode?.Trim() ?? string.Empty
    };

    public static string DisplayLabel(string routeCode) => Normalize(routeCode) switch
    {
        Manager => "Manager",
        TechnicalDirector => "Technical Director",
        ManagingDirector => "Managing Director",
        _ => routeCode
    };

    public static string? ApproverRoleCode(string routeCode) => Normalize(routeCode) switch
    {
        Manager => null,
        TechnicalDirector => "TECHNICAL_DIRECTOR",
        ManagingDirector => "MANAGING_DIRECTOR",
        _ => routeCode
    };
}

public sealed class PurchaseRequisition : AuditableEntity
{
    public string PrNumber { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public long PrSequence { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid? RequestingDepartmentId { get; set; }
    public Department? RequestingDepartment { get; set; }
    public Guid? RequesterEmployeeId { get; set; }
    public Employee? RequesterEmployee { get; set; }
    public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly RequiredByDate { get; set; }
    public string Priority { get; set; } = "Normal";
    public string PurposeJustification { get; set; } = string.Empty;
    public Guid? DeliveryWarehouseId { get; set; }
    public Warehouse? DeliveryWarehouse { get; set; }
    public string? CostCentre { get; set; }
    public string? ProjectReference { get; set; }
    public string? ServiceReference { get; set; }
    public string? WorkOrderReference { get; set; }
    public string? CustomerReference { get; set; }
    public string Status { get; set; } = PurchaseRequisitionStatuses.Draft;
    public decimal EstimatedTotal { get; set; }
    public string ApprovalRoute { get; set; } = PurchaseRequisitionApprovalRoutes.Manager;
    public string? SubmittedBy { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PurchaseRequisitionLine> Lines { get; set; } = [];
}

public sealed class PurchaseRequisitionLine : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public string? SpecificationSnapshot { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal EstimatedUnitPriceSnapshot { get; set; }
    public decimal EstimatedLineTotal { get; set; }
    public DateOnly RequiredDate { get; set; }
    public Guid? PreferredWarehouseId { get; set; }
    public Warehouse? PreferredWarehouse { get; set; }
    public string? ProjectReference { get; set; }
    public string? MachineReference { get; set; }
    public string? ServiceReference { get; set; }
    public decimal OnHandSnapshot { get; set; }
    public decimal ActiveReservedSnapshot { get; set; }
    public decimal AvailableSnapshot { get; set; }
    public decimal InTransitSnapshot { get; set; }
    public DateTimeOffset? StockCheckedAt { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal ProcurementHandoffQuantity { get; set; }
    public string LineStatus { get; set; } = PurchaseRequisitionLineStatuses.Draft;
}

public sealed class PurchaseRequisitionStatusHistory : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public string PrNumber { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseRequisitionApprovalHistory : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public string PrNumber { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ApprovalRoute { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseRequisitionAttachment : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}

public sealed class StockAvailabilityCheck : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public string CheckedBy { get; set; } = string.Empty;
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ResultStatus { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public List<StockAvailabilityCheckLine> Lines { get; set; } = [];
}

public sealed class StockAvailabilityCheckLine : AuditableEntity
{
    public Guid StockAvailabilityCheckId { get; set; }
    public StockAvailabilityCheck? StockAvailabilityCheck { get; set; }
    public Guid PurchaseRequisitionLineId { get; set; }
    public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string LocationKey { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ActiveReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal InTransitQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public string LineResultStatus { get; set; } = string.Empty;
}

public sealed class StockReservation : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public Guid PurchaseRequisitionLineId { get; set; }
    public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string LocationKey { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; }
    public string Status { get; set; } = "Active";
    public string ReservationNumber { get; set; } = string.Empty;
    public string ReservedBy { get; set; } = string.Empty;
    public DateTimeOffset ReservedAt { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class StockReservationHistory : AuditableEntity
{
    public Guid StockReservationId { get; set; }
    public StockReservation? StockReservation { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string ActorLoginId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseRequirementHandoff : AuditableEntity
{
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public Guid PurchaseRequisitionLineId { get; set; }
    public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string LocationKey { get; set; } = string.Empty;
    public decimal HandoffQuantity { get; set; }
    public string Status { get; set; } = "PendingRFQ";
    public string HandoffNumber { get; set; } = string.Empty;
    public string HandoffBy { get; set; } = string.Empty;
    public DateTimeOffset HandoffAt { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseApprovalRouteSetting : AuditableEntity
{
    public string RouteCode { get; set; } = string.Empty;
    public decimal MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public string? ApproverRoleCode { get; set; }
    public string ApproverResolutionType { get; set; } = PurchaseApproverResolutionTypes.FixedRole;
    public bool IsActive { get; set; } = true;
}

public sealed class DepartmentApprovalMapping : AuditableEntity
{
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string ApprovalRouteCode { get; set; } = PurchaseRequisitionApprovalRoutes.Manager;
    public string Scope { get; set; } = "ALL";
    public Guid PrimaryApproverEmployeeId { get; set; }
    public Employee? PrimaryApproverEmployee { get; set; }
    public Guid? AlternateApproverEmployeeId { get; set; }
    public Employee? AlternateApproverEmployee { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string Remarks { get; set; } = string.Empty;
}
public sealed class PurchaseNumberSequence : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public string Prefix { get; set; } = "PR";
    public long LastNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
