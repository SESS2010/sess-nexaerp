namespace SESS.NexaERP.Domain.Stores;

public sealed record ToolOpeningTypeRow(string SourceRow, string ToolTypeCode,
    int Purchased, int Issued, int Balance);
public sealed record ToolOpeningCustodyRow(string SourceRow, string ToolTypeCode,
    string HolderReference, int Quantity);
public sealed record ToolOpeningRegisterTotals(int Types, int Purchased, int Issued,
    int Balance, int CustodyLines, int Holders);
public sealed record ToolOpeningRegisterReconciliationResult(
    ToolOpeningRegisterTotals Totals, IReadOnlyList<string> Errors)
{
    public bool IsBalanced => Errors.Count == 0;
}

/// <summary>
/// Reconciles parsed source rows without fabricating missing assets, dates or values.
/// The importer must retain workbook identity, sheet/row references and original cells.
/// One custody row of quantity N requires N individual assets at actual import.
/// Passing this check alone does not authorize or perform an import.
/// </summary>
public static class ToolOpeningRegisterReconciliation
{
    public static readonly ToolOpeningRegisterTotals SessExpected = new(164, 730, 564, 166, 288, 23);

    public static ToolOpeningRegisterReconciliationResult Reconcile(
        IEnumerable<ToolOpeningTypeRow> typeRows, IEnumerable<ToolOpeningCustodyRow> custodyRows,
        ToolOpeningRegisterTotals expected)
    {
        ArgumentNullException.ThrowIfNull(typeRows); ArgumentNullException.ThrowIfNull(custodyRows);
        ArgumentNullException.ThrowIfNull(expected);
        var types = typeRows.ToArray(); var custody = custodyRows.ToArray();
        var errors = new List<string>();
        foreach (var row in types)
        {
            if (string.IsNullOrWhiteSpace(row.SourceRow) || string.IsNullOrWhiteSpace(row.ToolTypeCode))
                errors.Add("Every type requires its source row and mapped tool type code.");
            if (row.Purchased < 0 || row.Issued < 0 || row.Balance < 0 || row.Purchased - row.Issued != row.Balance)
                errors.Add($"Type {row.ToolTypeCode} at {row.SourceRow}: purchased minus issued must equal non-negative balance.");
        }
        foreach (var row in custody)
        {
            if (string.IsNullOrWhiteSpace(row.SourceRow) || string.IsNullOrWhiteSpace(row.ToolTypeCode)
                || string.IsNullOrWhiteSpace(row.HolderReference) || row.Quantity <= 0)
                errors.Add($"Custody at {row.SourceRow}: source row, mapped type, holder and positive individual quantity are required.");
        }
        AddDuplicateErrors(types.Select(x => x.SourceRow), "type source row", errors);
        AddDuplicateErrors(types.Select(x => x.ToolTypeCode), "tool type code", errors);
        AddDuplicateErrors(custody.Select(x => x.SourceRow), "custody source row", errors);
        var typeCodes = types.Select(x => x.ToolTypeCode).ToHashSet(StringComparer.Ordinal);
        foreach (var group in custody.GroupBy(x => x.ToolTypeCode, StringComparer.Ordinal))
            if (!typeCodes.Contains(group.Key)) errors.Add($"Custody references unknown tool type {group.Key}.");
        foreach (var type in types)
        {
            var held = custody.Where(x => string.Equals(x.ToolTypeCode, type.ToolTypeCode, StringComparison.Ordinal)).Sum(x => x.Quantity);
            if (held != type.Issued)
                errors.Add($"Type {type.ToolTypeCode}: custody quantity {held} differs from issued {type.Issued}.");
        }
        var totals = new ToolOpeningRegisterTotals(types.Length, types.Sum(x => x.Purchased),
            types.Sum(x => x.Issued), types.Sum(x => x.Balance), custody.Length,
            custody.Select(x => x.HolderReference).Distinct(StringComparer.Ordinal).Count());
        if (totals != expected) errors.Add($"Register control totals differ: expected {expected}; found {totals}.");
        return new(totals, Array.AsReadOnly(errors.ToArray()));
    }
    private static void AddDuplicateErrors(IEnumerable<string> values, string label, List<string> errors)
    {
        foreach (var group in values.GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1))
            errors.Add($"Duplicate {label}: {group.Key}.");
    }
}
