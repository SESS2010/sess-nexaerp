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
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InterruptedIssueRollsBackAndCanBeRetried(bool restartDatabase)
    {
        var observed = false;
        await RunCompletePurchaseFlow(issueRace: async context => {
            observed = true;
            return await RunInterruptedIssue(context,restartDatabase);
        }, durableDatabase: true);
        Assert.True(observed);
    }

    private static async Task<MaterialIssueView> RunInterruptedIssue(SerialIssueRaceContext context,bool restartDatabase)
    {
        var companyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var assignments = await Query(context.Options,async db =>
            (await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                .Where(x => x.CompanyId == companyId && x.EffectiveTo == null).ToListAsync())
            .ToDictionary(x => TaxWorkflowUser.AssignmentKey(x.EmployeeId,x.Role!.Code),
                x => new EffectiveRoleAssignment(x.Id,x.Role!.Code,x.AssignmentType)));
        var subject = await Query(context.Options,db => db.EmployeeIdentityMappings
            .Where(x => x.CompanyId == companyId && x.IsActive && x.EmployeeId == context.FirstOperatorId)
            .Select(x => x.Subject).SingleAsync());
        var actor = new TaxWorkflowUser(context.FirstOperatorId,subject,"STORES_EXECUTIVE",assignments);
        var named = new NpgsqlConnectionStringBuilder(context.RuntimeConnection) {
            ApplicationName = "failure-posting",Pooling = false
        }.ConnectionString;
        await using var host = await PurchaseFlowHost.StartAsync(named,actor,true,true);
        await using var db = new NexaErpDbContext(context.Options);
        var source = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal("127.0.0.1",source.Host);
        Assert.Equal("advance_parser",source.Database);
        Assert.Equal("postgres",source.Username);
        Assert.NotEqual(5432,source.Port);
        Assert.False(source.Pooling);
        async Task<string> Snapshot()
        {
            await using var connection = new NpgsqlConnection(source.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT jsonb_build_object(
                    'issues',(SELECT count(*) FROM advance.material_issues),
                    'lines',(SELECT count(*) FROM advance.material_issue_lines),
                    'history',(SELECT count(*) FROM advance.material_issue_history),
                    'batches',(SELECT count(*) FROM advance.stock_posting_batches),
                    'movements',(SELECT count(*) FROM advance.stock_movements),
                    'fifo',(SELECT count(*) FROM advance.fifo_cost_consumptions),
                    'audit',(SELECT count(*) FROM advance.audit_logs),
                    'requests',(SELECT count(*) FROM advance.command_requests),
                    'receipts',(SELECT count(*) FROM advance.command_receipts),
                    'requestStatus',(SELECT "Status" FROM advance.material_issue_requests WHERE "Id"=@request),
                    'requestVersion',(SELECT "Version" FROM advance.material_issue_requests WHERE "Id"=@request)
                )::text
                """,connection);
            command.Parameters.AddWithValue("request",context.Draft.Id);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        await using var observer = new NpgsqlConnection(source.ConnectionString);
        await observer.OpenAsync();
        await using (var settings = new NpgsqlCommand(
            "SELECT current_setting('fsync'),current_setting('synchronous_commit')",observer))
        await using (var rows = await settings.ExecuteReaderAsync())
        {
            Assert.True(await rows.ReadAsync());
            Assert.Equal("on",rows.GetString(0));
            Assert.Equal("on",rows.GetString(1));
        }
        var before = await Snapshot();
        await using (var install = new NpgsqlCommand("""
            CREATE FUNCTION advance.witness_interrupt_receipt() RETURNS trigger LANGUAGE plpgsql AS $f$
            BEGIN PERFORM pg_advisory_xact_lock(197260001::bigint); RETURN NEW; END $f$;
            CREATE TRIGGER witness_interrupt_receipt BEFORE INSERT ON advance.command_receipts
            FOR EACH ROW EXECUTE FUNCTION advance.witness_interrupt_receipt();
            SELECT pg_advisory_lock(197260001::bigint);
            """,observer))
            await install.ExecuteNonQueryAsync();
        var path = $"/api/v1/stores/material-issues/from-request/{context.Draft.Id}";
        var logStart = context.ReadPostgresLog().Length;
        var observations = new List<object>();
        Task<RaceHttpResult>? inFlight = null;
        RaceHttpResult? failed = null;
        try
        {
            inFlight = TimedRacePost(host.Client,path,context.Command);
            var pid = await ObserveEntityWriteWait(observer,"failure-posting",[observer.ProcessID],
                observations,"commit_command_receipt","SELECT");
            if (restartDatabase)
            {
                Assert.NotNull(context.RestartDatabase);
                context.RestartDatabase();
            }
            else
            {
                await using var terminate = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)",observer);
                terminate.Parameters.AddWithValue("pid",pid);
                Assert.True((bool)(await terminate.ExecuteScalarAsync())!);
            }
            failed = await inFlight.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            // A restarted server has released all advisory locks. The old
            // observer may be disconnected; cleanup uses a fresh connection.
            await observer.CloseAsync();
            await using var cleanup = new NpgsqlConnection(source.ConnectionString);
            await cleanup.OpenAsync();
            await using var remove = new NpgsqlCommand("""
                DROP TRIGGER IF EXISTS witness_interrupt_receipt ON advance.command_receipts;
                DROP FUNCTION IF EXISTS advance.witness_interrupt_receipt();
                """,cleanup);
            await remove.ExecuteNonQueryAsync();
        }
        Assert.NotNull(failed);
        var afterFailure = await Snapshot();
        var retry = await TimedRacePost(host.Client,path,context.Command);
        var replay = await TimedRacePost(host.Client,path,context.Command);
        var afterRetry = await Snapshot();
        var result = JsonSerializer.Deserialize<MaterialIssueView>(retry.Body)!;
        var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item26");
        Directory.CreateDirectory(directory);
        var name = restartDatabase ? "postgres-restart" : "posting-connection-loss";
        var log = context.ReadPostgresLog()[logStart..];
        await File.WriteAllTextAsync(Path.Combine(directory,name+".json"),JsonSerializer.Serialize(new {
            Durability = "fsync=on;synchronous_commit=on",Observations = observations,
            Failed = failed,Retry = retry,Replay = replay,Before = before,AfterFailure = afterFailure,AfterRetry = afterRetry,
            TimingNote = "Interruption and deliberate receipt lock wait included; not normal request latency."
        },new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(directory,name+"-postgresql.log"),log);
        Assert.Equal(before,afterFailure);
        Assert.Equal(HttpStatusCode.InternalServerError,failed.Status);
        Assert.Equal(HttpStatusCode.Created,retry.Status);
        Assert.Equal(HttpStatusCode.Created,replay.Status);
        Assert.False(result.Replayed);
        var replayed = JsonSerializer.Deserialize<MaterialIssueView>(replay.Body)!;
        Assert.True(replayed.Replayed);
        Assert.Equal(result.Id,replayed.Id);
        Assert.Equal(1,await Query(context.Options,state => state.MaterialIssues.CountAsync(x => x.MaterialIssueRequestId == context.Draft.Id)));
        Assert.Equal(1m,Assert.Single(result.Lines).QuantityBase);
        Assert.DoesNotContain("40P01",log,StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
