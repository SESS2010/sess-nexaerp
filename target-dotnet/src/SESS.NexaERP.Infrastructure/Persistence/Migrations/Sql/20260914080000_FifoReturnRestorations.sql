CREATE TABLE advance.fifo_consumption_creation_order (
  "FifoCostConsumptionId" uuid PRIMARY KEY REFERENCES advance.fifo_cost_consumptions("Id"),
  "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
  "CreationOrdinal" bigint GENERATED ALWAYS AS IDENTITY UNIQUE,
  "RecordedAt" timestamptz NOT NULL,
  "ReconstructedFromOriginalWriter" boolean NOT NULL
);
CREATE TABLE advance.fifo_cost_restorations (
  "Id" uuid PRIMARY KEY,
  "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
  "FifoCostConsumptionId" uuid NOT NULL REFERENCES advance.fifo_cost_consumptions("Id"),
  "MaterialReturnLineId" uuid NOT NULL REFERENCES advance.material_return_lines("Id"),
  "Quantity" numeric(24,6) NOT NULL CHECK ("Quantity">0),
  "UnitCost" numeric(24,6) NOT NULL CHECK ("UnitCost">=0),
  "RestoredValue" numeric(24,6) NOT NULL CHECK ("RestoredValue">=0),
  "EffectiveAt" timestamptz NOT NULL,
  "RecordedAt" timestamptz NOT NULL,
  "AcceptedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
  "RecordedBy" text NOT NULL CHECK (length(btrim("RecordedBy"))>0),
  "IsHistoricalReconciliation" boolean NOT NULL,
  UNIQUE ("CompanyId","MaterialReturnLineId","FifoCostConsumptionId")
);
CREATE INDEX ON advance.fifo_cost_restorations ("CompanyId","FifoCostConsumptionId","EffectiveAt");

-- Only valid after an exact baseline check of the old writer. Within each issue
-- line it created consumption in layer ReceivedAt/Id order, even if timestamps tie.
INSERT INTO advance.fifo_consumption_creation_order
  ("FifoCostConsumptionId","CompanyId","RecordedAt","ReconstructedFromOriginalWriter")
SELECT c."Id",c."CompanyId",clock_timestamp(),true
FROM advance.fifo_cost_consumptions c
JOIN advance.fifo_inventory_cost_layers f ON f."Id"=c."FifoInventoryCostLayerId"
JOIN advance.material_issue_lines il ON il."Id"=c."MaterialIssueLineId"
ORDER BY c."CompanyId",il."MaterialIssueId",il."LineNumber",f."ReceivedAt",f."Id";

CREATE FUNCTION advance.record_fifo_consumption_creation_order()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
BEGIN
  IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FIFO creation order requires controlled consumption.';
  END IF;
  INSERT INTO advance.fifo_consumption_creation_order
    ("FifoCostConsumptionId","CompanyId","RecordedAt","ReconstructedFromOriginalWriter")
  VALUES(NEW."Id",NEW."CompanyId",clock_timestamp(),false);
  RETURN NEW;
END $function$;
CREATE TRIGGER trg_fifo_consumption_creation_order AFTER INSERT
  ON advance.fifo_cost_consumptions FOR EACH ROW
  EXECUTE FUNCTION advance.record_fifo_consumption_creation_order();

CREATE FUNCTION advance.restore_fifo_for_material_return(
  p_company uuid,p_return uuid,p_historical_reconciliation boolean)
RETURNS integer LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
DECLARE return_row advance.material_returns%ROWTYPE; return_line record;
  consumed record; already_quantity numeric; already_value numeric;
  available_quantity numeric; needed numeric; take_quantity numeric;
  restored_value numeric; affected integer:=0; lock_item uuid;
