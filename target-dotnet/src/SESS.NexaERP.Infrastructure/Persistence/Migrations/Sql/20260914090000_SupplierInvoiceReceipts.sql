-- Supplier documents are immutable evidence, separate from accepted vendor bills.
CREATE TABLE advance.supplier_invoices(
 "Id" uuid PRIMARY KEY, "CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "PurchaseOrderId" uuid NOT NULL REFERENCES advance.purchase_orders("Id"),
 "RootPurchaseOrderId" uuid NOT NULL, -- stable revision-family identity, not a PO row foreign key
 "VendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),
 "InvoiceNumber" text NOT NULL CHECK(length(btrim("InvoiceNumber")) BETWEEN 1 AND 100),
 "InvoiceDate" date NOT NULL, "CurrencyCode" varchar(3) NOT NULL,
 "FileName" varchar(255) NOT NULL, "ContentType" varchar(100) NOT NULL,
 "Content" bytea NOT NULL CHECK(octet_length("Content") BETWEEN 1 AND 5242880),
 "ContentSha256" text GENERATED ALWAYS AS (encode(sha256("Content"),'hex')) STORED,
 "RecordedAt" timestamptz NOT NULL, "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "RecordedBy" text NOT NULL, "RoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 UNIQUE("CompanyId","Id"));
CREATE INDEX ON advance.supplier_invoices("CompanyId","VendorId",upper(btrim("InvoiceNumber")));
CREATE INDEX ON advance.supplier_invoices("CompanyId","RootPurchaseOrderId","RecordedAt");
CREATE TABLE advance.supplier_invoice_lines(
 "Id" uuid PRIMARY KEY, "CompanyId" uuid NOT NULL, "SupplierInvoiceId" uuid NOT NULL,
 "LineNumber" integer NOT NULL CHECK("LineNumber">0),
 "PurchaseOrderLineId" uuid NOT NULL REFERENCES advance.purchase_order_lines("Id"),
 "PurchaseRequirementHandoffId" uuid NOT NULL REFERENCES advance.purchase_requirement_handoffs("Id"),
 "ItemId" uuid NOT NULL REFERENCES advance.items("Id"), "ItemCode" text NOT NULL, "ItemName" text NOT NULL,
 "Uom" text NOT NULL, "Quantity" numeric(24,6) NOT NULL CHECK("Quantity">0),
 "UnitRate" numeric(24,6) NOT NULL CHECK("UnitRate">=0),
 "PayableValue" numeric(24,6) NOT NULL CHECK("PayableValue">=0),
 FOREIGN KEY("CompanyId","SupplierInvoiceId") REFERENCES advance.supplier_invoices("CompanyId","Id"),
 UNIQUE("CompanyId","Id"), UNIQUE("SupplierInvoiceId","LineNumber"),
 UNIQUE("SupplierInvoiceId","PurchaseOrderLineId"));
