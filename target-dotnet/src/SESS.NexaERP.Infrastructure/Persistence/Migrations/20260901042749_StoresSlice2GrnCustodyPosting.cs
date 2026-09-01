using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoresSlice2GrnCustodyPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StoresGrnSlice2Sql.GuardUp);
            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "InventorySerialId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "inventory_lots",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierLotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    NormalizedSupplierLotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ManufacturerLotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    NormalizedManufacturerLotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lots", x => x.Id);
                    table.UniqueConstraint("AK_inventory_lots_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_lot_expiry", "\"ManufactureDate\" IS NULL OR \"ExpiryDate\" IS NULL OR \"ExpiryDate\">=\"ManufactureDate\"");
                    table.CheckConstraint("CK_inventory_lot_normalized_manufacturer", "\"NormalizedManufacturerLotNumber\" IS NOT DISTINCT FROM CASE WHEN nullif(trim(\"ManufacturerLotNumber\"),'') IS NULL THEN NULL ELSE upper(trim(regexp_replace(\"ManufacturerLotNumber\",'[[:space:]]+',' ','g'))) END");
                    table.CheckConstraint("CK_inventory_lot_normalized_supplier", "\"NormalizedSupplierLotNumber\" IS NOT DISTINCT FROM CASE WHEN nullif(trim(\"SupplierLotNumber\"),'') IS NULL THEN NULL ELSE upper(trim(regexp_replace(\"SupplierLotNumber\",'[[:space:]]+',' ','g'))) END");
                    table.ForeignKey(
                        name: "FK_inventory_lots_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_lots_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_lots_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_line_lot_allocations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotOrdinal = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_line_lot_allocations", x => x.Id);
                    table.UniqueConstraint("AK_goods_receipt_line_lot_allocations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_grn_line_lot_ordinal", "\"LotOrdinal\">0");
                    table.CheckConstraint("CK_grn_line_lot_quantity", "\"Quantity\">0");
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_lot_allocations_goods_receipt_lines_Comp~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_lot_allocations_inventory_lots_CompanyId~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements",
                column: "GoodsReceiptLineLotAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_CompanyId_GoodsReceiptLineLotAll~",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                column: "GoodsReceiptLineLotAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_lot_allocations_CompanyId_GoodsReceiptLi~",
                schema: "advance",
                table: "goods_receipt_line_lot_allocations",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_lot_allocations_CompanyId_InventoryLotId",
                schema: "advance",
                table: "goods_receipt_line_lot_allocations",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_lot_allocations_GoodsReceiptLineId_Inven~",
                schema: "advance",
                table: "goods_receipt_line_lot_allocations",
                columns: new[] { "GoodsReceiptLineId", "InventoryLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_lot_allocations_GoodsReceiptLineId_LotOr~",
                schema: "advance",
                table: "goods_receipt_line_lot_allocations",
                columns: new[] { "GoodsReceiptLineId", "LotOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lots_CompanyId_ItemId_VendorId_NormalizedSupplier~",
                schema: "advance",
                table: "inventory_lots",
                columns: new[] { "CompanyId", "ItemId", "VendorId", "NormalizedSupplierLotNumber", "NormalizedManufacturerLotNumber", "ManufactureDate", "ExpiryDate" },
                unique: true,
                filter: "\"NormalizedSupplierLotNumber\" IS NOT NULL OR \"NormalizedManufacturerLotNumber\" IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lots_ItemId",
                schema: "advance",
                table: "inventory_lots",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lots_VendorId",
                schema: "advance",
                table: "inventory_lots",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipt_line_serials_goods_receipt_line_lot_allocatio~",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" },
                principalSchema: "advance",
                principalTable: "goods_receipt_line_lot_allocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_goods_receipt_line_lot_allocations_CompanyI~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" },
                principalSchema: "advance",
                principalTable: "goods_receipt_line_lot_allocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(StoresGrnSlice2Sql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StoresGrnSlice2Sql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipt_line_serials_goods_receipt_line_lot_allocatio~",
                schema: "advance",
                table: "goods_receipt_line_serials");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_goods_receipt_line_lot_allocations_CompanyI~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "goods_receipt_line_lot_allocations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_lots",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_line_serials_CompanyId_GoodsReceiptLineLotAll~",
                schema: "advance",
                table: "goods_receipt_line_serials");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_line_serials_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "goods_receipt_line_serials");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "goods_receipt_line_serials");

            migrationBuilder.AlterColumn<Guid>(
                name: "InventorySerialId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
