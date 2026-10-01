using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>Approved R1 master import owners: additive View/Create/Update/Download only.
/// Export and all other actions remain independent and unchanged.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20261002090000_R1MasterImportOwnerGrants")]
public sealed class R1MasterImportOwnerGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE r1_import_targets(role_code text,page_key text,PRIMARY KEY(role_code,page_key)) ON COMMIT DROP;
            INSERT INTO r1_import_targets VALUES
                ('PURCHASE_MANAGER','masters.vendors'),
                ('TECHNICAL_DIRECTOR','masters.vendors'),
                ('MANAGING_DIRECTOR','masters.vendors'),
                ('STORES_MANAGER','masters.items'),
                ('TECHNICAL_DIRECTOR','masters.items'),
                ('MANAGING_DIRECTOR','masters.items'),
                ('STORES_MANAGER','masters.uoms'),
                ('TECHNICAL_DIRECTOR','masters.uoms'),
                ('MANAGING_DIRECTOR','masters.uoms'),
                ('STORES_MANAGER','masters.manufacturers'),
                ('TECHNICAL_DIRECTOR','masters.manufacturers'),
                ('MANAGING_DIRECTOR','masters.manufacturers');
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM r1_import_targets t
                LEFT JOIN advance.roles r ON r."Code"=t.role_code AND r."IsActive"
                LEFT JOIN advance.page_definitions p ON p."PageKey"=t.page_key AND p."IsActive"
                WHERE r."Id" IS NULL OR p."Id" IS NULL) THEN
                RAISE EXCEPTION 'R1 master import grants require all approved active roles and pages.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_master_import_owner_backup(
              permission_id uuid PRIMARY KEY,before_row jsonb,after_row jsonb NOT NULL);
            REVOKE ALL ON advance.r1_master_import_owner_backup FROM PUBLIC;
            INSERT INTO advance.r1_master_import_owner_backup(permission_id,before_row,after_row)
            SELECT coalesce(g."Id",md5('R1MasterImportOwnerGrants:'||r."Code"||':'||p."PageKey")::uuid),
              CASE WHEN g."Id" IS NOT NULL THEN to_jsonb(g) END,'{}'::jsonb
            FROM r1_import_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            LEFT JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
            WHERE g."Id" IS NULL OR NOT g."CanView" OR NOT g."CanCreate" OR NOT g."CanUpdate" OR NOT g."CanDownload";
            UPDATE advance.role_page_permissions g SET
              "CanView"=true,"CanCreate"=true,"CanUpdate"=true,"CanDownload"=true,
              "UpdatedAt"=now(),"UpdatedBy"='R1MasterImportOwnerGrants',"Version"=g."Version"+1
            FROM advance.r1_master_import_owner_backup b,advance.page_definitions p
            WHERE g."Id"=b.permission_id AND p."Id"=g."PageDefinitionId";
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
              "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
              "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT b.permission_id,r."Id",p."Id",true,true,true,false,false,false,false,false,
              false,false,false,false,false,false,true,false,false,false,false,false,false,now(),'R1MasterImportOwnerGrants',0
            FROM r1_import_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            JOIN advance.r1_master_import_owner_backup b ON b.permission_id=md5('R1MasterImportOwnerGrants:'||r."Code"||':'||p."PageKey")::uuid
              AND b.before_row IS NULL;
            UPDATE advance.r1_master_import_owner_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_master_import_owner_backup b
                LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'R1 master import grants rollback refuses changed or removed permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET
              "CanView"=old."CanView","CanCreate"=old."CanCreate","CanUpdate"=old."CanUpdate","CanDownload"=old."CanDownload",
              "UpdatedAt"=old."UpdatedAt","UpdatedBy"=old."UpdatedBy","Version"=old."Version"
            FROM advance.r1_master_import_owner_backup b
            CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
            WHERE g."Id"=b.permission_id AND b.before_row IS NOT NULL;
            DELETE FROM advance.role_page_permissions g USING advance.r1_master_import_owner_backup b
              WHERE g."Id"=b.permission_id AND b.before_row IS NULL;
            DROP TABLE advance.r1_master_import_owner_backup;
            """);
    }
}
