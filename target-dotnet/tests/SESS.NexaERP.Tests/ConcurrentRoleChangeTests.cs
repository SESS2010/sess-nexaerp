using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Employees;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task AssignmentChangeDuringIssuePreservesAuditAndRefusesFormerRoleIssue()
    {
        var observed = false;
        await RunCompletePurchaseFlow(issueRace: context =>
        {
            observed = true;
            return RunRoleChangeDuringIssue(context);
        });
        Assert.True(observed, "The mid-command assignment-change callback must execute.");
    }

    private static async Task<MaterialIssueView> RunRoleChangeDuringIssue(SerialIssueRaceContext context)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var employeeId = context.SecondOperatorId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        async Task<Dictionary<string,EffectiveRoleAssignment>> CurrentAssignments() =>
            await Query(context.Options, async db => (await db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
                .Where(row => row.CompanyId == companyId && row.EffectiveFrom <= today
                    && (!row.EffectiveTo.HasValue || row.EffectiveTo >= today)).ToListAsync())
                .ToDictionary(row => TaxWorkflowUser.AssignmentKey(row.EmployeeId,row.Role!.Code),
                    row => new EffectiveRoleAssignment(row.Id,row.Role!.Code,row.AssignmentType)));
        var assignments = await CurrentAssignments();
        var subjects = await Query(context.Options, db => db.EmployeeIdentityMappings
            .Where(row => row.CompanyId == companyId && row.IsActive)
            .ToDictionaryAsync(row => row.EmployeeId,row => row.Subject));
        var directorId = await Query(context.Options, db => db.Employees
            .Where(row => row.EmployeeCode == "SESS-01").Select(row => row.Id).SingleAsync());
        var employeeCode = await Query(context.Options, db => db.Employees
            .Where(row => row.Id == employeeId).Select(row => row.EmployeeCode).SingleAsync());
        Assert.Equal("SESS-16", employeeCode);
        var previous = await Query(context.Options, db => db.EmployeeRoleAssignments.AsNoTracking().Include(row => row.Role)
            .Where(row => row.CompanyId == companyId && row.EmployeeId == employeeId
                && row.Role!.Code == "STORES_ASSISTANT" && row.EffectiveTo == null).SingleAsync());
        Assert.Equal("FULL", previous.AssignmentType);
        Assert.True(previous.EffectiveFrom < today);
        var operatorActor = new TaxWorkflowUser(employeeId,subjects[employeeId],"STORES_ASSISTANT",assignments);
        var director = new TaxWorkflowUser(directorId,subjects[directorId],"TECHNICAL_DIRECTOR",assignments);
        Assert.Equal(previous.Id,Assert.Single(operatorActor.EffectiveRoleAssignments).AssignmentId);
        string Named(string name) => new NpgsqlConnectionStringBuilder(context.RuntimeConnection)
            { ApplicationName = name, Pooling = false }.ConnectionString;
        await using var issueHost = await PurchaseFlowHost.StartAsync(Named("race-role-issue"),operatorActor,true,true);
        await using var directorHost = await PurchaseFlowHost.StartAsync(Named("race-role-transfer"),director,true,true);
        await using var db = new NexaErpDbContext(context.Options);
        await using var observer = new NpgsqlConnection(db.Database.GetConnectionString());
        await observer.OpenAsync();
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}"
            : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var gateKey = $"NUMBER:SESS_PVT_LTD:{year}:MI";
        await using (var gate = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended(@key,0))",observer))
        {
            gate.Parameters.AddWithValue("key",gateKey);
            await gate.ExecuteNonQueryAsync();
        }
        var path = $"/api/v1/stores/material-issues/from-request/{context.Draft.Id}";
        var observations = new List<object>();
        var logStart = context.ReadPostgresLog().Length;
        Task<RaceHttpResult>? issueTask = null;
        Task<RaceHttpResult>? transferTask = null;
        RaceHttpResult? transfer = null;
        string? observationError = null;
        var transferredWhileIssueBlocked = false;
        try
        {
            issueTask = TimedRacePost(issueHost.Client,path,context.Command);
            await ObserveEntityWriteWait(observer,"race-role-issue",[observer.ProcessID],
                observations,"pg_advisory_xact_lock","SELECT");
            transferTask = TimedRacePost(directorHost.Client,$"/api/v1/employees/{employeeCode}/roles/transfer",
                new TransferEmployeeRoleRequest(previous.Id,"SERVICE_ENGINEER","FULL",today,false,
                    "Controlled assignment authority change during an in-flight issue.",previous.Version));
            transfer = await transferTask.WaitAsync(TimeSpan.FromSeconds(30));
            transferredWhileIssueBlocked = !issueTask.IsCompleted;
        }
        catch (Exception error) { observationError = error.ToString(); }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(hashtextextended(@key,0))",observer);
            release.Parameters.AddWithValue("key",gateKey);
            await release.ExecuteNonQueryAsync();
            if (issueTask is not null) await issueTask;
            if (transferTask is not null) transfer = await transferTask;
        }
        var first = issueTask is null ? null : await issueTask;
        // A fresh request must resolve the actual current assignment, not reuse
        // the deliberately frozen ICurrentUser of the already-running request.
        var freshAssignments = await CurrentAssignments();
        var freshActor = new TaxWorkflowUser(employeeId,subjects[employeeId],"SERVICE_ENGINEER",freshAssignments);
        await using var freshHost = await PurchaseFlowHost.StartAsync(Named("race-role-fresh"),freshActor,true,true);
        var fresh = await TimedRacePost(freshHost.Client,path,
            context.Command with { IdempotencyKey = context.Command.IdempotencyKey + "-after-transfer" });
        var log = context.ReadPostgresLog()[logStart..];
        var state = await Query(context.Options, async stateDb => new
        {
            Assignments = await stateDb.EmployeeRoleAssignments.AsNoTracking()
                .Where(row => row.CompanyId == companyId && row.EmployeeId == employeeId
                    && (row.RoleId == previous.RoleId || row.Role!.Code == "SERVICE_ENGINEER"))
                .OrderBy(row => row.EffectiveFrom)
                .Select(row => new { row.Id,row.AssignmentType,row.EffectiveFrom,row.EffectiveTo,row.Version }).ToListAsync(),
            Issues = await stateDb.MaterialIssues.AsNoTracking().Where(row => row.MaterialIssueRequestId == context.Draft.Id)
                .Select(row => new { row.Id,row.IssuedByEmployeeId,row.ResolvedRoleAssignmentId,row.ResolvedRoleAssignmentType,
                    row.ActorRoleCode,row.StockPostingBatchId }).ToListAsync(),
            QuantityIssued = await stateDb.MaterialIssueLines.Where(row => row.MaterialIssueRequestLineId == context.Draft.Lines.Single().Id)
                .SumAsync(row => (decimal?)row.QuantityBase) ?? 0m,
            RoleEvents = await stateDb.EmployeeRoleAssignmentEvents.Where(row => row.CompanyId == companyId
                && row.EmployeeId == employeeId && row.Operation == "TRANSFER" && row.NewEffectiveFrom == today
                && row.FromRoleCode == "STORES_ASSISTANT" && row.ToRoleCode == "SERVICE_ENGINEER")
                .Select(row => new { row.ActorEmployeeId,row.AssignmentId,row.FromAssignmentType,row.ToAssignmentType }).ToListAsync(),
            Fifo = await stateDb.FifoCostConsumptions.Where(row =>
                row.MaterialIssueLine!.MaterialIssueRequestLineId == context.Draft.Lines.Single().Id)
                .Select(row => new { row.Quantity,row.UnitCost,row.ConsumedValue }).ToListAsync(),
            Balances = await stateDb.StockMovements.AsNoTracking()
                .Where(row => row.CompanyId == companyId && row.InventorySerialId == context.Command.Scans.Single().InventorySerialId)
                .GroupBy(row => new { row.ItemId,row.WarehouseConditionLocationId,row.ConditionCode,
                    row.OwnershipAccountId,row.CustodyAssignmentId,row.InventoryProvenanceLayerId,row.InventoryLotId,row.InventorySerialId })
                .Select(group => new { group.Key,Quantity = group.Sum(row => row.QuantityIn - row.QuantityOut) }).ToListAsync(),
            Histories = await stateDb.MaterialIssueHistories.Where(row => row.MaterialIssueRequestId == context.Draft.Id && row.Action == "ISSUE")
                .Select(row => new { row.ActorEmployeeId,row.ActorRoleCode,row.ResolvedRoleAssignmentId }).ToListAsync()
        });
        var evidence = Path.Combine(FindRepositoryRoot(),"local-evidence","item25");
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence,"role-change-issue-postgresql.log"),log);
        await File.WriteAllTextAsync(Path.Combine(evidence,"role-change-issue.json"),
            JsonSerializer.Serialize(new { Previous = new { previous.Id,previous.AssignmentType,previous.Version },
                Observations = observations, ObservationError = observationError,
                TransferredWhileIssueBlocked = transferredWhileIssueBlocked,
                Issue = first, Transfer = transfer, Fresh = fresh, State = state },
                new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(observationError is null,observationError + $" Issue:{first?.Body}; transfer:{transfer?.Body}");
        Assert.DoesNotContain("40P01",log,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deadlock detected",log,StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(first);
        Assert.NotNull(transfer);
        Assert.True(transfer.Status == HttpStatusCode.OK,transfer.Body);
        Assert.True(transferredWhileIssueBlocked);
        Assert.True(first.Status == HttpStatusCode.Created,first.Body);
        Assert.True(fresh.Status == HttpStatusCode.Forbidden,fresh.Body);
        var newAssignment = Assert.Single(freshActor.EffectiveRoleAssignments);
        Assert.Equal("FULL",newAssignment.AssignmentType);
        Assert.Equal("SERVICE_ENGINEER",newAssignment.RoleCode);
        Assert.NotEqual(previous.Id,newAssignment.AssignmentId);
        var old = Assert.Single(state.Assignments,row => row.Id == previous.Id);
        Assert.Equal(today.AddDays(-1),old.EffectiveTo);
        Assert.Equal(previous.Version + 1u,old.Version);
        var replacement = Assert.Single(state.Assignments,row => row.Id == newAssignment.AssignmentId);
        Assert.Equal(today,replacement.EffectiveFrom);
        Assert.Null(replacement.EffectiveTo);
        var issue = Assert.Single(state.Issues);
        Assert.Equal(employeeId,issue.IssuedByEmployeeId);
        Assert.Equal(previous.Id,issue.ResolvedRoleAssignmentId);
        Assert.Equal("FULL",issue.ResolvedRoleAssignmentType);
        Assert.Equal("STORES_ASSISTANT",issue.ActorRoleCode);
        Assert.NotNull(issue.StockPostingBatchId);
        Assert.Equal(1m,state.QuantityIssued);
        var roleEvent = Assert.Single(state.RoleEvents);
        Assert.Equal(directorId,roleEvent.ActorEmployeeId);
        Assert.Equal(newAssignment.AssignmentId,roleEvent.AssignmentId);
        var cost = Assert.Single(state.Fifo);
        Assert.Equal(1m,cost.Quantity);
        Assert.Equal(5900m,cost.UnitCost);
        Assert.Equal(5900m,cost.ConsumedValue);
        Assert.All(state.Balances,row => Assert.InRange(row.Quantity,0m,1m));
        Assert.Equal(1m,state.Balances.Sum(row => row.Quantity));
        var history = Assert.Single(state.Histories);
        Assert.Equal(employeeId,history.ActorEmployeeId);
        Assert.Equal(previous.Id,history.ResolvedRoleAssignmentId);
        return JsonSerializer.Deserialize<MaterialIssueView>(first.Body)!;
    }
}
