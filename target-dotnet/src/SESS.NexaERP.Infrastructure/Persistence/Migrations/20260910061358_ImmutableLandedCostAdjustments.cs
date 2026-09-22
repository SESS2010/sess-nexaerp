using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImmutableLandedCostAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ImmutableLandedCostAdjustmentsSql.Preflight);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalChargeValue",
                schema: "advance",
                table: "vendor_bills",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLandedValue",
                schema: "advance",
                table: "vendor_bills",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VerifiedGrossWeightKg",
                schema: "advance",
                table: "vendor_bill_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorBillLineId",
                schema: "advance",
                table: "actual_bom_entries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_actual_bom_entries_CompanyId_Id",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "Id" });

            migrationBuilder.CreateTable(
                name: "actual_bom_valuation_adjustments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualBomEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedMaterialValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AllocatedChargeValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalAcceptedValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actual_bom_valuation_adjustments", x => x.Id);
                    table.CheckConstraint("CK_actual_bom_valuation_adjustment", " \"AcceptedMaterialValue\">=0 AND \"AllocatedChargeValue\">=0 AND \"TotalAcceptedValue\"=\"AcceptedMaterialValue\"+\"AllocatedChargeValue\" ");
                    table.ForeignKey(
                        name: "FK_actual_bom_valuation_adjustments_actual_bom_entries_Company~",
                        columns: x => new { x.CompanyId, x.ActualBomEntryId },
                        principalSchema: "advance",
                        principalTable: "actual_bom_entries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_valuation_adjustments_vendor_bill_lines_CompanyI~",
                        columns: x => new { x.CompanyId, x.VendorBillLineId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fifo_landed_cost_adjustments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    FifoInventoryCostLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProvisionalUnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    LandedUnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AllocatedChargeValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ConsumedQuantityAtAcceptance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RemainingQuantityAtAcceptance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ConsumedCostAdjustmentValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RemainingStockAdjustmentValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fifo_landed_cost_adjustments", x => x.Id);
                    table.CheckConstraint("CK_fifo_landed_cost_adjustment", " \"ProvisionalUnitRate\">=0 AND \"LandedUnitRate\">=0 AND \"AllocatedChargeValue\">=0 AND \"ConsumedQuantityAtAcceptance\">=0 AND \"RemainingQuantityAtAcceptance\">=0 AND \"ConsumedCostAdjustmentValue\">=0 AND \"RemainingStockAdjustmentValue\">=0 AND \"ConsumedCostAdjustmentValue\"+\"RemainingStockAdjustmentValue\"=\"AllocatedChargeValue\" ");
                    table.ForeignKey(
                        name: "FK_fifo_landed_cost_adjustments_fifo_inventory_cost_layers_Com~",
                        columns: x => new { x.CompanyId, x.FifoInventoryCostLayerId },
                        principalSchema: "advance",
                        principalTable: "fifo_inventory_cost_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fifo_landed_cost_adjustments_vendor_bill_lines_CompanyId_Ve~",
                        columns: x => new { x.CompanyId, x.VendorBillLineId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_charges",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChargeNumber = table.Column<int>(type: "integer", nullable: false),
                    ChargeType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChargeValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IsRecoverableTax = table.Column<bool>(type: "boolean", nullable: false),
                    IncludedInInventoryCost = table.Column<bool>(type: "boolean", nullable: false),
                    AllocationBasis = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_charges", x => x.Id);
                    table.UniqueConstraint("AK_vendor_bill_charges_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_vendor_bill_charge", " \"ChargeNumber\">0 AND \"ChargeValue\">=0 AND \"ChargeType\" IN ('DUTY','INSURANCE','FREIGHT','PACKING','HANDLING','CLEARING_AGENT','MISC_INWARD','NON_CREDITABLE_TAX','RECOVERABLE_GST') AND \"AllocationBasis\" IN ('ITEM_VALUE','GROSS_WEIGHT','EXCLUDED') AND \"IncludedInInventoryCost\"<>\"IsRecoverableTax\" ");
                    table.ForeignKey(
                        name: "FK_vendor_bill_charges_vendor_bills_CompanyId_VendorBillId",
                        columns: x => new { x.CompanyId, x.VendorBillId },
                        principalSchema: "advance",
                        principalTable: "vendor_bills",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_charge_allocations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasisValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AllocatedChargeValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_charge_allocations", x => x.Id);
                    table.CheckConstraint("CK_vendor_bill_charge_allocation", " \"BasisValue\">0 AND \"AllocatedChargeValue\">=0 ");
                    table.ForeignKey(
                        name: "FK_vendor_bill_charge_allocations_vendor_bill_charges_CompanyI~",
                        columns: x => new { x.CompanyId, x.VendorBillChargeId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_charges",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_charge_allocations_vendor_bill_lines_CompanyId_~",
                        columns: x => new { x.CompanyId, x.VendorBillLineId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_valuation_adjustments_CompanyId_ActualBomEntryId~",
                schema: "advance",
                table: "actual_bom_valuation_adjustments",
                columns: new[] { "CompanyId", "ActualBomEntryId", "VendorBillLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_valuation_adjustments_CompanyId_VendorBillLineId",
                schema: "advance",
                table: "actual_bom_valuation_adjustments",
                columns: new[] { "CompanyId", "VendorBillLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_fifo_landed_cost_adjustments_CompanyId_FifoInventoryCostLay~",
                schema: "advance",
                table: "fifo_landed_cost_adjustments",
                columns: new[] { "CompanyId", "FifoInventoryCostLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_fifo_landed_cost_adjustments_CompanyId_VendorBillLineId_Fif~",
                schema: "advance",
                table: "fifo_landed_cost_adjustments",
                columns: new[] { "CompanyId", "VendorBillLineId", "FifoInventoryCostLayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_charge_allocations_CompanyId_VendorBillChargeId~",
                schema: "advance",
                table: "vendor_bill_charge_allocations",
                columns: new[] { "CompanyId", "VendorBillChargeId", "VendorBillLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_charge_allocations_CompanyId_VendorBillLineId",
                schema: "advance",
                table: "vendor_bill_charge_allocations",
                columns: new[] { "CompanyId", "VendorBillLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_charges_CompanyId_VendorBillId_ChargeNumber",
                schema: "advance",
                table: "vendor_bill_charges",
                columns: new[] { "CompanyId", "VendorBillId", "ChargeNumber" },
                unique: true);

            migrationBuilder.Sql(ImmutableLandedCostAdjustmentsSql.Up);
            migrationBuilder.Sql(ComponentFitmentActualBomSql.LandedCostCompatibleConfirm);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ComponentFitmentActualBomSql.RestoreBillRequiredConfirm);
            migrationBuilder.Sql(ImmutableLandedCostAdjustmentsSql.Down);
            migrationBuilder.Sql(ImmutableLandedCostAdjustmentsSql.RestoreLastPurchasePriceFunction);

            migrationBuilder.DropTable(
                name: "actual_bom_valuation_adjustments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "fifo_landed_cost_adjustments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bill_charge_allocations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bill_charges",
                schema: "advance");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_actual_bom_entries_CompanyId_Id",
                schema: "advance",
                table: "actual_bom_entries");

            migrationBuilder.DropColumn(
                name: "TotalChargeValue",
                schema: "advance",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "TotalLandedValue",
                schema: "advance",
                table: "vendor_bills");

            migrationBuilder.DropColumn(
                name: "VerifiedGrossWeightKg",
                schema: "advance",
                table: "vendor_bill_lines");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorBillLineId",
                schema: "advance",
                table: "actual_bom_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
