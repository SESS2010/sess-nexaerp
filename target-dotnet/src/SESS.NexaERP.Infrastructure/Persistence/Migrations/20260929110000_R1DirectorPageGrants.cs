using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>TD option B, 29 September: add only missing director View and six Pending
/// View/ViewAuditHistory grants. Preserve all existing actions; journal exact before/after
/// rows so rollback refuses any subsequent permission change instead of discarding it.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260929110000_R1DirectorPageGrants")]
public sealed class R1DirectorPageGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            CREATE TEMP TABLE r1_grant_targets(role_code text,page_key text,PRIMARY KEY(role_code,page_key)) ON COMMIT DROP;
            INSERT INTO r1_grant_targets VALUES
                ('TECHNICAL_DIRECTOR','stores.material-issues'),
                ('TECHNICAL_DIRECTOR','production.component-fitments'),
                ('TECHNICAL_DIRECTOR','accounts.intercompany-invoices'),
                ('TECHNICAL_DIRECTOR','accounts.inventory-periods'),
                ('TECHNICAL_DIRECTOR','accounts.supplier-invoices'),
                ('TECHNICAL_DIRECTOR','accounts.vendor-bills'),
                ('TECHNICAL_DIRECTOR','accounts.vendor-financial-evidence'),
                ('MANAGING_DIRECTOR','production.component-fitments'),
                ('MANAGING_DIRECTOR','production.fat-readiness'),
                ('MANAGING_DIRECTOR','production.job-orders'),
                ('MANAGING_DIRECTOR','production.production-bom'),
                ('MANAGING_DIRECTOR','accounts.intercompany-invoices'),
                ('MANAGING_DIRECTOR','accounts.inventory-periods'),
                ('MANAGING_DIRECTOR','accounts.supplier-invoices'),
                ('MANAGING_DIRECTOR','accounts.vendor-bills'),
                ('MANAGING_DIRECTOR','accounts.vendor-financial-evidence'),
                ('STORES_MANAGER','tracking.pending'),
                ('STORES_EXECUTIVE','tracking.pending'),
                ('QC_MANAGER','tracking.pending'),
                ('ACCOUNTS_MANAGER','tracking.pending'),
                ('TECHNICAL_DIRECTOR','tracking.pending'),
                ('MANAGING_DIRECTOR','tracking.pending');
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM r1_grant_targets t
                LEFT JOIN advance.roles r ON r."Code"=t.role_code AND r."IsActive"
                LEFT JOIN advance.page_definitions p ON p."PageKey"=t.page_key AND p."IsActive"
                WHERE r."Id" IS NULL OR p."Id" IS NULL) THEN
                RAISE EXCEPTION 'R1 director grants require all approved active roles and pages.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_director_page_grant_backup(
              permission_id uuid PRIMARY KEY,before_row jsonb,after_row jsonb NOT NULL);
            REVOKE ALL ON advance.r1_director_page_grant_backup FROM PUBLIC;
            INSERT INTO advance.r1_director_page_grant_backup(permission_id,before_row,after_row)
            SELECT coalesce(g."Id",md5('R1DirectorPageGrants:'||r."Code"||':'||p."PageKey")::uuid),
              CASE WHEN g."Id" IS NOT NULL THEN to_jsonb(g) END,'{}'::jsonb
            FROM r1_grant_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            LEFT JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
            WHERE g."Id" IS NULL OR NOT g."CanView" OR (t.page_key='tracking.pending' AND NOT g."CanViewAuditHistory");
            UPDATE advance.role_page_permissions g SET
              "CanView"=true,"CanViewAuditHistory"=g."CanViewAuditHistory" OR p."PageKey"='tracking.pending',
              "UpdatedAt"=now(),"UpdatedBy"='R1DirectorPageGrants',"Version"=g."Version"+1
            FROM advance.r1_director_page_grant_backup b,advance.page_definitions p
            WHERE g."Id"=b.permission_id AND p."Id"=g."PageDefinitionId";
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
              "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
              "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT b.permission_id,r."Id",p."Id",true,false,false,false,false,false,false,false,
              false,false,false,false,false,false,false,false,false,false,false,t.page_key='tracking.pending',false,now(),'R1DirectorPageGrants',0
            FROM r1_grant_targets t
            JOIN advance.roles r ON r."Code"=t.role_code
            JOIN advance.page_definitions p ON p."PageKey"=t.page_key
            JOIN advance.r1_director_page_grant_backup b ON b.permission_id=md5('R1DirectorPageGrants:'||r."Code"||':'||p."PageKey")::uuid
              AND b.before_row IS NULL;
            UPDATE advance.r1_director_page_grant_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_director_page_grant_backup b
                LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'R1 director grants rollback refuses changed or removed permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET
              "CanView"=old."CanView","CanViewAuditHistory"=old."CanViewAuditHistory",
              "UpdatedAt"=old."UpdatedAt","UpdatedBy"=old."UpdatedBy","Version"=old."Version"
            FROM advance.r1_director_page_grant_backup b
            CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
            WHERE g."Id"=b.permission_id AND b.before_row IS NOT NULL;
            DELETE FROM advance.role_page_permissions g USING advance.r1_director_page_grant_backup b
              WHERE g."Id"=b.permission_id AND b.before_row IS NULL;
            DROP TABLE advance.r1_director_page_grant_backup;
            """);
    }
}
