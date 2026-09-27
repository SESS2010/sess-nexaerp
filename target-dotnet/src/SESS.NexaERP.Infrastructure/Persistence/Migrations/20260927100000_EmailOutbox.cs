using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Email-lite for Release 1 (TD decision, 27 September 2026). The e-mail outbox is written in the same
/// transaction as the business event (PO issued) or by the 09:00 digest job, then composed and sent by
/// the worker. Rows are the sent-mail log: never deleted. The page admin.email lets the TD (view, retry,
/// test) and the IT Manager (view) read the log. Bodies are stored but never returned by the log API.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260927100000_EmailOutbox")]
public sealed class EmailOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            CREATE TABLE advance.email_outbox(
              "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
              "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
              "EventType" text NOT NULL CHECK("EventType" IN ('PO_ISSUED','DIGEST_PURCHASE','DIGEST_STORES','DIGEST_MANAGEMENT','TEST')),
              "SourceEntityType" text CHECK("SourceEntityType" IS NULL OR length("SourceEntityType") BETWEEN 1 AND 100),
              "SourceEntityId" uuid,
              "IdempotencyKey" text NOT NULL CHECK(length(btrim("IdempotencyKey")) BETWEEN 1 AND 200),
              "PayloadJson" jsonb NOT NULL DEFAULT '{}'::jsonb,
              "ToAddresses" text[] NOT NULL DEFAULT '{}',
              "CcAddresses" text[] NOT NULL DEFAULT '{}',
              "Subject" text CHECK("Subject" IS NULL OR length("Subject") BETWEEN 1 AND 300),
              "BodyHtml" text,
              "BodyText" text,
              "Status" text NOT NULL CHECK("Status" IN ('PENDING_COMPOSE','QUEUED','SENDING','SENT','FAILED','DEAD','BLOCKED_ALLOWLIST','SKIPPED')),
              "Attempts" integer NOT NULL DEFAULT 0 CHECK("Attempts" >= 0),
              "NextAttemptAt" timestamptz,
              "LastError" text CHECK("LastError" IS NULL OR length("LastError") <= 2000),
              "CreatedAt" timestamptz NOT NULL DEFAULT clock_timestamp(),
              "CreatedBy" text NOT NULL,
              "ComposedAt" timestamptz,
              "SentAt" timestamptz,
              "ProviderMessageId" text,
              CONSTRAINT "UQ_email_outbox_company_key" UNIQUE("CompanyId","IdempotencyKey"),
              CONSTRAINT "CK_email_outbox_composed" CHECK("Status" IN ('PENDING_COMPOSE','SKIPPED')
                OR ("Subject" IS NOT NULL AND "BodyHtml" IS NOT NULL AND cardinality("ToAddresses") > 0)),
              CONSTRAINT "CK_email_outbox_sent" CHECK(("Status" = 'SENT') = ("SentAt" IS NOT NULL)));
            CREATE INDEX "IX_email_outbox_due" ON advance.email_outbox("Status","NextAttemptAt","CreatedAt")
              WHERE "Status" IN ('PENDING_COMPOSE','QUEUED','FAILED');
            CREATE INDEX "IX_email_outbox_company_created" ON advance.email_outbox("CompanyId","CreatedAt" DESC);
            CREATE FUNCTION advance.guard_email_outbox_retention() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
            BEGIN
              RAISE EXCEPTION USING ERRCODE='42501', MESSAGE='The e-mail log is kept: rows are never deleted.';
            END $f$;
            CREATE TRIGGER trg_email_outbox_retention BEFORE DELETE ON advance.email_outbox FOR EACH ROW EXECUTE FUNCTION advance.guard_email_outbox_retention();
            DO $acl$ BEGIN
              IF EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='nexa_erp_runtime') THEN
                EXECUTE 'GRANT SELECT,INSERT,UPDATE ON advance.email_outbox TO nexa_erp_runtime';
              END IF;
            END $acl$;
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(md5('admin.email')::uuid,'admin.email','Admin','E-mail log','/admin/email',true,now(),'EmailOutbox',0);
            INSERT INTO advance.role_page_permissions(
              "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject",
              "CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport",
              "CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
            SELECT md5('admin.email:'||r."Code")::uuid, r."Id", md5('admin.email')::uuid, true, false, r."Code"='TECHNICAL_DIRECTOR', false, false, false, false, false,
              false, false, false, false, false, false, false, false, false, false, false, true, false, now(), 'EmailOutbox', 0
            FROM advance.roles r WHERE r."Code" IN ('TECHNICAL_DIRECTOR','IT_MANAGER') AND r."IsActive";
            DO $guard$ BEGIN
              IF (SELECT count(*) FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('admin.email')::uuid) <> 2 THEN
                RAISE EXCEPTION 'E-mail log page expected exactly two role grants (TD, IT Manager).';
              END IF;
            END $guard$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.email_outbox) THEN
                RAISE EXCEPTION 'E-mail outbox rollback refuses the retained e-mail log.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('admin.email')::uuid;
            DELETE FROM advance.page_definitions WHERE "PageKey"='admin.email';
            DROP TABLE advance.email_outbox;
            DROP FUNCTION advance.guard_email_outbox_retention();
            """);
    }
}
