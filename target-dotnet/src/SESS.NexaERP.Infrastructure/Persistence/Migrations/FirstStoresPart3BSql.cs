namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class FirstStoresPart3BSql
{
    internal static string PreUp => AdvanceSchemaSql.Expand(PreUpSql);
    internal static string Up => AdvanceSchemaSql.Expand(UpSql);
    internal static string Down => AdvanceSchemaSql.Expand(DownSql);

    private const string PreUpSql = """
        DO $guard$
        DECLARE required_table text;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'First Stores Part 3B requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'First Stores Part 3B refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN
            RAISE EXCEPTION 'First Stores Part 3B requires the advance schema.';
          END IF;
          FOREACH required_table IN ARRAY ARRAY[
            'goods_receipts','goods_receipt_lines','inventory_serials','qc_inspection_revisions',
            'material_issue_requests','material_issue_request_lines','delivery_challans',
            'delivery_challan_lines','warehouse_condition_locations','stock_movements'
          ] LOOP
            IF to_regclass('__advance_schema__.' || required_table) IS NULL THEN
              RAISE EXCEPTION 'First Stores Part 3B requires earlier table %.', required_table;
            END IF;
          END LOOP;
          IF to_regclass('__advance_schema__.stock_posting_batches') IS NOT NULL THEN
            RAISE EXCEPTION 'First Stores Part 3B refuses a partial or replayed schema.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_qc_revision_part3a_block' AND tgenabled<>'D')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_mir_part3a_block' AND tgenabled<>'D')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname='TR_dc_part3a_block' AND tgenabled<>'D') THEN
            RAISE EXCEPTION 'First Stores Part 3B requires all Part 3A transition blockers.';
          END IF;
        END $guard$;
        LOCK TABLE __advance_schema__.goods_receipts IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.qc_inspection_revisions IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.material_issue_requests IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.delivery_challans IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.stock_movements IN ACCESS EXCLUSIVE MODE;
        """;

    private const string UpSql = """
        ALTER TABLE __advance_schema__.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind"
          CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE __advance_schema__.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId")=1 AND "ReversesPostingBatchId" IS NULL)
              OR ("PostingKind"='REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId")=0 AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE __advance_schema__.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_fingerprint"
          CHECK ("RequestFingerprint" ~ '^[0-9a-fA-F]{64}$' AND length(trim("IdempotencyKey"))>0 AND length(trim("CorrelationId"))>0);

        ALTER TABLE __advance_schema__.stock_movements ADD CONSTRAINT "CK_stock_movement_quantity_direction"
          CHECK (("QuantityIn">0 AND "QuantityOut"=0) OR ("QuantityOut">0 AND "QuantityIn"=0));
        ALTER TABLE __advance_schema__.stock_movements ADD CONSTRAINT "CK_stock_movement_schema_version"
          CHECK ("LedgerSchemaVersion" IN (1,2));
        ALTER TABLE __advance_schema__.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK ("LedgerSchemaVersion"=1 OR (
            "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL
            AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId")=1));
        ALTER TABLE __advance_schema__.stock_movements ADD CONSTRAINT "CK_stock_movement_serial_quantity"
          CHECK ("InventorySerialId" IS NULL OR "QuantityIn"+"QuantityOut"=1);
        ALTER TABLE __advance_schema__.stock_movements ADD CONSTRAINT "CK_stock_movement_outbound_origin"
          CHECK ("LedgerSchemaVersion"=1 OR "MovementLeg" NOT IN ('ISSUE_OUT','DISPATCH_OUT') OR "OriginGoodsReceiptLineId" IS NOT NULL);

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3b_batch_insert_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $$
        DECLARE prior_hash text; expected_number text; expected_type text; source_company uuid;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stock posting batches are append-only.'; END IF;
          SELECT "RequestFingerprint" INTO prior_hash FROM __advance_schema__.stock_posting_batches
            WHERE "CompanyId"=NEW."CompanyId" AND "IdempotencyKey"=NEW."IdempotencyKey";
          IF FOUND THEN
            IF prior_hash=NEW."RequestFingerprint" THEN RETURN NULL; END IF;
            RAISE EXCEPTION 'Posting idempotency key was reused with a different fingerprint.';
          END IF;
          IF NEW."PostingKind"='GRN_CUSTODY' THEN
            expected_type:='GRN';
            SELECT "CompanyId","GrnNumber" INTO source_company,expected_number FROM __advance_schema__.goods_receipts WHERE "Id"=NEW."GoodsReceiptId";
          ELSIF NEW."PostingKind"='QC_DISPOSITION' THEN
            expected_type:='QC_INSPECTION';
            SELECT r."CompanyId",i."InspectionNumber" INTO source_company,expected_number
              FROM __advance_schema__.qc_inspection_revisions r JOIN __advance_schema__.qc_inspections i ON i."Id"=r."QcInspectionId"
              WHERE r."Id"=NEW."QcInspectionRevisionId";
          ELSIF NEW."PostingKind"='MATERIAL_ISSUE' THEN
            expected_type:='MATERIAL_ISSUE_REQUEST';
            SELECT "CompanyId","RequestNumber" INTO source_company,expected_number FROM __advance_schema__.material_issue_requests WHERE "Id"=NEW."MaterialIssueRequestId";
          ELSIF NEW."PostingKind" IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN
            expected_type:='DELIVERY_CHALLAN';
            SELECT "CompanyId","DcNumber" INTO source_company,expected_number FROM __advance_schema__.delivery_challans WHERE "Id"=NEW."DeliveryChallanId";
          ELSE
            expected_type:='REVERSAL';
            SELECT "CompanyId","ReferenceNumber" INTO source_company,expected_number FROM __advance_schema__.stock_posting_batches WHERE "Id"=NEW."ReversesPostingBatchId";
          END IF;
          IF source_company IS NULL OR source_company<>NEW."CompanyId" OR expected_number<>NEW."ReferenceNumber" OR expected_type<>NEW."ReferenceType" THEN
            RAISE EXCEPTION 'Posting batch company/reference must be derived from its typed source.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_stock_posting_batch_guard" BEFORE INSERT OR UPDATE OR DELETE
          ON __advance_schema__.stock_posting_batches FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_batch_insert_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3b_movement_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $$
        DECLARE b __advance_schema__.stock_posting_batches%ROWTYPE; loc __advance_schema__.warehouse_condition_locations%ROWTYPE;
                source_company uuid; source_header uuid; source_item uuid; original __advance_schema__.stock_movements%ROWTYPE;
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Stock movements are append-only; post a reversal.'; END IF;
          IF NEW."LedgerSchemaVersion"<>2 THEN RAISE EXCEPTION 'New stock movements must use ledger schema version 2.'; END IF;
          SELECT * INTO b FROM __advance_schema__.stock_posting_batches WHERE "Id"=NEW."StockPostingBatchId";
          IF NOT FOUND OR b."CompanyId"<>NEW."CompanyId" OR b."PostingDate"<>NEW."PostingDate"
             OR (NEW."ReferenceType",NEW."ReferenceNumber") IS DISTINCT FROM (b."ReferenceType",b."ReferenceNumber") THEN
            RAISE EXCEPTION 'Movement batch, company, posting date and reference snapshots must agree.';
          END IF;
          SELECT * INTO loc FROM __advance_schema__.warehouse_condition_locations WHERE "Id"=NEW."WarehouseConditionLocationId";
          IF NOT FOUND OR (loc."CompanyId",loc."WarehouseId",loc."RackBinId",loc."ConditionCode")
             IS DISTINCT FROM (NEW."CompanyId",NEW."WarehouseId",NEW."RackBinId",NEW."ConditionCode")
             OR NOT loc."IsActive" OR loc."EffectiveFrom">NEW."PostingDate" OR (loc."EffectiveTo" IS NOT NULL AND loc."EffectiveTo"<NEW."PostingDate") THEN
            RAISE EXCEPTION 'Movement condition/location snapshot is invalid or ineffective.';
          END IF;
          IF NEW."GoodsReceiptLineId" IS NOT NULL THEN
            SELECT l."CompanyId",l."GoodsReceiptId",l."ItemId" INTO source_company,source_header,source_item FROM __advance_schema__.goods_receipt_lines l WHERE l."Id"=NEW."GoodsReceiptLineId";
            IF b."PostingKind" NOT IN ('GRN_CUSTODY','REVERSAL') OR (b."PostingKind"='GRN_CUSTODY' AND source_header<>b."GoodsReceiptId") THEN RAISE EXCEPTION 'GRN movement source does not match its batch.'; END IF;
            IF b."PostingKind"='GRN_CUSTODY' AND ((NEW."ConditionCode"='QC_HOLD' AND NEW."WarehouseConditionLocationId"<>(SELECT "QcHoldConditionLocationIdSnapshot" FROM __advance_schema__.goods_receipt_lines WHERE "Id"=NEW."GoodsReceiptLineId"))
               OR (NEW."ConditionCode"='PENDING_RETURNABLE_DC' AND NOT EXISTS (SELECT 1 FROM __advance_schema__.goods_receipt_lines l JOIN __advance_schema__.warehouse_condition_locations q ON q."Id"=l."QcHoldConditionLocationIdSnapshot" WHERE l."Id"=NEW."GoodsReceiptLineId" AND q."WarehouseId"=NEW."WarehouseId" AND q."RackBinId"=NEW."RackBinId"))
               OR NEW."ConditionCode" NOT IN ('QC_HOLD','PENDING_RETURNABLE_DC')) THEN RAISE EXCEPTION 'GRN custody must use its snapshotted category QC rack.'; END IF;
          ELSIF NEW."QcInspectionRevisionId" IS NOT NULL THEN
            SELECT r."CompanyId",r."Id",coalesce(gl."ItemId",dl."ItemId") INTO source_company,source_header,source_item
              FROM __advance_schema__.qc_inspection_revisions r JOIN __advance_schema__.qc_inspections i ON i."Id"=r."QcInspectionId"
              LEFT JOIN __advance_schema__.goods_receipt_lines gl ON gl."Id"=i."GoodsReceiptLineId"
              LEFT JOIN __advance_schema__.delivery_challan_lines dl ON dl."Id"=i."DeliveryChallanLineId" WHERE r."Id"=NEW."QcInspectionRevisionId";
            IF b."PostingKind" NOT IN ('QC_DISPOSITION','REVERSAL') OR (b."PostingKind"='QC_DISPOSITION' AND source_header<>b."QcInspectionRevisionId") THEN RAISE EXCEPTION 'QC movement source does not match its batch.'; END IF;
            IF b."PostingKind"='QC_DISPOSITION' AND NOT EXISTS (SELECT 1 FROM __advance_schema__.qc_inspection_revisions r WHERE r."Id"=NEW."QcInspectionRevisionId" AND
                ((NEW."MovementLeg"='TRANSFER_OUT' AND NEW."WarehouseConditionLocationId"=r."QcHoldConditionLocationIdSnapshot" AND NEW."ConditionCode"='QC_HOLD')
                 OR (NEW."MovementLeg"='TRANSFER_IN' AND NEW."WarehouseConditionLocationId"=r."AcceptedConditionLocationId" AND NEW."ConditionCode"='AVAILABLE')
                 OR (NEW."MovementLeg"='TRANSFER_IN' AND NEW."WarehouseConditionLocationId"=r."PendingReturnConditionLocationIdSnapshot" AND NEW."ConditionCode"='PENDING_RETURNABLE_DC'))) THEN RAISE EXCEPTION 'QC movement does not follow its snapshotted hold/accepted/rejected route.'; END IF;
          ELSIF NEW."MaterialIssueRequestLineId" IS NOT NULL THEN
            SELECT "CompanyId","MaterialIssueRequestId","ItemId" INTO source_company,source_header,source_item FROM __advance_schema__.material_issue_request_lines WHERE "Id"=NEW."MaterialIssueRequestLineId";
            IF b."PostingKind" NOT IN ('MATERIAL_ISSUE','REVERSAL') OR (b."PostingKind"='MATERIAL_ISSUE' AND source_header<>b."MaterialIssueRequestId") THEN RAISE EXCEPTION 'Issue movement source does not match its batch.'; END IF;
            IF b."PostingKind"='MATERIAL_ISSUE' AND (NEW."MovementLeg"<>'ISSUE_OUT' OR NEW."ConditionCode"<>'AVAILABLE') THEN RAISE EXCEPTION 'Material issue may consume AVAILABLE stock only.'; END IF;
          ELSE
            SELECT "CompanyId","DeliveryChallanId","ItemId" INTO source_company,source_header,source_item FROM __advance_schema__.delivery_challan_lines WHERE "Id"=NEW."DeliveryChallanLineId";
            IF b."PostingKind" NOT IN ('DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL') OR (b."PostingKind"<>'REVERSAL' AND source_header<>b."DeliveryChallanId") THEN RAISE EXCEPTION 'DC movement source does not match its batch.'; END IF;
            IF b."PostingKind"='DC_DISPATCH' AND (NEW."MovementLeg"<>'DISPATCH_OUT' OR NEW."ConditionCode"<>(SELECT CASE WHEN h."Purpose"='REJECTED_MATERIAL' THEN 'PENDING_RETURNABLE_DC' ELSE 'AVAILABLE' END FROM __advance_schema__.delivery_challans h WHERE h."Id"=b."DeliveryChallanId")) THEN RAISE EXCEPTION 'DC dispatch uses an invalid custody condition.'; END IF;
            IF b."PostingKind"='DC_RETURN_CUSTODY' AND (NEW."MovementLeg"<>'RETURN_IN' OR NEW."ConditionCode"<>(SELECT CASE WHEN l."RequiresQcSnapshot" THEN 'QC_HOLD' ELSE 'AVAILABLE' END FROM __advance_schema__.delivery_challan_lines l WHERE l."Id"=NEW."DeliveryChallanLineId")) THEN RAISE EXCEPTION 'DC return custody condition does not match its QC requirement.'; END IF;
          END IF;
          IF source_company IS NULL OR (source_company,source_item) IS DISTINCT FROM (NEW."CompanyId",NEW."ItemId") THEN RAISE EXCEPTION 'Movement company/item must match its typed source.'; END IF;
          IF NEW."InventorySerialId" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM __advance_schema__.inventory_serials s WHERE s."Id"=NEW."InventorySerialId" AND s."CompanyId"=NEW."CompanyId" AND s."ItemId"=NEW."ItemId") THEN RAISE EXCEPTION 'Movement serial company/item mismatch.'; END IF;
          IF NEW."InventorySerialId" IS NOT NULL AND NEW."OriginGoodsReceiptLineId" IS NOT NULL AND NOT EXISTS
             (SELECT 1 FROM __advance_schema__.goods_receipt_line_serials s WHERE s."CompanyId"=NEW."CompanyId" AND s."InventorySerialId"=NEW."InventorySerialId" AND s."GoodsReceiptLineId"=NEW."OriginGoodsReceiptLineId") THEN RAISE EXCEPTION 'Serialized movement origin does not match receipt provenance.'; END IF;
          IF b."PostingKind"='REVERSAL' THEN
            SELECT * INTO original FROM __advance_schema__.stock_movements WHERE "Id"=NEW."ReversesStockMovementId";
            IF NOT FOUND OR original."StockPostingBatchId"<>b."ReversesPostingBatchId" OR NEW."MovementLeg"<>'REVERSAL'
               OR (NEW."CompanyId",NEW."ItemId",NEW."WarehouseConditionLocationId",NEW."ConditionCode",NEW."InventorySerialId",NEW."OriginGoodsReceiptLineId",
                   NEW."GoodsReceiptLineId",NEW."QcInspectionRevisionId",NEW."MaterialIssueRequestLineId",NEW."DeliveryChallanLineId",NEW."QuantityIn",NEW."QuantityOut")
                  IS DISTINCT FROM
                  (original."CompanyId",original."ItemId",original."WarehouseConditionLocationId",original."ConditionCode",original."InventorySerialId",original."OriginGoodsReceiptLineId",
                   original."GoodsReceiptLineId",original."QcInspectionRevisionId",original."MaterialIssueRequestLineId",original."DeliveryChallanLineId",original."QuantityOut",original."QuantityIn") THEN
              RAISE EXCEPTION 'Reversal movement must exactly negate one target movement.';
            END IF;
          ELSIF NEW."ReversesStockMovementId" IS NOT NULL OR NEW."MovementLeg"='REVERSAL' THEN RAISE EXCEPTION 'Only reversal batches may reverse movements.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_stock_movement_append_only_contract" BEFORE INSERT OR UPDATE OR DELETE
          ON __advance_schema__.stock_movements FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_movement_guard();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3b_reconcile_batch()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $$
        DECLARE batch_id uuid; b __advance_schema__.stock_posting_batches%ROWTYPE; source_status text; source_direction text;
        BEGIN
          batch_id:=CASE WHEN TG_TABLE_NAME='stock_posting_batches' THEN NEW."Id" ELSE NEW."StockPostingBatchId" END;
          SELECT * INTO b FROM __advance_schema__.stock_posting_batches WHERE "Id"=batch_id;
          IF NOT FOUND THEN RETURN NULL; END IF;
          IF NOT EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id") THEN RAISE EXCEPTION 'Posting batch must contain movements.'; END IF;
          IF b."PostingKind"='GRN_CUSTODY' THEN
            SELECT "Status" INTO source_status FROM __advance_schema__.goods_receipts WHERE "Id"=b."GoodsReceiptId";
            IF source_status<>'FINALIZED' OR EXISTS (
              SELECT 1 FROM __advance_schema__.goods_receipt_lines l WHERE l."GoodsReceiptId"=b."GoodsReceiptId" AND
                ((SELECT coalesce(sum(m."QuantityIn"),0) FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."GoodsReceiptLineId"=l."Id")<>l."DeliveredQuantitySnapshot"
                 OR (SELECT coalesce(sum(m."QuantityOut"),0) FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."GoodsReceiptLineId"=l."Id")<>0
                 OR (SELECT coalesce(sum(m."QuantityIn"),0) FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."GoodsReceiptLineId"=l."Id" AND m."ConditionCode"='QC_HOLD')<>l."ReceivedQuantity"
                 OR (SELECT coalesce(sum(m."QuantityIn"),0) FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."GoodsReceiptLineId"=l."Id" AND m."ConditionCode"='PENDING_RETURNABLE_DC')<>l."ExcessRejectedQuantity")
            ) OR EXISTS (SELECT 1 FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."MovementLeg"<>'RECEIPT_IN') THEN
              RAISE EXCEPTION 'GRN custody batch does not reconcile ordered QC hold and excess return custody.';
            END IF;
          ELSIF b."PostingKind"='QC_DISPOSITION' THEN
            SELECT "Status" INTO source_status FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId";
            IF source_status<>'FINALIZED' OR
               (SELECT coalesce(sum("QuantityIn"),0) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id")<>
               (SELECT "AcceptedQuantity"+"RejectedQuantity" FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId") OR
               (SELECT coalesce(sum("QuantityOut"),0) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id")<>
               (SELECT "AcceptedQuantity"+"RejectedQuantity" FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId") OR
               (SELECT coalesce(sum("QuantityOut"),0) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id" AND "ConditionCode"='QC_HOLD')<>
               (SELECT "AcceptedQuantity"+"RejectedQuantity" FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId") OR
               (SELECT coalesce(sum("QuantityIn"),0) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id" AND "ConditionCode"='AVAILABLE')<>
               (SELECT "AcceptedQuantity" FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId") OR
               (SELECT coalesce(sum("QuantityIn"),0) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id" AND "ConditionCode"='PENDING_RETURNABLE_DC')<>
               (SELECT "RejectedQuantity" FROM __advance_schema__.qc_inspection_revisions WHERE "Id"=b."QcInspectionRevisionId") OR
               EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id" AND "MovementLeg" NOT IN ('TRANSFER_OUT','TRANSFER_IN')) THEN
              RAISE EXCEPTION 'QC disposition batch must be a balanced hold-to-destination transfer.';
            END IF;
          ELSIF b."PostingKind"='MATERIAL_ISSUE' THEN
            SELECT "Status" INTO source_status FROM __advance_schema__.material_issue_requests WHERE "Id"=b."MaterialIssueRequestId";
            IF source_status NOT IN ('PARTIALLY_FULFILLED','FULFILLED') OR EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id" AND "MovementLeg"<>'ISSUE_OUT')
               OR EXISTS (SELECT 1 FROM __advance_schema__.material_issue_request_lines l WHERE l."MaterialIssueRequestId"=b."MaterialIssueRequestId" AND
                   (SELECT coalesce(sum(m."QuantityOut"-m."QuantityIn"),0) FROM __advance_schema__.stock_movements m WHERE m."MaterialIssueRequestLineId"=l."Id")>l."RequestedQuantity") THEN
              RAISE EXCEPTION 'Material issue batch exceeds or bypasses the approved request.';
            END IF;
          ELSIF b."PostingKind" IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN
            SELECT "Status","Direction" INTO source_status,source_direction FROM __advance_schema__.delivery_challans WHERE "Id"=b."DeliveryChallanId";
            IF (b."PostingKind"='DC_DISPATCH' AND (source_direction<>'OUTBOUND' OR source_status NOT IN ('DISPATCHED','OUTSTANDING','CLOSED')))
               OR (b."PostingKind"='DC_RETURN_CUSTODY' AND (source_direction<>'INBOUND_RETURN' OR source_status NOT IN ('RECEIVED','CLOSED')))
               OR EXISTS (SELECT 1 FROM __advance_schema__.delivery_challan_lines l WHERE l."DeliveryChallanId"=b."DeliveryChallanId" AND
                    (SELECT coalesce(sum(CASE WHEN source_direction='OUTBOUND' THEN m."QuantityOut" ELSE m."QuantityIn" END),0)
                     FROM __advance_schema__.stock_movements m WHERE m."StockPostingBatchId"=b."Id" AND m."DeliveryChallanLineId"=l."Id")<>l."Quantity") THEN
              RAISE EXCEPTION 'Delivery Challan posting does not reconcile its direction and lines.';
            END IF;
          ELSE
            IF (SELECT count(*) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."Id")<>
               (SELECT count(*) FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId"=b."ReversesPostingBatchId")
               OR EXISTS (SELECT 1 FROM __advance_schema__.stock_movements original WHERE original."StockPostingBatchId"=b."ReversesPostingBatchId"
                    AND NOT EXISTS (SELECT 1 FROM __advance_schema__.stock_movements reversal WHERE reversal."StockPostingBatchId"=b."Id" AND reversal."ReversesStockMovementId"=original."Id")) THEN
              RAISE EXCEPTION 'Reversal batch must negate every target movement exactly once.';
            END IF;
          END IF;
          RETURN NULL;
        END $$;
        CREATE CONSTRAINT TRIGGER "TR_stock_posting_batch_reconcile" AFTER INSERT ON __advance_schema__.stock_posting_batches
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_reconcile_batch();
        CREATE CONSTRAINT TRIGGER "TR_stock_movement_batch_reconcile" AFTER INSERT ON __advance_schema__.stock_movements
          DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_reconcile_batch();

        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3b_document_posting_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $$
        BEGIN
          IF TG_TABLE_NAME='goods_receipts' AND NEW."Status"='FINALIZED' AND
             (SELECT count(*) FROM __advance_schema__.stock_posting_batches WHERE "GoodsReceiptId"=NEW."Id" AND "PostingKind"='GRN_CUSTODY')<>1 THEN RAISE EXCEPTION 'Finalised GRN requires exactly one atomic custody posting batch.';
          ELSIF TG_TABLE_NAME='qc_inspection_revisions' AND NEW."Status"='FINALIZED' AND
             (SELECT count(*) FROM __advance_schema__.stock_posting_batches WHERE "QcInspectionRevisionId"=NEW."Id" AND "PostingKind"='QC_DISPOSITION')<>1 THEN RAISE EXCEPTION 'Finalised QC revision requires exactly one atomic disposition batch.';
          ELSIF TG_TABLE_NAME='material_issue_requests' AND NEW."Status" IN ('PARTIALLY_FULFILLED','FULFILLED') AND
             NOT EXISTS (SELECT 1 FROM __advance_schema__.stock_posting_batches WHERE "MaterialIssueRequestId"=NEW."Id" AND "PostingKind"='MATERIAL_ISSUE') THEN RAISE EXCEPTION 'Fulfilled material request requires an approved atomic issue batch.';
          ELSIF TG_TABLE_NAME='delivery_challans' AND NEW."Status" IN ('DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED') AND
             NOT EXISTS (SELECT 1 FROM __advance_schema__.stock_posting_batches WHERE "DeliveryChallanId"=NEW."Id" AND "PostingKind" IN ('DC_DISPATCH','DC_RETURN_CUSTODY')) THEN RAISE EXCEPTION 'Delivery Challan inventory transition requires an atomic posting batch.';
          END IF;
          RETURN NULL;
        END $$;
        CREATE CONSTRAINT TRIGGER "TR_goods_receipt_atomic_posting" AFTER INSERT OR UPDATE ON __advance_schema__.goods_receipts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_document_posting_guard();
        CREATE CONSTRAINT TRIGGER "TR_qc_revision_atomic_posting" AFTER INSERT OR UPDATE ON __advance_schema__.qc_inspection_revisions DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_document_posting_guard();
        CREATE CONSTRAINT TRIGGER "TR_mir_atomic_posting" AFTER INSERT OR UPDATE ON __advance_schema__.material_issue_requests DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_document_posting_guard();
        CREATE CONSTRAINT TRIGGER "TR_dc_atomic_posting" AFTER INSERT OR UPDATE ON __advance_schema__.delivery_challans DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3b_document_posting_guard();

        -- Activation is last: all complete ledger and atomic-document guards now exist.
        DROP TRIGGER "TR_qc_revision_part3a_block" ON __advance_schema__.qc_inspection_revisions;
        DROP TRIGGER "TR_mir_part3a_block" ON __advance_schema__.material_issue_requests;
        DROP TRIGGER "TR_dc_part3a_block" ON __advance_schema__.delivery_challans;
        DROP FUNCTION __advance_schema__.stores_p3a_transition_block();

        DO $witness$ BEGIN
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "LedgerSchemaVersion"<>1) THEN RAISE EXCEPTION 'Part 3B must preserve every pre-existing movement as version 1.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "StockPostingBatchId" IS NOT NULL OR "PostingIdentity" IS NOT NULL OR num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId")<>0) THEN RAISE EXCEPTION 'Part 3B must not invent legacy provenance.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_posting_batches) THEN RAISE EXCEPTION 'Part 3B posting batches must be empty immediately after apply.'; END IF;
        END $witness$;
        """;

    private const string DownSql = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'First Stores Part 3B down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'First Stores Part 3B down refuses a PostgreSQL administrative database.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_posting_batches) THEN RAISE EXCEPTION 'Part 3B rollback refuses any posting batch.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_movements WHERE "LedgerSchemaVersion"=2) THEN RAISE EXCEPTION 'Part 3B rollback refuses any version-2 movement.'; END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.goods_receipts WHERE "Status"='FINALIZED')
             OR EXISTS (SELECT 1 FROM __advance_schema__.qc_inspection_revisions WHERE "Status"='FINALIZED')
             OR EXISTS (SELECT 1 FROM __advance_schema__.material_issue_requests WHERE "Status" IN ('PARTIALLY_FULFILLED','FULFILLED','REVERSED'))
             OR EXISTS (SELECT 1 FROM __advance_schema__.delivery_challans WHERE "Status" IN ('DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED','REVERSED')) THEN
            RAISE EXCEPTION 'Part 3B rollback refuses any dependent document that crossed an inventory transition.';
          END IF;
          IF EXISTS (SELECT 1 FROM __advance_schema__.stock_movements
                     WHERE abs("QuantityIn")>=1000000000000000 OR abs("QuantityOut")>=1000000000000000
                        OR "QuantityIn"<>round("QuantityIn",3) OR "QuantityOut"<>round("QuantityOut",3)) THEN
            RAISE EXCEPTION 'Part 3B rollback cannot safely narrow retained legacy quantities to numeric(18,3).';
          END IF;
        END $guard$;

        -- Fail closed before dismantling: restore the Part 3A blockers first.
        CREATE OR REPLACE FUNCTION __advance_schema__.stores_p3a_transition_block()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,__advance_schema__ AS $$
        BEGIN
          IF TG_TABLE_NAME='qc_inspection_revisions' AND NEW."Status"='FINALIZED' THEN RAISE EXCEPTION 'QC finalisation is disabled until Stores Part 3B installs atomic ledger posting.';
          ELSIF TG_TABLE_NAME='material_issue_requests' AND NEW."Status" IN ('PARTIALLY_FULFILLED','FULFILLED','REVERSED') THEN RAISE EXCEPTION 'Material issue posting/finalisation is disabled until Stores Part 3B.';
          ELSIF TG_TABLE_NAME='delivery_challans' AND NEW."Status" IN ('DISPATCHED','OUTSTANDING','PARTIALLY_RETURNED','RECEIVED','CLOSED','REVERSED') THEN RAISE EXCEPTION 'DC dispatch, receipt and finalisation are disabled until Stores Part 3B.';
          END IF;
          RETURN NEW;
        END $$;
        CREATE TRIGGER "TR_qc_revision_part3a_block" BEFORE INSERT OR UPDATE ON __advance_schema__.qc_inspection_revisions FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();
        CREATE TRIGGER "TR_mir_part3a_block" BEFORE INSERT OR UPDATE ON __advance_schema__.material_issue_requests FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();
        CREATE TRIGGER "TR_dc_part3a_block" BEFORE INSERT OR UPDATE ON __advance_schema__.delivery_challans FOR EACH ROW EXECUTE FUNCTION __advance_schema__.stores_p3a_transition_block();

        DROP TRIGGER "TR_goods_receipt_atomic_posting" ON __advance_schema__.goods_receipts;
        DROP TRIGGER "TR_qc_revision_atomic_posting" ON __advance_schema__.qc_inspection_revisions;
        DROP TRIGGER "TR_mir_atomic_posting" ON __advance_schema__.material_issue_requests;
        DROP TRIGGER "TR_dc_atomic_posting" ON __advance_schema__.delivery_challans;
        DROP TRIGGER "TR_stock_movement_batch_reconcile" ON __advance_schema__.stock_movements;
        DROP TRIGGER "TR_stock_posting_batch_reconcile" ON __advance_schema__.stock_posting_batches;
        DROP TRIGGER "TR_stock_movement_append_only_contract" ON __advance_schema__.stock_movements;
        DROP TRIGGER "TR_stock_posting_batch_guard" ON __advance_schema__.stock_posting_batches;
        DROP FUNCTION __advance_schema__.stores_p3b_document_posting_guard();
        DROP FUNCTION __advance_schema__.stores_p3b_reconcile_batch();
        DROP FUNCTION __advance_schema__.stores_p3b_movement_guard();
        DROP FUNCTION __advance_schema__.stores_p3b_batch_insert_guard();
        ALTER TABLE __advance_schema__.stock_movements DROP CONSTRAINT "CK_stock_movement_outbound_origin";
        ALTER TABLE __advance_schema__.stock_movements DROP CONSTRAINT "CK_stock_movement_serial_quantity";
        ALTER TABLE __advance_schema__.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE __advance_schema__.stock_movements DROP CONSTRAINT "CK_stock_movement_schema_version";
        ALTER TABLE __advance_schema__.stock_movements DROP CONSTRAINT "CK_stock_movement_quantity_direction";
        """;
}
