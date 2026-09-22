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
    public async Task ReceiptQuantityMigrationRefusesBodyAuthorityAndTriggerDrift()
    {
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator=model.GetService<IMigrator>();
        const string previous="20260914020000_PurchaseSpending";
        const string current="20260914030000_ReceiptQuantityAcrossPoRevisions";
        server.Execute("receipt-quantity-predecessor.sql",migrator.GenerateScript("0",previous));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"receipt-quantity-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("receipt-quantity-up.sql",migrator.GenerateScript(previous,current));
        await using var connection=new NpgsqlConnection(server.ConnectionString);
        await connection.OpenAsync();
        async Task Sql(string value)
        {
            await using var command=new NpgsqlCommand(value,connection);
            await command.ExecuteNonQueryAsync();
        }
        async Task Refused()
        {
            await using var command=new NpgsqlCommand(ReceiptQuantityAcrossPoRevisionsSql.Guard(true),connection);
            Assert.Equal("P0001",(await Assert.ThrowsAsync<PostgresException>(()=>command.ExecuteNonQueryAsync())).SqlState);
        }
        for(var i=0;i<2;i++)
        {
            Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
            await Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(true));
        }
        await Sql("ALTER FUNCTION advance.stores_p2_goods_receipt_guard() SECURITY DEFINER;");
        await Refused();
        await Sql("ALTER FUNCTION advance.stores_p2_goods_receipt_guard() SECURITY INVOKER;");
        await Sql("GRANT EXECUTE ON FUNCTION advance.stores_p2_goods_receipt_line_guard() TO nexa_erp_bootstrap;");
        await Refused();
        await Sql("REVOKE EXECUTE ON FUNCTION advance.stores_p2_goods_receipt_line_guard() FROM nexa_erp_bootstrap;");
        await Sql("ALTER TABLE advance.goods_receipts DISABLE TRIGGER \"TR_goods_receipt_guard\";");
        await Refused();
        await Sql("ALTER TABLE advance.goods_receipts ENABLE TRIGGER \"TR_goods_receipt_guard\";");
        var changed=ReceiptQuantityAcrossPoRevisionsSql.Definition(true)
            .Replace(")>ordered_line.\"OrderedQuantity\"",")>=ordered_line.\"OrderedQuantity\"",StringComparison.Ordinal);
        Assert.NotEqual(ReceiptQuantityAcrossPoRevisionsSql.Definition(true),changed);
        await Sql(changed);
        await Refused();
        await Sql(ReceiptQuantityAcrossPoRevisionsSql.Definition(true));
        await Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(true));
        server.Execute("receipt-quantity-down.sql",migrator.GenerateScript(current,previous));
        server.Execute("receipt-quantity-reapply.sql",migrator.GenerateScript(previous,current));
        await Sql(ReceiptQuantityAcrossPoRevisionsSql.Guard(true));
    }
}
