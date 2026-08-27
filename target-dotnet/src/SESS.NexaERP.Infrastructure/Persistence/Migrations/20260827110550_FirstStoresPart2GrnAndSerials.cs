using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FirstStoresPart2GrnAndSerials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart2Sql.PreUp);

            migrationBuilder.AlterColumn<Guid>(
                name: "GateEntryId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NORMAL"),
                    ReversesGoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    VendorBillNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VendorBillDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VendorDcNumberSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModeOfTransportSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoReceiptVerificationJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConfigurationSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ConfigurationSnapshotHash = table.Column<string>(type: "character(64)", nullable: false),
                    QcCompletionDaysConfigVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcCompletionDaysSnapshot = table.Column<int>(type: "integer", nullable: false),
                    QcDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipts", x => x.Id);
                    table.UniqueConstraint("AK_goods_receipts_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.UniqueConstraint("AK_goods_receipts_GateEntryId_Id", x => new { x.GateEntryId, x.Id });
                    table.CheckConstraint("CK_goods_receipt_bill", "length(trim(\"VendorBillNumber\"))>0 AND \"VendorBillDate\" IS NOT NULL");
                    table.CheckConstraint("CK_goods_receipt_configuration_hash", "\"ConfigurationSnapshotHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_goods_receipt_configuration_json", "jsonb_typeof(\"ConfigurationSnapshotJson\")='object'");
                    table.CheckConstraint("CK_goods_receipt_document_kind", "\"DocumentKind\" IN ('NORMAL','REVERSAL')");
                    table.CheckConstraint("CK_goods_receipt_finalization", "(\"Status\"='DRAFT' AND \"FinalizedAt\" IS NULL AND \"FinalizedByEmployeeId\" IS NULL) OR (\"Status\"='FINALIZED' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedByEmployeeId\" IS NOT NULL)");
                    table.CheckConstraint("CK_goods_receipt_iso_json", "jsonb_typeof(\"IsoReceiptVerificationJson\")='object'");
                    table.CheckConstraint("CK_goods_receipt_qc_days", "\"QcCompletionDaysSnapshot\" > 0");
                    table.CheckConstraint("CK_goods_receipt_request_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
                    table.CheckConstraint("CK_goods_receipt_reversal", "(\"DocumentKind\"='NORMAL' AND \"ReversesGoodsReceiptId\" IS NULL AND \"ReversalReason\" IS NULL) OR (\"DocumentKind\"='REVERSAL' AND \"ReversesGoodsReceiptId\" IS NOT NULL AND length(trim(coalesce(\"ReversalReason\",'')))>0)");
                    table.CheckConstraint("CK_goods_receipt_status", "\"Status\" IN ('DRAFT','FINALIZED')");
                    table.ForeignKey(
                        name: "FK_goods_receipts_business_rule_configuration_versions_Company~",
                        columns: x => new { x.CompanyId, x.QcCompletionDaysConfigVersionId },
                        principalSchema: "advance",
                        principalTable: "business_rule_configuration_versions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_employees_FinalizedByEmployeeId",
                        column: x => x.FinalizedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_gate_entries_CompanyId_GateEntryId",
                        columns: x => new { x.CompanyId, x.GateEntryId },
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_goods_receipts_ReversesGoodsReceiptId",
                        column: x => x.ReversesGoodsReceiptId,
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_purchase_orders_CompanyId_PurchaseOrderId",
                        columns: x => new { x.CompanyId, x.PurchaseOrderId },
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_serials",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoredSerialNumber = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NormalizedStoredSerialNumber = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FirstCapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstCapturedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_serials", x => x.Id);
                    table.UniqueConstraint("AK_inventory_serials_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_serial_values", "length(trim(\"StoredSerialNumber\"))>0 AND length(trim(\"NormalizedStoredSerialNumber\"))>0");
                    table.ForeignKey(
                        name: "FK_inventory_serials_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serials_employees_FirstCapturedByEmployeeId",
                        column: x => x.FirstCapturedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serials_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateEntryLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ItemCategoryIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCategoryCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HsnSacCodeSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GstPercentageSnapshot = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    ModelSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ManufacturerPartNumberSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ManufacturerMakeSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    UomSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PoOrderedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PriorEffectiveReceivedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RemainingPoQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DeliveredQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExcessRejectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false, defaultValue: 0m),
                    ExcessDisposition = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LineValueSnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRateSnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    SerialThresholdConfigVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialThresholdValueSnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    SerialCaptureModeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SerialOverrideSettingId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcRouteIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    QcHoldConditionLocationIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    BillWarrantyLimitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InitialWarrantyExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_lines", x => x.Id);
                    table.UniqueConstraint("AK_goods_receipt_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_goods_receipt_line_excess", "(\"ExcessRejectedQuantity\"=0 AND \"ExcessDisposition\" IS NULL) OR (\"ExcessRejectedQuantity\">0 AND \"ExcessDisposition\"='PENDING_RETURNABLE_DC')");
                    table.CheckConstraint("CK_goods_receipt_line_gst", "\"GstPercentageSnapshot\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_goods_receipt_line_quantities", "\"PoOrderedQuantitySnapshot\">=0 AND \"PriorEffectiveReceivedQuantitySnapshot\">=0 AND \"RemainingPoQuantitySnapshot\">=0 AND \"DeliveredQuantitySnapshot\">0 AND \"ReceivedQuantity\">0 AND \"ExcessRejectedQuantity\">=0");
                    table.CheckConstraint("CK_goods_receipt_line_rate", "\"LineValueSnapshot\">=0 AND \"UnitRateSnapshot\">=0 AND \"UnitRateSnapshot\"=\"LineValueSnapshot\"/\"ReceivedQuantity\"");
                    table.CheckConstraint("CK_goods_receipt_line_reconciliation", "\"ReceivedQuantity\"+\"ExcessRejectedQuantity\"=\"DeliveredQuantitySnapshot\" AND \"ReceivedQuantity\"<=\"RemainingPoQuantitySnapshot\" AND \"RemainingPoQuantitySnapshot\"=\"PoOrderedQuantitySnapshot\"-\"PriorEffectiveReceivedQuantitySnapshot\"");
                    table.CheckConstraint("CK_goods_receipt_line_serial_mode", "\"SerialCaptureModeSnapshot\" IN ('REQUIRED','OPTIONAL')");
                    table.CheckConstraint("CK_goods_receipt_line_warranty", "\"InitialWarrantyExpiryDate\"=\"BillWarrantyLimitDate\"");
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_business_rule_configuration_versions_Co~",
                        columns: x => new { x.CompanyId, x.SerialThresholdConfigVersionId },
                        principalSchema: "advance",
                        principalTable: "business_rule_configuration_versions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_gate_entry_lines_CompanyId_GateEntryLin~",
                        columns: x => new { x.CompanyId, x.GateEntryLineId },
                        principalSchema: "advance",
                        principalTable: "gate_entry_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_goods_receipts_CompanyId_GoodsReceiptId",
                        columns: x => new { x.CompanyId, x.GoodsReceiptId },
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_item_categories_ItemCategoryIdSnapshot",
                        column: x => x.ItemCategoryIdSnapshot,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_item_company_inventory_settings_Company~",
                        columns: x => new { x.CompanyId, x.SerialOverrideSettingId },
                        principalSchema: "advance",
                        principalTable: "item_company_inventory_settings",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_purchase_order_lines_CompanyId_Purchase~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderLineId },
                        principalSchema: "advance",
                        principalTable: "purchase_order_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_store_category_routes_CompanyId_QcRoute~",
                        columns: x => new { x.CompanyId, x.QcRouteIdSnapshot },
                        principalSchema: "advance",
                        principalTable: "store_category_routes",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_warehouse_condition_locations_CompanyId~",
                        columns: x => new { x.CompanyId, x.QcHoldConditionLocationIdSnapshot },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_line_serials",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialOrdinal = table.Column<int>(type: "integer", nullable: false),
                    EnteredSerialNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StoredSerialNumberSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ReceiptDisposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisambiguationApplied = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DuplicateWarningAcknowledged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisambiguationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CapturedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_line_serials", x => x.Id);
                    table.UniqueConstraint("AK_goods_receipt_line_serials_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_goods_receipt_line_serial_disambiguation", "(\"DisambiguationApplied\" AND \"DuplicateWarningAcknowledged\" AND length(trim(coalesce(\"DisambiguationReason\",'')))>0) OR (NOT \"DisambiguationApplied\" AND \"DisambiguationReason\" IS NULL)");
                    table.CheckConstraint("CK_goods_receipt_line_serial_disposition", "\"ReceiptDisposition\" IN ('QC_INSPECTION','EXCESS_PENDING_RETURN')");
                    table.CheckConstraint("CK_goods_receipt_line_serial_ordinal", "\"SerialOrdinal\">0");
                    table.CheckConstraint("CK_goods_receipt_line_serial_values", "length(trim(\"EnteredSerialNumber\"))>0 AND length(trim(\"StoredSerialNumberSnapshot\"))>0");
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_serials_employees_CapturedByEmployeeId",
                        column: x => x.CapturedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_serials_goods_receipt_lines_CompanyId_Go~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_serials_inventory_serials_CompanyId_Inve~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_line_serials_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_GoodsReceiptId",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_GoodsReceiptId_OccurredAt",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "GoodsReceiptId", "OccurredAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_stores_document_status_part2_source",
                schema: "advance",
                table: "stores_document_status_history",
                sql: "num_nonnulls(\"GateEntryId\",\"GoodsReceiptId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_CapturedByEmployeeId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                column: "CapturedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_CompanyId_InventorySerialId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_CompanyId_ItemId_StoredSerialNum~",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "CompanyId", "ItemId", "StoredSerialNumberSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_GoodsReceiptLineId_InventorySeri~",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "GoodsReceiptLineId", "InventorySerialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_GoodsReceiptLineId_SerialOrdinal",
                schema: "advance",
                table: "goods_receipt_line_serials",
                columns: new[] { "GoodsReceiptLineId", "SerialOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_InventorySerialId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                column: "InventorySerialId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_line_serials_ItemId",
                schema: "advance",
                table: "goods_receipt_line_serials",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_GateEntryLineId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "GateEntryLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_GoodsReceiptId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_ItemId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_PurchaseOrderLineId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_QcHoldConditionLocationIdSnap~",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "QcHoldConditionLocationIdSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_QcRouteIdSnapshot",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "QcRouteIdSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_SerialOverrideSettingId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "SerialOverrideSettingId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_CompanyId_SerialThresholdConfigVersionId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "CompanyId", "SerialThresholdConfigVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_GoodsReceiptId_GateEntryLineId",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "GoodsReceiptId", "GateEntryLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_GoodsReceiptId_LineNumber",
                schema: "advance",
                table: "goods_receipt_lines",
                columns: new[] { "GoodsReceiptId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_ItemCategoryIdSnapshot",
                schema: "advance",
                table: "goods_receipt_lines",
                column: "ItemCategoryIdSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_ItemId",
                schema: "advance",
                table: "goods_receipt_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId",
                schema: "advance",
                table: "goods_receipts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_GateEntryId",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "GateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_GrnNumber",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "GrnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_PurchaseOrderId_ReceivedAt",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "PurchaseOrderId", "ReceivedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_QcCompletionDaysConfigVersionId",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "QcCompletionDaysConfigVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_QcDueAt",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "QcDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_CompanyId_VendorBillNumber",
                schema: "advance",
                table: "goods_receipts",
                columns: new[] { "CompanyId", "VendorBillNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_FinalizedByEmployeeId",
                schema: "advance",
                table: "goods_receipts",
                column: "FinalizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_GateEntryId",
                schema: "advance",
                table: "goods_receipts",
                column: "GateEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_ReceivedByEmployeeId",
                schema: "advance",
                table: "goods_receipts",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_ReversesGoodsReceiptId",
                schema: "advance",
                table: "goods_receipts",
                column: "ReversesGoodsReceiptId",
                unique: true,
                filter: "\"DocumentKind\"='REVERSAL' AND \"Status\"='FINALIZED'");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_VendorId",
                schema: "advance",
                table: "goods_receipts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serials_CompanyId_ItemId_StoredSerialNumber",
                schema: "advance",
                table: "inventory_serials",
                columns: new[] { "CompanyId", "ItemId", "StoredSerialNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serials_CompanyId_NormalizedStoredSerialNumber",
                schema: "advance",
                table: "inventory_serials",
                columns: new[] { "CompanyId", "NormalizedStoredSerialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serials_FirstCapturedByEmployeeId",
                schema: "advance",
                table: "inventory_serials",
                column: "FirstCapturedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serials_ItemId",
                schema: "advance",
                table: "inventory_serials",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_stores_document_status_history_goods_receipts_CompanyId_Goo~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "GoodsReceiptId" },
                principalSchema: "advance",
                principalTable: "goods_receipts",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(FirstStoresPart2Sql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart2Sql.Down);

            migrationBuilder.DropForeignKey(
                name: "FK_stores_document_status_history_goods_receipts_CompanyId_Goo~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropTable(
                name: "goods_receipt_line_serials",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "goods_receipt_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_serials",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "goods_receipts",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_CompanyId_GoodsReceiptId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_GoodsReceiptId_OccurredAt",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stores_document_status_part2_source",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.AlterColumn<Guid>(
                name: "GateEntryId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
