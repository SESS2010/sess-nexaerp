using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>F24: only an exact current snapshotted/recorded comparison or PO actor may read that timeline.
/// Existing page grants, current role/company/scopes, warehouse and own-record limits remain mandatory.
/// No decision, update or issue authority is introduced.</summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20261017180000_WorkflowActorTrackingHistory")]
public sealed class WorkflowActorTrackingHistory : Migration
{
    private const string Signature = "advance.tracking_history(text,uuid,uuid[],text,text,uuid)";
    protected override void Up(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            CREATE TABLE advance.r1_workflow_tracking_history_backup(before_value text NOT NULL,after_value text NOT NULL);
            REVOKE ALL ON advance.r1_workflow_tracking_history_backup FROM PUBLIC;
            DO $rewrite$ DECLARE installed text; original text; BEGIN
              SELECT replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') INTO STRICT installed
                FROM pg_proc p JOIN pg_language l ON l.oid=p.prolang
                WHERE p.oid='advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure
                  AND p.prosecdef AND l.lanname='sql';
              original:=installed;
              IF (length(installed)-length(replace(installed,E'x."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.commercial_comparisons x JOIN access a','')))/length(E'x."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.commercial_comparisons x JOIN access a')<>1 THEN
                RAISE EXCEPTION 'Workflow history refuses unexpected 1 document projection.';
              END IF;
              installed:=replace(installed,E'x."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.commercial_comparisons x JOIN access a',E'x."OwnerEmployeeId",ARRAY(SELECT p_employee\n   WHERE EXISTS(SELECT 1 FROM scopes scope WHERE\n     (scope."WarehouseId" IS NULL OR scope."WarehouseId"=r."DeliveryWarehouseId")\n     AND scope."RackBinId" IS NULL AND(NOT scope."OwnRecordsOnly" OR x."OwnerEmployeeId"=p_employee))\n   AND((x."ApprovalCycle">0 AND EXISTS(\n     SELECT 1 FROM jsonb_array_elements(CASE WHEN jsonb_typeof(x."ApprovalWorkflowSnapshotJson"::jsonb->''steps'')=''array''\n       THEN x."ApprovalWorkflowSnapshotJson"::jsonb->''steps'' ELSE ''[]''::jsonb END) step\n     WHERE step->>''employeeId''=p_employee::text\n       AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=step->>''roleCode'')))\n   OR EXISTS(SELECT 1 FROM advance.purchase_transaction_approval_history history\n     WHERE history."CompanyId"=x."CompanyId" AND history."CommercialComparisonId"=x."Id" AND history."StepNumber">0\n       AND((history."ResolvedEmployeeId"=p_employee AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=history."ResolvedRoleCode"))\n         OR(history."ActorEmployeeId"=p_employee AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=history."ActorRoleCode"))))))\n FROM advance.commercial_comparisons x JOIN access a');
              IF (length(installed)-length(replace(installed,E'p."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.purchase_orders p JOIN access a','')))/length(E'p."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.purchase_orders p JOIN access a')<>1 THEN
                RAISE EXCEPTION 'Workflow history refuses unexpected 2 document projection.';
              END IF;
              installed:=replace(installed,E'p."OwnerEmployeeId",''{}''::uuid[]\n FROM advance.purchase_orders p JOIN access a',E'p."OwnerEmployeeId",ARRAY(SELECT p_employee\n   WHERE EXISTS(SELECT 1 FROM scopes scope WHERE\n     (scope."WarehouseId" IS NULL OR scope."WarehouseId"=p."DeliveryWarehouseId")\n     AND scope."RackBinId" IS NULL AND(NOT scope."OwnRecordsOnly" OR p."OwnerEmployeeId"=p_employee))\n   AND((p."ApprovalCycle">0 AND EXISTS(\n     SELECT 1 FROM jsonb_array_elements(CASE WHEN jsonb_typeof(p."ApprovalWorkflowSnapshotJson"::jsonb->''steps'')=''array''\n       THEN p."ApprovalWorkflowSnapshotJson"::jsonb->''steps'' ELSE ''[]''::jsonb END) step\n     WHERE step->>''employeeId''=p_employee::text\n       AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=step->>''roleCode'')))\n   OR EXISTS(SELECT 1 FROM advance.purchase_order_history history\n     WHERE history."CompanyId"=p."CompanyId" AND history."PurchaseOrderId"=p."Id" AND history."StepNumber">0\n       AND((history."ResolvedEmployeeId"=p_employee AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=history."ResolvedRoleCode"))\n         OR(history."ActorEmployeeId"=p_employee AND EXISTS(SELECT 1 FROM effective_roles role WHERE role."Code"=history."ActorRoleCode"))))))\n FROM advance.purchase_orders p JOIN access a');
              EXECUTE installed;
              INSERT INTO advance.r1_workflow_tracking_history_backup
                SELECT original,replace(pg_get_functiondef('advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure),E'\r\n',E'\n');
            END $rewrite$;
            """));
        // CREATE OR REPLACE preserves the existing SECURITY DEFINER signature, owner, search_path and runtime ACL.
        // No new function or installer provisioning signature is introduced.
    }

    protected override void Down(MigrationBuilder builder)
    {
        PostgreSqlClusterGuard.Require(builder);
        builder.Sql(MigrationText.Lf("""
            DO $restore$ DECLARE saved record; BEGIN
              IF (SELECT count(*) FROM advance.r1_workflow_tracking_history_backup)<>1 THEN
                RAISE EXCEPTION 'Workflow tracking rollback refuses missing or ambiguous retained definition.';
              END IF;
              SELECT * INTO STRICT saved FROM advance.r1_workflow_tracking_history_backup;
              IF replace(pg_get_functiondef('advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure),E'\r\n',E'\n')
                IS DISTINCT FROM saved.after_value THEN
                RAISE EXCEPTION 'Workflow tracking rollback refuses a changed installed definition.';
              END IF;
              EXECUTE saved.before_value;
            END $restore$;
            DROP TABLE advance.r1_workflow_tracking_history_backup;
            """));
    }
}
