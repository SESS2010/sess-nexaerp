using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260918092000_StoresComponentFitmentRead")]
public sealed class StoresComponentFitmentRead : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE required record; role_id uuid; page_id uuid; grant_id uuid; prior jsonb; after_row jsonb;
            BEGIN
              FOR required IN SELECT * FROM (VALUES
                ('STORES_ASSISTANT','production.component-fitments',false),
                ('STORES_EXECUTIVE','production.component-fitments',false),
                ('STORES_MANAGER','production.component-fitments',false)) v(role_code,page_key,download_required)
              LOOP
                SELECT "Id" INTO STRICT role_id FROM advance.roles WHERE "Code"=required.role_code AND "IsActive";
                SELECT "Id" INTO STRICT page_id FROM advance.page_definitions WHERE "PageKey"=required.page_key AND "IsActive";
                SELECT to_jsonb(p),p."Id" INTO prior,grant_id FROM advance.role_page_permissions p
                  WHERE p."RoleId"=role_id AND p."PageDefinitionId"=page_id;
                IF prior IS NULL THEN
                  grant_id := md5('stores-fitment-read:'||required.role_code||':'||required.page_key)::uuid;
                  INSERT INTO advance.role_page_permissions
                    ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
                     "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
                     "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
                     "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
                     "CreatedAt","CreatedBy","Version")
                  VALUES(grant_id,role_id,page_id,
                    true,false,false,false,false,
                    false,false,false,false,false,false,
                    false,false,false,required.download_required,false,false,
                    false,false,false,false,clock_timestamp(),'StoresComponentFitmentRead',0);
                ELSIF NOT ((prior->>'CanView')::boolean OR (required.page_key<>'purchase.vendor-quotations' AND (prior->>'HasFullControl')::boolean))
                  OR (required.download_required AND NOT (prior->>'CanDownload')::boolean) THEN
                  UPDATE advance.role_page_permissions SET "CanView"=true,"CanDownload"="CanDownload" OR required.download_required,"Version"="Version"+1,
                    "UpdatedAt"=clock_timestamp(),"UpdatedBy"='StoresComponentFitmentRead' WHERE "Id"=grant_id;
                END IF;
                SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','GrantRequiredRead','RolePagePermission',grant_id::text,
                  session_user,'','Success','StoresComponentFitmentRead:'||required.role_code||':'||required.page_key,
                  prior::text,after_row::text,clock_timestamp(),'StoresComponentFitmentRead',0);
              END LOOP;
            END $grant$;
            """.Replace("\r\n", "\n", StringComparison.Ordinal));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $revert$
            DECLARE receipt record; current_row jsonb; prior jsonb; after_row jsonb; grant_id uuid;
            BEGIN
              IF (SELECT count(*) FROM advance.audit_logs a
                WHERE a."CreatedBy"='StoresComponentFitmentRead' AND a."Action"='GrantRequiredRead'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='StoresComponentFitmentRead'
                    AND r."Action"='RevertRequiredRead' AND r."CorrelationId"=a."Id"::text)) <> 3 THEN
                RAISE EXCEPTION 'Required read-grant rollback requires three intact unreverted receipts';
              END IF;
              FOR receipt IN SELECT a.* FROM advance.audit_logs a
                WHERE a."CreatedBy"='StoresComponentFitmentRead' AND a."Action"='GrantRequiredRead'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='StoresComponentFitmentRead'
                    AND r."Action"='RevertRequiredRead' AND r."CorrelationId"=a."Id"::text)
              LOOP
                grant_id := receipt."EntityId"::uuid;
                SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                  RAISE EXCEPTION 'Required read-grant rollback refuses a changed or missing permission: %',grant_id;
                END IF;
                prior := receipt."BeforeJson"::jsonb;
                IF prior IS NULL THEN
                  DELETE FROM advance.role_page_permissions WHERE "Id"=grant_id;
                  after_row := NULL;
                ELSE
                  UPDATE advance.role_page_permissions SET "CanView"=(prior->>'CanView')::boolean,"CanDownload"=(prior->>'CanDownload')::boolean,
                    "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,
                    "UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                  SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                  IF after_row IS DISTINCT FROM prior THEN
                    RAISE EXCEPTION 'Required read-grant rollback failed to restore the exact prior row: %',grant_id;
                  END IF;
                END IF;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','RevertRequiredRead','RolePagePermission',grant_id::text,
                  session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,
                  clock_timestamp(),'StoresComponentFitmentRead',0);
              END LOOP;
            END $revert$;
            """.Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
