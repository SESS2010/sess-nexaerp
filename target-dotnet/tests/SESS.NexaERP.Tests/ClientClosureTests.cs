using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ClosingClientBeforeApprovalLeavesDurablePendingWorkForAnotherApiInstance()
    {
        var observed=false;
        await RunCompletePurchaseFlow(prRace:async context =>
        {
            observed=true;
            var company=Guid.Parse("70000000-0000-0000-0000-000000000001");
            var assignments=await Query(context.Options,async db =>
                (await db.EmployeeRoleAssignments.AsNoTracking().Include(x=>x.Role)
                    .Where(x=>x.CompanyId==company && x.EffectiveTo==null).ToListAsync())
                .ToDictionary(x=>TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                    x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
            var subject=await Query(context.Options,db=>db.EmployeeIdentityMappings
                .Where(x=>x.CompanyId==company && x.EmployeeId==context.FirstApproverId && x.IsActive)
                .Select(x=>x.Subject).SingleAsync());
            var actor=new TaxWorkflowUser(context.FirstApproverId,subject,"ACCOUNTS_MANAGER",assignments);
            var path=$"/api/v1/purchase/requisitions/{context.Draft.PrNumber}";
            async Task<int[]> Counts() => await Query(context.Options,async db=>new[] {
                await db.PurchaseRequisitionStatusHistories.CountAsync(x=>x.PurchaseRequisitionId==context.Draft.Id),
                await db.PurchaseRequisitionApprovalHistories.CountAsync(x=>x.PurchaseRequisitionId==context.Draft.Id),
                await db.AuditLogs.CountAsync(x=>x.EntityName=="PurchaseRequisition" && x.EntityId==context.Draft.Id.ToString()) });
            await using var first=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            var pending=await Get<PurchaseRequisitionDetail>(first.Client,path);
            Assert.Equal(PurchaseRequisitionStatuses.PendingApproval,pending.Status);
            Assert.Equal(context.Draft.Version,pending.Version);
            var before=await Counts();
            var firstAddress=first.Client.BaseAddress;
            // No command is in flight: closing the HTTP client models a browser
            // closing between completed submit/verification and a later approval.
            first.Client.Dispose();
            Assert.Equal(before,await Counts());
            await using var second=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,
                new TaxWorkflowUser(context.FirstApproverId,subject,"ACCOUNTS_MANAGER",assignments),true,true);
            Assert.NotEqual(firstAddress,second.Client.BaseAddress);
            var resumed=await Get<PurchaseRequisitionDetail>(second.Client,path);
            Assert.Equal(pending.Id,resumed.Id);
            Assert.Equal(pending.Version,resumed.Version);
            Assert.Equal(pending.Status,resumed.Status);
            Assert.Equal(before,await Counts());
            var command=new PurchaseRequisitionActionRequest("Level 1 approved after reconnect",resumed.Version,context.FirstKey);
            var approved=await Post<PurchaseRequisitionDetail>(second.Client,path+"/approve",command);
            var after=await Counts();
            Assert.Equal(before[0]+1,after[0]);
            Assert.Equal(before[1]+1,after[1]);
            Assert.Equal(before[2]+1,after[2]);
            using var firstInstanceClient=new HttpClient { BaseAddress=firstAddress };
            firstInstanceClient.DefaultRequestHeaders.Authorization=new("PurchaseFlow");
            var visible=await Get<PurchaseRequisitionDetail>(firstInstanceClient,path);
            Assert.Equal(approved.Id,visible.Id);
            Assert.Equal(approved.Version,visible.Version);
            var replay=await TimedRacePost(firstInstanceClient,path+"/approve",command);
            Assert.Equal(HttpStatusCode.Conflict,replay.Status);
            Assert.Contains("CONCURRENCY_CONFLICT",replay.Body,StringComparison.Ordinal);
            Assert.Equal(after,await Counts());
            var refreshed=await Get<PurchaseRequisitionDetail>(firstInstanceClient,path);
            Assert.Equal(approved.Id,refreshed.Id);
            Assert.Equal(approved.Version,refreshed.Version);
            var directory=Path.Combine(FindRepositoryRoot(),"local-evidence","item26");
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory,"client-close-approval.json"),JsonSerializer.Serialize(new {
                pending.Id,BeforeVersion=pending.Version,ResumedVersion=resumed.Version,AfterVersion=approved.Version,
                Before=before,After=after,DuplicateApproval=replay,FirstApi=firstAddress,SecondApi=second.Client.BaseAddress,
                Mode="HTTP client disposal between completed commands; two running Kestrel instances; no browser UI"
            },new JsonSerializerOptions { WriteIndented=true }));
            return approved;
        });
        Assert.True(observed);
    }
}
