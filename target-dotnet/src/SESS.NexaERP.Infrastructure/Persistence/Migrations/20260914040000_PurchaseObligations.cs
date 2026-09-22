using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914040000_PurchaseObligations")]
public sealed class PurchaseObligations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.Guard(false));
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.PermissionUp);
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.Definition);
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.Ownership);
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.Guard(true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(PurchaseObligationsMigrationSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.page_definitions
                WHERE "PageKey"='dashboards.purchase-obligations' AND ("CreatedBy"<>'PurchaseObligations' OR "Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions rp JOIN advance.page_definitions p
                  ON p."Id"=rp."PageDefinitionId" WHERE p."PageKey"='dashboards.purchase-obligations'
                    AND(rp."CreatedBy"<>'PurchaseObligations' OR rp."Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.employee_page_permissions ep JOIN advance.page_definitions p
                  ON p."Id"=ep."PageDefinitionId" WHERE p."PageKey"='dashboards.purchase-obligations') THEN
                RAISE EXCEPTION 'Purchase obligations rollback refuses changed runtime permissions.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=
              (SELECT "Id" FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations');
            DELETE FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations';
            DROP FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer);
            """);
    }
}

internal static class PurchaseObligationsMigrationSql
{
    internal const string Signature = "advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)";
    internal static string Definition
    {
        get
        {
            using var stream = typeof(PurchaseObligationsMigrationSql).Assembly
                .GetManifestResourceStream("PurchaseObligations.20260914040000.sql")
                ?? throw new InvalidOperationException("Purchase obligations SQL resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n");
        }
    }

    internal const string PermissionUp = """
        INSERT INTO advance.page_definitions
          ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
        VALUES(md5('dashboards.purchase-obligations')::uuid,'dashboards.purchase-obligations','Dashboards',
          'Purchase obligations','/dashboards/purchase/obligations',true,now(),'PurchaseObligations',0);
        INSERT INTO advance.role_page_permissions(
          "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
          "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
          "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
          "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('dashboards.purchase-obligations:'||r."Id"::text)::uuid,r."Id",md5('dashboards.purchase-obligations')::uuid,
          true,false,false,false,false,false,
          false,false,false,false,false,false,
          false,false,false,false,false,false,
          source."CanViewCommercialValues" OR source."HasFullControl",false,false,now(),'PurchaseObligations',0
        FROM advance.roles r
        JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
        JOIN advance.page_definitions baseline ON baseline."Id"=source."PageDefinitionId"
          AND baseline."PageKey"='purchase.po' AND baseline."IsActive"
        WHERE r."IsActive" AND r."Code" IN('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
          AND(source."CanView" OR source."HasFullControl");
        """;

    internal const string Ownership = """
        REVOKE ALL ON FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer) FROM PUBLIC;
        DO $owner$ BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $owner$;
        """;

    internal static string Guard(bool installed)
    {
        const string cluster = """
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer<170000 OR
                lower(current_database()) IN('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Purchase obligations refuses this cluster or protected database.';
              END IF;
            END $guard$;
            """;
        if (!installed) return cluster + """
            DO $guard$ BEGIN
              IF to_regprocedure('advance.purchase_obligations(text,uuid,uuid[],text,text,uuid,text,uuid,bigint,integer)') IS NOT NULL
                OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations') THEN
                RAISE EXCEPTION 'Purchase obligations refuses an existing or partial installation.';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='purchase.po' AND "IsActive") THEN
                RAISE EXCEPTION 'Purchase obligations requires the Purchase Order permission baseline.';
              END IF;
            END $guard$;
            """;
        var definition = Definition;
        var first = definition.IndexOf("$obligations$", StringComparison.Ordinal);
        var last = definition.LastIndexOf("$obligations$", StringComparison.Ordinal);
        if (first < 0 || last <= first) throw new InvalidOperationException("Purchase obligations SQL delimiter changed.");
        var body = definition[(first + "$obligations$".Length)..last].Replace("'", "''");
        return cluster + $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.purchase-obligations')
                OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{Signature}}')
                  AND p.prosecdef AND p.provolatile='s'
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.purchase_orders'::regclass)
                  AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND
                      (a.grantee<>coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner) OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Purchase obligations refuses changed function authority or definition.';
              END IF;
            END $guard$;
            """;
    }
}
