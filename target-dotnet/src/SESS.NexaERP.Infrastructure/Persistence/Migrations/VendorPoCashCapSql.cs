namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class VendorPoCashCapSql
{
    internal const string CashLock =
        " PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':VENDOR-CASH',0));\n";
    internal const string TotalsSignature = "advance.vendor_po_cash_totals(uuid,uuid,text,uuid)";
    internal const string LimitSignature = "advance.require_vendor_po_cash_limit(uuid,uuid,numeric,text)";
    internal const string TriggerSignature = "advance.guard_purchase_order_vendor_cash()";
    internal const string AdvanceSignature = "advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)";
    internal const string PaymentSignature = "advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)";
    internal const string OptionsSignature = "advance.list_vendor_advance_purchase_orders(uuid,uuid)";

    internal const string Create = """
CREATE INDEX "IX_vendor_advances_company_po" ON advance.vendor_advances("CompanyId","PurchaseOrderId");

CREATE FUNCTION advance.vendor_po_cash_totals(
 p_company uuid,p_root uuid,p_currency text,p_vendor uuid)
RETURNS TABLE("AdvanceAmount" numeric,"BillPaymentAmount" numeric)
LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
BEGIN
 IF p_company IS NULL OR p_root IS NULL OR p_vendor IS NULL OR length(coalesce(p_currency,''))<>3 THEN
  RAISE EXCEPTION 'Vendor cash totals require a company, PO root, vendor and currency.';
 END IF;
 IF EXISTS(
  SELECT 1 FROM advance.vendor_advances a JOIN advance.purchase_orders po ON po."Id"=a."PurchaseOrderId"
  WHERE a."CompanyId"=p_company AND po."CompanyId"=p_company AND po."RootPurchaseOrderId"=p_root
   AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
    WHERE r."CompanyId"=p_company AND r."VendorAdvanceId"=a."Id")
   AND(a."CurrencyCode"<>p_currency OR po."CurrencyCode"<>p_currency
    OR a."VendorId"<>p_vendor OR po."VendorId"<>p_vendor))
 OR EXISTS(
  SELECT 1 FROM advance.vendor_payment_allocations a
  JOIN advance.vendor_payments p ON p."Id"=a."VendorPaymentId" AND p."CompanyId"=p_company
  JOIN advance.vendor_bills b ON b."Id"=a."VendorBillId" AND b."CompanyId"=p_company
  JOIN advance.purchase_orders po ON po."Id"=b."PurchaseOrderId" AND po."CompanyId"=p_company
  WHERE a."CompanyId"=p_company AND po."RootPurchaseOrderId"=p_root
   AND(p."CurrencyCode"<>p_currency OR po."CurrencyCode"<>p_currency
    OR p."VendorId"<>p_vendor OR b."VendorId"<>p_vendor OR po."VendorId"<>p_vendor)) THEN
  RAISE EXCEPTION 'Vendor cash history does not match the Purchase Order currency or vendor. Accounts review is required.';
 END IF;
 RETURN QUERY
 WITH versions AS MATERIALIZED(
  SELECT po."Id" FROM advance.purchase_orders po
  WHERE po."CompanyId"=p_company AND po."RootPurchaseOrderId"=p_root)
 SELECT
  coalesce((SELECT sum(a."Amount") FROM advance.vendor_advances a
   WHERE a."CompanyId"=p_company AND a."PurchaseOrderId" IN(SELECT "Id" FROM versions)
    AND NOT EXISTS(SELECT 1 FROM advance.vendor_advance_reversals r
     WHERE r."CompanyId"=p_company AND r."VendorAdvanceId"=a."Id")),0),
  coalesce((SELECT sum(a."Amount") FROM advance.vendor_payment_allocations a
   JOIN advance.vendor_bills b ON b."Id"=a."VendorBillId" AND b."CompanyId"=p_company
   WHERE a."CompanyId"=p_company AND b."PurchaseOrderId" IN(SELECT "Id" FROM versions)),0);
END $f$;

CREATE FUNCTION advance.require_vendor_po_cash_limit(
 p_company uuid,p_root uuid,p_amount numeric,p_currency text)
RETURNS void LANGUAGE plpgsql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE po advance.purchase_orders%ROWTYPE; cash record;
BEGIN
 IF current_setting('transaction_isolation')<>'serializable' THEN
  RAISE EXCEPTION 'Vendor cash changes and PO issuance require a serializable transaction.';
 END IF;
 IF p_amount IS NULL OR p_amount<=0 THEN RAISE EXCEPTION 'Positive vendor cash amount is required.'; END IF;
 SELECT * INTO po FROM advance.purchase_orders
 WHERE "CompanyId"=p_company AND "RootPurchaseOrderId"=p_root AND "IssuedAt" IS NOT NULL
 ORDER BY "RevisionNumber" DESC LIMIT 1;
 IF NOT FOUND THEN RAISE EXCEPTION 'Vendor cash requires an issued Purchase Order version.'; END IF;
 IF po."CurrencyCode" IS DISTINCT FROM upper(btrim(p_currency)) THEN
  RAISE EXCEPTION 'Vendor cash currency must match the latest issued Purchase Order.';
 END IF;
 SELECT * INTO cash FROM advance.vendor_po_cash_totals(p_company,p_root,po."CurrencyCode",po."VendorId");
 IF cash."AdvanceAmount"+cash."BillPaymentAmount"+p_amount>po."TotalPayableValue" THEN
  RAISE EXCEPTION 'Total vendor cash % exceeds Purchase Order value %.',
   cash."AdvanceAmount"+cash."BillPaymentAmount"+p_amount,po."TotalPayableValue";
 END IF;
END $f$;

CREATE FUNCTION advance.guard_purchase_order_vendor_cash()
RETURNS trigger LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
DECLARE cash record;
BEGIN
 IF NEW."Status"<>'Issued' THEN RETURN NEW; END IF;
 IF TG_OP='UPDATE' AND OLD."Status"='Issued'
   AND NEW."CompanyId"=OLD."CompanyId" AND NEW."RootPurchaseOrderId"=OLD."RootPurchaseOrderId"
   AND NEW."CurrencyCode"=OLD."CurrencyCode" AND NEW."VendorId"=OLD."VendorId"
   AND NEW."TotalPayableValue"=OLD."TotalPayableValue" THEN RETURN NEW; END IF;
 IF current_setting('transaction_isolation')<>'serializable' THEN
  RAISE EXCEPTION 'Vendor cash changes and PO issuance require a serializable transaction.';
 END IF;
 IF NEW."Status"='Issued' THEN
  PERFORM pg_advisory_xact_lock(hashtextextended(NEW."CompanyId"::text||':VENDOR-CASH',0));
  SELECT * INTO cash FROM advance.vendor_po_cash_totals(
   NEW."CompanyId",NEW."RootPurchaseOrderId",NEW."CurrencyCode",NEW."VendorId");
  IF cash."AdvanceAmount"+cash."BillPaymentAmount">NEW."TotalPayableValue" THEN
   RAISE EXCEPTION USING ERRCODE='23514',CONSTRAINT='CK_vendor_po_cash_cap',
    MESSAGE='Issued Purchase Order value cannot be below already paid vendor cash.';
  END IF;
 END IF;
 RETURN NEW;
END $f$;

CREATE TRIGGER trg_purchase_order_vendor_cash BEFORE INSERT OR UPDATE ON advance.purchase_orders
 FOR EACH ROW EXECUTE FUNCTION advance.guard_purchase_order_vendor_cash();
""";

    internal static string AdvanceBefore => VendorBankAdviceMigrationSql.AdvanceAfter;
    internal static string PaymentBefore => VendorBankAdviceMigrationSql.PaymentAfter;
    internal static string OptionsBefore => Extract(VendorAdvancePaymentSql.Up, "list_vendor_advance_purchase_orders");

    internal static string AdvanceAfter
    {
        get
        {
            var sql = ReplaceOnce(AdvanceBefore, " SELECT * INTO po FROM advance.purchase_orders",
                CashLock + " SELECT * INTO po FROM advance.purchase_orders");
            sql = ReplaceOnce(sql, "AND \"IsCurrentVersion\" FOR UPDATE;", "AND \"IsCurrentVersion\";");
            var start = sql.IndexOf(" SELECT coalesce(sum(a.\"Amount\"),0) INTO used", StringComparison.Ordinal);
            var end = sql.IndexOf(" PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':ADV',0));", StringComparison.Ordinal);
            if (start < 0 || end <= start) throw new InvalidOperationException("Advance cash limit anchor changed.");
            return sql[..start] + " PERFORM advance.require_vendor_po_cash_limit(p_company,po.\"RootPurchaseOrderId\",p_amount,p_currency);\n" + sql[end..];
        }
    }

    internal static string PaymentAfter
    {
        get
        {
            var sql = ReplaceOnce(PaymentBefore, " paid numeric; adjusted numeric;",
                " cash record; paid numeric; adjusted numeric;");
            sql = ReplaceOnce(sql, " FOR j IN SELECT", CashLock + " FOR j IN SELECT");
            const string guard = """
             FOR cash IN
              SELECT cash_po."RootPurchaseOrderId" root,sum((allocation_entry.value->>'amount')::numeric) amount
              FROM jsonb_array_elements(p_lines) allocation_entry(value)
              JOIN advance.vendor_bills cash_bill ON cash_bill."Id"=(allocation_entry.value->>'vendorBillId')::uuid AND cash_bill."CompanyId"=p_company
              JOIN advance.purchase_orders cash_po ON cash_po."Id"=cash_bill."PurchaseOrderId" AND cash_po."CompanyId"=p_company
              GROUP BY cash_po."RootPurchaseOrderId" ORDER BY cash_po."RootPurchaseOrderId"
             LOOP
              PERFORM advance.require_vendor_po_cash_limit(p_company,cash.root,cash.amount,p_currency);
             END LOOP;

            """;
            return ReplaceOnce(sql, " PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':PAY',0));",
                guard + " PERFORM pg_advisory_xact_lock(hashtextextended(p_company::text||':PAY',0));");
        }
    }

    internal const string OptionsAfter = """
CREATE OR REPLACE FUNCTION advance.list_vendor_advance_purchase_orders(p_company uuid,p_vendor uuid)
RETURNS jsonb LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,advance AS $f$
WITH q AS(
 SELECT po."Id" pid,po."PoNumber" pn,po."VendorId" vid,v."VendorCode" vc,v."Name" vn,
  po."CurrencyCode" currency,po."TotalPayableValue" po_value,
  cash."AdvanceAmount" advanced,cash."BillPaymentAmount" paid
 FROM advance.purchase_orders po JOIN advance.vendors v ON v."Id"=po."VendorId"
 CROSS JOIN LATERAL advance.vendor_po_cash_totals(p_company,po."RootPurchaseOrderId",po."CurrencyCode",po."VendorId") cash
 WHERE po."CompanyId"=p_company AND po."IsCurrentVersion" AND po."Status"='Issued'
  AND(p_vendor IS NULL OR po."VendorId"=p_vendor))
SELECT coalesce(jsonb_agg(jsonb_build_object(
 'purchaseOrderId',pid,'purchaseOrderNumber',pn,'vendorId',vid,
 'vendorCode',vc,'vendorName',vn,'currencyCode',currency,'purchaseOrderValue',po_value,
 'activeAdvanceAmount',advanced,'billPaymentAmount',paid,'availableAdvanceAmount',po_value-advanced-paid)
 ORDER BY pn),'[]'::jsonb)
FROM q WHERE po_value-advanced-paid>0;
$f$;
""";

    internal static string Extract(string source, string name)
    {
        source = source.Replace("\r\n", "\n");
        var marker = "CREATE FUNCTION advance." + name + "(";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Financial function definition was not found.");
        // SQL-language readers finish with a standalone delimiter.
        var first = source.IndexOf("$f$", start, StringComparison.Ordinal);
        var end = source.IndexOf("$f$;", first + 3, StringComparison.Ordinal) + 4;
        if (first < 0 || end <= first) throw new InvalidOperationException("Financial function delimiter was not found.");
        return source[start..end].Replace(marker, "CREATE OR REPLACE FUNCTION advance." + name + "(", StringComparison.Ordinal);
    }

    private static string ReplaceOnce(string source, string marker, string replacement)
    {
        source = source.Replace("\r\n", "\n");
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(marker, start + marker.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException("Financial cash-limit anchor changed.");
        return source[..start] + replacement.Replace("\r\n", "\n") + source[(start + marker.Length)..];
    }
}
