CREATE FUNCTION advance.purchase_workload(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_queue text,p_route text,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $workload$
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
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.purchase','purchase.requisitions',
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
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.purchase'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.purchase'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.purchase'),false) commercial
),
definitions(key,title,page_key,coverage) AS (
 VALUES
 ('pr-department-verification','Requisitions awaiting department verification','purchase.requisitions','Requisition estimated amounts are INR.'),
 ('pr-approval','Requisitions awaiting approval','purchase.requisitions','Stored approval route and next snapshot step; estimated amounts are INR.'),
 ('pr-stock-check','Requisitions awaiting stock check','purchase.requisitions','Final approval has completed; stock check is pending.'),
 ('rfq-no-quotation','RFQs issued with no quotation','purchase.rfq','One RFQ per count, with no current non-draft, non-withdrawn or non-rejected quotation.'),
 ('quotation-technical-verification','Quotations awaiting technical verification','purchase.vendor-quotations','One quotation per count; pending lines are separate.'),
 ('comparison-decision','Comparisons awaiting recommendation or approval','purchase.commercial-comparisons','Unrecommended comparisons have no selected commercial value.'),
 ('po-approved-unissued','POs approved but not issued','purchase.po','Current approved, unissued revisions; native-currency amounts.')
),
tile_access AS MATERIALIZED (
 SELECT d.*,a.allowed AND(coalesce(r.can_view,false) OR coalesce(e.can_view,false)) allowed,
   -- PR read permission already includes EstimatedTotal in the existing PR API.
   -- The dashboard commercial grant still controls whether estimates are displayed.
   a.commercial AND(d.page_key='purchase.requisitions' OR coalesce(r.can_commercial,false)) commercial
 FROM definitions d CROSS JOIN access a
 LEFT JOIN role_grants r ON r."PageKey"=d.page_key
 LEFT JOIN employee_grants e ON e."PageKey"=d.page_key
),
source AS MATERIALIZED (
 WITH visible_pr AS MATERIALIZED (
 SELECT p.*
 FROM advance.purchase_requisitions p JOIN access a ON a.allowed AND p."CompanyId"=a.company_id
 WHERE p."IsActive" AND (
   p."RequesterEmployeeId"=p_employee OR p."CreatorEmployeeId"=p_employee
   OR (p."ApprovalCycle">0 AND p."CompletedApprovalStepCount"<p."RequiredApprovalStepCount"
       AND p."ApprovalWorkflowSnapshotJson"::jsonb @> jsonb_build_object(
         'steps',jsonb_build_array(jsonb_build_object('employeeId',p_employee::text))))
   OR EXISTS(SELECT 1 FROM scopes s WHERE
      (s."DepartmentId" IS NULL OR s."DepartmentId"=p."RequestingDepartmentId")
      AND(s."WarehouseId" IS NULL OR s."WarehouseId"=p."DeliveryWarehouseId")
      AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR p."RequesterEmployeeId"=p_employee))
   OR EXISTS(SELECT 1 FROM scopes s WHERE s."AllowsPrivilegedCrossScope"
      AND EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
visible_rfq AS MATERIALIZED (
 SELECT r.* FROM advance.request_for_quotations r JOIN access a ON a.allowed AND r."CompanyId"=a.company_id
 WHERE r."IsActive" AND EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=r."RequestingDepartmentId")
     AND(s."WarehouseId" IS NULL OR s."WarehouseId"=r."DeliveryWarehouseId")
     AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR r."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
visible_comparison AS MATERIALIZED (
 SELECT c.* FROM advance.commercial_comparisons c
 JOIN access a ON a.allowed AND c."CompanyId"=a.company_id
 JOIN advance.request_for_quotations r ON r."Id"=c."RequestForQuotationId" AND r."CompanyId"=a.company_id
 WHERE EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=r."RequestingDepartmentId")
     AND(s."WarehouseId" IS NULL OR s."WarehouseId"=r."DeliveryWarehouseId")
     AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR c."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
visible_po AS MATERIALIZED (
 SELECT p.* FROM advance.purchase_orders p JOIN access a ON a.allowed AND p."CompanyId"=a.company_id
 WHERE p."IsCurrentVersion" AND EXISTS(SELECT 1 FROM scopes s WHERE
   ((s."DepartmentId" IS NULL OR s."DepartmentId"=p."RequestingDepartmentId")
     AND(s."WarehouseId" IS NULL OR s."WarehouseId"=p."DeliveryWarehouseId")
     AND s."RackBinId" IS NULL AND(NOT s."OwnRecordsOnly" OR p."OwnerEmployeeId"=p_employee))
   OR(s."AllowsPrivilegedCrossScope" AND EXISTS(SELECT 1 FROM effective_roles
     WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))))
),
raw AS (
 SELECT CASE p."Status" WHEN 'Submitted' THEN 'pr-department-verification'
       WHEN 'StockCheckPending' THEN 'pr-stock-check' ELSE 'pr-approval' END AS queue,
   'purchase.requisitions'::text AS page_key, 'PR'::text AS kind,p."Id" AS id,p."PrNumber" AS number,
   p."Status" AS status,
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_requisition_status_history h
     WHERE h."CompanyId"=p."CompanyId" AND h."PurchaseRequisitionId"=p."Id"
       AND h."NewStatus"=p."Status" AND h."PreviousStatus" IS DISTINCT FROM h."NewStatus"),p."CreatedAt") AS waiting_since,
   p."ApprovalRoute" AS approval_route,
   p."ApprovalWorkflowSnapshotJson"::jsonb AS snapshot,p."CompletedApprovalStepCount"+1 AS next_step,
   'INR'::text AS currency,p."EstimatedTotal" AS amount,
   '[]'::jsonb AS vendors,NULL::bigint AS pending_lines,
   '/api/v1/purchase/requisitions/'||p."PrNumber" AS detail_path
 FROM visible_pr p WHERE p."Status" IN('Submitted','DepartmentVerified','PendingApproval','StockCheckPending')
 UNION ALL
 SELECT 'rfq-no-quotation','purchase.rfq','RFQ',r."Id",r."RfqNumber",r."Status",
   coalesce(r."IssuedAt",r."CreatedAt"),NULL,'{}'::jsonb,0,NULL,NULL,
   coalesce((SELECT jsonb_agg(v."Name" ORDER BY v."Name",v."Id")
       FROM advance.rfq_vendor_invitations i JOIN advance.vendors v ON v."Id"=i."VendorId"
       WHERE i."CompanyId"=r."CompanyId" AND i."RequestForQuotationId"=r."Id" AND i."Status"='Issued'),'[]'::jsonb),
   NULL,'/api/v1/purchase/rfqs/'||r."RfqNumber"
 FROM visible_rfq r WHERE r."Status"='Issued' AND NOT EXISTS(
   SELECT 1 FROM advance.vendor_quotations q JOIN advance.rfq_vendor_invitations i
     ON i."Id"=q."RfqVendorInvitationId" AND i."CompanyId"=r."CompanyId"
   WHERE q."CompanyId"=r."CompanyId" AND i."RequestForQuotationId"=r."Id"
     AND q."IsCurrentRevision" AND q."Status" NOT IN('Draft','Withdrawn','Rejected','Superseded'))
 UNION ALL
 SELECT 'quotation-technical-verification','purchase.vendor-quotations','QUOTATION',
   q."Id",q."QuotationNumber",q."Status",q."SubmittedAt",NULL,'{}'::jsonb,0,
   q."CurrencyCode",q."TotalPayableValue",jsonb_build_array(v."Name"),pending.n,
   '/api/v1/purchase/quotations/'||q."QuotationNumber"
 FROM advance.vendor_quotations q
 JOIN advance.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId" AND i."CompanyId"=q."CompanyId"
 JOIN visible_rfq r ON r."Id"=i."RequestForQuotationId" AND r."CompanyId"=q."CompanyId"
 JOIN advance.vendors v ON v."Id"=q."VendorId"
 CROSS JOIN LATERAL (SELECT count(*) n FROM advance.vendor_quotation_lines l
   WHERE l."CompanyId"=q."CompanyId" AND l."VendorQuotationId"=q."Id" AND NOT EXISTS(
     SELECT 1 FROM advance.quotation_technical_verifications tv
       WHERE tv."CompanyId"=q."CompanyId" AND tv."VendorQuotationLineId"=l."Id")) pending
 WHERE q."IsCurrentRevision" AND q."Status"='Submitted' AND pending.n>0
 UNION ALL
 SELECT 'comparison-decision','purchase.commercial-comparisons','COMPARISON',
   c."Id",c."ComparisonNumber",c."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_transaction_status_history h
     WHERE h."CompanyId"=c."CompanyId" AND h."EntityId"=c."Id" AND h."EntityType"='CommercialComparison'
       AND h."ToStatus"=c."Status" AND h."FromStatus" IS DISTINCT FROM h."ToStatus"),c."CreatedAt"),
   c."ApprovalRoute",c."ApprovalWorkflowSnapshotJson"::jsonb,c."CompletedApprovalStepCount"+1,
   c."CurrencyCode",CASE WHEN c."RecommendedVendorQuotationId" IS NOT NULL THEN c."TotalPayableValue" END,
   '[]'::jsonb,NULL,'/api/v1/purchase/comparisons/'||c."ComparisonNumber"
 FROM visible_comparison c WHERE c."Status" IN('Draft','RevisionRequested','PendingApproval')
 UNION ALL
 SELECT 'po-approved-unissued','purchase.po','PO',p."Id",p."PoNumber",p."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_transaction_status_history h
     WHERE h."CompanyId"=p."CompanyId" AND h."EntityId"=p."Id" AND h."EntityType"='PurchaseOrder'
       AND h."ToStatus"='Approved' AND h."FromStatus" IS DISTINCT FROM h."ToStatus"),p."CreatedAt"),
   p."ApprovalRoute",'{}'::jsonb,0,p."CurrencyCode",p."TotalPayableValue",
   jsonb_build_array(v."Name"),NULL,'/api/v1/purchase/purchase-orders/'||p."PoNumber"
 FROM visible_po p JOIN advance.vendors v ON v."Id"=p."VendorId"
 WHERE p."Status"='Approved' AND p."IssuedAt" IS NULL
)
SELECT * FROM raw
),
visible AS MATERIALIZED (
 SELECT s.*,t.commercial,
   greatest(0,(CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date
     -(s.waiting_since AT TIME ZONE p_report_timezone)::date) age_days
 FROM source s JOIN tile_access t ON t.key=s.queue AND t.allowed
),
amounts AS (
 SELECT queue,jsonb_agg(jsonb_build_object('currency',currency,'amount',amount) ORDER BY currency) value
 FROM(SELECT queue,currency,sum(amount) amount FROM visible
   WHERE commercial AND amount IS NOT NULL GROUP BY queue,currency) sums GROUP BY queue
),
band_amounts AS (
 SELECT queue,approval_route,jsonb_agg(jsonb_build_object('currency',currency,'amount',amount) ORDER BY currency) value
 FROM(SELECT queue,approval_route,currency,sum(amount) amount FROM visible
   WHERE queue='pr-approval' AND commercial AND amount IS NOT NULL
   GROUP BY queue,approval_route,currency) sums GROUP BY queue,approval_route
),
bands AS (
 SELECT stats.queue,jsonb_agg(jsonb_build_object('approvalRoute',stats.approval_route,
   'count',stats.n,'oldestAgeDays',stats.age,'amounts',coalesce(a.value,'[]'::jsonb))
   ORDER BY stats.approval_route) value
 FROM(SELECT queue,approval_route,count(*) n,max(age_days) age FROM visible
   WHERE queue='pr-approval' GROUP BY queue,approval_route) stats
 LEFT JOIN band_amounts a ON a.queue=stats.queue AND a.approval_route=stats.approval_route
 GROUP BY stats.queue
),
tiles AS (
 SELECT t.key,jsonb_build_object('key',t.key,'title',t.title,
   'state',CASE WHEN t.allowed THEN 'READY' ELSE 'ACCESS_DENIED' END,
   'count',CASE WHEN t.allowed THEN(count(*) FILTER(WHERE v.id IS NOT NULL)) END,
   'oldestAgeDays',CASE WHEN t.allowed THEN max(v.age_days) END,
   'commercialValuesVisible',t.allowed AND t.commercial,
   'amounts',coalesce(a.value,'[]'::jsonb), 'approvalBands',coalesce(b.value,'[]'::jsonb), 'unvaluedDocumentCount',CASE WHEN t.allowed AND t.commercial AND t.key<>'rfq-no-quotation' THEN(count(*) FILTER(WHERE v.id IS NOT NULL AND v.amount IS NULL)) END, 'coverage',t.coverage) value
 FROM tile_access t LEFT JOIN visible v ON v.queue=t.key LEFT JOIN amounts a ON a.queue=t.key LEFT JOIN bands b ON b.queue=t.key
 GROUP BY t.key,t.title,t.allowed,t.commercial,t.coverage,a.value,b.value
),
selected AS MATERIALIZED (
 SELECT * FROM visible WHERE(p_queue IS NULL OR queue=p_queue)
   AND(p_route IS NULL OR approval_route=p_route)
),
paged AS (
 SELECT * FROM selected ORDER BY waiting_since,queue,id OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.waiting_since,p.queue,p.id,jsonb_build_object(
   'queue',p.queue,'documentId',p.id,'documentType',p.kind,'documentNumber',p.number,
   'status',p.status,'waitingSince',p.waiting_since,'ageDays',p.age_days,
   'approvalRoute',p.approval_route,'nextApproverEmployeeId',step.value->>'employeeId',
   'nextApproverEmployeeCode',step.value->>'employeeCode','nextApproverRole',step.value->>'roleCode',
   'responsibilityIssue',CASE WHEN p.queue='pr-approval' OR(p.queue='comparison-decision' AND p.status='PendingApproval')
     THEN CASE WHEN step.value IS NULL THEN 'Administrator must resolve the missing or ambiguous next approval step.' END END,
   'currency',CASE WHEN p.commercial THEN p.currency END,'value',CASE WHEN p.commercial THEN p.amount END,
   'vendors',p.vendors,'pendingLineCount',p.pending_lines,'detailPath',p.detail_path) value
 FROM paged p LEFT JOIN LATERAL(
   SELECT CASE WHEN count(*)=1 AND bool_and(value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' AND length(btrim(value->>'roleCode'))>0) THEN jsonb_agg(value)->0 END value
   FROM jsonb_array_elements(CASE WHEN jsonb_typeof(p.snapshot->'steps')='array'
     THEN p.snapshot->'steps' ELSE '[]'::jsonb END)
   WHERE value->>'stepNumber'=p.next_step::text
 ) step ON true
)
SELECT jsonb_build_object('allowed',a.allowed,'data',CASE WHEN a.allowed THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'tiles',coalesce((SELECT jsonb_agg(value ORDER BY key) FROM tiles),'[]'::jsonb),
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY waiting_since,queue,id) FROM rows),'[]'::jsonb)) END)
FROM access a;
$workload$;
