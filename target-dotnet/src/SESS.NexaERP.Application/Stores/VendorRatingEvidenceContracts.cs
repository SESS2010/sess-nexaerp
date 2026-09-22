namespace SESS.NexaERP.Application.Stores;

public sealed record VendorConcessionEvidence(Guid Id, long Version, Guid QcRevisionId, Guid AllocationId,
    decimal AllocatedQuantity, Guid? ReversalId, long? ReversalVersion, decimal AppliedQuantity);
public sealed record VendorQcLotEvidence(Guid ReceiptLotAllocationId, Guid InventoryLotId, decimal ReceivedQuantity,
    Guid? QcRevisionId, long? QcVersion, int? QcRevisionNumber, decimal? AcceptedWithoutConcession,
    decimal? RejectedQuantity, decimal? DiscrepancyPendingQuantity, decimal ConcessionAcceptedQuantity,
    IReadOnlyList<VendorConcessionEvidence> Concessions, decimal? QualityPoints, string Status);
public sealed record VendorRatingLineEvidence(Guid GoodsReceiptLineId, int LineNumber, Guid ItemId, string ItemCode,
    string Uom, decimal ReceivedQuantity, IReadOnlyList<VendorQcLotEvidence> Lots, decimal? QualityPoints,
    Guid? CommercialComparisonLineId, long? CommercialComparisonLineVersion, string CommittedDateSnapshot,
    DateOnly? CommittedDate, int? DaysLate, decimal? DeliveryPoints, string DeliveryStatus);
public sealed record VendorRatingReceiptEvidence(Guid CompanyId, Guid GoodsReceiptId, long GoodsReceiptVersion,
    Guid VendorId, Guid PurchaseOrderId, DateOnly ReceivedDate, string TimeZone, string RuleVersion,
    IReadOnlyList<VendorRatingLineEvidence> Lines, decimal? ShipmentDeliveryPoints, string ShipmentDeliveryStatus,
    string SourceFingerprint, DateTimeOffset GeneratedAt);
public sealed record VendorRatingReceiptOption(Guid GoodsReceiptId, long GoodsReceiptVersion, string GrnNumber, Guid VendorId, DateTimeOffset ReceivedAt);
public sealed record VendorRatingReceiptPage(int TotalCount, int Page, int PageSize, IReadOnlyList<VendorRatingReceiptOption> Items);
public interface IVendorRatingEvidenceService
{
    Task<VendorRatingReceiptPage> ListReceiptsAsync(int page, int pageSize, CancellationToken ct);
    Task<VendorRatingReceiptEvidence?> GetAsync(Guid goodsReceiptId, CancellationToken ct);
}
