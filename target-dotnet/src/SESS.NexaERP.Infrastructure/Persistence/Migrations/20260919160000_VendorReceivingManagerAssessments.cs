using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;
namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260919160000_VendorReceivingManagerAssessments")]
public sealed class VendorReceivingManagerAssessments : Migration
{
    private const string Signature = "advance.record_vendor_manual_assessment(uuid,uuid,uuid,bigint,uuid,numeric,numeric,numeric,text,uuid,text,uuid,text,text)";
    private const string Marker = "VendorReceivingManagerAssessments";
    private static string OriginalFunction()
    {
        using var stream=typeof(VendorReceivingManagerAssessments).Assembly.GetManifestResourceStream("VendorManualAssessments.20260919130000.sql")
            ?? throw new InvalidOperationException("Missing retained manual assessment SQL.");
        using var reader=new StreamReader(stream);
        var sql=reader.ReadToEnd().Replace("\r\n","\n",StringComparison.Ordinal);
        var start=sql.IndexOf("CREATE FUNCTION advance.record_vendor_manual_assessment(",StringComparison.Ordinal);
        if(start<0)throw new InvalidOperationException("The retained assessment function is absent.");
        var end=sql.IndexOf("END $rating$;",start,StringComparison.Ordinal);
        if(end<0)throw new InvalidOperationException("The retained assessment function is incomplete.");
        return sql[start..(end+"END $rating$;".Length)].Replace("CREATE FUNCTION ","CREATE OR REPLACE FUNCTION ",StringComparison.Ordinal);
    }
    private static string Once(string value,string before,string after)
    {
        if(value.Split(before,StringSplitOptions.None).Length!=2)throw new InvalidOperationException("Retained assessment source differs from the reviewed baseline.");
        return value.Replace(before,after,StringComparison.Ordinal);
    }
    private static string ExtendedFunction()
    {
        var sql=Once(OriginalFunction(),"p_role IS DISTINCT FROM 'QC_MANAGER'","(p_role IS NULL OR p_role NOT IN ('QC_MANAGER','PRODUCTION_MANAGER'))");
        sql=Once(sql,"Manual assessment requires the exact governed QC Manager command.",
            "Assessment requires the exact governed QC or receiving Production Manager command.");
        return Once(sql,
            "SELECT * INTO receipt FROM advance.goods_receipts WHERE \"CompanyId\"=p_company AND \"Id\"=p_receipt FOR UPDATE;",
            """
            SELECT * INTO receipt FROM advance.goods_receipts WHERE "CompanyId"=p_company AND "Id"=p_receipt FOR UPDATE;
             IF p_role='PRODUCTION_MANAGER' AND (receipt."Id" IS NULL OR receipt."ReceivedByEmployeeId" IS DISTINCT FROM p_actor)
             THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Production Manager may assess only a GRN they personally received.'; END IF;
            """);
    }
    private static string Body(string sql) => sql.Split("$rating$",StringSplitOptions.None)[1];
    private static string FunctionGuard(string expected) => $"""
        DO $guard$ BEGIN
         IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
         THEN RAISE EXCEPTION 'Receiving-manager assessment requires PostgreSQL 17 and an application database.'; END IF;
         IF (SELECT replace(prosrc,chr(13)||chr(10),chr(10)) FROM pg_proc WHERE oid=to_regprocedure('{Signature}')) IS DISTINCT FROM $expected${Body(expected)}$expected$
         THEN RAISE EXCEPTION 'Manual assessment function differs from the reviewed authority contract.'; END IF;
        END $guard$;
        """;
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FunctionGuard(OriginalFunction()));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF (SELECT count(*) FROM advance.roles WHERE "Code"='PRODUCTION_MANAGER')<>1
              OR (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
               JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
               WHERE r."Code"='QC_MANAGER' AND d."PageKey"='quality.vendor-manual-assessments' AND d."IsActive"
                 AND p."CanView" AND p."CanCreate" AND p."CanViewAuditHistory" AND NOT p."HasFullControl"
                 AND NOT EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(p)) flag
                  WHERE (flag.key LIKE 'Can%' OR flag.key='HasFullControl')
                   AND flag.key NOT IN('CanView','CanCreate','CanViewAuditHistory') AND flag.value IS DISTINCT FROM 'false'::jsonb))<>1
             THEN RAISE EXCEPTION 'Receiving-manager authority requires the established manual-assessment permission source.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
               JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
               WHERE r."Code"='PRODUCTION_MANAGER' AND d."PageKey"='quality.vendor-manual-assessments')
             THEN RAISE EXCEPTION 'Receiving-manager permission is already or partially installed.'; END IF;
            END $guard$;
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('quality.vendor-manual-assessments:'||target."Id"::text)::uuid,'RoleId',target."Id",
             'CreatedAt',now(),'CreatedBy','VendorReceivingManagerAssessments','UpdatedAt',NULL,'UpdatedBy',NULL,'Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            JOIN advance.roles original ON original."Id"=source."RoleId" CROSS JOIN advance.roles target
            WHERE d."PageKey"='quality.vendor-manual-assessments' AND original."Code"='QC_MANAGER' AND target."Code"='PRODUCTION_MANAGER';
            """);
        migrationBuilder.Sql(ExtendedFunction());
        migrationBuilder.Sql(VendorManualAssessment20260919AccessSql.Provision);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FunctionGuard(ExtendedFunction()));
        migrationBuilder.Sql($"""
            DO $guard$ BEGIN
             IF EXISTS(SELECT 1 FROM advance.vendor_manual_assessments a JOIN advance.command_requests r ON r."CommandId"=a."Id"
               WHERE r."ActorRoleCode"='PRODUCTION_MANAGER')
             THEN RAISE EXCEPTION 'Receiving-manager rollback refuses retained Production Manager assessment evidence.'; END IF;
             IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.roles r ON r."Id"=p."RoleId"
               JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
               JOIN advance.roles qr ON qr."Code"='QC_MANAGER'
               JOIN advance.role_page_permissions q ON q."RoleId"=qr."Id" AND q."PageDefinitionId"=d."Id"
               WHERE r."Code"='PRODUCTION_MANAGER' AND d."PageKey"='quality.vendor-manual-assessments'
                AND p."Id"=md5('quality.vendor-manual-assessments:'||r."Id"::text)::uuid
                AND p."CreatedBy"='{Marker}' AND p."Version"=0 AND p."UpdatedAt" IS NULL AND p."UpdatedBy" IS NULL
                AND (to_jsonb(p)-ARRAY['Id','RoleId','CreatedAt','CreatedBy','UpdatedAt','UpdatedBy'])
                  =(to_jsonb(q)-ARRAY['Id','RoleId','CreatedAt','CreatedBy','UpdatedAt','UpdatedBy']))<>1
             THEN RAISE EXCEPTION 'Receiving-manager rollback refuses changed or absent permission evidence.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions p USING advance.roles r,advance.page_definitions d
             WHERE r."Id"=p."RoleId" AND d."Id"=p."PageDefinitionId" AND r."Code"='PRODUCTION_MANAGER'
              AND d."PageKey"='quality.vendor-manual-assessments';
            """);
        migrationBuilder.Sql(OriginalFunction());
        migrationBuilder.Sql(VendorManualAssessment20260919AccessSql.Provision);
    }
}
