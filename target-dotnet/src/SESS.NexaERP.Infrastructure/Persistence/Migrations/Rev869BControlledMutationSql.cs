namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Rev869BControlledMutationSql
{
    public const string Install = """
        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_history_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE matches bigint; parent_version bigint; parent_login text; parent_org text; parent_number text; parent_status text;
        BEGIN
          IF TG_TABLE_NAME='purchase_transaction_status_history' THEN
            SELECT count(*),min(p.version),min(p.login),min(p.org),min(p.number),min(p.status)
              INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status FROM (
              SELECT r."Version" version,coalesce(r."UpdatedBy",r."CreatedBy") login,r."OrganizationId" org,r."RfqNumber" number,r."Status" status FROM nexa.request_for_quotations r WHERE NEW."EntityType"='RFQ' AND r."Id"=NEW."EntityId" AND r.xmin::text::bigint=txid_current()
              UNION ALL SELECT i."Version",coalesce(i."UpdatedBy",i."CreatedBy"),r."OrganizationId",r."RfqNumber",i."Status" FROM nexa.rfq_vendor_invitations i JOIN nexa.request_for_quotations r ON r."Id"=i."RequestForQuotationId" WHERE NEW."EntityType"='RFQInvitation' AND i."Id"=NEW."EntityId" AND i.xmin::text::bigint=txid_current()
              UNION ALL SELECT q."Version",coalesce(q."UpdatedBy",q."CreatedBy"),q."OrganizationId",q."QuotationNumber",q."Status" FROM nexa.vendor_quotations q WHERE NEW."EntityType"='VendorQuotation' AND q."Id"=NEW."EntityId" AND q.xmin::text::bigint=txid_current()
              UNION ALL SELECT v."Version",coalesce(v."UpdatedBy",v."CreatedBy"),q."OrganizationId",q."QuotationNumber",v."ComplianceStatus" FROM nexa.quotation_technical_verifications v JOIN nexa.vendor_quotation_lines l ON l."Id"=v."VendorQuotationLineId" JOIN nexa.vendor_quotations q ON q."Id"=l."VendorQuotationId" WHERE NEW."EntityType"='TechnicalVerification' AND v."Id"=NEW."EntityId" AND v.xmin::text::bigint=txid_current()
              UNION ALL SELECT c."Version",coalesce(c."UpdatedBy",c."CreatedBy"),c."OrganizationId",c."ComparisonNumber",c."Status" FROM nexa.commercial_comparisons c WHERE NEW."EntityType"='CommercialComparison' AND c."Id"=NEW."EntityId" AND c.xmin::text::bigint=txid_current()
              UNION ALL SELECT p."Version",coalesce(p."UpdatedBy",p."CreatedBy"),p."OrganizationId",p."PoNumber",p."Status" FROM nexa.purchase_orders p WHERE NEW."EntityType"='PurchaseOrder' AND p."Id"=NEW."EntityId" AND p.xmin::text::bigint=txid_current()
              UNION ALL SELECT h."Version",coalesce(h."UpdatedBy",h."CreatedBy"),p."OrganizationId",h."HandoffNumber",h."Status" FROM nexa.material_followup_handoffs h JOIN nexa.purchase_orders p ON p."Id"=h."PurchaseOrderId" WHERE NEW."EntityType"='MaterialFollowUp' AND h."Id"=NEW."EntityId" AND h.xmin::text::bigint=txid_current()
            ) p;
            IF matches<>1 OR NEW."OrganizationId"<>parent_org OR NEW."DocumentNumber"<>parent_number OR NEW."ToStatus"<>parent_status THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_history_parent_transition',MESSAGE='Status history requires the exact parent mutation in the current transaction.';
            END IF;
          ELSIF TG_TABLE_NAME='purchase_transaction_approval_history' THEN
            SELECT count(*),min(c."Version"),min(coalesce(c."UpdatedBy",c."CreatedBy")),min(c."OrganizationId"),min(c."ComparisonNumber"),min(c."Status") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status
              FROM nexa.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId" AND c.xmin::text::bigint=txid_current();
            IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."ApprovalRoute"<>(SELECT c."ApprovalRoute" FROM nexa.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_approval_history_parent_transition',MESSAGE='Approval history requires the exact comparison transition in the current transaction.';
            END IF;
          ELSE
            SELECT count(*),min(p."Version"),min(coalesce(p."UpdatedBy",p."CreatedBy")),min(p."OrganizationId"),min(p."PoNumber"),min(p."Status") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status
              FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId" AND p.xmin::text::bigint=txid_current();
            IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."RevisionNumber"<>(SELECT p."RevisionNumber" FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_history_parent_transition',MESSAGE='PO history requires the exact purchase-order transition in the current transaction.';
            END IF;
          END IF;
          IF NEW."ActorLoginId" IS DISTINCT FROM parent_login OR NEW."CreatedBy" IS DISTINCT FROM parent_login OR length(trim(coalesce(NEW."CorrelationId",'')))=0 OR
             length(trim(coalesce(CASE WHEN TG_TABLE_NAME='purchase_order_history' THEN NEW."Reason" ELSE NEW."Remarks" END,'')))=0 OR
             NOT EXISTS (SELECT 1 FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId" WHERE m."Subject"=NEW."ActorLoginId" AND m."EmployeeId"=NEW."ActorEmployeeId" AND m."OrganizationId"=parent_org AND m."IsActive" AND m."EffectiveFrom"<=statement_timestamp()::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date) AND e."Status"='Active' AND e."LoginEnabled") OR
             NOT EXISTS (SELECT 1 FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId" WHERE a."EmployeeId"=NEW."ActorEmployeeId" AND r."Code"=NEW."ActorRoleCode" AND r."IsActive" AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date)) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_actor_binding',MESSAGE='History actor, role, correlation and remarks must match the current controlled transition.';
          END IF;
          IF NOT (
            (TG_TABLE_NAME='purchase_transaction_status_history' AND (
              (NEW."EntityType" IN ('RFQ','RFQInvitation') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
              (NEW."EntityType"='VendorQuotation' AND NEW."Action" IN ('Verify','RejectTechnical') AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
              (NEW."EntityType"='VendorQuotation' AND NEW."Action" NOT IN ('Verify','RejectTechnical') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
              (NEW."EntityType"='TechnicalVerification' AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
              (NEW."EntityType"='CommercialComparison' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
              (NEW."EntityType"='PurchaseOrder' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
              (NEW."EntityType"='MaterialFollowUp' AND NEW."ActorRoleCode" IN ('STORES_EXECUTIVE','STORES_MANAGER')) OR
              (NEW."EntityType" IN ('CommercialComparison','PurchaseOrder') AND NEW."Action" IN ('Approve','Reject','RequestRevision') AND
                ((NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER') AND EXISTS (SELECT 1 FROM nexa.purchase_transaction_approval_policies p WHERE p."OrganizationId"=parent_org AND p."RouteCode"='MANAGER' AND p."IsActive")) OR
                 NEW."ActorRoleCode" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')))
            )) OR
            (TG_TABLE_NAME='purchase_transaction_approval_history' AND
              ((NEW."ApprovalRoute"='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
               (NEW."ApprovalRoute"='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
               (NEW."ApprovalRoute"='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
            (TG_TABLE_NAME='purchase_order_history' AND
              ((NEW."Action" IN ('Approve','Reject','RequestRevision') AND
                (((SELECT p."ApprovalRoute" FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
                 ((SELECT p."ApprovalRoute" FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
                 ((SELECT p."ApprovalRoute" FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
               (NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER')))
          ) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_action_role',MESSAGE='History role is not authorized for the exact controlled action and approval route.';
          END IF;
          NEW."Version":=parent_version; NEW."CreatedAt":=statement_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
          RETURN NEW;
        END $rev869b$;

        ALTER TABLE nexa.material_followup_handoffs DROP CONSTRAINT "CK_material_followup_quantity";
        ALTER TABLE nexa.material_followup_handoffs ADD CONSTRAINT "CK_material_followup_quantity"
          CHECK ("OrderedQuantitySnapshot">0 AND "Status" IN ('PendingFollowUp','InProgress','Completed'));
        DROP TRIGGER IF EXISTS trg_rev869b_followup_immutable ON nexa.material_followup_handoffs;

        CREATE OR REPLACE FUNCTION nexa.rev869b_reject_controlled_delete()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        BEGIN
          RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_controlled_delete_guard',
            MESSAGE=format('REV869B controlled relation % rejects destructive DELETE.',TG_TABLE_NAME);
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_guard_explicit_mutation()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE parent_status text;
        BEGIN
          IF TG_OP='INSERT' THEN
            IF NEW."Version"<>0 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_version_zero',MESSAGE=format('%s INSERT requires version zero.',TG_TABLE_NAME); END IF;
            IF TG_TABLE_NAME='purchase_transaction_approval_policies' THEN
              IF length(trim(coalesce(NEW."CreatedBy",'')))=0 OR NEW."EffectiveTo" IS NOT NULL AND NEW."EffectiveTo"<NEW."EffectiveFrom" THEN
                RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_policy_insert_contract',MESSAGE='Approval-policy INSERT requires an actor and valid effective dates.';
              END IF;
              NEW."CreatedAt":=statement_timestamp();
            END IF;
            RETURN NEW;
          END IF;
          IF NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_exact_version_increment',MESSAGE=format('%s UPDATE requires exact version +1.',TG_TABLE_NAME); END IF;
          IF TG_TABLE_NAME='request_for_quotations' AND
             to_jsonb(NEW)-ARRAY['Status','IssuedAt','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_rfq_update_allowlist',MESSAGE='RFQ update altered a field outside its exact lifecycle allowlist.';
          ELSIF TG_TABLE_NAME='rfq_vendor_invitations' AND
             to_jsonb(NEW)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_invitation_update_allowlist',MESSAGE='RFQ invitation update altered qualification, provenance or another protected field.';
          ELSIF TG_TABLE_NAME='vendor_quotations' AND
             to_jsonb(NEW)-ARRAY['Status','IsCurrentRevision','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentRevision','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_quotation_update_allowlist',MESSAGE='Quotation update altered immutable commercial, revision or provenance facts.';
          ELSIF TG_TABLE_NAME='commercial_comparisons' AND NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested') AND
             to_jsonb(NEW)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_recommendation_allowlist',MESSAGE='Comparison recommendation altered a field outside the Draft/RevisionRequested correction boundary.';
          ELSIF TG_TABLE_NAME='commercial_comparisons' AND NOT (NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested')) AND
             to_jsonb(NEW)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_allowlist',MESSAGE='Comparison transition altered protected commercial, selection or provenance fields.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Issued' AND
             to_jsonb(NEW)-ARRAY['Status','IssuedAt','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_issue_allowlist',MESSAGE='PO issue altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Cancelled' AND
             to_jsonb(NEW)-ARRAY['Status','CancelledAt','CancellationReason','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','CancelledAt','CancellationReason','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_cancel_allowlist',MESSAGE='PO cancellation altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" IN ('Approved','Rejected','Superseded') AND
             to_jsonb(NEW)-ARRAY['Status','IsCurrentVersion','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentVersion','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_approval_allowlist',MESSAGE='PO approval/rejection/supersession altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" NOT IN ('Issued','Cancelled','Approved','Rejected','Superseded') AND
             to_jsonb(NEW)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_allowlist',MESSAGE='PO transition altered protected terms, snapshots, current-version or provenance fields.';
          END IF;
          IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND NEW."Status"=OLD."Status" AND
             to_jsonb(NEW)-ARRAY['Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_protected_fields',MESSAGE='Same-status reservation cannot alter controlled fields.';
          ELSIF TG_TABLE_NAME='commercial_comparison_lines' THEN
            SELECT c."Status" INTO STRICT parent_status FROM nexa.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId";
            IF parent_status NOT IN ('Draft','RevisionRequested') OR
               to_jsonb(NEW)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_line_editable_boundary',MESSAGE='Comparison line correction requires its exact editable parent and immutable provenance.';
            END IF;
          ELSIF TG_TABLE_NAME='material_followup_handoffs' THEN
            IF (OLD."Status",NEW."Status") NOT IN (('PendingFollowUp','InProgress'),('InProgress','Completed')) OR
               (NEW."PurchaseOrderId",NEW."PurchaseOrderLineId",NEW."HandoffNumber",NEW."OrderedQuantitySnapshot",NEW."HandoffAt") IS DISTINCT FROM (OLD."PurchaseOrderId",OLD."PurchaseOrderLineId",OLD."HandoffNumber",OLD."OrderedQuantitySnapshot",OLD."HandoffAt") OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_followup_transition',MESSAGE='Material Follow-up permits only PendingFollowUp to InProgress to Completed.';
            END IF;
          ELSIF TG_TABLE_NAME='purchase_transaction_approval_policies' THEN
            IF (NEW."OrganizationId",NEW."RouteCode",NEW."MinimumAmount",NEW."MaximumAmount",NEW."ApproverRoleCode",NEW."EffectiveFrom") IS DISTINCT FROM (OLD."OrganizationId",OLD."RouteCode",OLD."MinimumAmount",OLD."MaximumAmount",OLD."ApproverRoleCode",OLD."EffectiveFrom") OR NEW."IsActive"=OLD."IsActive" OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_policy_controlled_lifecycle',MESSAGE='Approval policy UPDATE permits only controlled activation/deactivation.';
            END IF;
            NEW."UpdatedAt":=statement_timestamp();
          END IF;
          RETURN NEW;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_write_bound_history()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE org text; number text; login text; employee uuid; role text; old_status text; action text; correlation text; now_at timestamptz:=statement_timestamp(); matches bigint;
        BEGIN
          IF TG_OP='UPDATE' AND NEW."Status" IS NOT DISTINCT FROM OLD."Status" THEN RETURN NEW; END IF;
          login:=coalesce(NEW."UpdatedBy",NEW."CreatedBy"); old_status:=CASE WHEN TG_OP='INSERT' THEN NULL ELSE OLD."Status" END;
          action:=CASE NEW."Status" WHEN 'Issued' THEN 'Issue' WHEN 'Submitted' THEN 'Submit' WHEN 'PendingApproval' THEN CASE WHEN old_status='RevisionRequested' THEN 'Resubmit' ELSE 'Recommend' END WHEN 'Resubmitted' THEN 'Resubmit' WHEN 'Approved' THEN 'Approve' WHEN 'Rejected' THEN 'Reject' WHEN 'RevisionRequested' THEN 'RequestRevision' WHEN 'Cancelled' THEN 'Cancel' WHEN 'Superseded' THEN 'Supersede' WHEN 'Closed' THEN 'Close' WHEN 'TechnicallyCompliant' THEN 'Verify' WHEN 'TechnicallyRejected' THEN 'RejectTechnical' WHEN 'Withdrawn' THEN 'Withdraw' WHEN 'InProgress' THEN 'StartFollowUp' WHEN 'Completed' THEN 'CompleteFollowUp' ELSE CASE WHEN TG_OP='INSERT' THEN 'Create' ELSE 'Transition' END END;
          SELECT count(*),min(m."EmployeeId") INTO matches,employee FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId"
           WHERE m."Subject"=login AND m."IsActive" AND m."EffectiveFrom"<=now_at::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=now_at::date) AND e."Status"='Active' AND e."LoginEnabled";
          IF matches<>1 THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_transition_actor_identity',MESSAGE='Transition actor must resolve to one active employee identity.'; END IF;
          SELECT min(r."Code") INTO role FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId" AND r."IsActive"
           WHERE a."EmployeeId"=employee AND a."EffectiveFrom"<=now_at::date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=now_at::date) AND a."ApprovalStatus" IN ('Approved','SeedApproved');
          IF role IS NULL THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_transition_actor_role',MESSAGE='Transition actor has no active approved role.'; END IF;
          IF NOT EXISTS (
            SELECT 1 FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId" AND r."IsActive"
            WHERE a."EmployeeId"=employee AND a."EffectiveFrom"<=now_at::date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=now_at::date)
              AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND (
                (TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations') AND r."Code" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
                (TG_TABLE_NAME='vendor_quotations' AND action IN ('Verify','RejectTechnical') AND r."Code" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
                (TG_TABLE_NAME='vendor_quotations' AND action NOT IN ('Verify','RejectTechnical') AND r."Code" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
                (TG_TABLE_NAME='commercial_comparisons' AND action IN ('Approve','Reject','RequestRevision') AND
                  ((NEW."ApprovalRoute"='MANAGER' AND r."Code" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
                   (NEW."ApprovalRoute"='TECHNICAL_DIRECTOR' AND r."Code"='TECHNICAL_DIRECTOR') OR
                   (NEW."ApprovalRoute"='MANAGING_DIRECTOR' AND r."Code"='MANAGING_DIRECTOR'))) OR
                (TG_TABLE_NAME='commercial_comparisons' AND action NOT IN ('Approve','Reject','RequestRevision') AND r."Code"='PURCHASE_MANAGER') OR
                (TG_TABLE_NAME='purchase_orders' AND action IN ('Approve','Reject','RequestRevision') AND
                  ((NEW."ApprovalRoute"='MANAGER' AND r."Code" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
                   (NEW."ApprovalRoute"='TECHNICAL_DIRECTOR' AND r."Code"='TECHNICAL_DIRECTOR') OR
                   (NEW."ApprovalRoute"='MANAGING_DIRECTOR' AND r."Code"='MANAGING_DIRECTOR'))) OR
                (TG_TABLE_NAME='purchase_orders' AND action NOT IN ('Approve','Reject','RequestRevision') AND r."Code"='PURCHASE_MANAGER') OR
                (TG_TABLE_NAME='material_followup_handoffs' AND r."Code" IN ('STORES_EXECUTIVE','STORES_MANAGER'))
              )) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_transition_role_authorization',MESSAGE='Transition actor role is not authorized for this exact action and route.';
          END IF;
          IF action='Approve' AND lower(login)=lower(NEW."CreatedBy") THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_creator_self_approval',MESSAGE='Creator self-approval is prohibited.'; END IF;
          IF TG_TABLE_NAME='purchase_orders' AND action='Approve' AND EXISTS (SELECT 1 FROM nexa.purchase_transaction_status_history h WHERE h."EntityType"='PurchaseOrder' AND h."EntityId"=NEW."Id" AND h."ToStatus" IN ('PendingApproval','Resubmitted') AND h."ActorEmployeeId"=employee) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_issuer_approver_separation',MESSAGE='The PO submitter/resubmitter cannot approve the same controlled version.';
          END IF;
          correlation:=format('REV869B|%s|%s|%s|%s',NEW."Id",NEW."Version",coalesce(old_status,'NULL'),NEW."Status");
          IF TG_TABLE_NAME='request_for_quotations' THEN org:=NEW."OrganizationId";number:=NEW."RfqNumber";
          ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN SELECT r."OrganizationId",r."RfqNumber" INTO STRICT org,number FROM nexa.request_for_quotations r WHERE r."Id"=NEW."RequestForQuotationId";
          ELSIF TG_TABLE_NAME='vendor_quotations' THEN org:=NEW."OrganizationId";number:=NEW."QuotationNumber";
          ELSIF TG_TABLE_NAME='commercial_comparisons' THEN org:=NEW."OrganizationId";number:=NEW."ComparisonNumber";
          ELSIF TG_TABLE_NAME='purchase_orders' THEN org:=NEW."OrganizationId";number:=NEW."PoNumber";
          ELSE SELECT p."OrganizationId" INTO STRICT org FROM nexa.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId";number:=NEW."HandoffNumber"; END IF;
          INSERT INTO nexa.purchase_transaction_status_history ("Id","OrganizationId","EntityType","EntityId","DocumentNumber","Action","FromStatus","ToStatus","ActorEmployeeId","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),org,CASE TG_TABLE_NAME WHEN 'request_for_quotations' THEN 'RFQ' WHEN 'rfq_vendor_invitations' THEN 'RFQInvitation' WHEN 'vendor_quotations' THEN 'VendorQuotation' WHEN 'commercial_comparisons' THEN 'CommercialComparison' WHEN 'purchase_orders' THEN 'PurchaseOrder' ELSE 'MaterialFollowUp' END,NEW."Id",number,action,old_status,NEW."Status",employee,login,role,'Database-bound controlled transition',correlation,now_at,login,NEW."Version");
          IF TG_TABLE_NAME='commercial_comparisons' AND action IN ('Approve','Reject','RequestRevision','Resubmit') THEN
            INSERT INTO nexa.purchase_transaction_approval_history ("Id","CommercialComparisonId","Action","FromStatus","ToStatus","ApprovalRoute","ActorEmployeeId","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),NEW."Id",action,old_status,NEW."Status",NEW."ApprovalRoute",employee,login,role,'Database-bound controlled transition',correlation,now_at,login,NEW."Version");
          ELSIF TG_TABLE_NAME='purchase_orders' AND TG_OP='UPDATE' THEN
            INSERT INTO nexa.purchase_order_history ("Id","PurchaseOrderId","Action","FromStatus","ToStatus","RevisionNumber","ActorEmployeeId","ActorLoginId","ActorRoleCode","Reason","CorrelationId","CreatedAt","CreatedBy","Version") VALUES(gen_random_uuid(),NEW."Id",action,old_status,NEW."Status",NEW."RevisionNumber",employee,login,role,'Database-bound controlled transition',correlation,now_at,login,NEW."Version");
          END IF;
          RETURN NEW;
        END $rev869b$;

        DROP FUNCTION IF EXISTS nexa.rev869b_write_bound_history();

        CREATE OR REPLACE FUNCTION nexa.rev869b_write_policy_history() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE employee uuid; matches bigint; actor_role text; actor text:=coalesce(NEW."UpdatedBy",NEW."CreatedBy");
        BEGIN
          SELECT count(*),min(m."EmployeeId") INTO matches,employee FROM nexa.employee_identity_mappings m JOIN nexa.employees e ON e."Id"=m."EmployeeId"
           WHERE m."Subject"=actor AND m."OrganizationId"=NEW."OrganizationId" AND m."IsActive" AND e."Status"='Active' AND e."LoginEnabled";
          IF matches<>1 THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_identity',MESSAGE='Approval-policy actor must resolve to one active organization identity.'; END IF;
          SELECT min(r."Code") INTO actor_role FROM nexa.employee_role_assignments a JOIN nexa.roles r ON r."Id"=a."RoleId" AND r."IsActive" WHERE a."EmployeeId"=employee AND r."Code" IN ('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND a."ApprovalStatus" IN ('Approved','SeedApproved');
          IF actor_role IS NULL THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_role',MESSAGE='Approval-policy lifecycle requires an authorized active role.'; END IF;
          INSERT INTO nexa.controlled_configuration_histories ("Id","OrganizationId","EntityType","EntityId","Action","BeforeJson","AfterJson","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),NEW."OrganizationId",'PurchaseTransactionApprovalPolicy',NEW."Id",CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."IsActive" THEN 'Activate' ELSE 'Deactivate' END,CASE WHEN TG_OP='INSERT' THEN NULL::jsonb ELSE to_jsonb(OLD) END,to_jsonb(NEW),actor,actor_role,'Database-bound approval-policy change',format('REV869B|POLICY|%s|%s',NEW."Id",NEW."Version"),statement_timestamp(),actor,NEW."Version"); RETURN NEW;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION nexa.rev869b_require_bound_history()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,nexa AS $rev869b$
        DECLARE entity_type text; old_status text; expected_action text; actor text; specialized bigint;
        BEGIN
          IF TG_OP='UPDATE' AND NEW."Status" IS NOT DISTINCT FROM OLD."Status" THEN RETURN NULL; END IF;
          IF TG_OP='INSERT' AND (
            (TG_TABLE_NAME='request_for_quotations' AND EXISTS (SELECT 1 FROM nexa.request_for_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='rfq_vendor_invitations' AND EXISTS (SELECT 1 FROM nexa.rfq_vendor_invitations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='vendor_quotations' AND EXISTS (SELECT 1 FROM nexa.vendor_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='commercial_comparisons' AND EXISTS (SELECT 1 FROM nexa.commercial_comparisons p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='purchase_orders' AND EXISTS (SELECT 1 FROM nexa.purchase_orders p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='material_followup_handoffs' AND EXISTS (SELECT 1 FROM nexa.material_followup_handoffs p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version")))
          ) THEN RETURN NULL; END IF;
          IF NOT EXISTS (SELECT 1 FROM nexa.purchase_transaction_status_history h WHERE h."EntityId"=NEW."Id" AND h."ToStatus"=NEW."Status" AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_requires_history',MESSAGE='Every controlled parent transition requires same-transaction status history.';
          END IF;
          actor:=coalesce(NEW."UpdatedBy",NEW."CreatedBy"); old_status:=CASE WHEN TG_OP='UPDATE' THEN OLD."Status" ELSE NULL END;
          IF TG_TABLE_NAME='request_for_quotations' THEN entity_type:='RFQ'; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Closed' THEN 'Close' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN entity_type:='RFQInvitation'; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'InviteVendor' WHEN NEW."Status"='Submitted' THEN 'Submit' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='vendor_quotations' THEN entity_type:='VendorQuotation'; expected_action:=CASE WHEN NEW."Status"='Submitted' THEN CASE WHEN NEW."PreviousRevisionId" IS NULL THEN 'Submit' ELSE 'Revise' END WHEN NEW."Status"='TechnicallyCompliant' THEN 'Verify' WHEN NEW."Status"='TechnicallyRejected' THEN 'RejectTechnical' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Rejected' THEN 'Reject' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='commercial_comparisons' THEN entity_type:='CommercialComparison'; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' AND OLD."Status"='Draft' THEN 'Recommend' WHEN NEW."Status"='PendingApproval' THEN 'Resubmit' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='RevisionRequested' THEN 'RequestRevision' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='purchase_orders' THEN
            entity_type:='PurchaseOrder';
            IF TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN SELECT p."Status" INTO STRICT old_status FROM nexa.purchase_orders p WHERE p."Id"=NEW."PreviousVersionId"; END IF;
            expected_action:=CASE WHEN TG_OP='INSERT' AND NEW."Status"='RevisionDraft' THEN 'ReviseRejected' WHEN TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN 'Amend' WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' THEN 'Submit' WHEN NEW."Status"='Resubmitted' THEN 'ResubmitRejected' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSE entity_type:='MaterialFollowUp'; expected_action:=CASE WHEN NEW."Status"='InProgress' THEN 'StartFollowUp' WHEN NEW."Status"='Completed' THEN 'CompleteFollowUp' ELSE 'Handoff' END;
          END IF;
          IF NOT EXISTS (SELECT 1 FROM nexa.purchase_transaction_status_history h WHERE h."EntityType"=entity_type AND h."EntityId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_history_exactness',MESSAGE='Transition history from/to/action/actor/version does not match its parent mutation.';
          END IF;
          IF TG_TABLE_NAME='commercial_comparisons' AND expected_action IN ('Approve','Reject','RequestRevision','Resubmit') THEN
            SELECT count(*) INTO specialized FROM nexa.purchase_transaction_approval_history h WHERE h."CommercialComparisonId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
            IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_requires_approval_history',MESSAGE='Comparison approval transition requires one exact same-transaction approval history.'; END IF;
          ELSIF TG_TABLE_NAME='purchase_orders' THEN
            SELECT count(*) INTO specialized FROM nexa.purchase_order_history h WHERE h."PurchaseOrderId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM coalesce(old_status,'') AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
            IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_requires_po_history',MESSAGE='Purchase-order transition requires one exact same-transaction PO history.'; END IF;
          END IF;
          RETURN NULL;
        END $rev869b$;

        CREATE TRIGGER trg_rev869b_explicit_rfq_mutation BEFORE INSERT OR UPDATE ON nexa.request_for_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_invitation_mutation BEFORE INSERT OR UPDATE ON nexa.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_quotation_mutation BEFORE INSERT OR UPDATE ON nexa.vendor_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_comparison_mutation BEFORE INSERT OR UPDATE ON nexa.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_comparison_line_mutation BEFORE INSERT OR UPDATE ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_po_mutation BEFORE INSERT OR UPDATE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_followup_mutation BEFORE INSERT OR UPDATE ON nexa.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_policy_mutation BEFORE INSERT OR UPDATE ON nexa.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_rfq_line_insert BEFORE INSERT ON nexa.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_quotation_line_insert BEFORE INSERT ON nexa.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_technical_insert BEFORE INSERT ON nexa.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_po_line_insert BEFORE INSERT ON nexa.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_guard_explicit_mutation();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_rfq_history AFTER INSERT OR UPDATE ON nexa.request_for_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_invitation_history AFTER INSERT OR UPDATE ON nexa.rfq_vendor_invitations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_quotation_history AFTER INSERT OR UPDATE ON nexa.vendor_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_comparison_history AFTER INSERT OR UPDATE ON nexa.commercial_comparisons DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_po_history AFTER INSERT OR UPDATE ON nexa.purchase_orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_followup_history AFTER INSERT OR UPDATE ON nexa.material_followup_handoffs DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_require_bound_history();
        CREATE TRIGGER trg_rev869b_bound_policy_history AFTER INSERT OR UPDATE ON nexa.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_write_policy_history();

        CREATE TRIGGER trg_rev869b_delete_rfq BEFORE DELETE ON nexa.request_for_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_rfq_line BEFORE DELETE ON nexa.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_invitation BEFORE DELETE ON nexa.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_quotation BEFORE DELETE ON nexa.vendor_quotations FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_quotation_line BEFORE DELETE ON nexa.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_technical BEFORE DELETE ON nexa.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_comparison BEFORE DELETE ON nexa.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_comparison_line BEFORE DELETE ON nexa.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_approval_history BEFORE DELETE ON nexa.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po BEFORE DELETE ON nexa.purchase_orders FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po_line BEFORE DELETE ON nexa.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po_history BEFORE DELETE ON nexa.purchase_order_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_followup BEFORE DELETE ON nexa.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_status_history BEFORE DELETE ON nexa.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_policy BEFORE DELETE ON nexa.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_reject_controlled_delete();
        """;

    public const string Remove = """
        DROP FUNCTION IF EXISTS nexa.rev869b_write_policy_history() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_require_bound_history() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_write_bound_history() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_guard_explicit_mutation() CASCADE;
        DROP FUNCTION IF EXISTS nexa.rev869b_reject_controlled_delete() CASCADE;
        """;
}
