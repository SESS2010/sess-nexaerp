using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev868PurchaseLocationAllocationCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_Status",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_requisitions_estimated_total_nonnegative",
                schema: "nexa",
                table: "purchase_requisitions");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "stock_reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationKey",
                schema: "nexa",
                table: "stock_reservations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RackBinId",
                schema: "nexa",
                table: "stock_reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckedAt",
                schema: "nexa",
                table: "stock_availability_check_lines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "LocationKey",
                schema: "nexa",
                table: "stock_availability_check_lines",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialYear",
                schema: "nexa",
                table: "purchase_requisitions",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "PrSequence",
                schema: "nexa",
                table: "purchase_requisitions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationKey",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "purchase_number_sequences",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_number_sequences", x => x.Id);
                    table.CheckConstraint("CK_purchase_number_sequences_last_number_nonnegative", "\"LastNumber\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ItemId_WarehouseId_RackBinId_Status",
                schema: "nexa",
                table: "stock_reservations",
                columns: new[] { "ItemId", "WarehouseId", "RackBinId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St~",
                schema: "nexa",
                table: "stock_reservations",
                columns: new[] { "PurchaseRequisitionLineId", "LocationKey", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_RackBinId",
                schema: "nexa",
                table: "stock_reservations",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_WarehouseId",
                schema: "nexa",
                table: "stock_reservations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId_Wa~",
                schema: "nexa",
                table: "stock_availability_check_lines",
                columns: new[] { "PurchaseRequisitionLineId", "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "nexa",
                table: "stock_availability_check_lines",
                columns: new[] { "StockAvailabilityCheckId", "PurchaseRequisitionLineId", "LocationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "WarehouseId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_stock_check_lines_quantities_valid",
                schema: "nexa",
                table: "stock_availability_check_lines",
                sql: "\"RequestedQuantity\" > 0 AND \"OnHandQuantity\" >= 0 AND \"ActiveReservedQuantity\" >= 0 AND \"AvailableQuantity\" >= 0 AND \"InTransitQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ShortageQuantity\" >= 0 AND \"ReservedQuantity\" <= \"RequestedQuantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen~",
                schema: "nexa",
                table: "purchase_requisitions",
                columns: new[] { "OrganizationId", "FinancialYear", "PrSequence" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_requisitions_estimated_total_nonnegative",
                schema: "nexa",
                table: "purchase_requisitions",
                sql: "\"EstimatedTotal\" >= 0 AND \"PrSequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_pr_lines_reconcile_requested",
                schema: "nexa",
                table: "purchase_requisition_lines",
                sql: "\"ReservedQuantity\" <= \"RequestedQuantity\" AND \"ShortageQuantity\" = GREATEST(\"RequestedQuantity\" - \"ReservedQuantity\", 0) AND \"ProcurementHandoffQuantity\" = \"ShortageQuantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "WarehouseId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_route_limits_valid",
                schema: "nexa",
                table: "purchase_approval_route_settings",
                sql: "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_number_sequences_OrganizationId_FinancialYear_Pref~",
                schema: "nexa",
                table: "purchase_number_sequences",
                columns: new[] { "OrganizationId", "FinancialYear", "Prefix" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_requirement_handoffs_rack_bins_RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "RackBinId",
                principalSchema: "nexa",
                principalTable: "rack_bins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_requirement_handoffs_warehouses_WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "WarehouseId",
                principalSchema: "nexa",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_availability_check_lines_rack_bins_RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "RackBinId",
                principalSchema: "nexa",
                principalTable: "rack_bins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_availability_check_lines_warehouses_WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "WarehouseId",
                principalSchema: "nexa",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_reservations_rack_bins_RackBinId",
                schema: "nexa",
                table: "stock_reservations",
                column: "RackBinId",
                principalSchema: "nexa",
                principalTable: "rack_bins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_reservations_warehouses_WarehouseId",
                schema: "nexa",
                table: "stock_reservations",
                column: "WarehouseId",
                principalSchema: "nexa",
                principalTable: "warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_requirement_handoffs_rack_bins_RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_requirement_handoffs_warehouses_WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_availability_check_lines_rack_bins_RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_availability_check_lines_warehouses_WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_reservations_rack_bins_RackBinId",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_reservations_warehouses_WarehouseId",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropTable(
                name: "purchase_number_sequences",
                schema: "nexa");

            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_ItemId_WarehouseId_RackBinId_Status",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St~",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_RackBinId",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_reservations_WarehouseId",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId_Wa~",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropIndex(
                name: "IX_stock_availability_check_lines_WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stock_check_lines_quantities_valid",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen~",
                schema: "nexa",
                table: "purchase_requisitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_requisitions_estimated_total_nonnegative",
                schema: "nexa",
                table: "purchase_requisitions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_pr_lines_reconcile_requested",
                schema: "nexa",
                table: "purchase_requisition_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_requirement_handoffs_RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.DropIndex(
                name: "IX_purchase_requirement_handoffs_WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_route_limits_valid",
                schema: "nexa",
                table: "purchase_approval_route_settings");

            migrationBuilder.DropColumn(
                name: "LocationKey",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropColumn(
                name: "RackBinId",
                schema: "nexa",
                table: "stock_reservations");

            migrationBuilder.DropColumn(
                name: "CheckedAt",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropColumn(
                name: "LocationKey",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropColumn(
                name: "RackBinId",
                schema: "nexa",
                table: "stock_availability_check_lines");

            migrationBuilder.DropColumn(
                name: "FinancialYear",
                schema: "nexa",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "PrSequence",
                schema: "nexa",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "LocationKey",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.DropColumn(
                name: "RackBinId",
                schema: "nexa",
                table: "purchase_requirement_handoffs");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "stock_reservations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_Status",
                schema: "nexa",
                table: "stock_reservations",
                columns: new[] { "PurchaseRequisitionLineId", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "PurchaseRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "nexa",
                table: "stock_availability_check_lines",
                columns: new[] { "StockAvailabilityCheckId", "PurchaseRequisitionLineId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_requisitions_estimated_total_nonnegative",
                schema: "nexa",
                table: "purchase_requisitions",
                sql: "\"EstimatedTotal\" >= 0");
        }
    }
}
