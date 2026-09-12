namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class FifoValuationReportSql
{
    // Accounting layers remain separate from physical serial/lot selection.
    private const string Layers = """
        WITH cost_layers AS (
          SELECT f.*,i."ItemCode",i."Name" AS item_name,coalesce(gl."UomSnapshot",u."Code",i."Uom") AS uom,
            coalesce(po."CurrencyCode",ownership."CurrencyCode") AS currency,owners.owner_count,owners.owner_id,
            coalesce(holder."HolderNameSnapshot",ownership."AccountCode") AS owner_name,
            coalesce(used.quantity,0) AS consumed_quantity,f."QuantityReceived"-coalesce(used.quantity,0) AS remaining_quantity,
            coalesce(landed."LandedUnitRate",f."UnitCost") AS effective_unit_cost,
            CASE WHEN landed."Id" IS NOT NULL THEN 'BILL_LANDED' ELSE f."CostBasis" END AS effective_basis,
            landed."VendorBillLineId" AS accepted_bill_line_id,g."GrnNumber",opening."LineReference",
            greatest(0,@to_date-(f."ReceivedAt" AT TIME ZONE @report_timezone)::date) AS age_days
          FROM advance.fifo_inventory_cost_layers f JOIN access a ON a.allowed AND f."CompanyId"=a.company_id
          JOIN advance.items i ON i."Id"=f."ItemId"
          LEFT JOIN advance.uoms u ON u."Id"=coalesce(i."BaseUomId",i."UomId")
          LEFT JOIN advance.goods_receipt_lines gl ON gl."Id"=f."GoodsReceiptLineId" AND gl."CompanyId"=a.company_id
          LEFT JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=a.company_id
          LEFT JOIN advance.purchase_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=a.company_id
          LEFT JOIN advance.opening_stock_lines opening ON opening."Id"=f."OpeningStockLineId" AND opening."CompanyId"=a.company_id
          CROSS JOIN LATERAL (
            SELECT count(DISTINCT m."OwnershipAccountId") AS owner_count,
              (array_agg(DISTINCT m."OwnershipAccountId"))[1] AS owner_id
            FROM advance.stock_movements m
            WHERE m."CompanyId"=a.company_id AND m."MovementLeg"='RECEIPT_IN' AND m."PostingDate"<=@to_date
              AND ((f."GoodsReceiptLineId" IS NOT NULL AND m."GoodsReceiptLineId"=f."GoodsReceiptLineId")
                OR (f."OpeningStockLineId" IS NOT NULL AND m."OpeningStockLineId"=f."OpeningStockLineId"))
          ) owners
          LEFT JOIN advance.inventory_ownership_accounts ownership ON ownership."Id"=owners.owner_id AND ownership."CompanyId"=a.company_id
          LEFT JOIN advance.inventory_account_holders holder ON holder."Id"=ownership."AccountHolderId" AND holder."CompanyId"=a.company_id
          LEFT JOIN LATERAL (
            SELECT sum(c."Quantity") AS quantity FROM advance.fifo_cost_consumptions c
            WHERE c."CompanyId"=a.company_id AND c."FifoInventoryCostLayerId"=f."Id"
              AND (c."ConsumedAt" AT TIME ZONE @report_timezone)::date<=@to_date
          ) used ON true
          LEFT JOIN LATERAL (
            SELECT adj.* FROM advance.fifo_landed_cost_adjustments adj
            JOIN advance.vendor_bill_lines bl ON bl."Id"=adj."VendorBillLineId" AND bl."CompanyId"=a.company_id
            JOIN advance.vendor_bills bill ON bill."Id"=bl."VendorBillId" AND bill."CompanyId"=a.company_id
            WHERE adj."CompanyId"=a.company_id AND adj."FifoInventoryCostLayerId"=f."Id"
              AND bill."Status" IN ('ACCEPTED','REVERSED')
              AND (bill."DecidedAt" AT TIME ZONE @report_timezone)::date<=@to_date
              AND (bill."ReversedAt" IS NULL OR (bill."ReversedAt" AT TIME ZONE @report_timezone)::date>@to_date)
            ORDER BY bill."DecidedAt" DESC,adj."CreatedAt" DESC,adj."Id" LIMIT 1
          ) landed ON true
          WHERE (f."ReceivedAt" AT TIME ZONE @report_timezone)::date<=@to_date
            AND (f."OpeningStockLineId" IS NOT NULL OR (g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
              AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversed
                WHERE reversed."CompanyId"=a.company_id AND reversed."ReversesGoodsReceiptId"=g."Id"
                  AND reversed."Status"='FINALIZED' AND (reversed."FinalizedAt" AT TIME ZONE @report_timezone)::date<=@to_date)))
        )
        """;

    internal static string Availability => Layers + """
        , problems AS (
          SELECT EXISTS(
            SELECT 1 FROM advance.material_returns r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
            JOIN advance.material_return_lines rl ON rl."MaterialReturnId"=r."Id" AND rl."CompanyId"=a.company_id
            WHERE r."Status"='ACCEPTED' AND (r."AcceptedAt" AT TIME ZONE @report_timezone)::date<=@to_date
              AND rl."ReturnedQuantityBase">0
              AND EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions consumed
                WHERE consumed."CompanyId"=a.company_id AND consumed."MaterialIssueLineId"=rl."MaterialIssueLineId")
          ) AS return_gap,
          EXISTS(SELECT 1 FROM cost_layers WHERE owner_count<>1 OR nullif(currency,'') IS NULL
            OR remaining_quantity<0 OR effective_unit_cost<0) AS invalid_source
        )
        SELECT NOT return_gap AND NOT invalid_source AS ready,
          CASE WHEN return_gap THEN 'FIFO_RETURN_CREDITS_REQUIRED'
            WHEN invalid_source THEN 'FIFO_SOURCE_INCONSISTENT' END AS issue_code
        FROM problems
        """;

    internal static string Source => Layers + """
        SELECT jsonb_build_object('itemId',l."ItemId",'ownershipAccountId',l.owner_id,'ownership',l.owner_name,'uom',l.uom,
            'currency',l.currency,'ageBucket',age.bucket,'costBasis',l.effective_basis) AS group_filter,
          labels.value AS labels,
          jsonb_build_object('ownershipAccountId',l.owner_id,'ownership',l.owner_name,'uom',l.uom,'currency',l.currency) AS total_group,
          labels.value||jsonb_build_object('layerId',l."Id",'receivedAt',l."ReceivedAt",'ageDays',l.age_days,
            'receivedQuantity',l."QuantityReceived",'consumedQuantity',l.consumed_quantity,
            'unitCost',l.effective_unit_cost,'grnNumber',l."GrnNumber",'grnLineId',l."GoodsReceiptLineId",
            'openingStockLineId',l."OpeningStockLineId",'openingLine',l."LineReference",
            'acceptedBillLineId',l.accepted_bill_line_id) AS detail,
          jsonb_build_object('quantity',l.remaining_quantity,'value',l.remaining_quantity*l.effective_unit_cost) AS metrics,
          l."ReceivedAt"::text||':'||l."Id"::text AS sort_key
        FROM cost_layers l
        CROSS JOIN LATERAL (SELECT CASE WHEN l.age_days<=30 THEN '0–30 days' WHEN l.age_days<=90 THEN '31–90 days'
          WHEN l.age_days<=180 THEN '91–180 days' WHEN l.age_days<=365 THEN '181–365 days' ELSE 'Over 365 days' END AS bucket) age
        CROSS JOIN LATERAL (SELECT jsonb_build_object('itemCode',l."ItemCode",'itemName',l.item_name,
          'ownership',l.owner_name,'uom',l.uom,'currency',l.currency,'ageBucket',age.bucket,'costBasis',l.effective_basis) AS value) labels
        WHERE l.remaining_quantity>0
        """;
}
