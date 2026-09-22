using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record GrnFinalizeRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection, GoodsReceiptResult Draft,
        Guid FirstOperatorId, Guid SecondOperatorId, string FirstKey, Func<string> ReadPostgresLog);

#if CONCURRENCY_WITNESS
    [Fact]
    public Task TwoReceiptOperatorsFinalizeOneGrnWithoutDuplicateStockOrFifo() =>
        RunCompletePurchaseFlow(grnRace: RunGrnFinalizeRace);

#endif
    private static async Task<GoodsReceiptResult> RunGrnFinalizeRace(GrnFinalizeRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subjects = await Query(context.Options, async db => await db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.IsActive)
            .ToDictionaryAsync(row => row.EmployeeId, row => row.Subject));
        Assert.NotEqual(context.FirstOperatorId, context.SecondOperatorId);
        var firstActor = new TaxWorkflowUser(context.FirstOperatorId, subjects[context.FirstOperatorId],
            "STORES_EXECUTIVE", assignments);
        var secondActor = new TaxWorkflowUser(context.SecondOperatorId, subjects[context.SecondOperatorId],
            "STORES_ASSISTANT", assignments);
        Assert.All(firstActor.EffectiveRoleAssignments.Concat(secondActor.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-grn-first"), firstActor,
            useRealPagePermissions: true, useRealOperationalScopes: true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-grn-second"), secondActor,
            useRealPagePermissions: true, useRealOperationalScopes: true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await observer.OpenAsync();
        var gateKey = $"STORES:GRN:FINALIZE:{companyId}:{context.Draft.Id}";
        await using var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))", observer);
        gate.Parameters.AddWithValue("key", gateKey);
        await gate.ExecuteNonQueryAsync();
        var logStart = context.ReadPostgresLog().Length;
        var path = $"/api/v1/stores/goods-receipts/{context.Draft.Id}/finalize";
        var firstRequest = new FinalizeGoodsReceiptRequest(context.Draft.Version, context.FirstKey);
        var secondRequest = new FinalizeGoodsReceiptRequest(context.Draft.Version, context.FirstKey + "-second-operator");
        var observations = new List<object>();
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstRequest);
            await ObserveBlockedBackend(observer, "race-grn-first", observer.ProcessID,
                "finalize_goods_receipt", observations);
            secondTask = TimedRacePost(secondHost.Client, path, secondRequest);
            await ObserveBlockedBackend(observer, "race-grn-second", observer.ProcessID,
                "finalize_goods_receipt", observations);
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtextextended(@key,0))", observer);
            release.Parameters.AddWithValue("key", gateKey);
            await release.ExecuteNonQueryAsync();
            if (firstTask is not null) await firstTask;
            if (secondTask is not null) await secondTask;
        }
        var first = firstTask is null ? null : await firstTask;
        var second = secondTask is null ? null : await secondTask;
        var retry = await TimedRacePost(secondHost.Client, path, secondRequest);
        var log = context.ReadPostgresLog()[logStart..];
        var states = await Query(context.Options, async state => new
        {
            Receipt = await state.GoodsReceipts.Where(row => row.Id == context.Draft.Id)
                .Select(row => new { row.Status, row.Version }).SingleAsync(),
            Batches = await state.StockPostingBatches.CountAsync(row => row.GoodsReceiptId == context.Draft.Id),
            Movements = await state.StockMovements.CountAsync(row =>
                row.StockPostingBatch!.GoodsReceiptId == context.Draft.Id),
            Quantity = await state.StockMovements.Where(row =>
                row.StockPostingBatch!.GoodsReceiptId == context.Draft.Id)
                .SumAsync(row => row.QuantityIn - row.QuantityOut),
            Layers = await state.FifoInventoryCostLayers.CountAsync(row =>
                row.GoodsReceiptLine!.GoodsReceiptId == context.Draft.Id),
            LayerQuantity = await state.FifoInventoryCostLayers.Where(row =>
                row.GoodsReceiptLine!.GoodsReceiptId == context.Draft.Id).SumAsync(row => row.QuantityReceived),
            Finalizations = await state.StoresDocumentStatusHistories.CountAsync(row =>
                row.GoodsReceiptId == context.Draft.Id && row.Action == "FINALIZED")
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "duplicate-grn-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "duplicate-grn.json"),
            JsonSerializer.Serialize(new { Observations = observations, ObservationError = observationError,
                FirstOperator = context.FirstOperatorId, SecondOperator = context.SecondOperatorId,
                First = first, Second = second, Retry = retry, States = states },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {first?.Body}; second: {second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.OK, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        using var conflict = JsonDocument.Parse(second.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.RootElement.GetProperty("Code").GetString());
        using var retryConflict = JsonDocument.Parse(retry.Body);
        Assert.Equal("BUSINESS_RULE_CONFLICT", retryConflict.RootElement.GetProperty("Code").GetString());
        Assert.Contains("finalized GRN is immutable", retryConflict.RootElement.GetProperty("Detail").GetString());
        Assert.Equal("FINALIZED", states.Receipt.Status);
        Assert.Equal(context.Draft.Version + 1, states.Receipt.Version);
        Assert.Equal(1, states.Finalizations);
        Assert.Equal(1, states.Batches);
        Assert.Equal(1, states.Movements);
        Assert.Equal(1m, states.Quantity);
        Assert.Equal(1, states.Layers);
        Assert.Equal(1m, states.LayerQuantity);
        return JsonSerializer.Deserialize<GoodsReceiptResult>(first.Body)!;
    }
}
