using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
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
    public const string RevisionDraft = "RevisionDraft";
    public const string Resubmitted = "Resubmitted";
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
    public static readonly IReadOnlySet<string> PurchaseOrder = Set(Rev869BStatuses.Draft, Rev869BStatuses.PendingApproval, Rev869BStatuses.Approved, Rev869BStatuses.Issued, Rev869BStatuses.Rejected, Rev869BStatuses.RevisionDraft, Rev869BStatuses.Resubmitted, Rev869BStatuses.Superseded, Rev869BStatuses.Cancelled);
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
        (Rev869BStatuses.Rejected, [Rev869BStatuses.RevisionDraft]),
        (Rev869BStatuses.RevisionDraft, [Rev869BStatuses.Resubmitted, Rev869BStatuses.Cancelled]),
        (Rev869BStatuses.Resubmitted, [Rev869BStatuses.Approved, Rev869BStatuses.Rejected, Rev869BStatuses.Cancelled]),
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
    decimal SgstRate, decimal IgstRate, decimal CessRate, decimal RoundOff, int RoundingScale)
{
    public decimal HeaderDiscountValue { get; init; }
    public string CurrencyCode { get; init; } = string.Concat('I', 'N', 'R');
    public decimal ExchangeRate { get; init; } = 1m;
}

public sealed record Rev869BCommercialBreakdown(
    decimal TaxableValue, decimal DiscountValue, decimal CgstValue, decimal SgstValue,
    decimal IgstValue, decimal CessValue, decimal PackingForwarding, decimal Freight,
    decimal Insurance, decimal OtherCharges, decimal RoundOff, decimal TotalPayableValue)
{
    public decimal GrossAmount { get; init; }
    public decimal HeaderDiscountValue { get; init; }
    public decimal AssessableValue { get; init; }
    public string CurrencyCode { get; init; } = string.Concat('I', 'N', 'R');
    public decimal ExchangeRate { get; init; } = 1m;
}

public sealed record Rev869BCommercialAggregate(
    decimal TaxableValue, decimal DiscountValue, decimal HeaderDiscountValue, decimal TaxValue, decimal PackingForwarding,
    decimal Freight, decimal Insurance, decimal OtherCharges, decimal RoundOff, decimal TotalPayableValue);

public sealed record Rev869BTaxRuleSnapshot(
    Guid Id, string OrganizationId, string JurisdictionCode, string HsnSacCode, string SupplyType,
    string SupplierStateCode, string PlaceOfSupplyStateCode, string VendorRegistrationType,
    decimal GstRate, decimal CgstRate, decimal SgstRate, decimal IgstRate, decimal CessRate,
    bool IsExempt, bool IsReverseCharge, string CurrencyCode, int RoundingScale,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, string ApprovalStatus, bool IsActive);

