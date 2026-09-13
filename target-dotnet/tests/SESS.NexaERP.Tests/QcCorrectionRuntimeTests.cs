using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record QcCorrectionContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, QcInspectionResult Original, FinalizeQcInspectionRequest OriginalCommand,
        Guid InspectorId, Func<string> ReadPostgresLog);

    [Fact]
    public async Task QcCorrectionReversesOriginalPostingAndRetainsInspectionHistory()
    {
        var observed = false;
        await RunCompletePurchaseFlow(qcCorrection: context =>
        {
            observed = true;
            return RunQcCorrection(context);
        });
        Assert.True(observed, "The serialized QC correction callback must execute.");
    }

    private static async Task<QcInspectionResult> RunQcCorrection(QcCorrectionContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subject = await Query(context.Options, db => db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.EmployeeId == context.InspectorId && row.IsActive)
            .Select(row => row.Subject).SingleAsync());
        var actor = new TaxWorkflowUser(context.InspectorId, subject, "QC_MANAGER", assignments);
        Assert.All(actor.EffectiveRoleAssignments, assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        await using var host = await PurchaseFlowHost.StartAsync(context.RuntimeConnection, actor, true, true);
        var original = context.Original;
        var command = context.OriginalCommand;
        var correction = new CorrectQcInspectionRequest(original.RevisionId,
            "Correct recorded measurement from 12 to 11; rejection remains required.",
            command.InspectionStartedAt, command.AcceptedQuantity, command.RejectedQuantity,
            command.DiscrepancyPendingQuantity, command.AcceptedConditionLocationId,
            command.ParameterResults.Select(row => row with { ObservedNumericValue = 11m }).ToArray(),
            command.SerialDispositions);
        var path = $"/api/v1/qc/inspections/{original.InspectionNumber}/corrections";
        const string key = "MD-qc-correction-001";
        var serialId = Assert.Single(original.SerialDispositions).InventorySerialId;
        var beforeMovements = await Query(context.Options, db => db.StockMovements.AsNoTracking()
            .Where(row => row.StockPostingBatchId == original.StockPostingBatchId)
            .OrderBy(row => row.BatchLineOrdinal).ToArrayAsync());
        var logStart = context.ReadPostgresLog().Length;
        var response = await TimedQcPost(host.Client, path, correction, key);
        var log = context.ReadPostgresLog()[logStart..];
        var state = await Query(context.Options, async db => new
        {
            Revisions = await db.QcInspectionRevisions.AsNoTracking()
                .Where(row => row.QcInspectionId == original.InspectionId).OrderBy(row => row.RevisionNumber)
                .Select(row => new { row.Id, row.RevisionNumber, row.RevisesRevisionId, row.Status }).ToArrayAsync(),
            OriginalMovements = await db.StockMovements.AsNoTracking()
                .Where(row => row.StockPostingBatchId == original.StockPostingBatchId)
                .OrderBy(row => row.BatchLineOrdinal).ToArrayAsync(),
            ReversalMovements = await db.StockMovements.AsNoTracking()
                .Where(row => row.StockPostingBatch!.ReversesPostingBatchId == original.StockPostingBatchId)
                .OrderBy(row => row.BatchLineOrdinal).ToArrayAsync(),
            Measurements = await db.QcInspectionParameterResults.AsNoTracking()
                .Where(row => row.QcInspectionRevision!.QcInspectionId == original.InspectionId)
                .OrderBy(row => row.QcInspectionRevision!.RevisionNumber)
                .Select(row => row.ObservedNumericValue).ToArrayAsync(),
            Balances = await db.StockMovements.AsNoTracking().Where(row => row.InventorySerialId == serialId)
                .GroupBy(row => new { row.ItemId, row.WarehouseConditionLocationId, row.ConditionCode,
                    row.OwnershipAccountId, row.CustodyAssignmentId, row.InventoryProvenanceLayerId,
                    row.InventoryLotId, row.InventorySerialId })
                .Select(group => new { group.Key, Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) })
                .ToArrayAsync(),
            ReversalCount = await db.StockPostingBatches.CountAsync(row =>
                row.ReversesPostingBatchId == original.StockPostingBatchId),
            SerialQuantity = await db.StockMovements.Where(row =>
                row.InventorySerialId == serialId)
                .SumAsync(row => row.QuantityIn - row.QuantityOut)
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-correction-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-correction.json"),
            JsonSerializer.Serialize(new { Original = original, Command = correction, Response = response, State = state },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(response.Status == HttpStatusCode.OK, response.Body);
        var result = JsonSerializer.Deserialize<QcInspectionResult>(response.Body)!;
        Assert.Equal(original.InspectionId, result.InspectionId);
        Assert.Equal(2, result.RevisionNumber);
        Assert.NotEqual(original.RevisionId, result.RevisionId);
        Assert.Equal(original.AcceptedQuantity, result.AcceptedQuantity);
        Assert.Equal(original.RejectedQuantity, result.RejectedQuantity);
        Assert.Equal(2, state.Revisions.Length);
        Assert.Equal(original.RevisionId, state.Revisions[1].RevisesRevisionId);
        Assert.All(state.Revisions, row => Assert.Equal("FINALIZED", row.Status));
        Assert.Equal(1, state.ReversalCount);
        Assert.Equal(1m, state.SerialQuantity);
        Assert.Equal(JsonSerializer.Serialize(beforeMovements), JsonSerializer.Serialize(state.OriginalMovements));
        Assert.Equal(new decimal?[] { 12m, 11m }, state.Measurements);
        Assert.Equal(beforeMovements.Length, state.ReversalMovements.Length);
        foreach (var movement in beforeMovements)
        {
            var reversal = Assert.Single(state.ReversalMovements, row => row.ReversesStockMovementId == movement.Id);
            Assert.Equal(movement.ItemId, reversal.ItemId);
            Assert.Equal(movement.WarehouseConditionLocationId, reversal.WarehouseConditionLocationId);
            Assert.Equal(movement.OwnershipAccountId, reversal.OwnershipAccountId);
            Assert.Equal(movement.CustodyAssignmentId, reversal.CustodyAssignmentId);
            Assert.Equal(movement.InventoryProvenanceLayerId, reversal.InventoryProvenanceLayerId);
            Assert.Equal(movement.InventoryLotId, reversal.InventoryLotId);
            Assert.Equal(movement.InventorySerialId, reversal.InventorySerialId);
            Assert.Equal(movement.OriginGoodsReceiptLineId, reversal.OriginGoodsReceiptLineId);
            Assert.Equal(movement.QcInspectionLotDispositionId, reversal.QcInspectionLotDispositionId);
            Assert.Equal(movement.QuantityIn, reversal.QuantityOut);
            Assert.Equal(movement.QuantityOut, reversal.QuantityIn);
        }
        Assert.All(state.Balances, row => Assert.InRange(row.Quantity, 0m, 1m));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "QC_HOLD").Sum(row => row.Quantity));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "AVAILABLE").Sum(row => row.Quantity));
        Assert.Equal(1m, state.Balances.Where(row => row.Key.ConditionCode == "PENDING_RETURNABLE_DC").Sum(row => row.Quantity));
        var replay = await TimedQcPost(host.Client, path, correction, key);
        Assert.True(replay.Status == HttpStatusCode.OK, replay.Body);
        var replayed = JsonSerializer.Deserialize<QcInspectionResult>(replay.Body)!;
        Assert.True(replayed.Replayed);
        Assert.Equal(result.RevisionId, replayed.RevisionId);
        Assert.Equal(result.StockPostingBatchId, replayed.StockPostingBatchId);
        return result;
    }

    private static async Task<RaceHttpResult> TimedQcPost<T>(HttpClient client, string path, T body, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key);
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        return new(response.StatusCode, elapsed.Elapsed.TotalSeconds, await response.Content.ReadAsStringAsync());
    }
}
