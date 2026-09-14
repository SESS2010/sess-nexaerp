using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{

    private sealed record OpenOrderAmendmentWitnessContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection,Rev869BDocumentResult Issued,HttpClient Client,TaxWorkflowUser Actor,Guid PurchaseManagerId);

    private static async Task<PurchaseOpenOrdersPage> ReadOpenOrderScenario(
        DbContextOptions<NexaErpDbContext> options,string runtimeConnection,List<object> observations,string stage,string artifact)
    {
        await using var source=new NexaErpDbContext(options);
        var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
        var employee=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-15");
        var assignments=await source.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
            .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==employee.Id&&x.EffectiveTo==null).ToListAsync();
        var user=new WorkloadWitnessUser(employee.Id,
            assignments.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
        await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(runtimeConnection).Options);
        var service=new EfPurchaseOpenOrdersService(runtime,user,WorkloadCalendar());
        await source.Database.OpenConnectionAsync();
        async Task<string> Counts()
        {
            await using var command=new NpgsqlCommand("""
                SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                  'requests',(SELECT count(*) FROM advance.command_requests),
                  'receipts',(SELECT count(*) FROM advance.command_receipts),
                  'movements',(SELECT count(*) FROM advance.stock_movements),
                  'pos',(SELECT count(*) FROM advance.purchase_orders),
                  'grns',(SELECT count(*) FROM advance.goods_receipts))::text
                """,(NpgsqlConnection)source.Database.GetDbConnection());
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var before=await Counts();
        var page=await ObserveSingleReportCommand(()=>service.GetAsync(new(),default));
        var after=await Counts();
        observations.Add(new{Stage=stage,Before=before,After=after,Page=page});
        var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item29");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence,artifact),
            JsonSerializer.Serialize(observations,new JsonSerializerOptions{WriteIndented=true}));
        Assert.Equal(before,after);
        return page;
    }

