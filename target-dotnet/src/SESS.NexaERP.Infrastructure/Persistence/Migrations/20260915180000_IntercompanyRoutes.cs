using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Database;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260915180000_IntercompanyRoutes")]
public sealed class IntercompanyRoutes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany routes require PostgreSQL 17 and an application database.'; END IF;
             IF to_regclass('advance.intercompany_routes') IS NOT NULL OR to_regclass('advance.intercompany_route_decisions') IS NOT NULL
              OR to_regprocedure('advance.intercompany_route_options(uuid)') IS NOT NULL
             THEN RAISE EXCEPTION 'Intercompany route package already exists.'; END IF;
             IF to_regclass('advance.company_sites') IS NULL OR to_regclass('advance.company_gst_registrations') IS NULL
              OR to_regclass('advance.vendor_company_relationships') IS NULL OR to_regclass('advance.customer_company_relationships') IS NULL
              OR to_regprocedure('advance.read_command_receipt(uuid)') IS NULL
              OR to_regprocedure('advance.resolve_employee_role_authority(uuid,uuid,date,text,text[])') IS NULL
             THEN RAISE EXCEPTION 'Intercompany routes require company master governance and the ordinary command ledger.'; END IF;
             IF (SELECT count(*) FROM advance.role_page_permissions p JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId" WHERE d."PageKey"='settings.tax-gst'
               AND r."Code" IN('ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))<>3
             THEN RAISE EXCEPTION 'Intercompany routes require the three established configuration role permissions.'; END IF;
            END $guard$;
            """);
        using var stream = typeof(IntercompanyRoutes).Assembly.GetManifestResourceStream("IntercompanyRoutes.20260915180000.sql")
            ?? throw new InvalidOperationException("Missing frozen intercompany route migration SQL.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
        migrationBuilder.Sql("""
            INSERT INTO advance.page_definitions("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
             VALUES(md5('stores.intercompany-routes')::uuid,'stores.intercompany-routes','Stores','Intercompany routes','/stores/intercompany-routes',true,now(),'IntercompanyRoutes',0);
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(source)||jsonb_build_object(
             'Id',md5('stores.intercompany-routes:'||source."RoleId"::text)::uuid,'PageDefinitionId',md5('stores.intercompany-routes')::uuid,
             'CanView',true,'CanCreate',r."Code"='ACCOUNTS_MANAGER','CanApprove',r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),
             'CanReject',r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),'CanDeactivate',r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'),
             'CanUpdate',false,'CanSubmit',false,'CanIssue',false,'CanVerify',false,'CanRequestClarification',false,'CanRequestRevision',false,
             'CanResubmit',false,'CanCancel',false,'CanPrint',false,'CanDownload',false,'CanExport',false,'CanUploadAttachment',false,
             'CanReplaceAttachment',false,'CanViewCommercialValues',false,'CanViewAuditHistory',true,'HasFullControl',false,
             'CreatedAt',now(),'CreatedBy','IntercompanyRoutes','Version',0))).*
            FROM advance.role_page_permissions source JOIN advance.page_definitions d ON d."Id"=source."PageDefinitionId"
            JOIN advance.roles r ON r."Id"=source."RoleId"
            WHERE d."PageKey"='settings.tax-gst' AND r."Code" IN('ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR');
            """);
        migrationBuilder.Sql(IntercompanyRoutes20260915AccessSql.Provision);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
             IF current_setting('server_version_num')::integer<170000 OR current_database() IN('postgres','template0','template1')
             THEN RAISE EXCEPTION 'Intercompany route rollback refuses this database.'; END IF;
             IF to_regclass('advance.intercompany_routes') IS NULL OR to_regclass('advance.intercompany_route_decisions') IS NULL
              OR to_regprocedure('advance.record_intercompany_route(uuid,uuid,uuid,text,jsonb,uuid,text,uuid,text,text)') IS NULL
             THEN RAISE EXCEPTION 'Intercompany route rollback requires the complete package.'; END IF;
             IF EXISTS(SELECT 1 FROM advance.intercompany_routes) OR EXISTS(SELECT 1 FROM advance.intercompany_route_decisions)
             THEN RAISE EXCEPTION 'Intercompany route rollback refuses retained business evidence.'; END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=md5('stores.intercompany-routes')::uuid;
            DELETE FROM advance.page_definitions WHERE "Id"=md5('stores.intercompany-routes')::uuid;
            DROP FUNCTION advance.record_intercompany_route(uuid,uuid,uuid,text,jsonb,uuid,text,uuid,text,text);
            DROP FUNCTION advance.intercompany_routes_page(uuid,integer,integer);
            DROP FUNCTION advance.intercompany_route_json(uuid,uuid);
            DROP FUNCTION advance.intercompany_route_options(uuid);
            DROP FUNCTION advance.validate_intercompany_route(advance.intercompany_routes);
            DROP TABLE advance.intercompany_route_decisions;
            DROP TABLE advance.intercompany_routes;
            DROP FUNCTION advance.guard_intercompany_route_evidence();
            """);
    }
}
