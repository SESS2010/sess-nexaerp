using ClosedXML.Excel;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Tests;

public sealed class OpeningStockTemplateTests
{
    // The opening-stock workbook the team fills for 1 October: mandatory identity, quantity,
    // rack and ex-tax unit value; optional provenance. Written to docs so it can be handed over
    // before the fresh database exists.
    [Fact]
    public void OpeningStockTemplateVersionTwoCarriesOptionalProvenance()
    {
        var definition = new OpeningStockImportDefinition();
        Assert.Equal(2, definition.TemplateVersion);
        Assert.Equal(["LineReference", "ItemCode", "WarehouseCode", "RackBinCode", "LotNumber", "SerialNumber", "Quantity", "Rate",
            "VendorName", "VendorBillNumber", "BillDate", "PurchaseDate", "Make", "Model", "PartNumber", "Remarks"],
            definition.Columns.Select(x => x.Key));
        Assert.Equal(["LineReference", "ItemCode", "WarehouseCode", "RackBinCode", "Quantity", "Rate"],
            definition.Columns.Where(x => x.RequiredOnCreate).Select(x => x.Key));
        Assert.Equal("Unit Value Ex-Tax", definition.Columns.Single(x => x.Key == "Rate").Header);
        var bytes = new MasterDataWorkbookService().Create(definition, definition.TemplateExampleRows, new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero));
        using (var workbook = new XLWorkbook(new MemoryStream(bytes)))
        {
            var data = workbook.Worksheet("Data");
            Assert.Equal("Unit Value Ex-Tax", data.Cell(1, 8).GetString());
            Assert.Equal("Vendor Bill Number", data.Cell(1, 10).GetString());
            Assert.Equal("INV-2024-0892", data.Cell(2, 10).GetString());
            Assert.Equal("No purchase history", data.Cell(3, 16).GetString());
        }
        var target = Path.Combine(AdvanceMigrationSqlSyntaxTests.FindRepositoryRoot(), "docs", "installation", "opening-stock-template-v2.xlsx");
        File.WriteAllBytes(target, bytes);
    }
}
