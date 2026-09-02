using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class StoresSlice0ContractTests
{
    private static string Sql()=> (string)typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.StoresControlledPostingSql",true)!.GetField("Up",BindingFlags.Static|BindingFlags.NonPublic)!.GetRawConstantValue()!;
    [Fact] public void ConcurrentIssuesLockTheItemAndAvailableLocationBeforeBalanceCheck(){var s=Sql();Assert.Contains("10:ITEM:",s);Assert.Contains("20:LOC:",s);Assert.Contains("AVAILABLE location below zero",s);}
    [Fact] public void FunctionWritesTheCompleteBatchAndAllLegsInOneCall(){var s=Sql();Assert.Contains("INSERT INTO advance.stock_posting_batches",s);Assert.Contains("INSERT INTO advance.stock_movements",s);Assert.Contains("complete non-empty posting leg array",s);}
    [Fact] public void TimeoutReplayReturnsTheExistingBatchAndFingerprintMismatchFails(){var s=Sql();Assert.Contains("RETURN QUERY SELECT b,true",s);Assert.Contains("different fingerprint",s);}
    [Fact] public void SerialAndAggregateQuantitiesAreCheckedTogether(){var s=Sql();Assert.Contains("30:SER:",s);Assert.Contains("serial balance negative or greater than one",s);Assert.Contains("sum(m.\"QuantityIn\"-m.\"QuantityOut\")",s);}
    [Fact] public void PartialQcAcceptedAndRejectedLegsRemainDatabaseReconciled(){var s=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Infrastructure","Persistence","Migrations","FirstStoresPart3BSql.cs"));Assert.Contains("AcceptedQuantity",s);Assert.Contains("RejectedQuantity",s);Assert.Contains("QC disposition batch must be a balanced",s);}
    [Fact] public void ReversalRetainsExactTypedAndReceiptProvenance(){var s=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Infrastructure","Persistence","Migrations","FirstStoresPart3BSql.cs"));Assert.Contains("Reversal movement must exactly negate one target movement",s);Assert.Contains("OriginGoodsReceiptLineId",s);Assert.Contains("InventorySerialId",s);}
    [Fact] public void AllPostingKindsUseOneSortedDeadlockSafeLockOrder(){var s=Sql();Assert.Contains("SELECT k FROM keys ORDER BY k",s);foreach(var k in new[]{"GRN_CUSTODY","QC_DISPOSITION","MATERIAL_ISSUE","DC_DISPATCH","DC_RETURN_CUSTODY","REVERSAL"})Assert.Contains(k,s);}
    [Fact] public void BalanceSemanticsRemainSpecificToReceiptTransferIssueDcAndReversal(){var s=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Infrastructure","Persistence","Migrations","FirstStoresPart3BSql.cs"));foreach(var x in new[]{"GRN custody batch does not reconcile","QC disposition batch must be a balanced","Material issue batch exceeds or bypasses","Delivery Challan posting does not reconcile","Reversal batch must negate"})Assert.Contains(x,s);}
    [Fact] public void GateEndpointsExistButNoStockPostingEndpointExists(){var s=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Api","Endpoints","StoresGateEntryEndpoints.cs"));foreach(var x in new[]{"MapPost(\"/\"","MapPut(\"/{id:guid}\"","/{id:guid}/finalize","MapGet(\"/{id:guid}\""})Assert.Contains(x,s);Assert.DoesNotContain("stock",s,StringComparison.OrdinalIgnoreCase);}
    [Fact] public void DraftLineReplacementUsesAControlledFunctionWithoutRuntimeDelete(){var s=Sql();var service=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Infrastructure","Stores","EfGateEntryService.cs"));var installer=File.ReadAllText(Path.Combine(Root(),"src","SESS.NexaERP.Installer","DatabasePrincipalProvisioningSql.cs"));Assert.Contains("CREATE FUNCTION advance.replace_gate_entry_draft",s);Assert.Contains("SECURITY DEFINER",s);Assert.Contains("GRANT EXECUTE ON FUNCTION advance.replace_gate_entry_draft",s);Assert.Contains("replace_gate_entry_draft",installer);Assert.Contains("Runtime stock ledger mutation must be available only through the controlled posting function",installer);Assert.DoesNotContain("ExecuteDeleteAsync",service);Assert.Contains("advance.replace_gate_entry_draft",service);}
    [Fact] public void TrialPackageHasExactOperationalMappingCountsAndRemovalOrder(){var a=File.ReadAllText(Path.Combine(Root(),"database","postgresql","trial-master-data-apply.sql"));var r=File.ReadAllText(Path.Combine(Root(),"database","postgresql","trial-master-data-remove.sql"));Assert.Contains("ARRAY[6,6,4,5,15,20,2,24,26,12,0]",a);Assert.Contains("DISABLE TRIGGER trg_rev869a_warehouse_condition_version_guard",r);Assert.Contains("ENABLE TRIGGER trg_rev869a_warehouse_condition_version_guard",r);Assert.True(r.IndexOf("DELETE FROM advance.store_category_routes",StringComparison.Ordinal)<r.IndexOf("DELETE FROM advance.warehouse_condition_locations",StringComparison.Ordinal));Assert.True(r.IndexOf("DELETE FROM advance.warehouse_condition_locations",StringComparison.Ordinal)<r.IndexOf("DELETE FROM advance.rack_bins",StringComparison.Ordinal));}
    private static string Root(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"SESS.NexaERP.slnx")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException();}
}

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void StoresControlledPostingMigrationGuardsUpAndDownOnDisposablePostgreSql()
    {
        var options=new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db=new NexaErpDbContext(options);var migrator=db.GetService<IMigrator>();var migrations=db.Database.GetMigrations().ToArray();const string target="20260831052559_StoresSlice0ControlledPostingAndGateApi";var index=Array.IndexOf(migrations,target);Assert.True(index>0);var predecessor=migrations[index-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());server.Execute("stores-slice0-prerequisite.sql",migrator.GenerateScript("0",predecessor));server.Execute("stores-slice0-up.sql",migrator.GenerateScript(predecessor,target)+"DO $a$ BEGIN IF to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NULL OR to_regprocedure('advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)') IS NULL THEN RAISE EXCEPTION 'Stores controlled function missing'; END IF; END $a$;");server.Execute("stores-slice0-down.sql",migrator.GenerateScript(target,predecessor)+"DO $a$ BEGIN IF to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NOT NULL OR to_regprocedure('advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb)') IS NOT NULL THEN RAISE EXCEPTION 'Stores controlled function survived down'; END IF; END $a$;");
    }
}
