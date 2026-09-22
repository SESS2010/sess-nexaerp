using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Decision of the Technical Director, 20 September 2026: the role that can view a report can
/// export it. The export right had stayed with the retired heads (STORE_HEAD, PURCHASE_HEAD,
/// ACCOUNTS_HEAD, QC_HEAD) when their successors received view only. Every employee-assignable
/// role with view on a reports.* page receives export; each change is audited and rolled back to
/// the exact prior row. Portal roles (customer, vendor) and non-assignable roles are untouched.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920180000_ReportExportFollowsView")]
public sealed class ReportExportFollowsView : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE target record; prior jsonb; after_row jsonb;
            BEGIN
              FOR target IN
                SELECT p."Id" grant_id, r."Code" role_code, d."PageKey" page_key
                FROM advance.role_page_permissions p
                JOIN advance.roles r ON r."Id"=p."RoleId" AND r."IsActive" AND r."IsEmployeeAssignable"
                JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId" AND d."IsActive" AND d."PageKey" LIKE 'reports.%'
                WHERE p."CanView" AND NOT p."CanExport" AND NOT p."HasFullControl"
                ORDER BY d."PageKey", r."Code"
              LOOP
                SELECT to_jsonb(p) INTO STRICT prior FROM advance.role_page_permissions p WHERE p."Id"=target.grant_id;
                UPDATE advance.role_page_permissions SET "CanExport"=true,"Version"="Version"+1,
                  "UpdatedAt"=clock_timestamp(),"UpdatedBy"='ReportExportFollowsView' WHERE "Id"=target.grant_id;
                SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=target.grant_id;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','GrantReportExport','RolePagePermission',target.grant_id::text,
                  session_user,'','Success','ReportExportFollowsView:'||target.role_code||':'||target.page_key,
                  prior::text,after_row::text,clock_timestamp(),'ReportExportFollowsView',0);
              END LOOP;
            END $grant$;
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $revert$
            DECLARE receipt record; current_row jsonb; prior jsonb; after_row jsonb; grant_id uuid;
            BEGIN
              FOR receipt IN SELECT a.* FROM advance.audit_logs a
                WHERE a."CreatedBy"='ReportExportFollowsView' AND a."Action"='GrantReportExport'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='ReportExportFollowsView'
                    AND r."Action"='RevertReportExport' AND r."CorrelationId"=a."Id"::text)
              LOOP
                grant_id := receipt."EntityId"::uuid;
                SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                  RAISE EXCEPTION 'Report export rollback refuses a changed or missing permission: %',grant_id;
                END IF;
                prior := receipt."BeforeJson"::jsonb;
                UPDATE advance.role_page_permissions SET "CanExport"=(prior->>'CanExport')::boolean,
                  "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,
                  "UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF after_row IS DISTINCT FROM prior THEN
                  RAISE EXCEPTION 'Report export rollback failed to restore the exact prior row: %',grant_id;
                END IF;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','RevertReportExport','RolePagePermission',grant_id::text,
                  session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,
                  clock_timestamp(),'ReportExportFollowsView',0);
              END LOOP;
            END $revert$;
            """));
    }
}
