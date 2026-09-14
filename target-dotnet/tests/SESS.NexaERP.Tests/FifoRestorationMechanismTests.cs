using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    // A focused database mechanism test. The complete purchase-flow witness
    // separately proves ordinary authorization, stock posting, audit and receipts.
    [Fact]
    public void FifoRestorationUnwindsRecordedCreationOrderWithoutRewritingConsumption()
    {
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("fifo-mechanism-schema.sql",FifoMechanismSchema);
        server.Execute("fifo-mechanism-ledger.sql",FifoReturnRestorationSql.Ledger);
        server.Execute("fifo-mechanism-consumption.sql","""
            BEGIN;
            SELECT set_config('sess.vendor_bill_write',txid_current()::text,true);
            INSERT INTO advance.fifo_cost_consumptions VALUES
              ('99999999-0000-0000-0000-000000000001','11111111-0000-0000-0000-000000000001',
               '55555555-0000-0000-0000-000000000001','44444444-0000-0000-0000-000000000001',
               1,100,100,'2026-09-14T00:00:00Z','MECHANISM_TEST');
            INSERT INTO advance.fifo_cost_consumptions VALUES
              ('00000000-0000-0000-0000-000000000002','11111111-0000-0000-0000-000000000001',
               '55555555-0000-0000-0000-000000000002','44444444-0000-0000-0000-000000000001',
               1,200,200,'2026-09-14T00:00:00Z','MECHANISM_TEST');
            COMMIT;
            CREATE TABLE advance.mechanism_original_consumptions AS SELECT * FROM advance.fifo_cost_consumptions;
            """);
        server.Execute("fifo-mechanism-half-return.sql","BEGIN;"+FifoMechanismReturn(1,.5m)+"""
            DO $assert$ BEGIN
              IF advance.restore_fifo_for_material_return('11111111-0000-0000-0000-000000000001',
                '66666666-0000-0000-0000-000000000001',false)<>1 THEN RAISE EXCEPTION 'Expected one restoration.'; END IF;
              IF advance.restore_fifo_for_material_return('11111111-0000-0000-0000-000000000001',
                '66666666-0000-0000-0000-000000000001',false)<>0 THEN RAISE EXCEPTION 'Replay added restoration.'; END IF;
              IF (SELECT count(*) FROM advance.fifo_cost_restorations)<>1
                OR NOT EXISTS(SELECT 1 FROM advance.fifo_cost_restorations
                  WHERE "FifoCostConsumptionId"='00000000-0000-0000-0000-000000000002'
                    AND "Quantity"=.5 AND "UnitCost"=200 AND "RestoredValue"=100)
                OR (SELECT sum("ConsumedValue") FROM advance.fifo_cost_consumptions)-
                   (SELECT sum("RestoredValue") FROM advance.fifo_cost_restorations)<>200 THEN
                RAISE EXCEPTION 'Half return must unwind the last-created 200 consumption, leaving cost 200.';
              END IF;
            END $assert$;
            COMMIT;
            """);
        server.Execute("fifo-mechanism-cross-layer-return.sql","BEGIN;"+FifoMechanismReturn(2,.75m)+"""
            SELECT advance.restore_fifo_for_material_return('11111111-0000-0000-0000-000000000001',
              '66666666-0000-0000-0000-000000000002',false);
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.fifo_cost_restorations)<>3
                OR (SELECT sum("RestoredValue") FROM advance.fifo_cost_restorations)<>225
                OR NOT EXISTS(SELECT 1 FROM advance.fifo_cost_restorations
                  WHERE "MaterialReturnLineId"='77777777-0000-0000-0000-000000000002'
                    AND "FifoCostConsumptionId"='99999999-0000-0000-0000-000000000001'
                    AND "Quantity"=.25 AND "RestoredValue"=25) THEN
                RAISE EXCEPTION 'Next return must finish the 200 consumption, then unwind 0.25 at 100.';
              END IF;
            END $assert$;
            COMMIT;
            """);
        server.AssertRejected("fifo-mechanism-excess.sql","BEGIN;"+FifoMechanismReturn(3,.8m)+"""
            SELECT advance.restore_fifo_for_material_return('11111111-0000-0000-0000-000000000001',
              '66666666-0000-0000-0000-000000000003',false);
            COMMIT;
            ""","exceeds the original issue quantity remaining unreturned");
        server.AssertRejected("fifo-mechanism-exhausted-consumption.sql","BEGIN;"+FifoMechanismReturn(4,.01m)+"""
            SELECT set_config('sess.fifo_restore_write',txid_current()::text,true);
            INSERT INTO advance.fifo_cost_restorations
              ("Id","CompanyId","FifoCostConsumptionId","MaterialReturnLineId","Quantity","UnitCost","RestoredValue",
               "EffectiveAt","RecordedAt","AcceptedByEmployeeId","RecordedBy","IsHistoricalReconciliation")
            VALUES(gen_random_uuid(),'11111111-0000-0000-0000-000000000001',
              '00000000-0000-0000-0000-000000000002','77777777-0000-0000-0000-000000000004',.01,200,2,
              '2026-09-14T01:00:00Z',clock_timestamp(),'22222222-0000-0000-0000-000000000001','MECHANISM_TEST',false);
            COMMIT;
            ""","already been fully restored");
        server.AssertRejected("fifo-mechanism-immutable-restoration.sql",
            "UPDATE advance.fifo_cost_restorations SET \"Quantity\"=0.1;","are immutable");
        server.AssertRejected("fifo-mechanism-immutable-order.sql",
            "DELETE FROM advance.fifo_consumption_creation_order;","are immutable");
        server.Execute("fifo-mechanism-retained-history.sql","""
            DO $assert$ BEGIN
              IF (SELECT count(*) FROM advance.material_returns)<>2
                OR (SELECT count(*) FROM advance.fifo_cost_restorations)<>3
                OR (SELECT sum("ConsumedValue") FROM advance.fifo_cost_consumptions)-
                   (SELECT sum("RestoredValue") FROM advance.fifo_cost_restorations)<>75
                OR EXISTS((SELECT * FROM advance.fifo_cost_consumptions EXCEPT SELECT * FROM advance.mechanism_original_consumptions)
                  UNION ALL (SELECT * FROM advance.mechanism_original_consumptions EXCEPT SELECT * FROM advance.fifo_cost_consumptions)) THEN
                RAISE EXCEPTION 'Failed restoration changed original consumption or committed partial evidence.';
              END IF;
            END $assert$;
            """);
    }

    private static string FifoMechanismReturn(int ordinal,decimal quantity)
    {
        var suffix=ordinal.ToString("D12",System.Globalization.CultureInfo.InvariantCulture);
        var amount=quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $$"""
            INSERT INTO advance.material_returns VALUES('66666666-0000-0000-0000-{{suffix}}',
              '11111111-0000-0000-0000-000000000001','33333333-0000-0000-0000-000000000001','ACCEPTED',
              '2026-09-14T01:00:00Z','22222222-0000-0000-0000-000000000001','MECHANISM_TEST','MECHANISM_TEST');
            INSERT INTO advance.material_return_lines VALUES('77777777-0000-0000-0000-{{suffix}}',
              '11111111-0000-0000-0000-000000000001','66666666-0000-0000-0000-{{suffix}}',
              '44444444-0000-0000-0000-000000000001','aaaaaaaa-0000-0000-0000-000000000001',1,{{amount}});
            INSERT INTO advance.stock_posting_batches VALUES('88888888-0000-0000-0000-{{suffix}}',
              '11111111-0000-0000-0000-000000000001','66666666-0000-0000-0000-{{suffix}}','MATERIAL_RETURN');
            """;
    }

    private const string FifoMechanismSchema="""
        CREATE SCHEMA advance;
        CREATE TABLE advance.companies("Id" uuid PRIMARY KEY);
        CREATE TABLE advance.employees("Id" uuid PRIMARY KEY);
        CREATE TABLE advance.material_issues("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL);
        CREATE TABLE advance.material_issue_lines("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
          "MaterialIssueId" uuid NOT NULL,"ItemId" uuid NOT NULL,"LineNumber" integer NOT NULL);
        CREATE TABLE advance.fifo_inventory_cost_layers("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"ReceivedAt" timestamptz NOT NULL);
        CREATE TABLE advance.fifo_cost_consumptions("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,
          "FifoInventoryCostLayerId" uuid NOT NULL,"MaterialIssueLineId" uuid NOT NULL,"Quantity" numeric(24,6) NOT NULL,
          "UnitCost" numeric(24,6) NOT NULL,"ConsumedValue" numeric(24,6) NOT NULL,"ConsumedAt" timestamptz NOT NULL,"CreatedBy" text NOT NULL);
        CREATE TABLE advance.material_returns("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"MaterialIssueId" uuid NOT NULL,
          "Status" text NOT NULL,"AcceptedAt" timestamptz NOT NULL,"AcceptedByEmployeeId" uuid NOT NULL,"UpdatedBy" text,"CreatedBy" text NOT NULL);
        CREATE TABLE advance.material_return_lines("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"MaterialReturnId" uuid NOT NULL,
          "MaterialIssueLineId" uuid NOT NULL,"ItemId" uuid NOT NULL,"LineNumber" integer NOT NULL,"ReturnedQuantityBase" numeric(24,6) NOT NULL);
        CREATE TABLE advance.stock_posting_batches("Id" uuid PRIMARY KEY,"CompanyId" uuid NOT NULL,"MaterialReturnId" uuid NOT NULL,"PostingKind" text NOT NULL);
        INSERT INTO advance.companies VALUES('11111111-0000-0000-0000-000000000001');
        INSERT INTO advance.employees VALUES('22222222-0000-0000-0000-000000000001');
        INSERT INTO advance.material_issues VALUES('33333333-0000-0000-0000-000000000001','11111111-0000-0000-0000-000000000001');
        INSERT INTO advance.material_issue_lines VALUES('44444444-0000-0000-0000-000000000001','11111111-0000-0000-0000-000000000001',
          '33333333-0000-0000-0000-000000000001','aaaaaaaa-0000-0000-0000-000000000001',1);
        INSERT INTO advance.fifo_inventory_cost_layers VALUES
          ('55555555-0000-0000-0000-000000000001','11111111-0000-0000-0000-000000000001','2026-01-01T00:00:00Z'),
          ('55555555-0000-0000-0000-000000000002','11111111-0000-0000-0000-000000000001','2026-02-01T00:00:00Z');
        """;
}
