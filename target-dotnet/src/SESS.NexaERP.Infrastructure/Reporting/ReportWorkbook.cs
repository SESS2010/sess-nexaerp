using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using SESS.NexaERP.Application.Reporting;

namespace SESS.NexaERP.Infrastructure.Reporting;

/// <summary>Streams worksheet XML into a compressed workbook without retaining source rows.</summary>
internal sealed class ReportWorkbook : IDisposable
{
    private const string Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const int DataRowsPerSheet = 1_048_575;
    private readonly MemoryStream output = new();
    private readonly ZipArchive archive;
    private readonly ReportDefinition definition;
    private readonly List<string> sheets = [];
    private XmlWriter? sheet;
    private MemoryStream? links;
    private XmlWriter? linkWriter;
    private long sheetRows;
    private int currentKind = -1;
    private int detailSheet;
    private bool completed;
    private JsonElement? stockLabels;
    private long stockGroupOrdinal;

    internal ReportWorkbook(ReportDefinition definition, string company, CompanyReportRequest request, JsonElement header)
    {
        this.definition = definition;
        archive = new ZipArchive(output, ZipArchiveMode.Create, true);
        StartSheet("About", [new("setting","Setting"),new("value","Value")]);
        WritePlain(["Report", definition.Title]);
        WritePlain(["Company", company]);
        if (definition.Coverage is not null) WritePlain(["Coverage", definition.Coverage]);
        WritePlain(["Generated (UTC)", header.GetProperty("generatedAt").GetDateTimeOffset().ToUniversalTime().ToString("O",CultureInfo.InvariantCulture)]);
        WritePlain(["Calendar timezone",header.TryGetProperty("timeZone",out var zone) ? zone.GetString() ?? "UTC" : "UTC"]);
        WritePlain(["Period", definition.UsesPeriod ? $"{request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}" : $"Through {request.ToDate:yyyy-MM-dd}"]);
        WritePlain(["Drill-through", "Click a numeric total or balance to open its underlying source rows. Details continue on numbered sheets when necessary."]);
        WritePlain(["Source rows", header.GetProperty("totalSourceRows").GetRawText()]);
        StartSheet("Totals", definition.TotalColumns);
        foreach (var total in header.GetProperty("totals").EnumerateArray())
            WriteRecord(total, definition.TotalColumns, true);
        currentKind = 1;
        StartSheet("Summary", definition.SummaryColumns);
    }

    internal void Write(int kind, JsonElement row)
    {
        if (kind is not (1 or 2) || kind < currentKind) throw new InvalidOperationException("Report rows are out of order.");
        var rowLimit = kind == 1 ? Math.Min(DataRowsPerSheet, 60000 / Math.Max(1, definition.SummaryColumns.Count(column => column.Type == "number"))) : DataRowsPerSheet;
        if (kind != currentKind || sheetRows - 1 >= rowLimit)
        {
            if (kind == 2) StartSheet(++detailSheet == 1 ? "Details" : $"Details {detailSheet}", definition.DetailColumns);
            else StartSheet($"Summary {sheets.Count(name => name.StartsWith("Summary", StringComparison.Ordinal)) + 1}", definition.SummaryColumns);
            currentKind = kind;
        }
        JsonElement? labels = null;
        if (kind == 2 && row.TryGetProperty("_stockGroupOrdinal",out var ordinal))
        {
            var group = ordinal.GetInt64();
            if (row.TryGetProperty("_stockLabels",out var supplied) && supplied.ValueKind == JsonValueKind.Object)
            {
                if (group <= stockGroupOrdinal) throw new InvalidOperationException("Stock export groups are out of order.");
                stockLabels = supplied.Clone();
                stockGroupOrdinal = group;
            }
            else if (stockLabels is null || group != stockGroupOrdinal)
                throw new InvalidOperationException("Stock export is missing its group labels.");
            labels = stockLabels;
        }
        WriteRecord(row, kind == 1 ? definition.SummaryColumns : definition.DetailColumns, kind == 1, labels);
    }

