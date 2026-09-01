using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class StoresSlice2GrnContractTests
{
    [Fact]
    public void GrnModelCarriesImmutableLotAllocationSerialAndLedgerProvenance()
    {
        using var db=CreateContext();var tables=db.Model.GetEntityTypes().Select(x=>x.GetTableName()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("inventory_lots",tables);Assert.Contains("goods_receipt_line_lot_allocations",tables);
        Assert.NotNull(db.Model.FindEntityType(typeof(GoodsReceiptLineSerial))!.FindProperty(nameof(GoodsReceiptLineSerial.GoodsReceiptLineLotAllocationId)));
        Assert.NotNull(db.Model.FindEntityType(typeof(StockMovement))!.FindProperty(nameof(StockMovement.GoodsReceiptLineLotAllocationId)));
        var index=db.Model.FindEntityType(typeof(InventoryLot))!.GetIndexes().Single(x=>x.Properties.Count==7);
        Assert.True(index.IsUnique);Assert.Contains("NormalizedSupplierLotNumber",index.GetFilter());
    }

    [Fact]
    public void FinalizationIsOneControlledDatabaseCallAndNeverDirectEfLedgerInsertion()
    {
        var root=Root();var service=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Stores","EfGoodsReceiptService.cs"));var sql=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Persistence","Migrations","StoresGrnSlice2Sql.cs"));
        Assert.Contains("advance.finalize_goods_receipt",service);Assert.Contains("advance.reverse_goods_receipt",service);Assert.Contains("/{id:guid}/reverse",File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Api","Endpoints","StoresGoodsReceiptEndpoints.cs")));Assert.DoesNotContain("StockMovements.Add",service);Assert.DoesNotContain("StockPostingBatches.Add",service);
        foreach(var evidence in new[]{"Over-receipt is refused","Duplicate serial warning is unresolved","GRN_CUSTODY","QcHoldConditionLocationIdSnapshot","GoodsReceiptLineLotAllocationId","RETURN QUERY SELECT existing_batch,true"})Assert.Contains(evidence,sql);
    }

    [Fact]
    public void ReceiptCommandsAreRestrictedToTheThreeSettledNamedOperators()
    {
        var root=Root();var gate=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Stores","EfGateEntryService.Queries.cs"));var grn=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Stores","EfGoodsReceiptService.cs"));var sql=File.ReadAllText(Path.Combine(root,"src","SESS.NexaERP.Infrastructure","Persistence","Migrations","StoresGrnSlice2Sql.cs"));
        foreach(var code in new[]{"SESS-16","SESS-35","SESS-41"}){Assert.Contains(code,gate);Assert.Contains(code,grn);Assert.Contains(code,sql);}
        Assert.Contains("STORES_SLICE2_GRN_OPERATOR",sql);Assert.Contains("employee_role_assignments",sql);Assert.Contains("r.\"Code\"=p_actor_role_code",sql);
    }

    [Fact]
    public void TrialCategoriesAndQcRackUseTheSettledExactShape()
    {
        var sql=File.ReadAllText(Path.Combine(Root(),"database","postgresql","trial-master-data-apply.sql"));
        foreach(var code in new[]{"ELE","REF","FAS","PLC","FAB","MEC"})Assert.Contains($"'{code}','TRIAL ",sql);
        foreach(var old in new[]{"TRIAL-ELE','TRIAL Electrical","TRIAL-REF','TRIAL Refrigeration","TRIAL-FAS','TRIAL Fasteners","TRIAL-PLC','TRIAL Controls","TRIAL-FAB','TRIAL Fabrication","TRIAL-MEC','TRIAL Mechanical"})Assert.DoesNotContain(old,sql);
        Assert.Contains("generate_series(1,6)",sql);Assert.Contains("ARRAY['ELE','REF','FAS','PLC','FAB','MEC']",sql);
    }

    private static NexaErpDbContext CreateContext()=>new(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
    private static string Root(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"SESS.NexaERP.slnx")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException();}
}

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void StoresSlice2GrnMigrationGuardsUpAndDownWithAbsentAndCompleteManagedRoles()
    {
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;using var db=new NexaErpDbContext(options);var migrator=db.GetService<IMigrator>();var migrations=db.Database.GetMigrations().ToArray();const string target="20260901042749_StoresSlice2GrnCustodyPosting";var i=Array.IndexOf(migrations,target);Assert.True(i>0);var predecessor=migrations[i-1];
        foreach(var withRoles in new[]{false,true})
        {
            using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());server.Execute($"stores-slice2-{withRoles}-pre.sql",migrator.GenerateScript("0",predecessor));
            if(withRoles)server.Execute("stores-slice2-roles.sql","CREATE ROLE nexa_erp_owner NOLOGIN; CREATE ROLE nexa_erp_migration NOLOGIN; CREATE ROLE nexa_erp_bootstrap NOLOGIN; CREATE ROLE nexa_erp_runtime NOLOGIN;");
            server.Execute($"stores-slice2-{withRoles}-up.sql",migrator.GenerateScript(predecessor,target)+"DO $a$ BEGIN IF to_regprocedure('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)') IS NULL OR to_regprocedure('advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)') IS NULL OR to_regclass('advance.inventory_lots') IS NULL OR to_regclass('advance.goods_receipt_line_lot_allocations') IS NULL THEN RAISE EXCEPTION 'Slice 2 objects missing'; END IF; END $a$;");
            server.Execute($"stores-slice2-{withRoles}-operators.sql","DO $a$ BEGIN IF (SELECT count(*) FROM advance.employee_role_assignments a JOIN advance.employees e ON e.\"Id\"=a.\"EmployeeId\" WHERE a.\"CreatedBy\"='STORES_SLICE2_GRN_OPERATOR' AND e.\"EmployeeCode\"='SESS-41')<>2 THEN RAISE EXCEPTION 'Karthick receipt roles missing'; END IF; END $a$;");
            server.Execute($"stores-slice2-{withRoles}-down.sql",migrator.GenerateScript(target,predecessor)+"DO $a$ BEGIN IF to_regprocedure('advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text)') IS NOT NULL OR to_regprocedure('advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text)') IS NOT NULL OR to_regclass('advance.inventory_lots') IS NOT NULL OR to_regclass('advance.goods_receipt_line_lot_allocations') IS NOT NULL OR EXISTS (SELECT 1 FROM advance.employee_role_assignments WHERE \"CreatedBy\"='STORES_SLICE2_GRN_OPERATOR') THEN RAISE EXCEPTION 'Slice 2 objects or operator roles survived Down'; END IF; END $a$;");
        }
    }
}