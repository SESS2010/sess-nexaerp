namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class StoresControlledPostingSql
{
    internal const string Up = """
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores controlled posting requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores controlled posting refuses a PostgreSQL administrative database.'; END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; Stores controlled posting requires all four managed roles or none.';
          END IF;
        END $guard$;
        CREATE FUNCTION advance.post_stores_stock_batch(p_company_id uuid,p_posting_kind text,p_source_id uuid,p_idempotency_key text,p_request_fingerprint text,p_correlation_id text,p_posting_date date,p_posted_by_employee_id uuid,p_created_by text,p_legs jsonb)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE b uuid; old_hash text; ref_type text; ref_number text; source_company uuid; lock_key text;
        BEGIN
          IF p_company_id IS NULL OR p_source_id IS NULL OR p_posted_by_employee_id IS NULL THEN RAISE EXCEPTION 'Company, source and posting employee are required.'; END IF;
          IF p_posting_kind NOT IN ('GRN_CUSTODY','QC_DISPOSITION','MATERIAL_ISSUE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL') THEN RAISE EXCEPTION 'Unsupported Stores posting kind %.',p_posting_kind; END IF;
          IF length(trim(coalesce(p_idempotency_key,'')))=0 OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$' OR length(trim(coalesce(p_correlation_id,'')))=0 OR length(trim(coalesce(p_created_by,'')))=0 THEN RAISE EXCEPTION 'Posting idempotency, fingerprint, correlation and actor are required.'; END IF;
          IF jsonb_typeof(p_legs)<>'array' OR jsonb_array_length(p_legs)=0 THEN RAISE EXCEPTION 'A complete non-empty posting leg array is required.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STORES:IDEMP:'||p_company_id||':'||trim(p_idempotency_key),0));
          SELECT "Id","RequestFingerprint" INTO b,old_hash FROM advance.stock_posting_batches WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=trim(p_idempotency_key);
          IF FOUND THEN
            IF old_hash=p_request_fingerprint THEN RETURN QUERY SELECT b,true; RETURN; END IF;
            RAISE EXCEPTION 'Posting idempotency key was reused with a different fingerprint.';
          END IF;
          -- Every path takes one sorted source, Item, location and serial lock order.
          FOR lock_key IN
            WITH leg AS (SELECT * FROM jsonb_to_recordset(p_legs) AS x("itemId" uuid,"warehouseConditionLocationId" uuid,"inventorySerialId" uuid)), keys AS (
              SELECT '00:SRC:'||p_company_id||':'||p_posting_kind||':'||p_source_id k
              UNION SELECT '10:ITEM:'||p_company_id||':'||"itemId" FROM leg
              UNION SELECT '20:LOC:'||p_company_id||':'||"itemId"||':'||"warehouseConditionLocationId" FROM leg
              UNION SELECT '30:SER:'||p_company_id||':'||"itemId"||':'||"inventorySerialId" FROM leg WHERE "inventorySerialId" IS NOT NULL)
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
            WITH leg AS (SELECT * FROM jsonb_to_recordset(p_legs) AS x("itemId" uuid,"warehouseConditionLocationId" uuid,"quantityIn" numeric,"quantityOut" numeric)),
            d AS (SELECT "itemId","warehouseConditionLocationId",sum("quantityIn"-"quantityOut") n FROM leg GROUP BY 1,2)
            SELECT 1 FROM d JOIN advance.warehouse_condition_locations l ON l."Id"=d."warehouseConditionLocationId" AND l."CompanyId"=p_company_id
            WHERE l."ConditionCode"='AVAILABLE' AND coalesce((SELECT sum(m."QuantityIn"-m."QuantityOut") FROM advance.stock_movements m WHERE m."CompanyId"=p_company_id AND m."ItemId"=d."itemId" AND m."WarehouseConditionLocationId"=d."warehouseConditionLocationId"),0)+d.n<0)
          THEN RAISE EXCEPTION 'Posting would drive an AVAILABLE location below zero.'; END IF;
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
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType","ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion","WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal","MovementLeg","GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId","OriginGoodsReceiptLineId","InventorySerialId","ReversesStockMovementId","PostingIdentity","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),p_company_id,x."itemId",l."WarehouseId",l."RackBinId",p_posting_kind,ref_type,ref_number,x."quantityIn",x."quantityOut",p_posting_date,2,l."Id",l."ConditionCode",b,x."batchLineOrdinal",x."movementLeg",x."goodsReceiptLineId",x."qcInspectionRevisionId",x."materialIssueRequestLineId",x."deliveryChallanLineId",x."originGoodsReceiptLineId",x."inventorySerialId",x."reversesStockMovementId",x."postingIdentity",clock_timestamp(),trim(p_created_by),0
          FROM jsonb_to_recordset(p_legs) AS x(
            "batchLineOrdinal" integer,"itemId" uuid,"warehouseConditionLocationId" uuid,"movementLeg" text,
            "quantityIn" numeric,"quantityOut" numeric,"goodsReceiptLineId" uuid,"qcInspectionRevisionId" uuid,
            "materialIssueRequestLineId" uuid,"deliveryChallanLineId" uuid,"originGoodsReceiptLineId" uuid,
            "inventorySerialId" uuid,"reversesStockMovementId" uuid,"postingIdentity" text)
          JOIN advance.warehouse_condition_locations l ON l."Id"=x."warehouseConditionLocationId" AND l."CompanyId"=p_company_id
          ORDER BY x."batchLineOrdinal";
          RETURN QUERY SELECT b,false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) FROM PUBLIC;
        CREATE FUNCTION advance.replace_gate_entry_draft(p_company_id uuid,p_gate_entry_id uuid,p_expected_version bigint,p_vendor_dc_number text,p_vehicle_number text,p_mode_of_transport text,p_arrived_at timestamptz,p_iso_receipt_verification jsonb,p_updated_by text,p_lines jsonb)
        RETURNS bigint
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE current_version bigint; current_status text; purchase_order_id uuid;
        BEGIN
          IF p_company_id IS NULL OR p_gate_entry_id IS NULL OR p_expected_version<0 OR length(trim(coalesce(p_vendor_dc_number,'')))=0 OR length(trim(coalesce(p_mode_of_transport,'')))=0 OR p_arrived_at IS NULL OR length(trim(coalesce(p_updated_by,'')))=0 THEN
            RAISE EXCEPTION 'Gate Entry company, document, Version, DC number, transport mode, arrival and actor are required.';
          END IF;
          IF jsonb_typeof(p_iso_receipt_verification)<>'object' THEN RAISE EXCEPTION 'Gate Entry ISO receipt verification must be a JSON object.'; END IF;
          IF jsonb_typeof(p_lines)<>'array' OR jsonb_array_length(p_lines)=0 THEN RAISE EXCEPTION 'Gate Entry requires a complete non-empty line array.'; END IF;
          SELECT "Version","Status","PurchaseOrderId" INTO current_version,current_status,purchase_order_id
          FROM advance.gate_entries WHERE "Id"=p_gate_entry_id AND "CompanyId"=p_company_id FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Gate Entry does not exist in the selected company.'; END IF;
          IF current_status<>'DRAFT' THEN RAISE EXCEPTION 'A finalised Gate Entry is immutable.'; END IF;
          IF current_version<>p_expected_version THEN RAISE EXCEPTION 'Gate Entry Version is stale.'; END IF;
          IF EXISTS (SELECT 1 FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"purchaseOrderLineId" uuid,"deliveredQuantity" numeric) WHERE x."lineNumber"<=0 OR x."purchaseOrderLineId" IS NULL OR x."deliveredQuantity"<=0)
             OR (SELECT count(*) FROM jsonb_to_recordset(p_lines) AS x("purchaseOrderLineId" uuid))<>(SELECT count(DISTINCT x."purchaseOrderLineId") FROM jsonb_to_recordset(p_lines) AS x("purchaseOrderLineId" uuid))
             OR (SELECT count(*) FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))<>(SELECT count(DISTINCT x."lineNumber") FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))
             OR (SELECT count(*) FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))<>(SELECT max(x."lineNumber") FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer))
             OR EXISTS (SELECT 1 FROM jsonb_to_recordset(p_lines) AS x("purchaseOrderLineId" uuid) WHERE NOT EXISTS (SELECT 1 FROM advance.purchase_order_lines pol WHERE pol."Id"=x."purchaseOrderLineId" AND pol."PurchaseOrderId"=purchase_order_id AND pol."CompanyId"=p_company_id)) THEN
            RAISE EXCEPTION 'Every Gate Entry line must be positive, unique and belong to the selected Purchase Order.';
          END IF;
          UPDATE advance.gate_entries SET
            "VendorDcNumber"=trim(p_vendor_dc_number),"VehicleNumber"=nullif(trim(coalesce(p_vehicle_number,'')),''),"ModeOfTransport"=trim(p_mode_of_transport),"ArrivedAt"=p_arrived_at,
            "IsoReceiptVerificationJson"=p_iso_receipt_verification,"Version"=p_expected_version+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=trim(p_updated_by)
          WHERE "Id"=p_gate_entry_id AND "CompanyId"=p_company_id AND "Status"='DRAFT' AND "Version"=p_expected_version;
          IF NOT FOUND THEN RAISE EXCEPTION 'Gate Entry Version is stale.'; END IF;
          DELETE FROM advance.gate_entry_lines WHERE "GateEntryId"=p_gate_entry_id AND "CompanyId"=p_company_id;
          INSERT INTO advance.gate_entry_lines
            ("Id","CompanyId","GateEntryId","PurchaseOrderId","PurchaseOrderLineId","LineNumber","ItemId","ItemCodeSnapshot","UomSnapshot","DeliveredQuantity","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),p_company_id,p_gate_entry_id,purchase_order_id,x."purchaseOrderLineId",x."lineNumber",pol."ItemId",pol."ItemCodeSnapshot",pol."UomSnapshot",x."deliveredQuantity",clock_timestamp(),trim(p_updated_by)
          FROM jsonb_to_recordset(p_lines) AS x("lineNumber" integer,"purchaseOrderLineId" uuid,"deliveredQuantity" numeric)
          JOIN advance.purchase_order_lines pol ON pol."Id"=x."purchaseOrderLineId" AND pol."PurchaseOrderId"=purchase_order_id AND pol."CompanyId"=p_company_id
          ORDER BY x."lineNumber";
          RETURN p_expected_version+1;
        END $function$;
        REVOKE ALL ON FUNCTION advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE INSERT,UPDATE,DELETE ON advance.stock_posting_batches,advance.stock_movements FROM nexa_erp_runtime;
            GRANT SELECT ON advance.stock_posting_batches,advance.stock_movements TO nexa_erp_runtime;
            REVOKE EXECUTE ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb),advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;
    internal const string Down = """
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores controlled posting down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores controlled posting down refuses a PostgreSQL administrative database.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.stock_posting_batches) THEN RAISE EXCEPTION 'Stores controlled posting rollback refuses existing posting batches.'; END IF;
        END $guard$;
        DROP FUNCTION advance.replace_gate_entry_draft(uuid,uuid,bigint,text,text,text,timestamptz,jsonb,text,jsonb);
        DROP FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb);
        """;
}
