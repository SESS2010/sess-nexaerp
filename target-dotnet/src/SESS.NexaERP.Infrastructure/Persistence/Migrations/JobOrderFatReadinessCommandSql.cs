namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class JobOrderFatReadinessCommandSql
{
    internal const string Up = """
        CREATE FUNCTION advance.create_fat_custody_explanation(
          p_company uuid,p_job uuid,p_line uuid,p_quantity numeric,p_disposition text,p_reason text,
          p_key text,p_hash text,p_correlation text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("ExplanationId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE existing advance.job_order_fat_custody_explanations%ROWTYPE;
          remaining numeric; result_id uuid:=md5('fat-explanation:'||p_company||':'||p_key)::uuid;
        BEGIN
          IF p_quantity<=0 OR p_disposition NOT IN ('LOST','SCRAPPED') OR length(btrim(p_reason))=0
             OR length(btrim(p_key))=0 OR p_hash !~ '^[0-9a-f]{64}$' THEN
            RAISE EXCEPTION 'Invalid FAT custody explanation input.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('FAT:EXPLAIN:'||p_company||':'||p_line,0));
          SELECT * INTO existing FROM advance.job_order_fat_custody_explanations
            WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
          IF FOUND THEN
            IF existing."RequestFingerprint"<>p_hash OR existing."ExplainedByEmployeeId"<>p_actor
               OR existing."ResolvedRoleAssignmentId"<>p_assignment THEN
              RAISE EXCEPTION 'Idempotency key was reused with different FAT explanation content or authority.';
            END IF;
            RETURN QUERY SELECT existing."Id",true; RETURN;
          END IF;
          IF NOT advance.fat_authority_valid(p_company,p_actor,p_role,p_assignment,p_type,
              ARRAY['PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FAT custody explanation requires a currently effective FULL fitment role.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.job_orders j WHERE j."CompanyId"=p_company AND j."Id"=p_job AND j."Status"='OPEN') THEN
            RAISE EXCEPTION 'FAT custody explanation requires an OPEN Job Order.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.material_issue_lines l JOIN advance.material_issues i
              ON i."CompanyId"=l."CompanyId" AND i."Id"=l."MaterialIssueId"
              WHERE l."CompanyId"=p_company AND l."Id"=p_line AND i."JobOrderId"=p_job
                AND i."IssuedToEmployeeId"=p_actor) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Only the named material custodian may explain this FAT custody.';
          END IF;
          SELECT b."UnexplainedQuantityBase" INTO remaining FROM advance.fat_live_balances(p_company,p_job) b
            WHERE b."MaterialIssueLineId"=p_line;
          IF remaining IS NULL OR p_quantity>remaining THEN
            RAISE EXCEPTION 'FAT explanation quantity % exceeds unexplained custody %.',p_quantity,coalesce(remaining,0);
          END IF;
          INSERT INTO advance.job_order_fat_custody_explanations
            ("Id","JobOrderId","MaterialIssueLineId","QuantityBase","Disposition","Reason",
             "ExplainedByEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType",
             "IdempotencyKey","RequestFingerprint","CreatedAt","CreatedBy","Version","CompanyId")
          VALUES (result_id,p_job,p_line,p_quantity,p_disposition,btrim(p_reason),p_actor,p_role,p_assignment,p_type,
             btrim(p_key),p_hash,now(),p_login,0,p_company);
          INSERT INTO advance.job_order_history
            ("Id","JobOrderId","Action","FromStatus","ToStatus","ActorEmployeeId","ActorRoleCode",
             "ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CorrelationId","Remarks",
             "CreatedAt","CreatedBy","Version","CompanyId")
          VALUES (md5('fat-history:'||p_correlation)::uuid,p_job,'FAT_EXPLAIN',NULL,'EXPLAINED',p_actor,p_role,
             p_assignment,p_type,p_correlation,btrim(p_reason),now(),p_login,0,p_company);
          RETURN QUERY SELECT result_id,false;
        END $function$;

        CREATE FUNCTION advance.reconcile_job_order_fat(
          p_company uuid,p_job uuid,p_reason text,p_key text,p_hash text,p_correlation text,
          p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("ReconciliationId" uuid,"Result" text,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE existing advance.job_order_fat_reconciliations%ROWTYPE;
          result_id uuid:=md5('fat-reconciliation:'||p_company||':'||p_key)::uuid;
          attempt_no integer; result_code text; issued numeric; fitted numeric; returned numeric;
          explained numeric; unexplained numeric; old_status text;
        BEGIN
          IF length(btrim(p_reason))=0 OR length(btrim(p_key))=0 OR p_hash !~ '^[0-9a-f]{64}$' THEN
            RAISE EXCEPTION 'Invalid FAT reconciliation input.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('FAT:RECONCILE:'||p_company||':'||p_job,0));
          SELECT * INTO existing FROM advance.job_order_fat_reconciliations
            WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
          IF FOUND THEN
            IF existing."RequestFingerprint"<>p_hash OR existing."ReconciledByEmployeeId"<>p_actor
               OR existing."ResolvedRoleAssignmentId"<>p_assignment OR existing."JobOrderId"<>p_job THEN
              RAISE EXCEPTION 'Idempotency key was reused with different FAT reconciliation content or authority.';
            END IF;
            RETURN QUERY SELECT existing."Id",existing."Result"::text,true; RETURN;
          END IF;
          IF NOT advance.fat_authority_valid(p_company,p_actor,p_role,p_assignment,p_type,
              ARRAY['QC_MANAGER','DESIGN_ENGINEER']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FAT reconciliation requires a currently effective FULL QC_MANAGER or DESIGN_ENGINEER assignment.';
          END IF;
          SELECT j."FatReadinessStatus" INTO old_status FROM advance.job_orders j
            WHERE j."CompanyId"=p_company AND j."Id"=p_job AND j."Status"='OPEN' FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'FAT readiness requires an OPEN Job Order.'; END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.actual_boms b WHERE b."CompanyId"=p_company AND b."JobOrderId"=p_job) THEN
            RAISE EXCEPTION 'FAT readiness requires a generated Actual BOM from confirmed fitment.';
          END IF;
          SELECT count(*)+1 INTO attempt_no FROM advance.job_order_fat_reconciliations
            WHERE "CompanyId"=p_company AND "JobOrderId"=p_job;
          SELECT sum(b."IssuedQuantityBase"),sum(b."FittedQuantityBase"),sum(b."ReturnedQuantityBase"),
            sum(b."ExplainedLostQuantityBase"+b."ExplainedScrappedQuantityBase"),sum(b."UnexplainedQuantityBase")
            INTO issued,fitted,returned,explained,unexplained FROM advance.fat_live_balances(p_company,p_job) b;
          IF issued IS NULL THEN RAISE EXCEPTION 'FAT readiness requires issued material evidence.'; END IF;
          result_code:=CASE WHEN unexplained=0 THEN 'READY' ELSE 'BLOCKED' END;
          INSERT INTO advance.job_order_fat_reconciliations
            ("Id","JobOrderId","AttemptNumber","Result","IssuedQuantityBase","FittedQuantityBase",
             "ReturnedQuantityBase","ExplainedQuantityBase","UnexplainedQuantityBase","ReconciledAt",
             "ReconciledByEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType",
             "Reason","IdempotencyKey","RequestFingerprint","CreatedAt","CreatedBy","Version","CompanyId")
          VALUES (result_id,p_job,attempt_no,result_code,issued,fitted,returned,explained,unexplained,now(),
             p_actor,p_role,p_assignment,p_type,btrim(p_reason),btrim(p_key),p_hash,now(),p_login,0,p_company);
          INSERT INTO advance.job_order_fat_reconciliation_lines
            ("Id","CompanyId","JobOrderFatReconciliationId","MaterialIssueLineId","ItemId","ItemCodeSnapshot",
             "CustodianEmployeeId","CustodianEmployeeCodeSnapshot","IssuedQuantityBase","FittedQuantityBase",
             "ReturnedQuantityBase","ReturnedLateQuantityBase","ExplainedLostQuantityBase",
             "ExplainedScrappedQuantityBase","UnexplainedQuantityBase","Classification","CreatedAt","CreatedBy")
          SELECT md5('fat-line:'||result_id||':'||b."MaterialIssueLineId")::uuid,p_company,result_id,b."MaterialIssueLineId",
             l."ItemId",item."ItemCode",i."IssuedToEmployeeId",employee."EmployeeCode",b."IssuedQuantityBase",
             b."FittedQuantityBase",b."ReturnedQuantityBase",b."ReturnedLateQuantityBase",
             b."ExplainedLostQuantityBase",b."ExplainedScrappedQuantityBase",b."UnexplainedQuantityBase",
             CASE WHEN b."UnexplainedQuantityBase">0 THEN 'UNEXPLAINED'
                  WHEN ((b."FittedQuantityBase">0)::int+(b."ReturnedQuantityBase">0)::int+
                        ((b."ExplainedLostQuantityBase"+b."ExplainedScrappedQuantityBase")>0)::int)>1 THEN 'MIXED'
                  WHEN b."FittedQuantityBase"=b."IssuedQuantityBase" THEN 'FITTED'
                  WHEN b."ReturnedQuantityBase"=b."IssuedQuantityBase" AND b."ReturnedLateQuantityBase">0 THEN 'RETURNED_LATE'
                  WHEN b."ReturnedQuantityBase"=b."IssuedQuantityBase" THEN 'RETURNED'
                  ELSE 'EXPLAINED' END,now(),p_login
          FROM advance.fat_live_balances(p_company,p_job) b
          JOIN advance.material_issue_lines l ON l."CompanyId"=p_company AND l."Id"=b."MaterialIssueLineId"
          JOIN advance.items item ON item."Id"=l."ItemId"
          JOIN advance.material_issues i ON i."CompanyId"=l."CompanyId" AND i."Id"=l."MaterialIssueId"
          JOIN advance.employees employee ON employee."Id"=i."IssuedToEmployeeId";
          UPDATE advance.job_orders SET "FatReadinessStatus"=result_code,"FatReconciledAt"=now(),
            "FatReconciledByEmployeeId"=p_actor,"LatestFatReconciliationId"=result_id,
            "Version"="Version"+1,"UpdatedAt"=now(),"UpdatedBy"=p_login
            WHERE "CompanyId"=p_company AND "Id"=p_job;
          INSERT INTO advance.job_order_history
            ("Id","JobOrderId","Action","FromStatus","ToStatus","ActorEmployeeId","ActorRoleCode",
             "ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CorrelationId","Remarks",
             "CreatedAt","CreatedBy","Version","CompanyId")
          VALUES (md5('fat-history:'||p_correlation)::uuid,p_job,'FAT_RECONCILE',old_status,'FAT_RESULT',p_actor,p_role,
             p_assignment,p_type,p_correlation,result_code||': '||btrim(p_reason),now(),p_login,0,p_company);
          RETURN QUERY SELECT result_id,result_code,false;
        END $function$;

        REVOKE ALL ON FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text),
          advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC;
        DO $roles$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            REVOKE ALL ON advance.job_order_fat_custody_explanations,advance.job_order_fat_reconciliations,
              advance.job_order_fat_reconciliation_lines FROM nexa_erp_runtime,nexa_erp_bootstrap,nexa_erp_migration;
            GRANT SELECT ON advance.job_order_fat_custody_explanations,advance.job_order_fat_reconciliations,
              advance.job_order_fat_reconciliation_lines TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text),
              advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text),
              advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DROP FUNCTION IF EXISTS advance.reconcile_job_order_fat(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.create_fat_custody_explanation(uuid,uuid,uuid,numeric,text,text,text,text,text,uuid,text,uuid,text,text);
        """;
}