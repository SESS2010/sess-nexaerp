namespace SESS.NexaERP.Application.Purchase;

public sealed record Rev869BRfqSourceLineRequest(Guid PurchaseRequirementHandoffId, decimal Quantity);
public sealed record Rev869BCreateRfqRequest(DateTimeOffset QuoteDueAt, string CurrencyCode, bool IsSingleSource, string? SingleSourceJustification, string IdempotencyKey, IReadOnlyList<Rev869BRfqSourceLineRequest> Lines);
public sealed record Rev869BInviteVendorRequest(Guid VendorId, string Remarks, string IdempotencyKey);

public sealed record Rev869BQuotationLineRequest(
    Guid RequestForQuotationLineId, decimal Quantity, decimal UnitRate, decimal DiscountValue,
    decimal PackingForwarding, decimal Freight, decimal Insurance, decimal OtherCharges,
    DateOnly PromisedDeliveryDate, string HsnSacCode, string SupplierStateCode,
    string PlaceOfSupplyStateCode, string VendorRegistrationType, decimal RoundOff);

public sealed record Rev869BSubmitQuotationRequest(
    string VendorQuoteReference, string CurrencyCode, string PaymentTerms, string DeliveryTerms,
    string WarrantyTerms, bool RequestLateAuthorization, string? LateAuthorizationRemarks,
    string IdempotencyKey, IReadOnlyList<Rev869BQuotationLineRequest> Lines);

public sealed record Rev869BTechnicalVerificationRequest(Guid VendorQuotationLineId, bool IsCompliant, string ComplianceEvidenceJson, string Remarks);
public sealed record Rev869BCreateComparisonRequest(string RfqNumber, string IdempotencyKey);
public sealed record Rev869BRecommendComparisonRequest(Guid VendorQuotationId, string RecommendationRemarks, string? SingleSourceJustification, uint Version, string IdempotencyKey);
public sealed record Rev869BApprovalActionRequest(string Remarks, uint Version, string IdempotencyKey);
public sealed record Rev869BCreatePurchaseOrderRequest(string ComparisonNumber, string IdempotencyKey);
public sealed record Rev869BIssuePurchaseOrderRequest(string Remarks, uint Version, string IdempotencyKey);
public sealed record Rev869BAmendPurchaseOrderRequest(string AmendmentReason, string PaymentTerms, string DeliveryTerms, string WarrantyTerms, uint Version, string IdempotencyKey);
public sealed record Rev869BCancelPurchaseOrderRequest(string Reason, uint Version, string IdempotencyKey);

public sealed record Rev869BDocumentResult(Guid Id, string Number, string Status, uint Version);

public interface IRev869BPurchaseService
{
    Task<Rev869BDocumentResult> CreateRfqAsync(Rev869BCreateRfqRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> InviteVendorAsync(string rfqNumber, Rev869BInviteVendorRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> SubmitQuotationRevisionAsync(Guid invitationId, Rev869BSubmitQuotationRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> VerifyTechnicalAsync(string quotationNumber, Rev869BTechnicalVerificationRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> CreateComparisonAsync(Rev869BCreateComparisonRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> RecommendAsync(string comparisonNumber, Rev869BRecommendComparisonRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> ApproveAsync(string comparisonNumber, Rev869BApprovalActionRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> RejectAsync(string comparisonNumber, Rev869BApprovalActionRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> RequestRevisionAsync(string comparisonNumber, Rev869BApprovalActionRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> ResubmitAsync(string comparisonNumber, Rev869BApprovalActionRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> CreatePurchaseOrderAsync(Rev869BCreatePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> IssuePurchaseOrderAsync(string poNumber, Rev869BIssuePurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> AmendPurchaseOrderAsync(string poNumber, Rev869BAmendPurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> ApprovePurchaseOrderAsync(string poNumber, Rev869BApprovalActionRequest request, CancellationToken cancellationToken);
    Task<Rev869BDocumentResult> CancelPurchaseOrderAsync(string poNumber, Rev869BCancelPurchaseOrderRequest request, CancellationToken cancellationToken);
}
