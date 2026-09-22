namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class PurchaseFinancialReportSql
{
    internal const string Grni = """
        SELECT jsonb_build_object('vendorId',v."Id",'itemId',i."Id",'uom',l."UomSnapshot",'currency',po."CurrencyCode") AS group_filter,
          jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',i."ItemCode",'itemName',i."Name",
            'uom',l."UomSnapshot",'currency',po."CurrencyCode") AS labels,
          jsonb_build_object('uom',l."UomSnapshot",'currency',po."CurrencyCode") AS total_group,
          jsonb_build_object('grnLineId',l."Id",'grnNumber',g."GrnNumber",'receivedDate',(g."ReceivedAt" AT TIME ZONE @report_timezone)::date,
            'vendorCode',v."VendorCode",'vendor',g."VendorNameSnapshot",'poNumber',po."PoNumber",'itemCode',l."ItemCodeSnapshot",
            'itemName',l."ItemNameSnapshot",'uom',l."UomSnapshot",'currency',po."CurrencyCode",
            'daysUninvoiced',greatest(0,@to_date-(g."ReceivedAt" AT TIME ZONE @report_timezone)::date),
            'receivedQuantity',l."ReceivedQuantity",'acceptedBilledQuantity',coalesce(billed.quantity,0),
            'unitRate',l."UnitRateSnapshot") AS detail,
          jsonb_build_object('quantity',l."ReceivedQuantity"-coalesce(billed.quantity,0),
            'receiptValue',(l."ReceivedQuantity"-coalesce(billed.quantity,0))*l."UnitRateSnapshot") AS metrics,
          g."ReceivedAt"::text||':'||l."Id"::text AS sort_key
        FROM advance.goods_receipts g JOIN access a ON a.allowed AND g."CompanyId"=a.company_id
        JOIN advance.goods_receipt_lines l ON l."GoodsReceiptId"=g."Id" AND l."CompanyId"=a.company_id
        JOIN advance.purchase_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=a.company_id
        JOIN advance.vendors v ON v."Id"=g."VendorId"
        JOIN advance.items i ON i."Id"=l."ItemId"
        LEFT JOIN LATERAL (
          SELECT sum(bl."BilledQuantity") AS quantity
          FROM advance.vendor_bill_lines bl
          JOIN advance.vendor_bills b ON b."Id"=bl."VendorBillId" AND b."CompanyId"=a.company_id
          WHERE bl."CompanyId"=a.company_id AND bl."GoodsReceiptLineId"=l."Id"
            AND (b."DecidedAt" AT TIME ZONE @report_timezone)::date<=@to_date
            AND (b."Status"='ACCEPTED' OR b."Status"='REVERSED' AND (b."ReversedAt" AT TIME ZONE @report_timezone)::date>@to_date)
        ) billed ON true
        WHERE g."DocumentKind"='NORMAL' AND g."FinalizedAt" IS NOT NULL
          AND (g."FinalizedAt" AT TIME ZONE @report_timezone)::date<=@to_date
          AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal
            WHERE reversal."CompanyId"=a.company_id AND reversal."ReversesGoodsReceiptId"=g."Id"
              AND reversal."Status"='FINALIZED' AND (reversal."FinalizedAt" AT TIME ZONE @report_timezone)::date<=@to_date)
          AND l."ReceivedQuantity"-coalesce(billed.quantity,0)<>0
        """;

    internal const string VendorPurchases = """
        SELECT jsonb_build_object('vendorId',v."Id",'itemId',i."Id",'uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS group_filter,
          jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',i."ItemCode",'itemName',i."Name",
            'uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS labels,
          jsonb_build_object('uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS total_group,
          jsonb_build_object('billLineId',l."Id",'billNumber',b."BillNumber",'billDate',b."BillDate",'event',event.kind,
            'eventDate',(event.occurred_at AT TIME ZONE @report_timezone)::date,'grnNumber',g."GrnNumber",
            'vendorCode',v."VendorCode",'vendor',g."VendorNameSnapshot",'poNumber',po."PoNumber",
            'itemCode',gl."ItemCodeSnapshot",'itemName',gl."ItemNameSnapshot",'uom',gl."UomSnapshot",
            'currency',po."CurrencyCode") AS detail,
          jsonb_build_object('quantity',event.sign*l."BilledQuantity",'materialValue',event.sign*l."BilledPayableValue",
            'allocatedCharges',event.sign*coalesce(charges.value,0),
            'landedValue',event.sign*(l."BilledPayableValue"+coalesce(charges.value,0))) AS metrics,
          event.occurred_at::text||':'||l."Id"::text||':'||event.kind AS sort_key
        FROM advance.vendor_bills b JOIN access a ON a.allowed AND b."CompanyId"=a.company_id
        JOIN advance.vendor_bill_lines l ON l."VendorBillId"=b."Id" AND l."CompanyId"=a.company_id
        JOIN advance.goods_receipt_lines gl ON gl."Id"=l."GoodsReceiptLineId" AND gl."CompanyId"=a.company_id
        JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=a.company_id
        JOIN advance.purchase_orders po ON po."Id"=b."PurchaseOrderId" AND po."CompanyId"=a.company_id
        JOIN advance.vendors v ON v."Id"=b."VendorId"
        JOIN advance.items i ON i."Id"=l."ItemId"
        CROSS JOIN LATERAL (
          SELECT 'ACCEPTED'::text AS kind,b."DecidedAt" AS occurred_at,1 AS sign
          WHERE b."Status" IN ('ACCEPTED','REVERSED')
          UNION ALL SELECT 'REVERSED',b."ReversedAt",-1 WHERE b."Status"='REVERSED'
        ) event
        LEFT JOIN LATERAL (
          SELECT sum(c."AllocatedChargeValue") AS value FROM advance.vendor_bill_charge_allocations c
          WHERE c."CompanyId"=a.company_id AND c."VendorBillLineId"=l."Id"
        ) charges ON true
        WHERE (event.occurred_at AT TIME ZONE @report_timezone)::date BETWEEN @from_date AND @to_date
        """;
}
