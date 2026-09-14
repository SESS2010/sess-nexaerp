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
    private sealed record SerialIssueRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection,
        MaterialIssueRequestView Draft, CreateMaterialIssue Command,
        Guid FirstOperatorId, Guid SecondOperatorId, Func<string> ReadPostgresLog, Action? RestartDatabase = null);

#if CONCURRENCY_WITNESS
    [Fact]
    public async Task TwoStoresOperatorsCannotIssueTheSameSerialTwice()
    {
        var observed = false;
        await RunCompletePurchaseFlow(issueRace: context =>
        {
            observed = true;
            return RunSerialIssueRace(context);
        });
        Assert.True(observed, "The existing serialized issue must execute the race callback.");
    }

#endif
    private static async Task<MaterialIssueView> RunSerialIssueRace(SerialIssueRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var scan = Assert.Single(context.Command.Scans);
        Assert.NotNull(scan.InventorySerialId);
        var serialId = scan.InventorySerialId.Value;
        var itemId = Assert.Single(context.Draft.Lines).ItemId;
        Assert.Equal(1m, scan.Quantity);
        Assert.Equal("APPROVED", context.Draft.Status);
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
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-issue-first"), firstActor, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-issue-second"), secondActor, true, true);

        var before = await Query(context.Options, async db => new
        {
            WarehouseQuantity = await db.StockMovements.Where(row => row.CompanyId == companyId
                && row.InventorySerialId == serialId && row.ConditionCode == "AVAILABLE"
                && row.CustodyAssignment!.CustodyAccount!.CustodyType == "WAREHOUSE")
                .SumAsync(row => row.QuantityIn - row.QuantityOut),
            Oldest = await db.FifoInventoryCostLayers.Where(row => row.CompanyId == companyId && row.ItemId == itemId)
                .Select(row => new { row.Id, row.ReceivedAt,
                    Remaining = row.QuantityReceived - (db.FifoCostConsumptions
                        .Where(use => use.FifoInventoryCostLayerId == row.Id).Sum(use => (decimal?)use.Quantity) ?? 0m) })
                .Where(row => row.Remaining > 0).OrderBy(row => row.ReceivedAt).ThenBy(row => row.Id).FirstAsync()
        });
        Assert.Equal(1m, before.WarehouseQuantity);
        Assert.Equal(1m, before.Oldest.Remaining);
        await using var db = new NexaErpDbContext(context.Options);
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await observer.OpenAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}"
            : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var gateKey = $"NUMBER:SESS_PVT_LTD:{year}:MI";
        // Hold the existing number lock. Both API transactions have already read
        // their request, but stock selection occurs after this production lock.
        await using var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))", observer);
        gate.Parameters.AddWithValue("key", gateKey);
        await gate.ExecuteNonQueryAsync();
        var firstCommand = context.Command;
        var secondCommand = context.Command with { IdempotencyKey = context.Command.IdempotencyKey + "-second-operator" };
        var path = $"/api/v1/stores/material-issues/from-request/{context.Draft.Id}";
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstCommand);
            var firstPid = await ObserveEntityWriteWait(observer, "race-issue-first",
                [observer.ProcessID], observations, "pg_advisory_xact_lock", "SELECT");
            secondTask = TimedRacePost(secondHost.Client, path, secondCommand);
            await ObserveEntityWriteWait(observer, "race-issue-second",
                [observer.ProcessID, firstPid], observations, "pg_advisory_xact_lock", "SELECT");
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
        var retry = await TimedRacePost(secondHost.Client, path, secondCommand);
        var replay = await TimedRacePost(firstHost.Client, path, firstCommand);
        var log = context.ReadPostgresLog()[logStart..];
        var states = await Query(context.Options, async state =>
        {
            var issues = await state.MaterialIssues.Where(row => row.MaterialIssueRequestId == context.Draft.Id)
                .Select(row => new { row.Id, row.IdempotencyKey, row.IssuedByEmployeeId, row.StockPostingBatchId }).ToListAsync();
            var ids = issues.Select(row => row.Id).ToArray();
            var batchIds = issues.Where(row => row.StockPostingBatchId.HasValue).Select(row => row.StockPostingBatchId!.Value).ToArray();
            var lines = await state.MaterialIssueLines.Where(row => ids.Contains(row.MaterialIssueId))
                .Select(row => new { row.Id, row.QuantityBase, row.InventoryLotId, row.InventorySerialId }).ToListAsync();
            var lineIds = lines.Select(row => row.Id).ToArray();
            return new
            {
                Request = await state.MaterialIssueRequests.Where(row => row.Id == context.Draft.Id)
                    .Select(row => new { row.Status, row.Version }).SingleAsync(),
                Issues = issues, Lines = lines,
                Histories = await state.MaterialIssueHistories
                    .Where(row => row.MaterialIssueRequestId == context.Draft.Id && row.Action == "ISSUE")
                    .Select(row => new { row.ActorEmployeeId, row.ActorRoleCode, row.CorrelationId }).ToListAsync(),
                Movements = await state.StockMovements.Where(row => row.StockPostingBatchId.HasValue
                    && batchIds.Contains(row.StockPostingBatchId.Value))
                    .Select(row => new { row.QuantityIn, row.QuantityOut, row.InventorySerialId, row.MovementLeg }).ToListAsync(),
                // Source references describe events; they are not stock-balance dimensions.
                ReferenceGroups = await state.StockMovements.Where(row => row.CompanyId == companyId && row.InventorySerialId == serialId)
                    .GroupBy(row => new { row.ConditionCode, row.OwnershipAccountId, row.CustodyAssignmentId,
                        row.InventoryProvenanceLayerId, row.CustodyCaseLineId, row.InventoryLotId,
                        row.InventorySerialId, row.OriginGoodsReceiptLineId, row.GoodsReceiptLineLotAllocationId,
                        row.QcInspectionLotDispositionId, row.WarehouseConditionLocationId })
                    .Select(group => new { group.Key, Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) }).ToListAsync(),
                Balances = await state.StockMovements.Where(row => row.CompanyId == companyId && row.InventorySerialId == serialId)
                    .GroupBy(row => new { row.ItemId, row.ConditionCode, row.OwnershipAccountId,
                        row.CustodyAssignmentId, row.InventoryProvenanceLayerId, row.InventoryLotId,
                        row.InventorySerialId, row.WarehouseConditionLocationId })
                    .Select(group => new { group.Key, Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) }).ToListAsync(),
                WarehouseQuantity = await state.StockMovements.Where(row => row.CompanyId == companyId
                    && row.InventorySerialId == serialId && row.ConditionCode == "AVAILABLE"
                    && row.CustodyAssignment!.CustodyAccount!.CustodyType == "WAREHOUSE")
                    .SumAsync(row => row.QuantityIn - row.QuantityOut),
                Consumption = await state.FifoCostConsumptions.Where(row => lineIds.Contains(row.MaterialIssueLineId))
                    .Select(row => new { row.FifoInventoryCostLayerId, row.Quantity, row.ConsumedValue }).ToListAsync(),
                FifoBalances = await state.FifoInventoryCostLayers.Where(row => row.CompanyId == companyId && row.ItemId == itemId)
                    .Select(row => new { row.Id, Remaining = row.QuantityReceived - (state.FifoCostConsumptions
                        .Where(use => use.FifoInventoryCostLayerId == row.Id).Sum(use => (decimal?)use.Quantity) ?? 0m) }).ToListAsync()
            };
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "serial-issue-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "serial-issue.json"), JsonSerializer.Serialize(
            new { GateKey = gateKey, Before = before, Observations = observations, ObservationError = observationError,
                First = first, Second = second, Retry = retry, Replay = replay, States = states },
            new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {first?.Body}; second: {second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.Created, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        Assert.True(replay.Status == HttpStatusCode.Created, replay.Body);
        Assert.True(JsonSerializer.Deserialize<MaterialIssueView>(replay.Body)!.Replayed);
        using var conflict = JsonDocument.Parse(second.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.RootElement.GetProperty("Code").GetString());
        using var stale = JsonDocument.Parse(retry.Body);
        Assert.Equal("BUSINESS_RULE_CONFLICT", stale.RootElement.GetProperty("Code").GetString());
        Assert.Equal("FULFILLED", states.Request.Status);
        Assert.Equal(context.Draft.Version + 1, states.Request.Version);
        var issue = Assert.Single(states.Issues);
        Assert.Equal(context.FirstOperatorId, issue.IssuedByEmployeeId);
        Assert.Equal(firstCommand.IdempotencyKey, issue.IdempotencyKey);
        Assert.NotNull(issue.StockPostingBatchId);
        var line = Assert.Single(states.Lines);
        Assert.Equal(serialId, line.InventorySerialId);
        Assert.NotNull(line.InventoryLotId);
        Assert.Equal(1m, line.QuantityBase);
        var history = Assert.Single(states.Histories);
        Assert.Equal(context.FirstOperatorId, history.ActorEmployeeId);
        Assert.Equal(firstCommand.IdempotencyKey, history.CorrelationId);
        Assert.Equal(2, states.Movements.Count);
        Assert.All(states.Movements, row => Assert.Equal(serialId, row.InventorySerialId));
        Assert.Equal(1m, states.Movements.Sum(row => row.QuantityOut));
        Assert.Equal(1m, states.Movements.Sum(row => row.QuantityIn));
        Assert.Equal(0m, states.WarehouseQuantity);
        Assert.All(states.Balances, row => Assert.True(row.Quantity >= 0m, JsonSerializer.Serialize(row)));
        var consumed = Assert.Single(states.Consumption);
        Assert.Equal(before.Oldest.Id, consumed.FifoInventoryCostLayerId);
        Assert.Equal(1m, consumed.Quantity);
        Assert.All(states.FifoBalances, row => Assert.True(row.Remaining >= 0m));
        Assert.Equal(before.Oldest.Remaining - Math.Min(1m, before.Oldest.Remaining),
            states.FifoBalances.Single(row => row.Id == before.Oldest.Id).Remaining);
        return JsonSerializer.Deserialize<MaterialIssueView>(first.Body)!;
    }
}