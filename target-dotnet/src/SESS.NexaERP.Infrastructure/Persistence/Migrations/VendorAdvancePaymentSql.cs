namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class VendorAdvancePaymentSql
{
    internal const string Up = """
CREATE TABLE advance.vendor_advances(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "AdvanceNumber" varchar(40) NOT NULL,"PurchaseOrderId" uuid NOT NULL REFERENCES advance.purchase_orders("Id"),
 "VendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),"PaidDate" date NOT NULL,
 "Amount" numeric(24,6) NOT NULL CHECK("Amount">0),"CurrencyCode" varchar(3) NOT NULL,
 "PaymentReference" varchar(160) NOT NULL,"EvidenceObjectKey" varchar(500) NOT NULL,
 "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "ActorRoleCode" varchar(100) NOT NULL,
 "ResolvedRoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "ResolvedRoleAssignmentType" varchar(20) NOT NULL CHECK("ResolvedRoleAssignmentType" IN('FULL','TEMPORARY')),
 "IdempotencyKey" varchar(100) NOT NULL,"RequestFingerprint" character(64) NOT NULL,
 "CreatedAt" timestamptz NOT NULL,"CreatedBy" varchar(160) NOT NULL,
 UNIQUE("CompanyId","AdvanceNumber"),UNIQUE("CompanyId","IdempotencyKey"),
 CHECK("CurrencyCode"=upper("CurrencyCode")));
CREATE INDEX "IX_vendor_advances_vendor_date" ON advance.vendor_advances("CompanyId","VendorId","PaidDate");

CREATE TABLE advance.vendor_advance_reversals(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
 "VendorAdvanceId" uuid NOT NULL REFERENCES advance.vendor_advances("Id"),
 "Reason" varchar(1000) NOT NULL,"ReversedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "ActorRoleCode" varchar(100) NOT NULL,
 "ResolvedRoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "ResolvedRoleAssignmentType" varchar(20) NOT NULL CHECK("ResolvedRoleAssignmentType" IN('FULL','TEMPORARY')),
 "IdempotencyKey" varchar(100) NOT NULL,"RequestFingerprint" character(64) NOT NULL,
 "ReversedAt" timestamptz NOT NULL,UNIQUE("CompanyId","VendorAdvanceId"),UNIQUE("CompanyId","IdempotencyKey"));

CREATE TABLE advance.vendor_advance_adjustments(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
 "VendorAdvanceId" uuid NOT NULL REFERENCES advance.vendor_advances("Id"),
 "VendorBillId" uuid NOT NULL REFERENCES advance.vendor_bills("Id"),
 "Amount" numeric(24,6) NOT NULL CHECK("Amount">0),"AdjustedAt" timestamptz NOT NULL,
 "CreatedBy" varchar(160) NOT NULL,UNIQUE("CompanyId","VendorAdvanceId","VendorBillId"));
CREATE TABLE advance.vendor_advance_adjustment_restorations(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
 "VendorAdvanceAdjustmentId" uuid NOT NULL REFERENCES advance.vendor_advance_adjustments("Id"),
 "VendorBillId" uuid NOT NULL REFERENCES advance.vendor_bills("Id"),
 "Amount" numeric(24,6) NOT NULL CHECK("Amount">0),"RestoredAt" timestamptz NOT NULL,
 "CreatedBy" varchar(160) NOT NULL,UNIQUE("CompanyId","VendorAdvanceAdjustmentId","VendorBillId"));

CREATE TABLE advance.vendor_payments(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL REFERENCES advance.companies("Id"),
 "PaymentNumber" varchar(40) NOT NULL,"VendorId" uuid NOT NULL REFERENCES advance.vendors("Id"),
 "PaidDate" date NOT NULL,"Amount" numeric(24,6) NOT NULL CHECK("Amount">0),
 "CurrencyCode" varchar(3) NOT NULL,"PaymentReference" varchar(160) NOT NULL,
 "EvidenceObjectKey" varchar(500) NOT NULL,
 "RecordedByEmployeeId" uuid NOT NULL REFERENCES advance.employees("Id"),
 "ActorRoleCode" varchar(100) NOT NULL,
 "ResolvedRoleAssignmentId" uuid NOT NULL REFERENCES advance.employee_role_assignments("Id"),
 "ResolvedRoleAssignmentType" varchar(20) NOT NULL CHECK("ResolvedRoleAssignmentType" IN('FULL','TEMPORARY')),
 "IdempotencyKey" varchar(100) NOT NULL,"RequestFingerprint" character(64) NOT NULL,
 "CreatedAt" timestamptz NOT NULL,"CreatedBy" varchar(160) NOT NULL,
 UNIQUE("CompanyId","PaymentNumber"),UNIQUE("CompanyId","IdempotencyKey"),
 CHECK("CurrencyCode"=upper("CurrencyCode")));
CREATE INDEX "IX_vendor_payments_vendor_date" ON advance.vendor_payments("CompanyId","VendorId","PaidDate");
CREATE TABLE advance.vendor_payment_allocations(
 "Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
 "VendorPaymentId" uuid NOT NULL REFERENCES advance.vendor_payments("Id"),
 "VendorBillId" uuid NOT NULL REFERENCES advance.vendor_bills("Id"),
 "Amount" numeric(24,6) NOT NULL CHECK("Amount">0),"CreatedAt" timestamptz NOT NULL,
 "CreatedBy" varchar(160) NOT NULL,UNIQUE("CompanyId","VendorPaymentId","VendorBillId"));
CREATE INDEX "IX_vendor_payment_allocations_bill" ON advance.vendor_payment_allocations("CompanyId","VendorBillId");

CREATE FUNCTION advance.guard_vendor_financial_evidence() RETURNS trigger
LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $f$
BEGIN
 IF TG_OP<>'INSERT' THEN RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor financial evidence is immutable.'; END IF;
 IF current_setting('sess.vendor_financial_write',true) IS DISTINCT FROM txid_current()::text THEN
  RAISE EXCEPTION USING ERRCODE='42501',MESSAGE='Vendor financial evidence may be inserted only through a controlled function.';
 END IF;
 RETURN NEW;
END $f$;
CREATE TRIGGER trg_vendor_advances_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_advances FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();
CREATE TRIGGER trg_vendor_advance_reversals_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_advance_reversals FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();
CREATE TRIGGER trg_vendor_advance_adjustments_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_advance_adjustments FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();
CREATE TRIGGER trg_vendor_advance_restorations_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_advance_adjustment_restorations FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();
CREATE TRIGGER trg_vendor_payments_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_payments FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();
CREATE TRIGGER trg_vendor_payment_allocations_immutable BEFORE INSERT OR UPDATE OR DELETE ON advance.vendor_payment_allocations FOR EACH ROW EXECUTE FUNCTION advance.guard_vendor_financial_evidence();

CREATE FUNCTION advance.vendor_financial_command_valid(p_company uuid,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_operation text)
RETURNS boolean LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
 SELECT session_user='nexa_erp_runtime' AND p_role='ACCOUNTS_MANAGER' AND p_type IN('FULL','TEMPORARY')
 AND EXISTS(SELECT 1 FROM advance.companies c WHERE c."Id"=p_company
  AND advance.ordinary_command_context_valid(c."Code",p_actor,current_setting('advance.ordinary_identity_issuer',true),current_setting('advance.ordinary_identity_subject',true),p_role))
 AND EXISTS(SELECT 1 FROM advance.command_requests r
  WHERE r."CommandId"=nullif(current_setting('advance.ordinary_command_id',true),'')::uuid
   AND r."Operation"=p_operation AND r."ResolvedRoleAssignmentId"=p_assignment)
 AND EXISTS(SELECT 1 FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
  WHERE a."Id"=p_assignment AND a."EmployeeId"=p_actor AND a."CompanyId"=p_company
   AND r."Code"=p_role AND a."AssignmentType"=p_type AND a."EffectiveFrom"<=CURRENT_DATE
   AND(a."EffectiveTo" IS NULL OR a."EffectiveTo">=CURRENT_DATE));
$f$;
CREATE FUNCTION advance.record_vendor_advance(
 p_company uuid,p_po uuid,p_date date,p_amount numeric,p_currency text,p_reference text,p_evidence text,
 p_key text,p_hash text,p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
RETURNS TABLE("EvidenceId" uuid,"Replayed" boolean)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE old advance.vendor_advances%ROWTYPE; po advance.purchase_orders%ROWTYPE;
 used numeric; n integer; eid uuid;
BEGIN
 SELECT * INTO old FROM advance.vendor_advances
  WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
 IF FOUND THEN
  IF old."RequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Vendor advance idempotency mismatch.'; END IF;
  RETURN QUERY SELECT old."Id",true; RETURN;
 END IF;
 IF NOT advance.vendor_financial_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'VendorAdvance.Record') THEN
  RAISE EXCEPTION USING ERRCODE='42501',
   MESSAGE='Vendor advance requires a current FULL or TEMPORARY ACCOUNTS_MANAGER assignment.';
 END IF;
 IF p_amount<=0 OR p_date IS NULL OR btrim(coalesce(p_reference,''))=''
  OR btrim(coalesce(p_evidence,''))='' THEN
  RAISE EXCEPTION 'Complete positive vendor advance evidence is required.';
 END IF;
 SELECT * INTO po FROM advance.purchase_orders
  WHERE "CompanyId"=p_company AND "Id"=p_po AND "IsCurrentVersion" FOR UPDATE;
 IF NOT FOUND OR po."Status"<>'Issued' THEN
  RAISE EXCEPTION 'Vendor advance requires an issued Purchase Order; draft and cancelled orders are refused.';
 END IF;
 IF po."CurrencyCode"<>upper(btrim(p_currency)) THEN
  RAISE EXCEPTION 'Vendor advance currency must match the Purchase Order currency.';
 END IF;
 SELECT coalesce(sum(a."Amount"),0) INTO used FROM advance.vendor_advances a
  WHERE a."CompanyId"=p_company AND a."PurchaseOrderId"=p_po
   AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
    WHERE r."CompanyId"=p_company AND r."VendorAdvanceId"=a."Id");
 IF used+p_amount>po."TotalPayableValue" THEN
  RAISE EXCEPTION 'Vendor advance total % exceeds Purchase Order value %.',
   used+p_amount,po."TotalPayableValue";
 END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':ADV',0));
 SELECT count(*)+1 INTO n FROM advance.vendor_advances WHERE "CompanyId"=p_company;
 eid:=gen_random_uuid();
 PERFORM set_config('sess.vendor_financial_write',txid_current()::text,true);
 INSERT INTO advance.vendor_advances VALUES(
  eid,p_company,'VADV-'||to_char(p_date,'YYYY')||'-'||lpad(n::text,6,'0'),
  p_po,po."VendorId",p_date,p_amount,upper(btrim(p_currency)),btrim(p_reference),
  btrim(p_evidence),p_actor,p_role,p_assignment,p_type,p_key,p_hash,clock_timestamp(),p_login);
 RETURN QUERY SELECT eid,false;
END $f$;

CREATE FUNCTION advance.reverse_vendor_advance(
 p_company uuid,p_advance uuid,p_reason text,p_key text,p_hash text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
RETURNS TABLE("EvidenceId" uuid,"Replayed" boolean)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE old advance.vendor_advance_reversals%ROWTYPE; adjusted numeric;
BEGIN
 SELECT * INTO old FROM advance.vendor_advance_reversals
  WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
 IF FOUND THEN
  IF old."VendorAdvanceId"<>p_advance OR old."RequestFingerprint"<>p_hash THEN
   RAISE EXCEPTION 'Vendor advance reversal idempotency mismatch.';
  END IF;
  RETURN QUERY SELECT p_advance,true; RETURN;
 END IF;
 IF NOT advance.vendor_financial_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'VendorAdvance.Reverse') THEN
  RAISE EXCEPTION USING ERRCODE='42501',
   MESSAGE='Vendor advance reversal requires a current FULL or TEMPORARY ACCOUNTS_MANAGER assignment.';
 END IF;
 IF NOT EXISTS(SELECT 1 FROM advance.vendor_advances
  WHERE "CompanyId"=p_company AND "Id"=p_advance) THEN
  RAISE EXCEPTION 'Vendor advance was not found.';
 END IF;
 IF EXISTS(SELECT 1 FROM advance.vendor_advance_reversals
  WHERE "CompanyId"=p_company AND "VendorAdvanceId"=p_advance) THEN
  RAISE EXCEPTION 'Vendor advance is already reversed.';
 END IF;
 SELECT coalesce(sum(a."Amount"),0)-coalesce(sum(r."Amount"),0) INTO adjusted
 FROM advance.vendor_advance_adjustments a
 LEFT JOIN advance.vendor_advance_adjustment_restorations r
  ON r."VendorAdvanceAdjustmentId"=a."Id"
 WHERE a."CompanyId"=p_company AND a."VendorAdvanceId"=p_advance;
 IF adjusted<>0 THEN
  RAISE EXCEPTION 'Adjusted vendor advance cannot be reversed before its accepted bill is reversed.';
 END IF;
 IF btrim(coalesce(p_reason,''))='' THEN RAISE EXCEPTION 'Reversal reason is required.'; END IF;
 PERFORM set_config('sess.vendor_financial_write',txid_current()::text,true);
 INSERT INTO advance.vendor_advance_reversals VALUES(
  gen_random_uuid(),p_company,p_advance,btrim(p_reason),p_actor,p_role,p_assignment,p_type,
  p_key,p_hash,clock_timestamp());
 RETURN QUERY SELECT p_advance,false;
END $f$;

CREATE FUNCTION advance.adjust_vendor_advances_for_bill() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE left_value numeric; x record; available numeric; take_value numeric;
BEGIN
 PERFORM set_config('sess.vendor_financial_write',txid_current()::text,true);
 IF OLD."Status"='DRAFT' AND NEW."Status"='ACCEPTED' THEN
  left_value:=NEW."TotalPayableValue";
  FOR x IN
   SELECT a.*,
    coalesce((SELECT sum(z."Amount") FROM advance.vendor_advance_adjustments z
     WHERE z."VendorAdvanceId"=a."Id"),0)
    -coalesce((SELECT sum(y."Amount")
      FROM advance.vendor_advance_adjustment_restorations y
      JOIN advance.vendor_advance_adjustments z ON z."Id"=y."VendorAdvanceAdjustmentId"
      WHERE z."VendorAdvanceId"=a."Id"),0) adjusted
   FROM advance.vendor_advances a
   WHERE a."CompanyId"=NEW."CompanyId" AND a."PurchaseOrderId"=NEW."PurchaseOrderId"
    AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
     WHERE r."VendorAdvanceId"=a."Id")
   ORDER BY a."PaidDate",a."CreatedAt",a."Id"
  LOOP
   EXIT WHEN left_value<=0;
   available:=x."Amount"-x.adjusted; take_value:=least(available,left_value);
   IF take_value>0 THEN
    INSERT INTO advance.vendor_advance_adjustments VALUES(
     gen_random_uuid(),NEW."CompanyId",x."Id",NEW."Id",take_value,clock_timestamp(),
     coalesce(NEW."UpdatedBy",NEW."CreatedBy"));
    left_value:=left_value-take_value;
   END IF;
  END LOOP;
 ELSIF OLD."Status"='ACCEPTED' AND NEW."Status"='REVERSED' THEN
  INSERT INTO advance.vendor_advance_adjustment_restorations
  SELECT gen_random_uuid(),a."CompanyId",a."Id",NEW."Id",a."Amount",clock_timestamp(),
   coalesce(NEW."UpdatedBy",NEW."CreatedBy")
  FROM advance.vendor_advance_adjustments a
  WHERE a."CompanyId"=NEW."CompanyId" AND a."VendorBillId"=NEW."Id"
   AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_adjustment_restorations r
    WHERE r."VendorAdvanceAdjustmentId"=a."Id");
 END IF;
 RETURN NEW;
END $f$;
CREATE TRIGGER trg_vendor_bill_advance_adjustment
 AFTER UPDATE OF "Status" ON advance.vendor_bills
 FOR EACH ROW EXECUTE FUNCTION advance.adjust_vendor_advances_for_bill();

CREATE FUNCTION advance.record_vendor_payment(
 p_company uuid,p_vendor uuid,p_date date,p_amount numeric,p_currency text,
 p_reference text,p_evidence text,p_lines jsonb,p_key text,p_hash text,
 p_actor uuid,p_role text,p_assignment uuid,p_type text,p_login text)
RETURNS TABLE("EvidenceId" uuid,"Replayed" boolean)
LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE old advance.vendor_payments%ROWTYPE; j jsonb; b advance.vendor_bills%ROWTYPE;
 paid numeric; adjusted numeric; total numeric:=0; line_amount numeric; eid uuid; n integer;
BEGIN
 SELECT * INTO old FROM advance.vendor_payments
  WHERE "CompanyId"=p_company AND "IdempotencyKey"=p_key;
 IF FOUND THEN
  IF old."RequestFingerprint"<>p_hash THEN RAISE EXCEPTION 'Vendor payment idempotency mismatch.'; END IF;
  RETURN QUERY SELECT old."Id",true; RETURN;
 END IF;
 IF NOT advance.vendor_financial_command_valid(p_company,p_actor,p_role,p_assignment,p_type,'VendorPayment.Record') THEN
  RAISE EXCEPTION USING ERRCODE='42501',
   MESSAGE='Vendor payment requires a current FULL or TEMPORARY ACCOUNTS_MANAGER assignment.';
 END IF;
 IF p_amount<=0 OR p_date IS NULL OR jsonb_typeof(p_lines)<>'array'
  OR jsonb_array_length(p_lines)=0 OR btrim(coalesce(p_reference,''))='' THEN
  RAISE EXCEPTION 'Complete positive vendor payment evidence and allocations are required.';
 END IF;
 IF (SELECT count(*) FROM jsonb_array_elements(p_lines))
  <>(SELECT count(DISTINCT(q->>'vendorBillId')::uuid) FROM jsonb_array_elements(p_lines) q) THEN
  RAISE EXCEPTION 'A bill may appear only once in a payment.';
 END IF;
 FOR j IN SELECT * FROM jsonb_array_elements(p_lines) LOOP
  line_amount:=(j->>'amount')::numeric;
  SELECT * INTO b FROM advance.vendor_bills
   WHERE "CompanyId"=p_company AND "Id"=(j->>'vendorBillId')::uuid FOR UPDATE;
  IF NOT FOUND OR b."Status"<>'ACCEPTED' THEN
   RAISE EXCEPTION 'A bill cannot be paid before acceptance.';
  END IF;
  IF b."VendorId"<>p_vendor THEN
   RAISE EXCEPTION 'Every allocated bill must belong to the payment vendor.';
  END IF;
  IF (SELECT "CurrencyCode" FROM advance.purchase_orders WHERE "Id"=b."PurchaseOrderId")
   <>upper(btrim(p_currency)) THEN
   RAISE EXCEPTION 'Payment currency must match every allocated Purchase Order.';
  END IF;
  SELECT coalesce(sum(a."Amount"),0)-coalesce(sum(r."Amount"),0) INTO adjusted
  FROM advance.vendor_advance_adjustments a
  LEFT JOIN advance.vendor_advance_adjustment_restorations r
   ON r."VendorAdvanceAdjustmentId"=a."Id"
  WHERE a."VendorBillId"=b."Id";
  SELECT coalesce(sum(a."Amount"),0) INTO paid
  FROM advance.vendor_payment_allocations a
  WHERE a."CompanyId"=p_company AND a."VendorBillId"=b."Id";
  IF line_amount<=0 OR paid+line_amount>b."TotalPayableValue"-adjusted THEN
   RAISE EXCEPTION 'Payment allocation % exceeds outstanding value % for bill %.',
    line_amount,b."TotalPayableValue"-adjusted-paid,b."BillNumber";
  END IF;
  total:=total+line_amount;
 END LOOP;
 IF total<>p_amount THEN
  RAISE EXCEPTION 'Payment amount % must equal allocation total %.',p_amount,total;
 END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':PAY',0));
 SELECT count(*)+1 INTO n FROM advance.vendor_payments WHERE "CompanyId"=p_company;
 eid:=gen_random_uuid();
 PERFORM set_config('sess.vendor_financial_write',txid_current()::text,true);
 INSERT INTO advance.vendor_payments VALUES(
  eid,p_company,'VPAY-'||to_char(p_date,'YYYY')||'-'||lpad(n::text,6,'0'),
  p_vendor,p_date,p_amount,upper(btrim(p_currency)),btrim(p_reference),
  btrim(coalesce(p_evidence,'')),p_actor,p_role,p_assignment,p_type,p_key,p_hash,
  clock_timestamp(),p_login);
 INSERT INTO advance.vendor_payment_allocations
 SELECT gen_random_uuid(),p_company,eid,(q->>'vendorBillId')::uuid,
  (q->>'amount')::numeric,clock_timestamp(),p_login
 FROM jsonb_array_elements(p_lines) q;
 RETURN QUERY SELECT eid,false;
END $f$;

CREATE FUNCTION advance.payment_due_date(p_at timestamptz,p_terms text)
RETURNS date LANGUAGE plpgsql IMMUTABLE SET search_path=pg_catalog AS $f$
DECLARE m text[];
BEGIN
 IF p_at IS NULL THEN RETURN NULL; END IF;
 m:=regexp_match(upper(coalesce(p_terms,'')),
  '(?:NET[[:space:]]*)?([0-9]+)[[:space:]]*(?:DAY|DAYS|D)');
 IF m IS NOT NULL THEN RETURN p_at::date+m[1]::integer; END IF;
 IF upper(coalesce(p_terms,''))~'(IMMEDIATE|ON[[:space:]]+ACCEPTANCE)' THEN
  RETURN p_at::date;
 END IF;
 RETURN NULL;
END $f$;

CREATE FUNCTION advance.vendor_advance_json(p_company uuid,p_id uuid,p_replay boolean)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
SELECT jsonb_build_object(
 'id',a."Id",'advanceNumber',a."AdvanceNumber",
 'purchaseOrderId',a."PurchaseOrderId",'purchaseOrderNumber',po."PoNumber",
 'vendorId',a."VendorId",'vendorCode',v."VendorCode",'vendorName',v."Name",
 'paidDate',a."PaidDate",'amount',a."Amount",'currencyCode',a."CurrencyCode",
 'paymentReference',a."PaymentReference",'evidenceObjectKey',a."EvidenceObjectKey",
 'adjustedAmount',
  coalesce((SELECT sum(x."Amount") FROM advance.vendor_advance_adjustments x
   WHERE x."VendorAdvanceId"=a."Id"),0)
  -coalesce((SELECT sum(r."Amount")
   FROM advance.vendor_advance_adjustment_restorations r
   JOIN advance.vendor_advance_adjustments x ON x."Id"=r."VendorAdvanceAdjustmentId"
   WHERE x."VendorAdvanceId"=a."Id"),0),
 'outstandingAmount',CASE
  WHEN EXISTS(SELECT 1 FROM advance.vendor_advance_reversals z
   WHERE z."VendorAdvanceId"=a."Id") THEN 0
  ELSE a."Amount"
   -coalesce((SELECT sum(x."Amount") FROM advance.vendor_advance_adjustments x
    WHERE x."VendorAdvanceId"=a."Id"),0)
   +coalesce((SELECT sum(r."Amount")
    FROM advance.vendor_advance_adjustment_restorations r
    JOIN advance.vendor_advance_adjustments x ON x."Id"=r."VendorAdvanceAdjustmentId"
    WHERE x."VendorAdvanceId"=a."Id"),0) END,
 'isReversed',EXISTS(SELECT 1 FROM advance.vendor_advance_reversals z
  WHERE z."VendorAdvanceId"=a."Id"),
 'purchaseOrderCancelled',po."Status"='Cancelled',
 'createdAt',a."CreatedAt",'replayed',p_replay)
FROM advance.vendor_advances a
JOIN advance.purchase_orders po ON po."Id"=a."PurchaseOrderId"
JOIN advance.vendors v ON v."Id"=a."VendorId"
WHERE a."CompanyId"=p_company AND a."Id"=p_id;
$f$;

CREATE FUNCTION advance.vendor_payment_json(p_company uuid,p_id uuid,p_replay boolean)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
SELECT jsonb_build_object(
 'id',p."Id",'paymentNumber',p."PaymentNumber",
 'vendorId',p."VendorId",'vendorCode',v."VendorCode",'vendorName',v."Name",
 'paidDate',p."PaidDate",'amount',p."Amount",'currencyCode',p."CurrencyCode",
 'paymentReference',p."PaymentReference",'evidenceObjectKey',p."EvidenceObjectKey",
 'createdAt',p."CreatedAt",'replayed',p_replay,
 'allocations',coalesce((
  SELECT jsonb_agg(jsonb_build_object(
   'vendorBillId',a."VendorBillId",'billNumber',b."BillNumber",
   'acceptedValue',b."TotalPayableValue",
   'advanceAdjustedValue',
    coalesce((SELECT sum(x."Amount") FROM advance.vendor_advance_adjustments x
     WHERE x."VendorBillId"=b."Id"),0)
    -coalesce((SELECT sum(r."Amount")
     FROM advance.vendor_advance_adjustment_restorations r
     JOIN advance.vendor_advance_adjustments x ON x."Id"=r."VendorAdvanceAdjustmentId"
     WHERE x."VendorBillId"=b."Id"),0),
   'previouslyPaidValue',
    (SELECT coalesce(sum(z."Amount"),0) FROM advance.vendor_payment_allocations z
     WHERE z."VendorBillId"=b."Id" AND z."CreatedAt"<a."CreatedAt"),
   'amount',a."Amount") ORDER BY b."BillDate",b."Id")
  FROM advance.vendor_payment_allocations a
  JOIN advance.vendor_bills b ON b."Id"=a."VendorBillId"
  WHERE a."CompanyId"=p_company AND a."VendorPaymentId"=p."Id"),'[]'::jsonb))
FROM advance.vendor_payments p
JOIN advance.vendors v ON v."Id"=p."VendorId"
WHERE p."CompanyId"=p_company AND p."Id"=p_id;
$f$;

CREATE FUNCTION advance.list_vendor_advance_purchase_orders(
 p_company uuid,p_vendor uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT po."Id" pid,po."PoNumber" pn,po."VendorId" vid,v."VendorCode" vc,
  v."Name" vn,po."CurrencyCode" currency,po."TotalPayableValue" po_value,
  coalesce((SELECT sum(a."Amount") FROM advance.vendor_advances a
   WHERE a."CompanyId"=p_company AND a."PurchaseOrderId"=po."Id"
    AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
     WHERE r."CompanyId"=p_company AND r."VendorAdvanceId"=a."Id")),0) advanced
 FROM advance.purchase_orders po JOIN advance.vendors v ON v."Id"=po."VendorId"
 WHERE po."CompanyId"=p_company AND po."IsCurrentVersion" AND po."Status"='Issued'
  AND(p_vendor IS NULL OR po."VendorId"=p_vendor))
SELECT coalesce(jsonb_agg(jsonb_build_object(
 'purchaseOrderId',pid,'purchaseOrderNumber',pn,'vendorId',vid,
 'vendorCode',vc,'vendorName',vn,'currencyCode',currency,
 'purchaseOrderValue',po_value,'activeAdvanceAmount',advanced,
 'availableAdvanceAmount',po_value-advanced)
 ORDER BY pn),'[]'::jsonb)
FROM q WHERE po_value-advanced>0;
$f$;

CREATE FUNCTION advance.list_vendor_advances(
 p_company uuid,p_vendor uuid,p_po uuid,p_only boolean,p_page integer,p_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT a."Id",a."PaidDate" FROM advance.vendor_advances a
 WHERE a."CompanyId"=p_company
  AND(p_vendor IS NULL OR a."VendorId"=p_vendor)
  AND(p_po IS NULL OR a."PurchaseOrderId"=p_po)
  AND(p_only IS NOT TRUE OR
   (advance.vendor_advance_json(p_company,a."Id",false)->>'outstandingAmount')::numeric>0)),
c AS(SELECT count(*) n FROM q),
p AS(SELECT * FROM q ORDER BY "PaidDate" DESC,"Id"
 OFFSET greatest(p_page-1,0)*p_size LIMIT p_size)
SELECT jsonb_build_object(
 'total',(SELECT n FROM c),'page',p_page,'pageSize',p_size,
 'items',coalesce((SELECT jsonb_agg(
  advance.vendor_advance_json(p_company,"Id",false)) FROM p),'[]'::jsonb));
$f$;

CREATE FUNCTION advance.list_vendor_payments(
 p_company uuid,p_vendor uuid,p_page integer,p_size integer)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT p."Id",p."PaidDate" FROM advance.vendor_payments p
 WHERE p."CompanyId"=p_company AND(p_vendor IS NULL OR p."VendorId"=p_vendor)),
c AS(SELECT count(*) n FROM q),
z AS(SELECT * FROM q ORDER BY "PaidDate" DESC,"Id"
 OFFSET greatest(p_page-1,0)*p_size LIMIT p_size)
SELECT jsonb_build_object(
 'total',(SELECT n FROM c),'page',p_page,'pageSize',p_size,
 'items',coalesce((SELECT jsonb_agg(
  advance.vendor_payment_json(p_company,"Id",false)) FROM z),'[]'::jsonb));
$f$;

CREATE FUNCTION advance.list_vendor_payables(
 p_company uuid,p_vendor uuid,p_overdue boolean)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT b.*,po."PoNumber",po."PaymentTermsSnapshot",v."VendorCode" vc,v."Name" vn,
  advance.payment_due_date(b."DecidedAt",po."PaymentTermsSnapshot") due,
  coalesce((SELECT sum(a."Amount") FROM advance.vendor_advance_adjustments a
   WHERE a."VendorBillId"=b."Id"),0)
  -coalesce((SELECT sum(r."Amount")
   FROM advance.vendor_advance_adjustment_restorations r
   JOIN advance.vendor_advance_adjustments a ON a."Id"=r."VendorAdvanceAdjustmentId"
   WHERE a."VendorBillId"=b."Id"),0) adv,
  coalesce((SELECT sum(a."Amount") FROM advance.vendor_payment_allocations a
   WHERE a."VendorBillId"=b."Id"),0) paid
 FROM advance.vendor_bills b
 JOIN advance.purchase_orders po ON po."Id"=b."PurchaseOrderId"
 JOIN advance.vendors v ON v."Id"=b."VendorId"
 WHERE b."CompanyId"=p_company AND b."Status"='ACCEPTED'
  AND(p_vendor IS NULL OR b."VendorId"=p_vendor))
SELECT coalesce(jsonb_agg(jsonb_build_object(
 'vendorBillId',"Id",'billNumber',"BillNumber",
 'purchaseOrderId',"PurchaseOrderId",'purchaseOrderNumber',"PoNumber",
 'vendorId',"VendorId",'vendorCode',vc,'vendorName',vn,
 'billDate',"BillDate",'acceptedAt',"DecidedAt",
 'paymentTerms',"PaymentTermsSnapshot",'dueDate',due,
 'acceptedValue',"TotalPayableValue",'advanceAdjustedValue',adv,
 'paidValue',paid,'outstandingValue',"TotalPayableValue"-adv-paid,
 'isOverdue',due<CURRENT_DATE)
 ORDER BY due NULLS LAST,"BillDate"),'[]'::jsonb)
FROM q
WHERE "TotalPayableValue"-adv-paid>0
 AND(p_overdue IS NOT TRUE OR due<CURRENT_DATE);
$f$;

CREATE FUNCTION advance.list_vendor_positions(p_company uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT v."Id",v."VendorCode",v."Name",
  coalesce((SELECT sum(
   (advance.vendor_advance_json(p_company,a."Id",false)->>'outstandingAmount')::numeric)
   FROM advance.vendor_advances a
   WHERE a."CompanyId"=p_company AND a."VendorId"=v."Id"),0) adv,
  coalesce((SELECT sum((j->>'outstandingValue')::numeric)
   FROM jsonb_array_elements(
    advance.list_vendor_payables(p_company,v."Id",false)) j),0) bills,
  (SELECT count(DISTINCT a."PurchaseOrderId")
   FROM advance.vendor_advances a
   JOIN advance.purchase_orders po ON po."Id"=a."PurchaseOrderId"
   WHERE a."CompanyId"=p_company AND a."VendorId"=v."Id"
    AND po."Status"='Cancelled'
    AND(advance.vendor_advance_json(p_company,a."Id",false)
      ->>'outstandingAmount')::numeric>0) cancelled
 FROM advance.vendors v)
SELECT coalesce(jsonb_agg(jsonb_build_object(
 'vendorId',"Id",'vendorCode',"VendorCode",'vendorName',"Name",
 'outstandingAdvance',adv,'outstandingBills',bills,
 'netPayable',bills-adv,
 'cancelledPurchaseOrdersWithOutstandingAdvance',cancelled)
 ORDER BY "VendorCode"),'[]'::jsonb)
FROM q WHERE adv<>0 OR bills<>0;
$f$;

INSERT INTO advance.page_definitions(
 "Id","PageKey","Module","Title","Route","IsActive","CreatedAt","CreatedBy","Version")
VALUES(
 md5('accounts.vendor-financial-evidence')::uuid,
 'accounts.vendor-financial-evidence','Accounts','Vendor Advances and Payments',
 '/accounts/vendor-financial-evidence',true,now(),'VendorAdvanceAndPaymentEvidence',0);
INSERT INTO advance.role_page_permissions(
 "Id","RoleId","PageDefinitionId","CanView","CanCreate","CanUpdate","CanSubmit",
 "CanIssue","CanVerify","CanApprove","CanReject","CanRequestClarification",
 "CanRequestRevision","CanResubmit","CanCancel","CanDeactivate","CanPrint",
 "CanDownload","CanExport","CanUploadAttachment","CanReplaceAttachment",
 "CanViewCommercialValues","CanViewAuditHistory","HasFullControl",
 "CreatedAt","CreatedBy","Version")
SELECT md5('accounts.vendor-financial-evidence:'||r."Id")::uuid,r."Id",p."Id",
 true,false,false,false,false,false,true,false,false,false,false,true,false,
 false,true,true,true,false,true,true,false,now(),'VendorAdvanceAndPaymentEvidence',0
FROM advance.roles r CROSS JOIN advance.page_definitions p
WHERE r."Code"='ACCOUNTS_MANAGER'
 AND p."PageKey"='accounts.vendor-financial-evidence';

REVOKE ALL ON TABLE
 advance.vendor_advances,advance.vendor_advance_reversals,
 advance.vendor_advance_adjustments,
 advance.vendor_advance_adjustment_restorations,
 advance.vendor_payments,advance.vendor_payment_allocations
FROM PUBLIC;
REVOKE ALL ON FUNCTION
 advance.guard_vendor_financial_evidence(),
 advance.vendor_financial_command_valid(uuid,uuid,text,uuid,text,text),
 advance.record_vendor_advance(
  uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text),
 advance.reverse_vendor_advance(
  uuid,uuid,text,text,text,uuid,text,uuid,text,text),
 advance.adjust_vendor_advances_for_bill(),
 advance.record_vendor_payment(
  uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text),
 advance.payment_due_date(timestamptz,text),
 advance.vendor_advance_json(uuid,uuid,boolean),
 advance.vendor_payment_json(uuid,uuid,boolean),
 advance.list_vendor_advance_purchase_orders(uuid,uuid),
 advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer),
 advance.list_vendor_payments(uuid,uuid,integer,integer),
 advance.list_vendor_payables(uuid,uuid,boolean),
 advance.list_vendor_positions(uuid)
FROM PUBLIC;

DO $acl$
BEGIN
 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
  ALTER TABLE advance.vendor_advances OWNER TO nexa_erp_owner;
  ALTER TABLE advance.vendor_advance_reversals OWNER TO nexa_erp_owner;
  ALTER TABLE advance.vendor_advance_adjustments OWNER TO nexa_erp_owner;
  ALTER TABLE advance.vendor_advance_adjustment_restorations OWNER TO nexa_erp_owner;
  ALTER TABLE advance.vendor_payments OWNER TO nexa_erp_owner;
  ALTER TABLE advance.vendor_payment_allocations OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.guard_vendor_financial_evidence()
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.vendor_financial_command_valid(uuid,uuid,text,uuid,text,text)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.record_vendor_advance(
   uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.reverse_vendor_advance(
   uuid,uuid,text,text,text,uuid,text,uuid,text,text)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.adjust_vendor_advances_for_bill()
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.record_vendor_payment(
   uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.payment_due_date(timestamptz,text)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.vendor_advance_json(uuid,uuid,boolean)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.vendor_payment_json(uuid,uuid,boolean)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.list_vendor_advance_purchase_orders(uuid,uuid)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.list_vendor_payments(uuid,uuid,integer,integer)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.list_vendor_payables(uuid,uuid,boolean)
   OWNER TO nexa_erp_owner;
  ALTER FUNCTION advance.list_vendor_positions(uuid)
   OWNER TO nexa_erp_owner;
 END IF;
 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_runtime') THEN
  REVOKE ALL ON TABLE
   advance.vendor_advances,advance.vendor_advance_reversals,
   advance.vendor_advance_adjustments,
   advance.vendor_advance_adjustment_restorations,
   advance.vendor_payments,advance.vendor_payment_allocations
  FROM nexa_erp_runtime;
  GRANT EXECUTE ON FUNCTION
   advance.record_vendor_advance(
    uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text),
   advance.reverse_vendor_advance(
    uuid,uuid,text,text,text,uuid,text,uuid,text,text),
   advance.record_vendor_payment(
    uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text),
   advance.vendor_advance_json(uuid,uuid,boolean),
   advance.vendor_payment_json(uuid,uuid,boolean),
   advance.list_vendor_advance_purchase_orders(uuid,uuid),
   advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer),
   advance.list_vendor_payments(uuid,uuid,integer,integer),
   advance.list_vendor_payables(uuid,uuid,boolean),
   advance.list_vendor_positions(uuid)
  TO nexa_erp_runtime;
 END IF;
END $acl$;
""";

    internal const string Down = """
DROP TRIGGER IF EXISTS trg_vendor_bill_advance_adjustment ON advance.vendor_bills;
DROP FUNCTION IF EXISTS advance.list_vendor_positions(uuid);
DROP FUNCTION IF EXISTS advance.list_vendor_payables(uuid,uuid,boolean);
DROP FUNCTION IF EXISTS advance.list_vendor_payments(uuid,uuid,integer,integer);
DROP FUNCTION IF EXISTS advance.list_vendor_advances(uuid,uuid,uuid,boolean,integer,integer);
DROP FUNCTION IF EXISTS advance.list_vendor_advance_purchase_orders(uuid,uuid);
DROP FUNCTION IF EXISTS advance.vendor_payment_json(uuid,uuid,boolean);
DROP FUNCTION IF EXISTS advance.vendor_advance_json(uuid,uuid,boolean);
DROP FUNCTION IF EXISTS advance.payment_due_date(timestamptz,text);
DROP FUNCTION IF EXISTS advance.record_vendor_payment(
 uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text);
DROP FUNCTION IF EXISTS advance.adjust_vendor_advances_for_bill();
DROP FUNCTION IF EXISTS advance.reverse_vendor_advance(
 uuid,uuid,text,text,text,uuid,text,uuid,text,text);
DROP FUNCTION IF EXISTS advance.record_vendor_advance(
 uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text);
DROP FUNCTION IF EXISTS advance.vendor_financial_command_valid(
 uuid,uuid,text,uuid,text,text);
DROP TRIGGER IF EXISTS trg_vendor_payment_allocations_immutable
 ON advance.vendor_payment_allocations;
DROP TRIGGER IF EXISTS trg_vendor_payments_immutable ON advance.vendor_payments;
DROP TRIGGER IF EXISTS trg_vendor_advance_restorations_immutable
 ON advance.vendor_advance_adjustment_restorations;
DROP TRIGGER IF EXISTS trg_vendor_advance_adjustments_immutable
 ON advance.vendor_advance_adjustments;
DROP TRIGGER IF EXISTS trg_vendor_advance_reversals_immutable
 ON advance.vendor_advance_reversals;
DROP TRIGGER IF EXISTS trg_vendor_advances_immutable ON advance.vendor_advances;
DROP FUNCTION IF EXISTS advance.guard_vendor_financial_evidence();
DROP TABLE IF EXISTS advance.vendor_payment_allocations;
DROP TABLE IF EXISTS advance.vendor_payments;
DROP TABLE IF EXISTS advance.vendor_advance_adjustment_restorations;
DROP TABLE IF EXISTS advance.vendor_advance_adjustments;
DROP TABLE IF EXISTS advance.vendor_advance_reversals;
DROP TABLE IF EXISTS advance.vendor_advances;
DELETE FROM advance.role_page_permissions
 WHERE "CreatedBy"='VendorAdvanceAndPaymentEvidence';
DELETE FROM advance.page_definitions
 WHERE "CreatedBy"='VendorAdvanceAndPaymentEvidence';
""";
}
