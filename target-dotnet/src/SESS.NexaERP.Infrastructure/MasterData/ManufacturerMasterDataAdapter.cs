using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.MasterData;

public sealed class ManufacturerImportDefinition : IMasterDataDefinition
{
    public string MasterKey => "manufacturers";
    public int TemplateVersion => 1;
    public string PageKey => "masters.manufacturers";
    public string BusinessCodeColumnKey => "Code";
    public IReadOnlyList<string> OperationalRolePriority => ["STORES_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR"];
    public MasterDataSensitivePermission? SensitiveResultPermission => null;
    public IReadOnlyList<string> WorkbookGuideNotes => ["All rows must be valid before any manufacturer is changed.", "New manufacturers are active. Existing changes require Record ID and Version from a current export. Use the governed lifecycle action to deactivate."];
    public IReadOnlyList<MasterDataColumnDefinition> Columns =>
    [
        ImportFields.R("RecordId", "Record ID", MasterDataColumnType.Guid),
        ImportFields.R("Version", "Version", MasterDataColumnType.UnsignedInteger),
        ImportFields.C("Code", "Code", MasterDataColumnType.Text, true, "Uppercase text, maximum 80", "Unique code", "Manufacturer code; preserved as text"),
        ImportFields.C("Name", "Name", MasterDataColumnType.Text, true, "Text, maximum 180", "Non-blank name", "Manufacturer name"),
        ImportFields.R("IsActive", "Is Active", MasterDataColumnType.Boolean)
    ];
    public IReadOnlyList<MasterDataExportRow> TemplateExampleRows =>
        [ImportFields.Example(("Code", "MAKE-001"), ("Name", "Example Manufacturer"), ("IsActive", true))];
}

public sealed class ManufacturerMasterDataAdapter(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IMasterDataAdapter
{
    public IMasterDataDefinition Definition { get; } = new ManufacturerImportDefinition();
    public string NormalizeBusinessCode(string value) => ImportFields.Code(value);

    public async Task<IReadOnlyList<MasterDataExportRow>> ExportAsync(MasterDataExportQuery query, CancellationToken ct)
    {
        var rows = db.Manufacturers.AsNoTracking();
        if (query.IsActive.HasValue) rows = rows.Where(x => x.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            rows = rows.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
        }
        return (await rows.OrderBy(x => x.Code).ToListAsync(ct)).Select(x =>
            ImportFields.Example(("RecordId", x.Id), ("Version", x.Version), ("Code", x.Code), ("Name", x.Name), ("IsActive", x.IsActive))).ToArray();
    }

    public async Task<MasterDataExistingSet> LoadExistingAsync(IReadOnlyCollection<string> codes, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var rows = await db.Manufacturers.AsNoTracking().Where(x => codes.Contains(x.Code) || ids.Contains(x.Id)).ToListAsync(ct);
        return ImportFields.Set(rows.Select(x => new MasterDataExistingRecord(x.Id, x.Code, x.Code, x.Version,
            new Dictionary<string, string?> { ["Code"] = x.Code, ["Name"] = x.Name, ["IsActive"] = x.IsActive.ToString() })));
    }
    public Task<object?> LoadLookupContextAsync(IReadOnlyList<MasterDataRawRow> rows, CancellationToken ct) => Task.FromResult<object?>(null);

    public IReadOnlyList<MasterDataRowError> Validate(MasterDataRawRow row, MasterDataExistingRecord? existing, object? context)
    {
        var errors = new List<MasterDataRowError>();
        foreach (var (key, limit) in new[] { ("Code", 80), ("Name", 180) })
        {
            ImportFields.Required(row, key, key, errors);
            if ((ImportFields.V(row, key)?.Length ?? 0) > limit)
                errors.Add(ImportFields.Error(key, key, "MAX_LENGTH", $"{key} cannot exceed {limit} characters.", ImportFields.V(row, key)));
        }
        var active = ImportFields.V(row, "IsActive");
        if (!string.IsNullOrWhiteSpace(active))
        {
            if (!bool.TryParse(active, out var value))
                errors.Add(ImportFields.Error("IsActive", "Is Active", "INVALID_VALUE", "Use TRUE or FALSE.", active));
            else if (value != (existing is null || bool.Parse(existing.MaterialValues["IsActive"]!)))
                errors.Add(ImportFields.Error("IsActive", "Is Active", "READ_ONLY", "Use the governed lifecycle action to change active status.", active));
        }
        return errors;
    }

    public bool IsMateriallyEqual(MasterDataRawRow row, MasterDataExistingRecord existing) =>
        NormalizeBusinessCode(ImportFields.V(row, "Code") ?? "") == existing.NormalizedBusinessCode
        && ImportFields.V(row, "Name") == existing.MaterialValues["Name"];

    public async Task<MasterDataApplyResult> CreateAsync(MasterDataRawRow row, CancellationToken ct)
    {
        var entity = new Manufacturer { Code = NormalizeBusinessCode(ImportFields.V(row, "Code")!), Name = ImportFields.V(row, "Name")!, CreatedBy = user.LoginId };
        db.Manufacturers.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Masters", "ImportCreate", nameof(Manufacturer), entity.Id.ToString(), null,
            new { entity.Code, entity.Name, entity.IsActive, entity.Version }, ct);
        return new(entity.Id, entity.Version);
    }
    public async Task<MasterDataApplyResult> UpdateAsync(MasterDataExistingRecord existing, MasterDataRawRow row, uint version, CancellationToken ct)
    {
        var entity = await db.Manufacturers.SingleOrDefaultAsync(x => x.Id == existing.Id, ct)
            ?? throw new MasterDataNotFoundException("Manufacturer no longer exists.");
        if (entity.Version != version) throw new MasterDataConflictException("Stale manufacturer version; export again.");
        var before = new { entity.Code, entity.Name, entity.IsActive, entity.Version };
        entity.Name = ImportFields.V(row, "Name")!;
        entity.Version = checked(entity.Version + 1);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = user.LoginId;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Masters", "ImportUpdate", nameof(Manufacturer), entity.Id.ToString(), before,
            new { entity.Code, entity.Name, entity.IsActive, entity.Version }, ct);
        return new(entity.Id, entity.Version);
    }
}