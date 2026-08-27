using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FirstStoresPart3BLedgerActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart3BSql.PreUp);
            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityOut",
                schema: "advance",
                table: "stock_movements",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityIn",
                schema: "advance",
                table: "stock_movements",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AddColumn<int>(
                name: "BatchLineOrdinal",
                schema: "advance",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConditionCode",
                schema: "advance",
                table: "stock_movements",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventorySerialId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "LedgerSchemaVersion",
                schema: "advance",
                table: "stock_movements",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovementLeg",
                schema: "advance",
                table: "stock_movements",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingIdentity",
                schema: "advance",
                table: "stock_movements",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockPostingBatchId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseConditionLocationId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stock_posting_batches",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingKind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryChallanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversesPostingBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_posting_batches", x => x.Id);
                    table.UniqueConstraint("AK_stock_posting_batches_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_delivery_challans_CompanyId_DeliveryC~",
                        columns: x => new { x.CompanyId, x.DeliveryChallanId },
                        principalSchema: "advance",
                        principalTable: "delivery_challans",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_employees_PostedByEmployeeId",
                        column: x => x.PostedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_goods_receipts_CompanyId_GoodsReceipt~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptId },
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_material_issue_requests_CompanyId_Mat~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestId },
                        principalSchema: "advance",
                        principalTable: "material_issue_requests",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_qc_inspection_revisions_CompanyId_QcI~",
                        columns: x => new { x.CompanyId, x.QcInspectionRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_posting_batches_stock_posting_batches_CompanyId_Rever~",
                        columns: x => new { x.CompanyId, x.ReversesPostingBatchId },
                        principalSchema: "advance",
                        principalTable: "stock_posting_batches",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "DeliveryChallanLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventorySerialId_PostingDate_Id",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventorySerialId", "PostingDate", "Id" },
                filter: "\"InventorySerialId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_ItemId_WarehouseConditionLocation~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "ItemId", "WarehouseConditionLocationId", "PostingDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialIssueRequestLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OriginGoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_PostingIdentity",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "PostingIdentity" },
                unique: true,
                filter: "\"PostingIdentity\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "ReversesStockMovementId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_StockPostingBatchId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "StockPostingBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_WarehouseConditionLocationId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "WarehouseConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements",
                column: "DeliveryChallanLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements",
                column: "MaterialIssueRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements",
                column: "OriginGoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements",
                column: "QcInspectionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements",
                column: "ReversesStockMovementId",
                unique: true,
                filter: "\"ReversesStockMovementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_StockPostingBatchId",
                schema: "advance",
                table: "stock_movements",
                column: "StockPostingBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_StockPostingBatchId_BatchLineOrdinal",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "StockPostingBatchId", "BatchLineOrdinal" },
                unique: true,
                filter: "\"StockPostingBatchId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_DeliveryChallanId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "DeliveryChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_GoodsReceiptId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_MaterialIssueRequestId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_PostingDate",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_ReversesPostingBatchId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "ReversesPostingBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CorrelationId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_DeliveryChallanId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "DeliveryChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_GoodsReceiptId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_MaterialIssueRequestId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "MaterialIssueRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_PostedByEmployeeId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "PostedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "QcInspectionRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_ReversesPostingBatchId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "ReversesPostingBatchId",
                unique: true,
                filter: "\"PostingKind\"='REVERSAL'");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_delivery_challan_lines_CompanyId_DeliveryCh~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "DeliveryChallanLineId" },
                principalSchema: "advance",
                principalTable: "delivery_challan_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_goods_receipt_lines_CompanyId_GoodsReceiptL~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" },
                principalSchema: "advance",
                principalTable: "goods_receipt_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_goods_receipt_lines_CompanyId_OriginGoodsRe~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OriginGoodsReceiptLineId" },
                principalSchema: "advance",
                principalTable: "goods_receipt_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_serials_CompanyId_InventorySerial~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventorySerialId" },
                principalSchema: "advance",
                principalTable: "inventory_serials",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_material_issue_request_lines_CompanyId_Mate~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialIssueRequestLineId" },
                principalSchema: "advance",
                principalTable: "material_issue_request_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_qc_inspection_revisions_CompanyId_QcInspect~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" },
                principalSchema: "advance",
                principalTable: "qc_inspection_revisions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_stock_movements_CompanyId_ReversesStockMove~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "ReversesStockMovementId" },
                principalSchema: "advance",
                principalTable: "stock_movements",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_stock_posting_batches_CompanyId_StockPostin~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "StockPostingBatchId" },
                principalSchema: "advance",
                principalTable: "stock_posting_batches",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_warehouse_condition_locations_CompanyId_War~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "WarehouseConditionLocationId" },
                principalSchema: "advance",
                principalTable: "warehouse_condition_locations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(FirstStoresPart3BSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart3BSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_delivery_challan_lines_CompanyId_DeliveryCh~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_goods_receipt_lines_CompanyId_GoodsReceiptL~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_goods_receipt_lines_CompanyId_OriginGoodsRe~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_serials_CompanyId_InventorySerial~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_material_issue_request_lines_CompanyId_Mate~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_qc_inspection_revisions_CompanyId_QcInspect~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_stock_movements_CompanyId_ReversesStockMove~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_stock_posting_batches_CompanyId_StockPostin~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_warehouse_condition_locations_CompanyId_War~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "stock_posting_batches",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventorySerialId_PostingDate_Id",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_ItemId_WarehouseConditionLocation~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_PostingIdentity",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_StockPostingBatchId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_WarehouseConditionLocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_StockPostingBatchId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_StockPostingBatchId_BatchLineOrdinal",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "BatchLineOrdinal",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "ConditionCode",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "DeliveryChallanLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventorySerialId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "LedgerSchemaVersion",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "MaterialIssueRequestLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "MovementLeg",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "OriginGoodsReceiptLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "PostingIdentity",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "QcInspectionRevisionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "ReversesStockMovementId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "StockPostingBatchId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "WarehouseConditionLocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityOut",
                schema: "advance",
                table: "stock_movements",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(24,6)",
                oldPrecision: 24,
                oldScale: 6,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityIn",
                schema: "advance",
                table: "stock_movements",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(24,6)",
                oldPrecision: 24,
                oldScale: 6,
                oldDefaultValue: 0m);
        }
    }
}
