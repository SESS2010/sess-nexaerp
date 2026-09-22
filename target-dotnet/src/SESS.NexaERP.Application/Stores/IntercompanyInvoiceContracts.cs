using System.Text.Json;

namespace SESS.NexaERP.Application.Stores;

public sealed record RecordIntercompanyInvoiceRequest(
    Guid CorrelationId, string InvoiceNumber, DateOnly InvoiceDate,
    SupplierInvoiceEvidenceInput Evidence, string IdempotencyKey);

public sealed record IntercompanyInvoiceView(
    Guid Id, Guid CompanyId, Guid SellerCompanyId, Guid BuyerCompanyId,
    Guid CorrelationId, string InvoiceNumber, DateOnly InvoiceDate,
    JsonElement OrderSnapshot, SupplierInvoiceEvidence Evidence, bool Replayed);

public interface IIntercompanyInvoiceService
{
    Task<IntercompanyInvoiceView> RecordAsync(RecordIntercompanyInvoiceRequest request, CancellationToken ct);
    Task<IReadOnlyList<IntercompanyInvoiceView>> ListForPurchaseAsync(Guid correlationId, CancellationToken ct);
    Task<IntercompanyInvoiceView?> GetAsync(Guid id, CancellationToken ct);
    Task<SupplierInvoiceContent> DownloadAsync(Guid id, CancellationToken ct);
}
