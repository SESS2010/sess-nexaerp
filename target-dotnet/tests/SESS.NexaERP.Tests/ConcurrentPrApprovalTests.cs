using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private sealed record PrApprovalRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection,
        PurchaseRequisitionDetail Draft, Guid FirstApproverId, Guid SecondApproverId,
        string FirstKey, Func<string> ReadPostgresLog);

#if CONCURRENCY_WITNESS
    [Fact]
    public async Task PrApprovalSessionsCannotDuplicateOrSkipTheNamedStep()
    {
        var observed = false;
        await RunCompletePurchaseFlow(mirRace: RunMirApprovalRace, prRace: context =>
        {
            observed = true;
            return RunPrApprovalRace(context);
        });
        Assert.True(observed, "The PR race callback must execute in the TD approval band.");
    }

#endif
    private static async Task<PurchaseRequisitionDetail> RunPrApprovalRace(PrApprovalRaceContext context)
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
        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, context.Draft.Status);
        var firstActor = new TaxWorkflowUser(context.FirstApproverId, subjects[context.FirstApproverId],
            "ACCOUNTS_MANAGER", assignments);
        var secondActor = new TaxWorkflowUser(context.FirstApproverId, subjects[context.FirstApproverId],
            "ACCOUNTS_MANAGER", assignments);
        var futureActor = new TaxWorkflowUser(context.SecondApproverId, subjects[context.SecondApproverId],
            "TECHNICAL_DIRECTOR", assignments);
        Assert.All(firstActor.EffectiveRoleAssignments.Concat(secondActor.EffectiveRoleAssignments).Concat(futureActor.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-pr-first"), firstActor, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-pr-second"), secondActor, true, true);
        await using var futureHost = await PurchaseFlowHost.StartAsync(Named("race-pr-future"), futureActor, true, true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var gateConnection = new NpgsqlConnection(db.Database.GetConnectionString());
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await gateConnection.OpenAsync();
        await observer.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        // The approval-history INSERT trigger locks its PR parent FOR UPDATE.
        // A test-only row lock permits reads and foreign-key KEY SHARE locks but
        // holds the write. No business row is changed by this observer transaction.
        await using var gate = new NpgsqlCommand("""
            SELECT "Id" FROM advance.purchase_requisitions
            WHERE "CompanyId"=@company AND "Id"=@id FOR NO KEY UPDATE
            """, gateConnection, gateTransaction);
        gate.Parameters.AddWithValue("company", companyId);
        gate.Parameters.AddWithValue("id", context.Draft.Id);
        Assert.Equal(context.Draft.Id, Assert.IsType<Guid>(await gate.ExecuteScalarAsync()));
        var path = $"/api/v1/purchase/requisitions/{context.Draft.PrNumber}/approve";
        var firstCommand = new PurchaseRequisitionActionRequest("Level 1 approved",
            context.Draft.Version, context.FirstKey);
        var secondCommand = new PurchaseRequisitionActionRequest("Duplicate first-step approval",
            context.Draft.Version, context.FirstKey + "-second-session");
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        RaceHttpResult? future = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstCommand);
            var firstPid = await ObserveEntityWriteWait(observer, "race-pr-first",
                [gateConnection.ProcessID], observations, "purchase_requisition_approval_history", "INSERT");
            future = await TimedRacePost(futureHost.Client, path,
                new PurchaseRequisitionActionRequest("Future step attempted early",
                    context.Draft.Version, context.FirstKey + "-future"));
            secondTask = TimedRacePost(secondHost.Client, path, secondCommand);
            await ObserveEntityWriteWait(observer, "race-pr-second",
                [gateConnection.ProcessID, firstPid], observations, "purchase_requisition_approval_history", "INSERT");
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
            Request = await state.PurchaseRequisitions.Where(row => row.Id == context.Draft.Id)
                .Select(row => new { row.Status, row.Version, row.CompletedApprovalStepCount }).SingleAsync(),
            Approvals = await state.PurchaseRequisitionApprovalHistories
                .Where(row => row.PurchaseRequisitionId == context.Draft.Id && row.Action == "Approve")
                .Select(row => new { row.ResolvedEmployeeId, row.ResolvedRoleCode, row.StepNumber, row.CorrelationId }).ToListAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "pr-approval-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "pr-approval.json"), JsonSerializer.Serialize(
            new { Observations = observations, ObservationError = observationError, First = first,
                Second = second, Future = future, Retry = retry, States = states }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {first?.Body}; second: {second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(future);
        Assert.True(future.Status == HttpStatusCode.Forbidden, future.Body);
        Assert.Contains("awaiting SESS-14 (ACCOUNTS_MANAGER)", future.Body, StringComparison.Ordinal);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.OK, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        using var conflict = JsonDocument.Parse(second.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.RootElement.GetProperty("Code").GetString());
        Assert.Contains("Stale record version", retry.Body, StringComparison.Ordinal);
        using var stale = JsonDocument.Parse(retry.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", stale.RootElement.GetProperty("Code").GetString());
        Assert.Equal(PurchaseRequisitionStatuses.PendingApproval, states.Request.Status);
        Assert.Equal(context.Draft.Version + 1, states.Request.Version);
        Assert.Equal(1, states.Request.CompletedApprovalStepCount);
        var approval = Assert.Single(states.Approvals);
        Assert.Equal(context.FirstApproverId, approval.ResolvedEmployeeId);
        Assert.Equal("ACCOUNTS_MANAGER", approval.ResolvedRoleCode);
        Assert.Equal(1, approval.StepNumber);
        Assert.Equal(context.FirstKey, approval.CorrelationId);
        return JsonSerializer.Deserialize<PurchaseRequisitionDetail>(first.Body)!;
    }

}
