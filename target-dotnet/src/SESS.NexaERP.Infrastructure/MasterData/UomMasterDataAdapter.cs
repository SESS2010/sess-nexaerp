using System.Globalization;
using System.Text.RegularExpressions;
using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class UomMasterDataDefinition : IMasterDataDefinition
{
    public string MasterKey => "uoms";
    public int TemplateVersion => 1;
    public string PageKey => "masters.uoms";
    public string BusinessCodeColumnKey => "Code";
    public IReadOnlyList<string> OperationalRolePriority { get; } =
        ["PURCHASE_MANAGER", "STORES_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR"];
    public MasterDataSensitivePermission? SensitiveResultPermission => null;
    public IReadOnlyList<string> WorkbookGuideNotes => [];
    public IReadOnlyList<MasterDataColumnDefinition> Columns { get; } =
    [
        new("RecordId", "Record ID", MasterDataColumnType.Guid, false, true, false, "UUID; blank for new rows", "Existing UOM identifier", null, "Read-only identity used to detect business-code rename attempts."),
        new("Version", "Version", MasterDataColumnType.UnsignedInteger, false, true, false, "Whole number; blank for new rows", "Current exported version", null, "Read-only optimistic concurrency version."),
        new("Code", "Code", MasterDataColumnType.Text, true, true, true, "Uppercase text, maximum 32 characters", "Unique UOM business code", null, "Immutable business code.", 32),
        new("Name", "Name", MasterDataColumnType.Text, true, true, true, "Text, maximum 120 characters", "Non-blank text", null, "UOM display name.", 120),
        new("MeasurementDimension", "Measurement Dimension", MasterDataColumnType.Text, true, true, true, "Uppercase code, maximum 40 characters", "Uppercase dimension code; examples COUNT, MASS, LENGTH, VOLUME", null, "Dimension used to prevent invalid conversions.", 40),
        new("QuantityPrecision", "Quantity Precision", MasterDataColumnType.Integer, true, true, true, "Whole number from 0 through 6", "0, 1, 2, 3, 4, 5, 6", null, "Decimal places allowed for quantities."),
        new("IsActive", "Is Active", MasterDataColumnType.Boolean, false, false, false, "TRUE or FALSE", "TRUE, FALSE", null, "Read-only lifecycle state; use the governed deactivate API.")
    ];
}

public sealed partial class UomMasterDataAdapter(IUomMasterService service) : IMasterDataAdapter
{
    private static readonly Regex DimensionPattern = DimensionRegex();
    public IMasterDataDefinition Definition { get; } = new UomMasterDataDefinition();

    public string NormalizeBusinessCode(string value) => value.Trim().ToUpperInvariant();

