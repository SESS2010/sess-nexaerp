using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Npgsql;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Reporting;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;
using SESS.NexaERP.Infrastructure.Reporting;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task StoresQcStockMigrationGuardsAuthorityAndReconcilesRuntimeExecution()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260914060000_StoresWorkload";
        const string current = "20260914070000_StoresQcStock";
        server.Execute("stores-qc-stock-predecessor.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "stores-qc-stock-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("stores-qc-stock-up.sql", migrator.GenerateScript(previous, current));
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            server.Execute("stores-qc-stock-guard.sql", StoresQcStockMigrationSql.Guard(true));
        }
        server.Execute("stores-qc-stock-bad-grant.sql",
            "GRANT EXECUTE ON FUNCTION " + StoresQcStockMigrationSql.Signature + " TO nexa_erp_bootstrap;");
        Assert.Equal(1, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("stores-qc-stock-grant-restored.sql", StoresQcStockMigrationSql.Guard(true));
        await using var owner = new NpgsqlConnection(server.ConnectionString);
        await owner.OpenAsync();
        await using (var change = new NpgsqlCommand("ALTER FUNCTION " + StoresQcStockMigrationSql.Signature + " SECURITY INVOKER", owner))
            await change.ExecuteNonQueryAsync();
        await using (var guard = new NpgsqlCommand(StoresQcStockMigrationSql.Guard(true), owner))
            Assert.Equal("P0001", (await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync())).SqlState);
        await using (var restore = new NpgsqlCommand("ALTER FUNCTION " + StoresQcStockMigrationSql.Signature + " SECURITY DEFINER", owner))
            await restore.ExecuteNonQueryAsync();
        server.Execute("stores-qc-stock-down.sql", migrator.GenerateScript(current, previous));
        server.Execute("stores-qc-stock-reapply.sql", migrator.GenerateScript(previous, current));
        server.Execute("stores-qc-stock-final-guard.sql", StoresQcStockMigrationSql.Guard(true));
        server.Execute("qc-discrepancy-final-guard.sql", QcDiscrepancyPostingSql.Guard(true));
    }




}
