CREATE FUNCTION advance.company_report_machine_dossier(p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
 p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text) LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance AS $report$
#variable_conflict use_column
BEGIN
 IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Reports require runtime.'; END IF;
 IF p_group_filter->>'machineSerial' IS NULL OR length(btrim(p_group_filter->>'machineSerial'))=0 THEN RAISE EXCEPTION 'Select one machine serial.'; END IF;
 RETURN QUERY
WITH company AS MATERIALIZED (
  SELECT c."Id",c."Code"
  FROM advance.companies c
  WHERE c."Code"=p_organization AND c."IsActive" AND c."Status"='ACTIVE'
    AND EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=p_employee
      AND e."LoginEnabled" AND upper(e."Status")='ACTIVE')
    AND (SELECT count(*) FROM advance.employee_company_assignments a
      WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=p_employee
        AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=CURRENT_DATE
        AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE))=1
    AND EXISTS(SELECT 1 FROM advance.employee_operational_scopes s
      WHERE s."CompanyId"=c."Id" AND s."OrganizationId"=c."Code"
        AND s."EmployeeId"=p_employee AND s."IsActive" AND s."EffectiveFrom"<=CURRENT_DATE
        AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=CURRENT_DATE))
),
effective_roles AS MATERIALIZED (
  SELECT DISTINCT r."Id",r."Code"
  FROM advance.employee_role_assignments a
  JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive"
  JOIN company c ON c."Id"=a."CompanyId"
  WHERE a."EmployeeId"=p_employee AND a."Id"=ANY(p_assignments)
    AND a."ApprovalStatus" IN ('Approved','SeedApproved')
    AND a."EffectiveFrom"<=CURRENT_DATE
    AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
    AND EXISTS(SELECT 1 FROM advance.company_role_activations ca
      WHERE ca."CompanyId"=a."CompanyId" AND ca."RoleId"=a."RoleId"
        AND ca."IsEnabled" AND ca."EffectiveFrom"<=CURRENT_DATE
        AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=CURRENT_DATE))
),
report_grants AS MATERIALIZED (
  SELECT p."PageKey",
    bool_or(rp."CanView" OR rp."HasFullControl") AS can_view,
    bool_or(rp."CanExport" OR rp."HasFullControl") AS can_export,
    bool_or(rp."CanViewCommercialValues" OR rp."HasFullControl") AS can_commercial
  FROM advance.page_definitions p
  JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
  JOIN effective_roles r ON r."Id"=rp."RoleId"
  WHERE p."IsActive" AND p."PageKey" LIKE 'reports.%'
  GROUP BY p."PageKey"
),
employee_report_grants AS MATERIALIZED (
  SELECT p."PageKey",bool_or(ep."CanView") AS can_view
  FROM advance.employee_page_permissions ep
  JOIN company c ON c."Id"=ep."CompanyId"
  JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId" AND p."IsActive"
  WHERE ep."EmployeeId"=p_employee AND p."PageKey" LIKE 'reports.%'
  GROUP BY p."PageKey"
),access AS MATERIALIZED (
  SELECT (SELECT "Id" FROM company) AS company_id,
    EXISTS(SELECT 1 FROM company)
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.machine-dossier'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.machine-dossier'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.machine-dossier'),false))
    AND (NOT true OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.machine-dossier'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.machine-dossier',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.machine-dossier',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (WITH RECURSIVE target AS (
 SELECT j.*,d."MachineSerial" AS delivered_serial,d."Id" AS dc_id,d."DcNumber",d."Nature",s."DeliveredAt",s."CustomerSignatory",s."ContentSha256" FROM advance.job_orders j JOIN advance.companies c ON c."Id"=j."CompanyId" JOIN access a ON a.allowed AND a.company_id=j."CompanyId" JOIN advance.machine_delivery_challans d ON d."CompanyId"=j."CompanyId" AND d."JobOrderId"=j."Id" JOIN advance.machine_delivery_signatures s ON s."CompanyId"=d."CompanyId" AND s."DeliveryChallanId"=d."Id"
 WHERE d."MachineSerial"=(p_group_filter->>'machineSerial') AND (s."DeliveredAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
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
FROM valued r CROSS JOIN LATERAL generate_series(0,ceil(length(r.payload::text)/10000.0)::integer) part(n)),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE true AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'quantity')::numeric),0) AS "quantity",coalesce(sum((metrics->>'materialValue')::numeric),0) AS "materialValue",coalesce(sum((metrics->>'allocatedCharges')::numeric),0) AS "allocatedCharges",coalesce(sum((metrics->>'landedValue')::numeric),0) AS "landedValue"
  FROM selected_source GROUP BY group_filter,labels,total_group HAVING true
),
numbered_groups AS MATERIALIZED (
  SELECT *,row_number() OVER group_order AS summary_ordinal,
    sum(source_count) OVER group_order-source_count+1 AS detail_start
  FROM report_groups
  WINDOW group_order AS (ORDER BY total_group::text,group_filter::text,labels::text ROWS UNBOUNDED PRECEDING)
),
report_details AS (
  SELECT s.*,row_number() OVER(ORDER BY g.summary_ordinal,s.sort_key) AS detail_ordinal
  FROM selected_source s JOIN numbered_groups g
    ON s.group_filter=g.group_filter AND s.labels=g.labels AND s.total_group=g.total_group
  WHERE p_export OR p_mode='details'
),
report_totals AS (
  SELECT total_group,sum("quantity") AS "quantity",sum("materialValue") AS "materialValue",sum("allocatedCharges") AS "allocatedCharges",sum("landedValue") AS "landedValue",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'quantity',g."quantity",'materialValue',g."materialValue",'allocatedCharges',g."allocatedCharges",'landedValue',g."landedValue",
      'detailStart',g.detail_start,'detailCount',g.source_count) AS payload
  FROM numbered_groups g
  WHERE (p_export OR p_mode='summary')
    AND (p_export OR g.summary_ordinal>p_offset AND g.summary_ordinal<=p_offset+p_page_size)
  UNION ALL
  SELECT 2,d.detail_ordinal,d.detail||d.metrics||jsonb_build_object('group',d.group_filter)
  FROM report_details d
  WHERE (p_export OR p_mode='details')
    AND (p_export OR d.detail_ordinal>p_offset AND d.detail_ordinal<=p_offset+p_page_size)
)
SELECT 0 AS kind,0::bigint AS ordinal,jsonb_build_object(
  'allowed',(SELECT allowed FROM access),'generatedAt',statement_timestamp(),'timeZone',p_report_timezone,
  'totalRows',CASE WHEN p_mode='details' THEN coalesce((SELECT sum(source_count) FROM numbered_groups),0)
    ELSE (SELECT count(*) FROM numbered_groups) END,
  'totalSourceRows',coalesce((SELECT sum(source_count) FROM numbered_groups),0),
  'totals',coalesce((SELECT jsonb_agg(t.total_group||jsonb_build_object(
    'group',t.total_group,'quantity',t."quantity",'materialValue',t."materialValue",'allocatedCharges',t."allocatedCharges",'landedValue',t."landedValue",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal; END $report$;