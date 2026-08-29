using System.Globalization;
using ClosedXML.Excel;
using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

internal sealed record MasterDataWorkbookReadResult(
    string MasterKey,
    int TemplateVersion,
    IReadOnlyList<MasterDataRawRow> Rows);

internal sealed class MasterDataWorkbookService
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Create(IMasterDataDefinition definition, IReadOnlyList<MasterDataExportRow> rows, DateTimeOffset generatedAt)
    {
        using var workbook = new XLWorkbook();
        var data = workbook.AddWorksheet("Data");
        WriteHeaders(data, definition.Columns);
        for (var column = 0; column < definition.Columns.Count; column++)
        {
            if (definition.Columns[column].Type is MasterDataColumnType.Text or MasterDataColumnType.Guid)
                data.Column(column + 1).Style.NumberFormat.Format = "@";
        }
        for (var index = 0; index < rows.Count; index++)
        {
            var targetRow = index + 2;
            for (var column = 0; column < definition.Columns.Count; column++)
            {
                var key = definition.Columns[column].Key;
                rows[index].Values.TryGetValue(key, out var value);
                WriteValue(data.Cell(targetRow, column + 1), value);
            }
        }
        data.SheetView.FreezeRows(1);
        data.Row(1).Style.Font.Bold = true;
        data.Columns(1, definition.Columns.Count).Width = 24;

        var guide = workbook.AddWorksheet("Column Guide");
        var guideHeaders = new[] { "Column", "Required on Create", "Required on Update", "Editable", "Format", "Allowed Values", "Lookup Master", "Description" };
        for (var index = 0; index < guideHeaders.Length; index++) WriteValue(guide.Cell(1, index + 1), guideHeaders[index]);
        guide.Row(1).Style.Font.Bold = true;
        for (var index = 0; index < definition.Columns.Count; index++)
        {
            var column = definition.Columns[index];
            var row = index + 2;
            WriteValue(guide.Cell(row, 1), column.Header);
            WriteValue(guide.Cell(row, 2), column.RequiredOnCreate ? "YES" : "NO");
            WriteValue(guide.Cell(row, 3), column.RequiredOnUpdate ? "YES" : "NO");
            WriteValue(guide.Cell(row, 4), column.Editable ? "YES" : "NO");
            WriteValue(guide.Cell(row, 5), column.Format);
            WriteValue(guide.Cell(row, 6), column.AllowedValues);
            WriteValue(guide.Cell(row, 7), column.LookupMasterKey ?? string.Empty);
            WriteValue(guide.Cell(row, 8), column.Description);
        }
        if (definition.WorkbookGuideNotes.Count > 0)
        {
            var noteRow = definition.Columns.Count + 3;
            WriteValue(guide.Cell(noteRow, 1), "Master Notes");
            guide.Cell(noteRow, 1).Style.Font.Bold = true;
            for (var index = 0; index < definition.WorkbookGuideNotes.Count; index++)
            {
                WriteValue(guide.Cell(noteRow + index + 1, 1), $"NOTE {index + 1}");
                WriteValue(guide.Cell(noteRow + index + 1, 8), definition.WorkbookGuideNotes[index]);
            }
        }
        guide.SheetView.FreezeRows(1);
        guide.Columns(1, 8).Width = 28;

        var metadata = workbook.AddWorksheet("_Metadata");
        WriteValue(metadata.Cell(1, 1), "MasterKey");
        WriteValue(metadata.Cell(1, 2), definition.MasterKey);
        WriteValue(metadata.Cell(2, 1), "TemplateVersion");
        WriteValue(metadata.Cell(2, 2), definition.TemplateVersion);
        WriteValue(metadata.Cell(3, 1), "GeneratedAtUtc");
        WriteValue(metadata.Cell(3, 2), generatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        metadata.Visibility = XLWorksheetVisibility.VeryHidden;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public MasterDataWorkbookReadResult Read(byte[] content, IMasterDataDefinition definition, int maximumRows)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var workbook = new XLWorkbook(stream);
        var names = workbook.Worksheets.Select(x => x.Name).ToArray();
        if (names.Length != 3 || !names.SequenceEqual(["Data", "Column Guide", "_Metadata"], StringComparer.Ordinal))
            throw new MasterDataValidationException("Workbook must contain exactly Data, Column Guide and hidden _Metadata sheets in that order.");

        var metadata = workbook.Worksheet("_Metadata");
        if (metadata.Visibility == XLWorksheetVisibility.Visible)
            throw new MasterDataValidationException("The _Metadata sheet must remain hidden.");
        var masterKey = metadata.Cell(1, 2).GetString().Trim();
        if (!int.TryParse(metadata.Cell(2, 2).GetFormattedString(CultureInfo.InvariantCulture), NumberStyles.None, CultureInfo.InvariantCulture, out var templateVersion))
            throw new MasterDataValidationException("Workbook template version is missing or invalid.");
        if (!string.Equals(masterKey, definition.MasterKey, StringComparison.OrdinalIgnoreCase))
            throw new MasterDataValidationException($"Workbook belongs to master '{masterKey}', not '{definition.MasterKey}'.");
        if (templateVersion != definition.TemplateVersion)
            throw new MasterDataValidationException($"Workbook template version {templateVersion} is not supported; download version {definition.TemplateVersion}.");

        var data = workbook.Worksheet("Data");
        var headers = definition.Columns.Select(x => x.Header).ToArray();
        var actualHeaders = Enumerable.Range(1, headers.Length).Select(x => data.Cell(1, x).GetString().Trim()).ToArray();
        if (!actualHeaders.SequenceEqual(headers, StringComparer.Ordinal))
            throw new MasterDataValidationException("Data sheet headers or their order do not match the current template.");
        var unexpectedHeader = data.Row(1).CellsUsed().FirstOrDefault(x => x.Address.ColumnNumber > headers.Length);
        if (unexpectedHeader is not null)
            throw new MasterDataValidationException($"Unexpected Data sheet column '{unexpectedHeader.GetString()}'.");

        var lastRow = data.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<MasterDataRawRow>();
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            var hasValue = false;
            for (var columnNumber = 1; columnNumber <= definition.Columns.Count; columnNumber++)
            {
                var cell = data.Cell(rowNumber, columnNumber);
                if (cell.HasFormula)
                    throw new MasterDataValidationException($"Formula cells are not allowed. Remove the formula at {cell.Address}.");
                var value = ReadValue(cell);
                if (!string.IsNullOrWhiteSpace(value)) hasValue = true;
                values[definition.Columns[columnNumber - 1].Key] = value;
            }
            if (hasValue) rows.Add(new(rowNumber, values));
            if (rows.Count > maximumRows)
                throw new MasterDataValidationException($"Workbook contains more than the configured maximum of {maximumRows} data rows.");
        }
        if (rows.Count == 0) throw new MasterDataValidationException("Workbook contains no data rows.");
        return new(masterKey, templateVersion, rows);
    }

    public byte[] CreateErrorWorkbook(
        IMasterDataDefinition definition,
        IReadOnlyList<(MasterDataRawRow Row, string Outcome, IReadOnlyList<MasterDataRowError> Errors)> rows,
        DateTimeOffset generatedAt)
    {
        var exportRows = rows.Select(x =>
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var column in definition.Columns)
                values[column.Key] = x.Row.Values.TryGetValue(column.Key, out var value) ? value : null;
            values["ImportOutcome"] = x.Outcome;
            values["ErrorColumns"] = string.Join(", ", x.Errors.Select(e => e.ColumnHeader).Distinct(StringComparer.Ordinal));
            values["ErrorMessages"] = string.Join(" | ", x.Errors.Select(e => e.Message));
            return new MasterDataExportRow(values);
        }).ToArray();

        var errorDefinition = new AdHocDefinition(definition,
        [
            .. definition.Columns,
            new("ImportOutcome", "Import Outcome", MasterDataColumnType.Text, false, false, false, "Text", "REJECTED, NOT_IMPORTED", null, "Import result."),
            new("ErrorColumns", "Error Columns", MasterDataColumnType.Text, false, false, false, "Text", "Column headers", null, "Columns containing errors."),
            new("ErrorMessages", "Error Messages", MasterDataColumnType.Text, false, false, false, "Text", "Validation messages", null, "All row validation messages.")
        ]);
        return Create(errorDefinition, exportRows, generatedAt);
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<MasterDataColumnDefinition> columns)
    {
        for (var index = 0; index < columns.Count; index++) WriteValue(worksheet.Cell(1, index + 1), columns[index].Header);
    }

    private static void WriteValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null: cell.SetValue(string.Empty); break;
            case bool boolean: cell.SetValue(boolean); break;
            case int integer: cell.SetValue(integer); break;
            case uint unsigned: cell.SetValue((long)unsigned); break;
            case long longValue: cell.SetValue(longValue); break;
            case decimal decimalValue: cell.SetValue(decimalValue); break;
            case DateOnly date: cell.SetValue(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); break;
            default: cell.SetValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty); break;
        }
    }

    private static string? ReadValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;
        return cell.DataType switch
        {
            XLDataType.Boolean => cell.GetBoolean() ? "TRUE" : "FALSE",
            XLDataType.Number => cell.GetDouble().ToString("G17", CultureInfo.InvariantCulture),
            XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture),
            _ => cell.GetString().Trim()
        };
    }

    private sealed class AdHocDefinition(IMasterDataDefinition source, IReadOnlyList<MasterDataColumnDefinition> columns) : IMasterDataDefinition
    {
        public string MasterKey => source.MasterKey;
        public int TemplateVersion => source.TemplateVersion;
        public string PageKey => source.PageKey;
        public string BusinessCodeColumnKey => source.BusinessCodeColumnKey;
        public IReadOnlyList<string> OperationalRolePriority => source.OperationalRolePriority;
        public MasterDataSensitivePermission? SensitiveResultPermission => source.SensitiveResultPermission;
        public IReadOnlyList<string> WorkbookGuideNotes => source.WorkbookGuideNotes;
        public IReadOnlyList<MasterDataColumnDefinition> Columns => columns;
    }
}
