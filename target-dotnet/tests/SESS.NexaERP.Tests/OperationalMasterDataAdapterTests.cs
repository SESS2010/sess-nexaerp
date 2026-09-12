using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Infrastructure.MasterData;

namespace SESS.NexaERP.Tests;

public sealed class OperationalMasterDataAdapterTests
{
    [Fact]
    public void ItemTemplateContainsRequiredOperationalColumnsExampleAndNoThousandRowCeiling()
    {
        var definition=new ItemImportDefinition();
        Assert.Equal(["RecordId","Version","ItemCode","Name","ItemType","IsReturnable","HsnSacCode","GstPercentage","UomCode","CategoryCode","SubcategoryCode","ManufacturerCode","PreferredVendorCode","SerialPolicy","ReorderLevel","Status","ApprovalStatus"],definition.Columns.Select(x=>x.Key));
        Assert.Single(definition.TemplateExampleRows);
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("DRAFT",StringComparison.Ordinal));
        Assert.Equal(10000,new MasterDataTransferOptions().MaxRows);
    }

    [Fact]
    public void EveryNewTemplateHasExactThreeSheetsAnExampleAndValidationRules()
    {
        var workbookService=new MasterDataWorkbookService();
        IMasterDataDefinition[] definitions=[new ItemImportDefinition(),new EmployeeImportDefinition(),new ItemVendorImportDefinition(),new WarehouseMasterDataDefinition(),new RackBinMasterDataDefinition(),new OpeningStockImportDefinition()];
        foreach(var definition in definitions)
        {
            var bytes=workbookService.Create(definition,definition.TemplateExampleRows,DateTimeOffset.Parse("2026-09-12T00:00:00Z"));
            using var workbook=new XLWorkbook(new MemoryStream(bytes));
            Assert.Equal(["Data","Column Guide","_Metadata"],workbook.Worksheets.Select(x=>x.Name));
            Assert.True(workbook.Worksheet("Data").Row(2).CellsUsed().Any());
            Assert.Equal(definition.Columns.Count, workbook.Worksheet("Data").Row(1).CellsUsed().Count());
        }
    }

    [Fact]
    public void EmployeeTemplateExplicitlyForbidsIdentityAndLoginCreation()
    {
        var definition=new EmployeeImportDefinition();
        Assert.Equal(["RecordId","Version","EmployeeCode","EmployeeName","DepartmentCode","DesignationCode","JoiningDate","CompanyAssignments","LoginEnabled"],definition.Columns.Select(x=>x.Key));
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("never creates an identity",StringComparison.OrdinalIgnoreCase));
        Assert.False((bool)definition.TemplateExampleRows.Single().Values["LoginEnabled"]!);
    }

    [Fact]
    public void OpeningStockTemplateMakesRackAndRateMandatoryAndOmitsLegacyValue()
    {
        var definition=new OpeningStockImportDefinition();
        Assert.True(definition.Columns.Single(x=>x.Key=="RackBinCode").RequiredOnCreate);
        Assert.True(definition.Columns.Single(x=>x.Key=="Rate").RequiredOnCreate);
        Assert.DoesNotContain(definition.Columns,x=>x.Key.Contains("Value",StringComparison.OrdinalIgnoreCase));
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("Quantity x Rate",StringComparison.Ordinal));
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("Each company",StringComparison.Ordinal));
    }
}

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Opening_stock_import_permissions_apply_revert_and_reapply_on_disposable_postgresql()
    {
        const string target="20260912051827_OpeningStockImportAdapterPermissions";
        using var model=new SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext(new DbContextOptionsBuilder<SESS.NexaERP.Infrastructure.Persistence.NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();var migrations=model.Database.GetMigrations().ToArray();var index=Array.IndexOf(migrations,target);Assert.True(index>0);var predecessor=migrations[index-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("opening-stock-import-predecessor.sql",migrator.GenerateScript("0",predecessor));
        const string assertions="""
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock')<>1 THEN RAISE EXCEPTION 'Expected one Opening Stock page.'; END IF;
              IF (SELECT count(*) FROM advance.role_page_permissions rp JOIN advance.page_definitions p ON p."Id"=rp."PageDefinitionId" WHERE p."PageKey"='stores.opening-stock' AND rp."CanView" AND rp."CanCreate" AND rp."CanUpdate" AND rp."CanDownload" AND rp."CanExport")<>3 THEN RAISE EXCEPTION 'Expected three complete import grants.'; END IF;
            END $assert$;
            """;
        server.Execute("opening-stock-import-up.sql",migrator.GenerateScript(predecessor,target)+assertions);
        server.Execute("opening-stock-import-down.sql",migrator.GenerateScript(target,predecessor));
        server.Execute("opening-stock-import-reapply.sql",migrator.GenerateScript(predecessor,target)+assertions);
    }
}
