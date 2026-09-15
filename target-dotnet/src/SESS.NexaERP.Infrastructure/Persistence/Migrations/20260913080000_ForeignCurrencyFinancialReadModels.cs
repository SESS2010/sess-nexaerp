using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260913080000_ForeignCurrencyFinancialReadModels")]
public sealed class ForeignCurrencyFinancialReadModels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorCurrencyReadSql.Guard(false));
        migrationBuilder.Sql(VendorCurrencyReadSql.PayablesAfter);
        migrationBuilder.Sql(VendorCurrencyReadSql.PositionsAfter);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(VendorCurrencyReadSql.Guard(true));
        migrationBuilder.Sql(VendorCurrencyReadSql.PositionsBefore);
        migrationBuilder.Sql(VendorCurrencyReadSql.PayablesBefore);
    }
}

internal static class VendorCurrencyReadSql
{
    internal static string PayablesBefore => Original("list_vendor_payables", "CREATE FUNCTION advance.list_vendor_positions(");
    internal static string PositionsBefore => Original("list_vendor_positions", "INSERT INTO advance.page_definitions(");
    internal static string PayablesAfter => ReplaceOnce(ReplaceOnce(PayablesBefore,
        "po.\"PaymentTermsSnapshot\",v.\"VendorCode\"",
        "po.\"PaymentTermsSnapshot\",po.\"CurrencyCode\" payment_currency,v.\"VendorCode\""),
        "'billDate',\"BillDate\",'acceptedAt',\"DecidedAt\",",
        "'billDate',\"BillDate\",'acceptedAt',\"DecidedAt\",'currencyCode',payment_currency,");

    internal const string PositionsAfter = """
CREATE OR REPLACE FUNCTION advance.list_vendor_positions(p_company uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH advance_rows AS MATERIALIZED (
 SELECT a."VendorId",a."CurrencyCode",a."PurchaseOrderId",po."Status",
  (advance.vendor_advance_json(p_company,a."Id",false)->>'outstandingAmount')::numeric outstanding
 FROM advance.vendor_advances a
 JOIN advance.purchase_orders po ON po."Id"=a."PurchaseOrderId"
 WHERE a."CompanyId"=p_company
), advance_totals AS (
 SELECT "VendorId","CurrencyCode",sum(outstanding) total,
  count(DISTINCT "PurchaseOrderId") FILTER(WHERE "Status"='Cancelled' AND outstanding>0) cancelled
 FROM advance_rows GROUP BY "VendorId","CurrencyCode"
), payable_totals AS (
 SELECT (j->>'vendorId')::uuid "VendorId",j->>'currencyCode' "CurrencyCode",
  sum((j->>'outstandingValue')::numeric) total
 FROM jsonb_array_elements(advance.list_vendor_payables(p_company,NULL,false)) j
 GROUP BY (j->>'vendorId')::uuid,j->>'currencyCode'
), currencies AS (
 SELECT "VendorId","CurrencyCode" FROM advance_totals
 UNION
 SELECT "VendorId","CurrencyCode" FROM payable_totals
), positions AS (
 SELECT v."Id",v."VendorCode",v."Name",k."CurrencyCode",
  coalesce(a.total,0) advances,coalesce(b.total,0) bills,coalesce(a.cancelled,0) cancelled
 FROM currencies k
 JOIN advance.vendors v ON v."Id"=k."VendorId"
 LEFT JOIN advance_totals a ON a."VendorId"=k."VendorId" AND a."CurrencyCode"=k."CurrencyCode"
 LEFT JOIN payable_totals b ON b."VendorId"=k."VendorId" AND b."CurrencyCode"=k."CurrencyCode"
)
SELECT coalesce(jsonb_agg(jsonb_build_object(
 'vendorId',"Id",'vendorCode',"VendorCode",'vendorName',"Name",'currencyCode',"CurrencyCode",
 'outstandingAdvance',advances,'outstandingBills',bills,'netPayable',bills-advances,
 'cancelledPurchaseOrdersWithOutstandingAdvance',cancelled)
 ORDER BY "VendorCode","CurrencyCode"),'[]'::jsonb)
FROM positions WHERE advances<>0 OR bills<>0;
$f$;
""";

    private static string Original(string function, string endMarker)
    {
        var source = VendorAdvancePaymentSql.Up.Replace("\r\n", "\n");
        var start = source.IndexOf("CREATE FUNCTION advance." + function + "(", StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start < 0 ? 0 : start, StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new InvalidOperationException("Immutable financial read function was not found.");
        return source[start..end].Replace("CREATE FUNCTION advance.", "CREATE OR REPLACE FUNCTION advance.", StringComparison.Ordinal);
    }
    private static string ReplaceOnce(string source, string before, string after)
    {
        var index = source.IndexOf(before, StringComparison.Ordinal);
        if (index < 0 || source.IndexOf(before, index + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("Immutable payable currency projection anchor changed.");
        return source[..index] + after + source[(index + before.Length)..];
    }

    internal static string Guard(bool installed) => """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer<170000
            OR lower(current_database()) IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Currency read migration refuses this cluster or protected database.';
          END IF;
          IF to_regclass('advance.vendor_advances') IS NULL THEN
            RAISE EXCEPTION 'Currency read migration requires vendor financial evidence.';
          END IF;
        END $guard$;
        """
        + FunctionGuard("advance.list_vendor_payables(uuid,uuid,boolean)", installed ? PayablesAfter : PayablesBefore)
        + FunctionGuard("advance.list_vendor_positions(uuid)", installed ? PositionsAfter : PositionsBefore);

    private static string FunctionGuard(string signature, string source)
    {
        var start = source.IndexOf("$f$", StringComparison.Ordinal);
        var end = source.LastIndexOf("$f$", StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new InvalidOperationException("Currency read function body was not found.");
        var body = source[(start + 3)..end].Replace("\r\n", "\n").Replace("'", "''");
        return $$"""
            DO $guard$
            BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_proc p WHERE p.oid=to_regprocedure('{{signature}}')
                AND p.prosecdef AND p.provolatile='s' AND p.prorettype='jsonb'::regtype
                AND p.proowner=(SELECT relowner FROM pg_class WHERE oid='advance.vendor_advances'::regclass)
                AND p.proconfig=ARRAY['search_path=pg_catalog, advance']
                AND replace(p.prosrc,E'\r\n',E'\n')='{{body}}'
                AND NOT EXISTS(SELECT 1 FROM aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE a.grantee<>p.proowner AND
                    (a.grantee<>coalesce(to_regrole('nexa_erp_runtime')::oid,p.proowner) OR a.is_grantable))) THEN
                RAISE EXCEPTION 'Currency read migration refuses an absent or changed function baseline: {{signature}}.';
              END IF;
            END $guard$;
            """;
    }
}
