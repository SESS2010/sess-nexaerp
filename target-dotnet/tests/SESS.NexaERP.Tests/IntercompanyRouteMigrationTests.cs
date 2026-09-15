using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task IntercompanyRouteMigrationPreservesPrivateAccessAcrossProvisionAndRollback()
    {
        const string target = "20260915180000_IntercompanyRoutes";
        const string deployAsOwner = """
            SET SESSION AUTHORIZATION nexa_erp_migration;
            SET ROLE nexa_erp_owner;
            DO $deployment$ BEGIN
             IF current_database()<>'sess_nexa_erp' OR session_user<>'nexa_erp_migration'
              OR current_user<>'nexa_erp_owner' OR current_setting('is_superuser')<>'off'
             THEN RAISE EXCEPTION 'Wrong disposable deployment identity.'; END IF;
            END $deployment$;
            """ + "\n";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var predecessor = migrations[Array.IndexOf(migrations, target) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), databaseName: "sess_nexa_erp");
        server.Execute("intercompany-predecessor.sql", migrator.GenerateScript("0", predecessor));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "intercompany-runtime-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("intercompany-up.sql", deployAsOwner + migrator.GenerateScript(predecessor, target));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.AssertRejected("intercompany-direct-read.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.intercompany_routes;", "permission denied");
        server.AssertRejected("intercompany-direct-write.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; DELETE FROM advance.intercompany_route_decisions;", "permission denied");
        server.AssertRejected("intercompany-internal-function.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT advance.validate_intercompany_route(NULL::advance.intercompany_routes);", "permission denied");
        server.Execute("intercompany-weakened-acl.sql", "GRANT SELECT ON advance.intercompany_routes TO nexa_erp_runtime;");
        Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        const string publication = "20260915190000_IntercompanyPurchasePublication";
        server.Execute("intercompany-publication-up.sql", deployAsOwner + migrator.GenerateScript(target, publication));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.AssertRejected("intercompany-publication-direct-read.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.intercompany_purchase_publication_lines;", "permission denied");
        server.Execute("intercompany-publication-down.sql", deployAsOwner + migrator.GenerateScript(publication, target));
        server.Execute("intercompany-down.sql", deployAsOwner + migrator.GenerateScript(target, predecessor));
        server.Execute("intercompany-reapply.sql", deployAsOwner + migrator.GenerateScript(predecessor, target));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("intercompany-no-business-rows.sql", """
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.intercompany_routes)
              OR EXISTS(SELECT 1 FROM advance.intercompany_route_decisions)
              OR (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('stores.intercompany-routes')::uuid)<>3
             THEN RAISE EXCEPTION 'Intercompany route installation changed business rows or permission counts.'; END IF;
            END $assert$;
            """);
    }
}
