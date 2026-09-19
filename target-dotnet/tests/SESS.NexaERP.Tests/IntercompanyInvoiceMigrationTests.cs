using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task IntercompanyInvoiceMigrationPreservesPrivateEvidenceAcrossReprovisionAndRollback()
    {
        const string target = "20260919100000_IntercompanyInvoiceEvidence";
        const string deploy = "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var predecessor = migrations[Array.IndexOf(migrations, target) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), databaseName: "sess_nexa_erp");
        server.Execute("ic-invoice-predecessor.sql", migrator.GenerateScript("0", predecessor));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "ic-invoice-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("ic-invoice-up.sql", deploy + migrator.GenerateScript(predecessor, target));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.AssertRejected("ic-invoice-direct-read.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.intercompany_invoice_evidence;", "permission denied");
        server.AssertRejected("ic-invoice-direct-delete.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; DELETE FROM advance.intercompany_invoice_evidence;", "permission denied");
        server.Execute("ic-invoice-weakened-acl.sql", "GRANT SELECT ON advance.intercompany_invoice_evidence TO nexa_erp_runtime;");
        Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.AssertRejected("ic-invoice-read-after-repair.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.intercompany_invoice_evidence;", "permission denied");
        server.Execute("ic-invoice-weakened-function.sql", "GRANT EXECUTE ON FUNCTION advance.guard_intercompany_invoice_evidence() TO nexa_erp_runtime;");
        Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("ic-invoice-down.sql", deploy + migrator.GenerateScript(target, predecessor));
        server.Execute("ic-invoice-reapply.sql", deploy + migrator.GenerateScript(predecessor, target));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("ic-invoice-install-effects.sql", """
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.intercompany_invoice_evidence)
              OR (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('accounts.intercompany-invoices')::uuid)<>1
             THEN RAISE EXCEPTION 'Invoice installation changed business evidence or Accounts permission count.'; END IF;
            END $assert$;
            """);
    }
}
