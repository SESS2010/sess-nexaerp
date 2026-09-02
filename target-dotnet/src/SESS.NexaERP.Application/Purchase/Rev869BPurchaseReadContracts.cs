using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Purchase;

public sealed record RfqListItem(
    Guid Id, string RfqNumber, DateTimeOffset QuoteDueAt, string Status,
    int InvitedVendorCount, DateTimeOffset CreatedAt, uint Version);

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

public sealed record MaterialFollowUpListItem(
    Guid Id, string HandoffNumber, Guid PurchaseOrderId, Guid PurchaseOrderLineId,
    decimal OrderedQuantity, string Status, DateTimeOffset HandoffAt);
