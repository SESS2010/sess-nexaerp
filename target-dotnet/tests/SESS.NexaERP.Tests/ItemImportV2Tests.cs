using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class ItemImportV2Tests
{
    internal static MasterDataRawRow Row(string code = "SYNTHETIC-001", string? cost = "125.50") => new(2,
        new Dictionary<string,string?> { ["ItemCode"] = code, ["Name"] = "Synthetic item " + code,
            ["HsnSacCode"] = "008419", ["GstPercentage"] = "18", ["UomCode"] = "NOS", ["CategoryCode"] = "REF",
            ["PartNumber"] = "MODEL/PART-001", ["Barcode"] = "0000123456789", ["StandardEstimatedPrice"] = cost });

    [Fact]
    public void BlankPoliciesUseApprovedCreateDefaultsAndWorkbookRetainsLeadingZeroes()
    {
        var definition = new ItemImportDefinition();
        Assert.Equal(2, definition.TemplateVersion);
        var example = Assert.Single(definition.TemplateExampleRows).Values;
        Assert.Equal(ItemTypes.RawMaterial, example["ItemType"]);
        Assert.False(Assert.IsType<bool>(example["IsReturnable"]));
        Assert.Equal("NONE", example["SerialPolicy"]);
        Assert.Equal(0m, example["ReorderLevel"]);
        var adapter = new ItemMasterDataAdapter(null!, null!);
        var row = Row();
        Assert.Empty(adapter.Validate(row, null, null));
        var normalized = ItemImportValues.Normalize(row, null);
        Assert.Equal("RAW_MATERIAL", normalized.Values["ItemType"]);
        Assert.Equal("FALSE", normalized.Values["IsReturnable"]);
        Assert.Equal("NONE", normalized.Values["SerialPolicy"]);
        Assert.Equal("0", normalized.Values["ReorderLevel"]);
        var service = new MasterDataWorkbookService();
        var bytes = service.Create(definition, [new(row.Values.ToDictionary(x => x.Key, x => (object?)x.Value))], DateTimeOffset.UtcNow);
        var read = Assert.Single(service.Read(bytes, definition, 10000).Rows);
        Assert.Equal("0000123456789", read.Values["Barcode"]);
        Assert.Equal("008419", read.Values["HsnSacCode"]);
        Assert.Equal("MODEL/PART-001", read.Values["PartNumber"]);
        Assert.Equal("125.50", read.Values["StandardEstimatedPrice"]);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        workbook.Worksheet("_Metadata").Cell(2, 2).Value = 1;
        using var output = new MemoryStream(); workbook.SaveAs(output);
        Assert.Throws<MasterDataValidationException>(() => service.Read(output.ToArray(), definition, 10000));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0.001")]
    [InlineData("10000000000000000")]
    [InlineData("not-cost")]
    public void InvalidCostIsReportedBeforeWriting(string cost) =>
        Assert.Contains(new ItemMasterDataAdapter(null!,null!).Validate(Row(cost: cost), null, null), x => x.ColumnKey == "StandardEstimatedPrice");

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("9999999999999999.99")]
    public void BlankZeroAndMaximumCostAreAccepted(string? cost) =>
        Assert.Empty(new ItemMasterDataAdapter(null!,null!).Validate(Row(cost: cost), null, null));

    [Fact]
    public void ExplicitPoliciesAreKeptAndInvalidPoliciesAreNotDefaulted()
    {
        var values = Row().Values.ToDictionary(x=>x.Key,x=>x.Value);
        values["ItemType"] = "TOOL"; values["IsReturnable"] = "Yes";
        values["SerialPolicy"] = "SERIAL"; values["ReorderLevel"] = "7";
        Assert.Empty(new ItemMasterDataAdapter(null!,null!).Validate(new(2,values),null,null));
        var normalized = ItemImportValues.Normalize(new(2,values),null);
        Assert.Equal("TRUE",normalized.Values["IsReturnable"]);
        values["ItemType"]="MATERIAL";values["SerialPolicy"]="unknown";
        var errors = new ItemMasterDataAdapter(null!,null!).Validate(new(2,values),null,null);
        Assert.Contains(errors,x=>x.ColumnKey=="ItemType");Assert.Contains(errors,x=>x.ColumnKey=="SerialPolicy");
    }
}

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task Item_import_v2_preserves_fields_defaults_governance_and_unique_barcodes()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("items-v2-schema.sql", model.GetService<IMigrator>().GenerateScript("0", model.Database.GetMigrations().Last()));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        if (!await db.Uoms.AnyAsync(x=>x.Code=="NOS")) db.Uoms.Add(new Uom { Code="NOS",Name="Numbers",MeasurementDimension="COUNT",QuantityPrecision=0 });
        if (!await db.ItemCategories.AnyAsync(x=>x.Code=="REF")) db.ItemCategories.Add(new ItemCategory { Code="REF",Name="Refrigeration" });
        db.Manufacturers.Add(new Manufacturer { Code="SYNTHETIC-MAKE",Name="Synthetic manufacturer" });
        await db.SaveChangesAsync();
        var adapter = new ItemMasterDataAdapter(db,new LifecycleUser("item-import-operator"));
        var rows = Enumerable.Range(1,1413).Select(i=>
        {
            var values=ItemImportV2Tests.Row($"SYNTHETIC-{i:0000}").Values.ToDictionary(x=>x.Key,x=>x.Value);
            values["Barcode"]=$"000{i:0000000000}";values["ManufacturerCode"]="SYNTHETIC-MAKE";
            return new MasterDataRawRow(i+1,values);
        }).ToArray();
        var lookups=await adapter.LoadLookupContextAsync(rows,default);
        var missing=rows[0].Values.ToDictionary(x=>x.Key,x=>x.Value);missing["UomCode"]="UNKNOWN";missing["ManufacturerCode"]="UNKNOWN";
        var missingErrors=adapter.Validate(new(2,missing),null,lookups);
        Assert.Contains(missingErrors,x=>x.ColumnKey=="UomCode"&&x.Code=="LOOKUP_NOT_FOUND");
        Assert.Contains(missingErrors,x=>x.ColumnKey=="ManufacturerCode"&&x.Code=="LOOKUP_NOT_FOUND");
        foreach(var row in rows) { Assert.Empty(adapter.Validate(row,null,lookups)); await adapter.CreateAsync(row,default); }
        db.ChangeTracker.Clear();
        var items=await db.Items.Where(x=>x.ItemCode.StartsWith("SYNTHETIC-")).ToListAsync();
        Assert.Equal(1413,items.Count);
        Assert.All(items,x=> { Assert.Equal(ItemTypes.RawMaterial,x.ItemType);Assert.False(x.IsReturnable);
            Assert.False(x.SerialNumberTracking||x.BatchTracking||x.ShelfLifeTracking);Assert.Equal(0,x.ReorderLevel);
            Assert.Equal("MODEL/PART-001",x.PartNumber);Assert.Equal(125.50m,x.StandardEstimatedPrice);
            Assert.StartsWith("000",x.Barcode);Assert.NotNull(x.ManufacturerId);
            Assert.Equal(MasterStatuses.Draft,x.Status);Assert.Equal(MasterApprovalStatuses.Draft,x.ApprovalStatus);Assert.Null(x.ApprovedAt); });
        var first=items.Single(x=>x.ItemCode=="SYNTHETIC-0001");
        first.ItemType=ItemTypes.Tool;first.IsReturnable=true;first.SerialNumberTracking=true;first.MaximumStock=10;first.ReorderLevel=7;
        await db.SaveChangesAsync();
        var existing=(await adapter.LoadExistingAsync([first.ItemCode],[],default)).ByCode[first.ItemCode];
        var blank=rows[0].Values.ToDictionary(x=>x.Key,x=>x.Value);
        foreach(var key in new[]{"ItemType","IsReturnable","SerialPolicy","ReorderLevel","PartNumber","Barcode","StandardEstimatedPrice"}) blank[key]="";
        await adapter.UpdateAsync(existing,new(2,blank),existing.Version,default);
        await db.Entry(first).ReloadAsync();
        Assert.Equal(ItemTypes.Tool,first.ItemType);Assert.True(first.IsReturnable);Assert.True(first.SerialNumberTracking);
        Assert.Equal(7,first.ReorderLevel);Assert.Equal("MODEL/PART-001",first.PartNumber);Assert.Equal(125.50m,first.StandardEstimatedPrice);
        var exported=await adapter.ExportAsync(new(null,null,null,null),default);
        var export=Assert.Single(exported,x=>Equals(x.Values["ItemCode"],first.ItemCode));
        Assert.Equal(first.Barcode,export.Values["Barcode"]);Assert.Equal(first.StandardEstimatedPrice,export.Values["StandardEstimatedPrice"]);
        var duplicate=ItemImportV2Tests.Row("SYNTHETIC-DUPLICATE").Values.ToDictionary(x=>x.Key,x=>x.Value);duplicate["Barcode"]=first.Barcode;
        var duplicateRow=new MasterDataRawRow(2,duplicate);lookups=await adapter.LoadLookupContextAsync([duplicateRow],default);
        Assert.Contains(adapter.Validate(duplicateRow,null,lookups),x=>x.Code=="DUPLICATE_BARCODE");
        duplicate["Barcode"]="UNUSED";duplicateRow=new(2,duplicate);lookups=await adapter.LoadLookupContextAsync([duplicateRow,duplicateRow],default);
        Assert.Contains(adapter.Validate(duplicateRow,null,lookups),x=>x.Code=="DUPLICATE_BARCODE");
        first.Status=MasterStatuses.Active;await db.SaveChangesAsync();
        existing=(await adapter.LoadExistingAsync([first.ItemCode],[],default)).ByCode[first.ItemCode];
        Assert.Contains(adapter.Validate(rows[0],existing,null),x=>x.Code=="GOVERNED_RECORD");
    }
}
