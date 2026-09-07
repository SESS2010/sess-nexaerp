using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureNarrenQcManagerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AdvanceSchemaSql.Expand("""
                DO $cluster_guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires PostgreSQL 17 or later.'; END IF;
                  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Narren QC_MANAGER correction refuses a PostgreSQL administrative database.'; END IF;
                  IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires the advance schema.'; END IF;
                  IF (SELECT count(*) FROM __advance_schema__.companies WHERE "Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND "IsActive" AND "Status"='ACTIVE')<>2 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires both active SESS companies.'; END IF;
                  IF (SELECT count(*) FROM __advance_schema__.employees WHERE "EmployeeCode"='SESS-33' AND "EmployeeName"='NARREN S' AND upper("Status")='ACTIVE')<>1 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires the unique active employee SESS-33 NARREN S.'; END IF;
                  IF (SELECT count(*) FROM __advance_schema__.roles WHERE "Code"='QC_MANAGER' AND "IsActive")<>1 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires the active QC_MANAGER role.'; END IF;
                  IF (SELECT count(*) FROM __advance_schema__.employee_company_assignments a JOIN __advance_schema__.companies c ON c."Id"=a."CompanyId" JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId" WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND e."EmployeeCode"='SESS-33' AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=DATE '2026-09-04' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04'))<>2 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction requires an effective SESS-33 company assignment in both companies.'; END IF;
                END $cluster_guard$;
                INSERT INTO __advance_schema__.employee_role_assignments
                  ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
                SELECT md5('NARREN_QC_MANAGER_CORRECTION|'||c."Id"||'|SESS-33')::uuid,c."Id",e."Id",r."Id",DATE '2026-09-04','SeedApproved',
                       'Restores the QC_MANAGER assignment required by QC service role checks',TIMESTAMPTZ '2026-09-04 00:00:00+00','NARREN_QC_MANAGER_CORRECTION',0
                FROM __advance_schema__.companies c CROSS JOIN __advance_schema__.employees e CROSS JOIN __advance_schema__.roles r
                WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND c."IsActive" AND c."Status"='ACTIVE'
                  AND e."EmployeeCode"='SESS-33' AND upper(e."Status")='ACTIVE' AND r."Code"='QC_MANAGER' AND r."IsActive"
                  AND NOT EXISTS (
                    SELECT 1 FROM __advance_schema__.employee_role_assignments a
                    WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=e."Id" AND a."RoleId"=r."Id"
                      AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND a."EffectiveFrom"<=DATE '2026-09-04'
                      AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04'));
                DO $acceptance$
                BEGIN
                  IF (SELECT count(*) FROM __advance_schema__.companies c WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND EXISTS (
                    SELECT 1 FROM __advance_schema__.employee_role_assignments a
                    JOIN __advance_schema__.employees e ON e."Id"=a."EmployeeId"
                    JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
                    WHERE a."CompanyId"=c."Id" AND e."EmployeeCode"='SESS-33' AND r."Code"='QC_MANAGER'
                      AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND a."EffectiveFrom"<=DATE '2026-09-04'
                      AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-04')))<>2 THEN
                    RAISE EXCEPTION 'NARREN S SESS-33 must hold effective QC_MANAGER in both companies.';
                  END IF;
                END $acceptance$;
                """));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AdvanceSchemaSql.Expand("""
                DO $cluster_guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Narren QC_MANAGER correction rollback requires PostgreSQL 17 or later.'; END IF;
                  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Narren QC_MANAGER correction rollback refuses a PostgreSQL administrative database.'; END IF;
                  IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Narren QC_MANAGER correction rollback requires the advance schema.'; END IF;
                  IF EXISTS (SELECT 1 FROM __advance_schema__.employee_role_assignments WHERE "CreatedBy"='NARREN_QC_MANAGER_CORRECTION' AND ("UpdatedBy" IS NOT NULL OR "Version"<>0)) THEN RAISE EXCEPTION 'Narren QC_MANAGER correction rows changed after migration; refusing destructive rollback.'; END IF;
                END $cluster_guard$;
                DELETE FROM __advance_schema__.employee_role_assignments WHERE "CreatedBy"='NARREN_QC_MANAGER_CORRECTION';
                """));
        }
    }
}
