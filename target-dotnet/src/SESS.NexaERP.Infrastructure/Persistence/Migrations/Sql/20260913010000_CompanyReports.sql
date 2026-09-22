CREATE FUNCTION advance.company_report_grni(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF false AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
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
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.grni'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.grni'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.grni'),false))
    AND (NOT true OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.grni'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.grni',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.grni',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (SELECT jsonb_build_object('vendorId',v."Id",'itemId',i."Id",'uom',l."UomSnapshot",'currency',po."CurrencyCode") AS group_filter,
  jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',i."ItemCode",'itemName',i."Name",
    'uom',l."UomSnapshot",'currency',po."CurrencyCode") AS labels,
  jsonb_build_object('uom',l."UomSnapshot",'currency',po."CurrencyCode") AS total_group,
  jsonb_build_object('grnLineId',l."Id",'grnNumber',g."GrnNumber",'receivedDate',(g."ReceivedAt" AT TIME ZONE p_report_timezone)::date,
    'vendorCode',v."VendorCode",'vendor',g."VendorNameSnapshot",'poNumber',po."PoNumber",'itemCode',l."ItemCodeSnapshot",
    'itemName',l."ItemNameSnapshot",'uom',l."UomSnapshot",'currency',po."CurrencyCode",
    'daysUninvoiced',greatest(0,p_to_date-(g."ReceivedAt" AT TIME ZONE p_report_timezone)::date),
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
    AND (b."DecidedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
    AND (b."Status"='ACCEPTED' OR b."Status"='REVERSED' AND (b."ReversedAt" AT TIME ZONE p_report_timezone)::date>p_to_date)
) billed ON true
WHERE g."DocumentKind"='NORMAL' AND g."FinalizedAt" IS NOT NULL
  AND (g."FinalizedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
  AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal
    WHERE reversal."CompanyId"=a.company_id AND reversal."ReversesGoodsReceiptId"=g."Id"
      AND reversal."Status"='FINALIZED' AND (reversal."FinalizedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date)
  AND l."ReceivedQuantity"-coalesce(billed.quantity,0)<>0),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE true AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'quantity')::numeric),0) AS "quantity",coalesce(sum((metrics->>'receiptValue')::numeric),0) AS "receiptValue"
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
  SELECT total_group,sum("quantity") AS "quantity",sum("receiptValue") AS "receiptValue",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'quantity',g."quantity",'receiptValue',g."receiptValue",
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
    'group',t.total_group,'quantity',t."quantity",'receiptValue',t."receiptValue",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
