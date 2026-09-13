using System.Diagnostics;
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
    private sealed record MirApprovalRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection,
        MaterialIssueRequestView Draft, Guid FirstApproverId, Guid SecondApproverId,
        string FirstKey, Func<string> ReadPostgresLog);

    [Fact]
    public Task ProductionAndStoresManagersCannotOverwriteTheSameMirApproval() =>
        RunCompletePurchaseFlow(mirRace: RunMirApprovalRace);

    private static async Task<MaterialIssueRequestView> RunMirApprovalRace(MirApprovalRaceContext context)
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
        Assert.NotEqual(context.FirstApproverId, context.SecondApproverId);
        Assert.Equal("SUBMITTED", context.Draft.Status);
        var firstActor = new TaxWorkflowUser(context.FirstApproverId, subjects[context.FirstApproverId],
            "PRODUCTION_MANAGER", assignments);
        var secondActor = new TaxWorkflowUser(context.SecondApproverId, subjects[context.SecondApproverId],
            "STORES_MANAGER", assignments);
        Assert.All(firstActor.EffectiveRoleAssignments.Concat(secondActor.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-mir-first"), firstActor, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-mir-second"), secondActor, true, true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var gateConnection = new NpgsqlConnection(db.Database.GetConnectionString());
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await gateConnection.OpenAsync();
        await observer.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        // A test-only row lock permits reads and foreign-key KEY SHARE locks but
        // holds the write. No business row is changed by this observer transaction.
        await using var gate = new NpgsqlCommand("""
            SELECT "Id" FROM advance.material_issue_requests
            WHERE "CompanyId"=@company AND "Id"=@id FOR NO KEY UPDATE
            """, gateConnection, gateTransaction);
        gate.Parameters.AddWithValue("company", companyId);
        gate.Parameters.AddWithValue("id", context.Draft.Id);
        Assert.Equal(context.Draft.Id, Assert.IsType<Guid>(await gate.ExecuteScalarAsync()));
        var path = $"/api/v1/stores/material-issue-requests/{context.Draft.Id}/approve";
        var firstCommand = new MaterialIssueTransitionRequest(context.Draft.Version,
            "Consumable custody approved", context.FirstKey);
        var secondCommand = new MaterialIssueTransitionRequest(context.Draft.Version,
            "Stores approval attempted concurrently", context.FirstKey + "-stores");
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstCommand);
            var firstPid = await ObserveMirWriteWait(observer, "race-mir-first",
                [gateConnection.ProcessID], observations);
            secondTask = TimedRacePost(secondHost.Client, path, secondCommand);
            await ObserveMirWriteWait(observer, "race-mir-second",
                [gateConnection.ProcessID, firstPid], observations);
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            await gateTransaction.RollbackAsync();
            if (firstTask is not null) await firstTask;
            if (secondTask is not null) await secondTask;
        }
        var first = firstTask is null ? null : await firstTask;
        var second = secondTask is null ? null : await secondTask;
        var retry = await TimedRacePost(secondHost.Client, path, secondCommand);
        var log = context.ReadPostgresLog()[logStart..];
        var states = await Query(context.Options, async state => new
        {
            Request = await state.MaterialIssueRequests.Where(row => row.Id == context.Draft.Id)
                .Select(row => new { row.Status, row.Version, row.ApprovedByEmployeeId }).SingleAsync(),
            Approvals = await state.MaterialIssueHistories
                .Where(row => row.MaterialIssueRequestId == context.Draft.Id && row.Action == "APPROVE")
                .Select(row => new { row.ActorEmployeeId, row.ActorRoleCode, row.CorrelationId }).ToListAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "mir-approval-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "mir-approval.json"), JsonSerializer.Serialize(
            new { Observations = observations, ObservationError = observationError, First = first,
                Second = second, Retry = retry, States = states }, new JsonSerializerOptions { WriteIndented = true }));
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
        using var stale = JsonDocument.Parse(retry.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", stale.RootElement.GetProperty("Code").GetString());
        Assert.Equal("APPROVED", states.Request.Status);
        Assert.Equal(context.Draft.Version + 1, states.Request.Version);
        Assert.Equal(context.FirstApproverId, states.Request.ApprovedByEmployeeId);
        var approval = Assert.Single(states.Approvals);
        Assert.Equal(context.FirstApproverId, approval.ActorEmployeeId);
        Assert.Equal("PRODUCTION_MANAGER", approval.ActorRoleCode);
        Assert.Equal(context.FirstKey, approval.CorrelationId);
        return JsonSerializer.Deserialize<MaterialIssueRequestView>(first.Body)!;
    }

    private static async Task<int> ObserveMirWriteWait(NpgsqlConnection observer, string name,
        int[] expectedBlockers, List<object> observations)
    {
        var elapsed = Stopwatch.StartNew();
        while (elapsed.Elapsed < TimeSpan.FromSeconds(15))
        {
            await using var command = new NpgsqlCommand("""
                SELECT pid,query,wait_event_type,wait_event,pg_blocking_pids(pid)
                FROM pg_stat_activity
                WHERE datname=current_database() AND application_name=@name
                  AND pg_blocking_pids(pid) && @blockers
                """, observer);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("blockers", expectedBlockers);
            await using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync() && !reader.IsDBNull(2) && !reader.IsDBNull(3)
                    && reader.GetString(2) == "Lock")
                {
                    var statement = reader.GetString(1);
                    Assert.Contains("material_issue_requests", statement, StringComparison.Ordinal);
                    Assert.Contains("UPDATE", statement, StringComparison.Ordinal);
                    var pid = reader.GetInt32(0);
                    observations.Add(new { ApplicationName = name, Pid = pid,
                        Blockers = reader.GetFieldValue<int[]>(4), Statement = statement,
                        WaitEvent = reader.GetString(3), Seconds = elapsed.Elapsed.TotalSeconds });
                    return pid;
                }
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Did not witness {name} blocked at its MIR write.");
    }
}
