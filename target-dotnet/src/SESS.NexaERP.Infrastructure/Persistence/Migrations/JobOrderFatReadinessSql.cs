namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class JobOrderFatReadinessSql
{
    internal const string Up = """
        DO $preflight$
        DECLARE found integer;
        BEGIN
          SELECT count(*) INTO found FROM pg_roles WHERE rolname IN
            ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF found NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; FAT readiness refuses to guess.';
          END IF;
          IF (SELECT count(*) FROM advance.roles WHERE "Code" IN
              ('PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER','QC_MANAGER','DESIGN_ENGINEER') AND "IsActive")<>6 THEN
            RAISE EXCEPTION 'FAT readiness requires all six confirmed active roles.';
          END IF;
        END $preflight$;

        CREATE FUNCTION advance.fat_authority_valid(
          p_company uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_allowed text[])
        RETURNS boolean LANGUAGE plpgsql STABLE SET search_path=pg_catalog,advance AS $function$
        DECLARE organization text;
        BEGIN
          SELECT "Code" INTO organization FROM advance.companies
            WHERE "Id"=p_company AND "IsActive" AND "Status"='ACTIVE';
          RETURN organization IS NOT NULL AND p_type='FULL' AND p_role=ANY(p_allowed)
            AND EXISTS (SELECT 1 FROM advance.employee_role_assignments a
              JOIN advance.roles r ON r."Id"=a."RoleId"
              JOIN advance.company_role_activations c ON c."CompanyId"=a."CompanyId" AND c."RoleId"=a."RoleId"
              WHERE a."Id"=p_assignment AND a."CompanyId"=p_company AND a."EmployeeId"=p_actor
                AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
                AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
                AND r."Code"=p_role AND r."IsActive" AND r."IsEmployeeAssignable"
                AND c."IsEnabled" AND c."EffectiveFrom"<=CURRENT_DATE
                AND (c."EffectiveTo" IS NULL OR c."EffectiveTo">=CURRENT_DATE))
            AND advance.ordinary_command_context_valid(organization,p_actor,
                current_setting('advance.ordinary_identity_issuer',true),
                current_setting('advance.ordinary_identity_subject',true),p_role);
        END $function$;

        CREATE FUNCTION advance.fat_live_balances(p_company uuid,p_job uuid)
        RETURNS TABLE(
          "MaterialIssueLineId" uuid,"IssuedQuantityBase" numeric,"FittedQuantityBase" numeric,
          "ReturnedQuantityBase" numeric,"ReturnedLateQuantityBase" numeric,
          "ExplainedLostQuantityBase" numeric,"ExplainedScrappedQuantityBase" numeric,
          "UnexplainedQuantityBase" numeric)
        LANGUAGE sql STABLE SET search_path=pg_catalog,advance AS $function$
          WITH issued AS (
            SELECT l."Id",l."QuantityBase",i."ReturnDueAt"
            FROM advance.material_issue_lines l
            JOIN advance.material_issues i ON i."CompanyId"=l."CompanyId" AND i."Id"=l."MaterialIssueId"
            WHERE l."CompanyId"=p_company AND i."JobOrderId"=p_job),
          fitted AS (
            SELECT f."MaterialIssueLineId",sum(f."QuantityBase") quantity
            FROM advance.component_fitments f
            WHERE f."CompanyId"=p_company
              AND NOT EXISTS (SELECT 1 FROM advance.component_fitment_reversals r
                WHERE r."CompanyId"=f."CompanyId" AND r."ComponentFitmentId"=f."Id")
            GROUP BY f."MaterialIssueLineId"),
          returned AS (
            SELECT l."MaterialIssueLineId",sum(l."ReturnedQuantityBase") quantity,
              sum(l."ReturnedQuantityBase") FILTER (WHERE r."AcceptedAt">i."ReturnDueAt") late_quantity
            FROM advance.material_return_lines l
            JOIN advance.material_returns r ON r."CompanyId"=l."CompanyId" AND r."Id"=l."MaterialReturnId" AND r."Status"='ACCEPTED'
            JOIN advance.material_issue_lines il ON il."CompanyId"=l."CompanyId" AND il."Id"=l."MaterialIssueLineId"
            JOIN advance.material_issues i ON i."CompanyId"=il."CompanyId" AND i."Id"=il."MaterialIssueId"
            WHERE l."CompanyId"=p_company GROUP BY l."MaterialIssueLineId"),
          explained AS (
            SELECT e."MaterialIssueLineId",
              sum(e."QuantityBase") FILTER (WHERE e."Disposition"='LOST') lost,
              sum(e."QuantityBase") FILTER (WHERE e."Disposition"='SCRAPPED') scrapped
            FROM advance.job_order_fat_custody_explanations e
            WHERE e."CompanyId"=p_company GROUP BY e."MaterialIssueLineId"),
          valueset AS (
            SELECT i."Id",i."QuantityBase" issued,coalesce(f.quantity,0) fitted,
              coalesce(r.quantity,0) returned,coalesce(r.late_quantity,0) returned_late,
              coalesce(e.lost,0) lost,coalesce(e.scrapped,0) scrapped
            FROM issued i LEFT JOIN fitted f ON f."MaterialIssueLineId"=i."Id"
            LEFT JOIN returned r ON r."MaterialIssueLineId"=i."Id"
            LEFT JOIN explained e ON e."MaterialIssueLineId"=i."Id")
          SELECT "Id",issued,fitted,returned,returned_late,lost,scrapped,
            greatest(issued-fitted-returned-lost-scrapped,0) FROM valueset;
        $function$;

        CREATE FUNCTION advance.guard_fat_evidence()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE actor uuid; role_code text; assignment uuid; assignment_type text;
          job_id uuid; remaining numeric;
        BEGIN
          IF TG_OP<>'INSERT' THEN
            RAISE EXCEPTION '% is immutable; FAT evidence is appended, never edited.',TG_TABLE_NAME;
          END IF;
          IF TG_TABLE_NAME='job_order_fat_custody_explanations' THEN
            actor:=NEW."ExplainedByEmployeeId"; role_code:=NEW."ActorRoleCode";
            assignment:=NEW."ResolvedRoleAssignmentId"; assignment_type:=NEW."ResolvedRoleAssignmentType";
            job_id:=NEW."JobOrderId";
            IF NOT advance.fat_authority_valid(NEW."CompanyId",actor,role_code,assignment,assignment_type,
                ARRAY['PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER']) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FAT custody explanation requires a currently effective FULL fitment role.';
            END IF;
            IF NOT EXISTS (SELECT 1 FROM advance.material_issue_lines l JOIN advance.material_issues i
                ON i."CompanyId"=l."CompanyId" AND i."Id"=l."MaterialIssueId"
                WHERE l."CompanyId"=NEW."CompanyId" AND l."Id"=NEW."MaterialIssueLineId"
                  AND i."JobOrderId"=NEW."JobOrderId" AND i."IssuedToEmployeeId"=actor) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Only the named material custodian may explain this FAT custody.';
            END IF;
            SELECT b."UnexplainedQuantityBase" INTO remaining FROM advance.fat_live_balances(NEW."CompanyId",job_id) b
              WHERE b."MaterialIssueLineId"=NEW."MaterialIssueLineId";
            IF remaining IS NULL OR NEW."QuantityBase">remaining THEN
              RAISE EXCEPTION 'FAT explanation quantity % exceeds unexplained custody %.',NEW."QuantityBase",coalesce(remaining,0);
            END IF;
          ELSIF TG_TABLE_NAME='job_order_fat_reconciliations' THEN
            actor:=NEW."ReconciledByEmployeeId"; role_code:=NEW."ActorRoleCode";
            assignment:=NEW."ResolvedRoleAssignmentId"; assignment_type:=NEW."ResolvedRoleAssignmentType";
            IF NOT advance.fat_authority_valid(NEW."CompanyId",actor,role_code,assignment,assignment_type,
                ARRAY['QC_MANAGER','DESIGN_ENGINEER']) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FAT reconciliation requires a currently effective FULL QC_MANAGER or DESIGN_ENGINEER assignment.';
            END IF;
          ELSE
            SELECT r."ReconciledByEmployeeId",r."ActorRoleCode",r."ResolvedRoleAssignmentId",
              r."ResolvedRoleAssignmentType",r."JobOrderId" INTO actor,role_code,assignment,assignment_type,job_id
            FROM advance.job_order_fat_reconciliations r
            WHERE r."CompanyId"=NEW."CompanyId" AND r."Id"=NEW."JobOrderFatReconciliationId";
            IF NOT advance.fat_authority_valid(NEW."CompanyId",actor,role_code,assignment,assignment_type,
                ARRAY['QC_MANAGER','DESIGN_ENGINEER']) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FAT reconciliation line has no valid reconciliation authority.';
            END IF;
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_fat_explanation_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.job_order_fat_custody_explanations FOR EACH ROW EXECUTE FUNCTION advance.guard_fat_evidence();
        CREATE TRIGGER trg_fat_reconciliation_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.job_order_fat_reconciliations FOR EACH ROW EXECUTE FUNCTION advance.guard_fat_evidence();
        CREATE TRIGGER trg_fat_reconciliation_line_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.job_order_fat_reconciliation_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_fat_evidence();

        CREATE FUNCTION advance.guard_job_order_fat_readiness()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE rec advance.job_order_fat_reconciliations%ROWTYPE;
        BEGIN
          IF NEW."FatReadinessStatus"='NOT_RECONCILED' THEN RETURN NEW; END IF;
          SELECT * INTO rec FROM advance.job_order_fat_reconciliations r
            WHERE r."CompanyId"=NEW."CompanyId" AND r."Id"=NEW."LatestFatReconciliationId"
              AND r."JobOrderId"=NEW."Id";
          IF NOT FOUND OR rec."Result"<>NEW."FatReadinessStatus"
             OR rec."ReconciledAt"<>NEW."FatReconciledAt"
             OR rec."ReconciledByEmployeeId"<>NEW."FatReconciledByEmployeeId" THEN
            RAISE EXCEPTION 'Job Order FAT state must reference its exact immutable reconciliation.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.job_order_fat_reconciliation_lines l
              WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id") THEN
            RAISE EXCEPTION 'FAT reconciliation requires issued-line evidence.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.fat_live_balances(NEW."CompanyId",NEW."Id") b
            FULL JOIN (
              SELECT snapshot.*
              FROM advance.job_order_fat_reconciliation_lines snapshot
              WHERE snapshot."CompanyId"=NEW."CompanyId"
                AND snapshot."JobOrderFatReconciliationId"=rec."Id") l
              ON l."MaterialIssueLineId"=b."MaterialIssueLineId"
            WHERE b."MaterialIssueLineId" IS NULL OR l."MaterialIssueLineId" IS NULL
              OR b."IssuedQuantityBase"<>l."IssuedQuantityBase"
              OR b."FittedQuantityBase"<>l."FittedQuantityBase"
              OR b."ReturnedQuantityBase"<>l."ReturnedQuantityBase"
              OR b."ReturnedLateQuantityBase"<>l."ReturnedLateQuantityBase"
              OR b."ExplainedLostQuantityBase"<>l."ExplainedLostQuantityBase"
              OR b."ExplainedScrappedQuantityBase"<>l."ExplainedScrappedQuantityBase"
              OR b."UnexplainedQuantityBase"<>l."UnexplainedQuantityBase") THEN
            RAISE EXCEPTION 'FAT reconciliation snapshot does not match live fitment, accepted return and custody evidence.';
          END IF;
          IF rec."IssuedQuantityBase"<>(SELECT sum(l."IssuedQuantityBase") FROM advance.job_order_fat_reconciliation_lines l WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id")
             OR rec."FittedQuantityBase"<>(SELECT sum(l."FittedQuantityBase") FROM advance.job_order_fat_reconciliation_lines l WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id")
             OR rec."ReturnedQuantityBase"<>(SELECT sum(l."ReturnedQuantityBase") FROM advance.job_order_fat_reconciliation_lines l WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id")
             OR rec."ExplainedQuantityBase"<>(SELECT sum(l."ExplainedLostQuantityBase"+l."ExplainedScrappedQuantityBase") FROM advance.job_order_fat_reconciliation_lines l WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id")
             OR rec."UnexplainedQuantityBase"<>(SELECT sum(l."UnexplainedQuantityBase") FROM advance.job_order_fat_reconciliation_lines l WHERE l."CompanyId"=NEW."CompanyId" AND l."JobOrderFatReconciliationId"=rec."Id") THEN
            RAISE EXCEPTION 'FAT reconciliation totals do not match immutable line evidence.';
          END IF;
          IF NEW."FatReadinessStatus"='READY' AND rec."UnexplainedQuantityBase"<>0 THEN
            RAISE EXCEPTION 'FAT cannot be READY while unexplained engineer custody remains.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE CONSTRAINT TRIGGER trg_job_order_fat_readiness
          AFTER INSERT OR UPDATE ON advance.job_orders DEFERRABLE INITIALLY DEFERRED
          FOR EACH ROW EXECUTE FUNCTION advance.guard_job_order_fat_readiness();

        DO $page$
        DECLARE page_id uuid:='52000000-0000-0000-0000-000000000010'; affected integer;
        BEGIN
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES (page_id,'production.fat-readiness','Production','FAT Readiness Reconciliation',
            '/production/fat-readiness',true,now(),'JobOrderFatReadiness',0);
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
          SELECT md5('fat-readiness-'||r."Id")::uuid,r."Id",page_id,true,
            r."Code" IN ('PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER'),
            false,false,false,r."Code" IN ('QC_MANAGER','DESIGN_ENGINEER'),false,false,false,false,false,
            false,false,false,false,false,false,false,false,true,false,now(),'JobOrderFatReadiness',0
          FROM advance.roles r WHERE r."Code" IN
            ('PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER','QC_MANAGER','DESIGN_ENGINEER');
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>6 THEN RAISE EXCEPTION 'Expected six FAT readiness page grants, found %.',affected; END IF;
        END $page$;

        REVOKE ALL ON advance.job_order_fat_custody_explanations,
          advance.job_order_fat_reconciliations,advance.job_order_fat_reconciliation_lines FROM PUBLIC;
        DO $roles$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.job_order_fat_custody_explanations OWNER TO nexa_erp_owner;
            ALTER TABLE advance.job_order_fat_reconciliations OWNER TO nexa_erp_owner;
            ALTER TABLE advance.job_order_fat_reconciliation_lines OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.fat_live_balances(uuid,uuid) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_fat_evidence() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_job_order_fat_readiness() OWNER TO nexa_erp_owner;
          END IF;
          REVOKE ALL ON FUNCTION advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]),
            advance.fat_live_balances(uuid,uuid),advance.guard_fat_evidence(),
            advance.guard_job_order_fat_readiness() FROM PUBLIC;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE ALL ON FUNCTION advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]),
              advance.fat_live_balances(uuid,uuid),advance.guard_fat_evidence(),
              advance.guard_job_order_fat_readiness()
              FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            REVOKE ALL ON advance.job_order_fat_custody_explanations,
              advance.job_order_fat_reconciliations,advance.job_order_fat_reconciliation_lines FROM nexa_erp_runtime;
            GRANT SELECT ON advance.job_order_fat_custody_explanations,
              advance.job_order_fat_reconciliations,advance.job_order_fat_reconciliation_lines TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DO $guard$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.job_order_fat_custody_explanations)
             OR EXISTS (SELECT 1 FROM advance.job_order_fat_reconciliations)
             OR EXISTS (SELECT 1 FROM advance.job_order_fat_reconciliation_lines)
             OR EXISTS (SELECT 1 FROM advance.job_orders WHERE "FatReadinessStatus"<>'NOT_RECONCILED') THEN
            RAISE EXCEPTION 'FAT readiness rollback refuses operational reconciliation evidence.';
          END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_job_order_fat_readiness ON advance.job_orders;
        DROP FUNCTION IF EXISTS advance.guard_job_order_fat_readiness();
        DROP TRIGGER IF EXISTS trg_fat_reconciliation_line_guard ON advance.job_order_fat_reconciliation_lines;
        DROP TRIGGER IF EXISTS trg_fat_reconciliation_guard ON advance.job_order_fat_reconciliations;
        DROP TRIGGER IF EXISTS trg_fat_explanation_guard ON advance.job_order_fat_custody_explanations;
        DROP FUNCTION IF EXISTS advance.guard_fat_evidence();
        DROP FUNCTION IF EXISTS advance.fat_live_balances(uuid,uuid);
        DROP FUNCTION IF EXISTS advance.fat_authority_valid(uuid,uuid,text,uuid,text,text[]);
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='JobOrderFatReadiness';
        DELETE FROM advance.page_definitions WHERE "CreatedBy"='JobOrderFatReadiness';
        """;
}