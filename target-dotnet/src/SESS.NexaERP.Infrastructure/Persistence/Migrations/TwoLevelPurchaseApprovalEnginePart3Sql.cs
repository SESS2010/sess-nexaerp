using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class TwoLevelPurchaseApprovalEnginePart3Sql
{
    internal static void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(SqlUp.Replace("__advance_schema__", DatabaseSchemas.Advance, StringComparison.Ordinal));
    internal static void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(SqlDown.Replace("__advance_schema__", DatabaseSchemas.Advance, StringComparison.Ordinal));

    private const string SqlUp = """
        CREATE OR REPLACE FUNCTION __advance_schema__.purchase_approval_document_guard()
        RETURNS trigger LANGUAGE plpgsql AS $guard$
        DECLARE oldj jsonb := to_jsonb(OLD); newj jsonb := to_jsonb(NEW);
        BEGIN
          IF oldj->>'CreatorEmployeeId'<>'00000000-0000-0000-0000-000000000000' AND newj->>'CreatorEmployeeId' <> oldj->>'CreatorEmployeeId' THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='purchase_approval_creator_immutable',MESSAGE='Approval creator employee is immutable.';
          END IF;
          IF (newj->>'ApprovalCycle')::int = (oldj->>'ApprovalCycle')::int THEN
            IF (oldj->>'ApprovalCycle')::int > 0 AND
               (newj->>'ApprovalRoute' <> oldj->>'ApprovalRoute' OR newj->'ApprovalWorkflowSnapshotJson' <> oldj->'ApprovalWorkflowSnapshotJson' OR
                newj->>'RequiredApprovalStepCount' <> oldj->>'RequiredApprovalStepCount') THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='purchase_approval_snapshot_immutable',MESSAGE='Workflow snapshot is immutable within an approval cycle.';
            END IF;
            IF (newj->>'CompletedApprovalStepCount')::int NOT IN ((oldj->>'CompletedApprovalStepCount')::int,(oldj->>'CompletedApprovalStepCount')::int+1) THEN
              RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='purchase_approval_step_order',MESSAGE='Approval progress may advance by one step only.';
            END IF;
          ELSIF (newj->>'ApprovalCycle')::int <> (oldj->>'ApprovalCycle')::int+1 OR (newj->>'CompletedApprovalStepCount')::int <> 0 THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='purchase_approval_cycle_reset',MESSAGE='Resubmission must start exactly one fresh cycle at step zero.';
          END IF;
          RETURN NEW;
        END $guard$;

        CREATE OR REPLACE FUNCTION __advance_schema__.purchase_approval_decision_guard()
        RETURNS trigger LANGUAGE plpgsql AS $guard$
        DECLARE parentj jsonb; prior_employee uuid; snapshot jsonb; parent_id uuid;
        BEGIN
          IF NEW."Action" NOT IN ('Approve','Reject','RequestRevision') THEN RETURN NEW; END IF;
          IF NEW."ActorRoleCode"='PURCHASE_MANAGER' OR NEW."ResolvedRoleCode"='PURCHASE_MANAGER' THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='purchase_manager_approval_denied',MESSAGE='PURCHASE_MANAGER cannot write an approval decision.';
          END IF;
          IF NEW."ResolvedEmployeeId" IS NULL OR NEW."ResolvedEmployeeId"='00000000-0000-0000-0000-000000000000'::uuid OR
             NEW."ResolvedEmployeeId"<>coalesce((to_jsonb(NEW)->>'ActorEmployeeId')::uuid,NEW."ResolvedEmployeeId") OR NEW."ResolvedRoleCode"<>NEW."ActorRoleCode" THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='purchase_approval_resolved_identity',MESSAGE='Resolved employee and role must exactly equal the actor.';
          END IF;
          parent_id := (to_jsonb(NEW)->>TG_ARGV[2])::uuid;
          EXECUTE format('SELECT to_jsonb(p) FROM __advance_schema__.%I p WHERE p."Id"=$1 FOR UPDATE',TG_ARGV[0]) INTO parentj USING parent_id;
          IF parentj IS NULL THEN RAISE EXCEPTION 'Approval parent is missing.'; END IF;
          snapshot := parentj->'ApprovalWorkflowSnapshotJson';
          IF NEW."ApprovalCycle"<>(parentj->>'ApprovalCycle')::int OR NEW."StepNumber" NOT IN ((parentj->>'CompletedApprovalStepCount')::int,(parentj->>'CompletedApprovalStepCount')::int+1) OR
             NEW."RequiredApprovalStepCount"<>(parentj->>'RequiredApprovalStepCount')::int OR NEW."ApprovalRoute"<>parentj->>'ApprovalRoute' OR
             NEW."SnapshotIdentity"<>snapshot->>'identity' THEN
            RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='purchase_approval_parent_state',MESSAGE='Decision does not match the locked parent cycle, next step, route or snapshot.';
          END IF;
          IF NEW."ResolvedEmployeeId"=(parentj->>'CreatorEmployeeId')::uuid THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='purchase_approval_self_approval',MESSAGE='Creator self-approval is prohibited.';
          END IF;
          IF NEW."ResolvedEmployeeId"<>(snapshot#>>ARRAY['steps',(NEW."StepNumber"-1)::text,'employeeId'])::uuid OR
             NEW."ResolvedRoleCode"<>(snapshot#>>ARRAY['steps',(NEW."StepNumber"-1)::text,'roleCode']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='purchase_approval_exact_step_actor',MESSAGE='Decision actor is not the immutable step employee and role.';
          END IF;
          IF NEW."StepNumber"=2 THEN
            EXECUTE format('SELECT h."ResolvedEmployeeId" FROM __advance_schema__.%I h WHERE h.%I=$1 AND h."ApprovalCycle"=$2 AND h."StepNumber"=1 AND h."Action"=''Approve''',TG_TABLE_NAME,TG_ARGV[2]) INTO prior_employee USING parent_id,NEW."ApprovalCycle";
            IF prior_employee IS NULL OR prior_employee=NEW."ResolvedEmployeeId" THEN
              RAISE EXCEPTION USING ERRCODE='42501',CONSTRAINT='purchase_approval_level_separation',MESSAGE='Level 2 employee must differ from the recorded level 1 employee.';
            END IF;
          END IF;
          RETURN NEW;
        END $guard$;

        CREATE TRIGGER purchase_requisition_approval_state_guard BEFORE UPDATE ON __advance_schema__.purchase_requisitions FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_document_guard();
        CREATE TRIGGER commercial_comparison_approval_state_guard BEFORE UPDATE ON __advance_schema__.commercial_comparisons FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_document_guard();
        CREATE TRIGGER purchase_order_approval_state_guard BEFORE UPDATE ON __advance_schema__.purchase_orders FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_document_guard();
        CREATE TRIGGER purchase_requisition_approval_decision_guard BEFORE INSERT ON __advance_schema__.purchase_requisition_approval_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_decision_guard('purchase_requisitions','','PurchaseRequisitionId');
        CREATE TRIGGER commercial_comparison_approval_decision_guard BEFORE INSERT ON __advance_schema__.purchase_transaction_approval_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_decision_guard('commercial_comparisons','','CommercialComparisonId');
        CREATE TRIGGER purchase_order_approval_decision_guard BEFORE INSERT ON __advance_schema__.purchase_order_history FOR EACH ROW EXECUTE FUNCTION __advance_schema__.purchase_approval_decision_guard('purchase_orders','','PurchaseOrderId');
        """;

    private const string SqlDown = """
        DROP TRIGGER IF EXISTS purchase_requisition_approval_decision_guard ON __advance_schema__.purchase_requisition_approval_history;
        DROP TRIGGER IF EXISTS commercial_comparison_approval_decision_guard ON __advance_schema__.purchase_transaction_approval_history;
        DROP TRIGGER IF EXISTS purchase_order_approval_decision_guard ON __advance_schema__.purchase_order_history;
        DROP TRIGGER IF EXISTS purchase_requisition_approval_state_guard ON __advance_schema__.purchase_requisitions;
        DROP TRIGGER IF EXISTS commercial_comparison_approval_state_guard ON __advance_schema__.commercial_comparisons;
        DROP TRIGGER IF EXISTS purchase_order_approval_state_guard ON __advance_schema__.purchase_orders;
        DROP FUNCTION IF EXISTS __advance_schema__.purchase_approval_decision_guard();
        DROP FUNCTION IF EXISTS __advance_schema__.purchase_approval_document_guard();
        """;
}
