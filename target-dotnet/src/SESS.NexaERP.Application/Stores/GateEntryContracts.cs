namespace SESS.NexaERP.Application.Stores;

public sealed record GateEntryLineRequest(Guid PurchaseOrderLineId, decimal DeliveredQuantity);
public sealed record CreateGateEntryRequest(string PurchaseOrderNumber,string VendorDcNumber,string? VehicleNumber,string ModeOfTransport,DateTimeOffset ArrivedAt,string IsoReceiptVerificationJson,IReadOnlyList<GateEntryLineRequest> Lines);
public sealed record UpdateGateEntryRequest(string VendorDcNumber,string? VehicleNumber,string ModeOfTransport,DateTimeOffset ArrivedAt,string IsoReceiptVerificationJson,IReadOnlyList<GateEntryLineRequest> Lines,uint Version);
public sealed record FinalizeGateEntryRequest(uint Version,string IdempotencyKey);
public sealed record GateEntryLineResult(Guid Id,int LineNumber,Guid PurchaseOrderLineId,Guid ItemId,string ItemCode,string Uom,decimal DeliveredQuantity);
public sealed record GateEntryHistoryResult(string? FromStatus,string ToStatus,string Action,Guid ActorEmployeeId,string ActorRoleCode,DateTimeOffset OccurredAt);
public sealed record GateEntryResult(Guid Id,string GateEntryNumber,string PurchaseOrderNumber,Guid PurchaseOrderId,Guid VendorId,string VendorName,string VendorDcNumber,string? VehicleNumber,string ModeOfTransport,DateTimeOffset ArrivedAt,string IsoReceiptVerificationJson,string Status,uint Version,IReadOnlyList<GateEntryLineResult> Lines,IReadOnlyList<GateEntryHistoryResult> History);
public sealed record GateEntryListResult(int TotalCount,int PageNumber,int PageSize,IReadOnlyList<GateEntryResult> Items);
public sealed record GateEntryPurchaseOrderLineCandidate(Guid PurchaseOrderLineId,int LineNumber,Guid ItemId,string ItemCode,string ItemName,string Uom,decimal OrderedQuantity);
public sealed record GateEntryPurchaseOrderCandidate(Guid PurchaseOrderId,string PurchaseOrderNumber,Guid VendorId,string VendorName,IReadOnlyList<GateEntryPurchaseOrderLineCandidate> Lines);

public interface IGateEntryService
{
    Task<GateEntryResult> CreateAsync(CreateGateEntryRequest request,string idempotencyKey,CancellationToken cancellationToken);
    Task<GateEntryResult> UpdateAsync(Guid id,UpdateGateEntryRequest request,CancellationToken cancellationToken);
    Task<GateEntryResult> FinalizeAsync(Guid id,FinalizeGateEntryRequest request,CancellationToken cancellationToken);
    Task<GateEntryResult?> GetAsync(Guid id,CancellationToken cancellationToken);
    Task<GateEntryListResult> ListAsync(string? gateEntryNumber,string? purchaseOrderNumber,Guid? vendorId,DateOnly? from,DateOnly? to,string? state,int page,int pageSize,CancellationToken cancellationToken);
    Task<IReadOnlyList<GateEntryPurchaseOrderCandidate>> ListPurchaseOrderCandidatesAsync(CancellationToken cancellationToken);
}

public sealed class StoresValidationException(string message) : Exception(message);
public sealed class StoresConflictException(string message) : Exception(message);
