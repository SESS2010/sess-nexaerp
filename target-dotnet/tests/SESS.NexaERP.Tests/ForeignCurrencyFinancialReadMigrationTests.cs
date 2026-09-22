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
    public async Task CurrencyFinancialReadMigrationPreservesAuthorityAcrossRollbackAndReprovision()
    {
        const string previous = "20260913070000_ForeignPaymentBankAdvice";
        const string target = "20260913080000_ForeignCurrencyFinancialReadModels";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("currency-read-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "currency-read-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("currency-read-up.sql", migrator.GenerateScript(previous, target));
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute($"currency-read-acl-{run}.sql", CurrencyReadPermissions + VendorCurrencyReadSql.Guard(true));
        }
        server.Execute("currency-read-drift.sql", VendorCurrencyReadSql.PositionsAfter.Replace(
            "'netPayable'", "'changedNetPayable'", StringComparison.Ordinal));
        await using (var connection = new NpgsqlConnection(server.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(VendorCurrencyReadSql.Guard(true), connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed function baseline", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("currency-read-reset.sql", VendorCurrencyReadSql.PositionsAfter);
        server.Execute("currency-read-down.sql", migrator.GenerateScript(target, previous)
            + VendorCurrencyReadSql.Guard(false) + CurrencyReadPermissions);
        server.Execute("currency-read-reapply.sql", migrator.GenerateScript(previous, target)
            + VendorCurrencyReadSql.Guard(true) + CurrencyReadPermissions);
    }

    private const string CurrencyReadPermissions = """
        DO $assert$
        DECLARE signature text; target regprocedure;
        BEGIN
          FOREACH signature IN ARRAY ARRAY['advance.list_vendor_payables(uuid,uuid,boolean)',
            'advance.list_vendor_positions(uuid)'] LOOP
            target:=to_regprocedure(signature);
            IF NOT EXISTS(SELECT 1 FROM pg_proc WHERE oid=target AND prosecdef
              AND proowner='nexa_erp_owner'::regrole
              AND proconfig=ARRAY['search_path=pg_catalog, advance']
              AND has_function_privilege('nexa_erp_runtime',oid,'EXECUTE')
              AND NOT has_function_privilege('nexa_erp_bootstrap',oid,'EXECUTE')
              AND NOT has_function_privilege('nexa_erp_migration',oid,'EXECUTE')
              AND NOT EXISTS(SELECT 1 FROM aclexplode(proacl) WHERE grantee=0))
              OR has_table_privilege('nexa_erp_runtime','advance.vendor_advances','SELECT')
              OR has_table_privilege('nexa_erp_runtime','advance.vendor_payments','SELECT') THEN
              RAISE EXCEPTION 'Currency read migration changed controlled read permissions.';
            END IF;
          END LOOP;
        END $assert$;
        """;
}
