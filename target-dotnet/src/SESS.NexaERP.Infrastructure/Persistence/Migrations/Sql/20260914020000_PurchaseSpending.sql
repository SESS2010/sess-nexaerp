CREATE FUNCTION advance.purchase_spending(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,
 p_period text,p_month date,p_vendor uuid,p_category uuid,p_currency text,p_bill uuid,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $spending$
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
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.purchase-spending','purchase.requisitions',
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
   AND coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-spending'),false)
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.purchase-spending'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.purchase-spending'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-spending'),false) commercial
),

calendar AS (
 SELECT (CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date today,
   date_trunc('month',CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date month_start,
   date_trunc('quarter',CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date quarter_start,
   (date_trunc('year',(CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)-interval '3 months')+interval '3 months')::date year_start
),
periods(key,from_date,to_date) AS (
 SELECT 'month',month_start,today FROM calendar
 UNION ALL SELECT 'quarter',quarter_start,today FROM calendar
 UNION ALL SELECT 'financial-year',year_start,today FROM calendar
),
months AS (
 SELECT to_char(value,'YYYY-MM') key,value::date from_date,
   least((value+interval '1 month'-interval '1 day')::date,c.today) to_date
 FROM calendar c CROSS JOIN LATERAL generate_series(c.month_start-interval '11 months',c.month_start,interval '1 month') value
),
selection AS (
 SELECT CASE p_period WHEN 'month' THEN coalesce(p_month,c.month_start)
   WHEN 'quarter' THEN c.quarter_start WHEN 'financial-year' THEN c.year_start
   WHEN 'twelve-months' THEN(c.month_start-interval '11 months')::date END from_date,
   CASE WHEN p_period='month' AND p_month IS NOT NULL THEN least((p_month+interval '1 month'-interval '1 day')::date,c.today)
     ELSE c.today END to_date,
   p_period IN('month','quarter','financial-year','twelve-months')
   AND(p_month IS NULL OR(p_period='month' AND extract(day FROM p_month)=1
      AND p_month BETWEEN(c.month_start-interval '11 months')::date AND c.month_start))
   AND p_offset>=0 AND p_page_size BETWEEN 1 AND 1000 valid
 FROM calendar c
),
source AS MATERIALIZED (
 SELECT b."Id" bill_id,l."Id" line_id,b."BillNumber" bill_number,
   event.kind,event.occurred_at,(event.occurred_at AT TIME ZONE p_report_timezone)::date event_date,
   po."Id" po_id,po."RootPurchaseOrderId" root_po_id,po."PoNumber" po_number,
   v."Id" vendor_id,v."VendorCode" vendor_code,v."Name" vendor_name,
   gl."ItemCategoryIdSnapshot" category_id,gl."ItemCategoryCodeSnapshot" category_code,
   gl."ItemId" item_id,gl."ItemCodeSnapshot" item_code,gl."ItemNameSnapshot" item_name,gl."UomSnapshot" uom,
   po."CurrencyCode" currency,event.sign*l."BilledQuantity" quantity,
   event.sign*l."BilledPayableValue" material_value,
   event.sign*coalesce(charges.value,0) allocated_charges,
   event.sign*(l."BilledPayableValue"+coalesce(charges.value,0)) amount
 FROM advance.vendor_bills b JOIN access a ON a.allowed AND b."CompanyId"=a.company_id
 JOIN advance.vendor_bill_lines l ON l."VendorBillId"=b."Id" AND l."CompanyId"=a.company_id
 JOIN advance.goods_receipt_lines gl ON gl."Id"=l."GoodsReceiptLineId" AND gl."CompanyId"=a.company_id
   AND gl."GoodsReceiptId"=b."GoodsReceiptId"
 JOIN advance.purchase_orders po ON po."Id"=b."PurchaseOrderId" AND po."CompanyId"=a.company_id
 JOIN advance.vendors v ON v."Id"=b."VendorId" AND v."Id"=po."VendorId"
 CROSS JOIN calendar c
 CROSS JOIN LATERAL (
   SELECT 'ACCEPTED'::text kind,b."DecidedAt" occurred_at,1 sign WHERE b."Status" IN('ACCEPTED','REVERSED')
   UNION ALL SELECT 'REVERSED',b."ReversedAt",-1 WHERE b."Status"='REVERSED'
 ) event
 LEFT JOIN LATERAL (
   SELECT sum(ch."AllocatedChargeValue") value FROM advance.vendor_bill_charge_allocations ch
   WHERE ch."CompanyId"=a.company_id AND ch."VendorBillLineId"=l."Id"
 ) charges ON true
 WHERE(event.occurred_at AT TIME ZONE p_report_timezone)::date BETWEEN(c.month_start-interval '11 months')::date AND c.today
 AND EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=po."RequestingDepartmentId")
    AND(s."WarehouseId" IS NULL OR s."WarehouseId"=po."DeliveryWarehouseId")
    AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR po."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
period_amounts AS (
 SELECT p.key,s.currency,sum(s.material_value) material_value,sum(s.allocated_charges) allocated_charges,
   sum(s.amount) amount,count(DISTINCT s.root_po_id) po_count,count(DISTINCT s.bill_id) bill_count
 FROM periods p JOIN source s ON s.event_date BETWEEN p.from_date AND p.to_date GROUP BY p.key,s.currency
),
period_json AS (
 SELECT p.key,jsonb_build_object('key',p.key,'fromDate',p.from_date,'toDate',p.to_date,
   'amounts',coalesce((SELECT jsonb_agg(jsonb_build_object('currency',a.currency,'materialValue',a.material_value,
     'allocatedCharges',a.allocated_charges,'amount',a.amount,'poCount',a.po_count,'billCount',a.bill_count)
     ORDER BY a.currency) FROM period_amounts a WHERE a.key=p.key),'[]'::jsonb)) value FROM periods p
),
month_amounts AS (
 SELECT m.key,s.currency,sum(s.material_value) material_value,sum(s.allocated_charges) allocated_charges,
   sum(s.amount) amount,count(DISTINCT s.root_po_id) po_count,count(DISTINCT s.bill_id) bill_count
 FROM months m JOIN source s ON s.event_date BETWEEN m.from_date AND m.to_date GROUP BY m.key,s.currency
),
month_json AS (
 SELECT m.key,jsonb_build_object('key',m.key,'fromDate',m.from_date,'toDate',m.to_date,
   'amounts',coalesce((SELECT jsonb_agg(jsonb_build_object('currency',a.currency,'materialValue',a.material_value,
     'allocatedCharges',a.allocated_charges,'amount',a.amount,'poCount',a.po_count,'billCount',a.bill_count)
     ORDER BY a.currency) FROM month_amounts a WHERE a.key=m.key),'[]'::jsonb)) value FROM months m
),
vendor_totals AS (
 SELECT s.vendor_id id,s.vendor_code code,s.vendor_name name,s.currency,sum(s.material_value) material_value,
   sum(s.allocated_charges) allocated_charges,sum(s.amount) amount,count(DISTINCT s.root_po_id) po_count,
   count(DISTINCT s.bill_id) bill_count
 FROM source s CROSS JOIN calendar c WHERE s.event_date>=c.year_start
 GROUP BY s.vendor_id,s.vendor_code,s.vendor_name,s.currency
),
ranked_vendors AS (
 SELECT *,row_number() OVER(PARTITION BY currency ORDER BY amount DESC,id) rank FROM vendor_totals
),
category_totals AS (
 SELECT s.category_id id,s.category_code code,s.category_code name,s.currency,sum(s.material_value) material_value,
   sum(s.allocated_charges) allocated_charges,sum(s.amount) amount,count(DISTINCT s.root_po_id) po_count,
   count(DISTINCT s.bill_id) bill_count
 FROM source s CROSS JOIN calendar c WHERE s.event_date>=c.year_start
 GROUP BY s.category_id,s.category_code,s.currency
),
group_json AS (
 SELECT 'vendor' kind,v.currency,v.amount,v.id,jsonb_build_object('id',v.id,'code',v.code,'name',v.name,
   'currency',v.currency,'materialValue',v.material_value,'allocatedCharges',v.allocated_charges,'amount',v.amount,
   'poCount',v.po_count,'billCount',v.bill_count) value FROM ranked_vendors v WHERE v.rank<=10
 UNION ALL
 SELECT 'category',v.currency,v.amount,v.id,jsonb_build_object('id',v.id,'code',v.code,'name',v.name,
   'currency',v.currency,'materialValue',v.material_value,'allocatedCharges',v.allocated_charges,'amount',v.amount,
   'poCount',v.po_count,'billCount',v.bill_count) FROM category_totals v
),
selected AS MATERIALIZED (
 SELECT s.* FROM source s CROSS JOIN selection f WHERE f.valid AND s.event_date BETWEEN f.from_date AND f.to_date
   AND(p_vendor IS NULL OR s.vendor_id=p_vendor) AND(p_category IS NULL OR s.category_id=p_category)
   AND(p_currency IS NULL OR s.currency=p_currency) AND(p_bill IS NULL OR s.bill_id=p_bill)
),
paged AS (
 SELECT * FROM selected ORDER BY occurred_at DESC,bill_id,line_id,kind OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.occurred_at,p.bill_id,p.line_id,p.kind,jsonb_build_object('billId',p.bill_id,'billLineId',p.line_id,
   'billNumber',p.bill_number,'event',p.kind,'eventDate',p.event_date,
   'purchaseOrderId',p.po_id,'rootPurchaseOrderId',p.root_po_id,'poNumber',p.po_number,
   'vendorId',p.vendor_id,'vendorCode',p.vendor_code,'vendorName',p.vendor_name,
   'categoryId',p.category_id,'categoryCode',p.category_code,'itemId',p.item_id,'itemCode',p.item_code,
   'itemName',p.item_name,'uom',p.uom,'quantity',p.quantity,'currency',p.currency,
   'materialValue',p.material_value,'allocatedCharges',p.allocated_charges,'amount',p.amount) value FROM paged p
)
SELECT jsonb_build_object('allowed',a.allowed,'valid',f.valid,'data',CASE WHEN a.allowed AND f.valid THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'fromDate',f.from_date,'toDate',f.to_date,
 'periods',(SELECT jsonb_agg(value ORDER BY key) FROM period_json),
 'topVendors',coalesce((SELECT jsonb_agg(value ORDER BY currency,amount DESC,id) FROM group_json WHERE kind='vendor'),'[]'::jsonb),
 'categories',coalesce((SELECT jsonb_agg(value ORDER BY currency,amount DESC,id) FROM group_json WHERE kind='category'),'[]'::jsonb),
 'monthlyTrend',(SELECT jsonb_agg(value ORDER BY key) FROM month_json),
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY occurred_at DESC,bill_id,line_id,kind) FROM rows),'[]'::jsonb)) END)
FROM access a CROSS JOIN selection f;
$spending$;
