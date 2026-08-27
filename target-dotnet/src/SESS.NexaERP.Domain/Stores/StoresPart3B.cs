using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Stores;

public sealed class StockPostingBatch : CompanyScopedAuditableEntity
{
    public string PostingKind { get; set; } = string.Empty;
    public Guid? GoodsReceiptId { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public Guid? QcInspectionRevisionId { get; set; }
    public QcInspectionRevision? QcInspectionRevision { get; set; }
    public Guid? MaterialIssueRequestId { get; set; }
    public MaterialIssueRequest? MaterialIssueRequest { get; set; }
    public Guid? DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }
    public Guid? ReversesPostingBatchId { get; set; }
    public StockPostingBatch? ReversesPostingBatch { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateOnly PostingDate { get; set; }
    public DateTimeOffset PostedAt { get; set; }
    public Guid PostedByEmployeeId { get; set; }
    public Employee? PostedByEmployee { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
