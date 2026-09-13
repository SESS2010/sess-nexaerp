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
    public async Task VendorCashCapMigrationReconcilesPrivateHelpersAndRefusesDrift()
    {
        const string previous = "20260913090000_GovernedVendorBankAdvice";
        const string target = "20260913100000_VendorPoCashCap";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("cash-cap-predecessor.sql", migrator.GenerateScript("0", previous));
        const string password = "vendor-cash-runtime-123456789";
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, password);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("cash-cap-up.sql", migrator.GenerateScript(previous, target));
        for (var run = 0; run < 2; run++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute($"cash-cap-guard-{run}.sql", VendorPoCashCapMigrationSql.Guard(true) + PurchaseOrderSupersedeHistorySql.Guard(true));
        }
        server.Execute("cash-cap-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION advance.vendor_po_cash_totals(uuid,uuid,text,uuid) TO nexa_erp_runtime;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("cash-cap-grant-reconciled.sql", VendorPoCashCapMigrationSql.Guard(true) + PurchaseOrderSupersedeHistorySql.Guard(true));

        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        string historyDefinition;
        await using (var read = new NpgsqlCommand("SELECT pg_get_functiondef('advance.rev869b_guard_history_insert()'::regprocedure)", owner))
            historyDefinition = (string)(await read.ExecuteScalarAsync())!;
        const string historyPredicate = "AND replacement.\"RequiredApprovalStepCount\">0";
        Assert.Contains(historyPredicate, historyDefinition, StringComparison.Ordinal);
        await using (var drift = new NpgsqlCommand(historyDefinition.Replace(historyPredicate,
            "AND replacement.\"RequiredApprovalStepCount\">=0", StringComparison.Ordinal), owner))
            await drift.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(PurchaseOrderSupersedeHistorySql.Guard(true), owner))
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("missing or changed approval guard", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        await using (var restore = new NpgsqlCommand(historyDefinition, owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("supersede-history-restored.sql", PurchaseOrderSupersedeHistorySql.Guard(true));

        foreach (var isolation in new[] { System.Data.IsolationLevel.ReadCommitted, System.Data.IsolationLevel.RepeatableRead })
        {
            await using var transaction = await owner.BeginTransactionAsync(isolation);
            await using var guard = new NpgsqlCommand(
                "SELECT advance.require_vendor_po_cash_limit(gen_random_uuid(),gen_random_uuid(),1,'INR')", owner, transaction);
            var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteScalarAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("require a serializable transaction", error.MessageText, StringComparison.OrdinalIgnoreCase);
            await transaction.RollbackAsync();
        }
        server.Execute("cash-cap-disabled.sql",
            "ALTER TABLE advance.purchase_orders DISABLE TRIGGER trg_purchase_order_vendor_cash;");
        await using (var guard = new NpgsqlCommand(VendorPoCashCapMigrationSql.Guard(true), owner))
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed trigger or index protection", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("cash-cap-enabled.sql",
            "ALTER TABLE advance.purchase_orders ENABLE TRIGGER trg_purchase_order_vendor_cash;");
        const string dropTrigger = "DROP TRIGGER trg_purchase_order_vendor_cash ON advance.purchase_orders;";
        const string trigger = "CREATE TRIGGER trg_purchase_order_vendor_cash BEFORE INSERT OR UPDATE ON advance.purchase_orders FOR EACH ROW ";
        server.Execute("cash-cap-false-condition.sql", dropTrigger + trigger +
            "WHEN (false) EXECUTE FUNCTION advance.guard_purchase_order_vendor_cash();");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        await using (var guard = new NpgsqlCommand(VendorPoCashCapMigrationSql.Guard(true), owner))
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed trigger or index protection", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("cash-cap-condition-restored.sql", dropTrigger + trigger +
            "EXECUTE FUNCTION advance.guard_purchase_order_vendor_cash();");
        server.Execute("cash-cap-body-drift.sql",
            "ALTER FUNCTION advance.vendor_po_cash_totals(uuid,uuid,text,uuid) RESET search_path;");
        await using (var guard = new NpgsqlCommand(VendorPoCashCapMigrationSql.Guard(true), owner))
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
            Assert.Equal("P0001", error.SqlState);
            Assert.Contains("changed function baseline", error.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        server.Execute("cash-cap-body-restored.sql",
            "ALTER FUNCTION advance.vendor_po_cash_totals(uuid,uuid,text,uuid) SET search_path=pg_catalog,advance;");
        server.Execute("cash-cap-down.sql", migrator.GenerateScript(target, previous) + VendorPoCashCapMigrationSql.Guard(false) + PurchaseOrderSupersedeHistorySql.Guard(false));
        server.Execute("cash-cap-reapply.sql", migrator.GenerateScript(previous, target) + VendorPoCashCapMigrationSql.Guard(true) + PurchaseOrderSupersedeHistorySql.Guard(true));
        await using var runtime = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_erp_runtime", Password = password, Pooling = false }.ConnectionString);
        await runtime.OpenAsync();
        foreach (var sql in new[]
        {
            "SELECT * FROM advance.vendor_po_cash_totals(gen_random_uuid(),gen_random_uuid(),'INR',gen_random_uuid())",
            "SELECT advance.require_vendor_po_cash_limit(gen_random_uuid(),gen_random_uuid(),1,'INR')",
            "SELECT advance.guard_purchase_order_vendor_cash()",
            "SELECT * FROM advance.vendor_advances"
        })
        {
            await using var denied = new NpgsqlCommand(sql, runtime);
            var error = await Assert.ThrowsAsync<PostgresException>(() => denied.ExecuteScalarAsync());
            Assert.Equal("42501", error.SqlState);
        }
    }
}
