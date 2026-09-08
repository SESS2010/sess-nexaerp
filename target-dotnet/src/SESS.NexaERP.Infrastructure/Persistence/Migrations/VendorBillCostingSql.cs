namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class VendorBillCostingSql
{
    internal const string Preflight = """
        DO $preflight$ BEGIN
          IF to_regclass('advance.vendor_bills') IS NOT NULL OR to_regclass('advance.vendor_bill_lines') IS NOT NULL
             OR to_regclass('advance.vendor_bill_history') IS NOT NULL OR to_regclass('advance.vendor_bill_cost_allocations') IS NOT NULL
             OR to_regclass('advance.fifo_inventory_cost_layers') IS NOT NULL OR to_regclass('advance.fifo_cost_consumptions') IS NOT NULL
             OR to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.guard_vendor_bill_financial_evidence()') IS NOT NULL
             OR EXISTS (SELECT 1 FROM advance.page_definitions WHERE "PageKey"='accounts.vendor-bills') THEN
            RAISE EXCEPTION 'Refusing Vendor Bill installation because its schema, functions or page are partially present.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.goods_receipts WHERE "Status"='FINALIZED' AND "DocumentKind"='NORMAL')
             OR EXISTS (SELECT 1 FROM advance.material_issue_lines) THEN
            RAISE EXCEPTION 'Vendor Bill costing requires explicit reconciliation before installation when finalized GRN or issue evidence already exists.';
          END IF;
        END $preflight$;
        """;
    internal const string Up = """
        CREATE FUNCTION advance.create_fifo_layers_for_grn(p_company uuid,p_grn uuid,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE affected integer;
        BEGIN
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FIFO layer creation requires the current company-scoped ordinary GRN command.';
          END IF;
          INSERT INTO advance.fifo_inventory_cost_layers ("Id","CompanyId","GoodsReceiptLineId","ItemId","QuantityReceived","UnitCost","LayerValue","ReceivedAt","CostBasis","CreatedAt","CreatedBy")
          SELECT gen_random_uuid(),g."CompanyId",l."Id",l."ItemId",l."ReceivedQuantity",p."TotalPayableValue"/p."OrderedQuantity",l."ReceivedQuantity"*p."TotalPayableValue"/p."OrderedQuantity",g."ReceivedAt",'PO_PROVISIONAL_IDENTICAL',clock_timestamp(),p_login
          FROM advance.goods_receipts g JOIN advance.goods_receipt_lines l ON l."GoodsReceiptId"=g."Id" JOIN advance.purchase_order_lines p ON p."Id"=l."PurchaseOrderLineId"
          WHERE g."CompanyId"=p_company AND g."Id"=p_grn AND g."Status"='FINALIZED' AND g."DocumentKind"='NORMAL'
          ON CONFLICT ("CompanyId","GoodsReceiptLineId") DO NOTHING;
          GET DIAGNOSTICS affected=ROW_COUNT;
          IF affected<>(SELECT count(*) FROM advance.goods_receipt_lines WHERE "CompanyId"=p_company AND "GoodsReceiptId"=p_grn)
             AND EXISTS (SELECT 1 FROM advance.goods_receipt_lines l WHERE l."CompanyId"=p_company AND l."GoodsReceiptId"=p_grn AND NOT EXISTS (SELECT 1 FROM advance.fifo_inventory_cost_layers f WHERE f."CompanyId"=p_company AND f."GoodsReceiptLineId"=l."Id")) THEN RAISE EXCEPTION 'Every finalized GRN line requires exactly one FIFO cost layer.'; END IF;
          RETURN affected;
        END $function$;

        CREATE FUNCTION advance.consume_fifo_for_issue(p_company uuid,p_issue uuid,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE issue_line record; layer record; remaining numeric; take_quantity numeric; affected integer:=0;
        BEGIN
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FIFO consumption requires the current company-scoped ordinary Issue command.'; END IF;
          FOR issue_line IN SELECT * FROM advance.material_issue_lines WHERE "CompanyId"=p_company AND "MaterialIssueId"=p_issue ORDER BY "LineNumber" LOOP
            IF EXISTS (SELECT 1 FROM advance.fifo_cost_consumptions WHERE "CompanyId"=p_company AND "MaterialIssueLineId"=issue_line."Id") THEN CONTINUE; END IF;
            PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||p_company||':'||issue_line."ItemId",0)); remaining:=issue_line."QuantityBase";
            FOR layer IN SELECT f.*,f."QuantityReceived"-coalesce((SELECT sum(c."Quantity") FROM advance.fifo_cost_consumptions c WHERE c."FifoInventoryCostLayerId"=f."Id"),0) available FROM advance.fifo_inventory_cost_layers f
              WHERE f."CompanyId"=p_company AND f."ItemId"=issue_line."ItemId" AND f."QuantityReceived">coalesce((SELECT sum(c."Quantity") FROM advance.fifo_cost_consumptions c WHERE c."FifoInventoryCostLayerId"=f."Id"),0)
              ORDER BY f."ReceivedAt",f."Id" FOR UPDATE LOOP
              EXIT WHEN remaining<=0; take_quantity:=least(remaining,layer.available);
              INSERT INTO advance.fifo_cost_consumptions ("Id","CompanyId","FifoInventoryCostLayerId","MaterialIssueLineId","Quantity","UnitCost","ConsumedValue","ConsumedAt","CreatedBy") VALUES(gen_random_uuid(),p_company,layer."Id",issue_line."Id",take_quantity,layer."UnitCost",take_quantity*layer."UnitCost",clock_timestamp(),p_login);
              affected:=affected+1; remaining:=remaining-take_quantity;
            END LOOP;
            IF remaining>0 THEN RAISE EXCEPTION 'Insufficient FIFO cost-layer quantity for material issue.'; END IF;
          END LOOP; RETURN affected;
        END $function$;

        CREATE FUNCTION advance.create_vendor_bill(p_company uuid,p_grn uuid,p_number text,p_date date,p_lines jsonb,p_key text,p_hash text,p_correlation text,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS TABLE("VendorBillId" uuid,"Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE existing advance.vendor_bills%ROWTYPE; receipt advance.goods_receipts%ROWTYPE; bill_id uuid:=gen_random_uuid(); input record; grn_line record; po_line record; expected_value numeric; line_match text; bill_match text:='MATCHED'; total_value numeric:=0; line_no integer:=0;
        BEGIN
          SELECT * INTO existing FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "CreateIdempotencyKey"=btrim(p_key);
          IF FOUND THEN
            IF existing."CreateRequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Idempotency key was reused with different Vendor Bill content.'; END IF;
            IF existing."CreatedByEmployeeId"<>p_actor OR existing."ResolvedRoleAssignmentId"<>p_assignment
               OR existing."ActorRoleCode"<>p_role OR existing."ResolvedRoleAssignmentType"<>p_assignment_type
               OR p_role NOT IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
               OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill replay requires the same currently effective employee assignment.';
            END IF;
            RETURN QUERY SELECT existing."Id",true; RETURN;
          END IF;
          PERFORM pg_advisory_xact_lock(hashtextextended('VENDOR_BILL:GRN:'||p_company||':'||p_grn,0));
          PERFORM pg_advisory_xact_lock(hashtextextended('VENDOR_BILL:NUMBER:'||p_company||':'||upper(btrim(p_number)),0));
          IF EXISTS (SELECT 1 FROM advance.vendor_bills active_bill WHERE active_bill."CompanyId"=p_company AND active_bill."Status"<>'REVERSED'
             AND (active_bill."GoodsReceiptId"=p_grn OR active_bill."BillNumber"=btrim(p_number))) THEN
            RAISE EXCEPTION 'An active Vendor Bill already exists for this GRN or bill number.';
          END IF;          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill creation requires the current company-scoped ordinary command and effective assignment.'; END IF;
          IF p_role NOT IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER') THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill entry requires an Accounts role.'; END IF;
          SELECT * INTO receipt FROM advance.goods_receipts WHERE "CompanyId"=p_company AND "Id"=p_grn FOR SHARE;
          IF NOT FOUND OR receipt."Status"<>'FINALIZED' OR receipt."DocumentKind"<>'NORMAL' THEN RAISE EXCEPTION 'Vendor Bill entry requires a finalized normal GRN.'; END IF;
          IF btrim(p_number)<>receipt."VendorBillNumber" OR p_date<>receipt."VendorBillDate" THEN RAISE EXCEPTION 'Vendor Bill number and date must match the immutable GRN evidence.'; END IF;
          IF jsonb_typeof(p_lines)<>'array' OR jsonb_array_length(p_lines)=0 OR jsonb_array_length(p_lines)<>(SELECT count(*) FROM advance.goods_receipt_lines WHERE "GoodsReceiptId"=p_grn) THEN RAISE EXCEPTION 'Vendor Bill must contain every GRN line exactly once.'; END IF;
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          INSERT INTO advance.vendor_bills ("Id","BillNumber","BillDate","GoodsReceiptId","PurchaseOrderId","VendorId","Status","MatchStatus","TotalPayableValue","CreatedByEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CreateIdempotencyKey","CreateRequestFingerprint","CreatedAt","CreatedBy","Version","CompanyId") VALUES(bill_id,btrim(p_number),p_date,p_grn,receipt."PurchaseOrderId",receipt."VendorId",'DRAFT','MATCHED',0,p_actor,p_role,p_assignment,p_assignment_type,btrim(p_key),p_hash,clock_timestamp(),p_login,0,p_company);
          FOR input IN SELECT * FROM jsonb_to_recordset(p_lines) AS x("goodsReceiptLineId" uuid,"quantity" numeric,"unitRate" numeric,"totalPayableValue" numeric) LOOP
            SELECT l.* INTO STRICT grn_line FROM advance.goods_receipt_lines l WHERE l."CompanyId"=p_company AND l."GoodsReceiptId"=p_grn AND l."Id"=input."goodsReceiptLineId";
            SELECT p.* INTO STRICT po_line FROM advance.purchase_order_lines p WHERE p."Id"=grn_line."PurchaseOrderLineId";
            IF input."quantity"<>grn_line."ReceivedQuantity" THEN RAISE EXCEPTION 'Bill quantity mismatch: GRN quantity %, bill quantity %.',grn_line."ReceivedQuantity",input."quantity"; END IF;
            expected_value:=po_line."TotalPayableValue"*grn_line."ReceivedQuantity"/po_line."OrderedQuantity";
            line_match:=CASE WHEN input."unitRate"=po_line."UnitRate" AND input."totalPayableValue"=expected_value THEN 'MATCHED' ELSE 'PRICE_MISMATCH' END;
            IF line_match<>'MATCHED' THEN bill_match:='PRICE_MISMATCH'; END IF; total_value:=total_value+input."totalPayableValue"; line_no:=line_no+1;
            INSERT INTO advance.vendor_bill_lines ("Id","CompanyId","VendorBillId","LineNumber","GoodsReceiptLineId","PurchaseOrderLineId","ItemId","BilledQuantity","ExpectedUnitRate","BilledUnitRate","ExpectedPayableValue","BilledPayableValue","MatchStatus","CreatedAt","CreatedBy") VALUES(gen_random_uuid(),p_company,bill_id,line_no,grn_line."Id",po_line."Id",grn_line."ItemId",input."quantity",po_line."UnitRate",input."unitRate",expected_value,input."totalPayableValue",line_match,clock_timestamp(),p_login);
          END LOOP;
          IF (SELECT count(DISTINCT created_line."GoodsReceiptLineId") FROM advance.vendor_bill_lines created_line WHERE created_line."VendorBillId"=bill_id)<>(SELECT count(*) FROM advance.goods_receipt_lines received_line WHERE received_line."GoodsReceiptId"=p_grn) THEN RAISE EXCEPTION 'Vendor Bill must contain each GRN line exactly once.'; END IF;
          UPDATE advance.vendor_bills SET "MatchStatus"=bill_match,"TotalPayableValue"=total_value WHERE "Id"=bill_id;
          INSERT INTO advance.vendor_bill_history ("Id","CompanyId","VendorBillId","Action","FromStatus","ToStatus","ActorEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CorrelationId","Remarks","OccurredAt") VALUES(gen_random_uuid(),p_company,bill_id,'CREATED',NULL,'DRAFT',p_actor,p_role,p_assignment,p_assignment_type,p_correlation,bill_match,clock_timestamp());
          RETURN QUERY SELECT bill_id,false;
        END $function$;

        CREATE FUNCTION advance.decide_vendor_bill(p_company uuid,p_bill uuid,p_version bigint,p_accept boolean,p_reason text,p_key text,p_hash text,p_correlation text,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS TABLE("Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE bill advance.vendor_bills%ROWTYPE; mismatch record; target_status text:=CASE WHEN p_accept THEN 'ACCEPTED' ELSE 'REJECTED' END;
        BEGIN
          SELECT * INTO bill FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "Id"=p_bill; IF NOT FOUND THEN RAISE EXCEPTION 'Vendor Bill was not found in the selected company.'; END IF;
          IF bill."DecisionIdempotencyKey"=btrim(p_key) THEN
            IF bill."DecisionRequestFingerprint"<>p_hash OR bill."Status"<>target_status THEN RAISE EXCEPTION 'Idempotency key was reused with a different bill decision.'; END IF;
            IF bill."DecidedByEmployeeId"<>p_actor OR bill."DecisionRoleAssignmentId"<>p_assignment
               OR bill."DecisionActorRoleCode"<>p_role OR bill."DecisionRoleAssignmentType"<>p_assignment_type
               OR p_role<>'ACCOUNTS_MANAGER' OR p_assignment_type='SUPPORT'
               OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,CASE WHEN p_accept THEN 'approve' ELSE 'reject' END,ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill decision replay requires the same currently effective FULL or TEMPORARY Accounts Manager assignment.';
            END IF;
            RETURN QUERY SELECT true; RETURN;
          END IF;
          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR p_role<>'ACCOUNTS_MANAGER' OR p_assignment_type='SUPPORT'
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,CASE WHEN p_accept THEN 'approve' ELSE 'reject' END,ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill accept/reject requires current FULL or TEMPORARY Accounts Manager authority.'; END IF;
          SELECT * INTO bill FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "Id"=p_bill FOR UPDATE;
          IF bill."Status"<>'DRAFT' THEN RAISE EXCEPTION 'Only a Draft Vendor Bill can be decided.'; END IF; IF bill."Version"<>p_version THEN RAISE EXCEPTION 'Vendor Bill Version is stale.'; END IF;
          IF p_accept AND bill."MatchStatus"<>'MATCHED' THEN SELECT "ExpectedUnitRate","BilledUnitRate","ExpectedPayableValue","BilledPayableValue" INTO mismatch FROM advance.vendor_bill_lines WHERE "VendorBillId"=p_bill AND "MatchStatus"='PRICE_MISMATCH' ORDER BY "LineNumber" LIMIT 1; RAISE EXCEPTION 'Price mismatch: PO unit rate %, bill unit rate %, PO payable value %, bill payable value %. Reject the bill or revise the PO.',mismatch."ExpectedUnitRate",mismatch."BilledUnitRate",mismatch."ExpectedPayableValue",mismatch."BilledPayableValue"; END IF;
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          IF p_accept THEN INSERT INTO advance.vendor_bill_cost_allocations ("Id","CompanyId","VendorBillLineId","FifoInventoryCostLayerId","AllocatedQuantity","AcceptedValue","CreatedAt","CreatedBy") SELECT gen_random_uuid(),p_company,l."Id",f."Id",l."BilledQuantity",l."BilledPayableValue",clock_timestamp(),p_login FROM advance.vendor_bill_lines l JOIN advance.fifo_inventory_cost_layers f ON f."CompanyId"=l."CompanyId" AND f."GoodsReceiptLineId"=l."GoodsReceiptLineId" WHERE l."VendorBillId"=p_bill; IF NOT FOUND THEN RAISE EXCEPTION 'Accepted Vendor Bill requires its GRN FIFO layers.'; END IF; END IF;
          UPDATE advance.vendor_bills SET "Status"=target_status,"DecidedAt"=clock_timestamp(),"DecidedByEmployeeId"=p_actor,"DecisionActorRoleCode"=p_role,"DecisionRoleAssignmentId"=p_assignment,"DecisionRoleAssignmentType"=p_assignment_type,"DecisionReason"=btrim(p_reason),"DecisionIdempotencyKey"=btrim(p_key),"DecisionRequestFingerprint"=p_hash,"UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"=p_version+1 WHERE "Id"=p_bill;
          INSERT INTO advance.vendor_bill_history ("Id","CompanyId","VendorBillId","Action","FromStatus","ToStatus","ActorEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CorrelationId","Remarks","OccurredAt") VALUES(gen_random_uuid(),p_company,p_bill,target_status,'DRAFT',target_status,p_actor,p_role,p_assignment,p_assignment_type,p_correlation,btrim(p_reason),clock_timestamp());
          RETURN QUERY SELECT false;
        END $function$;

        CREATE FUNCTION advance.reverse_vendor_bill(p_company uuid,p_bill uuid,p_version bigint,p_reason text,p_key text,p_hash text,p_correlation text,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS TABLE("Replayed" boolean) LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE bill advance.vendor_bills%ROWTYPE;
        BEGIN
          SELECT * INTO bill FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "Id"=p_bill;
          IF NOT FOUND THEN RAISE EXCEPTION 'Vendor Bill was not found in the selected company.'; END IF;
          IF bill."ReversalIdempotencyKey"=btrim(p_key) THEN
            IF bill."ReversalRequestFingerprint"<>p_hash OR bill."Status"<>'REVERSED' THEN RAISE EXCEPTION 'Idempotency key was reused with a different bill reversal.'; END IF;
            IF bill."ReversedByEmployeeId"<>p_actor OR bill."ReversalRoleAssignmentId"<>p_assignment
               OR bill."ReversalActorRoleCode"<>p_role OR bill."ReversalRoleAssignmentType"<>p_assignment_type
               OR p_role<>'ACCOUNTS_MANAGER' OR p_assignment_type='SUPPORT'
               OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'reverse',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
              RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill reversal replay requires the same currently effective FULL or TEMPORARY Accounts Manager assignment.';
            END IF;
            RETURN QUERY SELECT true; RETURN;
          END IF;
          IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR p_role<>'ACCOUNTS_MANAGER' OR p_assignment_type='SUPPORT'
             OR NOT EXISTS (SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'reverse',ARRAY[p_role]) r WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
            RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill reversal requires current FULL or TEMPORARY Accounts Manager authority.';
          END IF;
          SELECT * INTO bill FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "Id"=p_bill FOR UPDATE;
          IF bill."Status"<>'ACCEPTED' THEN RAISE EXCEPTION 'Only an Accepted Vendor Bill can be reversed.'; END IF;
          IF bill."Version"<>p_version THEN RAISE EXCEPTION 'Vendor Bill Version is stale.'; END IF;
          PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
          UPDATE advance.vendor_bills SET "Status"='REVERSED',"ReversedAt"=clock_timestamp(),"ReversedByEmployeeId"=p_actor,
            "ReversalActorRoleCode"=p_role,"ReversalRoleAssignmentId"=p_assignment,"ReversalRoleAssignmentType"=p_assignment_type,
            "ReversalReason"=btrim(p_reason),"ReversalIdempotencyKey"=btrim(p_key),"ReversalRequestFingerprint"=p_hash,
            "UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"=p_version+1 WHERE "Id"=p_bill;
          INSERT INTO advance.vendor_bill_history ("Id","CompanyId","VendorBillId","Action","FromStatus","ToStatus","ActorEmployeeId","ActorRoleCode","ResolvedRoleAssignmentId","ResolvedRoleAssignmentType","CorrelationId","Remarks","OccurredAt")
            VALUES(gen_random_uuid(),p_company,p_bill,'REVERSED','ACCEPTED','REVERSED',p_actor,p_role,p_assignment,p_assignment_type,p_correlation,btrim(p_reason),clock_timestamp());
          RETURN QUERY SELECT false;
        END $function$;

        CREATE FUNCTION advance.vendor_bill_json(p_company uuid,p_bill uuid)
        RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
          SELECT jsonb_build_object('id',b."Id",'billNumber',b."BillNumber",'billDate',b."BillDate",'goodsReceiptId',b."GoodsReceiptId",'purchaseOrderId',b."PurchaseOrderId",'vendorId',b."VendorId",'status',b."Status",'matchStatus',b."MatchStatus",'totalPayableValue',b."TotalPayableValue",'version',b."Version",'replayed',false,'actorRoleCode',b."ActorRoleCode",'resolvedRoleAssignmentId',b."ResolvedRoleAssignmentId",'resolvedRoleAssignmentType',b."ResolvedRoleAssignmentType",'decidedAt',b."DecidedAt",'decidedByEmployeeId',b."DecidedByEmployeeId",'decisionReason',b."DecisionReason",'reversedAt',b."ReversedAt",'reversedByEmployeeId',b."ReversedByEmployeeId",'reversalReason',b."ReversalReason",'lines',coalesce((SELECT jsonb_agg(jsonb_build_object('id',l."Id",'lineNumber',l."LineNumber",'goodsReceiptLineId',l."GoodsReceiptLineId",'purchaseOrderLineId',l."PurchaseOrderLineId",'itemId',l."ItemId",'billedQuantity',l."BilledQuantity",'expectedUnitRate',l."ExpectedUnitRate",'billedUnitRate',l."BilledUnitRate",'expectedPayableValue',l."ExpectedPayableValue",'billedPayableValue',l."BilledPayableValue",'matchStatus',l."MatchStatus") ORDER BY l."LineNumber") FROM advance.vendor_bill_lines l WHERE l."CompanyId"=p_company AND l."VendorBillId"=b."Id"),'[]'::jsonb))
          FROM advance.vendor_bills b WHERE b."CompanyId"=p_company AND b."Id"=p_bill;
        $function$;
        CREATE FUNCTION advance.get_vendor_bill(p_company uuid,p_bill uuid)
        RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        BEGIN IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill projection requires the runtime principal.'; END IF; RETURN advance.vendor_bill_json(p_company,p_bill); END $function$;
        CREATE FUNCTION advance.list_vendor_bills(p_company uuid,p_number text,p_status text,p_vendor uuid,p_page integer,p_page_size integer)
        RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE result jsonb;
        BEGIN
          IF session_user<>'nexa_erp_runtime' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill projection requires the runtime principal.'; END IF;
          IF p_page<1 OR p_page_size<1 OR p_page_size>100 THEN RAISE EXCEPTION 'page must be positive and pageSize must be 1-100.'; END IF;
          SELECT jsonb_build_object('total',count(*),'page',p_page,'pageSize',p_page_size,'items',coalesce((SELECT jsonb_agg(advance.vendor_bill_json(p_company,x."Id") ORDER BY x."BillDate" DESC,x."Id") FROM (SELECT b2."Id",b2."BillDate" FROM advance.vendor_bills b2 WHERE b2."CompanyId"=p_company AND (nullif(btrim(p_number),'') IS NULL OR b2."BillNumber"=btrim(p_number)) AND (nullif(btrim(p_status),'') IS NULL OR b2."Status"=upper(btrim(p_status))) AND (p_vendor IS NULL OR b2."VendorId"=p_vendor) ORDER BY b2."BillDate" DESC,b2."Id" OFFSET (p_page-1)*p_page_size LIMIT p_page_size) x),'[]'::jsonb)) INTO result FROM advance.vendor_bills b WHERE b."CompanyId"=p_company AND (nullif(btrim(p_number),'') IS NULL OR b."BillNumber"=btrim(p_number)) AND (nullif(btrim(p_status),'') IS NULL OR b."Status"=upper(btrim(p_status))) AND (p_vendor IS NULL OR b."VendorId"=p_vendor);
          RETURN result;
        END $function$;

        CREATE FUNCTION advance.guard_vendor_bill_financial_evidence() RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor Bill and FIFO evidence may mutate only through its controlled function.'; END IF;
          IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Vendor Bill and FIFO costing evidence is immutable.'; END IF;
          IF TG_TABLE_NAME='vendor_bills' AND TG_OP='UPDATE' THEN
            IF OLD."Status"='DRAFT' THEN NULL;
            ELSIF OLD."Status"='ACCEPTED' AND NEW."Status"='REVERSED' THEN
              IF (to_jsonb(NEW)-ARRAY['Status','ReversedAt','ReversedByEmployeeId','ReversalActorRoleCode','ReversalRoleAssignmentId','ReversalRoleAssignmentType','ReversalReason','ReversalIdempotencyKey','ReversalRequestFingerprint','UpdatedAt','UpdatedBy','Version'])
                 IS DISTINCT FROM
                 (to_jsonb(OLD)-ARRAY['Status','ReversedAt','ReversedByEmployeeId','ReversalActorRoleCode','ReversalRoleAssignmentId','ReversalRoleAssignmentType','ReversalReason','ReversalIdempotencyKey','ReversalRequestFingerprint','UpdatedAt','UpdatedBy','Version']) THEN
                RAISE EXCEPTION 'Vendor Bill reversal may not rewrite accepted content or decision evidence.';
              END IF;
            ELSE RAISE EXCEPTION 'Accepted, rejected and reversed Vendor Bill content is immutable.';
            END IF;
          ELSIF TG_TABLE_NAME<>'vendor_bills' AND TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Vendor Bill and FIFO costing evidence is immutable.'; END IF;          RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
        END $function$;
        CREATE TRIGGER trg_vendor_bill_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_bills FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();
        CREATE TRIGGER trg_vendor_bill_line_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_bill_lines FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();
        CREATE TRIGGER trg_vendor_bill_history_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_bill_history FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();
        CREATE TRIGGER trg_vendor_bill_allocation_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_bill_cost_allocations FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();
        CREATE TRIGGER trg_fifo_layer_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.fifo_inventory_cost_layers FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();
        CREATE TRIGGER trg_fifo_consumption_guard BEFORE INSERT OR UPDATE OR DELETE ON advance.fifo_cost_consumptions FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_bill_financial_evidence();

        DO $page$ DECLARE page_id uuid:='52000000-0000-0000-0000-000000000008'; BEGIN
          INSERT INTO advance.page_definitions ("Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version") VALUES(page_id,'accounts.vendor-bills','Accounts','Vendor Bills','/accounts/vendor-bills',true,now(),'VendorBillAndAcceptedBillCosting',0);
          INSERT INTO advance.role_page_permissions ("Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit","CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification","CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint","CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment","CanViewCommercialValues","CanViewAuditHistory","HasFullControl","CreatedAt","CreatedBy","Version") SELECT md5('vendor-bill-'||r."Id")::uuid,r."Id",page_id,true,true,false,false,false,false,r."Code"='ACCOUNTS_MANAGER',r."Code"='ACCOUNTS_MANAGER',false,false,false,false,false,false,false,true,false,false,true,true,false,now(),'VendorBillAndAcceptedBillCosting',0 FROM advance.roles r WHERE r."Code" IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER') AND r."IsActive";
          UPDATE advance.role_page_permissions p SET "CanCancel"=true FROM advance.roles r WHERE p."RoleId"=r."Id" AND p."CreatedBy"='VendorBillAndAcceptedBillCosting' AND r."Code"='ACCOUNTS_MANAGER';
          IF (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='VendorBillAndAcceptedBillCosting')<>2 THEN RAISE EXCEPTION 'Vendor Bill requires exactly two Accounts role grants.'; END IF;
        END $page$;
        REVOKE ALL ON TABLE advance.vendor_bills,advance.vendor_bill_lines,advance.vendor_bill_history,advance.vendor_bill_cost_allocations,advance.fifo_inventory_cost_layers,advance.fifo_cost_consumptions FROM PUBLIC;
        REVOKE ALL ON FUNCTION advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text),advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text),advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text),advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text),advance.vendor_bill_json(uuid,uuid),advance.get_vendor_bill(uuid,uuid),advance.list_vendor_bills(uuid,text,text,uuid,integer,integer),advance.guard_vendor_bill_financial_evidence() FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.vendor_bills OWNER TO nexa_erp_owner; ALTER TABLE advance.vendor_bill_lines OWNER TO nexa_erp_owner; ALTER TABLE advance.vendor_bill_history OWNER TO nexa_erp_owner; ALTER TABLE advance.vendor_bill_cost_allocations OWNER TO nexa_erp_owner; ALTER TABLE advance.fifo_inventory_cost_layers OWNER TO nexa_erp_owner; ALTER TABLE advance.fifo_cost_consumptions OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.vendor_bill_json(uuid,uuid) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.get_vendor_bill(uuid,uuid) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.list_vendor_bills(uuid,text,text,uuid,integer,integer) OWNER TO nexa_erp_owner; ALTER FUNCTION advance.guard_vendor_bill_financial_evidence() OWNER TO nexa_erp_owner;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE ALL ON advance.vendor_bills,advance.vendor_bill_lines,advance.vendor_bill_history,advance.vendor_bill_cost_allocations,advance.fifo_inventory_cost_layers,advance.fifo_cost_consumptions FROM nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text),advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text),advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text),advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text),advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text),advance.get_vendor_bill(uuid,uuid),advance.list_vendor_bills(uuid,text,text,uuid,integer,integer) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DO $guard$ BEGIN IF EXISTS (SELECT 1 FROM advance.vendor_bills) OR EXISTS (SELECT 1 FROM advance.vendor_bill_history) OR EXISTS (SELECT 1 FROM advance.fifo_inventory_cost_layers) OR EXISTS (SELECT 1 FROM advance.fifo_cost_consumptions) THEN RAISE EXCEPTION 'Vendor Bill rollback refuses financial or costing evidence.'; END IF; END $guard$;
        DROP TRIGGER IF EXISTS trg_fifo_consumption_guard ON advance.fifo_cost_consumptions; DROP TRIGGER IF EXISTS trg_fifo_layer_guard ON advance.fifo_inventory_cost_layers; DROP TRIGGER IF EXISTS trg_vendor_bill_allocation_guard ON advance.vendor_bill_cost_allocations; DROP TRIGGER IF EXISTS trg_vendor_bill_history_guard ON advance.vendor_bill_history; DROP TRIGGER IF EXISTS trg_vendor_bill_line_guard ON advance.vendor_bill_lines; DROP TRIGGER IF EXISTS trg_vendor_bill_guard ON advance.vendor_bills; DROP FUNCTION IF EXISTS advance.guard_vendor_bill_financial_evidence();
        DROP FUNCTION IF EXISTS advance.list_vendor_bills(uuid,text,text,uuid,integer,integer); DROP FUNCTION IF EXISTS advance.get_vendor_bill(uuid,uuid); DROP FUNCTION IF EXISTS advance.vendor_bill_json(uuid,uuid); DROP FUNCTION IF EXISTS advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text); DROP FUNCTION IF EXISTS advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text); DROP FUNCTION IF EXISTS advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text); DROP FUNCTION IF EXISTS advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text); DROP FUNCTION IF EXISTS advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text);
        DELETE FROM advance.role_page_permissions WHERE "CreatedBy"='VendorBillAndAcceptedBillCosting'; DELETE FROM advance.page_definitions WHERE "CreatedBy"='VendorBillAndAcceptedBillCosting';
        """;
}