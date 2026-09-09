using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string FitmentTarget = "20260908133110_ComponentFitmentAndGeneratedActualBom";

    [Fact]
    public void Component_fitment_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, FitmentTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("fitment-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("fitment-up.sql", migrator.GenerateScript(predecessor, FitmentTarget) + FitmentAssertions);
        server.Execute("fitment-down.sql", migrator.GenerateScript(FitmentTarget, predecessor));
        server.Execute("fitment-reapply.sql", migrator.GenerateScript(predecessor, FitmentTarget) + FitmentAssertions);
    }

    private const string FitmentAssertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.component_fitments') IS NULL
             OR to_regclass('advance.component_fitment_reversals') IS NULL
             OR to_regclass('advance.actual_boms') IS NULL
             OR to_regclass('advance.actual_bom_entries') IS NULL THEN
            RAISE EXCEPTION 'Fitment and Actual BOM tables are missing.';
          END IF;
          IF to_regprocedure('advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL THEN
            RAISE EXCEPTION 'Controlled fitment functions are missing.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_component_fitment_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_fitment_batch_reconcile') THEN
            RAISE EXCEPTION 'Fitment immutability or stock reconciliation guard is missing.';
          END IF;
          IF (SELECT count(*) FROM advance.roles WHERE "Code"='SERVICE_MANAGER')<>1 THEN
            RAISE EXCEPTION 'SERVICE_MANAGER role was not created exactly once.';
          END IF;
          IF (SELECT count(*) FROM advance.company_role_activations a
                JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE r."Code"='SERVICE_MANAGER' AND a."IsEnabled")<>2 THEN
            RAISE EXCEPTION 'SERVICE_MANAGER requires two company activations.';
          END IF;
          IF (SELECT count(*) FROM advance.page_definitions
                WHERE "CreatedBy"='ComponentFitmentAndGeneratedActualBom')<>1 THEN
            RAISE EXCEPTION 'Expected one fitment page definition.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions
                WHERE "CreatedBy"='ComponentFitmentAndGeneratedActualBom')<>5 THEN
            RAISE EXCEPTION 'Expected five fitment page grants.';
          END IF;
          IF (SELECT count(*) FROM advance.employee_role_assignments a
                JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE r."Code"='SERVICE_MANAGER')<>0 THEN
            RAISE EXCEPTION 'SERVICE_MANAGER must not be assigned by this migration.';
          END IF;
        END $assert$;
        """;
}