CREATE FUNCTION advance.tracking_source(p_company uuid,p_report_timezone text)
RETURNS TABLE(queue text,doc_type text,document_id uuid,number text,status text,waiting_since timestamptz,
  pending_role text,pending_employee_id uuid,department_id uuid,warehouse_id uuid,owner_id uuid,involved uuid[],
  not_for uuid,age_days integer,overdue_after_days integer,is_overdue boolean,link text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $trackingsource$
WITH calendar AS (
 SELECT (CURRENT_TIMESTAMP AT TIME ZONE p_report_timezone)::date today
),
effective_grn AS MATERIALIZED (
 SELECT g.*,p."RequestingDepartmentId" department_id,p."DeliveryWarehouseId" warehouse_id
 FROM advance.goods_receipts g
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 WHERE g."CompanyId"=p_company AND g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
   AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal WHERE reversal."CompanyId"=g."CompanyId"
     AND reversal."ReversesGoodsReceiptId"=g."Id" AND reversal."Status"='FINALIZED')
),
raw(queue,doc_type,document_id,number,status,waiting_since,pending_role,pending_employee_id,
    department_id,warehouse_id,owner_id,involved,not_for,link) AS (
 SELECT CASE p."Status" WHEN 'Submitted' THEN 'pr-department-verification'
       WHEN 'StockCheckPending' THEN 'pr-stock-check' ELSE 'pr-approval' END,
   'PR',p."Id",p."PrNumber",p."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_requisition_status_history h
     WHERE h."CompanyId"=p."CompanyId" AND h."PurchaseRequisitionId"=p."Id"
       AND h."NewStatus"=p."Status" AND h."PreviousStatus" IS DISTINCT FROM h."NewStatus"),p."CreatedAt"),
   CASE p."Status" WHEN 'Submitted' THEN verifier.role WHEN 'StockCheckPending' THEN NULL ELSE step.role END,
   CASE p."Status" WHEN 'Submitted' THEN verifier.employee WHEN 'StockCheckPending' THEN NULL ELSE step.employee END,
   p."RequestingDepartmentId",p."DeliveryWarehouseId",p."RequesterEmployeeId",
   ARRAY[p."RequesterEmployeeId",p."CreatorEmployeeId"],NULL::uuid,
   '/purchase/requisitions/'||p."PrNumber"
 FROM advance.purchase_requisitions p CROSS JOIN calendar c
 LEFT JOIN LATERAL (
   SELECT CASE WHEN count(*)=1 THEN min(m."ApproverRoleCode") END role,
     CASE WHEN count(*)=1 THEN (array_agg(m."PrimaryApproverEmployeeId"))[1] END employee
   FROM advance.department_approval_mappings m
   WHERE m."CompanyId"=p."CompanyId" AND m."DepartmentId"=p."RequestingDepartmentId"
     AND m."ApprovalRouteCode"='MANAGER' AND m."IsActive"
     AND m."EffectiveFrom"<=c.today AND(m."EffectiveTo" IS NULL OR m."EffectiveTo">=c.today)
 ) verifier ON true
 LEFT JOIN LATERAL (
   SELECT CASE WHEN count(*)=1 THEN (jsonb_agg(value)->0->>'roleCode') END role,
     CASE WHEN count(*)=1 AND bool_and(value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')
       THEN (jsonb_agg(value)->0->>'employeeId')::uuid END employee
   FROM jsonb_array_elements(CASE WHEN jsonb_typeof(p."ApprovalWorkflowSnapshotJson"::jsonb->'steps')='array'
     THEN p."ApprovalWorkflowSnapshotJson"::jsonb->'steps' ELSE '[]'::jsonb END)
   WHERE value->>'stepNumber'=(p."CompletedApprovalStepCount"+1)::text
 ) step ON true
 WHERE p."CompanyId"=p_company AND p."IsActive"
   AND p."Status" IN('Submitted','DepartmentVerified','PendingApproval','StockCheckPending')
 UNION ALL
 SELECT 'rfq-no-quotation','RFQ',r."Id",r."RfqNumber",r."Status",coalesce(r."IssuedAt",r."CreatedAt"),
   NULL,NULL,r."RequestingDepartmentId",r."DeliveryWarehouseId",r."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/rfqs/'||r."RfqNumber"
 FROM advance.request_for_quotations r
 WHERE r."CompanyId"=p_company AND r."IsActive" AND r."Status"='Issued' AND NOT EXISTS(
   SELECT 1 FROM advance.vendor_quotations q JOIN advance.rfq_vendor_invitations i
     ON i."Id"=q."RfqVendorInvitationId" AND i."CompanyId"=r."CompanyId"
   WHERE q."CompanyId"=r."CompanyId" AND i."RequestForQuotationId"=r."Id"
     AND q."IsCurrentRevision" AND q."Status" NOT IN('Draft','Withdrawn','Rejected','Superseded'))
 UNION ALL
 SELECT 'quotation-technical-verification','QUOTATION',q."Id",q."QuotationNumber",q."Status",
   coalesce(q."SubmittedAt",q."CreatedAt"),NULL,NULL,
   r."RequestingDepartmentId",r."DeliveryWarehouseId",r."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/quotations/'||q."QuotationNumber"
 FROM advance.vendor_quotations q
 JOIN advance.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId" AND i."CompanyId"=q."CompanyId"
 JOIN advance.request_for_quotations r ON r."Id"=i."RequestForQuotationId" AND r."CompanyId"=q."CompanyId" AND r."IsActive"
 WHERE q."CompanyId"=p_company AND q."IsCurrentRevision" AND q."Status"='Submitted'
   AND EXISTS(SELECT 1 FROM advance.vendor_quotation_lines l
     WHERE l."CompanyId"=q."CompanyId" AND l."VendorQuotationId"=q."Id" AND NOT EXISTS(
       SELECT 1 FROM advance.quotation_technical_verifications tv
       WHERE tv."CompanyId"=q."CompanyId" AND tv."VendorQuotationLineId"=l."Id"))
 UNION ALL
 SELECT 'comparison-decision','COMPARISON',x."Id",x."ComparisonNumber",x."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_transaction_status_history h
     WHERE h."CompanyId"=x."CompanyId" AND h."EntityId"=x."Id" AND h."EntityType"='CommercialComparison'
       AND h."ToStatus"=x."Status" AND h."FromStatus" IS DISTINCT FROM h."ToStatus"),x."CreatedAt"),
   CASE WHEN x."Status"='PendingApproval' THEN step.role END,
   CASE WHEN x."Status"='PendingApproval' THEN step.employee ELSE x."OwnerEmployeeId" END,
   r."RequestingDepartmentId",r."DeliveryWarehouseId",x."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/comparisons/'||x."ComparisonNumber"
 FROM advance.commercial_comparisons x
 JOIN advance.request_for_quotations r ON r."Id"=x."RequestForQuotationId" AND r."CompanyId"=x."CompanyId"
 LEFT JOIN LATERAL (
   SELECT CASE WHEN count(*)=1 THEN (jsonb_agg(value)->0->>'roleCode') END role,
     CASE WHEN count(*)=1 AND bool_and(value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')
       THEN (jsonb_agg(value)->0->>'employeeId')::uuid END employee
   FROM jsonb_array_elements(CASE WHEN jsonb_typeof(x."ApprovalWorkflowSnapshotJson"::jsonb->'steps')='array'
     THEN x."ApprovalWorkflowSnapshotJson"::jsonb->'steps' ELSE '[]'::jsonb END)
   WHERE value->>'stepNumber'=(x."CompletedApprovalStepCount"+1)::text
 ) step ON true
 WHERE x."CompanyId"=p_company AND x."Status" IN('Draft','RevisionRequested','PendingApproval')
 UNION ALL
 SELECT 'po-pending-approval','PO',p."Id",p."PoNumber",p."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_transaction_status_history h
     WHERE h."CompanyId"=p."CompanyId" AND h."EntityId"=p."Id" AND h."EntityType"='PurchaseOrder'
       AND h."ToStatus"=p."Status" AND h."FromStatus" IS DISTINCT FROM h."ToStatus"),p."CreatedAt"),
   step.role,step.employee,p."RequestingDepartmentId",p."DeliveryWarehouseId",p."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/purchase-orders/'||p."PoNumber"
 FROM advance.purchase_orders p
 LEFT JOIN LATERAL (
   SELECT CASE WHEN count(*)=1 THEN (jsonb_agg(value)->0->>'roleCode') END role,
     CASE WHEN count(*)=1 AND bool_and(value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')
       THEN (jsonb_agg(value)->0->>'employeeId')::uuid END employee
   FROM jsonb_array_elements(CASE WHEN jsonb_typeof(p."ApprovalWorkflowSnapshotJson"::jsonb->'steps')='array'
     THEN p."ApprovalWorkflowSnapshotJson"::jsonb->'steps' ELSE '[]'::jsonb END)
   WHERE value->>'stepNumber'=(p."CompletedApprovalStepCount"+1)::text
 ) step ON true
 WHERE p."CompanyId"=p_company AND p."IsCurrentVersion" AND p."Status" IN('PendingApproval','Resubmitted')
 UNION ALL
 SELECT 'po-approved-unissued','PO',p."Id",p."PoNumber",p."Status",
   coalesce((SELECT max(h."CreatedAt") FROM advance.purchase_transaction_status_history h
     WHERE h."CompanyId"=p."CompanyId" AND h."EntityId"=p."Id" AND h."EntityType"='PurchaseOrder'
       AND h."ToStatus"='Approved' AND h."FromStatus" IS DISTINCT FROM h."ToStatus"),p."CreatedAt"),
   NULL,NULL,p."RequestingDepartmentId",p."DeliveryWarehouseId",p."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/purchase-orders/'||p."PoNumber"
 FROM advance.purchase_orders p
 WHERE p."CompanyId"=p_company AND p."IsCurrentVersion" AND p."Status"='Approved' AND p."IssuedAt" IS NULL
 UNION ALL
 SELECT 'po-delivery-overdue','PO',po."Id",po."PoNumber",po."Status",
   (late.promised::timestamp AT TIME ZONE p_report_timezone),NULL,po."OwnerEmployeeId",
   po."RequestingDepartmentId",po."DeliveryWarehouseId",po."OwnerEmployeeId",'{}'::uuid[],NULL,
   '/purchase/purchase-orders/'||po."PoNumber"
 FROM (SELECT DISTINCT ON(x."RootPurchaseOrderId") x.* FROM advance.purchase_orders x
   WHERE x."CompanyId"=p_company AND x."IssuedAt" IS NOT NULL
   ORDER BY x."RootPurchaseOrderId",x."RevisionNumber" DESC) po
 CROSS JOIN calendar c
 CROSS JOIN LATERAL (
   SELECT min(ql."PromisedDeliveryDate") promised
   FROM advance.purchase_order_lines l
   JOIN advance.commercial_comparison_lines cl ON cl."Id"=l."CommercialComparisonLineId" AND cl."CompanyId"=po."CompanyId"
   JOIN advance.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId" AND ql."CompanyId"=po."CompanyId"
   JOIN advance.vendor_quotations q ON q."Id"=ql."VendorQuotationId" AND q."CompanyId"=po."CompanyId"
   WHERE l."CompanyId"=po."CompanyId" AND l."PurchaseOrderId"=po."Id"
     AND cl."DeliverySnapshot"=to_char(ql."PromisedDeliveryDate",'YYYY-MM-DD')
     AND po."DeliveryTermsSnapshot"=q."DeliveryTermsSnapshot"
     AND ql."PromisedDeliveryDate"<c.today
     AND l."OrderedQuantity">coalesce((
       SELECT sum(gl."ReceivedQuantity") FROM advance.goods_receipt_lines gl
       JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=po."CompanyId"
       JOIN advance.purchase_order_lines old_line ON old_line."Id"=gl."PurchaseOrderLineId"
         AND old_line."CompanyId"=po."CompanyId" AND old_line."CommercialComparisonLineId"=l."CommercialComparisonLineId"
       JOIN advance.purchase_orders old_po ON old_po."Id"=old_line."PurchaseOrderId"
         AND old_po."CompanyId"=po."CompanyId" AND old_po."RootPurchaseOrderId"=po."RootPurchaseOrderId"
       WHERE gl."CompanyId"=po."CompanyId" AND g."Status"='FINALIZED' AND g."DocumentKind"='NORMAL'
         AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts reversal WHERE reversal."CompanyId"=po."CompanyId"
           AND reversal."ReversesGoodsReceiptId"=g."Id" AND reversal."Status"='FINALIZED')),0)
 ) late
 WHERE po."Status"<>'Cancelled' AND late.promised IS NOT NULL
 UNION ALL
 SELECT 'gate-no-grn','GATE_ENTRY',g."Id",g."GateEntryNumber",g."Status",g."ArrivedAt",NULL,NULL,
   p."RequestingDepartmentId",p."DeliveryWarehouseId",g."ReceivedByEmployeeId",'{}'::uuid[],NULL,
   '/stores/gate-entries/'||g."Id"::text
 FROM advance.gate_entries g
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 WHERE g."CompanyId"=p_company AND g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
   AND NOT EXISTS(SELECT 1 FROM advance.gate_entries reversal WHERE reversal."CompanyId"=g."CompanyId"
     AND reversal."ReversesGateEntryId"=g."Id" AND reversal."Status"='FINALIZED')
   AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts grn WHERE grn."CompanyId"=g."CompanyId"
     AND grn."GateEntryId"=g."Id" AND grn."DocumentKind"='NORMAL')
 UNION ALL
 SELECT 'grn-not-finalised','GRN',g."Id",g."GrnNumber",g."Status",g."CreatedAt",NULL,NULL,
   p."RequestingDepartmentId",p."DeliveryWarehouseId",g."ReceivedByEmployeeId",'{}'::uuid[],NULL,
   '/stores/grns/'||g."Id"::text
 FROM advance.goods_receipts g
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 WHERE g."CompanyId"=p_company AND g."Status"='DRAFT'
 UNION ALL
 SELECT 'qc-pending','QC',g."Id",g."GrnNumber",'QC_PENDING',g."ReceivedAt",NULL,NULL,
   g.department_id,g.warehouse_id,g."ReceivedByEmployeeId",'{}'::uuid[],NULL,
   '/stores/qc?grn='||g."GrnNumber"
 FROM effective_grn g
 WHERE EXISTS(SELECT 1 FROM advance.goods_receipt_lines gl
   JOIN advance.goods_receipt_line_lot_allocations a ON a."GoodsReceiptLineId"=gl."Id" AND a."CompanyId"=gl."CompanyId"
   WHERE gl."CompanyId"=g."CompanyId" AND gl."GoodsReceiptId"=g."Id" AND(
     NOT EXISTS(SELECT 1 FROM advance.qc_inspections i WHERE i."CompanyId"=a."CompanyId" AND i."GoodsReceiptLineLotAllocationId"=a."Id")
     OR EXISTS(SELECT 1 FROM advance.qc_inspections i
       JOIN advance.qc_inspection_revisions rv ON rv."QcInspectionId"=i."Id" AND rv."CompanyId"=i."CompanyId"
       WHERE i."CompanyId"=a."CompanyId" AND i."GoodsReceiptLineLotAllocationId"=a."Id" AND rv."DiscrepancyPendingQuantity">0
         AND NOT EXISTS(SELECT 1 FROM advance.qc_inspection_revisions nx
           WHERE nx."CompanyId"=rv."CompanyId" AND nx."RevisesRevisionId"=rv."Id"))))
 UNION ALL
 SELECT CASE WHEN m."Status"='SUBMITTED' THEN 'mir-approval' ELSE 'mir-unissued' END,
   'MIR',m."Id",m."RequestNumber",m."Status",
   CASE WHEN m."Status"='SUBMITTED' THEN coalesce(
     (SELECT max(h."OccurredAt") FROM advance.material_issue_history h WHERE h."CompanyId"=m."CompanyId"
       AND h."MaterialIssueRequestId"=m."Id" AND h."Action"='SUBMIT'),m."CreatedAt")
     ELSE coalesce(m."ApprovedAt",m."CreatedAt") END,
   NULL,NULL,m."RequestingDepartmentId",NULL::uuid,m."RequestedByEmployeeId",'{}'::uuid[],NULL,
   '/stores/material-issue-requests/'||m."Id"::text
 FROM advance.material_issue_requests m
 WHERE m."CompanyId"=p_company AND m."Status" IN('SUBMITTED','APPROVED','PARTIALLY_FULFILLED')
   AND EXISTS(SELECT 1 FROM advance.material_issue_request_lines l
     WHERE l."CompanyId"=m."CompanyId" AND l."MaterialIssueRequestId"=m."Id"
       AND(m."Status"='SUBMITTED' OR l."RequestedBaseQuantity">coalesce(
         (SELECT sum(i."QuantityBase") FROM advance.material_issue_lines i WHERE i."CompanyId"=m."CompanyId"
           AND i."MaterialIssueRequestLineId"=l."Id"),0)))
 UNION ALL
 SELECT 'bill-awaiting-decision','VENDOR_BILL',b."Id",b."BillNumber",b."Status",b."CreatedAt",NULL,NULL,
   p."RequestingDepartmentId",p."DeliveryWarehouseId",b."CreatedByEmployeeId",'{}'::uuid[],b."CreatedByEmployeeId",
   '/accounts/vendor-bills/'||b."Id"::text
 FROM advance.vendor_bills b
 JOIN advance.purchase_orders p ON p."Id"=b."PurchaseOrderId" AND p."CompanyId"=b."CompanyId"
 WHERE b."CompanyId"=p_company AND b."Status"='DRAFT'
 UNION ALL
 SELECT 'grn-without-bill','GRN',g."Id",g."GrnNumber",g."Status",g."ReceivedAt",NULL,NULL,
   g.department_id,g.warehouse_id,g."ReceivedByEmployeeId",'{}'::uuid[],NULL,
   '/stores/grns/'||g."Id"::text
 FROM effective_grn g
 WHERE NOT EXISTS(SELECT 1 FROM advance.vendor_bills b WHERE b."CompanyId"=g."CompanyId"
     AND b."GoodsReceiptId"=g."Id" AND b."Status"='DRAFT')
   AND EXISTS(SELECT 1 FROM advance.goods_receipt_lines l WHERE l."CompanyId"=g."CompanyId" AND l."GoodsReceiptId"=g."Id"
     AND l."ReceivedQuantity">coalesce((SELECT sum(bl."BilledQuantity") FROM advance.vendor_bill_lines bl
       JOIN advance.vendor_bills b ON b."Id"=bl."VendorBillId" AND b."CompanyId"=bl."CompanyId"
       WHERE bl."CompanyId"=l."CompanyId" AND bl."GoodsReceiptLineId"=l."Id" AND b."Status"='ACCEPTED'),0))
)
SELECT r.queue,r.doc_type,r.document_id,r.number,r.status,r.waiting_since,
  coalesce(r.pending_role,q."PendingWithRole"),r.pending_employee_id,r.department_id,r.warehouse_id,r.owner_id,
  array_remove(r.involved||r.pending_employee_id,NULL),r.not_for,a.age,q."OverdueAfterDays",a.age>q."OverdueAfterDays",r.link
