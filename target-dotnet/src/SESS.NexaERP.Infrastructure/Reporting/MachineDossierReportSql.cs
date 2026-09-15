namespace SESS.NexaERP.Infrastructure.Reporting;
internal static class MachineDossierReportSql
{
 internal const string Source = """
WITH RECURSIVE target AS (
 SELECT j.*,d."MachineSerial" AS delivered_serial,d."Id" AS dc_id,d."DcNumber",d."Nature",s."DeliveredAt",s."CustomerSignatory",s."ContentSha256" FROM advance.job_orders j JOIN advance.companies c ON c."Id"=j."CompanyId" JOIN access a ON a.allowed AND a.company_id=j."CompanyId" JOIN advance.machine_delivery_challans d ON d."CompanyId"=j."CompanyId" AND d."JobOrderId"=j."Id" JOIN advance.machine_delivery_signatures s ON s."CompanyId"=d."CompanyId" AND s."DeliveryChallanId"=d."Id"
 WHERE d."MachineSerial"=(@group_filter->>'machineSerial') AND (s."DeliveredAt" AT TIME ZONE @report_timezone)::date<=@to_date
), entries AS (
 SELECT e.*,j.dc_id,j."DcNumber",j.delivered_serial AS "MachineSerial",j."DeliveredAt",j."CustomerSignatory",j."ContentSha256",b."Id" AS bom_id FROM target j JOIN advance.actual_boms b ON b."CompanyId"=j."CompanyId" AND b."JobOrderId"=j."Id"
 JOIN advance.actual_bom_entries e ON e."CompanyId"=b."CompanyId" AND e."ActualBomId"=b."Id" JOIN advance.machine_delivery_bom_entries snapshot ON snapshot."CompanyId"=e."CompanyId" AND snapshot."DeliveryChallanId"=j.dc_id AND snapshot."ActualBomEntryId"=e."Id"
), ancestors("EntryId","CompanyId","LayerId") AS (
 SELECT "Id","CompanyId","InventoryProvenanceLayerId" FROM entries WHERE "InventoryProvenanceLayerId" IS NOT NULL
 UNION
 SELECT a."EntryId",a."CompanyId",edge."FromProvenanceLayerId" FROM ancestors a
 JOIN advance.inventory_provenance_edges edge ON edge."CompanyId"=a."CompanyId" AND edge."ToProvenanceLayerId"=a."LayerId"
), root_lines("EntryId","CompanyId","LineId") AS (
 SELECT "Id","CompanyId","GoodsReceiptLineId" FROM entries WHERE "GoodsReceiptLineId" IS NOT NULL
 UNION
 SELECT a."EntryId",a."CompanyId",allocation."GoodsReceiptLineId" FROM ancestors a
 JOIN advance.inventory_provenance_goods_receipt_lot_origins origin ON origin."CompanyId"=a."CompanyId" AND origin."InventoryProvenanceLayerId"=a."LayerId"
 JOIN advance.goods_receipt_line_lot_allocations allocation ON allocation."CompanyId"=a."CompanyId" AND allocation."Id"=origin."GoodsReceiptLineLotAllocationId"
), component_rows AS (
 SELECT e.*,item."ItemCode",item."Name" AS item_name,jsonb_build_object(
  'EntryId',e."Id",'Kind',e."EntryKind",'Quantity',e."QuantityBase",'ItemCode',item."ItemCode",
  'RecordedMaterialValue',e."AcceptedMaterialValue",'RecordedCharges',e."AllocatedChargeValue",
  'IssueLine',(SELECT to_jsonb(il) FROM advance.material_issue_lines il WHERE il."CompanyId"=e."CompanyId" AND il."Id"=e."MaterialIssueLineId"),'Issue',(SELECT to_jsonb(ih) FROM advance.material_issue_lines il JOIN advance.material_issues ih ON ih."CompanyId"=il."CompanyId" AND ih."Id"=il."MaterialIssueId" WHERE il."CompanyId"=e."CompanyId" AND il."Id"=e."MaterialIssueLineId"),'MaterialIssueLineId',e."MaterialIssueLineId",'FitmentId',e."ComponentFitmentId",'ReversalId',e."ComponentFitmentReversalId",
  'SourceGrns',coalesce((SELECT jsonb_agg(jsonb_build_object(
    'GrnId',g."Id",'GrnNumber',g."GrnNumber",'LineId',gl."Id",'VendorId',v."Id",'Vendor',v."Name",
    'AcceptedBills',coalesce((SELECT jsonb_agg(jsonb_build_object('BillId',bill."Id",'BillNumber',bill."BillNumber",'BillLineId',bl."Id",
       'BilledQuantity',bl."BilledQuantity",'PayableValue',bl."BilledPayableValue",
       'ChargeAllocations',coalesce((SELECT jsonb_agg(to_jsonb(ca)) FROM advance.vendor_bill_charge_allocations ca
          WHERE ca."CompanyId"=e."CompanyId" AND ca."VendorBillLineId"=bl."Id"),'[]'::jsonb)))
      FROM advance.vendor_bill_lines bl JOIN advance.vendor_bills bill ON bill."CompanyId"=bl."CompanyId" AND bill."Id"=bl."VendorBillId"
      WHERE bl."CompanyId"=e."CompanyId" AND bl."GoodsReceiptLineId"=gl."Id" AND bill."Status"='ACCEPTED'),'[]'::jsonb),
    'QcHistory',coalesce((SELECT jsonb_agg(jsonb_build_object('InspectionId',q."Id",'InspectionNumber',q."InspectionNumber",
        'Revision',to_jsonb(rev),'Parameters',coalesce((SELECT jsonb_agg(to_jsonb(pr)) FROM advance.qc_inspection_parameter_results pr
          WHERE pr."CompanyId"=e."CompanyId" AND pr."QcInspectionRevisionId"=rev."Id"),'[]'::jsonb),
        'Concessions',coalesce((SELECT jsonb_agg(to_jsonb(concession)) FROM advance.inventory_concessions concession
          WHERE concession."CompanyId"=e."CompanyId" AND concession."QcInspectionRevisionId"=rev."Id"),'[]'::jsonb)) ORDER BY rev."RevisionNumber")
       FROM advance.qc_inspections q JOIN advance.qc_inspection_revisions rev ON rev."CompanyId"=q."CompanyId" AND rev."QcInspectionId"=q."Id"
       WHERE q."CompanyId"=e."CompanyId" AND q."GoodsReceiptLineId"=gl."Id"),'[]'::jsonb)) ORDER BY g."GrnNumber",gl."Id")
    FROM root_lines r JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=r."CompanyId" AND gl."Id"=r."LineId"
    JOIN advance.goods_receipts g ON g."CompanyId"=gl."CompanyId" AND g."Id"=gl."GoodsReceiptId"
    JOIN advance.vendors v ON v."Id"=g."VendorId" WHERE r."EntryId"=e."Id"),'[]'::jsonb),
  'AncestorLayers',coalesce((SELECT jsonb_agg(to_jsonb(layer) ORDER BY layer."CreatedAt",layer."Id")
    FROM ancestors a JOIN advance.inventory_provenance_layers layer ON layer."CompanyId"=a."CompanyId" AND layer."Id"=a."LayerId"
    WHERE a."EntryId"=e."Id"),'[]'::jsonb)
 ) AS payload FROM entries e JOIN advance.items item ON item."Id"=e."ItemId"
)
, valuation AS (
 SELECT v.* FROM (SELECT DISTINCT "CompanyId",bom_id FROM entries) e
 CROSS JOIN LATERAL jsonb_to_recordset(advance.get_actual_bom_landed_valuations(e."CompanyId",e.bom_id))
 AS v("actualBomEntryId" uuid,"acceptedMaterialValue" numeric,"allocatedChargeValue" numeric,"totalAcceptedValue" numeric)
), valued AS (
 SELECT r.*,u."Code" AS uom,
 r."AcceptedMaterialValue"+coalesce(v.material,0)-coalesce(rv.material,0) AS material,
 r."AllocatedChargeValue"+coalesce(v.charges,0)-coalesce(rv.charges,0) AS charges,
 coalesce((SELECT po."CurrencyCode" FROM advance.goods_receipt_lines gl JOIN advance.goods_receipts g ON g."CompanyId"=gl."CompanyId" AND g."Id"=gl."GoodsReceiptId"
 JOIN advance.purchase_orders po ON po."CompanyId"=g."CompanyId" AND po."Id"=g."PurchaseOrderId"
 WHERE gl."CompanyId"=r."CompanyId" AND gl."Id"=r."GoodsReceiptLineId"),'UNVALUED') AS currency
 FROM component_rows r JOIN advance.uoms u ON u."Id"=r."UomId"
 LEFT JOIN LATERAL(SELECT sum("acceptedMaterialValue") material,sum("allocatedChargeValue") charges FROM valuation WHERE "actualBomEntryId"=r."Id") v ON true
 LEFT JOIN LATERAL(SELECT sum(vv."acceptedMaterialValue") material,sum(vv."allocatedChargeValue") charges
 FROM advance.component_fitment_reversals reverse JOIN entries original ON original."ComponentFitmentId"=reverse."ComponentFitmentId" AND original."CompanyId"=reverse."CompanyId"
 JOIN valuation vv ON vv."actualBomEntryId"=original."Id" WHERE reverse."CompanyId"=r."CompanyId" AND reverse."Id"=r."ComponentFitmentReversalId") rv ON true
)
SELECT jsonb_build_object('machineSerial',r."MachineSerial",'itemId',r."ItemId",'uom',r.uom,'currency',r.currency) AS group_filter,
 jsonb_build_object('machineSerial',r."MachineSerial",'dcNumber',r."DcNumber",'itemCode',r."ItemCode",'itemName',r.item_name,'uom',r.uom,'currency',r.currency) AS labels,
 jsonb_build_object('machineSerial',r."MachineSerial",'uom',r.uom,'currency',r.currency) AS total_group,
 jsonb_build_object('machineSerial',r."MachineSerial",'dcNumber',r."DcNumber",'deliveredAt',r."DeliveredAt",'customerSignatory',r."CustomerSignatory",'signatureSha256',r."ContentSha256",
 'entryId',r."Id",'entryKind',r."EntryKind",'itemCode',r."ItemCode",'itemName',r.item_name,'uom',r.uom,'currency',r.currency,
 'fitmentId',r."ComponentFitmentId",'reversalId',r."ComponentFitmentReversalId",'issueLineId',r."MaterialIssueLineId",'grnLineId',r."GoodsReceiptLineId",
 'grnDocuments',(SELECT string_agg(g->>'GrnNumber','; ') FROM jsonb_array_elements(r.payload->'SourceGrns') g),
 'vendors',(SELECT string_agg(DISTINCT g->>'Vendor','; ') FROM jsonb_array_elements(r.payload->'SourceGrns') g),
 'bills',(SELECT string_agg(b->>'BillNumber','; ') FROM jsonb_array_elements(r.payload->'SourceGrns') g CROSS JOIN LATERAL jsonb_array_elements(g->'AcceptedBills') b),
 'qcDocuments',(SELECT string_agg(q->>'InspectionNumber','; ') FROM jsonb_array_elements(r.payload->'SourceGrns') g CROSS JOIN LATERAL jsonb_array_elements(g->'QcHistory') q),
 'evidencePart',part.n,'evidence',CASE WHEN part.n=0 THEN '' ELSE substring(r.payload::text FROM (part.n-1)*10000+1 FOR 10000) END) AS detail,
 jsonb_build_object('quantity',CASE WHEN part.n=0 THEN r."QuantityBase" ELSE 0 END,
 'materialValue',CASE WHEN part.n=0 THEN r.material ELSE 0 END,'allocatedCharges',CASE WHEN part.n=0 THEN r.charges ELSE 0 END,
 'landedValue',CASE WHEN part.n=0 THEN r.material+r.charges ELSE 0 END) AS metrics,
 r."OccurredAt"::text||':'||r."Id"::text||':'||lpad(part.n::text,8,'0') AS sort_key
FROM valued r CROSS JOIN LATERAL generate_series(0,ceil(length(r.payload::text)/10000.0)::integer) part(n)
""";
}
