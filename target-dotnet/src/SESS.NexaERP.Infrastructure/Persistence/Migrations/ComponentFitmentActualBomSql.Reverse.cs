namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static partial class ComponentFitmentActualBomSql
{
    private const string ReverseFunction = """
        CREATE FUNCTION advance.reverse_component_fitment(
          p_company uuid,p_fitment uuid,p_reason text,p_key text,p_hash text,p_correlation text,
          p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE f advance.component_fitments%ROWTYPE; rid uuid; prior_hash text; prior_actor uuid;
          prior_assignment uuid; ob advance.stock_posting_batches%ROWTYPE;
          om advance.stock_movements%ROWTYPE; bid uuid; ae advance.actual_bom_entries%ROWTYPE;
        BEGIN
          IF p_company IS NULL OR p_fitment IS NULL OR p_actor IS NULL OR p_assignment IS NULL
             OR length(btrim(coalesce(p_reason,'')))=0 OR length(btrim(coalesce(p_key,'')))=0
             OR p_hash !~ '^[0-9a-fA-F]{64}$' OR length(btrim(coalesce(p_correlation,'')))=0 THEN
            RAISE EXCEPTION 'Fitment reversal requires fitment, reason, idempotency and actor evidence.';
          END IF;

          PERFORM pg_advisory_xact_lock(hashtextextended('FITMENT-REV:'||p_company||':'||btrim(p_key),0));
          SELECT "Id","RequestFingerprint","ReversedByEmployeeId","ResolvedRoleAssignmentId"
            INTO rid,prior_hash,prior_actor,prior_assignment
            FROM advance.component_fitment_reversals
            WHERE "CompanyId"=p_company AND "IdempotencyKey"=btrim(p_key);
          IF FOUND THEN
            IF prior_hash=p_hash AND prior_actor=p_actor AND prior_assignment=p_assignment THEN
              RETURN QUERY SELECT true; RETURN;
            END IF;
            RAISE EXCEPTION 'Fitment reversal idempotency key was reused with different content or authority.';
          END IF;
          IF NOT advance.fitment_authority_valid(p_company,p_actor,p_role,p_assignment,p_type,
              ARRAY['PRODUCTION_MANAGER','SERVICE_MANAGER']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Fitment reversal requires a currently effective FULL PRODUCTION_MANAGER or SERVICE_MANAGER assignment in this company.';
          END IF;
          SELECT * INTO f FROM advance.component_fitments
            WHERE "CompanyId"=p_company AND "Id"=p_fitment FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Component fitment was not found in this company.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.component_fitment_reversals
              WHERE "CompanyId"=p_company AND "ComponentFitmentId"=p_fitment) THEN
            RAISE EXCEPTION 'Component fitment is already reversed.';
          END IF;
          SELECT * INTO STRICT ob FROM advance.stock_posting_batches
            WHERE "CompanyId"=p_company AND "ComponentFitmentId"=p_fitment
              AND "PostingKind"='FITMENT_CONSUMPTION';
          SELECT * INTO STRICT om FROM advance.stock_movements
            WHERE "CompanyId"=p_company AND "StockPostingBatchId"=ob."Id"
              AND "ComponentFitmentId"=p_fitment;
          SELECT * INTO STRICT ae FROM advance.actual_bom_entries
            WHERE "CompanyId"=p_company AND "ComponentFitmentId"=p_fitment AND "EntryKind"='FITMENT';
          rid:=gen_random_uuid();
          PERFORM set_config('advance.fitment_mutation',p_correlation,true);
          INSERT INTO advance.component_fitment_reversals
            ("Id","CompanyId","ComponentFitmentId","ReversedAt","ReversedByEmployeeId",
             "ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","Reason",
             "IsSelfReversal","IdempotencyKey","RequestFingerprint","CreatedAt","CreatedBy","Version")
          VALUES (rid,p_company,p_fitment,clock_timestamp(),p_actor,p_role,p_assignment,p_type,
             btrim(p_reason),p_actor=f."ConfirmedByEmployeeId",btrim(p_key),p_hash,
             clock_timestamp(),p_login,0);
          bid:=gen_random_uuid();
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","ReversesPostingBatchId","ReferenceType","ReferenceNumber",
             "PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint",
             "CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES (bid,p_company,'REVERSAL',ob."Id",'REVERSAL',f."FitmentNumber",
             CURRENT_DATE,clock_timestamp(),p_actor,'FITMENT-REV:'||btrim(p_key),
             p_hash,p_correlation,clock_timestamp(),p_login,0);
          INSERT INTO advance.stock_movements
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType",
             "ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion",
             "WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal",
             "MovementLeg","MaterialIssueRequestLineId","MaterialIssueLineId","ComponentFitmentId",
             "OriginGoodsReceiptLineId","OwnershipAccountId","CustodyAssignmentId",
             "InventoryProvenanceLayerId","CustodyCaseLineId","InventoryLotId","InventorySerialId",
             "GoodsReceiptLineLotAllocationId","QcInspectionLotDispositionId","ReversesStockMovementId",
             "PostingIdentity","CreatedAt","CreatedBy","Version")
          VALUES (gen_random_uuid(),p_company,om."ItemId",om."WarehouseId",om."RackBinId",
             'REVERSAL','REVERSAL',f."FitmentNumber",om."QuantityOut",0,CURRENT_DATE,2,
             om."WarehouseConditionLocationId",om."ConditionCode",bid,1,'REVERSAL',
             om."MaterialIssueRequestLineId",om."MaterialIssueLineId",p_fitment,
             om."OriginGoodsReceiptLineId",om."OwnershipAccountId",om."CustodyAssignmentId",
             om."InventoryProvenanceLayerId",om."CustodyCaseLineId",om."InventoryLotId",
             om."InventorySerialId",om."GoodsReceiptLineLotAllocationId",
             om."QcInspectionLotDispositionId",om."Id",'FITMENT:'||p_fitment||':REVERSAL',
             clock_timestamp(),p_login,0);
          INSERT INTO advance.actual_bom_entries
            ("Id","CompanyId","ActualBomId","ComponentFitmentReversalId","EntryKind",
             "MaterialIssueLineId","ItemId","UomId","QuantityBase","InventoryProvenanceLayerId",
             "InventoryLotId","InventorySerialId","GoodsReceiptLineId","GrnNumberSnapshot",
             "VendorBillLineId","VendorBillNumberSnapshot",
             "AcceptedMaterialValue","AllocatedChargeValue","TotalAcceptedValue","OccurredAt","CreatedBy")
          VALUES (gen_random_uuid(),p_company,ae."ActualBomId",rid,'REVERSAL',
             ae."MaterialIssueLineId",ae."ItemId",ae."UomId",-ae."QuantityBase",
             ae."InventoryProvenanceLayerId",ae."InventoryLotId",ae."InventorySerialId",
             ae."GoodsReceiptLineId",ae."GrnNumberSnapshot",ae."VendorBillLineId",
             ae."VendorBillNumberSnapshot",-ae."AcceptedMaterialValue",
             -ae."AllocatedChargeValue",-ae."TotalAcceptedValue",clock_timestamp(),p_login);
          RETURN QUERY SELECT false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.reverse_component_fitment(
          uuid,uuid,text,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC;
        """;

    private const string Reconciliation = """
        CREATE FUNCTION advance.guard_fitment_stock_posting()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE bid uuid; b advance.stock_posting_batches%ROWTYPE;
          f advance.component_fitments%ROWTYPE; original advance.stock_posting_batches%ROWTYPE;
        BEGIN
          IF TG_TABLE_NAME='stock_posting_batches' THEN
            bid:=NEW."Id";
          ELSE
            bid:=NEW."StockPostingBatchId";
          END IF;
          SELECT * INTO b FROM advance.stock_posting_batches WHERE "Id"=bid;
          IF NOT FOUND THEN RETURN NULL; END IF;
          IF b."PostingKind"='FITMENT_CONSUMPTION' THEN
            SELECT * INTO f FROM advance.component_fitments
              WHERE "CompanyId"=b."CompanyId" AND "Id"=b."ComponentFitmentId";
            IF NOT FOUND OR (SELECT count(*) FROM advance.stock_movements m
                  WHERE m."StockPostingBatchId"=b."Id")<>1
               OR NOT EXISTS (SELECT 1 FROM advance.stock_movements m
                  JOIN advance.material_issue_lines il ON il."Id"=f."MaterialIssueLineId"
                  WHERE m."StockPostingBatchId"=b."Id" AND m."ComponentFitmentId"=f."Id"
                    AND m."MovementLeg"='CONSUMPTION_OUT' AND m."QuantityOut"=f."QuantityBase"
                    AND m."QuantityIn"=0 AND m."OwnershipAccountId"=il."OwnershipAccountId"
                    AND m."CustodyAssignmentId"=il."ToCustodyAssignmentId"
                    AND m."InventoryProvenanceLayerId"=il."InventoryProvenanceLayerId") THEN
              RAISE EXCEPTION 'Fitment must atomically consume the exact quantity from engineer custody while preserving ownership and provenance.';
            END IF;
          ELSIF b."PostingKind"='REVERSAL' THEN
            SELECT * INTO original FROM advance.stock_posting_batches WHERE "Id"=b."ReversesPostingBatchId";
            IF original."PostingKind"<>'FITMENT_CONSUMPTION' THEN RETURN NULL; END IF;
            IF (SELECT count(*) FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id")<>1
               OR NOT EXISTS (SELECT 1 FROM advance.stock_movements reversal
                 JOIN advance.stock_movements consumed ON consumed."Id"=reversal."ReversesStockMovementId"
                 WHERE reversal."StockPostingBatchId"=b."Id"
                   AND consumed."StockPostingBatchId"=original."Id"
                   AND reversal."QuantityIn"=consumed."QuantityOut" AND reversal."QuantityOut"=0
                   AND reversal."OwnershipAccountId"=consumed."OwnershipAccountId"
                   AND reversal."CustodyAssignmentId"=consumed."CustodyAssignmentId"
                   AND reversal."InventoryProvenanceLayerId"=consumed."InventoryProvenanceLayerId") THEN
              RAISE EXCEPTION 'Fitment reversal must be the exact compensating custody and provenance entry.';
            END IF;
          END IF;
          RETURN NULL;
        END $function$;
        CREATE CONSTRAINT TRIGGER trg_fitment_batch_reconcile AFTER INSERT ON advance.stock_posting_batches
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_stock_posting();
        CREATE CONSTRAINT TRIGGER trg_fitment_movement_reconcile AFTER INSERT ON advance.stock_movements
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_fitment_stock_posting();
        """;
}