BEGIN
  SELECT * INTO return_row FROM advance.material_returns
    WHERE "CompanyId"=p_company AND "Id"=p_return FOR UPDATE;
  IF NOT FOUND OR return_row."Status"<>'ACCEPTED' THEN
    RAISE EXCEPTION 'FIFO restoration requires an accepted material return.';
  END IF;
  PERFORM 1 FROM advance.material_issues
    WHERE "CompanyId"=p_company AND "Id"=return_row."MaterialIssueId" FOR UPDATE;
  IF NOT FOUND THEN RAISE EXCEPTION 'FIFO restoration issue is absent in this company.'; END IF;
  IF (SELECT count(*) FROM advance.stock_posting_batches
      WHERE "CompanyId"=p_company AND "MaterialReturnId"=p_return AND "PostingKind"='MATERIAL_RETURN')<>1 THEN
    RAISE EXCEPTION 'FIFO restoration requires the atomic physical return batch.';
  END IF;
  PERFORM set_config('sess.fifo_restore_write',txid_current()::text,true);
  FOR lock_item IN SELECT DISTINCT "ItemId" FROM advance.material_return_lines
    WHERE "CompanyId"=p_company AND "MaterialReturnId"=p_return ORDER BY "ItemId"
  LOOP
    PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||p_company||':'||lock_item,0));
  END LOOP;
  FOR return_line IN
    SELECT rl.*,il."MaterialIssueId" AS issue_id
    FROM advance.material_return_lines rl
    JOIN advance.material_issue_lines il ON il."CompanyId"=rl."CompanyId" AND il."Id"=rl."MaterialIssueLineId"
    WHERE rl."CompanyId"=p_company AND rl."MaterialReturnId"=p_return
    ORDER BY rl."LineNumber"
  LOOP
    IF return_line.issue_id<>return_row."MaterialIssueId" THEN
      RAISE EXCEPTION 'FIFO restoration cannot cross issues.';
    END IF;
    PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||p_company||':'||return_line."ItemId",0));
    SELECT coalesce(sum("Quantity"),0) INTO already_quantity FROM advance.fifo_cost_restorations
      WHERE "CompanyId"=p_company AND "MaterialReturnLineId"=return_line."Id";
    IF already_quantity=return_line."ReturnedQuantityBase" THEN CONTINUE; END IF;
    IF already_quantity<>0 THEN
      RAISE EXCEPTION 'FIFO restoration refuses incomplete previously recorded return evidence.';
    END IF;
    needed:=return_line."ReturnedQuantityBase";
    FOR consumed IN
      SELECT c.*,o."CreationOrdinal" FROM advance.fifo_cost_consumptions c
      JOIN advance.fifo_consumption_creation_order o
        ON o."FifoCostConsumptionId"=c."Id" AND o."CompanyId"=c."CompanyId"
      WHERE c."CompanyId"=p_company AND c."MaterialIssueLineId"=return_line."MaterialIssueLineId"
      ORDER BY o."CreationOrdinal" DESC FOR UPDATE OF c
    LOOP
      EXIT WHEN needed=0;
      SELECT coalesce(sum("Quantity"),0),coalesce(sum("RestoredValue"),0)
        INTO already_quantity,already_value FROM advance.fifo_cost_restorations
        WHERE "CompanyId"=p_company AND "FifoCostConsumptionId"=consumed."Id";
      available_quantity:=consumed."Quantity"-already_quantity;
      IF available_quantity<0 THEN RAISE EXCEPTION 'FIFO consumption is already over-restored.'; END IF;
      IF available_quantity=0 THEN CONTINUE; END IF;
      take_quantity:=least(needed,available_quantity);
      restored_value:=CASE WHEN take_quantity=available_quantity
        THEN consumed."ConsumedValue"-already_value
        ELSE round((already_quantity+take_quantity)*consumed."UnitCost",6)-already_value END;
      INSERT INTO advance.fifo_cost_restorations
        ("Id","CompanyId","FifoCostConsumptionId","MaterialReturnLineId","Quantity","UnitCost",
         "RestoredValue","EffectiveAt","RecordedAt","AcceptedByEmployeeId","RecordedBy","IsHistoricalReconciliation")
      VALUES(gen_random_uuid(),p_company,consumed."Id",return_line."Id",take_quantity,consumed."UnitCost",
        restored_value,return_row."AcceptedAt",clock_timestamp(),return_row."AcceptedByEmployeeId",
        CASE WHEN p_historical_reconciliation THEN 'FIFO_RETURN_RECONCILIATION'
          ELSE coalesce(return_row."UpdatedBy",return_row."CreatedBy") END,p_historical_reconciliation);
      needed:=needed-take_quantity; affected:=affected+1;
    END LOOP;
    IF needed<>0 THEN
      RAISE EXCEPTION 'Material return exceeds the original issue quantity remaining unreturned.';
    END IF;
  END LOOP;
  RETURN affected;