CREATE TABLE advance.supplier_invoice_cancellations(
 "Id" uuid PRIMARY KEY, "CompanyId" uuid NOT NULL, "SupplierInvoiceId" uuid NOT NULL UNIQUE,
 "Reason" text NOT NULL CHECK(length(btrim("Reason")) BETWEEN 1 AND 2000),
 "RecordedAt" timestamptz NOT NULL, "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 FOREIGN KEY("CompanyId","SupplierInvoiceId") REFERENCES advance.supplier_invoices("CompanyId","Id"));
CREATE TABLE advance.supplier_invoice_receipt_matches(
 "Id" uuid PRIMARY KEY, "CompanyId" uuid NOT NULL, "SupplierInvoiceLineId" uuid NOT NULL,
 "GoodsReceiptLineId" uuid NOT NULL REFERENCES advance.goods_receipt_lines("Id"),
 "ReceiptEventId" uuid NOT NULL REFERENCES advance.goods_receipts("Id"),
 "Quantity" numeric(24,6) NOT NULL CHECK("Quantity"<>0),
 "ReversesMatchId" uuid UNIQUE REFERENCES advance.supplier_invoice_receipt_matches("Id"),
 "EffectiveAt" timestamptz NOT NULL, "RecordedAt" timestamptz NOT NULL,
 "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 FOREIGN KEY("CompanyId","SupplierInvoiceLineId") REFERENCES advance.supplier_invoice_lines("CompanyId","Id"),
 CHECK(("Quantity">0 AND "ReversesMatchId" IS NULL) OR ("Quantity"<0 AND "ReversesMatchId" IS NOT NULL)));
CREATE UNIQUE INDEX ON advance.supplier_invoice_receipt_matches("SupplierInvoiceLineId","GoodsReceiptLineId") WHERE "Quantity">0;
CREATE INDEX ON advance.supplier_invoice_receipt_matches("CompanyId","SupplierInvoiceLineId","EffectiveAt");
CREATE INDEX ON advance.supplier_invoice_receipt_matches("CompanyId","GoodsReceiptLineId");
CREATE TABLE advance.supplier_invoice_bill_links(
 "Id" uuid PRIMARY KEY, "CompanyId" uuid NOT NULL, "SupplierInvoiceId" uuid NOT NULL,
 "VendorBillId" uuid NOT NULL REFERENCES advance.vendor_bills("Id"),
 "RecordedAt" timestamptz NOT NULL, "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 FOREIGN KEY("CompanyId","SupplierInvoiceId") REFERENCES advance.supplier_invoices("CompanyId","Id"),
 UNIQUE("CompanyId","VendorBillId"));

CREATE FUNCTION advance.guard_supplier_invoice_evidence()
RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $supplier_invoice$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION 'Supplier invoice evidence is immutable; append a correction.'; END IF;
 IF current_setting('sess.supplier_invoice_write',true) IS DISTINCT FROM txid_current()::text
  OR current_user IS DISTINCT FROM (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid=TG_RELID) THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Supplier invoice evidence requires a controlled command.';
 END IF;
 RETURN NEW;
END $supplier_invoice$;
DO $triggers$
DECLARE relation text;
BEGIN
 FOREACH relation IN ARRAY ARRAY['supplier_invoices','supplier_invoice_lines','supplier_invoice_cancellations',
   'supplier_invoice_receipt_matches','supplier_invoice_bill_links']
 LOOP
  EXECUTE format('CREATE TRIGGER %I BEFORE INSERT OR UPDATE OR DELETE ON advance.%I FOR EACH ROW EXECUTE FUNCTION advance.guard_supplier_invoice_evidence()','trg_'||relation,relation);
 END LOOP;
END $triggers$;

CREATE FUNCTION advance.supplier_invoice_command_valid(
 p_company uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_operation text,p_action text)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
 SELECT session_user='nexa_erp_runtime'
 AND p_role IN('ACCOUNTS_ASSISTANT','ACCOUNTS_MANAGER')
 AND (p_type IN('FULL','TEMPORARY') OR (p_type='SUPPORT' AND p_action='create'))
 AND (p_action='create' OR p_role='ACCOUNTS_MANAGER')
 AND EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company AND c."IsActive" AND c."Status"='ACTIVE'
  AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),
   current_setting('advance.ordinary_identity_subject',true),p_role))
 AND EXISTS(SELECT 1 FROM advance.command_requests r
  WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
   AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
 AND EXISTS(SELECT 1 FROM advance.resolve_employee_role_authority(p_actor,p_company,current_date,p_action,ARRAY[p_role]) a
   WHERE a."AssignmentId"=p_assignment AND a."AssignmentType"=p_type);
$supplier_invoice$;

CREATE FUNCTION advance.supplier_invoice_version(p_company uuid,p_invoice uuid)
RETURNS bigint LANGUAGE sql STABLE SET search_path=pg_catalog,advance AS $supplier_invoice$
 SELECT 1+(SELECT count(*) FROM advance.supplier_invoice_cancellations WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice)
  +(SELECT count(*) FROM advance.supplier_invoice_bill_links WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice)
  +(SELECT count(*) FROM advance.supplier_invoice_receipt_matches m JOIN advance.supplier_invoice_lines l
    ON l."CompanyId"=m."CompanyId" AND l."Id"=m."SupplierInvoiceLineId"
    WHERE l."CompanyId"=p_company AND l."SupplierInvoiceId"=p_invoice);
$supplier_invoice$;

-- Private helper: callers are the documentary command or the governed GRN trigger.
CREATE FUNCTION advance.reconcile_supplier_invoice_receipts(p_company uuid,p_invoice uuid,p_actor uuid)
RETURNS void LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $supplier_invoice$
DECLARE invoice advance.supplier_invoices%ROWTYPE; line record; receipt record; reversal record;
 remaining numeric; available numeric; take_quantity numeric;
BEGIN
 SELECT * INTO invoice FROM advance.supplier_invoices WHERE "CompanyId"=p_company AND "Id"=p_invoice;
 IF NOT FOUND THEN RAISE EXCEPTION 'Supplier invoice not found in this company.'; END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended('SUPPLIER-INVOICE:'||p_company||':'||invoice."RootPurchaseOrderId",0));
 IF EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice) THEN RETURN; END IF;
 PERFORM set_config('sess.supplier_invoice_write',txid_current()::text,true);
 FOR reversal IN
  SELECT m.*,r."Id" AS reversal_id,r."FinalizedAt" AS reversed_at
  FROM advance.supplier_invoice_receipt_matches m
  JOIN advance.supplier_invoice_lines l ON l."CompanyId"=p_company AND l."Id"=m."SupplierInvoiceLineId"
  JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=p_company AND gl."Id"=m."GoodsReceiptLineId"
  JOIN advance.goods_receipts r ON r."CompanyId"=p_company AND r."ReversesGoodsReceiptId"=gl."GoodsReceiptId"
   AND r."DocumentKind"='REVERSAL' AND r."Status"='FINALIZED'
  WHERE m."CompanyId"=p_company AND l."SupplierInvoiceId"=p_invoice AND m."Quantity">0
   AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_receipt_matches x WHERE x."ReversesMatchId"=m."Id")
  ORDER BY m."Id"
 LOOP
  INSERT INTO advance.supplier_invoice_receipt_matches VALUES(gen_random_uuid(),p_company,reversal."SupplierInvoiceLineId",
   reversal."GoodsReceiptLineId",reversal.reversal_id,-reversal."Quantity",reversal."Id",
   greatest(invoice."RecordedAt",reversal.reversed_at),clock_timestamp(),p_actor);
 END LOOP;
 FOR line IN SELECT * FROM advance.supplier_invoice_lines WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice ORDER BY "LineNumber"
 LOOP
  SELECT line."Quantity"-coalesce(sum("Quantity"),0) INTO remaining
   FROM advance.supplier_invoice_receipt_matches WHERE "CompanyId"=p_company AND "SupplierInvoiceLineId"=line."Id";
  FOR receipt IN
   SELECT gl.*,g."FinalizedAt" AS finalized_at,g."Id" AS receipt_id
   FROM advance.goods_receipts g
   JOIN advance.purchase_orders po ON po."CompanyId"=p_company AND po."Id"=g."PurchaseOrderId"
   JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=p_company AND gl."GoodsReceiptId"=g."Id"
   JOIN advance.purchase_order_lines pl ON pl."CompanyId"=p_company AND pl."Id"=gl."PurchaseOrderLineId"
   WHERE g."CompanyId"=p_company AND g."VendorId"=invoice."VendorId" AND po."RootPurchaseOrderId"=invoice."RootPurchaseOrderId"
    AND po."CurrencyCode"=invoice."CurrencyCode" AND upper(btrim(g."VendorBillNumber"))=upper(btrim(invoice."InvoiceNumber"))
    AND g."VendorBillDate"=invoice."InvoiceDate" AND g."DocumentKind"='NORMAL' AND g."Status"='FINALIZED'
    AND pl."PurchaseRequirementHandoffId"=line."PurchaseRequirementHandoffId" AND gl."ItemId"=line."ItemId"
    AND gl."UomSnapshot"=line."Uom"
    AND NOT EXISTS(SELECT 1 FROM advance.goods_receipts r WHERE r."CompanyId"=p_company AND r."ReversesGoodsReceiptId"=g."Id" AND r."Status"='FINALIZED')
    AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_receipt_matches m WHERE m."CompanyId"=p_company
      AND m."SupplierInvoiceLineId"=line."Id" AND m."GoodsReceiptLineId"=gl."Id" AND m."Quantity">0)
   ORDER BY g."FinalizedAt",g."Id",gl."LineNumber"
  LOOP
   EXIT WHEN remaining<=0;
   SELECT receipt."ReceivedQuantity"-coalesce(sum(m."Quantity"),0) INTO available
   FROM advance.supplier_invoice_receipt_matches m
   JOIN advance.supplier_invoice_lines il ON il."CompanyId"=p_company AND il."Id"=m."SupplierInvoiceLineId"
   WHERE m."CompanyId"=p_company AND m."GoodsReceiptLineId"=receipt."Id"
    AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations c WHERE c."CompanyId"=p_company AND c."SupplierInvoiceId"=il."SupplierInvoiceId");
   take_quantity:=least(remaining,available);
   IF take_quantity>0 THEN
    INSERT INTO advance.supplier_invoice_receipt_matches VALUES(gen_random_uuid(),p_company,line."Id",receipt."Id",
     receipt.receipt_id,take_quantity,NULL,greatest(invoice."RecordedAt",receipt.finalized_at),clock_timestamp(),p_actor);
    remaining:=remaining-take_quantity;
   END IF;
  END LOOP;
 END LOOP;
