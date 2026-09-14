CREATE FUNCTION advance.stores_workload(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_queue text,p_document uuid,p_offset bigint,p_page_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $storesworkload$
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
 WHERE p."IsActive" AND p."PageKey" IN('dashboards.stores-workload','inventory.grn','stores.material-issue-requests')
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
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='dashboards.stores-workload'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='dashboards.stores-workload'),false)) allowed,
   coalesce((SELECT can_commercial FROM role_grants WHERE "PageKey"='dashboards.stores-workload'),false) commercial
),
definitions(key,title,page_key,coverage) AS (
 VALUES
 ('gate-no-grn','Gate entries awaiting GRN','inventory.grn','Finalized normal unreversed gate entries with no normal GRN, including no draft GRN. Age starts at arrival.'),
 ('mir-approval','MIRs awaiting approval','stores.material-issue-requests','Submitted requests. Eligible approval roles are Stores Manager or Production Manager; no named approval assignment is currently recorded. Age starts at submission, or creation if submission history is unavailable.'),
 ('mir-unissued','Approved MIRs awaiting issue','stores.material-issue-requests','Approved or partially fulfilled requests with remaining lines. Returns do not reopen a fulfilled request. Age starts at approval.')
),
tile_access AS MATERIALIZED (
 SELECT d.*,a.allowed AND(coalesce(r.can_view,false) OR coalesce(e.can_view,false)) allowed
 FROM definitions d CROSS JOIN access a
 LEFT JOIN role_grants r ON r."PageKey"=d.page_key
 LEFT JOIN employee_grants e ON e."PageKey"=d.page_key
),
source(queue,id,kind,number,status,waiting_since,pending_lines,vendor_id,vendor_name,
  department_id,warehouse_id,rack_id,owner_id) AS MATERIALIZED (
 SELECT 'gate-no-grn',g."Id",'GATE_ENTRY',g."GateEntryNumber",g."Status",g."ArrivedAt",
   (SELECT count(*) FROM advance.gate_entry_lines l WHERE l."CompanyId"=g."CompanyId" AND l."GateEntryId"=g."Id"),
   g."VendorId",g."VendorNameSnapshot",p."RequestingDepartmentId",p."DeliveryWarehouseId",NULL::uuid,g."ReceivedByEmployeeId"
 FROM advance.gate_entries g JOIN access a ON a.allowed AND g."CompanyId"=a.company_id
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 WHERE g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
   AND NOT EXISTS(SELECT 1 FROM advance.gate_entries reversal WHERE reversal."CompanyId"=g."CompanyId"
     AND reversal."ReversesGateEntryId"=g."Id" AND reversal."Status"='FINALIZED')
   AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts grn WHERE grn."CompanyId"=g."CompanyId"
     AND grn."GateEntryId"=g."Id" AND grn."DocumentKind"='NORMAL')
 UNION ALL
 SELECT CASE WHEN m."Status"='SUBMITTED' THEN 'mir-approval' ELSE 'mir-unissued' END,
   m."Id",'MIR',m."RequestNumber",m."Status",
   CASE WHEN m."Status"='SUBMITTED' THEN coalesce(
     (SELECT max(h."OccurredAt") FROM advance.material_issue_history h WHERE h."CompanyId"=m."CompanyId"
       AND h."MaterialIssueRequestId"=m."Id" AND h."Action"='SUBMIT'),m."CreatedAt")
     ELSE coalesce(m."ApprovedAt",m."CreatedAt") END,
   lines.pending,m."VendorId",v."Name",m."RequestingDepartmentId",NULL::uuid,NULL::uuid,m."RequestedByEmployeeId"
 FROM advance.material_issue_requests m JOIN access a ON a.allowed AND m."CompanyId"=a.company_id
 LEFT JOIN advance.vendors v ON v."Id"=m."VendorId"
 CROSS JOIN LATERAL(
   SELECT count(*) pending FROM advance.material_issue_request_lines l
   WHERE l."CompanyId"=m."CompanyId" AND l."MaterialIssueRequestId"=m."Id"
     AND(m."Status"='SUBMITTED' OR l."RequestedBaseQuantity">coalesce(
       (SELECT sum(i."QuantityBase") FROM advance.material_issue_lines i WHERE i."CompanyId"=m."CompanyId"
         AND i."MaterialIssueRequestLineId"=l."Id"),0))
 ) lines
 WHERE m."Status" IN('SUBMITTED','APPROVED','PARTIALLY_FULFILLED') AND lines.pending>0
),
visible AS MATERIALIZED (
 SELECT s.*,greatest(0,(CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date
   -(s.waiting_since AT TIME ZONE p_report_timezone)::date) age_days
 FROM source s JOIN tile_access t ON t.key=s.queue AND t.allowed
 WHERE EXISTS(SELECT 1 FROM scopes scope WHERE
   (scope."DepartmentId" IS NULL OR scope."DepartmentId"=s.department_id)
   AND(scope."WarehouseId" IS NULL OR scope."WarehouseId"=s.warehouse_id)
   AND(scope."RackBinId" IS NULL OR scope."RackBinId"=s.rack_id)
   AND(NOT scope."OwnRecordsOnly" OR s.owner_id=p_employee))
   OR(EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
     AND EXISTS(SELECT 1 FROM scopes WHERE "AllowsPrivilegedCrossScope"))
),
tiles AS (
 SELECT t.key,jsonb_build_object('key',t.key,'title',t.title,
   'state',CASE WHEN t.allowed THEN 'READY' ELSE 'ACCESS_DENIED' END,
   'count',CASE WHEN t.allowed THEN count(v.id) END,
   'oldestAgeDays',CASE WHEN t.allowed THEN max(v.age_days) END,'coverage',t.coverage) value
 FROM tile_access t LEFT JOIN visible v ON v.queue=t.key
 GROUP BY t.key,t.title,t.allowed,t.coverage
),
selected AS MATERIALIZED (
 SELECT * FROM visible WHERE(p_queue IS NULL OR queue=p_queue) AND(p_document IS NULL OR id=p_document)
),
paged AS (
 SELECT * FROM selected ORDER BY waiting_since,queue,id OFFSET p_offset LIMIT p_page_size
),
rows AS (
 SELECT p.waiting_since,p.queue,p.id,jsonb_build_object(
   'queue',p.queue,'documentId',p.id,'documentType',p.kind,'documentNumber',p.number,
   'status',p.status,'waitingSince',p.waiting_since,'ageDays',p.age_days,'pendingLineCount',p.pending_lines,
   'vendorId',p.vendor_id,'vendorName',p.vendor_name,
   'eligibleApprovalRoles',CASE WHEN p.queue='mir-approval'
     THEN '["STORES_MANAGER","PRODUCTION_MANAGER"]'::jsonb ELSE '[]'::jsonb END,
   'assignedApproverEmployeeId',NULL,
   'responsibilityIssue',CASE WHEN p.queue='mir-approval' THEN 'No named approver is assigned by the current MIR workflow.' END,
   'detailPath',CASE WHEN p.kind='GATE_ENTRY' THEN '/api/v1/stores/gate-entries/' ELSE '/api/v1/stores/material-issue-requests/' END||p.id::text) value
 FROM paged p
)
SELECT jsonb_build_object('allowed',a.allowed,'data',CASE WHEN a.allowed THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,'tiles',coalesce((SELECT jsonb_agg(value ORDER BY key) FROM tiles),'[]'::jsonb),
 'totalRows',(SELECT count(*) FROM selected),
 'rows',coalesce((SELECT jsonb_agg(value ORDER BY waiting_since,queue,id) FROM rows),'[]'::jsonb)) END)
FROM access a;
$storesworkload$;
