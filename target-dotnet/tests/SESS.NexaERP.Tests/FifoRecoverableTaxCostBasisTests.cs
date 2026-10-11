using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void F32_migration_round_trip_preserves_function_security_and_refuses_financial_evidence()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        Assert.False(model.Database.HasPendingModelChanges());
        var migrator = model.GetService<IMigrator>();
        const string before = "20261006150000_R1VendorOwnerWorkflowGrants";
        const string after = "20261011100000_FifoRecoverableTaxCostBasis";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("f32-predecessor.sql", migrator.GenerateScript("0", before) + """
            CREATE TABLE public.f32_security_before AS
            SELECT oid,proowner,proacl,prosecdef,proconfig FROM pg_proc WHERE oid IN
              ('advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)'::regprocedure,
               'advance.allocate_vendor_bill_landed_cost()'::regprocedure);
            """);
        server.Execute("f32-up.sql", migrator.GenerateScript(before, after) + F32SecurityAssertions);
        server.Execute("f32-down.sql", migrator.GenerateScript(after, before) + F32SecurityAssertions);
        server.Execute("f32-reapply.sql", migrator.GenerateScript(before, after) + F32SecurityAssertions);
        // Execute the actual generated Down against a disposable evidence probe.
        using var connection = new NpgsqlConnection(server.ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using (var probe = new NpgsqlCommand("ALTER TABLE advance.fifo_inventory_cost_layers RENAME TO f32_original_layers; CREATE TABLE advance.fifo_inventory_cost_layers AS SELECT 1 AS evidence;", connection, transaction))
            probe.ExecuteNonQuery();
        using (var down = new NpgsqlCommand(migrator.GenerateScript(after, before), connection, transaction))
        {
            var refused = Assert.Throws<PostgresException>(() => down.ExecuteNonQuery());
            Assert.Equal("P0001", refused.SqlState);
            Assert.Contains("F32 rollback refuses existing FIFO financial evidence", refused.MessageText);
        }
        transaction.Rollback();
    }

    private const string F32SecurityAssertions = """
        DO $assert$ BEGIN
          IF EXISTS(SELECT 1 FROM public.f32_security_before b JOIN pg_proc p ON p.oid=b.oid
            WHERE (p.proowner,p.proacl,p.prosecdef,p.proconfig) IS DISTINCT FROM
              (b.proowner,b.proacl,b.prosecdef,b.proconfig)) THEN
            RAISE EXCEPTION 'F32 changed installed ownership, permissions or search_path';
          END IF;
        END $assert$;
        """;

    [Theory]
    [InlineData("FULLY_RECOVERABLE", null, 1500)]
    [InlineData("BLOCKED", null, 1770)]
    [InlineData("PARTIALLY_RECOVERABLE", 50, 1635)]
    [InlineData("PARTIALLY_RECOVERABLE", 75, 1567.5)]
    public void F32_new_grn_layers_use_captured_itc_and_keep_legacy_layers(string eligibility,
        int? recoverablePercent, decimal expected)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("f32-layer-fixture.sql", """
            CREATE SCHEMA advance;
            CREATE ROLE nexa_erp_runtime LOGIN;
            CREATE TABLE advance.companies("Id" uuid,"Code" text);
            CREATE TABLE advance.goods_receipts("Id" uuid,"CompanyId" uuid,"ReceivedAt" timestamptz,"Status" text,"DocumentKind" text);
            CREATE TABLE advance.goods_receipt_lines("Id" uuid,"CompanyId" uuid,"GoodsReceiptId" uuid,"PurchaseOrderLineId" uuid,"ItemId" uuid,"ReceivedQuantity" numeric);
            CREATE TABLE advance.purchase_order_lines("Id" uuid,"CompanyId" uuid,"UnitRate" numeric,"TotalPayableValue" numeric,"OrderedQuantity" numeric,"CommercialSnapshotJson" jsonb,"TaxRuleSnapshotJson" jsonb);
            CREATE TABLE advance.vendor_bill_lines("Id" uuid,"CompanyId" uuid,"PurchaseOrderLineId" uuid,"BilledUnitRate" numeric,"BilledQuantity" numeric);
            CREATE TABLE advance.vendor_bill_charge_allocations("CompanyId" uuid,"VendorBillLineId" uuid,"AllocatedChargeValue" numeric);
            CREATE TABLE advance.fifo_inventory_cost_layers("Id" uuid,"CompanyId" uuid,"GoodsReceiptLineId" uuid,"ItemId" uuid,"QuantityReceived" numeric,"UnitCost" numeric,"LayerValue" numeric,"ReceivedAt" timestamptz,"CostBasis" text,"CreatedAt" timestamptz,"CreatedBy" text,UNIQUE("CompanyId","GoodsReceiptLineId"));
            CREATE FUNCTION advance.ordinary_command_context_valid(text,uuid,text,text,text) RETURNS boolean LANGUAGE sql AS 'SELECT $2=''00000000-0000-0000-0000-000000000001''::uuid AND $1=''PVT''';
            CREATE FUNCTION advance.resolve_employee_role_authority(uuid,uuid,date,text,text[]) RETURNS TABLE("AssignmentId" uuid,"AssignmentType" text) LANGUAGE sql AS 'SELECT ''00000000-0000-0000-0000-000000000001''::uuid,''FULL''::text';
            INSERT INTO advance.companies VALUES('00000000-0000-0000-0000-000000000010','PVT');
            INSERT INTO advance.goods_receipts VALUES('00000000-0000-0000-0000-000000000020','00000000-0000-0000-0000-000000000010',now(),'FINALIZED','NORMAL');
            INSERT INTO advance.goods_receipt_lines VALUES('00000000-0000-0000-0000-000000000030','00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000020','00000000-0000-0000-0000-000000000040','00000000-0000-0000-0000-000000000050',10);
            INSERT INTO advance.fifo_inventory_cost_layers VALUES('00000000-0000-0000-0000-000000000070','00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000080','00000000-0000-0000-0000-000000000050',10,1770,17700,now(),'PO_PROVISIONAL_IDENTICAL',now(),'OLD_DEMO');
            """);
        server.Execute("f32-tax-function.sql", ActualBomLandedRateValuation.RateFunctions);
        var start = VendorBillCostingSql.Up.IndexOf("CREATE FUNCTION advance.create_fifo_layers_for_grn", StringComparison.Ordinal);
        var end = VendorBillCostingSql.Up.IndexOf("CREATE FUNCTION advance.consume_fifo_for_issue", start, StringComparison.Ordinal);
        server.Execute("f32-installed-layer.sql", VendorBillCostingSql.Up[start..end]);
        server.Execute("f32-prospective-rewrite.sql", InstalledFunctionSql.Rewrite(FifoRecoverableTaxCostBasis.LayerSignature,
            (FifoRecoverableTaxCostBasis.BeforeLayer, FifoRecoverableTaxCostBasis.AfterLayer, 2)));
        var partial = recoverablePercent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        server.Execute("f32-snapshot.sql", $$$"""
            INSERT INTO advance.purchase_order_lines VALUES('00000000-0000-0000-0000-000000000040','00000000-0000-0000-0000-000000000010',1500,17700,10,
              '{"result":{"taxableValue":15000,"cgstValue":1350,"sgstValue":1350,"igstValue":0,"cessValue":0,"roundOff":0}}',
              '{"itcEligibility":"{{{eligibility}}}","recoverableTaxPercent":{{{partial}}}}');
            """);
        var call = "advance.create_fifo_layers_for_grn('00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000020','00000000-0000-0000-0000-000000000001','STORES_ASSISTANT','00000000-0000-0000-0000-000000000001','FULL','F32')";
        server.Execute("f32-layer-assertions.sql", FormattableString.Invariant($$"""
            DO $assert$ BEGIN
              IF {{call}}<>1 THEN RAISE EXCEPTION 'Expected one new receipt layer'; END IF;
              IF {{call}}<>0 THEN RAISE EXCEPTION 'Receipt replay duplicated layer'; END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers WHERE "CreatedBy"='F32' AND "UnitCost"={{expected}} AND "LayerValue"={{expected*10}}) THEN RAISE EXCEPTION 'Incorrect captured ITC cost'; END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers WHERE "CreatedBy"='OLD_DEMO' AND "UnitCost"=1770 AND "LayerValue"=17700) THEN RAISE EXCEPTION 'Historical evidence was revalued'; END IF;
              IF (SELECT "TotalPayableValue" FROM advance.purchase_order_lines)<>17700 THEN RAISE EXCEPTION 'Financial payable changed'; END IF;
            END $assert$;
            """));
        server.Execute("f32-allocation-fixture.sql", """
            ALTER TABLE advance.vendor_bill_lines ADD "VendorBillId" uuid, ADD "LineNumber" integer,
              ADD "GoodsReceiptLineId" uuid, ADD "BilledPayableValue" numeric, ADD "VerifiedGrossWeightKg" numeric;
            ALTER TABLE advance.vendor_bill_charge_allocations ADD "Id" uuid,ADD "VendorBillChargeId" uuid,
              ADD "BasisValue" numeric,ADD "CreatedAt" timestamptz,ADD "CreatedBy" text;
            CREATE TABLE advance.vendor_bills("Id" uuid,"CompanyId" uuid,"Status" text,"UpdatedBy" text,"CreatedBy" text);
            CREATE TABLE advance.vendor_bill_charges("Id" uuid,"CompanyId" uuid,"VendorBillId" uuid,"IncludedInInventoryCost" boolean,"ChargeNumber" integer,"AllocationBasis" text,"ChargeValue" numeric);
            CREATE TABLE advance.fifo_cost_consumptions("CompanyId" uuid,"FifoInventoryCostLayerId" uuid,"Quantity" numeric);
            CREATE TABLE advance.fifo_landed_cost_adjustments("Id" uuid,"CompanyId" uuid,"VendorBillLineId" uuid,"FifoInventoryCostLayerId" uuid,"ProvisionalUnitRate" numeric,"LandedUnitRate" numeric,"AllocatedChargeValue" numeric,"ConsumedQuantityAtAcceptance" numeric,"RemainingQuantityAtAcceptance" numeric,"ConsumedCostAdjustmentValue" numeric,"RemainingStockAdjustmentValue" numeric,"CreatedAt" timestamptz,"CreatedBy" text);
            CREATE TABLE advance.actual_bom_entries("Id" uuid,"CompanyId" uuid,"GoodsReceiptLineId" uuid,"EntryKind" text,"VendorBillLineId" uuid,"ComponentFitmentId" uuid,"QuantityBase" numeric);
            CREATE TABLE advance.component_fitment_reversals("Id" uuid,"CompanyId" uuid,"ComponentFitmentId" uuid);
            CREATE TABLE advance.actual_bom_valuation_adjustments("Id" uuid,"CompanyId" uuid,"ActualBomEntryId" uuid,"VendorBillLineId" uuid,"AcceptedMaterialValue" numeric,"AllocatedChargeValue" numeric,"TotalAcceptedValue" numeric,"CreatedAt" timestamptz,"CreatedBy" text);
            INSERT INTO advance.vendor_bills VALUES('00000000-0000-0000-0000-000000000090','00000000-0000-0000-0000-000000000010','DRAFT','F32','F32');
            INSERT INTO advance.vendor_bill_lines("Id","CompanyId","PurchaseOrderLineId","BilledUnitRate","BilledQuantity","VendorBillId","LineNumber","GoodsReceiptLineId","BilledPayableValue")
            VALUES('00000000-0000-0000-0000-000000000091','00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000040',1500,10,'00000000-0000-0000-0000-000000000090',1,'00000000-0000-0000-0000-000000000030',17700);
            INSERT INTO advance.vendor_bill_charges VALUES
              ('00000000-0000-0000-0000-000000000092','00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000090',true,1,'ITEM_VALUE',30),
              ('00000000-0000-0000-0000-000000000093','00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000090',false,2,'EXCLUDED',54);
            INSERT INTO advance.fifo_cost_consumptions SELECT "CompanyId","Id",2 FROM advance.fifo_inventory_cost_layers WHERE "CreatedBy"='F32';
            """);
        start = ImmutableLandedCostAdjustmentsSql.Up.IndexOf("CREATE FUNCTION advance.allocate_vendor_bill_landed_cost()", StringComparison.Ordinal);
        end = ImmutableLandedCostAdjustmentsSql.Up.IndexOf("CREATE OR REPLACE FUNCTION advance.refresh_item_company_last_purchase", start, StringComparison.Ordinal);
        server.Execute("f32-installed-allocator.sql", ImmutableLandedCostAdjustmentsSql.Up[start..end]);
        server.Execute("f32-allocator-rewrite.sql", InstalledFunctionSql.Rewrite(FifoRecoverableTaxCostBasis.AllocationSignature,
            (FifoRecoverableTaxCostBasis.BeforeAllocation, FifoRecoverableTaxCostBasis.AfterAllocation)));
        server.Execute("f32-accept-and-assert.sql", FormattableString.Invariant($$"""
            SELECT set_config('sess.vendor_bill_write',txid_current()::text,false);
            BEGIN;
            SELECT set_config('sess.vendor_bill_write',txid_current()::text,true);
            UPDATE advance.vendor_bills SET "Status"='ACCEPTED';
            DO $assert$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM advance.fifo_landed_cost_adjustments WHERE "ProvisionalUnitRate"={{expected}}
                AND "LandedUnitRate"={{expected+3}} AND "AllocatedChargeValue"=30
                AND "ConsumedQuantityAtAcceptance"=2 AND "RemainingQuantityAtAcceptance"=8
                AND "ConsumedCostAdjustmentValue"=6 AND "RemainingStockAdjustmentValue"=24) THEN
                RAISE EXCEPTION 'Landed cost must add included inward charges once and split consumed/remaining values';
              END IF;
              IF (SELECT sum("AllocatedChargeValue") FROM advance.vendor_bill_charge_allocations)<>30 THEN
                RAISE EXCEPTION 'Recoverable GST was allocated or freight duplicated';
              END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers WHERE "CreatedBy"='OLD_DEMO' AND "UnitCost"=1770 AND "LayerValue"=17700) THEN
                RAISE EXCEPTION 'Acceptance rewrote old DEMO evidence';
              END IF;
              IF advance.vendor_bill_inventory_landed_rate('00000000-0000-0000-0000-000000000010','00000000-0000-0000-0000-000000000091')<>{{expected+3}} THEN
                RAISE EXCEPTION 'Bill reader and FIFO adjustment valuation differ';
              END IF;
            END $assert$;
            COMMIT;
            """));
        using var owner = new NpgsqlConnection(server.ConnectionString);
        owner.Open();
        using var denied = new NpgsqlCommand("SELECT " + call.Replace("'00000000-0000-0000-0000-000000000001','STORES_ASSISTANT'", "'00000000-0000-0000-0000-000000000099','STORES_ASSISTANT'", StringComparison.Ordinal), owner);
        Assert.Equal("42501", Assert.Throws<PostgresException>(() => denied.ExecuteScalar()).SqlState);
    }
}