namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class BilledNotReceivedReportSql
{
    internal const string Source = """
        SELECT jsonb_build_object('vendorId',v."Id",'itemId',l."ItemId",'uom',l."Uom",'currency',i."CurrencyCode") AS group_filter,
         jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',l."ItemCode",'itemName',l."ItemName",
          'uom',l."Uom",'currency',i."CurrencyCode") AS labels,
         jsonb_build_object('uom',l."Uom",'currency',i."CurrencyCode") AS total_group,
         jsonb_build_object('invoiceId',i."Id",'invoiceLineId',l."Id",'invoiceNumber',i."InvoiceNumber",'invoiceDate',i."InvoiceDate",
          'recordedAt',i."RecordedAt",'poNumber',po."PoNumber",'purchaseOrderLineId',l."PurchaseOrderLineId",
          'vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',l."ItemCode",'itemName',l."ItemName",'uom',l."Uom",
          'currency',i."CurrencyCode",'invoiceQuantity',l."Quantity",'receivedQuantity',coalesce(received.quantity,0),
          'invoiceUnitRate',l."UnitRate",'invoicePayableValue',l."PayableValue",
          'daysOutstanding',greatest(0,@to_date-i."InvoiceDate"),'evidenceFileName',i."FileName",'evidenceSha256',i."ContentSha256",
          'receiptHistory',coalesce(received.history,''),'acceptedBillLinks',coalesce(bills.history,'')) AS detail,
         jsonb_build_object('quantity',l."Quantity"-coalesce(received.quantity,0),
          'invoiceValue',round(l."PayableValue"*(l."Quantity"-coalesce(received.quantity,0))/l."Quantity",6)) AS metrics,
         i."RecordedAt"::text||':'||i."Id"::text||':'||l."LineNumber"::text AS sort_key
        FROM advance.supplier_invoices i JOIN access a ON a.allowed AND i."CompanyId"=a.company_id
        JOIN advance.supplier_invoice_lines l ON l."CompanyId"=a.company_id AND l."SupplierInvoiceId"=i."Id"
        JOIN advance.vendors v ON v."Id"=i."VendorId"
        JOIN advance.purchase_orders po ON po."CompanyId"=a.company_id AND po."Id"=i."PurchaseOrderId"
        LEFT JOIN LATERAL(
         SELECT sum(m."Quantity") AS quantity,
          string_agg(g."GrnNumber"||' / line '||gl."LineNumber"||' / quantity '||m."Quantity"||' / match '||m."Id",'; ' ORDER BY m."EffectiveAt",m."Id") AS history
         FROM advance.supplier_invoice_receipt_matches m
         JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=a.company_id AND gl."Id"=m."GoodsReceiptLineId"
         JOIN advance.goods_receipts g ON g."CompanyId"=a.company_id AND g."Id"=m."ReceiptEventId"
         WHERE m."CompanyId"=a.company_id AND m."SupplierInvoiceLineId"=l."Id"
          AND (m."EffectiveAt" AT TIME ZONE @report_timezone)::date<=@to_date
        ) received ON true
        LEFT JOIN LATERAL(
         SELECT string_agg(b."BillNumber"||' / '||b."Id",'; ' ORDER BY link."RecordedAt",link."Id") AS history
         FROM advance.supplier_invoice_bill_links link JOIN advance.vendor_bills b ON b."CompanyId"=a.company_id AND b."Id"=link."VendorBillId"
         WHERE link."CompanyId"=a.company_id AND link."SupplierInvoiceId"=i."Id"
          AND (link."RecordedAt" AT TIME ZONE @report_timezone)::date<=@to_date
        ) bills ON true
        WHERE (i."RecordedAt" AT TIME ZONE @report_timezone)::date<=@to_date
         AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations c WHERE c."CompanyId"=a.company_id AND c."SupplierInvoiceId"=i."Id"
          AND (c."RecordedAt" AT TIME ZONE @report_timezone)::date<=@to_date)
         AND l."Quantity"-coalesce(received.quantity,0)>0
        """;
}
