using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    public partial class JobOrderAccountsReturnAndResubmission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(GovernanceSql(enableRecovery: true));
            migrationBuilder.DropCheckConstraint(
                name: "CK_job_order_joint_governance", schema: "advance", table: "job_orders");
            migrationBuilder.AddCheckConstraint(
                name: "CK_job_order_joint_governance", schema: "advance", table: "job_orders",
                sql: "(\"CustomerPurchaseOrderId\" IS NULL AND \"CustomerPurchaseOrderLineId\" IS NULL AND \"MachineOrdinal\" IS NULL AND \"InitiatedByEmployeeId\" IS NULL AND \"InitiatedRoleAssignmentId\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL) OR (\"CustomerPurchaseOrderId\" IS NOT NULL AND \"CustomerPurchaseOrderLineId\" IS NOT NULL AND \"MachineOrdinal\">0 AND \"InitiatedByEmployeeId\" IS NOT NULL AND \"InitiatedActorRoleCode\" IS NOT NULL AND \"InitiatedRoleAssignmentId\" IS NOT NULL AND \"InitiatedRoleAssignmentType\" IS NOT NULL AND ((\"Status\" IN ('DRAFT','PENDING_ACCOUNTS') AND \"AccountsConfirmedAt\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NULL) OR (\"Status\"='OPEN' AND \"AccountsConfirmedAt\" IS NOT NULL AND \"AccountsConfirmedByEmployeeId\" IS NOT NULL AND \"AccountsConfirmationActorRoleCode\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentType\" IS NOT NULL AND NULLIF(btrim(\"AccountsConfirmationReason\"),'') IS NOT NULL)))");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(GovernanceSql(enableRecovery: false));
            migrationBuilder.DropCheckConstraint(
                name: "CK_job_order_joint_governance", schema: "advance", table: "job_orders");
            migrationBuilder.AddCheckConstraint(
                name: "CK_job_order_joint_governance", schema: "advance", table: "job_orders",
                sql: "(\"CustomerPurchaseOrderId\" IS NULL AND \"CustomerPurchaseOrderLineId\" IS NULL AND \"MachineOrdinal\" IS NULL AND \"InitiatedByEmployeeId\" IS NULL AND \"InitiatedRoleAssignmentId\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL) OR (\"CustomerPurchaseOrderId\" IS NOT NULL AND \"CustomerPurchaseOrderLineId\" IS NOT NULL AND \"MachineOrdinal\">0 AND \"InitiatedByEmployeeId\" IS NOT NULL AND \"InitiatedActorRoleCode\" IS NOT NULL AND \"InitiatedRoleAssignmentId\" IS NOT NULL AND \"InitiatedRoleAssignmentType\" IS NOT NULL AND ((\"Status\"='PENDING_ACCOUNTS' AND \"AccountsConfirmedAt\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NULL) OR (\"Status\"='OPEN' AND \"AccountsConfirmedAt\" IS NOT NULL AND \"AccountsConfirmedByEmployeeId\" IS NOT NULL AND \"AccountsConfirmationActorRoleCode\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentType\" IS NOT NULL AND NULLIF(btrim(\"AccountsConfirmationReason\"),'') IS NOT NULL)))");
        }

        private static string GovernanceSql(bool enableRecovery)
        {
            var productionExpected = enableRecovery ? "NOT p.\"CanUpdate\" AND NOT p.\"CanSubmit\"" : "p.\"CanUpdate\" AND p.\"CanSubmit\"";
            var accountsExpected = enableRecovery ? "NOT p.\"CanReject\"" : "p.\"CanReject\"";
            var productionNext = enableRecovery ? "TRUE" : "FALSE";
            var accountsNext = enableRecovery ? "TRUE" : "FALSE";
            var machineMetadataInImmutableTuple = enableRecovery ? string.Empty : ",NEW.\"MachineSerial\",NEW.\"JobOrderDate\"";
            var oldMachineMetadataInImmutableTuple = enableRecovery ? string.Empty : ",OLD.\"MachineSerial\",OLD.\"JobOrderDate\"";
            var draftMetadataGuard = enableRecovery ? """
              IF TG_OP='UPDATE' AND (NEW."MachineSerial",NEW."JobOrderDate",NEW."PlannedCompletionDate")
                 IS DISTINCT FROM (OLD."MachineSerial",OLD."JobOrderDate",OLD."PlannedCompletionDate")
                 AND NOT (OLD."Status"='DRAFT' AND NEW."Status"='DRAFT') THEN
                RAISE EXCEPTION 'Job Order serial and schedule metadata change only while it remains Draft.';
              END IF;
            """ : string.Empty;
            var recoveryHistoryGuard = enableRecovery ? """
              IF NEW."Action"='RETURN_TO_DRAFT' AND
                 (NEW."ActorRoleCode" NOT IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
                  OR NEW."ResolvedRoleAssignmentType"='SUPPORT') THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Returning a Job Order to Draft requires FULL or TEMPORARY Accounts authority.';
              END IF;
              IF NEW."Action" IN ('REVISE_DRAFT','RESUBMIT')
                 AND NEW."ActorRoleCode" NOT IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER') THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Job Order Draft correction and resubmission require Production authority.';
              END IF;
            """ : string.Empty;
            var rollbackGuard = enableRecovery ? string.Empty : """
              IF EXISTS (SELECT 1 FROM advance.job_orders
                         WHERE "CustomerPurchaseOrderId" IS NOT NULL AND "Status"='DRAFT')
                 OR EXISTS (SELECT 1 FROM advance.job_order_history
                            WHERE "Action" IN ('RETURN_TO_DRAFT','REVISE_DRAFT','RESUBMIT')) THEN
                RAISE EXCEPTION 'Refusing rollback: Job Order recovery state or immutable evidence exists.';
              END IF;
            """;
            return $"""
            DO $guard$
            DECLARE production_grants integer; accounts_grants integer;
            BEGIN
              IF to_regclass('advance.job_order_history') IS NULL
                 OR to_regprocedure('advance.guard_governed_job_order()') IS NULL
                 OR to_regprocedure('advance.guard_job_order_history()') IS NULL THEN
                RAISE EXCEPTION 'Governed Job Order installation is absent or partial.';
              END IF;
              SELECT count(*) INTO production_grants FROM advance.role_page_permissions p
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId"
              WHERE d."PageKey"='production.job-orders'
                AND r."Code" IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER')
                AND p."CanCreate" AND {productionExpected};
              SELECT count(*) INTO accounts_grants FROM advance.role_page_permissions p
              JOIN advance.page_definitions d ON d."Id"=p."PageDefinitionId"
              JOIN advance.roles r ON r."Id"=p."RoleId"
              WHERE d."PageKey"='production.job-orders'
                AND r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
                AND p."CanVerify" AND {accountsExpected};
              IF production_grants<>2 OR accounts_grants<>2 THEN
                RAISE EXCEPTION 'Expected exactly two Production and two Accounts Job Order grants at the predecessor contract.';
              END IF;
              {rollbackGuard}
            END $guard$;

            UPDATE advance.role_page_permissions p
              SET "CanUpdate"={productionNext},"CanSubmit"={productionNext},
                  "UpdatedAt"=now(),"UpdatedBy"='JobOrderAccountsReturnAndResubmission',"Version"=p."Version"+1
            FROM advance.page_definitions d,advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId"
              AND d."PageKey"='production.job-orders'
              AND r."Code" IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER');
            UPDATE advance.role_page_permissions p
              SET "CanReject"={accountsNext},"UpdatedAt"=now(),
                  "UpdatedBy"='JobOrderAccountsReturnAndResubmission',"Version"=p."Version"+1
            FROM advance.page_definitions d,advance.roles r
            WHERE d."Id"=p."PageDefinitionId" AND r."Id"=p."RoleId"
              AND d."PageKey"='production.job-orders'
              AND r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER');

            CREATE OR REPLACE FUNCTION advance.guard_governed_job_order()
            RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
            DECLARE po_company uuid; po_revision integer; line_po uuid; line_revision integer; line_quantity numeric;
            BEGIN
              IF NEW."CustomerPurchaseOrderId" IS NULL THEN RETURN NEW; END IF;
              SELECT "CompanyId","CurrentRevisionNumber" INTO po_company,po_revision FROM advance.customer_purchase_orders WHERE "Id"=NEW."CustomerPurchaseOrderId";
              SELECT "CustomerPurchaseOrderId","RevisionNumber","Quantity" INTO line_po,line_revision,line_quantity FROM advance.customer_purchase_order_lines WHERE "Id"=NEW."CustomerPurchaseOrderLineId";
              IF po_company IS DISTINCT FROM NEW."CompanyId" OR line_po IS DISTINCT FROM NEW."CustomerPurchaseOrderId" OR line_revision IS DISTINCT FROM po_revision THEN
                RAISE EXCEPTION 'Job Order must reference a current Customer PO line in the same company.';
              END IF;
              IF line_quantity IS NULL OR line_quantity<=0 OR line_quantity<>trunc(line_quantity) OR NEW."MachineOrdinal">line_quantity THEN
                RAISE EXCEPTION 'Job Order machine ordinal must identify one unit within the whole-number Customer PO line quantity.';
              END IF;
              IF TG_OP='INSERT' AND (NEW."InitiatedActorRoleCode" NOT IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER')
                 OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(NEW."InitiatedByEmployeeId",NEW."CompanyId",current_date,'create',ARRAY[NEW."InitiatedActorRoleCode"]) r WHERE r."AssignmentId"=NEW."InitiatedRoleAssignmentId" AND r."AssignmentType"=NEW."InitiatedRoleAssignmentType")) THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Job Order initiation requires its effective Production assignment.';
              END IF;
              IF NEW."Status"='OPEN' AND (NEW."AccountsConfirmationActorRoleCode" NOT IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
                 OR NEW."AccountsConfirmedByEmployeeId"=NEW."InitiatedByEmployeeId"
                 OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(NEW."AccountsConfirmedByEmployeeId",NEW."CompanyId",NEW."AccountsConfirmedAt"::date,'verify',ARRAY[NEW."AccountsConfirmationActorRoleCode"]) r WHERE r."AssignmentId"=NEW."AccountsConfirmationRoleAssignmentId" AND r."AssignmentType"=NEW."AccountsConfirmationRoleAssignmentType")) THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Job Order activation requires a different employee with an effective Accounts assignment.';
              END IF;
              IF TG_OP='UPDATE' AND (NEW."CustomerPurchaseOrderId",NEW."CustomerPurchaseOrderLineId",NEW."MachineOrdinal",NEW."MachineModel",NEW."CustomerName",NEW."InitiatedByEmployeeId",NEW."InitiatedActorRoleCode",NEW."InitiatedRoleAssignmentId",NEW."InitiatedRoleAssignmentType"{machineMetadataInImmutableTuple})
                 IS DISTINCT FROM (OLD."CustomerPurchaseOrderId",OLD."CustomerPurchaseOrderLineId",OLD."MachineOrdinal",OLD."MachineModel",OLD."CustomerName",OLD."InitiatedByEmployeeId",OLD."InitiatedActorRoleCode",OLD."InitiatedRoleAssignmentId",OLD."InitiatedRoleAssignmentType"{oldMachineMetadataInImmutableTuple}) THEN
                RAISE EXCEPTION 'Job Order machine and Production initiation evidence is immutable.';
              END IF;
              {draftMetadataGuard}
              RETURN NEW;
            END $function$;

            CREATE OR REPLACE FUNCTION advance.guard_job_order_history()
            RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
            BEGIN
              IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Job Order history is immutable.'; END IF;
              IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=NEW."CompanyId"),NEW."ActorEmployeeId",current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode")
                 OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(NEW."ActorEmployeeId",NEW."CompanyId",current_date,lower(NEW."Action"),ARRAY[NEW."ActorRoleCode"]) r WHERE r."AssignmentId"=NEW."ResolvedRoleAssignmentId" AND r."AssignmentType"=NEW."ResolvedRoleAssignmentType") THEN
                RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Job Order history requires the current ordinary-command actor and assignment.';
              END IF;
              {recoveryHistoryGuard}
              RETURN NEW;
            END $function$;
            """;
        }
    }
}