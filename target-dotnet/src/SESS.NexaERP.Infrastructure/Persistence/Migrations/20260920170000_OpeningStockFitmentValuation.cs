using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// Opening stock in the Actual BOM (decision of the Technical Director, 20 September 2026).
/// Valuation resolves by origin: a GRN-origin fitment is valued from the accepted bill's landed
/// unit rate (unchanged); an opening-stock-origin fitment is valued from the opening line's
/// Accounts-confirmed ex-tax unit value, with no charges, and is OPENING_CONFIRMED at once.
/// Declared vendor and bill provenance never affects the value. Actual BOM entries carry either a
/// GRN line or an opening stock line, and the machine dossier states which numbers this system
/// proved (accepted, matched, paid) and which SESS declared at opening stock.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260920170000_OpeningStockFitmentValuation")]
public sealed class OpeningStockFitmentValuation : Migration
{
    private const string Confirm = "advance.confirm_component_fitment(uuid,uuid,uuid,numeric,timestamptz,text,uuid,text,text,text,uuid,text,uuid,text,text)";
    private const string Reverse = "advance.reverse_component_fitment(uuid,uuid,text,text,text,text,uuid,text,uuid,text,text)";
    private const string Dossier = "advance.company_report_machine_dossier(text,uuid,uuid[],boolean,text,text,date,date,text,text,jsonb,bigint,integer,text)";

