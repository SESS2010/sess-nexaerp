using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task InterruptedMigrationRollsBackDdlAndHistoryBeforeRetry()
    {
        const string target = "20260913060000_CommandReceiptReplay";
        const string previous = "20260913050000_ConcessionSerialDecisionHistory";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(),durable:true);
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString,x => x.MigrationsHistoryTable("__EFMigrationsHistory","advance")).Options;
        await using var model = new NexaErpDbContext(options);
        var migrator = model.GetService<IMigrator>();
        server.Execute("interrupted-migration-predecessor.sql",migrator.GenerateScript("0",previous));
        const string password = "migration-interruption-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString,password);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        await using var observer = new NpgsqlConnection(server.ConnectionString);
        await observer.OpenAsync();
        async Task<string> Snapshot()
        {
            await using var command = new NpgsqlCommand("""
                SELECT jsonb_build_object(
                  'fsync',current_setting('fsync'),'synchronousCommit',current_setting('synchronous_commit'),
                  'history',(SELECT jsonb_agg("MigrationId" ORDER BY "MigrationId") FROM advance."__EFMigrationsHistory"),
                  'functionPresent',to_regprocedure('advance.read_command_receipt(uuid)') IS NOT NULL,
                  'requests',(SELECT count(*) FROM advance.command_requests),
                  'receipts',(SELECT count(*) FROM advance.command_receipts))::text
                """,observer);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        var before = await Snapshot();
        using (var settings = JsonDocument.Parse(before))
        {
            Assert.Equal("on",settings.RootElement.GetProperty("fsync").GetString());
            Assert.Equal("on",settings.RootElement.GetProperty("synchronousCommit").GetString());
        }
        await using (var gate = new NpgsqlCommand("""
            CREATE FUNCTION advance.witness_migration_gate() RETURNS trigger LANGUAGE plpgsql AS $f$
            BEGIN PERFORM pg_advisory_xact_lock(197260002::bigint); RETURN NEW; END $f$;
            CREATE TRIGGER witness_migration_gate BEFORE INSERT ON advance."__EFMigrationsHistory"
            FOR EACH ROW WHEN (NEW."MigrationId"='20260913060000_CommandReceiptReplay')
            EXECUTE FUNCTION advance.witness_migration_gate();
            SELECT pg_advisory_lock(197260002::bigint);
            """,observer))
            await gate.ExecuteNonQueryAsync();
        var named = new NpgsqlConnectionStringBuilder(server.ConnectionString) { ApplicationName="failure-migration" }.ConnectionString;
        var observations = new List<object>();
        var logStart = server.ReadDiagnosticLog().Length;
        string failure;
        await using (var migration = new NpgsqlConnection(named))
        {
            await migration.OpenAsync();
            await using var apply = new NpgsqlCommand(migrator.GenerateScript(previous,target),migration) { CommandTimeout=60 };
            var inFlight = apply.ExecuteNonQueryAsync();
            try
            {
                var pid = await ObserveEntityWriteWait(observer,"failure-migration",[observer.ProcessID],
                    observations,"__EFMigrationsHistory","INSERT");
                Assert.Equal(migration.ProcessID,pid);
                Assert.Equal(before,await Snapshot());
                await using var kill = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)",observer);
                kill.Parameters.AddWithValue("pid",pid);
                Assert.True((bool)(await kill.ExecuteScalarAsync())!);
                var error = await Assert.ThrowsAnyAsync<NpgsqlException>(()=>inFlight);
                failure = error.GetType().Name;
            }
            finally
            {
                await using var release = new NpgsqlCommand("""
                    SELECT pg_advisory_unlock(197260002::bigint);
                    DROP TRIGGER witness_migration_gate ON advance."__EFMigrationsHistory";
                    DROP FUNCTION advance.witness_migration_gate();
                    """,observer);
                await release.ExecuteNonQueryAsync();
            }
        }
        var afterFailure = await Snapshot();
        Assert.Equal(before,afterFailure);
        await observer.CloseAsync();
        await migrator.MigrateAsync(target);
        await observer.OpenAsync();
        var afterRetry = await Snapshot();
        server.Execute("interrupted-migration-retry-acl.sql",ReceiptReplayAssertions);
        using (var state = JsonDocument.Parse(afterRetry))
        {
            Assert.True(state.RootElement.GetProperty("functionPresent").GetBoolean());
            Assert.Equal(1,state.RootElement.GetProperty("history").EnumerateArray().Count(x=>x.GetString()==target));
        }
        await observer.CloseAsync();
        await migrator.MigrateAsync(target);
        await observer.OpenAsync();
        Assert.Equal(afterRetry,await Snapshot());
        var directory = Path.Combine(FindRepositoryRoot(),"local-evidence","item26");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory,"migration-interruption.json"),
            JsonSerializer.Serialize(new { Before=before,AfterFailure=afterFailure,AfterRetry=afterRetry,
                Failure=failure,Observations=observations },new JsonSerializerOptions { WriteIndented=true }));
        await File.WriteAllTextAsync(Path.Combine(directory,"migration-interruption-postgresql.log"),
            server.ReadDiagnosticLog()[logStart..]);
    }
}
