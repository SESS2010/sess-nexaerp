using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public Task HistoricalAcceptedReturnsGainRestorationsWithoutRewritingHistory() =>
        RunCompletePurchaseFlow(historicalFifoUpgrade:true,mixedRun:_=>Task.CompletedTask);

#endif

    // The historical FIFO upgrade intentionally starts before QcPolicyDecisions.
    // Retain its old approved-policy fixture; this is not evidence of an API approval.
    // The routine purchase witness always creates and approves the policy over HTTP.
    private static async Task CreateHistoricalQcPolicyFixture(
        DbContextOptions<NexaErpDbContext> options, Guid itemId)
    {
        await using var db = new NexaErpDbContext(options);
        Assert.DoesNotContain("20260916170000_QcPolicyDecisions", await db.Database.GetAppliedMigrationsAsync());
        var uom = await db.Uoms.OrderBy(x => x.Code).Select(x => x.Id).FirstAsync();
        db.QcInspectionPolicies.Add(new SESS.NexaERP.Domain.Inventory.QcInspectionPolicy
        {
            CompanyId = Guid.Parse("70000000-0000-0000-0000-000000000001"),
            OrganizationId = "SESS_PVT_LTD", ItemId = itemId,
            ParameterCode = "DIMENSIONAL_LIMIT", MeasurementUomId = uom,
            LowerLimit = 0, UpperLimit = 10, InspectionMethod = "Historical calibrated measurement",
            SampleSize = 1, EffectiveFrom = new DateOnly(2026, 1, 1),
            ApprovalStatus = "APPROVED", IsActive = true, CreatedBy = "HISTORICAL_FIFO_FIXTURE"
        });
        await db.SaveChangesAsync();
    }

    private static async Task<string> ReadOriginalFifoHistory(DbContextOptions<NexaErpDbContext> options)
    {
        await using var db=new NexaErpDbContext(options);
        return await db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'layers',(SELECT jsonb_agg(to_jsonb(f) ORDER BY f."Id") FROM advance.fifo_inventory_cost_layers f),
              'consumptions',(SELECT jsonb_agg(to_jsonb(c) ORDER BY c."Id") FROM advance.fifo_cost_consumptions c),
              'returns',(SELECT jsonb_agg(to_jsonb(r) ORDER BY r."Id") FROM advance.material_returns r),
              'returnLines',(SELECT jsonb_agg(to_jsonb(r) ORDER BY r."Id") FROM advance.material_return_lines r),
              'returnHistory',(SELECT jsonb_agg(to_jsonb(h) ORDER BY h."Id") FROM advance.material_return_history h),
              'movements',(SELECT jsonb_agg(to_jsonb(m) ORDER BY m."Id") FROM advance.stock_movements m),
              'adjustments',(SELECT jsonb_agg(to_jsonb(a) ORDER BY a."Id") FROM advance.fifo_landed_cost_adjustments a)
            )::text AS "Value"
            """).SingleAsync();
    }

    private static async Task VerifyHistoricalFifoRestoration(DbContextOptions<NexaErpDbContext> options,
        string originalHistory)
    {
        Assert.Equal(originalHistory,await ReadOriginalFifoHistory(options));
        await using var db=new NexaErpDbContext(options);
        Assert.Equal(1.63m,await db.Database.SqlQueryRaw<decimal>("""
            SELECT sum("Quantity") AS "Value" FROM advance.fifo_cost_restorations
            WHERE "IsHistoricalReconciliation"
            """).SingleAsync());
        Assert.Equal(0,await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM advance.fifo_cost_restorations r
            JOIN advance.material_return_lines rl ON rl."Id"=r."MaterialReturnLineId"
            JOIN advance.material_returns h ON h."Id"=rl."MaterialReturnId"
            WHERE NOT r."IsHistoricalReconciliation" OR r."EffectiveAt"<>h."AcceptedAt"
              OR r."RecordedAt"<r."EffectiveAt"
            """).SingleAsync());
    }
}