public sealed record Rev869BPoCommercialSnapshot(
    Guid VendorQuotationId, Guid VendorQuotationLineId, Guid RequestForQuotationId,
    Guid CommercialComparisonId, Guid VendorId, string OrganizationId,
    string VendorQualificationSnapshotJson, string AttachmentObjectKey, string AttachmentSha256,
    string ComparisonApprovalRoute, DateTimeOffset ComparisonApprovedAt, DateTimeOffset QuotationReceivedAt,
    Rev869BCommercialInput Input, Rev869BCommercialBreakdown Result)
{
    public int QuotationRevision { get; init; }
    public Guid ItemId { get; init; }
    public decimal Quantity { get; init; }
    public string Uom { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
}

public static class Rev869BCommercialCalculator
{
    public const decimal MaximumSupportedValue = 999999999999999999.999999m;

    public static decimal TaxableValue(Rev869BCommercialInput input)
    {
        ValidateInput(input);
        var assessable = Round(Multiply(input.Quantity, input.UnitRate), input.RoundingScale, "line gross amount");
        var taxableCharges = Add(input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges);
        var beforeDiscount = Add(assessable, taxableCharges);
        var totalDiscount = Add(input.DiscountValue, input.HeaderDiscountValue);
        if (totalDiscount > beforeDiscount) throw new InvalidOperationException("Line and header discounts cannot exceed assessable value plus taxable charges.");
        return Ensure(Round(beforeDiscount - totalDiscount, input.RoundingScale, "taxable value"), "taxable value");
    }

    public static Rev869BCommercialBreakdown Calculate(Rev869BCommercialInput input)
    {
        ValidateInput(input);
        var taxable = TaxableValue(input);
        decimal Tax(decimal rate) => Round(Multiply(taxable, rate) / 100m, input.RoundingScale, "tax component");
        var cgst = Tax(input.CgstRate);
        var sgst = Tax(input.SgstRate);
        var igst = Tax(input.IgstRate);
        var cess = Tax(input.CessRate);
        var total = Round(Add(taxable, cgst, sgst, igst, cess, input.RoundOff), input.RoundingScale, "total payable value");
        if (total < 0) throw new InvalidOperationException("Total payable value cannot be negative.");
        var gross = Round(Multiply(input.Quantity, input.UnitRate), input.RoundingScale, "line gross amount");
        return new(taxable, input.DiscountValue, cgst, sgst, igst, cess, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges, input.RoundOff, total)
        { GrossAmount = gross, AssessableValue = Add(gross, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges), HeaderDiscountValue = input.HeaderDiscountValue, CurrencyCode = input.CurrencyCode, ExchangeRate = input.ExchangeRate };
    }

    public static Rev869BCommercialAggregate Aggregate(IEnumerable<Rev869BCommercialBreakdown> lines)
    {
        var values = lines.ToArray();
        if (values.Length == 0) throw new InvalidOperationException("At least one commercial line is required.");
        return new(
            Add(values.Select(x => x.TaxableValue).ToArray()),
            Add(values.Select(x => x.DiscountValue).ToArray()),
            Add(values.Select(x => x.HeaderDiscountValue).ToArray()),
            Add(values.SelectMany(x => new[] { x.CgstValue, x.SgstValue, x.IgstValue, x.CessValue }).ToArray()),
            Add(values.Select(x => x.PackingForwarding).ToArray()),
            Add(values.Select(x => x.Freight).ToArray()),
            Add(values.Select(x => x.Insurance).ToArray()),
            Add(values.Select(x => x.OtherCharges).ToArray()),
            Add(values.Select(x => x.RoundOff).ToArray()),
            Add(values.Select(x => x.TotalPayableValue).ToArray()));
    }

    public static Rev869BCommercialBreakdown Reconcile(
        Rev869BCommercialInput input, Rev869BCommercialBreakdown stored, Rev869BTaxRuleSnapshot taxRule, DateOnly effectiveDate)
    {
        if (taxRule.Id == Guid.Empty || string.IsNullOrWhiteSpace(taxRule.OrganizationId) || !taxRule.IsActive ||
            taxRule.ApprovalStatus != MasterApprovalStatuses.Approved || taxRule.EffectiveFrom > effectiveDate ||
            taxRule.EffectiveTo.HasValue && taxRule.EffectiveTo.Value < effectiveDate)
            throw new InvalidOperationException("The immutable GST rule snapshot is invalid or was not effective on the quotation receipt date.");
        if (taxRule.SupplyType != TaxGstSetting.ResolveSupplyType(taxRule.SupplierStateCode, taxRule.PlaceOfSupplyStateCode) ||
            taxRule.SupplyType == "INTRASTATE" && (taxRule.IgstRate != 0m || taxRule.CgstRate + taxRule.SgstRate != taxRule.GstRate) ||
            taxRule.SupplyType == "INTERSTATE" && (taxRule.CgstRate != 0m || taxRule.SgstRate != 0m || taxRule.IgstRate != taxRule.GstRate))
            throw new InvalidOperationException("The immutable GST component split is invalid.");
        var calculated = Calculate(input with
        {
            CgstRate = taxRule.CgstRate, SgstRate = taxRule.SgstRate, IgstRate = taxRule.IgstRate,
            CessRate = taxRule.CessRate, RoundingScale = taxRule.RoundingScale
        });
        if (calculated != stored) throw new InvalidOperationException("Client or stored commercial totals do not reconcile with the authoritative calculation.");
        return calculated;
    }

    public static decimal Add(params decimal[] values)
    {
        decimal total = 0m;
        try { foreach (var value in values) total = checked(total + Ensure(value, "commercial component")); }
        catch (OverflowException) { throw new InvalidOperationException("Aggregate commercial value exceeds numeric(24,6) capacity."); }
        return Ensure(total, "aggregate commercial value");
    }

    public static decimal Ensure(decimal value, string name)
    {
        if (value < -MaximumSupportedValue || value > MaximumSupportedValue) throw new InvalidOperationException($"{name} exceeds numeric(24,6) capacity.");
        if (DecimalScale(value) > 6) throw new InvalidOperationException($"{name} must not exceed six decimal places.");
        return value;
    }

    private static decimal Multiply(decimal left, decimal right)
    {
        try { return Capacity(checked(left * right), "commercial multiplication"); }
        catch (OverflowException) { throw new InvalidOperationException("Commercial multiplication exceeds numeric(24,6) capacity."); }
    }

    private static decimal Round(decimal value, int scale, string name) => Ensure(decimal.Round(value, scale, MidpointRounding.AwayFromZero), name);

    private static void ValidateInput(Rev869BCommercialInput input)
    {
        if (input.Quantity <= 0 || input.UnitRate < 0 || input.DiscountValue < 0 || input.HeaderDiscountValue < 0 || input.PackingForwarding < 0 || input.Freight < 0 || input.Insurance < 0 || input.OtherCharges < 0 || input.ExchangeRate <= 0 || input.RoundingScale is < 0 or > 6 || string.IsNullOrWhiteSpace(input.CurrencyCode))
            throw new InvalidOperationException("Commercial quantities, values or rounding scale are invalid.");
        foreach (var rate in new[] { input.CgstRate, input.SgstRate, input.IgstRate, input.CessRate })
        {
            Ensure(rate, "tax rate");
            if (!TaxGstSetting.IsValidRate(rate)) throw new InvalidOperationException("Tax rate is outside the approved range.");
        }
        foreach (var value in new[] { input.Quantity, input.UnitRate, input.DiscountValue, input.HeaderDiscountValue, input.PackingForwarding, input.Freight, input.Insurance, input.OtherCharges, input.RoundOff, input.ExchangeRate })
            Ensure(value, "commercial input");
    }
    private static decimal Capacity(decimal value, string name) => value < -MaximumSupportedValue || value > MaximumSupportedValue
        ? throw new InvalidOperationException($"{name} exceeds numeric(24,6) capacity.") : value;
    private static int DecimalScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
}

public static class Rev869BIdempotencyFingerprint
{
    public static string CommandScope(string organizationId, string operation, string idempotencyKey) =>
        Hash(Required(organizationId, "Organization") + "\\n" + Required(operation, "Operation") + "\\n" + Required(idempotencyKey, "Idempotency key"));

    public static string Create(string organizationId, string operation, string idempotencyKey, object canonicalPayload)
    {
        ArgumentNullException.ThrowIfNull(canonicalPayload);
        return CommandScope(organizationId, operation, idempotencyKey) + "." + Hash(CanonicalJson(canonicalPayload));
    }

    public static bool SameCommand(string storedFingerprint, string organizationId, string operation, string idempotencyKey) =>
        !string.IsNullOrWhiteSpace(storedFingerprint) && storedFingerprint.StartsWith(CommandScope(organizationId, operation, idempotencyKey) + ".", StringComparison.Ordinal);

    private static string CanonicalJson(object payload)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        var output = new StringBuilder();
        Write(document.RootElement, output);
        return output.ToString();
    }

    private static void Write(JsonElement value, StringBuilder output)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                output.Append('{'); var first = true;
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                { if (!first) output.Append(','); first = false; output.Append(JsonSerializer.Serialize(property.Name)).Append(':'); Write(property.Value, output); }
                output.Append('}'); break;
            case JsonValueKind.Array:
                output.Append('['); var initial = true;
                var canonicalItems = value.EnumerateArray().Select(item => { var itemOutput = new StringBuilder(); Write(item, itemOutput); return itemOutput.ToString(); }).OrderBy(item => item, StringComparer.Ordinal);
                foreach (var item in canonicalItems) { if (!initial) output.Append(','); initial = false; output.Append(item); }
                output.Append(']'); break;
            case JsonValueKind.String:
                output.Append(JsonSerializer.Serialize((value.GetString() ?? string.Empty).Trim())); break;
            default: output.Append(value.GetRawText()); break;
        }
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Required(string value, string name) => !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new InvalidOperationException($"{name} is required for idempotency.");
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
    public decimal HeaderDiscountValue { get; set; }
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
    public decimal HeaderDiscountValue { get; set; }
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
    public decimal HeaderDiscountValue { get; set; }
    public decimal TaxValue { get; set; }
    public decimal PackingForwarding { get; set; }
    public decimal Freight { get; set; }
    public decimal Insurance { get; set; }
    public decimal OtherCharges { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalPayableValue { get; set; }
    public string ApprovalPolicySnapshotJson { get; set; } = "{}";
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

public static class Rev869BPurchaseOrderSnapshot
{
    private static readonly JsonSerializerOptions SnapshotJson = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    public static void RequireComplete(PurchaseOrder purchaseOrder, bool requireApproved = true)
    {
        if (requireApproved && purchaseOrder.Status != Rev869BStatuses.Approved) throw new InvalidOperationException("Only an approved purchase order can be issued.");
        if (purchaseOrder.Lines.Count == 0 || string.IsNullOrWhiteSpace(purchaseOrder.PaymentTermsSnapshot) ||
            string.IsNullOrWhiteSpace(purchaseOrder.DeliveryTermsSnapshot) || string.IsNullOrWhiteSpace(purchaseOrder.WarrantyTermsSnapshot) ||
            string.IsNullOrWhiteSpace(purchaseOrder.ApprovalRoute) || string.IsNullOrWhiteSpace(purchaseOrder.ApprovalPolicySnapshotJson) || purchaseOrder.ApprovalPolicySnapshotJson == "{}")
            throw new InvalidOperationException("The pre-issue purchase-order snapshot is incomplete.");
        if (purchaseOrder.Lines.Any(x => x.OrderedQuantity <= 0 || x.ApprovedOutstandingQuantitySnapshot <= 0 ||
            string.IsNullOrWhiteSpace(x.CommercialSnapshotJson) || x.CommercialSnapshotJson == "{}" ||
            string.IsNullOrWhiteSpace(x.TaxRuleSnapshotJson) || x.TaxRuleSnapshotJson == "{}"))
            throw new InvalidOperationException("A pre-issue purchase-order line snapshot is incomplete.");
        var reconciled = new List<Rev869BCommercialBreakdown>();
        foreach (var line in purchaseOrder.Lines)
        {
            Rev869BPoCommercialSnapshot commercial;
            Rev869BTaxRuleSnapshot tax;
            try
            {
                commercial = JsonSerializer.Deserialize<Rev869BPoCommercialSnapshot>(line.CommercialSnapshotJson, SnapshotJson)
                    ?? throw new JsonException();
                tax = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(line.TaxRuleSnapshotJson, SnapshotJson)
                    ?? throw new JsonException();
            }
            catch (JsonException) { throw new InvalidOperationException("A pre-issue commercial or GST snapshot is malformed."); }
            if (commercial.VendorQuotationId == Guid.Empty || commercial.VendorQuotationLineId == Guid.Empty ||
                commercial.RequestForQuotationId == Guid.Empty || commercial.CommercialComparisonId != purchaseOrder.CommercialComparisonId ||
                commercial.VendorId != purchaseOrder.VendorId || commercial.OrganizationId != purchaseOrder.OrganizationId ||
                commercial.QuotationRevision <= 0 || commercial.ItemId != line.ItemId || commercial.Quantity != line.OrderedQuantity ||
                commercial.Uom != line.UomSnapshot || commercial.CurrencyCode != purchaseOrder.CurrencyCode || commercial.ExchangeRate != 1m ||
                string.IsNullOrWhiteSpace(commercial.VendorQualificationSnapshotJson) || commercial.VendorQualificationSnapshotJson == "{}" ||
                string.IsNullOrWhiteSpace(commercial.AttachmentObjectKey) || commercial.AttachmentSha256.Length != 64 ||
                commercial.ComparisonApprovalRoute != purchaseOrder.ApprovalRoute || commercial.ComparisonApprovedAt == default ||
                tax.OrganizationId != purchaseOrder.OrganizationId)
                throw new InvalidOperationException("The pre-issue provenance, qualification, attachment or approval snapshot is incomplete or mismatched.");
            var result = Rev869BCommercialCalculator.Reconcile(commercial.Input, commercial.Result, tax, DateOnly.FromDateTime(commercial.QuotationReceivedAt.UtcDateTime));
            if (result.TotalPayableValue != line.TotalPayableValue) throw new InvalidOperationException("The pre-issue line payable value is stale or mismatched.");
            reconciled.Add(result);
        }
        var aggregate = Rev869BCommercialCalculator.Aggregate(reconciled);
        if (aggregate.TaxableValue != purchaseOrder.TaxableValue || aggregate.DiscountValue != purchaseOrder.DiscountValue || aggregate.HeaderDiscountValue != purchaseOrder.HeaderDiscountValue ||
            aggregate.TaxValue != purchaseOrder.TaxValue || aggregate.PackingForwarding != purchaseOrder.PackingForwarding ||
            aggregate.Freight != purchaseOrder.Freight || aggregate.Insurance != purchaseOrder.Insurance ||
            aggregate.OtherCharges != purchaseOrder.OtherCharges || aggregate.RoundOff != purchaseOrder.RoundOff ||
            aggregate.TotalPayableValue != purchaseOrder.TotalPayableValue)
            throw new InvalidOperationException("The pre-issue header and immutable commercial line snapshots do not reconcile.");
        try
        {
            using var policy = JsonDocument.Parse(purchaseOrder.ApprovalPolicySnapshotJson);
            var root = policy.RootElement;
            if (root.GetProperty("organizationId").GetString() != purchaseOrder.OrganizationId || root.GetProperty("routeCode").GetString() != purchaseOrder.ApprovalRoute ||
                root.GetProperty("approvalValue").GetDecimal() != purchaseOrder.TotalPayableValue || string.IsNullOrWhiteSpace(root.GetProperty("effectiveOn").GetString()))
                throw new InvalidOperationException("The immutable approval-policy snapshot does not reconcile.");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or FormatException)
        { throw new InvalidOperationException("The immutable approval-policy snapshot is incomplete or malformed."); }
        foreach (var value in new[] { purchaseOrder.TaxableValue, purchaseOrder.DiscountValue, purchaseOrder.HeaderDiscountValue, purchaseOrder.TaxValue, purchaseOrder.PackingForwarding,
                     purchaseOrder.Freight, purchaseOrder.Insurance, purchaseOrder.OtherCharges, purchaseOrder.RoundOff, purchaseOrder.TotalPayableValue })
            Rev869BCommercialCalculator.Ensure(value, "pre-issue commercial snapshot");
    }
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
