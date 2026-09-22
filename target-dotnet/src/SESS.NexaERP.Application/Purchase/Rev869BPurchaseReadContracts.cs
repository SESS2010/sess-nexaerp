using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Purchase;

public sealed record RfqListItem(
    Guid Id, string RfqNumber, DateTimeOffset QuoteDueAt, string Status,
    int InvitedVendorCount, DateTimeOffset CreatedAt, uint Version);

public sealed record RfqDetail(
    Guid Id, Guid CompanyId, string OrganizationId, string RfqNumber, string FinancialYear,
    long SequenceNumber, Guid PurchaseRequisitionId, Guid? RequestingDepartmentId,
    Guid? DeliveryWarehouseId, Guid OwnerEmployeeId, DateTimeOffset QuoteDueAt,
    string CurrencyCode, string Status, bool IsSingleSource, string? SingleSourceJustification,
    string IdempotencyKey, string TransitionCorrelationId, DateTimeOffset? IssuedAt, bool IsActive,
    DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset? UpdatedAt, string? UpdatedBy,
    uint Version, IReadOnlyList<RfqLineDetail> Lines);

public sealed record RfqLineDetail(
    Guid Id, Guid CompanyId, Guid RequestForQuotationId, Guid PurchaseRequirementHandoffId,
    Guid PurchaseRequisitionLineId, Guid ItemId, int LineNumber, string PrNumberSnapshot,
    int PrLineNumberSnapshot, string ItemCodeSnapshot, string ItemNameSnapshot, string UomSnapshot,
    string? SpecificationSnapshot, decimal ApprovedQuantitySnapshot,
    decimal AlreadyOrderedQuantitySnapshot, decimal OutstandingQuantitySnapshot,
    decimal RfqQuantity, DateOnly RequiredDateSnapshot, DateTimeOffset CreatedAt, string CreatedBy,
    DateTimeOffset? UpdatedAt, string? UpdatedBy, uint Version);

public sealed record QuotationListItem(
    Guid Id, string QuotationNumber, string RfqNumber, Guid VendorId,
    string VendorCode, string VendorName, int RevisionNumber,
    DateTimeOffset ReceivedAt, string Status, decimal? TotalPayableValue, uint Version);

public sealed record ComparisonListItem(
    Guid Id, string ComparisonNumber, string RfqNumber, Guid? SelectedVendorId,
    string? SelectedVendorCode, string? SelectedVendorName, string Status,
    DateTimeOffset CreatedAt, decimal? TotalPayableValue, uint Version);

public sealed record PurchaseOrderListItem(
    Guid Id, string PurchaseOrderNumber, int RevisionNumber, Guid VendorId,
    string VendorCode, string VendorName, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt, decimal? TotalPayableValue, uint Version);

public sealed record PurchaseOrderCommercialDetail(
    Guid Id, Guid CompanyId, string OrganizationId, string PoNumber, string FinancialYear,
    long SequenceNumber, Guid RootPurchaseOrderId, Guid? PreviousVersionId, int RevisionNumber,
    bool IsCurrentVersion, Guid CommercialComparisonId, Guid VendorId, Guid? RequestingDepartmentId,
    Guid? DeliveryWarehouseId, Guid OwnerEmployeeId, string Status, string CurrencyCode,
    string ApprovalRoute, int ApprovalCycle, int RequiredApprovalStepCount,
    int CompletedApprovalStepCount, string ApprovalWorkflowSnapshotJson, Guid CreatorEmployeeId,
    decimal TaxableValue, decimal DiscountValue, decimal HeaderDiscountValue, decimal TaxValue,
    decimal PackingForwarding, decimal Freight, decimal Insurance, decimal OtherCharges,
    decimal RoundOff, decimal TotalPayableValue, string ApprovalPolicySnapshotJson,
    string PaymentTermsSnapshot, string DeliveryTermsSnapshot, string WarrantyTermsSnapshot,
    string? AmendmentReason, DateTimeOffset? IssuedAt, DateTimeOffset? CancelledAt,
    string? CancellationReason, string IdempotencyKey, string TransitionCorrelationId,
    DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset? UpdatedAt, string? UpdatedBy,
    uint Version, IReadOnlyList<PurchaseOrderLineCommercialDetail> Lines);

public sealed record PurchaseOrderLineCommercialDetail(
    Guid Id, Guid CompanyId, Guid PurchaseOrderId, Guid CommercialComparisonLineId,
    Guid PurchaseRequisitionLineId, Guid PurchaseRequirementHandoffId, Guid ItemId, int LineNumber,
    string ItemCodeSnapshot, string ItemNameSnapshot, string UomSnapshot, decimal OrderedQuantity,
    decimal ApprovedOutstandingQuantitySnapshot, decimal UnitRate, string CommercialSnapshotJson,
    string TaxRuleSnapshotJson, decimal TotalPayableValue, DateTimeOffset CreatedAt, string CreatedBy,
    DateTimeOffset? UpdatedAt, string? UpdatedBy, uint Version);

public sealed record MaterialFollowUpListItem(
    Guid Id, string HandoffNumber, Guid PurchaseOrderId, Guid PurchaseOrderLineId,
    decimal OrderedQuantity, string Status, DateTimeOffset HandoffAt, uint Version);

public sealed record RfqVendorCandidate(Guid VendorId, string VendorCode, string VendorName);
public sealed record RfqInvitationLineCandidate(Guid RequestForQuotationLineId, int LineNumber, Guid ItemId,
    string ItemCode, string ItemName, string Uom, decimal Quantity);
public sealed record RfqInvitationCandidate(Guid InvitationId, uint InvitationVersion, string RfqNumber,
    Guid VendorId, string VendorCode, string VendorName, string CurrencyCode, DateTimeOffset QuoteDueAt,
    string Status, uint? CurrentQuotationVersion, IReadOnlyList<RfqInvitationLineCandidate> Lines);
public sealed record ComparisonRfqCandidate(Guid RequestForQuotationId, string RfqNumber, uint RfqVersion,
    string CurrencyCode, int TechnicallyCompliantQuotationCount);
