namespace SESS.NexaERP.Application.Masters;

public static class MasterDataImportModes
{
    public const string ImportValidRows = "IMPORT_VALID_ROWS";
    public const string RejectEntireFile = "REJECT_ENTIRE_FILE";
}

public static class MasterDataImportStatuses
{
    public const string Processing = "PROCESSING";
    public const string Completed = "COMPLETED";
    public const string CompletedWithErrors = "COMPLETED_WITH_ERRORS";
    public const string Rejected = "REJECTED";
    public const string Failed = "FAILED";
}

public static class MasterDataRowOutcomes
{
    public const string Created = "CREATED";
    public const string Updated = "UPDATED";
    public const string Unchanged = "UNCHANGED";
    public const string Rejected = "REJECTED";
    public const string NotImported = "NOT_IMPORTED";
}

public static class MasterDataIntendedActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string NoChange = "NO_CHANGE";
}

public enum MasterDataColumnType
{
    Text, Guid, UnsignedInteger, Integer, Boolean, Decimal, Date
}

public sealed record MasterDataColumnDefinition(
    string Key,
    string Header,
    MasterDataColumnType Type,
    bool RequiredOnCreate,
    bool RequiredOnUpdate,
    bool Editable,
    string Format,
    string AllowedValues,
    string? LookupMasterKey,
    string Description,
    int? MaximumLength = null);

public sealed record MasterDataSensitivePermission(string PageKey, string Permission);

public interface IMasterDataDefinition
{
    string MasterKey { get; }
    int TemplateVersion { get; }
    string PageKey { get; }
    string BusinessCodeColumnKey { get; }
    IReadOnlyList<string> OperationalRolePriority { get; }
    MasterDataSensitivePermission? SensitiveResultPermission { get; }
    IReadOnlyList<string> WorkbookGuideNotes { get; }
    IReadOnlyList<MasterDataColumnDefinition> Columns { get; }
}

public sealed record MasterDataRawRow(int SourceRowNumber, IReadOnlyDictionary<string, string?> Values);
public sealed record MasterDataExportRow(IReadOnlyDictionary<string, object?> Values);
public sealed record MasterDataExportQuery(string? Search, bool? IsActive, string? SortBy, string? SortDirection);

public sealed record MasterDataExistingRecord(
    Guid Id,
    string BusinessCode,
    string NormalizedBusinessCode,
    uint Version,
    IReadOnlyDictionary<string, string?> MaterialValues);

public sealed record MasterDataExistingSet(
    IReadOnlyDictionary<string, MasterDataExistingRecord> ByCode,
    IReadOnlyDictionary<Guid, MasterDataExistingRecord> ById);

public sealed record MasterDataRowError(
    string ColumnKey,
    string ColumnHeader,
    string Code,
    string Message,
    string? AttemptedValue);

public sealed record MasterDataPreparedRow(
    MasterDataRawRow Source,
    string BusinessCode,
    string NormalizedBusinessCode,
    Guid? RecordId,
    uint? Version,
    MasterDataExistingRecord? Existing,
    string IntendedAction,
    IReadOnlyList<MasterDataRowError> Errors);

public sealed record MasterDataApplyResult(Guid RecordId, uint Version);
public sealed record MasterDataPartyIdentityRecord(Guid Id, string BusinessCode, string? GstNumber, string? PanNumber, string LegalName);

public interface IMasterDataAdapter
{
    IMasterDataDefinition Definition { get; }
    string NormalizeBusinessCode(string value);
    Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(
        IReadOnlyCollection<string> normalizedCodes,
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken);
    Task<object?> LoadLookupContextAsync(IReadOnlyList<MasterDataRawRow> rows, CancellationToken cancellationToken);
    IReadOnlyList<MasterDataRowError> Validate(MasterDataRawRow row, MasterDataExistingRecord? existing, object? lookupContext);
    bool IsMateriallyEqual(MasterDataRawRow row, MasterDataExistingRecord existing);
    Task<MasterDataApplyResult> CreateAsync(MasterDataRawRow row, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, MasterDataRawRow row, uint expectedVersion, CancellationToken cancellationToken);
}

