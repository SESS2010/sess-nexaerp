namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class Foundation3InventoryProvenanceGenealogySql
{
    internal const string PreUp = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Foundation 3 requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Foundation 3 refuses a PostgreSQL administrative database.';
          END IF;
          IF to_regclass('advance.stock_movements') IS NULL
             OR to_regclass('advance.inventory_lots') IS NULL
             OR to_regclass('advance.inventory_serials') IS NULL THEN
            RAISE EXCEPTION 'Foundation 3 requires the prior Stores ledger, lot and serial foundations.';
          END IF;
        END $guard$;
        LOCK TABLE advance.stock_movements IN ACCESS EXCLUSIVE MODE;
        LOCK TABLE advance.inventory_lots, advance.inventory_serials IN SHARE ROW EXCLUSIVE MODE;
        DO $preflight$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.stock_movements) THEN
            RAISE EXCEPTION 'Foundation 3 requires zero stock_movements; no backfill or fabricated ownership/custody/provenance is permitted.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.inventory_lots l
            WHERE NOT EXISTS (
              SELECT 1 FROM advance.goods_receipt_line_lot_allocations a
              WHERE a."CompanyId"=l."CompanyId" AND a."InventoryLotId"=l."Id"))
          THEN RAISE EXCEPTION 'Foundation 3 refuses standalone inventory_lots without receipt allocation provenance.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM advance.inventory_serials s
            WHERE NOT EXISTS (
              SELECT 1 FROM advance.goods_receipt_line_serials r
              WHERE r."CompanyId"=s."CompanyId" AND r."InventorySerialId"=s."Id"))
          THEN RAISE EXCEPTION 'Foundation 3 refuses standalone inventory_serials without receipt capture provenance.';
          END IF;
        END $preflight$;
        """;

    internal const string UpContract = """
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId",
                "InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1
              AND "ReversesPostingBatchId" IS NULL)
            OR ("PostingKind"='REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId",
                "InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=0
              AND "ReversesPostingBatchId" IS NOT NULL));

        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_schema_version";
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_schema_version"
          CHECK ("LedgerSchemaVersion"=2);
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

        CREATE OR REPLACE FUNCTION advance.stores_foundation3_movement_identity_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE item_batch boolean; effective_serial_mode text; assignment_case_line uuid;
                provenance advance.inventory_provenance_layers%ROWTYPE; original advance.stock_movements%ROWTYPE;
        BEGIN
          SELECT i."BatchTracking",
                 coalesce(
                   (SELECT l."SerialCaptureModeSnapshot" FROM advance.goods_receipt_lines l
                    WHERE l."CompanyId"=NEW."CompanyId"
                      AND l."Id"=coalesce(NEW."GoodsReceiptLineId",NEW."OriginGoodsReceiptLineId")),
                   (SELECT nullif(s."SerialCaptureMode",'INHERIT') FROM advance.item_company_inventory_settings s
                    WHERE s."CompanyId"=NEW."CompanyId" AND s."ItemId"=NEW."ItemId" AND s."IsActive"),
                   CASE WHEN i."SerialNumberTracking" THEN 'REQUIRED' ELSE 'OPTIONAL' END)
            INTO item_batch,effective_serial_mode
          FROM advance.items i WHERE i."Id"=NEW."ItemId";
          IF item_batch AND NEW."InventoryLotId" IS NULL THEN
            RAISE EXCEPTION 'Effective lot policy requires InventoryLotId on every movement.';
          END IF;
          IF effective_serial_mode='REQUIRED' AND NEW."InventorySerialId" IS NULL THEN
            RAISE EXCEPTION 'Effective serial policy requires InventorySerialId on every movement.';
          END IF;
          IF NEW."InventoryLotId" IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM advance.inventory_lots l
            WHERE l."CompanyId"=NEW."CompanyId" AND l."Id"=NEW."InventoryLotId" AND l."ItemId"=NEW."ItemId") THEN
            RAISE EXCEPTION 'Movement lot company/item mismatch.';
          END IF;
          SELECT * INTO provenance FROM advance.inventory_provenance_layers p
            WHERE p."CompanyId"=NEW."CompanyId" AND p."Id"=NEW."InventoryProvenanceLayerId";
          IF NOT FOUND OR provenance."ItemId"<>NEW."ItemId"
             OR provenance."InventoryLotId" IS DISTINCT FROM NEW."InventoryLotId"
             OR provenance."InventorySerialId" IS DISTINCT FROM NEW."InventorySerialId" THEN
            RAISE EXCEPTION 'Movement provenance must match company, item, lot and serial exactly.';
          END IF;
          SELECT a."CustodyCaseLineId" INTO assignment_case_line
            FROM advance.inventory_custody_assignments a
            WHERE a."CompanyId"=NEW."CompanyId" AND a."Id"=NEW."CustodyAssignmentId";
          IF NOT FOUND OR assignment_case_line IS DISTINCT FROM NEW."CustodyCaseLineId" THEN
            RAISE EXCEPTION 'Movement custody assignment and custody case line must agree.';
          END IF;
          IF NEW."GoodsReceiptLineLotAllocationId" IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM advance.goods_receipt_line_lot_allocations a
            WHERE a."CompanyId"=NEW."CompanyId" AND a."Id"=NEW."GoodsReceiptLineLotAllocationId"
              AND a."InventoryLotId"=NEW."InventoryLotId") THEN
            RAISE EXCEPTION 'Movement receipt allocation and lot must agree.';
          END IF;
          IF NEW."ReversesStockMovementId" IS NOT NULL THEN
            SELECT * INTO original FROM advance.stock_movements m WHERE m."Id"=NEW."ReversesStockMovementId";
            IF NOT FOUND OR
              (NEW."OwnershipAccountId",NEW."CustodyAssignmentId",NEW."CustodyCaseLineId",NEW."InventoryProvenanceLayerId",
               NEW."InventoryLotId",NEW."InventorySerialId",NEW."GoodsReceiptLineLotAllocationId",
               NEW."QcInspectionLotDispositionId",NEW."InventoryCustodyHandoffLineId",NEW."InventoryOwnershipTransferLineId",
               NEW."InventoryTransformationInputId",NEW."InventoryTransformationOutputId",NEW."InventoryConcessionAllocationId")
              IS DISTINCT FROM
              (original."OwnershipAccountId",original."CustodyAssignmentId",original."CustodyCaseLineId",original."InventoryProvenanceLayerId",
               original."InventoryLotId",original."InventorySerialId",original."GoodsReceiptLineLotAllocationId",
               original."QcInspectionLotDispositionId",original."InventoryCustodyHandoffLineId",original."InventoryOwnershipTransferLineId",
               original."InventoryTransformationInputId",original."InventoryTransformationOutputId",original."InventoryConcessionAllocationId")
            THEN RAISE EXCEPTION 'Reversal must preserve exact ownership, custody, provenance, lot, serial and source identity.';
            END IF;
          END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER "TR_stock_movement_foundation3_identity"
          BEFORE INSERT ON advance.stock_movements
          FOR EACH ROW EXECUTE FUNCTION advance.stores_foundation3_movement_identity_guard();

        CREATE OR REPLACE FUNCTION advance.foundation3_prepare_grn_legs(
          p_company_id uuid,p_goods_receipt_id uuid,p_actor_login text,p_legs jsonb)
        RETURNS jsonb LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        DECLARE holder_id uuid; ownership_id uuid; custody_id uuid; assignment_id uuid; provenance_id uuid;
                leg jsonb; enriched jsonb:='[]'::jsonb; item_id uuid; location_id uuid; allocation_id uuid;
                lot_id uuid; serial_id uuid; warehouse_id uuid; rack_bin_id uuid; uom_id uuid;
                quantity numeric; account_code text; identity_hash text;
        BEGIN
          INSERT INTO advance.inventory_account_holders
            ("Id","CompanyId","HolderType","HolderCompanyId","HolderCode","HolderNameSnapshot","IsActive","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),p_company_id,'COMPANY',p_company_id,'COMPANY-INVENTORY',c."LegalName",true,clock_timestamp(),p_actor_login,0
          FROM advance.companies c WHERE c."Id"=p_company_id
          ON CONFLICT ("CompanyId","HolderCode") DO NOTHING;
          SELECT "Id" INTO holder_id FROM advance.inventory_account_holders
            WHERE "CompanyId"=p_company_id AND "HolderCode"='COMPANY-INVENTORY'
              AND "HolderType"='COMPANY' AND "HolderCompanyId"=p_company_id AND "IsActive";
          IF holder_id IS NULL THEN RAISE EXCEPTION 'Foundation 3 company inventory holder is missing or incompatible.'; END IF;

          INSERT INTO advance.inventory_ownership_accounts
            ("Id","CompanyId","AccountHolderId","AccountCode","OwnershipType","InventoryValuationBasis","CurrencyCode","IsActive","CreatedAt","CreatedBy","Version")
          VALUES (gen_random_uuid(),p_company_id,holder_id,'SESS-INVENTORY','SESS_INVENTORY','FIFO','INR',true,clock_timestamp(),p_actor_login,0)
          ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
          SELECT "Id" INTO ownership_id FROM advance.inventory_ownership_accounts
            WHERE "CompanyId"=p_company_id AND "AccountCode"='SESS-INVENTORY'
              AND "AccountHolderId"=holder_id AND "OwnershipType"='SESS_INVENTORY'
              AND "InventoryValuationBasis"='FIFO' AND "IsActive";
          IF ownership_id IS NULL THEN RAISE EXCEPTION 'Foundation 3 SESS inventory ownership account is missing or incompatible.'; END IF;

          FOR leg IN SELECT value FROM jsonb_array_elements(p_legs) ORDER BY (value->>'batchLineOrdinal')::integer
          LOOP
            item_id:=(leg->>'itemId')::uuid;
            location_id:=(leg->>'warehouseConditionLocationId')::uuid;
            allocation_id:=(leg->>'goodsReceiptLineLotAllocationId')::uuid;
            serial_id:=(leg->>'inventorySerialId')::uuid;
            quantity:=(leg->>'quantityIn')::numeric;
            SELECT a."InventoryLotId" INTO STRICT lot_id FROM advance.goods_receipt_line_lot_allocations a
              JOIN advance.goods_receipt_lines l ON l."Id"=a."GoodsReceiptLineId"
              WHERE a."CompanyId"=p_company_id AND a."Id"=allocation_id
                AND l."GoodsReceiptId"=p_goods_receipt_id AND l."ItemId"=item_id;
            SELECT w."WarehouseId",w."RackBinId" INTO STRICT warehouse_id,rack_bin_id
              FROM advance.warehouse_condition_locations w
              WHERE w."CompanyId"=p_company_id AND w."Id"=location_id;
            SELECT i."BaseUomId" INTO STRICT uom_id FROM advance.items i WHERE i."Id"=item_id;
            account_code:='WH-'||left(replace(warehouse_id::text,'-',''),12)||'-'||left(replace(rack_bin_id::text,'-',''),12);
            INSERT INTO advance.inventory_custody_accounts
              ("Id","CompanyId","AccountHolderId","AccountCode","CustodyType","WarehouseId","RackBinId","IsActive","CreatedAt","CreatedBy","Version")
            VALUES (gen_random_uuid(),p_company_id,holder_id,account_code,'WAREHOUSE',warehouse_id,rack_bin_id,true,clock_timestamp(),p_actor_login,0)
            ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
            SELECT "Id" INTO custody_id FROM advance.inventory_custody_accounts
              WHERE "CompanyId"=p_company_id AND "AccountCode"=account_code AND "AccountHolderId"=holder_id
                AND "CustodyType"='WAREHOUSE' AND "WarehouseId"=warehouse_id
                AND "RackBinId"=rack_bin_id AND "IsActive";
            IF custody_id IS NULL THEN RAISE EXCEPTION 'Foundation 3 warehouse custody account is missing or incompatible.'; END IF;

            assignment_id:=md5('F3:CUST:'||p_company_id||':'||p_goods_receipt_id||':'||allocation_id||':'||coalesce(serial_id::text,'BULK'))::uuid;
            INSERT INTO advance.inventory_custody_assignments
              ("Id","CompanyId","CustodyAccountId","WarehouseId","RackBinId","AssignedQuantity","EffectiveFrom","IsCurrent","AssignmentReason","CreatedAt","CreatedBy","Version")
            VALUES (assignment_id,p_company_id,custody_id,warehouse_id,rack_bin_id,quantity,clock_timestamp(),true,
              'GRN custody receipt '||p_goods_receipt_id,clock_timestamp(),p_actor_login,0)
            ON CONFLICT ("Id") DO NOTHING;

            identity_hash:=encode(public.digest(
              convert_to('F3:RECEIPT:'||p_company_id||':'||p_goods_receipt_id||':'||allocation_id||':'||coalesce(serial_id::text,'BULK'),'UTF8'),'sha256'),'hex');
            INSERT INTO advance.inventory_provenance_layers
              ("Id","CompanyId","ItemId","InventoryLotId","InventorySerialId","LayerType","QuantityCreated","UomId","Status","IdentityHash","CreatedAt","CreatedBy")
            VALUES (gen_random_uuid(),p_company_id,item_id,lot_id,serial_id,'RECEIPT',quantity,uom_id,'ACTIVE',identity_hash,clock_timestamp(),p_actor_login)
            ON CONFLICT ("CompanyId","IdentityHash") DO NOTHING;
            SELECT "Id" INTO provenance_id FROM advance.inventory_provenance_layers
              WHERE "CompanyId"=p_company_id AND "IdentityHash"=identity_hash
                AND "ItemId"=item_id AND "InventoryLotId"=lot_id
                AND "InventorySerialId" IS NOT DISTINCT FROM serial_id;
            IF provenance_id IS NULL THEN RAISE EXCEPTION 'Foundation 3 receipt provenance identity is incompatible.'; END IF;
            INSERT INTO advance.inventory_provenance_goods_receipt_lot_origins
              ("Id","CompanyId","InventoryProvenanceLayerId","OriginRole","CreatedAt","CreatedBy","GoodsReceiptLineLotAllocationId")
            VALUES (gen_random_uuid(),p_company_id,provenance_id,'PRIMARY',clock_timestamp(),p_actor_login,allocation_id)
            ON CONFLICT DO NOTHING;

            enriched:=enriched||jsonb_build_array(leg||jsonb_build_object(
              'ownershipAccountId',ownership_id,'custodyAssignmentId',assignment_id,
              'inventoryProvenanceLayerId',provenance_id,'inventoryLotId',lot_id));
          END LOOP;
          RETURN enriched;
        END $function$;
        REVOKE ALL ON FUNCTION advance.foundation3_prepare_grn_legs(uuid,uuid,text,jsonb) FROM PUBLIC;
        """;

    internal const string ControlledPosting = """
        CREATE OR REPLACE FUNCTION advance.post_stores_stock_batch(p_company_id uuid,p_posting_kind text,p_source_id uuid,p_idempotency_key text,p_request_fingerprint text,p_correlation_id text,p_posting_date date,p_posted_by_employee_id uuid,p_created_by text,p_legs jsonb)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE b uuid; old_hash text; ref_type text; ref_number text; source_company uuid; lock_key text;
        BEGIN
          IF p_company_id IS NULL OR p_source_id IS NULL OR p_posted_by_employee_id IS NULL THEN RAISE EXCEPTION 'Company, source and posting employee are required.'; END IF;
          IF p_posting_kind NOT IN ('GRN_CUSTODY','QC_DISPOSITION','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL') THEN RAISE EXCEPTION 'Unsupported Stores posting kind %.',p_posting_kind; END IF;
          IF length(trim(coalesce(p_idempotency_key,'')))=0 OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$' OR length(trim(coalesce(p_correlation_id,'')))=0 OR length(trim(coalesce(p_created_by,'')))=0 THEN RAISE EXCEPTION 'Posting idempotency, fingerprint, correlation and actor are required.'; END IF;
          IF jsonb_typeof(p_legs)<>'array' OR jsonb_array_length(p_legs)=0 THEN RAISE EXCEPTION 'A complete non-empty posting leg array is required.'; END IF;
          IF p_posting_kind='GRN_CUSTODY' THEN
            p_legs:=advance.foundation3_prepare_grn_legs(p_company_id,p_source_id,trim(p_created_by),p_legs);
          ELSIF p_posting_kind='REVERSAL' THEN
            SELECT jsonb_agg(e.value||jsonb_build_object(
              'ownershipAccountId',m."OwnershipAccountId",'custodyAssignmentId',m."CustodyAssignmentId",
              'custodyCaseLineId',m."CustodyCaseLineId",'inventoryProvenanceLayerId',m."InventoryProvenanceLayerId",
              'inventoryLotId',m."InventoryLotId",'inventorySerialId',m."InventorySerialId",
              'goodsReceiptLineLotAllocationId',m."GoodsReceiptLineLotAllocationId",
              'qcInspectionLotDispositionId',m."QcInspectionLotDispositionId",
              'inventoryCustodyHandoffLineId',m."InventoryCustodyHandoffLineId",
              'inventoryOwnershipTransferLineId',m."InventoryOwnershipTransferLineId",
              'inventoryTransformationInputId',m."InventoryTransformationInputId",
              'inventoryTransformationOutputId',m."InventoryTransformationOutputId",
              'inventoryConcessionAllocationId',m."InventoryConcessionAllocationId")
              ORDER BY (e.value->>'batchLineOrdinal')::integer) INTO p_legs
            FROM jsonb_array_elements(p_legs) e
            JOIN advance.stock_movements m ON m."CompanyId"=p_company_id
              AND m."Id"=(e.value->>'reversesStockMovementId')::uuid;
          END IF;
          IF EXISTS (
            SELECT 1 FROM jsonb_to_recordset(p_legs) AS x(
              "batchLineOrdinal" integer,"itemId" uuid,"warehouseConditionLocationId" uuid,
              "ownershipAccountId" uuid,"custodyAssignmentId" uuid,"inventoryProvenanceLayerId" uuid)
            WHERE x."batchLineOrdinal" IS NULL OR x."batchLineOrdinal"<=0 OR x."itemId" IS NULL
               OR x."warehouseConditionLocationId" IS NULL OR x."ownershipAccountId" IS NULL
               OR x."custodyAssignmentId" IS NULL OR x."inventoryProvenanceLayerId" IS NULL)
          THEN RAISE EXCEPTION 'Every Foundation 3 leg requires ordinal, item, location, ownership, custody and provenance.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STORES:IDEMP:'||p_company_id||':'||trim(p_idempotency_key),0));
          SELECT "Id","RequestFingerprint" INTO b,old_hash FROM advance.stock_posting_batches WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=trim(p_idempotency_key);
          IF FOUND THEN
            IF old_hash=p_request_fingerprint THEN RETURN QUERY SELECT b,true; RETURN; END IF;
            RAISE EXCEPTION 'Posting idempotency key was reused with a different fingerprint.';
          END IF;
          -- Canonical deadlock prevention: source -> ownership -> custody -> provenance -> item -> lot -> location -> serial.
          FOR lock_key IN
            WITH leg AS (
              SELECT * FROM jsonb_to_recordset(p_legs) AS x(
                "itemId" uuid,"warehouseConditionLocationId" uuid,"ownershipAccountId" uuid,
                "custodyAssignmentId" uuid,"inventoryProvenanceLayerId" uuid,
                "inventoryLotId" uuid,"inventorySerialId" uuid)
            ), keys AS (
              SELECT '00:SRC:'||p_company_id||':'||p_posting_kind||':'||p_source_id k
              UNION SELECT '10:OWN:'||p_company_id||':'||"ownershipAccountId" FROM leg
              UNION SELECT '20:CUST:'||p_company_id||':'||"custodyAssignmentId" FROM leg
              UNION SELECT '30:PROV:'||p_company_id||':'||"inventoryProvenanceLayerId" FROM leg
              UNION SELECT '40:ITEM:'||p_company_id||':'||"itemId" FROM leg
              UNION SELECT '50:LOT:'||p_company_id||':'||"itemId"||':'||"inventoryLotId" FROM leg WHERE "inventoryLotId" IS NOT NULL
              UNION SELECT '60:LOC:'||p_company_id||':'||"itemId"||':'||"warehouseConditionLocationId" FROM leg
              UNION SELECT '70:SER:'||p_company_id||':'||"itemId"||':'||"inventorySerialId" FROM leg WHERE "inventorySerialId" IS NOT NULL)
            SELECT k FROM keys ORDER BY k
          LOOP PERFORM pg_advisory_xact_lock(hashtextextended('STORES:POST:'||lock_key,0)); END LOOP;
          IF p_posting_kind='GRN_CUSTODY' THEN ref_type:='GRN'; SELECT "CompanyId","GrnNumber" INTO source_company,ref_number FROM advance.goods_receipts WHERE "Id"=p_source_id FOR UPDATE;
          ELSIF p_posting_kind='QC_DISPOSITION' THEN ref_type:='QC_INSPECTION'; SELECT r."CompanyId",i."InspectionNumber" INTO source_company,ref_number FROM advance.qc_inspection_revisions r JOIN advance.qc_inspections i ON i."Id"=r."QcInspectionId" WHERE r."Id"=p_source_id FOR UPDATE OF r;
          ELSIF p_posting_kind='MATERIAL_ISSUE' THEN ref_type:='MATERIAL_ISSUE_REQUEST'; SELECT "CompanyId","RequestNumber" INTO source_company,ref_number FROM advance.material_issue_requests WHERE "Id"=p_source_id FOR UPDATE;
          ELSIF p_posting_kind IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN ref_type:='DELIVERY_CHALLAN'; SELECT "CompanyId","DcNumber" INTO source_company,ref_number FROM advance.delivery_challans WHERE "Id"=p_source_id FOR UPDATE;
          ELSE ref_type:='REVERSAL'; SELECT "CompanyId","ReferenceNumber" INTO source_company,ref_number FROM advance.stock_posting_batches WHERE "Id"=p_source_id FOR UPDATE; END IF;
          IF source_company IS NULL THEN RAISE EXCEPTION 'Stores posting source does not exist.'; END IF;
          IF source_company<>p_company_id THEN RAISE EXCEPTION 'Stores posting source belongs to another company.'; END IF;
          IF EXISTS (
            WITH leg AS (SELECT * FROM jsonb_to_recordset(p_legs) AS x(
              "itemId" uuid,"warehouseConditionLocationId" uuid,"ownershipAccountId" uuid,
              "custodyAssignmentId" uuid,"inventoryProvenanceLayerId" uuid,"inventoryLotId" uuid,
              "inventorySerialId" uuid,"quantityIn" numeric,"quantityOut" numeric)),
            d AS (SELECT "itemId","warehouseConditionLocationId","ownershipAccountId","custodyAssignmentId",
                         "inventoryProvenanceLayerId","inventoryLotId","inventorySerialId",sum("quantityIn"-"quantityOut") n
                  FROM leg GROUP BY 1,2,3,4,5,6,7)
            SELECT 1 FROM d JOIN advance.warehouse_condition_locations l ON l."Id"=d."warehouseConditionLocationId" AND l."CompanyId"=p_company_id
            WHERE l."ConditionCode"='AVAILABLE' AND coalesce((
              SELECT sum(m."QuantityIn"-m."QuantityOut") FROM advance.stock_movements m
              WHERE m."CompanyId"=p_company_id AND m."ItemId"=d."itemId"
                AND m."WarehouseConditionLocationId"=d."warehouseConditionLocationId"
                AND m."OwnershipAccountId"=d."ownershipAccountId" AND m."CustodyAssignmentId"=d."custodyAssignmentId"
                AND m."InventoryProvenanceLayerId"=d."inventoryProvenanceLayerId"
                AND m."InventoryLotId" IS NOT DISTINCT FROM d."inventoryLotId"
                AND m."InventorySerialId" IS NOT DISTINCT FROM d."inventorySerialId"),0)+d.n<0)
          THEN RAISE EXCEPTION 'Posting would drive an AVAILABLE ownership/custody/provenance layer below zero.'; END IF;
          IF EXISTS (
            WITH leg AS (SELECT * FROM jsonb_to_recordset(p_legs) AS x("itemId" uuid,"inventorySerialId" uuid,"quantityIn" numeric,"quantityOut" numeric)),
            d AS (SELECT "itemId","inventorySerialId",sum("quantityIn"-"quantityOut") n FROM leg WHERE "inventorySerialId" IS NOT NULL GROUP BY 1,2)
            SELECT 1 FROM d WHERE coalesce((SELECT sum(m."QuantityIn"-m."QuantityOut") FROM advance.stock_movements m WHERE m."CompanyId"=p_company_id AND m."ItemId"=d."itemId" AND m."InventorySerialId"=d."inventorySerialId"),0)+d.n NOT BETWEEN 0 AND 1)
          THEN RAISE EXCEPTION 'Posting would make a serial balance negative or greater than one.'; END IF;
          b:=gen_random_uuid();
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId","ReversesPostingBatchId","ReferenceType","ReferenceNumber","PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES (b,p_company_id,p_posting_kind,
            CASE WHEN p_posting_kind='GRN_CUSTODY' THEN p_source_id END,
            CASE WHEN p_posting_kind='QC_DISPOSITION' THEN p_source_id END,
            CASE WHEN p_posting_kind='MATERIAL_ISSUE' THEN p_source_id END,
            CASE WHEN p_posting_kind IN ('DC_DISPATCH','DC_RETURN_CUSTODY') THEN p_source_id END,
            CASE WHEN p_posting_kind='REVERSAL' THEN p_source_id END,
            ref_type,ref_number,p_posting_date,clock_timestamp(),p_posted_by_employee_id,trim(p_idempotency_key),p_request_fingerprint,trim(p_correlation_id),clock_timestamp(),trim(p_created_by),0);
          INSERT INTO advance.stock_movements
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType","ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion",
             "WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal","MovementLeg",
             "GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId","OriginGoodsReceiptLineId",
             "OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId","CustodyCaseLineId","InventoryLotId","InventorySerialId",
             "GoodsReceiptLineLotAllocationId","QcInspectionLotDispositionId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId",
             "InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId",
             "ReversesStockMovementId","PostingIdentity","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),p_company_id,x."itemId",l."WarehouseId",l."RackBinId",p_posting_kind,ref_type,ref_number,x."quantityIn",x."quantityOut",p_posting_date,2,
             l."Id",l."ConditionCode",b,x."batchLineOrdinal",x."movementLeg",
             x."goodsReceiptLineId",x."qcInspectionRevisionId",x."materialIssueRequestLineId",x."deliveryChallanLineId",x."originGoodsReceiptLineId",
             x."ownershipAccountId",x."custodyAssignmentId",x."inventoryProvenanceLayerId",x."custodyCaseLineId",x."inventoryLotId",x."inventorySerialId",
             x."goodsReceiptLineLotAllocationId",x."qcInspectionLotDispositionId",x."inventoryCustodyHandoffLineId",x."inventoryOwnershipTransferLineId",
             x."inventoryTransformationInputId",x."inventoryTransformationOutputId",x."inventoryConcessionAllocationId",
             x."reversesStockMovementId",x."postingIdentity",clock_timestamp(),trim(p_created_by),0
          FROM jsonb_to_recordset(p_legs) AS x(
            "batchLineOrdinal" integer,"itemId" uuid,"warehouseConditionLocationId" uuid,"movementLeg" text,
            "quantityIn" numeric,"quantityOut" numeric,"goodsReceiptLineId" uuid,"qcInspectionRevisionId" uuid,
            "materialIssueRequestLineId" uuid,"deliveryChallanLineId" uuid,"originGoodsReceiptLineId" uuid,
            "ownershipAccountId" uuid,"custodyAssignmentId" uuid,"inventoryProvenanceLayerId" uuid,"custodyCaseLineId" uuid,
            "inventoryLotId" uuid,"inventorySerialId" uuid,"goodsReceiptLineLotAllocationId" uuid,"qcInspectionLotDispositionId" uuid,
            "inventoryCustodyHandoffLineId" uuid,"inventoryOwnershipTransferLineId" uuid,"inventoryTransformationInputId" uuid,
            "inventoryTransformationOutputId" uuid,"inventoryConcessionAllocationId" uuid,
            "reversesStockMovementId" uuid,"postingIdentity" text)
          JOIN advance.warehouse_condition_locations l ON l."Id"=x."warehouseConditionLocationId" AND l."CompanyId"=p_company_id
          ORDER BY x."batchLineOrdinal";
          RETURN QUERY SELECT b,false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) FROM PUBLIC;
        """;

    internal const string DownContract = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Foundation 3 rollback requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Foundation 3 rollback refuses a PostgreSQL administrative database.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.stock_movements)
             OR EXISTS (SELECT 1 FROM advance.stock_posting_batches)
             OR EXISTS (SELECT 1 FROM advance.inventory_lot_attribute_revisions)
             OR EXISTS (SELECT 1 FROM advance.inventory_provenance_layers)
             OR EXISTS (SELECT 1 FROM advance.inventory_transformations)
             OR EXISTS (SELECT 1 FROM advance.inventory_provenance_edges)
             OR EXISTS (SELECT 1 FROM advance.inventory_serial_identity_revisions)
             OR EXISTS (SELECT 1 FROM advance.inventory_serial_genealogy_events)
             OR EXISTS (SELECT 1 FROM advance.qc_inspection_lot_dispositions)
             OR EXISTS (SELECT 1 FROM advance.inventory_concessions)
             OR EXISTS (SELECT 1 FROM advance.inventory_provenance_annotations) THEN
            RAISE EXCEPTION 'Foundation 3 rollback refuses persisted ledger, provenance, genealogy, QC or concession evidence.';
          END IF;
        END $guard$;
        DROP TRIGGER "TR_stock_movement_foundation3_identity" ON advance.stock_movements;
        DROP FUNCTION advance.stores_foundation3_movement_identity_guard();
        DROP FUNCTION advance.foundation3_prepare_grn_legs(uuid,uuid,text,jsonb);
        DROP FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb);
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source"
          CHECK (("PostingKind"<>'REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId")=1
              AND "ReversesPostingBatchId" IS NULL)
            OR ("PostingKind"='REVERSAL'
              AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","DeliveryChallanId")=0
              AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_schema_version";
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_schema_version"
          CHECK ("LedgerSchemaVersion" IN (1,2));
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract"
          CHECK ("LedgerSchemaVersion"=1 OR (
            "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL
            AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0
            AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL')
            AND "PostingIdentity" IS NOT NULL
            AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId")=1));
        """;
}
