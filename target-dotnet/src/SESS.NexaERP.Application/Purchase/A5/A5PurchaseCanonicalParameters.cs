using System.Text.Json.Serialization;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Purchase.A5;

public interface IA5PurchaseActionParameters;

public abstract record A5PurchaseActionParameters : IA5PurchaseActionParameters
{
    [JsonPropertyName("canonicalFormVersion")]
    public uint CanonicalFormVersion => A5PurchaseCanonicalSerializer.CanonicalFormVersion;
}

public enum A5SubmissionSource
{
    EMAIL_RECEIVED = 1,
    PHYSICAL_RECEIVED = 2
}

public enum A5MaterialFollowUpTargetStatus
{
    InProgress = 1,
    Completed = 2
}

public sealed record A5RfqSourceLineParameters(
    [property: JsonPropertyName("purchaseRequirementHandoffId")] Guid PurchaseRequirementHandoffId,
    [property: JsonPropertyName("quantity")] decimal Quantity);

public sealed record A5RfqCreateParameters(
    [property: JsonPropertyName("quoteDueAt")] DateTimeOffset QuoteDueAt,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("isSingleSource")] bool IsSingleSource,
    [property: JsonPropertyName("singleSourceJustification")] string? SingleSourceJustification,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey,
    [property: JsonPropertyName("lines")] IReadOnlyList<A5RfqSourceLineParameters> Lines) : A5PurchaseActionParameters;

public sealed record A5RfqVendorInviteParameters(
    [property: JsonPropertyName("rfqNumber")] string RfqNumber,
    [property: JsonPropertyName("vendorId")] Guid VendorId,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("rfqVersion")] uint RfqVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;
public sealed record A5QuotationLineParameters(
    [property: JsonPropertyName("requestForQuotationLineId")] Guid RequestForQuotationLineId,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("unitRate")] decimal UnitRate,
    [property: JsonPropertyName("discountValue")] decimal DiscountValue,
    [property: JsonPropertyName("packingForwarding")] decimal PackingForwarding,
    [property: JsonPropertyName("freight")] decimal Freight,
    [property: JsonPropertyName("insurance")] decimal Insurance,
    [property: JsonPropertyName("otherCharges")] decimal OtherCharges,
    [property: JsonPropertyName("promisedDeliveryDate")] DateOnly PromisedDeliveryDate,
    [property: JsonPropertyName("hsnSacCode")] string HsnSacCode,
    [property: JsonPropertyName("supplierStateCode")] string SupplierStateCode,
    [property: JsonPropertyName("placeOfSupplyStateCode")] string PlaceOfSupplyStateCode,
    [property: JsonPropertyName("vendorRegistrationType")] VendorRegistrationType VendorRegistrationType,
    [property: JsonPropertyName("roundOff")] decimal RoundOff);

public sealed record A5QuotationRevisionSubmitParameters(
    [property: JsonPropertyName("invitationId")] Guid InvitationId,
    [property: JsonPropertyName("vendorQuoteReference")] string VendorQuoteReference,
    [property: JsonPropertyName("currencyCode")] string CurrencyCode,
    [property: JsonPropertyName("paymentTerms")] string PaymentTerms,
    [property: JsonPropertyName("deliveryTerms")] string DeliveryTerms,
    [property: JsonPropertyName("warrantyTerms")] string WarrantyTerms,
    [property: JsonPropertyName("requestLateAuthorization")] bool RequestLateAuthorization,
    [property: JsonPropertyName("lateAuthorizationRemarks")] string? LateAuthorizationRemarks,
    [property: JsonPropertyName("submissionSource")] A5SubmissionSource SubmissionSource,
    [property: JsonPropertyName("receivedAt")] DateTimeOffset ReceivedAt,
    [property: JsonPropertyName("attachmentObjectKey")] string AttachmentObjectKey,
    [property: JsonPropertyName("attachmentSha256")] string AttachmentSha256,
    [property: JsonPropertyName("vendorAttestation")] string VendorAttestation,
    [property: JsonPropertyName("invitationVersion")] uint InvitationVersion,
    [property: JsonPropertyName("previousQuotationVersion")] uint? PreviousQuotationVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey,
    [property: JsonPropertyName("lines")] IReadOnlyList<A5QuotationLineParameters> Lines,
    [property: JsonPropertyName("headerDiscountValue")] decimal HeaderDiscountValue) : A5PurchaseActionParameters;

public sealed record A5QuotationTechnicalVerifyParameters(
    [property: JsonPropertyName("quotationNumber")] string QuotationNumber,
    [property: JsonPropertyName("vendorQuotationLineId")] Guid VendorQuotationLineId,
    [property: JsonPropertyName("isCompliant")] bool IsCompliant,
    [property: JsonPropertyName("complianceEvidenceJson")] string ComplianceEvidenceJson,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("quotationVersion")] uint QuotationVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5ComparisonCreateParameters(
    [property: JsonPropertyName("rfqNumber")] string RfqNumber,
    [property: JsonPropertyName("rfqVersion")] uint RfqVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5ComparisonRecommendParameters(
    [property: JsonPropertyName("comparisonNumber")] string ComparisonNumber,
    [property: JsonPropertyName("vendorQuotationId")] Guid VendorQuotationId,
    [property: JsonPropertyName("recommendationRemarks")] string RecommendationRemarks,
    [property: JsonPropertyName("singleSourceJustification")] string? SingleSourceJustification,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5ComparisonApprovalParameters(
    [property: JsonPropertyName("comparisonNumber")] string ComparisonNumber,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderCreateParameters(
    [property: JsonPropertyName("comparisonNumber")] string ComparisonNumber,
    [property: JsonPropertyName("comparisonVersion")] uint ComparisonVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderSubmitParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderIssueParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderAmendParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("amendmentReason")] string AmendmentReason,
    [property: JsonPropertyName("paymentTerms")] string PaymentTerms,
    [property: JsonPropertyName("deliveryTerms")] string DeliveryTerms,
    [property: JsonPropertyName("warrantyTerms")] string WarrantyTerms,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderReviseRejectedParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("revisionReason")] string RevisionReason,
    [property: JsonPropertyName("paymentTerms")] string PaymentTerms,
    [property: JsonPropertyName("deliveryTerms")] string DeliveryTerms,
    [property: JsonPropertyName("warrantyTerms")] string WarrantyTerms,
    [property: JsonPropertyName("rejectedVersion")] uint RejectedVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderApprovalParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("remarks")] string Remarks,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("expectedCurrentVersion")] uint? ExpectedCurrentVersion,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5PurchaseOrderCancelParameters(
    [property: JsonPropertyName("poNumber")] string PoNumber,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;

public sealed record A5MaterialFollowUpTransitionParameters(
    [property: JsonPropertyName("handoffId")] Guid HandoffId,
    [property: JsonPropertyName("toStatus")] A5MaterialFollowUpTargetStatus ToStatus,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("version")] uint Version,
    [property: JsonPropertyName("idempotencyKey")] string IdempotencyKey) : A5PurchaseActionParameters;