END $supplier_invoice$;

CREATE FUNCTION advance.match_supplier_invoices_on_receipt()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
DECLARE invoice_id uuid;
BEGIN
 IF NEW."Status"<>'FINALIZED' THEN RETURN NEW; END IF;
 IF TG_OP='UPDATE' AND OLD."Status"='FINALIZED' THEN RETURN NEW; END IF;
 FOR invoice_id IN
  SELECT i."Id" FROM advance.supplier_invoices i
  JOIN advance.purchase_orders po ON po."CompanyId"=NEW."CompanyId" AND po."Id"=NEW."PurchaseOrderId"
  WHERE i."CompanyId"=NEW."CompanyId" AND i."RootPurchaseOrderId"=po."RootPurchaseOrderId"
   AND i."VendorId"=NEW."VendorId" ORDER BY i."Id"
 LOOP
  PERFORM advance.reconcile_supplier_invoice_receipts(NEW."CompanyId",invoice_id,NEW."FinalizedByEmployeeId");
 END LOOP;
 RETURN NEW;
END $supplier_invoice$;
CREATE TRIGGER trg_supplier_invoice_receipt_match AFTER INSERT OR UPDATE OF "Status"
 ON advance.goods_receipts FOR EACH ROW EXECUTE FUNCTION advance.match_supplier_invoices_on_receipt();

