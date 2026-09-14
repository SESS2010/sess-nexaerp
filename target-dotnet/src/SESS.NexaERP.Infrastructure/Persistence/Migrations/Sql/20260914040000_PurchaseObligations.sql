CREATE FUNCTION advance.purchase_obligations(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_queue text,p_vendor uuid,p_currency text,p_document uuid,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $obligations$
WITH company AS MATERIALIZED (
 SELECT c."Id",c."Code" FROM advance.companies c
 WHERE c."Code"=p_organization AND c."IsActive" AND c."Status"='ACTIVE'
   AND EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=p_employee
     AND e."LoginEnabled" AND upper(e."Status")='ACTIVE')
   AND(SELECT count(*) FROM advance.employee_company_assignments a
     WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=p_employee
       AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=current_date
       AND(a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date))=1
),
effective_roles AS MATERIALIZED (
 SELECT DISTINCT r."Id",r."Code" FROM advance.employee_role_assignments a
 JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive"
 JOIN company c ON c."Id"=a."CompanyId"
 WHERE a."EmployeeId"=p_employee AND a."Id"=ANY(p_assignments)
   AND a."ApprovalStatus" IN('Approved','SeedApproved') AND a."EffectiveFrom"<=current_date
   AND(a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date)
   AND EXISTS(SELECT 1 FROM advance.company_role_activations ca WHERE ca."CompanyId"=a."CompanyId"
     AND ca."RoleId"=a."RoleId" AND ca."IsEnabled" AND ca."EffectiveFrom"<=current_date
     AND(ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=current_date))
),
scopes AS MATERIALIZED (
 SELECT s.* FROM advance.employee_operational_scopes s JOIN company c
   ON c."Id"=s."CompanyId" AND c."Code"=s."OrganizationId"
 WHERE s."EmployeeId"=p_employee AND s."IsActive" AND s."EffectiveFrom"<=current_date
   AND(s."EffectiveTo" IS NULL OR s."EffectiveTo">=current_date)
),
role_grants AS MATERIALIZED (
 SELECT p."PageKey",bool_or(rp."CanView" OR rp."HasFullControl") can_view,
   bool_or(rp."CanViewCommercialValues" OR rp."HasFullControl") can_commercial
 FROM advance.page_definitions p
 JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
 JOIN effective_roles r ON r."Id"=rp."RoleId"
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.purchase-obligations','purchase.requisitions',
   'purchase.rfq','purchase.vendor-quotations','purchase.commercial-comparisons','purchase.po')
 GROUP BY p."PageKey"
),
employee_grants AS MATERIALIZED (
 SELECT p."PageKey",bool_or(ep."CanView") can_view
 FROM advance.employee_page_permissions ep JOIN company c ON c."Id"=ep."CompanyId"
 JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId" AND p."IsActive"
 WHERE ep."EmployeeId"=p_employee GROUP BY p."PageKey"
),
access AS MATERIALIZED (
 SELECT (SELECT "Id" FROM company) company_id,
   EXISTS(SELECT 1 FROM company) AND EXISTS(SELECT 1 FROM scopes)
   AND EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
   AND coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-obligations'),false)
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.purchase-obligations'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.purchase-obligations'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-obligations'),false) commercial
),


