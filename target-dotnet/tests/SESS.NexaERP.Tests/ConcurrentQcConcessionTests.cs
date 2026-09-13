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
    private sealed record QcConcessionRaceContext(DbContextOptions<NexaErpDbContext> Options,
        string RuntimeConnection, QcInspectionResult Inspection, FinalizeQcInspectionRequest InspectionCommand,
        InventoryConcessionResult Draft, Guid AvailableLocationId, Guid InspectorId, Guid DirectorId,
        Func<string> ReadPostgresLog);

    [Fact]
    public async Task QcCorrectionAndConcessionCannotReleaseSameRejectedSerialTwice()
    {
        var observed = false;
        await RunCompletePurchaseFlow(qcRace: context =>
        {
            observed = true;
            return RunQcConcessionRace(context);
        });
        Assert.True(observed, "The serialized QC/concession race callback must execute.");
    }

    private static async Task<InventoryConcessionResult> RunQcConcessionRace(QcConcessionRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var serialId = Assert.Single(context.Inspection.SerialDispositions).InventorySerialId;
        Assert.Equal(1m, context.Inspection.RejectedQuantity);
        Assert.Equal("DRAFT", context.Draft.Status);
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subjects = await Query(context.Options, db => db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.IsActive)
            .ToDictionaryAsync(row => row.EmployeeId, row => row.Subject));
        var director = new TaxWorkflowUser(context.DirectorId, subjects[context.DirectorId],
            "TECHNICAL_DIRECTOR", assignments);
        var inspector = new TaxWorkflowUser(context.InspectorId, subjects[context.InspectorId],
            "QC_MANAGER", assignments);
        Assert.NotEqual(context.DirectorId, context.InspectorId);
        Assert.NotEmpty(director.EffectiveRoleAssignments);
        Assert.NotEmpty(inspector.EffectiveRoleAssignments);
        Assert.All(director.EffectiveRoleAssignments.Concat(inspector.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
            { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-qc-concession"), director, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-qc-correction"), inspector, true, true);
        var approvalPath = $"/api/v1/qc/concessions/{context.Draft.ConcessionNumber}/approve";
        var approval = new ApproveInventoryConcessionRequest(context.Draft.Version,
            context.AvailableLocationId, "Direct technical acceptance");
        const string approvalKey = "MD-concession-approve";
        var correctionPath = $"/api/v1/qc/inspections/{context.Inspection.InspectionNumber}/corrections";
        var command = context.InspectionCommand;
        var correction = new CorrectQcInspectionRequest(context.Inspection.RevisionId,
            "Remeasurement would accept the same rejected serial.",
            command.InspectionStartedAt, 1m, 0m, 0m, context.AvailableLocationId,
            command.ParameterResults.Select(row => row with { ObservedNumericValue = 5m, Result = "PASS" }).ToArray(),
            command.SerialDispositions.Select(row => row with { Disposition = "ACCEPTED", Reason = null }).ToArray());
        const string correctionKey = "MD-qc-concession-race-correction";
        var ownershipId = await Query(context.Options, db => db.StockMovements
            .Where(row => row.StockPostingBatchId == context.Inspection.StockPostingBatchId)
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
            firstTask = TimedQcPost(firstHost.Client, approvalPath, approval, approvalKey);
            var firstPid = await ObserveEntityWriteWait(observer, "race-qc-concession",
                [observer.ProcessID], observations, "post_stores_stock_batch", "SELECT");
            secondTask = TimedQcPost(secondHost.Client, correctionPath, correction, correctionKey);
            await ObserveEntityWriteWait(observer, "race-qc-correction",
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
        var retry = await TimedQcPost(secondHost.Client, correctionPath, correction, correctionKey);
        var replay = await TimedQcPost(firstHost.Client, approvalPath, approval, approvalKey);
        var log = context.ReadPostgresLog()[logStart..];
        var state = await Query(context.Options, async stateDb => new
        {
            RevisionCount = await stateDb.QcInspectionRevisions.CountAsync(row =>
                row.QcInspectionId == context.Inspection.InspectionId),
            ReversalCount = await stateDb.StockPostingBatches.CountAsync(row =>
                row.ReversesPostingBatchId == context.Inspection.StockPostingBatchId),
            Concession = await stateDb.InventoryConcessions.AsNoTracking()
                .Where(row => row.Id == context.Draft.Id)
                .Select(row => new { row.Status, row.Version, row.DecidedByEmployeeId }).SingleAsync(),
            ConcessionBatches = await stateDb.StockPostingBatches.CountAsync(row =>
                row.InventoryConcessionId == context.Draft.Id && row.PostingKind == "CONCESSION_ACCEPTANCE"),
            ConcessionMovements = await stateDb.StockMovements.CountAsync(row =>
                row.StockPostingBatch!.InventoryConcessionId == context.Draft.Id),
            Balances = await stateDb.StockMovements.AsNoTracking().Where(row =>
                row.CompanyId == companyId && row.InventorySerialId == serialId)
                .GroupBy(row => new { row.ItemId, row.WarehouseConditionLocationId, row.ConditionCode,
                    row.OwnershipAccountId, row.CustodyAssignmentId, row.InventoryProvenanceLayerId,
                    row.InventoryLotId, row.InventorySerialId })
                .Select(group => new { group.Key, Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) })
                .ToArrayAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-concession-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "qc-concession.json"),
            JsonSerializer.Serialize(new { Original = context.Inspection, Correction = correction,
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
        Assert.Contains("QC stock or its decision changed", second.Body, StringComparison.Ordinal);
        Assert.Contains("source stock is no longer available", retry.Body, StringComparison.Ordinal);
        Assert.True(replay.Status == HttpStatusCode.OK, replay.Body);
        Assert.True(JsonSerializer.Deserialize<InventoryConcessionResult>(replay.Body)!.Replayed);
        Assert.Equal(1, state.RevisionCount);
        Assert.Equal(0, state.ReversalCount);
        Assert.Equal("APPROVED", state.Concession.Status);
        Assert.Equal(context.Draft.Version + 1u, state.Concession.Version);
        Assert.Equal(context.DirectorId, state.Concession.DecidedByEmployeeId);
        Assert.Equal(1, state.ConcessionBatches);
        Assert.Equal(2, state.ConcessionMovements);
        Assert.All(state.Balances, row => Assert.InRange(row.Quantity, 0m, 1m));
        Assert.Equal(1m, state.Balances.Sum(row => row.Quantity));
        Assert.Equal(1m, state.Balances.Where(row => row.Key.ConditionCode == "AVAILABLE").Sum(row => row.Quantity));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "QC_HOLD").Sum(row => row.Quantity));
        Assert.Equal(0m, state.Balances.Where(row => row.Key.ConditionCode == "PENDING_RETURNABLE_DC").Sum(row => row.Quantity));
        return JsonSerializer.Deserialize<InventoryConcessionResult>(first.Body)!;
    }

    private static async Task<string> QcPostingFunctionMetadata(DbContextOptions<NexaErpDbContext> options)
    {
        var value = await Query(options, db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object('oid',p.oid::bigint,'owner',r.rolname,
              'securityDefiner',p.prosecdef,'configuration',to_jsonb(p.proconfig),
              'acl',coalesce(array_to_string(p.proacl,','),''),
              'runtimeExecute',has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE'),
              'runtimeTableInsert',has_table_privilege('nexa_erp_runtime','advance.stock_movements','INSERT')
            )::text AS "Value"
            FROM pg_proc p JOIN pg_roles r ON r.oid=p.proowner
            WHERE p.oid='advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)'::regprocedure
            """).SingleAsync());
        using var document = JsonDocument.Parse(value);
        Assert.Equal("nexa_erp_owner", document.RootElement.GetProperty("owner").GetString());
        Assert.True(document.RootElement.GetProperty("securityDefiner").GetBoolean());
        Assert.True(document.RootElement.GetProperty("runtimeExecute").GetBoolean());
        Assert.False(document.RootElement.GetProperty("runtimeTableInsert").GetBoolean());
        return value;
    }
}
