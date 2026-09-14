using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260914070000_StoresQcStock")]
public sealed class StoresQcStock : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StoresQcStockMigrationSql.Guard(false));
        migrationBuilder.Sql(StoresQcStockMigrationSql.PermissionUp);
        migrationBuilder.Sql(StoresQcStockMigrationSql.Definition);
        migrationBuilder.Sql(StoresQcStockMigrationSql.Ownership);
        migrationBuilder.Sql(StoresQcStockMigrationSql.Guard(true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StoresQcStockMigrationSql.Guard(true));
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              IF EXISTS(SELECT 1 FROM advance.page_definitions
                WHERE "PageKey"='dashboards.stores-qc-stock' AND ("CreatedBy"<>'StoresQcStock' OR "Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.role_page_permissions rp JOIN advance.page_definitions p
                  ON p."Id"=rp."PageDefinitionId" WHERE p."PageKey"='dashboards.stores-qc-stock'
                    AND(rp."CreatedBy"<>'StoresQcStock' OR rp."Version"<>0))
                OR EXISTS(SELECT 1 FROM advance.employee_page_permissions ep JOIN advance.page_definitions p
                  ON p."Id"=ep."PageDefinitionId" WHERE p."PageKey"='dashboards.stores-qc-stock') THEN
                RAISE EXCEPTION 'Stores QC stock rollback refuses changed runtime permissions.';
              END IF;
            END $guard$;
            DELETE FROM advance.role_page_permissions WHERE "PageDefinitionId"=
              (SELECT "Id" FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock');
            DELETE FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock';
            DROP FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer);
            """);
    }
}

internal static class StoresQcStockMigrationSql
{
    internal const string Signature = "advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)";
    internal static string Definition
    {
        get
        {
            using var stream = typeof(StoresQcStockMigrationSql).Assembly
                .GetManifestResourceStream("StoresQcStock.20260914070000.sql")
                ?? throw new InvalidOperationException("Stores QC stock SQL resource is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n");
        }
    }

    internal const string PermissionUp = """
        INSERT INTO advance.page_definitions
          ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
        VALUES(md5('dashboards.stores-qc-stock')::uuid,'dashboards.stores-qc-stock','Dashboards',
          'Stores QC stock','/dashboards/stores/qc-stock',true,now(),'StoresQcStock',0);
        INSERT INTO advance.role_page_permissions(
          "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify",
          "CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel",
          "CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
          "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
        SELECT md5('dashboards.stores-qc-stock:'||r."Id"::text)::uuid,r."Id",md5('dashboards.stores-qc-stock')::uuid,
          true,false,false,false,false,false,
          false,false,false,false,false,false,
          false,false,false,false,false,false,
          bool_or(source."CanViewCommercialValues" OR source."HasFullControl"),false,false,now(),'StoresQcStock',0
        FROM advance.roles r
        JOIN advance.role_page_permissions source ON source."RoleId"=r."Id"
        JOIN advance.page_definitions baseline ON baseline."Id"=source."PageDefinitionId"
          AND baseline."PageKey" IN('inventory.grn') AND baseline."IsActive"
        WHERE r."IsActive" AND r."Code" IN('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')
          AND(source."CanView" OR source."HasFullControl") GROUP BY r."Id";
        """;

    internal const string Ownership = """
        REVOKE ALL ON FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer) FROM PUBLIC;
        DO $owner$ BEGIN
          IF to_regrole('nexa_erp_owner') IS NOT NULL THEN
            ALTER FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer) OWNER TO nexa_erp_owner;
            REVOKE ALL ON FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer) TO nexa_erp_runtime;
          END IF;
        END $owner$;
        """;

    internal static string Guard(bool installed)
    {
        const string cluster = """
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer<170000 OR
                lower(current_database()) IN('postgres','template0','template1','sess_nexaerp','sess_nexa_erp') THEN
                RAISE EXCEPTION 'Stores QC stock refuses this cluster or protected database.';
              END IF;
            END $guard$;
            """;
        if (!installed) return cluster + """
            DO $guard$ BEGIN
              IF to_regprocedure('advance.stores_qc_stock(text,uuid,uuid[],text,text,uuid,bigint,integer)') IS NOT NULL
                OR EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock') THEN
                RAISE EXCEPTION 'Stores QC stock refuses an existing or partial installation.';
              END IF;
              IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey" IN('inventory.grn') AND "IsActive")<>1 THEN
                RAISE EXCEPTION 'Stores QC stock requires the GRN permission baseline.';
              END IF;
            END $guard$;
            """;
        var definition = Definition;
        var first = definition.IndexOf("$storesqcstock$", StringComparison.Ordinal);
        var last = definition.LastIndexOf("$storesqcstock$", StringComparison.Ordinal);
        if (first < 0 || last <= first) throw new InvalidOperationException("Stores QC stock SQL delimiter changed.");
        var body = definition[(first + "$storesqcstock$".Length)..last].Replace("'", "''");
        return cluster + $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='dashboards.stores-qc-stock')
                OR NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{Signature}}')
                  AND p.prosecdef AND p.provolatile='s'
                  AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                  AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.goods_receipts'::regclass)
                  AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                  AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                    WHERE a.grantee<>p.proowner AND
                      (a.grantee<>coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner) OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Stores QC stock refuses changed function authority or definition.';
              END IF;
            END $guard$;
            """;
    }
}
