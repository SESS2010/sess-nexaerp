using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class LookupReachabilityCorrectionsSql
{
    internal static void ApplyUp(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Up);
    internal static void ApplyDown(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Down);

    internal const string Up = """
        LOCK TABLE advance.roles, advance.page_definitions, advance.role_page_permissions
          IN SHARE ROW EXCLUSIVE MODE;
        DO $lookup_grants$
        DECLARE missing text; existing_count integer; inserted_count integer;
        BEGIN
          WITH required(role_code,page_key) AS (VALUES
            ('TECHNICAL_SUPPORT_MANAGER'::text,'production.job-orders'::text),
            ('QC_MANAGER','production.job-orders'),
            ('TECHNICAL_SUPPORT_MANAGER','masters.uoms'),
            ('PRODUCTION_MANAGER','masters.uoms'))
          SELECT string_agg(required.role_code||':'||required.page_key,', ' ORDER BY required.role_code,required.page_key) INTO missing
          FROM required
          LEFT JOIN advance.roles role ON role."Code"=required.role_code AND role."IsActive"
          LEFT JOIN advance.page_definitions page ON page."PageKey"=required.page_key AND page."IsActive"
          WHERE role."Id" IS NULL OR page."Id" IS NULL;
          IF missing IS NOT NULL THEN
            RAISE EXCEPTION USING ERRCODE='55000',MESSAGE='Lookup reachability prerequisites are missing: '||missing;
          END IF;

          WITH required(role_code,page_key) AS (VALUES
            ('TECHNICAL_SUPPORT_MANAGER'::text,'production.job-orders'::text),
            ('QC_MANAGER','production.job-orders'),
            ('TECHNICAL_SUPPORT_MANAGER','masters.uoms'),
            ('PRODUCTION_MANAGER','masters.uoms'))
          SELECT count(*) INTO existing_count
          FROM required
          JOIN advance.roles role ON role."Code"=required.role_code
          JOIN advance.page_definitions page ON page."PageKey"=required.page_key
          JOIN advance.role_page_permissions permission
            ON permission."RoleId"=role."Id" AND permission."PageDefinitionId"=page."Id";
          IF existing_count<>0 THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE=format('Lookup reachability expected four absent grants; %s already exist.',existing_count);
          END IF;

          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
             "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
             "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
             "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('lookup-reachability:'||required.role_code||':'||required.page_key)::uuid,
            role."Id",page."Id",true,false,false,false,false,false,false,false,false,false,false,false,false,
            false,false,false,false,false,false,false,false,clock_timestamp(),'LookupReachabilityCorrections',0
          FROM (VALUES
            ('TECHNICAL_SUPPORT_MANAGER'::text,'production.job-orders'::text),
            ('QC_MANAGER','production.job-orders'),
            ('TECHNICAL_SUPPORT_MANAGER','masters.uoms'),
            ('PRODUCTION_MANAGER','masters.uoms')) required(role_code,page_key)
          JOIN advance.roles role ON role."Code"=required.role_code AND role."IsActive"
          JOIN advance.page_definitions page ON page."PageKey"=required.page_key AND page."IsActive";
          GET DIAGNOSTICS inserted_count=ROW_COUNT;
          IF inserted_count<>4 THEN
            RAISE EXCEPTION USING ERRCODE='55000',MESSAGE=format('Lookup reachability expected four inserts; inserted %s.',inserted_count);
          END IF;
        END $lookup_grants$;
        """;

    internal const string Down = """
        LOCK TABLE advance.role_page_permissions IN SHARE ROW EXCLUSIVE MODE;
        DO $lookup_grants_down$
        DECLARE owned_count integer; drift_count integer;
        BEGIN
          SELECT count(*),count(*) FILTER (WHERE NOT "CanView" OR "CanCreate" OR "CanUpdate" OR "CanSubmit"
            OR "CanIssue" OR "CanVerify" OR "CanApprove" OR "CanReject" OR "CanRequestClarification"
            OR "CanRequestRevision" OR "CanResubmit" OR "CanCancel" OR "CanDeactivate" OR "CanPrint"
            OR "CanDownload" OR "CanExport" OR "CanUploadAttachment" OR "CanReplaceAttachment"
            OR "CanViewCommercialValues" OR "CanViewAuditHistory" OR "HasFullControl"
            OR "UpdatedAt" IS NOT NULL OR "UpdatedBy" IS NOT NULL OR "Version"<>0)
          INTO owned_count,drift_count
          FROM advance.role_page_permissions WHERE "CreatedBy"='LookupReachabilityCorrections';
          IF owned_count<>4 OR drift_count<>0 THEN
            RAISE EXCEPTION USING ERRCODE='55000',
              MESSAGE=format('Lookup reachability Down refuses changed or incomplete grants: rows=%s drift=%s.',owned_count,drift_count);
          END IF;
          DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='LookupReachabilityCorrections';
        END $lookup_grants_down$;
        """;
}