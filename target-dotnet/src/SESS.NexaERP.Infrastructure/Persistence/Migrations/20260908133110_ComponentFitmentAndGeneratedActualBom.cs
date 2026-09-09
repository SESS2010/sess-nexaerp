using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComponentFitmentAndGeneratedActualBom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ComponentFitmentActualBomSql.Preflight);
            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedChargeValue",
                schema: "advance",
                table: "vendor_bill_cost_allocations",
                type: "numeric(24,6)",
                precision: 24,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ComponentFitmentId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComponentFitmentId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "actual_boms",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actual_boms", x => x.Id);
                    table.UniqueConstraint("AK_actual_boms_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_actual_boms_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_boms_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "component_fitments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FitmentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReverifiesFitmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    FittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_component_fitments", x => x.Id);
                    table.UniqueConstraint("AK_component_fitments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_component_fitment_quantity", "\"QuantityBase\">0 AND \"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"ConfirmationNote\"))>0");
                    table.ForeignKey(
                        name: "FK_component_fitments_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitments_component_fitments_CompanyId_ReverifiesF~",
                        columns: x => new { x.CompanyId, x.ReverifiesFitmentId },
                        principalSchema: "advance",
                        principalTable: "component_fitments",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitments_employee_role_assignments_ResolvedRoleAs~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitments_employees_ConfirmedByEmployeeId",
                        column: x => x.ConfirmedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitments_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitments_material_issue_lines_CompanyId_MaterialI~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "component_fitment_reversals",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentFitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReversedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsSelfReversal = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_component_fitment_reversals", x => x.Id);
                    table.UniqueConstraint("AK_component_fitment_reversals_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_component_fitment_reversal", "\"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"Reason\"))>0");
                    table.ForeignKey(
                        name: "FK_component_fitment_reversals_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitment_reversals_component_fitments_CompanyId_Co~",
                        columns: x => new { x.CompanyId, x.ComponentFitmentId },
                        principalSchema: "advance",
                        principalTable: "component_fitments",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitment_reversals_employee_role_assignments_Resol~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_component_fitment_reversals_employees_ReversedByEmployeeId",
                        column: x => x.ReversedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actual_bom_entries",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualBomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentFitmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComponentFitmentReversalId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntryKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNumberSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VendorBillLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorBillNumberSnapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AcceptedMaterialValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AllocatedChargeValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalAcceptedValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_actual_bom_entries", x => x.Id);
                    table.CheckConstraint("CK_actual_bom_entry_source", "num_nonnulls(\"ComponentFitmentId\",\"ComponentFitmentReversalId\")=1 AND ((\"EntryKind\"='FITMENT' AND \"QuantityBase\">0 AND \"AcceptedMaterialValue\">=0 AND \"AllocatedChargeValue\">=0 AND \"TotalAcceptedValue\">=0) OR (\"EntryKind\"='REVERSAL' AND \"QuantityBase\"<0 AND \"AcceptedMaterialValue\"<=0 AND \"AllocatedChargeValue\"<=0 AND \"TotalAcceptedValue\"<=0))");
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_actual_boms_CompanyId_ActualBomId",
                        columns: x => new { x.CompanyId, x.ActualBomId },
                        principalSchema: "advance",
                        principalTable: "actual_boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_component_fitment_reversals_CompanyId_Co~",
                        columns: x => new { x.CompanyId, x.ComponentFitmentReversalId },
                        principalSchema: "advance",
                        principalTable: "component_fitment_reversals",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_component_fitments_CompanyId_ComponentFi~",
                        columns: x => new { x.CompanyId, x.ComponentFitmentId },
                        principalSchema: "advance",
                        principalTable: "component_fitments",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_goods_receipt_lines_CompanyId_GoodsRecei~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_inventory_lots_CompanyId_InventoryLotId",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_inventory_provenance_layers_CompanyId_In~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_inventory_serials_CompanyId_InventorySer~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_material_issue_lines_CompanyId_MaterialI~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_actual_bom_entries_vendor_bill_lines_CompanyId_VendorBillLi~",
                        columns: x => new { x.CompanyId, x.VendorBillLineId },
                        principalSchema: "advance",
                        principalTable: "vendor_bill_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "ComponentFitmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "ComponentFitmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_ActualBomId_OccurredAt",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "ActualBomId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "ComponentFitmentId" },
                unique: true,
                filter: "\"ComponentFitmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_ComponentFitmentReversalId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "ComponentFitmentReversalId" },
                unique: true,
                filter: "\"ComponentFitmentReversalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_InventoryLotId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_InventoryProvenanceLayerId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_InventorySerialId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_CompanyId_VendorBillLineId",
                schema: "advance",
                table: "actual_bom_entries",
                columns: new[] { "CompanyId", "VendorBillLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_ItemId",
                schema: "advance",
                table: "actual_bom_entries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_actual_bom_entries_UomId",
                schema: "advance",
                table: "actual_bom_entries",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_actual_boms_CompanyId",
                schema: "advance",
                table: "actual_boms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_actual_boms_CompanyId_JobOrderId",
                schema: "advance",
                table: "actual_boms",
                columns: new[] { "CompanyId", "JobOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_fitment_reversals_CompanyId",
                schema: "advance",
                table: "component_fitment_reversals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitment_reversals_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "component_fitment_reversals",
                columns: new[] { "CompanyId", "ComponentFitmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_fitment_reversals_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "component_fitment_reversals",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_fitment_reversals_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "component_fitment_reversals",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitment_reversals_ReversedByEmployeeId",
                schema: "advance",
                table: "component_fitment_reversals",
                column: "ReversedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId",
                schema: "advance",
                table: "component_fitments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId_FitmentNumber",
                schema: "advance",
                table: "component_fitments",
                columns: new[] { "CompanyId", "FitmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "component_fitments",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId_JobOrderId_MaterialIssueLineId",
                schema: "advance",
                table: "component_fitments",
                columns: new[] { "CompanyId", "JobOrderId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "component_fitments",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_CompanyId_ReverifiesFitmentId",
                schema: "advance",
                table: "component_fitments",
                columns: new[] { "CompanyId", "ReverifiesFitmentId" },
                unique: true,
                filter: "\"ReverifiesFitmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_ConfirmedByEmployeeId",
                schema: "advance",
                table: "component_fitments",
                column: "ConfirmedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_component_fitments_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "component_fitments",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_component_fitments_CompanyId_ComponentFitme~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "ComponentFitmentId" },
                principalSchema: "advance",
                principalTable: "component_fitments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_component_fitments_CompanyId_Componen~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "ComponentFitmentId" },
                principalSchema: "advance",
                principalTable: "component_fitments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ComponentFitmentActualBomSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ComponentFitmentActualBomSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_component_fitments_CompanyId_ComponentFitme~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_component_fitments_CompanyId_Componen~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropTable(
                name: "actual_bom_entries",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "actual_boms",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "component_fitment_reversals",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "component_fitments",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_ComponentFitmentId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "AllocatedChargeValue",
                schema: "advance",
                table: "vendor_bill_cost_allocations");

            migrationBuilder.DropColumn(
                name: "ComponentFitmentId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "ComponentFitmentId",
                schema: "advance",
                table: "stock_movements");
        }
    }
}
