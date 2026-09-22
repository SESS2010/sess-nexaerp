using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913010000_CompanyReportPermissions")]
public sealed class CompanyReportPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$
            BEGIN
              IF current_setting('server_version_num')::integer < 170000
                OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Company report migration refuses this PostgreSQL cluster or protected database.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey" IN ('reports.stock-balance','reports.movement-roll-forward','reports.grni','reports.vendor-purchases','reports.fifo-valuation','reports.engineer-custody','reports.purchase-register','reports.pending-approvals')) THEN
                RAISE EXCEPTION 'Company report installation found pre-existing page state.';
              END IF;
              IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey" IN ('stores.stock-check','accounts.vendor-bills','purchase.po') AND "IsActive")<>3 THEN
                RAISE EXCEPTION 'Company reports require the stock-check and vendor-bill permission baselines.';
              END IF;
            END $guard$;
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
            SELECT md5(v.key)::uuid,v.key,'Reports',v.title,'/reports/'||substring(v.key from 9),true,now(),'CompanyReportPermissions',0
            FROM (VALUES ('reports.stock-balance','Stock balance'),('reports.movement-roll-forward','Movement roll-forward'),('reports.grni','Goods received, not invoiced'),('reports.vendor-purchases','Vendor purchase summary'),('reports.engineer-custody','Custody by engineer'),('reports.purchase-register','PR to PO to GRN to bill register'),('reports.pending-approvals','Pending approvals'),('reports.fifo-valuation','FIFO valuation and ageing')) v(key,title);

            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
              "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
              "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
              "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5(p."PageKey"||':'||r."Id"::text)::uuid,r."Id",p."Id",true,false,false,false,false,false,
              false,false,false,false,false,false,false,false,false,
              source."CanExport" OR source."HasFullControl",false,false,
              p."PageKey" IN ('reports.grni','reports.vendor-purchases','reports.fifo-valuation') AND (source."CanViewCommercialValues" OR source."HasFullControl"),false,false,now(),'CompanyReportPermissions',0
            FROM advance.page_definitions p
            CROSS JOIN advance.roles r
            JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
            JOIN advance.page_definitions baseline ON baseline."Id"=source."PageDefinitionId" AND baseline."PageKey"=CASE WHEN p."PageKey" IN ('reports.grni','reports.vendor-purchases','reports.fifo-valuation') THEN 'accounts.vendor-bills' WHEN p."PageKey"='reports.purchase-register' THEN 'purchase.po' ELSE 'stores.stock-check' END
            WHERE p."CreatedBy"='CompanyReportPermissions' AND p."PageKey"<>'reports.pending-approvals' AND r."IsActive"
              AND (source."CanView" OR source."HasFullControl");
            """);
        migrationBuilder.Sql("""

            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
              "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
              "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
              "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5(p."PageKey"||':'||r."Id"::text)::uuid,r."Id",p."Id",true,false,false,false,false,false,
              false,false,false,false,false,false,false,false,false,
              bool_or(source."CanExport" OR source."HasFullControl"),false,false,false,false,false,now(),'CompanyReportPermissions',0
            FROM advance.page_definitions p CROSS JOIN advance.roles r
            JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
            JOIN advance.page_definitions baseline ON baseline."Id"=source."PageDefinitionId" AND baseline."IsActive"
            WHERE p."PageKey"='reports.pending-approvals' AND r."IsActive"
              AND (source."CanApprove" OR source."CanVerify" OR source."HasFullControl")
            GROUP BY p."PageKey",p."Id",r."Id";
            """);
        migrationBuilder.Sql(ControlledCompanyReportSql.Up);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$
            BEGIN
              IF current_setting('server_version_num')::integer < 170000
                OR current_database() IN ('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Company report migration refuses this PostgreSQL cluster or protected database.';
              END IF;
              IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey" IN ('reports.stock-balance','reports.movement-roll-forward','reports.grni','reports.vendor-purchases','reports.fifo-valuation','reports.engineer-custody','reports.purchase-register','reports.pending-approvals')
                  AND "CreatedBy"='CompanyReportPermissions' AND "Version"=0)<>8
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions rp JOIN advance.page_definitions p ON p."Id"=rp."PageDefinitionId"
                  WHERE p."CreatedBy"='CompanyReportPermissions' AND (rp."CreatedBy"<>'CompanyReportPermissions' OR rp."Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.employee_page_permissions ep JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId"
                  WHERE p."CreatedBy"='CompanyReportPermissions') THEN
                RAISE EXCEPTION 'Company report rollback refuses missing pages or changed runtime permissions.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId" IN
              (SELECT "Id" FROM advance.page_definitions WHERE "CreatedBy"='CompanyReportPermissions');
            DELETE FROM advance.page_definitions WHERE "CreatedBy"='CompanyReportPermissions';
            """);
        migrationBuilder.Sql(ControlledCompanyReportSql.Down);
    }
}
