using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void Estimated_bom_foundation_applies_and_reverts_on_disposable_postgresql()
    {
        const string target = "20260907034428_EstimatedBomFoundation";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("estimated-bom-pre.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("estimated-bom-up.sql", migrator.GenerateScript(predecessor, target));
        server.Execute("estimated-bom-down.sql", migrator.GenerateScript(target, predecessor));
        server.Execute("estimated-bom-reup.sql", migrator.GenerateScript(predecessor, target));
    }
}
