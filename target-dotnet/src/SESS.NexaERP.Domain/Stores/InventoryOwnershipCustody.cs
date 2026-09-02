using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Sales;

namespace SESS.NexaERP.Domain.Stores;

public static class InventoryCustodyCaseTypes
{
    public const string CustomerOtherBrandModification = "CUSTOMER_OTHER_BRAND_MODIFICATION";
    public const string CustomerSessMachineWarranty = "CUSTOMER_SESS_MACHINE_WARRANTY";
    public const string CustomerSessSpareWarranty = "CUSTOMER_SESS_SPARE_WARRANTY";
    public const string CustomerRemovedPart = "CUSTOMER_REMOVED_PART";
    public const string SupplierLoan = "SUPPLIER_LOAN";
    public const string DemoCustody = "DEMO_CUSTODY";
}

public sealed class InventoryExternalParty : CompanyScopedAuditableEntity
{
    public string PartyType { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string PartyCode { get; set; } = string.Empty;
    public string PartyNameSnapshot { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryAccountHolder : CompanyScopedAuditableEntity
{
    public string HolderType { get; set; } = string.Empty;
    public Guid? HolderCompanyId { get; set; }
    public Company? HolderCompany { get; set; }
    public Guid? ExternalPartyId { get; set; }
    public InventoryExternalParty? ExternalParty { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string HolderCode { get; set; } = string.Empty;
    public string HolderNameSnapshot { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryOwnershipAccount : CompanyScopedAuditableEntity
{
    public Guid AccountHolderId { get; set; }
    public InventoryAccountHolder? AccountHolder { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string OwnershipType { get; set; } = string.Empty;
    public string InventoryValuationBasis { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryCustodyAccount : CompanyScopedAuditableEntity
{
    public Guid AccountHolderId { get; set; }
    public InventoryAccountHolder? AccountHolder { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string CustodyType { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string? VehicleReference { get; set; }
    public string? SiteReference { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryCustodyCase : CompanyScopedAuditableEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public string CaseType { get; set; } = string.Empty;
    public string Status { get; set; } = "RECEIVED";
    public string CommercialAuthorizationStatus { get; set; } = "NOT_REQUIRED";
    public Guid ExternalPartyId { get; set; }
    public InventoryExternalParty? ExternalParty { get; set; }
    public Guid OwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? OwnershipAccount { get; set; }
    public Guid CustodyAccountId { get; set; }
    public InventoryCustodyAccount? CustodyAccount { get; set; }
    public string InboundReturnableDcNumber { get; set; } = string.Empty;
    public DateOnly InboundReturnableDcDate { get; set; }
    public string? OfferReference { get; set; }
    public Guid? CustomerPurchaseOrderId { get; set; }
    public CustomerPurchaseOrder? CustomerPurchaseOrder { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? DueDateSetByEmployeeId { get; set; }
    public Employee? DueDateSetByEmployee { get; set; }
    public DateTimeOffset? DueDateSetAt { get; set; }
    public string? CustomerInstructionReference { get; set; }
    public string? ClosureReason { get; set; }
    public List<InventoryCustodyCaseLine> Lines { get; set; } = [];
}

public sealed class InventoryCustodyCaseLine : CompanyScopedAuditableEntity
{
    public Guid CustodyCaseId { get; set; }
    public InventoryCustodyCase? CustodyCase { get; set; }
    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string DescriptionSnapshot { get; set; } = string.Empty;
    public string? ExternalAssetIdentifier { get; set; }
    public string? SerialNumberSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public Guid UomId { get; set; }
    public Uom? Uom { get; set; }
    public string UomCodeSnapshot { get; set; } = string.Empty;
    public Guid OwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? OwnershipAccount { get; set; }
    public string CommercialScopeStatus { get; set; } = "AWAITING_AUTHORIZATION";
    public Guid? CustomerPurchaseOrderLineId { get; set; }
    public CustomerPurchaseOrderLine? CustomerPurchaseOrderLine { get; set; }
    public string? OfferReference { get; set; }
    public string? ScopeDecisionReason { get; set; }
}

public abstract class InventoryCustodyCaseSourceLink : CompanyScopedAuditableEntity
{
    public Guid CustodyCaseId { get; set; }
    public InventoryCustodyCase? CustodyCase { get; set; }
    public Guid? CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public string LinkRole { get; set; } = string.Empty;
}
public sealed class InventoryCustodyCaseGateEntryLink : InventoryCustodyCaseSourceLink { public Guid GateEntryId { get; set; } public GateEntry? GateEntry { get; set; } }
public sealed class InventoryCustodyCaseGoodsReceiptLink : InventoryCustodyCaseSourceLink { public Guid GoodsReceiptId { get; set; } public GoodsReceipt? GoodsReceipt { get; set; } }
public sealed class InventoryCustodyCaseDeliveryChallanLink : InventoryCustodyCaseSourceLink { public Guid DeliveryChallanId { get; set; } public DeliveryChallan? DeliveryChallan { get; set; } }
public sealed class InventoryCustodyCasePurchaseOrderLink : InventoryCustodyCaseSourceLink { public Guid PurchaseOrderId { get; set; } public PurchaseOrder? PurchaseOrder { get; set; } }
public sealed class InventoryCustodyCaseCustomerPurchaseOrderLink : InventoryCustodyCaseSourceLink { public Guid CustomerPurchaseOrderId { get; set; } public CustomerPurchaseOrder? CustomerPurchaseOrder { get; set; } }
public sealed class InventoryCustodyCaseJobOrderLink : InventoryCustodyCaseSourceLink { public Guid JobOrderId { get; set; } public JobOrder? JobOrder { get; set; } }

public sealed class InventoryCustodyAssignment : CompanyScopedAuditableEntity
{
    public Guid CustodyAccountId { get; set; }
    public InventoryCustodyAccount? CustodyAccount { get; set; }
    public Guid? CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public decimal AssignedQuantity { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string AssignmentReason { get; set; } = string.Empty;
}

public sealed class InventoryCustodyHandoff : CompanyScopedAuditableEntity
{
    public string HandoffNumber { get; set; } = string.Empty;
    public Guid FromCustodyAccountId { get; set; }
    public InventoryCustodyAccount? FromCustodyAccount { get; set; }
    public Guid ToCustodyAccountId { get; set; }
    public InventoryCustodyAccount? ToCustodyAccount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateTimeOffset? HandedOverAt { get; set; }
    public Guid? HandedOverByEmployeeId { get; set; }
    public Employee? HandedOverByEmployee { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedByEmployee { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<InventoryCustodyHandoffLine> Lines { get; set; } = [];
}

public sealed class InventoryCustodyHandoffLine : CompanyScopedAuditableEntity
{
    public Guid CustodyHandoffId { get; set; }
    public InventoryCustodyHandoff? CustodyHandoff { get; set; }
    public int LineNumber { get; set; }
    public Guid CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public Guid FromCustodyAssignmentId { get; set; }
    public InventoryCustodyAssignment? FromCustodyAssignment { get; set; }
    public Guid ToCustodyAssignmentId { get; set; }
    public InventoryCustodyAssignment? ToCustodyAssignment { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class InventoryOwnershipTransfer : CompanyScopedAuditableEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public string TransferType { get; set; } = string.Empty;
    public Guid FromOwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? FromOwnershipAccount { get; set; }
    public Guid ToOwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? ToOwnershipAccount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string Reason { get; set; } = string.Empty;
    public string? AgreementReference { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedByEmployee { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedRoleCode { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public List<InventoryOwnershipTransferLine> Lines { get; set; } = [];
}

public sealed class InventoryOwnershipTransferLine : CompanyScopedAuditableEntity
{
    public Guid OwnershipTransferId { get; set; }
    public InventoryOwnershipTransfer? OwnershipTransfer { get; set; }
    public int LineNumber { get; set; }
    public Guid CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class InventoryMemoLiabilityEvent : CompanyScopedAuditableEntity
{
    public Guid OwnershipAccountId { get; set; }
    public InventoryOwnershipAccount? OwnershipAccount { get; set; }
    public Guid CustodyCaseLineId { get; set; }
    public InventoryCustodyCaseLine? CustodyCaseLine { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal MemoValue { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid? ReversesEventId { get; set; }
    public InventoryMemoLiabilityEvent? ReversesEvent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
