using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void KolkataStockAdjustmentInstantsPreserveMetadataAndRoundTripWithoutHistory()
    {
        const string head = "20261018031500_KolkataStockAdjustmentInstants";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var previous = migrations[Array.IndexOf(migrations, head) - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("adjustment-ist-before.sql", migrator.GenerateScript("0", previous));
        server.Execute("adjustment-ist-snapshot.sql", """
            CREATE TABLE public.adjustment_instant_function_before AS SELECT to_jsonb(p) data FROM pg_proc p
              WHERE p.oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure;
            CREATE TABLE public.adjustment_instant_grants_before AS SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") data
              FROM advance.role_page_permissions p;
            """);
        var up = migrator.GenerateScript(previous, head);
        var down = migrator.GenerateScript(head, previous);
        server.Execute("adjustment-ist-up.sql", up);
        server.Execute("adjustment-ist-metadata.sql", """
            DO $assert$ DECLARE body text; BEGIN
              SELECT prosrc INTO STRICT body FROM pg_proc
                WHERE oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure;
              IF EXISTS(SELECT 1 FROM public.adjustment_instant_function_before b CROSS JOIN pg_proc p
                    WHERE p.oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure
                      AND(to_jsonb(p)-'prosrc') IS DISTINCT FROM(b.data-'prosrc'))
                 OR (SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") FROM advance.role_page_permissions p)
                      IS DISTINCT FROM(SELECT data FROM public.adjustment_instant_grants_before)
                 OR position('adj."EffectiveDate"::timestamp AT TIME ZONE ''UTC''' IN body)>0
                 OR (length(body)-length(replace(body,'adj."EffectiveDate"::timestamp AT TIME ZONE ''Asia/Kolkata''','')))
                      /length('adj."EffectiveDate"::timestamp AT TIME ZONE ''Asia/Kolkata''')<>2 THEN
                RAISE EXCEPTION 'Adjustment IST migration changed metadata/grants or failed either date anchor.';
              END IF;
            END $assert$;
            DO $drift$ DECLARE changed text; BEGIN
              SELECT pg_get_functiondef('advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure)
                INTO STRICT changed;
              EXECUTE replace(changed,'BEGIN',E'BEGIN\n -- disposable adjustment drift\n');
            END $drift$;
            """);
        server.AssertRejected("adjustment-ist-drift-down.sql", down, "refuses a changed installed definition");
        server.Execute("adjustment-ist-restore-after.sql", """
            DO $restore$ DECLARE saved text; BEGIN
              SELECT after_value INTO STRICT saved FROM advance.r1_adjustment_business_date_backup;
              EXECUTE saved;
            END $restore$;
            """);
        server.Execute("adjustment-ist-down.sql", down);
        server.Execute("adjustment-ist-restored-metadata.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.adjustment_instant_function_before b CROSS JOIN pg_proc p
                  WHERE p.oid='advance.post_stock_adjustment(uuid,uuid,uuid,text,uuid,text,text,text,text)'::regprocedure
                    AND to_jsonb(p) IS DISTINCT FROM b.data)
                 OR to_regclass('advance.r1_adjustment_business_date_backup') IS NOT NULL THEN
                RAISE EXCEPTION 'Adjustment IST rollback did not exactly restore the prior function.';
              END IF;
            END $assert$;
            """);
        server.Execute("adjustment-ist-reapply.sql", up);
    }
}
