using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ReceiptReplayMigrationPreservesRestrictedPermissionsAcrossReprovisionAndRollback()
    {
        const string target = "20260913060000_CommandReceiptReplay";
        const string previous = "20260913050000_ConcessionSerialDecisionHistory";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("receipt-replay-predecessor.sql",migrator.GenerateScript("0",previous));
        const string password = "receipt-replay-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString,password);
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("receipt-replay-up.sql",migrator.GenerateScript(previous,target));
        for (var run=0;run<2;run++)
        {
            Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
            server.Execute($"receipt-replay-acl-{run}.sql",ReceiptReplayAssertions);
        }
        server.Execute("receipt-replay-down.sql",migrator.GenerateScript(target,previous));
        server.Execute("receipt-replay-absent.sql","""
            DO $a$ BEGIN
              IF to_regprocedure('advance.read_command_receipt(uuid)') IS NOT NULL THEN
                RAISE EXCEPTION 'Receipt reader remains after rollback.';
              END IF;
            END $a$;
            """);
        server.Execute("receipt-replay-reapply.sql",migrator.GenerateScript(previous,target)+ReceiptReplayAssertions);
        await using var runtime = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(server.ConnectionString)
            { Username="nexa_erp_runtime",Password=password,Pooling=false }.ConnectionString);
        await runtime.OpenAsync();
        foreach (var sql in new[] {
            "SELECT advance.read_command_receipt(gen_random_uuid())",
            "SELECT * FROM advance.command_requests",
            "SELECT * FROM advance.command_receipts" })
        {
            await using var denied = new NpgsqlCommand(sql,runtime);
            var error = await Assert.ThrowsAsync<PostgresException>(()=>denied.ExecuteScalarAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege,error.SqlState);
        }
    }

    private const string ReceiptReplayAssertions = """
        DO $a$
        BEGIN
          IF NOT EXISTS(SELECT 1 FROM pg_proc p
            WHERE p.oid=to_regprocedure('advance.read_command_receipt(uuid)')
              AND p.prosecdef AND p.proowner='nexa_erp_owner'::regrole
              AND p.prorettype='jsonb'::regtype
              AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
              AND has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE')
              AND NOT has_function_privilege('nexa_erp_bootstrap',p.oid,'EXECUTE')
              AND NOT has_function_privilege('nexa_erp_migration',p.oid,'EXECUTE')
              AND NOT EXISTS(SELECT 1 FROM aclexplode(p.proacl) a WHERE a.grantee=0))
            OR has_table_privilege('nexa_erp_runtime','advance.command_requests','SELECT')
            OR has_table_privilege('nexa_erp_runtime','advance.command_receipts','SELECT') THEN
            RAISE EXCEPTION 'Receipt replay authority or table ACL changed.';
          END IF;
        END $a$;
        """;
}
