using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Infrastructure.Stores;

internal static class EstimatedBomWorkbook
{
    internal const string ContentType = MasterDataWorkbookService.ContentType;
    private static readonly Definition Schema = new();
    internal static byte[] CreateTemplate(DateTimeOffset now) => new MasterDataWorkbookService().Create(Schema, [], now);

    internal static (string JobOrderNumber, string RevisionReason, IReadOnlyList<ImportLine> Lines) Read(byte[] content)
    {
        var workbook = new MasterDataWorkbookService().Read(content, Schema, int.MaxValue);
        var lines = new List<ImportLine>(workbook.Rows.Count);
        string? job = null; string? reason = null;
        foreach (var row in workbook.Rows)
        {
            var currentJob = Required(row, "JobOrderNumber", "Job Order Number").ToUpperInvariant();
            var currentReason = Required(row, "RevisionReason", "Revision Reason");
            job ??= currentJob; reason ??= currentReason;
            if (job != currentJob || reason != currentReason)
                throw new MasterDataValidationException($"Row {row.SourceRowNumber}: every line must identify the same job order and revision reason.");
            if (!int.TryParse(Required(row, "LineNumber", "Line Number"), out var number) || number < 1)
                throw new MasterDataValidationException($"Row {row.SourceRowNumber}: Line Number must be a positive integer.");
            if (!decimal.TryParse(Required(row, "Quantity", "Quantity"), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                throw new MasterDataValidationException($"Row {row.SourceRowNumber}: Quantity must be a positive invariant decimal.");
            lines.Add(new(number, Required(row, "ItemCode", "Item Code").ToUpperInvariant(), Required(row, "UomCode", "UOM Code").ToUpperInvariant(), quantity, Optional(row, "Remarks")));
        }
        if (lines.Select(x => x.LineNumber).Distinct().Count() != lines.Count)
            throw new MasterDataValidationException("Line Number must be unique within the workbook.");
        return (job!, reason!, lines.OrderBy(x => x.LineNumber).ToArray());
    }

    private static string Required(MasterDataRawRow row, string key, string label) =>
        row.Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim() : throw new MasterDataValidationException($"Row {row.SourceRowNumber}: {label} is required.");
    private static string? Optional(MasterDataRawRow row, string key) =>
        row.Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    internal sealed record ImportLine(int LineNumber, string ItemCode, string UomCode, decimal Quantity, string? Remarks);

    private sealed class Definition : IMasterDataDefinition
    {
        public string MasterKey => "estimated-bom";
        public int TemplateVersion => 1;
        public string PageKey => "design.estimated-bom";
        public string BusinessCodeColumnKey => "JobOrderNumber";
        public IReadOnlyList<string> OperationalRolePriority => ["DESIGN_ENGINEER", "TECHNICAL_DIRECTOR"];
        public MasterDataSensitivePermission? SensitiveResultPermission => null;
        public IReadOnlyList<string> WorkbookGuideNotes =>
        [
            "One workbook creates one Draft Estimated BOM revision.",
            "There is no line ceiling; the complete workbook is validated before any row is committed.",
            "Draft items are allowed; merged item codes resolve to their terminal survivor.",
            "Formula cells are prohibited. Use literal values only."
        ];
        public IReadOnlyList<MasterDataColumnDefinition> Columns =>
        [
            C("JobOrderNumber", "Job Order Number", "Existing job order code"),
            C("RevisionReason", "Revision Reason", "Reason for this immutable revision"),
            C("LineNumber", "Line Number", "Positive unique integer"),
            C("ItemCode", "Item Code", "Existing Draft or Approved item code"),
            C("UomCode", "UOM Code", "Active UOM comparable with the item base UOM"),
            new("Quantity", "Quantity", MasterDataColumnType.Decimal, true, true, true, "Positive decimal; six-decimal precision", "", null, "Required quantity"),
            new("Remarks", "Remarks", MasterDataColumnType.Text, false, false, true, "Text up to 1000 characters", "", null, "Optional line note")
        ];
        private static MasterDataColumnDefinition C(string key, string header, string description) =>
            new(key, header, MasterDataColumnType.Text, true, true, true, "Text", "", null, description);
    }
}