END $function$;
REVOKE ALL ON FUNCTION advance.restore_fifo_for_material_return(uuid,uuid,boolean),
  advance.record_fifo_consumption_creation_order() FROM PUBLIC;

CREATE FUNCTION advance.guard_fifo_restoration_evidence()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $function$
DECLARE consumed advance.fifo_cost_consumptions%ROWTYPE;
  returned record; prior_quantity numeric; prior_value numeric; expected_value numeric;
BEGIN
  IF TG_OP<>'INSERT' THEN
    RAISE EXCEPTION 'FIFO consumption order and restoration evidence are immutable.';
  END IF;
  IF TG_TABLE_NAME='fifo_consumption_creation_order' THEN
    IF current_setting('sess.vendor_bill_write',true) IS DISTINCT FROM txid_current()::text
      OR NOT EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions
        WHERE "Id"=NEW."FifoCostConsumptionId" AND "CompanyId"=NEW."CompanyId")
      OR NEW."ReconstructedFromOriginalWriter" THEN
      RAISE EXCEPTION 'New consumption order requires the controlled original consumption.';
    END IF;
    RETURN NEW;
  END IF;
  IF current_setting('sess.fifo_restore_write',true) IS DISTINCT FROM txid_current()::text THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FIFO restoration requires controlled accepted-return posting.';
  END IF;
  SELECT rl."MaterialIssueLineId",rl."ReturnedQuantityBase",r."AcceptedAt",r."AcceptedByEmployeeId"
    INTO returned
    FROM advance.material_return_lines rl
    JOIN advance.material_returns r ON r."Id"=rl."MaterialReturnId" AND r."CompanyId"=rl."CompanyId"
    WHERE rl."Id"=NEW."MaterialReturnLineId" AND rl."CompanyId"=NEW."CompanyId" AND r."Status"='ACCEPTED'
    FOR UPDATE OF r;
  IF NOT FOUND THEN RAISE EXCEPTION 'FIFO restoration requires the accepted return in this company.'; END IF;
  SELECT * INTO consumed FROM advance.fifo_cost_consumptions
    WHERE "Id"=NEW."FifoCostConsumptionId" AND "CompanyId"=NEW."CompanyId" FOR UPDATE;
  IF NOT FOUND OR consumed."MaterialIssueLineId"<>returned."MaterialIssueLineId"
    OR NEW."UnitCost"<>consumed."UnitCost" OR NEW."EffectiveAt"<>returned."AcceptedAt"
    OR NEW."AcceptedByEmployeeId"<>returned."AcceptedByEmployeeId" THEN
    RAISE EXCEPTION 'FIFO restoration must reference its own issue consumption, recorded rate and return authority.';
  END IF;
  SELECT coalesce(sum("Quantity"),0),coalesce(sum("RestoredValue"),0)
    INTO prior_quantity,prior_value FROM advance.fifo_cost_restorations
    WHERE "CompanyId"=NEW."CompanyId" AND "FifoCostConsumptionId"=NEW."FifoCostConsumptionId";
  IF prior_quantity>=consumed."Quantity" THEN
    RAISE EXCEPTION 'FIFO consumption has already been fully restored.';
  END IF;
  IF NEW."Quantity"+prior_quantity>consumed."Quantity" THEN
    RAISE EXCEPTION 'FIFO restoration exceeds the unreturned consumption quantity.';
  END IF;
  IF NEW."Quantity"+coalesce((SELECT sum("Quantity") FROM advance.fifo_cost_restorations
      WHERE "CompanyId"=NEW."CompanyId" AND "MaterialReturnLineId"=NEW."MaterialReturnLineId"),0)
      >returned."ReturnedQuantityBase" THEN
    RAISE EXCEPTION 'FIFO restoration exceeds the accepted return line quantity.';
  END IF;
  expected_value:=CASE WHEN NEW."Quantity"+prior_quantity=consumed."Quantity"
    THEN consumed."ConsumedValue"-prior_value
    ELSE round((prior_quantity+NEW."Quantity")*consumed."UnitCost",6)-prior_value END;
  IF NEW."RestoredValue"<>expected_value THEN
    RAISE EXCEPTION 'FIFO restoration value must unwind the recorded consumption without rounding drift.';
  END IF;
  RETURN NEW;
