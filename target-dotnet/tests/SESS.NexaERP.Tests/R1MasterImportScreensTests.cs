using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Tests;

public sealed class R1MasterImportScreensTests
{
    [Fact]
    public void Manufacturer_template_round_trips_text_codes_and_rejects_bad_names_and_duplicate_codes()
    {
        var adapter = new ManufacturerMasterDataAdapter(null!, null!, null!);
        var definition = adapter.Definition;
        var workbook = new MasterDataWorkbookService();
        var content = workbook.Create(definition,
            [new(new Dictionary<string, object?> { ["Code"] = "00012", ["Name"] = "Synthetic manufacturer" })],
            DateTimeOffset.Parse("2026-10-01T00:00:00Z"));
        var row = Assert.Single(workbook.Read(content, definition, 100).Rows);
        Assert.Equal("00012", row.Values["Code"]);
        Assert.Empty(adapter.Validate(row, null, null));
        var empty = new MasterDataExistingSet(new Dictionary<string, MasterDataExistingRecord>(), new Dictionary<Guid, MasterDataExistingRecord>());
        var duplicate = EfMasterDataTransferService.Prepare(row, adapter, empty, null, new HashSet<string> { "00012" });
        Assert.Contains(duplicate.Errors, x => x.Code == "DUPLICATE_IN_FILE");
        var bad = new MasterDataRawRow(3, new Dictionary<string, string?> { ["Code"] = "BAD", ["Name"] = "", ["IsActive"] = "FALSE" });
        Assert.Contains(adapter.Validate(bad, null, null), x => x.ColumnKey == "Name" && x.Code == "REQUIRED");
        Assert.Contains(adapter.Validate(bad, null, null), x => x.ColumnKey == "IsActive" && x.Code == "READ_ONLY");
    }

    [Theory]
    [InlineData("UomCode")]
    [InlineData("ManufacturerCode")]
    public void Item_import_refuses_unknown_references_before_application(string key)
    {
        var values = ItemImportV2Tests.Row().Values.ToDictionary(x => x.Key, x => x.Value);
        values[key] = "MISSING";
        var lookup = new ItemLookup(new() { ["NOS"] = Guid.NewGuid() }, new() { ["REF"] = Guid.NewGuid() },
            new(), new(), new(), new(), new());
        var errors = new ItemMasterDataAdapter(null!, null!).Validate(new(2, values), null, lookup);
        Assert.Contains(errors, x => x.ColumnKey == key && x.Code == "LOOKUP_NOT_FOUND");
    }

    [Fact]
    public void Item_export_workbook_keeps_code_and_barcode_as_text_in_the_import_column_order()
    {
        var definition = new ItemImportDefinition();
        var values = ItemImportV2Tests.Row("000123").Values.ToDictionary(x => x.Key, x => (object?)x.Value);
        var bytes = new MasterDataWorkbookService().Create(definition, [new(values)], DateTimeOffset.UtcNow);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Data");
        Assert.Equal(definition.Columns.Select(x => x.Header), Enumerable.Range(1, definition.Columns.Count).Select(i => sheet.Cell(1, i).GetString()));
        foreach (var key in new[] { "ItemCode", "Barcode" })
        {
            var index = definition.Columns.Select(x => x.Key).ToList().IndexOf(key) + 1;
            Assert.Equal(XLDataType.Text, sheet.Cell(2, index).DataType);
            Assert.Equal("@", sheet.Column(index).Style.NumberFormat.Format);
            Assert.Equal((string)values[key]!, sheet.Cell(2, index).GetString());
        }
    }

    [Theory]
    [InlineData("vendors")]
    [InlineData("items")]
    [InlineData("uoms")]
    [InlineData("manufacturers")]
    public async Task Partial_mode_is_rejected_before_any_database_work(string key)
    {
        IMasterDataAdapter adapter = key switch
        {
            "vendors" => new VendorMasterDataAdapter(null!),
            "items" => new ItemMasterDataAdapter(null!, null!),
            "uoms" => new UomMasterDataAdapter(null!),
            _ => new ManufacturerMasterDataAdapter(null!, null!, null!)
        };
        var service = new EfMasterDataTransferService(null!, new MasterDataRegistry([adapter]), null!, null!,
            new SystemClock(), Options.Create(new MasterDataTransferOptions()));
        var error = await Assert.ThrowsAsync<MasterDataValidationException>(() => service.ImportAsync(
            new(key, MasterDataImportModes.ImportValidRows, "policy-proof", "draft.xlsx", [1], Guid.NewGuid()), CancellationToken.None));
        Assert.Contains("REJECT_ENTIRE_FILE", error.Message);
    }
}