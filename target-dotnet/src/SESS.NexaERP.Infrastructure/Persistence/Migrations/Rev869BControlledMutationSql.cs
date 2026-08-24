namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Rev869BControlledMutationSql
{
    public static string Install => AdvanceSchemaSql.Expand(InstallTemplate);
    private const string InstallTemplate = """
        DO $rev869b_qualification_preflight$
        BEGIN
          IF EXISTS (SELECT 1 FROM __advance_schema__.vendor_qualifications q WHERE NOT (
            (q."VerificationStatus"='Draft' AND q."ApprovalStatus"='Draft' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NULL) OR
            (q."VerificationStatus"='Pending Approval' AND q."ApprovalStatus"='Pending Approval' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NULL AND q."IsActive") OR
            (q."VerificationStatus"='Verified' AND q."ApprovalStatus"='Pending Approval' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NULL AND q."IsActive") OR
            (q."VerificationStatus" IN ('Verified','Approved') AND q."ApprovalStatus"='Approved' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND q."IsActive") OR
            (q."VerificationStatus"='Pending Approval' AND q."ApprovalStatus"='Rejected' AND q."VerifiedByEmployeeId" IS NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND NOT q."IsActive") OR
            (q."VerificationStatus"='Verified' AND q."ApprovalStatus"='Rejected' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND NOT q."IsActive") OR
            (q."VerificationStatus" IN ('Verified','Approved') AND q."ApprovalStatus"='Revision Requested' AND q."VerifiedByEmployeeId" IS NOT NULL AND q."ApprovedByEmployeeId" IS NOT NULL AND q."VerifiedByEmployeeId"<>q."ApprovedByEmployeeId" AND NOT q."IsActive")
          )) THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_existing_state_preflight',
              MESSAGE='Existing vendor qualification lifecycle values require controlled correction before REV869B can apply.';
          END IF;
        END $rev869b_qualification_preflight$;
        ALTER TABLE __advance_schema__.vendor_qualifications DROP CONSTRAINT IF EXISTS "CK_vendor_qualification_rev869b_lifecycle";
        ALTER TABLE __advance_schema__.vendor_qualifications ADD CONSTRAINT "CK_vendor_qualification_rev869b_lifecycle" CHECK (
          ("VerificationStatus"='Draft' AND "ApprovalStatus"='Draft' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NULL) OR
          ("VerificationStatus"='Pending Approval' AND "ApprovalStatus"='Pending Approval' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NULL AND "IsActive") OR
          ("VerificationStatus"='Verified' AND "ApprovalStatus"='Pending Approval' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NULL AND "IsActive") OR
          ("VerificationStatus" IN ('Verified','Approved') AND "ApprovalStatus"='Approved' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND "IsActive") OR
          ("VerificationStatus"='Pending Approval' AND "ApprovalStatus"='Rejected' AND "VerifiedByEmployeeId" IS NULL AND "ApprovedByEmployeeId" IS NOT NULL AND NOT "IsActive") OR
          ("VerificationStatus"='Verified' AND "ApprovalStatus"='Rejected' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND NOT "IsActive") OR
          ("VerificationStatus" IN ('Verified','Approved') AND "ApprovalStatus"='Revision Requested' AND "VerifiedByEmployeeId" IS NOT NULL AND "ApprovedByEmployeeId" IS NOT NULL AND "VerifiedByEmployeeId"<>"ApprovedByEmployeeId" AND NOT "IsActive")
        );

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_durable_audit_retention()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE cleanup_reason text:=nullif(trim(current_setting('__advance_schema__.rev869b_audit_cleanup_reason',true)),'');
          cleanup_correlation text:=nullif(trim(current_setting('__advance_schema__.rev869b_audit_cleanup_correlation',true)),'');
          database_owner name;
        BEGIN
          IF TG_OP='UPDATE' THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='__advance_schema__',TABLE='audit_logs',
              CONSTRAINT='rev869b_audit_immutable',MESSAGE='Durable audit evidence is immutable.';
          END IF;
          IF OLD."CreatedAt">statement_timestamp()-interval '10 years' THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='__advance_schema__',TABLE='audit_logs',
              CONSTRAINT='rev869b_audit_minimum_ten_year_retention',MESSAGE='Durable audit evidence must be retained for at least ten years.';
          END IF;
          SELECT pg_get_userbyid(d.datdba) INTO STRICT database_owner FROM pg_database d WHERE d.datname=current_database();
          IF session_user IS DISTINCT FROM database_owner OR cleanup_reason IS NULL OR octet_length(cleanup_reason)>1000 OR
             cleanup_correlation IS NULL OR octet_length(cleanup_correlation)>120 THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='__advance_schema__',TABLE='audit_logs',
              CONSTRAINT='rev869b_audit_controlled_cleanup',MESSAGE='Expired audit cleanup requires the database owner, an exact correlation, and a bounded reason.';
          END IF;
          INSERT INTO __advance_schema__.audit_logs
            ("Id","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),'Security','PurgeExpiredAudit','AuditLogRetention',
            encode(public.digest(convert_to(OLD."Id"::text,'UTF8'),'sha256'),'hex'),session_user,'Success',cleanup_correlation,NULL,
            jsonb_build_object('minimumRetentionYears',10,'reason',cleanup_reason,'deletedCreatedAt',OLD."CreatedAt")::text,
            statement_timestamp(),session_user,0);
          RETURN OLD;
        END $rev869b$;
        REVOKE ALL ON FUNCTION __advance_schema__.rev869b_guard_durable_audit_retention() FROM PUBLIC;
        CREATE TRIGGER trg_rev869b_durable_audit_retention BEFORE UPDATE OR DELETE ON __advance_schema__.audit_logs
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_durable_audit_retention();

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_history_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE matches bigint; creator_matches bigint; parent_version bigint; parent_login text; parent_org text; parent_number text; parent_status text; parent_correlation text; parent_creator text; parent_creator_employee uuid;
        BEGIN
          IF TG_TABLE_NAME='purchase_transaction_status_history' THEN
            SELECT count(*),min(p.version),min(p.login),min(p.org),min(p.number),min(p.status),min(p.correlation),min(p.creator)
              INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator FROM (
              SELECT r."Version" version,coalesce(r."UpdatedBy",r."CreatedBy") login,r."OrganizationId" org,r."RfqNumber" number,r."Status" status,r."TransitionCorrelationId" correlation,r."CreatedBy" creator FROM __advance_schema__.request_for_quotations r WHERE NEW."EntityType"='RFQ' AND r."Id"=NEW."EntityId" AND r.xmin::text::bigint=txid_current()
              UNION ALL SELECT i."Version",coalesce(i."UpdatedBy",i."CreatedBy"),r."OrganizationId",r."RfqNumber",i."Status",i."TransitionCorrelationId",i."CreatedBy" FROM __advance_schema__.rfq_vendor_invitations i JOIN __advance_schema__.request_for_quotations r ON r."Id"=i."RequestForQuotationId" WHERE NEW."EntityType"='RFQInvitation' AND i."Id"=NEW."EntityId" AND i.xmin::text::bigint=txid_current()
              UNION ALL SELECT q."Version",coalesce(q."UpdatedBy",q."CreatedBy"),q."OrganizationId",q."QuotationNumber",q."Status",q."TransitionCorrelationId",q."CreatedBy" FROM __advance_schema__.vendor_quotations q WHERE NEW."EntityType"='VendorQuotation' AND q."Id"=NEW."EntityId" AND q.xmin::text::bigint=txid_current()
              UNION ALL SELECT v."Version",coalesce(v."UpdatedBy",v."CreatedBy"),q."OrganizationId",q."QuotationNumber",v."ComplianceStatus",v."CorrelationId",v."CreatedBy" FROM __advance_schema__.quotation_technical_verifications v JOIN __advance_schema__.vendor_quotation_lines l ON l."Id"=v."VendorQuotationLineId" JOIN __advance_schema__.vendor_quotations q ON q."Id"=l."VendorQuotationId" WHERE NEW."EntityType"='TechnicalVerification' AND v."Id"=NEW."EntityId" AND v.xmin::text::bigint=txid_current()
              UNION ALL SELECT c."Version",coalesce(c."UpdatedBy",c."CreatedBy"),c."OrganizationId",c."ComparisonNumber",c."Status",c."TransitionCorrelationId",c."CreatedBy" FROM __advance_schema__.commercial_comparisons c WHERE NEW."EntityType"='CommercialComparison' AND c."Id"=NEW."EntityId" AND c.xmin::text::bigint=txid_current()
              UNION ALL SELECT p."Version",coalesce(p."UpdatedBy",p."CreatedBy"),p."OrganizationId",p."PoNumber",p."Status",p."TransitionCorrelationId",p."CreatedBy" FROM __advance_schema__.purchase_orders p WHERE NEW."EntityType"='PurchaseOrder' AND p."Id"=NEW."EntityId" AND p.xmin::text::bigint=txid_current()
              UNION ALL SELECT h."Version",coalesce(h."UpdatedBy",h."CreatedBy"),p."OrganizationId",h."HandoffNumber",h."Status",h."CorrelationId",h."CreatedBy" FROM __advance_schema__.material_followup_handoffs h JOIN __advance_schema__.purchase_orders p ON p."Id"=h."PurchaseOrderId" WHERE NEW."EntityType"='MaterialFollowUp' AND h."Id"=NEW."EntityId" AND h.xmin::text::bigint=txid_current()
            ) p;
            IF matches<>1 OR NEW."OrganizationId"<>parent_org OR NEW."DocumentNumber"<>parent_number OR NEW."ToStatus"<>parent_status THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_history_parent_transition',MESSAGE='Status history requires the exact parent mutation in the current transaction.';
            END IF;
          ELSIF TG_TABLE_NAME='purchase_transaction_approval_history' THEN
            SELECT count(*),min(c."Version"),min(coalesce(c."UpdatedBy",c."CreatedBy")),min(c."OrganizationId"),min(c."ComparisonNumber"),min(c."Status"),min(c."TransitionCorrelationId"),min(c."CreatedBy") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator
              FROM __advance_schema__.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId" AND c.xmin::text::bigint=txid_current();
            IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."ApprovalRoute"<>(SELECT c."ApprovalRoute" FROM __advance_schema__.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_approval_history_parent_transition',MESSAGE='Approval history requires the exact comparison transition in the current transaction.';
            END IF;
          ELSE
            SELECT count(*),min(p."Version"),min(coalesce(p."UpdatedBy",p."CreatedBy")),min(p."OrganizationId"),min(p."PoNumber"),min(p."Status"),min(p."TransitionCorrelationId"),min(p."CreatedBy") INTO matches,parent_version,parent_login,parent_org,parent_number,parent_status,parent_correlation,parent_creator
              FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId" AND p.xmin::text::bigint=txid_current();
            IF matches<>1 OR NEW."ToStatus"<>parent_status OR NEW."RevisionNumber"<>(SELECT p."RevisionNumber" FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_history_parent_transition',MESSAGE='PO history requires the exact purchase-order transition in the current transaction.';
            END IF;
          END IF;
          IF NEW."ActorLoginId" IS DISTINCT FROM parent_login OR NEW."CreatedBy" IS DISTINCT FROM parent_login OR NEW."CorrelationId" IS DISTINCT FROM parent_correlation OR
             __advance_schema__.rev869b_command_context_valid(parent_org,NEW."ActorEmployeeId",
               current_setting('__advance_schema__.rev869b_identity_issuer',true),NEW."ActorLoginId",NEW."ActorRoleCode") IS NOT TRUE OR
             length(trim(coalesce(CASE WHEN TG_TABLE_NAME='purchase_order_history' THEN NEW."Reason" ELSE NEW."Remarks" END,'')))=0 OR
             NOT EXISTS (SELECT 1 FROM __advance_schema__.employee_identity_mappings m JOIN __advance_schema__.employees e ON e."Id"=m."EmployeeId" WHERE m."Subject"=NEW."ActorLoginId" AND m."EmployeeId"=NEW."ActorEmployeeId" AND m."OrganizationId"=parent_org AND m."IsActive" AND m."EffectiveFrom"<=statement_timestamp()::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date) AND e."Status"='Active' AND e."LoginEnabled") OR
             NOT EXISTS (SELECT 1 FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId" WHERE a."EmployeeId"=NEW."ActorEmployeeId" AND r."Code"=NEW."ActorRoleCode" AND r."IsActive" AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date)) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_actor_binding',MESSAGE='History actor, role, correlation and remarks must match the current controlled transition.';
          END IF;
          SELECT count(*),min(m."EmployeeId") INTO creator_matches,parent_creator_employee
            FROM __advance_schema__.employee_identity_mappings m JOIN __advance_schema__.employees e ON e."Id"=m."EmployeeId"
            WHERE m."Subject"=parent_creator AND m."OrganizationId"=parent_org AND m."IsActive"
              AND m."EffectiveFrom"<=statement_timestamp()::date AND (m."EffectiveTo" IS NULL OR m."EffectiveTo">=statement_timestamp()::date)
              AND e."Status"='Active' AND e."LoginEnabled";
          IF creator_matches<>1 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_parent_creator_identity_binding',MESSAGE='Parent creator must resolve to exactly one active employee identity.';
          END IF;
          IF NEW."Action"='Approve' AND NEW."ActorEmployeeId"=parent_creator_employee THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_creator_self_approval',MESSAGE='Creator self-approval is prohibited.';
          END IF;
          IF NEW."Action"='Approve' AND TG_TABLE_NAME='purchase_transaction_approval_history' AND EXISTS (
            SELECT 1 FROM __advance_schema__.commercial_comparisons c JOIN __advance_schema__.commercial_comparison_lines cl ON cl."CommercialComparisonId"=c."Id" AND cl."IsRecommended"
            JOIN __advance_schema__.vendor_quotation_lines ql ON ql."Id"=cl."VendorQuotationLineId" JOIN __advance_schema__.quotation_technical_verifications v ON v."VendorQuotationLineId"=ql."Id"
            WHERE c."Id"=NEW."CommercialComparisonId" AND v."VerifierEmployeeId"=NEW."ActorEmployeeId") THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_verifier_approver_separation',MESSAGE='Technical verifier cannot approve the same controlled comparison.';
          END IF;
          IF NEW."Action"='Approve' AND TG_TABLE_NAME='purchase_order_history' AND EXISTS (
            SELECT 1 FROM __advance_schema__.purchase_transaction_status_history h WHERE h."EntityType"='PurchaseOrder' AND h."EntityId"=NEW."PurchaseOrderId"
              AND h."ToStatus" IN ('PendingApproval','Resubmitted') AND h."ActorEmployeeId"=NEW."ActorEmployeeId" AND h."Version"=parent_version-1) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_issuer_approver_separation',MESSAGE='The PO submitter or resubmitter cannot approve the same controlled version.';
          END IF;
          IF NEW."Action"='Issue' AND TG_TABLE_NAME='purchase_order_history' AND EXISTS (
            SELECT 1 FROM __advance_schema__.purchase_order_history approved
            WHERE approved."PurchaseOrderId"=NEW."PurchaseOrderId" AND approved."Action"='Approve'
              AND approved."ToStatus"='Approved' AND approved."RevisionNumber"=NEW."RevisionNumber"
              AND approved."ActorEmployeeId"=NEW."ActorEmployeeId") THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_po_approver_issuer_separation',
              MESSAGE='The PO approver cannot issue the same controlled revision.';
          END IF;
          IF NOT (
            (TG_TABLE_NAME='purchase_transaction_status_history' AND (
              (NEW."EntityType" IN ('RFQ','RFQInvitation') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
              (NEW."EntityType"='VendorQuotation' AND NEW."Action" IN ('Verify','RejectTechnical','ReserveTechnicalVerification') AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
              (NEW."EntityType"='VendorQuotation' AND NEW."Action" NOT IN ('Verify','RejectTechnical','ReserveTechnicalVerification') AND NEW."ActorRoleCode" IN ('PURCHASE_EXECUTIVE','PURCHASE_MANAGER')) OR
              (NEW."EntityType"='TechnicalVerification' AND NEW."ActorRoleCode" IN ('TECHNICAL_ENGINEER','TECHNICAL_DIRECTOR')) OR
              (NEW."EntityType"='CommercialComparison' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
              (NEW."EntityType"='PurchaseOrder' AND NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR
              (NEW."EntityType"='MaterialFollowUp' AND ((NEW."Action"='Handoff' AND NEW."ActorRoleCode"='PURCHASE_MANAGER') OR (NEW."Action"<>'Handoff' AND NEW."ActorRoleCode" IN ('STORES_EXECUTIVE','STORES_MANAGER')))) OR
              (NEW."EntityType" IN ('CommercialComparison','PurchaseOrder') AND NEW."Action" IN ('Approve','Reject','RequestRevision') AND
                ((NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER') AND EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_approval_policies p WHERE p."OrganizationId"=parent_org AND p."RouteCode"='MANAGER' AND p."IsActive")) OR
                 NEW."ActorRoleCode" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')))
            )) OR
            (TG_TABLE_NAME='purchase_transaction_approval_history' AND
              ((NEW."ApprovalRoute"='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
               (NEW."ApprovalRoute"='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
               (NEW."ApprovalRoute"='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
            (TG_TABLE_NAME='purchase_order_history' AND
              ((NEW."Action" IN ('Approve','Reject','RequestRevision') AND
                (((SELECT p."ApprovalRoute" FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGER' AND NEW."ActorRoleCode" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')) OR
                 ((SELECT p."ApprovalRoute" FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='TECHNICAL_DIRECTOR' AND NEW."ActorRoleCode"='TECHNICAL_DIRECTOR') OR
                 ((SELECT p."ApprovalRoute" FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PurchaseOrderId")='MANAGING_DIRECTOR' AND NEW."ActorRoleCode"='MANAGING_DIRECTOR'))) OR
               (NEW."Action" NOT IN ('Approve','Reject','RequestRevision') AND NEW."ActorRoleCode"='PURCHASE_MANAGER')))
          ) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_history_action_role',MESSAGE='History role is not authorized for the exact controlled action and approval route.';
          END IF;
          PERFORM __advance_schema__.rev869b_claim_command_context(TG_TABLE_NAME,NEW."Id",
            CASE WHEN TG_TABLE_NAME='purchase_transaction_status_history' THEN NEW."EntityType"
                 WHEN TG_TABLE_NAME='purchase_transaction_approval_history' THEN 'CommercialComparison'
                 ELSE 'PurchaseOrder' END,
            CASE WHEN TG_TABLE_NAME='purchase_transaction_status_history' THEN NEW."EntityId"
                 WHEN TG_TABLE_NAME='purchase_transaction_approval_history' THEN NEW."CommercialComparisonId"
                 ELSE NEW."PurchaseOrderId" END,
            NEW."Action",parent_version,NEW."FromStatus",NEW."ToStatus",NEW."CorrelationId",
            CASE WHEN TG_TABLE_NAME='purchase_order_history' THEN NEW."Reason" ELSE NEW."Remarks" END);
          NEW."Version":=parent_version; NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
          RETURN NEW;
        END $rev869b$;

        ALTER TABLE __advance_schema__.material_followup_handoffs DROP CONSTRAINT "CK_material_followup_quantity";
        ALTER TABLE __advance_schema__.material_followup_handoffs ADD CONSTRAINT "CK_material_followup_quantity"
          CHECK ("OrderedQuantitySnapshot">0 AND "Status" IN ('PendingFollowUp','InProgress','Completed'));
        DROP TRIGGER IF EXISTS trg_rev869b_followup_immutable ON __advance_schema__.material_followup_handoffs;

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_reject_controlled_delete()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        BEGIN
          RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_controlled_delete_guard',
            MESSAGE=format('REV869B controlled relation % rejects destructive DELETE.',TG_TABLE_NAME);
        END $rev869b$;

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_qualification_lifecycle()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE actor_employee uuid; creator_employee uuid; actor_matches bigint; creator_matches bigint; authorized_roles bigint;
          legacy_normalization boolean;
        BEGIN
          IF TG_OP='DELETE' THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_delete_guard',MESSAGE='Vendor qualification lifecycle is retained and cannot be deleted.';
          END IF;
          IF TG_OP='INSERT' THEN
            IF NEW."Version"<>0 OR NEW."VerificationStatus"<>'Pending Approval' OR NEW."ApprovalStatus"<>'Pending Approval' OR
               NEW."VerifiedByEmployeeId" IS NOT NULL OR NEW."ApprovedByEmployeeId" IS NOT NULL OR NOT NEW."IsActive" OR length(trim(coalesce(NEW."CreatedBy",'')))=0 THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_initial_state',MESSAGE='Qualification INSERT must start pending with no verifier or approver identity.';
            END IF;
            IF __advance_schema__.rev869b_command_context_valid(
                 NEW."OrganizationId",nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid,
                 current_setting('__advance_schema__.rev869b_identity_issuer',true),NEW."CreatedBy",
                 current_setting('__advance_schema__.rev869b_actor_role',true)) IS NOT TRUE THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_command_context',
                MESSAGE='Qualification INSERT requires a protected server-issued command context.';
            END IF;
            RETURN NEW;
          END IF;
          SELECT count(*),min(m."EmployeeId") INTO actor_matches,actor_employee FROM __advance_schema__.employee_identity_mappings m JOIN __advance_schema__.employees e ON e."Id"=m."EmployeeId"
           WHERE m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=NEW."UpdatedBy" AND m."IsActive" AND e."Status"='Active' AND e."LoginEnabled";
          SELECT count(*),min(m."EmployeeId") INTO creator_matches,creator_employee FROM __advance_schema__.employee_identity_mappings m
           WHERE m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=OLD."CreatedBy" AND m."IsActive";
          IF actor_matches<>1 THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_actor_binding',MESSAGE='Qualification actor must resolve to exactly one active employee.';
          END IF;
          IF __advance_schema__.rev869b_command_context_valid(
               NEW."OrganizationId",actor_employee,current_setting('__advance_schema__.rev869b_identity_issuer',true),
               NEW."UpdatedBy",current_setting('__advance_schema__.rev869b_actor_role',true)) IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_command_context',
              MESSAGE='Qualification lifecycle requires a protected server-issued command context.';
          END IF;

          legacy_normalization:=OLD."VerificationStatus"='Draft' AND OLD."ApprovalStatus"='Draft' AND
            OLD."VerifiedByEmployeeId" IS NULL AND OLD."ApprovedByEmployeeId" IS NULL;
          IF legacy_normalization THEN
            IF creator_matches<>0 OR NEW."Version"<>OLD."Version"+1 OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 OR
               NEW."CreatedBy" IS DISTINCT FROM NEW."UpdatedBy" OR
               NEW."VerificationStatus"<>'Pending Approval' OR NEW."ApprovalStatus"<>'Pending Approval' OR
               NEW."VerifiedByEmployeeId" IS NOT NULL OR NEW."ApprovedByEmployeeId" IS NOT NULL OR NOT NEW."IsActive" OR
               to_jsonb(NEW)-ARRAY['CreatedBy','VerificationStatus','ApprovalStatus','Version','UpdatedAt','UpdatedBy']
               IS DISTINCT FROM to_jsonb(OLD)-ARRAY['CreatedBy','VerificationStatus','ApprovalStatus','Version','UpdatedAt','UpdatedBy'] THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_legacy_normalization',
                MESSAGE='Only an actorless legacy Draft may be normalized once to Pending Approval by adopting the signed actor as creator.';
            END IF;
            SELECT count(*) INTO authorized_roles FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
             WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('accounts_head','technical_director')
               AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
               AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
            IF authorized_roles=0 THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_legacy_normalizer_binding',
                MESSAGE='Legacy qualification normalization requires an authorized signed employee.';
            END IF;
          ELSE
            IF NEW."Version"<>OLD."Version"+1 OR length(trim(coalesce(NEW."UpdatedBy",'')))=0 OR
               to_jsonb(NEW)-ARRAY['VerificationStatus','VerifiedByEmployeeId','ApprovalStatus','ApprovedByEmployeeId','IsActive','Version','UpdatedAt','UpdatedBy']
               IS DISTINCT FROM to_jsonb(OLD)-ARRAY['VerificationStatus','VerifiedByEmployeeId','ApprovalStatus','ApprovedByEmployeeId','IsActive','Version','UpdatedAt','UpdatedBy'] THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_lifecycle_allowlist',MESSAGE='Qualification lifecycle permits only exact versioned verifier/approver changes.';
            END IF;
            IF creator_matches<>1 OR actor_employee=creator_employee THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_actor_binding',MESSAGE='Qualification actor must differ from the uniquely mapped creator.';
            END IF;
            IF OLD."VerificationStatus"='Pending Approval' AND OLD."ApprovalStatus"='Pending Approval' AND
             NEW."VerificationStatus"='Verified' AND NEW."ApprovalStatus"='Pending Approval' THEN
              SELECT count(*) INTO authorized_roles FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
               WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('accounts_head','technical_director')
                 AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
                 AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
              IF OLD."VerifiedByEmployeeId" IS NOT NULL OR NEW."VerifiedByEmployeeId" IS DISTINCT FROM actor_employee OR
                  NEW."ApprovedByEmployeeId" IS DISTINCT FROM OLD."ApprovedByEmployeeId" OR authorized_roles=0 OR NOT NEW."IsActive" THEN
                RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_verifier_binding',MESSAGE='Qualification verification requires the exact independent employee.';
              END IF;
            ELSIF OLD."VerificationStatus"='Verified' AND OLD."ApprovalStatus"='Pending Approval' AND
                NEW."VerificationStatus"='Verified' AND NEW."ApprovalStatus"='Approved' THEN
              SELECT count(*) INTO authorized_roles FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
               WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
                 AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
                 AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
               IF OLD."VerifiedByEmployeeId" IS NULL OR NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
                  NEW."ApprovedByEmployeeId" IS DISTINCT FROM actor_employee OR actor_employee=OLD."VerifiedByEmployeeId" OR authorized_roles=0 OR NOT NEW."IsActive" THEN
                 RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_approver_binding',MESSAGE='Qualification approval requires an employee distinct from creator and verifier.';
               END IF;
            ELSIF OLD."ApprovalStatus"='Pending Approval' AND NEW."ApprovalStatus"='Rejected' AND
                NEW."VerificationStatus" IS NOT DISTINCT FROM OLD."VerificationStatus" THEN
              SELECT count(*) INTO authorized_roles FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
               WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
                 AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
                 AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
              IF NEW."ApprovedByEmployeeId" IS DISTINCT FROM actor_employee OR NEW."IsActive" OR authorized_roles=0 OR
                 NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
                 (OLD."VerifiedByEmployeeId" IS NOT NULL AND actor_employee=OLD."VerifiedByEmployeeId") THEN
                RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_rejector_binding',MESSAGE='Qualification rejection requires an independent approval-tier employee and deactivation.';
              END IF;
            ELSIF OLD."VerificationStatus" IN ('Verified','Approved') AND OLD."ApprovalStatus"='Approved' AND OLD."IsActive" AND
                NEW."VerificationStatus" IS NOT DISTINCT FROM OLD."VerificationStatus" AND NEW."ApprovalStatus"='Revision Requested' THEN
              SELECT count(*) INTO authorized_roles FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId"
               WHERE a."EmployeeId"=actor_employee AND r."IsActive" AND lower(r."Code") IN ('managing_director','technical_director')
                 AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=statement_timestamp()::date
                 AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=statement_timestamp()::date);
              IF NEW."VerifiedByEmployeeId" IS DISTINCT FROM OLD."VerifiedByEmployeeId" OR
                 NEW."ApprovedByEmployeeId" IS DISTINCT FROM OLD."ApprovedByEmployeeId" OR NEW."IsActive" OR
                 actor_employee=OLD."VerifiedByEmployeeId" OR authorized_roles=0 THEN
                RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_qualification_correction_binding',MESSAGE='Approved qualification correction requires an independent approval-tier employee and deactivation.';
              END IF;
            ELSE
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_qualification_transition',MESSAGE='Qualification permits only verify, approve, reject, or approved-record correction transitions.';
            END IF;
          END IF;
          NEW."UpdatedAt":=statement_timestamp();
          RETURN NEW;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_qualification_history_insert()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE actor_employee uuid;
        BEGIN
          IF NEW."EntityType"<>'VendorQualification' THEN RETURN NEW; END IF;
          actor_employee:=nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid;
          IF NEW."ActorLoginId" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_identity_subject',true) OR
             NEW."CreatedBy" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_identity_subject',true) OR
             NEW."ActorRoleCode" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_actor_role',true) OR
             __advance_schema__.rev869b_command_context_valid(NEW."OrganizationId",actor_employee,
               current_setting('__advance_schema__.rev869b_identity_issuer',true),
               current_setting('__advance_schema__.rev869b_identity_subject',true),
               current_setting('__advance_schema__.rev869b_actor_role',true)) IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE='42501',SCHEMA='__advance_schema__',TABLE='controlled_configuration_histories',
              CONSTRAINT='rev869b_qualification_history_actor_binding',
              MESSAGE='Qualification history must use the exact signed command principal.';
          END IF;
          NEW."CreatedAt":=transaction_timestamp(); NEW."UpdatedAt":=NULL; NEW."UpdatedBy":=NULL;
          RETURN NEW;
        END $rev869b$;

        DROP TRIGGER IF EXISTS trg_rev869b_qualification_history_insert_guard ON __advance_schema__.controlled_configuration_histories;
        CREATE TRIGGER trg_rev869b_qualification_history_insert_guard
          BEFORE INSERT ON __advance_schema__.controlled_configuration_histories
          FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_qualification_history_insert();

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_require_qualification_history()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE expected_action text; expected_employee uuid; expected_creator text; expected_correlation text;
          expected_history_id uuid; history_matches bigint; history_remarks text;
        BEGIN
          expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN OLD."VerificationStatus"='Draft' THEN 'Normalize'
            WHEN NEW."ApprovalStatus"='Rejected' THEN 'Reject' WHEN NEW."ApprovalStatus"='Revision Requested' THEN 'RequestCorrection'
            WHEN NEW."ApprovalStatus"='Approved' THEN 'Approve' ELSE 'Verify' END;
          expected_employee:=nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid;
          expected_creator:=CASE WHEN TG_OP='INSERT' THEN NEW."CreatedBy" ELSE NEW."UpdatedBy" END;
          expected_correlation:=format('REV869B|QUALIFICATION|%s|%s|%s',replace(NEW."Id"::text,'-',''),NEW."Version",upper(expected_action));
          SELECT count(*),min(h."Id"::text)::uuid,min(h."Remarks")
            INTO history_matches,expected_history_id,history_remarks
          FROM __advance_schema__.controlled_configuration_histories h
            JOIN __advance_schema__.employee_identity_mappings m ON m."OrganizationId"=NEW."OrganizationId" AND m."Subject"=h."ActorLoginId" AND m."EmployeeId"=expected_employee AND m."IsActive"
            WHERE h."EntityType"='VendorQualification' AND h."EntityId"=NEW."Id" AND h."OrganizationId"=NEW."OrganizationId"
              AND h."Action"=expected_action AND h."Version"=NEW."Version" AND h."CreatedBy"=expected_creator
              AND h."CorrelationId"=expected_correlation AND h."ActorRoleCode"=current_setting('__advance_schema__.rev869b_actor_role',true)
              AND h."CreatedAt"=transaction_timestamp() AND length(trim(h."Remarks"))>0
              AND ((expected_action='Create' AND h."BeforeJson" IS NULL AND h."AfterJson"->>'VerificationStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND (h."AfterJson"->>'Version')::bigint=0) OR
                   (expected_action='Normalize' AND h."BeforeJson"->>'VerificationStatus'='Draft' AND h."BeforeJson"->>'ApprovalStatus'='Draft' AND h."AfterJson"->>'VerificationStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'CreatedBy'=h."ActorLoginId" AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
                    (expected_action='Verify' AND h."BeforeJson"->>'VerificationStatus'='Pending Approval' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'VerificationStatus'='Verified' AND h."AfterJson"->>'ApprovalStatus'='Pending Approval' AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
                   (expected_action='Approve' AND h."BeforeJson"->>'VerificationStatus'='Verified' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'VerificationStatus'='Verified' AND h."AfterJson"->>'ApprovalStatus'='Approved' AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
                   (expected_action='Reject' AND h."BeforeJson"->>'ApprovalStatus'='Pending Approval' AND h."AfterJson"->>'ApprovalStatus'='Rejected' AND (h."AfterJson"->>'IsActive')::boolean IS FALSE AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version") OR
                   (expected_action='RequestCorrection' AND h."BeforeJson"->>'ApprovalStatus'='Approved' AND h."AfterJson"->>'ApprovalStatus'='Revision Requested' AND (h."AfterJson"->>'IsActive')::boolean IS FALSE AND (h."BeforeJson"->>'Version')::bigint=OLD."Version" AND (h."AfterJson"->>'Version')::bigint=NEW."Version"))
              AND h.xmin::text::bigint=txid_current();
          IF history_matches<>1 THEN
            RAISE EXCEPTION USING ERRCODE='23514',SCHEMA='__advance_schema__',TABLE='controlled_configuration_histories',
              CONSTRAINT='rev869b_qualification_requires_history',
              MESSAGE='Qualification lifecycle requires one exact same-transaction immutable history.';
          END IF;
          PERFORM __advance_schema__.rev869b_claim_command_context(
            'qualification_history',expected_history_id,'VendorQualification',NEW."Id",expected_action,
            CASE WHEN TG_OP='INSERT' THEN 0 ELSE OLD."Version" END,
            CASE WHEN TG_OP='INSERT' THEN NULL WHEN expected_action IN ('Approve','Reject','RequestCorrection') THEN OLD."ApprovalStatus" ELSE OLD."VerificationStatus" END,
            CASE WHEN expected_action IN ('Create','Normalize') THEN 'Pending Approval' WHEN expected_action='Verify' THEN 'Verified' WHEN expected_action='Reject' THEN 'Rejected' WHEN expected_action='RequestCorrection' THEN 'Revision Requested' ELSE 'Approved' END,
            expected_correlation,history_remarks);
          RETURN NULL;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE parent_status text;
        BEGIN
          IF current_setting('__advance_schema__.rev869b_actor_employee_id',true) IS NULL OR
             current_setting('__advance_schema__.rev869b_actor_login',true) IS NULL OR
             current_setting('__advance_schema__.rev869b_actor_role',true) IS NULL OR
             current_setting('__advance_schema__.rev869b_organization',true) IS NULL OR
             __advance_schema__.rev869b_command_context_valid(
               current_setting('__advance_schema__.rev869b_organization',true),
               nullif(current_setting('__advance_schema__.rev869b_actor_employee_id',true),'')::uuid,
               current_setting('__advance_schema__.rev869b_identity_issuer',true),
               current_setting('__advance_schema__.rev869b_identity_subject',true),
               current_setting('__advance_schema__.rev869b_actor_role',true)) IS NOT TRUE THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_context_required',MESSAGE='REV869B mutation requires an authenticated transaction-local command context.';
          END IF;
          IF TG_OP='INSERT' THEN
            IF NEW."CreatedBy" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_actor_login',true) THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_actor_binding',MESSAGE='Controlled INSERT actor must match command context.';
            END IF;
            IF NEW."Version"<>0 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_version_zero',MESSAGE=format('%s INSERT requires version zero.',TG_TABLE_NAME); END IF;
            IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND
               (length(trim(coalesce(NEW."TransitionCorrelationId",'')))=0 OR NEW."TransitionCorrelationId" IS DISTINCT FROM NEW."IdempotencyKey") THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_command_correlation',MESSAGE='Controlled aggregate INSERT must bind its initial command fingerprint.';
            ELSIF TG_TABLE_NAME IN ('quotation_technical_verifications','material_followup_handoffs') AND length(trim(coalesce(NEW."CorrelationId",'')))=0 THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_initial_command_correlation',MESSAGE='Controlled event INSERT must bind its command fingerprint.';
            END IF;
            IF TG_TABLE_NAME='purchase_transaction_approval_policies' THEN
              IF length(trim(coalesce(NEW."CreatedBy",'')))=0 OR NEW."EffectiveTo" IS NOT NULL AND NEW."EffectiveTo"<NEW."EffectiveFrom" THEN
                RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_policy_insert_contract',MESSAGE='Approval-policy INSERT requires an actor and valid effective dates.';
              END IF;
              NEW."CreatedAt":=statement_timestamp();
            END IF;
            RETURN NEW;
          END IF;
          IF NEW."Version"<>OLD."Version"+1 THEN RAISE EXCEPTION USING ERRCODE='40001',CONSTRAINT='rev869b_exact_version_increment',MESSAGE=format('%s UPDATE requires exact version +1.',TG_TABLE_NAME); END IF;
          IF NEW."UpdatedBy" IS DISTINCT FROM current_setting('__advance_schema__.rev869b_actor_login',true) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_command_actor_binding',MESSAGE='Controlled UPDATE actor must match command context.';
          END IF;
          IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND
             (length(trim(coalesce(NEW."TransitionCorrelationId",'')))=0 OR NEW."TransitionCorrelationId" IS NOT DISTINCT FROM OLD."TransitionCorrelationId") THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_command_correlation',MESSAGE='Every aggregate UPDATE requires a new exact command correlation.';
          END IF;
          IF TG_TABLE_NAME='request_for_quotations' AND
             to_jsonb(NEW)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_rfq_update_allowlist',MESSAGE='RFQ update altered a field outside its exact lifecycle allowlist.';
          ELSIF TG_TABLE_NAME='rfq_vendor_invitations' AND
             to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_invitation_update_allowlist',MESSAGE='RFQ invitation update altered qualification, provenance or another protected field.';
          ELSIF TG_TABLE_NAME='vendor_quotations' AND
             to_jsonb(NEW)-ARRAY['Status','IsCurrentRevision','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentRevision','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_quotation_update_allowlist',MESSAGE='Quotation update altered immutable commercial, revision or provenance facts.';
          ELSIF TG_TABLE_NAME='commercial_comparisons' AND NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested') AND
             to_jsonb(NEW)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['RecommendedVendorQuotationId','SelectedVendorId','TotalPayableValue','ApprovalRoute','SingleSourceJustification','RecommendationRemarks','Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_recommendation_allowlist',MESSAGE='Comparison recommendation altered a field outside the Draft/RevisionRequested correction boundary.';
          ELSIF TG_TABLE_NAME='commercial_comparisons' AND NOT (NEW."Status"='PendingApproval' AND OLD."Status" IN ('Draft','RevisionRequested')) AND
             to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_allowlist',MESSAGE='Comparison transition altered protected commercial, selection or provenance fields.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Issued' AND
             to_jsonb(NEW)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IssuedAt','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_issue_allowlist',MESSAGE='PO issue altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status"='Cancelled' AND
             to_jsonb(NEW)-ARRAY['Status','CancelledAt','CancellationReason','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','CancelledAt','CancellationReason','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_cancel_allowlist',MESSAGE='PO cancellation altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" IN ('Approved','Rejected','Superseded') AND
             to_jsonb(NEW)-ARRAY['Status','IsCurrentVersion','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','IsCurrentVersion','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_approval_allowlist',MESSAGE='PO approval/rejection/supersession altered a protected snapshot or lifecycle field.';
          ELSIF TG_TABLE_NAME='purchase_orders' AND NEW."Status" NOT IN ('Issued','Cancelled','Approved','Rejected','Superseded') AND
             to_jsonb(NEW)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['Status','TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_allowlist',MESSAGE='PO transition altered protected terms, snapshots, current-version or provenance fields.';
          END IF;
          IF TG_TABLE_NAME IN ('request_for_quotations','rfq_vendor_invitations','vendor_quotations','commercial_comparisons','purchase_orders') AND NEW."Status"=OLD."Status" AND
             to_jsonb(NEW)-ARRAY['TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['TransitionCorrelationId','Version','UpdatedAt','UpdatedBy'] THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_protected_fields',MESSAGE='Same-status reservation cannot alter controlled fields.';
          ELSIF TG_TABLE_NAME='commercial_comparison_lines' THEN
            SELECT c."Status" INTO STRICT parent_status FROM __advance_schema__.commercial_comparisons c WHERE c."Id"=NEW."CommercialComparisonId";
            IF parent_status NOT IN ('Draft','RevisionRequested') OR
               to_jsonb(NEW)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] IS DISTINCT FROM to_jsonb(OLD)-ARRAY['IsRecommended','RecommendationReason','TotalPayableValue','CommercialSnapshotJson','Version','UpdatedAt','UpdatedBy'] THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_line_editable_boundary',MESSAGE='Comparison line correction requires its exact editable parent and immutable provenance.';
            END IF;
          ELSIF TG_TABLE_NAME='material_followup_handoffs' THEN
            IF NEW."CorrelationId" IS NOT DISTINCT FROM OLD."CorrelationId" OR length(trim(coalesce(NEW."CorrelationId",'')))=0 OR
               (OLD."Status",NEW."Status") NOT IN (('PendingFollowUp','InProgress'),('InProgress','Completed')) OR
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


        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_write_policy_history() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE employee uuid; matches bigint; actor_role text; actor text:=coalesce(NEW."UpdatedBy",NEW."CreatedBy");
        BEGIN
          SELECT count(*),min(m."EmployeeId") INTO matches,employee FROM __advance_schema__.employee_identity_mappings m JOIN __advance_schema__.employees e ON e."Id"=m."EmployeeId"
           WHERE m."Subject"=actor AND m."OrganizationId"=NEW."OrganizationId" AND m."IsActive" AND e."Status"='Active' AND e."LoginEnabled";
          IF matches<>1 THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_identity',MESSAGE='Approval-policy actor must resolve to one active organization identity.'; END IF;
          SELECT min(r."Code") INTO actor_role FROM __advance_schema__.employee_role_assignments a JOIN __advance_schema__.roles r ON r."Id"=a."RoleId" AND r."IsActive" WHERE a."EmployeeId"=employee AND r."Code" IN ('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND a."ApprovalStatus" IN ('Approved','SeedApproved');
          IF actor_role IS NULL THEN RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='rev869b_policy_actor_role',MESSAGE='Approval-policy lifecycle requires an authorized active role.'; END IF;
          INSERT INTO __advance_schema__.controlled_configuration_histories ("Id","OrganizationId","EntityType","EntityId","Action","BeforeJson","AfterJson","ActorLoginId","ActorRoleCode","Remarks","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),NEW."OrganizationId",'PurchaseTransactionApprovalPolicy',NEW."Id",CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."IsActive" THEN 'Activate' ELSE 'Deactivate' END,CASE WHEN TG_OP='INSERT' THEN NULL::jsonb ELSE to_jsonb(OLD) END,to_jsonb(NEW),actor,actor_role,'Database-bound approval-policy change',format('REV869B|POLICY|%s|%s',NEW."Id",NEW."Version"),statement_timestamp(),actor,NEW."Version"); RETURN NEW;
        END $rev869b$;

        CREATE OR REPLACE FUNCTION __advance_schema__.rev869b_require_bound_history()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $rev869b$
        DECLARE entity_type text; old_status text; expected_action text; actor text; parent_correlation text; specialized bigint;
        BEGIN
          IF TG_TABLE_NAME='quotation_technical_verifications' THEN
            actor:=coalesce(NEW."UpdatedBy",NEW."CreatedBy");
            IF NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_status_history h WHERE h."EntityType"='TechnicalVerification' AND h."EntityId"=NEW."Id"
              AND h."FromStatus" IS NULL AND h."ToStatus"=NEW."ComplianceStatus" AND h."Action"='Verify' AND h."ActorLoginId"=actor
              AND h."CorrelationId"=NEW."CorrelationId" AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_technical_verification_requires_history',MESSAGE='Technical verification requires exact same-command status history.';
            END IF;
            RETURN NULL;
          END IF;
          IF TG_OP='INSERT' AND (
            (TG_TABLE_NAME='request_for_quotations' AND EXISTS (SELECT 1 FROM __advance_schema__.request_for_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='rfq_vendor_invitations' AND EXISTS (SELECT 1 FROM __advance_schema__.rfq_vendor_invitations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='vendor_quotations' AND EXISTS (SELECT 1 FROM __advance_schema__.vendor_quotations p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='commercial_comparisons' AND EXISTS (SELECT 1 FROM __advance_schema__.commercial_comparisons p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='purchase_orders' AND EXISTS (SELECT 1 FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version"))) OR
            (TG_TABLE_NAME='material_followup_handoffs' AND EXISTS (SELECT 1 FROM __advance_schema__.material_followup_handoffs p WHERE p."Id"=NEW."Id" AND (p."Status",p."Version") IS DISTINCT FROM (NEW."Status",NEW."Version")))
          ) THEN RETURN NULL; END IF;
          IF NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_status_history h WHERE h."EntityId"=NEW."Id" AND h."ToStatus"=NEW."Status" AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_requires_history',MESSAGE='Every controlled parent transition requires same-transaction status history.';
          END IF;
          actor:=coalesce(NEW."UpdatedBy",NEW."CreatedBy"); old_status:=CASE WHEN TG_OP='UPDATE' THEN OLD."Status" ELSE NULL END;
          IF TG_OP='UPDATE' AND NEW."Status" IS NOT DISTINCT FROM OLD."Status" THEN
            parent_correlation:=NEW."TransitionCorrelationId";
            IF TG_TABLE_NAME='request_for_quotations' THEN entity_type:='RFQ';
              SELECT h."Action" INTO expected_action FROM __advance_schema__.purchase_transaction_status_history h WHERE h."EntityType"=entity_type AND h."EntityId"=NEW."Id" AND h."Action" IN ('ReserveInvitation','ReserveComparison') AND h."CorrelationId"=NEW."TransitionCorrelationId" AND h.xmin::text::bigint=txid_current();
            ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN entity_type:='RFQInvitation'; expected_action:='ReserveQuotation';
            ELSIF TG_TABLE_NAME='vendor_quotations' THEN entity_type:='VendorQuotation'; expected_action:='ReserveTechnicalVerification';
            ELSIF TG_TABLE_NAME='commercial_comparisons' THEN entity_type:='CommercialComparison'; expected_action:='ReservePurchaseOrder';
            ELSIF TG_TABLE_NAME='purchase_orders' THEN entity_type:='PurchaseOrder'; expected_action:='ReserveAmendment';
            ELSE RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_update_forbidden',MESSAGE='This aggregate has no controlled same-status mutation.';
            END IF;
            IF expected_action IS NULL THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_same_status_history_action',MESSAGE='Same-status mutation requires its exact reservation action.'; END IF;
          ELSIF TG_TABLE_NAME='request_for_quotations' THEN entity_type:='RFQ'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Closed' THEN 'Close' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='rfq_vendor_invitations' THEN entity_type:='RFQInvitation'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'InviteVendor' WHEN NEW."Status"='Submitted' THEN 'Submit' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='vendor_quotations' THEN entity_type:='VendorQuotation'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' AND NEW."Status"='Draft' THEN 'Create' WHEN NEW."Status"='Submitted' THEN CASE WHEN NEW."PreviousRevisionId" IS NULL THEN 'Submit' ELSE 'Revise' END WHEN NEW."Status"='TechnicallyCompliant' THEN 'Verify' WHEN NEW."Status"='TechnicallyRejected' THEN 'RejectTechnical' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Withdrawn' THEN 'Withdraw' WHEN NEW."Status"='Rejected' THEN 'Reject' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='commercial_comparisons' THEN entity_type:='CommercialComparison'; parent_correlation:=NEW."TransitionCorrelationId"; expected_action:=CASE WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' AND OLD."Status"='Draft' THEN 'Recommend' WHEN NEW."Status"='PendingApproval' THEN 'Resubmit' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='RevisionRequested' THEN 'RequestRevision' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSIF TG_TABLE_NAME='purchase_orders' THEN
            entity_type:='PurchaseOrder';
            parent_correlation:=NEW."TransitionCorrelationId";
            IF TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN SELECT p."Status" INTO STRICT old_status FROM __advance_schema__.purchase_orders p WHERE p."Id"=NEW."PreviousVersionId"; END IF;
            expected_action:=CASE WHEN TG_OP='INSERT' AND NEW."Status"='RevisionDraft' THEN 'ReviseRejected' WHEN TG_OP='INSERT' AND NEW."PreviousVersionId" IS NOT NULL THEN 'Amend' WHEN TG_OP='INSERT' THEN 'Create' WHEN NEW."Status"='PendingApproval' THEN 'Submit' WHEN NEW."Status"='Resubmitted' THEN 'ResubmitRejected' WHEN NEW."Status"='Approved' THEN 'Approve' WHEN NEW."Status"='Rejected' THEN 'Reject' WHEN NEW."Status"='Issued' THEN 'Issue' WHEN NEW."Status"='Superseded' THEN 'Supersede' WHEN NEW."Status"='Cancelled' THEN 'Cancel' ELSE 'Transition' END;
          ELSE entity_type:='MaterialFollowUp'; parent_correlation:=NEW."CorrelationId"; expected_action:=CASE WHEN NEW."Status"='InProgress' THEN 'StartFollowUp' WHEN NEW."Status"='Completed' THEN 'CompleteFollowUp' ELSE 'Handoff' END;
          END IF;
          IF NOT EXISTS (SELECT 1 FROM __advance_schema__.purchase_transaction_status_history h WHERE h."EntityType"=entity_type AND h."EntityId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."CorrelationId"=parent_correlation AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current()) THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_transition_history_exactness',MESSAGE='Transition history from/to/action/actor/version does not match its parent mutation.';
          END IF;
          IF TG_TABLE_NAME='commercial_comparisons' AND expected_action IN ('Approve','Reject','RequestRevision','Resubmit') THEN
            SELECT count(*) INTO specialized FROM __advance_schema__.purchase_transaction_approval_history h WHERE h."CommercialComparisonId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM old_status AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
            IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_comparison_transition_requires_approval_history',MESSAGE='Comparison approval transition requires one exact same-transaction approval history.'; END IF;
          ELSIF TG_TABLE_NAME='purchase_orders' THEN
            SELECT count(*) INTO specialized FROM __advance_schema__.purchase_order_history h WHERE h."PurchaseOrderId"=NEW."Id" AND h."FromStatus" IS NOT DISTINCT FROM coalesce(old_status,'') AND h."ToStatus"=NEW."Status" AND h."Action"=expected_action AND h."ActorLoginId"=actor AND h."Version"=NEW."Version" AND h.xmin::text::bigint=txid_current();
            IF specialized<>1 THEN RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='rev869b_po_transition_requires_po_history',MESSAGE='Purchase-order transition requires one exact same-transaction PO history.'; END IF;
          END IF;
          RETURN NULL;
        END $rev869b$;

        CREATE TRIGGER trg_rev869b_explicit_rfq_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.request_for_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_invitation_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_quotation_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.vendor_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_comparison_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_comparison_line_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_po_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_followup_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_policy_mutation BEFORE INSERT OR UPDATE ON __advance_schema__.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_qualification_lifecycle BEFORE INSERT OR UPDATE OR DELETE ON __advance_schema__.vendor_qualifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_qualification_lifecycle();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_qualification_history AFTER INSERT OR UPDATE ON __advance_schema__.vendor_qualifications DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_qualification_history();
        CREATE TRIGGER trg_rev869b_explicit_rfq_line_insert BEFORE INSERT ON __advance_schema__.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_quotation_line_insert BEFORE INSERT ON __advance_schema__.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_technical_insert BEFORE INSERT ON __advance_schema__.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE TRIGGER trg_rev869b_explicit_po_line_insert BEFORE INSERT ON __advance_schema__.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_guard_explicit_mutation();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_rfq_history AFTER INSERT OR UPDATE ON __advance_schema__.request_for_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_invitation_history AFTER INSERT OR UPDATE ON __advance_schema__.rfq_vendor_invitations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_quotation_history AFTER INSERT OR UPDATE ON __advance_schema__.vendor_quotations DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_technical_history AFTER INSERT ON __advance_schema__.quotation_technical_verifications DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_comparison_history AFTER INSERT OR UPDATE ON __advance_schema__.commercial_comparisons DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_po_history AFTER INSERT OR UPDATE ON __advance_schema__.purchase_orders DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE CONSTRAINT TRIGGER trg_rev869b_bound_followup_history AFTER INSERT OR UPDATE ON __advance_schema__.material_followup_handoffs DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_require_bound_history();
        CREATE TRIGGER trg_rev869b_bound_policy_history AFTER INSERT OR UPDATE ON __advance_schema__.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_write_policy_history();

        CREATE TRIGGER trg_rev869b_delete_rfq BEFORE DELETE ON __advance_schema__.request_for_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_rfq_line BEFORE DELETE ON __advance_schema__.request_for_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_invitation BEFORE DELETE ON __advance_schema__.rfq_vendor_invitations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_quotation BEFORE DELETE ON __advance_schema__.vendor_quotations FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_quotation_line BEFORE DELETE ON __advance_schema__.vendor_quotation_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_technical BEFORE DELETE ON __advance_schema__.quotation_technical_verifications FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_comparison BEFORE DELETE ON __advance_schema__.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_comparison_line BEFORE DELETE ON __advance_schema__.commercial_comparison_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_approval_history BEFORE DELETE ON __advance_schema__.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po BEFORE DELETE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po_line BEFORE DELETE ON __advance_schema__.purchase_order_lines FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_po_history BEFORE DELETE ON __advance_schema__.purchase_order_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_followup BEFORE DELETE ON __advance_schema__.material_followup_handoffs FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_status_history BEFORE DELETE ON __advance_schema__.purchase_transaction_status_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        CREATE TRIGGER trg_rev869b_delete_policy BEFORE DELETE ON __advance_schema__.purchase_transaction_approval_policies FOR EACH ROW EXECUTE FUNCTION __advance_schema__.rev869b_reject_controlled_delete();
        """;

    public static string Remove => AdvanceSchemaSql.Expand(RemoveTemplate);
    private const string RemoveTemplate = """
        DROP TRIGGER IF EXISTS trg_rev869b_durable_audit_retention ON __advance_schema__.audit_logs;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_guard_durable_audit_retention();
        ALTER TABLE __advance_schema__.vendor_qualifications DROP CONSTRAINT IF EXISTS "CK_vendor_qualification_rev869b_lifecycle";
        DROP TRIGGER IF EXISTS trg_rev869b_qualification_history_insert_guard ON __advance_schema__.controlled_configuration_histories;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_require_qualification_history() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_guard_qualification_history_insert() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_guard_qualification_lifecycle() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_write_policy_history() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_require_bound_history() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_guard_explicit_mutation() CASCADE;
        DROP FUNCTION IF EXISTS __advance_schema__.rev869b_reject_controlled_delete() CASCADE;
        """;
}
