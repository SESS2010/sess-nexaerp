using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Purchase;

public static class Rev869BStatuses
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string Submitted = "Submitted";
    public const string Superseded = "Superseded";
    public const string Withdrawn = "Withdrawn";
    public const string TechnicallyCompliant = "TechnicallyCompliant";
    public const string TechnicallyRejected = "TechnicallyRejected";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
    public const string Cancelled = "Cancelled";
    public const string Closed = "Closed";
    public const string PendingFollowUp = "PendingFollowUp";
}

public static class Rev869BStatusContracts
{
    public static readonly IReadOnlySet<string> Rfq = Set(Rev869BStatuses.Draft, Rev869BStatuses.Issued, Rev869BStatuses.Closed, Rev869BStatuses.Cancelled);
    public static readonly IReadOnlySet<string> Invitation = Set(Rev869BStatuses.Issued, Rev869BStatuses.Submitted, Rev869BStatuses.Withdrawn, Rev869BStatuses.Cancelled);
    public static readonly IReadOnlySet<string> Quotation = Set(Rev869BStatuses.Submitted, Rev869BStatuses.TechnicallyCompliant, Rev869BStatuses.TechnicallyRejected, Rev869BStatuses.Superseded, Rev869BStatuses.Withdrawn, Rev869BStatuses.Rejected);
    public static readonly IReadOnlySet<string> TechnicalVerification = Set(Rev869BStatuses.TechnicallyCompliant, Rev869BStatuses.TechnicallyRejected);
    public static readonly IReadOnlySet<string> Comparison = Set(Rev869BStatuses.Draft, Rev869BStatuses.PendingApproval, Rev869BStatuses.Approved, Rev869BStatuses.Rejected, Rev869BStatuses.RevisionRequested, Rev869BStatuses.Cancelled);
    public static readonly IReadOnlySet<string> PurchaseOrder = Set(Rev869BStatuses.Draft, Rev869BStatuses.PendingApproval, Rev869BStatuses.Approved, Rev869BStatuses.Issued, Rev869BStatuses.Rejected, Rev869BStatuses.Superseded, Rev869BStatuses.Cancelled);
    public static readonly IReadOnlySet<string> MaterialFollowUp = Set(Rev869BStatuses.PendingFollowUp, Rev869BStatuses.Closed, Rev869BStatuses.Cancelled);

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> RfqTransitions = Transitions(
        (Rev869BStatuses.Draft, [Rev869BStatuses.Issued, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.Issued, [Rev869BStatuses.Closed, Rev869BStatuses.Cancelled]));
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> InvitationTransitions = Transitions(
        (Rev869BStatuses.Issued, [Rev869BStatuses.Submitted, Rev869BStatuses.Withdrawn, Rev869BStatuses.Cancelled]));
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> QuotationTransitions = Transitions(
        (Rev869BStatuses.Submitted, [Rev869BStatuses.TechnicallyCompliant, Rev869BStatuses.TechnicallyRejected, Rev869BStatuses.Superseded, Rev869BStatuses.Withdrawn]),
        (Rev869BStatuses.TechnicallyCompliant, [Rev869BStatuses.Superseded, Rev869BStatuses.Withdrawn]),
        (Rev869BStatuses.TechnicallyRejected, [Rev869BStatuses.Superseded, Rev869BStatuses.Withdrawn, Rev869BStatuses.Rejected]));
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ComparisonTransitions = Transitions(
        (Rev869BStatuses.Draft, [Rev869BStatuses.PendingApproval, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.PendingApproval, [Rev869BStatuses.Approved, Rev869BStatuses.Rejected, Rev869BStatuses.RevisionRequested]),
        (Rev869BStatuses.RevisionRequested, [Rev869BStatuses.PendingApproval, Rev869BStatuses.Cancelled]));
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PurchaseOrderTransitions = Transitions(
        (Rev869BStatuses.Draft, [Rev869BStatuses.PendingApproval, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.PendingApproval, [Rev869BStatuses.Approved, Rev869BStatuses.Rejected, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.Approved, [Rev869BStatuses.Issued, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.Issued, [Rev869BStatuses.Superseded, Rev869BStatuses.Cancelled]));

    public static void RequireRfq(string from, string to) => Require("RFQ", Rfq, RfqTransitions, from, to);
    public static void RequireInvitation(string from, string to) => Require("invitation", Invitation, InvitationTransitions, from, to);
    public static void RequireQuotation(string from, string to) => Require("quotation", Quotation, QuotationTransitions, from, to);
    public static void RequireComparison(string from, string to) => Require("comparison", Comparison, ComparisonTransitions, from, to);
    public static void RequirePurchaseOrder(string from, string to) => Require("purchase order", PurchaseOrder, PurchaseOrderTransitions, from, to);
    public static bool IsTerminalQuotation(string status) => status is Rev869BStatuses.Superseded or Rev869BStatuses.Withdrawn or Rev869BStatuses.Rejected;
    public static bool IsImmutablePurchaseOrder(string status) => status is Rev869BStatuses.Issued or Rev869BStatuses.Cancelled or Rev869BStatuses.Superseded;

    private static void Require(string aggregate, IReadOnlySet<string> allowed, IReadOnlyDictionary<string, IReadOnlySet<string>> transitions, string from, string to)
    {
        if (!allowed.Contains(from) || !allowed.Contains(to) || !transitions.TryGetValue(from, out var destinations) || !destinations.Contains(to))
            throw new InvalidOperationException($"Invalid {aggregate} status transition: {from} -> {to}.");
    }

    private static IReadOnlySet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.Ordinal);
    private static IReadOnlyDictionary<string, IReadOnlySet<string>> Transitions(params (string From, string[] To)[] values) =>
        values.ToDictionary(x => x.From, x => (IReadOnlySet<string>)new HashSet<string>(x.To, StringComparer.Ordinal), StringComparer.Ordinal);
}

public static class Rev869BApprovalRoutes
{
    public const string Manager = "MANAGER";
    public const string TechnicalDirector = "TECHNICAL_DIRECTOR";
    public const string ManagingDirector = "MANAGING_DIRECTOR";

    public static string Resolve(decimal totalPayableValue, IEnumerable<PurchaseTransactionApprovalPolicy> policies, DateOnly onDate, string organizationId)
    {
        if (totalPayableValue < 0 || string.IsNullOrWhiteSpace(organizationId)) throw new InvalidOperationException("Approval value and organization are required.");
        var matches = policies.Where(x => x.OrganizationId == organizationId && x.IsActive && x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= onDate))
            .Where(x => totalPayableValue >= x.MinimumAmount && (!x.MaximumAmount.HasValue || totalPayableValue <= x.MaximumAmount.Value)).ToList();
        return matches.Count == 1 ? matches[0].RouteCode : throw new InvalidOperationException("A single effective purchase approval policy could not be resolved.");
    }
}

public sealed record Rev869BCommercialInput(
    decimal Quantity, decimal UnitRate, decimal DiscountValue, decimal PackingForwarding,
    decimal Freight, decimal Insurance, decimal OtherCharges, decimal CgstRate,
    decimal SgstRate, decimal IgstRate, decimal CessRate, decimal RoundOff, int RoundingScale);

public sealed record Rev869BCommercialBreakdown(
    decimal TaxableValue, decimal DiscountValue, decimal CgstValue, decimal SgstValue,
    decimal IgstValue, decimal CessValue, decimal PackingForwarding, decimal Freight,
    decimal Insurance, decimal OtherCharges, decimal RoundOff, decimal TotalPayableValue);

public static class Rev869BCommercialCalculator
{
    public const decimal MaximumSupportedValue = 999999999999999999m;

    public static Rev869BCommercialBreakdown Calculate(Rev869BCommercialInput input)
    {
        if (input.Quantity <= 0 || input.UnitRate < 0 || input.DiscountValue < 0 || input.PackingForwarding < 0 || input.Freight < 0 || input.Insurance < 0 || input.OtherCharges < 0 || input.RoundingScale is < 0 or > 6)
            throw new InvalidOperationException("Commercial quantities, values or rounding scale are invalid.");
        foreach (var rate in new[] { input.CgstRate, input.SgstRate, input.IgstRate, input.CessRate })
            if (!TaxGstSetting.IsValidRate(rate)) throw new InvalidOperationException("Tax rate is outside the approved range.");
        foreach (var value in new[] { input.Quantity, input.UnitRate, input.DiscountValue, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges })
            if (value > MaximumSupportedValue) throw new InvalidOperationException("Commercial value exceeds numeric(24,6) capacity.");
        decimal taxable;
        try { taxable = decimal.Round(checked(input.Quantity * input.UnitRate), input.RoundingScale, MidpointRounding.AwayFromZero); }
        catch (OverflowException) { throw new InvalidOperationException("Commercial multiplication exceeds supported precision."); }
        var taxBase = taxable - input.DiscountValue;
        if (taxBase < 0) throw new InvalidOperationException("Discount cannot exceed taxable value.");
        decimal Tax(decimal rate) => decimal.Round(taxBase * rate / 100m, input.RoundingScale, MidpointRounding.AwayFromZero);
        var cgst = Tax(input.CgstRate);
        var sgst = Tax(input.SgstRate);
        var igst = Tax(input.IgstRate);
        var cess = Tax(input.CessRate);
        var total = decimal.Round(taxable - input.DiscountValue + cgst + sgst + igst + cess + input.PackingForwarding + input.Freight + input.Insurance + input.OtherCharges + input.RoundOff, input.RoundingScale, MidpointRounding.AwayFromZero);
        if (total < 0) throw new InvalidOperationException("Total payable value cannot be negative.");
        return new(taxable, input.DiscountValue, cgst, sgst, igst, cess, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges, input.RoundOff, total);
    }
}

public sealed class RequestForQuotation : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string RfqNumber { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public Guid PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }
    public Guid? RequestingDepartmentId { get; set; }
    public Department? RequestingDepartment { get; set; }
    public Guid? DeliveryWarehouseId { get; set; }
    public Warehouse? DeliveryWarehouse { get; set; }
    public Guid OwnerEmployeeId { get; set; }
    public Employee? OwnerEmployee { get; set; }
    public DateTimeOffset QuoteDueAt { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = Rev869BStatuses.Draft;
    public bool IsSingleSource { get; set; }
    public string? SingleSourceJustification { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? IssuedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RequestForQuotationLine> Lines { get; set; } = [];
    public List<RfqVendorInvitation> Invitations { get; set; } = [];
}

public sealed class RequestForQuotationLine : AuditableEntity
{
    public Guid RequestForQuotationId { get; set; }
    public RequestForQuotation? RequestForQuotation { get; set; }
    public Guid PurchaseRequirementHandoffId { get; set; }
    public PurchaseRequirementHandoff? PurchaseRequirementHandoff { get; set; }
    public Guid PurchaseRequisitionLineId { get; set; }
    public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public int LineNumber { get; set; }
    public string PrNumberSnapshot { get; set; } = string.Empty;
    public int PrLineNumberSnapshot { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public string? SpecificationSnapshot { get; set; }
    public decimal ApprovedQuantitySnapshot { get; set; }
    public decimal AlreadyOrderedQuantitySnapshot { get; set; }
    public decimal OutstandingQuantitySnapshot { get; set; }
    public decimal RfqQuantity { get; set; }
    public DateOnly RequiredDateSnapshot { get; set; }
}

public sealed class RfqVendorInvitation : AuditableEntity
{
    public Guid RequestForQuotationId { get; set; }
    public RequestForQuotation? RequestForQuotation { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string Status { get; set; } = Rev869BStatuses.Issued;
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset QuoteDueAtSnapshot { get; set; }
    public string VendorQualificationSnapshotJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class VendorQuotation : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string QuotationNumber { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public Guid RfqVendorInvitationId { get; set; }
    public RfqVendorInvitation? RfqVendorInvitation { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid RootQuotationId { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public VendorQuotation? PreviousRevision { get; set; }
    public int RevisionNumber { get; set; }
    public bool IsCurrentRevision { get; set; } = true;
    public string VendorQuoteReference { get; set; } = string.Empty;
    public string SubmissionSource { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public string AttachmentObjectKey { get; set; } = string.Empty;
    public string AttachmentSha256 { get; set; } = string.Empty;
    public string VendorAttestation { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "INR";
    public string Status { get; set; } = Rev869BStatuses.Submitted;
    public DateTimeOffset SubmittedAt { get; set; }
    public bool IsLateSubmission { get; set; }
    public Guid? LateAuthorizedByEmployeeId { get; set; }
    public Employee? LateAuthorizedByEmployee { get; set; }
    public string? LateAuthorizationRemarks { get; set; }
    public string PaymentTermsSnapshot { get; set; } = string.Empty;
    public string DeliveryTermsSnapshot { get; set; } = string.Empty;
    public string WarrantyTermsSnapshot { get; set; } = string.Empty;
    public decimal TotalPayableValue { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<VendorQuotationLine> Lines { get; set; } = [];
}

public sealed class VendorQuotationLine : AuditableEntity
{
    public Guid VendorQuotationId { get; set; }
    public VendorQuotation? VendorQuotation { get; set; }
    public Guid RequestForQuotationLineId { get; set; }
    public RequestForQuotationLine? RequestForQuotationLine { get; set; }
    public int LineNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal PackingForwarding { get; set; }
    public decimal Freight { get; set; }
    public decimal Insurance { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal TaxableValue { get; set; }
    public Guid TaxGstSettingId { get; set; }
    public TaxGstSetting? TaxGstSetting { get; set; }
    public string TaxRuleSnapshotJson { get; set; } = "{}";
    public string HsnSacCode { get; set; } = string.Empty;
    public string SupplierStateCode { get; set; } = string.Empty;
    public string PlaceOfSupplyStateCode { get; set; } = string.Empty;
    public string VendorRegistrationType { get; set; } = string.Empty;
    public decimal CgstValue { get; set; }
    public decimal SgstValue { get; set; }
    public decimal IgstValue { get; set; }
    public decimal CessValue { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalPayableValue { get; set; }
    public DateOnly PromisedDeliveryDate { get; set; }
}

public sealed class QuotationTechnicalVerification : AuditableEntity
{
    public Guid VendorQuotationLineId { get; set; }
    public VendorQuotationLine? VendorQuotationLine { get; set; }
    public Guid VerifierEmployeeId { get; set; }
    public Employee? VerifierEmployee { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
    public string ComplianceSnapshotJson { get; set; } = "{}";
    public string Remarks { get; set; } = string.Empty;
    public DateTimeOffset VerifiedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class CommercialComparison : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string ComparisonNumber { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public Guid RequestForQuotationId { get; set; }
    public RequestForQuotation? RequestForQuotation { get; set; }
    public Guid? RecommendedVendorQuotationId { get; set; }
    public VendorQuotation? RecommendedVendorQuotation { get; set; }
    public Guid? SelectedVendorId { get; set; }
    public Vendor? SelectedVendor { get; set; }
    public Guid OwnerEmployeeId { get; set; }
    public Employee? OwnerEmployee { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public decimal TotalPayableValue { get; set; }
    public string ApprovalRoute { get; set; } = string.Empty;
    public string Status { get; set; } = Rev869BStatuses.Draft;
    public bool IsSingleSource { get; set; }
    public string? SingleSourceJustification { get; set; }
    public string? RecommendationRemarks { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<CommercialComparisonLine> Lines { get; set; } = [];
}

public sealed class CommercialComparisonLine : AuditableEntity
{
    public Guid CommercialComparisonId { get; set; }
    public CommercialComparison? CommercialComparison { get; set; }
    public Guid VendorQuotationLineId { get; set; }
    public VendorQuotationLine? VendorQuotationLine { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string TechnicalComplianceSnapshot { get; set; } = string.Empty;
    public string CommercialSnapshotJson { get; set; } = "{}";
    public string DeliverySnapshot { get; set; } = string.Empty;
    public string WarrantySnapshot { get; set; } = string.Empty;
    public string PaymentTermsSnapshot { get; set; } = string.Empty;
    public decimal TotalPayableValue { get; set; }
    public bool IsRecommended { get; set; }
    public string? RecommendationReason { get; set; }
}

public sealed class PurchaseTransactionApprovalHistory : AuditableEntity
{
    public Guid CommercialComparisonId { get; set; }
    public CommercialComparison? CommercialComparison { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string ApprovalRoute { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseOrder : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public string FinancialYear { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public Guid RootPurchaseOrderId { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public PurchaseOrder? PreviousVersion { get; set; }
    public int RevisionNumber { get; set; }
    public bool IsCurrentVersion { get; set; } = true;
    public Guid CommercialComparisonId { get; set; }
    public CommercialComparison? CommercialComparison { get; set; }
    public Guid VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? RequestingDepartmentId { get; set; }
    public Department? RequestingDepartment { get; set; }
    public Guid? DeliveryWarehouseId { get; set; }
    public Warehouse? DeliveryWarehouse { get; set; }
    public Guid OwnerEmployeeId { get; set; }
    public Employee? OwnerEmployee { get; set; }
    public string Status { get; set; } = Rev869BStatuses.Draft;
    public string CurrencyCode { get; set; } = "INR";
    public string ApprovalRoute { get; set; } = string.Empty;
    public decimal TaxableValue { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal TaxValue { get; set; }
    public decimal PackingForwarding { get; set; }
    public decimal Freight { get; set; }
    public decimal Insurance { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalPayableValue { get; set; }
    public string PaymentTermsSnapshot { get; set; } = string.Empty;
    public string DeliveryTermsSnapshot { get; set; } = string.Empty;
    public string WarrantyTermsSnapshot { get; set; } = string.Empty;
    public string? AmendmentReason { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<PurchaseOrderLine> Lines { get; set; } = [];
}

public sealed class PurchaseOrderLine : AuditableEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid CommercialComparisonLineId { get; set; }
    public CommercialComparisonLine? CommercialComparisonLine { get; set; }
    public Guid PurchaseRequisitionLineId { get; set; }
    public PurchaseRequisitionLine? PurchaseRequisitionLine { get; set; }
    public Guid PurchaseRequirementHandoffId { get; set; }
    public PurchaseRequirementHandoff? PurchaseRequirementHandoff { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public int LineNumber { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ApprovedOutstandingQuantitySnapshot { get; set; }
    public decimal UnitRate { get; set; }
    public string CommercialSnapshotJson { get; set; } = "{}";
    public string TaxRuleSnapshotJson { get; set; } = "{}";
    public decimal TotalPayableValue { get; set; }
}

public sealed class PurchaseOrderHistory : AuditableEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class MaterialFollowUpHandoff : AuditableEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public string HandoffNumber { get; set; } = string.Empty;
    public decimal OrderedQuantitySnapshot { get; set; }
    public string Status { get; set; } = Rev869BStatuses.PendingFollowUp;
    public DateTimeOffset HandoffAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseTransactionStatusHistory : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid ActorEmployeeId { get; set; }
    public Employee? ActorEmployee { get; set; }
    public string ActorLoginId { get; set; } = string.Empty;
    public string ActorRoleCode { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class PurchaseTransactionApprovalPolicy : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string RouteCode { get; set; } = string.Empty;
    public decimal MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
    public string ApproverRoleCode { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