END $function$;
CREATE TRIGGER trg_fifo_restoration_immutable BEFORE INSERT OR UPDATE OR DELETE
  ON advance.fifo_cost_restorations FOR EACH ROW EXECUTE FUNCTION advance.guard_fifo_restoration_evidence();
CREATE TRIGGER trg_fifo_creation_order_immutable BEFORE INSERT OR UPDATE OR DELETE
  ON advance.fifo_consumption_creation_order FOR EACH ROW EXECUTE FUNCTION advance.guard_fifo_restoration_evidence();
REVOKE ALL ON FUNCTION advance.guard_fifo_restoration_evidence() FROM PUBLIC;

CREATE OR REPLACE FUNCTION advance.consume_fifo_for_issue(p_company uuid,p_issue uuid,p_actor uuid,p_role text,p_assignment uuid,p_assignment_type text,p_login text)
RETURNS integer LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
DECLARE issue_line record; layer record; remaining numeric; take_quantity numeric;
  affected integer:=0; pool_currency text; lock_item uuid;
BEGIN
  PERFORM set_config('sess.vendor_bill_write',txid_current()::text,true);
  IF NOT advance.ordinary_command_context_valid((SELECT "Code" FROM advance.companies WHERE "Id"=p_company),
      p_actor,current_setting('advance.ordinary_identity_issuer',true),p_login,p_role)
    OR NOT EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,'create',ARRAY[p_role]) r
      WHERE r."AssignmentId"=p_assignment AND r."AssignmentType"=p_assignment_type) THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='FIFO consumption requires the current company-scoped ordinary Issue command.';
  END IF;
  FOR lock_item IN SELECT DISTINCT "ItemId" FROM advance.material_issue_lines
    WHERE "CompanyId"=p_company AND "MaterialIssueId"=p_issue ORDER BY "ItemId"
  LOOP
    PERFORM pg_advisory_xact_lock(hashtextextended('FIFO:'||p_company||':'||lock_item,0));
  END LOOP;
  FOR issue_line IN SELECT * FROM advance.material_issue_lines
    WHERE "CompanyId"=p_company AND "MaterialIssueId"=p_issue ORDER BY "LineNumber"
  LOOP
    IF EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions WHERE "CompanyId"=p_company
      AND "MaterialIssueLineId"=issue_line."Id") THEN CONTINUE; END IF;
    SELECT coalesce(po."CurrencyCode",o."CurrencyCode") INTO pool_currency
      FROM advance.inventory_ownership_accounts o
      LEFT JOIN advance.goods_receipt_lines gl ON gl."Id"=issue_line."OriginGoodsReceiptLineId" AND gl."CompanyId"=p_company
      LEFT JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=p_company
      LEFT JOIN advance.purchase_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=p_company
      WHERE o."CompanyId"=p_company AND o."Id"=issue_line."OwnershipAccountId";
    IF nullif(pool_currency,'') IS NULL THEN RAISE EXCEPTION 'FIFO issue requires an identified ownership and currency pool.'; END IF;
    remaining:=issue_line."QuantityBase";
    FOR layer IN
      SELECT f.*,f."QuantityReceived"-coalesce((SELECT sum(c."Quantity"-coalesce(
        (SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r
          WHERE r."CompanyId"=p_company AND r."FifoCostConsumptionId"=c."Id"),0))
        FROM advance.fifo_cost_consumptions c WHERE c."CompanyId"=p_company AND c."FifoInventoryCostLayerId"=f."Id"),0) AS available
      FROM advance.fifo_inventory_cost_layers f
      LEFT JOIN advance.goods_receipt_lines gl ON gl."Id"=f."GoodsReceiptLineId" AND gl."CompanyId"=p_company
      LEFT JOIN advance.goods_receipts g ON g."Id"=gl."GoodsReceiptId" AND g."CompanyId"=p_company
      LEFT JOIN advance.purchase_orders po ON po."Id"=g."PurchaseOrderId" AND po."CompanyId"=p_company
      JOIN advance.inventory_ownership_accounts o ON o."CompanyId"=p_company AND o."Id"=issue_line."OwnershipAccountId"
      WHERE f."CompanyId"=p_company AND f."ItemId"=issue_line."ItemId"
        AND coalesce(po."CurrencyCode",o."CurrencyCode")=pool_currency
        AND (SELECT count(DISTINCT m."OwnershipAccountId")=1
          AND bool_and(m."OwnershipAccountId"=issue_line."OwnershipAccountId")
          FROM advance.stock_movements m WHERE m."CompanyId"=p_company AND m."MovementLeg"='RECEIPT_IN'
            AND ((f."GoodsReceiptLineId" IS NOT NULL AND m."GoodsReceiptLineId"=f."GoodsReceiptLineId")
              OR (f."OpeningStockLineId" IS NOT NULL AND m."OpeningStockLineId"=f."OpeningStockLineId")))
      ORDER BY f."ReceivedAt",f."Id" FOR UPDATE OF f
    LOOP
      EXIT WHEN remaining<=0;
      IF layer.available<0 THEN RAISE EXCEPTION 'FIFO cost layer has inconsistent net consumption.'; END IF;
      IF layer.available=0 THEN CONTINUE; END IF;
      take_quantity:=least(remaining,layer.available);
      INSERT INTO advance.fifo_cost_consumptions
        ("Id","CompanyId","FifoInventoryCostLayerId","MaterialIssueLineId","Quantity","UnitCost","ConsumedValue","ConsumedAt","CreatedBy")
      VALUES(gen_random_uuid(),p_company,layer."Id",issue_line."Id",take_quantity,layer."UnitCost",
        take_quantity*layer."UnitCost",clock_timestamp(),p_login);
      affected:=affected+1; remaining:=remaining-take_quantity;
    END LOOP;
    IF remaining>0 THEN RAISE EXCEPTION 'Insufficient FIFO cost-layer quantity in this ownership and currency pool.'; END IF;
  END LOOP;
  RETURN affected;
