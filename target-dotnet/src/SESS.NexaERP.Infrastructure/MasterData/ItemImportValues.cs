using System.Globalization;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Inventory;

namespace SESS.NexaERP.Infrastructure.MasterData;

internal static class ItemImportValues
{
    internal static MasterDataRawRow Normalize(MasterDataRawRow row, MasterDataExistingRecord? existing)
    {
        var values = row.Values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        foreach (var (key, fallback) in new[] { ("ItemType", ItemTypes.RawMaterial), ("IsReturnable", "FALSE"),
            ("SerialPolicy", "NONE"), ("ReorderLevel", "0"), ("PartNumber", (string?)null),
            ("Barcode", (string?)null), ("StandardEstimatedPrice", (string?)null) })
        {
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                values[key] = existing?.MaterialValues.GetValueOrDefault(key) ?? fallback;
        }
        var returnable = values["IsReturnable"]?.Trim();
        if (string.Equals(returnable, "YES", StringComparison.OrdinalIgnoreCase)) values["IsReturnable"] = "TRUE";
        if (string.Equals(returnable, "NO", StringComparison.OrdinalIgnoreCase)) values["IsReturnable"] = "FALSE";
        return new(row.SourceRowNumber, values);
    }

    internal static void Validate(MasterDataRawRow row, List<MasterDataRowError> errors)
    {
        foreach (var (key, header, limit) in new[] { ("PartNumber", "Model/Part No", 120), ("Barcode", "Bar Code", 128) })
            if ((ImportFields.V(row, key)?.Length ?? 0) > limit)
                errors.Add(ImportFields.Error(key, header, "TOO_LONG", $"{header} must be at most {limit} characters.", ImportFields.V(row, key)));
        var cost = ImportFields.V(row, "StandardEstimatedPrice");
        if (!string.IsNullOrWhiteSpace(cost) && (!decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 0 || parsed >= 10000000000000000m || decimal.Round(parsed, 2) != parsed))
            errors.Add(ImportFields.Error("StandardEstimatedPrice", "Cost", "INVALID_VALUE", "Cost must be non-negative, with at most 16 integer digits and 2 decimal places.", cost));
    }

    internal static void Apply(Item item, MasterDataRawRow row)
    {
        item.PartNumber = Null(ImportFields.V(row, "PartNumber"));
        item.Barcode = Null(ImportFields.V(row, "Barcode"));
        item.StandardEstimatedPrice = Null(ImportFields.V(row, "StandardEstimatedPrice")) is { } cost
            ? decimal.Parse(cost, CultureInfo.InvariantCulture) : null;
    }

    private static string? Null(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
