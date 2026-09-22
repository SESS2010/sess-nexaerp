namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class PurchaseRegisterReportSql
{
    internal const string Source = """
        WITH register_events AS (
          SELECT prl."Id" AS pr_line_id,prl."UomSnapshot" AS uom,'PR'::text AS stage,pr."Id" AS document_id,
            pr."PrNumber" AS document_number,pr."Status" AS status,pr."CreatedAt" AS occurred_at,
            prl."Id" AS source_id,NULL::uuid AS po_line_id,NULL::uuid AS grn_line_id,NULL::uuid AS bill_line_id,
            ''::text AS vendor,prl."RequestedQuantity" AS requested,0::numeric AS ordered,0::numeric AS received,0::numeric AS billed
          FROM advance.purchase_requisitions pr JOIN access a ON a.allowed AND pr."CompanyId"=a.company_id
          JOIN advance.purchase_requisition_lines prl ON prl."PurchaseRequisitionId"=pr."Id" AND prl."CompanyId"=a.company_id
          UNION ALL
          SELECT pol."PurchaseRequisitionLineId",pol."UomSnapshot",'PO',po."Id",po."PoNumber",po."Status",po."IssuedAt",
            pol."Id",pol."Id",NULL,NULL,v."Name",0,pol."OrderedQuantity",0,0
          FROM advance.purchase_orders po JOIN access a ON a.allowed AND po."CompanyId"=a.company_id
          JOIN advance.purchase_order_lines pol ON pol."PurchaseOrderId"=po."Id" AND pol."CompanyId"=a.company_id
          JOIN advance.vendors v ON v."Id"=po."VendorId"
          WHERE po."IsCurrentVersion" AND po."Status"='Issued'
          UNION ALL
          SELECT pol."PurchaseRequisitionLineId",gl."UomSnapshot",event.stage,event.id,event.number,'FINALIZED',event.occurred_at,
            gl."Id",pol."Id",gl."Id",NULL,g."VendorNameSnapshot",0,0,event.sign*gl."ReceivedQuantity",0
          FROM advance.goods_receipts g JOIN access a ON a.allowed AND g."CompanyId"=a.company_id
          JOIN advance.goods_receipt_lines gl ON gl."GoodsReceiptId"=g."Id" AND gl."CompanyId"=a.company_id
          JOIN advance.purchase_order_lines pol ON pol."Id"=gl."PurchaseOrderLineId" AND pol."CompanyId"=a.company_id
          CROSS JOIN LATERAL (
            SELECT 'GRN'::text AS stage,g."Id" AS id,g."GrnNumber" AS number,g."FinalizedAt" AS occurred_at,1 AS sign
            WHERE g."FinalizedAt" IS NOT NULL
            UNION ALL SELECT 'GRN_REVERSAL',r."Id",r."GrnNumber",r."FinalizedAt",-1
            FROM advance.goods_receipts r WHERE r."CompanyId"=a.company_id
              AND r."ReversesGoodsReceiptId"=g."Id" AND r."Status"='FINALIZED'
          ) event
          WHERE g."DocumentKind"='NORMAL'
          UNION ALL
          SELECT pol."PurchaseRequisitionLineId",gl."UomSnapshot",event.stage,b."Id",b."BillNumber",b."Status",event.occurred_at,
            bl."Id",pol."Id",gl."Id",bl."Id",g."VendorNameSnapshot",0,0,0,event.sign*bl."BilledQuantity"
          FROM advance.vendor_bills b JOIN access a ON a.allowed AND b."CompanyId"=a.company_id
          JOIN advance.vendor_bill_lines bl ON bl."VendorBillId"=b."Id" AND bl."CompanyId"=a.company_id
          JOIN advance.goods_receipt_lines gl ON gl."Id"=bl."GoodsReceiptLineId" AND gl."CompanyId"=a.company_id
          JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=a.company_id
          JOIN advance.purchase_order_lines pol ON pol."Id"=bl."PurchaseOrderLineId" AND pol."CompanyId"=a.company_id
          CROSS JOIN LATERAL (
            SELECT 'BILL_RECORDED'::text AS stage,b."CreatedAt" AS occurred_at,0 AS sign
            UNION ALL SELECT 'BILL_ACCEPTED',b."DecidedAt",1 WHERE b."Status" IN ('ACCEPTED','REVERSED')
            UNION ALL SELECT 'BILL_REVERSED',b."ReversedAt",-1 WHERE b."Status"='REVERSED'
          ) event
        )
        SELECT jsonb_build_object('prLineId',prl."Id",'itemId',prl."ItemId",'uom',event.uom) AS group_filter,
          jsonb_build_object('prNumber',pr."PrNumber",'prLineNumber',prl."LineNumber",'prStatus',pr."Status",
            'itemCode',prl."ItemCodeSnapshot",'itemName',prl."ItemNameSnapshot",'uom',event.uom) AS labels,
          jsonb_build_object('uom',event.uom) AS total_group,
          jsonb_build_object('prNumber',pr."PrNumber",'prLineNumber',prl."LineNumber",'prStatus',pr."Status",
            'prLineId',prl."Id",'poLineId',event.po_line_id,'grnLineId',event.grn_line_id,'billLineId',event.bill_line_id,
            'stage',event.stage,'documentId',event.document_id,'document',event.document_number,'status',event.status,
            'eventDate',(event.occurred_at AT TIME ZONE @report_timezone)::date,
            'itemCode',prl."ItemCodeSnapshot",'itemName',prl."ItemNameSnapshot",'uom',event.uom,'vendor',event.vendor) AS detail,
          jsonb_build_object('requested',event.requested,'ordered',event.ordered,'received',event.received,'billed',event.billed) AS metrics,
          event.occurred_at::text||':'||event.stage||':'||event.source_id::text||':'||event.document_id::text AS sort_key
        FROM register_events event
        JOIN access a ON a.allowed
        JOIN advance.purchase_requisition_lines prl ON prl."Id"=event.pr_line_id AND prl."CompanyId"=a.company_id
        JOIN advance.purchase_requisitions pr ON pr."Id"=prl."PurchaseRequisitionId" AND pr."CompanyId"=a.company_id
        WHERE (event.occurred_at AT TIME ZONE @report_timezone)::date<=@to_date
        """;
}