FROM raw r
JOIN advance.tracking_queues q ON q."Queue"=r.queue
CROSS JOIN calendar c
CROSS JOIN LATERAL(SELECT greatest(0,c.today-(r.waiting_since AT TIME ZONE p_report_timezone)::date)::integer age) a;
$trackingsource$;

CREATE FUNCTION advance.tracking_pending(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $trackingpending$
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
 SELECT p."PageKey",bool_or(rp."CanView" OR rp."HasFullControl") can_view
 FROM advance.page_definitions p
 JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
 JOIN effective_roles r ON r."Id"=rp."RoleId"
 WHERE p."IsActive" GROUP BY p."PageKey"
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
   AND(coalesce((SELECT can_view FROM role_grants WHERE "PageKey"='tracking.pending'),false)
     OR coalesce((SELECT can_view FROM employee_grants WHERE "PageKey"='tracking.pending'),false)) allowed,
   EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
     AND EXISTS(SELECT 1 FROM scopes WHERE "AllowsPrivilegedCrossScope") privileged
),
queues AS MATERIALIZED (
 SELECT q.*,a.allowed AND(coalesce(r.can_view,false) OR coalesce(e.can_view,false)) allowed
 FROM advance.tracking_queues q CROSS JOIN access a
 LEFT JOIN role_grants r ON r."PageKey"=q."PageKey"
 LEFT JOIN employee_grants e ON e."PageKey"=q."PageKey"
),
visible AS MATERIALIZED (
 SELECT s.* FROM access a
 CROSS JOIN LATERAL advance.tracking_source(a.company_id,p_report_timezone) s
 JOIN queues q ON q."Queue"=s.queue AND q.allowed
 WHERE a.allowed AND(q."ScopeRule"='COMPANY' OR a.privileged OR p_employee=ANY(s.involved) OR EXISTS(SELECT 1 FROM scopes scope WHERE
   (scope."DepartmentId" IS NULL OR scope."DepartmentId"=s.department_id)
   AND(scope."WarehouseId" IS NULL OR scope."WarehouseId"=s.warehouse_id)
   AND scope."RackBinId" IS NULL
   AND(NOT scope."OwnRecordsOnly" OR s.owner_id=p_employee)))
)
SELECT jsonb_build_object('allowed',a.allowed,'data',CASE WHEN a.allowed THEN jsonb_build_object(
 'generatedAt',CURRENT_TIMESTAMP,
 'roleCodes',coalesce((SELECT jsonb_agg("Code" ORDER BY "Code") FROM effective_roles),'[]'::jsonb),
 'queues',(SELECT jsonb_agg(jsonb_build_object('queue',q."Queue",'docType',q."DocType",'title',q."Title",
   'state',CASE WHEN q.allowed THEN 'READY' ELSE 'ACCESS_DENIED' END,'overdueAfterDays',q."OverdueAfterDays")
   ORDER BY q."SortOrder") FROM queues q),
 'rows',coalesce((SELECT jsonb_agg(jsonb_build_object('docType',v.doc_type,'queue',v.queue,'documentId',v.document_id,
   'number',v.number,'status',v.status,'pendingWithRole',v.pending_role,'pendingWithRoleName',r."Name",
   'pendingWithEmployeeId',v.pending_employee_id,'pendingWithEmployeeCode',e."EmployeeCode",
   'pendingWithEmployeeName',e."EmployeeName",'notForEmployeeId',v.not_for,'waitingSince',v.waiting_since,
   'ageDays',v.age_days,'overdueAfterDays',v.overdue_after_days,'isOverdue',v.is_overdue,'link',v.link)
   ORDER BY v.is_overdue DESC,v.waiting_since,v.queue,v.document_id)
   FROM visible v LEFT JOIN advance.roles r ON r."Code"=v.pending_role
   LEFT JOIN advance.employees e ON e."Id"=v.pending_employee_id),'[]'::jsonb)) END)
