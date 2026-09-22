namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static partial class ComponentFitmentActualBomSql
{
    internal static string LandedCostCompatibleConfirm
    {
        get
        {
            const string startToken = "SELECT a.\"AllocatedQuantity\"";
            const string endToken = "fitment_id:=gen_random_uuid();";
            var start = ConfirmFunction.IndexOf(startToken, StringComparison.Ordinal);
            var end = ConfirmFunction.IndexOf(endToken, start, StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new InvalidOperationException("Fitment valuation SQL markers changed.");
            var replacement = """
          SELECT a."AllocatedQuantity",a."AcceptedValue",
                 coalesce((SELECT sum(ca."AllocatedChargeValue")
                   FROM advance.vendor_bill_charge_allocations ca
                   WHERE ca."CompanyId"=p_company AND ca."VendorBillLineId"=l."Id"),0)
                   "AllocatedChargeValue",
                 CASE WHEN b."Id" IS NOT NULL THEN l."Id" END bill_line_id,
                 b."BillNumber" bill_number,g."GrnNumber" grn_number
            INTO allocation_row
            FROM advance.goods_receipt_lines gl
            JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId"
            LEFT JOIN advance.vendor_bill_lines l
              ON l."CompanyId"=gl."CompanyId" AND l."GoodsReceiptLineId"=gl."Id"
            LEFT JOIN advance.vendor_bills b
              ON b."CompanyId"=l."CompanyId" AND b."Id"=l."VendorBillId"
              AND b."Status"='ACCEPTED'
            LEFT JOIN advance.vendor_bill_cost_allocations a
              ON a."CompanyId"=l."CompanyId" AND a."VendorBillLineId"=l."Id"
              AND b."Id" IS NOT NULL
            WHERE gl."CompanyId"=p_company
              AND gl."Id"=line_row."OriginGoodsReceiptLineId"
            ORDER BY b."DecidedAt" DESC NULLS LAST LIMIT 1;
          IF allocation_row."AllocatedQuantity" IS NOT NULL
             AND allocation_row."AllocatedQuantity">0 THEN
            material_value:=round(allocation_row."AcceptedValue"/
              allocation_row."AllocatedQuantity"*p_quantity,6);
            charge_value:=round(allocation_row."AllocatedChargeValue"/
              allocation_row."AllocatedQuantity"*p_quantity,6);
          ELSE
            material_value:=0; charge_value:=0;
          END IF;
""";
            var sql = ConfirmFunction[..start] + replacement + ConfirmFunction[end..];
            sql = sql.Replace("CREATE FUNCTION advance.confirm_component_fitment",
                "CREATE OR REPLACE FUNCTION advance.confirm_component_fitment",
                StringComparison.Ordinal);
            sql = sql.Replace(
                "allocation_row.bill_line_id,allocation_row.bill_number,material_value,charge_value,material_value+charge_value,p_fitted_at,p_login);",
                "allocation_row.bill_line_id,coalesce(allocation_row.bill_number,''),material_value,charge_value,material_value+charge_value,p_fitted_at,p_login);",
                StringComparison.Ordinal);
            return sql;
        }
    }

    internal static string RestoreBillRequiredConfirm => """
        DROP FUNCTION IF EXISTS advance.confirm_component_fitment(
          uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text);
        """ + ConfirmFunction;
}