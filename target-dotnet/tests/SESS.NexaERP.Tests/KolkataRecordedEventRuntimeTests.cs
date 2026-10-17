using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void KolkataRecordedEventMigrationPreservesPreUpgradeDatesAndRoundTripsAuthority()
    {
        const string baseline = "20261017110000_KolkataEventSnapshotDates";
        const string head = "20261017120000_KolkataRecordedEventDates";
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
            DO $assert$ DECLARE cutover timestamptz; BEGIN
              SELECT advance.business_calendar_cutover() INTO cutover;
              IF advance.business_event_date(cutover-interval '1 second') IS DISTINCT FROM ((cutover-interval '1 second') AT TIME ZONE 'UTC')::date
                 OR advance.business_event_date(cutover) IS DISTINCT FROM (cutover AT TIME ZONE 'Asia/Kolkata')::date
                 OR advance.business_event_date(NULL) IS NOT NULL THEN
                RAISE EXCEPTION 'Recorded event calendar failed its historical cutover contract';
              END IF;
              IF (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='advance'
                  AND p.proname IN ('confirm_component_fitment','guard_governed_job_order','guard_material_issue_governance','guard_material_return_governance','payment_due_date','post_material_issue_custody','post_material_return_acceptance','stores_p1_configuration_guard','stores_p1_notification_recipient_guard','stores_p1_status_history_guard') AND p.prosrc LIKE '%advance.business_event_date(%')<>10 THEN
                RAISE EXCEPTION 'Recorded event calendar did not cover all ten functions';
              END IF;
              IF EXISTS(SELECT 1 FROM public.event_function_baseline b JOIN pg_proc p ON p.oid=b.oid
                  WHERE p.proname NOT IN ('confirm_component_fitment','guard_governed_job_order','guard_material_issue_governance','guard_material_return_governance','payment_due_date','post_material_issue_custody','post_material_return_acceptance','stores_p1_configuration_guard','stores_p1_notification_recipient_guard','stores_p1_status_history_guard') AND pg_get_functiondef(p.oid)<>b.definition) THEN
                RAISE EXCEPTION 'Recorded event migration changed unrelated definitions';
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