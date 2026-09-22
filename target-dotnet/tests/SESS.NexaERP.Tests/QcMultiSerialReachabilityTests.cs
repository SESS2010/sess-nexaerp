using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static Task<decimal> PurchaseWitnessRequestedQuantity(DbContextOptions<NexaErpDbContext> options, decimal shortfall) =>
        Query(options, async db =>
        {
            var company = await db.Companies.Where(x => x.Code == "SESS_PVT_LTD").Select(x => x.Id).SingleAsync();
            var item = await db.Items.Where(x => x.ItemCode == "TRIAL-ITEM-001").Select(x => x.Id).SingleAsync();
            var warehouse = await db.Warehouses.Where(x => x.CompanyId == company && x.WarehouseCode == "TRIAL-WH-C01").Select(x => x.Id).SingleAsync();
            var rack = await db.RackBins.Where(x => x.CompanyId == company && x.WarehouseId == warehouse && x.BinCode == "TRIAL-C01-GEN-01").Select(x => x.Id).SingleAsync();
            var onHand = await db.StockMovements.Where(x => x.CompanyId == company && x.ItemId == item && x.WarehouseId == warehouse && x.RackBinId == rack)
                .SumAsync(x => x.QuantityIn - x.QuantityOut);
            var reserved = await db.StockReservations.Where(x => x.CompanyId == company && x.ItemId == item && x.WarehouseId == warehouse && x.RackBinId == rack && x.Status == "Active")
                .SumAsync(x => x.ReservedQuantity);
            return Math.Max(onHand - reserved, 0m) + shortfall;
        });

    private static async Task ProveMultiSerialQcDiscrepancy(HttpClient client,
        DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, GoodsReceiptResult grn, Guid qcId, Guid tdId)
    {
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        var queue = await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?pageSize=100");
        var lot = Assert.Single(queue.Items, x => x.GrnNumber == grn.GrnNumber);
        Assert.Equal(2, lot.InventorySerialIds.Count);
        var policies = await Get<JsonElement>(client,
            "/api/v1/rev869a/configuration/qc-inspection-policies?effectiveOnly=true&itemId=" + lot.ItemId);
        var policyId = Assert.Single(policies.EnumerateArray()).GetProperty("Id").GetGuid();
        var samples = new[] { new QcParameterResultRequest(policyId, 1, 12, null, "FAIL", "Requires review") };
        var before = await Query(options, db => db.StockMovements.CountAsync());
        var command = new FinalizeQcInspectionRequest(lot.GoodsReceiptLineLotAllocationId,
            DateTimeOffset.UtcNow, 0, 0, 2, null, samples, []);
        var inspection = await Post<QcInspectionResult>(client, "/api/v1/qc/inspections", command, "multi-qc-discrepancy");
        Assert.Null(inspection.StockPostingBatchId);
        Assert.Equal(before, await Query(options, db => db.StockMovements.CountAsync()));
        var replay = await Post<QcInspectionResult>(client, "/api/v1/qc/inspections", command, "multi-qc-discrepancy");
        Assert.True(replay.Replayed);
        Assert.Equal(inspection.RevisionId, replay.RevisionId);
        await AssertQcSerialBalances(options, lot, "QC_HOLD", 2m);
        // Reopen the pending queue: no browser memory or administrator lookup supplies correction inputs.
        var pending = Assert.Single((await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?pageSize=100")).Items,
            x => x.GoodsReceiptLineLotAllocationId == lot.GoodsReceiptLineLotAllocationId);
        Assert.Equal(inspection.RevisionId, pending.CurrentRevisionId);
        Assert.Equal(2m, pending.DiscrepancyPendingQuantity);
        var detail = await Get<QcInspectionResult>(client, "/api/v1/qc/inspections/" + pending.InspectionNumber);
        Assert.Equal(inspection.RevisionId, detail.RevisionId);
        Assert.Equal(lot.InventorySerialIds, detail.InventorySerialIds);
        var locations = await Get<JsonElement>(client, "/api/v1/rev869a/configuration/warehouse-condition-locations");
        var available = locations.EnumerateArray().First(x => x.GetProperty("ConditionCode").GetString() == "AVAILABLE")
            .GetProperty("Id").GetGuid();
        var serials = new[]
        {
            new QcSerialDispositionRequest(detail.InventorySerialIds[0], "ACCEPTED", null),
            new QcSerialDispositionRequest(detail.InventorySerialIds[1], "REJECTED", "Failed measured limit")
        };
        var corrected = await Post<QcInspectionResult>(client, "/api/v1/qc/inspections/" + inspection.InspectionNumber + "/corrections",
            new CorrectQcInspectionRequest(detail.RevisionId, "Discrepancy resolved by recount and inspection",
                DateTimeOffset.UtcNow, 1, 1, 0, available, samples, serials), "multi-qc-resolved");
        Assert.NotNull(corrected.StockPostingBatchId);
        Assert.DoesNotContain((await Get<PagedResponse<QcQueueItem>>(client, "/api/v1/qc/queue?pageSize=100")).Items,
            x => x.GoodsReceiptLineLotAllocationId == lot.GoodsReceiptLineLotAllocationId);
        Assert.Equal(2, corrected.SerialDispositions.Count);
        await AssertQcSerialBalances(options, lot, "QC_HOLD", 0m);
        await AssertQcSerialBalances(options, lot, "AVAILABLE", 1m);
        await AssertQcSerialBalances(options, lot, "PENDING_RETURNABLE_DC", 1m);
        // Each remaining governed QC decision is exercised by its seeded actor.
        var inspectionRead = await Get<QcInspectionResult>(client, "/api/v1/qc/inspections/" + inspection.InspectionNumber);
        var failed = Assert.Single(inspectionRead.ParameterResults, x => x.Result == "FAIL");
        var rejectedSerial = Assert.Single(inspectionRead.SerialDispositions, x => x.Disposition == "REJECTED").InventorySerialId;
        var concessionRequest = new CreateInventoryConcessionRequest(inspectionRead.QcInspectionLotDispositionId,
            failed.Id, 1m, failed.ParameterCode, failed.MeasuredValue, "Controlled measured deviation",
            "QC reachability witness", [rejectedSerial]);
        var first = await Post<InventoryConcessionResult>(client, "/api/v1/qc/concessions",
            concessionRequest, "multi-concession-first");
        Assert.Equal("DRAFT", first.Status);
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        first = await Get<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + first.ConcessionNumber);
        var denied = await Post<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + first.ConcessionNumber + "/reject",
            new RejectInventoryConcessionRequest(first.Version, "Deviation refused"), "multi-concession-reject");
        Assert.Equal("REJECTED", denied.Status);
        user.Set(qcId, "SESS-33", "QC_MANAGER");
        var second = await Post<InventoryConcessionResult>(client, "/api/v1/qc/concessions",
            concessionRequest, "multi-concession-second");
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        second = await Get<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + second.ConcessionNumber);
        var tdLocations = await Get<JsonElement>(client, "/api/v1/rev869a/configuration/warehouse-condition-locations");
        Assert.Contains(tdLocations.EnumerateArray(), x => x.GetProperty("Id").GetGuid() == available);
        var approved = await Post<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + second.ConcessionNumber + "/approve",
            new ApproveInventoryConcessionRequest(second.Version, available, "Accept controlled deviation"),
            "multi-concession-approve");
        Assert.Equal("APPROVED", approved.Status);
        approved = await Get<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + approved.ConcessionNumber);
        var reversed = await Post<InventoryConcessionResult>(client, "/api/v1/qc/concessions/" + approved.ConcessionNumber + "/reverse",
            new ReverseInventoryConcessionRequest(approved.Version, "Withdraw concession before use"), "multi-concession-reverse");
        Assert.Equal("REVERSED", reversed.Status);
        await AssertQcSerialBalances(options, lot, "AVAILABLE", 1m);
        await AssertQcSerialBalances(options, lot, "PENDING_RETURNABLE_DC", 1m);
        await using var db = new NexaErpDbContext(options);
        var revisions = await db.QcInspectionRevisions.AsNoTracking().Where(x =>
            x.QcInspectionId == inspection.InspectionId).OrderBy(x => x.RevisionNumber).ToListAsync();
        Assert.Equal(2, revisions.Count);
        Assert.Equal(2m, revisions[0].DiscrepancyPendingQuantity);
        Assert.Equal(revisions[0].Id, revisions[1].RevisesRevisionId);
        Assert.Equal(4, await db.StockMovements.CountAsync(x => x.StockPostingBatchId == corrected.StockPostingBatchId));
        Assert.Equal(2, await db.StockMovements.Where(x => x.StockPostingBatchId == corrected.StockPostingBatchId)
            .Select(x => x.InventorySerialId).Distinct().CountAsync());
    }

    private static async Task AssertQcSerialBalances(DbContextOptions<NexaErpDbContext> options,
        QcQueueItem lot, string condition, decimal total)
    {
        await using var db = new NexaErpDbContext(options);
        var balances = await db.StockMovements.Where(x => x.GoodsReceiptLineLotAllocationId == lot.GoodsReceiptLineLotAllocationId
            && x.ConditionCode == condition).GroupBy(x => x.InventorySerialId)
            .Select(x => new { Serial = x.Key, Quantity = x.Sum(m => m.QuantityIn - m.QuantityOut) }).ToListAsync();
        Assert.Equal(total, balances.Sum(x => x.Quantity));
        Assert.All(balances, x => { Assert.Contains(x.Serial!.Value, lot.InventorySerialIds); Assert.InRange(x.Quantity, 0m, 1m); });
    }
}
