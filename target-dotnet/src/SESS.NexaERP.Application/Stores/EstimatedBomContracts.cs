using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Application.Stores;

public sealed record EstimatedBomLineInput(Guid ItemId, Guid UomId, decimal Quantity, string? Remarks, decimal? EstimatedUnitValue = null);
public sealed record CreateEstimatedBomRequest(Guid JobOrderId, string RevisionReason, IReadOnlyList<EstimatedBomLineInput> Lines, string IdempotencyKey);
public sealed record ReplaceEstimatedBomLinesRequest(uint ExpectedVersion, string RevisionReason, IReadOnlyList<EstimatedBomLineInput> Lines, string IdempotencyKey);
public sealed record EstimatedBomActionRequest(uint ExpectedVersion, string Remarks, string IdempotencyKey);
public sealed record NewEstimatedBomRevisionRequest(uint ExpectedBomVersion, string RevisionReason, string IdempotencyKey);
public sealed record MergeItemRequest(Guid SurvivorItemId, string Reason, string IdempotencyKey);

public sealed record EstimatedBomLineView(Guid Id, int LineNumber, Guid OriginalItemId, string OriginalItemCode,
    Guid CanonicalItemId, string CanonicalItemCode, bool CanonicalItemActive, string CanonicalItemApprovalStatus,
    Guid UomId, string UomCode, decimal Quantity, string? Remarks, decimal? EstimatedUnitValue,
    bool EstimatedUnitValueOverridden, string CurrencyCode);
public sealed record EstimatedBomCanonicalLineView(Guid CanonicalItemId, string CanonicalItemCode,
    bool CanonicalItemActive, string CanonicalItemApprovalStatus, Guid BaseUomId, string BaseUomCode,
    decimal BaseQuantity, IReadOnlyList<EstimatedBomLineView> SourceLines);
public sealed record EstimatedBomRevisionView(Guid Id, int RevisionNumber, string Status, string RevisionReason,
    Guid PreparedByEmployeeId, DateTimeOffset? SubmittedAt, DateTimeOffset? ApprovedAt, Guid? ApprovedByEmployeeId,
    string? ApprovalReason, uint Version, IReadOnlyList<EstimatedBomLineView> Lines,
    IReadOnlyList<EstimatedBomCanonicalLineView> CanonicalLines);
public sealed record EstimatedBomView(Guid Id, string BomNumber, Guid JobOrderId, string JobOrderNumber,
    int CurrentRevisionNumber, Guid? ApprovedRevisionId, Guid? CommercialBaselineRevisionId, string Status,
    uint Version, EstimatedBomRevisionView CurrentRevision);
public sealed record EstimatedBomSummary(Guid Id, string BomNumber, Guid JobOrderId, string JobOrderNumber,
    int CurrentRevisionNumber, string Status, Guid? ApprovedRevisionId, Guid? CommercialBaselineRevisionId, uint Version);
public sealed record EstimatedBomHistoryView(Guid Id, Guid EstimatedBomRevisionId, string Action, string? FromStatus,
    string ToStatus, Guid ActorEmployeeId, string ActorRoleCode, Guid ResolvedRoleAssignmentId,
    string ResolvedRoleAssignmentType, string CorrelationId, string Remarks, DateTimeOffset CreatedAt);
public sealed record EstimatedBomWorkbookFile(string FileName, string ContentType, byte[] Content);

public interface IEstimatedBomService
{
    Task<PagedResponse<EstimatedBomSummary>> ListAsync(int? page, int? pageSize, string? search, string? status, CancellationToken ct);
    Task<EstimatedBomView?> GetAsync(string bomNumber, CancellationToken ct);
    Task<EstimatedBomView> CreateAsync(CreateEstimatedBomRequest request, CancellationToken ct);
    Task<EstimatedBomView> ReplaceDraftAsync(string bomNumber, ReplaceEstimatedBomLinesRequest request, CancellationToken ct);
    Task<EstimatedBomView> SubmitAsync(string bomNumber, EstimatedBomActionRequest request, CancellationToken ct);
    Task<EstimatedBomView> ApproveAsync(string bomNumber, EstimatedBomActionRequest request, CancellationToken ct);
    Task<EstimatedBomView> CreateRevisionAsync(string bomNumber, NewEstimatedBomRevisionRequest request, CancellationToken ct);
    Task<IReadOnlyList<EstimatedBomHistoryView>> HistoryAsync(string bomNumber, CancellationToken ct);
    Task<EstimatedBomWorkbookFile> TemplateAsync(CancellationToken ct);
    Task<EstimatedBomView> ImportAsync(byte[] content, string idempotencyKey, CancellationToken ct);
    Task MergeItemAsync(Guid sourceItemId, MergeItemRequest request, CancellationToken ct);
}
