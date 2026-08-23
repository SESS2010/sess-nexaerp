using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Purchase.A5;

public interface IA5PurchaseActionParameters;

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
    Guid PurchaseRequirementHandoffId,
    decimal Quantity);

public sealed record A5RfqCreateParameters(
    DateTimeOffset QuoteDueAt,
    string CurrencyCode,
    bool IsSingleSource,
    string? SingleSourceJustification,
    string IdempotencyKey,
    IReadOnlyList<A5RfqSourceLineParameters> Lines) : IA5PurchaseActionParameters;

public sealed record A5RfqVendorInviteParameters(
    string RfqNumber,
    Guid VendorId,
    string Remarks,
    uint RfqVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5QuotationLineParameters(
    Guid RequestForQuotationLineId,
    decimal Quantity,
    decimal UnitRate,
    decimal DiscountValue,
    decimal PackingForwarding,
    decimal Freight,
    decimal Insurance,
    decimal OtherCharges,
    DateOnly PromisedDeliveryDate,
    string HsnSacCode,
    string SupplierStateCode,
    string PlaceOfSupplyStateCode,
    VendorRegistrationType VendorRegistrationType,
    decimal RoundOff);

public sealed record A5QuotationRevisionSubmitParameters(
    Guid InvitationId,
    string VendorQuoteReference,
    string CurrencyCode,
    string PaymentTerms,
    string DeliveryTerms,
    string WarrantyTerms,
    bool RequestLateAuthorization,
    string? LateAuthorizationRemarks,
    A5SubmissionSource SubmissionSource,
    DateTimeOffset ReceivedAt,
    string AttachmentObjectKey,
    string AttachmentSha256,
    string VendorAttestation,
    uint InvitationVersion,
    uint? PreviousQuotationVersion,
    string IdempotencyKey,
    IReadOnlyList<A5QuotationLineParameters> Lines,
    decimal HeaderDiscountValue) : IA5PurchaseActionParameters;

public sealed record A5QuotationTechnicalVerifyParameters(
    string QuotationNumber,
    Guid VendorQuotationLineId,
    bool IsCompliant,
    string ComplianceEvidenceJson,
    string Remarks,
    uint QuotationVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5ComparisonCreateParameters(
    string RfqNumber,
    uint RfqVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5ComparisonRecommendParameters(
    string ComparisonNumber,
    Guid VendorQuotationId,
    string RecommendationRemarks,
    string? SingleSourceJustification,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5ComparisonApprovalParameters(
    string ComparisonNumber,
    string Remarks,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderCreateParameters(
    string ComparisonNumber,
    uint ComparisonVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderSubmitParameters(
    string PoNumber,
    string Remarks,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderIssueParameters(
    string PoNumber,
    string Remarks,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderAmendParameters(
    string PoNumber,
    string AmendmentReason,
    string PaymentTerms,
    string DeliveryTerms,
    string WarrantyTerms,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderReviseRejectedParameters(
    string PoNumber,
    string RevisionReason,
    string PaymentTerms,
    string DeliveryTerms,
    string WarrantyTerms,
    uint RejectedVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderApprovalParameters(
    string PoNumber,
    string Remarks,
    uint Version,
    uint? ExpectedCurrentVersion,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5PurchaseOrderCancelParameters(
    string PoNumber,
    string Reason,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;

public sealed record A5MaterialFollowUpTransitionParameters(
    Guid HandoffId,
    A5MaterialFollowUpTargetStatus ToStatus,
    string Reason,
    uint Version,
    string IdempotencyKey) : IA5PurchaseActionParameters;