#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOpenOrdersRetainNonemptyCommitmentUntilAmendmentIssueAndFlagChangedDelivery()
    {
        var observations=new List<object>();
        const string artifact="purchase-open-orders-nonempty-amendment.json";
        await RunCompletePurchaseFlow(openOrderAmendment: async context=>
        {
            async Task<PurchaseOpenOrdersPage> Read(string stage) =>
                await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,stage,artifact);
            await using var source=new NexaErpDbContext(context.Options);
            var prior=await source.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==context.Issued.Id);
            async Task UseActor(Guid employee,string role)
            {
                var code=await source.Employees.Where(x=>x.Id==employee).Select(x=>x.EmployeeCode).SingleAsync();
                context.Actor.Set(employee,code,role);
            }
            var before=await Read("ORIGINAL_ISSUED");
            var original=Assert.Single(before.Rows);
            Assert.True(before.Complete); Assert.True(before.DeliveryComplete);
            Assert.Equal(1,before.OpenPoCount); Assert.Equal(4720m,original.Value);
            Assert.Equal(prior.Id,original.PurchaseOrderId);
            await UseActor(context.PurchaseManagerId,"PURCHASE_MANAGER");
            var path="/api/v1/purchase/purchase-orders/"+Uri.EscapeDataString(prior.PoNumber);
            var revision=await Post<Rev869BDocumentResult>(context.Client,path+"/amend",
                new Rev869BAmendPurchaseOrderRequest("Revise delivery terms before receipt",
                    prior.PaymentTermsSnapshot,"Delivery date to be reconfirmed",prior.WarrantyTermsSnapshot,
                    prior.Version,"open-orders-amend"));
            var draft=await Read("AMENDMENT_DRAFT");
            Assert.Equal(prior.Id,Assert.Single(draft.Rows).PurchaseOrderId);
            Assert.Equal(4720m,Assert.Single(draft.Amounts!).Value);
            revision=await Post<Rev869BDocumentResult>(context.Client,path+"/submit",
                new Rev869BSubmitPurchaseOrderRequest("Submit amended terms",revision.Version,"open-orders-submit"));
            var snapshot=await source.PurchaseOrders.AsNoTracking().Where(x=>x.Id==revision.Id)
                .Select(x=>x.ApprovalWorkflowSnapshotJson).SingleAsync();
            using(var workflow=JsonDocument.Parse(snapshot))
            {
                foreach(var step in workflow.RootElement.GetProperty("steps").EnumerateArray()
                    .OrderBy(x=>x.GetProperty("stepNumber").GetInt32()))
                {
                    await UseActor(step.GetProperty("employeeId").GetGuid(),step.GetProperty("roleCode").GetString()!);
                    var priorVersion=await source.PurchaseOrders.AsNoTracking().Where(x=>x.Id==prior.Id).Select(x=>x.Version).SingleAsync();
                    revision=await Post<Rev869BDocumentResult>(context.Client,path+"/approve",
                        new Rev869BPoApprovalActionRequest("Approve delivery change",revision.Version,priorVersion,
                            "open-orders-approve-"+step.GetProperty("stepNumber").GetInt32()));
                }
            }
            Assert.Equal("Approved",revision.Status);
            var approved=await Read("AMENDMENT_APPROVED_NOT_ISSUED");
            var retained=Assert.Single(approved.Rows);
            Assert.True(approved.Complete); Assert.True(approved.DeliveryComplete);
            Assert.Equal(prior.Id,retained.PurchaseOrderId);
            Assert.Equal("Approved",retained.CurrentStatus);
            Assert.Equal(2,retained.CurrentRevisionNumber); Assert.Equal(1,retained.RevisionNumber);
            Assert.Equal(original.FirstIssuedAt,retained.FirstIssuedAt);
            Assert.Equal(4720m,retained.Value);
            await UseActor(context.PurchaseManagerId,"PURCHASE_MANAGER");
            revision=await Post<Rev869BDocumentResult>(context.Client,path+"/issue",
                new Rev869BIssuePurchaseOrderRequest("Issue approved delivery terms",revision.Version,"open-orders-issue"));
            var issued=await Read("AMENDMENT_ISSUED_DATE_UNCONFIRMED");
            var changed=Assert.Single(issued.Rows);
            Assert.True(issued.Complete); Assert.False(issued.DeliveryComplete);
            Assert.Equal(revision.Id,changed.PurchaseOrderId); Assert.Equal(2,changed.RevisionNumber);
            Assert.Equal(original.FirstIssuedAt,changed.FirstIssuedAt);
            Assert.Equal(4720m,changed.Value); Assert.Equal(1,issued.OpenPoCount);
            Assert.Equal(1,issued.DeliveryDateUnconfirmedPoCount); Assert.Null(issued.OverduePoCount);
            Assert.Null(Assert.Single(issued.Amounts!).OverdueValue);
            Assert.Null(changed.CommittedDeliveryDate); Assert.Null(changed.DaysLate);
            Assert.Equal("CONFIRMATION_REQUIRED",changed.DeliveryState);
            Assert.Equal(original.QuotedDeliveryDate,changed.QuotedDeliveryDate);
            return revision;
        },additionalIssuedPoVersions:1,mixedRun: async context=>
        {
            var final=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"ALL_RECEIVED",artifact);
            Assert.True(final.Complete); Assert.True(final.DeliveryComplete);
            Assert.Equal(0,final.OpenPoCount); Assert.Empty(final.Rows);
        });
        Assert.Equal(5,observations.Count);
    }

#endif
#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOpenOrdersKeepReceivedHistoryClosedWhenUnissuedAmendmentCancelled()
    {
        var observations=new List<object>();
        const string artifact="purchase-open-orders-cancelled-amendment.json";
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
            var initial=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"ISSUED_AND_RECEIVED",artifact);
            Assert.True(initial.Complete); Assert.Equal(0,initial.OpenPoCount);
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
            var approved=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"APPROVED_UNISSUED",artifact);
            Assert.True(approved.Complete); Assert.Equal(0,approved.OpenPoCount);
            var director=await source.Employees.Where(x=>x.EmployeeCode=="SESS-01").Select(x=>x.Id).SingleAsync();
            await UseActor(director,"TECHNICAL_DIRECTOR");
            var cancelled=await Post<Rev869BDocumentResult>(host.Client,path+"/cancel",
                new Rev869BCancelPurchaseOrderRequest("Do not issue this amendment",revision.Version,"open-cancel-command"));
            Assert.Equal("Cancelled",cancelled.Status);
            var page=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"CANCELLED_UNISSUED",artifact);
            Assert.True(page.Complete); Assert.True(page.DeliveryComplete);
            Assert.Equal(0,page.OpenPoCount); Assert.Equal(0,page.OverduePoCount);
            Assert.Empty(page.Amounts!); Assert.Empty(page.SourceIssues);
        });
        Assert.Equal(3,observations.Count);
    }


