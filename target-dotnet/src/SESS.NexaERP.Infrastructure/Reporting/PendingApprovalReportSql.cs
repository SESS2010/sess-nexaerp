namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class PendingApprovalReportSql
{
    internal const string Source = """
        WITH approval_roles AS (
          SELECT DISTINCT r."Code"
          FROM advance.employee_role_assignments assignment
          JOIN effective_roles r ON r."Id"=assignment."RoleId"
          JOIN access a ON a.allowed AND assignment."CompanyId"=a.company_id
          WHERE assignment."EmployeeId"=@employee AND assignment."Id"=ANY(@assignments)
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
            greatest(0,@to_date-(p.waiting_since AT TIME ZONE @report_timezone)::date) AS age_days,
            CASE WHEN p.approver_id IS NOT NULL THEN p.approver_id::text||':'||array_to_string(p.roles,'|')
              WHEN cardinality(p.roles)>0 THEN 'ROLE:'||array_to_string(p.roles,'|')
              ELSE 'UNASSIGNED:'||p.kind END AS approver_key,
            CASE WHEN p.approver_id IS NOT NULL THEN 'NAMED_EMPLOYEE'
              WHEN cardinality(p.roles)>0 THEN 'ROLE_POOL' ELSE 'UNRESOLVED' END AS assignment_kind
          FROM pending_actions p
          LEFT JOIN advance.employees e ON e."Id"=p.approver_id
          WHERE EXISTS(SELECT 1 FROM approval_roles WHERE "Code" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR'))
            OR ((p.approver_id IS NULL OR p.approver_id=@employee)
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
        ) labels
        """;
}
