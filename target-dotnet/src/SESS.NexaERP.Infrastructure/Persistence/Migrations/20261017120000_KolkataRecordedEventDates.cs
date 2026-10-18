using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017120000_KolkataRecordedEventDates")]
public sealed class KolkataRecordedEventDates : Migration
{
    // A fixed installation instant preserves pre-upgrade event interpretation. It is not a caller override.
    private static readonly (string Signature, (string From, string To, int Occurrences)[] Changes)[] Functions =
    [
        ("advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamp with time zone,text,uuid,text,text,text,uuid,text,uuid,text,text)", [
            ("p_fitted_at::date", "advance.business_event_date(p_fitted_at)", 2),
        ]),
        ("advance.guard_governed_job_order()", [
            ("NEW.\"AccountsConfirmedAt\"::date", "advance.business_event_date(NEW.\"AccountsConfirmedAt\")", 1),
        ]),
        ("advance.guard_material_issue_governance()", [
            ("NEW.\"DecidedAt\"::date", "advance.business_event_date(NEW.\"DecidedAt\")", 2),
        ]),
        ("advance.guard_material_return_governance()", [
            ("NEW.\"OccurredAt\"::date", "advance.business_event_date(NEW.\"OccurredAt\")", 2),
            ("NEW.\"AcceptedAt\"::date", "advance.business_event_date(NEW.\"AcceptedAt\")", 2),
        ]),
        ("advance.payment_due_date(timestamp with time zone,text)", [
            ("p_at::date", "advance.business_event_date(p_at)", 2),
        ]),
        ("advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)", [
            ("issue_row.\"IssuedAt\"::date", "advance.business_event_date(issue_row.\"IssuedAt\")", 2),
        ]),
        ("advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)", [
            ("return_row.\"AcceptedAt\"::date", "advance.business_event_date(return_row.\"AcceptedAt\")", 2),
        ]),
        ("advance.stores_p1_configuration_guard()", [
            ("NEW.\"ChangedAt\"::date", "advance.business_event_date(NEW.\"ChangedAt\")", 1),
        ]),
        ("advance.stores_p1_notification_recipient_guard()", [
            ("NEW.\"ResolvedAt\"::date", "advance.business_event_date(NEW.\"ResolvedAt\")", 1),
        ]),
        ("advance.stores_p1_status_history_guard()", [
            ("NEW.\"OccurredAt\"::date", "advance.business_event_date(NEW.\"OccurredAt\")", 1),
        ]),
    ];
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            DO $install$ DECLARE cutover timestamptz:=clock_timestamp(); BEGIN
              EXECUTE format($definition$
                CREATE FUNCTION advance.business_calendar_cutover() RETURNS timestamptz
                LANGUAGE sql IMMUTABLE SECURITY INVOKER SET search_path=pg_catalog,advance
                AS $clock$ SELECT %L::timestamptz $clock$
              $definition$,cutover);
            END $install$;
            CREATE FUNCTION advance.business_event_date(p_at timestamptz) RETURNS date
            LANGUAGE sql IMMUTABLE SECURITY INVOKER SET search_path=pg_catalog,advance
            AS $event$ SELECT CASE WHEN p_at<advance.business_calendar_cutover()
              THEN (p_at AT TIME ZONE 'UTC')::date ELSE (p_at AT TIME ZONE 'Asia/Kolkata')::date END $event$;
            REVOKE ALL ON FUNCTION advance.business_calendar_cutover(),advance.business_event_date(timestamptz) FROM PUBLIC;
            DO $grant$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
                GRANT EXECUTE ON FUNCTION advance.business_calendar_cutover(),advance.business_event_date(timestamptz) TO nexa_erp_runtime;
              END IF;
            END $grant$;
            """));
        Rewrite(builder,false);
    }
    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            DO $guard$ BEGIN
              LOCK TABLE advance.material_issues,advance.material_returns,advance.stock_movements,
                advance.business_rule_configuration_versions,advance.stores_document_status_history,
                advance.job_orders,advance.component_fitments,advance.notification_recipients,advance.vendor_bills IN ACCESS EXCLUSIVE MODE;
              IF EXISTS(SELECT 1 FROM advance.material_issues)
                 OR EXISTS(SELECT 1 FROM advance.material_returns)
                 OR EXISTS(SELECT 1 FROM advance.stock_movements)
                 OR EXISTS(SELECT 1 FROM advance.job_orders WHERE "AccountsConfirmedAt">=advance.business_calendar_cutover())
                 OR EXISTS(SELECT 1 FROM advance.component_fitments)
                 OR EXISTS(SELECT 1 FROM advance.notification_recipients WHERE "ResolvedAt">=advance.business_calendar_cutover())
                 OR EXISTS(SELECT 1 FROM advance.vendor_bills WHERE "DecidedAt">=advance.business_calendar_cutover())
                 OR EXISTS(SELECT 1 FROM advance.business_rule_configuration_versions WHERE "ChangedAt">=advance.business_calendar_cutover())
                 OR EXISTS(SELECT 1 FROM advance.stores_document_status_history WHERE "OccurredAt">=advance.business_calendar_cutover()) THEN
                RAISE EXCEPTION 'Kolkata event rollback refuses retained transaction/calendar evidence; keep the forward migration.';
              END IF;
            END $guard$;
            """));
        Rewrite(builder,true);
        builder.Sql("DROP FUNCTION advance.business_event_date(timestamptz); DROP FUNCTION advance.business_calendar_cutover();");
    }
    private static void Rewrite(MigrationBuilder builder,bool down)
    {
        foreach(var (signature,changes) in Functions)
            builder.Sql(InstalledFunctionSql.Rewrite(signature,true,down
                ? changes.Reverse().Select(c=>(From:c.To,To:c.From,Occurrences:c.Occurrences)).ToArray():changes));
    }
}