FROM access a;
$trackingpending$;

CREATE FUNCTION advance.tracking_history(p_organization text,p_employee uuid,p_assignments uuid[],p_report_timezone text,p_doc_type text,p_document uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $trackinghistory$
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
 SELECT p."PageKey",bool_or(rp."CanView" OR rp."HasFullControl") can_view
 FROM advance.page_definitions p
 JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
 JOIN effective_roles r ON r."Id"=rp."RoleId"
 WHERE p."IsActive" GROUP BY p."PageKey"
),
employee_grants AS MATERIALIZED (
 SELECT p."PageKey",bool_or(ep."CanView") can_view
 FROM advance.employee_page_permissions ep JOIN company c ON c."Id"=ep."CompanyId"
 JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId" AND p."IsActive"
 WHERE ep."EmployeeId"=p_employee GROUP BY p."PageKey"
),
granted AS MATERIALIZED (
 SELECT "PageKey" FROM role_grants WHERE can_view UNION SELECT "PageKey" FROM employee_grants WHERE can_view
),
access AS MATERIALIZED (
 SELECT (SELECT "Id" FROM company) company_id,
   EXISTS(SELECT 1 FROM company) AND EXISTS(SELECT 1 FROM scopes)
   AND EXISTS(SELECT 1 FROM granted WHERE "PageKey"='tracking.pending') allowed,
   EXISTS(SELECT 1 FROM effective_roles WHERE "Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
     AND EXISTS(SELECT 1 FROM scopes WHERE "AllowsPrivilegedCrossScope") privileged
),
doc(page_key,company_wide,number,status,department_id,warehouse_id,owner_id,involved) AS MATERIALIZED (
 SELECT 'purchase.requisitions',false,p."PrNumber",p."Status",p."RequestingDepartmentId",p."DeliveryWarehouseId",p."RequesterEmployeeId",
   ARRAY[p."RequesterEmployeeId",p."CreatorEmployeeId"]||ARRAY(SELECT (value->>'employeeId')::uuid
     FROM jsonb_array_elements(CASE WHEN jsonb_typeof(p."ApprovalWorkflowSnapshotJson"::jsonb->'steps')='array'
       THEN p."ApprovalWorkflowSnapshotJson"::jsonb->'steps' ELSE '[]'::jsonb END)
     WHERE value->>'employeeId' ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')
 FROM advance.purchase_requisitions p JOIN access a ON p."CompanyId"=a.company_id
 WHERE p_doc_type='PR' AND p."Id"=p_document AND p."IsActive"
 UNION ALL
 SELECT 'purchase.rfq',false,r."RfqNumber",r."Status",r."RequestingDepartmentId",r."DeliveryWarehouseId",r."OwnerEmployeeId",'{}'::uuid[]
 FROM advance.request_for_quotations r JOIN access a ON r."CompanyId"=a.company_id
 WHERE p_doc_type='RFQ' AND r."Id"=p_document AND r."IsActive"
 UNION ALL
 SELECT 'purchase.vendor-quotations',false,q."QuotationNumber",q."Status",r."RequestingDepartmentId",r."DeliveryWarehouseId",r."OwnerEmployeeId",'{}'::uuid[]
 FROM advance.vendor_quotations q JOIN access a ON q."CompanyId"=a.company_id
 JOIN advance.rfq_vendor_invitations i ON i."Id"=q."RfqVendorInvitationId" AND i."CompanyId"=q."CompanyId"
 JOIN advance.request_for_quotations r ON r."Id"=i."RequestForQuotationId" AND r."CompanyId"=q."CompanyId"
 WHERE p_doc_type='QUOTATION' AND q."Id"=p_document
 UNION ALL
 SELECT 'purchase.commercial-comparisons',false,x."ComparisonNumber",x."Status",r."RequestingDepartmentId",r."DeliveryWarehouseId",x."OwnerEmployeeId",'{}'::uuid[]
 FROM advance.commercial_comparisons x JOIN access a ON x."CompanyId"=a.company_id
 JOIN advance.request_for_quotations r ON r."Id"=x."RequestForQuotationId" AND r."CompanyId"=x."CompanyId"
 WHERE p_doc_type='COMPARISON' AND x."Id"=p_document
 UNION ALL
 SELECT 'purchase.po',false,p."PoNumber",p."Status",p."RequestingDepartmentId",p."DeliveryWarehouseId",p."OwnerEmployeeId",'{}'::uuid[]
 FROM advance.purchase_orders p JOIN access a ON p."CompanyId"=a.company_id
 WHERE p_doc_type='PO' AND p."Id"=p_document
 UNION ALL
 SELECT 'inventory.grn',false,g."GateEntryNumber",g."Status",p."RequestingDepartmentId",p."DeliveryWarehouseId",g."ReceivedByEmployeeId",'{}'::uuid[]
 FROM advance.gate_entries g JOIN access a ON g."CompanyId"=a.company_id
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 WHERE p_doc_type='GATE_ENTRY' AND g."Id"=p_document
 UNION ALL
 SELECT page.key,page.company_wide,
   g."GrnNumber",g."Status",p."RequestingDepartmentId",p."DeliveryWarehouseId",g."ReceivedByEmployeeId",'{}'::uuid[]
 FROM advance.goods_receipts g JOIN access a ON g."CompanyId"=a.company_id
 JOIN advance.purchase_orders p ON p."Id"=g."PurchaseOrderId" AND p."CompanyId"=g."CompanyId"
 JOIN(VALUES('GRN','inventory.grn',false),('GRN','accounts.vendor-bills',true),('QC','qc.inspection-policies',true)) page(doc_type,key,company_wide)
   ON page.doc_type=p_doc_type
 WHERE p_doc_type IN('GRN','QC') AND g."Id"=p_document
 UNION ALL
 SELECT 'stores.material-issue-requests',false,m."RequestNumber",m."Status",m."RequestingDepartmentId",NULL::uuid,m."RequestedByEmployeeId",'{}'::uuid[]
 FROM advance.material_issue_requests m JOIN access a ON m."CompanyId"=a.company_id
 WHERE p_doc_type='MIR' AND m."Id"=p_document
 UNION ALL
 SELECT 'accounts.vendor-bills',true,b."BillNumber",b."Status",p."RequestingDepartmentId",p."DeliveryWarehouseId",b."CreatedByEmployeeId",'{}'::uuid[]
 FROM advance.vendor_bills b JOIN access a ON b."CompanyId"=a.company_id
 JOIN advance.purchase_orders p ON p."Id"=b."PurchaseOrderId" AND p."CompanyId"=b."CompanyId"
 WHERE p_doc_type='VENDOR_BILL' AND b."Id"=p_document
),
visible_doc AS MATERIALIZED (
 SELECT d.* FROM doc d CROSS JOIN access a
 WHERE a.allowed AND EXISTS(SELECT 1 FROM granted g WHERE g."PageKey"=d.page_key)
   AND(d.company_wide OR a.privileged OR p_employee=ANY(d.involved) OR EXISTS(SELECT 1 FROM scopes scope WHERE
     (scope."DepartmentId" IS NULL OR scope."DepartmentId"=d.department_id)
     AND(scope."WarehouseId" IS NULL OR scope."WarehouseId"=d.warehouse_id)
     AND scope."RackBinId" IS NULL
     AND(NOT scope."OwnRecordsOnly" OR d.owner_id=p_employee)))
 ORDER BY d.company_wide LIMIT 1
),
purchase_chain(entity_id) AS MATERIALIZED (
 WITH roots AS (
   SELECT CASE p_doc_type
       WHEN 'RFQ' THEN p_document
       WHEN 'COMPARISON' THEN (SELECT x."RequestForQuotationId" FROM advance.commercial_comparisons x WHERE x."Id"=p_document)
       WHEN 'PO' THEN (SELECT x."RequestForQuotationId" FROM advance.commercial_comparisons x
         JOIN advance.purchase_orders p ON p."CommercialComparisonId"=x."Id" WHERE p."Id"=p_document) END rfq_id,
     CASE p_doc_type
       WHEN 'COMPARISON' THEN p_document
       WHEN 'PO' THEN (SELECT p."CommercialComparisonId" FROM advance.purchase_orders p WHERE p."Id"=p_document) END comparison_id
   FROM visible_doc WHERE p_doc_type IN('RFQ','QUOTATION','COMPARISON','PO')
 ),
 quotations AS (
   SELECT q."Id" FROM roots r JOIN advance.rfq_vendor_invitations i ON i."RequestForQuotationId"=r.rfq_id
   JOIN advance.vendor_quotations q ON q."RfqVendorInvitationId"=i."Id"
   UNION SELECT p_document FROM visible_doc WHERE p_doc_type='QUOTATION'
 )
 SELECT rfq_id FROM roots WHERE rfq_id IS NOT NULL
 UNION SELECT i."Id" FROM roots r JOIN advance.rfq_vendor_invitations i ON i."RequestForQuotationId"=r.rfq_id
 UNION SELECT "Id" FROM quotations
 UNION SELECT tv."Id" FROM quotations q JOIN advance.vendor_quotation_lines l ON l."VendorQuotationId"=q."Id"
   JOIN advance.quotation_technical_verifications tv ON tv."VendorQuotationLineId"=l."Id"
 UNION SELECT comparison_id FROM roots WHERE comparison_id IS NOT NULL
 UNION SELECT p."Id" FROM advance.purchase_orders p
   WHERE p_doc_type='PO' AND p."RootPurchaseOrderId"=(SELECT x."RootPurchaseOrderId" FROM advance.purchase_orders x WHERE x."Id"=p_document)
),
events(at,stage,action,from_status,to_status,employee_id,login_id,role_code,remarks) AS MATERIALIZED (
 SELECT h."CreatedAt",'PR',CASE WHEN h."PreviousStatus" IS NULL OR h."PreviousStatus"='' THEN 'Create' ELSE 'StatusChange' END,
   nullif(h."PreviousStatus",''),h."NewStatus",NULL::uuid,h."ActorLoginId",h."ActorRoleCode",nullif(h."Reason",'')
 FROM advance.purchase_requisition_status_history h JOIN visible_doc d ON p_doc_type='PR'
 WHERE h."PurchaseRequisitionId"=p_document AND NOT EXISTS(SELECT 1 FROM advance.purchase_requisition_approval_history x
   WHERE x."PurchaseRequisitionId"=h."PurchaseRequisitionId" AND x."CorrelationId"=h."CorrelationId")
 UNION ALL
 SELECT h."CreatedAt",'PR',h."Action",h."FromStatus",h."ToStatus",h."ResolvedEmployeeId",h."ActorLoginId",h."ActorRoleCode",nullif(h."Remarks",'')
 FROM advance.purchase_requisition_approval_history h JOIN visible_doc d ON p_doc_type='PR'
 WHERE h."PurchaseRequisitionId"=p_document
 UNION ALL
 SELECT h."CreatedAt",CASE WHEN h."EntityType" IN('RFQ','RFQInvitation') THEN 'RFQ'
     WHEN h."EntityType" IN('VendorQuotation','TechnicalVerification') THEN 'QUOTATION'
     WHEN h."EntityType"='CommercialComparison' THEN 'COMPARISON' WHEN h."EntityType"='PurchaseOrder' THEN 'PO' ELSE upper(h."EntityType") END,
   h."Action",h."FromStatus",h."ToStatus",h."ActorEmployeeId",h."ActorLoginId",h."ActorRoleCode",nullif(h."Remarks",'')
 FROM advance.purchase_transaction_status_history h JOIN purchase_chain c ON c.entity_id=h."EntityId"
 CROSS JOIN access a
 WHERE h."CompanyId"=a.company_id AND h."Action"<>'ReserveAmendment'
 UNION ALL
 SELECT h."OccurredAt",CASE WHEN h."GateEntryId" IS NOT NULL THEN 'GATE_ENTRY' ELSE 'GRN' END,h."Action",h."FromStatus",h."ToStatus",
   h."ActorEmployeeId",NULL,h."ActorRoleCode",nullif(h."Reason",'')
 FROM advance.stores_document_status_history h JOIN visible_doc d ON p_doc_type IN('GATE_ENTRY','GRN','QC')
 CROSS JOIN access a
 WHERE h."CompanyId"=a.company_id AND(
   (p_doc_type='GATE_ENTRY' AND h."GateEntryId"=p_document)
   OR(p_doc_type IN('GRN','QC') AND(h."GoodsReceiptId"=p_document
     OR h."GateEntryId"=(SELECT g."GateEntryId" FROM advance.goods_receipts g WHERE g."Id"=p_document))))
 UNION ALL
 SELECT x.at,'QC',x.action,NULL,x.to_status,x.employee_id,NULL,NULL,x.remarks
 FROM visible_doc d
 JOIN advance.goods_receipt_lines gl ON gl."GoodsReceiptId"=p_document AND p_doc_type IN('GRN','QC')
 JOIN advance.goods_receipt_line_lot_allocations al ON al."GoodsReceiptLineId"=gl."Id"
 JOIN advance.qc_inspections i ON i."GoodsReceiptLineLotAllocationId"=al."Id"
 JOIN advance.qc_inspection_revisions rv ON rv."QcInspectionId"=i."Id"
 CROSS JOIN LATERAL(VALUES
   (rv."InspectionStartedAt",'QC_STARTED',NULL::text,rv."InspectorEmployeeId",i."InspectionNumber"||' line '||gl."LineNumber"::text),
   (rv."FinalizedAt",'QC_FINALIZED',rv."Decision",rv."FinalizedByEmployeeId",i."InspectionNumber"||' line '||gl."LineNumber"::text)
 ) x(at,action,to_status,employee_id,remarks)
 WHERE x.at IS NOT NULL
 UNION ALL
 SELECT h."OccurredAt",'MIR',h."Action",h."FromStatus",h."ToStatus",h."ActorEmployeeId",NULL,h."ActorRoleCode",nullif(h."Remarks",'')
 FROM advance.material_issue_history h JOIN visible_doc d ON p_doc_type='MIR'
 WHERE h."MaterialIssueRequestId"=p_document
 UNION ALL
 SELECT h."OccurredAt",'VENDOR_BILL',h."Action",h."FromStatus",h."ToStatus",h."ActorEmployeeId",NULL,h."ActorRoleCode",nullif(h."Remarks",'')
 FROM advance.vendor_bill_history h JOIN visible_doc d ON p_doc_type='VENDOR_BILL'
 WHERE h."VendorBillId"=p_document
),
pending AS (
 SELECT s.* FROM access a CROSS JOIN LATERAL advance.tracking_source(a.company_id,p_report_timezone) s
 WHERE EXISTS(SELECT 1 FROM visible_doc) AND s.document_id=p_document AND s.doc_type=p_doc_type
 ORDER BY s.waiting_since LIMIT 1
)
SELECT jsonb_build_object('allowed',a.allowed,'found',EXISTS(SELECT 1 FROM visible_doc),'data',
 (SELECT jsonb_build_object('docType',p_doc_type,'documentId',p_document,'number',d.number,'currentStatus',d.status,
   'pendingWithRole',(SELECT pending_role FROM pending),'waitingSince',(SELECT waiting_since FROM pending),
   'ageDays',(SELECT age_days FROM pending),'isOverdue',(SELECT is_overdue FROM pending),
   'events',coalesce((SELECT jsonb_agg(jsonb_build_object('at',e.at,'stage',e.stage,'action',e.action,
     'fromStatus',e.from_status,'toStatus',e.to_status,'employeeCode',emp."EmployeeCode",'employeeName',emp."EmployeeName",
     'loginId',e.login_id,'roleCode',e.role_code,'remarks',e.remarks) ORDER BY e.at DESC,e.stage,e.action)
     FROM events e LEFT JOIN advance.employees emp ON emp."Id"=e.employee_id),'[]'::jsonb))
  FROM visible_doc d))
FROM access a;
$trackinghistory$;