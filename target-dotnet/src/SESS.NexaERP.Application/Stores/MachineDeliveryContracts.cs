using System.Text.Json;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
namespace SESS.NexaERP.Application.Stores;

/// <remarks>VehicleNo, Transporter, EwayBillNo and EwayBillDate are optional dispatch details (26 Sep DC print addendum).</remarks>
public sealed record DispatchMachineRequest(Guid JobOrderId, string DcNumber, string Nature, string Purpose,
    DateOnly DispatchDate, DateOnly? ExpectedReturnDate, string Destination, string IdempotencyKey,
    string? VehicleNo = null, string? Transporter = null, string? EwayBillNo = null, DateOnly? EwayBillDate = null);
/// <summary>R6: the printed machine DC. Items option A: the machine itself is the single line.</summary>
public sealed record MachineDeliveryPrintLine(int LineNumber, string Description, string MachineModel, string MachineSerial, decimal Quantity, string Uom);
public sealed record MachineDeliveryPrintParty(string Code, string Name, string? LegalName, string? Gstin, string? Address, string? State, string? StateCode,
    string? ContactPerson, string? Phone);
public sealed record MachineDeliveryPrintView(CompanyPrintHeader Company, Guid Id, string DcNumber, DateOnly DispatchDate, string Nature, string Purpose,
    DateOnly? ExpectedReturnDate, string Destination, string? VehicleNo, string? Transporter, string? EwayBillNo, DateOnly? EwayBillDate,
    string DcState, MachineDeliveryPrintParty Customer, string CustomerPoNumber, DateOnly? CustomerPoDate, string JobOrderNumber,
    IReadOnlyList<MachineDeliveryPrintLine> Items, string RecordedByEmployeeCode, string RecordedByEmployeeName, DateTimeOffset RecordedAt,
    DateTimeOffset? DeliveredAt, string? CustomerSignatory, DateTimeOffset PrintedAt, string PrintedBy);
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
    Task<MachineDeliveryPrintView?> PrintAsync(Guid id, CancellationToken ct);
}
