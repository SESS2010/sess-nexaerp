namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static partial class ComponentFitmentActualBomSql
{
    private const string ConfirmFunction = """
        CREATE FUNCTION advance.confirm_component_fitment(
          p_company uuid,p_job uuid,p_issue_line uuid,p_quantity numeric,p_fitted_at timestamptz,
          p_note text,p_reverifies uuid,p_key text,p_hash text,p_correlation text,
          p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
        RETURNS TABLE("ComponentFitmentId" uuid,"Replayed" boolean)
        LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE fitment_id uuid; prior_hash text; prior_actor uuid; prior_assignment uuid;
          fitment_number text; job_row advance.job_orders%ROWTYPE;
          line_row advance.material_issue_lines%ROWTYPE; issue_row advance.material_issues%ROWTYPE;
          item_row advance.items%ROWTYPE; location_row advance.warehouse_condition_locations%ROWTYPE;
          available numeric; returned numeric; fitted numeric; allocation_row record;
          bom_id uuid; batch_id uuid; material_value numeric; charge_value numeric;
        BEGIN
          IF p_company IS NULL OR p_job IS NULL OR p_issue_line IS NULL OR p_actor IS NULL
             OR p_assignment IS NULL OR p_quantity<=0 OR p_fitted_at IS NULL
             OR length(btrim(coalesce(p_note,'')))=0 OR length(btrim(coalesce(p_key,'')))=0
             OR p_hash !~ '^[0-9a-fA-F]{64}$' OR length(btrim(coalesce(p_correlation,'')))=0
             OR length(btrim(coalesce(p_login,'')))=0 THEN
            RAISE EXCEPTION 'Fitment requires job, issue line, positive quantity, date, note, idempotency and actor evidence.';
          END IF;

          PERFORM pg_advisory_xact_lock(hashtextextended('FITMENT:'||p_company||':'||btrim(p_key),0));
          SELECT "Id","RequestFingerprint","ConfirmedByEmployeeId","ResolvedRoleAssignmentId"
            INTO fitment_id,prior_hash,prior_actor,prior_assignment
            FROM advance.component_fitments WHERE "CompanyId"=p_company AND "IdempotencyKey"=btrim(p_key);
          IF FOUND THEN
            IF prior_hash=p_hash AND prior_actor=p_actor AND prior_assignment=p_assignment THEN
              RETURN QUERY SELECT fitment_id,true; RETURN;
            END IF;
            RAISE EXCEPTION 'Fitment idempotency key was reused with different content or authority.';
          END IF;
          IF NOT advance.fitment_authority_valid(p_company,p_actor,p_role,p_assignment,p_type,
              ARRAY['PRODUCTION_OPERATOR','PRODUCTION_COORDINATOR','PRODUCTION_MANAGER','SERVICE_ENGINEER']) THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Fitment confirmation requires a currently effective FULL PRODUCTION_OPERATOR, PRODUCTION_COORDINATOR, PRODUCTION_MANAGER or SERVICE_ENGINEER assignment in this company.';
          END IF;
          SELECT * INTO job_row FROM advance.job_orders
            WHERE "CompanyId"=p_company AND "Id"=p_job FOR UPDATE;
          IF NOT FOUND OR job_row."Status"<>'OPEN' THEN RAISE EXCEPTION 'Fitment requires an OPEN Job Order.'; END IF;
          SELECT * INTO line_row FROM advance.material_issue_lines
            WHERE "CompanyId"=p_company AND "Id"=p_issue_line FOR UPDATE;
          IF NOT FOUND THEN RAISE EXCEPTION 'MaterialIssueLineId was not found in this company.'; END IF;
          SELECT * INTO issue_row FROM advance.material_issues
            WHERE "CompanyId"=p_company AND "Id"=line_row."MaterialIssueId" FOR UPDATE;
          IF NOT FOUND OR issue_row."JobOrderId" IS DISTINCT FROM p_job
             OR issue_row."Status" NOT IN ('ISSUED','PARTIALLY_RETURNED','RETURNED') THEN
            RAISE EXCEPTION 'Fitment issue line must belong to the selected Job Order.';
          END IF;
          SELECT * INTO item_row FROM advance.items WHERE "Id"=line_row."ItemId";
          IF item_row."SerialNumberTracking" AND (p_quantity<>1 OR line_row."InventorySerialId" IS NULL) THEN
            RAISE EXCEPTION 'A serial-tracked fitment requires exactly one issued serial.';
          END IF;
          SELECT coalesce(sum(rl."ReturnedQuantityBase"),0) INTO returned
            FROM advance.material_return_lines rl JOIN advance.material_returns r ON r."Id"=rl."MaterialReturnId"
            WHERE rl."CompanyId"=p_company AND rl."MaterialIssueLineId"=p_issue_line AND r."Status"='ACCEPTED';
          SELECT coalesce(sum(f."QuantityBase"),0) INTO fitted
            FROM advance.component_fitments f LEFT JOIN advance.component_fitment_reversals r
              ON r."CompanyId"=f."CompanyId" AND r."ComponentFitmentId"=f."Id"
            WHERE f."CompanyId"=p_company AND f."MaterialIssueLineId"=p_issue_line AND r."Id" IS NULL;
          available:=line_row."QuantityBase"-returned-fitted;
          IF p_quantity>available THEN
            RAISE EXCEPTION 'Fitment quantity % exceeds engineer custody available % for this issue line.',p_quantity,available;
          END IF;
          IF p_reverifies IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM advance.component_fitments old
            JOIN advance.component_fitment_reversals rev ON rev."CompanyId"=old."CompanyId"
              AND rev."ComponentFitmentId"=old."Id"
            WHERE old."CompanyId"=p_company AND old."Id"=p_reverifies
              AND old."JobOrderId"=p_job AND old."MaterialIssueLineId"=p_issue_line
              AND old."QuantityBase"=p_quantity) THEN
            RAISE EXCEPTION 'Re-verification requires a reversed fitment for the same job, issue line and quantity.';
          END IF;
          SELECT a."AllocatedQuantity",a."AcceptedValue",a."AllocatedChargeValue",l."Id" bill_line_id,
                 b."BillNumber" bill_number,g."GrnNumber" grn_number
            INTO allocation_row
            FROM advance.vendor_bill_cost_allocations a
            JOIN advance.vendor_bill_lines l ON l."Id"=a."VendorBillLineId"
            JOIN advance.vendor_bills b ON b."Id"=l."VendorBillId"
            JOIN advance.goods_receipt_lines gl ON gl."Id"=l."GoodsReceiptLineId"
            JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId"
            WHERE a."CompanyId"=p_company AND l."GoodsReceiptLineId"=line_row."OriginGoodsReceiptLineId"
              AND b."Status"='ACCEPTED' ORDER BY b."DecidedAt" DESC NULLS LAST LIMIT 1;
          IF allocation_row IS NULL OR allocation_row."AllocatedQuantity"<=0 THEN
            RAISE EXCEPTION 'Fitment requires an accepted Vendor Bill allocation for the issued GRN line.';
          END IF;
          material_value:=round(allocation_row."AcceptedValue"/allocation_row."AllocatedQuantity"*p_quantity,6);
          charge_value:=round(allocation_row."AllocatedChargeValue"/allocation_row."AllocatedQuantity"*p_quantity,6);
          fitment_id:=gen_random_uuid();
          PERFORM pg_advisory_xact_lock(hashtextextended('FITMENT-NUMBER:'||p_company||':'||to_char(p_fitted_at,'YYYY'),0));
          SELECT 'FIT-'||c."Code"||'-'||to_char(p_fitted_at,'YYYY')||'-'
                 ||lpad((count(f."Id")+1)::text,6,'0') INTO fitment_number
            FROM advance.companies c LEFT JOIN advance.component_fitments f ON f."CompanyId"=c."Id"
            WHERE c."Id"=p_company GROUP BY c."Code";
          PERFORM set_config('advance.fitment_mutation',p_correlation,true);
          INSERT INTO advance.component_fitments
            ("Id","CompanyId","FitmentNumber","JobOrderId","MaterialIssueLineId","ReverifiesFitmentId",
             "QuantityBase","FittedAt","ConfirmedByEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId",
             "ResolvedRoleAssignmentType","ConfirmationNote","IdempotencyKey","RequestFingerprint",
             "CreatedAt","CreatedBy","Version")
          VALUES (fitment_id,p_company,fitment_number,p_job,p_issue_line,p_reverifies,p_quantity,p_fitted_at,
             p_actor,p_role,p_assignment,p_type,btrim(p_note),btrim(p_key),p_hash,clock_timestamp(),p_login,0);
          INSERT INTO advance.actual_boms
            ("Id","CompanyId","JobOrderId","GeneratedAt","CreatedAt","CreatedBy","Version")
          VALUES (gen_random_uuid(),p_company,p_job,clock_timestamp(),clock_timestamp(),p_login,0)
          ON CONFLICT ("CompanyId","JobOrderId") DO NOTHING;
          SELECT "Id" INTO STRICT bom_id FROM advance.actual_boms WHERE "CompanyId"=p_company AND "JobOrderId"=p_job;
          INSERT INTO advance.actual_bom_entries
            ("Id","CompanyId","ActualBomId","ComponentFitmentId","EntryKind","MaterialIssueLineId",
             "ItemId","UomId","QuantityBase","InventoryProvenanceLayerId","InventoryLotId",
             "InventorySerialId","GoodsReceiptLineId","GrnNumberSnapshot","VendorBillLineId",
             "VendorBillNumberSnapshot","AcceptedMaterialValue",
             "AllocatedChargeValue","TotalAcceptedValue","OccurredAt","CreatedBy")
          VALUES (gen_random_uuid(),p_company,bom_id,fitment_id,'FITMENT',p_issue_line,line_row."ItemId",
             item_row."BaseUomId",p_quantity,line_row."InventoryProvenanceLayerId",line_row."InventoryLotId",
             line_row."InventorySerialId",line_row."OriginGoodsReceiptLineId",allocation_row.grn_number,
             allocation_row.bill_line_id,allocation_row.bill_number,material_value,charge_value,material_value+charge_value,p_fitted_at,p_login);
          SELECT * INTO location_row FROM advance.warehouse_condition_locations
            WHERE "CompanyId"=p_company AND "Id"=line_row."WarehouseConditionLocationId";
          SELECT coalesce(sum(m."QuantityIn"-m."QuantityOut"),0) INTO available
            FROM advance.stock_movements m WHERE m."CompanyId"=p_company
              AND m."ItemId"=line_row."ItemId" AND m."CustodyAssignmentId"=line_row."ToCustodyAssignmentId"
              AND m."OwnershipAccountId"=line_row."OwnershipAccountId"
              AND m."InventoryProvenanceLayerId"=line_row."InventoryProvenanceLayerId"
              AND m."InventoryLotId" IS NOT DISTINCT FROM line_row."InventoryLotId"
              AND m."InventorySerialId" IS NOT DISTINCT FROM line_row."InventorySerialId";
          IF available<p_quantity THEN RAISE EXCEPTION 'Fitment exceeds the engineer custody stock balance.'; END IF;
          batch_id:=gen_random_uuid();
          INSERT INTO advance.stock_posting_batches
            ("Id","CompanyId","PostingKind","ComponentFitmentId","ReferenceType","ReferenceNumber",
             "PostingDate","PostedAt","PostedByEmployeeId","IdempotencyKey","RequestFingerprint",
             "CorrelationId","CreatedAt","CreatedBy","Version")
          VALUES (batch_id,p_company,'FITMENT_CONSUMPTION',fitment_id,'COMPONENT_FITMENT',
             fitment_number,p_fitted_at::date,clock_timestamp(),p_actor,'FITMENT:'||btrim(p_key),
             p_hash,p_correlation,clock_timestamp(),p_login,0);
          INSERT INTO advance.stock_movements
            ("Id","CompanyId","ItemId","WarehouseId","RackBinId","MovementType","ReferenceType",
             "ReferenceNumber","QuantityIn","QuantityOut","PostingDate","LedgerSchemaVersion",
             "WarehouseConditionLocationId","ConditionCode","StockPostingBatchId","BatchLineOrdinal",
             "MovementLeg","MaterialIssueRequestLineId","MaterialIssueLineId","ComponentFitmentId",
             "OriginGoodsReceiptLineId","OwnershipAccountId","CustodyAssignmentId",
             "InventoryProvenanceLayerId","CustodyCaseLineId","InventoryLotId","InventorySerialId",
             "GoodsReceiptLineLotAllocationId","QcInspectionLotDispositionId","PostingIdentity",
             "CreatedAt","CreatedBy","Version")
          VALUES (gen_random_uuid(),p_company,line_row."ItemId",location_row."WarehouseId",
             location_row."RackBinId",'CONSUMPTION_OUT','COMPONENT_FITMENT',fitment_number,0,p_quantity,
             p_fitted_at::date,2,line_row."WarehouseConditionLocationId",'AVAILABLE',batch_id,1,
             'CONSUMPTION_OUT',line_row."MaterialIssueRequestLineId",p_issue_line,fitment_id,
             line_row."OriginGoodsReceiptLineId",line_row."OwnershipAccountId",
             line_row."ToCustodyAssignmentId",line_row."InventoryProvenanceLayerId",
             line_row."CustodyCaseLineId",line_row."InventoryLotId",line_row."InventorySerialId",
             line_row."GoodsReceiptLineLotAllocationId",line_row."QcInspectionLotDispositionId",
             'FITMENT:'||fitment_id||':CONSUMPTION_OUT',clock_timestamp(),p_login,0);
          RETURN QUERY SELECT fitment_id,false;
        END $function$;
        REVOKE ALL ON FUNCTION advance.confirm_component_fitment(
          uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text) FROM PUBLIC;
        """;
}