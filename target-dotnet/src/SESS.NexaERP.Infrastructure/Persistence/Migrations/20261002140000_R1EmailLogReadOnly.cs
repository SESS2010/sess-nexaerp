using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>R1 uses manual PO email; only TD/MD retain the informational email page, View only.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20261002140000_R1EmailLogReadOnly")]
public sealed class R1EmailLogReadOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='admin.email' AND "IsActive")
                OR (SELECT count(*) FROM advance.roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND "IsActive")<>2 THEN
                RAISE EXCEPTION 'R1 email restriction requires the active email page and director roles.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_email_read_only_backup(permission_id uuid PRIMARY KEY,before_row jsonb,after_row jsonb NOT NULL);
            REVOKE ALL ON advance.r1_email_read_only_backup FROM PUBLIC;
            INSERT INTO advance.r1_email_read_only_backup
              SELECT g."Id",to_jsonb(g),'{}'::jsonb FROM advance.role_page_permissions g
              JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId" WHERE p."PageKey"='admin.email';
            INSERT INTO advance.r1_email_read_only_backup
              SELECT md5('R1EmailLogReadOnly:'||r."Code")::uuid,NULL,'{}'::jsonb
              FROM advance.roles r CROSS JOIN advance.page_definitions p
              WHERE r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND p."PageKey"='admin.email'
                AND NOT EXISTS(SELECT 1 FROM advance.role_page_permissions g WHERE g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id");
            UPDATE advance.role_page_permissions g SET
              "CanView"=r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),"CanCreate"=false,"CanUpdate"=false,"CanSubmit"=false,"CanIssue"=false,"CanVerify"=false,"CanApprove"=false,"CanReject"=false,"CanRequestClarification"=false,"CanRequestRevision"=false,"CanResubmit"=false,"CanCancel"=false,"CanDeactivate"=false,"CanPrint"=false,"CanDownload"=false,"CanExport"=false,"CanUploadAttachment"=false,"CanReplaceAttachment"=false,"CanViewCommercialValues"=false,"CanViewAuditHistory"=false,"HasFullControl"=false,
              "UpdatedAt"=now(),"UpdatedBy"='R1EmailLogReadOnly',"Version"=g."Version"+1
            FROM advance.roles r,advance.r1_email_read_only_backup b WHERE g."RoleId"=r."Id" AND g."Id"=b.permission_id;
            INSERT INTO advance.role_page_permissions("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
              SELECT b.permission_id,r."Id",p."Id",true,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,false,now(),'R1EmailLogReadOnly',0
              FROM advance.roles r CROSS JOIN advance.page_definitions p
              JOIN advance.r1_email_read_only_backup b ON b.before_row IS NULL
              WHERE p."PageKey"='admin.email' AND r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
                AND b.permission_id=md5('R1EmailLogReadOnly:'||r."Code")::uuid;
            UPDATE advance.r1_email_read_only_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_email_read_only_backup b LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row)
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions g JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId"
                  WHERE p."PageKey"='admin.email' AND NOT EXISTS(SELECT 1 FROM advance.r1_email_read_only_backup b WHERE b.permission_id=g."Id")) THEN
                RAISE EXCEPTION 'R1 email read-only rollback refuses changed or added permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET "CanView"=old."CanView","CanCreate"=old."CanCreate","CanUpdate"=old."CanUpdate","CanSubmit"=old."CanSubmit","CanIssue"=old."CanIssue","CanVerify"=old."CanVerify","CanApprove"=old."CanApprove","CanReject"=old."CanReject","CanRequestClarification"=old."CanRequestClarification","CanRequestRevision"=old."CanRequestRevision","CanResubmit"=old."CanResubmit","CanCancel"=old."CanCancel","CanDeactivate"=old."CanDeactivate","CanPrint"=old."CanPrint","CanDownload"=old."CanDownload","CanExport"=old."CanExport","CanUploadAttachment"=old."CanUploadAttachment","CanReplaceAttachment"=old."CanReplaceAttachment","CanViewCommercialValues"=old."CanViewCommercialValues","CanViewAuditHistory"=old."CanViewAuditHistory","HasFullControl"=old."HasFullControl","UpdatedAt"=old."UpdatedAt","UpdatedBy"=old."UpdatedBy","Version"=old."Version"
              FROM advance.r1_email_read_only_backup b CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
              WHERE g."Id"=b.permission_id AND b.before_row IS NOT NULL;
            DELETE FROM advance.role_page_permissions g USING advance.r1_email_read_only_backup b
              WHERE g."Id"=b.permission_id AND b.before_row IS NULL;
            DROP TABLE advance.r1_email_read_only_backup;
            """);
    }
}