    private const string ConfirmElseBefore = "ELSE\n            material_value:=0; charge_value:=0;\n          END IF;";
    private const string ConfirmElseAfter = """
        ELSIF line_row."OriginOpeningStockLineId" IS NOT NULL THEN
            -- Opening-stock origin: the Accounts-confirmed ex-tax unit value; declared provenance never values it.
            SELECT round(o."UnitRate"*p_quantity,6) INTO material_value FROM advance.opening_stock_lines o
              WHERE o."CompanyId"=p_company AND o."Id"=line_row."OriginOpeningStockLineId";
            IF material_value IS NULL THEN RAISE EXCEPTION 'Fitment origin opening stock line was not found.'; END IF;
            charge_value:=0;
          ELSE
            material_value:=0; charge_value:=0;
          END IF;
        """;
    private const string ConfirmColumnsBefore = @"""InventorySerialId"",""GoodsReceiptLineId"",""GrnNumberSnapshot"",""VendorBillLineId"",";
    private const string ConfirmColumnsAfter = @"""InventorySerialId"",""GoodsReceiptLineId"",""OpeningStockLineId"",""GrnNumberSnapshot"",""VendorBillLineId"",";
    private const string ConfirmValuesBefore = @"line_row.""InventorySerialId"",line_row.""OriginGoodsReceiptLineId"",allocation_row.grn_number,";
    private const string ConfirmValuesAfter = @"line_row.""InventorySerialId"",line_row.""OriginGoodsReceiptLineId"",line_row.""OriginOpeningStockLineId"",coalesce(allocation_row.grn_number,''),";
    private const string ReverseColumnsBefore = @"""InventoryLotId"",""InventorySerialId"",""GoodsReceiptLineId"",""GrnNumberSnapshot"",";
    private const string ReverseColumnsAfter = @"""InventoryLotId"",""InventorySerialId"",""GoodsReceiptLineId"",""OpeningStockLineId"",""GrnNumberSnapshot"",";
    private const string ReverseValuesBefore = @"ae.""GoodsReceiptLineId"",ae.""GrnNumberSnapshot"",ae.""VendorBillLineId"",";
    private const string ReverseValuesAfter = @"ae.""GoodsReceiptLineId"",ae.""OpeningStockLineId"",ae.""GrnNumberSnapshot"",ae.""VendorBillLineId"",";
    private const string DossierCurrencyBefore = @" WHERE gl.""CompanyId""=r.""CompanyId"" AND gl.""Id""=r.""GoodsReceiptLineId""),'UNVALUED') AS currency";
    private const string DossierCurrencyAfter = @" WHERE gl.""CompanyId""=r.""CompanyId"" AND gl.""Id""=r.""GoodsReceiptLineId""),CASE WHEN r.""OpeningStockLineId"" IS NOT NULL THEN 'INR' ELSE 'UNVALUED' END) AS currency";
    private const string DossierDetailBefore = @" 'evidencePart',part.n,";
    private const string DossierDetailAfter = @" 'provenance',CASE WHEN r.""OpeningStockLineId"" IS NOT NULL THEN (SELECT CASE WHEN o.""VendorBillNumber"" IS NOT NULL THEN 'Bill '||o.""VendorBillNumber""||' - declared at opening stock, not verified in this system' ELSE 'Opening stock, authorised '||to_char(s.""AuthorizedAt"" AT TIME ZONE p_report_timezone,'DD Mon YYYY')||' by '||coalesce(emp.""EmployeeCode"",'?') END FROM advance.opening_stock_lines o JOIN advance.opening_stocks s ON s.""CompanyId""=o.""CompanyId"" AND s.""Id""=o.""OpeningStockId"" LEFT JOIN advance.employees emp ON emp.""Id""=s.""AuthorizedByEmployeeId"" WHERE o.""CompanyId""=r.""CompanyId"" AND o.""Id""=r.""OpeningStockLineId"") ELSE coalesce((SELECT 'Bill '||bill.""BillNumber""||' - accepted, matched'||CASE WHEN coalesce(paid.total,0)>=bill.""TotalLandedValue"" THEN ', paid' WHEN coalesce(paid.total,0)>0 THEN ', part-paid' ELSE ', unpaid' END FROM advance.vendor_bill_lines bl JOIN advance.vendor_bills bill ON bill.""CompanyId""=bl.""CompanyId"" AND bill.""Id""=bl.""VendorBillId"" AND bill.""Status""='ACCEPTED' LEFT JOIN LATERAL (SELECT sum(pa.""Amount"") AS total FROM advance.vendor_payment_allocations pa WHERE pa.""CompanyId""=bill.""CompanyId"" AND pa.""VendorBillId""=bill.""Id"") paid ON true WHERE bl.""CompanyId""=r.""CompanyId"" AND bl.""GoodsReceiptLineId""=r.""GoodsReceiptLineId"" ORDER BY bill.""DecidedAt"" DESC NULLS LAST LIMIT 1),'GRN '||r.""GrnNumberSnapshot""||' - bill not yet accepted') END,
 'evidencePart',part.n,";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF to_regclass('advance.actual_bom_entries') IS NULL OR to_regclass('advance.opening_stock_lines') IS NULL
                 OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='advance' AND table_name='material_issue_lines' AND column_name='OriginOpeningStockLineId')
                 OR to_regprocedure('{{Confirm}}') IS NULL OR to_regprocedure('{{Reverse}}') IS NULL OR to_regprocedure('{{Dossier}}') IS NULL THEN
                RAISE EXCEPTION 'Opening-stock fitment valuation requires the Actual BOM, the opening origin (#26) and the dossier report.';
              END IF;
            END $guard$;
            ALTER TABLE advance.actual_bom_entries ALTER COLUMN "GoodsReceiptLineId" DROP NOT NULL;
            ALTER TABLE advance.actual_bom_entries ADD COLUMN "OpeningStockLineId" uuid NULL;
            ALTER TABLE advance.actual_bom_entries ADD CONSTRAINT "FK_actual_bom_entries_opening_stock_lines_CompanyId_OpeningStockLineId"
              FOREIGN KEY ("CompanyId","OpeningStockLineId") REFERENCES advance.opening_stock_lines("CompanyId","Id") ON DELETE RESTRICT;
            CREATE INDEX "IX_actual_bom_entries_CompanyId_OpeningStockLineId" ON advance.actual_bom_entries("CompanyId","OpeningStockLineId");
            ALTER TABLE advance.actual_bom_entries ADD CONSTRAINT "CK_actual_bom_entry_origin" CHECK (num_nonnulls("GoodsReceiptLineId","OpeningStockLineId")=1);
            {{InstalledFunctionSql.Rewrite(Confirm, (ConfirmElseBefore, ConfirmElseAfter), (ConfirmColumnsBefore, ConfirmColumnsAfter), (ConfirmValuesBefore, ConfirmValuesAfter))}}
            {{InstalledFunctionSql.Rewrite(Reverse, (ReverseColumnsBefore, ReverseColumnsAfter), (ReverseValuesBefore, ReverseValuesAfter))}}
            {{InstalledFunctionSql.Rewrite(Dossier, (DossierCurrencyBefore, DossierCurrencyAfter), (DossierDetailBefore, DossierDetailAfter))}}
            """));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(MigrationText.Lf($$"""
            DO $guard$
            BEGIN
              IF EXISTS (SELECT 1 FROM advance.actual_bom_entries WHERE "OpeningStockLineId" IS NOT NULL) THEN
                RAISE EXCEPTION 'Refusing rollback: opening stock has been fitted into a machine.';
              END IF;
            END $guard$;
            {{InstalledFunctionSql.Rewrite(Dossier, (DossierCurrencyAfter, DossierCurrencyBefore), (DossierDetailAfter, DossierDetailBefore))}}
            {{InstalledFunctionSql.Rewrite(Reverse, (ReverseColumnsAfter, ReverseColumnsBefore), (ReverseValuesAfter, ReverseValuesBefore))}}
            {{InstalledFunctionSql.Rewrite(Confirm, (ConfirmElseAfter, ConfirmElseBefore), (ConfirmColumnsAfter, ConfirmColumnsBefore), (ConfirmValuesAfter, ConfirmValuesBefore))}}
            ALTER TABLE advance.actual_bom_entries DROP CONSTRAINT "CK_actual_bom_entry_origin";
            DROP INDEX advance."IX_actual_bom_entries_CompanyId_OpeningStockLineId";
            ALTER TABLE advance.actual_bom_entries DROP CONSTRAINT "FK_actual_bom_entries_opening_stock_lines_CompanyId_OpeningStockLineId";
            ALTER TABLE advance.actual_bom_entries DROP COLUMN "OpeningStockLineId";
            ALTER TABLE advance.actual_bom_entries ALTER COLUMN "GoodsReceiptLineId" SET NOT NULL;
            """));
    }
}
