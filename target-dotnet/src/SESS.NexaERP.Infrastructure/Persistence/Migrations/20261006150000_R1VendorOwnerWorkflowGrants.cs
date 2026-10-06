using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261006150000_R1VendorOwnerWorkflowGrants")]
public sealed class R1VendorOwnerWorkflowGrants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM advance.role_page_permissions g
                JOIN advance.roles r ON r."Id"=g."RoleId" AND r."IsActive"
                JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId" AND p."IsActive"
                WHERE r."Code"='PURCHASE_MANAGER' AND p."PageKey"='masters.vendors'
                  AND g."CanView" AND g."CanCreate" AND g."CanUpdate" AND g."CanDownload") <> 1 THEN
                RAISE EXCEPTION 'Vendor workflow grants require the existing Purchase Manager import-owner permission.';
              END IF;
            END $guard$;
            CREATE TABLE advance.r1_vendor_owner_workflow_backup(
              permission_id uuid PRIMARY KEY,before_row jsonb NOT NULL,after_row jsonb NOT NULL);
            INSERT INTO advance.r1_vendor_owner_workflow_backup(permission_id,before_row,after_row)
              SELECT g."Id",to_jsonb(g),'{}'::jsonb FROM advance.role_page_permissions g
              JOIN advance.roles r ON r."Id"=g."RoleId"
              JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId"
              WHERE r."Code"='PURCHASE_MANAGER' AND p."PageKey"='masters.vendors'
                AND (NOT g."CanSubmit" OR NOT g."CanUploadAttachment");
            UPDATE advance.role_page_permissions g SET "CanSubmit"=true,"CanUploadAttachment"=true,
              "UpdatedAt"=now(),"UpdatedBy"='R1VendorOwnerWorkflowGrants',"Version"=g."Version"+1
              FROM advance.r1_vendor_owner_workflow_backup b WHERE g."Id"=b.permission_id;
            UPDATE advance.r1_vendor_owner_workflow_backup b SET after_row=to_jsonb(g)
              FROM advance.role_page_permissions g WHERE g."Id"=b.permission_id;
            DO $protect$ DECLARE entry record;
            BEGIN
              IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                ALTER TABLE advance.r1_vendor_owner_workflow_backup OWNER TO nexa_erp_owner;
              END IF;
              FOR entry IN SELECT DISTINCT acl.grantee FROM pg_class c
                CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) acl
                WHERE c.oid='advance.r1_vendor_owner_workflow_backup'::regclass AND acl.grantee<>c.relowner LOOP
                IF entry.grantee=0 THEN REVOKE ALL ON advance.r1_vendor_owner_workflow_backup FROM PUBLIC;
                ELSE EXECUTE format('REVOKE ALL ON advance.r1_vendor_owner_workflow_backup FROM %I',pg_get_userbyid(entry.grantee)); END IF;
              END LOOP;
            END $protect$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.r1_vendor_owner_workflow_backup b
                LEFT JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE to_jsonb(g) IS DISTINCT FROM b.after_row) THEN
                RAISE EXCEPTION 'Vendor workflow rollback refuses changed or removed permissions.';
              END IF;
            END $guard$;
            UPDATE advance.role_page_permissions g SET "CanSubmit"=old."CanSubmit",
              "CanUploadAttachment"=old."CanUploadAttachment","UpdatedAt"=old."UpdatedAt",
              "UpdatedBy"=old."UpdatedBy","Version"=old."Version"
              FROM advance.r1_vendor_owner_workflow_backup b
              CROSS JOIN LATERAL jsonb_populate_record(NULL::advance.role_page_permissions,b.before_row) old
              WHERE g."Id"=b.permission_id;
            DROP TABLE advance.r1_vendor_owner_workflow_backup;
            """);
    }
}