CREATE FUNCTION advance.record_supplier_invoice(
 p_company uuid,p_id uuid,p_po uuid,p_number text,p_date date,p_currency text,p_lines jsonb,
 p_filename text,p_mime text,p_content bytea,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
RETURNS uuid LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
DECLARE po advance.purchase_orders%ROWTYPE; input record; line advance.purchase_order_lines%ROWTYPE; n integer:=0;
BEGIN
 IF NOT advance.supplier_invoice_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'SupplierInvoice.Record','create')
  OR p_id IS DISTINCT FROM nullif(current_setting('advance.ordinary_command_id',true),'')::uuid THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Supplier invoice recording requires its current Accounts creation command.';
 END IF;
 SELECT * INTO po FROM advance.purchase_orders WHERE "CompanyId"=p_company AND "Id"=p_po;
 IF NOT FOUND OR po."Status"<>'Issued' OR po."CurrencyCode" IS DISTINCT FROM upper(btrim(p_currency)) THEN
  RAISE EXCEPTION 'Supplier invoice requires an issued PO in the selected company and its original currency.';
 END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended('SUPPLIER-INVOICE:'||p_company||':'||po."RootPurchaseOrderId",0));
 PERFORM pg_advisory_xact_lock(hashtextextended('SUPPLIER-INVOICE-NUMBER:'||p_company||':'||po."VendorId"||':'||upper(btrim(p_number)),0));
 IF EXISTS(SELECT 1 FROM advance.supplier_invoices i WHERE i."CompanyId"=p_company AND i."VendorId"=po."VendorId"
   AND upper(btrim(i."InvoiceNumber"))=upper(btrim(p_number))
   AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations c WHERE c."SupplierInvoiceId"=i."Id")) THEN
  RAISE EXCEPTION 'This vendor invoice is already recorded; retain it or cancel and replace it through the governed process.';
 END IF;
 IF length(btrim(coalesce(p_number,''))) NOT BETWEEN 1 AND 100 OR p_date IS NULL
  OR jsonb_typeof(p_lines) IS DISTINCT FROM 'array' OR jsonb_array_length(p_lines) NOT BETWEEN 1 AND 1000
  OR length(btrim(coalesce(p_filename,''))) NOT BETWEEN 1 AND 255
  OR p_content IS NULL OR octet_length(p_content) NOT BETWEEN 1 AND 5242880
  OR NOT coalesce((p_mime='application/pdf' AND substring(p_content FROM 1 FOR 5)=decode('255044462d','hex'))
   OR(p_mime='image/jpeg' AND substring(p_content FROM 1 FOR 3)=decode('ffd8ff','hex'))
   OR(p_mime='image/png' AND substring(p_content FROM 1 FOR 8)=decode('89504e470d0a1a0a','hex')),false) THEN
  RAISE EXCEPTION 'Invoice number/date, lines and retained PDF, JPEG or PNG evidence of at most 5 MB are required.';
 END IF;
 PERFORM set_config('sess.supplier_invoice_write',txid_current()::text,true);
 INSERT INTO advance.supplier_invoices("Id","CompanyId","PurchaseOrderId","RootPurchaseOrderId","VendorId",
  "InvoiceNumber","InvoiceDate","CurrencyCode","FileName","ContentType","Content",
  "RecordedAt","RecordedByEmployeeId","RecordedBy","RoleAssignmentId")
 VALUES(p_id,p_company,p_po,po."RootPurchaseOrderId",po."VendorId",btrim(p_number),p_date,po."CurrencyCode",
  p_filename,p_mime,p_content,clock_timestamp(),p_actor,p_login,p_assignment);
 FOR input IN SELECT * FROM jsonb_to_recordset(p_lines)
  AS x("purchaseOrderLineId" uuid,quantity numeric,"unitRate" numeric,"payableValue" numeric)
 LOOP
  SELECT * INTO line FROM advance.purchase_order_lines
   WHERE "CompanyId"=p_company AND "PurchaseOrderId"=p_po AND "Id"=input."purchaseOrderLineId";
  IF NOT FOUND OR input.quantity IS NULL OR input.quantity<=0 OR input.quantity>line."OrderedQuantity"
   OR input."unitRate" IS NULL OR input."unitRate"<0 OR input."payableValue" IS NULL OR input."payableValue"<0
   OR round(input.quantity,6)<>input.quantity OR round(input."unitRate",6)<>input."unitRate"
   OR round(input."payableValue",6)<>input."payableValue" THEN
   RAISE EXCEPTION 'Each invoice line requires a PO line, positive quantity within its order and non-negative six-decimal values.';
  END IF;
  n:=n+1;
  INSERT INTO advance.supplier_invoice_lines VALUES(gen_random_uuid(),p_company,p_id,n,line."Id",
   line."PurchaseRequirementHandoffId",line."ItemId",line."ItemCodeSnapshot",line."ItemNameSnapshot",
   line."UomSnapshot",input.quantity,input."unitRate",input."payableValue");
 END LOOP;
 PERFORM advance.reconcile_supplier_invoice_receipts(p_company,p_id,p_actor);
 RETURN p_id;
END $supplier_invoice$;

CREATE FUNCTION advance.cancel_supplier_invoice(p_company uuid,p_invoice uuid,p_version bigint,p_reason text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text)
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
DECLARE invoice advance.supplier_invoices%ROWTYPE;
BEGIN
 IF NOT advance.supplier_invoice_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'SupplierInvoice.Cancel','cancel') THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Invoice correction requires current Accounts Manager cancellation authority.';
 END IF;
 SELECT * INTO invoice FROM advance.supplier_invoices WHERE "CompanyId"=p_company AND "Id"=p_invoice;
 IF NOT FOUND THEN RAISE EXCEPTION 'Supplier invoice not found in this company.'; END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended('SUPPLIER-INVOICE:'||p_company||':'||invoice."RootPurchaseOrderId",0));
 IF advance.supplier_invoice_version(p_company,p_invoice)<>p_version THEN RAISE EXCEPTION 'Invoice changed; refresh before correction.'; END IF;
 IF length(btrim(coalesce(p_reason,''))) NOT BETWEEN 1 AND 2000 THEN RAISE EXCEPTION 'A correction reason is required.'; END IF;
 IF EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice) THEN
  RAISE EXCEPTION 'Supplier invoice is already cancelled.';
 END IF;
 IF EXISTS(SELECT 1 FROM advance.vendor_bills b JOIN advance.goods_receipts g ON g."CompanyId"=p_company AND g."Id"=b."GoodsReceiptId"
  JOIN advance.purchase_orders po ON po."CompanyId"=p_company AND po."Id"=g."PurchaseOrderId"
  WHERE b."CompanyId"=p_company AND b."Status"='ACCEPTED' AND b."VendorId"=invoice."VendorId"
   AND po."RootPurchaseOrderId"=invoice."RootPurchaseOrderId"
   AND upper(btrim(b."BillNumber"))=upper(btrim(invoice."InvoiceNumber")) AND b."BillDate"=invoice."InvoiceDate") THEN
  RAISE EXCEPTION 'Active accepted-bill evidence prevents cancellation; use its governed reversal first.';
 END IF;
 PERFORM set_config('sess.supplier_invoice_write',txid_current()::text,true);
 INSERT INTO advance.supplier_invoice_cancellations VALUES(gen_random_uuid(),p_company,p_invoice,btrim(p_reason),clock_timestamp(),p_actor);