#endif
    private sealed class OpenOrderCancellationScenarioComplete : Exception { }

#if WORKFLOW_WITNESS
    [Fact]
    public async Task PurchaseOpenOrdersFlagCancelledUnissuedAmendmentWithOutstandingQuantity()
    {
        var observations=new List<object>();
        var completed=false;
        const string artifact="purchase-open-orders-outstanding-cancellation.json";
        // Cancellation is terminal here: intentionally leave the receipt as a draft.
        // The other witnesses complete the full three-band PR-to-Actual-BOM chain.
        await Assert.ThrowsAsync<OpenOrderCancellationScenarioComplete>(()=>RunCompletePurchaseFlow(grnRace: async context=>
        {
            await using var source=new NexaErpDbContext(context.Options);
            var prior=await source.PurchaseOrders.AsNoTracking().SingleAsync(x=>x.Id==context.Draft.PurchaseOrderId);
            var actor=await BankAdviceActor(context.Options);
            // Use the established workflow scope fixture for mutation steps;
            // the projection independently checks the real stored operational scopes.
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,false);
            async Task UseActor(Guid employee,string role)
            {
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==prior.CompanyId&&
                    x.EmployeeId==employee&&x.IsActive).Select(x=>x.Subject).SingleAsync();
                actor.Set(employee,subject,role);
            }
            var initial=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"ISSUED_NOT_RECEIVED",artifact);
            Assert.True(initial.Complete); Assert.Equal(1,initial.OpenPoCount);
            Assert.Equal(4720m,Assert.Single(initial.Amounts!).Value);
            await UseActor(prior.OwnerEmployeeId,"PURCHASE_MANAGER");
            var path="/api/v1/purchase/purchase-orders/"+Uri.EscapeDataString(prior.PoNumber);
            var revision=await Post<Rev869BDocumentResult>(host.Client,path+"/amend",
                new Rev869BAmendPurchaseOrderRequest("Cancel amendment while receipt remains a draft",
                    prior.PaymentTermsSnapshot,prior.DeliveryTermsSnapshot,prior.WarrantyTermsSnapshot,
                    prior.Version,"outstanding-cancel-amend"));
            revision=await Post<Rev869BDocumentResult>(host.Client,path+"/submit",
                new Rev869BSubmitPurchaseOrderRequest("Submit amendment",revision.Version,"outstanding-cancel-submit"));
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
                        new Rev869BPoApprovalActionRequest("Approve amendment",revision.Version,priorVersion,
                            "outstanding-cancel-approve-"+step.GetProperty("stepNumber").GetInt32()));
                }
            }
            var approved=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"APPROVED_UNISSUED",artifact);
            Assert.True(approved.Complete); Assert.Equal(1,approved.OpenPoCount);
            Assert.Equal(prior.Id,Assert.Single(approved.Rows).PurchaseOrderId);
            var director=await source.Employees.Where(x=>x.EmployeeCode=="SESS-01").Select(x=>x.Id).SingleAsync();
            await UseActor(director,"TECHNICAL_DIRECTOR");
            var cancelled=await Post<Rev869BDocumentResult>(host.Client,path+"/cancel",
                new Rev869BCancelPurchaseOrderRequest("Cancel before issue",revision.Version,"outstanding-cancel-command"));
            Assert.Equal("Cancelled",cancelled.Status);
            var page=await ReadOpenOrderScenario(context.Options,context.RuntimeConnection,observations,"CANCELLED_OUTSTANDING",artifact);
            Assert.False(page.Complete); Assert.False(page.DeliveryComplete);
            Assert.Null(page.OpenPoCount); Assert.Null(page.OverduePoCount); Assert.Null(page.Amounts);
            var issue=Assert.Single(page.SourceIssues);
            Assert.Equal(prior.RootPurchaseOrderId,issue.RootPurchaseOrderId);
            Assert.Equal("CANCELLED_UNISSUED_AMENDMENT",issue.Code);
            Assert.Equal("DRAFT",await source.GoodsReceipts.Where(x=>x.Id==context.Draft.Id).Select(x=>x.Status).SingleAsync());
            Assert.Equal(0,await source.FifoInventoryCostLayers.CountAsync());
            Assert.Equal(0,await source.StockPostingBatches.CountAsync());
            completed=true;
            throw new OpenOrderCancellationScenarioComplete();
        }));
        Assert.True(completed); Assert.Equal(3,observations.Count);
    }
#endif
}
