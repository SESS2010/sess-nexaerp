using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>TD decision, 29 September: admin.email View for IT/TD/MD and Update
/// for TD/MD. Add only missing grants, retaining all existing actions and refusing
/// rollback if a migrated permission row has subsequently changed.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260929123000_R1EmailPageGrants")]
public sealed class R1EmailPageGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE r1_email_grant_targets(role_code text,page_key text,PRIMARY KEY(role_code,page_key)) ON COMMIT DROP;
            INSERT INTO r1_email_grant_targets VALUES
                ('IT_MANAGER','admin.email'),
                ('TECHNICAL_DIRECTOR','admin.email'),
                ('MANAGING_DIRECTOR','admin.email');
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM r1_email_grant_targets t
                LEFT JOIN advance.roles r ON r."Code"=t.role_code AND r."IsActive"
                LEFT JOIN advance.page_definitions p ON p."PageKey"=t.page_key AND p."IsActive"
                WHERE r."Id" IS NULL OR p."Id" IS NULL) THEN
                RAISE EXCEPTION 'R1 email grants require all approved active roles and pages.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_email_page_grant_backup(
              permission_id uuid PRIMARY KEY,before_row jsonb,after_row jsonb NOT NULL);
            REVOKE ALL ON advance.r1_email_page_grant_backup FROM PUBLIC;
            INSERT INTO advance.r1_email_page_grant_backup(permission_id,before_row,after_row)
            SELECT coalesce(g."Id",md5('R1EmailPageGrants:'||r."Code"||':'||p."PageKey")::uuid),
              CASE WHEN g."Id" IS NOT NULL THEN to_jsonb(g) END,'{}'::jsonb
            FROM r1_email_grant_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            LEFT JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
            WHERE g."Id" IS NULL OR NOT g."CanView" OR (t.role_code IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND NOT g."CanUpdate");
            UPDATE advance.role_page_permissions g SET
              "CanView"=true,"CanUpdate"=g."CanUpdate" OR r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),
              "UpdatedAt"=now(),"UpdatedBy"='R1EmailPageGrants',"Version"=g."Version"+1
            FROM advance.r1_email_page_grant_backup b,advance.roles r
            WHERE g."Id"=b.permission_id AND r."Id"=g."RoleId";
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
              "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
              "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT b.permission_id,r."Id",p."Id",true,false,t.role_code IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),false,false,false,false,false,
              false,false,false,false,false,false,false,false,false,false,false,false,false,now(),'R1EmailPageGrants',0
            FROM r1_email_grant_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            JOIN advance.r1_email_page_grant_backup b ON b.permission_id=md5('R1EmailPageGrants:'||r."Code"||':'||p."PageKey")::uuid
              AND b.before_row IS NULL;
            UPDATE advance.r1_email_page_grant_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_email_page_grant_backup b
                LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'R1 email grants rollback refuses changed or removed permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET
              "CanView"=old."CanView","CanUpdate"=old."CanUpdate",
              "UpdatedAt"=old."UpdatedAt","UpdatedBy"=old."UpdatedBy","Version"=old."Version"
            FROM advance.r1_email_page_grant_backup b
            CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
            WHERE g."Id"=b.permission_id AND b.before_row IS NOT NULL;
            DELETE FROM advance.role_page_permissions g USING advance.r1_email_page_grant_backup b
              WHERE g."Id"=b.permission_id AND b.before_row IS NULL;
            DROP TABLE advance.r1_email_page_grant_backup;
            """);
    }
}