REVOKE ALL ON FUNCTION advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) FROM PUBLIC;
DO $roles$
BEGIN
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
    ALTER FUNCTION advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) OWNER TO nexa_erp_owner;
  END IF;
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
    GRANT EXECUTE ON FUNCTION advance.company_report_grni(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
END $roles$;
CREATE FUNCTION advance.company_report_vendor_purchases(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF false AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
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
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.vendor-purchases'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.vendor-purchases'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.vendor-purchases'),false))
    AND (NOT true OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.vendor-purchases'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.vendor-purchases',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.vendor-purchases',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (SELECT jsonb_build_object('vendorId',v."Id",'itemId',i."Id",'uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS group_filter,
  jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',i."ItemCode",'itemName',i."Name",
    'uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS labels,
  jsonb_build_object('uom',gl."UomSnapshot",'currency',po."CurrencyCode") AS total_group,
  jsonb_build_object('billLineId',l."Id",'billNumber',b."BillNumber",'billDate',b."BillDate",'event',event.kind,
    'eventDate',(event.occurred_at AT TIME ZONE p_report_timezone)::date,'grnNumber',g."GrnNumber",
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
WHERE (event.occurred_at AT TIME ZONE p_report_timezone)::date BETWEEN p_from_date AND p_to_date),
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
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
REVOKE ALL ON FUNCTION advance.company_report_vendor_purchases(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) FROM PUBLIC;
DO $roles$
BEGIN
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
    ALTER FUNCTION advance.company_report_vendor_purchases(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) OWNER TO nexa_erp_owner;
  END IF;
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
    GRANT EXECUTE ON FUNCTION advance.company_report_vendor_purchases(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
END $roles$;
CREATE FUNCTION advance.company_report_purchase_register(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF false AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
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
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.purchase-register'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.purchase-register'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.purchase-register'),false))
    AND (NOT false OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.purchase-register'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.purchase-register',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.purchase-register',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (WITH register_events AS (
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
    'eventDate',(event.occurred_at AT TIME ZONE p_report_timezone)::date,
    'itemCode',prl."ItemCodeSnapshot",'itemName',prl."ItemNameSnapshot",'uom',event.uom,'vendor',event.vendor) AS detail,
  jsonb_build_object('requested',event.requested,'ordered',event.ordered,'received',event.received,'billed',event.billed) AS metrics,
  event.occurred_at::text||':'||event.stage||':'||event.source_id::text||':'||event.document_id::text AS sort_key
FROM register_events event
JOIN access a ON a.allowed
JOIN advance.purchase_requisition_lines prl ON prl."Id"=event.pr_line_id AND prl."CompanyId"=a.company_id
JOIN advance.purchase_requisitions pr ON pr."Id"=prl."PurchaseRequisitionId" AND pr."CompanyId"=a.company_id
WHERE (event.occurred_at AT TIME ZONE p_report_timezone)::date<=p_to_date),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE true AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'requested')::numeric),0) AS "requested",coalesce(sum((metrics->>'ordered')::numeric),0) AS "ordered",coalesce(sum((metrics->>'received')::numeric),0) AS "received",coalesce(sum((metrics->>'billed')::numeric),0) AS "billed"
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
  SELECT total_group,sum("requested") AS "requested",sum("ordered") AS "ordered",sum("received") AS "received",sum("billed") AS "billed",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'requested',g."requested",'ordered',g."ordered",'received',g."received",'billed',g."billed",
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
    'group',t.total_group,'requested',t."requested",'ordered',t."ordered",'received',t."received",'billed',t."billed",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
REVOKE ALL ON FUNCTION advance.company_report_purchase_register(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) FROM PUBLIC;
DO $roles$
BEGIN
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
    ALTER FUNCTION advance.company_report_purchase_register(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) OWNER TO nexa_erp_owner;
  END IF;
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
    GRANT EXECUTE ON FUNCTION advance.company_report_purchase_register(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
END $roles$;
CREATE FUNCTION advance.company_report_pending_approvals(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF true AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
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
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.pending-approvals'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.pending-approvals'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.pending-approvals'),false))
    AND (NOT false OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.pending-approvals'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.pending-approvals',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.pending-approvals',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (WITH approval_roles AS (
  SELECT DISTINCT r."Code"
  FROM advance.employee_role_assignments assignment
  JOIN effective_roles r ON r."Id"=assignment."RoleId"
  JOIN access a ON a.allowed AND assignment."CompanyId"=a.company_id
  WHERE assignment."EmployeeId"=p_employee AND assignment."Id"=ANY(p_assignments)
    AND assignment."AssignmentType"<>'SUPPORT'
    AND assignment."ApprovalStatus" IN ('Approved','SeedApproved')
    AND assignment."EffectiveFrom"<=current_date
    AND (assignment."EffectiveTo" IS NULL OR assignment."EffectiveTo">=current_date)
),
snapshot_documents AS (
  SELECT 'PR'::text AS kind,pr."Id" AS id,pr."PrNumber" AS number,pr."Status" AS status,
    coalesce(pr."UpdatedAt",pr."CreatedAt") AS waiting_since,
    pr."CompletedApprovalStepCount"+1 AS step_number,pr."ApprovalWorkflowSnapshotJson"::jsonb AS snapshot
  FROM advance.purchase_requisitions pr JOIN access a ON a.allowed AND pr."CompanyId"=a.company_id
  WHERE pr."Status" IN ('DepartmentVerified','PendingApproval')
  UNION ALL
  SELECT 'COMPARISON',c."Id",c."ComparisonNumber",c."Status",coalesce(c."UpdatedAt",c."CreatedAt"),
    c."CompletedApprovalStepCount"+1,c."ApprovalWorkflowSnapshotJson"::jsonb
  FROM advance.commercial_comparisons c JOIN access a ON a.allowed AND c."CompanyId"=a.company_id
  WHERE c."Status"='PendingApproval'
  UNION ALL
  SELECT 'PO',p."Id",p."PoNumber",p."Status",coalesce(p."UpdatedAt",p."CreatedAt"),
    p."CompletedApprovalStepCount"+1,p."ApprovalWorkflowSnapshotJson"::jsonb
  FROM advance.purchase_orders p JOIN access a ON a.allowed AND p."CompanyId"=a.company_id
  WHERE p."IsCurrentVersion" AND p."Status" IN ('PendingApproval','Resubmitted')
),
named_actions AS (
  SELECT d.kind,d.id,d.number,d.status,d.waiting_since,d.step_number,
    CASE WHEN step.value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN (step.value->>'employeeId')::uuid END AS approver_id,step.value->>'employeeCode' AS employee_code,
    CASE WHEN nullif(step.value->>'roleCode','') IS NOT NULL AND step.value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' THEN ARRAY[step.value->>'roleCode'] ELSE ARRAY[]::text[] END AS roles,
    CASE WHEN step.value IS NULL THEN 'Administrator must resolve the missing or ambiguous next approval step.' ELSE 'Workflow snapshot names the next employee and role.' END AS responsibility
  FROM snapshot_documents d
  LEFT JOIN LATERAL (
    SELECT CASE WHEN count(*)=1 THEN jsonb_agg(value)->0 END AS value FROM jsonb_array_elements(CASE WHEN jsonb_typeof(d.snapshot->'steps')='array' THEN d.snapshot->'steps' ELSE '[]'::jsonb END)
    WHERE value->>'stepNumber'=d.step_number::text
  ) step ON true
),
department_actions AS (
  SELECT 'PR_DEPARTMENT_VERIFY'::text AS kind,pr."Id" AS id,pr."PrNumber" AS number,pr."Status" AS status,
    coalesce(pr."UpdatedAt",pr."CreatedAt") AS waiting_since,0 AS step_number,
    CASE WHEN mapping.n=1 THEN mapping.employee_id END AS approver_id,NULL::text AS employee_code,
    CASE WHEN mapping.n=1 THEN ARRAY[mapping.role_code] ELSE ARRAY[]::text[] END AS roles,
    'Independent department verification by the single effective mapped approver.'::text AS responsibility
  FROM advance.purchase_requisitions pr JOIN access a ON a.allowed AND pr."CompanyId"=a.company_id
  CROSS JOIN LATERAL (
    SELECT count(*) AS n,(array_agg(m."PrimaryApproverEmployeeId"))[1] AS employee_id,
      (array_agg(m."ApproverRoleCode"))[1] AS role_code
    FROM advance.department_approval_mappings m
    WHERE m."CompanyId"=a.company_id AND m."DepartmentId"=pr."RequestingDepartmentId"
      AND m."ApprovalRouteCode"='MANAGER' AND m."IsActive" AND m."EffectiveFrom"<=current_date
      AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=current_date)
  ) mapping
  WHERE pr."Status"='Submitted'
),
role_actions AS (
  SELECT 'MIR'::text AS kind,m."Id" AS id,m."RequestNumber" AS number,m."Status" AS status,
    coalesce(m."UpdatedAt",m."CreatedAt") AS waiting_since,1 AS step_number,
    NULL::uuid AS approver_id,NULL::text AS employee_code,
    ARRAY['STORES_MANAGER','PRODUCTION_MANAGER']::text[] AS roles,
    'An independent Stores or Production Manager must decide.'::text AS responsibility
  FROM advance.material_issue_requests m JOIN access a ON a.allowed AND m."CompanyId"=a.company_id
  WHERE m."Status"='SUBMITTED'
  UNION ALL
  SELECT 'MIR_EXCESS',l."Id",m."RequestNumber"||' / line '||l."LineNumber",m."Status",
    l."CreatedAt",1,NULL,NULL,ARRAY['TECHNICAL_DIRECTOR'],'Technical Director decision on excess; creator cannot decide.'
  FROM advance.material_issue_request_lines l JOIN access a ON a.allowed AND l."CompanyId"=a.company_id
  JOIN advance.material_issue_requests m ON m."Id"=l."MaterialIssueRequestId" AND m."CompanyId"=a.company_id
  WHERE m."Status" IN ('SUBMITTED','APPROVED') AND l."ExcessBaseQuantitySnapshot">0
    AND NOT EXISTS(SELECT 1 FROM advance.material_issue_excess_decisions d
      WHERE d."CompanyId"=a.company_id AND d."MaterialIssueRequestLineId"=l."Id")
  UNION ALL
  SELECT 'VENDOR_BILL',b."Id",b."BillNumber",b."Status",coalesce(b."UpdatedAt",b."CreatedAt"),1,NULL,NULL,
    ARRAY['ACCOUNTS_MANAGER'],CASE WHEN b."MatchStatus"='MATCHED' THEN 'Accounts Manager acceptance or rejection.'
      ELSE 'Accounts review required; a mismatch cannot be accepted until corrected.' END
  FROM advance.vendor_bills b JOIN access a ON a.allowed AND b."CompanyId"=a.company_id
  WHERE b."Status"='DRAFT'
  UNION ALL
  SELECT 'JOB_ORDER_ACCOUNTS',j."Id",j."JobOrderNumber",j."Status",coalesce(j."UpdatedAt",j."CreatedAt"),1,NULL,NULL,
    ARRAY['ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER'],'Accounts confirmation is required before production work.'
  FROM advance.job_orders j JOIN access a ON a.allowed AND j."CompanyId"=a.company_id
  WHERE j."Status"='PENDING_ACCOUNTS'
  UNION ALL
  SELECT 'OPENING_STOCK',o."Id",'Opening stock through '||o."PeriodEnd",o."Status",
    coalesce(o."ValuedAt",o."CountedAt"),CASE WHEN o."Status"='COUNTED' THEN 1 ELSE 2 END,NULL,NULL,
    CASE WHEN o."Status"='COUNTED' THEN ARRAY['ACCOUNTS_MANAGER'] ELSE ARRAY['TECHNICAL_DIRECTOR'] END,
    CASE WHEN o."Status"='COUNTED' THEN 'Accounts value confirmation by a different employee.'
      ELSE 'Technical Director authorization; all three ceremony employees must be distinct.' END
  FROM advance.opening_stocks o JOIN access a ON a.allowed AND o."CompanyId"=a.company_id
  WHERE o."Status" IN ('COUNTED','VALUED')
  UNION ALL
  SELECT 'QC_CONCESSION',c."Id",c."ConcessionNumber",c."Status",c."CreatedAt",1,NULL,NULL,
    ARRAY['TECHNICAL_DIRECTOR'],'Technical acceptance or rejection of a failed QC result.'
  FROM advance.inventory_concessions c JOIN access a ON a.allowed AND c."CompanyId"=a.company_id
  WHERE c."Status"='DRAFT'
  UNION ALL
  SELECT 'ESTIMATED_BOM',r."Id",b."BomNumber"||' / revision '||r."RevisionNumber",r."Status",
    coalesce(r."SubmittedAt",r."CreatedAt"),1,NULL,NULL,ARRAY['TECHNICAL_DIRECTOR'],
    'Technical Director approval; the preparer cannot approve.'
  FROM advance.estimated_bom_revisions r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
  JOIN advance.estimated_boms b ON b."Id"=r."EstimatedBomId" AND b."CompanyId"=a.company_id
  WHERE r."Status"='SUBMITTED' AND r."RevisionNumber"=b."CurrentRevisionNumber"
  UNION ALL
  SELECT 'PRODUCTION_BOM',r."Id",b."BomNumber"||' / revision '||r."RevisionNumber",r."Status",
    coalesce(r."SubmittedAt",r."CreatedAt"),1,NULL,NULL,ARRAY['TECHNICAL_DIRECTOR'],
    'Technical Director approval; the preparer cannot approve.'
  FROM advance.production_bom_revisions r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
  JOIN advance.production_boms b ON b."Id"=r."ProductionBomId" AND b."CompanyId"=a.company_id
  WHERE r."Status"='SUBMITTED' AND r."RevisionNumber"=b."CurrentRevisionNumber"
  UNION ALL
  SELECT 'ENGINEERING_DOCUMENT',r."Id",d."DocumentNumber"||' / revision '||r."RevisionNumber",r."Status",
    coalesce(r."UpdatedAt",r."CreatedAt"),1,NULL,NULL,ARRAY['TECHNICAL_DIRECTOR'],
    'Technical Director approval of the submitted engineering revision.'
  FROM advance.engineering_document_revisions r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
  JOIN advance.engineering_documents d ON d."Id"=r."EngineeringDocumentId" AND d."CompanyId"=a.company_id
  WHERE r."Status"='SUBMITTED' AND r."Id"=d."CurrentRevisionId"
  UNION ALL
  SELECT 'TAX_GST',t."Id",t."HsnSacCode"||' / '||t."SupplyType",t."ApprovalStatus",
    coalesce(t."UpdatedAt",t."CreatedAt"),1,NULL,NULL,ARRAY['TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'],
    'Independent director decision on the proposed tax setting.'
  FROM advance.tax_gst_settings t JOIN access a ON a.allowed AND t."CompanyId"=a.company_id
  WHERE t."ApprovalStatus"='PendingApproval' AND t."DecisionEmployeeId" IS NULL
  UNION ALL
  SELECT 'VENDOR_QUALIFICATION',q."Id",q."QualificationCode",q."VerificationStatus"||' / '||q."ApprovalStatus",
    coalesce(q."UpdatedAt",q."CreatedAt"),CASE WHEN q."VerificationStatus"='Verified' THEN 2 ELSE 1 END,NULL,NULL,
    CASE WHEN q."VerificationStatus"='Verified' THEN
      (SELECT CASE WHEN count(*)=1 THEN array_agg(upper(btrim(p."PolicyValue"))) ELSE ARRAY[]::text[] END
       FROM advance.organization_policies p WHERE p."CompanyId"=a.company_id
         AND p."PolicyCode"='VENDOR_FINAL_APPROVER' AND p."IsActive" AND p."EffectiveFrom"<=current_date
         AND (p."EffectiveTo" IS NULL OR p."EffectiveTo">=current_date))
    ELSE ARRAY(SELECT r."Code" FROM advance.role_page_permissions rp
      JOIN advance.page_definitions page ON page."Id"=rp."PageDefinitionId" AND page."IsActive"
      JOIN advance.roles r ON r."Id"=rp."RoleId" AND r."IsActive"
      WHERE page."PageKey"='masters.vendor-qualifications' AND (rp."CanVerify" OR rp."HasFullControl") ORDER BY r."Code") END,
    'Independent qualification verification, then approval by the configured final approver.'
  FROM advance.vendor_qualifications q JOIN access a ON a.allowed AND q."CompanyId"=a.company_id
  WHERE q."IsActive" AND q."ApprovalStatus"='PendingApproval' AND q."VerificationStatus" IN ('PendingApproval','Verified')
  UNION ALL
  SELECT 'ITEM_MASTER',i."Id",i."ItemCode",i."ApprovalStatus",coalesce(i."UpdatedAt",i."CreatedAt"),1,NULL,NULL,
    ARRAY['STORES_MANAGER','PURCHASE_MANAGER'],'Shared item master; an independent Stores or Purchase Manager must approve.'
  FROM advance.items i JOIN access a ON a.allowed
  WHERE i."ApprovalStatus" IN ('Submitted','PendingApproval')
  UNION ALL
  SELECT 'CUSTOMER_MASTER',c."Id",c."CustomerCode",c."ApprovalStatus",coalesce(c."UpdatedAt",c."CreatedAt"),1,NULL,NULL,
    ARRAY(SELECT r."Code" FROM advance.role_page_permissions rp
      JOIN advance.page_definitions page ON page."Id"=rp."PageDefinitionId" AND page."IsActive"
      JOIN advance.roles r ON r."Id"=rp."RoleId" AND r."IsActive"
      WHERE page."PageKey"='masters.customers' AND (rp."CanApprove" OR rp."HasFullControl") ORDER BY r."Code"),
    'Shared customer master; configured customer approvers must decide.'
  FROM advance.customers c JOIN access a ON a.allowed
  WHERE c."ApprovalStatus" IN ('Submitted','PendingApproval')
  UNION ALL
  SELECT 'VENDOR_MASTER',v."Id",v."VendorCode",v."ApprovalStatus",coalesce(v."UpdatedAt",v."CreatedAt"),1,NULL,NULL,
    CASE WHEN v."CommercialVerificationStatus"<>'Approved' OR v."RequiresReverification" THEN ARRAY['ACCOUNTS_HEAD']
      ELSE (SELECT CASE WHEN count(*)=1 THEN array_agg(upper(btrim(p."PolicyValue"))) ELSE ARRAY[]::text[] END
       FROM advance.organization_policies p WHERE p."CompanyId"=a.company_id
         AND p."PolicyCode"='VENDOR_FINAL_APPROVER' AND p."IsActive" AND p."EffectiveFrom"<=current_date
         AND (p."EffectiveTo" IS NULL OR p."EffectiveTo">=current_date)) END,
    CASE WHEN v."CommercialVerificationStatus"<>'Approved' OR v."RequiresReverification"
      THEN 'Commercial verification is required before final vendor approval.'
      ELSE 'Shared vendor master; the configured final approver must decide.' END
  FROM advance.vendors v JOIN access a ON a.allowed
  WHERE v."ApprovalStatus" IN ('Submitted','PendingApproval')
  UNION ALL
  SELECT 'EMPLOYEE_MASTER',e."Id",e."EmployeeCode",e."ApprovalStatus",coalesce(e."UpdatedAt",e."CreatedAt"),1,NULL,NULL,
    ARRAY(SELECT r."Code" FROM advance.role_page_permissions rp
      JOIN advance.page_definitions page ON page."Id"=rp."PageDefinitionId" AND page."IsActive"
      JOIN advance.roles r ON r."Id"=rp."RoleId" AND r."IsActive"
      WHERE page."PageKey"='employees.master' AND (rp."CanApprove" OR rp."HasFullControl") ORDER BY r."Code"),
    'Employee master approval for an employee assigned to the selected company.'
  FROM advance.employees e JOIN access a ON a.allowed
  WHERE e."ApprovalStatus" IN ('Submitted','PendingApproval')
    AND EXISTS(SELECT 1 FROM advance.employee_company_assignments membership
      WHERE membership."CompanyId"=a.company_id AND membership."EmployeeId"=e."Id"
        AND membership."IsActive" AND membership."Status"='ACTIVE' AND membership."EffectiveFrom"<=current_date
        AND (membership."EffectiveTo" IS NULL OR membership."EffectiveTo">=current_date))


  UNION ALL
  SELECT 'MATERIAL_RETURN',r."Id",r."ReturnNumber",r."Status",r."DeclaredAt",1,NULL,NULL,
    ARRAY['STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER'],
    'Stores must accept the declared return; the returning employee cannot accept it.'
  FROM advance.material_returns r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
  WHERE r."Status"='SUBMITTED'
  UNION ALL
  SELECT 'QUOTATION_TECHNICAL_VERIFY',l."Id",q."QuotationNumber"||' / line '||l."LineNumber",q."Status",q."SubmittedAt",1,NULL,NULL,
    ARRAY['TECHNICAL_SUPPORT_MANAGER','TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR'],
    'Immutable technical verification of the current submitted quotation line.'
  FROM advance.vendor_quotation_lines l JOIN access a ON a.allowed AND l."CompanyId"=a.company_id
  JOIN advance.vendor_quotations q ON q."Id"=l."VendorQuotationId" AND q."CompanyId"=a.company_id
  WHERE q."IsCurrentRevision" AND q."Status"='Submitted'
    AND NOT EXISTS(SELECT 1 FROM advance.quotation_technical_verifications v
      WHERE v."CompanyId"=a.company_id AND v."VendorQuotationLineId"=l."Id")
  UNION ALL
  SELECT 'WAREHOUSE_MASTER',w."Id",w."WarehouseCode",w."ApprovalStatus",coalesce(w."UpdatedAt",w."CreatedAt"),1,NULL,NULL,
    ARRAY(SELECT r."Code" FROM advance.role_page_permissions rp
      JOIN advance.page_definitions page ON page."Id"=rp."PageDefinitionId" AND page."IsActive"
      JOIN advance.roles r ON r."Id"=rp."RoleId" AND r."IsActive"
      WHERE page."PageKey"='masters.warehouses' AND (rp."CanApprove" OR rp."HasFullControl") ORDER BY r."Code"),
    'Warehouse master approval in the selected company.'
  FROM advance.warehouses w JOIN access a ON a.allowed AND w."CompanyId"=a.company_id
  WHERE w."ApprovalStatus" IN ('Submitted','PendingApproval')
  UNION ALL
  SELECT 'RACK_BIN_MASTER',b."Id",b."BinCode",b."ApprovalStatus",coalesce(b."UpdatedAt",b."CreatedAt"),1,NULL,NULL,
    ARRAY(SELECT r."Code" FROM advance.role_page_permissions rp
      JOIN advance.page_definitions page ON page."Id"=rp."PageDefinitionId" AND page."IsActive"
      JOIN advance.roles r ON r."Id"=rp."RoleId" AND r."IsActive"
      WHERE page."PageKey"='masters.rack-bins' AND (rp."CanApprove" OR rp."HasFullControl") ORDER BY r."Code"),
    'Rack/bin master approval in the selected company.'
  FROM advance.rack_bins b JOIN access a ON a.allowed AND b."CompanyId"=a.company_id
  WHERE b."ApprovalStatus" IN ('Submitted','PendingApproval')
),
pending_actions AS (
  SELECT * FROM named_actions UNION ALL SELECT * FROM department_actions UNION ALL SELECT * FROM role_actions
),
visible_actions AS (
  SELECT p.*,e."EmployeeName",coalesce(e."EmployeeCode",p.employee_code) AS approver_code,
    greatest(0,p_to_date-(p.waiting_since AT TIME ZONE p_report_timezone)::date) AS age_days,
    CASE WHEN p.approver_id IS NOT NULL THEN p.approver_id::text||':'||array_to_string(p.roles,'|')
      WHEN cardinality(p.roles)>0 THEN 'ROLE:'||array_to_string(p.roles,'|')
      ELSE 'UNASSIGNED:'||p.kind END AS approver_key,
    CASE WHEN p.approver_id IS NOT NULL THEN 'NAMED_EMPLOYEE'
      WHEN cardinality(p.roles)>0 THEN 'ROLE_POOL' ELSE 'UNRESOLVED' END AS assignment_kind
  FROM pending_actions p
  LEFT JOIN advance.employees e ON e."Id"=p.approver_id
  WHERE EXISTS(SELECT 1 FROM approval_roles WHERE "Code" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
    OR ((p.approver_id IS NULL OR p.approver_id=p_employee)
      AND EXISTS(SELECT 1 FROM approval_roles WHERE "Code"=ANY(p.roles)))
)
SELECT jsonb_build_object('approverKey',v.approver_key,'uom','ACTIONS') AS group_filter,
  labels.value AS labels,jsonb_build_object('uom','ACTIONS') AS total_group,
  labels.value||jsonb_build_object('documentType',v.kind,'documentId',v.id,'document',v.number,
    'status',v.status,'sourceScope',CASE WHEN v.kind IN ('ITEM_MASTER','CUSTOMER_MASTER','VENDOR_MASTER') THEN 'SHARED_MASTER' ELSE 'COMPANY' END,'step',v.step_number,'waitingSince',v.waiting_since,'ageDays',v.age_days,
    'responsibility',v.responsibility) AS detail,
  jsonb_build_object('pendingActions',1,'age0to1',CASE WHEN v.age_days<=1 THEN 1 ELSE 0 END,
    'age2to7',CASE WHEN v.age_days BETWEEN 2 AND 7 THEN 1 ELSE 0 END,
    'age8to30',CASE WHEN v.age_days BETWEEN 8 AND 30 THEN 1 ELSE 0 END,
    'ageOver30',CASE WHEN v.age_days>30 THEN 1 ELSE 0 END) AS metrics,
  v.waiting_since::text||':'||v.kind||':'||v.id::text AS sort_key
FROM visible_actions v
CROSS JOIN LATERAL (
  SELECT jsonb_build_object('approverCode',coalesce(v.approver_code,
      CASE WHEN v.assignment_kind='ROLE_POOL' THEN 'ROLE POOL' ELSE 'UNASSIGNED' END),
    'approverName',coalesce(v."EmployeeName",
      CASE WHEN v.assignment_kind='ROLE_POOL' THEN array_to_string(v.roles,' / ') ELSE 'Administrator must resolve the approver' END),
    'role',array_to_string(v.roles,' / '),'assignmentKind',v.assignment_kind) AS value
) labels),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE true AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'pendingActions')::numeric),0) AS "pendingActions",coalesce(sum((metrics->>'age0to1')::numeric),0) AS "age0to1",coalesce(sum((metrics->>'age2to7')::numeric),0) AS "age2to7",coalesce(sum((metrics->>'age8to30')::numeric),0) AS "age8to30",coalesce(sum((metrics->>'ageOver30')::numeric),0) AS "ageOver30"
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
  SELECT total_group,sum("pendingActions") AS "pendingActions",sum("age0to1") AS "age0to1",sum("age2to7") AS "age2to7",sum("age8to30") AS "age8to30",sum("ageOver30") AS "ageOver30",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'pendingActions',g."pendingActions",'age0to1',g."age0to1",'age2to7',g."age2to7",'age8to30',g."age8to30",'ageOver30',g."ageOver30",
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
    'group',t.total_group,'pendingActions',t."pendingActions",'age0to1',t."age0to1",'age2to7',t."age2to7",'age8to30',t."age8to30",'ageOver30',t."ageOver30",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
