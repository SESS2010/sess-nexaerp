using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task VendorManualAssessmentMigrationUsesDeploymentOwnerAndRoundTripsUnusedPackage()
    {
        const string previous = "20260919120000_GovernedInventoryPeriods";
        const string current = "20260919130000_VendorManualAssessments";
        const string deploy = "SET SESSION AUTHORIZATION nexa_erp_migration; SET ROLE nexa_erp_owner;\n";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), databaseName: "sess_nexa_erp");
        server.Execute("manual-deploy-previous.sql", migrator.GenerateScript("0", previous));
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "manual-assessment-disposable-only");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("manual-deploy-up.sql", deploy + migrator.GenerateScript(previous, current));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("manual-install-row-effects.sql", """
            DO $assert$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.vendor_manual_assessments)
              OR EXISTS(SELECT 1 FROM advance.stock_movements)
              OR (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='quality.vendor-manual-assessments')<>1
              OR (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid)<>1
              OR NOT EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                WHERE p."PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid AND r."Code"='QC_MANAGER'
                 AND p."CanView" AND p."CanCreate" AND NOT p."HasFullControl" AND NOT p."CanApprove"
                 AND NOT p."CanViewCommercialValues")
             THEN RAISE EXCEPTION 'Manual assessment install requires exactly its QC page/grant and zero business rows.'; END IF;
            END $assert$;
            """);
        server.AssertRejected("manual-private-read.sql", "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.vendor_manual_assessments;", "permission denied");
        server.Execute("manual-acl-drift.sql", "GRANT SELECT ON advance.vendor_manual_assessments TO PUBLIC;");
        Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("manual-unused-down.sql", deploy + migrator.GenerateScript(current, previous));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        server.Execute("manual-down-assert.sql", """
            DO $assert$ BEGIN
             IF to_regclass('advance.vendor_manual_assessments') IS NOT NULL
              OR to_regprocedure('advance.vendor_manual_assessment_history(uuid,uuid)') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='quality.vendor-manual-assessments')
             THEN RAISE EXCEPTION 'Unused manual assessment package survived rollback.'; END IF;
            END $assert$;
            """);
        server.Execute("manual-reapply.sql", deploy + migrator.GenerateScript(previous, current));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
    }
}
