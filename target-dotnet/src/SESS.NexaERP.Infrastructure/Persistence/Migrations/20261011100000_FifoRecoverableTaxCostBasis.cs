using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20261011100000_FifoRecoverableTaxCostBasis")]
public sealed class FifoRecoverableTaxCostBasis : Migration
{
    internal const string LayerSignature = "advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)";
    internal const string AllocationSignature = "advance.allocate_vendor_bill_landed_cost()";
    internal const string BeforeLayer = "p.\"TotalPayableValue\"/p.\"OrderedQuantity\"";
    internal const string AfterLayer = "advance.inventory_tax_unit_rate(p.\"CommercialSnapshotJson\"::jsonb,p.\"TaxRuleSnapshotJson\"::jsonb,p.\"OrderedQuantity\",p.\"UnitRate\")";
    internal const string BeforeAllocation = "round(line_row.\"UnitCost\"+line_charge/line_row.\"QuantityReceived\",6)";
    // The recorded layer is already the captured inventory tax basis for new receipts.
    // Add included inward charges once; never replace old DEMO layers with current tax values.
    internal const string AfterAllocation = "round((line_row.\"UnitCost\"*line_row.\"QuantityReceived\"+line_charge)/line_row.\"QuantityReceived\",6)";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(LayerSignature, (BeforeLayer, AfterLayer, 2)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(AllocationSignature, (BeforeAllocation, AfterAllocation)));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DO $guard$ BEGIN
              LOCK TABLE advance.fifo_inventory_cost_layers,advance.fifo_cost_consumptions,
                advance.fifo_landed_cost_adjustments IN ACCESS EXCLUSIVE MODE;
              IF EXISTS(SELECT 1 FROM advance.fifo_inventory_cost_layers)
                 OR EXISTS(SELECT 1 FROM advance.fifo_cost_consumptions)
                 OR EXISTS(SELECT 1 FROM advance.fifo_landed_cost_adjustments) THEN
                RAISE EXCEPTION 'F32 rollback refuses existing FIFO financial evidence; retain migration 151 and reconcile the release.';
              END IF;
            END $guard$;
            """);
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(AllocationSignature, (AfterAllocation, BeforeAllocation)));
        migrationBuilder.Sql(InstalledFunctionSql.Rewrite(LayerSignature, (AfterLayer, BeforeLayer, 2)));
    }
}