REVOKE ALL ON FUNCTION advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) FROM PUBLIC;
DO $roles$
BEGIN
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
    ALTER FUNCTION advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) OWNER TO nexa_erp_owner;
  END IF;
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
    GRANT EXECUTE ON FUNCTION advance.company_report_pending_approvals(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
END $roles$;
CREATE FUNCTION advance.company_report_fifo_valuation(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF false AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
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
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.fifo-valuation'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.fifo-valuation'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.fifo-valuation'),false))
    AND (NOT true OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.fifo-valuation'),false))
    AS allowed
),
report_availability AS MATERIALIZED (WITH cost_layers AS (
  SELECT f.*,i."ItemCode",i."Name" AS item_name,coalesce(gl."UomSnapshot",u."Code",i."Uom") AS uom,
    coalesce(po."CurrencyCode",ownership."CurrencyCode") AS currency,owners.owner_count,owners.owner_id,
    coalesce(holder."HolderNameSnapshot",ownership."AccountCode") AS owner_name,
    coalesce(used.quantity,0) AS consumed_quantity,f."QuantityReceived"-coalesce(used.quantity,0) AS remaining_quantity,
    coalesce(landed."LandedUnitRate",f."UnitCost") AS effective_unit_cost,
    CASE WHEN landed."Id" IS NOT NULL THEN 'BILL_LANDED' ELSE f."CostBasis" END AS effective_basis,
    landed."VendorBillLineId" AS accepted_bill_line_id,g."GrnNumber",opening."LineReference",
    greatest(0,p_to_date-(f."ReceivedAt" AT TIME ZONE p_report_timezone)::date) AS age_days
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
    WHERE m."CompanyId"=a.company_id AND m."MovementLeg"='RECEIPT_IN' AND m."PostingDate"<=p_to_date
      AND ((f."GoodsReceiptLineId" IS NOT NULL AND m."GoodsReceiptLineId"=f."GoodsReceiptLineId")
        OR (f."OpeningStockLineId" IS NOT NULL AND m."OpeningStockLineId"=f."OpeningStockLineId"))
  ) owners
  LEFT JOIN advance.inventory_ownership_accounts ownership ON ownership."Id"=owners.owner_id AND ownership."CompanyId"=a.company_id
  LEFT JOIN advance.inventory_account_holders holder ON holder."Id"=ownership."AccountHolderId" AND holder."CompanyId"=a.company_id
  LEFT JOIN LATERAL (
    SELECT sum(c."Quantity") AS quantity FROM advance.fifo_cost_consumptions c
    WHERE c."CompanyId"=a.company_id AND c."FifoInventoryCostLayerId"=f."Id"
      AND (c."ConsumedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
  ) used ON true
  LEFT JOIN LATERAL (
    SELECT adj.* FROM advance.fifo_landed_cost_adjustments adj
    JOIN advance.vendor_bill_lines bl ON bl."Id"=adj."VendorBillLineId" AND bl."CompanyId"=a.company_id
    JOIN advance.vendor_bills bill ON bill."Id"=bl."VendorBillId" AND bill."CompanyId"=a.company_id
    WHERE adj."CompanyId"=a.company_id AND adj."FifoInventoryCostLayerId"=f."Id"
      AND bill."Status" IN ('ACCEPTED','REVERSED')
      AND (bill."DecidedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
      AND (bill."ReversedAt" IS NULL OR (bill."ReversedAt" AT TIME ZONE p_report_timezone)::date>p_to_date)
    ORDER BY bill."DecidedAt" DESC,adj."CreatedAt" DESC,adj."Id" LIMIT 1
  ) landed ON true
  WHERE (f."ReceivedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
    AND (f."OpeningStockLineId" IS NOT NULL OR (g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
      AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversed
        WHERE reversed."CompanyId"=a.company_id AND reversed."ReversesGoodsReceiptId"=g."Id"
          AND reversed."Status"='FINALIZED' AND (reversed."FinalizedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date)))
), problems AS (
  SELECT EXISTS(
    SELECT 1 FROM advance.material_returns r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
    JOIN advance.material_return_lines rl ON rl."MaterialReturnId"=r."Id" AND rl."CompanyId"=a.company_id
    WHERE r."Status"='ACCEPTED' AND (r."AcceptedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
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
FROM problems),report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN NOT a.allowed THEN 'Denied' WHEN (SELECT ready FROM report_availability) THEN 'Export' ELSE 'Unavailable' END,
    'CompanyReport','reports.fifo-valuation',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed AND (SELECT ready FROM report_availability) THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.fifo-valuation',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode,'sourceIssue',(SELECT issue_code FROM report_availability))::text
  FROM access a WHERE p_export OR NOT a.allowed OR NOT (SELECT ready FROM report_availability)
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (WITH cost_layers AS (
  SELECT f.*,i."ItemCode",i."Name" AS item_name,coalesce(gl."UomSnapshot",u."Code",i."Uom") AS uom,
    coalesce(po."CurrencyCode",ownership."CurrencyCode") AS currency,owners.owner_count,owners.owner_id,
    coalesce(holder."HolderNameSnapshot",ownership."AccountCode") AS owner_name,
    coalesce(used.quantity,0) AS consumed_quantity,f."QuantityReceived"-coalesce(used.quantity,0) AS remaining_quantity,
    coalesce(landed."LandedUnitRate",f."UnitCost") AS effective_unit_cost,
    CASE WHEN landed."Id" IS NOT NULL THEN 'BILL_LANDED' ELSE f."CostBasis" END AS effective_basis,
    landed."VendorBillLineId" AS accepted_bill_line_id,g."GrnNumber",opening."LineReference",
    greatest(0,p_to_date-(f."ReceivedAt" AT TIME ZONE p_report_timezone)::date) AS age_days
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
    WHERE m."CompanyId"=a.company_id AND m."MovementLeg"='RECEIPT_IN' AND m."PostingDate"<=p_to_date
      AND ((f."GoodsReceiptLineId" IS NOT NULL AND m."GoodsReceiptLineId"=f."GoodsReceiptLineId")
        OR (f."OpeningStockLineId" IS NOT NULL AND m."OpeningStockLineId"=f."OpeningStockLineId"))
  ) owners
  LEFT JOIN advance.inventory_ownership_accounts ownership ON ownership."Id"=owners.owner_id AND ownership."CompanyId"=a.company_id
  LEFT JOIN advance.inventory_account_holders holder ON holder."Id"=ownership."AccountHolderId" AND holder."CompanyId"=a.company_id
  LEFT JOIN LATERAL (
    SELECT sum(c."Quantity") AS quantity FROM advance.fifo_cost_consumptions c
    WHERE c."CompanyId"=a.company_id AND c."FifoInventoryCostLayerId"=f."Id"
      AND (c."ConsumedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
  ) used ON true
  LEFT JOIN LATERAL (
    SELECT adj.* FROM advance.fifo_landed_cost_adjustments adj
    JOIN advance.vendor_bill_lines bl ON bl."Id"=adj."VendorBillLineId" AND bl."CompanyId"=a.company_id
    JOIN advance.vendor_bills bill ON bill."Id"=bl."VendorBillId" AND bill."CompanyId"=a.company_id
    WHERE adj."CompanyId"=a.company_id AND adj."FifoInventoryCostLayerId"=f."Id"
      AND bill."Status" IN ('ACCEPTED','REVERSED')
      AND (bill."DecidedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
      AND (bill."ReversedAt" IS NULL OR (bill."ReversedAt" AT TIME ZONE p_report_timezone)::date>p_to_date)
    ORDER BY bill."DecidedAt" DESC,adj."CreatedAt" DESC,adj."Id" LIMIT 1
  ) landed ON true
  WHERE (f."ReceivedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
    AND (f."OpeningStockLineId" IS NOT NULL OR (g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
      AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversed
        WHERE reversed."CompanyId"=a.company_id AND reversed."ReversesGoodsReceiptId"=g."Id"
          AND reversed."Status"='FINALIZED' AND (reversed."FinalizedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date)))
)SELECT jsonb_build_object('itemId',l."ItemId",'ownershipAccountId',l.owner_id,'ownership',l.owner_name,'uom',l.uom,
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
WHERE l.remaining_quantity>0),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE (SELECT ready FROM report_availability) AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'quantity')::numeric),0) AS "quantity",coalesce(sum((metrics->>'value')::numeric),0) AS "value"
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
  SELECT total_group,sum("quantity") AS "quantity",sum("value") AS "value",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'quantity',g."quantity",'value',g."value",
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
  'allowed',(SELECT allowed FROM access),'generatedAt',statement_timestamp(),'timeZone',p_report_timezone,'sourceReady',(SELECT ready FROM report_availability),'sourceIssue',(SELECT issue_code FROM report_availability),
  'totalRows',CASE WHEN p_mode='details' THEN coalesce((SELECT sum(source_count) FROM numbered_groups),0)
    ELSE (SELECT count(*) FROM numbered_groups) END,
  'totalSourceRows',coalesce((SELECT sum(source_count) FROM numbered_groups),0),
  'totals',coalesce((SELECT jsonb_agg(t.total_group||jsonb_build_object(
    'group',t.total_group,'quantity',t."quantity",'value',t."value",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
REVOKE ALL ON FUNCTION advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) FROM PUBLIC;
DO $roles$
BEGIN
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
    ALTER FUNCTION advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) OWNER TO nexa_erp_owner;
  END IF;
  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
    GRANT EXECUTE ON FUNCTION advance.company_report_fifo_valuation(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text) TO nexa_erp_runtime;
  END IF;
END $roles$;
