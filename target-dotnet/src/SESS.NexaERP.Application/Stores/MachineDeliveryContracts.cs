using System.Text.Json;
using SESS.NexaERP.Application.Common;
namespace SESS.NexaERP.Application.Stores;

public sealed record DispatchMachineRequest(Guid JobOrderId, string DcNumber, string Nature, string Purpose,
    DateOnly DispatchDate, DateOnly? ExpectedReturnDate, string Destination, string IdempotencyKey);
public sealed record SignMachineDeliveryRequest(DateTimeOffset DeliveredAt, string CustomerSignatory,
    SupplierInvoiceEvidenceInput Evidence, string IdempotencyKey);
public sealed record MachineDeliveryJobOrderCandidate(Guid JobOrderId, string JobOrderNumber, string MachineSerial,
    string MachineModel, string CustomerName, string FatReadinessStatus);
public interface IMachineDeliveryService
{
    Task<PagedResponse<MachineDeliveryJobOrderCandidate>> JobOrdersAsync(int? page, int? pageSize, string? search, CancellationToken ct);
    Task<JsonElement> DispatchAsync(DispatchMachineRequest request, CancellationToken ct);
    Task<JsonElement> SignAsync(Guid id, SignMachineDeliveryRequest request, CancellationToken ct);
    Task<SupplierInvoiceEvidenceInput?> SignatureAsync(Guid id, CancellationToken ct);
    Task<JsonElement?> GetAsync(Guid id, CancellationToken ct);
}