    public async Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken) =>
        (await service.ExportAsync(query, cancellationToken))
        .Select(x => new MasterDataExportRow(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RecordId"] = x.Id.ToString(),
            ["Version"] = x.Version,
            ["Code"] = x.Code,
            ["Name"] = x.Name,
            ["MeasurementDimension"] = x.MeasurementDimension,
            ["QuantityPrecision"] = x.QuantityPrecision,
            ["IsActive"] = x.IsActive
        })).ToArray();

    public Task<MasterDataExistingSet> LoadExistingAsync(
        IReadOnlyCollection<string> normalizedCodes,
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken) =>
        service.LoadExistingAsync(normalizedCodes, recordIds, cancellationToken);

    public Task<object?> LoadLookupContextAsync(IReadOnlyList<MasterDataRawRow> rows, CancellationToken cancellationToken) =>
        Task.FromResult<object?>(null);

    public IReadOnlyList<MasterDataRowError> Validate(
        MasterDataRawRow row,
        MasterDataExistingRecord? existing,
        object? lookupContext)
    {
        var errors = new List<MasterDataRowError>();
        RequiredText(row, "Code", "Code", 32, errors);
        RequiredText(row, "Name", "Name", 120, errors);
        var dimension = RequiredText(row, "MeasurementDimension", "Measurement Dimension", 40, errors);
        if (!string.IsNullOrWhiteSpace(dimension) && !DimensionPattern.IsMatch(dimension.Trim()))
            errors.Add(Error("MeasurementDimension", "Measurement Dimension", "INVALID_FORMAT", "Use an uppercase dimension code containing only A-Z, 0-9 and underscore.", dimension));

        var precisionValue = Value(row, "QuantityPrecision");
        if (!int.TryParse(precisionValue, NumberStyles.None, CultureInfo.InvariantCulture, out var precision) || precision is < 0 or > 6)
            errors.Add(Error("QuantityPrecision", "Quantity Precision", "INVALID_VALUE", "Quantity precision must be a whole number from 0 through 6.", precisionValue));

        var activeValue = Value(row, "IsActive");
        if (!string.IsNullOrWhiteSpace(activeValue) && !bool.TryParse(activeValue, out var active))
            errors.Add(Error("IsActive", "Is Active", "INVALID_VALUE", "Is Active must be TRUE or FALSE.", activeValue));
        else if (existing is null && bool.TryParse(activeValue, out active) && !active)
            errors.Add(Error("IsActive", "Is Active", "READ_ONLY", "New UOMs are active. Use the governed deactivate API after creation.", activeValue));
        else if (existing is not null && bool.TryParse(activeValue, out active)
            && existing.MaterialValues.TryGetValue("IsActive", out var current)
            && active != bool.Parse(current!))
            errors.Add(Error("IsActive", "Is Active", "READ_ONLY", "Is Active cannot be changed by upload. Use the governed lifecycle API.", activeValue));
        return errors;
    }

    public bool IsMateriallyEqual(MasterDataRawRow row, MasterDataExistingRecord existing) =>
        Equal(row, existing, "Code", NormalizeBusinessCode)
        && Equal(row, existing, "Name", x => x.Trim())
        && Equal(row, existing, "MeasurementDimension", x => x.Trim().ToUpperInvariant())
        && Equal(row, existing, "QuantityPrecision", x => x.Trim())
        && (string.IsNullOrWhiteSpace(Value(row, "IsActive")) || Equal(row, existing, "IsActive", x => x.Trim().ToUpperInvariant()));

    public async Task<MasterDataApplyResult> CreateAsync(MasterDataRawRow row, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(Request(row, null), cancellationToken);
        return new(result.Id, result.Version);
    }

    public async Task<MasterDataApplyResult> UpdateAsync(
        MasterDataExistingRecord existing,
        MasterDataRawRow row,
        uint expectedVersion,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(existing.Id, Request(row, expectedVersion), cancellationToken);
        return new(result.Id, result.Version);
    }

    private static UpsertUomMasterRequest Request(MasterDataRawRow row, uint? version) => new(
        Value(row, "Code")!,
        Value(row, "Name")!,
        Value(row, "MeasurementDimension")!,
        version,
        int.Parse(Value(row, "QuantityPrecision")!, CultureInfo.InvariantCulture));

    private static string? RequiredText(
        MasterDataRawRow row,
        string key,
        string header,
        int maximum,
        ICollection<MasterDataRowError> errors)
    {
        var value = Value(row, key);
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(Error(key, header, "REQUIRED", $"{header} is required.", value));
        else if (value.Trim().Length > maximum)
            errors.Add(Error(key, header, "MAX_LENGTH", $"{header} cannot exceed {maximum} characters.", value));
        return value;
    }

    private static bool Equal(
        MasterDataRawRow row,
        MasterDataExistingRecord existing,
        string key,
        Func<string, string> normalize)
    {
        var submitted = Value(row, key);
        return submitted is not null
            && existing.MaterialValues.TryGetValue(key, out var current)
            && current is not null
            && string.Equals(normalize(submitted), normalize(current), StringComparison.Ordinal);
    }

    private static string? Value(MasterDataRawRow row, string key) =>
        row.Values.TryGetValue(key, out var value) ? value : null;

    private static MasterDataRowError Error(string key, string header, string code, string message, string? attempted) =>
        new(key, header, code, message, attempted);

    [GeneratedRegex("^[A-Z][A-Z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex DimensionRegex();
}
