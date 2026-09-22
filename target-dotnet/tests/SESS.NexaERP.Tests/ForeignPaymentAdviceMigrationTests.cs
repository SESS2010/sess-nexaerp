using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task ForeignPaymentAdviceMigrationPreservesPermissionsAndRefusesChangedBaseline()
    {
        const string previous = "20260913060000_CommandReceiptReplay";
        const string target = "20260913070000_ForeignPaymentBankAdvice";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("foreign-advice-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "foreign-advice-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("foreign-advice-up.sql", migrator.GenerateScript(previous, target));
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute($"foreign-advice-acl-{run}.sql", ForeignAdvicePermissions);
        }
        server.Execute("foreign-advice-drift.sql", ForeignPaymentBankAdviceSql.After.Replace(
            "Bank advice evidence reference", "Changed advice evidence reference", StringComparison.Ordinal));
        await using (var connection = new NpgsqlConnection(server.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                ForeignPaymentBankAdviceSql.Guard(ForeignPaymentBankAdviceSql.After), connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed function baseline", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("foreign-advice-reset.sql", ForeignPaymentBankAdviceSql.After);
        server.Execute("foreign-advice-down.sql", migrator.GenerateScript(target, previous)
            + ForeignPaymentBankAdviceSql.Guard(ForeignPaymentBankAdviceSql.Before) + ForeignAdvicePermissions);
        server.Execute("foreign-advice-reapply.sql", migrator.GenerateScript(previous, target)
            + ForeignPaymentBankAdviceSql.Guard(ForeignPaymentBankAdviceSql.After) + ForeignAdvicePermissions);
    }

    private const string ForeignAdvicePermissions = """
        DO $assert$
        DECLARE target regprocedure:='advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)'::regprocedure;
        BEGIN
          IF NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid=target AND prosecdef
            AND proowner='nexa_erp_owner'::regrole
            AND proconfig=ARRAY['search_path=pg_catalog, advance']
            AND has_function_privilege('nexa_erp_runtime',oid,'EXECUTE')
            AND NOT has_function_privilege('nexa_erp_bootstrap',oid,'EXECUTE')
            AND NOT has_function_privilege('nexa_erp_migration',oid,'EXECUTE')
            AND NOT EXISTS(SELECT 1 FROM aclexplode(proacl) WHERE grantee=0))
            OR has_table_privilege('nexa_erp_runtime','advance.vendor_payments','SELECT')
            OR has_table_privilege('nexa_erp_runtime','advance.vendor_payments','INSERT') THEN
            RAISE EXCEPTION 'Foreign advice migration changed controlled payment permissions.';
          END IF;
        END $assert$;
        """;
}
