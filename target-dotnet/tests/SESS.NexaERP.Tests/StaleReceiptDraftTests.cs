using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if CONCURRENCY_WITNESS
    [Fact]
    public async Task ASecondReceiptDraftCannotFinalizeAfterThePoQuantityIsReceived()
    {
        var observed=false;
        await RunCompletePurchaseFlow(grnRace: async context =>
        {
            observed=true;
            await using var db=new NexaErpDbContext(context.Options);
            var po=await db.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==context.Draft.PurchaseOrderId);
            var line=Assert.Single(context.Draft.Lines);
            var actor=await BankAdviceActor(context.Options);
            var subject=await db.EmployeeIdentityMappings.Where(x=>x.CompanyId==po.CompanyId&&
                x.EmployeeId==context.FirstOperatorId&&x.IsActive).Select(x=>x.Subject).SingleAsync();
            actor.Set(context.FirstOperatorId,subject,"STORES_EXECUTIVE");
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            var today=DateOnly.FromDateTime(DateTime.UtcNow);
            const string key="stale-receipt-draft";
            var gate=await Post<GateEntryResult>(host.Client,"/api/v1/stores/gate-entries/",
                new CreateGateEntryRequest(po.PoNumber,key+"-DC","TRIAL-VEHICLE","ROAD",DateTimeOffset.UtcNow,
                    "{\"packagesChecked\":true}",[new(line.PurchaseOrderLineId,1m)]),key+"-gate");
            gate=await Post<GateEntryResult>(host.Client,$"/api/v1/stores/gate-entries/{gate.Id}/finalize",
                new FinalizeGateEntryRequest(gate.Version,key+"-gate-final"));
            var second=await Post<GoodsReceiptResult>(host.Client,"/api/v1/stores/goods-receipts/",
                new CreateGoodsReceiptRequest(gate.GateEntryNumber,key+"-BILL",today,DateTimeOffset.UtcNow,
                    "{\"billChecked\":true}",[new(gate.Lines.Single().Id,
                        [new(1,1m,key+"-LOT",null,today.AddMonths(-1),today.AddYears(2))],[])]),key+"-create");
            Assert.Equal("DRAFT",second.Status);
            Assert.NotEqual(context.Draft.Id,second.Id);
            await db.Database.OpenConnectionAsync();
            async Task<JsonElement> Snapshot()
            {
                await using var command=new NpgsqlCommand("""
                    SELECT jsonb_build_object(
                      'Status',(SELECT "Status" FROM advance.goods_receipts WHERE "Id"=@second),
                      'Version',(SELECT "Version" FROM advance.goods_receipts WHERE "Id"=@second),
                      'Finalized',(SELECT count(*) FROM advance.goods_receipts WHERE "Status"='FINALIZED'),
                      'Postings',(SELECT count(*) FROM advance.stock_posting_batches),
                      'Movements',(SELECT count(*) FROM advance.stock_movements),
                      'Layers',(SELECT count(*) FROM advance.fifo_inventory_cost_layers),
                      'Audits',(SELECT count(*) FROM advance.audit_logs),
                      'BusinessAudits',(SELECT count(*) FROM advance.audit_logs WHERE "Result"='Success'),
                      'Requests',(SELECT count(*) FROM advance.command_requests),
                      'Receipts',(SELECT count(*) FROM advance.command_receipts))::text
                    """,(NpgsqlConnection)db.Database.GetDbConnection());
                command.Parameters.AddWithValue("second",second.Id);
                using var json=JsonDocument.Parse((string)(await command.ExecuteScalarAsync())!);
                return json.RootElement.Clone();
            }
            var beforeFirst=await Snapshot();
            var firstRequest=new FinalizeGoodsReceiptRequest(context.Draft.Version,context.FirstKey);
            var first=await Post<GoodsReceiptResult>(host.Client,$"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",firstRequest);
            Assert.Equal("FINALIZED",first.Status);
            var afterFirst=await Snapshot();
            foreach(var field in new[]{"Finalized","Postings","Movements","Layers","BusinessAudits","Requests","Receipts"})
                Assert.Equal(beforeFirst.GetProperty(field).GetInt64()+1,afterFirst.GetProperty(field).GetInt64());
            var secondRequest=new FinalizeGoodsReceiptRequest(second.Version,key+"-finalize");
            var refused=await TimedRacePost(host.Client,$"/api/v1/stores/goods-receipts/{second.Id}/finalize",secondRequest);
            var afterRefusal=await Snapshot();
            var retry=await TimedRacePost(host.Client,$"/api/v1/stores/goods-receipts/{second.Id}/finalize",secondRequest);
            var afterRetry=await Snapshot();
            var replay=await Post<GoodsReceiptResult>(host.Client,$"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",firstRequest);
            Assert.Equal(first.Id,replay.Id);
            Assert.Equal(first.StockPostingBatchId,replay.StockPostingBatchId);
            Assert.Equal(afterRetry.GetRawText(),(await Snapshot()).GetRawText());
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"stale-receipt-draft.json"),
                JsonSerializer.Serialize(new{First=context.Draft.Id,Second=second.Id,BeforeFirst=beforeFirst,
                    AfterFirst=afterFirst,Refused=refused,AfterRefusal=afterRefusal,Retry=retry,AfterRetry=afterRetry,
                    Replay=replay},new JsonSerializerOptions{WriteIndented=true}));
            Assert.True(refused.Status==HttpStatusCode.Conflict,refused.Body);
            Assert.Contains("remaining quantity",refused.Body,StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.Conflict,retry.Status);
            foreach(var property in afterFirst.EnumerateObject().Where(x=>x.Name!="Audits"))
            {
                Assert.Equal(property.Value.GetRawText(),afterRefusal.GetProperty(property.Name).GetRawText());
                Assert.Equal(property.Value.GetRawText(),afterRetry.GetProperty(property.Name).GetRawText());
            }
            Assert.Equal("DRAFT",afterRetry.GetProperty("Status").GetString());
            Assert.Equal(0,afterRetry.GetProperty("Version").GetInt64());
            return first;
        },additionalDraftReceipts:1);
        Assert.True(observed);
    }
#endif
}
