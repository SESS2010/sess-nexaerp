using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913100000_VendorPoCashCap")]
public sealed class VendorPoCashCap : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorPoCashCapMigrationSql.Guard(false));
        migrationBuilder.Sql(VendorPoCashCapSql.Create);
        migrationBuilder.Sql(VendorPoCashCapSql.AdvanceAfter);
        migrationBuilder.Sql(VendorPoCashCapSql.PaymentAfter);
        migrationBuilder.Sql(VendorPoCashCapSql.OptionsAfter);
        migrationBuilder.Sql(VendorPoCashCapMigrationSql.Ownership);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorPoCashCapMigrationSql.Guard(true));
        migrationBuilder.Sql("DROP TRIGGER trg_purchase_order_vendor_cash ON advance.purchase_orders;");
        migrationBuilder.Sql(VendorPoCashCapSql.AdvanceBefore);
        migrationBuilder.Sql(VendorPoCashCapSql.PaymentBefore);
        migrationBuilder.Sql(VendorPoCashCapSql.OptionsBefore);
        migrationBuilder.Sql("""
            DROP FUNCTION advance.guard_purchase_order_vendor_cash();
            DROP FUNCTION advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text);
            DROP FUNCTION advance.vendor_po_cash_totals(uuid,uuid,text,uuid);
            DROP INDEX advance."IX_vendor_advances_company_po";
            """);
    }
}

internal static class VendorPoCashCapMigrationSql
{
    private static readonly (string Signature,string Name)[] Helpers =
    [
        (VendorPoCashCapSql.TotalsSignature,"vendor_po_cash_totals"),
        (VendorPoCashCapSql.LimitSignature,"require_vendor_po_cash_limit"),
        (VendorPoCashCapSql.TriggerSignature,"guard_purchase_order_vendor_cash")
    ];

    internal static string Ownership
    {
        get
        {
            var signatures = string.Join(",", Helpers.Select(x => x.Signature));
            return "REVOKE ALL ON FUNCTION " + signatures + " FROM PUBLIC;\nDO $owner$ BEGIN\n"
                + "IF to_regrole('nexa_erp_owner') IS NOT NULL THEN\n"
                + string.Join("\n", Helpers.Select(x => "ALTER FUNCTION " + x.Signature + " OWNER TO nexa_erp_owner;"))
                + "\nREVOKE ALL ON FUNCTION " + signatures + " FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;\n"
                + "END IF; END $owner$;";
        }
    }

    internal static string Guard(bool installed)
    {
        var sql = """
            DO $guard$ BEGIN
              IF current_setting('server_version_num')::integer<170000
                OR lower(current_database()) IN('postgres','template0','template1') THEN
                RAISE EXCEPTION 'Vendor cash migration refuses this cluster or protected database.';
              END IF;
            END $guard$;
            """;
        if (!installed)
        {
            sql += VendorBankAdviceMigrationSql.Guard(true) + """
                DO $guard$ BEGIN
                  IF to_regprocedure('advance.vendor_po_cash_totals(uuid,uuid,text,uuid)') IS NOT NULL
                    OR to_regprocedure('advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)') IS NOT NULL
                    OR to_regprocedure('advance.guard_purchase_order_vendor_cash()') IS NOT NULL
                    OR to_regclass('advance."IX_vendor_advances_company_po"') IS NOT NULL
                    OR EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.purchase_orders'::regclass
                      AND tgname='trg_purchase_order_vendor_cash') THEN
                    RAISE EXCEPTION 'Vendor cash migration refuses an existing or partial installation.';
                  END IF;
                END $guard$;
                """;
        }
        else
        {
            sql += """
                DO $guard$ BEGIN
                  IF NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.purchase_orders'::regclass
                    AND tgname='trg_purchase_order_vendor_cash' AND tgenabled='O' AND tgtype=23
                    AND tgqual IS NULL AND tgnargs=0 AND tgattr=''::int2vector AND tgconstraint=0
                    AND NOT tgisinternal AND tgfoid='advance.guard_purchase_order_vendor_cash()'::regprocedure)
                    OR NOT EXISTS(
                      SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid
                      JOIN pg_class t ON t.oid=i.indrelid JOIN pg_am a ON a.oid=c.relam
                      WHERE i.indexrelid=to_regclass('advance."IX_vendor_advances_company_po"')
                        AND i.indrelid='advance.vendor_advances'::regclass AND c.relowner=t.relowner
                        AND i.indisvalid AND i.indisready AND NOT i.indisunique AND a.amname='btree'
                        AND i.indnkeyatts=2 AND i.indnatts=2 AND i.indexprs IS NULL AND i.indpred IS NULL
                        AND ARRAY(SELECT att.attname::text FROM unnest(i.indkey::smallint[]) WITH ORDINALITY k(attnum,n)
                          JOIN pg_attribute att ON att.attrelid=i.indrelid AND att.attnum=k.attnum
                          ORDER BY k.n)=ARRAY['CompanyId','PurchaseOrderId']) THEN
                    RAISE EXCEPTION 'Vendor cash rollback refuses changed trigger or index protection.';
                  END IF;
                END $guard$;
                """;
            foreach (var helper in Helpers)
                sql += FunctionGuard(helper.Signature,
                    VendorPoCashCapSql.Extract(VendorPoCashCapSql.Create, helper.Name), false);
        }
        return sql
            + FunctionGuard(VendorPoCashCapSql.AdvanceSignature, installed ? VendorPoCashCapSql.AdvanceAfter : VendorPoCashCapSql.AdvanceBefore, true)
            + FunctionGuard(VendorPoCashCapSql.PaymentSignature, installed ? VendorPoCashCapSql.PaymentAfter : VendorPoCashCapSql.PaymentBefore, true)
            + FunctionGuard(VendorPoCashCapSql.OptionsSignature, installed ? VendorPoCashCapSql.OptionsAfter : VendorPoCashCapSql.OptionsBefore, true);
    }

    private static string FunctionGuard(string signature, string sql, bool runtime)
    {
        var start = sql.IndexOf("$f$", StringComparison.Ordinal);
        var end = sql.LastIndexOf("$f$", StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new InvalidOperationException("Financial function body was not found.");
        var body = sql[(start + 3)..end].Replace("\r\n", "\n").Replace("'", "''");
        var permitted = runtime ? "coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner)" : "p.proowner";
        return $$"""
            DO $guard$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{signature}}')
                AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.vendor_advances'::regclass)
                AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND (a.grantee<>{{permitted}} OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Vendor cash migration refuses an absent or changed function baseline: {{signature}}.';
              END IF;
            END $guard$;
            """;
    }
}