END $function$;

CREATE FUNCTION advance.guard_material_return_fifo_restoration()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $function$
DECLARE return_id uuid; company_id uuid;
BEGIN
  IF TG_TABLE_NAME='material_returns' THEN
    return_id:=NEW."Id"; company_id:=NEW."CompanyId";
  ELSE
    SELECT "MaterialReturnId","CompanyId" INTO return_id,company_id
      FROM advance.material_return_lines WHERE "Id"=NEW."MaterialReturnLineId";
  END IF;
  IF EXISTS(SELECT 1 FROM advance.material_returns WHERE "Id"=return_id AND "CompanyId"=company_id AND "Status"='ACCEPTED')
    AND EXISTS(SELECT 1 FROM advance.material_return_lines rl
      WHERE rl."MaterialReturnId"=return_id AND rl."CompanyId"=company_id
        AND rl."ReturnedQuantityBase"<>coalesce((SELECT sum(r."Quantity") FROM advance.fifo_cost_restorations r
          WHERE r."CompanyId"=company_id AND r."MaterialReturnLineId"=rl."Id"),0)) THEN
    RAISE EXCEPTION 'Every accepted material return must atomically restore its original FIFO consumption.';
  END IF;
  RETURN NULL;
END $function$;
CREATE CONSTRAINT TRIGGER trg_material_return_fifo_complete AFTER INSERT OR UPDATE ON advance.material_returns
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_fifo_restoration();
CREATE CONSTRAINT TRIGGER trg_fifo_restoration_return_complete AFTER INSERT ON advance.fifo_cost_restorations
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION advance.guard_material_return_fifo_restoration();
REVOKE ALL ON FUNCTION advance.guard_material_return_fifo_restoration() FROM PUBLIC;