    private void StartSheet(string name, IReadOnlyList<ReportColumn> columns)
    {
        EndSheet();
        sheets.Add(name);
        sheet = CreateXml($"xl/worksheets/sheet{sheets.Count}.xml");
        sheet.WriteStartElement("worksheet", Spreadsheet);
        sheet.WriteStartElement("sheetViews");
        sheet.WriteStartElement("sheetView"); sheet.WriteAttributeString("workbookViewId","0");
        sheet.WriteStartElement("pane"); sheet.WriteAttributeString("ySplit","1"); sheet.WriteAttributeString("topLeftCell","A2");
        sheet.WriteAttributeString("activePane","bottomLeft"); sheet.WriteAttributeString("state","frozen"); sheet.WriteEndElement();
        sheet.WriteEndElement(); sheet.WriteEndElement();
        sheet.WriteStartElement("cols");
        for (var index = 0; index < columns.Count; index++)
        {
            sheet.WriteStartElement("col");
            sheet.WriteAttributeString("min",(index + 1).ToString(CultureInfo.InvariantCulture));
            sheet.WriteAttributeString("max",(index + 1).ToString(CultureInfo.InvariantCulture));
            sheet.WriteAttributeString("width",Math.Clamp(columns[index].Label.Length + 4,18,42).ToString(CultureInfo.InvariantCulture));
            sheet.WriteAttributeString("customWidth","1"); sheet.WriteEndElement();
        }
        sheet.WriteEndElement();
        sheet.WriteStartElement("sheetData");
        links = new MemoryStream();
        linkWriter = XmlWriter.Create(links, new XmlWriterSettings { Encoding = new UTF8Encoding(false), ConformanceLevel = ConformanceLevel.Fragment, CloseOutput = false });
        sheetRows = 0;
        WritePlain(columns.Select(column => column.Label).ToArray());
    }

    private void WriteRecord(JsonElement row, IReadOnlyList<ReportColumn> columns, bool drill, JsonElement? labels = null)
    {
        sheet!.WriteStartElement("row"); sheet.WriteAttributeString("r", (++sheetRows).ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var reference = Cell(index, sheetRows);
            sheet.WriteStartElement("c"); sheet.WriteAttributeString("r", reference);
            var found = row.TryGetProperty(column.Key, out var value);
            if (!found && labels is { } shared) found = shared.TryGetProperty(column.Key,out value);
            if (found && column.Type == "number" && value.ValueKind == JsonValueKind.Number)
            {
                sheet.WriteElementString("v", value.GetRawText());
                if (drill && row.TryGetProperty("detailStart", out var start) &&
                    row.TryGetProperty("detailCount", out var count) && count.GetInt64() > 0)
                {
                    var first = start.GetInt64();
                    var page = (first - 1) / DataRowsPerSheet + 1;
                    var local = (first - 1) % DataRowsPerSheet + 2;
                    var end = Math.Min(DataRowsPerSheet + 1L, local + count.GetInt64() - 1);
                    var detailName = page == 1 ? "Details" : $"Details {page}";
                    linkWriter!.WriteStartElement("hyperlink", Spreadsheet);
                    linkWriter.WriteAttributeString("ref", reference);
                    linkWriter.WriteAttributeString("location", $"'{detailName}'!A{local}:{Cell(definition.DetailColumns.Count - 1, end)}");
                    linkWriter.WriteAttributeString("tooltip", $"{count.GetInt64()} source rows; continue on the next Details sheet if the group crosses a sheet boundary.");
                    linkWriter.WriteEndElement();
                }
            }
            else WriteText(found && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                ? value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText() : "");
            sheet.WriteEndElement();
        }
        sheet.WriteEndElement();
    }

    private void WritePlain(IReadOnlyList<string> cells)
    {
        sheet!.WriteStartElement("row"); sheet.WriteAttributeString("r", (++sheetRows).ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < cells.Count; index++)
        {
            sheet.WriteStartElement("c"); sheet.WriteAttributeString("r",Cell(index, sheetRows));
            WriteText(cells[index]); sheet.WriteEndElement();
        }
        sheet.WriteEndElement();
    }

    private void WriteText(string value)
    {
        // Inline strings keep untrusted values beginning with '=' as text, never formulas.
        sheet!.WriteAttributeString("t","inlineStr"); sheet.WriteStartElement("is"); sheet.WriteStartElement("t");
        sheet.WriteAttributeString("xml","space","http://www.w3.org/XML/1998/namespace","preserve");
        var clean = XmlConvert.VerifyXmlChars(value);
        sheet.WriteString(clean.Length > 32767 ? throw new InvalidOperationException("A report cell exceeds Excel's text limit.") : clean);
        sheet.WriteEndElement(); sheet.WriteEndElement();
    }

