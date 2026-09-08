using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterialReturnToStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MaterialReturnSql.Preflight);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "material_returns",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaterialIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclaredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreateIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreateRequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcceptedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AcceptanceReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AcceptanceIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcceptanceRequestFingerprint = table.Column<string>(type: "character(64)", nullable: true),
                    StockPostingBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_returns", x => x.Id);
                    table.UniqueConstraint("AK_material_returns_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_returns_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_employee_role_assignments_AcceptedRoleAssi~",
                        column: x => x.AcceptedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_employee_role_assignments_ResolvedRoleAssi~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_employees_AcceptedByEmployeeId",
                        column: x => x.AcceptedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_employees_ReturnedByEmployeeId",
                        column: x => x.ReturnedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_material_issues_CompanyId_MaterialIssueId",
                        columns: x => new { x.CompanyId, x.MaterialIssueId },
                        principalSchema: "advance",
                        principalTable: "material_issues",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_returns_stock_posting_batches_StockPostingBatchId",
                        column: x => x.StockPostingBatchId,
                        principalSchema: "advance",
                        principalTable: "stock_posting_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_return_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialReturnId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_material_return_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_material_return_history_material_returns_CompanyId_Material~",
                        columns: x => new { x.CompanyId, x.MaterialReturnId },
                        principalSchema: "advance",
                        principalTable: "material_returns",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_return_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReportedConsumedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReportedStillHeldQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ScanCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_return_lines", x => x.Id);
                    table.UniqueConstraint("AK_material_return_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_return_lines_inventory_serials_CompanyId_Inventory~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_return_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_return_lines_material_issue_lines_CompanyId_Materi~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_return_lines_material_returns_CompanyId_MaterialRe~",
                        columns: x => new { x.CompanyId, x.MaterialReturnId },
                        principalSchema: "advance",
                        principalTable: "material_returns",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "MaterialReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "MaterialReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialReturnLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements",
                column: "MaterialReturnLineId");

            migrationBuilder.CreateIndex(
                name: "IX_material_return_history_CompanyId_MaterialReturnId",
                schema: "advance",
                table: "material_return_history",
                columns: new[] { "CompanyId", "MaterialReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_return_history_CorrelationId",
                schema: "advance",
                table: "material_return_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_return_lines_CompanyId_InventorySerialId",
                schema: "advance",
                table: "material_return_lines",
                columns: new[] { "CompanyId", "InventorySerialId" },
                unique: true,
                filter: "\"InventorySerialId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_material_return_lines_CompanyId_MaterialIssueLineId",
                schema: "advance",
                table: "material_return_lines",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_return_lines_CompanyId_MaterialReturnId",
                schema: "advance",
                table: "material_return_lines",
                columns: new[] { "CompanyId", "MaterialReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_return_lines_ItemId",
                schema: "advance",
                table: "material_return_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_material_return_lines_MaterialReturnId_LineNumber",
                schema: "advance",
                table: "material_return_lines",
                columns: new[] { "MaterialReturnId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_AcceptedByEmployeeId",
                schema: "advance",
                table: "material_returns",
                column: "AcceptedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_AcceptedRoleAssignmentId",
                schema: "advance",
                table: "material_returns",
                column: "AcceptedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CompanyId",
                schema: "advance",
                table: "material_returns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CompanyId_AcceptanceIdempotencyKey",
                schema: "advance",
                table: "material_returns",
                columns: new[] { "CompanyId", "AcceptanceIdempotencyKey" },
                unique: true,
                filter: "\"AcceptanceIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CompanyId_CreateIdempotencyKey",
                schema: "advance",
                table: "material_returns",
                columns: new[] { "CompanyId", "CreateIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CompanyId_MaterialIssueId",
                schema: "advance",
                table: "material_returns",
                columns: new[] { "CompanyId", "MaterialIssueId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CompanyId_ReturnNumber",
                schema: "advance",
                table: "material_returns",
                columns: new[] { "CompanyId", "ReturnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_CreatedByEmployeeId",
                schema: "advance",
                table: "material_returns",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "material_returns",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_ReturnedByEmployeeId",
                schema: "advance",
                table: "material_returns",
                column: "ReturnedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_returns_StockPostingBatchId",
                schema: "advance",
                table: "material_returns",
                column: "StockPostingBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_material_return_lines_CompanyId_MaterialRet~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MaterialReturnLineId" },
                principalSchema: "advance",
                principalTable: "material_return_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_material_returns_CompanyId_MaterialRe~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "MaterialReturnId" },
                principalSchema: "advance",
                principalTable: "material_returns",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(MaterialReturnSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MaterialReturnSql.Down);

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_material_return_lines_CompanyId_MaterialRet~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_material_returns_CompanyId_MaterialRe~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropTable(
                name: "material_return_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_return_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_returns",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "MaterialReturnId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "MaterialReturnLineId",
                schema: "advance",
                table: "stock_movements");
        }
    }
}
