namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class ImmutableLandedCostAdjustmentsSql
{
    internal const string Preflight = """
        DO $guard$
        DECLARE found_count integer;
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN
            RAISE EXCEPTION 'Immutable landed cost requires PostgreSQL 17 or later.';
          END IF;
          IF current_database() IN ('postgres','template0','template1') THEN
            RAISE EXCEPTION 'Immutable landed cost refuses a PostgreSQL administrative database.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.vendor_bills) THEN
            RAISE EXCEPTION 'Existing Vendor Bills require an explicit landed-cost reconciliation; this migration will not invent history.';
          END IF;
          IF to_regclass('advance.vendor_bill_charges') IS NOT NULL
             OR to_regclass('advance.vendor_bill_charge_allocations') IS NOT NULL
             OR to_regclass('advance.fifo_landed_cost_adjustments') IS NOT NULL
             OR to_regclass('advance.actual_bom_valuation_adjustments') IS NOT NULL
             OR to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NOT NULL
             OR to_regprocedure('advance.allocate_vendor_bill_landed_cost()') IS NOT NULL THEN
            RAISE EXCEPTION 'Immutable landed-cost objects are partially present; refusing to guess.';
          END IF;
          SELECT count(*) INTO found_count FROM pg_roles WHERE rolname IN
            ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime');
          IF found_count NOT IN (0,4) THEN
            RAISE EXCEPTION 'Partial NexaERP principal state; refusing to guess grants or ownership.';
          END IF;
        END $guard$;
        """;

    internal const string Up = """
        CREATE FUNCTION advance.record_vendor_bill_charges(
          p_company uuid,p_bill uuid,p_lines jsonb,p_charges jsonb,p_actor uuid,p_role text,
          p_assignment uuid,p_assignment_type text,p_login text)
        RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE bill advance.vendor_bills%ROWTYPE; charge_input record; charge_no integer:=0;
          weight_complete boolean; charge_type text; recoverable boolean; affected integer:=0;
        BEGIN
          IF NOT advance.ordinary_command_context_valid(
              (SELECT "Code" FROM advance.companies WHERE "Id"=p_company),p_actor,
              current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
             OR NOT EXISTS (
               SELECT 1 FROM advance.resolve_employee_role_authority(
                 p_actor,p_company,current_date,'create',ARRAY[p_role]) r
               WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type)
             OR p_role NOT IN ('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER') THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Vendor Bill charge entry requires the current company-scoped Accounts command.';
          END IF;
          SELECT * INTO bill FROM advance.vendor_bills
            WHERE "CompanyId"=p_company AND "Id"=p_bill FOR UPDATE;
          IF NOT FOUND OR bill."Status"<>'DRAFT' OR bill."CreatedByEmployeeId"<>p_actor
             OR bill."ResolvedRoleAssignmentId"<>p_assignment THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Charges may be recorded only with the command that created this Draft Vendor Bill.';
          END IF;
          IF EXISTS (SELECT 1 FROM advance.vendor_bill_charges
                     WHERE "CompanyId"=p_company AND "VendorBillId"=p_bill) THEN
            RETURN 0;
          END IF;
          IF jsonb_typeof(coalesce(p_lines,'[]'::jsonb))<>'array'
             OR jsonb_typeof(coalesce(p_charges,'[]'::jsonb))<>'array' THEN
            RAISE EXCEPTION 'Vendor Bill lines and charges must be arrays.';
          END IF;
          PERFORM set_config('sess.landed_cost_write',txid_current()::text,true);
          UPDATE advance.vendor_bill_lines l SET "VerifiedGrossWeightKg"=x.weight
          FROM (
            SELECT "goodsReceiptLineId" id,"verifiedGrossWeightKg" weight
            FROM jsonb_to_recordset(p_lines)
              AS z("goodsReceiptLineId" uuid,"verifiedGrossWeightKg" numeric)
          ) x
          WHERE l."CompanyId"=p_company AND l."VendorBillId"=p_bill
            AND l."GoodsReceiptLineId"=x.id;
          IF EXISTS (SELECT 1 FROM advance.vendor_bill_lines
                     WHERE "CompanyId"=p_company AND "VendorBillId"=p_bill
                       AND "VerifiedGrossWeightKg" IS NOT NULL AND "VerifiedGrossWeightKg"<=0) THEN
            RAISE EXCEPTION 'Verified gross weight must be positive when supplied.';
          END IF;
          SELECT bool_and("VerifiedGrossWeightKg">0) INTO weight_complete
          FROM advance.vendor_bill_lines WHERE "CompanyId"=p_company AND "VendorBillId"=p_bill;
          FOR charge_input IN
            SELECT * FROM jsonb_to_recordset(coalesce(p_charges,'[]'::jsonb))
              AS x("chargeType" text,"chargeValue" numeric,"isRecoverableTax" boolean)
          LOOP
            charge_type:=upper(btrim(coalesce(charge_input."chargeType",'')));
            recoverable:=coalesce(charge_input."isRecoverableTax",false);
            IF charge_type NOT IN ('DUTY','INSURANCE','FREIGHT','PACKING','HANDLING',
                'CLEARING_AGENT','MISC_INWARD','NON_CREDITABLE_TAX','RECOVERABLE_GST')
               OR charge_input."chargeValue" IS NULL OR charge_input."chargeValue"<=0 THEN
              RAISE EXCEPTION 'Every Vendor Bill charge requires a supported type and positive value.';
            END IF;
            IF recoverable IS DISTINCT FROM (charge_type='RECOVERABLE_GST') THEN
              RAISE EXCEPTION 'Only RECOVERABLE_GST may be excluded as recoverable tax.';
            END IF;
            charge_no:=charge_no+1;
            INSERT INTO advance.vendor_bill_charges
              ("Id","CompanyId","VendorBillId","ChargeNumber","ChargeType","ChargeValue",
               "IsRecoverableTax","IncludedInInventoryCost","AllocationBasis","CreatedAt","CreatedBy")
            VALUES(gen_random_uuid(),p_company,p_bill,charge_no,charge_type,
              round(charge_input."chargeValue",2),recoverable,NOT recoverable,
              CASE WHEN recoverable THEN 'EXCLUDED'
                   WHEN charge_type='FREIGHT' AND weight_complete THEN 'GROSS_WEIGHT'
                   ELSE 'ITEM_VALUE' END,clock_timestamp(),p_login);
            affected:=affected+1;
          END LOOP;
          UPDATE advance.vendor_bills SET
            "TotalChargeValue"=coalesce((SELECT sum("ChargeValue")
              FROM advance.vendor_bill_charges WHERE "CompanyId"=p_company
                AND "VendorBillId"=p_bill),0),
            "TotalLandedValue"="TotalPayableValue"+coalesce((SELECT sum("ChargeValue")
              FROM advance.vendor_bill_charges WHERE "CompanyId"=p_company
                AND "VendorBillId"=p_bill AND "IncludedInInventoryCost"),0)
          WHERE "CompanyId"=p_company AND "Id"=p_bill;
          RETURN affected;
        END $function$;

        CREATE FUNCTION advance.allocate_vendor_bill_landed_cost()
        RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE charge_row record; line_row record; total_basis numeric; running numeric;
          allocation numeric; line_count integer; line_index integer; consumed numeric;
          remaining numeric; line_charge numeric; consumed_adjustment numeric;
        BEGIN
          IF NOT (OLD."Status"='DRAFT' AND NEW."Status"='ACCEPTED') THEN RETURN NEW; END IF;
          IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Landed cost may be allocated only in controlled Vendor Bill acceptance.';
          END IF;
          FOR charge_row IN SELECT * FROM advance.vendor_bill_charges
            WHERE "CompanyId"=NEW."CompanyId" AND "VendorBillId"=NEW."Id"
              AND "IncludedInInventoryCost" ORDER BY "ChargeNumber"
          LOOP
            SELECT count(*),sum(CASE WHEN charge_row."AllocationBasis"='GROSS_WEIGHT'
              THEN "VerifiedGrossWeightKg" ELSE "BilledPayableValue" END)
              INTO line_count,total_basis
            FROM advance.vendor_bill_lines WHERE "CompanyId"=NEW."CompanyId"
              AND "VendorBillId"=NEW."Id";
            IF total_basis IS NULL OR total_basis<=0 THEN
              RAISE EXCEPTION 'Landed-cost allocation basis must be positive.';
            END IF;
            running:=0; line_index:=0;
            FOR line_row IN SELECT * FROM advance.vendor_bill_lines
              WHERE "CompanyId"=NEW."CompanyId" AND "VendorBillId"=NEW."Id"
              ORDER BY "LineNumber"
            LOOP
              line_index:=line_index+1;
              allocation:=CASE WHEN line_index=line_count
                THEN charge_row."ChargeValue"-running
                ELSE round(charge_row."ChargeValue"*
                  (CASE WHEN charge_row."AllocationBasis"='GROSS_WEIGHT'
                    THEN line_row."VerifiedGrossWeightKg" ELSE line_row."BilledPayableValue" END)
                  /total_basis,2) END;
              INSERT INTO advance.vendor_bill_charge_allocations
                ("Id","CompanyId","VendorBillChargeId","VendorBillLineId","BasisValue",
                 "AllocatedChargeValue","CreatedAt","CreatedBy")
              VALUES(gen_random_uuid(),NEW."CompanyId",charge_row."Id",line_row."Id",
                CASE WHEN charge_row."AllocationBasis"='GROSS_WEIGHT'
                  THEN line_row."VerifiedGrossWeightKg" ELSE line_row."BilledPayableValue" END,
                allocation,clock_timestamp(),coalesce(NEW."UpdatedBy",NEW."CreatedBy"));
              running:=running+allocation;
            END LOOP;
            IF running<>charge_row."ChargeValue" THEN
              RAISE EXCEPTION 'Landed-cost allocations must exactly equal their charge.';
            END IF;
          END LOOP;

          FOR line_row IN
            SELECT l.*,f."Id" layer_id,f."UnitCost",f."QuantityReceived"
            FROM advance.vendor_bill_lines l
            JOIN advance.fifo_inventory_cost_layers f
              ON f."CompanyId"=l."CompanyId" AND f."GoodsReceiptLineId"=l."GoodsReceiptLineId"
            WHERE l."CompanyId"=NEW."CompanyId" AND l."VendorBillId"=NEW."Id"
            ORDER BY l."LineNumber"
          LOOP
            SELECT coalesce(sum("AllocatedChargeValue"),0) INTO line_charge
            FROM advance.vendor_bill_charge_allocations
            WHERE "CompanyId"=NEW."CompanyId" AND "VendorBillLineId"=line_row."Id";
            SELECT coalesce(sum("Quantity"),0) INTO consumed
            FROM advance.fifo_cost_consumptions
            WHERE "CompanyId"=NEW."CompanyId"
              AND "FifoInventoryCostLayerId"=line_row.layer_id;
            remaining:=line_row."QuantityReceived"-consumed;
            consumed_adjustment:=CASE WHEN consumed=0 THEN 0
              WHEN remaining=0 THEN line_charge
              ELSE round(line_charge*consumed/line_row."QuantityReceived",2) END;
            INSERT INTO advance.fifo_landed_cost_adjustments
              ("Id","CompanyId","VendorBillLineId","FifoInventoryCostLayerId",
               "ProvisionalUnitRate","LandedUnitRate","AllocatedChargeValue",
               "ConsumedQuantityAtAcceptance","RemainingQuantityAtAcceptance",
               "ConsumedCostAdjustmentValue","RemainingStockAdjustmentValue","CreatedAt","CreatedBy")
            VALUES(gen_random_uuid(),NEW."CompanyId",line_row."Id",line_row.layer_id,
              line_row."UnitCost",round(line_row."UnitCost"+line_charge/line_row."QuantityReceived",6),
              line_charge,consumed,remaining,consumed_adjustment,line_charge-consumed_adjustment,
              clock_timestamp(),coalesce(NEW."UpdatedBy",NEW."CreatedBy"));

            INSERT INTO advance.actual_bom_valuation_adjustments
              ("Id","CompanyId","ActualBomEntryId","VendorBillLineId","AcceptedMaterialValue",
               "AllocatedChargeValue","TotalAcceptedValue","CreatedAt","CreatedBy")
            SELECT gen_random_uuid(),NEW."CompanyId",e."Id",line_row."Id",
              round(line_row."BilledPayableValue"/line_row."BilledQuantity"*e."QuantityBase",6),
              round(line_charge/line_row."BilledQuantity"*e."QuantityBase",6),
              round((line_row."BilledPayableValue"+line_charge)/
                line_row."BilledQuantity"*e."QuantityBase",6),
              clock_timestamp(),coalesce(NEW."UpdatedBy",NEW."CreatedBy")
            FROM advance.actual_bom_entries e
            LEFT JOIN advance.component_fitment_reversals r
              ON r."CompanyId"=e."CompanyId" AND r."ComponentFitmentId"=e."ComponentFitmentId"
            WHERE e."CompanyId"=NEW."CompanyId"
              AND e."GoodsReceiptLineId"=line_row."GoodsReceiptLineId"
              AND e."EntryKind"='FITMENT' AND e."VendorBillLineId" IS NULL AND r."Id" IS NULL;
          END LOOP;
          RETURN NEW;
        END $function$;

        CREATE TRIGGER trg_aaa_vendor_bill_landed_cost
          AFTER UPDATE OF "Status" ON advance.vendor_bills
          FOR EACH ROW EXECUTE FUNCTION advance.allocate_vendor_bill_landed_cost();

        CREATE OR REPLACE FUNCTION advance.refresh_item_company_last_purchase(
          p_company uuid,p_item uuid,p_login text)
        RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
        DECLARE source_bill uuid; source_date date; source_rate numeric(24,6);
        BEGIN
          SELECT b."Id",b."BillDate",
            round((sum(l."BilledPayableValue")+coalesce(sum(la.charge_value),0))
              /sum(l."BilledQuantity"),6)
            INTO source_bill,source_date,source_rate
          FROM advance.vendor_bills b
          JOIN advance.vendor_bill_lines l ON l."CompanyId"=b."CompanyId"
            AND l."VendorBillId"=b."Id" AND l."ItemId"=p_item
          LEFT JOIN LATERAL (
            SELECT sum(a."AllocatedChargeValue") charge_value
            FROM advance.vendor_bill_charge_allocations a
            WHERE a."CompanyId"=l."CompanyId" AND a."VendorBillLineId"=l."Id"
          ) la ON true
          WHERE b."CompanyId"=p_company AND b."Status"='ACCEPTED'
          GROUP BY b."Id",b."BillDate",b."DecidedAt"
          ORDER BY b."DecidedAt" DESC,b."Id" DESC LIMIT 1;
          IF FOUND THEN
            INSERT INTO advance.item_company_last_purchases
              ("Id","CompanyId","ItemId","LastPurchaseRate","LastPurchaseDate",
               "LastPurchaseBillId","CreatedAt","CreatedBy","Version")
            VALUES(gen_random_uuid(),p_company,p_item,source_rate,source_date,source_bill,
              clock_timestamp(),p_login,0)
            ON CONFLICT ("CompanyId","ItemId") DO UPDATE SET
              "LastPurchaseRate"=excluded."LastPurchaseRate",
              "LastPurchaseDate"=excluded."LastPurchaseDate",
              "LastPurchaseBillId"=excluded."LastPurchaseBillId",
              "UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,
              "Version"=advance.item_company_last_purchases."Version"+1;
          ELSE
            UPDATE advance.item_company_last_purchases SET "LastPurchaseRate"=NULL,
              "LastPurchaseDate"=NULL,"LastPurchaseBillId"=NULL,
              "UpdatedAt"=clock_timestamp(),"UpdatedBy"=p_login,"Version"="Version"+1
            WHERE "CompanyId"=p_company AND "ItemId"=p_item;
          END IF;
        END $function$;

        CREATE FUNCTION advance.get_actual_bom_landed_valuations(p_company uuid,p_actual_bom uuid)
        RETURNS jsonb LANGUAGE plpgsql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $function$
        DECLARE result jsonb;
        BEGIN
          IF session_user<>'nexa_erp_runtime' THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Actual BOM landed valuation projection requires the runtime principal.';
          END IF;
          SELECT coalesce(jsonb_agg(jsonb_build_object(
            'actualBomEntryId',a."ActualBomEntryId",
            'vendorBillLineId',a."VendorBillLineId",
            'billNumber',b."BillNumber",
            'acceptedMaterialValue',a."AcceptedMaterialValue",
            'allocatedChargeValue',a."AllocatedChargeValue",
            'totalAcceptedValue',a."TotalAcceptedValue",
            'createdAt',a."CreatedAt") ORDER BY a."CreatedAt",a."Id"),'[]'::jsonb)
            INTO result
          FROM advance.actual_bom_valuation_adjustments a
          JOIN advance.actual_bom_entries e
            ON e."CompanyId"=a."CompanyId" AND e."Id"=a."ActualBomEntryId"
          JOIN advance.vendor_bill_lines l
            ON l."CompanyId"=a."CompanyId" AND l."Id"=a."VendorBillLineId"
          JOIN advance.vendor_bills b
            ON b."CompanyId"=l."CompanyId" AND b."Id"=l."VendorBillId"
          WHERE a."CompanyId"=p_company AND e."ActualBomId"=p_actual_bom
            AND b."Status"='ACCEPTED';
          RETURN result;
        END $function$;
        CREATE OR REPLACE FUNCTION advance.vendor_bill_json(p_company uuid,p_bill uuid)
        RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER
        SET search_path=pg_catalog,advance AS $function$
          SELECT jsonb_build_object(
            'id',b."Id",'billNumber',b."BillNumber",'billDate',b."BillDate",
            'goodsReceiptId',b."GoodsReceiptId",'purchaseOrderId',b."PurchaseOrderId",
            'vendorId',b."VendorId",'status',b."Status",'matchStatus',b."MatchStatus",
            'totalPayableValue',b."TotalPayableValue",
            'totalChargeValue',b."TotalChargeValue",'totalLandedValue',b."TotalLandedValue",
            'version',b."Version",'replayed',false,'actorRoleCode',b."ActorRoleCode",
            'resolvedRoleAssignmentId',b."ResolvedRoleAssignmentId",
            'resolvedRoleAssignmentType',b."ResolvedRoleAssignmentType",
            'decidedAt',b."DecidedAt",'decidedByEmployeeId',b."DecidedByEmployeeId",
            'decisionReason',b."DecisionReason",'reversedAt',b."ReversedAt",
            'reversedByEmployeeId',b."ReversedByEmployeeId",'reversalReason',b."ReversalReason",
            'lines',coalesce((
              SELECT jsonb_agg(jsonb_build_object(
                'id',l."Id",'lineNumber',l."LineNumber",
                'goodsReceiptLineId',l."GoodsReceiptLineId",
                'purchaseOrderLineId',l."PurchaseOrderLineId",'itemId',l."ItemId",
                'billedQuantity',l."BilledQuantity",'expectedUnitRate',l."ExpectedUnitRate",
                'billedUnitRate',l."BilledUnitRate",
                'expectedPayableValue',l."ExpectedPayableValue",
                'billedPayableValue',l."BilledPayableValue",'matchStatus',l."MatchStatus",
                'verifiedGrossWeightKg',l."VerifiedGrossWeightKg",
                'allocatedChargeValue',coalesce((
                  SELECT sum(a."AllocatedChargeValue")
                  FROM advance.vendor_bill_charge_allocations a
                  WHERE a."CompanyId"=p_company AND a."VendorBillLineId"=l."Id"),0),
                'landedUnitRate',coalesce((
                  SELECT f."LandedUnitRate" FROM advance.fifo_landed_cost_adjustments f
                  WHERE f."CompanyId"=p_company AND f."VendorBillLineId"=l."Id"
                  ORDER BY f."CreatedAt" DESC LIMIT 1),l."BilledUnitRate"))
                ORDER BY l."LineNumber")
              FROM advance.vendor_bill_lines l
              WHERE l."CompanyId"=p_company AND l."VendorBillId"=b."Id"),'[]'::jsonb),
            'charges',coalesce((
              SELECT jsonb_agg(jsonb_build_object(
                'id',c."Id",'chargeNumber',c."ChargeNumber",'chargeType',c."ChargeType",
                'chargeValue',c."ChargeValue",'isRecoverableTax',c."IsRecoverableTax",
                'includedInInventoryCost',c."IncludedInInventoryCost",
                'allocationBasis',c."AllocationBasis") ORDER BY c."ChargeNumber")
              FROM advance.vendor_bill_charges c
              WHERE c."CompanyId"=p_company AND c."VendorBillId"=b."Id"),'[]'::jsonb))
          FROM advance.vendor_bills b
          WHERE b."CompanyId"=p_company AND b."Id"=p_bill;
        $function$;
        CREATE OR REPLACE FUNCTION advance.guard_vendor_bill_financial_evidence()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Vendor Bill and FIFO evidence may mutate only through its controlled function.';
          END IF;
          IF TG_OP='DELETE' THEN
            RAISE EXCEPTION 'Vendor Bill and FIFO costing evidence is immutable.';
          END IF;
          IF TG_TABLE_NAME='vendor_bills' AND TG_OP='UPDATE' THEN
            IF OLD."Status"='DRAFT' THEN NULL;
            ELSIF OLD."Status"='ACCEPTED' AND NEW."Status"='REVERSED' THEN
              IF (to_jsonb(NEW)-ARRAY['Status','ReversedAt','ReversedByEmployeeId',
                  'ReversalActorRoleCode','ReversalRoleAssignmentId',
                  'ReversalRoleAssignmentType','ReversalReason','ReversalIdempotencyKey',
                  'ReversalRequestFingerprint','UpdatedAt','UpdatedBy','Version'])
                 IS DISTINCT FROM
                 (to_jsonb(OLD)-ARRAY['Status','ReversedAt','ReversedByEmployeeId',
                  'ReversalActorRoleCode','ReversalRoleAssignmentId',
                  'ReversalRoleAssignmentType','ReversalReason','ReversalIdempotencyKey',
                  'ReversalRequestFingerprint','UpdatedAt','UpdatedBy','Version']) THEN
                RAISE EXCEPTION 'Vendor Bill reversal may not rewrite accepted content or decision evidence.';
              END IF;
            ELSE
              RAISE EXCEPTION 'Accepted, rejected and reversed Vendor Bill content is immutable.';
            END IF;
          ELSIF TG_TABLE_NAME='vendor_bill_lines' AND TG_OP='UPDATE'
            AND current_setting('sess.landed_cost_write',true)=txid_current()::text THEN
            IF (to_jsonb(NEW)-'VerifiedGrossWeightKg')
               IS DISTINCT FROM (to_jsonb(OLD)-'VerifiedGrossWeightKg')
               OR OLD."VerifiedGrossWeightKg" IS NOT NULL THEN
              RAISE EXCEPTION 'Vendor Bill line weight evidence is immutable after initial capture.';
            END IF;
          ELSIF TG_TABLE_NAME<>'vendor_bills' AND TG_OP<>'INSERT' THEN
            RAISE EXCEPTION 'Vendor Bill and FIFO costing evidence is immutable.';
          END IF;
          RETURN CASE WHEN TG_OP='DELETE' THEN OLD ELSE NEW END;
        END $function$;
        CREATE FUNCTION advance.guard_landed_cost_evidence()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
        BEGIN
          IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN
            RAISE EXCEPTION USING ERRCODE='42501',
              MESSAGE='Landed-cost evidence may mutate only through controlled Vendor Bill functions.';
          END IF;
          IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Landed-cost evidence is immutable.'; END IF;
          RETURN NEW;
        END $function$;
        CREATE TRIGGER trg_vendor_bill_charge_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.vendor_bill_charges FOR EACH ROW EXECUTE FUNCTION advance.guard_landed_cost_evidence();
        CREATE TRIGGER trg_vendor_bill_charge_allocation_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.vendor_bill_charge_allocations FOR EACH ROW EXECUTE FUNCTION advance.guard_landed_cost_evidence();
        CREATE TRIGGER trg_fifo_landed_cost_adjustment_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.fifo_landed_cost_adjustments FOR EACH ROW EXECUTE FUNCTION advance.guard_landed_cost_evidence();
        CREATE TRIGGER trg_actual_bom_valuation_adjustment_guard BEFORE INSERT OR UPDATE OR DELETE
          ON advance.actual_bom_valuation_adjustments FOR EACH ROW EXECUTE FUNCTION advance.guard_landed_cost_evidence();

        REVOKE ALL ON TABLE advance.vendor_bill_charges,advance.vendor_bill_charge_allocations,
          advance.fifo_landed_cost_adjustments,advance.actual_bom_valuation_adjustments FROM PUBLIC;
        REVOKE ALL ON FUNCTION
          advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text),
          advance.allocate_vendor_bill_landed_cost(),advance.guard_landed_cost_evidence(),
          advance.get_actual_bom_landed_valuations(uuid,uuid) FROM PUBLIC;
        DO $roles$ BEGIN
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
            ALTER TABLE advance.vendor_bill_charges OWNER TO nexa_erp_owner;
            ALTER TABLE advance.vendor_bill_charge_allocations OWNER TO nexa_erp_owner;
            ALTER TABLE advance.fifo_landed_cost_adjustments OWNER TO nexa_erp_owner;
            ALTER TABLE advance.actual_bom_valuation_adjustments OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.record_vendor_bill_charges(
              uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.allocate_vendor_bill_landed_cost() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.guard_landed_cost_evidence() OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.get_actual_bom_landed_valuations(uuid,uuid) OWNER TO nexa_erp_owner;
            ALTER FUNCTION advance.refresh_item_company_last_purchase(uuid,uuid,text)
              OWNER TO nexa_erp_owner;
          END IF;
          IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
            REVOKE ALL ON advance.vendor_bill_charges,advance.vendor_bill_charge_allocations,
              advance.fifo_landed_cost_adjustments,advance.actual_bom_valuation_adjustments
              FROM nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.record_vendor_bill_charges(
              uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text) TO nexa_erp_runtime;
            GRANT EXECUTE ON FUNCTION advance.get_actual_bom_landed_valuations(uuid,uuid) TO nexa_erp_runtime;
          END IF;
        END $roles$;
        """;

    internal const string Down = """
        DROP TRIGGER IF EXISTS trg_aaa_vendor_bill_landed_cost ON advance.vendor_bills;
        DROP TRIGGER IF EXISTS trg_actual_bom_valuation_adjustment_guard
          ON advance.actual_bom_valuation_adjustments;
        DROP TRIGGER IF EXISTS trg_fifo_landed_cost_adjustment_guard
          ON advance.fifo_landed_cost_adjustments;
        DROP TRIGGER IF EXISTS trg_vendor_bill_charge_allocation_guard
          ON advance.vendor_bill_charge_allocations;
        DROP TRIGGER IF EXISTS trg_vendor_bill_charge_guard ON advance.vendor_bill_charges;
        DROP FUNCTION IF EXISTS advance.guard_landed_cost_evidence();
        DROP FUNCTION IF EXISTS advance.allocate_vendor_bill_landed_cost();
        DROP FUNCTION IF EXISTS advance.get_actual_bom_landed_valuations(uuid,uuid);
        DROP FUNCTION IF EXISTS advance.record_vendor_bill_charges(
          uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text);
        """;

    internal static string RestoreLastPurchasePriceFunction
    {
        get
        {
            const string startToken = "CREATE FUNCTION advance.refresh_item_company_last_purchase";
            const string endToken = "CREATE FUNCTION advance.maintain_item_company_last_purchase";
            var start = ItemCompanyLastPurchasePricingSql.Up.IndexOf(startToken, StringComparison.Ordinal);
            var end = ItemCompanyLastPurchasePricingSql.Up.IndexOf(endToken, start, StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new InvalidOperationException("Last-purchase SQL markers changed.");
            return ItemCompanyLastPurchasePricingSql.Up[start..end].Replace(
                "CREATE FUNCTION advance.refresh_item_company_last_purchase",
                "CREATE OR REPLACE FUNCTION advance.refresh_item_company_last_purchase",
                StringComparison.Ordinal);
        }
    }}