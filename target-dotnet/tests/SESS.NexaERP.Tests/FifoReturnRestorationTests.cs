using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task FifoReturnRestorationMigrationKeepsLedgersPrivateAndSupportsEmptyRollback()
    {
        const string target="20260914080000_FifoReturnRestorations";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        var migrations=model.Database.GetMigrations().ToArray();
        var index=Array.IndexOf(migrations,target);
        Assert.True(index>0);
        var predecessor=migrations[index-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("fifo-restoration-predecessor.sql",migrator.GenerateScript("0",predecessor));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"fifo-restoration-runtime-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("fifo-restoration-up.sql",migrator.GenerateScript(predecessor,target));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("fifo-restoration-private.sql",FifoReturnRestorationSql.Guard(true)+"""
            DO $assert$ BEGIN
              IF has_table_privilege('nexa_erp_runtime','advance.fifo_cost_restorations','SELECT')
                OR has_table_privilege('nexa_erp_runtime','advance.fifo_cost_restorations','INSERT')
                OR has_table_privilege('nexa_erp_runtime','advance.fifo_consumption_creation_order','SELECT')
                OR has_function_privilege('nexa_erp_runtime','advance.restore_fifo_for_material_return(uuid,uuid,boolean)','EXECUTE')
                OR has_function_privilege('nexa_erp_runtime','advance.guard_material_return_fifo_restoration()','EXECUTE')
                OR NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.material_returns'::regclass
                  AND tgname='trg_material_return_fifo_complete' AND tgdeferrable AND tginitdeferred AND tgenabled='O') THEN
                RAISE EXCEPTION 'FIFO restoration private authority or deferred completeness guard is invalid.';
              END IF;
            END $assert$;
            """);
        server.Execute("fifo-restoration-tamper.sql","GRANT EXECUTE ON FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean) TO nexa_erp_runtime;");
        server.AssertRejected("fifo-restoration-tamper-refused.sql",FifoReturnRestorationSql.Guard(true),"changed function body or authority");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("fifo-restoration-repaired.sql",FifoReturnRestorationSql.Guard(true));
        server.Execute("fifo-restoration-down.sql",migrator.GenerateScript(target,predecessor));
        server.Execute("fifo-restoration-reapply.sql",migrator.GenerateScript(predecessor,target));
        server.Execute("fifo-restoration-reapplied-guard.sql",FifoReturnRestorationSql.Guard(true));
    }
}