public interface IMasterDataRegistry
{
    IMasterDataAdapter GetRequired(string masterKey);
    bool TryGet(string masterKey, out IMasterDataAdapter? adapter);
}

public sealed record MasterDataFileResult(string FileName, string ContentType, byte[] Content);

public sealed record MasterDataImportRequest(
    string MasterKey,
    string Mode,
    string IdempotencyKey,
    string OriginalFileName,
    byte[] Content,
    Guid CorrelationId);

public sealed record MasterDataImportRowResult(
    int SourceRowNumber,
    string? BusinessCode,
    IReadOnlyDictionary<string, string?>? SubmittedValues,
    string Outcome,
    IReadOnlyList<MasterDataRowError> Errors,
    Guid? ResultRecordId,
    uint? ResultVersion);

public sealed record MasterDataImportResult(
    Guid BatchId,
    string MasterKey,
    string Status,
    string Mode,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int CreatedRows,
    int UpdatedRows,
    int UnchangedRows,
    int RejectedRows,
    int NotImportedRows,
    Guid UploadedByEmployeeId,
    DateTimeOffset UploadedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset RetentionExpiresAt,
    DateTimeOffset? SensitiveValuesPurgedAt,
    IReadOnlyList<MasterDataImportRowResult> Rows);

public sealed record MasterDataImportRowsPage(int Total, int Page, int PageSize, IReadOnlyList<MasterDataImportRowResult> Rows);

public interface IMasterDataTransferService
{
    Task<MasterDataFileResult> CreateTemplateAsync(string masterKey, CancellationToken cancellationToken);
    Task<MasterDataFileResult> ExportAsync(string masterKey, MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataImportResult> ImportAsync(MasterDataImportRequest request, CancellationToken cancellationToken);
    Task<MasterDataImportResult?> GetImportAsync(Guid batchId, CancellationToken cancellationToken);
    Task<MasterDataImportRowsPage?> GetImportRowsAsync(Guid batchId, int page, int pageSize, CancellationToken cancellationToken);
    Task<MasterDataFileResult?> CreateErrorWorkbookAsync(Guid batchId, CancellationToken cancellationToken);
}

public sealed class MasterDataValidationException(string message) : Exception(message);
public sealed class MasterDataConflictException(string message) : Exception(message);
public sealed class MasterDataNotFoundException(string message) : Exception(message);

public interface IUomMasterService
{
    Task<IReadOnlyList<UomSummary>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken);
    Task<UomSummary> CreateAsync(UpsertUomMasterRequest request, CancellationToken cancellationToken);
    Task<UomSummary> UpdateAsync(Guid id, UpsertUomMasterRequest request, CancellationToken cancellationToken);
}

public interface ICustomerMasterDataService
{
    Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> CreateAsync(UpsertCustomerRequest request, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertCustomerRequest request, CancellationToken cancellationToken);
}

public interface IVendorMasterDataService
{
    Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<MasterDataPartyIdentityRecord>> LoadIdentityRecordsAsync(IReadOnlyCollection<string> gstins, IReadOnlyCollection<string> pans, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> CreateAsync(UpsertVendorRequest request, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, UpsertVendorRequest request, CancellationToken cancellationToken);
}

public interface IWarehouseMasterDataService
{
    Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> CreateAsync(SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest request, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest request, CancellationToken cancellationToken);
}

public interface IRackBinMasterDataService
{
    Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken);
    Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> normalizedCodes, IReadOnlyCollection<Guid> recordIds, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> CreateAsync(SESS.NexaERP.Application.Inventory.UpsertRackBinRequest request, CancellationToken cancellationToken);
    Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, SESS.NexaERP.Application.Inventory.UpsertRackBinRequest request, CancellationToken cancellationToken);
}
