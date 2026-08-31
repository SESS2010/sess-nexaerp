using ClosedXML.Excel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class WarehouseRackMasterTests
{
    [Fact]
    public void WarehouseAndRackTemplatesFollowTheSharedImportContract()
    {
        var warehouse=new WarehouseMasterDataDefinition();var rack=new RackBinMasterDataDefinition();
        Assert.Equal("WarehouseCode",warehouse.BusinessCodeColumnKey);Assert.Equal("BinCode",rack.BusinessCodeColumnKey);
        foreach(IMasterDataDefinition definition in new IMasterDataDefinition[]{warehouse,rack})
        {
            Assert.True(definition.Columns.Single(x=>x.Key=="RecordId").RequiredOnUpdate);
            Assert.True(definition.Columns.Single(x=>x.Key=="Version").RequiredOnUpdate);
            Assert.False(definition.Columns.Single(x=>x.Key=="IsActive").Editable);
            var bytes=new MasterDataWorkbookService().Create(definition,[],DateTimeOffset.Parse("2026-08-31T00:00:00Z"));
            using var stream=new MemoryStream(bytes);using var workbook=new XLWorkbook(stream);
            Assert.Equal(definition.Columns.Select(x=>x.Header),workbook.Worksheet("Data").Row(1).Cells(1,definition.Columns.Count).Select(x=>x.GetString()));
            Assert.True(workbook.Worksheet("_Metadata").Visibility!=XLWorksheetVisibility.Visible);
        }
    }

    [Fact]
    public void RackPartitionsAreSeparateBinsAndConditionLocationsAreVersionedSeparately()
    {
        var definition=new RackBinMasterDataDefinition();
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("partition is one bin row",StringComparison.OrdinalIgnoreCase));
        Assert.Contains(definition.WorkbookGuideNotes,x=>x.Contains("created and closed separately",StringComparison.OrdinalIgnoreCase));
        Assert.Contains(definition.Columns,x=>x.Key=="RackName");
        Assert.Contains(definition.Columns,x=>x.Key=="BinNameNumber");
        Assert.DoesNotContain(definition.Columns,x=>x.Key.Contains("Parent",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModelScopesWarehouseAndBinBusinessCodesByCompany()
    {
        using var db=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var model=db.GetService<IDesignTimeModel>().Model;
        var warehouse=model.FindEntityType(typeof(Warehouse))!;
        var rack=model.FindEntityType(typeof(RackBin))!;
        Assert.Contains(warehouse.GetIndexes(),x=>x.IsUnique&&x.Properties.Select(p=>p.Name).SequenceEqual([nameof(Warehouse.CompanyId),nameof(Warehouse.WarehouseCode)]));
        Assert.Contains(rack.GetIndexes(),x=>x.IsUnique&&x.Properties.Select(p=>p.Name).SequenceEqual([nameof(RackBin.CompanyId),nameof(RackBin.BinCode)]));
    }

    [Fact]
    public void MigrationAndEndpointsFailClosedOnCurrentStockAndGuardBothDirections()
    {
        var root=FindRoot();var migration=File.ReadAllText(Directory.GetFiles(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Persistence","Migrations"),"*WarehouseAndRackMaster.cs").Single());
        var inventory=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Api","Endpoints","InventoryEndpoints.cs"));
        var configuration=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Api","Endpoints","Rev869AConfigurationEndpoints.cs"));
        Assert.Equal(2,Count(migration,"PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("QuantityIn\" - sm.\"QuantityOut",migration);
        Assert.Contains("guard_condition_location_close_stock",migration);
        Assert.Contains("current stock balance",inventory);
        Assert.Contains("WarehouseConditionLocationId==row.Id",configuration);
        Assert.Contains("warehouse-condition-locations/{locationId:guid}/close",configuration);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void MigrationClusterGuardRejectsNonPostgreSqlInBothDirections(string methodName)
    {
        var migration=new SESS.NexaERP.Infrastructure.Persistence.Migrations.WarehouseAndRackMaster();
        var method=migration.GetType().GetMethod(methodName,BindingFlags.Instance|BindingFlags.NonPublic)!;
        var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(migration,[new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    private static int Count(string text,string value){var count=0;for(var offset=0;(offset=text.IndexOf(value,offset,StringComparison.Ordinal))>=0;offset+=value.Length)count++;return count;}
    private static string FindRoot(){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null&&!File.Exists(Path.Combine(directory.FullName,"SESS.NexaERP.slnx")))directory=directory.Parent;return directory?.FullName??throw new DirectoryNotFoundException();}
}
