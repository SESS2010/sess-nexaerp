CREATE FUNCTION advance.purchase_open_orders(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_vendor uuid,p_currency text,p_root uuid,p_overdue_only boolean,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $openorders$
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
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.purchase-open-orders','purchase.requisitions',
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
   AND coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-open-orders'),false)
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.purchase-open-orders'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.purchase-open-orders'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase-open-orders'),false) commercial
),


calendar AS (
 SELECT (CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date today
),
issued AS MATERIALIZED (
 SELECT DISTINCT ON(po."CompanyId",po."RootPurchaseOrderId") po.*
 FROM advance.purchase_orders po JOIN access a ON a.allowed AND po."CompanyId"=a.company_id
 WHERE po."IssuedAt" IS NOT NULL
 ORDER BY po."CompanyId",po."RootPurchaseOrderId",po."RevisionNumber" DESC
),
permitted AS MATERIALIZED (
 SELECT po.*,first_issue.occurred_at first_issued_at,current_version."Id" current_id,
   current_version."Status" current_status,current_version."IssuedAt" current_issued_at,
   current_version."RevisionNumber" current_revision
 FROM issued po
 LEFT JOIN advance.purchase_orders current_version ON current_version."CompanyId"=po."CompanyId"
   AND current_version."RootPurchaseOrderId"=po."RootPurchaseOrderId" AND current_version."IsCurrentVersion"
 CROSS JOIN LATERAL (
   SELECT min(x."IssuedAt") occurred_at FROM advance.purchase_orders x
   WHERE x."CompanyId"=po."CompanyId" AND x."RootPurchaseOrderId"=po."RootPurchaseOrderId"
 ) first_issue
 WHERE EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=po."RequestingDepartmentId")
    AND(s."WarehouseId" IS NULL OR s."WarehouseId"=po."DeliveryWarehouseId")
    AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR po."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
line_values AS MATERIALIZED (
 SELECT po."Id" po_id,po."RootPurchaseOrderId" root_id,po."PoNumber" po_number,
   po."RevisionNumber" revision,po."Status" po_status,po."CurrencyCode" currency,
   po."IssuedAt" issued_at,po.first_issued_at,po.current_id,po.current_status,
   po.current_issued_at,po.current_revision,po."DeliveryTermsSnapshot" delivery_terms,
   v."Id" vendor_id,v."VendorCode" vendor_code,v."Name" vendor_name,
   l."Id" line_id,l."CommercialComparisonLineId" comparison_line_id,
   l."ItemId" item_id,l."ItemCodeSnapshot" item_code,l."ItemNameSnapshot" item_name,l."UomSnapshot" uom,
   l."OrderedQuantity" ordered_quantity,coalesce(received.quantity,0) received_quantity,
   l."OrderedQuantity"-coalesce(received.quantity,0) remaining_quantity,
   CASE WHEN l."OrderedQuantity">0 THEN
     (l."OrderedQuantity"-coalesce(received.quantity,0))*l."TotalPayableValue"/l."OrderedQuantity" END amount,
   ql."PromisedDeliveryDate" quoted_delivery_date,
   CASE WHEN cl."DeliverySnapshot"=to_char(ql."PromisedDeliveryDate",'YYYY-MM-DD')
     AND po."DeliveryTermsSnapshot"=q."DeliveryTermsSnapshot" THEN ql."PromisedDeliveryDate" END committed_delivery_date,
   EXISTS(SELECT 1 FROM advance.purchase_order_lines old_line
     JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId"
       AND old_po."CompanyId"=po."CompanyId" AND old_po."RootPurchaseOrderId"=po."RootPurchaseOrderId"
     WHERE old_line."CompanyId"=po."CompanyId" AND old_line."CommercialComparisonLineId"=l."CommercialComparisonLineId"
       AND(old_line."PurchaseRequisitionLineId" IS DISTINCT FROM l."PurchaseRequisitionLineId"
         OR old_line."PurchaseRequirementHandoffId" IS DISTINCT FROM l."PurchaseRequirementHandoffId"
         OR old_line."ItemId" IS DISTINCT FROM l."ItemId" OR old_line."UomSnapshot" IS DISTINCT FROM l."UomSnapshot")) lineage_changed
 FROM permitted po
 JOIN advance.purchase_order_lines l ON l."PurchaseOrderId"=po."Id" AND l."CompanyId"=po."CompanyId"
 JOIN advance.vendors v ON v."Id"=po."VendorId"
 JOIN advance.commercial_comparison_lines cl ON cl."Id"=l."CommercialComparisonLineId" AND cl."CompanyId"=po."CompanyId"
 JOIN advance.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId" AND ql."CompanyId"=po."CompanyId"
 JOIN advance.vendor_quotations q ON q."Id"=ql."VendorQuotationId" AND q."CompanyId"=po."CompanyId"
 LEFT JOIN LATERAL (
   SELECT sum(gl."ReceivedQuantity") quantity FROM advance.goods_receipt_lines gl
   JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=po."CompanyId"
   JOIN advance.purchase_order_lines old_line ON old_line."Id"=gl."PurchaseOrderLineId"
     AND old_line."CompanyId"=po."CompanyId" AND old_line."CommercialComparisonLineId"=l."CommercialComparisonLineId"
   JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId"
     AND old_po."CompanyId"=po."CompanyId" AND old_po."RootPurchaseOrderId"=po."RootPurchaseOrderId"
   WHERE gl."CompanyId"=po."CompanyId" AND g."Status"='FINALIZED' AND g."DocumentKind"='NORMAL'
     AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal WHERE reversal."CompanyId"=po."CompanyId"
       AND reversal."ReversesGoodsReceiptId"=g."Id" AND reversal."Status"='FINALIZED')
 ) received ON true
),
issues AS MATERIALIZED (
 SELECT DISTINCT root_id,po_number,CASE
   WHEN current_id IS NULL THEN 'CURRENT_REVISION_UNAVAILABLE'
   WHEN current_status='Cancelled' AND current_issued_at IS NULL AND current_revision>revision AND remaining_quantity>0
     THEN 'CANCELLED_UNISSUED_AMENDMENT'
   WHEN lineage_changed THEN 'LINE_PROVENANCE_INCONSISTENT'
   WHEN ordered_quantity<=0 OR remaining_quantity<0 OR amount<0 THEN 'RECEIPT_QUANTITY_INCONSISTENT'
   END code
 FROM line_values
 WHERE current_id IS NULL OR(current_status='Cancelled' AND current_issued_at IS NULL AND current_revision>revision AND remaining_quantity>0)
   OR lineage_changed OR ordered_quantity<=0 OR remaining_quantity<0 OR amount<0
),
visible AS MATERIALIZED (
 SELECT l.*,greatest(0,c.today-(l.first_issued_at AT TIME ZONE p_report_timezone)::date) age_days,
   CASE WHEN l.committed_delivery_date IS NULL THEN NULL WHEN l.committed_delivery_date<c.today THEN c.today-l.committed_delivery_date ELSE 0 END days_late
 FROM line_values l CROSS JOIN calendar c
 WHERE l.po_status<>'Cancelled' AND l.remaining_quantity>0
   AND NOT EXISTS(SELECT 1 FROM issues i WHERE i.root_id=l.root_id)
),
amounts AS (
 SELECT currency,count(DISTINCT root_id) po_count,sum(amount) amount,
   count(DISTINCT root_id) FILTER(WHERE days_late>0) overdue_count,
   coalesce(sum(amount) FILTER(WHERE days_late>0),0) overdue_amount
 FROM visible GROUP BY currency
),
selected AS MATERIALIZED (
 SELECT * FROM visible WHERE(p_vendor IS NULL OR vendor_id=p_vendor)
   AND(p_currency IS NULL OR currency=p_currency) AND(p_root IS NULL OR root_id=p_root)
   AND(NOT p_overdue_only OR days_late>0)
),
paged AS (
 SELECT * FROM selected ORDER BY first_issued_at,po_id,line_id OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.first_issued_at,p.po_id,p.line_id,jsonb_build_object(
   'purchaseOrderId',p.po_id,'rootPurchaseOrderId',p.root_id,'poNumber',p.po_number,
   'revisionNumber',p.revision,'currentRevisionNumber',p.current_revision,'currentStatus',p.current_status,
   'firstIssuedAt',p.first_issued_at,'issuedAt',p.issued_at,'ageDays',p.age_days,
   'vendorId',p.vendor_id,'vendorCode',p.vendor_code,'vendorName',p.vendor_name,
   'lineId',p.line_id,'itemId',p.item_id,'itemCode',p.item_code,'itemName',p.item_name,'uom',p.uom,
   'orderedQuantity',p.ordered_quantity,'receivedQuantity',p.received_quantity,'remainingQuantity',p.remaining_quantity,
   'currency',p.currency,'value',p.amount,'quotedDeliveryDate',p.quoted_delivery_date,
   'committedDeliveryDate',p.committed_delivery_date,'deliveryTerms',p.delivery_terms,'daysLate',p.days_late,
   'deliveryState',CASE WHEN p.committed_delivery_date IS NULL THEN 'CONFIRMATION_REQUIRED'
     WHEN p.days_late>0 THEN 'OVERDUE' ELSE 'WITHIN_COMMITMENT' END) value FROM paged p
)
SELECT jsonb_build_object('allowed',a.allowed,'data',CASE WHEN a.allowed THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'complete',NOT EXISTS(SELECT 1 FROM issues),
 'openPoCount',CASE WHEN NOT EXISTS(SELECT 1 FROM issues) THEN(SELECT count(DISTINCT root_id) FROM visible) END,
 'oldestAgeDays',CASE WHEN NOT EXISTS(SELECT 1 FROM issues) THEN(SELECT max(age_days) FROM visible) END,
 'deliveryComplete',NOT EXISTS(SELECT 1 FROM issues) AND NOT EXISTS(SELECT 1 FROM visible WHERE committed_delivery_date IS NULL),
 'overduePoCount',CASE WHEN NOT EXISTS(SELECT 1 FROM issues) AND NOT EXISTS(SELECT 1 FROM visible WHERE committed_delivery_date IS NULL) THEN(SELECT count(DISTINCT root_id) FROM visible WHERE days_late>0) END,
 'deliveryDateUnconfirmedPoCount',(SELECT count(DISTINCT root_id) FROM visible WHERE committed_delivery_date IS NULL),
 'sourceIssues',coalesce((SELECT jsonb_agg(jsonb_build_object('rootPurchaseOrderId',root_id,'poNumber',po_number,'code',code)
   ORDER BY po_number,root_id,code) FROM issues),'[]'::jsonb),
 'amounts',CASE WHEN NOT EXISTS(SELECT 1 FROM issues) THEN coalesce((SELECT jsonb_agg(jsonb_build_object(
   'currency',currency,'poCount',po_count,'value',amount,'overduePoCount',CASE WHEN NOT EXISTS(SELECT 1 FROM visible WHERE committed_delivery_date IS NULL) THEN overdue_count END,'overdueValue',CASE WHEN NOT EXISTS(SELECT 1 FROM visible WHERE committed_delivery_date IS NULL) THEN overdue_amount END)
   ORDER BY currency) FROM amounts),'[]'::jsonb) END,
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY first_issued_at,po_id,line_id) FROM rows),'[]'::jsonb)) END)
FROM access a;
$openorders$;
