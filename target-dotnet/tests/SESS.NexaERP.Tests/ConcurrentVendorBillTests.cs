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
    private sealed record VendorBillRaceContext(
        DbContextOptions<NexaErpDbContext> Options, string RuntimeConnection,
        VendorBillView Draft, Guid ApproverId,
        string FirstKey, Func<string> ReadPostgresLog);

    [Fact]
    public async Task TwoAccountsManagerSessionsCannotAcceptSameVendorBillTwice()
    {
        var observed = false;
        await RunCompletePurchaseFlow(mirRace: RunMirApprovalRace, prRace: RunPrApprovalRace, billRace: context =>
        {
            observed = true;
            return RunVendorBillRace(context);
        });
        Assert.True(observed, "The bill race callback must execute for the third receipt.");
    }

    private static async Task<VendorBillView> RunVendorBillRace(VendorBillRaceContext context)
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
        Assert.Equal("DRAFT", context.Draft.Status);
        var firstActor = new TaxWorkflowUser(context.ApproverId, subjects[context.ApproverId],
            "ACCOUNTS_MANAGER", assignments);
        var secondActor = new TaxWorkflowUser(context.ApproverId, subjects[context.ApproverId],
            "ACCOUNTS_MANAGER", assignments);
        Assert.All(firstActor.EffectiveRoleAssignments.Concat(secondActor.EffectiveRoleAssignments), assignment =>
        {
            Assert.NotEqual(Guid.Empty, assignment.AssignmentId);
            Assert.Equal("FULL", assignment.AssignmentType);
        });
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
        { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var firstHost = await PurchaseFlowHost.StartAsync(Named("race-bill-first"), firstActor, true, true);
        await using var secondHost = await PurchaseFlowHost.StartAsync(Named("race-bill-second"), secondActor, true, true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var gateConnection = new NpgsqlConnection(db.Database.GetConnectionString());
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await gateConnection.OpenAsync();
        await observer.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        // A test-only row lock permits reads and foreign-key KEY SHARE locks but
        // holds the write. No business row is changed by this observer transaction.
        await using var gate = new NpgsqlCommand("""
            SELECT "Id" FROM advance.vendor_bills
            WHERE "CompanyId"=@company AND "Id"=@id FOR NO KEY UPDATE
            """, gateConnection, gateTransaction);
        gate.Parameters.AddWithValue("company", companyId);
        gate.Parameters.AddWithValue("id", context.Draft.Id);
        Assert.Equal(context.Draft.Id, Assert.IsType<Guid>(await gate.ExecuteScalarAsync()));
        var path = $"/api/v1/accounts/vendor-bills/{context.Draft.Id}/accept";
        var firstCommand = new VendorBillDecisionRequest(context.Draft.Version,
            "Three-way match accepted", context.FirstKey);
        var secondCommand = new VendorBillDecisionRequest(context.Draft.Version,
            "Concurrent acceptance attempted", context.FirstKey + "-second-session");
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? firstTask = null;
        Task<RaceHttpResult>? secondTask = null;
        string? observationError = null;
        try
        {
            firstTask = TimedRacePost(firstHost.Client, path, firstCommand);
            var firstPid = await ObserveEntityWriteWait(observer, "race-bill-first",
                [gateConnection.ProcessID], observations, "decide_vendor_bill", "SELECT");
            secondTask = TimedRacePost(secondHost.Client, path, secondCommand);
            await ObserveEntityWriteWait(observer, "race-bill-second",
                [gateConnection.ProcessID, firstPid], observations, "decide_vendor_bill", "SELECT");
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
        var replay = await TimedRacePost(firstHost.Client, path, firstCommand);
        var log = context.ReadPostgresLog()[logStart..];
        var states = await Query(context.Options, async state => new
        {
            Request = await state.VendorBills.Where(row => row.Id == context.Draft.Id)
                .Select(row => new { row.Status, row.Version, row.DecidedByEmployeeId, row.DecisionIdempotencyKey }).SingleAsync(),
            Approvals = await state.VendorBillHistories
                .Where(row => row.VendorBillId == context.Draft.Id && row.Action == "ACCEPTED")
                .Select(row => new { row.ActorEmployeeId, row.ActorRoleCode, row.CorrelationId }).ToListAsync(),
            Allocations = await state.VendorBillCostAllocations
                .Where(row => row.VendorBillLine!.VendorBillId == context.Draft.Id)
                .Select(row => new { row.AllocatedQuantity, row.AcceptedValue, row.FifoInventoryCostLayerId }).ToListAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "vendor-bill-acceptance-postgresql.log"), log);
        await File.WriteAllTextAsync(Path.Combine(evidence, "vendor-bill-acceptance.json"), JsonSerializer.Serialize(
            new { Observations = observations, ObservationError = observationError, First = first,
                Second = second, Retry = retry, Replay = replay, States = states }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null, observationError + $" First: {first?.Body}; second: {second?.Body}");
        Assert.DoesNotContain("40P01", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected", log, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Status == HttpStatusCode.OK, first.Body);
        Assert.True(second.Status == HttpStatusCode.Conflict, second.Body);
        Assert.True(retry.Status == HttpStatusCode.Conflict, retry.Body);
        Assert.True(replay.Status == HttpStatusCode.OK, replay.Body);
        Assert.True(JsonSerializer.Deserialize<VendorBillView>(replay.Body)!.Replayed);
        using var conflict = JsonDocument.Parse(second.Body);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.RootElement.GetProperty("Code").GetString());
        using var stale = JsonDocument.Parse(retry.Body);
        Assert.Equal("BUSINESS_RULE_CONFLICT", stale.RootElement.GetProperty("Code").GetString());
        Assert.Equal("ACCEPTED", states.Request.Status);
        Assert.Equal(context.Draft.Version + 1, states.Request.Version);
        Assert.Equal(context.ApproverId, states.Request.DecidedByEmployeeId);
        var approval = Assert.Single(states.Approvals);
        Assert.Equal(context.ApproverId, approval.ActorEmployeeId);
        Assert.Equal("ACCOUNTS_MANAGER", approval.ActorRoleCode);
        Assert.Equal(context.FirstKey, states.Request.DecisionIdempotencyKey);
        var allocation = Assert.Single(states.Allocations);
        Assert.Equal(context.Draft.Lines.Sum(row => row.BilledQuantity), allocation.AllocatedQuantity);
        Assert.Equal(context.Draft.TotalPayableValue, allocation.AcceptedValue);
        return JsonSerializer.Deserialize<VendorBillView>(first.Body)!;
    }

}
