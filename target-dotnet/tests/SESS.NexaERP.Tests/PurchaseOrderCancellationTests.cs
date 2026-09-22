using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOrderCancellationRequiresDirectorAndCommitsOnce()
    {
        await RunPurchaseOrderRevisionCashWitness(async context=>
        {
            await using var source=new NexaErpDbContext(context.Options);
            var prior=await source.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==context.RevisionId);
            var actor=await BankAdviceActor(context.Options);
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            async Task UseActor(Guid employee,string role)
            {
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==prior.CompanyId&&
                    x.EmployeeId==employee&&x.IsActive).Select(x=>x.Subject).SingleAsync();
                actor.Set(employee,subject,role);
            }
            await UseActor(prior.OwnerEmployeeId,"PURCHASE_MANAGER");
            var path="/api/v1/purchase/purchase-orders/"+Uri.EscapeDataString(prior.PoNumber);
            var revision=await Post<Rev869BDocumentResult>(host.Client,path+"/amend",
                new Rev869BAmendPurchaseOrderRequest("Cancel an approved unissued amendment",
                    prior.PaymentTermsSnapshot,prior.DeliveryTermsSnapshot,prior.WarrantyTermsSnapshot,
                    prior.Version,"open-cancel-amend"));
            revision=await Post<Rev869BDocumentResult>(host.Client,path+"/submit",
                new Rev869BSubmitPurchaseOrderRequest("Submit cancellation witness revision",revision.Version,"open-cancel-submit"));
            var snapshot=await source.PurchaseOrders.AsNoTracking().Where(x=>x.Id==revision.Id)
                .Select(x=>x.ApprovalWorkflowSnapshotJson).SingleAsync();
            using(var workflow=JsonDocument.Parse(snapshot))
            {
                foreach(var step in workflow.RootElement.GetProperty("steps").EnumerateArray()
                    .OrderBy(x=>x.GetProperty("stepNumber").GetInt32()))
                {
                    await UseActor(step.GetProperty("employeeId").GetGuid(),step.GetProperty("roleCode").GetString()!);
                    var priorVersion=await source.PurchaseOrders.AsNoTracking().Where(x=>x.Id==prior.Id).Select(x=>x.Version).SingleAsync();
                    revision=await Post<Rev869BDocumentResult>(host.Client,path+"/approve",
                        new Rev869BPoApprovalActionRequest("Approve before cancellation",revision.Version,priorVersion,
                            "open-cancel-approve-"+step.GetProperty("stepNumber").GetInt32()));
                }
            }
            Assert.Equal("Approved",revision.Status);
            var director=await source.Employees.Where(x=>x.EmployeeCode=="SESS-01").Select(x=>x.Id).SingleAsync();
            await source.Database.OpenConnectionAsync();
            async Task<Dictionary<string,long>> Counts()
            {
                await using var command=new NpgsqlCommand("""
                    SELECT jsonb_build_object(
                      'history',(SELECT count(*) FROM advance.purchase_order_history),
                      'statusHistory',(SELECT count(*) FROM advance.purchase_transaction_status_history),
                      'audits',(SELECT count(*) FROM advance.audit_logs),
                      'businessAudits',(SELECT count(*) FROM advance.audit_logs WHERE "Result"='Success'),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'layers',(SELECT count(*) FROM advance.fifo_inventory_cost_layers),
                      'grns',(SELECT count(*) FROM advance.goods_receipts),
                      'advances',(SELECT count(*) FROM advance.vendor_advances),
                      'payments',(SELECT count(*) FROM advance.vendor_payments))::text
                    """,(NpgsqlConnection)source.Database.GetDbConnection());
                return JsonSerializer.Deserialize<Dictionary<string,long>>((string)(await command.ExecuteScalarAsync())!)!;
            }
            await UseActor(prior.OwnerEmployeeId,"PURCHASE_MANAGER");
            var beforeDenied=await Counts();
            using(var denied=await host.Client.PostAsJsonAsync(path+"/cancel",
                new Rev869BCancelPurchaseOrderRequest("PM cannot cancel",revision.Version,"cancel-pm-refused")))
                Assert.Equal(System.Net.HttpStatusCode.Forbidden,denied.StatusCode);
            var afterDenied=await Counts();
            foreach(var pair in beforeDenied.Where(x=>x.Key!="audits"))
                Assert.Equal(pair.Value,afterDenied[pair.Key]);
            await UseActor(director,"TECHNICAL_DIRECTOR");
            var before=await Counts();
            var request=new Rev869BCancelPurchaseOrderRequest("Do not issue this amendment",revision.Version,"cancel-director-command");
            var cancelled=await Post<Rev869BDocumentResult>(host.Client,path+"/cancel",request);
            Assert.Equal("Cancelled",cancelled.Status);
            Assert.Equal(revision.Version+1,cancelled.Version);
            var after=await Counts();
            var incremented=new HashSet<string>{"history","statusHistory","audits","businessAudits","requests","receipts"};
            foreach(var pair in before)
                Assert.Equal(pair.Value+(incremented.Contains(pair.Key)?1:0),after[pair.Key]);
            var stored=await source.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==revision.Id);
            Assert.Equal("Cancelled",stored.Status); Assert.NotNull(stored.CancelledAt);
            Assert.Equal(request.Reason,stored.CancellationReason);
            var replay=await Post<Rev869BDocumentResult>(host.Client,path+"/cancel",request);
            Assert.Equal(cancelled,replay);
            var replayCounts=await Counts();
            foreach(var pair in after) Assert.Equal(pair.Value,replayCounts[pair.Key]);
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"po-cancellation-atomicity.json"),
                JsonSerializer.Serialize(new{beforeDenied,afterDenied,before,after,replayCounts,cancelled,replay},
                    new JsonSerializerOptions{WriteIndented=true}));
        });
    }


#endif
}
