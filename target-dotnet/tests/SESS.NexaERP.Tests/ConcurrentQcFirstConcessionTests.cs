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
    [Fact]
    public async Task QcCorrectionFirstPreventsApprovalOfTheSupersededConcession()
    {
        var observed = false;
        await RunCompletePurchaseFlow(qcCorrection: context =>
        {
            observed = true;
            return RunQcFirstConcessionRace(context);
        }, concessionHistoryWitness: true);
        Assert.True(observed, "The QC-first race callback must execute.");
    }

    private static async Task<QcInspectionResult> RunQcFirstConcessionRace(QcCorrectionContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var original = context.Original;
        var serialId = Assert.Single(original.SerialDispositions).InventorySerialId;
        var failed = Assert.Single(original.ParameterResults);
        Assert.Equal(1m, original.RejectedQuantity);
        var directorId = await Query(context.Options, db => db.Employees
            .Where(row => row.EmployeeCode == "SESS-01").Select(row => row.Id).SingleAsync());
        var available = await Query(context.Options, db => db.WarehouseConditionLocations
            .Where(row => row.CompanyId == companyId && row.ConditionCode == "AVAILABLE" && row.IsActive)
            .OrderBy(row => row.Id).Select(row => row.Id).FirstAsync());
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subjects = await Query(context.Options, db => db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.IsActive)
            .ToDictionaryAsync(row => row.EmployeeId, row => row.Subject));
        var inspector = new TaxWorkflowUser(context.InspectorId, subjects[context.InspectorId], "QC_MANAGER", assignments);
        var director = new TaxWorkflowUser(directorId, subjects[directorId], "TECHNICAL_DIRECTOR", assignments);
        Assert.NotEqual(context.InspectorId, directorId);
        Assert.NotEmpty(inspector.EffectiveRoleAssignments);
        Assert.NotEmpty(director.EffectiveRoleAssignments);
        Assert.All(inspector.EffectiveRoleAssignments.Concat(director.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
            { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var qcHost = await PurchaseFlowHost.StartAsync(Named("race-qc-first"), inspector, true, true);
        await using var tdHost = await PurchaseFlowHost.StartAsync(Named("race-concession-second"), director, true, true);
        var draft = await Post<InventoryConcessionResult>(qcHost.Client, "/api/v1/qc/concessions",
            new CreateInventoryConcessionRequest(original.QcInspectionLotDispositionId, failed.Id, 1m,
                failed.ParameterCode, failed.MeasuredValue, "Original rejection submitted for direct TD review.",
                "Controlled internal test fixture", [serialId]), "MD-qc-first-old-concession-create");
        Assert.Equal("DRAFT", draft.Status);
        await AssertActiveConcessionSerialGuard(context.RuntimeConnection, draft.Id);
        var command = context.OriginalCommand;
        var correction = new CorrectQcInspectionRequest(original.RevisionId,
            "Correct recorded measurement from 12 to 11; rejection remains required.",
            command.InspectionStartedAt, command.AcceptedQuantity, command.RejectedQuantity,
            command.DiscrepancyPendingQuantity, command.AcceptedConditionLocationId,
            command.ParameterResults.Select(row => row with { ObservedNumericValue = 11m }).ToArray(),
            command.SerialDispositions);
        var correctionPath = $"/api/v1/qc/inspections/{original.InspectionNumber}/corrections";
        const string correctionKey = "MD-qc-first-correction";
        var approvalPath = $"/api/v1/qc/concessions/{draft.ConcessionNumber}/approve";
        var approval = new ApproveInventoryConcessionRequest(draft.Version, available, "Direct technical acceptance");
        const string approvalKey = "MD-qc-first-old-concession-approve";
        var ownershipId = await Query(context.Options, db => db.StockMovements
            .Where(row => row.StockPostingBatchId == original.StockPostingBatchId)
            .Select(row => row.OwnershipAccountId).Distinct().SingleAsync());
        await using var db = new NexaErpDbContext(context.Options);
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await observer.OpenAsync();
        var gateKey = $"STORES:POST:10:OWN:{companyId}:{ownershipId}";
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))", observer))
        {
            gate.Parameters.AddWithValue("key", gateKey);
            await gate.ExecuteNonQueryAsync();
        }
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedQcPost(qcHost.Client, correctionPath, correction, correctionKey);
            var firstPid = await ObserveEntityWriteWait(observer, "race-qc-first",
                [observer.ProcessID], observations, "post_stores_stock_batch", "SELECT");
            secondTask = TimedQcPost(tdHost.Client, approvalPath, approval, approvalKey);
            await ObserveEntityWriteWait(observer, "race-concession-second",
                [observer.ProcessID, firstPid], observations, "post_stores_stock_batch", "SELECT");
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
        var retry = await TimedQcPost(tdHost.Client, approvalPath, approval, approvalKey);
        var replay = await TimedQcPost(qcHost.Client, correctionPath, correction, correctionKey);
        var log = context.ReadPostgresLog()[logStart..];
        var state = await Query(context.Options, async stateDb => new
        {
            RevisionCount = await stateDb.QcInspectionRevisions.CountAsync(row => row.QcInspectionId == original.InspectionId),
            ReversalCount = await stateDb.StockPostingBatches.CountAsync(row => row.ReversesPostingBatchId == original.StockPostingBatchId),
            Concession = await stateDb.InventoryConcessions.AsNoTracking().Where(row => row.Id == draft.Id)
                .Select(row => new { row.Status, row.Version, row.DecidedByEmployeeId }).SingleAsync(),
            ConcessionBatches = await stateDb.StockPostingBatches.CountAsync(row => row.InventoryConcessionId == draft.Id),
            Balances = await stateDb.StockMovements.AsNoTracking().Where(row =>
                row.CompanyId == companyId && row.InventorySerialId == serialId)
                .GroupBy(row => new { row.ItemId, row.WarehouseConditionLocationId, row.ConditionCode,
                    row.OwnershipAccountId, row.CustodyAssignmentId, row.InventoryProvenanceLayerId,
                    row.InventoryLotId, row.InventorySerialId })
                .Select(group => new { group.Key, Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) }).ToArrayAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-first-concession-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-first-concession.json"),
            JsonSerializer.Serialize(new { Original = original, OriginalDraft = draft, Correction = correction,
                Observations = observations, ObservationError = observationError,
                First = first, Second = second, Retry = retry, Replay = replay, State = state },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First:{first?.Body}; second:{second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.OK, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        Assert.Contains("source stock is no longer available", retry.Body, StringComparison.Ordinal);
        Assert.True(replay.Status == HttpStatusCode.OK, replay.Body);
        var corrected = JsonSerializer.Deserialize<QcInspectionResult>(first.Body)!;
        var replayed = JsonSerializer.Deserialize<QcInspectionResult>(replay.Body)!;
        Assert.True(replayed.Replayed);
        Assert.Equal(corrected.RevisionId, replayed.RevisionId);
        Assert.Equal(2, corrected.RevisionNumber);
        Assert.Equal(original.InspectionId, corrected.InspectionId);
        Assert.NotEqual(original.RevisionId, corrected.RevisionId);
        Assert.Equal(1m, corrected.RejectedQuantity);
        Assert.Equal(0m, corrected.AcceptedQuantity);
        Assert.Equal(2, state.RevisionCount);
        Assert.Equal(1, state.ReversalCount);
        Assert.Equal("DRAFT", state.Concession.Status);
        Assert.Equal(draft.Version, state.Concession.Version);
        Assert.Null(state.Concession.DecidedByEmployeeId);
        Assert.Equal(0, state.ConcessionBatches);
        Assert.All(state.Balances, row => Assert.InRange(row.Quantity, 0m, 1m));
        Assert.Equal(1m, state.Balances.Sum(row => row.Quantity));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "AVAILABLE").Sum(row => row.Quantity));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "QC_HOLD").Sum(row => row.Quantity));
        Assert.Equal(1m, state.Balances.Where(row => row.Key.ConditionCode == "PENDING_RETURNABLE_DC").Sum(row => row.Quantity));
        // The superseded draft is closed through its governed operation. The ordinary
        // fixture then creates and approves a fresh concession against corrected QC.
        var rejected = await Post<InventoryConcessionResult>(tdHost.Client,
            $"/api/v1/qc/concessions/{draft.ConcessionNumber}/reject",
            new RejectInventoryConcessionRequest(draft.Version, "QC was corrected; a new concession must reference the corrected revision."));
        Assert.Equal("REJECTED", rejected.Status);
        Assert.Equal(draft.Version + 1u, rejected.Version);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-first-concession-rejected-draft.json"),
            JsonSerializer.Serialize(rejected, new JsonSerializerOptions { WriteIndented = true }));
        return corrected;
    }
}
