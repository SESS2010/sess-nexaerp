using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("FULLY_RECOVERABLE", null, false, 0, 1250)]
    [InlineData("FULLY_RECOVERABLE", null, true, 0, 1250)]
    [InlineData("FULLY_RECOVERABLE", null, false, 50, 1300)]
    [InlineData("FULLY_RECOVERABLE", null, false, 175, 1425)]
    [InlineData("BLOCKED", null, false, 0, 1475)]
    [InlineData("BLOCKED", null, true, 175, 1650)]
    [InlineData("PARTIALLY_RECOVERABLE", 50, false, 0, 1362.5)]
    [InlineData("PARTIALLY_RECOVERABLE", 75, true, 175, 1481.25)]
    public void Actual_bom_uses_landed_rate_for_existing_and_prebill_entries(string eligibility,
        int? recoverablePercent, bool reverseCharge, decimal capitalizedCharges, decimal landedRate)
    {
        // Minimal synthetic reader fixture, not a deployed migration-chain witness.
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("landed-rate-reader-fixture.sql", """
            CREATE SCHEMA advance;
            CREATE ROLE nexa_erp_runtime LOGIN;
            GRANT USAGE ON SCHEMA advance TO nexa_erp_runtime;
            CREATE TABLE advance.actual_bom_entries(
              "Id" uuid,"CompanyId" uuid,"ActualBomId" uuid,"GoodsReceiptLineId" uuid,"EntryKind" text,
              "QuantityBase" numeric,"AcceptedMaterialValue" numeric,"AllocatedChargeValue" numeric,"TotalAcceptedValue" numeric);
            CREATE TABLE advance.vendor_bill_lines("Id" uuid,"CompanyId" uuid,"VendorBillId" uuid,"GoodsReceiptLineId" uuid,"BilledPayableValue" numeric,"BilledQuantity" numeric,"BilledUnitRate" numeric,"PurchaseOrderLineId" uuid);
            CREATE TABLE advance.purchase_order_lines("Id" uuid,"CompanyId" uuid,"OrderedQuantity" numeric,"CommercialSnapshotJson" jsonb,"TaxRuleSnapshotJson" jsonb);
            CREATE TABLE advance.vendor_bill_charge_allocations("CompanyId" uuid,"VendorBillLineId" uuid,"AllocatedChargeValue" numeric);
            CREATE TABLE advance.vendor_bills("Id" uuid,"CompanyId" uuid,"BillNumber" text,"Status" text,"DecidedAt" timestamptz);
            CREATE TABLE advance.fifo_landed_cost_adjustments("CompanyId" uuid,"VendorBillLineId" uuid,"FifoInventoryCostLayerId" uuid,
              "LandedUnitRate" numeric,"AllocatedChargeValue" numeric,"CreatedAt" timestamptz);
            CREATE TABLE advance.fifo_inventory_cost_layers("Id" uuid,"CompanyId" uuid,"QuantityReceived" numeric);
            INSERT INTO advance.vendor_bills VALUES('00000000-0000-0000-0000-000000000001',
              '00000000-0000-0000-0000-000000000010','BILL-1475','ACCEPTED',now());
            INSERT INTO advance.vendor_bill_lines VALUES('00000000-0000-0000-0000-000000000002',
              '00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000001',
              '00000000-0000-0000-0000-000000000003',2950,2,1250,'00000000-0000-0000-0000-000000000005');
            INSERT INTO advance.fifo_inventory_cost_layers VALUES('00000000-0000-0000-0000-000000000004',
              '00000000-0000-0000-0000-000000000010',1);
            INSERT INTO advance.actual_bom_entries
            SELECT ('00000000-0000-0000-0000-'||lpad(n::text,12,'0'))::uuid,
              '00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000020',
              '00000000-0000-0000-0000-000000000003','FITMENT',quantity,old_value,0,old_value
            FROM (VALUES(101,1,1475),(102,1,0),(103,0.25,368.75)) v(n,quantity,old_value);
            """);
        var capturedCommercial = System.Text.Json.JsonSerializer.Serialize(new
        { result = new { taxableValue=2500m, cgstValue=225m, sgstValue=225m, igstValue=0m, cessValue=0m, roundOff=0m } });
        var capturedTax = System.Text.Json.JsonSerializer.Serialize(new
        {
            vendorRegistrationType = "REGULAR", isReverseCharge = reverseCharge,
            itcEligibility = eligibility, recoverableTaxPercent = recoverablePercent
        });
        server.Execute("captured-tax-rule.sql", FormattableString.Invariant($"""
            INSERT INTO advance.purchase_order_lines VALUES('00000000-0000-0000-0000-000000000005',
              '00000000-0000-0000-0000-000000000010',2,
              '{capturedCommercial}',
              '{capturedTax}');
            INSERT INTO advance.vendor_bill_charge_allocations VALUES('00000000-0000-0000-0000-000000000010',
              '00000000-0000-0000-0000-000000000002',{capitalizedCharges*2});
            -- Deliberately retain the old, tax-inclusive immutable FIFO rate.
            INSERT INTO advance.fifo_landed_cost_adjustments VALUES(
              '00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000002',
              '00000000-0000-0000-0000-000000000004',{1475+capitalizedCharges},{capitalizedCharges*2},now());
            """));
        server.Execute("landed-rate-functions.sql", ActualBomLandedRateValuation.RateFunctions);
        server.Execute("landed-rate-projection.sql", ActualBomLandedRateValuation.Projection);
        server.Execute("landed-rate-assertions.sql", FormattableString.Invariant($"""
            SET SESSION AUTHORIZATION nexa_erp_runtime;
            DO $assert$
            DECLARE rows jsonb; row jsonb; original numeric; quantity numeric; target numeric;
            BEGIN
              rows:=advance.get_actual_bom_landed_valuations('00000000-0000-0000-0000-000000000010',
                '00000000-0000-0000-0000-000000000020');
              IF jsonb_array_length(rows)<>3 THEN RAISE EXCEPTION 'Expected exactly three fitment projections'; END IF;
              FOR row IN SELECT value FROM jsonb_array_elements(rows) LOOP
                quantity:=CASE WHEN row->>'actualBomEntryId'='00000000-0000-0000-0000-000000000103' THEN 0.25 ELSE 1 END;
                original:=CASE row->>'actualBomEntryId'
                  WHEN '00000000-0000-0000-0000-000000000101' THEN 1475
                  WHEN '00000000-0000-0000-0000-000000000103' THEN 368.75 ELSE 0 END;
                target:={landedRate}*quantity;
                -- Frontend prediction recorded before this correction: one fitted
                -- unit against an estimated two units at 1250 must be -1250.
                IF '{eligibility}'='FULLY_RECOVERABLE' AND {capitalizedCharges}=0 AND row->>'actualBomEntryId'='00000000-0000-0000-0000-000000000101' THEN
                  IF original-2*1250<>-1025 THEN RAISE EXCEPTION 'Old variance fixture changed'; END IF;
                  IF original+(row->>'totalAcceptedValue')::numeric-2*1250<>-1250 THEN
                    RAISE EXCEPTION 'Predicted corrected variance must be -1250: %',row;
                  END IF;
                END IF;
                IF (row->>'totalAcceptedValue')::numeric+original<>target
                  OR (row->>'acceptedMaterialValue')::numeric+original<>({landedRate}-{capitalizedCharges})*quantity
                  OR (row->>'allocatedChargeValue')::numeric<>{capitalizedCharges}*quantity
                THEN RAISE EXCEPTION 'Actual BOM used payable instead of landed rate: %',row; END IF;
              END LOOP;
              IF advance.get_actual_bom_landed_valuations('00000000-0000-0000-0000-000000000099',
                '00000000-0000-0000-0000-000000000020')<>'[]'::jsonb THEN
                RAISE EXCEPTION 'Cross-company valuation leak'; END IF;
            END $assert$;
            RESET SESSION AUTHORIZATION;
            DO $assert$ BEGIN
              IF (SELECT "TotalAcceptedValue" FROM advance.actual_bom_entries WHERE "Id"='00000000-0000-0000-0000-000000000101')<>1475
              THEN RAISE EXCEPTION 'Immutable source evidence was rewritten'; END IF;
            END $assert$;
            """));
    }
}