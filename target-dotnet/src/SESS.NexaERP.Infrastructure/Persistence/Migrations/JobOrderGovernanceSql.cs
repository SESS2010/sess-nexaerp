namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class JobOrderGovernanceSql
{
    internal const string Preflight = """
        DO $preflight$
        DECLARE present_count integer;
        BEGIN
          SELECT count(*) INTO present_count FROM information_schema.columns
          WHERE table_schema='advance' AND table_name='job_orders' AND column_name IN
            ('CustomerPurchaseOrderId','CustomerPurchaseOrderLineId','MachineOrdinal','InitiatedByEmployeeId','InitiatedRoleAssignmentId','AccountsConfirmedByEmployeeId','ConfirmationIdempotencyKey');
          IF present_count<>0 OR to_regclass('advance.job_order_history') IS NOT NULL
             OR EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey"='production.job-orders') THEN
            RAISE EXCEPTION 'Governed Job Order installation is partial or already present; refusing to guess.';
          END IF;
        END $preflight$;
        """;

    internal const string Up = """
        ALTER TABLE advance.job_orders DROP CONSTRAINT "CK_job_order_lifecycle";
        ALTER TABLE advance.job_orders ADD CONSTRAINT "CK_job_order_lifecycle"
          CHECK ("Status" IN ('DRAFT','PENDING_ACCOUNTS','OPEN','INSTALLED','CLOSED')
            AND ("Status" NOT IN ('INSTALLED','CLOSED') OR "InstallationDate" IS NOT NULL)
            AND (("Status"='CLOSED')=("ClosedAt" IS NOT NULL)));

        CREATE FUNCTION advance.guard_governed_job_order()
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
          IF TG_OP='UPDATE' AND (NEW."CustomerPurchaseOrderId",NEW."CustomerPurchaseOrderLineId",NEW."MachineOrdinal",NEW."MachineModel",NEW."MachineSerial",NEW."CustomerName",NEW."JobOrderDate",NEW."InitiatedByEmployeeId",NEW."InitiatedActorRoleCode",NEW."InitiatedRoleAssignmentId",NEW."InitiatedRoleAssignmentType")
             IS DISTINCT FROM (OLD."CustomerPurchaseOrderId",OLD."CustomerPurchaseOrderLineId",OLD."MachineOrdinal",OLD."MachineModel",OLD."MachineSerial",OLD."CustomerName",OLD."JobOrderDate",OLD."InitiatedByEmployeeId",OLD."InitiatedActorRoleCode",OLD."InitiatedRoleAssignmentId",OLD."InitiatedRoleAssignmentType") THEN
            RAISE EXCEPTION 'Job Order machine and Production initiation evidence is immutable.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_governed_job_order BEFORE INSERT OR UPDATE ON advance.job_orders FOR EACH ROW EXECUTE FUNCTION advance.guard_governed_job_order();

        CREATE FUNCTION advance.guard_job_order_history()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Job Order history is immutable.'; END IF;
          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=NEW."CompanyId"),NEW."ActorEmployeeId",current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode")
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(NEW."ActorEmployeeId",NEW."CompanyId",current_date,lower(NEW."Action"),ARRAY[NEW."ActorRoleCode"]) r WHERE r."AssignmentId"=NEW."ResolvedRoleAssignmentId" AND r."AssignmentType"=NEW."ResolvedRoleAssignmentType") THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Job Order history requires the current ordinary-command actor and assignment.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_job_order_history BEFORE INSERT OR UPDATE OR DELETE ON advance.job_order_history FOR EACH ROW EXECUTE FUNCTION advance.guard_job_order_history();

        DO $page$ DECLARE page_id uuid; BEGIN
          INSERT INTO advance.page_definitions ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES(md5('production.job-orders')::uuid,'production.job-orders','Production','Job Orders','/production/job-orders',true,now(),'GovernedJobOrderCreationWorkflow',0) RETURNING "Id" INTO page_id;
          INSERT INTO advance.role_page_permissions ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version")
          SELECT md5('production.job-orders:'||r."Id")::uuid,r."Id",page_id,true,r."Code" IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER'),false,false,false,r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER'),false,false,false,false,false,false,false,false,false,false,false,false,false,true,false,now(),'GovernedJobOrderCreationWorkflow',0
          FROM advance.roles r WHERE r."Code" IN ('PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER') AND r."IsActive";
          IF (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='GovernedJobOrderCreationWorkflow')<>4 THEN RAISE EXCEPTION 'Job Order workflow requires exactly four Production/Accounts grants.'; END IF;
        END $page$;
        REVOKE ALL ON FUNCTION advance.guard_governed_job_order(),advance.guard_job_order_history() FROM PUBLIC;
        DO $owner$ BEGIN IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN ALTER FUNCTION advance.guard_governed_job_order() OWNER TO nexa_erp_owner; ALTER FUNCTION advance.guard_job_order_history() OWNER TO nexa_erp_owner; END IF; END $owner$;
        """;

    internal const string Down = """
        DO $guard$ BEGIN
          IF EXISTS (SELECT 1 FROM advance.job_orders WHERE "CustomerPurchaseOrderId" IS NOT NULL) OR EXISTS (SELECT 1 FROM advance.job_order_history) THEN
            RAISE EXCEPTION 'Job Order governance rollback refuses governed business or history rows.';
          END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_job_order_history ON advance.job_order_history;
        DROP TRIGGER IF EXISTS trg_governed_job_order ON advance.job_orders;
        DROP FUNCTION IF EXISTS advance.guard_job_order_history();
        DROP FUNCTION IF EXISTS advance.guard_governed_job_order();
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='GovernedJobOrderCreationWorkflow';
        DELETE FROM advance.page_definitions WHERE "CreatedBy"='GovernedJobOrderCreationWorkflow';
        ALTER TABLE advance.job_orders DROP CONSTRAINT "CK_job_order_lifecycle";
        ALTER TABLE advance.job_orders ADD CONSTRAINT "CK_job_order_lifecycle"
          CHECK ("Status" IN ('DRAFT','OPEN','INSTALLED','CLOSED')
            AND ("Status" NOT IN ('INSTALLED','CLOSED') OR "InstallationDate" IS NOT NULL)
            AND (("Status"='CLOSED')=("ClosedAt" IS NOT NULL)));
        """;
}
