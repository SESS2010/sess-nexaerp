using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class EfMasterDataTransferService(
    NexaErpDbContext db,
    IMasterDataRegistry registry,
    ICurrentUser user,
    IPagePermissionService permissions,
    IDateTimeProvider clock,
    IOptions<MasterDataTransferOptions> configuredOptions) : IMasterDataTransferService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MasterDataWorkbookService workbooks = new();
    private readonly MasterDataTransferOptions options = ValidateOptions(configuredOptions.Value);

    public Task<MasterDataFileResult> CreateTemplateAsync(string masterKey, CancellationToken cancellationToken)
    {
        var definition = registry.GetRequired(masterKey).Definition;
        var content = workbooks.Create(definition, [], clock.UtcNow);
        return Task.FromResult(new MasterDataFileResult($"{definition.MasterKey}-template.xlsx", MasterDataWorkbookService.ContentType, content));
    }

    public async Task<MasterDataFileResult> ExportAsync(string masterKey, MasterDataExportQuery query, CancellationToken cancellationToken)
    {
        var adapter = registry.GetRequired(masterKey);
        var rows = await adapter.ExportAsync(query, cancellationToken);
        if (rows.Count > options.MaxRows)
            throw new MasterDataValidationException($"Filtered export contains {rows.Count} rows; the synchronous limit is {options.MaxRows}. Narrow the filters.");
        var content = workbooks.Create(adapter.Definition, rows, clock.UtcNow);
        return new($"{adapter.Definition.MasterKey}-export.xlsx", MasterDataWorkbookService.ContentType, content);
    }

    public async Task<MasterDataImportResult> ImportAsync(MasterDataImportRequest request, CancellationToken cancellationToken)
    {
        var adapter = registry.GetRequired(request.MasterKey);
        ValidateImportRequest(request);
        ValidateArchive(request.Content);
        var company = await ResolveCompanyAsync(cancellationToken);
        var employee = await db.Employees.AsNoTracking().SingleAsync(x => x.Id == user.EmployeeId!.Value, cancellationToken);
        var actingRole = await ResolveOperationalRoleAsync(adapter.Definition, cancellationToken);
        var fileHash = Hex(SHA256.HashData(request.Content));
        var fingerprint = Hex(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{adapter.Definition.MasterKey}\n{adapter.Definition.TemplateVersion}\n{request.Mode}\n{fileHash}")));

        var existingBatch = await db.MasterImportBatches.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CompanyId == company.Id
                && x.MasterKey == adapter.Definition.MasterKey
                && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingBatch is not null)
        {
            if (!string.Equals(existingBatch.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                throw new MasterDataConflictException("The idempotency key was already used for a different master-data import request.");
            return await MapBatchAsync(existingBatch.Id, includeRows: true, authorizeHistoricalRead: false, cancellationToken)
                ?? throw new InvalidOperationException("Idempotent import batch disappeared.");
        }

        var now = clock.UtcNow;
        var batch = new MasterImportBatch
        {
            MasterKey = adapter.Definition.MasterKey,
            TemplateVersion = adapter.Definition.TemplateVersion,
            CompanyId = company.Id,
            ImportMode = request.Mode,
            Status = MasterDataImportStatuses.Processing,
            OriginalFileName = SafeFileName(request.OriginalFileName),
            FileSizeBytes = request.Content.LongLength,
            FileSha256 = fileHash,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            RequestFingerprint = fingerprint,
            UploadedByEmployeeId = employee.Id,
            UploadedByEmployeeCode = employee.EmployeeCode,
            OperationalRoleCode = actingRole,
            UploadedAt = now,
            RetentionExpiresAt = now.AddDays(options.SensitiveRowRetentionDays),
            CorrelationId = request.CorrelationId,
            CreatedAt = now,
            CreatedBy = user.LoginId
        };
        db.MasterImportBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        MasterDataWorkbookReadResult workbook;
        try
        {
            workbook = workbooks.Read(request.Content, adapter.Definition, options.MaxRows);
        }
        catch (MasterDataValidationException ex)
        {
            await CompleteStructuralFailureAsync(batch.Id, ex.Message, cancellationToken);
            throw;
        }

        try
        {
            return await ValidateAndApplyAsync(batch.Id, adapter, workbook.Rows, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await MarkFailedIfProcessingAsync(batch.Id, "Import failed before a complete result could be committed.", cancellationToken);
            throw;
        }
    }

    public Task<MasterDataImportResult?> GetImportAsync(Guid batchId, CancellationToken cancellationToken) =>
        MapBatchAsync(batchId, includeRows: true, authorizeHistoricalRead: true, cancellationToken);

    public async Task<MasterDataImportRowsPage?> GetImportRowsAsync(
        Guid batchId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var batch = await GetAuthorizedBatchAsync(batchId, cancellationToken);
        if (batch is null) return null;
        var query = db.MasterImportRowResults.AsNoTracking().Where(x => x.ImportBatchId == batchId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.SourceRowNumber).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new(total, page, pageSize, rows.Select(MapRow).ToArray());
    }

    public async Task<MasterDataFileResult?> CreateErrorWorkbookAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await GetAuthorizedBatchAsync(batchId, cancellationToken);
        if (batch is null) return null;
        var adapter = registry.GetRequired(batch.MasterKey);
        var stored = await db.MasterImportRowResults.AsNoTracking()
            .Where(x => x.ImportBatchId == batchId
                && (x.Outcome == MasterDataRowOutcomes.Rejected || x.Outcome == MasterDataRowOutcomes.NotImported))
            .OrderBy(x => x.SourceRowNumber)
            .ToListAsync(cancellationToken);
        if (stored.Count == 0) throw new MasterDataNotFoundException("This import has no rejected or not-imported rows.");
        if (stored.Any(x => x.SubmittedValuesJson is null))
            throw new MasterDataNotFoundException("Sensitive row values have expired and the error workbook can no longer be reconstructed.");
        var rows = stored.Select(x => (
            new MasterDataRawRow(x.SourceRowNumber, DeserializeValues(x.SubmittedValuesJson!)),
            x.Outcome,
            DeserializeErrors(x.ErrorsJson))).ToArray();
        var content = workbooks.CreateErrorWorkbook(adapter.Definition, rows, clock.UtcNow);
        return new($"{batch.MasterKey}-import-{batch.Id}-errors.xlsx", MasterDataWorkbookService.ContentType, content);
    }

    private async Task<MasterDataImportResult> ValidateAndApplyAsync(
        Guid batchId,
        IMasterDataAdapter adapter,
        IReadOnlyList<MasterDataRawRow> rows,
        CancellationToken cancellationToken)
    {
        var submittedCodes = rows.Select(x => Value(x, adapter.Definition.BusinessCodeColumnKey))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => adapter.NormalizeBusinessCode(x!)).ToArray();
        var codes = submittedCodes.Distinct(StringComparer.Ordinal).ToArray();
        var ids = rows.Select(x => ParseGuid(Value(x, "RecordId"))).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var existing = await adapter.LoadExistingAsync(codes, ids, cancellationToken);
        var lookupContext = await adapter.LoadLookupContextAsync(rows, cancellationToken);
        var duplicateCodes = submittedCodes.GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
        var prepared = rows.Select(row => Prepare(row, adapter, existing, lookupContext, duplicateCodes)).ToArray();
        var invalid = prepared.Count(x => x.Errors.Count > 0);
        var valid = prepared.Length - invalid;

        var mode = await db.MasterImportBatches.AsNoTracking().Where(x => x.Id == batchId)
            .Select(x => x.ImportMode).SingleAsync(cancellationToken);
        if (mode == MasterDataImportModes.RejectEntireFile && invalid > 0)
        {
            await PersistRejectedWholeFileAsync(batchId, prepared, valid, invalid, cancellationToken);
            return await MapBatchAsync(batchId, includeRows: true, authorizeHistoricalRead: false, cancellationToken)
                ?? throw new InvalidOperationException("Rejected import batch disappeared.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var outcomes = new List<RowOutcome>(prepared.Length);
        foreach (var item in prepared)
        {
            if (item.Errors.Count > 0)
            {
                outcomes.Add(RowOutcome.Rejected(item));
                continue;
            }
            if (item.IntendedAction == MasterDataIntendedActions.NoChange)
            {
                outcomes.Add(RowOutcome.Unchanged(item));
                continue;
            }

            var savepoint = $"row_{item.Source.SourceRowNumber}";
            await transaction.CreateSavepointAsync(savepoint, cancellationToken);
            try
            {
                var result = item.IntendedAction == MasterDataIntendedActions.Create
                    ? await adapter.CreateAsync(item.Source, cancellationToken)
                    : await adapter.UpdateAsync(item.Existing!, item.Source, item.Version!.Value, cancellationToken);
                outcomes.Add(RowOutcome.Success(item, result));
                await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
            }
            catch (Exception ex) when (IsRowFailure(ex))
            {
                await transaction.RollbackToSavepointAsync(savepoint, cancellationToken);
                db.ChangeTracker.Clear();
                outcomes.Add(RowOutcome.ApplicationFailure(item, ex.Message));
                if (mode == MasterDataImportModes.RejectEntireFile) break;
            }
        }

        if (mode == MasterDataImportModes.RejectEntireFile && outcomes.Any(x => x.Outcome == MasterDataRowOutcomes.Rejected))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var completedRows = outcomes.ToDictionary(x => x.Item.Source.SourceRowNumber);
            var rejectedAll = prepared.Select(x => completedRows.TryGetValue(x.Source.SourceRowNumber, out var outcome)
                ? outcome
                : RowOutcome.NotImported(x)).ToArray();
            await PersistOutcomesAsync(batchId, rejectedAll, valid, invalid, MasterDataImportStatuses.Rejected, cancellationToken);
        }
        else
        {
            await PersistOutcomesAsync(batchId, outcomes, valid, invalid,
                outcomes.Any(x => x.Outcome == MasterDataRowOutcomes.Rejected)
                    ? MasterDataImportStatuses.CompletedWithErrors
                    : MasterDataImportStatuses.Completed,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        return await MapBatchAsync(batchId, includeRows: true, authorizeHistoricalRead: false, cancellationToken)
            ?? throw new InvalidOperationException("Completed import batch disappeared.");
    }

    internal static MasterDataPreparedRow Prepare(
        MasterDataRawRow row,
        IMasterDataAdapter adapter,
        MasterDataExistingSet existingSet,
        object? lookupContext,
        IReadOnlySet<string> duplicateCodes)
    {
        var errors = new List<MasterDataRowError>();
        var codeValue = Value(row, adapter.Definition.BusinessCodeColumnKey);
        var normalizedCode = string.IsNullOrWhiteSpace(codeValue) ? string.Empty : adapter.NormalizeBusinessCode(codeValue);
        if (normalizedCode.Length == 0)
            errors.Add(Error(adapter.Definition.BusinessCodeColumnKey, "Code", "REQUIRED", "Business code is required.", codeValue));
        if (normalizedCode.Length > 0 && duplicateCodes.Contains(normalizedCode))
            errors.Add(Error(adapter.Definition.BusinessCodeColumnKey, "Code", "DUPLICATE_IN_FILE", "Business code occurs more than once in this workbook.", codeValue));

        var idValue = Value(row, "RecordId");
        var recordId = ParseGuid(idValue);
        if (!string.IsNullOrWhiteSpace(idValue) && !recordId.HasValue)
            errors.Add(Error("RecordId", "Record ID", "INVALID_FORMAT", "Record ID must be a UUID.", idValue));
        var versionValue = Value(row, "Version");
        uint? version = null;
        if (!string.IsNullOrWhiteSpace(versionValue))
        {
            if (uint.TryParse(versionValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)) version = parsed;
            else errors.Add(Error("Version", "Version", "INVALID_FORMAT", "Version must be a non-negative whole number.", versionValue));
        }

        existingSet.ByCode.TryGetValue(normalizedCode, out var byCode);
        MasterDataExistingRecord? byId = null;
        if (recordId.HasValue) existingSet.ById.TryGetValue(recordId.Value, out byId);
        if (recordId.HasValue && byId is null)
            errors.Add(Error("RecordId", "Record ID", "RECORD_NOT_FOUND", "Record ID does not identify an existing record.", idValue));
        if (byId is not null && !string.Equals(byId.NormalizedBusinessCode, normalizedCode, StringComparison.Ordinal))
            errors.Add(Error(adapter.Definition.BusinessCodeColumnKey, "Code", "BUSINESS_CODE_IMMUTABLE", "Business code cannot be renamed by upload.", codeValue));
        if (byCode is not null && byId is not null && byCode.Id != byId.Id)
            errors.Add(Error("RecordId", "Record ID", "RECORD_ID_MISMATCH", "Record ID and business code identify different records.", idValue));

        var existing = byId ?? byCode;
        var action = MasterDataIntendedActions.Create;
        if (existing is not null)
        {
            if (!recordId.HasValue && !version.HasValue && adapter.IsMateriallyEqual(row, existing))
                action = MasterDataIntendedActions.NoChange;
            else
            {
                action = adapter.IsMateriallyEqual(row, existing) ? MasterDataIntendedActions.NoChange : MasterDataIntendedActions.Update;
                if (!recordId.HasValue)
                    errors.Add(Error("RecordId", "Record ID", "RECORD_ID_REQUIRED", "Existing records can be changed only from a current export containing Record ID.", idValue));
                if (!version.HasValue)
                    errors.Add(Error("Version", "Version", "VERSION_REQUIRED", "Existing records can be changed only from a current export containing Version.", versionValue));
                else if (version.Value != existing.Version)
                    errors.Add(Error("Version", "Version", "STALE_VERSION", $"Expected current Version {existing.Version}. Export current data and retry.", versionValue));
            }
        }
        else if (version.HasValue)
            errors.Add(Error("Version", "Version", "VERSION_WITH_NEW_RECORD", "Version must be blank for a new business code.", versionValue));

        errors.AddRange(adapter.Validate(row, existing, lookupContext));
        return new(row, codeValue?.Trim() ?? string.Empty, normalizedCode, recordId, version, existing, action, errors);
    }

    private async Task PersistRejectedWholeFileAsync(
        Guid batchId,
        IReadOnlyList<MasterDataPreparedRow> rows,
        int valid,
        int invalid,
        CancellationToken cancellationToken)
    {
        var outcomes = rows.Select(x => x.Errors.Count > 0 ? RowOutcome.Rejected(x) : RowOutcome.NotImported(x)).ToArray();
        await PersistOutcomesAsync(batchId, outcomes, valid, invalid, MasterDataImportStatuses.Rejected, cancellationToken);
    }

    private async Task PersistOutcomesAsync(
        Guid batchId,
        IReadOnlyList<RowOutcome> outcomes,
        int valid,
        int invalid,
        string status,
        CancellationToken cancellationToken)
    {
        var batch = await db.MasterImportBatches.SingleAsync(x => x.Id == batchId, cancellationToken);
        var now = clock.UtcNow;
        foreach (var outcome in outcomes)
        {
            db.MasterImportRowResults.Add(new()
            {
                ImportBatchId = batchId,
                SourceRowNumber = outcome.Item.Source.SourceRowNumber,
                BusinessCode = NullIfEmpty(outcome.Item.BusinessCode),
                NormalizedBusinessCode = NullIfEmpty(outcome.Item.NormalizedBusinessCode),
                IntendedAction = outcome.Item.IntendedAction,
                Outcome = outcome.Outcome,
                SubmittedValuesJson = outcome.Outcome is MasterDataRowOutcomes.Rejected or MasterDataRowOutcomes.NotImported
                    ? JsonSerializer.Serialize(outcome.Item.Source.Values, JsonOptions) : null,
                ErrorsJson = JsonSerializer.Serialize(outcome.Errors, JsonOptions),
                ResultRecordId = outcome.Result?.RecordId,
                ResultVersion = outcome.Result?.Version,
                ProcessedAt = now,
                CreatedAt = now,
                CreatedBy = user.LoginId
            });
        }
        batch.Status = status;
        batch.TotalRows = outcomes.Count;
        batch.ValidRows = valid;
        batch.InvalidRows = invalid;
        batch.CreatedRows = outcomes.Count(x => x.Outcome == MasterDataRowOutcomes.Created);
        batch.UpdatedRows = outcomes.Count(x => x.Outcome == MasterDataRowOutcomes.Updated);
        batch.UnchangedRows = outcomes.Count(x => x.Outcome == MasterDataRowOutcomes.Unchanged);
        batch.RejectedRows = outcomes.Count(x => x.Outcome == MasterDataRowOutcomes.Rejected);
        batch.NotImportedRows = outcomes.Count(x => x.Outcome == MasterDataRowOutcomes.NotImported);
        batch.CompletedAt = now;
        batch.UpdatedAt = now;
        batch.UpdatedBy = user.LoginId;
        batch.Version = checked(batch.Version + 1);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MasterDataImportResult?> MapBatchAsync(
        Guid batchId,
        bool includeRows,
        bool authorizeHistoricalRead,
        CancellationToken cancellationToken)
    {
        var batch = authorizeHistoricalRead
            ? await GetAuthorizedBatchAsync(batchId, cancellationToken)
            : await db.MasterImportBatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null) return null;
        var rows = includeRows
            ? await db.MasterImportRowResults.AsNoTracking().Where(x => x.ImportBatchId == batchId).OrderBy(x => x.SourceRowNumber).ToListAsync(cancellationToken)
            : [];
        return new(batch.Id, batch.MasterKey, batch.Status, batch.ImportMode, batch.TotalRows, batch.ValidRows,
            batch.InvalidRows, batch.CreatedRows, batch.UpdatedRows, batch.UnchangedRows, batch.RejectedRows,
            batch.NotImportedRows, batch.UploadedByEmployeeId, batch.UploadedAt, batch.CompletedAt,
            batch.RetentionExpiresAt, batch.SensitiveValuesPurgedAt, rows.Select(MapRow).ToArray());
    }

    private async Task<MasterImportBatch?> GetAuthorizedBatchAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var company = await ResolveCompanyAsync(cancellationToken);
        var batch = await db.MasterImportBatches.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == batchId && x.CompanyId == company.Id, cancellationToken);
        if (batch is null) return null;
        var definition = registry.GetRequired(batch.MasterKey).Definition;
        await RequireHistoricalPermissionsAsync(
            definition, batch.UploadedByEmployeeId == user.EmployeeId, user.RoleCodes, permissions, cancellationToken);
        return batch;
    }

    internal static async Task RequireHistoricalPermissionsAsync(
        IMasterDataDefinition definition,
        bool isUploader,
        IReadOnlyCollection<string> roleCodes,
        IPagePermissionService permissions,
        CancellationToken cancellationToken)
    {
        var permission = isUploader ? PagePermissionActions.View : PagePermissionActions.ViewAuditHistory;
        if (!await permissions.HasPermissionAsync(roleCodes, definition.PageKey, permission, cancellationToken))
            throw new UnauthorizedAccessException("The import batch is outside the caller's authorized audit scope.");
        if (definition.SensitiveResultPermission is { } sensitive
            && !await permissions.HasPermissionAsync(roleCodes, sensitive.PageKey, sensitive.Permission, cancellationToken))
            throw new UnauthorizedAccessException("Viewing retained row values requires the master's commercial-values permission.");
    }

    private async Task<Domain.Foundation.Company> ResolveCompanyAsync(CancellationToken cancellationToken)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException("Resolved employee and company scope are required.");
        return await db.Companies.AsNoTracking().SingleOrDefaultAsync(
            x => x.Code == user.OrganizationId && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Resolved organization does not identify an active company.");
    }

    private async Task<string> ResolveOperationalRoleAsync(IMasterDataDefinition definition, CancellationToken cancellationToken)
    {
        foreach (var role in definition.OperationalRolePriority)
        {
            if (!string.Equals(user.ActingRoleCode, role, StringComparison.OrdinalIgnoreCase)) continue;
            if (await permissions.HasPermissionAsync([role], definition.PageKey, PagePermissionActions.Create, cancellationToken)
                && await permissions.HasPermissionAsync([role], definition.PageKey, PagePermissionActions.Update, cancellationToken))
                return role;
        }
        throw new UnauthorizedAccessException("No effective role has both create and update authority for this master-data import.");
    }

    private async Task CompleteStructuralFailureAsync(Guid batchId, string message, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var batch = await db.MasterImportBatches.SingleAsync(x => x.Id == batchId, cancellationToken);
        batch.Status = MasterDataImportStatuses.Rejected;
        batch.FailureSummary = message.Length <= 2000 ? message : message[..2000];
        batch.CompletedAt = clock.UtcNow;
        batch.UpdatedAt = clock.UtcNow;
        batch.UpdatedBy = user.LoginId;
        batch.Version = checked(batch.Version + 1);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedIfProcessingAsync(Guid batchId, string message, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var batch = await db.MasterImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null || batch.Status != MasterDataImportStatuses.Processing) return;
        batch.Status = MasterDataImportStatuses.Failed;
        batch.FailureSummary = message;
        batch.CompletedAt = clock.UtcNow;
        batch.UpdatedAt = clock.UtcNow;
        batch.UpdatedBy = user.LoginId;
        batch.Version = checked(batch.Version + 1);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private void ValidateImportRequest(MasterDataImportRequest request)
    {
        if (request.Mode is not (MasterDataImportModes.ImportValidRows or MasterDataImportModes.RejectEntireFile))
            throw new MasterDataValidationException("Import mode must be IMPORT_VALID_ROWS or REJECT_ENTIRE_FILE.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Trim().Length > 128)
            throw new MasterDataValidationException("Idempotency key is required and cannot exceed 128 characters.");
        if (!string.Equals(Path.GetExtension(request.OriginalFileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new MasterDataValidationException("Only .xlsx workbooks are accepted.");
        if (request.Content.Length == 0 || request.Content.LongLength > options.MaxFileBytes)
            throw new MasterDataValidationException($"Workbook size must be between 1 byte and {options.MaxFileBytes} bytes.");
    }

    private void ValidateArchive(byte[] content)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                expanded = checked(expanded + entry.Length);
                var name = entry.FullName.Replace('\\', '/');
                if (name.Contains("vbaProject", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("externalLinks/", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                    throw new MasterDataValidationException("Macros, binary payloads and external workbook links are not allowed.");
            }
            if (expanded > options.MaxExpandedBytes)
                throw new MasterDataValidationException($"Expanded workbook content exceeds {options.MaxExpandedBytes} bytes.");
        }
        catch (InvalidDataException ex)
        {
            throw new MasterDataValidationException($"Workbook is not a valid .xlsx archive: {ex.Message}");
        }
    }

    private static MasterDataImportRowResult MapRow(MasterImportRowResult row) =>
        new(row.SourceRowNumber, row.BusinessCode,
            row.SubmittedValuesJson is null ? null : DeserializeValues(row.SubmittedValuesJson),
            row.Outcome, DeserializeErrors(row.ErrorsJson), row.ResultRecordId, row.ResultVersion);

    private static IReadOnlyDictionary<string, string?> DeserializeValues(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions)
        ?? new Dictionary<string, string?>(StringComparer.Ordinal);

    private static IReadOnlyList<MasterDataRowError> DeserializeErrors(string json) =>
        JsonSerializer.Deserialize<List<MasterDataRowError>>(json, JsonOptions) ?? [];

    private static string? Value(MasterDataRawRow row, string key) =>
        row.Values.TryGetValue(key, out var value) ? value : null;
    private static Guid? ParseGuid(string? value) => Guid.TryParse(value, out var parsed) ? parsed : null;
    private static string Hex(byte[] value) => Convert.ToHexStringLower(value);
    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
    private static string SafeFileName(string value)
    {
        var name = Path.GetFileName(value.Trim());
        return string.IsNullOrWhiteSpace(name) ? "upload.xlsx" : name.Length <= 255 ? name : name[..255];
    }
    private static MasterDataRowError Error(string key, string header, string code, string message, string? attempted) =>
        new(key, header, code, message, attempted);
    private static bool IsRowFailure(Exception ex) =>
        ex is MasterDataValidationException or MasterDataConflictException or MasterDataNotFoundException
        or DbUpdateException or DbUpdateConcurrencyException;
    private static MasterDataTransferOptions ValidateOptions(MasterDataTransferOptions value)
    {
        if (value.MaxRows is < 1 or > 1000) throw new InvalidOperationException("MasterDataTransfer:MaxRows must be from 1 through the synchronous ceiling of 1000.");
        if (value.MaxFileBytes is < 1 or > 50 * 1024 * 1024) throw new InvalidOperationException("MasterDataTransfer:MaxFileBytes must be from 1 byte through 50 MiB.");
        if (value.MaxExpandedBytes < value.MaxFileBytes || value.MaxExpandedBytes > 250 * 1024 * 1024) throw new InvalidOperationException("MasterDataTransfer:MaxExpandedBytes must be at least MaxFileBytes and at most 250 MiB.");
        if (value.SensitiveRowRetentionDays != 90) throw new InvalidOperationException("MasterDataTransfer:SensitiveRowRetentionDays is fixed at the approved 90 days.");
        return value;
    }

    private sealed record RowOutcome(
        MasterDataPreparedRow Item,
        string Outcome,
        IReadOnlyList<MasterDataRowError> Errors,
        MasterDataApplyResult? Result)
    {
        public static RowOutcome Rejected(MasterDataPreparedRow item) => new(item, MasterDataRowOutcomes.Rejected, item.Errors, null);
        public static RowOutcome NotImported(MasterDataPreparedRow item) => new(item, MasterDataRowOutcomes.NotImported, [], null);
        public static RowOutcome Unchanged(MasterDataPreparedRow item) => new(item, MasterDataRowOutcomes.Unchanged, [], new(item.Existing!.Id, item.Existing.Version));
        public static RowOutcome Success(MasterDataPreparedRow item, MasterDataApplyResult result) =>
            new(item, item.IntendedAction == MasterDataIntendedActions.Create ? MasterDataRowOutcomes.Created : MasterDataRowOutcomes.Updated, [], result);
        public static RowOutcome ApplicationFailure(MasterDataPreparedRow item, string message) =>
            new(item, MasterDataRowOutcomes.Rejected,
                [new("", "", "APPLICATION_REJECTED", message, null)], null);
    }
}
