using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260915190000_IntercompanyPurchasePublication")]
public sealed class IntercompanyPurchasePublication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany publication requires PostgreSQL 17 and an application database.'; END IF;
             IF to_regclass('advance.intercompany_purchase_publications') IS NOT NULL
              OR to_regclass('advance.intercompany_purchase_publication_lines') IS NOT NULL
              OR to_regprocedure('advance.intercompany_purchase_options(uuid,uuid)') IS NOT NULL
             THEN RAISE EXCEPTION 'Intercompany publication package already exists.'; END IF;
             IF to_regprocedure('advance.validate_intercompany_route(advance.intercompany_routes)') IS NULL
              OR to_regclass('advance.purchase_order_history') IS NULL OR to_regclass('advance.employee_operational_scopes') IS NULL
             THEN RAISE EXCEPTION 'Publication requires governed routes, ordinary PO history and operational scopes.'; END IF;
             IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId" WHERE d."PageKey"='purchase.po' AND r."Code" IN('PURCHASE_MANAGER','ACCOUNTS_MANAGER'))<>2
             THEN RAISE EXCEPTION 'Publication requires established Purchase and Accounts permissions.'; END IF;
            END $guard$;
            """);
        using var stream = typeof(IntercompanyPurchasePublication).Assembly.GetManifestResourceStream("IntercompanyPurchasePublication.20260915190000.sql")
            ?? throw new InvalidOperationException("Missing frozen intercompany publication SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('purchase.intercompany-orders')::uuid,'purchase.intercompany-orders','Purchase','Intercompany orders','/purchase/intercompany-orders',true,now(),'IntercompanyPurchasePublication',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('purchase.intercompany-orders:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('purchase.intercompany-orders')::uuid,
             'CanView',true,'CanIssue',r."Code"='PURCHASE_MANAGER','CanCreate',false,'CanApprove',false,'CanReject',false,'CanDeactivate',false,
             'CanUpdate',false,'CanSubmit',false,'CanVerify',false,'CanRequestClarification',false,'CanRequestRevision',false,
             'CanResubmit',false,'CanCancel',false,'CanPrint',false,'CanDownload',false,'CanExport',false,'CanUploadAttachment',false,
             'CanReplaceAttachment',false,'CanViewCommercialValues',true,'CanViewAuditHistory',true,'HasFullControl',false,
             'CreatedAt',now(),'CreatedBy','IntercompanyPurchasePublication','Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            JOIN advance.roles r ON r."Id"=source."RoleId"
            WHERE d."PageKey"='purchase.po' AND r."Code" IN('PURCHASE_MANAGER','ACCOUNTS_MANAGER');
            """);
        migrationBuilder.Sql(IntercompanyPurchase20260915AccessSql.Provision);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany publication rollback refuses this database.'; END IF;
             IF to_regclass('advance.intercompany_purchase_publications') IS NULL OR to_regclass('advance.intercompany_purchase_publication_lines') IS NULL
              OR to_regprocedure('advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text)') IS NULL
             THEN RAISE EXCEPTION 'Publication rollback requires the complete package.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.intercompany_purchase_publications) OR EXISTS(SELECT 1 FROM advance.intercompany_purchase_publication_lines)
             THEN RAISE EXCEPTION 'Publication rollback refuses retained business evidence.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('purchase.intercompany-orders')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('purchase.intercompany-orders')::uuid;
            DROP FUNCTION advance.publish_intercompany_purchase(uuid,uuid,uuid,uuid,bigint,text,uuid,text,uuid,text,text);
            DROP FUNCTION advance.intercompany_purchases_page(uuid,uuid,text,integer,integer);
            DROP FUNCTION advance.intercompany_purchase_json(uuid,uuid,uuid,text);
            DROP FUNCTION advance.intercompany_purchase_options(uuid,uuid);
            DROP FUNCTION advance.intercompany_purchase_scope(uuid,uuid,advance.purchase_orders);
            DROP TABLE advance.intercompany_purchase_publication_lines;
            DROP TABLE advance.intercompany_purchase_publications;
            DROP FUNCTION advance.guard_intercompany_purchase_evidence();
            """);
    }
}
