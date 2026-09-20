using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Finding #18. The operating grant on the warehouse and rack/bin masters belonged to the
/// legacy role STORE_HEAD, which no employee holds; STORES_MANAGER had no grant at all.
/// A fresh company therefore could not create the warehouse or the racks that an AVAILABLE
/// condition location needs, and every new installation stopped before its first receipt.
/// STORES_MANAGER receives the same operating grant STORE_HEAD held (plus audit history);
/// STORES_EXECUTIVE receives view. Approval stays with the Technical Director.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920090000_StoresWarehouseRackGrants")]
public sealed class StoresWarehouseRackGrants : Migration
{
    internal const string Actor = "StoresWarehouseRackGrants";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE required record; role_id uuid; page_id uuid; grant_id uuid; prior jsonb; after_row jsonb; operate boolean;
            BEGIN
              IF EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
                        WHERE r."Code"='STORE_HEAD' AND a."EffectiveTo" IS NULL) THEN
                RAISE EXCEPTION 'STORE_HEAD is still assigned; reconcile that legacy role before moving its Stores grant.';
              END IF;
              FOR required IN SELECT * FROM (VALUES
                ('STORES_MANAGER','masters.warehouses','OPERATE'),
                ('STORES_MANAGER','masters.rack-bins','OPERATE'),
                ('STORES_EXECUTIVE','masters.warehouses','VIEW')) v(role_code,page_key,kind)
              LOOP
                operate := required.kind='OPERATE';
                SELECT "Id" INTO STRICT role_id FROM advance.roles WHERE "Code"=required.role_code AND "IsActive";
                SELECT "Id" INTO STRICT page_id FROM advance.page_definitions WHERE "PageKey"=required.page_key AND "IsActive";
                SELECT to_jsonb(p),p."Id" INTO prior,grant_id FROM advance.role_page_permissions p
                  WHERE p."RoleId"=role_id AND p."PageDefinitionId"=page_id;
                IF prior IS NULL THEN
                  grant_id := md5('stores-warehouse-rack-grant:'||required.role_code||':'||required.page_key)::uuid;
                  INSERT INTO advance.role_page_permissions
                    ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
                     "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
                     "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
                     "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
                     "CreatedAt","CreatedBy","Version")
                  VALUES(grant_id,role_id,page_id,
                    true,operate,operate,operate,false,
                    false,false,false,false,false,operate,
                    false,false,true,true,operate,operate,
                    false,false,operate,false,clock_timestamp(),'StoresWarehouseRackGrants',0);
                ELSE
                  UPDATE advance.role_page_permissions SET
                    "CanView"=true,"CanCreate"="CanCreate" OR operate,"CanUpdate"="CanUpdate" OR operate,
                    "CanSubmit"="CanSubmit" OR operate,"CanResubmit"="CanResubmit" OR operate,
                    "CanPrint"=true,"CanDownload"=true,"CanExport"="CanExport" OR operate,
                    "CanUploadAttachment"="CanUploadAttachment" OR operate,"CanViewAuditHistory"="CanViewAuditHistory" OR operate,
                    "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"='StoresWarehouseRackGrants'
                  WHERE "Id"=grant_id;
                END IF;
                SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','GrantStoresTopology','RolePagePermission',grant_id::text,
                  session_user,'','Success','StoresWarehouseRackGrants:'||required.role_code||':'||required.page_key,
                  prior::text,after_row::text,clock_timestamp(),'StoresWarehouseRackGrants',0);
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
                WHERE a."CreatedBy"='StoresWarehouseRackGrants' AND a."Action"='GrantStoresTopology'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='StoresWarehouseRackGrants'
                    AND r."Action"='RevertStoresTopology' AND r."CorrelationId"=a."Id"::text)) <> 3 THEN
                RAISE EXCEPTION 'Stores topology grant rollback requires three intact unreverted receipts';
              END IF;
              FOR receipt IN SELECT a.* FROM advance.audit_logs a
                WHERE a."CreatedBy"='StoresWarehouseRackGrants' AND a."Action"='GrantStoresTopology'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='StoresWarehouseRackGrants'
                    AND r."Action"='RevertStoresTopology' AND r."CorrelationId"=a."Id"::text)
              LOOP
                grant_id := receipt."EntityId"::uuid;
                SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                  RAISE EXCEPTION 'Stores topology grant rollback refuses a changed or missing permission: %',grant_id;
                END IF;
                prior := receipt."BeforeJson"::jsonb;
                IF prior IS NULL THEN
                  DELETE FROM advance.role_page_permissions WHERE "Id"=grant_id;
                  after_row := NULL;
                ELSE
                  UPDATE advance.role_page_permissions SET
                    "CanView"=(prior->>'CanView')::boolean,"CanCreate"=(prior->>'CanCreate')::boolean,
                    "CanUpdate"=(prior->>'CanUpdate')::boolean,"CanSubmit"=(prior->>'CanSubmit')::boolean,
                    "CanResubmit"=(prior->>'CanResubmit')::boolean,"CanPrint"=(prior->>'CanPrint')::boolean,
                    "CanDownload"=(prior->>'CanDownload')::boolean,"CanExport"=(prior->>'CanExport')::boolean,
                    "CanUploadAttachment"=(prior->>'CanUploadAttachment')::boolean,
                    "CanViewAuditHistory"=(prior->>'CanViewAuditHistory')::boolean,
                    "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,
                    "UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                  SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                  IF after_row IS DISTINCT FROM prior THEN
                    RAISE EXCEPTION 'Stores topology grant rollback failed to restore the exact prior row: %',grant_id;
                  END IF;
                END IF;
                INSERT INTO advance.audit_logs
                  ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                   "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
                VALUES(gen_random_uuid(),'GLOBAL','Security','RevertStoresTopology','RolePagePermission',grant_id::text,
                  session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,
                  clock_timestamp(),'StoresWarehouseRackGrants',0);
              END LOOP;
            END $revert$;
            """));
    }
}
