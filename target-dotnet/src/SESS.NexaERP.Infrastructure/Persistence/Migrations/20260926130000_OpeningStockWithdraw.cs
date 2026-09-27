using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Opening stock Option A (approved 26 September 2026, OS1). A wrong COUNTED or VALUED record can be
/// withdrawn, with a reason and an event, by the Stores Manager who counted it or by the Technical
/// Director. WITHDRAWN frees the company and period for a recount from a new, corrected import (the
/// old import batch stays used). A POSTED record cannot be withdrawn; stock adjustment corrects it.
///
/// The status CHECK gains WITHDRAWN, the period unique index ignores WITHDRAWN rows, and
/// record_opening_stock_count's period check is rewritten from its installed body to ignore them too.
/// The EF model keeps its three-status description (SQL-only change; the snapshot is unchanged).
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926130000_OpeningStockWithdraw")]
public sealed class OpeningStockWithdraw : Migration
{
    private const string CountSignature = "advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text)";
    private const string WithdrawSignature = "advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text)";
    private const string PeriodCheckBefore = "WHERE \"CompanyId\"=p_company AND \"PeriodStart\"=p_from AND \"PeriodEnd\"=p_to)";
    private const string PeriodCheckAfter = "WHERE \"CompanyId\"=p_company AND \"PeriodStart\"=p_from AND \"PeriodEnd\"=p_to AND \"Status\"<>'WITHDRAWN')";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.opening_stocks", "CK_opening_stock_status", "'POSTED'", "'WITHDRAWN'",
            "\"Status\" IN ('COUNTED','VALUED','POSTED','WITHDRAWN')"));
        migrationBuilder.Sql("""
            DROP INDEX advance."IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd";
            CREATE UNIQUE INDEX "IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd" ON advance.opening_stocks("CompanyId","PeriodStart","PeriodEnd") WHERE "Status"<>'WITHDRAWN';
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(CountSignature, (PeriodCheckBefore, PeriodCheckAfter)));
        migrationBuilder.Sql("""
            CREATE FUNCTION advance.withdraw_opening_stock(p_company uuid,p_opening uuid,p_version bigint,p_reason text,p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
            RETURNS TABLE("OpeningStockId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
            DECLARE row advance.opening_stocks%ROWTYPE;
            BEGIN
              SELECT * INTO row FROM advance.opening_stocks WHERE "CompanyId"=p_company AND "Id"=p_opening FOR UPDATE;
              IF NOT FOUND THEN RAISE EXCEPTION 'Opening Stock was not found.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.opening_stock_events e WHERE e."CompanyId"=p_company AND e."OpeningStockId"=p_opening AND e."Action"='WITHDRAW' AND e."CorrelationId"=p_hash) THEN
                RETURN QUERY SELECT p_opening,true; RETURN;
              END IF;
              IF p_role NOT IN ('STORES_MANAGER','TECHNICAL_DIRECTOR')
                 OR NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.Withdraw',p_role) THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Withdrawing Opening Stock requires current FULL or TEMPORARY STORES_MANAGER or TECHNICAL_DIRECTOR authority.';
              END IF;
              IF row."Status" NOT IN ('COUNTED','VALUED') THEN
                RAISE EXCEPTION 'This Opening Stock is %, so it cannot be withdrawn. Only a COUNTED or VALUED record can be; a POSTED one is corrected by a stock adjustment.',row."Status";
              END IF;
              IF row."Version"<>p_version THEN RAISE EXCEPTION 'Opening Stock withdrawal has a stale version. Reload and retry.'; END IF;
              IF p_role='STORES_MANAGER' AND row."CountedByEmployeeId"<>p_actor THEN
                RAISE EXCEPTION 'Only the Stores Manager who recorded this count, or the Technical Director, may withdraw it.';
              END IF;
              IF btrim(coalesce(p_reason,''))='' THEN RAISE EXCEPTION 'A reason is required to withdraw Opening Stock.'; END IF;
              PERFORM set_config('sess.opening_stock_write',txid_current()::text,true);
              UPDATE advance.opening_stocks SET "Status"='WITHDRAWN',"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1 WHERE "Id"=p_opening;
              INSERT INTO advance.opening_stock_events VALUES(gen_random_uuid(),p_company,p_opening,'WITHDRAW',row."Status",'WITHDRAWN',p_actor,p_role,p_assignment,p_type,btrim(p_reason),p_hash,clock_timestamp());
              RETURN QUERY SELECT p_opening,false;
            END $f$;
            REVOKE ALL ON FUNCTION advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC;
            DO $roles$ BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                ALTER FUNCTION advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
                GRANT EXECUTE ON FUNCTION advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
              END IF;
            END $roles$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.opening_stocks WHERE "Status"='WITHDRAWN') THEN
                RAISE EXCEPTION 'Opening Stock withdrawal rollback refuses retained WITHDRAWN evidence.';
              END IF;
            END $guard$;
            DROP FUNCTION advance.withdraw_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text);
            DROP INDEX advance."IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd";
            CREATE UNIQUE INDEX "IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd" ON advance.opening_stocks("CompanyId","PeriodStart","PeriodEnd");
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(CountSignature, (PeriodCheckAfter, PeriodCheckBefore)));
        migrationBuilder.Sql(InstalledFunctionSql.ReplaceCheck("advance.opening_stocks", "CK_opening_stock_status", "'WITHDRAWN'", "'CANCELLED'",
            "\"Status\" IN ('COUNTED','VALUED','POSTED')"));
    }
}