calendar AS (
 SELECT (CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date today
),
permitted_orders AS MATERIALIZED (
 SELECT po.* FROM advance.purchase_orders po JOIN access a ON a.allowed AND po."CompanyId"=a.company_id
 WHERE EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=po."RequestingDepartmentId")
    AND(s."WarehouseId" IS NULL OR s."WarehouseId"=po."DeliveryWarehouseId")
    AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR po."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
unbilled AS MATERIALIZED (
 SELECT 'grni'::text queue,g."Id" document_id,g."GrnNumber" document_number,l."Id" line_id,
   po."Id" po_id,po."RootPurchaseOrderId" root_po_id,po."PoNumber" po_number,
   v."Id" vendor_id,v."VendorCode" vendor_code,g."VendorNameSnapshot" vendor_name,
   l."ItemId" item_id,l."ItemCodeSnapshot" item_code,l."ItemNameSnapshot" item_name,l."UomSnapshot" uom,
   (g."ReceivedAt" AT TIME ZONE p_report_timezone)::date source_date,
   l."ReceivedQuantity"-coalesce(billed.quantity,0) quantity,po."CurrencyCode" currency,
   (l."ReceivedQuantity"-coalesce(billed.quantity,0))*l."UnitRateSnapshot" amount,
   l."UnitRateSnapshot" unit_rate,NULL::numeric original_amount,NULL::numeric adjusted_amount
 FROM advance.goods_receipts g
 JOIN permitted_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=g."CompanyId"
 JOIN advance.goods_receipt_lines l ON l."GoodsReceiptId"=g."Id" AND l."CompanyId"=g."CompanyId"
 JOIN advance.vendors v ON v."Id"=g."VendorId" AND v."Id"=po."VendorId"
 LEFT JOIN LATERAL (
   SELECT sum(bl."BilledQuantity") quantity FROM advance.vendor_bill_lines bl
   JOIN advance.vendor_bills b ON b."Id"=bl."VendorBillId" AND b."CompanyId"=g."CompanyId"
   WHERE bl."CompanyId"=g."CompanyId" AND bl."GoodsReceiptLineId"=l."Id" AND b."Status"='ACCEPTED'
 ) billed ON true
 WHERE g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
   AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal
     WHERE reversal."CompanyId"=g."CompanyId" AND reversal."ReversesGoodsReceiptId"=g."Id"
       AND reversal."Status"='FINALIZED')
),
advances AS MATERIALIZED (
 SELECT 'vendor-advances'::text queue,a."Id" document_id,a."AdvanceNumber" document_number,NULL::uuid line_id,
   po."Id" po_id,po."RootPurchaseOrderId" root_po_id,po."PoNumber" po_number,
   v."Id" vendor_id,v."VendorCode" vendor_code,v."Name" vendor_name,
   NULL::uuid item_id,NULL::text item_code,NULL::text item_name,NULL::text uom,
   a."PaidDate" source_date,NULL::numeric quantity,a."CurrencyCode" currency,
   a."Amount"-coalesce(adjustments.amount,0)+coalesce(restorations.amount,0) amount,
   NULL::numeric unit_rate,a."Amount" original_amount,
   coalesce(adjustments.amount,0)-coalesce(restorations.amount,0) adjusted_amount
 FROM advance.vendor_advances a JOIN permitted_orders po
   ON po."Id"=a."PurchaseOrderId" AND po."CompanyId"=a."CompanyId"
 JOIN advance.vendors v ON v."Id"=a."VendorId" AND v."Id"=po."VendorId"
 LEFT JOIN LATERAL (
   SELECT sum(x."Amount") amount FROM advance.vendor_advance_adjustments x
   WHERE x."CompanyId"=a."CompanyId" AND x."VendorAdvanceId"=a."Id"
 ) adjustments ON true
 LEFT JOIN LATERAL (
   SELECT sum(r."Amount") amount FROM advance.vendor_advance_adjustment_restorations r
   JOIN advance.vendor_advance_adjustments x ON x."Id"=r."VendorAdvanceAdjustmentId"
     AND x."CompanyId"=a."CompanyId"
   WHERE r."CompanyId"=a."CompanyId" AND x."VendorAdvanceId"=a."Id"
 ) restorations ON true
 WHERE NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
   WHERE r."CompanyId"=a."CompanyId" AND r."VendorAdvanceId"=a."Id")
),
source AS MATERIALIZED (
 SELECT * FROM unbilled WHERE quantity<>0
 UNION ALL SELECT * FROM advances WHERE amount<>0 OR adjusted_amount<0
),
integrity AS (
 SELECT NOT EXISTS(SELECT 1 FROM source WHERE amount<0 OR quantity<0 OR adjusted_amount<0
   OR adjusted_amount>original_amount OR length(currency)<>3) valid
),
visible AS MATERIALIZED (
 SELECT s.*,greatest(0,c.today-s.source_date) age_days FROM source s CROSS JOIN calendar c
 WHERE (s.queue='grni' AND s.quantity>0) OR(s.queue='vendor-advances' AND s.amount>0)
),
queues(key,title,basis) AS (VALUES
 ('grni','Goods received not billed','Unbilled receipt quantity at receipt unit rate, by native PO currency; age from receipt date.'),
 ('vendor-advances','Vendor advances outstanding','Unreversed advances less adjustments plus restorations, by native currency; age from payment date.')
),
amounts AS (
 SELECT queue,currency,count(DISTINCT document_id) document_count,count(*) line_count,
   sum(amount) amount,max(age_days) oldest_age_days FROM visible GROUP BY queue,currency
),
tiles AS (
 SELECT q.key,jsonb_build_object('key',q.key,'title',q.title,'basis',q.basis,
   'count',(SELECT count(DISTINCT document_id) FROM visible WHERE queue=q.key),
   'oldestAgeDays',(SELECT max(age_days) FROM visible WHERE queue=q.key),
   'amounts',coalesce((SELECT jsonb_agg(jsonb_build_object('currency',a.currency,
     'documentCount',a.document_count,'lineCount',a.line_count,'value',a.amount,
     'oldestAgeDays',a.oldest_age_days) ORDER BY a.currency) FROM amounts a WHERE a.queue=q.key),'[]'::jsonb)) value
 FROM queues q
),
vendors AS (
 SELECT queue,vendor_id,vendor_code,max(vendor_name) vendor_name,currency,
   count(DISTINCT document_id) document_count,sum(amount) amount,max(age_days) oldest_age_days
 FROM visible GROUP BY queue,vendor_id,vendor_code,currency
),
selected AS MATERIALIZED (
 SELECT * FROM visible WHERE(p_queue IS NULL OR queue=p_queue)
   AND(p_vendor IS NULL OR vendor_id=p_vendor) AND(p_currency IS NULL OR currency=p_currency)
   AND(p_document IS NULL OR document_id=p_document)
),
paged AS (
 SELECT * FROM selected ORDER BY source_date,queue,document_id,line_id OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.source_date,p.queue,p.document_id,p.line_id,jsonb_build_object(
   'queue',p.queue,'documentId',p.document_id,'documentNumber',p.document_number,'lineId',p.line_id,
   'purchaseOrderId',p.po_id,'rootPurchaseOrderId',p.root_po_id,'poNumber',p.po_number,
   'vendorId',p.vendor_id,'vendorCode',p.vendor_code,'vendorName',p.vendor_name,
   'itemId',p.item_id,'itemCode',p.item_code,'itemName',p.item_name,'uom',p.uom,
   'sourceDate',p.source_date,'ageDays',p.age_days,'quantity',p.quantity,'currency',p.currency,
   'value',p.amount,'unitRate',p.unit_rate,'originalAmount',p.original_amount,'adjustedAmount',p.adjusted_amount) value
 FROM paged p
)
SELECT jsonb_build_object('allowed',a.allowed,'valid',i.valid,
 'requestValid',(p_queue IS NULL OR p_queue IN('grni','vendor-advances')) AND p_offset>=0 AND p_page_size BETWEEN 1 AND 1000,
 'data',CASE WHEN a.allowed AND i.valid THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'tiles',(SELECT jsonb_agg(value ORDER BY key) FROM tiles),
 'vendors',coalesce((SELECT jsonb_agg(jsonb_build_object('queue',v.queue,'vendorId',v.vendor_id,
   'vendorCode',v.vendor_code,'vendorName',v.vendor_name,'currency',v.currency,
   'documentCount',v.document_count,'value',v.amount,'oldestAgeDays',v.oldest_age_days)
   ORDER BY v.queue,v.amount DESC,v.vendor_id,v.currency) FROM vendors v),'[]'::jsonb),
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY source_date,queue,document_id,line_id) FROM rows),'[]'::jsonb)) END)
FROM access a CROSS JOIN integrity i;
$obligations$;
