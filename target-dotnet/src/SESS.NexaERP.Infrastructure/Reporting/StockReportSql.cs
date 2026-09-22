namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class StockReportSql
{
    private static readonly (string Key, string Expression, string Type)[] Filters =
    [
        ("itemId","m.\"ItemId\"","uuid"),("warehouseId","m.\"WarehouseId\"","uuid"),
        ("rackBinId","m.\"RackBinId\"","uuid"),("lotId","m.\"InventoryLotId\"","uuid"),
        ("serialId","m.\"InventorySerialId\"","uuid"),("ownershipAccountId","m.\"OwnershipAccountId\"","uuid"),
        ("custodyAssignmentId","m.\"CustodyAssignmentId\"","uuid"),("conditionCode","m.\"ConditionCode\"","text"),
        ("uom","(SELECT coalesce(fu.\"Code\",fi.\"Uom\",'') FROM advance.items fi LEFT JOIN advance.uoms fu ON fu.\"Id\"=coalesce(fi.\"BaseUomId\",fi.\"UomId\") WHERE fi.\"Id\"=m.\"ItemId\")","text")
    ];

    internal static IReadOnlySet<string> FilterKeys => Filters.Select(filter => filter.Key).ToHashSet(StringComparer.Ordinal);

    internal static string Build(bool rollForward)
    {
        const string dimensions = """
            "ItemId","WarehouseId","RackBinId","InventoryLotId","InventorySerialId",
            "OwnershipAccountId","CustodyAssignmentId","ConditionCode"
            """;
        var filter = string.Join("\n AND ",Filters.Select(value =>
            $"(NOT (@group_filter ? '{value.Key}') OR {value.Expression} IS NOT DISTINCT FROM (@group_filter->>'{value.Key}')::{value.Type})"));
        var joins = string.Join(" AND ",new[] { "ItemId","WarehouseId","RackBinId","InventoryLotId","InventorySerialId",
            "OwnershipAccountId","CustodyAssignmentId","ConditionCode" }
            .Select(name => name == "ItemId" ? "s.\"ItemId\"=g.\"ItemId\"" :
                name == "ConditionCode"
                    ? "(coalesce(s.\"ConditionCode\",'')=coalesce(g.\"ConditionCode\",'') AND (s.\"ConditionCode\" IS NULL)=(g.\"ConditionCode\" IS NULL))"
                    : $"(coalesce(s.\"{name}\",'00000000-0000-0000-0000-000000000000'::uuid)=coalesce(g.\"{name}\",'00000000-0000-0000-0000-000000000000'::uuid) AND (s.\"{name}\" IS NULL)=(g.\"{name}\" IS NULL))"));
        var keep = rollForward ? "period_activity<>0 OR opening<>0" : "quantity<>0";
        // Read filtered ledger rows once, then group dimensions before label joins. Expand detail arrays after
        // the final sort, so large exports do not sort repeated JSON field names.
        return $$"""
            WITH {{ReportAccessSql.Context}},{{ReportAccessSql.Access}},
            stock_source AS MATERIALIZED (
              SELECT m."Id",m."ItemId",m."WarehouseId",m."RackBinId",m."InventoryLotId",m."InventorySerialId",
                m."OwnershipAccountId",m."CustodyAssignmentId",m."ConditionCode",m."PostingDate",
                m."MovementType",m."MovementLeg",m."ReferenceType",m."ReferenceNumber",
                m."QuantityIn",m."QuantityOut",m."OpeningStockLineId",m."GoodsReceiptLineId"
              FROM advance.stock_movements m
              JOIN access a ON a.allowed AND m."CompanyId"=a.company_id
              WHERE m."PostingDate"<=@to_date
                AND (@group_filter IS NULL OR ({{filter}}))
                AND (@mode<>'details' OR @metric IS NULL OR
                  CASE @metric
                    WHEN 'closing' THEN true
                    WHEN 'opening' THEN m."PostingDate"<@from_date
                    WHEN 'receipts' THEN m."PostingDate">=@from_date
                      AND m."MovementLeg"='RECEIPT_IN' AND m."MovementType"<>'OPENING_BALANCE' AND m."QuantityIn"<>0
                    WHEN 'issues' THEN m."PostingDate">=@from_date
                      AND m."MovementLeg" IN ('ISSUE_OUT','DISPATCH_OUT','CONSUMPTION_OUT') AND m."QuantityOut"<>0
                    WHEN 'opening-stock' THEN m."PostingDate">=@from_date AND m."MovementType"='OPENING_BALANCE'
                    WHEN 'adjustments' THEN m."PostingDate">=@from_date AND
                      (m."QuantityIn"-m."QuantityOut"
                       -CASE WHEN m."MovementLeg"='RECEIPT_IN' AND m."MovementType"<>'OPENING_BALANCE' THEN m."QuantityIn" ELSE 0 END
                       +CASE WHEN m."MovementLeg" IN ('ISSUE_OUT','DISPATCH_OUT','CONSUMPTION_OUT') THEN m."QuantityOut" ELSE 0 END)<>0
                    ELSE false END)
            ),
            grouped_stock AS NOT MATERIALIZED (
              SELECT {{dimensions}},count(*) AS source_count,
                sum("QuantityIn"-"QuantityOut") AS quantity,
                coalesce(sum("QuantityIn"-"QuantityOut") FILTER(WHERE "PostingDate"<@from_date),0) AS opening,
                coalesce(sum("QuantityIn") FILTER(WHERE "PostingDate">=@from_date AND "MovementLeg"='RECEIPT_IN'
                  AND "MovementType"<>'OPENING_BALANCE'),0) AS receipts,
                coalesce(sum("QuantityOut") FILTER(WHERE "PostingDate">=@from_date
                  AND "MovementLeg" IN ('ISSUE_OUT','DISPATCH_OUT','CONSUMPTION_OUT')),0) AS issues,
                coalesce(sum("QuantityIn"-"QuantityOut") FILTER(WHERE "PostingDate">=@from_date),0) AS period_net,
                coalesce(sum("QuantityIn"+"QuantityOut") FILTER(WHERE "PostingDate">=@from_date),0) AS period_activity,
                coalesce(sum("QuantityIn"-"QuantityOut") FILTER(WHERE "PostingDate">=@from_date
                  AND "MovementType"='OPENING_BALANCE'),0) AS opening_introduced
              FROM stock_source GROUP BY {{dimensions}}
            ),
            kept_stock AS NOT MATERIALIZED (
              SELECT g.*,period_net-receipts+issues AS adjustments,
                coalesce(u."Code",i."Uom",'') AS uom,i."ItemCode" AS item_code
              FROM grouped_stock g
              JOIN advance.items i ON i."Id"=g."ItemId"
              LEFT JOIN advance.uoms u ON u."Id"=coalesce(i."BaseUomId",i."UomId")
              WHERE {{keep}}
            ),
            numbered_stock AS MATERIALIZED (
              SELECT *,row_number() OVER stock_order AS summary_ordinal,
                sum(source_count) OVER stock_order-source_count+1 AS detail_start
              FROM kept_stock
              WINDOW stock_order AS (ORDER BY uom,item_code,"ItemId","WarehouseId","RackBinId","InventoryLotId",
                "InventorySerialId","OwnershipAccountId","CustodyAssignmentId","ConditionCode" ROWS UNBOUNDED PRECEDING)
            ),
            stock_labels AS NOT MATERIALIZED (
              SELECT n.*,
                jsonb_build_object('itemId',n."ItemId",'warehouseId',n."WarehouseId",'rackBinId',n."RackBinId",
                  'lotId',n."InventoryLotId",'serialId',n."InventorySerialId",'ownershipAccountId',n."OwnershipAccountId",
                  'custodyAssignmentId',n."CustodyAssignmentId",'conditionCode',n."ConditionCode") AS group_filter,
                jsonb_build_object('itemCode',n.item_code,'itemName',i."Name",'uom',n.uom,
                  'warehouse',coalesce(w."WarehouseCode",'')||CASE WHEN w."Name" IS NULL THEN '' ELSE ' / '||w."Name" END,
                  'rackBin',coalesce(rb."BinCode",''),
                  'lot',coalesce(nullif(concat_ws(' / ',l."SupplierLotNumber",l."ManufacturerLotNumber"),''),n."InventoryLotId"::text,''),
                  'serial',coalesce(sn."StoredSerialNumber",''),
                  'ownership',coalesce(oh."HolderNameSnapshot",oa."AccountCode",'')||' / '||coalesce(oa."OwnershipType",''),
                  'condition',coalesce(n."ConditionCode",'UNCLASSIFIED'),
                  'custody',coalesce(ch."HolderNameSnapshot",ca."AccountCode",'')
                ) AS labels
              FROM numbered_stock n
              JOIN advance.items i ON i."Id"=n."ItemId"
              LEFT JOIN advance.warehouses w ON w."Id"=n."WarehouseId" AND w."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.rack_bins rb ON rb."Id"=n."RackBinId" AND rb."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_lots l ON l."Id"=n."InventoryLotId" AND l."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_serials sn ON sn."Id"=n."InventorySerialId" AND sn."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_ownership_accounts oa ON oa."Id"=n."OwnershipAccountId" AND oa."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_account_holders oh ON oh."Id"=oa."AccountHolderId" AND oh."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_custody_assignments cas ON cas."Id"=n."CustodyAssignmentId" AND cas."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_custody_accounts ca ON ca."Id"=cas."CustodyAccountId" AND ca."CompanyId"=(SELECT company_id FROM access)
              LEFT JOIN advance.inventory_account_holders ch ON ch."Id"=ca."AccountHolderId" AND ch."CompanyId"=(SELECT company_id FROM access)
            ),
            stock_details AS (
              SELECT s.*,g.summary_ordinal,g.detail_start,
                row_number() OVER(ORDER BY g.summary_ordinal,s."PostingDate",s."Id") AS detail_ordinal
              FROM stock_source s JOIN numbered_stock g ON {{joins}}
              WHERE @export OR @mode='details'
            ),
            unit_totals AS (
              SELECT uom,sum(quantity) AS quantity,sum(opening) AS opening,sum(receipts) AS receipts,
                sum(issues) AS issues,sum(adjustments) AS adjustments,sum(quantity) AS closing,
                sum(opening_introduced) AS opening_introduced,sum(source_count) AS source_count,
                min(detail_start) AS detail_start
              FROM numbered_stock GROUP BY uom
            ),
            output_rows AS (
              SELECT 1 AS kind,g.summary_ordinal AS ordinal,
                g.labels||jsonb_build_object('group',g.group_filter,
                  'quantity',g.quantity,'opening',g.opening,'receipts',g.receipts,'issues',g.issues,
                  'adjustments',g.adjustments,'openingIntroduced',g.opening_introduced,'closing',g.quantity,
                  'detailStart',g.detail_start,'detailCount',g.source_count) AS payload
              FROM stock_labels g
              WHERE (@export OR @mode='summary')
                AND (@export OR g.summary_ordinal>@offset AND g.summary_ordinal<=@offset+@page_size)
              UNION ALL
              SELECT 2,d.detail_ordinal,jsonb_build_array(
                CASE WHEN NOT @export OR d.detail_ordinal=d.detail_start THEN g.labels END,
                CASE WHEN @export THEN to_jsonb(d.summary_ordinal) ELSE g.group_filter END,
                d."Id",d."PostingDate",d."MovementType",
                d."ReferenceType",d."ReferenceNumber",
                d."QuantityIn",d."QuantityOut",
                d."QuantityIn"-d."QuantityOut",
                d."OpeningStockLineId",d."GoodsReceiptLineId",
                CASE WHEN d."PostingDate"<@from_date THEN d."QuantityIn"-d."QuantityOut" ELSE 0 END,
                CASE WHEN d."PostingDate">=@from_date AND d."MovementLeg"='RECEIPT_IN'
                  AND d."MovementType"<>'OPENING_BALANCE' THEN d."QuantityIn" ELSE 0 END,
                CASE WHEN d."PostingDate">=@from_date
                  AND d."MovementLeg" IN ('ISSUE_OUT','DISPATCH_OUT','CONSUMPTION_OUT') THEN d."QuantityOut" ELSE 0 END,
                CASE WHEN d."PostingDate">=@from_date THEN d."QuantityIn"-d."QuantityOut"
                  -CASE WHEN d."MovementLeg"='RECEIPT_IN' AND d."MovementType"<>'OPENING_BALANCE' THEN d."QuantityIn" ELSE 0 END
                  +CASE WHEN d."MovementLeg" IN ('ISSUE_OUT','DISPATCH_OUT','CONSUMPTION_OUT') THEN d."QuantityOut" ELSE 0 END ELSE 0 END,
                CASE WHEN d."PostingDate">=@from_date AND d."MovementType"='OPENING_BALANCE'
                  THEN d."QuantityIn"-d."QuantityOut" ELSE 0 END)
              FROM stock_details d JOIN stock_labels g ON g.summary_ordinal=d.summary_ordinal
              WHERE (@export OR @mode='details')
                AND (@export OR d.detail_ordinal>@offset AND d.detail_ordinal<=@offset+@page_size)
            )
            SELECT kind,ordinal,
              (CASE WHEN kind=2 THEN (CASE WHEN @export THEN jsonb_build_object('_stockGroupOrdinal',payload->1,'_stockLabels',payload->0) ELSE (payload->0)||jsonb_build_object('group',payload->1) END)||jsonb_build_object('movementId',payload->2,'postingDate',payload->3,'movementType',payload->4,'referenceType',payload->5,'referenceNumber',payload->6,'quantityIn',payload->7,'quantityOut',payload->8,'netQuantity',payload->9,'openingStockLineId',payload->10,'grnLineId',payload->11,'openingContribution',payload->12,'receiptContribution',payload->13,'issueContribution',payload->14,'adjustmentContribution',payload->15,'openingStockContribution',payload->16) ELSE payload END)::text AS payload
            FROM (
            SELECT 0 AS kind,0::bigint AS ordinal,jsonb_build_object(
              'allowed',(SELECT allowed FROM access),'generatedAt',statement_timestamp(),'timeZone',@report_timezone,
              'totalRows',CASE WHEN @mode='details' THEN coalesce((SELECT sum(source_count) FROM numbered_stock),0)
                ELSE (SELECT count(*) FROM numbered_stock) END,
              'totalSourceRows',coalesce((SELECT sum(source_count) FROM numbered_stock),0),
              'totals',coalesce((SELECT jsonb_agg(jsonb_build_object('uom',uom,'group',jsonb_build_object('uom',uom),
                'quantity',quantity,'opening',opening,'receipts',receipts,'issues',issues,'adjustments',adjustments,
                'closing',closing,'openingIntroduced',opening_introduced,'detailStart',detail_start,
                'detailCount',source_count) ORDER BY uom) FROM unit_totals),'[]'::jsonb)
            ) AS payload
            UNION ALL SELECT kind,ordinal,payload FROM output_rows
            ORDER BY kind,ordinal OFFSET 0
            ) ordered_rows ORDER BY kind,ordinal
            """;
    }
}
