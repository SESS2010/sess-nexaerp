using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task StoresWorkloadKeepsPartiallyIssuedMirUntilRemainingQuantityIsIssued()
    {
        var observations=new List<object>();
        await RunCompletePurchaseFlow(storesWorkload:_=>Task.CompletedTask,mixedRun:async context=>
        {
            await using var source=new NexaErpDbContext(context.Options);
            var company=await source.Companies.SingleAsync(x=>x.Code=="SESS_PVT_LTD");
            var line=await source.GoodsReceiptLines.AsNoTracking().Include(x=>x.GoodsReceipt)
                .OrderBy(x=>x.UnitRateSnapshot).FirstAsync();
            var department=await source.PurchaseOrders.Where(x=>x.Id==line.GoodsReceipt!.PurchaseOrderId)
                .Select(x=>x.RequestingDepartmentId).SingleAsync()??throw new InvalidOperationException("Witness department missing.");
            var uom=await source.Uoms.Where(x=>x.Code==line.UomSnapshot).Select(x=>x.Id).SingleAsync();
            var reporter=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-41");
            var roles=await source.EmployeeRoleAssignments.Include(x=>x.Role)
                .Where(x=>x.CompanyId==company.Id&&x.EmployeeId==reporter.Id&&x.EffectiveTo==null).ToListAsync();
            var reader=new StoresWorkloadUser(reporter.Id,roles.Select(x=>new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)).ToArray());
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(context.RuntimeConnection).Options);
            var service=new EfStoresWorkloadService(runtime,reader,WorkloadCalendar());
            var actor=await BankAdviceActor(context.Options);
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            async Task Use(string employeeCode,string role)
            {
                var employee=await source.Employees.SingleAsync(x=>x.EmployeeCode==employeeCode);
                var subject=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==company.Id&&x.EmployeeId==employee.Id&&x.IsActive)
                    .Select(x=>x.Subject).SingleAsync();
                actor.Set(employee.Id,subject,role);
            }
            await source.Database.OpenConnectionAsync();
            async Task<string> Counts()
            {
                await using var command=new NpgsqlCommand("""
                    SELECT jsonb_build_object('audits',(SELECT count(*) FROM advance.audit_logs),
                      'requests',(SELECT count(*) FROM advance.command_requests),
                      'receipts',(SELECT count(*) FROM advance.command_receipts),
                      'movements',(SELECT count(*) FROM advance.stock_movements),
                      'issues',(SELECT count(*) FROM advance.material_issues),
                      'fifo',(SELECT count(*) FROM advance.fifo_cost_consumptions))::text
                    """,(NpgsqlConnection)source.Database.GetDbConnection());
                return (string)(await command.ExecuteScalarAsync())!;
            }
            async Task<StoresWorkloadPage> Read(string stage,Guid id)
            {
                var before=await Counts();
                var page=await ObserveSingleReportCommand(()=>service.GetAsync(new("mir-unissued",id),default));
                var after=await Counts();
                observations.Add(new{Stage=stage,before,after,Page=page});
                Assert.Equal(before,after);
                return page;
            }
            Assert.True(await source.MaterialReturns.AnyAsync(x=>x.Status=="ACCEPTED"));
            await Use("SESS-15","PURCHASE_MANAGER");
            var mir=await Post<MaterialIssueRequestView>(host.Client,"/api/v1/stores/material-issue-requests",
                new CreateMaterialIssueRequest("FACTORY_ASSEMBLY","CONSUMABLE_OFFICE","DEPARTMENT",
                    null,null,null,department,"Partial issue witness",department,DateOnly.FromDateTime(DateTime.UtcNow),
                    [new(line.ItemId,uom,.02m,null,null)],"stores-partial-create"));
            mir=await Post<MaterialIssueRequestView>(host.Client,$"/api/v1/stores/material-issue-requests/{mir.Id}/submit",
                new MaterialIssueTransitionRequest(mir.Version,"Submit partial issue witness","stores-partial-submit"));
            await Use("SESS-25","PRODUCTION_MANAGER");
            mir=await Post<MaterialIssueRequestView>(host.Client,$"/api/v1/stores/material-issue-requests/{mir.Id}/approve",
                new MaterialIssueTransitionRequest(mir.Version,"Approve partial issue witness","stores-partial-approve"));
            var initial=await Read("APPROVED",mir.Id);
            Assert.Equal("APPROVED",Assert.Single(initial.Rows).Status);
            Assert.Equal(1,Assert.Single(initial.Tiles,x=>x.Key=="mir-unissued").Count);
            await Use("SESS-35","STORES_EXECUTIVE");
            var recipient=await source.Employees.Where(x=>x.EmployeeCode=="SESS-05").Select(x=>x.Id).SingleAsync();
            var firstCommand=new CreateMaterialIssue("stores-partial-first",recipient,DateTimeOffset.UtcNow,
                [new(mir.Lines.Single().Id,line.ItemCodeSnapshot,null,.01m)]);
            await Post<MaterialIssueView>(host.Client,$"/api/v1/stores/material-issues/from-request/{mir.Id}",firstCommand);
            var partial=await Read("PARTIALLY_FULFILLED",mir.Id);
            var row=Assert.Single(partial.Rows);
            Assert.Equal("PARTIALLY_FULFILLED",row.Status); Assert.Equal(1,row.PendingLineCount);
            Assert.Equal(1,Assert.Single(partial.Tiles,x=>x.Key=="mir-unissued").Count);
            Assert.Equal(.01m,await source.MaterialIssueLines.Where(x=>x.MaterialIssueRequestLineId==mir.Lines.Single().Id).SumAsync(x=>x.QuantityBase));
            var secondCommand=new CreateMaterialIssue("stores-partial-second",recipient,DateTimeOffset.UtcNow,
                [new(mir.Lines.Single().Id,line.ItemCodeSnapshot,null,.01m)]);
            var issued=await Post<MaterialIssueView>(host.Client,$"/api/v1/stores/material-issues/from-request/{mir.Id}",secondCommand);
            var complete=await Read("FULFILLED",mir.Id);
            Assert.Empty(complete.Rows); Assert.Equal(0,Assert.Single(complete.Tiles,x=>x.Key=="mir-unissued").Count);
            Assert.Equal("FULFILLED",await source.MaterialIssueRequests.Where(x=>x.Id==mir.Id).Select(x=>x.Status).SingleAsync());
            Assert.Equal(.02m,await source.MaterialIssueLines.Where(x=>x.MaterialIssueRequestLineId==mir.Lines.Single().Id).SumAsync(x=>x.QuantityBase));
            var beforeReplay=await Counts();
            var replay=await Post<MaterialIssueView>(host.Client,$"/api/v1/stores/material-issues/from-request/{mir.Id}",secondCommand);
            Assert.True(replay.Replayed);Assert.Equal(issued.Id,replay.Id);Assert.Equal(beforeReplay,await Counts());
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item30");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"stores-workload-partial-issue.json"),
                JsonSerializer.Serialize(new{observations,beforeReplay,replay},new JsonSerializerOptions{WriteIndented=true}));
        });
        Assert.Equal(3,observations.Count);
    }
}
