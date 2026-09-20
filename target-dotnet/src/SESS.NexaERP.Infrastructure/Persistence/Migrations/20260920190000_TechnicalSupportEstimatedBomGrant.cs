using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Estimated BOM preparers were hard-coded by employee code (SESS-04, SESS-05) in the service;
/// the rule is now by role. The two named employees hold TECHNICAL_SUPPORT_MANAGER (FULL and
/// SUPPORT), which never had the design.estimated-bom page grant the service rule implied. This
/// gives that role the preparer grant the Design Engineer holds (view, create, update, submit;
/// never approve or reject), audited and exactly reversible.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920190000_TechnicalSupportEstimatedBomGrant")]
public sealed class TechnicalSupportEstimatedBomGrant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf("""
            SET LOCAL TIME ZONE 'UTC';
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $grant$
            DECLARE role_id uuid; page_id uuid; grant_id uuid; prior jsonb; after_row jsonb;
            BEGIN
              SELECT "Id" INTO STRICT role_id FROM advance.roles WHERE "Code"='TECHNICAL_SUPPORT_MANAGER' AND "IsActive";
              SELECT "Id" INTO STRICT page_id FROM advance.page_definitions WHERE "PageKey"='design.estimated-bom' AND "IsActive";
              IF NOT EXISTS (SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
                  WHERE r."Code"='DESIGN_ENGINEER' AND p."PageDefinitionId"=page_id AND p."CanView" AND p."CanCreate" AND p."CanUpdate" AND p."CanSubmit" AND NOT p."CanApprove") THEN
                RAISE EXCEPTION 'The Design Engineer preparer grant on design.estimated-bom is not at the expected contract.';
              END IF;
              SELECT to_jsonb(p),p."Id" INTO prior,grant_id FROM advance.role_page_permissions p WHERE p."RoleId"=role_id AND p."PageDefinitionId"=page_id;
              IF prior IS NULL THEN
                grant_id := md5('tsm-estimated-bom:TECHNICAL_SUPPORT_MANAGER:design.estimated-bom')::uuid;
                INSERT INTO advance.role_page_permissions
                  ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
                   "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
                   "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
                   "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
                   "CreatedAt","CreatedBy","Version")
                VALUES(grant_id,role_id,page_id,true,true,true,true,false,false,false,false,false,false,false,
                  false,false,false,false,false,false,false,false,false,false,clock_timestamp(),'TechnicalSupportEstimatedBomGrant',0);
              ELSE
                UPDATE advance.role_page_permissions SET "CanView"=true,"CanCreate"=true,"CanUpdate"=true,"CanSubmit"=true,
                  "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"='TechnicalSupportEstimatedBomGrant' WHERE "Id"=grant_id;
              END IF;
              SELECT to_jsonb(p) INTO STRICT after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
              INSERT INTO advance.audit_logs
                ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                 "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
              VALUES(gen_random_uuid(),'GLOBAL','Security','GrantEstimatedBomPreparer','RolePagePermission',grant_id::text,
                session_user,'','Success','TechnicalSupportEstimatedBomGrant:TECHNICAL_SUPPORT_MANAGER:design.estimated-bom',
                prior::text,after_row::text,clock_timestamp(),'TechnicalSupportEstimatedBomGrant',0);
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
              SELECT a.* INTO STRICT receipt FROM advance.audit_logs a
                WHERE a."CreatedBy"='TechnicalSupportEstimatedBomGrant' AND a."Action"='GrantEstimatedBomPreparer'
                  AND NOT EXISTS(SELECT 1 FROM advance.audit_logs r WHERE r."CreatedBy"='TechnicalSupportEstimatedBomGrant'
                    AND r."Action"='RevertEstimatedBomPreparer' AND r."CorrelationId"=a."Id"::text);
              grant_id := receipt."EntityId"::uuid;
              SELECT to_jsonb(p) INTO current_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
              IF current_row IS DISTINCT FROM receipt."AfterJson"::jsonb THEN
                RAISE EXCEPTION 'Estimated BOM preparer rollback refuses a changed or missing permission: %',grant_id;
              END IF;
              prior := receipt."BeforeJson"::jsonb;
              IF prior IS NULL THEN
                DELETE FROM advance.role_page_permissions WHERE "Id"=grant_id; after_row := NULL;
              ELSE
                UPDATE advance.role_page_permissions SET "CanView"=(prior->>'CanView')::boolean,"CanCreate"=(prior->>'CanCreate')::boolean,
                  "CanUpdate"=(prior->>'CanUpdate')::boolean,"CanSubmit"=(prior->>'CanSubmit')::boolean,
                  "Version"=(prior->>'Version')::bigint,"UpdatedAt"=(prior->>'UpdatedAt')::timestamptz,"UpdatedBy"=prior->>'UpdatedBy' WHERE "Id"=grant_id;
                SELECT to_jsonb(p) INTO after_row FROM advance.role_page_permissions p WHERE p."Id"=grant_id;
                IF after_row IS DISTINCT FROM prior THEN RAISE EXCEPTION 'Estimated BOM preparer rollback failed to restore the exact prior row: %',grant_id; END IF;
              END IF;
              INSERT INTO advance.audit_logs
                ("Id","Scope","Module","Action","EntityName","EntityId","UserLoginId","ActorRoleCode","Result",
                 "CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
              VALUES(gen_random_uuid(),'GLOBAL','Security','RevertEstimatedBomPreparer','RolePagePermission',grant_id::text,
                session_user,'','Success',receipt."Id"::text,current_row::text,after_row::text,clock_timestamp(),'TechnicalSupportEstimatedBomGrant',0);
            END $revert$;
            """));
    }
}
