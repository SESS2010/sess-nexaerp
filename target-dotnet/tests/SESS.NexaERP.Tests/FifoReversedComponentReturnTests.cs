#if WORKFLOW_WITNESS
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public Task ReversedComponentReturnedToStoresDoesNotChargeTheMachineTwice() =>
        RunCompletePurchaseFlow(fifoPartialReturn: RunReversedComponentReturn);

    private static async Task RunReversedComponentReturn(FifoPartialFitmentReturnContext context)
    {
        await using var db = new NexaErpDbContext(context.Options);
        var client = context.Client;
        var user = context.User;
        var fitment = await db.ComponentFitments.AsNoTracking()
            .Where(f => !db.ComponentFitmentReversals.Any(r => r.ComponentFitmentId == f.Id)).SingleAsync();
        Assert.Equal(.30m, fitment.QuantityBase);
        var issueLine = await db.MaterialIssueLines.AsNoTracking().SingleAsync(x => x.Id == fitment.MaterialIssueLineId);
        var issue = await db.MaterialIssues.AsNoTracking().SingleAsync(x => x.Id == issueLine.MaterialIssueId);
        var engineer = await db.Employees.AsNoTracking().SingleAsync(x => x.Id == issue.IssuedToEmployeeId);
        var itemCode = await db.Items.Where(x => x.Id == issueLine.ItemId).Select(x => x.ItemCode).SingleAsync();
        var originalConsumptions = await db.FifoCostConsumptions.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Quantity, x.UnitCost, x.ConsumedValue, x.ConsumedAt }).ToArrayAsync();
        var originalLayers = await db.FifoInventoryCostLayers.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.QuantityReceived, x.UnitCost, x.LayerValue, x.ReceivedAt }).ToArrayAsync();
        user.Set(context.AccountsId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var fifoBefore = await Get<CompanyReportPage>(client, WitnessReportPath("/api/v1/reports/fifo-valuation"));
        Assert.Equal(2.58m, Assert.Single(fifoBefore.Totals).GetProperty("quantity").GetDecimal());

        user.Set(context.ProductionId, "SESS-25", "PRODUCTION_MANAGER");
        var path = $"/api/v1/production/component-fitments/job-orders/{fitment.JobOrderId}/actual-bom";
        var before = await Get<ActualBomView>(client, path);
        Assert.Equal(1419.60m, before.TotalAcceptedValue);
        await Post<ComponentFitmentSummary>(client, $"/api/v1/production/component-fitments/{fitment.Id}/reverse",
            new ReverseComponentFitmentRequest("Remove this component and return it to Stores without re-fitment",
                "reversed-component-return-reverse"));
        var reversed = await Get<ActualBomView>(client, path);
        Assert.Equal(0m, reversed.Entries.Sum(x => x.QuantityBase));
        Assert.Equal(0m, reversed.TotalAcceptedMaterialValue);
        Assert.Equal(0m, reversed.TotalAllocatedChargeValue);
        Assert.Equal(0m, reversed.TotalAcceptedValue);
        Assert.Equal(0m, reversed.CommercialVariance.ActualAcceptedValue);

        user.Set(engineer.Id, engineer.EmployeeCode, "TECHNICAL_SUPPORT_MANAGER", "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER");
        var declared = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/from-issue/{issue.Id}",
            new CreateMaterialReturn(DateTimeOffset.UtcNow,
                [new MaterialReturnLineInput(issueLine.Id, itemCode, .30m, 0m, .05m)],
                "reversed-component-return-declare"));
        user.Set(context.StoresId, "SESS-35", Rev869ARoleCodes.StoresExecutive);
        var accept = new AcceptMaterialReturn(declared.Version, DateTimeOffset.UtcNow,
            "Accept the reversed 0.30 component; 0.05 remains accountable with the engineer",
            "reversed-component-return-accept");
        var accepted = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/{declared.Id}/accept", accept);
        async Task<string> Counts() => await db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'restorations',(SELECT count(*) FROM advance.fifo_cost_restorations),
              'movements',(SELECT count(*) FROM advance.stock_movements),
              'history',(SELECT count(*) FROM advance.material_return_history),
              'requests',(SELECT count(*) FROM advance.command_requests),
              'receipts',(SELECT count(*) FROM advance.command_receipts))::text AS "Value"
            """).SingleAsync();
        var afterAcceptanceCounts = await Counts();
        var replay = await Post<MaterialReturnView>(client, $"/api/v1/stores/material-returns/{declared.Id}/accept", accept);
        Assert.True(replay.Replayed);
        Assert.Equal(afterAcceptanceCounts, await Counts());

        user.Set(context.ProductionId, "SESS-25", "PRODUCTION_MANAGER");
        var afterReturn = await Get<ActualBomView>(client, path);
        Assert.Equal(reversed.Entries.Select(x => x.Id), afterReturn.Entries.Select(x => x.Id));
        Assert.Equal(0m, afterReturn.Entries.Sum(x => x.QuantityBase));
        Assert.Equal(0m, afterReturn.TotalAcceptedMaterialValue);
        Assert.Equal(0m, afterReturn.TotalAllocatedChargeValue);
        Assert.Equal(0m, afterReturn.TotalAcceptedValue);
        Assert.Equal(0m, afterReturn.CommercialVariance.ActualAcceptedValue);
        Assert.Equal(before.CommercialVariance.BaselineRevisionId, afterReturn.CommercialVariance.BaselineRevisionId);
        Assert.Equal(before.CommercialVariance.BaselineValue, afterReturn.CommercialVariance.BaselineValue);
        var returnLineId = Assert.Single(accepted.Lines).Id;
        var restoredQuantity = await db.Database.SqlQuery<decimal>($"""
            SELECT coalesce(sum("Quantity"),0) AS "Value" FROM advance.fifo_cost_restorations
            WHERE "MaterialReturnLineId"={returnLineId}
            """).SingleAsync();
        var restoredValue = await db.Database.SqlQuery<decimal>($"""
            SELECT coalesce(sum("RestoredValue"),0) AS "Value" FROM advance.fifo_cost_restorations
            WHERE "MaterialReturnLineId"={returnLineId}
            """).SingleAsync();
        Assert.Equal(.30m, restoredQuantity);
        Assert.Equal(1416m, restoredValue);
        Assert.Equal(originalConsumptions, await db.FifoCostConsumptions.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Quantity, x.UnitCost, x.ConsumedValue, x.ConsumedAt }).ToArrayAsync());
        Assert.Equal(originalLayers, await db.FifoInventoryCostLayers.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.QuantityReceived, x.UnitCost, x.LayerValue, x.ReceivedAt }).ToArrayAsync());
        user.Set(context.AccountsId, "SESS-14", Rev869ARoleCodes.AccountsManager);
        var fifoAfter = await Get<CompanyReportPage>(client, WitnessReportPath("/api/v1/reports/fifo-valuation"));
        Assert.Equal(2.88m, Assert.Single(fifoAfter.Totals).GetProperty("quantity").GetDecimal());
        Assert.Equal(1419.60m, fifoAfter.Totals.Single().GetProperty("value").GetDecimal()
            - fifoBefore.Totals.Single().GetProperty("value").GetDecimal());
        Assert.Equal(afterAcceptanceCounts, await Counts());
        var output = Path.Combine(FindRepositoryRoot(), "local-evidence", "item15");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "fifo-reversed-component-return.json"),
            JsonSerializer.Serialize(new { Before = before, Reversed = reversed, AfterReturn = afterReturn,
                FifoBefore = fifoBefore, FifoAfter = fifoAfter, Return = accepted, restoredQuantity, restoredValue,
                afterAcceptanceCounts }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
#endif