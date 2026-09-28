using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

/// <summary>
/// The backend batch of 26 September (migrations 132-136) goes down to migration 131 and up again
/// on an unused database, and each installed rewrite comes back byte for byte.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void The_26_September_batch_migrations_go_down_to_131_and_up_again()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        const string baseline = "20260926090000_DevelopmentLoginsOffByDefault";
        Assert.Contains(baseline, migrations);
        var batch = migrations.SkipWhile(x => x != baseline).Skip(1).ToArray();
        Assert.Equal(["20260926100000_VendorBillSeparateDecider", "20260926110000_CompanyProfileAndWarehouseState",
            "20260926120000_MachineDeliveryDispatchDetails", "20260926130000_OpeningStockWithdraw", "20260926140000_PrintPermissionGrants"], batch);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("batch-baseline.sql", migrator.GenerateScript("0", baseline));
        const string fingerprint = """
            CREATE TEMP TABLE IF NOT EXISTS batch_fingerprints(stage text, value text);
            INSERT INTO batch_fingerprints SELECT '{0}', md5(string_agg(x, '|' ORDER BY x)) FROM (
              SELECT p.oid::regprocedure::text||':'||md5(replace(p.prosrc,E'\r\n',E'\n')) x FROM pg_proc p
                WHERE p.oid IN ('advance.record_machine_delivery(uuid,uuid,uuid,text,jsonb,bytea,uuid,text,uuid,text,text)'::regprocedure,
                                'advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)'::regprocedure)
              UNION ALL SELECT conname||':'||pg_get_constraintdef(oid) FROM pg_constraint WHERE conname='CK_opening_stock_status'
              UNION ALL SELECT indexname||':'||indexdef FROM pg_indexes WHERE indexname='IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd'
              UNION ALL SELECT 'print:'||count(*) FILTER (WHERE "CanPrint") FROM advance.role_page_permissions) s;
            """;
        server.Execute("batch-before.sql", fingerprint.Replace("{0}", "before") + """
            CREATE TABLE public.batch_fingerprint_before AS SELECT value FROM batch_fingerprints WHERE stage='before';
            """);
        server.Execute("batch-up.sql", migrator.GenerateScript(baseline, batch[^1]));
        server.Execute("batch-down.sql", migrator.GenerateScript(batch[^1], baseline));
        server.Execute("batch-after-down.sql", fingerprint.Replace("{0}", "after") + """
            DO $same$ BEGIN
              IF (SELECT value FROM batch_fingerprints WHERE stage='after') IS DISTINCT FROM (SELECT value FROM public.batch_fingerprint_before) THEN
                RAISE EXCEPTION 'The batch Down did not restore the functions, constraint, index and print grants exactly.'; END IF;
              IF to_regclass('advance.company_profiles') IS NOT NULL OR to_regprocedure('advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
                 OR EXISTS(SELECT 1 FROM pg_trigger WHERE tgname='trg_vendor_bills_separate_decider')
                 OR EXISTS(SELECT 1 FROM information_schema.columns WHERE table_name='machine_delivery_challans' AND column_name='VehicleNo') THEN
                RAISE EXCEPTION 'The batch Down left an object behind.'; END IF;
            END $same$;
            """);
        server.Execute("batch-up-again.sql", migrator.GenerateScript(baseline, batch[^1]));
    }
}