END $supplier_invoice$;

CREATE FUNCTION advance.link_supplier_invoice_bill(p_company uuid,p_invoice uuid,p_version bigint,p_bill uuid,
 p_actor uuid,p_role text,p_assignment uuid,p_type text)
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
DECLARE invoice advance.supplier_invoices%ROWTYPE; bill advance.vendor_bills%ROWTYPE;
BEGIN
 IF NOT advance.supplier_invoice_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'SupplierInvoice.LinkBill','approve') THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Accepted-bill linkage requires current Accounts Manager authority.';
 END IF;
 SELECT * INTO invoice FROM advance.supplier_invoices WHERE "CompanyId"=p_company AND "Id"=p_invoice;
 IF NOT FOUND THEN RAISE EXCEPTION 'Supplier invoice not found in this company.'; END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended('SUPPLIER-INVOICE:'||p_company||':'||invoice."RootPurchaseOrderId",0));
 IF advance.supplier_invoice_version(p_company,p_invoice)<>p_version THEN RAISE EXCEPTION 'Invoice changed; refresh before linking.'; END IF;
 IF EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations WHERE "CompanyId"=p_company AND "SupplierInvoiceId"=p_invoice) THEN
  RAISE EXCEPTION 'A cancelled invoice cannot receive accepted-bill linkage.';
 END IF;
 SELECT * INTO bill FROM advance.vendor_bills WHERE "CompanyId"=p_company AND "Id"=p_bill FOR UPDATE;
 IF NOT FOUND OR bill."Status"<>'ACCEPTED' OR bill."VendorId"<>invoice."VendorId"
  OR upper(btrim(bill."BillNumber"))<>upper(btrim(invoice."InvoiceNumber")) OR bill."BillDate"<>invoice."InvoiceDate"
  OR NOT EXISTS(SELECT 1 FROM advance.purchase_orders WHERE "CompanyId"=p_company AND "Id"=bill."PurchaseOrderId"
   AND "RootPurchaseOrderId"=invoice."RootPurchaseOrderId" AND "CurrencyCode"=invoice."CurrencyCode") THEN
  RAISE EXCEPTION 'Linkage requires this invoice identity and an accepted bill in the same vendor, PO root and currency.';
 END IF;
 IF NOT EXISTS(SELECT 1 FROM advance.vendor_bill_lines WHERE "CompanyId"=p_company AND "VendorBillId"=p_bill)
  OR EXISTS(SELECT 1 FROM advance.vendor_bill_lines bl WHERE bl."CompanyId"=p_company AND bl."VendorBillId"=p_bill
   AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_lines il
    JOIN advance.supplier_invoice_receipt_matches m ON m."CompanyId"=p_company AND m."SupplierInvoiceLineId"=il."Id"
    WHERE il."CompanyId"=p_company AND il."SupplierInvoiceId"=p_invoice AND m."GoodsReceiptLineId"=bl."GoodsReceiptLineId"
     AND il."ItemId"=bl."ItemId" AND il."UnitRate"=bl."BilledUnitRate"
     AND round(il."PayableValue"*bl."BilledQuantity"/il."Quantity",6)=bl."BilledPayableValue"
    GROUP BY il."Id" HAVING sum(m."Quantity")>=bl."BilledQuantity")) THEN
  RAISE EXCEPTION 'Every accepted bill line must match this invoice receipt quantity and documentary price.';
 END IF;
 PERFORM set_config('sess.supplier_invoice_write',txid_current()::text,true);
 INSERT INTO advance.supplier_invoice_bill_links VALUES(gen_random_uuid(),p_company,p_invoice,p_bill,clock_timestamp(),p_actor);
END $supplier_invoice$;

