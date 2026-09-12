using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpeningStockImportAdapterPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 OR current_database() IN ('postgres','template0','template1') THEN
                    RAISE EXCEPTION 'Opening Stock import adapter refuses this PostgreSQL cluster or administrative database.';
                  END IF;
                  IF (SELECT count(*) FROM advance.roles WHERE "Code" IN ('STORES_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR') AND "IsActive") <> 3 THEN
                    RAISE EXCEPTION 'Opening Stock import adapter requires all three actor roles.';
                  END IF;
                  IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock') THEN
                    RAISE EXCEPTION 'Opening Stock import adapter found pre-existing or partial page state.';
                  END IF;
                END $guard$;
                INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
                VALUES(md5('stores.opening-stock')::uuid,'stores.opening-stock','Stores','Opening Stock','/stores/opening-stock',true,now(),'OpeningStockImportAdapterPermissions',0);
                INSERT INTO advance.role_page_permissions(
                  "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
                SELECT md5('stores.opening-stock:'||r."Code")::uuid,r."Id",p."Id",true,true,true,false,false,false,false,false,false,false,false,false,false,false,true,true,false,false,true,true,false,now(),'OpeningStockImportAdapterPermissions',0
                FROM advance.roles r CROSS JOIN advance.page_definitions p
                WHERE r."Code" IN ('STORES_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR') AND r."IsActive" AND p."PageKey"='stores.opening-stock';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                  IF current_setting('server_version_num')::integer < 170000 OR current_database() IN ('postgres','template0','template1') THEN
                    RAISE EXCEPTION 'Opening Stock import adapter rollback refuses this PostgreSQL cluster or administrative database.';
                  END IF;
                  IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock') <> 1
                     OR (SELECT count(*) FROM advance.role_page_permissions rp JOIN advance.page_definitions p ON p."Id"=rp."PageDefinitionId" WHERE p."PageKey"='stores.opening-stock') <> 3 THEN
                    RAISE EXCEPTION 'Opening Stock import adapter rollback found absent or partial state.';
                  END IF;
                END $guard$;
                DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=(SELECT "Id" FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock');
                DELETE FROM advance.page_definitions WHERE "PageKey"='stores.opening-stock';
                """);
        }
    }
}
