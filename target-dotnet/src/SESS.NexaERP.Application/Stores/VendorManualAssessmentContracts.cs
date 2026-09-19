namespace SESS.NexaERP.Application.Stores;

public sealed record RecordVendorManualAssessmentRequest(Guid GoodsReceiptId, long GoodsReceiptVersion,
    Guid? SupersedesAssessmentId, decimal TechnicalPoints, decimal ResponsePoints,
    decimal OverallPoints, string Reason, string IdempotencyKey);
public sealed record VendorManualAssessmentView(Guid Id, Guid CompanyId, Guid GoodsReceiptId,
    long GoodsReceiptVersion, Guid VendorId, Guid PurchaseOrderId, int RevisionNumber,
    Guid? SupersedesAssessmentId, decimal TechnicalPoints, decimal ResponsePoints,
    decimal OverallPoints, string Reason, DateTimeOffset RecordedAt, Guid ActorEmployeeId,
    Guid RoleAssignmentId, string RoleAssignmentType, bool Replayed);
public interface IVendorManualAssessmentService
{
    Task<VendorManualAssessmentView> RecordAsync(RecordVendorManualAssessmentRequest request, CancellationToken ct);
    Task<IReadOnlyList<VendorManualAssessmentView>> HistoryAsync(Guid goodsReceiptId, CancellationToken ct);
}
