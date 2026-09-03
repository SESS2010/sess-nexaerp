using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockCheckAndPriyaPurchaseScopeV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f6a81af-626b-1fe2-7bfd-6a65e20597f8"),
                columns: new[] { "CanDownload", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4fb4bcd-a855-58f8-4858-8a4e825185dd"),
                columns: new[] { "CanDownload", "CanExport", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { true, true, true, true, true });

            migrationBuilder.Sql(AdvanceSchemaSql.Expand("""
                DO $cluster_guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stock-check and Purchase-scope correction requires PostgreSQL 17 or later.'; END IF;
                  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stock-check and Purchase-scope correction refuses a PostgreSQL administrative database.'; END IF;
                  IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Stock-check and Purchase-scope correction requires the advance schema.'; END IF;
                END $cluster_guard$;
                INSERT INTO __advance_schema__.employee_operational_scopes
                  ("Id","CompanyId","OrganizationId","EmployeeId","DepartmentId","OwnRecordsOnly","AllowsPrivilegedCrossScope","EffectiveFrom","IsActive","Remarks","CreatedAt","CreatedBy","Version")
                SELECT md5('STOCK_CHECK_SCOPE_CORRECTION|'||c."Id"||'|SESS-15|PURCHASE')::uuid,c."Id",c."Code",e."Id",d."Id",false,false,DATE '2026-09-03',true,
                       'Purchase scope required by effective Purchase roles',TIMESTAMPTZ '2026-09-03 00:00:00+00','STOCK_CHECK_SCOPE_CORRECTION',0
                FROM __advance_schema__.companies c JOIN __advance_schema__.employee_company_assignments a ON a."CompanyId"=c."Id" AND a."IsActive" AND a."Status"='ACTIVE'
                JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId" AND e."EmployeeCode"='SESS-15' AND upper(e."Status")='ACTIVE' CROSS JOIN __advance_schema__.departments d
                WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND c."IsActive" AND c."Status"='ACTIVE' AND d."Code"='PURCHASE' AND d."IsActive"
                  AND NOT EXISTS (SELECT 1 FROM __advance_schema__.employee_operational_scopes s WHERE s."CompanyId"=c."Id" AND s."EmployeeId"=e."Id" AND s."DepartmentId"=d."Id" AND s."WarehouseId" IS NULL AND s."RackBinId" IS NULL AND NOT s."OwnRecordsOnly" AND s."IsActive" AND s."EffectiveFrom"<=DATE '2026-09-03' AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=DATE '2026-09-03'));
                DO $acceptance$ BEGIN
                  IF (SELECT count(*) FROM __advance_schema__.companies c WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND c."IsActive" AND EXISTS(SELECT 1 FROM __advance_schema__.employee_operational_scopes s JOIN __advance_schema__.employees e ON e."Id"=s."EmployeeId" JOIN __advance_schema__.departments d ON d."Id"=s."DepartmentId" WHERE s."CompanyId"=c."Id" AND e."EmployeeCode"='SESS-15' AND d."Code"='PURCHASE' AND s."IsActive" AND s."EffectiveFrom"<=DATE '2026-09-03' AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=DATE '2026-09-03')))<>2 THEN RAISE EXCEPTION 'PRIYA SESS-15 must have an effective Purchase scope in both companies.'; END IF;
                END $acceptance$;
                """));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AdvanceSchemaSql.Expand("""
                DO $cluster_guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stock-check and Purchase-scope rollback requires PostgreSQL 17 or later.'; END IF;
                  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stock-check and Purchase-scope rollback refuses a PostgreSQL administrative database.'; END IF;
                  IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Stock-check and Purchase-scope rollback requires the advance schema.'; END IF;
                  IF EXISTS (SELECT 1 FROM __advance_schema__.employee_operational_scopes WHERE "CreatedBy"='STOCK_CHECK_SCOPE_CORRECTION' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)) THEN RAISE EXCEPTION 'Correction scope rows changed after migration; refusing destructive rollback.'; END IF;
                END $cluster_guard$;
                ALTER TABLE __advance_schema__.employee_operational_scopes DISABLE TRIGGER trg_rev869a_scope_version_guard;
                DELETE FROM __advance_schema__.employee_operational_scopes WHERE "CreatedBy"='STOCK_CHECK_SCOPE_CORRECTION';
                ALTER TABLE __advance_schema__.employee_operational_scopes ENABLE TRIGGER trg_rev869a_scope_version_guard;
                """));
            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f6a81af-626b-1fe2-7bfd-6a65e20597f8"),
                columns: new[] { "CanDownload", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4fb4bcd-a855-58f8-4858-8a4e825185dd"),
                columns: new[] { "CanDownload", "CanExport", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { false, false, false, false, false });
        }
    }
}
