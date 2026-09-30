using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>TD Option B: add six operational page grants; remove only Production
/// Manager PO/comparison Approve. Preserve PR/MIR approval and all unrelated actions.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260929181000_R1PurchaseProductionGrants")]
public sealed class R1PurchaseProductionGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE r1_purchase_production_targets(role_code text,page_key text,mode text,PRIMARY KEY(role_code,page_key)) ON COMMIT DROP;
            INSERT INTO r1_purchase_production_targets VALUES
                ('PRODUCTION_MANAGER','purchase.po','REMOVE_APPROVE'),
                ('PRODUCTION_MANAGER','purchase.commercial-comparisons','REMOVE_APPROVE'),
                ('PRODUCTION_MANAGER','design.estimated-bom','VIEW'),
                ('PRODUCTION_MANAGER','stores.material-issues','VIEW'),
                ('PRODUCTION_MANAGER','stores.machine-deliveries','VIEW'),
                ('PURCHASE_MANAGER','purchase.requisitions','AUTHOR'),
                ('PURCHASE_MANAGER','purchase.rfq','AUTHOR'),
                ('PURCHASE_MANAGER','purchase.vendor-quotations','AUTHOR');
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM r1_purchase_production_targets t
                LEFT JOIN advance.roles r ON r."Code"=t.role_code AND r."IsActive"
                LEFT JOIN advance.page_definitions p ON p."PageKey"=t.page_key AND p."IsActive"
                WHERE r."Id" IS NULL OR p."Id" IS NULL) THEN
                RAISE EXCEPTION 'R1 purchase/production grants require all approved active roles and pages.';
              END IF;
              IF EXISTS(SELECT 1 FROM r1_purchase_production_targets t
                JOIN advance.roles r ON r."Code"=t.role_code
                JOIN advance.page_definitions p ON p."PageKey"=t.page_key
                JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
                WHERE t.mode IN ('REMOVE_APPROVE','AUTHOR') AND (g."HasFullControl" OR
                  (t.mode='AUTHOR' AND g."CanApprove"))) THEN
                RAISE EXCEPTION 'R1 purchase/production grants refuse conflicting custom approval or full-control grants.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_purchase_production_backup(
              permission_id uuid PRIMARY KEY,before_row jsonb,after_row jsonb NOT NULL);
            REVOKE ALL ON advance.r1_purchase_production_backup FROM PUBLIC;
            INSERT INTO advance.r1_purchase_production_backup(permission_id,before_row,after_row)
            SELECT coalesce(g."Id",md5('R1PurchaseProductionGrants:'||r."Code"||':'||p."PageKey")::uuid),
              CASE WHEN g."Id" IS NOT NULL THEN to_jsonb(g) END,'{}'::jsonb
            FROM r1_purchase_production_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            LEFT JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
            WHERE (t.mode='REMOVE_APPROVE' AND g."CanApprove") OR
              (t.mode<>'REMOVE_APPROVE' AND (g."Id" IS NULL OR NOT g."CanView" OR
                (t.mode='AUTHOR' AND (NOT g."CanCreate" OR NOT g."CanSubmit"))));
            UPDATE advance.role_page_permissions g SET
              "CanView"=CASE WHEN t.mode='REMOVE_APPROVE' THEN g."CanView" ELSE true END,
              "CanCreate"=g."CanCreate" OR t.mode='AUTHOR',
              "CanSubmit"=g."CanSubmit" OR t.mode='AUTHOR',
              "CanApprove"=CASE WHEN t.mode='REMOVE_APPROVE' THEN false ELSE g."CanApprove" END,
              "UpdatedAt"=now(),"UpdatedBy"='R1PurchaseProductionGrants',"Version"=g."Version"+1
            FROM advance.r1_purchase_production_backup b,advance.roles r,advance.page_definitions p,r1_purchase_production_targets t
            WHERE g."Id"=b.permission_id AND r."Id"=g."RoleId" AND p."Id"=g."PageDefinitionId"
              AND t.role_code=r."Code" AND t.page_key=p."PageKey";
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
              "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
              "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT b.permission_id,r."Id",p."Id",true,t.mode='AUTHOR',false,t.mode='AUTHOR',false,false,false,false,
              false,false,false,false,false,false,false,false,false,false,false,false,false,now(),'R1PurchaseProductionGrants',0
            FROM r1_purchase_production_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            JOIN advance.r1_purchase_production_backup b ON b.permission_id=md5('R1PurchaseProductionGrants:'||r."Code"||':'||p."PageKey")::uuid
              AND b.before_row IS NULL;
            UPDATE advance.r1_purchase_production_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_purchase_production_backup b
                LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'R1 purchase/production grants rollback refuses changed or removed permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET
              "CanView"=old."CanView","CanCreate"=old."CanCreate","CanSubmit"=old."CanSubmit","CanApprove"=old."CanApprove",
              "UpdatedAt"=old."UpdatedAt","UpdatedBy"=old."UpdatedBy","Version"=old."Version"
            FROM advance.r1_purchase_production_backup b
            CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
            WHERE g."Id"=b.permission_id AND b.before_row IS NOT NULL;
            DELETE FROM advance.role_page_permissions g USING advance.r1_purchase_production_backup b
              WHERE g."Id"=b.permission_id AND b.before_row IS NULL;
            DROP TABLE advance.r1_purchase_production_backup;
            """);
    }
}
