using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record ReturnFitmentRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection,
        Guid CompanyId, Guid JobId, Guid IssueLineId, Guid ReturnId, AcceptMaterialReturn Accept,
        Guid StoresId, Guid ProductionId, Func<string> ReadPostgresLog,
        decimal FitQuantity = .95m, string EvidenceName = "return-fitment", Guid? SerialId = null);

#if CONCURRENCY_WITNESS
    [Fact]
    public Task ReturnAcceptanceAndFitmentOverlapWithoutDeadlockOrLostCustody() =>
        RunCompletePurchaseFlow(RunReturnFitmentRace);

#endif
#if CONCURRENCY_WITNESS
    [Fact]
    public Task SameSerialReturnAndJobFitmentOverlapWithoutDeadlockOrDoubleConsumption() =>
        RunCompletePurchaseFlow(RunReturnFitmentRace, serializedRace: true);

#endif
    private static async Task<MaterialReturnView> RunReturnFitmentRace(ReturnFitmentRaceContext context)
    {
        var assignments = await Query(context.Options, async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == context.CompanyId && row.EffectiveTo == null).ToListAsync())
            .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId, row.Role!.Code),
                row => new EffectiveRoleAssignment(row.Id, row.Role!.Code, row.AssignmentType)));
        var subjects = await Query(context.Options, async db => await db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == context.CompanyId && row.IsActive)
            .ToDictionaryAsync(row => row.EmployeeId, row => row.Subject));
        // Each concurrent host has a distinct actor, DbContext scope and connection.
        // Neither actor is mutated while requests are in flight.
        var stores = new TaxWorkflowUser(context.StoresId, subjects[context.StoresId], "STORES_EXECUTIVE", assignments);
        var production = new TaxWorkflowUser(context.ProductionId, subjects[context.ProductionId], "PRODUCTION_MANAGER", assignments);
        string NamedConnection(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var returnHost = await PurchaseFlowHost.StartAsync(NamedConnection("race-return"), stores,
            useRealPagePermissions: true, useRealOperationalScopes: true);
        await using var fitmentHost = await PurchaseFlowHost.StartAsync(NamedConnection("race-fitment"), production,
            useRealPagePermissions: true, useRealOperationalScopes: true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await observer.OpenAsync();
        var gateKey = $"STORES:IDEMP:{context.CompanyId}:POST:{context.Accept.IdempotencyKey}";
        await using var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))", observer);
        gate.Parameters.AddWithValue("key", gateKey);
        await gate.ExecuteNonQueryAsync();
        var logStart = context.ReadPostgresLog().Length;
        var observations = new List<object>();
        var fitRequest = new ConfirmComponentFitmentRequest(context.JobId, context.IssueLineId, context.FitQuantity,
            DateTimeOffset.UtcNow, "Concurrent fitment competes with a physical return", null,
            "race-fitment-vs-return");
        Task<RaceHttpResult>? returning = null;
        Task<RaceHttpResult>? fitting = null;
        try
        {
            returning = TimedRacePost(returnHost.Client,
                $"/api/v1/stores/material-returns/{context.ReturnId}/accept", context.Accept);
            var returnPid = await ObserveBlockedBackend(observer, "race-return", observer.ProcessID,
                "post_material_return_acceptance", observations);
            fitting = TimedRacePost(fitmentHost.Client, "/api/v1/production/component-fitments", fitRequest);
            await ObserveBlockedBackend(observer, "race-fitment", returnPid,
                "confirm_component_fitment", observations);
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtextextended(@key,0))", observer);
            release.Parameters.AddWithValue("key", gateKey);
            await release.ExecuteNonQueryAsync();
            // Drain requests even if observation failed, before disposing either host.
            if (returning is not null) await returning;
            if (fitting is not null) await fitting;
        }
        var returned = await returning!;
        var fitted = await fitting!;
        var retry = await TimedRacePost(fitmentHost.Client, "/api/v1/production/component-fitments", fitRequest);
        var log = context.ReadPostgresLog()[logStart..];
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, context.EvidenceName + "-postgresql.log"), log);
        var states = await Query(context.Options, async state => new
        {
            Return = await state.MaterialReturns.Where(row => row.Id == context.ReturnId)
                .Select(row => new { row.Status, row.StockPostingBatchId, row.Version }).SingleAsync(),
            Fitments = await state.ComponentFitments.CountAsync(row => row.MaterialIssueLineId == context.IssueLineId),
            ReturnBatches = await state.StockPostingBatches.CountAsync(row => row.MaterialReturnId == context.ReturnId),
            ReturnMovements = await state.StockMovements.CountAsync(row => row.MaterialReturnLineId != null &&
                row.MaterialIssueLineId == context.IssueLineId)
        });
        await File.WriteAllTextAsync(Path.Combine(evidence, context.EvidenceName + ".json"),
            JsonSerializer.Serialize(new { Observations = observations, Return = returned, Fitment = fitted, Retry = retry, States = states },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.True(returned.Status == HttpStatusCode.OK, returned.Body);
        Assert.True(fitted.Status == HttpStatusCode.Conflict, fitted.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        Assert.Contains("exceeds engineer custody available", retry.Body, StringComparison.Ordinal);
        Assert.Equal("ACCEPTED", states.Return.Status);
        Assert.Equal(0, states.Fitments);
        Assert.Equal(1, states.ReturnBatches);
        Assert.Equal(2, states.ReturnMovements);
        if (context.SerialId.HasValue)
        {
            var serials = await Query(context.Options, async state => await state.StockMovements
                .Where(row => row.MaterialIssueLineId == context.IssueLineId && row.MaterialReturnLineId != null)
                .Select(row => row.InventorySerialId).ToArrayAsync());
            Assert.Equal(2, serials.Length);
            Assert.All(serials, serial => Assert.Equal(context.SerialId, serial));
        }
        return JsonSerializer.Deserialize<MaterialReturnView>(returned.Body)!;
    }

    private sealed record RaceHttpResult(HttpStatusCode Status, double Seconds, string Body);
    private static async Task<RaceHttpResult> TimedRacePost<T>(HttpClient client, string path, T body)
    {
        var elapsed = Stopwatch.StartNew();
        using var response = await client.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        return new(response.StatusCode, elapsed.Elapsed.TotalSeconds, content);
    }

    private static async Task<int> ObserveBlockedBackend(NpgsqlConnection observer, string applicationName,
        int blocker, string expectedStatement, List<object> observations)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(15))
        {
            await using var command = new NpgsqlCommand("""
                SELECT pid,query,wait_event_type,wait_event
                FROM pg_stat_activity
                WHERE datname=current_database() AND application_name=@name
                  AND @blocker=ANY(pg_blocking_pids(pid))
                """, observer);
            command.Parameters.AddWithValue("name", applicationName);
            command.Parameters.AddWithValue("blocker", blocker);
            await using (var reader = await command.ExecuteReaderAsync())
            {
                // Lock-manager and backend wait-event samples can briefly disagree.
                // Require a complete lock sample before accepting the observation.
                if (await reader.ReadAsync() && !reader.IsDBNull(2) && !reader.IsDBNull(3) &&
                    reader.GetString(2) == "Lock")
                {
                    var statement = reader.GetString(1);
                    Assert.Contains(expectedStatement, statement, StringComparison.Ordinal);
                    Assert.Equal("Lock", reader.GetString(2));
                    var pid = reader.GetInt32(0);
                    observations.Add(new { ApplicationName = applicationName, Pid = pid, Blocker = blocker,
                        Statement = statement, WaitEvent = reader.GetString(3), Seconds = elapsed.Elapsed.TotalSeconds });
                    return pid;
                }
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Did not witness {applicationName} blocked by backend {blocker}.");
    }
}
