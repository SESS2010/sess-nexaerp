using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914010000_PurchaseWorkload")]
public sealed class PurchaseWorkload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.Guard(false));
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.PermissionUp);
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.Definition);
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.Ownership);
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.Guard(true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseWorkloadMigrationSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.page_definitions
                WHERE "PageKey"='dashboards.purchase' AND ("CreatedBy"<>'PurchaseWorkload' OR "Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions rp JOIN advance.page_definitions p
                  ON p."Id"=rp."PageDefinitionId" WHERE p."PageKey"='dashboards.purchase'
                    AND(rp."CreatedBy"<>'PurchaseWorkload' OR rp."Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.employee_page_permissions ep JOIN advance.page_definitions p
                  ON p."Id"=ep."PageDefinitionId" WHERE p."PageKey"='dashboards.purchase') THEN
                RAISE EXCEPTION 'Purchase workload rollback refuses changed runtime permissions.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=
              (SELECT "Id" FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase');
            DELETE FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase';
            DROP FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer);
            """);
    }
}

internal static class PurchaseWorkloadMigrationSql
{
    internal const string Signature = "advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)";
    internal static string Definition
    {
        get
        {
            using var stream = typeof(PurchaseWorkloadMigrationSql).Assembly
                .GetManifestResourceStream("PurchaseWorkload.20260914010000.sql")
                ?? throw new InvalidOperationException("Purchase workload SQL resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n");
        }
    }

    internal const string PermissionUp = """
        INSERT INTO advance.page_definitions
          ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
        VALUES(md5('dashboards.purchase')::uuid,'dashboards.purchase','Dashboards',
          'Purchase workload','/dashboards/purchase',true,now(),'PurchaseWorkload',0);
        INSERT INTO advance.role_page_permissions(
          "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
          "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
          "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
          "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('dashboards.purchase:'||r."Id"::text)::uuid,r."Id",md5('dashboards.purchase')::uuid,
          true,false,false,false,false,false,
          false,false,false,false,false,false,
          false,false,false,false,false,false,
          source."CanViewCommercialValues" OR source."HasFullControl",false,false,now(),'PurchaseWorkload',0
        FROM advance.roles r
        JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
        JOIN advance.page_definitions baseline ON baseline."Id"=source."PageDefinitionId"
          AND baseline."PageKey"='purchase.po' AND baseline."IsActive"
        WHERE r."IsActive" AND r."Code" IN('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
          AND(source."CanView" OR source."HasFullControl");
        """;

    internal const string Ownership = """
        REVOKE ALL ON FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer) FROM PUBLIC;
        DO $owner$ BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $owner$;
        """;

    internal static string Guard(bool installed)
    {
        const string cluster = """
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer<170000 OR
                lower(current_database()) IN('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Purchase workload refuses this cluster or protected database.';
              END IF;
            END $guard$;
            """;
        if (!installed) return cluster + """
            DO $guard$ BEGIN
              IF to_regprocedure('advance.purchase_workload(text,uuid,uuid[],text,text,text,bigint,integer)') IS NOT NULL
                OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase') THEN
                RAISE EXCEPTION 'Purchase workload refuses an existing or partial installation.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='purchase.po' AND "IsActive") THEN
                RAISE EXCEPTION 'Purchase workload requires the Purchase Order permission baseline.';
              END IF;
            END $guard$;
            """;
        var definition = Definition;
        var first = definition.IndexOf("$workload$", StringComparison.Ordinal);
        var last = definition.LastIndexOf("$workload$", StringComparison.Ordinal);
        if (first < 0 || last <= first) throw new InvalidOperationException("Purchase workload SQL delimiter changed.");
        var body = definition[(first + "$workload$".Length)..last].Replace("'", "''");
        return cluster + $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase')
                OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{Signature}}')
                  AND p.prosecdef AND p.provolatile='s'
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.purchase_orders'::regclass)
                  AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND
                      (a.grantee<>coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner) OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Purchase workload refuses changed function authority or definition.';
              END IF;
            END $guard$;
            """;
    }
}
