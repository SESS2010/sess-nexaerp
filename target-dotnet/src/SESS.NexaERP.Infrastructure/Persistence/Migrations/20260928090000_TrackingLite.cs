using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Tracking-lite for Release 1 (TD decision, 27 September 2026; R1 decisions 10 and 11). One list of what is
/// pending, with whom and for how long, over sixteen queues of Stores and Purchase. The queues and their overdue
/// thresholds live in advance.tracking_queues (decision-10 defaults). tracking_source is the company-wide list
/// for the 09:00 digest; tracking_pending and tracking_history apply the dashboard scope rule for one user:
/// a queue needs View on its own page, a row needs a matching operational scope (or the privileged cross scope),
/// except the QC and Accounts queues, which are company-wide as in their own modules (ScopeRule COMPANY).
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260928090000_TrackingLite")]
public sealed class TrackingLite : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(TrackingLiteMigrationSql.Tables);
        migrationBuilder.Sql(TrackingLiteMigrationSql.Definition);
        migrationBuilder.Sql(TrackingLiteMigrationSql.Ownership);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.tracking_queues WHERE "UpdatedBy"<>'TrackingLite')
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('tracking.pending')::uuid
                  AND("CreatedBy"<>'TrackingLite' OR "Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.employee_page_permissions WHERE "PageDefinitionId"=md5('tracking.pending')::uuid) THEN
                RAISE EXCEPTION 'Tracking-lite rollback refuses changed overdue thresholds or tracking permissions.';
              END IF;
            END $guard$;
            DROP FUNCTION advance.tracking_history(text,uuid,uuid[],text,text,uuid);
            DROP FUNCTION advance.tracking_pending(text,uuid,uuid[],text);
            DROP FUNCTION advance.tracking_source(uuid,text);
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('tracking.pending')::uuid;
            DELETE FROM advance.page_definitions WHERE "PageKey"='tracking.pending';
            DROP TABLE advance.tracking_queues;
            """);
    }
}

internal static class TrackingLiteMigrationSql
{
    internal static readonly string[] Signatures =
    [
        "advance.tracking_source(uuid,text)",
        "advance.tracking_pending(text,uuid,uuid[],text)",
        "advance.tracking_history(text,uuid,uuid[],text,text,uuid)"
    ];

    internal static string Definition
    {
        get
        {
            using var stream = typeof(TrackingLiteMigrationSql).Assembly
                .GetManifestResourceStream("TrackingLite.20260928090000.sql")
                ?? throw new InvalidOperationException("Tracking-lite SQL resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n");
        }
    }

    internal const string Tables = """
        DO $guard$ BEGIN
          IF (SELECT count(*) FROM advance.page_definitions WHERE "IsActive" AND "PageKey" IN('purchase.requisitions','purchase.rfq',
              'purchase.vendor-quotations','purchase.commercial-comparisons','purchase.po','inventory.grn','qc.inspection-policies',
              'stores.material-issue-requests','accounts.vendor-bills'))<>9 THEN
            RAISE EXCEPTION 'Tracking-lite requires the nine source page definitions.';
          END IF;
        END $guard$;
        CREATE TABLE advance.tracking_queues(
          "Queue" text PRIMARY KEY CHECK("Queue" ~ '^[a-z][a-z-]{2,60}$'),
          "DocType" text NOT NULL CHECK("DocType" IN('PR','RFQ','QUOTATION','COMPARISON','PO','GATE_ENTRY','GRN','QC','MIR','VENDOR_BILL')),
          "Title" text NOT NULL CHECK(length(btrim("Title")) BETWEEN 1 AND 120),
          "PageKey" text NOT NULL,
          "PendingWithRole" text,
          "ScopeRule" text NOT NULL DEFAULT 'OPERATIONAL' CHECK("ScopeRule" IN('OPERATIONAL','COMPANY')),
          "OverdueAfterDays" integer NOT NULL CHECK("OverdueAfterDays" BETWEEN 0 AND 365),
          "SortOrder" integer NOT NULL UNIQUE,
          "UpdatedAt" timestamptz NOT NULL DEFAULT now(),
          "UpdatedBy" text NOT NULL CHECK(length(btrim("UpdatedBy")) BETWEEN 1 AND 100));
        INSERT INTO advance.tracking_queues("Queue","DocType","Title","PageKey","PendingWithRole","OverdueAfterDays","SortOrder","UpdatedBy") VALUES
          ('pr-department-verification','PR','Requisitions awaiting department verification','purchase.requisitions',NULL,1,10,'TrackingLite'),
          ('pr-approval','PR','Requisitions awaiting approval','purchase.requisitions',NULL,2,20,'TrackingLite'),
          ('pr-stock-check','PR','Requisitions awaiting stock check','purchase.requisitions','STORES_EXECUTIVE',1,30,'TrackingLite'),
          ('rfq-no-quotation','RFQ','RFQs issued with no quotation','purchase.rfq','PURCHASE_EXECUTIVE',5,40,'TrackingLite'),
          ('quotation-technical-verification','QUOTATION','Quotations awaiting technical verification','purchase.vendor-quotations',NULL,2,50,'TrackingLite'),
          ('comparison-decision','COMPARISON','Comparisons awaiting recommendation or approval','purchase.commercial-comparisons','PURCHASE_EXECUTIVE',2,60,'TrackingLite'),
          ('po-pending-approval','PO','POs waiting for approval','purchase.po',NULL,1,70,'TrackingLite'),
          ('po-approved-unissued','PO','POs approved but not issued','purchase.po','PURCHASE_MANAGER',1,80,'TrackingLite'),
          ('po-delivery-overdue','PO','POs past the promised delivery date','purchase.po','PURCHASE_EXECUTIVE',0,90,'TrackingLite'),
          ('gate-no-grn','GATE_ENTRY','Gate entries awaiting GRN','inventory.grn','STORES_EXECUTIVE',1,100,'TrackingLite'),
          ('grn-not-finalised','GRN','GRNs not finalised','inventory.grn','STORES_MANAGER',1,110,'TrackingLite'),
          ('qc-pending','QC','Receipts awaiting QC','qc.inspection-policies','QC_MANAGER',2,120,'TrackingLite'),
          ('mir-approval','MIR','MIRs awaiting approval','stores.material-issue-requests','STORES_MANAGER',1,130,'TrackingLite'),
          ('mir-unissued','MIR','Approved MIRs awaiting issue','stores.material-issue-requests','STORES_EXECUTIVE',1,140,'TrackingLite'),
          ('bill-awaiting-decision','VENDOR_BILL','Vendor bills awaiting accept or reject','accounts.vendor-bills','ACCOUNTS_MANAGER',3,150,'TrackingLite'),
          ('grn-without-bill','GRN','Receipts with no vendor bill','accounts.vendor-bills','ACCOUNTS_ASSISTANT',7,160,'TrackingLite');
        -- QC and Accounts work company-wide by role in their own modules (QC queue, vendor bills); their queues follow that.
        UPDATE advance.tracking_queues SET "ScopeRule"='COMPANY' WHERE "Queue" IN('qc-pending','bill-awaiting-decision','grn-without-bill');
        DO $guard$ BEGIN
          IF EXISTS(SELECT 1 FROM advance.tracking_queues q WHERE NOT EXISTS(SELECT 1 FROM advance.page_definitions p WHERE p."PageKey"=q."PageKey")
              OR(q."PendingWithRole" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM advance.roles r WHERE r."Code"=q."PendingWithRole" AND r."IsActive"))) THEN
            RAISE EXCEPTION 'Tracking-lite queue references an unknown page or role.';
          END IF;
        END $guard$;
        DO $acl$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_runtime') THEN
            EXECUTE 'GRANT SELECT ON advance.tracking_queues TO nexa_erp_runtime';
          END IF;
        END $acl$;
        INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
        VALUES(md5('tracking.pending')::uuid,'tracking.pending','Tracking','Pending documents','/tracking/pending',true,now(),'TrackingLite',0);
        INSERT INTO advance.role_page_permissions(
          "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
          "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
          "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('tracking.pending:'||r."Code")::uuid,r."Id",md5('tracking.pending')::uuid,true,false,false,false,false,false,false,false,
          false,false,false,false,false,false,false,false,false,false,false,true,false,now(),'TrackingLite',0
        FROM advance.roles r
        WHERE r."IsActive" AND EXISTS(SELECT 1 FROM advance.role_page_permissions source
          JOIN advance.page_definitions p ON p."Id"=source."PageDefinitionId" AND p."IsActive"
          JOIN advance.tracking_queues q ON q."PageKey"=p."PageKey"
          WHERE source."RoleId"=r."Id" AND(source."CanView" OR source."HasFullControl"));
        """;

    internal const string Ownership = """
        REVOKE ALL ON FUNCTION advance.tracking_source(uuid,text) FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.tracking_pending(text,uuid,uuid[],text) FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.tracking_history(text,uuid,uuid[],text,text,uuid) FROM PUBLIC;
        DO $owner$ BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.tracking_source(uuid,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.tracking_pending(text,uuid,uuid[],text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.tracking_history(text,uuid,uuid[],text,text,uuid) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.tracking_source(uuid,text) FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.tracking_pending(text,uuid,uuid[],text) FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON FUNCTION advance.tracking_history(text,uuid,uuid[],text,text,uuid) FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.tracking_source(uuid,text) TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.tracking_pending(text,uuid,uuid[],text) TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.tracking_history(text,uuid,uuid[],text,text,uuid) TO nexa_erp_runtime;
          END IF;
        END $owner$;
        """;
}
