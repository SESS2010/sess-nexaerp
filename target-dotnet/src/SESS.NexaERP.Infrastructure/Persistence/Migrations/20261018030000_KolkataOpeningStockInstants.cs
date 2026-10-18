using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>Prospective opening custody/FIFO dates use the start of the Kolkata business day.
/// Old rows, quantities, costs, posting dates and actor/permission contracts are retained.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20261018030000_KolkataOpeningStockInstants")]
public sealed class KolkataOpeningStockInstants : Migration
{
    private const string Signature = "advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)";
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf($$"""
            LOCK TABLE advance.opening_stocks,advance.opening_stock_events,
              advance.fifo_inventory_cost_layers,advance.stock_movements,
              advance.inventory_custody_assignments IN ACCESS EXCLUSIVE MODE;
            CREATE TABLE advance.r1_opening_business_date_backup(
              singleton boolean PRIMARY KEY CHECK(singleton),
              before_value text NOT NULL,after_value text NOT NULL,
              fifo_ids uuid[] NOT NULL,custody_ids uuid[] NOT NULL,authorization_ids uuid[] NOT NULL);
            REVOKE ALL ON advance.r1_opening_business_date_backup FROM PUBLIC;
            DO $owner$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                ALTER TABLE advance.r1_opening_business_date_backup OWNER TO nexa_erp_owner;
              END IF;
            END $owner$;
            INSERT INTO advance.r1_opening_business_date_backup
              SELECT true,replace(pg_get_functiondef(p.oid),E'\r\n',E'\n'),'',
                ARRAY(SELECT "Id" FROM advance.fifo_inventory_cost_layers WHERE "OpeningStockLineId" IS NOT NULL),
                ARRAY(SELECT DISTINCT "CustodyAssignmentId" FROM advance.stock_movements
                  WHERE "OpeningStockLineId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL),
                ARRAY(SELECT "Id" FROM advance.opening_stock_events WHERE "Action"='AUTHORIZE')
              FROM pg_proc p JOIN pg_language l ON l.oid=p.prolang
              WHERE p.oid='{{Signature}}'::regprocedure AND p.prosecdef AND l.lanname='plpgsql';
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM advance.r1_opening_business_date_backup)<>1 THEN
                RAISE EXCEPTION 'Opening-stock business date requires the existing SECURITY DEFINER authorization contract.';
              END IF;
            END $guard$;
            """));
        builder.Sql(InstalledFunctionSql.Rewrite(Signature, true,
            ("row.\"PeriodEnd\"::timestamp AT TIME ZONE 'UTC'", "row.\"PeriodEnd\"::timestamp AT TIME ZONE 'Asia/Kolkata'", 2)));
        builder.Sql(MigrationText.Lf($$"""
            UPDATE advance.r1_opening_business_date_backup SET after_value=
              replace(pg_get_functiondef('{{Signature}}'::regprocedure),E'\r\n',E'\n');
            """));
        // The existing function signature/owner/SECURITY DEFINER/search_path/runtime ACL are preserved.
        // Installer provisioning already includes this function; no new function or grant is introduced.
    }

    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf($$"""
            LOCK TABLE advance.opening_stocks,advance.opening_stock_events,
              advance.fifo_inventory_cost_layers,advance.stock_movements,
              advance.inventory_custody_assignments IN ACCESS EXCLUSIVE MODE;
            DO $restore$ DECLARE saved record; BEGIN
              IF (SELECT count(*) FROM advance.r1_opening_business_date_backup)<>1 THEN
                RAISE EXCEPTION 'Opening-stock date rollback refuses missing or ambiguous retained definition.';
              END IF;
              SELECT * INTO STRICT saved FROM advance.r1_opening_business_date_backup;
              IF replace(pg_get_functiondef('{{Signature}}'::regprocedure),E'\r\n',E'\n')
                IS DISTINCT FROM saved.after_value THEN
                RAISE EXCEPTION 'Opening-stock date rollback refuses a changed installed definition.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers
                    WHERE "OpeningStockLineId" IS NOT NULL AND NOT("Id"=ANY(saved.fifo_ids)))
                 OR EXISTS(SELECT 1 FROM advance.stock_movements WHERE "OpeningStockLineId" IS NOT NULL
                    AND "CustodyAssignmentId" IS NOT NULL AND NOT("CustodyAssignmentId"=ANY(saved.custody_ids)))
                 OR EXISTS(SELECT 1 FROM advance.opening_stock_events
                    WHERE "Action"='AUTHORIZE' AND NOT("Id"=ANY(saved.authorization_ids))) THEN
                RAISE EXCEPTION 'Opening-stock date rollback refuses retained opening-stock date evidence.';
              END IF;
              EXECUTE saved.before_value;
            END $restore$;
            DROP TABLE advance.r1_opening_business_date_backup;
            """));
    }
}
