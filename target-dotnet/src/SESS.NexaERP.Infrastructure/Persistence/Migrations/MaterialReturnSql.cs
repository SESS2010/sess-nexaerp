namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class MaterialReturnSql
{
    internal const string Preflight = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Material Return requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Material Return refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regclass('advance.material_issues') IS NULL
             OR to_regprocedure('advance.post_material_issue_custody(uuid,uuid,text,text,text,uuid,text)') IS NULL
             OR to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NULL THEN
            RAISE EXCEPTION 'Material Return requires the committed MIR/Issue and ordinary command-ledger baseline.';
          END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN
             ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; refusing to guess grants or ownership.';
          END IF;
          IF to_regclass('advance.material_returns') IS NOT NULL
             OR to_regclass('advance.material_return_lines') IS NOT NULL
             OR to_regclass('advance.material_return_history') IS NOT NULL
             OR EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey"='stores.material-returns') THEN
            RAISE EXCEPTION 'Material Return is partially or already installed.';
          END IF;
        END $guard$;
        """;

    internal static string Up => Contracts + BatchGuard + MovementGuard + ReconcileGuard + Governance + Posting + Reconcile + PageAndRoles;

    private const string Contracts = """
        ALTER TABLE advance.material_returns ADD CONSTRAINT "CK_material_returns_lifecycle"
          CHECK (("Status"='SUBMITTED' AND num_nonnulls("AcceptedAt","AcceptedByEmployeeId",
                    "AcceptedActorRoleCode","AcceptedRoleAssignmentId","AcceptedRoleAssignmentType",
                    "AcceptanceReason","AcceptanceIdempotencyKey","AcceptanceRequestFingerprint","StockPostingBatchId")=0)
              OR ("Status"='ACCEPTED' AND num_nonnulls("AcceptedAt","AcceptedByEmployeeId",
                    "AcceptedActorRoleCode","AcceptedRoleAssignmentId","AcceptedRoleAssignmentType",
                    "AcceptanceReason","AcceptanceIdempotencyKey","AcceptanceRequestFingerprint")=8));
        ALTER TABLE advance.material_return_lines ADD CONSTRAINT "CK_material_return_line_statement"
          CHECK ("LineNumber">0 AND "ReturnedQuantityBase">0
            AND "ReportedConsumedQuantityBase">=0 AND "ReportedStillHeldQuantityBase">=0
            AND length(btrim("ScanCode"))>0);
        ALTER TABLE advance.material_return_history ADD CONSTRAINT "CK_material_return_history_action"
          CHECK ("Action" IN ('CREATE','ACCEPT'));

        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE',
            'MATERIAL_ISSUE','MATERIAL_RETURN','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","DeliveryChallanId","InventoryCustodyHandoffId",
                "InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1
              AND "ReversesPostingBatchId" IS NULL)
            OR ("PostingKind"='REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId",
                "MaterialReturnId","DeliveryChallanId","InventoryCustodyHandoffId",
                "InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=0
              AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK ("OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL
            AND "InventoryProvenanceLayerId" IS NOT NULL AND "WarehouseId" IS NOT NULL
            AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL
            AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL
            AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT',
              'RETURN_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId",
              "DeliveryChallanLineId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
              "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")
              + CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL
                           AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END = 1
            AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL)
            AND ("MaterialReturnLineId" IS NULL OR "MaterialIssueLineId" IS NOT NULL));
        """;

    internal static string ActiveBatchGuard => BatchGuard;
    internal static string ActiveMovementGuard => MovementGuard;
    internal static string ActiveReconcileGuard => ReconcileGuard;

    private static string BatchGuard => StoresSlice3QcConcessionSql.ActiveBatchGuard.Replace(
        "  ELSE\n    expected_type:='REVERSAL';",
        """
          ELSIF NEW."PostingKind"='MATERIAL_RETURN' THEN
            expected_type:='MATERIAL_RETURN';
            SELECT "CompanyId","ReturnNumber" INTO source_company,expected_number
              FROM advance.material_returns WHERE "Id"=NEW."MaterialReturnId";
          ELSE
            expected_type:='REVERSAL';
        """);

    private static string MovementGuard => StoresSlice3QcConcessionSql.MaterialIssueAwareMovementGuard.Replace(
        """  ELSIF NEW."MaterialIssueRequestLineId" IS NOT NULL THEN""",
        """
          ELSIF NEW."MaterialReturnLineId" IS NOT NULL THEN
            SELECT r."CompanyId",r."Id",il."ItemId" INTO source_company,source_header,source_item
              FROM advance.material_return_lines rl
              JOIN advance.material_returns r ON r."Id"=rl."MaterialReturnId"
              JOIN advance.material_issue_lines il ON il."Id"=rl."MaterialIssueLineId"
              WHERE rl."Id"=NEW."MaterialReturnLineId";
            IF b."PostingKind" NOT IN ('MATERIAL_RETURN','REVERSAL')
               OR (b."PostingKind"='MATERIAL_RETURN' AND source_header<>b."MaterialReturnId") THEN
              RAISE EXCEPTION 'Material Return movement source does not match its batch.';
            END IF;
            IF b."PostingKind"='MATERIAL_RETURN' AND
               NOT ((NEW."MovementLeg"='RETURN_OUT' AND NEW."QuantityOut">0)
                 OR (NEW."MovementLeg"='RETURN_IN' AND NEW."QuantityIn">0)) THEN
              RAISE EXCEPTION 'Material Return requires employee-out and Stores-in custody legs.';
            END IF;
          ELSIF NEW."MaterialIssueRequestLineId" IS NOT NULL THEN
        """);

    private static string ReconcileGuard => StoresSlice3QcConcessionSql.ActiveReconcileGuard.Replace(
        "  ELSE\n    IF (SELECT count(*)",
        """
          ELSIF b."PostingKind"='MATERIAL_RETURN' THEN
            NULL;
          ELSE
            IF (SELECT count(*)
        """);

    private const string Governance = """
        CREATE FUNCTION advance.guard_material_return_governance()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE issue_row advance.material_issues%ROWTYPE; assignment_row advance.employee_role_assignments%ROWTYPE;
                role_code text; organization text;
        BEGIN
          IF TG_TABLE_NAME IN ('material_return_lines','material_return_history') THEN
            IF TG_OP<>'INSERT' THEN RAISE EXCEPTION '% is immutable.',TG_TABLE_NAME; END IF;
            IF TG_TABLE_NAME='material_return_history' THEN
              SELECT a.* INTO assignment_row FROM advance.employee_role_assignments a
                WHERE a."Id"=NEW."ResolvedRoleAssignmentId";
              SELECT r."Code" INTO role_code FROM advance.roles r WHERE r."Id"=assignment_row."RoleId";
              SELECT "Code" INTO STRICT organization FROM advance.companies WHERE "Id"=NEW."CompanyId";
              IF assignment_row."EmployeeId" IS DISTINCT FROM NEW."ActorEmployeeId"
                 OR assignment_row."CompanyId" IS DISTINCT FROM NEW."CompanyId"
                 OR role_code IS DISTINCT FROM NEW."ActorRoleCode"
                 OR assignment_row."AssignmentType" IS DISTINCT FROM NEW."ResolvedRoleAssignmentType"
                 OR assignment_row."ApprovalStatus" NOT IN ('Approved','SeedApproved')
                 OR assignment_row."EffectiveFrom">NEW."OccurredAt"::date
                 OR (assignment_row."EffectiveTo" IS NOT NULL AND assignment_row."EffectiveTo"<NEW."OccurredAt"::date)
                 OR NOT advance.ordinary_command_context_valid(organization,NEW."ActorEmployeeId",
                    current_setting('advance.ordinary_identity_issuer',true),
                    current_setting('advance.ordinary_identity_subject',true),NEW."ActorRoleCode") THEN
                RAISE EXCEPTION 'Material Return history requires the recorded effective command authority.';
              END IF;
              IF NEW."Action"='ACCEPT' AND (assignment_row."AssignmentType"='SUPPORT'
                 OR role_code NOT IN ('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER')) THEN
                RAISE EXCEPTION 'Return acceptance requires non-SUPPORT Stores authority.';
              END IF;
            END IF;
            RETURN NEW;
          END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Material Returns are immutable.'; END IF;
          IF TG_OP='INSERT' THEN
            SELECT * INTO issue_row FROM advance.material_issues
              WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."MaterialIssueId";
            IF NOT FOUND OR issue_row."IssuedToEmployeeId"<>NEW."ReturnedByEmployeeId"
               OR NEW."CreatedByEmployeeId"<>NEW."ReturnedByEmployeeId" THEN
              RAISE EXCEPTION 'Only the named issue custodian may declare a return.';
            END IF;
            RETURN NEW;
          END IF;
          IF OLD."Status"='SUBMITTED' AND NEW."Status"='ACCEPTED' THEN
            SELECT a.* INTO assignment_row FROM advance.employee_role_assignments a
              WHERE a."Id"=NEW."AcceptedRoleAssignmentId";
            SELECT r."Code" INTO role_code FROM advance.roles r WHERE r."Id"=assignment_row."RoleId";
            IF NEW."AcceptedByEmployeeId"=NEW."ReturnedByEmployeeId"
               OR assignment_row."EmployeeId" IS DISTINCT FROM NEW."AcceptedByEmployeeId"
               OR assignment_row."CompanyId" IS DISTINCT FROM NEW."CompanyId"
               OR role_code IS DISTINCT FROM NEW."AcceptedActorRoleCode"
               OR assignment_row."AssignmentType" IS DISTINCT FROM NEW."AcceptedRoleAssignmentType"
               OR assignment_row."AssignmentType"='SUPPORT'
               OR role_code NOT IN ('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER')
               OR assignment_row."ApprovalStatus" NOT IN ('Approved','SeedApproved')
               OR assignment_row."EffectiveFrom">NEW."AcceptedAt"::date
               OR (assignment_row."EffectiveTo" IS NOT NULL AND assignment_row."EffectiveTo"<NEW."AcceptedAt"::date)
               OR (to_jsonb(NEW)-'Status'-'AcceptedAt'-'AcceptedByEmployeeId'-'AcceptedActorRoleCode'
                    -'AcceptedRoleAssignmentId'-'AcceptedRoleAssignmentType'-'AcceptanceReason'
                    -'AcceptanceIdempotencyKey'-'AcceptanceRequestFingerprint'-'UpdatedAt'-'UpdatedBy'-'Version')
                  IS DISTINCT FROM
                  (to_jsonb(OLD)-'Status'-'AcceptedAt'-'AcceptedByEmployeeId'-'AcceptedActorRoleCode'
                    -'AcceptedRoleAssignmentId'-'AcceptedRoleAssignmentType'-'AcceptanceReason'
                    -'AcceptanceIdempotencyKey'-'AcceptanceRequestFingerprint'-'UpdatedAt'-'UpdatedBy'-'Version') THEN
              RAISE EXCEPTION 'Material Return acceptance authority or immutable fields are invalid.';
            END IF;
            RETURN NEW;
          END IF;
          IF OLD."Status"='ACCEPTED' AND NEW."Status"='ACCEPTED'
             AND OLD."StockPostingBatchId" IS NULL AND NEW."StockPostingBatchId" IS NOT NULL
             AND (to_jsonb(NEW)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version')
                IS NOT DISTINCT FROM (to_jsonb(OLD)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version') THEN
            RETURN NEW;
          END IF;
          RAISE EXCEPTION 'Material Return permits only acceptance and its one-time atomic posting link.';
        END $function$;
        CREATE TRIGGER trg_material_return_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.material_returns FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_governance();
        CREATE TRIGGER trg_material_return_line_guard BEFORE UPDATE OR DELETE
          ON advance.material_return_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_governance();
        CREATE TRIGGER trg_material_return_history_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.material_return_history FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_governance();

        CREATE OR REPLACE FUNCTION advance.guard_material_issue_governance()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE request_row advance.material_issue_requests%ROWTYPE;
                assignment_row advance.employee_role_assignments%ROWTYPE; role_code text;
        BEGIN
          IF TG_TABLE_NAME IN ('material_issue_history','material_issue_lines') THEN
            IF TG_OP<>'INSERT' THEN RAISE EXCEPTION '% is immutable.',TG_TABLE_NAME; END IF; RETURN NEW;
          END IF;
          IF TG_TABLE_NAME='material_issue_excess_decisions' THEN
            IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Material issue excess decisions are immutable.'; END IF;
            SELECT a.* INTO assignment_row FROM advance.employee_role_assignments a WHERE a."Id"=NEW."ResolvedRoleAssignmentId";
            SELECT r."Code" INTO role_code FROM advance.roles r WHERE r."Id"=assignment_row."RoleId";
            IF NOT FOUND OR assignment_row."CompanyId"<>NEW."CompanyId" OR assignment_row."EmployeeId"<>NEW."DecidedByEmployeeId"
               OR NEW."DecidedByEmployeeId"=(SELECT h."RequestedByEmployeeId" FROM advance.material_issue_request_lines l
                    JOIN advance.material_issue_requests h ON h."Id"=l."MaterialIssueRequestId" WHERE l."Id"=NEW."MaterialIssueRequestLineId")
               OR role_code<>NEW."ActorRoleCode" OR assignment_row."AssignmentType"='SUPPORT'
               OR assignment_row."EffectiveFrom">NEW."DecidedAt"::date
               OR (assignment_row."EffectiveTo" IS NOT NULL AND assignment_row."EffectiveTo"<NEW."DecidedAt"::date) THEN
              RAISE EXCEPTION 'TD excess decision requires the recorded effective non-SUPPORT assignment.';
            END IF; RETURN NEW;
          END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Material issues are immutable.'; END IF;
          IF TG_OP='UPDATE' THEN
            IF OLD."StockPostingBatchId" IS NULL AND NEW."StockPostingBatchId" IS NOT NULL
               AND (to_jsonb(NEW)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version')
                 IS NOT DISTINCT FROM (to_jsonb(OLD)-'StockPostingBatchId'-'UpdatedAt'-'UpdatedBy'-'Version') THEN RETURN NEW; END IF;
            IF OLD."StockPostingBatchId" IS NOT NULL AND NEW."StockPostingBatchId"=OLD."StockPostingBatchId"
               AND OLD."Status" IN ('ISSUED','PARTIALLY_RETURNED')
               AND NEW."Status" IN ('PARTIALLY_RETURNED','RETURNED')
               AND (to_jsonb(NEW)-'Status'-'UpdatedAt'-'UpdatedBy'-'Version')
                 IS NOT DISTINCT FROM (to_jsonb(OLD)-'Status'-'UpdatedAt'-'UpdatedBy'-'Version') THEN RETURN NEW; END IF;
            RAISE EXCEPTION 'Material issue permits only its posting link or derived return status.';
          END IF;
          SELECT * INTO request_row FROM advance.material_issue_requests WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."MaterialIssueRequestId";
          IF NOT FOUND OR request_row."Status" NOT IN ('APPROVED','PARTIALLY_FULFILLED','FULFILLED')
             OR request_row."JobOrderId" IS DISTINCT FROM NEW."JobOrderId"
             OR (request_row."Situation" IN ('CHAMBER_MANUFACTURE','SERVICE_CUSTOMER_PO','SITE_PROJECT_PO') AND NEW."JobOrderId" IS NULL)
             OR (request_row."Situation"='CONSUMABLE_OFFICE' AND NEW."JobOrderId" IS NOT NULL) THEN
            RAISE EXCEPTION 'Material issue request status and Job Order custody evidence are invalid.';
          END IF; RETURN NEW;
        END $function$;
        """;

    private const string Posting = """
        CREATE FUNCTION advance.post_material_return_acceptance(
          p_company_id uuid,p_return_id uuid,p_idempotency_key text,p_request_fingerprint text,
          p_correlation_id text,p_posted_by_employee_id uuid,p_created_by text)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE return_row advance.material_returns%ROWTYPE; issue_row advance.material_issues%ROWTYPE;
                batch_id uuid; prior_hash text; movement_count integer;
        BEGIN
          IF p_company_id IS NULL OR p_return_id IS NULL OR p_posted_by_employee_id IS NULL
             OR length(btrim(coalesce(p_idempotency_key,'')))=0
             OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$'
             OR length(btrim(coalesce(p_correlation_id,'')))=0 OR length(btrim(coalesce(p_created_by,'')))=0 THEN
            RAISE EXCEPTION 'Return posting requires company, return, idempotency, fingerprint, correlation and actor.';
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STORES:IDEMP:'||p_company_id||':'||btrim(p_idempotency_key),0));
          SELECT "Id","RequestFingerprint" INTO batch_id,prior_hash FROM advance.stock_posting_batches
            WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=btrim(p_idempotency_key);
          IF FOUND THEN
            IF prior_hash=p_request_fingerprint THEN RETURN QUERY SELECT batch_id,true; RETURN; END IF;
            RAISE EXCEPTION 'Posting idempotency key was reused with a different fingerprint.';
          END IF;
          SELECT * INTO return_row FROM advance.material_returns
            WHERE "CompanyId"=p_company_id AND "Id"=p_return_id FOR UPDATE;
          IF NOT FOUND OR return_row."Status"<>'ACCEPTED' OR return_row."StockPostingBatchId" IS NOT NULL
             OR return_row."AcceptedByEmployeeId"<>p_posted_by_employee_id THEN
            RAISE EXCEPTION 'Return is absent, not newly accepted, already posted, or actor does not match.';
          END IF;
          SELECT * INTO STRICT issue_row FROM advance.material_issues
            WHERE "CompanyId"=p_company_id AND "Id"=return_row."MaterialIssueId" FOR UPDATE;
          IF NOT EXISTS (SELECT 1 FROM advance.material_return_lines WHERE "MaterialReturnId"=p_return_id) THEN
            RAISE EXCEPTION 'Return requires at least one scanned line.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.material_return_lines rl
            JOIN advance.material_issue_lines il ON il."CompanyId"=rl."CompanyId" AND il."Id"=rl."MaterialIssueLineId"
            WHERE rl."MaterialReturnId"=p_return_id AND (il."MaterialIssueId"<>issue_row."Id" OR il."ItemId"<>rl."ItemId"
              OR coalesce((SELECT sum(m."QuantityIn"-m."QuantityOut") FROM advance.stock_movements m
                    WHERE m."CompanyId"=il."CompanyId" AND m."ItemId"=il."ItemId"
                      AND m."CustodyAssignmentId"=il."ToCustodyAssignmentId"
                      AND m."InventoryProvenanceLayerId"=il."InventoryProvenanceLayerId"
                      AND m."InventoryLotId" IS NOT DISTINCT FROM il."InventoryLotId"
                      AND m."InventorySerialId" IS NOT DISTINCT FROM il."InventorySerialId"),0)<rl."ReturnedQuantityBase")) THEN
            RAISE EXCEPTION 'Return exceeds exact issued engineer custody or breaks issue ancestry.';
          END IF;
          batch_id:=gen_random_uuid();
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","MaterialReturnId","ReferenceType","ReferenceNumber",
             "PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint",
             "CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES (batch_id,p_company_id,'MATERIAL_RETURN',return_row."Id",'MATERIAL_RETURN',
             return_row."ReturnNumber",return_row."AcceptedAt"::date,clock_timestamp(),p_posted_by_employee_id,
             btrim(p_idempotency_key),p_request_fingerprint,btrim(p_correlation_id),clock_timestamp(),btrim(p_created_by),0);
          INSERT INTO advance.stock_movements
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType",
             "ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion",
             "WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal",
             "MovementLeg","MaterialIssueRequestLineId","MaterialIssueLineId","MaterialReturnLineId",
             "OriginGoodsReceiptLineId","OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId",
             "CustodyCaseLineId","InventoryLotId","InventorySerialId","GoodsReceiptLineLotAllocationId",
             "QcInspectionLotDispositionId","PostingIdentity","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),il."CompanyId",il."ItemId",w."WarehouseId",w."RackBinId",
             'MATERIAL_RETURN','MATERIAL_RETURN',return_row."ReturnNumber",
             CASE WHEN direction.n=2 THEN rl."ReturnedQuantityBase" ELSE 0 END,
             CASE WHEN direction.n=1 THEN rl."ReturnedQuantityBase" ELSE 0 END,
             return_row."AcceptedAt"::date,2,il."WarehouseConditionLocationId",'AVAILABLE',batch_id,
             rl."LineNumber"*2-2+direction.n,CASE WHEN direction.n=1 THEN 'RETURN_OUT' ELSE 'RETURN_IN' END,
             il."MaterialIssueRequestLineId",il."Id",rl."Id",il."OriginGoodsReceiptLineId",il."OwnershipAccountId",
             CASE WHEN direction.n=1 THEN il."ToCustodyAssignmentId" ELSE il."FromCustodyAssignmentId" END,
             il."InventoryProvenanceLayerId",il."CustodyCaseLineId",il."InventoryLotId",il."InventorySerialId",
             il."GoodsReceiptLineLotAllocationId",il."QcInspectionLotDispositionId",
             'MATERIAL_RETURN:'||rl."Id"||CASE WHEN direction.n=1 THEN ':ENGINEER_OUT' ELSE ':STORES_IN' END,
             clock_timestamp(),btrim(p_created_by),0
          FROM advance.material_return_lines rl JOIN advance.material_issue_lines il ON il."Id"=rl."MaterialIssueLineId"
          JOIN advance.warehouse_condition_locations w ON w."CompanyId"=il."CompanyId" AND w."Id"=il."WarehouseConditionLocationId"
          CROSS JOIN (VALUES (1),(2)) direction(n) WHERE rl."MaterialReturnId"=return_row."Id"
          ORDER BY rl."LineNumber",direction.n;
          GET DIAGNOSTICS movement_count=ROW_COUNT;
          IF movement_count<>(SELECT count(*)*2 FROM advance.material_return_lines WHERE "MaterialReturnId"=return_row."Id") THEN
            RAISE EXCEPTION 'Every return allocation requires an atomic engineer-out/Stores-in pair.';
          END IF;
          RETURN QUERY SELECT batch_id,false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text) FROM PUBLIC;
        """;
    private const string Reconcile = """
        CREATE FUNCTION advance.guard_material_return_posting()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE batch_id uuid; b advance.stock_posting_batches%ROWTYPE;
        BEGIN
          IF TG_TABLE_NAME='stock_posting_batches' THEN
            batch_id:=NEW."Id";
          ELSE
            batch_id:=NEW."StockPostingBatchId";
          END IF;
          SELECT * INTO b FROM advance.stock_posting_batches WHERE "Id"=batch_id;
          IF NOT FOUND OR b."PostingKind"<>'MATERIAL_RETURN' THEN RETURN NULL; END IF;
          IF b."MaterialReturnId" IS NULL OR NOT EXISTS (SELECT 1 FROM advance.material_returns r
              WHERE r."CompanyId"=b."CompanyId" AND r."Id"=b."MaterialReturnId"
                AND r."Status"='ACCEPTED' AND r."StockPostingBatchId"=b."Id") THEN
            RAISE EXCEPTION 'Accepted Material Return requires its atomically linked posting batch.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.material_return_lines rl
            JOIN advance.material_issue_lines il ON il."Id"=rl."MaterialIssueLineId"
            WHERE rl."MaterialReturnId"=b."MaterialReturnId" AND (
              (SELECT count(*) FROM advance.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialReturnLineId"=rl."Id")<>2
              OR (SELECT coalesce(sum(m."QuantityOut"),0) FROM advance.stock_movements m
                    WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialReturnLineId"=rl."Id"
                      AND m."CustodyAssignmentId"=il."ToCustodyAssignmentId")<>rl."ReturnedQuantityBase"
              OR (SELECT coalesce(sum(m."QuantityIn"),0) FROM advance.stock_movements m
                    WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialReturnLineId"=rl."Id"
                      AND m."CustodyAssignmentId"=il."FromCustodyAssignmentId")<>rl."ReturnedQuantityBase"
              OR EXISTS (SELECT 1 FROM advance.stock_movements m
                    WHERE m."StockPostingBatchId"=b."Id" AND m."MaterialReturnLineId"=rl."Id"
                      AND (m."OwnershipAccountId"<>il."OwnershipAccountId"
                        OR m."InventoryProvenanceLayerId"<>il."InventoryProvenanceLayerId")))) THEN
            RAISE EXCEPTION 'Material Return must be a balanced custody-only transfer preserving ownership and provenance.';
          END IF; RETURN NULL;
        END $function$;
        CREATE CONSTRAINT TRIGGER trg_material_return_batch_reconcile AFTER INSERT ON advance.stock_posting_batches
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_posting();
        CREATE CONSTRAINT TRIGGER trg_material_return_movement_reconcile AFTER INSERT ON advance.stock_movements
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_posting();
        """;
    private const string PageAndRoles = """
        DO $page$
        DECLARE page_id uuid:='52000000-0000-0000-0000-000000000007'; expected integer; affected integer;
        BEGIN
          INSERT INTO advance.page_definitions
            ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
          VALUES (page_id,'stores.material-returns','Stores','Material Returns','/stores/material-returns',
            true,now(),'MaterialReturnToStores',0);
          SELECT count(*) INTO expected FROM advance.roles WHERE "IsActive" AND "IsEmployeeAssignable";
          INSERT INTO advance.role_page_permissions
            ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue",
             "CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit",
             "CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment",
             "CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
             "CreatedAt","CreatedBy","Version")
          SELECT md5('material-return-'||r."Id")::uuid,r."Id",page_id,true,true,false,false,false,false,
             r."Code" IN ('STORES_ASSISTANT','STORES_EXECUTIVE','STORES_MANAGER'),false,false,false,false,false,false,
             false,false,false,false,false,false,true,false,now(),'MaterialReturnToStores',0
          FROM advance.roles r WHERE r."IsActive" AND r."IsEmployeeAssignable";
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>expected THEN RAISE EXCEPTION 'Expected % Material Return grants, found %.',expected,affected; END IF;
        END $page$;
        DO $roles$
        BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            GRANT SELECT,INSERT,UPDATE ON advance.material_returns TO nexa_erp_runtime;
            GRANT SELECT,INSERT ON advance.material_return_lines,advance.material_return_history TO nexa_erp_runtime;
            REVOKE DELETE ON advance.material_returns,advance.material_return_lines,advance.material_return_history FROM nexa_erp_runtime;
            REVOKE UPDATE ON advance.material_return_lines,advance.material_return_history FROM nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text) TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text)
              FROM nexa_erp_bootstrap,nexa_erp_migration;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER FUNCTION advance.guard_material_return_governance() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_material_return_posting() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text) OWNER TO nexa_erp_owner;
          END IF;
        END $roles$;
        """;
    internal static string Down => """
        DO $guard$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.material_returns)
             OR EXISTS (SELECT 1 FROM advance.material_return_history) THEN
            RAISE EXCEPTION 'Material Return rollback refuses existing operational or audit evidence.';
          END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_material_return_movement_reconcile ON advance.stock_movements;
        DROP TRIGGER IF EXISTS trg_material_return_batch_reconcile ON advance.stock_posting_batches;
        DROP FUNCTION IF EXISTS advance.guard_material_return_posting();
        DROP TRIGGER IF EXISTS trg_material_return_history_guard ON advance.material_return_history;
        DROP TRIGGER IF EXISTS trg_material_return_line_guard ON advance.material_return_lines;
        DROP TRIGGER IF EXISTS trg_material_return_guard ON advance.material_returns;
        DROP FUNCTION IF EXISTS advance.guard_material_return_governance();
        DROP FUNCTION IF EXISTS advance.post_material_return_acceptance(uuid,uuid,text,text,text,uuid,text);
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='MaterialReturnToStores';
        DELETE FROM advance.page_definitions WHERE "CreatedBy"='MaterialReturnToStores';
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId",
            "MaterialIssueRequestId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId",
            "InventoryTransformationId","InventoryConcessionId")=1 AND "ReversesPostingBatchId" IS NULL)
          OR ("PostingKind"='REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId",
            "MaterialIssueRequestId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId",
            "InventoryTransformationId","InventoryConcessionId")=0 AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK ("OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL
            AND "InventoryProvenanceLayerId" IS NOT NULL AND "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL
            AND "WarehouseConditionLocationId" IS NOT NULL AND "ConditionCode" IS NOT NULL
            AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId",
              "InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId","InventoryTransformationInputId",
              "InventoryTransformationOutputId","InventoryConcessionAllocationId")
              + CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END = 1
            AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL));
        """ + StoresSlice3QcConcessionSql.ActiveBatchGuard
          + StoresSlice3QcConcessionSql.MaterialIssueAwareMovementGuard
          + StoresSlice3QcConcessionSql.ActiveReconcileGuard
          + MaterialIssueCustodySql.ActiveGovernance;
}
