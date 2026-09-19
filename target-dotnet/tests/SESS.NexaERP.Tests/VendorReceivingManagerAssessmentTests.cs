using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed class ReceivingManagerWitnessComplete : Exception { }
    [Fact]
    public async Task ReceivingProductionManagerCanAssessOnlyTheirActualReceipts()
    {
        await Assert.ThrowsAsync<ReceivingManagerWitnessComplete>(()=>RunCompletePurchaseFlow(grnRace:async context=>
        {
            await using var db=new NexaErpDbContext(context.Options);
            var company=Guid.Parse("70000000-0000-0000-0000-000000000001");
            var subjects=await db.EmployeeIdentityMappings.Where(x=>x.CompanyId==company&&x.IsActive).ToDictionaryAsync(x=>x.EmployeeId,x=>x.Subject);
            var actor=await BankAdviceActor(context.Options);
            actor.Set(context.FirstOperatorId,subjects[context.FirstOperatorId],"STORES_EXECUTIVE");
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            var receipt=await Post<GoodsReceiptResult>(host.Client,$"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",
                new FinalizeGoodsReceiptRequest(context.Draft.Version,context.FirstKey));
            var actual=await db.GoodsReceipts.AsNoTracking().SingleAsync(x=>x.Id==receipt.Id);
            Assert.Equal(context.FirstOperatorId,actual.ReceivedByEmployeeId);
            var stockBefore=await db.StockMovements.CountAsync();
            var td=await db.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-01");
            var receiver=await db.Employees.SingleAsync(x=>x.Id==context.FirstOperatorId);
            actor.Set(td.Id,subjects[td.Id],"TECHNICAL_DIRECTOR");
            var today=DateOnly.FromDateTime(DateTime.UtcNow);
            // Real governed cover, not a direct SQL role or receipt-actor rewrite.
            var cover=await Post<EmployeeRoleSummary>(host.Client,$"/api/v1/employees/{receiver.EmployeeCode}/roles/temporary-cover",
                new TemporaryRoleCoverRequest("PRODUCTION_MANAGER",today,today.AddDays(1),"Receiving-manager assessment witness cover"));
            Assert.Equal("PRODUCTION_MANAGER",cover.RoleCode);
            var qc=await db.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-33");
            await Post<EmployeeRoleSummary>(host.Client,$"/api/v1/employees/{qc.EmployeeCode}/roles/temporary-cover",
                new TemporaryRoleCoverRequest("PRODUCTION_MANAGER",today,today.AddDays(1),"Dual QC and receiving-manager authority witness"));
            var assignments=await db.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                .Where(x=>x.CompanyId==company&&x.EffectiveFrom<=today&&(x.EffectiveTo==null||x.EffectiveTo>=today))
                .ToDictionaryAsync(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType));
            var manager=new TaxWorkflowUser(receiver.Id,subjects[receiver.Id],"PRODUCTION_MANAGER",assignments);
            await using var managerHost=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,manager,true,true);
            const string path="/api/v1/quality/vendor-manual-assessments";
            var request=new RecordVendorManualAssessmentRequest(receipt.Id,receipt.Version,null,12,4,4,
                "Manual assessment by the recorded receiver acting as Production Manager","receiving-manager-first");
            var first=await Post<VendorManualAssessmentView>(managerHost.Client,path,request);
            Assert.Equal(receiver.Id,first.ActorEmployeeId);Assert.Equal(cover.Id,first.RoleAssignmentId);
            Assert.Equal("TEMPORARY",first.RoleAssignmentType);Assert.Equal(1,first.RevisionNumber);
            var retainedRole=await db.Database.SqlQuery<string>($"""
                SELECT "ActorRoleCode" AS "Value" FROM advance.command_requests WHERE "CommandId"={first.Id}
                """).SingleAsync();
            Assert.Equal("PRODUCTION_MANAGER",retainedRole);
            var page=await Get<VendorRatingReceiptPage>(managerHost.Client,path+"/receipts?page=1&pageSize=50");
            var option=Assert.Single(page.Items,x=>x.GoodsReceiptId==receipt.Id);Assert.Equal(receipt.Version,option.GoodsReceiptVersion);
            var history=await Get<VendorManualAssessmentView[]>(managerHost.Client,$"{path}/for-receipt/{option.GoodsReceiptId}");
            Assert.Equal(first.Id,Assert.Single(history).Id);
            var replay=await Post<VendorManualAssessmentView>(managerHost.Client,path,request);Assert.True(replay.Replayed);Assert.Equal(first.Id,replay.Id);
            var other=await db.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-25");
            manager.Set(other.Id,subjects[other.Id],"PRODUCTION_MANAGER");
            Assert.Empty((await Get<VendorRatingReceiptPage>(managerHost.Client,path+"/receipts")).Items);
            Assert.Empty(await Get<VendorManualAssessmentView[]>(managerHost.Client,$"{path}/for-receipt/{receipt.Id}"));
            using(var forbidden=await managerHost.Client.PostAsJsonAsync(path,request with {SupersedesAssessmentId=first.Id,IdempotencyKey="different-receiver-refused"}))
                Assert.Equal(HttpStatusCode.Forbidden,forbidden.StatusCode);

            var qcActor=new TaxWorkflowUser(qc.Id,subjects[qc.Id],"QC_MANAGER",assignments);
            qcActor.Set(qc.Id,subjects[qc.Id],"QC_MANAGER","QC_MANAGER","PRODUCTION_MANAGER");
            Assert.Equal("PRODUCTION_MANAGER",qcActor.ForRequest().RequireRole("create","QC_MANAGER","PRODUCTION_MANAGER"));
            await using var qcHost=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,qcActor,true,true);
            Assert.Contains((await Get<VendorRatingReceiptPage>(qcHost.Client,path+"/receipts")).Items,x=>x.GoodsReceiptId==receipt.Id);
            var correction=await Post<VendorManualAssessmentView>(qcHost.Client,path,request with {
                SupersedesAssessmentId=first.Id,TechnicalPoints=13,Reason="QC correction retains the earlier receiving-manager evidence",IdempotencyKey="qc-after-receiving-manager"});
            Assert.Equal(qc.Id,correction.ActorEmployeeId);Assert.Equal(2,correction.RevisionNumber);
            Assert.Equal(assignments[TaxWorkflowUser.AssignmentKey(qc.Id,"QC_MANAGER")].AssignmentId,correction.RoleAssignmentId);
            var extension=new VendorReceivingManagerAssessments {ActiveProvider="Npgsql.EntityFrameworkCore.PostgreSQL"};
            var refusedDown=await Assert.ThrowsAsync<PostgresException>(async()=> {
                foreach(var operation in extension.DownOperations.Cast<SqlOperation>())
                    await db.Database.ExecuteSqlRawAsync(operation.Sql);
            });
            Assert.Contains("retained Production Manager assessment evidence",refusedDown.MessageText);
            manager.Set(receiver.Id,subjects[receiver.Id],"PRODUCTION_MANAGER");
            var retained=await Get<VendorManualAssessmentView[]>(managerHost.Client,$"{path}/for-receipt/{receipt.Id}");
            Assert.Equal(new[]{first.Id,correction.Id},retained.Select(x=>x.Id));
            manager.SetOrganization("SESS_PROPRIETORSHIP");
            using(var foreign=await managerHost.Client.GetAsync(path+"/receipts"))
            {if(foreign.StatusCode==HttpStatusCode.OK)Assert.Empty((await foreign.Content.ReadFromJsonAsync<VendorRatingReceiptPage>())!.Items);else Assert.Equal(HttpStatusCode.Forbidden,foreign.StatusCode);}
            Assert.Equal(stockBefore,await db.StockMovements.CountAsync());
            throw new ReceivingManagerWitnessComplete();
        }));
    }
}