    private void EndSheet()
    {
        if (sheet is null) return;
        sheet.WriteEndElement(); // sheetData
        linkWriter!.Flush();
        if (links!.Length > 0)
        {
            sheet.WriteStartElement("hyperlinks");
            links.Position = 0;
            using var reader = XmlReader.Create(links, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, CloseInput = false });
            while (!reader.EOF) { if (reader.NodeType == XmlNodeType.None) reader.Read(); else sheet.WriteNode(reader, true); }
            sheet.WriteEndElement();
        }
        sheet.WriteEndElement();
        sheet.Dispose(); sheet = null;
        linkWriter.Dispose(); linkWriter = null; links.Dispose(); links = null;
    }

    internal byte[] Complete()
    {
        if (completed) throw new InvalidOperationException("Workbook has already been completed.");
        if (detailSheet == 0) { StartSheet("Details", definition.DetailColumns); detailSheet = 1; }
        EndSheet();
        using (var xml = CreateXml("xl/workbook.xml"))
        {
            xml.WriteStartElement("workbook",Spreadsheet); xml.WriteStartElement("sheets");
            for (var index = 0; index < sheets.Count; index++)
            {
                xml.WriteStartElement("sheet"); xml.WriteAttributeString("name",sheets[index]);
                xml.WriteAttributeString("sheetId",(index + 1).ToString(CultureInfo.InvariantCulture));
                xml.WriteAttributeString("r","id",Relationships,$"rId{index + 1}"); xml.WriteEndElement();
            }
            xml.WriteEndElement(); xml.WriteEndElement();
        }
        WriteRelationships("_rels/.rels", [("rId1",Relationships + "/officeDocument","xl/workbook.xml")]);
        WriteRelationships("xl/_rels/workbook.xml.rels", sheets.Select((_,index) => ($"rId{index + 1}",Relationships + "/worksheet",$"worksheets/sheet{index + 1}.xml")));
        using (var xml = CreateXml("[Content_Types].xml"))
        {
            xml.WriteStartElement("Types","http://schemas.openxmlformats.org/package/2006/content-types");
            xml.WriteStartElement("Default"); xml.WriteAttributeString("Extension","rels");
            xml.WriteAttributeString("ContentType","application/vnd.openxmlformats-package.relationships+xml"); xml.WriteEndElement();
            xml.WriteStartElement("Default"); xml.WriteAttributeString("Extension","xml"); xml.WriteAttributeString("ContentType","application/xml"); xml.WriteEndElement();
            ContentType(xml,"/xl/workbook.xml","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
            for (var index = 0; index < sheets.Count; index++)
                ContentType(xml,$"/xl/worksheets/sheet{index + 1}.xml","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
            xml.WriteEndElement();
        }
        archive.Dispose(); completed = true;
        return output.ToArray();
    }

    private XmlWriter CreateXml(string path) => XmlWriter.Create(archive.CreateEntry(path,CompressionLevel.Fastest).Open(),
        new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = true });
    private void WriteRelationships(string path, IEnumerable<(string Id,string Type,string Target)> values)
    {
        using var xml = CreateXml(path);
        xml.WriteStartElement("Relationships","http://schemas.openxmlformats.org/package/2006/relationships");
        foreach (var value in values)
        {
            xml.WriteStartElement("Relationship"); xml.WriteAttributeString("Id",value.Id);
            xml.WriteAttributeString("Type",value.Type); xml.WriteAttributeString("Target",value.Target); xml.WriteEndElement();
        }
        xml.WriteEndElement();
    }
    private static void ContentType(XmlWriter xml,string path,string type)
    {
        xml.WriteStartElement("Override"); xml.WriteAttributeString("PartName",path);
        xml.WriteAttributeString("ContentType",type); xml.WriteEndElement();
    }
    private static string Cell(int column,long row)
    {
        var name = "";
        for (var value = column + 1; value > 0; value = (value - 1) / 26) name = (char)('A' + (value - 1) % 26) + name;
        return name + row.ToString(CultureInfo.InvariantCulture);
    }
    public void Dispose()
    {
        EndSheet();
        if (!completed) archive.Dispose();
        output.Dispose();
    }
}
