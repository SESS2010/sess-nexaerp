using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VendorBillAndAcceptedBillCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(VendorBillCostingSql.Preflight);
            migrationBuilder.CreateTable(
                name: "fifo_inventory_cost_layers",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    LayerValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CostBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fifo_inventory_cost_layers", x => x.Id);
                    table.UniqueConstraint("AK_fifo_inventory_cost_layers_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_fifo_cost_layer", "\"QuantityReceived\">0 AND \"UnitCost\">=0 AND \"LayerValue\">=0 AND \"CostBasis\"='PO_PROVISIONAL_IDENTICAL'");
                    table.ForeignKey(
                        name: "FK_fifo_inventory_cost_layers_goods_receipt_lines_CompanyId_Go~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fifo_inventory_cost_layers_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bills",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BillDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreateIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreateRequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DecisionRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecisionIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DecisionRequestFingerprint = table.Column<string>(type: "character(64)", nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReversalRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReversalIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReversalRequestFingerprint = table.Column<string>(type: "character(64)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bills", x => x.Id);
                    table.UniqueConstraint("AK_vendor_bills_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_vendor_bill_match", "\"MatchStatus\" IN ('MATCHED','PRICE_MISMATCH')");
                    table.CheckConstraint("CK_vendor_bill_status", "\"Status\" IN ('DRAFT','ACCEPTED','REJECTED','REVERSED')");
                    table.CheckConstraint("CK_vendor_bill_value", "\"TotalPayableValue\">=0");
                    table.ForeignKey(
                        name: "FK_vendor_bills_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employee_role_assignments_DecisionRoleAssignme~",
                        column: x => x.DecisionRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employee_role_assignments_ResolvedRoleAssignme~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employee_role_assignments_ReversalRoleAssignme~",
                        column: x => x.ReversalRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employees_DecidedByEmployeeId",
                        column: x => x.DecidedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_employees_ReversedByEmployeeId",
                        column: x => x.ReversedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_goods_receipts_CompanyId_GoodsReceiptId",
                        columns: x => new { x.CompanyId, x.GoodsReceiptId },
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bills_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fifo_cost_consumptions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FifoInventoryCostLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ConsumedValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fifo_cost_consumptions", x => x.Id);
                    table.CheckConstraint("CK_fifo_cost_consumption", "\"Quantity\">0 AND \"UnitCost\">=0 AND \"ConsumedValue\">=0");
                    table.ForeignKey(
                        name: "FK_fifo_cost_consumptions_fifo_inventory_cost_layers_CompanyId~",
                        columns: x => new { x.CompanyId, x.FifoInventoryCostLayerId },
                        principalSchema: "advance",
                        principalTable: "fifo_inventory_cost_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fifo_cost_consumptions_material_issue_lines_CompanyId_Mater~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_bill_history_vendor_bills_CompanyId_VendorBillId",
                        columns: x => new { x.CompanyId, x.VendorBillId },
                        principalSchema: "advance",
                        principalTable: "vendor_bills",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BilledQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExpectedUnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    BilledUnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExpectedPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    BilledPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    MatchStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_lines", x => x.Id);
                    table.UniqueConstraint("AK_vendor_bill_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_vendor_bill_line_values", "\"BilledQuantity\">0 AND \"ExpectedUnitRate\">=0 AND \"BilledUnitRate\">=0 AND \"ExpectedPayableValue\">=0 AND \"BilledPayableValue\">=0");
                    table.ForeignKey(
                        name: "FK_vendor_bill_lines_goods_receipt_lines_CompanyId_GoodsReceip~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_lines_purchase_order_lines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "advance",
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_lines_vendor_bills_CompanyId_VendorBillId",
                        columns: x => new { x.CompanyId, x.VendorBillId },
                        principalSchema: "advance",
                        principalTable: "vendor_bills",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_cost_allocations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    FifoInventoryCostLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocatedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AcceptedValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_cost_allocations", x => x.Id);
                    table.CheckConstraint("CK_vendor_bill_cost_allocation", "\"AllocatedQuantity\">0 AND \"AcceptedValue\">=0");
                    table.ForeignKey(
                        name: "FK_vendor_bill_cost_allocations_fifo_inventory_cost_layers_Com~",
                        columns: x => new { x.CompanyId, x.FifoInventoryCostLayerId },
                        principalSchema: "advance",
                        principalTable: "fifo_inventory_cost_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_cost_allocations_vendor_bill_lines_CompanyId_Ve~",
                        columns: x => new { x.CompanyId, x.VendorBillLineId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fifo_cost_consumptions_CompanyId_FifoInventoryCostLayerId_M~",
                schema: "advance",
                table: "fifo_cost_consumptions",
                columns: new[] { "CompanyId", "FifoInventoryCostLayerId", "MaterialIssueLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fifo_cost_consumptions_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "fifo_cost_consumptions",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_ItemId_ReceivedAt",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "ItemId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_ItemId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_cost_allocations_CompanyId_FifoInventoryCostLay~",
                schema: "advance",
                table: "vendor_bill_cost_allocations",
                columns: new[] { "CompanyId", "FifoInventoryCostLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_cost_allocations_CompanyId_VendorBillLineId_Fif~",
                schema: "advance",
                table: "vendor_bill_cost_allocations",
                columns: new[] { "CompanyId", "VendorBillLineId", "FifoInventoryCostLayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_history_CompanyId_VendorBillId",
                schema: "advance",
                table: "vendor_bill_history",
                columns: new[] { "CompanyId", "VendorBillId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_history_CorrelationId",
                schema: "advance",
                table: "vendor_bill_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_lines_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "vendor_bill_lines",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_lines_CompanyId_VendorBillId_GoodsReceiptLineId",
                schema: "advance",
                table: "vendor_bill_lines",
                columns: new[] { "CompanyId", "VendorBillId", "GoodsReceiptLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_lines_ItemId",
                schema: "advance",
                table: "vendor_bill_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_lines_PurchaseOrderLineId",
                schema: "advance",
                table: "vendor_bill_lines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_lines_VendorBillId_LineNumber",
                schema: "advance",
                table: "vendor_bill_lines",
                columns: new[] { "VendorBillId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId",
                schema: "advance",
                table: "vendor_bills",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_BillNumber",
                schema: "advance",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "BillNumber" },
                unique: true,
                filter: "\"Status\"<>'REVERSED'");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_CreateIdempotencyKey",
                schema: "advance",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "CreateIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_DecisionIdempotencyKey",
                schema: "advance",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "DecisionIdempotencyKey" },
                unique: true,
                filter: "\"DecisionIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_GoodsReceiptId",
                schema: "advance",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "GoodsReceiptId" },
                unique: true,
                filter: "\"Status\"<>'REVERSED'");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CompanyId_ReversalIdempotencyKey",
                schema: "advance",
                table: "vendor_bills",
                columns: new[] { "CompanyId", "ReversalIdempotencyKey" },
                unique: true,
                filter: "\"ReversalIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_CreatedByEmployeeId",
                schema: "advance",
                table: "vendor_bills",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_DecidedByEmployeeId",
                schema: "advance",
                table: "vendor_bills",
                column: "DecidedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_DecisionRoleAssignmentId",
                schema: "advance",
                table: "vendor_bills",
                column: "DecisionRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_PurchaseOrderId",
                schema: "advance",
                table: "vendor_bills",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "vendor_bills",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_ReversalRoleAssignmentId",
                schema: "advance",
                table: "vendor_bills",
                column: "ReversalRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_ReversedByEmployeeId",
                schema: "advance",
                table: "vendor_bills",
                column: "ReversedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bills_VendorId",
                schema: "advance",
                table: "vendor_bills",
                column: "VendorId");
            migrationBuilder.Sql(VendorBillCostingSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(VendorBillCostingSql.Down);
            migrationBuilder.DropTable(
                name: "fifo_cost_consumptions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bill_cost_allocations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bill_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "fifo_inventory_cost_layers",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bill_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_bills",
                schema: "advance");
        }
    }
}
