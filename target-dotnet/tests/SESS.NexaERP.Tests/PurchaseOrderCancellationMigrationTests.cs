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
    public async Task PurchaseOrderCancellationMigrationRefusesAuthorityAndTriggerDrift()
    {
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator=model.GetService<IMigrator>();
        const string previous="20260914040000_PurchaseObligations";
        const string current="20260914045000_PurchaseOrderCancellationHistory";
        server.Execute("cancel-predecessor.sql",migrator.GenerateScript("0",previous));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"cancel-history-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("cancel-up.sql",migrator.GenerateScript(previous,current));
        await using var connection=new NpgsqlConnection(server.ConnectionString);
        await connection.OpenAsync();
        async Task Sql(string value)
        {
            await using var command=new NpgsqlCommand(value,connection);
            await command.ExecuteNonQueryAsync();
        }
        async Task Refused()
        {
            await using var command=new NpgsqlCommand(PurchaseOrderCancellationHistorySql.Guard(true),connection);
            Assert.Equal("P0001",(await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync())).SqlState);
        }
        for(var i=0;i<2;i++)
        {
            Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
            await Sql(PurchaseOrderCancellationHistorySql.Guard(true));
        }
        await Sql("ALTER FUNCTION advance.rev869b_guard_history_insert() SECURITY DEFINER;");
        await Refused();
        await Sql("ALTER FUNCTION advance.rev869b_guard_history_insert() SECURITY INVOKER;");
        await Sql("GRANT EXECUTE ON FUNCTION advance.rev869b_guard_history_insert() TO nexa_erp_bootstrap;");
        await Refused();
        await Sql("REVOKE EXECUTE ON FUNCTION advance.rev869b_guard_history_insert() FROM nexa_erp_bootstrap;");
        await Sql("ALTER TABLE advance.purchase_order_history DISABLE TRIGGER trg_rev869b_po_history_insert_guard;");
        await Refused();
        await Sql("ALTER TABLE advance.purchase_order_history ENABLE TRIGGER trg_rev869b_po_history_insert_guard;");
        string original;
        await using(var command=new NpgsqlCommand("SELECT pg_get_functiondef('advance.rev869b_guard_history_insert()'::regprocedure)",connection))
            original=(string)(await command.ExecuteScalarAsync())!;
        var changed=original.Replace("cancelled.\"CancelledAt\" IS NOT NULL","true",StringComparison.Ordinal);
        Assert.NotEqual(original,changed);
        await Sql(changed);
        await Refused();
        await Sql(original);
        await Sql(PurchaseOrderCancellationHistorySql.Guard(true));
        server.Execute("cancel-down.sql",migrator.GenerateScript(current,previous));
        server.Execute("cancel-reapply.sql",migrator.GenerateScript(previous,current));
        await Sql(PurchaseOrderCancellationHistorySql.Guard(true));
    }
}
