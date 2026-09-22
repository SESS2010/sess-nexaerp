namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class OpeningStockSql
{
    internal const string Preflight = """
        DO $guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Opening Stock requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Opening Stock refuses a PostgreSQL administrative database.'; END IF;
          IF (SELECT count(*) FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) NOT IN (0,4) THEN RAISE EXCEPTION 'Partial NexaERP principal state; refusing to guess grants or ownership.'; END IF;
          IF to_regclass('advance.opening_stocks') IS NOT NULL OR to_regclass('advance.opening_stock_lines') IS NOT NULL OR to_regclass('advance.opening_stock_events') IS NOT NULL OR to_regclass('advance.opening_stock_import_staging_lines') IS NOT NULL THEN RAISE EXCEPTION 'Opening Stock is partially or already installed.'; END IF;
          IF to_regprocedure('advance.register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NULL OR to_regclass('advance.stock_movements') IS NULL OR to_regclass('advance.fifo_inventory_cost_layers') IS NULL THEN RAISE EXCEPTION 'Opening Stock requires the ordinary command ledger and witnessed stock/FIFO foundations.'; END IF;
        END $guard$;
        """;

    internal static string Up => Constraints + BatchGuard + MovementGuard + ReconcileGuard
        + Governance + StageFunction + CommandFunctions + Grants;

    private const string Constraints = """
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind" CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','MATERIAL_RETURN','FITMENT_CONSUMPTION','OPENING_BALANCE','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source" CHECK (("PostingKind"<>'REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","MaterialReturnId","ComponentFitmentId","OpeningStockId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1 AND "ReversesPostingBatchId" IS NULL) OR ("PostingKind"='REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","MaterialReturnId","ComponentFitmentId","OpeningStockId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=0 AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract" CHECK ("OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL AND "InventoryProvenanceLayerId" IS NOT NULL AND "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0 AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','RETURN_OUT','CONSUMPTION_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL') AND "PostingIdentity" IS NOT NULL AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","OpeningStockLineId","DeliveryChallanLineId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId","InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")+CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END=1 AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL) AND ("MaterialReturnLineId" IS NULL OR "MaterialIssueLineId" IS NOT NULL) AND ("ComponentFitmentId" IS NULL OR "MaterialIssueLineId" IS NOT NULL));
        """;

    private static string BatchGuard => ComponentFitmentActualBomSql.ActiveBatchGuard.Replace(
        "  ELSIF NEW.\"PostingKind\"='FITMENT_CONSUMPTION' THEN",
        """
          ELSIF NEW."PostingKind"='OPENING_BALANCE' THEN
            expected_type:='OPENING_BALANCE';
            SELECT "CompanyId",'OPENING-'||to_char("PeriodEnd",'YYYYMMDD') INTO source_company,expected_number FROM advance.opening_stocks WHERE "Id"=NEW."OpeningStockId";
          ELSIF NEW."PostingKind"='FITMENT_CONSUMPTION' THEN
        """);

    private static string MovementGuard => ComponentFitmentActualBomSql.ActiveMovementGuard.Replace(
        @"  ELSIF NEW.""MaterialReturnLineId"" IS NOT NULL THEN",
        """
          ELSIF NEW."OpeningStockLineId" IS NOT NULL THEN
            SELECT s."CompanyId",s."Id",l."ItemId" INTO source_company,source_header,source_item FROM advance.opening_stock_lines l JOIN advance.opening_stocks s ON s."Id"=l."OpeningStockId" WHERE l."Id"=NEW."OpeningStockLineId";
            IF b."PostingKind"<>'OPENING_BALANCE' OR source_header<>b."OpeningStockId" THEN RAISE EXCEPTION 'Opening Stock movement source does not match its batch.'; END IF;
            IF NEW."MovementLeg"<>'RECEIPT_IN' OR NEW."QuantityIn"<=0 OR NEW."QuantityOut"<>0 OR NEW."ConditionCode"<>'AVAILABLE' THEN RAISE EXCEPTION 'Opening Stock must enter AVAILABLE through a positive receipt leg.'; END IF;
          ELSIF NEW."MaterialReturnLineId" IS NOT NULL THEN
        """);

    private static string ReconcileGuard => ComponentFitmentActualBomSql.ActiveReconcileGuard.Replace(
        @"  ELSIF b.""PostingKind"" IN ('MATERIAL_RETURN','FITMENT_CONSUMPTION') THEN",
        """
          ELSIF b."PostingKind"='OPENING_BALANCE' THEN
            IF (SELECT count(*) FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id")<>(SELECT count(*) FROM advance.opening_stock_lines WHERE "OpeningStockId"=b."OpeningStockId") OR (SELECT coalesce(sum("QuantityIn"),0) FROM advance.stock_movements WHERE "StockPostingBatchId"=b."Id")<>(SELECT coalesce(sum("Quantity"),0) FROM advance.opening_stock_lines WHERE "OpeningStockId"=b."OpeningStockId") THEN RAISE EXCEPTION 'Opening Stock posting does not reconcile to its immutable imported lines.'; END IF;
          ELSIF b."PostingKind" IN ('MATERIAL_RETURN','FITMENT_CONSUMPTION') THEN
        """);

    private const string Governance = """
        CREATE FUNCTION advance.guard_opening_stock_evidence() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
        BEGIN
          IF current_setting('sess.opening_stock_write',true) IS DISTINCT FROM txid_current()::text THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock evidence may change only through a controlled function.'; END IF;
          IF TG_TABLE_NAME='opening_stock_lines' AND TG_OP='UPDATE' THEN
            IF num_nonnulls(OLD."InventoryLotId",OLD."InventorySerialId",OLD."InventoryProvenanceLayerId",OLD."FifoInventoryCostLayerId")<>0
               OR NEW."InventoryProvenanceLayerId" IS NULL OR NEW."FifoInventoryCostLayerId" IS NULL
               OR (OLD."Id",OLD."CompanyId",OLD."OpeningStockId",OLD."ImportStagingLineId",OLD."LineNumber",
                   OLD."LineReference",OLD."ItemId",OLD."WarehouseId",OLD."RackBinId",
                   OLD."WarehouseConditionLocationId",OLD."LotNumber",OLD."SerialNumber",OLD."Quantity",
                   OLD."UnitRate",OLD."LineValue",OLD."CreatedAt",OLD."CreatedBy")
                  IS DISTINCT FROM
                  (NEW."Id",NEW."CompanyId",NEW."OpeningStockId",NEW."ImportStagingLineId",NEW."LineNumber",
                   NEW."LineReference",NEW."ItemId",NEW."WarehouseId",NEW."RackBinId",
                   NEW."WarehouseConditionLocationId",NEW."LotNumber",NEW."SerialNumber",NEW."Quantity",
                   NEW."UnitRate",NEW."LineValue",NEW."CreatedAt",NEW."CreatedBy")
               OR NOT EXISTS(SELECT 1 FROM advance.opening_stocks s
                    WHERE s."Id"=NEW."OpeningStockId" AND s."CompanyId"=NEW."CompanyId" AND s."Status"='VALUED')
            THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock line identity may be bound exactly once during controlled authorization.'; END IF;
            RETURN NEW;
          END IF;
          IF TG_TABLE_NAME IN ('opening_stock_lines','opening_stock_events','opening_stock_import_staging_lines') AND TG_OP<>'INSERT' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock evidence is immutable.'; END IF;
          IF TG_TABLE_NAME='opening_stocks' AND TG_OP='DELETE' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock evidence is immutable.'; END IF;
          RETURN NEW;
        END $f$;
        CREATE TRIGGER trg_opening_stock_staging_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.opening_stock_import_staging_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_opening_stock_evidence();
        CREATE TRIGGER trg_opening_stock_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.opening_stocks FOR EACH ROW EXECUTE FUNCTION advance.guard_opening_stock_evidence();
        CREATE TRIGGER trg_opening_stock_line_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.opening_stock_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_opening_stock_evidence();
        CREATE TRIGGER trg_opening_stock_event_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.opening_stock_events FOR EACH ROW EXECUTE FUNCTION advance.guard_opening_stock_evidence();
        CREATE FUNCTION advance.opening_stock_command_valid(p_company uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_operation text,p_required_role text)
        RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
          SELECT session_user='nexa_erp_runtime' AND p_role=p_required_role AND p_type IN ('FULL','TEMPORARY')
           AND EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role))
           AND EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
           AND EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."Id"=p_assignment AND a."EmployeeId"=p_actor AND a."CompanyId"=p_company AND r."Code"=p_role AND a."AssignmentType"=p_type AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE));
        $f$;
        """;

    private const string StageFunction = """
        CREATE FUNCTION advance.stage_opening_stock_import_line(p_company uuid,p_reference text,p_item uuid,p_warehouse uuid,p_bin uuid,p_lot text,p_serial text,p_quantity numeric,p_rate numeric,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE result_id uuid:=gen_random_uuid(); organization text;
        BEGIN
          SELECT "Code" INTO organization FROM advance.companies WHERE "Id"=p_company AND "IsActive" AND "Status"='ACTIVE';
          IF session_user<>'nexa_erp_runtime' OR organization IS NULL OR p_role NOT IN ('STORES_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR') OR NOT EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId" WHERE a."Id"=p_assignment AND a."CompanyId"=p_company AND a."EmployeeId"=p_actor AND r."Code"=p_role AND a."AssignmentType"=p_type AND a."ApprovalStatus" IN ('Approved','SeedApproved') AND a."EffectiveFrom"<=CURRENT_DATE AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)) OR NOT advance.ordinary_command_context_valid(organization,p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role) OR NOT EXISTS(SELECT 1 FROM advance.command_requests r WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid AND r."Operation"='OpeningStock.Import' AND r."ResolvedRoleAssignmentId"=p_assignment) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock staging requires a registered current company-scoped import command.'; END IF;
          IF p_quantity<=0 OR p_rate<0 OR btrim(coalesce(p_reference,''))='' THEN RAISE EXCEPTION 'Opening Stock staging requires reference, positive quantity and non-negative rate.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM advance.items WHERE "Id"=p_item AND "IsActive" AND "ApprovalStatus"='Approved') THEN RAISE EXCEPTION 'Opening Stock staging requires an approved active item.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM advance.warehouses WHERE "Id"=p_warehouse AND "CompanyId"=p_company AND "IsActive") THEN RAISE EXCEPTION 'Opening Stock staging requires an active warehouse in the selected company.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM advance.rack_bins WHERE "Id"=p_bin AND "CompanyId"=p_company AND "WarehouseId"=p_warehouse AND "IsActive") THEN RAISE EXCEPTION 'Opening Stock staging requires an active rack in the selected company warehouse.'; END IF;
          PERFORM set_config('sess.opening_stock_write',txid_current()::text,true);
          INSERT INTO advance.opening_stock_import_staging_lines("Id","CompanyId","LineReference","ItemId","WarehouseId","RackBinId","LotNumber","SerialNumber","Quantity","UnitRate","CreatedAt","CreatedBy","Version") VALUES(result_id,p_company,btrim(p_reference),p_item,p_warehouse,p_bin,nullif(btrim(p_lot),''),nullif(btrim(p_serial),''),p_quantity,p_rate,clock_timestamp(),p_login,0);
          RETURN result_id;
        END $f$;
        """;

    private const string CommandFunctions = """
        CREATE FUNCTION advance.record_opening_stock_count(p_company uuid,p_batch uuid,p_from date,p_to date,p_reason text,p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("OpeningStockId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE old advance.opening_stocks%ROWTYPE; result_id uuid:=gen_random_uuid(); row_count integer;
        BEGIN
          SELECT * INTO old FROM advance.opening_stocks WHERE "CompanyId"=p_company AND "CountIdempotencyKey"=p_key;
          IF FOUND THEN
            IF old."CountRequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Opening Stock count idempotency mismatch.'; END IF;
            RETURN QUERY SELECT old."Id",true; RETURN;
          END IF;
          IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.RecordCount','STORES_MANAGER') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock count requires current FULL or TEMPORARY STORES_MANAGER authority.'; END IF;
          IF p_from IS NULL OR p_to IS NULL OR p_from>p_to OR btrim(coalesce(p_reason,''))='' THEN RAISE EXCEPTION 'Opening Stock requires a valid period and count reason.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('OPENING:'||p_company,0));
          IF EXISTS(SELECT 1 FROM advance.stock_movements WHERE "CompanyId"=p_company) THEN RAISE EXCEPTION 'Opening Stock is refused because this company already has stock movements.'; END IF;
          IF EXISTS(SELECT 1 FROM advance.opening_stocks WHERE "CompanyId"=p_company AND "PeriodStart"=p_from AND "PeriodEnd"=p_to) THEN RAISE EXCEPTION 'Opening Stock already exists for this company and period.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM advance.master_import_batches WHERE "Id"=p_batch AND "CompanyId"=p_company AND "MasterKey"='opening-stock' AND "Status"='COMPLETED' AND "InvalidRows"=0 AND "CreatedRows">0) THEN RAISE EXCEPTION 'Opening Stock requires a completed error-free import for this company.'; END IF;
          PERFORM set_config('sess.opening_stock_write',txid_current()::text,true);
          INSERT INTO advance.opening_stocks("Id","CompanyId","ImportBatchId","PeriodStart","PeriodEnd","Status","CountedByEmployeeId","CountActorRoleCode","CountRoleAssignmentId","CountRoleAssignmentType","CountedAt","CountReason","CountIdempotencyKey","CountRequestFingerprint","CreatedAt","CreatedBy","Version")
          VALUES(result_id,p_company,p_batch,p_from,p_to,'COUNTED',p_actor,p_role,p_assignment,p_type,clock_timestamp(),btrim(p_reason),p_key,p_hash,clock_timestamp(),p_login,0);
          INSERT INTO advance.opening_stock_lines("Id","CompanyId","OpeningStockId","ImportStagingLineId","LineNumber","LineReference","ItemId","WarehouseId","RackBinId","WarehouseConditionLocationId","LotNumber","SerialNumber","Quantity","UnitRate","LineValue","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),p_company,result_id,s."Id",row_number() OVER(ORDER BY r."SourceRowNumber"),s."LineReference",s."ItemId",s."WarehouseId",s."RackBinId",w."Id",s."LotNumber",s."SerialNumber",s."Quantity",s."UnitRate",s."Quantity"*s."UnitRate",clock_timestamp(),p_login
          FROM advance.master_import_row_results r JOIN advance.opening_stock_import_staging_lines s ON s."Id"=r."ResultRecordId" JOIN advance.warehouse_condition_locations w ON w."CompanyId"=p_company AND w."WarehouseId"=s."WarehouseId" AND w."RackBinId"=s."RackBinId" AND w."ConditionCode"='AVAILABLE' AND w."IsActive" AND w."EffectiveFrom"<=p_to AND (w."EffectiveTo" IS NULL OR w."EffectiveTo">=p_to)
          WHERE r."ImportBatchId"=p_batch AND r."Outcome"='CREATED' ORDER BY r."SourceRowNumber";
          GET DIAGNOSTICS row_count=ROW_COUNT;
          IF row_count<>(SELECT "CreatedRows" FROM advance.master_import_batches WHERE "Id"=p_batch) THEN RAISE EXCEPTION 'Every imported Opening Stock row requires one effective AVAILABLE rack location.'; END IF;
          INSERT INTO advance.opening_stock_events VALUES(gen_random_uuid(),p_company,result_id,'COUNT',NULL,'COUNTED',p_actor,p_role,p_assignment,p_type,btrim(p_reason),p_hash,clock_timestamp());
          RETURN QUERY SELECT result_id,false;
        END $f$;

        CREATE FUNCTION advance.confirm_opening_stock_value(p_company uuid,p_opening uuid,p_version bigint,p_reason text,p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("OpeningStockId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE row advance.opening_stocks%ROWTYPE;
        BEGIN
          SELECT * INTO row FROM advance.opening_stocks WHERE "CompanyId"=p_company AND "Id"=p_opening FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Opening Stock was not found.'; END IF;
          IF row."ValueIdempotencyKey"=p_key THEN
            IF row."ValueRequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Opening Stock value idempotency mismatch.'; END IF;
            RETURN QUERY SELECT row."Id",true; RETURN;
          END IF;
          IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.ConfirmValue','ACCOUNTS_MANAGER') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock valuation requires current FULL or TEMPORARY ACCOUNTS_MANAGER authority.'; END IF;
          IF row."Status"<>'COUNTED' OR row."Version"<>p_version THEN RAISE EXCEPTION 'Opening Stock valuation has stale version or invalid status.'; END IF;
          IF row."CountedByEmployeeId"=p_actor THEN RAISE EXCEPTION 'One employee may not count and value Opening Stock.'; END IF;
          IF btrim(coalesce(p_reason,''))='' THEN RAISE EXCEPTION 'Opening Stock valuation reason is required.'; END IF;
          PERFORM set_config('sess.opening_stock_write',txid_current()::text,true);
          UPDATE advance.opening_stocks SET "Status"='VALUED',"ValuedByEmployeeId"=p_actor,"ValueActorRoleCode"=p_role,"ValueRoleAssignmentId"=p_assignment,"ValueRoleAssignmentType"=p_type,"ValuedAt"=clock_timestamp(),"ValueReason"=btrim(p_reason),"ValueIdempotencyKey"=p_key,"ValueRequestFingerprint"=p_hash,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1 WHERE "Id"=p_opening;
          INSERT INTO advance.opening_stock_events VALUES(gen_random_uuid(),p_company,p_opening,'VALUE','COUNTED','VALUED',p_actor,p_role,p_assignment,p_type,btrim(p_reason),p_hash,clock_timestamp());
          RETURN QUERY SELECT p_opening,false;
        END $f$;

        CREATE FUNCTION advance.authorize_opening_stock(p_company uuid,p_opening uuid,p_version bigint,p_reason text,p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("OpeningStockId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
        DECLARE row advance.opening_stocks%ROWTYPE; line advance.opening_stock_lines%ROWTYPE;
                holder_id uuid; ownership_id uuid; custody_id uuid; custody_assignment_id uuid;
                lot_id uuid; serial_id uuid; provenance_id uuid; fifo_id uuid; batch_id uuid:=gen_random_uuid();
                account_code text; identity_hash text; normalized text;
        BEGIN
          SELECT * INTO row FROM advance.opening_stocks WHERE "CompanyId"=p_company AND "Id"=p_opening FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'Opening Stock was not found.'; END IF;
          IF row."AuthorizationIdempotencyKey"=p_key THEN
            IF row."AuthorizationRequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Opening Stock authorization idempotency mismatch.'; END IF;
            RETURN QUERY SELECT row."Id",true; RETURN;
          END IF;
          IF NOT advance.opening_stock_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'OpeningStock.Authorize','TECHNICAL_DIRECTOR') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Opening Stock authorization requires current FULL or TEMPORARY TECHNICAL_DIRECTOR authority.'; END IF;
          IF row."Status"<>'VALUED' OR row."Version"<>p_version THEN RAISE EXCEPTION 'Opening Stock authorization has stale version or invalid status.'; END IF;
          IF row."CountedByEmployeeId"=p_actor OR row."ValuedByEmployeeId"=p_actor OR row."CountedByEmployeeId"=row."ValuedByEmployeeId" THEN RAISE EXCEPTION 'Opening Stock requires three separate employees for count, value and authorization.'; END IF;
          IF btrim(coalesce(p_reason,''))='' THEN RAISE EXCEPTION 'Opening Stock authorization reason is required.'; END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('OPENING:'||p_company,0));
          IF EXISTS(SELECT 1 FROM advance.stock_movements WHERE "CompanyId"=p_company) THEN RAISE EXCEPTION 'Opening Stock is refused because this company already has stock movements.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM advance.opening_stock_lines osl WHERE osl."OpeningStockId"=p_opening) THEN RAISE EXCEPTION 'Opening Stock has no imported lines.'; END IF;
          PERFORM set_config('sess.opening_stock_write',txid_current()::text,true);
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);

          INSERT INTO advance.inventory_account_holders
            ("Id","CompanyId","HolderType","HolderCompanyId","HolderCode","HolderNameSnapshot","IsActive","CreatedAt","CreatedBy","Version")
          SELECT gen_random_uuid(),p_company,'COMPANY',p_company,'COMPANY-INVENTORY',c."LegalName",true,clock_timestamp(),p_login,0
          FROM advance.companies c WHERE c."Id"=p_company ON CONFLICT ("CompanyId","HolderCode") DO NOTHING;
          SELECT "Id" INTO holder_id FROM advance.inventory_account_holders WHERE "CompanyId"=p_company AND "HolderCode"='COMPANY-INVENTORY' AND "HolderType"='COMPANY' AND "HolderCompanyId"=p_company AND "IsActive";
          IF holder_id IS NULL THEN RAISE EXCEPTION 'Company inventory holder is missing or incompatible.'; END IF;
          INSERT INTO advance.inventory_ownership_accounts
            ("Id","CompanyId","AccountHolderId","AccountCode","OwnershipType","InventoryValuationBasis","CurrencyCode","IsActive","CreatedAt","CreatedBy","Version")
          VALUES(gen_random_uuid(),p_company,holder_id,'SESS-INVENTORY','SESS_INVENTORY','FIFO','INR',true,clock_timestamp(),p_login,0)
          ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
          SELECT "Id" INTO ownership_id FROM advance.inventory_ownership_accounts WHERE "CompanyId"=p_company AND "AccountCode"='SESS-INVENTORY' AND "AccountHolderId"=holder_id AND "OwnershipType"='SESS_INVENTORY' AND "InventoryValuationBasis"='FIFO' AND "IsActive";
          IF ownership_id IS NULL THEN RAISE EXCEPTION 'SESS inventory ownership account is missing or incompatible.'; END IF;

          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","OpeningStockId","ReferenceType","ReferenceNumber","PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint","CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES(batch_id,p_company,'OPENING_BALANCE',p_opening,'OPENING_BALANCE','OPENING-'||to_char(row."PeriodEnd",'YYYYMMDD'),row."PeriodEnd",clock_timestamp(),p_actor,p_key,p_hash,p_hash,clock_timestamp(),p_login,0);

          FOR line IN SELECT osl.* FROM advance.opening_stock_lines osl WHERE osl."OpeningStockId"=p_opening ORDER BY osl."LineNumber" LOOP
            lot_id:=NULL; serial_id:=NULL;
            IF line."LotNumber" IS NOT NULL THEN
              normalized:=upper(btrim(regexp_replace(line."LotNumber",'[[:space:]]+',' ','g')));
              INSERT INTO advance.inventory_lots("Id","CompanyId","ItemId","SupplierLotNumber","NormalizedSupplierLotNumber","CreatedAt","CreatedBy")
              VALUES(gen_random_uuid(),p_company,line."ItemId",line."LotNumber",normalized,clock_timestamp(),p_login)
              RETURNING "Id" INTO lot_id;
            END IF;
            IF line."SerialNumber" IS NOT NULL THEN
              normalized:=upper(regexp_replace(btrim(line."SerialNumber"),'[^A-Z0-9]','','g'));
              INSERT INTO advance.inventory_serials("Id","CompanyId","ItemId","StoredSerialNumber","NormalizedStoredSerialNumber","FirstCapturedAt","FirstCapturedByEmployeeId","CreatedAt","CreatedBy")
              VALUES(gen_random_uuid(),p_company,line."ItemId",line."SerialNumber",normalized,clock_timestamp(),p_actor,clock_timestamp(),p_login)
              RETURNING "Id" INTO serial_id;
            END IF;
            account_code:='WH-'||left(replace(line."WarehouseId"::text,'-',''),12)||'-'||left(replace(line."RackBinId"::text,'-',''),12);
            INSERT INTO advance.inventory_custody_accounts
              ("Id","CompanyId","AccountHolderId","AccountCode","CustodyType","WarehouseId","RackBinId","IsActive","CreatedAt","CreatedBy","Version")
            VALUES(gen_random_uuid(),p_company,holder_id,account_code,'WAREHOUSE',line."WarehouseId",line."RackBinId",true,clock_timestamp(),p_login,0)
            ON CONFLICT ("CompanyId","AccountCode") DO NOTHING;
            SELECT "Id" INTO custody_id FROM advance.inventory_custody_accounts WHERE "CompanyId"=p_company AND "AccountCode"=account_code AND "AccountHolderId"=holder_id AND "CustodyType"='WAREHOUSE' AND "WarehouseId"=line."WarehouseId" AND "RackBinId"=line."RackBinId" AND "IsActive";
            IF custody_id IS NULL THEN RAISE EXCEPTION 'Opening Stock warehouse custody account is missing or incompatible.'; END IF;
            custody_assignment_id:=md5('OPEN:CUST:'||p_company||':'||line."Id")::uuid;
            INSERT INTO advance.inventory_custody_assignments
              ("Id","CompanyId","CustodyAccountId","WarehouseId","RackBinId","AssignedQuantity","EffectiveFrom","IsCurrent","AssignmentReason","CreatedAt","CreatedBy","Version")
            VALUES(custody_assignment_id,p_company,custody_id,line."WarehouseId",line."RackBinId",line."Quantity",row."PeriodEnd"::timestamp AT TIME ZONE 'UTC',true,'Opening balance '||line."LineReference",clock_timestamp(),p_login,0);
            identity_hash:=encode(pg_catalog.sha256(convert_to('OPENING:'||p_company||':'||line."Id",'UTF8')),'hex');
            INSERT INTO advance.inventory_provenance_layers
              ("Id","CompanyId","ItemId","InventoryLotId","InventorySerialId","LayerType","QuantityCreated","UomId","Status","IdentityHash","CreatedAt","CreatedBy")
            SELECT gen_random_uuid(),p_company,line."ItemId",lot_id,serial_id,'RECEIPT',line."Quantity",i."BaseUomId",'ACTIVE',identity_hash,clock_timestamp(),p_login FROM advance.items i WHERE i."Id"=line."ItemId"
            RETURNING "Id" INTO provenance_id;
            INSERT INTO advance.fifo_inventory_cost_layers
              ("Id","CompanyId","OpeningStockLineId","ItemId","QuantityReceived","UnitCost","LayerValue","ReceivedAt","CostBasis","CreatedAt","CreatedBy")
            VALUES(gen_random_uuid(),p_company,line."Id",line."ItemId",line."Quantity",line."UnitRate",line."LineValue",row."PeriodEnd"::timestamp AT TIME ZONE 'UTC','OPENING_LANDED',clock_timestamp(),p_login)
            RETURNING "Id" INTO fifo_id;
            UPDATE advance.opening_stock_lines SET "InventoryLotId"=lot_id,"InventorySerialId"=serial_id,"InventoryProvenanceLayerId"=provenance_id,"FifoInventoryCostLayerId"=fifo_id WHERE "Id"=line."Id";
            INSERT INTO advance.stock_movements
              ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType","ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion","WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal","MovementLeg","OpeningStockLineId","OwnershipAccountId","CustodyAssignmentId","InventoryProvenanceLayerId","InventoryLotId","InventorySerialId","PostingIdentity","CreatedAt","CreatedBy","Version")
            VALUES(gen_random_uuid(),p_company,line."ItemId",line."WarehouseId",line."RackBinId",'OPENING_BALANCE','OPENING_BALANCE','OPENING-'||to_char(row."PeriodEnd",'YYYYMMDD'),line."Quantity",0,row."PeriodEnd",2,line."WarehouseConditionLocationId",'AVAILABLE',batch_id,line."LineNumber",'RECEIPT_IN',line."Id",ownership_id,custody_assignment_id,provenance_id,lot_id,serial_id,'OPENING_BALANCE:'||line."Id",clock_timestamp(),p_login,0);
          END LOOP;
          UPDATE advance.opening_stocks SET "Status"='POSTED',"AuthorizedByEmployeeId"=p_actor,"AuthorizationActorRoleCode"=p_role,"AuthorizationRoleAssignmentId"=p_assignment,"AuthorizationRoleAssignmentType"=p_type,"AuthorizedAt"=clock_timestamp(),"AuthorizationReason"=btrim(p_reason),"AuthorizationIdempotencyKey"=p_key,"AuthorizationRequestFingerprint"=p_hash,"StockPostingBatchId"=batch_id,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1 WHERE "Id"=p_opening;
          INSERT INTO advance.opening_stock_events VALUES(gen_random_uuid(),p_company,p_opening,'AUTHORIZE','VALUED','POSTED',p_actor,p_role,p_assignment,p_type,btrim(p_reason),p_hash,clock_timestamp());
          RETURN QUERY SELECT p_opening,false;
        END $f$;
        """;
    private const string Grants = """
        REVOKE ALL ON FUNCTION advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text),advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text),advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text),advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC;
        REVOKE ALL ON advance.opening_stock_import_staging_lines,advance.opening_stocks,advance.opening_stock_lines,advance.opening_stock_events FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.opening_stock_import_staging_lines OWNER TO nexa_erp_owner;
            ALTER TABLE advance.opening_stocks OWNER TO nexa_erp_owner;
            ALTER TABLE advance.opening_stock_lines OWNER TO nexa_erp_owner;
            ALTER TABLE advance.opening_stock_events OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_opening_stock_evidence() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            REVOKE ALL ON advance.opening_stock_import_staging_lines,advance.opening_stocks,advance.opening_stock_lines,advance.opening_stock_events FROM nexa_erp_runtime;
            GRANT SELECT ON advance.opening_stock_import_staging_lines,advance.opening_stocks,advance.opening_stock_lines,advance.opening_stock_events TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text),advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text),advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text),advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        DO $grants$ DECLARE affected integer; BEGIN
          UPDATE advance.role_page_permissions p SET "CanView"=true,"CanDownload"=true,"CanExport"=true,
            "CanCreate"=r."Code"='STORES_MANAGER',"CanVerify"=r."Code"='ACCOUNTS_MANAGER',"CanApprove"=r."Code"='TECHNICAL_DIRECTOR',
            "UpdatedAt"=clock_timestamp(),"UpdatedBy"='GovernedOpeningStockThreeActorCeremony',"Version"=p."Version"+1
          FROM advance.roles r,advance.page_definitions d
          WHERE p."RoleId"=r."Id" AND p."PageDefinitionId"=d."Id" AND d."PageKey"='stores.opening-stock' AND r."Code" IN ('STORES_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR');
          GET DIAGNOSTICS affected=ROW_COUNT; IF affected<>3 THEN RAISE EXCEPTION 'Opening Stock expected exactly three role-page grant rows, found %.',affected; END IF;
        END $grants$;
        """;
    internal static string Down => """
        DO $guard$ BEGIN
          IF current_setting('server_version_num')::integer < 170000 OR current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Opening Stock rollback guard refused this cluster or database.'; END IF;
          IF EXISTS(SELECT 1 FROM advance.opening_stock_import_staging_lines) OR EXISTS(SELECT 1 FROM advance.opening_stocks) OR EXISTS(SELECT 1 FROM advance.opening_stock_lines) OR EXISTS(SELECT 1 FROM advance.opening_stock_events) OR EXISTS(SELECT 1 FROM advance.stock_movements WHERE "OpeningStockLineId" IS NOT NULL) OR EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers WHERE "OpeningStockLineId" IS NOT NULL) OR EXISTS(SELECT 1 FROM advance.inventory_lots WHERE "VendorId" IS NULL) THEN RAISE EXCEPTION 'Opening Stock rollback refuses imported, workflow, stock, costing or vendorless-lot evidence.'; END IF;
        END $guard$;
        DROP TRIGGER IF EXISTS trg_opening_stock_event_guard ON advance.opening_stock_events;
        DROP TRIGGER IF EXISTS trg_opening_stock_line_guard ON advance.opening_stock_lines;
        DROP TRIGGER IF EXISTS trg_opening_stock_guard ON advance.opening_stocks;
        DROP TRIGGER IF EXISTS trg_opening_stock_staging_guard ON advance.opening_stock_import_staging_lines;
        DROP FUNCTION IF EXISTS advance.authorize_opening_stock(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.confirm_opening_stock_value(uuid,uuid,bigint,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.record_opening_stock_count(uuid,uuid,date,date,text,text,text,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.stage_opening_stock_import_line(uuid,text,uuid,uuid,uuid,text,text,numeric,numeric,uuid,text,uuid,text,text);
        DROP FUNCTION IF EXISTS advance.opening_stock_command_valid(uuid,uuid,text,uuid,text,text,text);
        DROP FUNCTION IF EXISTS advance.guard_opening_stock_evidence();
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_kind";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_kind" CHECK ("PostingKind" IN ('GRN_CUSTODY','QC_DISPOSITION','CONCESSION_ACCEPTANCE','MATERIAL_ISSUE','MATERIAL_RETURN','FITMENT_CONSUMPTION','DC_DISPATCH','DC_RETURN_CUSTODY','REVERSAL'));
        ALTER TABLE advance.stock_posting_batches DROP CONSTRAINT "CK_stock_posting_batch_source";
        ALTER TABLE advance.stock_posting_batches ADD CONSTRAINT "CK_stock_posting_batch_source" CHECK (("PostingKind"<>'REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","MaterialReturnId","ComponentFitmentId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=1 AND "ReversesPostingBatchId" IS NULL) OR ("PostingKind"='REVERSAL' AND num_nonnulls("GoodsReceiptId","QcInspectionRevisionId","MaterialIssueRequestId","MaterialReturnId","ComponentFitmentId","DeliveryChallanId","InventoryCustodyHandoffId","InventoryOwnershipTransferId","InventoryTransformationId","InventoryConcessionId")=0 AND "ReversesPostingBatchId" IS NOT NULL));
        ALTER TABLE advance.stock_movements DROP CONSTRAINT "CK_stock_movement_v2_contract";
        ALTER TABLE advance.stock_movements ADD CONSTRAINT "CK_stock_movement_v2_contract" CHECK ("OwnershipAccountId" IS NOT NULL AND "CustodyAssignmentId" IS NOT NULL AND "InventoryProvenanceLayerId" IS NOT NULL AND "WarehouseId" IS NOT NULL AND "RackBinId" IS NOT NULL AND "WarehouseConditionLocationId" IS NOT NULL AND "ConditionCode" IS NOT NULL AND "StockPostingBatchId" IS NOT NULL AND "BatchLineOrdinal">0 AND "MovementLeg" IN ('RECEIPT_IN','TRANSFER_OUT','TRANSFER_IN','ISSUE_OUT','RETURN_OUT','CONSUMPTION_OUT','DISPATCH_OUT','RETURN_IN','REVERSAL') AND "PostingIdentity" IS NOT NULL AND num_nonnulls("GoodsReceiptLineId","QcInspectionRevisionId","MaterialIssueRequestLineId","DeliveryChallanLineId","InventoryCustodyHandoffLineId","InventoryOwnershipTransferLineId","InventoryTransformationInputId","InventoryTransformationOutputId","InventoryConcessionAllocationId")+CASE WHEN "QcInspectionLotDispositionId" IS NOT NULL AND "MaterialIssueRequestLineId" IS NULL THEN 1 ELSE 0 END=1 AND ("MaterialIssueLineId" IS NULL OR "MaterialIssueRequestLineId" IS NOT NULL) AND ("MaterialReturnLineId" IS NULL OR "MaterialIssueLineId" IS NOT NULL) AND ("ComponentFitmentId" IS NULL OR "MaterialIssueLineId" IS NOT NULL));
        """ + ComponentFitmentActualBomSql.ActiveBatchGuard + ComponentFitmentActualBomSql.ActiveMovementGuard + ComponentFitmentActualBomSql.ActiveReconcileGuard;
}
