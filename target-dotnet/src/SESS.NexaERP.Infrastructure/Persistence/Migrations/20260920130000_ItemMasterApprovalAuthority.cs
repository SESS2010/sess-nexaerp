using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Item permission move, decided by the Technical Director. The item approval and merge
/// services already require STORES_MANAGER or PURCHASE_MANAGER, but the masters.items page
/// grants belonged to the retired heads (STORE_HEAD, PURCHASE_HEAD) and the directors, so
/// neither manager could reach the operation: 14 reachability diagnostics. The two managers
/// now hold the item master operating grant including approval. The Technical Director keeps
/// the page grant for the two exceptions the service reserves to that role: a correction to
/// a record more than one month old (since creation) and a duplicate merge.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920130000_ItemMasterApprovalAuthority")]
public sealed class ItemMasterApprovalAuthority : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE required record; role_id uuid; page_id uuid; grant_id uuid; prior jsonb; after_row jsonb;
            BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                        WHERE r."Code" IN ('STORE_HEAD','PURCHASE_HEAD') AND a."EffectiveTo" IS NULL) THEN
                RAISE EXCEPTION 'A legacy head role is still assigned; reconcile it before moving the item master grant.';
              END IF;
              FOR required IN SELECT * FROM (VALUES ('STORES_MANAGER'),('PURCHASE_MANAGER')) v(role_code)
              LOOP
                SELECT "Id" INTO STRICT role_id FROM advance.roles WHERE "Code"=required.role_code AND "IsActive";
                SELECT "Id" INTO STRICT page_id FROM advance.page_definitions WHERE "PageKey"='masters.items' AND "IsActive";
                SELECT to_jsonb(p),p."Id" INTO prior,grant_id FROM advance.role_page_permissions p
                  WHERE p."RoleId"=role_id AND p."PageDefinitionId"=page_id;
                IF prior IS NULL THEN
                  grant_id := md5('item-master-approval-authority:'||required.role_code||':masters.items')::uuid;
                  INSERT INTO advance.role_page_permissions
                    ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
                     "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
                     "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
                     "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
                     "CreatedAt","CreatedBy","Version")
                  VALUES(grant_id,role_id,page_id,
                    true,true,true,true,false,
                    false,true,true,true,true,true,
                    false,false,true,true,true,true,
                    false,false,true,false,clock_timestamp(),'ItemMasterApprovalAuthority',0);
                ELSE
                  UPDATE advance.role_page_permissions SET
                    "CanView"=true,"CanCreate"=true,"CanUpdate"=true,"CanSubmit"=true,"CanApprove"=true,"CanReject"=true,
                    "CanRequestClarification"=true,"CanRequestRevision"=true,"CanResubmit"=true,"CanPrint"=true,"CanDownload"=true,
                    "CanExport"=true,"CanUploadAttachment"=true,"CanViewAuditHistory"=true,
                    "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"='ItemMasterApprovalAuthority'
                  WHERE "Id"=grant_id;
                END IF;
                SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','GrantItemMasterAuthority','RolePagePermission',grant_id::text,
                  session_user,'','Success','ItemMasterApprovalAuthority:'||required.role_code||':masters.items',
                  prior::text,after_row::text,clock_timestamp(),'ItemMasterApprovalAuthority',0);
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
              IF (SELECT count(*) FROM advance.audit_logs a
                WHERE a."CreatedBy"='ItemMasterApprovalAuthority' AND a."Action"='GrantItemMasterAuthority'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='ItemMasterApprovalAuthority'
                    AND r."Action"='RevertItemMasterAuthority' AND r."CorrelationId"=a."Id"::text)) <> 2 THEN
                RAISE EXCEPTION 'Item master authority rollback requires two intact unreverted receipts';
              END IF;
              FOR receipt IN SELECT a.* FROM advance.audit_logs a
                WHERE a."CreatedBy"='ItemMasterApprovalAuthority' AND a."Action"='GrantItemMasterAuthority'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='ItemMasterApprovalAuthority'
                    AND r."Action"='RevertItemMasterAuthority' AND r."CorrelationId"=a."Id"::text)
              LOOP
                grant_id := receipt."EntityId"::uuid;
                SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                  RAISE EXCEPTION 'Item master authority rollback refuses a changed or missing permission: %',grant_id;
                END IF;
                prior := receipt."BeforeJson"::jsonb;
                IF prior IS NULL THEN
                  DELETE FROM advance.role_page_permissions WHERE "Id"=grant_id;
                  after_row := NULL;
                ELSE
                  UPDATE advance.role_page_permissions p SET
                    "CanView"=(prior->>'CanView')::boolean,"CanCreate"=(prior->>'CanCreate')::boolean,"CanUpdate"=(prior->>'CanUpdate')::boolean,
                    "CanSubmit"=(prior->>'CanSubmit')::boolean,"CanApprove"=(prior->>'CanApprove')::boolean,"CanReject"=(prior->>'CanReject')::boolean,
                    "CanRequestClarification"=(prior->>'CanRequestClarification')::boolean,"CanRequestRevision"=(prior->>'CanRequestRevision')::boolean,
                    "CanResubmit"=(prior->>'CanResubmit')::boolean,"CanPrint"=(prior->>'CanPrint')::boolean,"CanDownload"=(prior->>'CanDownload')::boolean,
                    "CanExport"=(prior->>'CanExport')::boolean,"CanUploadAttachment"=(prior->>'CanUploadAttachment')::boolean,
                    "CanViewAuditHistory"=(prior->>'CanViewAuditHistory')::boolean,
                    "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,
                    "UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                  SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                  IF after_row IS DISTINCT FROM prior THEN
                    RAISE EXCEPTION 'Item master authority rollback failed to restore the exact prior row: %',grant_id;
                  END IF;
                END IF;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','RevertItemMasterAuthority','RolePagePermission',grant_id::text,
                  session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,
                  clock_timestamp(),'ItemMasterApprovalAuthority',0);
              END LOOP;
            END $revert$;
            """));
    }
}
