using System.Net;
using System.Net.Http.Json;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed class QcDiscrepancyWitnessComplete:Exception {}

#if WORKFLOW_WITNESS
    [Fact]
    public async Task StoresQcStockRetainsInspectedDiscrepancyWithoutStockDisposition()
    {
        await Assert.ThrowsAsync<QcDiscrepancyWitnessComplete>(()=>RunCompletePurchaseFlow(grnRace:async context=>
        {
            await using var source=new NexaErpDbContext(context.Options);
            var actor=await BankAdviceActor(context.Options);
            var subjects=await source.EmployeeIdentityMappings.Where(x=>x.CompanyId==Guid.Parse("70000000-0000-0000-0000-000000000001")&&x.IsActive)
                .ToDictionaryAsync(x=>x.EmployeeId,x=>x.Subject);
            actor.Set(context.FirstOperatorId,subjects[context.FirstOperatorId],"STORES_EXECUTIVE");
            await using var host=await PurchaseFlowHost.StartAsync(context.RuntimeConnection,actor,true,true);
            var grn=await Post<GoodsReceiptResult>(host.Client,$"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize",
                new FinalizeGoodsReceiptRequest(context.Draft.Version,context.FirstKey));
            var allocation=grn.Lines.Single().Lots.Single();
            var qc=await source.Employees.SingleAsync(x=>x.EmployeeCode=="SESS-33");
            actor.Set(qc.Id,subjects[qc.Id],"QC_MANAGER");
            var countsBefore=await QcStockCounts(source);
            var command=new FinalizeQcInspectionRequest(allocation.Id,DateTimeOffset.UtcNow,0,0,1,null,[],[]);
            var logStart=context.ReadPostgresLog().Length;
            string baselineLog;
            await source.Database.ExecuteSqlRawAsync(QcDiscrepancyPostingSql.Definition(false));
            try
            {
                using var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/qc/inspections"){Content=JsonContent.Create(command)};
                request.Headers.Add("Idempotency-Key","qc-stock-pure-discrepancy");
                using var failed=await host.Client.SendAsync(request);
                Assert.Equal(HttpStatusCode.InternalServerError,failed.StatusCode);
                baselineLog=context.ReadPostgresLog()[logStart..];
                Assert.Contains("Finalised QC revision requires exactly one atomic disposition batch.",baselineLog,StringComparison.Ordinal);
                Assert.Equal(countsBefore,await QcStockCounts(source));
            }
            finally{await source.Database.ExecuteSqlRawAsync(QcDiscrepancyPostingSql.Definition(true));}
            var inspection=await Post<QcInspectionResult>(host.Client,"/api/v1/qc/inspections",command,"qc-stock-pure-discrepancy");
            var replay=await Post<QcInspectionResult>(host.Client,"/api/v1/qc/inspections",command,"qc-stock-pure-discrepancy");
            Assert.Equal(inspection.RevisionId,replay.RevisionId); Assert.True(replay.Replayed);
            Assert.Null(inspection.StockPostingBatchId);
            Assert.Equal(0,inspection.AcceptedQuantity);Assert.Equal(0,inspection.RejectedQuantity);
            var oldQueue=await Get<PagedResponse<QcQueueItem>>(host.Client,"/api/v1/qc/queue?page=1&pageSize=100");
            Assert.DoesNotContain(oldQueue.Items,x=>x.GoodsReceiptLineLotAllocationId==allocation.Id);
            await using var runtime=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql(context.RuntimeConnection).Options);
            var reader=await QcStockReader(source,"SESS-35");
            var service=new EfStoresQcStockService(runtime,reader,WorkloadCalendar());
            var before=await QcStockCounts(source);
            var page=await ObserveSingleReportCommand(()=>service.GetAsync(new("QC_HOLD",grn.Id),default));
            var after=await QcStockCounts(source);
            Assert.Equal(before,after);
            var row=Assert.Single(page.Rows);
            Assert.Equal(allocation.Id,row.AllocationId); Assert.Equal(1m,row.Quantity); Assert.True(row.IsOverdue);
            Assert.Null(row.ReceiptProvisionalValue);
            var policy=new SESS.NexaERP.Domain.Inventory.QcInspectionPolicy
            {
                CompanyId=Guid.Parse("70000000-0000-0000-0000-000000000001"),OrganizationId="SESS_PVT_LTD",
                ItemId=grn.Lines.Single().ItemId,ParameterCode="DISCREPANCY_RESOLUTION",
                MeasurementUomId=await source.Uoms.OrderBy(x=>x.Code).Select(x=>x.Id).FirstAsync(),
                LowerLimit=0,UpperLimit=10,InspectionMethod="Disposable discrepancy resolution",SampleSize=1,
                EffectiveFrom=new DateOnly(2026,1,1),ApprovalStatus="APPROVED",IsActive=true,CreatedBy="QC_STOCK_FIXTURE"
            };
            source.QcInspectionPolicies.Add(policy);await source.SaveChangesAsync();
            var available=await source.WarehouseConditionLocations.Where(x=>x.CompanyId==policy.CompanyId&&x.ConditionCode=="AVAILABLE"&&x.IsActive)
                .OrderBy(x=>x.Id).Select(x=>x.Id).FirstAsync();
            var correction=new CorrectQcInspectionRequest(inspection.RevisionId,"Half resolved by measurement",DateTimeOffset.UtcNow,
                .5m,0,.5m,available,[new QcParameterResultRequest(policy.Id,1,5,null,"PASS",null)],[]);
            var partial=await Post<QcInspectionResult>(host.Client,$"/api/v1/qc/inspections/{inspection.InspectionNumber}/corrections",
                correction,"qc-stock-half-resolved");
            Assert.NotNull(partial.StockPostingBatchId);Assert.Equal(2,partial.RevisionNumber);
            var partialBefore=await QcStockCounts(source);
            var partialPage=await ObserveSingleReportCommand(()=>service.GetAsync(new("QC_HOLD",grn.Id),default));
            var partialAfter=await QcStockCounts(source);
            Assert.Equal(partialBefore,partialAfter);Assert.Equal(.5m,Assert.Single(partialPage.Rows).Quantity);
            var finalCommand=new CorrectQcInspectionRequest(partial.RevisionId,"Remaining discrepancy resolved",DateTimeOffset.UtcNow,
                1m,0,0,available,[new QcParameterResultRequest(policy.Id,1,5,null,"PASS",null)],[]);
            var resolved=await Post<QcInspectionResult>(host.Client,$"/api/v1/qc/inspections/{inspection.InspectionNumber}/corrections",
                finalCommand,"qc-stock-fully-resolved");
            Assert.NotNull(resolved.StockPostingBatchId);Assert.Equal(3,resolved.RevisionNumber);
            var resolvedBefore=await QcStockCounts(source);
            var resolvedPage=await ObserveSingleReportCommand(()=>service.GetAsync(new("QC_HOLD",grn.Id),default));
            var resolvedAfter=await QcStockCounts(source);
            Assert.Equal(resolvedBefore,resolvedAfter);Assert.Empty(resolvedPage.Rows);
            var finalReplay=await Post<QcInspectionResult>(host.Client,$"/api/v1/qc/inspections/{inspection.InspectionNumber}/corrections",
                finalCommand,"qc-stock-fully-resolved");
            Assert.Equal(resolved.RevisionId,finalReplay.RevisionId);Assert.Equal(resolvedAfter,await QcStockCounts(source));
            var evidence=Path.Combine(FindRepositoryRoot(),"local-evidence","item30");
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence,"stores-qc-stock-discrepancy.json"),
                JsonSerializer.Serialize(new{countsBefore,before,after,baselineLog,inspection.InspectionId,inspection.StockPostingBatchId,Page=page,
                    partialBefore,partialAfter,PartialPage=partialPage,resolvedBefore,resolvedAfter,ResolvedPage=resolvedPage,
                    Completion="Terminal QC discrepancy edge; not the full PR-to-Actual-BOM chain."},
                    new JsonSerializerOptions{WriteIndented=true}));
            throw new QcDiscrepancyWitnessComplete();
        }));
    }
#endif
}
