using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Masters;

public sealed class EfUomMasterService(
    NexaErpDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IDateTimeProvider clock) : IUomMasterService
{
    public async Task<IReadOnlyList<UomSummary>> ExportAsync(MasterDataExportQuery query, CancellationToken cancellationToken)
    {
        var rows = db.Uoms.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            rows = rows.Where(x => x.Code.ToUpper().Contains(term)
                || x.Name.ToUpper().Contains(term)
                || x.MeasurementDimension.ToUpper().Contains(term));
        }
        if (query.IsActive.HasValue) rows = rows.Where(x => x.IsActive == query.IsActive.Value);
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        rows = string.Equals(query.SortBy, "name", StringComparison.OrdinalIgnoreCase)
            ? descending ? rows.OrderByDescending(x => x.Name).ThenByDescending(x => x.Code) : rows.OrderBy(x => x.Name).ThenBy(x => x.Code)
            : descending ? rows.OrderByDescending(x => x.Code) : rows.OrderBy(x => x.Code);
        return await rows.Select(x => Summary(x)).ToListAsync(cancellationToken);
    }

    public async Task<MasterDataExistingSet> LoadExistingAsync(
        IReadOnlyCollection<string> normalizedCodes,
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken)
    {
        var rows = await db.Uoms.AsNoTracking()
            .Where(x => normalizedCodes.Contains(x.Code) || recordIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var records = rows.Select(ToExisting).ToArray();
        return new(
            records.ToDictionary(x => x.NormalizedBusinessCode, StringComparer.Ordinal),
            records.ToDictionary(x => x.Id));
    }

    public async Task<UomSummary> CreateAsync(UpsertUomMasterRequest request, CancellationToken cancellationToken)
    {
        var values = Validate(request, creating: true);
        if (await db.Uoms.AnyAsync(x => x.Code == values.Code, cancellationToken))
            throw new MasterDataConflictException("UOM code already exists.");

        var row = new Uom
        {
            Code = values.Code,
            Name = values.Name,
            MeasurementDimension = values.Dimension,
            QuantityPrecision = values.Precision,
            CreatedAt = clock.UtcNow,
            CreatedBy = user.LoginId
        };
        db.Uoms.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", "Create", nameof(Uom), row.Id.ToString(), null, Summary(row), cancellationToken);
        return Summary(row);
    }

    public async Task<UomSummary> UpdateAsync(Guid id, UpsertUomMasterRequest request, CancellationToken cancellationToken)
    {
        var row = await db.Uoms.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new MasterDataNotFoundException("UOM not found.");
        if (!request.Version.HasValue || request.Version.Value != row.Version)
            throw new MasterDataConflictException("Stale record version. Refresh and retry.");

        var values = Validate(request, creating: false, row.QuantityPrecision);
        if (!string.Equals(row.Code, values.Code, StringComparison.Ordinal))
            throw new MasterDataValidationException("UOM business code is immutable. Use the governed rename API.");

        var before = Summary(row);
        row.Name = values.Name;
        row.MeasurementDimension = values.Dimension;
        row.QuantityPrecision = values.Precision;
        row.Version = checked(row.Version + 1);
        row.UpdatedAt = clock.UtcNow;
        row.UpdatedBy = user.LoginId;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("Masters", "Update", nameof(Uom), row.Id.ToString(), before, Summary(row), cancellationToken);
        return Summary(row);
    }

    private static (string Code, string Name, string Dimension, int Precision) Validate(
        UpsertUomMasterRequest request,
        bool creating,
        int currentPrecision = 6)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        var dimension = request.MeasurementDimension.Trim().ToUpperInvariant();
        var precision = request.QuantityPrecision ?? (creating ? 6 : currentPrecision);
        if (code.Length is 0 or > 32 || name.Length is 0 or > 120 || dimension.Length is 0 or > 40)
            throw new MasterDataValidationException("UOM code, name and measurement dimension are required and must fit their documented lengths.");
        if (precision is < 0 or > 6)
            throw new MasterDataValidationException("Quantity precision must be a whole number from 0 through 6.");
        return (code, name, dimension, precision);
    }

    private static UomSummary Summary(Uom x) =>
        new(x.Id, x.Code, x.Name, x.MeasurementDimension, x.QuantityPrecision, x.IsActive, x.Version);

    private static MasterDataExistingRecord ToExisting(Uom x) => new(
        x.Id,
        x.Code,
        x.Code.Trim().ToUpperInvariant(),
        x.Version,
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Code"] = x.Code,
            ["Name"] = x.Name,
            ["MeasurementDimension"] = x.MeasurementDimension,
            ["QuantityPrecision"] = x.QuantityPrecision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["IsActive"] = x.IsActive ? "TRUE" : "FALSE"
        });
}