CREATE FUNCTION advance.get_supplier_invoice(p_company uuid,p_invoice uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
 SELECT jsonb_build_object('id',i."Id",'invoiceNumber',i."InvoiceNumber",'invoiceDate',i."InvoiceDate",
  'purchaseOrderId',i."PurchaseOrderId",'purchaseOrderNumber',po."PoNumber",'vendorId',i."VendorId",
  'vendorCode',v."VendorCode",'vendorName',v."Name",'currencyCode',i."CurrencyCode",
  'status',CASE WHEN c."Id" IS NULL THEN 'RECORDED' ELSE 'CANCELLED' END,
  'version',advance.supplier_invoice_version(p_company,p_invoice),'replayed',false,'cancellationReason',c."Reason",
  'evidence',jsonb_build_object('fileName',i."FileName",'contentType',i."ContentType",'sizeBytes',octet_length(i."Content"),
   'sha256',i."ContentSha256",'recordedAt',i."RecordedAt",'recordedByEmployeeId',i."RecordedByEmployeeId"),
  'lines',coalesce((SELECT jsonb_agg(jsonb_build_object('id',l."Id",'lineNumber',l."LineNumber",
   'purchaseOrderLineId',l."PurchaseOrderLineId",'itemId',l."ItemId",'itemCode',l."ItemCode",'itemName',l."ItemName",'uom',l."Uom",
   'quantity',l."Quantity",'unitRate',l."UnitRate",'payableValue',l."PayableValue",
   'receivedQuantity',coalesce(m.qty,0),'outstandingQuantity',l."Quantity"-coalesce(m.qty,0)) ORDER BY l."LineNumber")
   FROM advance.supplier_invoice_lines l LEFT JOIN LATERAL(
    SELECT sum("Quantity") qty FROM advance.supplier_invoice_receipt_matches WHERE "CompanyId"=p_company AND "SupplierInvoiceLineId"=l."Id") m ON true
   WHERE l."CompanyId"=p_company AND l."SupplierInvoiceId"=p_invoice),'[]'::jsonb),
  'receiptMatches',coalesce((SELECT jsonb_agg(jsonb_build_object('id',m."Id",'supplierInvoiceLineId',m."SupplierInvoiceLineId",
   'goodsReceiptLineId',m."GoodsReceiptLineId",'receiptEventId',m."ReceiptEventId",'quantity',m."Quantity",
   'effectiveAt',m."EffectiveAt",'recordedAt',m."RecordedAt",'recordedByEmployeeId',m."RecordedByEmployeeId",'reversesMatchId',m."ReversesMatchId")
   ORDER BY m."RecordedAt",m."Id") FROM advance.supplier_invoice_receipt_matches m
   JOIN advance.supplier_invoice_lines l ON l."CompanyId"=p_company AND l."Id"=m."SupplierInvoiceLineId"
   WHERE m."CompanyId"=p_company AND l."SupplierInvoiceId"=p_invoice),'[]'::jsonb),
  'acceptedBillIds',coalesce((SELECT jsonb_agg(b."VendorBillId" ORDER BY b."RecordedAt") FROM advance.supplier_invoice_bill_links b
   WHERE b."CompanyId"=p_company AND b."SupplierInvoiceId"=p_invoice),'[]'::jsonb))
 FROM advance.supplier_invoices i
 JOIN advance.purchase_orders po ON po."CompanyId"=p_company AND po."Id"=i."PurchaseOrderId"
 JOIN advance.vendors v ON v."Id"=i."VendorId"
 LEFT JOIN advance.supplier_invoice_cancellations c ON c."CompanyId"=p_company AND c."SupplierInvoiceId"=i."Id"
 WHERE i."CompanyId"=p_company AND i."Id"=p_invoice AND session_user='nexa_erp_runtime';
$supplier_invoice$;
CREATE FUNCTION advance.supplier_invoice_content(p_company uuid,p_invoice uuid)
RETURNS TABLE("FileName" text,"ContentType" text,"Content" bytea,"Sha256" text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $supplier_invoice$
 SELECT "FileName"::text,"ContentType"::text,"Content","ContentSha256" FROM advance.supplier_invoices
 WHERE "CompanyId"=p_company AND "Id"=p_invoice AND session_user='nexa_erp_runtime';
$supplier_invoice$;

CREATE FUNCTION advance.company_report_billed_not_received(
  p_organization text,p_employee uuid,p_assignments uuid[],p_export boolean,p_login text,p_correlation text,
  p_from_date date,p_to_date date,p_mode text,p_metric text,p_group_filter jsonb,p_offset bigint,p_page_size integer,p_report_timezone text)
RETURNS TABLE(kind integer,ordinal bigint,payload text)
LANGUAGE plpgsql VOLATILE SECURITY DEFINER SET search_path=pg_catalog,advance
AS $company_report$
#variable_conflict use_column
BEGIN
  IF session_user<>'nexa_erp_runtime' THEN
    RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Controlled company reports require the runtime principal.';
  END IF;
  IF p_report_timezone IS NULL OR btrim(p_report_timezone)='' THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='A report calendar timezone is required.';
  END IF;
  PERFORM timezone(p_report_timezone,current_timestamp);
  IF p_export IS NULL OR p_mode NOT IN ('summary','details') OR p_mode IS NULL
    OR p_from_date IS NULL OR p_to_date IS NULL OR p_from_date>p_to_date
    OR p_offset IS NULL OR p_offset<0 OR p_page_size IS NULL OR p_page_size NOT BETWEEN 1 AND 1000 THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='Invalid company report parameters.';
  END IF;
  IF false AND p_to_date<>(current_timestamp AT TIME ZONE p_report_timezone)::date THEN
    RAISE EXCEPTION USING ERRCODE='22023',MESSAGE='This report requires the current queue date.';
  END IF;
  RETURN QUERY
  WITH company AS MATERIALIZED (
  SELECT c."Id",c."Code"
  FROM advance.companies c
  WHERE c."Code"=p_organization AND c."IsActive" AND c."Status"='ACTIVE'
    AND EXISTS(SELECT 1 FROM advance.employees e WHERE e."Id"=p_employee
      AND e."LoginEnabled" AND upper(e."Status")='ACTIVE')
    AND (SELECT count(*) FROM advance.employee_company_assignments a
      WHERE a."CompanyId"=c."Id" AND a."EmployeeId"=p_employee
        AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=CURRENT_DATE
        AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE))=1
    AND EXISTS(SELECT 1 FROM advance.employee_operational_scopes s
      WHERE s."CompanyId"=c."Id" AND s."OrganizationId"=c."Code"
        AND s."EmployeeId"=p_employee AND s."IsActive" AND s."EffectiveFrom"<=CURRENT_DATE
        AND (s."EffectiveTo" IS NULL OR s."EffectiveTo">=CURRENT_DATE))
),
effective_roles AS MATERIALIZED (
  SELECT DISTINCT r."Id",r."Code"
  FROM advance.employee_role_assignments a
  JOIN advance.roles r ON r."Id"=a."RoleId" AND r."IsActive"
  JOIN company c ON c."Id"=a."CompanyId"
  WHERE a."EmployeeId"=p_employee AND a."Id"=ANY(p_assignments)
    AND a."ApprovalStatus" IN ('Approved','SeedApproved')
    AND a."EffectiveFrom"<=CURRENT_DATE
    AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE)
    AND EXISTS(SELECT 1 FROM advance.company_role_activations ca
      WHERE ca."CompanyId"=a."CompanyId" AND ca."RoleId"=a."RoleId"
        AND ca."IsEnabled" AND ca."EffectiveFrom"<=CURRENT_DATE
        AND (ca."EffectiveTo" IS NULL OR ca."EffectiveTo">=CURRENT_DATE))
),
report_grants AS MATERIALIZED (
  SELECT p."PageKey",
    bool_or(rp."CanView" OR rp."HasFullControl") AS can_view,
    bool_or(rp."CanExport" OR rp."HasFullControl") AS can_export,
    bool_or(rp."CanViewCommercialValues" OR rp."HasFullControl") AS can_commercial
  FROM advance.page_definitions p
  JOIN advance.role_page_permissions rp ON rp."PageDefinitionId"=p."Id"
  JOIN effective_roles r ON r."Id"=rp."RoleId"
  WHERE p."IsActive" AND p."PageKey" LIKE 'reports.%'
  GROUP BY p."PageKey"
),
employee_report_grants AS MATERIALIZED (
  SELECT p."PageKey",bool_or(ep."CanView") AS can_view
  FROM advance.employee_page_permissions ep
  JOIN company c ON c."Id"=ep."CompanyId"
  JOIN advance.page_definitions p ON p."Id"=ep."PageDefinitionId" AND p."IsActive"
  WHERE ep."EmployeeId"=p_employee AND p."PageKey" LIKE 'reports.%'
  GROUP BY p."PageKey"
),access AS MATERIALIZED (
  SELECT (SELECT "Id" FROM company) AS company_id,
    EXISTS(SELECT 1 FROM company)
    AND (coalesce((SELECT can_view FROM report_grants WHERE "PageKey"='reports.billed-not-received'),false)
      OR coalesce((SELECT can_view FROM employee_report_grants WHERE "PageKey"='reports.billed-not-received'),false))
    AND (NOT p_export OR coalesce((SELECT can_export FROM report_grants WHERE "PageKey"='reports.billed-not-received'),false))
    AND (NOT true OR coalesce((SELECT can_commercial FROM report_grants WHERE "PageKey"='reports.billed-not-received'),false))
    AS allowed
),
report_audit AS (
  INSERT INTO advance.audit_logs
    ("Id","CompanyId","Scope","Module","Action","EntityName","EntityId",
     "UserLoginId","ActorRoleCode","Result","CorrelationId","CreatedAt","CreatedBy","Version","AfterJson")
  SELECT gen_random_uuid(),a.company_id,
    CASE WHEN a.company_id IS NULL THEN 'GLOBAL' ELSE 'COMPANY' END,
    'Reports',CASE WHEN a.allowed THEN 'Export' ELSE 'Denied' END,
    'CompanyReport','reports.billed-not-received',p_login,
    coalesce((SELECT min("Code") FROM effective_roles),'none'),
    CASE WHEN a.allowed THEN 'Success' ELSE 'Failure' END,
    p_correlation,clock_timestamp(),p_login,0,
    jsonb_build_object('organization',p_organization,'report','reports.billed-not-received',
      'timeZone',p_report_timezone,'fromDate',p_from_date,'toDate',p_to_date,'mode',p_mode)::text
  FROM access a WHERE p_export OR NOT a.allowed
  RETURNING "Id"
),
report_source AS NOT MATERIALIZED (SELECT jsonb_build_object('vendorId',v."Id",'itemId',l."ItemId",'uom',l."Uom",'currency',i."CurrencyCode") AS group_filter,
 jsonb_build_object('vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',l."ItemCode",'itemName',l."ItemName",
  'uom',l."Uom",'currency',i."CurrencyCode") AS labels,
 jsonb_build_object('uom',l."Uom",'currency',i."CurrencyCode") AS total_group,
 jsonb_build_object('invoiceId',i."Id",'invoiceLineId',l."Id",'invoiceNumber',i."InvoiceNumber",'invoiceDate',i."InvoiceDate",
  'recordedAt',i."RecordedAt",'poNumber',po."PoNumber",'purchaseOrderLineId',l."PurchaseOrderLineId",
  'vendorCode',v."VendorCode",'vendor',v."Name",'itemCode',l."ItemCode",'itemName',l."ItemName",'uom',l."Uom",
  'currency',i."CurrencyCode",'invoiceQuantity',l."Quantity",'receivedQuantity',coalesce(received.quantity,0),
  'invoiceUnitRate',l."UnitRate",'invoicePayableValue',l."PayableValue",
  'daysOutstanding',greatest(0,p_to_date-i."InvoiceDate"),'evidenceFileName',i."FileName",'evidenceSha256',i."ContentSha256",
  'receiptHistory',coalesce(received.history,''),'acceptedBillLinks',coalesce(bills.history,'')) AS detail,
 jsonb_build_object('quantity',l."Quantity"-coalesce(received.quantity,0),
  'invoiceValue',round(l."PayableValue"*(l."Quantity"-coalesce(received.quantity,0))/l."Quantity",6)) AS metrics,
 i."RecordedAt"::text||':'||i."Id"::text||':'||l."LineNumber"::text AS sort_key
FROM advance.supplier_invoices i JOIN access a ON a.allowed AND i."CompanyId"=a.company_id
JOIN advance.supplier_invoice_lines l ON l."CompanyId"=a.company_id AND l."SupplierInvoiceId"=i."Id"
JOIN advance.vendors v ON v."Id"=i."VendorId"
JOIN advance.purchase_orders po ON po."CompanyId"=a.company_id AND po."Id"=i."PurchaseOrderId"
LEFT JOIN LATERAL(
 SELECT sum(m."Quantity") AS quantity,
  string_agg(g."GrnNumber"||' / line '||gl."LineNumber"||' / quantity '||m."Quantity"||' / match '||m."Id",'; ' ORDER BY m."EffectiveAt",m."Id") AS history
 FROM advance.supplier_invoice_receipt_matches m
 JOIN advance.goods_receipt_lines gl ON gl."CompanyId"=a.company_id AND gl."Id"=m."GoodsReceiptLineId"
 JOIN advance.goods_receipts g ON g."CompanyId"=a.company_id AND g."Id"=m."ReceiptEventId"
 WHERE m."CompanyId"=a.company_id AND m."SupplierInvoiceLineId"=l."Id"
  AND (m."EffectiveAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
) received ON true
LEFT JOIN LATERAL(
 SELECT string_agg(b."BillNumber"||' / '||b."Id",'; ' ORDER BY link."RecordedAt",link."Id") AS history
 FROM advance.supplier_invoice_bill_links link JOIN advance.vendor_bills b ON b."CompanyId"=a.company_id AND b."Id"=link."VendorBillId"
 WHERE link."CompanyId"=a.company_id AND link."SupplierInvoiceId"=i."Id"
  AND (link."RecordedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
) bills ON true
WHERE (i."RecordedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date
 AND NOT EXISTS(SELECT 1 FROM advance.supplier_invoice_cancellations c WHERE c."CompanyId"=a.company_id AND c."SupplierInvoiceId"=i."Id"
  AND (c."RecordedAt" AT TIME ZONE p_report_timezone)::date<=p_to_date)
 AND l."Quantity"-coalesce(received.quantity,0)>0),
selected_source AS NOT MATERIALIZED (
  SELECT * FROM report_source
  WHERE true AND (p_group_filter IS NULL OR group_filter @> p_group_filter)
    AND (p_mode<>'details' OR p_metric IS NULL OR coalesce((metrics->>p_metric)::numeric,0)<>0)
),
report_groups AS (
  SELECT group_filter,labels,total_group,count(*) AS source_count,coalesce(sum((metrics->>'quantity')::numeric),0) AS "quantity",coalesce(sum((metrics->>'invoiceValue')::numeric),0) AS "invoiceValue"
  FROM selected_source GROUP BY group_filter,labels,total_group HAVING true
),
numbered_groups AS MATERIALIZED (
  SELECT *,row_number() OVER group_order AS summary_ordinal,
    sum(source_count) OVER group_order-source_count+1 AS detail_start
  FROM report_groups
  WINDOW group_order AS (ORDER BY total_group::text,group_filter::text,labels::text ROWS UNBOUNDED PRECEDING)
),
report_details AS (
  SELECT s.*,row_number() OVER(ORDER BY g.summary_ordinal,s.sort_key) AS detail_ordinal
  FROM selected_source s JOIN numbered_groups g
    ON s.group_filter=g.group_filter AND s.labels=g.labels AND s.total_group=g.total_group
  WHERE p_export OR p_mode='details'
),
report_totals AS (
  SELECT total_group,sum("quantity") AS "quantity",sum("invoiceValue") AS "invoiceValue",sum(source_count) AS source_count,min(detail_start) AS detail_start
  FROM numbered_groups GROUP BY total_group
),
output_rows AS (
  SELECT 1 AS kind,g.summary_ordinal AS ordinal,
    g.labels||jsonb_build_object('group',g.group_filter,'quantity',g."quantity",'invoiceValue',g."invoiceValue",
      'detailStart',g.detail_start,'detailCount',g.source_count) AS payload
  FROM numbered_groups g
  WHERE (p_export OR p_mode='summary')
    AND (p_export OR g.summary_ordinal>p_offset AND g.summary_ordinal<=p_offset+p_page_size)
  UNION ALL
  SELECT 2,d.detail_ordinal,d.detail||d.metrics||jsonb_build_object('group',d.group_filter)
  FROM report_details d
  WHERE (p_export OR p_mode='details')
    AND (p_export OR d.detail_ordinal>p_offset AND d.detail_ordinal<=p_offset+p_page_size)
)
SELECT 0 AS kind,0::bigint AS ordinal,jsonb_build_object(
  'allowed',(SELECT allowed FROM access),'generatedAt',statement_timestamp(),'timeZone',p_report_timezone,
  'totalRows',CASE WHEN p_mode='details' THEN coalesce((SELECT sum(source_count) FROM numbered_groups),0)
    ELSE (SELECT count(*) FROM numbered_groups) END,
  'totalSourceRows',coalesce((SELECT sum(source_count) FROM numbered_groups),0),
  'totals',coalesce((SELECT jsonb_agg(t.total_group||jsonb_build_object(
    'group',t.total_group,'quantity',t."quantity",'invoiceValue',t."invoiceValue",'detailStart',t.detail_start,'detailCount',t.source_count)
    ORDER BY t.total_group::text) FROM report_totals t),'[]'::jsonb)
)::text AS payload
UNION ALL SELECT kind,ordinal,payload::text FROM output_rows ORDER BY kind,ordinal;
END
$company_report$;
