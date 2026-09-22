using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260919130000_VendorManualAssessments")]
public sealed class VendorManualAssessments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Manual vendor assessment requires PostgreSQL 17 and an application database.'; END IF;
             IF to_regclass('advance.vendor_manual_assessments') IS NOT NULL
              OR to_regprocedure('advance.guard_vendor_manual_assessment()') IS NOT NULL
              OR to_regprocedure('advance.vendor_manual_assessment_json(uuid,uuid)') IS NOT NULL
              OR to_regprocedure('advance.vendor_manual_assessment_history(uuid,uuid)') IS NOT NULL
              OR to_regprocedure('advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)') IS NOT NULL
              OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='quality.vendor-manual-assessments')
             THEN RAISE EXCEPTION 'Manual assessment package is already or partially installed.'; END IF;
             IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId" WHERE d."PageKey"='qc.inspection-policies' AND r."Code"='QC_MANAGER')<>1
             THEN RAISE EXCEPTION 'Manual assessment requires the established QC Manager permission source.'; END IF;
            END $guard$;
            """);
        using var stream = typeof(VendorManualAssessments).Assembly.GetManifestResourceStream("VendorManualAssessments.20260919130000.sql")
            ?? throw new InvalidOperationException("Missing manual vendor assessment SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('quality.vendor-manual-assessments')::uuid,'quality.vendor-manual-assessments','Quality','Vendor manual assessments',
              '/quality/vendor-manual-assessments',true,now(),'VendorManualAssessments',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('quality.vendor-manual-assessments:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('quality.vendor-manual-assessments')::uuid,
             'CanView',true,'CanCreate',true,'CanDownload',false,'CanUploadAttachment',false,'CanViewCommercialValues',false,'CanViewAuditHistory',true,
             'CanIssue',false,'CanApprove',false,'CanReject',false,'CanDeactivate',false,'CanUpdate',false,'CanSubmit',false,'CanVerify',false,
             'CanRequestClarification',false,'CanRequestRevision',false,'CanResubmit',false,'CanCancel',false,'CanPrint',false,'CanExport',false,
             'CanReplaceAttachment',false,'HasFullControl',false,'CreatedAt',now(),'CreatedBy','VendorManualAssessments','Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            JOIN advance.roles r ON r."Id"=source."RoleId"
            WHERE d."PageKey"='qc.inspection-policies' AND r."Code"='QC_MANAGER';
            """);
        migrationBuilder.Sql(VendorManualAssessment20260919AccessSql.Provision);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Manual assessment rollback refuses this database.'; END IF;
             IF to_regclass('advance.vendor_manual_assessments') IS NULL THEN RAISE EXCEPTION 'Manual assessment package is absent.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.vendor_manual_assessments)
             THEN RAISE EXCEPTION 'Manual assessment rollback refuses retained business evidence.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('quality.vendor-manual-assessments')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('quality.vendor-manual-assessments')::uuid;
            DROP FUNCTION advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text);
            DROP FUNCTION advance.vendor_manual_assessment_history(uuid,uuid);
            DROP FUNCTION advance.vendor_manual_assessment_json(uuid,uuid);
            DROP TABLE advance.vendor_manual_assessments;
            DROP FUNCTION advance.guard_vendor_manual_assessment();
            """);
    }
}
