namespace SESS.NexaERP.Application.Purchase.A5;

public enum A5PurchaseActionId
{
    RFQ_CREATE = 1,
    RFQ_VENDOR_INVITE,
    QUOTATION_REVISION_SUBMIT,
    QUOTATION_TECHNICAL_VERIFY,
    COMPARISON_CREATE,
    COMPARISON_RECOMMEND,
    COMPARISON_APPROVE,
    COMPARISON_REJECT,
    COMPARISON_REVISION_REQUEST,
    COMPARISON_RESUBMIT,
    PO_CREATE,
    PO_SUBMIT,
    PO_ISSUE,
    PO_AMEND,
    PO_REVISE_REJECTED,
    PO_APPROVE,
    PO_REJECT,
    PO_CANCEL,
    MATERIAL_FOLLOW_UP_TRANSITION
}

public sealed record A5PurchaseActionBinding(
    A5PurchaseActionId ActionId,
    Type ParameterType,
    string BusinessMethodName);

public static class A5PurchaseActionRegistry
{
    public const int Count = 19;

    public static IReadOnlyList<A5PurchaseActionId> ActionIds { get; } =
        Array.AsReadOnly(Enum.GetValues<A5PurchaseActionId>());

    public static A5PurchaseActionBinding GetBinding(A5PurchaseActionId actionId) => actionId switch
    {
        A5PurchaseActionId.RFQ_CREATE => Bind<A5RfqCreateParameters>(actionId, nameof(IRev869BPurchaseService.CreateRfqAsync)),
        A5PurchaseActionId.RFQ_VENDOR_INVITE => Bind<A5RfqVendorInviteParameters>(actionId, nameof(IRev869BPurchaseService.InviteVendorAsync)),
        A5PurchaseActionId.QUOTATION_REVISION_SUBMIT => Bind<A5QuotationRevisionSubmitParameters>(actionId, nameof(IRev869BPurchaseService.SubmitQuotationRevisionAsync)),
        A5PurchaseActionId.QUOTATION_TECHNICAL_VERIFY => Bind<A5QuotationTechnicalVerifyParameters>(actionId, nameof(IRev869BPurchaseService.VerifyTechnicalAsync)),
        A5PurchaseActionId.COMPARISON_CREATE => Bind<A5ComparisonCreateParameters>(actionId, nameof(IRev869BPurchaseService.CreateComparisonAsync)),
        A5PurchaseActionId.COMPARISON_RECOMMEND => Bind<A5ComparisonRecommendParameters>(actionId, nameof(IRev869BPurchaseService.RecommendAsync)),
        A5PurchaseActionId.COMPARISON_APPROVE => Bind<A5ComparisonApprovalParameters>(actionId, nameof(IRev869BPurchaseService.ApproveAsync)),
        A5PurchaseActionId.COMPARISON_REJECT => Bind<A5ComparisonApprovalParameters>(actionId, nameof(IRev869BPurchaseService.RejectAsync)),
        A5PurchaseActionId.COMPARISON_REVISION_REQUEST => Bind<A5ComparisonApprovalParameters>(actionId, nameof(IRev869BPurchaseService.RequestRevisionAsync)),
        A5PurchaseActionId.COMPARISON_RESUBMIT => Bind<A5ComparisonApprovalParameters>(actionId, nameof(IRev869BPurchaseService.ResubmitAsync)),
        A5PurchaseActionId.PO_CREATE => Bind<A5PurchaseOrderCreateParameters>(actionId, nameof(IRev869BPurchaseService.CreatePurchaseOrderAsync)),
        A5PurchaseActionId.PO_SUBMIT => Bind<A5PurchaseOrderSubmitParameters>(actionId, nameof(IRev869BPurchaseService.SubmitPurchaseOrderAsync)),
        A5PurchaseActionId.PO_ISSUE => Bind<A5PurchaseOrderIssueParameters>(actionId, nameof(IRev869BPurchaseService.IssuePurchaseOrderAsync)),
        A5PurchaseActionId.PO_AMEND => Bind<A5PurchaseOrderAmendParameters>(actionId, nameof(IRev869BPurchaseService.AmendPurchaseOrderAsync)),
        A5PurchaseActionId.PO_REVISE_REJECTED => Bind<A5PurchaseOrderReviseRejectedParameters>(actionId, nameof(IRev869BPurchaseService.ReviseRejectedPurchaseOrderAsync)),
        A5PurchaseActionId.PO_APPROVE => Bind<A5PurchaseOrderApprovalParameters>(actionId, nameof(IRev869BPurchaseService.ApprovePurchaseOrderAsync)),
        A5PurchaseActionId.PO_REJECT => Bind<A5PurchaseOrderApprovalParameters>(actionId, nameof(IRev869BPurchaseService.RejectPurchaseOrderAsync)),
        A5PurchaseActionId.PO_CANCEL => Bind<A5PurchaseOrderCancelParameters>(actionId, nameof(IRev869BPurchaseService.CancelPurchaseOrderAsync)),
        A5PurchaseActionId.MATERIAL_FOLLOW_UP_TRANSITION => Bind<A5MaterialFollowUpTransitionParameters>(actionId, nameof(IRev869BPurchaseService.TransitionMaterialFollowUpAsync)),
        _ => throw new ArgumentOutOfRangeException(nameof(actionId), actionId, "Unknown A5 Purchase action id.")
    };

    public static void ValidateParameters(A5PurchaseActionId actionId, IA5PurchaseActionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var expected = GetBinding(actionId).ParameterType;
        if (parameters.GetType() != expected)
            throw new ArgumentException($"Action {actionId} requires {expected.Name} parameters.", nameof(parameters));
    }

    private static A5PurchaseActionBinding Bind<T>(A5PurchaseActionId actionId, string methodName)
        where T : IA5PurchaseActionParameters => new(actionId, typeof(T), methodName);
}
