using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914090000_SupplierInvoiceReceipts")]
public sealed class SupplierInvoiceReceipts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF to_regclass('advance.supplier_invoices') IS NOT NULL
              OR to_regprocedure('advance.get_supplier_invoice(uuid,uuid)') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey" IN ('reports.billed-not-received','accounts.supplier-invoices')) THEN
              RAISE EXCEPTION 'Supplier invoice installation refuses pre-existing package state.';
             END IF;
             IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='accounts.vendor-bills' AND "IsActive") THEN
              RAISE EXCEPTION 'Supplier invoices require the Accounts vendor-bill permission baseline.';
             END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(SupplierInvoiceMigrationSql.Snapshot);
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(md5('reports.billed-not-received')::uuid,'reports.billed-not-received','Reports','Billed, not received',
             '/reports/billed-not-received',true,now(),'SupplierInvoiceReceipts',0);
            INSERT INTO advance.role_page_permissions(
             "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
             "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
             "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5('reports.billed-not-received:'||r."Id"::text)::uuid,r."Id",md5('reports.billed-not-received')::uuid,
             true,false,false,false,false,false,false,false,false,false,false,false,false,false,false,
             source."CanExport" OR source."HasFullControl",false,false,
             source."CanViewCommercialValues" OR source."HasFullControl",false,false,now(),'SupplierInvoiceReceipts',0
            FROM advance.roles r JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
            JOIN advance.page_definitions p ON p."Id"=source."PageDefinitionId" AND p."PageKey"='accounts.vendor-bills'
            WHERE r."IsActive" AND (source."CanView" OR source."HasFullControl");
            """);
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(md5('accounts.supplier-invoices')::uuid,'accounts.supplier-invoices','Accounts','Supplier Invoices',
             '/accounts/supplier-invoices',true,now(),'SupplierInvoiceReceipts',0);
            INSERT INTO advance.role_page_permissions(
             "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
             "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
             "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5('accounts.supplier-invoices:'||r."Id"::text)::uuid,r."Id",md5('accounts.supplier-invoices')::uuid,
             source."CanView" OR source."HasFullControl",source."CanCreate" OR source."HasFullControl",
             false,false,false,false,
             r."Code"='ACCOUNTS_MANAGER' AND (source."CanApprove" OR source."HasFullControl"),
             false,false,false,false,
             r."Code"='ACCOUNTS_MANAGER' AND (source."CanCancel" OR source."HasFullControl"),
             false,false,
             (source."CanView" OR source."HasFullControl") AND (source."CanViewCommercialValues" OR source."HasFullControl"),
             false,source."CanCreate" OR source."HasFullControl",false,
             source."CanViewCommercialValues" OR source."HasFullControl",
             source."CanViewAuditHistory" OR source."HasFullControl",false,now(),'SupplierInvoiceReceipts',0
            FROM advance.roles r JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
            JOIN advance.page_definitions p ON p."Id"=source."PageDefinitionId" AND p."PageKey"='accounts.vendor-bills'
            WHERE r."IsActive" AND r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER');
            """);
        migrationBuilder.Sql(SupplierInvoiceMigrationSql.ConfigureOwnership);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.supplier_invoices) THEN
              RAISE EXCEPTION 'Supplier invoice rollback refuses retained financial evidence.';
             END IF;
             IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey" IN ('reports.billed-not-received','accounts.supplier-invoices')
               AND "CreatedBy"='SupplierInvoiceReceipts' AND "Version"=0)<>2
              OR EXISTS(SELECT 1 FROM advance.role_page_permissions rp JOIN advance.page_definitions p ON p."Id"=rp."PageDefinitionId"
               WHERE p."PageKey" IN ('reports.billed-not-received','accounts.supplier-invoices') AND (rp."CreatedBy"<>'SupplierInvoiceReceipts' OR rp."Version"<>0))
              OR EXISTS(SELECT 1 FROM advance.employee_page_permissions ep JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId"
               WHERE p."PageKey" IN ('reports.billed-not-received','accounts.supplier-invoices')) THEN
              RAISE EXCEPTION 'Supplier invoice rollback refuses changed runtime permissions.';
             END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId" IN (md5('reports.billed-not-received')::uuid,md5('accounts.supplier-invoices')::uuid);
            DELETE FROM advance.page_definitions WHERE "Id" IN (md5('reports.billed-not-received')::uuid,md5('accounts.supplier-invoices')::uuid);
            DROP TRIGGER trg_supplier_invoice_receipt_match ON advance.goods_receipts;
            DROP FUNCTION advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text);
            DROP FUNCTION advance.supplier_invoice_content(uuid,uuid);
            DROP FUNCTION advance.get_supplier_invoice(uuid,uuid);
            DROP FUNCTION advance.link_supplier_invoice_bill(uuid,uuid,bigint,uuid,uuid,text,uuid,text);
            DROP FUNCTION advance.cancel_supplier_invoice(uuid,uuid,bigint,text,uuid,text,uuid,text);
            DROP FUNCTION advance.record_supplier_invoice(uuid,uuid,uuid,text,date,text,jsonb,text,text,bytea,uuid,text,uuid,text,text);
            DROP FUNCTION advance.match_supplier_invoices_on_receipt();
            DROP FUNCTION advance.reconcile_supplier_invoice_receipts(uuid,uuid,uuid);
            DROP FUNCTION advance.supplier_invoice_version(uuid,uuid);
            DROP FUNCTION advance.supplier_invoice_command_valid(uuid,uuid,text,uuid,text,text,text);
            DROP TABLE advance.supplier_invoice_bill_links;
            DROP TABLE advance.supplier_invoice_receipt_matches;
            DROP TABLE advance.supplier_invoice_cancellations;
            DROP TABLE advance.supplier_invoice_lines;
            DROP TABLE advance.supplier_invoices;
            DROP FUNCTION advance.guard_supplier_invoice_evidence();
            """);
    }
}

public static class SupplierInvoiceMigrationSql
{
    internal static string Snapshot
    {
        get
        {
            using var stream = typeof(SupplierInvoiceMigrationSql).Assembly
                .GetManifestResourceStream("SupplierInvoiceReceipts.20260914090000.sql")
                ?? throw new InvalidOperationException("Supplier invoice SQL snapshot is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
    public const string ConfigureOwnership = """
        DO $supplier_acl$
        DECLARE relation text; signature text; principal text;
        BEGIN
         IF to_regclass('advance.supplier_invoices') IS NOT NULL THEN
          FOREACH relation IN ARRAY ARRAY['supplier_invoices','supplier_invoice_lines','supplier_invoice_cancellations','supplier_invoice_receipt_matches','supplier_invoice_bill_links']
          LOOP
           IF to_regclass('advance.'||relation) IS NULL THEN RAISE EXCEPTION 'Supplier invoice package is partially installed.'; END IF;
           EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM PUBLIC',relation);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER TABLE advance.%I OWNER TO nexa_erp_owner',relation); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration']
           LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON TABLE advance.%I FROM %I',relation,principal); END IF;
           END LOOP;
          END LOOP;
          FOREACH signature IN ARRAY ARRAY['advance.guard_supplier_invoice_evidence()','advance.supplier_invoice_command_valid(uuid,uuid,text,uuid,text,text,text)','advance.supplier_invoice_version(uuid,uuid)','advance.reconcile_supplier_invoice_receipts(uuid,uuid,uuid)','advance.match_supplier_invoices_on_receipt()','advance.record_supplier_invoice(uuid,uuid,uuid,text,date,text,jsonb,text,text,bytea,uuid,text,uuid,text,text)','advance.cancel_supplier_invoice(uuid,uuid,bigint,text,uuid,text,uuid,text)','advance.link_supplier_invoice_bill(uuid,uuid,bigint,uuid,uuid,text,uuid,text)','advance.get_supplier_invoice(uuid,uuid)','advance.supplier_invoice_content(uuid,uuid)','advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)']
          LOOP
           IF to_regprocedure(signature) IS NULL THEN RAISE EXCEPTION 'Supplier invoice function is missing: %',signature; END IF;
           EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC',signature);
           IF to_regrole('nexa_erp_owner') IS NOT NULL THEN EXECUTE format('ALTER FUNCTION %s OWNER TO nexa_erp_owner',signature); END IF;
           FOREACH principal IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap','nexa_erp_migration']
           LOOP
            IF to_regrole(principal) IS NOT NULL THEN EXECUTE format('REVOKE ALL ON FUNCTION %s FROM %I',signature,principal); END IF;
           END LOOP;
          END LOOP;
          IF to_regrole('nexa_erp_runtime') IS NOT NULL THEN
           GRANT EXECUTE ON FUNCTION advance.record_supplier_invoice(uuid,uuid,uuid,text,date,text,jsonb,text,text,bytea,uuid,text,uuid,text,text),advance.cancel_supplier_invoice(uuid,uuid,bigint,text,uuid,text,uuid,text),advance.link_supplier_invoice_bill(uuid,uuid,bigint,uuid,uuid,text,uuid,text),advance.get_supplier_invoice(uuid,uuid),advance.supplier_invoice_content(uuid,uuid),advance.company_report_billed_not_received(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)
            TO nexa_erp_runtime;
          END IF;
         END IF;
        END $supplier_acl$;
        """;
}
