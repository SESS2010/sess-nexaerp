using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterialIssueRequestAndCustodyIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(MaterialIssueCustodySql.Preflight);
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Situation",
                schema: "advance",
                table: "material_issue_requests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CustomerPoBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedBomBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExcessBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ExcessClassification",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ProductionBomBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedBaseQuantity",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                schema: "advance",
                table: "material_issue_request_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "material_issue_excess_decisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DecidedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_issue_excess_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_issue_excess_decisions_employee_role_assignments_R~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_excess_decisions_employees_DecidedByEmployee~",
                        column: x => x.DecidedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_excess_decisions_material_issue_request_line~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_request_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_issue_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialIssueId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_material_issue_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "material_issues",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedToEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReturnDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StockPostingBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    IssuedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_issues", x => x.Id);
                    table.UniqueConstraint("AK_material_issues_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_issues_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_employee_role_assignments_ResolvedRoleAssig~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_employees_IssuedByEmployeeId",
                        column: x => x.IssuedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_employees_IssuedToEmployeeId",
                        column: x => x.IssuedToEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_material_issue_requests_CompanyId_MaterialI~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestId },
                        principalSchema: "advance",
                        principalTable: "material_issue_requests",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issues_stock_posting_batches_StockPostingBatchId",
                        column: x => x.StockPostingBatchId,
                        principalSchema: "advance",
                        principalTable: "stock_posting_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_issue_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueRequestLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCustodyAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToCustodyAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginGoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptLineLotAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcInspectionLotDispositionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_issue_lines", x => x.Id);
                    table.UniqueConstraint("AK_material_issue_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_issue_lines_goods_receipt_line_lot_allocations_Com~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_line_lot_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_goods_receipt_lines_CompanyId_OriginGo~",
                        columns: x => new { x.CompanyId, x.OriginGoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_inventory_custody_case_lines_CompanyId~",
                        columns: x => new { x.CompanyId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_material_issue_request_lines_CompanyId~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_request_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_material_issues_CompanyId_MaterialIssu~",
                        columns: x => new { x.CompanyId, x.MaterialIssueId },
                        principalSchema: "advance",
                        principalTable: "material_issues",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_lines_qc_inspection_lot_dispositions_Company~",
                        columns: x => new { x.CompanyId, x.QcInspectionLotDispositionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_lot_dispositions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "MaterialIssueId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "MaterialIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements",
                column: "MaterialIssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "material_issue_request_lines",
                column: "CustomerPurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_UomId",
                schema: "advance",
                table: "material_issue_request_lines",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_order_lines_ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_order_lines_UomId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_excess_decisions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "material_issue_excess_decisions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_excess_decisions_CompanyId_MaterialIssueRequ~",
                schema: "advance",
                table: "material_issue_excess_decisions",
                columns: new[] { "CompanyId", "MaterialIssueRequestLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_excess_decisions_DecidedByEmployeeId",
                schema: "advance",
                table: "material_issue_excess_decisions",
                column: "DecidedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_excess_decisions_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "material_issue_excess_decisions",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_history_CorrelationId",
                schema: "advance",
                table: "material_issue_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_CustodyCaseLineId",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_GoodsReceiptLineLotAllocatio~",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_MaterialIssueId",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "MaterialIssueId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_MaterialIssueRequestLineId",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "MaterialIssueRequestLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_OriginGoodsReceiptLineId",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "OriginGoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_CompanyId_QcInspectionLotDispositionId",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "CompanyId", "QcInspectionLotDispositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_ItemId",
                schema: "advance",
                table: "material_issue_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_lines_MaterialIssueId_LineNumber",
                schema: "advance",
                table: "material_issue_lines",
                columns: new[] { "MaterialIssueId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_CompanyId",
                schema: "advance",
                table: "material_issues",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "material_issues",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_CompanyId_IssueNumber",
                schema: "advance",
                table: "material_issues",
                columns: new[] { "CompanyId", "IssueNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_CompanyId_JobOrderId",
                schema: "advance",
                table: "material_issues",
                columns: new[] { "CompanyId", "JobOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_CompanyId_MaterialIssueRequestId",
                schema: "advance",
                table: "material_issues",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_IssuedByEmployeeId",
                schema: "advance",
                table: "material_issues",
                column: "IssuedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_IssuedToEmployeeId",
                schema: "advance",
                table: "material_issues",
                column: "IssuedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "material_issues",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issues_StockPostingBatchId",
                schema: "advance",
                table: "material_issues",
                column: "StockPostingBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_purchase_order_lines_items_ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                column: "ItemId",
                principalSchema: "advance",
                principalTable: "items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_purchase_order_lines_uoms_UomId",
                schema: "advance",
                table: "customer_purchase_order_lines",
                column: "UomId",
                principalSchema: "advance",
                principalTable: "uoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_material_issue_request_lines_customer_purchase_order_lines_~",
                schema: "advance",
                table: "material_issue_request_lines",
                column: "CustomerPurchaseOrderLineId",
                principalSchema: "advance",
                principalTable: "customer_purchase_order_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_material_issue_request_lines_uoms_UomId",
                schema: "advance",
                table: "material_issue_request_lines",
                column: "UomId",
                principalSchema: "advance",
                principalTable: "uoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_material_issue_lines_CompanyId_MaterialIssu~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialIssueLineId" },
                principalSchema: "advance",
                principalTable: "material_issue_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_material_issues_CompanyId_MaterialIss~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "MaterialIssueId" },
                principalSchema: "advance",
                principalTable: "material_issues",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(MaterialIssueCustodySql.Up);
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.MaterialIssueAwareMovementGuard);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(MaterialIssueCustodySql.Down);
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.ActiveMovementGuard);
            migrationBuilder.DropForeignKey(
                name: "FK_customer_purchase_order_lines_items_ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_purchase_order_lines_uoms_UomId",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_material_issue_request_lines_customer_purchase_order_lines_~",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_material_issue_request_lines_uoms_UomId",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_material_issue_lines_CompanyId_MaterialIssu~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_material_issues_CompanyId_MaterialIss~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropTable(
                name: "material_issue_excess_decisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_issue_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_issue_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_issues",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_material_issue_request_lines_CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropIndex(
                name: "IX_material_issue_request_lines_UomId",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropIndex(
                name: "IX_customer_purchase_order_lines_ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_customer_purchase_order_lines_UomId",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "MaterialIssueId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "MaterialIssueLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "Situation",
                schema: "advance",
                table: "material_issue_requests");

            migrationBuilder.DropColumn(
                name: "CustomerPoBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "EstimatedBomBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "ExcessBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "ExcessClassification",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "ProductionBomBaseQuantitySnapshot",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "RequestedBaseQuantity",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "UomId",
                schema: "advance",
                table: "material_issue_request_lines");

            migrationBuilder.DropColumn(
                name: "ItemId",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropColumn(
                name: "UomId",
                schema: "advance",
                table: "customer_purchase_order_lines");
        }
    }
}
