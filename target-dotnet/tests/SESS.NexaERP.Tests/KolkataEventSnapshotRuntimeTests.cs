using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void KolkataEventSnapshotMigrationRoundTripsWithoutRewritingLegacyRowsOrAuthority()
    {
        const string baseline = "20261017100000_KolkataCurrentBusinessDates";
        const string head = "20261017110000_KolkataEventSnapshotDates";
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("event-baseline.sql", migrator.GenerateScript("0", baseline));
        server.Execute("event-before.sql", """
            CREATE TABLE public.event_function_baseline AS
              SELECT p.oid,pg_get_functiondef(p.oid) definition,p.proowner,p.prosecdef,
                p.proacl::text acl,p.proconfig::text configuration,p.prolang,p.provolatile,p.proparallel
              FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='advance' AND p.prokind='f';
            """);
        const string authority = """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.event_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                WHERE p.proowner IS DISTINCT FROM b.proowner OR p.prosecdef IS DISTINCT FROM b.prosecdef
                  OR p.proacl::text IS DISTINCT FROM b.acl OR p.proconfig::text IS DISTINCT FROM b.configuration
                  OR p.prolang IS DISTINCT FROM b.prolang OR p.provolatile IS DISTINCT FROM b.provolatile
                  OR p.proparallel IS DISTINCT FROM b.proparallel) THEN
                RAISE EXCEPTION 'Event date repair changed installed function authority';
              END IF;
            END $assert$;
            """;
        const string policies = """
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                  WHERE n.nspname='advance' AND p.proname IN ('rev869b_commercial_snapshot_reconciles',
                    'rev869b_guard_authoritative_transition','stores_p2_goods_receipt_line_guard','finalize_goods_receipt')
                    AND p.prosrc LIKE '%ASIA_KOLKATA_V1%')<>4 THEN
                RAISE EXCEPTION 'Event date migration did not install all four policy contracts';
              END IF;
              IF EXISTS(SELECT 1 FROM public.event_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                  WHERE p.proname NOT IN ('rev869b_commercial_snapshot_reconciles',
                    'rev869b_guard_authoritative_transition','stores_p2_goods_receipt_line_guard','finalize_goods_receipt')
                    AND pg_get_functiondef(p.oid)<>b.definition) THEN
                RAISE EXCEPTION 'Event policy migration changed unrelated function bodies';
              END IF;
            END $assert$;
            """;
        server.Execute("event-up.sql", migrator.GenerateScript(baseline, head));
        server.Execute("event-up-contracts.sql", authority + policies);
        server.Execute("event-down.sql", migrator.GenerateScript(head, baseline));
        server.Execute("event-down-contracts.sql", authority + """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.event_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                  WHERE pg_get_functiondef(p.oid)<>b.definition) THEN
                RAISE EXCEPTION 'Event policy Down did not restore all installed definitions exactly';
              END IF;
            END $assert$;
            """);
        server.Execute("event-up-again.sql", migrator.GenerateScript(baseline, head));
        server.Execute("event-up-contracts-again.sql", authority + policies);
    }
}