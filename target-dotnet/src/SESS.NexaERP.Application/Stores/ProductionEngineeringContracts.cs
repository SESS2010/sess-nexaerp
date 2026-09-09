namespace SESS.NexaERP.Application.Stores;

public sealed record ProductionBomLineInput(Guid ItemId, Guid UomId, decimal Quantity, string? Remarks);
public sealed record CreateProductionBomRequest(Guid JobOrderId, string RevisionReason, string IdempotencyKey);
public sealed record ReplaceProductionBomRequest(long ExpectedVersion, string RevisionReason,
    IReadOnlyList<ProductionBomLineInput> Lines, string IdempotencyKey);
public sealed record ProductionBomActionRequest(long ExpectedVersion, string Remarks, string IdempotencyKey);
public sealed record NewProductionBomRevisionRequest(long ExpectedBomVersion, string RevisionReason, string IdempotencyKey);
public sealed record PinProductionBomRevisionRequest(Guid RevisionId, long ExpectedJobOrderVersion, string Reason, string IdempotencyKey);
public sealed record ProductionBomLineView(Guid Id, int LineNumber, Guid ItemId, string ItemCode,
    Guid UomId, string UomCode, decimal Quantity, string? Remarks, decimal? PlannedUnitValue, string CurrencyCode);
public sealed record ProductionBomRevisionView(Guid Id, int RevisionNumber, Guid SourceEstimatedBomRevisionId,
    Guid? SupersedesRevisionId, string Status, string RevisionReason, Guid PreparedByEmployeeId,
    DateTimeOffset? SubmittedAt, DateTimeOffset? ApprovedAt, Guid? ApprovedByEmployeeId,
    string? ApprovalReason, long Version, IReadOnlyList<ProductionBomLineView> Lines);
public sealed record ProductionBomView(Guid Id, string BomNumber, Guid JobOrderId, string JobOrderNumber,
    Guid? PinnedRevisionId, int CurrentRevisionNumber, string Status, long Version,
    ProductionBomRevisionView CurrentRevision);

public sealed record EngineeringDocumentRevisionInput(string RevisionCode, Guid DrawnByEmployeeId,
    Guid CheckedByEmployeeId, string RevisionNote, DateOnly DocumentDate, string StorageKey,
    string FileName, string ContentType, long SizeBytes, string Sha256);
public sealed record CreateEngineeringDocumentRequest(Guid JobOrderId, string DocumentType, string Title,
    EngineeringDocumentRevisionInput Revision, string IdempotencyKey);
public sealed record NewEngineeringDocumentRevisionRequest(long ExpectedDocumentVersion,
    EngineeringDocumentRevisionInput Revision, string IdempotencyKey);
public sealed record EngineeringDocumentActionRequest(long ExpectedVersion, string Remarks, string IdempotencyKey);
public sealed record EngineeringDocumentRevisionView(Guid Id, int RevisionNumber, string RevisionCode,
    Guid? SupersedesRevisionId, Guid DrawnByEmployeeId, Guid CheckedByEmployeeId, Guid? ApprovedByEmployeeId,
    string RevisionNote, DateOnly DocumentDate, string StorageKey, string FileName, string ContentType,
    long SizeBytes, string Sha256, string Status, DateTimeOffset? SubmittedAt, DateTimeOffset? ApprovedAt, long Version);
public sealed record EngineeringDocumentView(Guid Id, string DocumentNumber, string DocumentType,
    Guid JobOrderId, string JobOrderNumber, string Title, Guid? CurrentRevisionId, string Status,
    long Version, IReadOnlyList<EngineeringDocumentRevisionView> Revisions);

public interface IProductionEngineeringService
{
    Task<IReadOnlyList<ProductionBomView>> ListProductionBomsAsync(CancellationToken ct);
    Task<ProductionBomView?> GetProductionBomAsync(string number, CancellationToken ct);
    Task<ProductionBomView> CreateProductionBomAsync(CreateProductionBomRequest request, CancellationToken ct);
    Task<ProductionBomView> ReplaceProductionBomAsync(string number, ReplaceProductionBomRequest request, CancellationToken ct);
    Task<ProductionBomView> SubmitProductionBomAsync(string number, ProductionBomActionRequest request, CancellationToken ct);
    Task<ProductionBomView> ApproveProductionBomAsync(string number, ProductionBomActionRequest request, CancellationToken ct);
    Task<ProductionBomView> CreateProductionBomRevisionAsync(string number, NewProductionBomRevisionRequest request, CancellationToken ct);
    Task<ProductionBomView> PinProductionBomRevisionAsync(string number, PinProductionBomRevisionRequest request, CancellationToken ct);
    Task<IReadOnlyList<EngineeringDocumentView>> ListEngineeringDocumentsAsync(Guid? jobOrderId, string? type, CancellationToken ct);
    Task<EngineeringDocumentView?> GetEngineeringDocumentAsync(string number, CancellationToken ct);
    Task<EngineeringDocumentView> CreateEngineeringDocumentAsync(CreateEngineeringDocumentRequest request, CancellationToken ct);
    Task<EngineeringDocumentView> CreateEngineeringDocumentRevisionAsync(string number, NewEngineeringDocumentRevisionRequest request, CancellationToken ct);
    Task<EngineeringDocumentView> SubmitEngineeringDocumentAsync(string number, EngineeringDocumentActionRequest request, CancellationToken ct);
    Task<EngineeringDocumentView> ApproveEngineeringDocumentAsync(string number, EngineeringDocumentActionRequest request, CancellationToken ct);
}
