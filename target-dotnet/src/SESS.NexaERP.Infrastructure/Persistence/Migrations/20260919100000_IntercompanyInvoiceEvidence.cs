using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260919100000_IntercompanyInvoiceEvidence")]
public sealed class IntercompanyInvoiceEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany invoice evidence requires PostgreSQL 17 and an application database.'; END IF;
             IF to_regclass('advance.intercompany_invoice_evidence') IS NOT NULL
              OR to_regprocedure('advance.intercompany_invoice_json(uuid,uuid)') IS NOT NULL
              OR to_regprocedure('advance.intercompany_invoice_content(uuid,uuid)') IS NOT NULL
              OR to_regprocedure('advance.intercompany_invoices_for_purchase(uuid,uuid)') IS NOT NULL
              OR to_regprocedure('advance.guard_intercompany_invoice_evidence()') IS NOT NULL
              OR to_regprocedure('advance.record_intercompany_invoice(uuid,uuid,uuid,text,date,text,text,bytea,uuid,text,uuid,text,text)') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='accounts.intercompany-invoices')
             THEN RAISE EXCEPTION 'Intercompany invoice package is already or partially installed.'; END IF;
             IF to_regclass('advance.intercompany_purchase_publications') IS NULL
              OR to_regprocedure('advance.intercompany_purchase_json(uuid,uuid,uuid,text)') IS NULL
             THEN RAISE EXCEPTION 'Invoice evidence requires governed intercompany PO publication.'; END IF;
             IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId" WHERE d."PageKey"='purchase.intercompany-orders' AND r."Code"='ACCOUNTS_MANAGER')<>1
             THEN RAISE EXCEPTION 'Invoice evidence requires the established Accounts commercial reader.'; END IF;
            END $guard$;
            """);
        using var stream = typeof(IntercompanyInvoiceEvidence).Assembly.GetManifestResourceStream("IntercompanyInvoiceEvidence.20260919100000.sql")
            ?? throw new InvalidOperationException("Missing intercompany invoice evidence SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('accounts.intercompany-invoices')::uuid,'accounts.intercompany-invoices','Accounts','Intercompany invoices',
              '/accounts/intercompany-invoices',true,now(),'IntercompanyInvoiceEvidence',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('accounts.intercompany-invoices:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('accounts.intercompany-invoices')::uuid,
             'CanView',true,'CanCreate',true,'CanDownload',true,'CanUploadAttachment',true,'CanViewCommercialValues',true,'CanViewAuditHistory',true,
             'CanIssue',false,'CanApprove',false,'CanReject',false,'CanDeactivate',false,'CanUpdate',false,'CanSubmit',false,'CanVerify',false,
             'CanRequestClarification',false,'CanRequestRevision',false,'CanResubmit',false,'CanCancel',false,'CanPrint',false,'CanExport',false,
             'CanReplaceAttachment',false,'HasFullControl',false,'CreatedAt',now(),'CreatedBy','IntercompanyInvoiceEvidence','Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            JOIN advance.roles r ON r."Id"=source."RoleId"
            WHERE d."PageKey"='purchase.intercompany-orders' AND r."Code"='ACCOUNTS_MANAGER';
            """);
        migrationBuilder.Sql(IntercompanyInvoice20260919AccessSql.Provision);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany invoice rollback refuses this database.'; END IF;
             IF to_regclass('advance.intercompany_invoice_evidence') IS NULL THEN RAISE EXCEPTION 'Invoice package is absent.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.intercompany_invoice_evidence)
             THEN RAISE EXCEPTION 'Invoice rollback refuses retained business evidence.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('accounts.intercompany-invoices')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('accounts.intercompany-invoices')::uuid;
            DROP FUNCTION advance.record_intercompany_invoice(uuid,uuid,uuid,text,date,text,text,bytea,uuid,text,uuid,text,text);
            DROP FUNCTION advance.intercompany_invoices_for_purchase(uuid,uuid);
            DROP FUNCTION advance.intercompany_invoice_json(uuid,uuid);
            DROP FUNCTION advance.intercompany_invoice_content(uuid,uuid);
            DROP TABLE advance.intercompany_invoice_evidence;
            DROP FUNCTION advance.guard_intercompany_invoice_evidence();
            """);
    }
}
