using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260919120000_GovernedInventoryPeriods")]
public sealed class GovernedInventoryPeriods : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN ('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Inventory period governance refuses this database.'; END IF;
             IF to_regclass('advance.inventory_period_events') IS NOT NULL
              OR to_regprocedure('advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text)') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='accounts.inventory-periods')
             THEN RAISE EXCEPTION 'Inventory period package is already or partially installed.'; END IF;
             IF NOT EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER'
              AND "IsActive" AND "Audience"='INTERNAL_EMPLOYEE' AND "IsEmployeeAssignable")
             THEN RAISE EXCEPTION 'Inventory periods require the documented CFO role; MD authority is not substituted.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.financial_periods WHERE upper(btrim("PeriodType"))='INVENTORY'
               AND ("PeriodType"<>'INVENTORY' OR NOT isfinite("StartDate") OR NOT isfinite("EndDate")))
              OR EXISTS(SELECT 1 FROM advance.financial_periods a JOIN advance.financial_periods b
               ON a."CompanyId"=b."CompanyId" AND a."Id"<b."Id" AND a."StartDate"<=b."EndDate" AND a."EndDate">=b."StartDate"
               WHERE a."PeriodType"='INVENTORY' AND b."PeriodType"='INVENTORY')
             THEN RAISE EXCEPTION 'Existing inventory periods require reconciliation; installation will not rewrite dates or reopen periods.'; END IF;
            END $guard$;
            """);
        migrationBuilder.DropIndex(name: "IX_financial_periods_CompanyId_StartDate_EndDate",schema: "advance",table: "financial_periods");
        migrationBuilder.CreateIndex(name: "IX_financial_periods_CompanyId_PeriodType_StartDate_EndDate",schema: "advance",
            table: "financial_periods",columns: ["CompanyId","PeriodType","StartDate","EndDate"],unique: true);
        using var stream = typeof(GovernedInventoryPeriods).Assembly.GetManifestResourceStream("InventoryPeriods.20260919120000.sql")
            ?? throw new InvalidOperationException("Missing inventory period SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('accounts.inventory-periods')::uuid,'accounts.inventory-periods','Accounts','Inventory periods',
              '/accounts/inventory-periods',true,now(),'GovernedInventoryPeriods',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('accounts.inventory-periods:CFO')::uuid,
             'RoleId',(SELECT "Id" FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER'),
             'PageDefinitionId',md5('accounts.inventory-periods')::uuid,
             'CanView',true,'CanApprove',true,'CanViewAuditHistory',true,'CanCreate',false,'CanUpdate',false,'CanSubmit',false,
             'CanVerify',false,'CanReject',false,'CanDeactivate',false,'CanIssue',false,'CanRequestClarification',false,
             'CanRequestRevision',false,'CanResubmit',false,'CanCancel',false,'CanPrint',false,'CanExport',false,'CanDownload',false,
             'CanUploadAttachment',false,'CanReplaceAttachment',false,'CanViewCommercialValues',false,'HasFullControl',false,
             'CreatedAt',now(),'CreatedBy','GovernedInventoryPeriods','Version',0))).*
            FROM (SELECT p.* FROM advance.role_page_permissions p ORDER BY p."Id" LIMIT 1) source;
            DO $guard$ BEGIN
             IF (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('accounts.inventory-periods')::uuid)<>1
             THEN RAISE EXCEPTION 'The explicit CFO period grant was not installed.'; END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(InventoryPeriod20260919AccessSql.Provision);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN ('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Inventory period rollback refuses this database.'; END IF;
             IF to_regclass('advance.inventory_period_events') IS NULL THEN
              RAISE EXCEPTION 'Inventory period package is absent.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.inventory_period_events) THEN
              RAISE EXCEPTION 'Inventory period rollback refuses retained decisions.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.financial_periods GROUP BY "CompanyId","StartDate","EndDate" HAVING count(*)>1)
             THEN RAISE EXCEPTION 'Inventory period rollback refuses dates used by multiple period types.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('accounts.inventory-periods')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('accounts.inventory-periods')::uuid;
            DROP TRIGGER trg_inventory_period_guard ON advance.financial_periods;
            DROP TABLE advance.inventory_period_events;
            DROP FUNCTION advance.inventory_periods_json(uuid);
            DROP FUNCTION advance.inventory_period_json(uuid,uuid);
            DROP FUNCTION advance.close_inventory_period(uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text);
            DROP FUNCTION advance.open_inventory_period(uuid,uuid,text,text,date,date,text,uuid,text,uuid,text,text);
            DROP FUNCTION advance.require_inventory_period_authority(uuid,uuid,uuid,text,uuid,text,text);
            DROP FUNCTION advance.guard_inventory_period_evidence();
            DROP INDEX advance."IX_financial_periods_CompanyId_PeriodType_StartDate_EndDate";
            CREATE UNIQUE INDEX "IX_financial_periods_CompanyId_StartDate_EndDate" ON advance.financial_periods("CompanyId","StartDate","EndDate");
            """);
    }
}
