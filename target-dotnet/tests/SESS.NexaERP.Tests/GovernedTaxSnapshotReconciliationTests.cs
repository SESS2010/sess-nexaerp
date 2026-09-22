using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Comparison_retains_exact_captured_tax_evidence_including_legacy_shape(bool legacy)
    {
        var tax = new SESS.NexaERP.Domain.Purchase.Rev869BTaxRuleSnapshot(Guid.NewGuid(),
            "SESS_PVT_LTD", "IN", "9025", "INTRASTATE", "KA", "KA", "REGULAR",
            18m, 9m, 9m, 0m, 0m, false, false, "INR", 2, new DateOnly(2026, 1, 1), null, "Approved", true);
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        var captured = System.Text.Json.JsonSerializer.SerializeToNode(tax, options)!.AsObject();
        if (legacy) { captured.Remove("itcEligibility"); captured.Remove("recoverableTaxPercent"); }
        var line = new SESS.NexaERP.Domain.Purchase.VendorQuotationLine
        {
            Quantity = 2, UnitRate = 1250, TaxRuleSnapshotJson = captured.ToJsonString(),
            RequestForQuotationLine = new SESS.NexaERP.Domain.Purchase.RequestForQuotationLine { UomSnapshot = "NOS" }
        };
        var quote = new SESS.NexaERP.Domain.Purchase.VendorQuotation { CurrencyCode = "INR" };
        var comparison = new SESS.NexaERP.Domain.Purchase.CommercialComparison { OrganizationId = "SESS_PVT_LTD" };
        var input = new SESS.NexaERP.Domain.Purchase.Rev869BCommercialInput(2, 1250, 0, 0, 0, 0, 0, 9, 9, 0, 0, 0, 2);
        var calculated = (SESS.NexaERP.Domain.Purchase.Rev869BCommercialCalculator.Calculate(input), tax);
        var method = typeof(SESS.NexaERP.Infrastructure.Purchase.EfRev869BPurchaseService).GetMethod(
            "ComparisonSnapshotJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var json = (string)method.Invoke(null, [comparison, quote, line, calculated])!;
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(captured,
            System.Text.Json.Nodes.JsonNode.Parse(json)!["taxRule"]));
    }

    [Fact]
    public void Governed_itc_snapshot_reconciles_and_rejects_altered_or_missing_evidence()
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("itc-reconciliation-fixture.sql", """
            CREATE SCHEMA advance;
            CREATE TABLE advance.vendor_quotation_lines AS SELECT * FROM jsonb_to_record('{"Id": "00000000-0000-0000-0000-000000000001", "VendorQuotationId": "00000000-0000-0000-0000-000000000001", "Quantity": 2, "UnitRate": 1250, "DiscountValue": 0, "HeaderDiscountValue": 0, "PackingForwarding": 0, "Freight": 0, "Insurance": 0, "OtherCharges": 0, "RoundOff": 0, "TaxableValue": 2500, "CgstValue": 225, "SgstValue": 225, "IgstValue": 0, "CessValue": 0, "TotalPayableValue": 2950, "TaxGstSettingId": "00000000-0000-0000-0000-000000000001", "HsnSacCode": "9025", "SupplierStateCode": "KA", "PlaceOfSupplyStateCode": "KA", "VendorRegistrationType": "REGULAR"}') AS x("Id" uuid,"VendorQuotationId" uuid,"Quantity" numeric,"UnitRate" numeric,"DiscountValue" numeric,"HeaderDiscountValue" numeric,"PackingForwarding" numeric,"Freight" numeric,"Insurance" numeric,"OtherCharges" numeric,"RoundOff" numeric,"TaxableValue" numeric,"CgstValue" numeric,"SgstValue" numeric,"IgstValue" numeric,"CessValue" numeric,"TotalPayableValue" numeric,"TaxGstSettingId" uuid,"HsnSacCode" text,"SupplierStateCode" text,"PlaceOfSupplyStateCode" text,"VendorRegistrationType" text);
            CREATE TABLE advance.vendor_quotations AS SELECT * FROM jsonb_to_record('{"Id": "00000000-0000-0000-0000-000000000001", "OrganizationId": "SESS_PVT_LTD", "CurrencyCode": "INR", "ReceivedAt": "2026-09-18"}') AS x("Id" uuid,"OrganizationId" text,"CurrencyCode" text,"ReceivedAt" date);
            CREATE TABLE advance.tax_gst_settings AS SELECT * FROM jsonb_to_record('{"Id": "00000000-0000-0000-0000-000000000001", "JurisdictionCode": "IN", "SupplyType": "INTRASTATE", "GstRate": 18, "CgstRate": 9, "SgstRate": 9, "IgstRate": 0, "CessRate": 0, "IsExempt": false, "IsReverseCharge": false, "CurrencyCode": "INR", "RoundingScale": 2, "EffectiveFrom": "2026-01-01", "EffectiveTo": null, "ApprovalStatus": "Approved", "IsActive": true, "ItcEligibility": "FULLY_RECOVERABLE", "RecoverableTaxPercent": null}') AS x("Id" uuid,"JurisdictionCode" text,"SupplyType" text,"GstRate" numeric,"CgstRate" numeric,"SgstRate" numeric,"IgstRate" numeric,"CessRate" numeric,"IsExempt" boolean,"IsReverseCharge" boolean,"CurrencyCode" text,"RoundingScale" integer,"EffectiveFrom" date,"EffectiveTo" date,"ApprovalStatus" text,"IsActive" boolean,"ItcEligibility" text,"RecoverableTaxPercent" numeric);
            """);
        server.Execute("itc-reconciliation-function.sql", GovernedTaxInputCreditSql.Install);
        server.Execute("itc-reconciliation-assertions.sql", """
            DO $test$
            DECLARE legacy jsonb := '{"id": "00000000-0000-0000-0000-000000000001", "organizationId": "SESS_PVT_LTD", "jurisdictionCode": "IN", "hsnSacCode": "9025", "supplyType": "INTRASTATE", "supplierStateCode": "KA", "placeOfSupplyStateCode": "KA", "vendorRegistrationType": "REGULAR", "gstRate": 18, "cgstRate": 9, "sgstRate": 9, "igstRate": 0, "cessRate": 0, "isExempt": false, "isReverseCharge": false, "currencyCode": "INR", "roundingScale": 2, "effectiveFrom": "2026-01-01", "effectiveTo": null, "approvalStatus": "Approved", "isActive": true}'; commercial jsonb := '{"input": {"quantity": 2, "unitRate": 1250, "discountValue": 0, "packingForwarding": 0, "freight": 0, "insurance": 0, "otherCharges": 0, "cgstRate": 9, "sgstRate": 9, "igstRate": 0, "cessRate": 0, "roundOff": 0, "roundingScale": 2, "headerDiscountValue": 0, "currencyCode": "INR", "exchangeRate": 1}, "result": {"taxableValue": 2500, "discountValue": 0, "cgstValue": 225, "sgstValue": 225, "igstValue": 0, "cessValue": 0, "packingForwarding": 0, "freight": 0, "insurance": 0, "otherCharges": 0, "roundOff": 0, "totalPayableValue": 2950, "grossAmount": 2500, "headerDiscountValue": 0, "assessableValue": 2500, "currencyCode": "INR", "exchangeRate": 1}}'; captured jsonb; state text; recovery numeric;
            BEGIN
             FOREACH state IN ARRAY ARRAY['FULLY_RECOVERABLE','BLOCKED','PARTIALLY_RECOVERABLE'] LOOP
              recovery:=CASE WHEN state='PARTIALLY_RECOVERABLE' THEN 37.5 ELSE NULL END;
              UPDATE advance.tax_gst_settings SET "ItcEligibility"=state,"RecoverableTaxPercent"=recovery;
              captured:=legacy||jsonb_build_object('itcEligibility',state,'recoverableTaxPercent',recovery);
              IF advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial,captured) IS NOT TRUE OR
                 advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial||jsonb_build_object('taxRule',captured),captured) IS NOT TRUE THEN
               RAISE EXCEPTION 'Governed snapshot failed reconciliation: %',state;
              END IF;
              IF advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial,legacy) IS DISTINCT FROM (state='FULLY_RECOVERABLE') THEN
               RAISE EXCEPTION 'Legacy evidence accepted for non-default eligibility or rejected for full credit';
              END IF;
              IF advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial,captured||'{"recoverableTaxPercent":42}') IS NOT FALSE OR
                 advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial,captured||'{"itcEligibility":"UNKNOWN"}') IS NOT FALSE OR
                 advance.rev869b_commercial_snapshot_reconciles('00000000-0000-0000-0000-000000000001',commercial||jsonb_build_object('taxRule',legacy),captured) IS NOT FALSE THEN
               RAISE EXCEPTION 'Altered or inconsistent ITC evidence was accepted';
              END IF;
             END LOOP;
            END $test$;
            """);
    }
}
