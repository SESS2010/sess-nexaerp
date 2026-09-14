CREATE FUNCTION advance.stores_qc_stock(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_queue text,p_document uuid,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $storesqcstock$
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
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.stores-qc-stock','inventory.grn')
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
   AND EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.stores-qc-stock'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.stores-qc-stock'),false)) AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='inventory.grn'),false) OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='inventory.grn'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.stores-qc-stock'),false) AND coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='inventory.grn'),false) commercial
),
balances AS MATERIALIZED (
 SELECT m."GoodsReceiptLineLotAllocationId" allocation_id,
   m."WarehouseId" warehouse_id,m."RackBinId" rack_id,m."OwnershipAccountId" ownership_id,
   m."CustodyAssignmentId" custody_id,m."InventoryProvenanceLayerId" provenance_id,m."InventorySerialId" serial_id,
   m."ConditionCode" queue,sum(m."QuantityIn"-m."QuantityOut") quantity
 FROM advance.stock_movements m JOIN access a ON a.allowed AND m."CompanyId"=a.company_id
 WHERE m."ConditionCode" IN('QC_HOLD','PENDING_RETURNABLE_DC') AND m."GoodsReceiptLineLotAllocationId" IS NOT NULL
 GROUP BY m."GoodsReceiptLineLotAllocationId",m."WarehouseId",m."RackBinId",
   m."OwnershipAccountId",m."CustodyAssignmentId",m."InventoryProvenanceLayerId",m."InventorySerialId",m."ConditionCode"
 HAVING sum(m."QuantityIn"-m."QuantityOut")>0
),
visible AS MATERIALIZED (
 SELECT b.*,l."Id" line_id,g."Id" document_id,g."GrnNumber" number,l."ItemId" item_id,l."ItemCodeSnapshot" item_code,
   l."ItemNameSnapshot" item_name,l."UomSnapshot" uom,g."ReceivedAt" received_at,g."QcDueAt" due_at,
   b.queue='QC_HOLD' AND g."QcDueAt"<CURRENT_TIMESTAMP overdue,
   greatest(0,(CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date-
     (g."ReceivedAt" AT TIME ZONE p_report_timezone)::date) age_days,
   po."CurrencyCode" currency,b.quantity*l."UnitRateSnapshot" receipt_value
 FROM balances b JOIN access a ON a.allowed
 JOIN advance.goods_receipt_line_lot_allocations allocation ON allocation."Id"=b.allocation_id AND allocation."CompanyId"=a.company_id
 JOIN advance.goods_receipt_lines l ON l."Id"=allocation."GoodsReceiptLineId" AND l."CompanyId"=a.company_id
 JOIN advance.goods_receipts g ON g."Id"=l."GoodsReceiptId" AND g."CompanyId"=a.company_id
 JOIN advance.purchase_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=a.company_id
 WHERE EXISTS(SELECT 1 FROM scopes scope WHERE
   (scope."DepartmentId" IS NULL OR scope."DepartmentId"=po."RequestingDepartmentId")
   AND(scope."WarehouseId" IS NULL OR scope."WarehouseId"=b.warehouse_id)
   AND(scope."RackBinId" IS NULL OR scope."RackBinId"=b.rack_id)
   AND(NOT scope."OwnRecordsOnly" OR g."ReceivedByEmployeeId"=p_employee))
   OR(EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
     AND EXISTS(SELECT 1 FROM scopes WHERE "AllowsPrivilegedCrossScope"))
),
currencies AS (
 SELECT queue,currency,sum(receipt_value) value FROM visible GROUP BY queue,currency
),
tiles AS (
 SELECT d.key,jsonb_build_object('key',d.key,'lineCount',count(DISTINCT v.line_id),
   'overdueLineCount',count(DISTINCT v.line_id) FILTER(WHERE v.overdue),'oldestReceiptAgeDays',max(v.age_days),
   'values',CASE WHEN a.commercial THEN coalesce((SELECT jsonb_agg(jsonb_build_object(
     'currency',c.currency,'receiptProvisionalValue',c.value) ORDER BY c.currency)
     FROM currencies c WHERE c.queue=d.key),'[]'::jsonb) END) value
 FROM (VALUES('QC_HOLD'),('PENDING_RETURNABLE_DC')) d(key) CROSS JOIN access a
 LEFT JOIN visible v ON v.queue=d.key GROUP BY d.key,a.commercial
),
selected AS MATERIALIZED (
 SELECT * FROM visible WHERE(p_queue IS NULL OR queue=p_queue) AND(p_document IS NULL OR document_id=p_document)
),
paged AS (
 SELECT * FROM selected ORDER BY received_at,queue,allocation_id,warehouse_id,rack_id,ownership_id,custody_id,provenance_id,serial_id
 OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.*,jsonb_build_object('queue',p.queue,'documentId',p.document_id,'documentNumber',p.number,
   'lineId',p.line_id,'allocationId',p.allocation_id,'itemId',p.item_id,'itemCode',p.item_code,'itemName',p.item_name,
   'uom',p.uom,'quantity',p.quantity,'warehouseId',p.warehouse_id,'rackBinId',p.rack_id,
   'ownershipAccountId',p.ownership_id,'custodyAssignmentId',p.custody_id,'provenanceLayerId',p.provenance_id,
   'serialId',p.serial_id,'receivedAt',p.received_at,'qcDueAt',p.due_at,'isOverdue',p.overdue,'receiptAgeDays',p.age_days,
   'currency',p.currency,'receiptProvisionalValue',CASE WHEN a.commercial THEN p.receipt_value END,
   'detailPath','/api/v1/stores/goods-receipts/'||p.document_id::text) value
 FROM paged p CROSS JOIN access a
)
SELECT jsonb_build_object('allowed',a.allowed,'data',CASE WHEN a.allowed THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'commercial',a.commercial,
 'valueBasis','Current GRN-origin held quantity times receipt unit rate, in native PO currency. Provisional receipt value; excludes later bill and landed-cost adjustments.',
 'tiles',coalesce((SELECT jsonb_agg(value ORDER BY key) FROM tiles),'[]'::jsonb),
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY received_at,queue,allocation_id,warehouse_id,rack_id,ownership_id,custody_id,provenance_id,serial_id) FROM rows),'[]'::jsonb)) END)
FROM access a;
$storesqcstock$;
