using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string FatReadinessTarget = "20260908195922_JobOrderFatReadiness";

    [Fact]
    public void Job_order_FAT_readiness_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, FatReadinessTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("fat-readiness-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("fat-readiness-up.sql", migrator.GenerateScript(predecessor, FatReadinessTarget) + FatReadinessAssertions);
        server.Execute("fat-readiness-down.sql", migrator.GenerateScript(FatReadinessTarget, predecessor));
        server.Execute("fat-readiness-reapply.sql", migrator.GenerateScript(predecessor, FatReadinessTarget) + FatReadinessAssertions);
    }

    private const string FatReadinessAssertions = """
        DO $assert$
        BEGIN
          IF to_regclass('advance.job_order_fat_custody_explanations') IS NULL
             OR to_regclass('advance.job_order_fat_reconciliations') IS NULL
             OR to_regclass('advance.job_order_fat_reconciliation_lines') IS NULL THEN
            RAISE EXCEPTION 'FAT readiness evidence tables are missing.';
          END IF;
          IF to_regprocedure('advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.fat_live_balances(uuid,uuid)') IS NULL THEN
            RAISE EXCEPTION 'FAT readiness controlled functions are missing.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_fat_explanation_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_fat_reconciliation_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_fat_reconciliation_line_guard')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='trg_job_order_fat_readiness' AND tgdeferrable) THEN
            RAISE EXCEPTION 'FAT immutable or deferred readiness guard is missing.';
          END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "CreatedBy"='JobOrderFatReadiness')<>1
             OR (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='JobOrderFatReadiness')<>6 THEN
            RAISE EXCEPTION 'Expected one FAT page and six exact role grants.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.job_orders WHERE "FatReadinessStatus"<>'NOT_RECONCILED'
              OR "FatReconciledAt" IS NOT NULL OR "FatReconciledByEmployeeId" IS NOT NULL
              OR "LatestFatReconciliationId" IS NOT NULL) THEN
            RAISE EXCEPTION 'Existing Job Orders did not receive the safe FAT default.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.job_order_fat_custody_explanations)
             OR EXISTS (SELECT 1 FROM advance.job_order_fat_reconciliations)
             OR EXISTS (SELECT 1 FROM advance.job_order_fat_reconciliation_lines) THEN
            RAISE EXCEPTION 'FAT migration invented operational evidence.';
          END IF;
        END $assert$;
        """;
}