-- Read-only component-source trace. Not a delivered-machine report or permission witness.
-- Costs below are retained BOM values; final report must reuse late valuation adjustments.
\set ON_ERROR_STOP on
BEGIN READ ONLY;
WITH RECURSIVE target AS (
 SELECT j.* FROM advance.job_orders j JOIN advance.companies c ON c."Id"=j."CompanyId"
 WHERE c."Code"=:'company_code' AND j."MachineSerial"=:'machine_serial'
), entries AS (
 SELECT e.* FROM target j JOIN advance.actual_boms b ON b."CompanyId"=j."CompanyId" AND b."JobOrderId"=j."Id"
 JOIN advance.actual_bom_entries e ON e."CompanyId"=b."CompanyId" AND e."ActualBomId"=b."Id"
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
 SELECT e."Id",e."OccurredAt",jsonb_build_object(
  'EntryId',e."Id",'Kind',e."EntryKind",'Quantity',e."QuantityBase",'ItemCode',item."ItemCode",
  'RecordedMaterialValue',e."AcceptedMaterialValue",'RecordedCharges',e."AllocatedChargeValue",
  'FitmentId',e."ComponentFitmentId",'ReversalId',e."ComponentFitmentReversalId",
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
SELECT jsonb_build_object('Status','SOURCE_TRACE_ONLY_NOT_DELIVERY_PROOF',
 'Jobs',coalesce((SELECT jsonb_agg(jsonb_build_object('JobId',"Id",'MachineSerial',"MachineSerial",'FatReadinessStatus',"FatReadinessStatus")) FROM target),'[]'::jsonb),
 'EntryCount',(SELECT count(*) FROM entries),
 'Components',coalesce((SELECT jsonb_agg(payload ORDER BY "OccurredAt","Id") FROM component_rows),'[]'::jsonb));
ROLLBACK;