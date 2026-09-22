using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260918090000_ActualBomLandedRateValuation")]
public sealed class ActualBomLandedRateValuation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.Guard(FitmentIssueHeaderLockOrderSql.After));
        migrationBuilder.Sql(RateFunctions);
        migrationBuilder.Sql(BillJson(true));
        migrationBuilder.Sql(CorrectedConfirm);
        migrationBuilder.Sql(Projection);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.Guard(CorrectedConfirm));
        migrationBuilder.Sql(FitmentIssueHeaderLockOrderSql.After);
        migrationBuilder.Sql(BillJson(false));
        migrationBuilder.Sql(OriginalProjection(ImmutableLandedCostAdjustmentsSql.Up));
        migrationBuilder.Sql("DROP FUNCTION advance.vendor_bill_inventory_landed_rate(uuid,uuid); DROP FUNCTION advance.inventory_tax_unit_rate(jsonb,jsonb,numeric,numeric);");
    }

    internal static string OriginalProjection(string source)
    {
        const string start = "CREATE FUNCTION advance.get_actual_bom_landed_valuations";
        const string end = "CREATE OR REPLACE FUNCTION advance.vendor_bill_json";
        source = MigrationText.Lf(source);
        var first = source.IndexOf(start, StringComparison.Ordinal);
        var last = source.IndexOf(end, first, StringComparison.Ordinal);
        if (first < 0 || last <= first) throw new InvalidOperationException("Original valuation SQL markers changed.");
        return source[first..last].Replace(start,
            "CREATE OR REPLACE FUNCTION advance.get_actual_bom_landed_valuations", StringComparison.Ordinal);
    }

    internal static string CorrectedConfirm
    {
        get
        {
            var sql = FitmentIssueHeaderLockOrderSql.After;
            sql = ReplaceOnce(sql, "SELECT a.\"AllocatedQuantity\",a.\"AcceptedValue\",",
                "SELECT a.\"AllocatedQuantity\",a.\"AcceptedValue\",advance.vendor_bill_inventory_landed_rate(l.\"CompanyId\",l.\"Id\") AS \"LandedUnitRate\",");
            sql = ReplaceOnce(sql, "AND allocation_row.\"AllocatedQuantity\">0 THEN",
                "AND allocation_row.\"AllocatedQuantity\">0 AND allocation_row.\"LandedUnitRate\" IS NOT NULL THEN");
            sql = ReplaceOnce(sql, """
            material_value:=round(allocation_row."AcceptedValue"/
              allocation_row."AllocatedQuantity"*p_quantity,6);
            charge_value:=round(allocation_row."AllocatedChargeValue"/
              allocation_row."AllocatedQuantity"*p_quantity,6);
""", """
            charge_value:=round(allocation_row."AllocatedChargeValue"/
              allocation_row."AllocatedQuantity"*p_quantity,6);
            material_value:=round(allocation_row."LandedUnitRate"*p_quantity,6)-charge_value;
""");
            return sql;
        }
    }

    internal static string BillJson(bool corrected) => BillJson(corrected, ImmutableLandedCostAdjustmentsSql.Up);

    internal static string BillJson(bool corrected, string source)
    {
        source = MigrationText.Lf(source);
        var start = source.IndexOf("CREATE OR REPLACE FUNCTION advance.vendor_bill_json", StringComparison.Ordinal);
        var end = source.IndexOf("CREATE OR REPLACE FUNCTION advance.guard_vendor_bill_financial_evidence", start, StringComparison.Ordinal);
        if (start < 0 || end <= start) throw new InvalidOperationException("Vendor bill reader SQL markers changed.");
        var sql = source[start..end];
        return !corrected ? sql : ReplaceOnce(sql, """
coalesce((
          SELECT f."LandedUnitRate" FROM advance.fifo_landed_cost_adjustments f
          WHERE f."CompanyId"=p_company AND f."VendorBillLineId"=l."Id"
          ORDER BY f."CreatedAt" DESC LIMIT 1),l."BilledUnitRate")
""", "advance.vendor_bill_inventory_landed_rate(p_company,l.\"Id\")");
    }

    internal const string RateFunctions = """
        CREATE FUNCTION advance.inventory_tax_unit_rate(commercial jsonb,tax jsonb,quantity numeric,legacy_rate numeric)
        RETURNS numeric LANGUAGE plpgsql IMMUTABLE SET search_path=pg_catalog AS $function$
        DECLARE eligibility text:=coalesce(tax->>'itcEligibility','FULLY_RECOVERABLE');
          recovery numeric; components jsonb:=commercial->'result'; tax_value numeric; cost numeric;
        BEGIN
          IF quantity IS NULL OR quantity<=0 THEN RAISE EXCEPTION 'Inventory valuation requires positive agreed quantity.'; END IF;
          IF eligibility='FULLY_RECOVERABLE' AND (tax->>'recoverableTaxPercent') IS NULL THEN recovery:=100;
          ELSIF eligibility='BLOCKED' AND (tax->>'recoverableTaxPercent') IS NULL THEN recovery:=0;
          ELSIF eligibility='PARTIALLY_RECOVERABLE' THEN
            recovery:=(tax->>'recoverableTaxPercent')::numeric;
            IF recovery IS NULL OR recovery<=0 OR recovery>=100 OR round(recovery,6)<>recovery THEN
              RAISE EXCEPTION 'Partial ITC requires a recoverable percentage strictly between zero and 100 (six decimals maximum).';
            END IF;
          ELSE RAISE EXCEPTION 'Invalid captured ITC eligibility or unexpected recovery percentage.'; END IF;
          IF components IS NULL OR components->>'taxableValue' IS NULL THEN
            -- Pre-snapshot legacy evidence retains its agreed ex-tax unit rate under
            -- the explicitly adopted FULLY_RECOVERABLE compatibility default.
            IF eligibility<>'FULLY_RECOVERABLE' THEN RAISE EXCEPTION 'Non-recoverable ITC valuation requires captured commercial tax amounts.'; END IF;
            IF legacy_rate IS NULL OR legacy_rate<0 THEN RAISE EXCEPTION 'Missing legacy agreed unit rate.'; END IF;
            RETURN legacy_rate;
          END IF;
          IF components->>'cgstValue' IS NULL OR components->>'sgstValue' IS NULL OR
             components->>'igstValue' IS NULL OR components->>'cessValue' IS NULL OR components->>'roundOff' IS NULL THEN
            RAISE EXCEPTION 'Incomplete captured commercial tax amounts.';
          END IF;
          tax_value:=(components->>'cgstValue')::numeric+(components->>'sgstValue')::numeric+
            (components->>'igstValue')::numeric+(components->>'cessValue')::numeric;
          cost:=(components->>'taxableValue')::numeric+(components->>'roundOff')::numeric+tax_value*(1-recovery/100);
          IF tax_value<0 OR cost<0 THEN RAISE EXCEPTION 'Negative captured inventory cost.'; END IF;
          RETURN round(cost/quantity,6);
        END $function$;
        CREATE FUNCTION advance.vendor_bill_inventory_landed_rate(p_company uuid,p_line uuid)
        RETURNS numeric LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
          SELECT round(advance.inventory_tax_unit_rate(p."CommercialSnapshotJson"::jsonb,p."TaxRuleSnapshotJson"::jsonb,
            p."OrderedQuantity",l."BilledUnitRate")+
            coalesce((SELECT sum(a."AllocatedChargeValue") FROM advance.vendor_bill_charge_allocations a
              WHERE a."CompanyId"=l."CompanyId" AND a."VendorBillLineId"=l."Id"),0)/l."BilledQuantity",6)
          FROM advance.vendor_bill_lines l
          JOIN advance.purchase_order_lines p ON p."CompanyId"=l."CompanyId" AND p."Id"=l."PurchaseOrderLineId"
          WHERE l."CompanyId"=p_company AND l."Id"=p_line;
        $function$;
        REVOKE ALL ON FUNCTION advance.inventory_tax_unit_rate(jsonb,jsonb,numeric,numeric),advance.vendor_bill_inventory_landed_rate(uuid,uuid) FROM PUBLIC;
        DO $owner$ BEGIN
          IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.inventory_tax_unit_rate(jsonb,jsonb,numeric,numeric) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.vendor_bill_inventory_landed_rate(uuid,uuid) OWNER TO nexa_erp_owner;
          END IF;
        END $owner$;
        """;

    private static string ReplaceOnce(string sql, string before, string after)
    {
        // The searched baseline is a compiled raw literal too; normalize both sides.
        sql = MigrationText.Lf(sql);
        before = MigrationText.Lf(before);
        after = MigrationText.Lf(after);
        var index = sql.IndexOf(before, StringComparison.Ordinal);
        if (index < 0 || sql.IndexOf(before, index + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("Actual BOM fitment valuation baseline changed.");
        return sql[..index] + after + sql[(index + before.Length)..];
    }
    // Return deltas against immutable fitment evidence. The application applies the
    // opposite delta to each reversal, preserving the original audit records.
    internal const string Projection = """
        CREATE OR REPLACE FUNCTION advance.get_actual_bom_landed_valuations(p_company uuid,p_actual_bom uuid)
        RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $function$
        DECLARE result jsonb;
        BEGIN
          IF session_user<>'nexa_erp_runtime' THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Actual BOM landed valuation projection requires the runtime principal.';
          END IF;
          WITH values_at_landed_rate AS (
            SELECT DISTINCT ON (e."Id") e."Id", l."Id" bill_line_id, b."BillNumber",
              e."AcceptedMaterialValue" original_material,
              e."AllocatedChargeValue" original_charges, e."TotalAcceptedValue" original_total,
              round(advance.vendor_bill_inventory_landed_rate(l."CompanyId",l."Id")*e."QuantityBase",6) total_value,
              round(charges.value/l."BilledQuantity"*e."QuantityBase",6) charge_value,
              b."DecidedAt" AS "CreatedAt"
            FROM advance.actual_bom_entries e
            JOIN advance.vendor_bill_lines l ON l."CompanyId"=e."CompanyId"
              AND l."GoodsReceiptLineId"=e."GoodsReceiptLineId"
            JOIN advance.vendor_bills b ON b."CompanyId"=l."CompanyId"
              AND b."Id"=l."VendorBillId" AND b."Status"='ACCEPTED'
            CROSS JOIN LATERAL (SELECT coalesce(sum(a."AllocatedChargeValue"),0) value
              FROM advance.vendor_bill_charge_allocations a
              WHERE a."CompanyId"=l."CompanyId" AND a."VendorBillLineId"=l."Id") charges
            WHERE e."CompanyId"=p_company AND e."ActualBomId"=p_actual_bom AND e."EntryKind"='FITMENT'
            ORDER BY e."Id", b."DecidedAt" DESC, b."Id" DESC
          )
          SELECT coalesce(jsonb_agg(jsonb_build_object(
            'actualBomEntryId',"Id", 'vendorBillLineId',bill_line_id, 'billNumber',"BillNumber",
            'acceptedMaterialValue',total_value-charge_value-original_material,
            'allocatedChargeValue',charge_value-original_charges,
            'totalAcceptedValue',total_value-original_total,
            'createdAt',"CreatedAt") ORDER BY "CreatedAt","Id"),'[]'::jsonb)
          INTO result FROM values_at_landed_rate;
          RETURN result;
        END $function$;
        """;
}