using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record FifoPartialFitmentReturnContext(HttpClient Client,
        DbContextOptions<NexaErpDbContext> Options,TaxWorkflowUser User,
        Guid ProductionId,Guid StoresId,Guid AccountsId);

#if WORKFLOW_WITNESS
    [Fact]
    public Task PartialComponentReturnPreservesRetainedMachineCostAndRestoresIssueCost() =>
        RunCompletePurchaseFlow(fifoPartialReturn:RunPartialFitmentReturn);

#endif
    private static async Task RunPartialFitmentReturn(FifoPartialFitmentReturnContext context)
    {
        var client=context.Client; var user=context.User;
        await using var db=new NexaErpDbContext(context.Options);
        var fitment=await db.ComponentFitments.AsNoTracking()
            .Where(f=>!db.ComponentFitmentReversals.Any(r=>r.ComponentFitmentId==f.Id)).SingleAsync();
        Assert.Equal(.30m,fitment.QuantityBase);
        var issueLine=await db.MaterialIssueLines.AsNoTracking().SingleAsync(x=>x.Id==fitment.MaterialIssueLineId);
        var issue=await db.MaterialIssues.AsNoTracking().SingleAsync(x=>x.Id==issueLine.MaterialIssueId);
        var engineer=await db.Employees.AsNoTracking().SingleAsync(x=>x.Id==issue.IssuedToEmployeeId);
        var itemCode=await db.Items.Where(x=>x.Id==issueLine.ItemId).Select(x=>x.ItemCode).SingleAsync();
        var originalConsumptions=await db.FifoCostConsumptions.AsNoTracking().OrderBy(x=>x.Id)
            .Select(x=>new{x.Id,x.Quantity,x.UnitCost,x.ConsumedValue,x.ConsumedAt}).ToArrayAsync();
        var originalLayers=await db.FifoInventoryCostLayers.AsNoTracking().OrderBy(x=>x.Id)
            .Select(x=>new{x.Id,x.QuantityReceived,x.UnitCost,x.LayerValue,x.ReceivedAt}).ToArrayAsync();
        user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
        var fifoBefore=await Get<CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/fifo-valuation"));
        Assert.Equal(2.58m,Assert.Single(fifoBefore.Totals).GetProperty("quantity").GetDecimal());
        user.Set(context.ProductionId,"SESS-25","PRODUCTION_MANAGER");
        var path=$"/api/v1/production/component-fitments/job-orders/{fitment.JobOrderId}/actual-bom";
        var before=await Get<ActualBomView>(client,path);
        Assert.Equal(1419.60m,before.TotalAcceptedValue);
        await Post<ComponentFitmentSummary>(client,$"/api/v1/production/component-fitments/{fitment.Id}/reverse",
            new ReverseComponentFitmentRequest("Undo the original fitment before retaining 0.20 and returning 0.10",
                "fifo-partial-fitment-reverse"));
        var reversed=await Get<ActualBomView>(client,path);
        Assert.Equal(0m,reversed.TotalAcceptedValue);
        await Post<ComponentFitmentSummary>(client,"/api/v1/production/component-fitments",
            new ConfirmComponentFitmentRequest(fitment.JobOrderId,issueLine.Id,.20m,DateTimeOffset.UtcNow,
                "Confirm the retained component quantity after the full reversal",null,"fifo-partial-fitment-retain"));
        var retained=await Get<ActualBomView>(client,path);
        Assert.Equal(.20m,retained.Entries.Sum(x=>x.QuantityBase));
        Assert.Equal(944m,retained.TotalAcceptedMaterialValue);
        Assert.Equal(2.4m,retained.TotalAllocatedChargeValue);
        Assert.Equal(946.4m,retained.TotalAcceptedValue);

        user.Set(engineer.Id,engineer.EmployeeCode,"TECHNICAL_SUPPORT_MANAGER","TECHNICAL_SUPPORT_MANAGER","SERVICE_ENGINEER");
        var declared=await Post<MaterialReturnView>(client,$"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                // The fitted 0.20 has left the engineer's custody; the statement reconciles the outstanding 0.15 only.
                [new MaterialReturnLineInput(issueLine.Id,itemCode,.10m,0m,.05m)],"fifo-partial-return-declare"));
        user.Set(context.StoresId,"SESS-35",Rev869ARoleCodes.StoresExecutive);
        var accept=new AcceptMaterialReturn(declared.Version,DateTimeOffset.UtcNow,
            "Accept the removed 0.10 while 0.20 remains fitted and 0.05 remains accountable","fifo-partial-return-accept");
        var accepted=await Post<MaterialReturnView>(client,$"/api/v1/stores/material-returns/{declared.Id}/accept",accept);

        async Task<string> Counts()=>await db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'restorations',(SELECT count(*) FROM advance.fifo_cost_restorations),
              'movements',(SELECT count(*) FROM advance.stock_movements),
              'history',(SELECT count(*) FROM advance.material_return_history),
              'requests',(SELECT count(*) FROM advance.command_requests),
              'receipts',(SELECT count(*) FROM advance.command_receipts))::text AS "Value"
            """).SingleAsync();
        var beforeReplay=await Counts();
        var replay=await Post<MaterialReturnView>(client,$"/api/v1/stores/material-returns/{declared.Id}/accept",accept);
        Assert.True(replay.Replayed);
        Assert.Equal(beforeReplay,await Counts());

        user.Set(context.ProductionId,"SESS-25","PRODUCTION_MANAGER");
        var after=await Get<ActualBomView>(client,path);
        Assert.Equal(retained.TotalAcceptedValue,after.TotalAcceptedValue);
        Assert.Equal(retained.Entries.Select(x=>x.Id),after.Entries.Select(x=>x.Id));
        Assert.Equal(.20m,after.Entries.Sum(x=>x.QuantityBase));
        Assert.Equal(before.CommercialVariance.BaselineRevisionId,after.CommercialVariance.BaselineRevisionId);
        Assert.Equal(before.CommercialVariance.BaselineValue,after.CommercialVariance.BaselineValue);
        Assert.Equal(946.4m,after.CommercialVariance.ActualAcceptedValue);
        var returnLineId=accepted.Lines.Single().Id;
        var restoredQuantity=await db.Database.SqlQuery<decimal>($"""
            SELECT coalesce(sum("Quantity"),0) AS "Value" FROM advance.fifo_cost_restorations
            WHERE "MaterialReturnLineId"={returnLineId}
            """).SingleAsync();
        var restoredValue=await db.Database.SqlQuery<decimal>($"""
            SELECT coalesce(sum("RestoredValue"),0) AS "Value" FROM advance.fifo_cost_restorations
            WHERE "MaterialReturnLineId"={returnLineId}
            """).SingleAsync();
        Assert.Equal(.10m,restoredQuantity); Assert.Equal(472m,restoredValue);
        Assert.Equal(originalConsumptions,await db.FifoCostConsumptions.AsNoTracking().OrderBy(x=>x.Id)
            .Select(x=>new{x.Id,x.Quantity,x.UnitCost,x.ConsumedValue,x.ConsumedAt}).ToArrayAsync());
        Assert.Equal(originalLayers,await db.FifoInventoryCostLayers.AsNoTracking().OrderBy(x=>x.Id)
            .Select(x=>new{x.Id,x.QuantityReceived,x.UnitCost,x.LayerValue,x.ReceivedAt}).ToArrayAsync());

        user.Set(engineer.Id,engineer.EmployeeCode,"TECHNICAL_SUPPORT_MANAGER","TECHNICAL_SUPPORT_MANAGER","SERVICE_ENGINEER");
        await AssertPostStatus(client,$"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issueLine.Id,itemCode,.26m,0m,0m)],"fifo-partial-return-excess"),
            HttpStatusCode.Conflict);
        Assert.Equal(beforeReplay,await Counts());
        user.Set(context.AccountsId,"SESS-14",Rev869ARoleCodes.AccountsManager);
        var fifoAfter=await Get<CompanyReportPage>(client,WitnessReportPath("/api/v1/reports/fifo-valuation"));
        Assert.Equal(2.68m,Assert.Single(fifoAfter.Totals).GetProperty("quantity").GetDecimal());
        Assert.Equal(.10m*4732m,fifoAfter.Totals.Single().GetProperty("value").GetDecimal()-
            fifoBefore.Totals.Single().GetProperty("value").GetDecimal());
        var output=Path.Combine(FindRepositoryRoot(),"local-evidence","item15");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output,"fifo-partial-fitment-return.json"),
            JsonSerializer.Serialize(new{Before=before,AfterReversal=reversed,Retained=retained,AfterReturn=after,
                FifoBefore=fifoBefore,FifoAfter=fifoAfter,Return=accepted,restoredQuantity,restoredValue},
                new JsonSerializerOptions{WriteIndented=true}));
    }
}
