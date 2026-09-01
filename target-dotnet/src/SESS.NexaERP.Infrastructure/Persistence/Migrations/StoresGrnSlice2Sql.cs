namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class StoresGrnSlice2Sql
{
    internal static string GuardUp => Guard;
    internal static string Up => BuildPostingFunction(true) + BuildReconcileFunction(true) + BuildDocumentPostingGuard(true) + BuildGoodsReceiptGuard(true) + Install + ReverseInstall;
    internal static string Down => DownGuard + BuildPostingFunction(false) + BuildReconcileFunction(false) + BuildDocumentPostingGuard(false) + BuildGoodsReceiptGuard(false);

    private static string BuildPostingFunction(bool withLot)
    {
        var source=StoresControlledPostingSql.Up;
        var signature="REVOKE ALL ON FUNCTION advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb) FROM PUBLIC;";
        var start=source.IndexOf("CREATE FUNCTION advance.post_stores_stock_batch",StringComparison.Ordinal);
        var end=source.IndexOf(signature,StringComparison.Ordinal)+signature.Length;
        if(start<0||end<signature.Length)throw new InvalidOperationException("Stores posting function template was not found.");
        var function=source[start..end].Replace("CREATE FUNCTION advance.post_stores_stock_batch","CREATE OR REPLACE FUNCTION advance.post_stores_stock_batch",StringComparison.Ordinal);
        if(!withLot)return function;
        function=ReplaceOnce(function,"\"goodsReceiptLineId\" uuid,\"qcInspectionRevisionId\" uuid","\"goodsReceiptLineId\" uuid,\"goodsReceiptLineLotAllocationId\" uuid,\"qcInspectionRevisionId\" uuid");
        function=ReplaceOnce(function,"\"GoodsReceiptLineId\",\"QcInspectionRevisionId\"","\"GoodsReceiptLineId\",\"GoodsReceiptLineLotAllocationId\",\"QcInspectionRevisionId\"");
        function=ReplaceOnce(function,"x.\"goodsReceiptLineId\",x.\"qcInspectionRevisionId\"","x.\"goodsReceiptLineId\",x.\"goodsReceiptLineLotAllocationId\",x.\"qcInspectionRevisionId\"");
        return function;
    }

    private static string BuildReconcileFunction(bool fixedBranch)
    {
        var source=FirstStoresPart3BSql.Up;
        var start=source.IndexOf("CREATE OR REPLACE FUNCTION advance.stores_p3b_reconcile_batch()",StringComparison.Ordinal);
        var end=source.IndexOf("CREATE CONSTRAINT TRIGGER \"TR_stock_posting_batch_reconcile\"",start,StringComparison.Ordinal);
        if(start<0||end<0)throw new InvalidOperationException("Stores reconcile function template was not found.");
        var function=source[start..end].TrimEnd();
        if(fixedBranch)function=ReplaceOnce(function,
            "batch_id:=CASE WHEN TG_TABLE_NAME='stock_posting_batches' THEN NEW.\"Id\" ELSE NEW.\"StockPostingBatchId\" END;",
            "IF TG_TABLE_NAME='stock_posting_batches' THEN batch_id:=NEW.\"Id\"; ELSE batch_id:=NEW.\"StockPostingBatchId\"; END IF;");
        return function;
    }
    private static string BuildDocumentPostingGuard(bool withGrnReversal)
    {
        var source=FirstStoresPart3BSql.Up;
        var start=source.IndexOf("CREATE OR REPLACE FUNCTION advance.stores_p3b_document_posting_guard()",StringComparison.Ordinal);
        var end=source.IndexOf("CREATE CONSTRAINT TRIGGER \"TR_goods_receipt_atomic_posting\"",start,StringComparison.Ordinal);
        if(start<0||end<0)throw new InvalidOperationException("Stores document posting guard template was not found.");
        var function=source[start..end].TrimEnd();
        if(withGrnReversal)
        {
            function=ReplaceOnce(function,"IF TG_TABLE_NAME='goods_receipts' AND NEW.\"Status\"='FINALIZED' AND","IF TG_TABLE_NAME='goods_receipts' AND NEW.\"Status\"='FINALIZED' AND NEW.\"DocumentKind\"='NORMAL' AND");
            function=ReplaceOnce(function,"THEN RAISE EXCEPTION 'Finalised GRN requires exactly one atomic custody posting batch.';","THEN RAISE EXCEPTION 'Finalised GRN requires exactly one atomic custody posting batch.';\n          ELSIF TG_TABLE_NAME='goods_receipts' AND NEW.\"Status\"='FINALIZED' AND NEW.\"DocumentKind\"='REVERSAL' AND (SELECT count(*) FROM advance.stock_posting_batches reversal JOIN advance.stock_posting_batches original ON original.\"Id\"=reversal.\"ReversesPostingBatchId\" WHERE reversal.\"PostingKind\"='REVERSAL' AND original.\"PostingKind\"='GRN_CUSTODY' AND original.\"GoodsReceiptId\"=NEW.\"ReversesGoodsReceiptId\")<>1 THEN RAISE EXCEPTION 'Finalised GRN reversal requires exactly one atomic reversal posting batch.';");
        }
        return function;
    }

    private static string BuildGoodsReceiptGuard(bool distinguishReversal)
    {
        var source=FirstStoresPart2Sql.Up;
        var start=source.IndexOf("CREATE OR REPLACE FUNCTION advance.stores_p2_goods_receipt_guard()",StringComparison.Ordinal);
        var end=source.IndexOf("CREATE TRIGGER \"TR_goods_receipt_guard\"",start,StringComparison.Ordinal);
        if(start<0||end<0)throw new InvalidOperationException("Stores GRN guard template was not found.");
        var function=source[start..end].TrimEnd();
        if(distinguishReversal)function=ReplaceOnce(function,"IF NEW.\"Status\"='FINALIZED' THEN","IF NEW.\"Status\"='FINALIZED' AND NEW.\"DocumentKind\"='NORMAL' THEN");
        return function;
    }
    private static string ReplaceOnce(string value,string oldValue,string newValue)
    {
        var first=value.IndexOf(oldValue,StringComparison.Ordinal);
        if(first<0||value.IndexOf(oldValue,first+oldValue.Length,StringComparison.Ordinal)>=0)throw new InvalidOperationException($"Expected one Stores SQL template fragment: {oldValue}");
        return value[..first]+newValue+value[(first+oldValue.Length)..];
    }

    private const string Guard="""
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores Slice 2 GRN requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores Slice 2 GRN refuses a PostgreSQL administrative database.'; END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN RAISE EXCEPTION 'Partial NexaERP principal state; Stores Slice 2 GRN requires all four managed roles or none.'; END IF;
          IF to_regprocedure('advance.post_stores_stock_batch(uuid,text,uuid,text,text,text,date,uuid,text,jsonb)') IS NULL OR to_regclass('advance.goods_receipts') IS NULL OR to_regclass('advance.stock_movements') IS NULL THEN RAISE EXCEPTION 'Stores Slice 2 GRN requires the witnessed Stores ledger foundation.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipts) OR EXISTS (SELECT 1 FROM advance.goods_receipt_lines) OR EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials) OR EXISTS (SELECT 1 FROM advance.stock_posting_batches) THEN RAISE EXCEPTION 'Stores Slice 2 GRN requires an empty pre-GRN document and posting set.'; END IF;
        END $guard$;
        """;

    private const string Install="""
        INSERT INTO advance.employee_role_assignments
          ("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('STORES_SLICE2_GRN_OPERATOR|'||c."Id"||'|SESS-41')::uuid,c."Id",e."Id",r."Id",DATE '2026-09-01','SeedApproved',
               'Settled Gate Entry and GRN receipt operator',TIMESTAMPTZ '2026-09-01 00:00:00+00','STORES_SLICE2_GRN_OPERATOR',0
        FROM advance.companies c CROSS JOIN advance.employees e CROSS JOIN advance.roles r
        WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND e."EmployeeCode"='SESS-41' AND r."Code"='STORES_ASSISTANT'
          AND NOT EXISTS (SELECT 1 FROM advance.employee_role_assignments a WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=e."Id" AND a."RoleId"=r."Id" AND a."EffectiveFrom"<=DATE '2026-09-01' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-01'));
        DO $operators$ BEGIN
          IF (SELECT count(*) FROM advance.companies c CROSS JOIN (VALUES ('SESS-16'),('SESS-35'),('SESS-41')) expected(employee_code)
              WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND EXISTS (
                SELECT 1 FROM advance.employee_role_assignments a JOIN advance.employees e ON e."Id"=a."EmployeeId" JOIN advance.roles r ON r."Id"=a."RoleId"
                WHERE a."CompanyId"=c."Id" AND e."EmployeeCode"=expected.employee_code AND r."Code" IN ('STORES_EXECUTIVE','STORES_ASSISTANT')
                  AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND a."EffectiveFrom"<=DATE '2026-09-01' AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=DATE '2026-09-01')))<>6
          THEN RAISE EXCEPTION 'Stores Slice 2 requires effective receipt roles for SUDALAI (SESS-35), KAMALI (SESS-16) and KARTHICK (SESS-41) in both companies.'; END IF;
        END $operators$;
        ALTER TABLE advance.goods_receipt_line_serials ALTER COLUMN "GoodsReceiptLineLotAllocationId" DROP DEFAULT;
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_slice2_grn_lot"
          CHECK ("LedgerSchemaVersion"=1 OR "MovementType"<>'GRN_CUSTODY' OR "GoodsReceiptLineLotAllocationId" IS NOT NULL);

        CREATE OR REPLACE FUNCTION advance.stores_slice2_inventory_lot_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        BEGIN
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Inventory lot identity is immutable; use a typed correction revision.'; END IF;
          RETURN NEW;
        END $f$;
        CREATE TRIGGER "TR_inventory_lot_immutable" BEFORE UPDATE OR DELETE ON advance.inventory_lots FOR EACH ROW EXECUTE FUNCTION advance.stores_slice2_inventory_lot_guard();

        CREATE OR REPLACE FUNCTION advance.stores_slice2_lot_allocation_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        DECLARE parent_status text; line_item uuid; line_company uuid; receipt_vendor uuid; lot_row advance.inventory_lots%ROWTYPE;
        BEGIN
          SELECT h."Status",l."ItemId",l."CompanyId",h."VendorId" INTO parent_status,line_item,line_company,receipt_vendor
          FROM advance.goods_receipt_lines l JOIN advance.goods_receipts h ON h."Id"=l."GoodsReceiptId"
          WHERE l."Id"=coalesce(NEW."GoodsReceiptLineId",OLD."GoodsReceiptLineId") FOR SHARE OF h;
          IF parent_status IS DISTINCT FROM 'DRAFT' THEN RAISE EXCEPTION 'GRN lot allocations are mutable only while the GRN is DRAFT.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT * INTO lot_row FROM advance.inventory_lots WHERE "Id"=NEW."InventoryLotId";
          IF NOT FOUND OR (NEW."CompanyId",line_company,line_item,receipt_vendor) IS DISTINCT FROM (lot_row."CompanyId",lot_row."CompanyId",lot_row."ItemId",lot_row."VendorId") THEN RAISE EXCEPTION 'GRN lot allocation company, Item and vendor must match its receipt line.'; END IF;
          RETURN NEW;
        END $f$;
        CREATE TRIGGER "TR_grn_line_lot_allocation_guard" BEFORE INSERT OR UPDATE OR DELETE ON advance.goods_receipt_line_lot_allocations FOR EACH ROW EXECUTE FUNCTION advance.stores_slice2_lot_allocation_guard();

        CREATE OR REPLACE FUNCTION advance.stores_p2_line_serial_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        DECLARE line_row advance.goods_receipt_lines%ROWTYPE; header_status text; serial_row advance.inventory_serials%ROWTYPE; allocation_row advance.goods_receipt_line_lot_allocations%ROWTYPE;
        BEGIN
          SELECT * INTO line_row FROM advance.goods_receipt_lines WHERE "Id"=coalesce(NEW."GoodsReceiptLineId",OLD."GoodsReceiptLineId");
          SELECT "Status" INTO header_status FROM advance.goods_receipts WHERE "Id"=line_row."GoodsReceiptId" FOR SHARE;
          IF header_status IS DISTINCT FROM 'DRAFT' THEN RAISE EXCEPTION 'GRN serial captures are mutable only while the GRN is DRAFT.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT * INTO allocation_row FROM advance.goods_receipt_line_lot_allocations WHERE "Id"=NEW."GoodsReceiptLineLotAllocationId";
          IF NOT FOUND OR (allocation_row."CompanyId",allocation_row."GoodsReceiptLineId") IS DISTINCT FROM (NEW."CompanyId",NEW."GoodsReceiptLineId") THEN RAISE EXCEPTION 'GRN serial must reference a lot allocation on the same receipt line.'; END IF;
          IF NEW."InventorySerialId" IS NOT NULL THEN
            SELECT * INTO serial_row FROM advance.inventory_serials WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."InventorySerialId";
            IF NOT FOUND OR line_row."ItemId"<>NEW."ItemId" OR serial_row."ItemId"<>NEW."ItemId" OR serial_row."StoredSerialNumber"<>NEW."StoredSerialNumberSnapshot" THEN RAISE EXCEPTION 'GRN serial company, Item and durable identity must match.'; END IF;
          END IF;
          IF NEW."EnteredSerialNumber"<>NEW."StoredSerialNumberSnapshot" AND NOT (NEW."DisambiguationApplied" AND NEW."DuplicateWarningAcknowledged") THEN RAISE EXCEPTION 'A changed stored serial requires acknowledged disambiguation.'; END IF;
          RETURN NEW;
        END $f$;

        CREATE OR REPLACE FUNCTION advance.stores_slice2_grn_movement_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        BEGIN
          IF NEW."LedgerSchemaVersion"=2 AND NEW."MovementType"='REVERSAL' AND NEW."GoodsReceiptLineId" IS NOT NULL AND NEW."GoodsReceiptLineLotAllocationId" IS DISTINCT FROM (SELECT original."GoodsReceiptLineLotAllocationId" FROM advance.stock_movements original WHERE original."Id"=NEW."ReversesStockMovementId") THEN RAISE EXCEPTION 'GRN reversal must preserve the exact lot-allocation provenance of its target movement.'; END IF;
          IF NEW."LedgerSchemaVersion"=2 AND NEW."MovementType"='GRN_CUSTODY' AND NOT EXISTS (
            SELECT 1 FROM advance.goods_receipt_line_lot_allocations a JOIN advance.goods_receipt_lines l ON l."Id"=a."GoodsReceiptLineId"
            WHERE a."Id"=NEW."GoodsReceiptLineLotAllocationId" AND a."CompanyId"=NEW."CompanyId" AND l."Id"=NEW."GoodsReceiptLineId" AND l."ItemId"=NEW."ItemId"
              AND (NEW."InventorySerialId" IS NULL OR EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineLotAllocationId"=a."Id" AND s."InventorySerialId"=NEW."InventorySerialId")))
          THEN RAISE EXCEPTION 'GRN custody movement must preserve exact line, lot allocation, Item and serial provenance.'; END IF;
          RETURN NEW;
        END $f$;
        CREATE TRIGGER "TR_stock_movement_grn_lot_guard" BEFORE INSERT ON advance.stock_movements FOR EACH ROW EXECUTE FUNCTION advance.stores_slice2_grn_movement_guard();

        CREATE OR REPLACE FUNCTION advance.finalize_goods_receipt(p_company_id uuid,p_goods_receipt_id uuid,p_expected_version bigint,p_idempotency_key text,p_request_fingerprint text,p_correlation_id text,p_actor_employee_id uuid,p_actor_role_code text,p_actor_login text)
        RETURNS TABLE("StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE receipt advance.goods_receipts%ROWTYPE; existing_batch uuid; existing_hash text; finalized_at timestamptz; legs jsonb; serial_capture record; durable_serial uuid; normalized_serial text; posted record;
        BEGIN
          IF p_company_id IS NULL OR p_goods_receipt_id IS NULL OR p_expected_version<0 OR p_actor_employee_id IS NULL OR length(trim(coalesce(p_actor_login,'')))=0 THEN RAISE EXCEPTION 'GRN company, document, Version and actor are required.'; END IF;
          IF p_actor_role_code NOT IN ('STORES_EXECUTIVE','STORES_ASSISTANT') THEN RAISE EXCEPTION 'GRN finalization requires a Stores receipt operational role.'; END IF;
          IF NOT EXISTS (
            SELECT 1 FROM advance.employees e JOIN advance.employee_role_assignments a ON a."EmployeeId"=e."Id" AND a."CompanyId"=p_company_id
            JOIN advance.roles r ON r."Id"=a."RoleId"
            WHERE e."Id"=p_actor_employee_id AND e."EmployeeCode" IN ('SESS-16','SESS-35','SESS-41') AND r."Code"=p_actor_role_code
              AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date))
          THEN RAISE EXCEPTION 'GRN finalization requires SUDALAI, KAMALI or KARTHICK with the effective named receipt role.'; END IF;
          IF length(trim(coalesce(p_idempotency_key,'')))=0 OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$' OR length(trim(coalesce(p_correlation_id,'')))=0 THEN RAISE EXCEPTION 'GRN finalization idempotency, fingerprint and correlation are required.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STORES:GRN:FINALIZE:'||p_company_id||':'||p_goods_receipt_id,0));
          SELECT "Id","RequestFingerprint" INTO existing_batch,existing_hash FROM advance.stock_posting_batches WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=trim(p_idempotency_key);
          IF FOUND THEN
            IF existing_hash=p_request_fingerprint AND EXISTS (SELECT 1 FROM advance.stock_posting_batches WHERE "Id"=existing_batch AND "GoodsReceiptId"=p_goods_receipt_id AND "PostingKind"='GRN_CUSTODY') THEN RETURN QUERY SELECT existing_batch,true; RETURN; END IF;
            RAISE EXCEPTION 'GRN finalization idempotency key was reused with different data.';
          END IF;
          SELECT * INTO receipt FROM advance.goods_receipts WHERE "Id"=p_goods_receipt_id AND "CompanyId"=p_company_id FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'GRN does not exist in the selected company.'; END IF;
          IF receipt."Status"<>'DRAFT' THEN RAISE EXCEPTION 'A finalized GRN is immutable; correct it by reversal and a new document.'; END IF;
          IF receipt."Version"<>p_expected_version THEN RAISE EXCEPTION 'GRN Version is stale.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipt_lines l WHERE l."GoodsReceiptId"=receipt."Id" AND (l."ExcessRejectedQuantity"<>0 OR l."ReceivedQuantity">l."RemainingPoQuantitySnapshot")) THEN RAISE EXCEPTION 'Over-receipt is refused.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipt_lines l WHERE l."GoodsReceiptId"=receipt."Id" AND (SELECT coalesce(sum(a."Quantity"),0) FROM advance.goods_receipt_line_lot_allocations a WHERE a."GoodsReceiptLineId"=l."Id")<>l."ReceivedQuantity") THEN RAISE EXCEPTION 'Every GRN line must be completely allocated to one or more lots.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipt_lines l WHERE l."GoodsReceiptId"=receipt."Id" AND ((l."SerialCaptureModeSnapshot"='REQUIRED' AND (l."ReceivedQuantity"<>trunc(l."ReceivedQuantity") OR (SELECT count(*) FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id")<>l."ReceivedQuantity")) OR (l."SerialCaptureModeSnapshot"='OPTIONAL' AND EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id") AND (SELECT count(*) FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id")<>l."ReceivedQuantity"))) THEN RAISE EXCEPTION 'GRN serial capture is incomplete for the snapshotted threshold policy.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipt_line_lot_allocations a JOIN advance.goods_receipt_lines l ON l."Id"=a."GoodsReceiptLineId" WHERE l."GoodsReceiptId"=receipt."Id" AND EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id") AND (SELECT count(*) FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineLotAllocationId"=a."Id")<>a."Quantity") THEN RAISE EXCEPTION 'GRN serial capture must reconcile to each exact lot allocation.'; END IF;
          IF EXISTS (SELECT advance.stores_p2_normalize_serial(s."StoredSerialNumberSnapshot") FROM advance.goods_receipt_line_serials s JOIN advance.goods_receipt_lines l ON l."Id"=s."GoodsReceiptLineId" WHERE l."GoodsReceiptId"=receipt."Id" GROUP BY 1 HAVING count(*)>1)
             OR EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials s JOIN advance.goods_receipt_lines l ON l."Id"=s."GoodsReceiptLineId" JOIN advance.inventory_serials i ON i."CompanyId"=p_company_id AND i."NormalizedStoredSerialNumber"=advance.stores_p2_normalize_serial(s."StoredSerialNumberSnapshot") WHERE l."GoodsReceiptId"=receipt."Id") THEN
            RAISE EXCEPTION 'Duplicate serial warning is unresolved; make every stored serial unique before finalization.';
          END IF;
          FOR serial_capture IN SELECT s.* FROM advance.goods_receipt_line_serials s JOIN advance.goods_receipt_lines l ON l."Id"=s."GoodsReceiptLineId" WHERE l."GoodsReceiptId"=receipt."Id" ORDER BY l."LineNumber",s."SerialOrdinal"
          LOOP
            normalized_serial:=advance.stores_p2_normalize_serial(serial_capture."StoredSerialNumberSnapshot"); durable_serial:=gen_random_uuid();
            INSERT INTO advance.inventory_serials ("Id","CompanyId","ItemId","StoredSerialNumber","NormalizedStoredSerialNumber","FirstCapturedAt","FirstCapturedByEmployeeId","CreatedAt","CreatedBy") VALUES (durable_serial,p_company_id,serial_capture."ItemId",serial_capture."StoredSerialNumberSnapshot",normalized_serial,clock_timestamp(),p_actor_employee_id,clock_timestamp(),trim(p_actor_login));
            UPDATE advance.goods_receipt_line_serials SET "InventorySerialId"=durable_serial WHERE "Id"=serial_capture."Id";
          END LOOP;
          finalized_at:=clock_timestamp();
          UPDATE advance.goods_receipts SET "Status"='FINALIZED',"FinalizedAt"=finalized_at,"FinalizedByEmployeeId"=p_actor_employee_id,"QcDueAt"=finalized_at+make_interval(days=>"QcCompletionDaysSnapshot"),"Version"=p_expected_version+1,"UpdatedAt"=finalized_at,"UpdatedBy"=trim(p_actor_login) WHERE "Id"=receipt."Id" AND "Status"='DRAFT' AND "Version"=p_expected_version;
          IF NOT FOUND THEN RAISE EXCEPTION 'GRN Version is stale.'; END IF;
          INSERT INTO advance.stores_document_status_history ("Id","CompanyId","GoodsReceiptId","FromStatus","ToStatus","Action","ActorEmployeeId","ActorRoleCode","OccurredAt","CorrelationId") VALUES (gen_random_uuid(),p_company_id,receipt."Id",'DRAFT','FINALIZED','FINALIZED',p_actor_employee_id,p_actor_role_code,finalized_at,p_correlation_id);
          WITH raw_leg AS (
            SELECT l."LineNumber",a."LotOrdinal",s."SerialOrdinal",l."ItemId",l."Id" line_id,a."Id" allocation_id,l."QcHoldConditionLocationIdSnapshot" location_id,1::numeric quantity,s."InventorySerialId" serial_id
            FROM advance.goods_receipt_lines l JOIN advance.goods_receipt_line_lot_allocations a ON a."GoodsReceiptLineId"=l."Id" JOIN advance.goods_receipt_line_serials s ON s."GoodsReceiptLineLotAllocationId"=a."Id" WHERE l."GoodsReceiptId"=receipt."Id"
            UNION ALL
            SELECT l."LineNumber",a."LotOrdinal",0,l."ItemId",l."Id",a."Id",l."QcHoldConditionLocationIdSnapshot",a."Quantity",NULL::uuid
            FROM advance.goods_receipt_lines l JOIN advance.goods_receipt_line_lot_allocations a ON a."GoodsReceiptLineId"=l."Id" WHERE l."GoodsReceiptId"=receipt."Id" AND NOT EXISTS (SELECT 1 FROM advance.goods_receipt_line_serials s WHERE s."GoodsReceiptLineId"=l."Id")
          ), numbered AS (SELECT row_number() OVER(ORDER BY "LineNumber","LotOrdinal","SerialOrdinal") n,* FROM raw_leg)
          SELECT jsonb_agg(jsonb_build_object('batchLineOrdinal',n,'itemId',"ItemId",'warehouseConditionLocationId',location_id,'movementLeg','RECEIPT_IN','quantityIn',quantity,'quantityOut',0,'goodsReceiptLineId',line_id,'goodsReceiptLineLotAllocationId',allocation_id,'originGoodsReceiptLineId',line_id,'inventorySerialId',serial_id,'postingIdentity','GRN:'||receipt."Id"||':LOT:'||allocation_id||':'||coalesce(serial_id::text,'BULK')) ORDER BY n) INTO legs FROM numbered;
          SELECT * INTO posted FROM advance.post_stores_stock_batch(p_company_id,'GRN_CUSTODY',receipt."Id",trim(p_idempotency_key),p_request_fingerprint,p_correlation_id,receipt."ReceivedAt"::date,p_actor_employee_id,trim(p_actor_login),legs);
          RETURN QUERY SELECT posted."StockPostingBatchId",posted."Replayed";
        END $f$;
        REVOKE ALL ON FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE EXECUTE ON FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;
    private const string ReverseInstall="""
        CREATE OR REPLACE FUNCTION advance.reverse_goods_receipt(p_company_id uuid,p_goods_receipt_id uuid,p_expected_version bigint,p_reversal_number text,p_reason text,p_idempotency_key text,p_request_fingerprint text,p_correlation_id text,p_actor_employee_id uuid,p_actor_role_code text,p_actor_login text)
        RETURNS TABLE("ReversalGoodsReceiptId" uuid,"StockPostingBatchId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE original advance.goods_receipts%ROWTYPE; reversal advance.goods_receipts%ROWTYPE; original_batch uuid; existing_receipt uuid; existing_hash text; existing_batch uuid; legs jsonb; posted record; reversed_at timestamptz;
        BEGIN
          IF p_company_id IS NULL OR p_goods_receipt_id IS NULL OR p_expected_version<0 OR p_actor_employee_id IS NULL OR length(trim(coalesce(p_reversal_number,'')))=0 OR length(trim(coalesce(p_reason,'')))=0 OR length(trim(coalesce(p_actor_login,'')))=0 THEN RAISE EXCEPTION 'GRN reversal company, document, Version, number, reason and actor are required.'; END IF;
          IF p_actor_role_code NOT IN ('STORES_EXECUTIVE','STORES_ASSISTANT') THEN RAISE EXCEPTION 'GRN reversal requires a Stores receipt operational role.'; END IF;
          IF NOT EXISTS (
            SELECT 1 FROM advance.employees e JOIN advance.employee_role_assignments a ON a."EmployeeId"=e."Id" AND a."CompanyId"=p_company_id JOIN advance.roles r ON r."Id"=a."RoleId"
            WHERE e."Id"=p_actor_employee_id AND e."EmployeeCode" IN ('SESS-16','SESS-35','SESS-41') AND r."Code"=p_actor_role_code
              AND a."ApprovalStatus" IN ('SeedApproved','Approved') AND a."EffectiveFrom"<=current_date AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=current_date))
          THEN RAISE EXCEPTION 'GRN reversal requires SUDALAI, KAMALI or KARTHICK with the effective named receipt role.'; END IF;
          IF length(trim(coalesce(p_idempotency_key,'')))=0 OR p_request_fingerprint !~ '^[0-9a-fA-F]{64}$' OR length(trim(coalesce(p_correlation_id,'')))=0 THEN RAISE EXCEPTION 'GRN reversal idempotency, fingerprint and correlation are required.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('STORES:GRN:REVERSE:'||p_company_id||':'||p_goods_receipt_id,0));
          SELECT "Id","RequestFingerprint" INTO existing_receipt,existing_hash FROM advance.goods_receipts WHERE "CompanyId"=p_company_id AND "IdempotencyKey"=trim(p_idempotency_key);
          IF FOUND THEN
            IF existing_hash=p_request_fingerprint AND EXISTS (SELECT 1 FROM advance.goods_receipts WHERE "Id"=existing_receipt AND "DocumentKind"='REVERSAL' AND "ReversesGoodsReceiptId"=p_goods_receipt_id AND "Status"='FINALIZED') THEN
              SELECT reversal_batch."Id" INTO existing_batch FROM advance.stock_posting_batches reversal_batch JOIN advance.stock_posting_batches target ON target."Id"=reversal_batch."ReversesPostingBatchId" WHERE reversal_batch."PostingKind"='REVERSAL' AND target."GoodsReceiptId"=p_goods_receipt_id;
              RETURN QUERY SELECT existing_receipt,existing_batch,true; RETURN;
            END IF;
            RAISE EXCEPTION 'GRN reversal idempotency key was reused with different data.';
          END IF;
          SELECT * INTO original FROM advance.goods_receipts WHERE "Id"=p_goods_receipt_id AND "CompanyId"=p_company_id FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'GRN does not exist in the selected company.'; END IF;
          IF original."DocumentKind"<>'NORMAL' OR original."Status"<>'FINALIZED' THEN RAISE EXCEPTION 'Only a finalized normal GRN can be reversed.'; END IF;
          IF original."Version"<>p_expected_version THEN RAISE EXCEPTION 'GRN Version is stale.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipts WHERE "CompanyId"=p_company_id AND "DocumentKind"='REVERSAL' AND "ReversesGoodsReceiptId"=original."Id" AND "Status"='FINALIZED') THEN RAISE EXCEPTION 'GRN is already reversed.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.qc_inspections i JOIN advance.goods_receipt_lines l ON l."Id"=i."GoodsReceiptLineId" WHERE l."GoodsReceiptId"=original."Id") THEN RAISE EXCEPTION 'GRN custody cannot be reversed after QC evidence exists; reverse the downstream evidence first.'; END IF;
          SELECT "Id" INTO original_batch FROM advance.stock_posting_batches WHERE "CompanyId"=p_company_id AND "GoodsReceiptId"=original."Id" AND "PostingKind"='GRN_CUSTODY' FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Finalized GRN custody posting is missing.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.stock_posting_batches WHERE "ReversesPostingBatchId"=original_batch) THEN RAISE EXCEPTION 'GRN custody posting is already reversed.'; END IF;
          reversed_at:=clock_timestamp(); reversal:=original; reversal."Id":=gen_random_uuid(); reversal."GrnNumber":=trim(p_reversal_number); reversal."DocumentKind":='REVERSAL'; reversal."ReversesGoodsReceiptId":=original."Id"; reversal."ReversalReason":=trim(p_reason); reversal."Status":='FINALIZED'; reversal."FinalizedAt":=reversed_at; reversal."FinalizedByEmployeeId":=p_actor_employee_id; reversal."IdempotencyKey":=trim(p_idempotency_key); reversal."RequestFingerprint":=p_request_fingerprint; reversal."CreatedAt":=reversed_at; reversal."CreatedBy":=trim(p_actor_login); reversal."UpdatedAt":=NULL; reversal."UpdatedBy":=NULL; reversal."Version":=0;
          INSERT INTO advance.goods_receipts SELECT (reversal).*;
          INSERT INTO advance.stores_document_status_history ("Id","CompanyId","GoodsReceiptId","FromStatus","ToStatus","Action","ActorEmployeeId","ActorRoleCode","OccurredAt","CorrelationId") VALUES
            (gen_random_uuid(),p_company_id,reversal."Id",NULL,'DRAFT','CREATED',p_actor_employee_id,p_actor_role_code,reversed_at,p_correlation_id||':CREATED'),
            (gen_random_uuid(),p_company_id,reversal."Id",'DRAFT','FINALIZED','FINALIZED',p_actor_employee_id,p_actor_role_code,reversed_at,p_correlation_id);
          SELECT jsonb_agg(jsonb_build_object('batchLineOrdinal',m."BatchLineOrdinal",'itemId',m."ItemId",'warehouseConditionLocationId',m."WarehouseConditionLocationId",'movementLeg','REVERSAL','quantityIn',m."QuantityOut",'quantityOut',m."QuantityIn",'goodsReceiptLineId',m."GoodsReceiptLineId",'goodsReceiptLineLotAllocationId',m."GoodsReceiptLineLotAllocationId",'qcInspectionRevisionId',m."QcInspectionRevisionId",'materialIssueRequestLineId',m."MaterialIssueRequestLineId",'deliveryChallanLineId',m."DeliveryChallanLineId",'originGoodsReceiptLineId',m."OriginGoodsReceiptLineId",'inventorySerialId',m."InventorySerialId",'reversesStockMovementId',m."Id",'postingIdentity','REVERSAL:'||original_batch||':'||m."Id") ORDER BY m."BatchLineOrdinal") INTO legs
          FROM advance.stock_movements m WHERE m."StockPostingBatchId"=original_batch;
          SELECT * INTO posted FROM advance.post_stores_stock_batch(p_company_id,'REVERSAL',original_batch,trim(p_idempotency_key),p_request_fingerprint,p_correlation_id,current_date,p_actor_employee_id,trim(p_actor_login),legs);
          RETURN QUERY SELECT reversal."Id",posted."StockPostingBatchId",posted."Replayed";
        END $f$;
        REVOKE ALL ON FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE EXECUTE ON FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text) FROM nexa_erp_bootstrap,nexa_erp_migration;
            GRANT EXECUTE ON FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;
    private const string DownGuard="""
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Stores Slice 2 GRN down requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Stores Slice 2 GRN down refuses a PostgreSQL administrative database.'; END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipts) OR EXISTS (SELECT 1 FROM advance.goods_receipt_line_lot_allocations) OR EXISTS (SELECT 1 FROM advance.inventory_lots) OR EXISTS (SELECT 1 FROM advance.stock_posting_batches) OR EXISTS (SELECT 1 FROM advance.stock_movements WHERE "GoodsReceiptLineLotAllocationId" IS NOT NULL) THEN RAISE EXCEPTION 'Stores Slice 2 GRN rollback refuses receipt, lot or posting evidence.'; END IF;
        END $guard$;
        DELETE FROM advance.employee_role_assignments WHERE "CreatedBy"='STORES_SLICE2_GRN_OPERATOR';
        DROP FUNCTION advance.reverse_goods_receipt(uuid,uuid,bigint,text,text,text,text,text,uuid,text,text);
        DROP FUNCTION advance.finalize_goods_receipt(uuid,uuid,bigint,text,text,text,uuid,text,text);
        DROP TRIGGER "TR_stock_movement_grn_lot_guard" ON advance.stock_movements;
        DROP FUNCTION advance.stores_slice2_grn_movement_guard();
        DROP TRIGGER "TR_grn_line_lot_allocation_guard" ON advance.goods_receipt_line_lot_allocations;
        DROP FUNCTION advance.stores_slice2_lot_allocation_guard();
        DROP TRIGGER "TR_inventory_lot_immutable" ON advance.inventory_lots;
        DROP FUNCTION advance.stores_slice2_inventory_lot_guard();
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_slice2_grn_lot";

        CREATE OR REPLACE FUNCTION advance.stores_p2_line_serial_guard()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        DECLARE line_row advance.goods_receipt_lines%ROWTYPE; header_status text; serial_row advance.inventory_serials%ROWTYPE;
        BEGIN
          SELECT * INTO line_row FROM advance.goods_receipt_lines WHERE "Id"=coalesce(NEW."GoodsReceiptLineId",OLD."GoodsReceiptLineId");
          SELECT "Status" INTO header_status FROM advance.goods_receipts WHERE "Id"=line_row."GoodsReceiptId" FOR SHARE;
          IF header_status IS DISTINCT FROM 'DRAFT' THEN RAISE EXCEPTION 'GRN serial captures are mutable only while the GRN is DRAFT.'; END IF;
          IF TG_OP='DELETE' THEN RETURN OLD; END IF;
          SELECT * INTO serial_row FROM advance.inventory_serials WHERE "CompanyId"=NEW."CompanyId" AND "Id"=NEW."InventorySerialId";
          IF NOT FOUND OR line_row."CompanyId"<>NEW."CompanyId" OR line_row."ItemId"<>NEW."ItemId" OR serial_row."ItemId"<>NEW."ItemId" OR serial_row."StoredSerialNumber"<>NEW."StoredSerialNumberSnapshot" THEN RAISE EXCEPTION 'GRN serial company, Item and durable identity must match.'; END IF;
          IF NEW."EnteredSerialNumber"<>NEW."StoredSerialNumberSnapshot" AND NOT (NEW."DisambiguationApplied" AND NEW."DuplicateWarningAcknowledged") THEN RAISE EXCEPTION 'A changed stored serial requires acknowledged disambiguation.'; END IF;
          RETURN NEW;
        END $f$;
        """;
}