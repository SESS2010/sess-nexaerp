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
    public async Task BankAdviceMigrationRoundtripPreservesPrivateEvidenceAndRefusesAuthorityDrift()
    {
        const string previous = "20260913080000_ForeignCurrencyFinancialReadModels";
        const string target = "20260913090000_GovernedVendorBankAdvice";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("bank-advice-predecessor.sql", migrator.GenerateScript("0", previous));
        const string password = "bank-advice-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("bank-advice-up.sql", migrator.GenerateScript(previous, target));
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute($"bank-advice-authority-{run}.sql", VendorBankAdviceMigrationSql.Guard(true));
        }
        server.Execute("bank-advice-drift.sql",
            "ALTER FUNCTION advance.vendor_bank_advice_content(uuid,uuid) RESET search_path;");
        await using (var connection = new NpgsqlConnection(server.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(VendorBankAdviceMigrationSql.Guard(true), connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed function baseline", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("bank-advice-reset.sql",
            "ALTER FUNCTION advance.vendor_bank_advice_content(uuid,uuid) SET search_path=pg_catalog,advance;");
        server.Execute("bank-advice-down.sql", migrator.GenerateScript(target, previous)
            + VendorBankAdviceMigrationSql.Guard(false));
        server.Execute("bank-advice-reapply.sql", migrator.GenerateScript(previous, target)
            + VendorBankAdviceMigrationSql.Guard(true));
        await using var runtime = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_erp_runtime", Password = password, Pooling = false }.ConnectionString);
        await runtime.OpenAsync();
        foreach (var sql in new[]
        {
            "SELECT * FROM advance.vendor_bank_advices",
            "SELECT advance.require_vendor_bank_advice(gen_random_uuid(),gen_random_uuid(),'missing')",
            """
            SELECT * FROM advance.record_vendor_bank_advice(
              gen_random_uuid(),gen_random_uuid(),'test.pdf','application/pdf',decode('255044462d','hex'),
              repeat('a',64),'unauthorized-bank-advice',repeat('b',64),gen_random_uuid(),
              'ACCOUNTS_MANAGER',gen_random_uuid(),'FULL','fixture')
            """
        })
        {
            await using var denied = new NpgsqlCommand(sql, runtime);
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.ExecuteScalarAsync());
            Assert.Equal("42501", error.SqlState);
        }
        server.Execute("bank-advice-empty.sql", """
            DO $a$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.vendor_bank_advices) THEN
                RAISE EXCEPTION 'Unauthorized file upload left bank advice evidence.';
              END IF;
            END $a$;
            """);
    }
}
