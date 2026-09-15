using System.Text.Json;
namespace SESS.NexaERP.Application.Stores;

public sealed record DispatchMachineRequest(Guid JobOrderId, string DcNumber, string Nature, string Purpose,
    DateOnly DispatchDate, DateOnly? ExpectedReturnDate, string Destination, string IdempotencyKey);
public sealed record SignMachineDeliveryRequest(DateTimeOffset DeliveredAt, string CustomerSignatory,
    SupplierInvoiceEvidenceInput Evidence, string IdempotencyKey);
public interface IMachineDeliveryService
{
    Task<JsonElement> DispatchAsync(DispatchMachineRequest request, CancellationToken ct);
    Task<JsonElement> SignAsync(Guid id, SignMachineDeliveryRequest request, CancellationToken ct);
    Task<SupplierInvoiceEvidenceInput?> SignatureAsync(Guid id, CancellationToken ct);
    Task<JsonElement?> GetAsync(Guid id, CancellationToken ct);
}
