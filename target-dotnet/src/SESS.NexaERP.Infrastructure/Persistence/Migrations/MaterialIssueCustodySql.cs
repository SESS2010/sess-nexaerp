namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class MaterialIssueCustodySql
{
    internal static string ActiveGovernance
    {
        get
        {
            const string marker = "CREATE FUNCTION advance.guard_material_issue_governance()";
            const string endMarker = "CREATE TRIGGER trg_material_issue_guard";
            var start = Up.IndexOf(marker, StringComparison.Ordinal);
            var end = Up.IndexOf(endMarker, start, StringComparison.Ordinal);
            return Up[start..end].Replace(
                "CREATE FUNCTION advance.guard_material_issue_governance()",
                "CREATE OR REPLACE FUNCTION advance.guard_material_issue_governance()",
                StringComparison.Ordinal);
        }
    }

    internal const string Preflight = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Material Issue custody requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Material Issue custody refuses a PostgreSQL administrative database.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.customer_purchase_order_lines)
             OR EXISTS (SELECT 1 FROM advance.material_issue_request_lines) THEN
            RAISE EXCEPTION 'Item/UOM identity cannot be guessed for existing CPO or MIR lines; reconcile before migration.';
          END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN
             ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; refusing to guess grants or ownership.';
          END IF;
        END $guard$;
        """;

    internal const string Up = """
        -- Legacy seed assignments remain valid effective assignments. The two BOM guards
        -- must use the same Approved/SeedApproved vocabulary as the canonical resolver.
        CREATE OR REPLACE FUNCTION advance.guard_estimated_bom_governance()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE a record; organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN
            RAISE EXCEPTION 'Estimated BOM history and item merge evidence are immutable.';
          END IF;
          SELECT r."Code",e."AssignmentType" INTO a
          FROM advance.employee_role_assignments e JOIN advance.roles r ON r."Id"=e."RoleId"
          WHERE e."Id"=NEW."ResolvedRoleAssignmentId" AND e."EmployeeId"=NEW."ActorEmployeeId"
            AND e."CompanyId"=COALESCE(NEW."CompanyId",e."CompanyId")
            AND e."ApprovalStatus" IN ('Approved','SeedApproved')
            AND e."EffectiveFrom"<=CURRENT_DATE
            AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode"
             OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN
            RAISE EXCEPTION 'Resolved role assignment evidence does not match a currently effective assignment.';
          END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",
              current_setting('advance.ordinary_identity_issuer',true),
              current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN
            RAISE EXCEPTION 'Estimated BOM and item merge evidence requires the current ordinary command transaction.';
          END IF;
          IF TG_TABLE_NAME='estimated_bom_history' AND NEW."Action"='Approve'
             AND a."AssignmentType"='SUPPORT' THEN
            RAISE EXCEPTION 'SUPPORT authority cannot approve an Estimated BOM.';
          END IF;
          IF TG_TABLE_NAME='item_merge_aliases' AND
             (a."AssignmentType"='SUPPORT' OR a."Code" NOT IN ('STORES_MANAGER','PURCHASE_MANAGER')) THEN
            RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY STORES_MANAGER or PURCHASE_MANAGER authority.';
          END IF;
          RETURN NEW;
        END $function$;

        CREATE OR REPLACE FUNCTION advance.guard_production_engineering_history()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE a record; organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN RAISE EXCEPTION 'Production engineering history is immutable.'; END IF;
          SELECT r."Code",e."AssignmentType" INTO a
          FROM advance.employee_role_assignments e JOIN advance.roles r ON r."Id"=e."RoleId"
          WHERE e."Id"=NEW."ResolvedRoleAssignmentId" AND e."EmployeeId"=NEW."ActorEmployeeId"
            AND e."CompanyId"=NEW."CompanyId" AND e."ApprovalStatus" IN ('Approved','SeedApproved')
            AND e."EffectiveFrom"<=CURRENT_DATE
            AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode"
             OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN
            RAISE EXCEPTION 'Resolved role assignment evidence is not currently effective in this company.';
          END IF;
          IF NEW."Action"='Approve' AND a."AssignmentType"='SUPPORT' THEN
            RAISE EXCEPTION 'SUPPORT authority cannot approve Production engineering records.';
          END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",
              current_setting('advance.ordinary_identity_issuer',true),
              current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN
            RAISE EXCEPTION 'Production engineering evidence requires the current ordinary command transaction.';
          END IF;
          RETURN NEW;
        END $function$;

        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK (
            "OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL
            AND "InventoryProvenanceLayerId" IS NOT NULL
            AND "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL
            AND "WarehouseConditionLocationId" IS NOT NULL AND "ConditionCode" IS NOT NULL
            AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId",
              "InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
              "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")
              + CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL
                           AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END = 1
            AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL));

        ALTER TABLE advance.material_issue_requests DROP CONSTRAINT "CK_mir_lifecycle";
        ALTER TABLE advance.material_issue_requests ADD CONSTRAINT "CK_mir_lifecycle"
          CHECK ("Purpose" IN ('FACTORY_ASSEMBLY','PROJECT','SERVICE','WARRANTY','DEMO','SALE','FREE_OF_COST')
            AND "DestinationType" IN ('JOB_ORDER','CUSTOMER','VENDOR','DEPARTMENT','OTHER')
            AND "Status" IN ('DRAFT','SUBMITTED','APPROVED','REJECTED','CANCELLED','PARTIALLY_FULFILLED','FULFILLED','REVERSED')
            AND jsonb_typeof("ApprovalRouteSnapshotJson")='object');
        ALTER TABLE advance.material_issue_requests ADD CONSTRAINT "CK_mir_situation"
          CHECK (("Situation" IN ('CHAMBER_MANUFACTURE','SERVICE_CUSTOMER_PO','SITE_PROJECT_PO')
                   AND "DestinationType"='JOB_ORDER' AND "JobOrderId" IS NOT NULL)
              OR ("Situation"='CONSUMABLE_OFFICE' AND "DestinationType" IN ('DEPARTMENT','OTHER')
                   AND "JobOrderId" IS NULL));
        ALTER TABLE advance.material_issue_request_lines ADD CONSTRAINT "CK_mir_line_base_quantities"
          CHECK ("RequestedBaseQuantity">0 AND "EstimatedBomBaseQuantitySnapshot">=0
            AND "ProductionBomBaseQuantitySnapshot">=0 AND "CustomerPoBaseQuantitySnapshot">=0
            AND "ExcessBaseQuantitySnapshot">=0
            AND "ExcessClassification" IN ('NONE','CUSTOMER_FACING_EXCESS')
            AND (("ExcessBaseQuantitySnapshot"=0 AND "ExcessClassification"='NONE')
              OR ("ExcessBaseQuantitySnapshot">0 AND "ExcessClassification"='CUSTOMER_FACING_EXCESS')));
        ALTER TABLE advance.material_issues ADD CONSTRAINT "CK_material_issues_status"
          CHECK ("Status" IN ('ISSUED','PARTIALLY_RETURNED','RETURNED','REVERSED')
            AND "ReturnDueAt"="IssuedAt"+interval '1 day');
        ALTER TABLE advance.material_issue_lines ADD CONSTRAINT "CK_material_issue_lines_quantity"
          CHECK ("LineNumber">0 AND "QuantityBase">0
            AND "FromCustodyAssignmentId"<>"ToCustodyAssignmentId");
        ALTER TABLE advance.material_issue_excess_decisions ADD CONSTRAINT "CK_mir_excess_decision"
          CHECK ("Decision" IN ('APPROVED','REJECTED') AND length(btrim("Reason"))>0
            AND "ActorRoleCode"='TECHNICAL_DIRECTOR'
            AND "ResolvedRoleAssignmentType" IN ('FULL','TEMPORARY'));
        ALTER TABLE advance.material_issue_history ADD CONSTRAINT "CK_material_issue_history_source"
          CHECK ("MaterialIssueRequestId" IS NOT NULL
            AND ("MaterialIssueId" IS NULL OR "MaterialIssueRequestId" IS NOT NULL));

        CREATE FUNCTION advance.guard_material_issue_governance()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE request_row advance.material_issue_requests%ROWTYPE;
                assignment_row advance.employee_role_assignments%ROWTYPE;
                role_code text;
        BEGIN
          IF TG_TABLE_NAME IN ('material_issue_history','material_issue_lines') THEN
            IF TG_OP<>'INSERT' THEN RAISE EXCEPTION '% is immutable.',TG_TABLE_NAME; END IF;
            RETURN NEW;
          END IF;
          IF TG_TABLE_NAME='material_issue_excess_decisions' THEN
            IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Material issue excess decisions are immutable.'; END IF;
            SELECT a.* INTO assignment_row
              FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
              WHERE a."Id"=NEW."ResolvedRoleAssignmentId";
            SELECT r."Code" INTO role_code FROM advance.roles r
              WHERE r."Id"=assignment_row."RoleId";
            IF NOT FOUND OR assignment_row."CompanyId"<>NEW."CompanyId"
               OR assignment_row."EmployeeId"<>NEW."DecidedByEmployeeId"
               OR NEW."DecidedByEmployeeId"=(SELECT h."RequestedByEmployeeId"
                    FROM advance.material_issue_request_lines l
                    JOIN advance.material_issue_requests h ON h."Id"=l."MaterialIssueRequestId"
                    WHERE l."Id"=NEW."MaterialIssueRequestLineId")
               OR role_code<>NEW."ActorRoleCode"
               OR assignment_row."AssignmentType"='SUPPORT'
               OR assignment_row."EffectiveFrom">NEW."DecidedAt"::date
               OR (assignment_row."EffectiveTo" IS NOT NULL AND assignment_row."EffectiveTo"<NEW."DecidedAt"::date) THEN
              RAISE EXCEPTION 'TD excess decision requires the recorded effective non-SUPPORT assignment.';
            END IF;
            RETURN NEW;
          END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Material issues are immutable.'; END IF;
          IF TG_OP='UPDATE' THEN
            IF OLD."StockPostingBatchId" IS NOT NULL OR NEW."StockPostingBatchId" IS NULL
               OR (to_jsonb(NEW)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version')
                  IS DISTINCT FROM
                  (to_jsonb(OLD)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version') THEN
              RAISE EXCEPTION 'A material issue permits only its one-time atomic posting link.';
            END IF;
            RETURN NEW;
          END IF;
          SELECT * INTO request_row FROM advance.material_issue_requests
            WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."MaterialIssueRequestId";
          IF NOT FOUND OR request_row."Status" NOT IN ('APPROVED','PARTIALLY_FULFILLED','FULFILLED')
             OR request_row."JobOrderId" IS DISTINCT FROM NEW."JobOrderId"
             OR (request_row."Situation" IN ('CHAMBER_MANUFACTURE','SERVICE_CUSTOMER_PO','SITE_PROJECT_PO')
                   AND NEW."JobOrderId" IS NULL)
             OR (request_row."Situation"='CONSUMABLE_OFFICE' AND NEW."JobOrderId" IS NOT NULL) THEN
            RAISE EXCEPTION 'Material issue request status and Job Order custody evidence are invalid.';
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_material_issue_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.material_issues FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_governance();
        CREATE TRIGGER trg_material_issue_line_guard BEFORE UPDATE OR DELETE
          ON advance.material_issue_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_governance();
        CREATE TRIGGER trg_material_issue_history_guard BEFORE UPDATE OR DELETE
          ON advance.material_issue_history FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_governance();
        CREATE TRIGGER trg_material_issue_excess_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.material_issue_excess_decisions FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_governance();

        CREATE FUNCTION advance.post_material_issue_custody(
          p_company_id uuid,p_issue_id uuid,p_idempotency_key text,p_request_fingerprint text,
          p_correlation_id text,p_posted_by_employee_id uuid,p_created_by text)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE issue_row advance.material_issues%ROWTYPE;
                request_row advance.material_issue_requests%ROWTYPE;
                batch_id uuid; prior_hash text; movement_count integer;
        BEGIN
          IF p_company_id IS NULL OR p_issue_id IS NULL OR p_posted_by_employee_id IS NULL
             OR length(btrim(coalesce(p_idempotency_key,'')))=0
             OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$'
             OR length(btrim(coalesce(p_correlation_id,'')))=0
             OR length(btrim(coalesce(p_created_by,'')))=0 THEN
            RAISE EXCEPTION 'Issue posting requires company, issue, idempotency, fingerprint, correlation and actor.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended(
            'STORES:IDEMP:'||p_company_id||':'||btrim(p_idempotency_key),0));
          SELECT "Id","RequestFingerprint" INTO batch_id,prior_hash
            FROM advance.stock_posting_batches
            WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=btrim(p_idempotency_key);
          IF FOUND THEN
            IF prior_hash=p_request_fingerprint THEN
              RETURN QUERY SELECT batch_id,true; RETURN;
            END IF;
            RAISE EXCEPTION 'Posting idempotency key was reused with a different fingerprint.';
          END IF;
          SELECT * INTO issue_row FROM advance.material_issues
            WHERE "CompanyId"=p_company_id AND "Id"=p_issue_id FOR UPDATE;
          IF NOT FOUND OR issue_row."Status"<>'ISSUED'
             OR issue_row."IssuedByEmployeeId"<>p_posted_by_employee_id THEN
            RAISE EXCEPTION 'Issue document is absent, not ISSUED, or actor does not match.';
          END IF;
          SELECT * INTO request_row FROM advance.material_issue_requests
            WHERE "CompanyId"=p_company_id AND "Id"=issue_row."MaterialIssueRequestId" FOR UPDATE;
          IF NOT FOUND OR request_row."Status" NOT IN ('PARTIALLY_FULFILLED','FULFILLED') THEN
            RAISE EXCEPTION 'Issue requires an approved and atomically fulfilled MIR.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.material_issue_lines l
            WHERE l."MaterialIssueId"=issue_row."Id" AND (
              NOT EXISTS (SELECT 1 FROM advance.material_issue_request_lines r
                WHERE r."CompanyId"=l."CompanyId" AND r."Id"=l."MaterialIssueRequestLineId"
                  AND r."MaterialIssueRequestId"=request_row."Id" AND r."ItemId"=l."ItemId")
              OR NOT EXISTS (SELECT 1 FROM advance.inventory_custody_assignments a
                JOIN advance.inventory_custody_accounts c ON c."Id"=a."CustodyAccountId"
                JOIN advance.inventory_account_holders h ON h."Id"=c."AccountHolderId"
                WHERE a."CompanyId"=l."CompanyId" AND a."Id"=l."ToCustodyAssignmentId"
                  AND c."CustodyType"='EMPLOYEE' AND h."EmployeeId"=issue_row."IssuedToEmployeeId"))) THEN
            RAISE EXCEPTION 'Issue line request/item or engineer custody assignment is invalid.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.material_issue_lines WHERE "MaterialIssueId"=issue_row."Id") THEN
            RAISE EXCEPTION 'Issue requires at least one line.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.material_issue_lines l
            WHERE l."MaterialIssueId"=issue_row."Id" AND
              coalesce((SELECT sum(m."QuantityIn"-m."QuantityOut")
                FROM advance.stock_movements m
                WHERE m."CompanyId"=l."CompanyId" AND m."ItemId"=l."ItemId"
                  AND m."WarehouseConditionLocationId"=l."WarehouseConditionLocationId"
                  AND m."OwnershipAccountId"=l."OwnershipAccountId"
                  AND m."CustodyAssignmentId"=l."FromCustodyAssignmentId"
                  AND m."InventoryProvenanceLayerId"=l."InventoryProvenanceLayerId"
                  AND m."InventoryLotId" IS NOT DISTINCT FROM l."InventoryLotId"
                  AND m."InventorySerialId" IS NOT DISTINCT FROM l."InventorySerialId"),0)<l."QuantityBase") THEN
            RAISE EXCEPTION 'Insufficient AVAILABLE Stores custody for issue.';
          END IF;
          batch_id:=gen_random_uuid();
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","MaterialIssueRequestId","MaterialIssueId",
             "ReferenceType","ReferenceNumber","PostingDate","PostedAt","PostedByEmployeeId",
             "IdempotencyKey","RequestFingerprint","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES (batch_id,p_company_id,'MATERIAL_ISSUE',request_row."Id",issue_row."Id",
             'MATERIAL_ISSUE_REQUEST',request_row."RequestNumber",issue_row."IssuedAt"::date,
             clock_timestamp(),p_posted_by_employee_id,btrim(p_idempotency_key),
             p_request_fingerprint,btrim(p_correlation_id),clock_timestamp(),btrim(p_created_by),0);
          INSERT INTO advance.stock_movements
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType",
             "ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion",
             "WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal",
             "MovementLeg","MaterialIssueRequestLineId","MaterialIssueLineId","OriginGoodsReceiptLineId",
             "OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId","CustodyCaseLineId",
             "InventoryLotId","InventorySerialId","GoodsReceiptLineLotAllocationId",
             "QcInspectionLotDispositionId","PostingIdentity","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),l."CompanyId",l."ItemId",w."WarehouseId",w."RackBinId",
             'MATERIAL_ISSUE','MATERIAL_ISSUE_REQUEST',request_row."RequestNumber",
             CASE WHEN direction.n=2 THEN l."QuantityBase" ELSE 0 END,
             CASE WHEN direction.n=1 THEN l."QuantityBase" ELSE 0 END,
             issue_row."IssuedAt"::date,2,l."WarehouseConditionLocationId",'AVAILABLE',
             batch_id,l."LineNumber"*2-2+direction.n,'ISSUE_OUT',
             l."MaterialIssueRequestLineId",l."Id",l."OriginGoodsReceiptLineId",
             l."OwnershipAccountId",
             CASE WHEN direction.n=1 THEN l."FromCustodyAssignmentId" ELSE l."ToCustodyAssignmentId" END,
             l."InventoryProvenanceLayerId",l."CustodyCaseLineId",l."InventoryLotId",l."InventorySerialId",
             l."GoodsReceiptLineLotAllocationId",l."QcInspectionLotDispositionId",
             'MATERIAL_ISSUE:'||l."Id"||CASE WHEN direction.n=1 THEN ':STORES_OUT' ELSE ':ENGINEER_IN' END,
             clock_timestamp(),btrim(p_created_by),0
          FROM advance.material_issue_lines l
          JOIN advance.warehouse_condition_locations w
            ON w."CompanyId"=l."CompanyId" AND w."Id"=l."WarehouseConditionLocationId"
          CROSS JOIN (VALUES (1),(2)) direction(n)
          WHERE l."MaterialIssueId"=issue_row."Id"
          ORDER BY l."LineNumber",direction.n;
          GET DIAGNOSTICS movement_count=ROW_COUNT;
          IF movement_count<>(SELECT count(*)*2 FROM advance.material_issue_lines
              WHERE "MaterialIssueId"=issue_row."Id") THEN
            RAISE EXCEPTION 'Every issue allocation requires an atomic Stores-out/engineer-in pair.';
          END IF;
          RETURN QUERY SELECT batch_id,false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.post_material_issue_custody(
          uuid,uuid,text,text,text,uuid,text) FROM PUBLIC;

        CREATE FUNCTION advance.guard_material_issue_posting()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE batch_id uuid; b advance.stock_posting_batches%ROWTYPE;
        BEGIN
          IF TG_TABLE_NAME='stock_posting_batches' THEN
            batch_id:=NEW."Id";
          ELSE
            batch_id:=NEW."StockPostingBatchId";
          END IF;
          SELECT * INTO b FROM advance.stock_posting_batches WHERE "Id"=batch_id;
          IF NOT FOUND OR b."PostingKind"<>'MATERIAL_ISSUE' THEN RETURN NULL; END IF;
          IF b."MaterialIssueId" IS NULL OR NOT EXISTS (
            SELECT 1 FROM advance.material_issues i
            WHERE i."CompanyId"=b."CompanyId" AND i."Id"=b."MaterialIssueId"
              AND i."MaterialIssueRequestId"=b."MaterialIssueRequestId"
              AND i."StockPostingBatchId"=b."Id") THEN
            RAISE EXCEPTION 'Material issue posting requires its explicit, atomically linked Issue document.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.material_issue_lines l
            WHERE l."MaterialIssueId"=b."MaterialIssueId" AND (
              (SELECT count(*) FROM advance.stock_movements m
                 WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialIssueLineId"=l."Id")<>2
              OR (SELECT coalesce(sum(m."QuantityOut"),0) FROM advance.stock_movements m
                 WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialIssueLineId"=l."Id"
                   AND m."CustodyAssignmentId"=l."FromCustodyAssignmentId")<>l."QuantityBase"
              OR (SELECT coalesce(sum(m."QuantityIn"),0) FROM advance.stock_movements m
                 WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialIssueLineId"=l."Id"
                   AND m."CustodyAssignmentId"=l."ToCustodyAssignmentId")<>l."QuantityBase"
              OR EXISTS (SELECT 1 FROM advance.stock_movements m
                 WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialIssueLineId"=l."Id"
                   AND (m."OwnershipAccountId"<>l."OwnershipAccountId"
                     OR m."InventoryProvenanceLayerId"<>l."InventoryProvenanceLayerId")))) THEN
            RAISE EXCEPTION 'Material issue must be a balanced custody-only transfer preserving ownership and provenance.';
          END IF;
          RETURN NULL;
        END $function$;
        CREATE CONSTRAINT TRIGGER trg_material_issue_batch_reconcile
          AFTER INSERT ON advance.stock_posting_batches DEFERRABLE INITIALLY DEFERRED
          FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_posting();
        CREATE CONSTRAINT TRIGGER trg_material_issue_movement_reconcile
          AFTER INSERT ON advance.stock_movements DEFERRABLE INITIALLY DEFERRED
          FOR EACH ROW EXECUTE FUNCTION advance.guard_material_issue_posting();

        DO $pages$
        DECLARE request_page uuid:='52000000-0000-0000-0000-000000000004';
                issue_page uuid:='52000000-0000-0000-0000-000000000005';
                excess_page uuid:='52000000-0000-0000-0000-000000000006';
                affected integer; expected integer;
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey" IN
             ('stores.material-issue-requests','stores.material-issues','stores.material-issue-excess')) THEN
            RAISE EXCEPTION 'Material issue pages are partially or already installed.';
          END IF;
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES
            (request_page,'stores.material-issue-requests','Stores','Material Issue Requests',
              '/stores/material-issue-requests',true,now(),'MaterialIssueRequestAndCustodyIssue',0),
            (issue_page,'stores.material-issues','Stores','Material Issues',
              '/stores/material-issues',true,now(),'MaterialIssueRequestAndCustodyIssue',0),
            (excess_page,'stores.material-issue-excess','Stores','MIR Excess Decisions',
              '/stores/material-issue-excess',true,now(),'MaterialIssueRequestAndCustodyIssue',0);
          SELECT count(*) INTO expected FROM advance.roles
            WHERE "IsActive" AND "IsEmployeeAssignable";
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('mir-request-'||r."Id")::uuid,r."Id",request_page,
             true,true,true,true,false,false,r."Code" IN ('STORES_MANAGER','PRODUCTION_MANAGER'),
             r."Code" IN ('STORES_MANAGER','PRODUCTION_MANAGER'),false,false,false,
             r."Code"='STORES_MANAGER',false,false,false,false,false,false,false,true,false,
             now(),'MaterialIssueRequestAndCustodyIssue',0
          FROM advance.roles r WHERE r."IsActive" AND r."IsEmployeeAssignable";
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>expected THEN RAISE EXCEPTION 'Expected % MIR request grants, found %.',expected,affected; END IF;
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('material-issue-'||r."Id")::uuid,r."Id",issue_page,
             true,false,false,false,true,false,false,false,false,false,false,false,false,false,
             false,false,false,false,false,true,false,now(),'MaterialIssueRequestAndCustodyIssue',0
          FROM advance.roles r WHERE r."Code" IN ('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER') AND r."IsActive";
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>3 THEN RAISE EXCEPTION 'Expected three material issue grants, found %.',affected; END IF;
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
             "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
             "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
             "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
             "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('mir-excess-'||r."Id")::uuid,r."Id",excess_page,
             true,false,false,false,false,false,true,false,false,false,false,false,false,false,
             false,false,false,false,false,true,false,now(),'MaterialIssueRequestAndCustodyIssue',0
          FROM advance.roles r WHERE r."Code"='TECHNICAL_DIRECTOR' AND r."IsActive";
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>1 THEN RAISE EXCEPTION 'Expected one TD excess-decision grant, found %.',affected; END IF;
        END $pages$;

        DO $roles$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            GRANT SELECT,INSERT,UPDATE ON advance.material_issue_requests,
              advance.material_issue_request_lines,advance.material_issues,
              advance.material_issue_lines TO nexa_erp_runtime;
            GRANT SELECT,INSERT ON advance.material_issue_excess_decisions,
              advance.material_issue_history TO nexa_erp_runtime;
            REVOKE DELETE ON advance.material_issue_requests,advance.material_issue_request_lines,
              advance.material_issues,advance.material_issue_lines,
              advance.material_issue_excess_decisions,advance.material_issue_history FROM nexa_erp_runtime;
            REVOKE UPDATE ON advance.material_issue_excess_decisions,
              advance.material_issue_history FROM nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.post_material_issue_custody(
              uuid,uuid,text,text,text,uuid,text) TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.post_material_issue_custody(
              uuid,uuid,text,text,text,uuid,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.guard_material_issue_governance() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_material_issue_posting() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)
              OWNER TO nexa_erp_owner;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DO $guard$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.material_issues)
             OR EXISTS (SELECT 1 FROM advance.material_issue_excess_decisions)
             OR EXISTS (SELECT 1 FROM advance.material_issue_history) THEN
            RAISE EXCEPTION 'Material Issue rollback refuses existing operational or audit evidence.';
          END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_material_issue_movement_reconcile ON advance.stock_movements;
        DROP TRIGGER IF EXISTS trg_material_issue_batch_reconcile ON advance.stock_posting_batches;
        DROP FUNCTION IF EXISTS advance.guard_material_issue_posting();
        DROP TRIGGER IF EXISTS trg_material_issue_excess_guard ON advance.material_issue_excess_decisions;
        DROP TRIGGER IF EXISTS trg_material_issue_history_guard ON advance.material_issue_history;
        DROP TRIGGER IF EXISTS trg_material_issue_line_guard ON advance.material_issue_lines;
        DROP TRIGGER IF EXISTS trg_material_issue_guard ON advance.material_issues;
        DROP FUNCTION IF EXISTS advance.guard_material_issue_governance();
        DROP FUNCTION IF EXISTS advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text);
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='MaterialIssueRequestAndCustodyIssue';
        DELETE FROM advance.page_definitions WHERE "CreatedBy"='MaterialIssueRequestAndCustodyIssue';
        ALTER TABLE advance.material_issue_request_lines DROP CONSTRAINT "CK_mir_line_base_quantities";
        ALTER TABLE advance.material_issue_requests DROP CONSTRAINT "CK_mir_situation";
        ALTER TABLE advance.material_issue_requests DROP CONSTRAINT "CK_mir_lifecycle";
        ALTER TABLE advance.material_issue_requests ADD CONSTRAINT "CK_mir_lifecycle"
          CHECK ("Purpose" IN ('FACTORY_ASSEMBLY','PROJECT','SERVICE','WARRANTY','DEMO','SALE','FREE_OF_COST')
            AND "DestinationType" IN ('JOB_ORDER','CUSTOMER','VENDOR','DEPARTMENT','OTHER')
            AND "Status" IN ('DRAFT','SUBMITTED','APPROVED','REJECTED','PARTIALLY_FULFILLED','FULFILLED','REVERSED')
            AND jsonb_typeof("ApprovalRouteSnapshotJson")='object');
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK (
            "OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL
            AND "InventoryProvenanceLayerId" IS NOT NULL
            AND "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL
            AND "WarehouseConditionLocationId" IS NOT NULL AND "ConditionCode" IS NOT NULL
            AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId",
              "QcInspectionLotDispositionId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
              "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")=1);
        CREATE OR REPLACE FUNCTION advance.guard_estimated_bom_governance()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE a record; organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN RAISE EXCEPTION 'Estimated BOM history and item merge evidence are immutable.'; END IF;
          SELECT r."Code",e."AssignmentType" INTO a FROM advance.employee_role_assignments e
          JOIN advance.roles r ON r."Id"=e."RoleId" WHERE e."Id"=NEW."ResolvedRoleAssignmentId"
            AND e."EmployeeId"=NEW."ActorEmployeeId" AND e."CompanyId"=COALESCE(NEW."CompanyId",e."CompanyId")
            AND e."ApprovalStatus"='Approved' AND e."EffectiveFrom"<=CURRENT_DATE
            AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode" OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN RAISE EXCEPTION 'Resolved role assignment evidence does not match a currently effective assignment.'; END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN RAISE EXCEPTION 'Estimated BOM and item merge evidence requires the current ordinary command transaction.'; END IF;
          IF TG_TABLE_NAME='estimated_bom_history' AND NEW."Action"='Approve' AND a."AssignmentType"='SUPPORT' THEN RAISE EXCEPTION 'SUPPORT authority cannot approve an Estimated BOM.'; END IF;
          IF TG_TABLE_NAME='item_merge_aliases' AND (a."AssignmentType"='SUPPORT' OR a."Code" NOT IN ('STORES_MANAGER','PURCHASE_MANAGER')) THEN RAISE EXCEPTION 'Item merge requires FULL or TEMPORARY STORES_MANAGER or PURCHASE_MANAGER authority.'; END IF;
          RETURN NEW;
        END $function$;
        CREATE OR REPLACE FUNCTION advance.guard_production_engineering_history()
        RETURNS trigger LANGUAGE plpgsql AS $function$
        DECLARE a record; organization text;
        BEGIN
          IF TG_OP IN ('UPDATE','DELETE') THEN RAISE EXCEPTION 'Production engineering history is immutable.'; END IF;
          SELECT r."Code",e."AssignmentType" INTO a FROM advance.employee_role_assignments e JOIN advance.roles r ON r."Id"=e."RoleId"
          WHERE e."Id"=NEW."ResolvedRoleAssignmentId" AND e."EmployeeId"=NEW."ActorEmployeeId" AND e."CompanyId"=NEW."CompanyId"
            AND e."ApprovalStatus"='Approved' AND e."EffectiveFrom"<=CURRENT_DATE AND (e."EffectiveTo" IS NULL OR e."EffectiveTo">=CURRENT_DATE);
          IF a IS NULL OR a."Code"<>NEW."ActorRoleCode" OR a."AssignmentType"<>NEW."ResolvedRoleAssignmentType" THEN RAISE EXCEPTION 'Resolved role assignment evidence is not currently effective in this company.'; END IF;
          IF NEW."Action"='Approve' AND a."AssignmentType"='SUPPORT' THEN RAISE EXCEPTION 'SUPPORT authority cannot approve Production engineering records.'; END IF;
          SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
          IF NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN RAISE EXCEPTION 'Production engineering evidence requires the current ordinary command transaction.'; END IF;
          RETURN NEW;
        END $function$;
        """;
}
