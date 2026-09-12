using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GovernedOpeningStockThreeActorCeremony : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(OpeningStockSql.Preflight);

            migrationBuilder.DropIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fifo_cost_layer",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.AddColumn<Guid>(
                name: "OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OpeningStockLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                schema: "advance",
                table: "inventory_lots",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "OpeningStockLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "opening_stock_import_staging_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opening_stock_import_staging_lines", x => x.Id);
                    table.UniqueConstraint("AK_opening_stock_import_staging_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_opening_stock_staging_quantity", "\"Quantity\">0");
                    table.CheckConstraint("CK_opening_stock_staging_rate", "\"UnitRate\">=0");
                    table.ForeignKey(
                        name: "FK_opening_stock_import_staging_lines_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_import_staging_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_import_staging_lines_rack_bins_CompanyId_Rack~",
                        columns: x => new { x.CompanyId, x.RackBinId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_import_staging_lines_warehouses_CompanyId_War~",
                        columns: x => new { x.CompanyId, x.WarehouseId },
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opening_stocks",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CountedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CountedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CountReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CountIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountRequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    ValuedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValueActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValueRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValueRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ValuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValueReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ValueIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValueRequestFingerprint = table.Column<string>(type: "character(64)", nullable: true),
                    AuthorizedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorizationRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorizationRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AuthorizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AuthorizationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AuthorizationIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorizationRequestFingerprint = table.Column<string>(type: "character(64)", nullable: true),
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
                    table.PrimaryKey("PK_opening_stocks", x => x.Id);
                    table.UniqueConstraint("AK_opening_stocks_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_opening_stock_period", "\"PeriodStart\"<=\"PeriodEnd\"");
                    table.CheckConstraint("CK_opening_stock_separate_actors", "\"ValuedByEmployeeId\" IS NULL OR (\"CountedByEmployeeId\"<>\"ValuedByEmployeeId\" AND (\"AuthorizedByEmployeeId\" IS NULL OR (\"AuthorizedByEmployeeId\"<>\"CountedByEmployeeId\" AND \"AuthorizedByEmployeeId\"<>\"ValuedByEmployeeId\")))");
                    table.CheckConstraint("CK_opening_stock_status", "\"Status\" IN ('COUNTED','VALUED','POSTED')");
                    table.ForeignKey(
                        name: "FK_opening_stocks_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employee_role_assignments_AuthorizationRoleA~",
                        column: x => x.AuthorizationRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employee_role_assignments_CountRoleAssignmen~",
                        column: x => x.CountRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employee_role_assignments_ValueRoleAssignmen~",
                        column: x => x.ValueRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employees_AuthorizedByEmployeeId",
                        column: x => x.AuthorizedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employees_CountedByEmployeeId",
                        column: x => x.CountedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_employees_ValuedByEmployeeId",
                        column: x => x.ValuedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_master_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "advance",
                        principalTable: "master_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stocks_stock_posting_batches_CompanyId_StockPosting~",
                        columns: x => new { x.CompanyId, x.StockPostingBatchId },
                        principalSchema: "advance",
                        principalTable: "stock_posting_batches",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opening_stock_events",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpeningStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opening_stock_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_opening_stock_events_employee_role_assignments_ResolvedRole~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_events_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_events_opening_stocks_CompanyId_OpeningStockId",
                        columns: x => new { x.CompanyId, x.OpeningStockId },
                        principalSchema: "advance",
                        principalTable: "opening_stocks",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "opening_stock_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpeningStockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportStagingLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    LineReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    LineValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    FifoInventoryCostLayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opening_stock_lines", x => x.Id);
                    table.UniqueConstraint("AK_opening_stock_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_opening_stock_line_posted_identity", "num_nonnulls(\"InventoryProvenanceLayerId\",\"FifoInventoryCostLayerId\") IN (0,2)");
                    table.CheckConstraint("CK_opening_stock_line_values", "\"LineNumber\">0 AND \"Quantity\">0 AND \"UnitRate\">=0 AND \"LineValue\"=\"Quantity\"*\"UnitRate\"");
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_fifo_inventory_cost_layers_CompanyId_Fi~",
                        columns: x => new { x.CompanyId, x.FifoInventoryCostLayerId },
                        principalSchema: "advance",
                        principalTable: "fifo_inventory_cost_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_inventory_lots_CompanyId_InventoryLotId",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_inventory_provenance_layers_CompanyId_I~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_inventory_serials_CompanyId_InventorySe~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_opening_stock_import_staging_lines_Comp~",
                        columns: x => new { x.CompanyId, x.ImportStagingLineId },
                        principalSchema: "advance",
                        principalTable: "opening_stock_import_staging_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_opening_stocks_CompanyId_OpeningStockId",
                        columns: x => new { x.CompanyId, x.OpeningStockId },
                        principalSchema: "advance",
                        principalTable: "opening_stocks",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_rack_bins_CompanyId_RackBinId",
                        columns: x => new { x.CompanyId, x.RackBinId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_warehouse_condition_locations_CompanyId~",
                        columns: x => new { x.CompanyId, x.WarehouseConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opening_stock_lines_warehouses_CompanyId_WarehouseId",
                        columns: x => new { x.CompanyId, x.WarehouseId },
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "OpeningStockId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "OpeningStockId",
                unique: true,
                filter: "\"OpeningStockId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_OpeningStockLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OpeningStockLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_OpeningStockLineId",
                schema: "advance",
                table: "stock_movements",
                column: "OpeningStockLineId");

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_OpeningStockLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "OpeningStockLineId" },
                unique: true,
                filter: "\"OpeningStockLineId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_fifo_cost_layer",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                sql: "\"QuantityReceived\">0 AND \"UnitCost\">=0 AND \"LayerValue\">=0 AND \"CostBasis\" IN ('PO_PROVISIONAL_IDENTICAL','OPENING_LANDED') AND num_nonnulls(\"GoodsReceiptLineId\",\"OpeningStockLineId\")=1");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_events_ActorEmployeeId",
                schema: "advance",
                table: "opening_stock_events",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_events_CompanyId_OpeningStockId",
                schema: "advance",
                table: "opening_stock_events",
                columns: new[] { "CompanyId", "OpeningStockId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_events_CorrelationId",
                schema: "advance",
                table: "opening_stock_events",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_events_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "opening_stock_events",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_import_staging_lines_CompanyId",
                schema: "advance",
                table: "opening_stock_import_staging_lines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_import_staging_lines_CompanyId_RackBinId",
                schema: "advance",
                table: "opening_stock_import_staging_lines",
                columns: new[] { "CompanyId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_import_staging_lines_CompanyId_WarehouseId",
                schema: "advance",
                table: "opening_stock_import_staging_lines",
                columns: new[] { "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_import_staging_lines_ItemId",
                schema: "advance",
                table: "opening_stock_import_staging_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_FifoInventoryCostLayerId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "FifoInventoryCostLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_ImportStagingLineId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "ImportStagingLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_InventoryLotId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_InventoryProvenanceLayerId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_InventorySerialId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_OpeningStockId_LineNumber",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "OpeningStockId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_OpeningStockId_LineReference",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "OpeningStockId", "LineReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_RackBinId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_WarehouseConditionLocationId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "WarehouseConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_CompanyId_WarehouseId",
                schema: "advance",
                table: "opening_stock_lines",
                columns: new[] { "CompanyId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_ImportStagingLineId",
                schema: "advance",
                table: "opening_stock_lines",
                column: "ImportStagingLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stock_lines_ItemId",
                schema: "advance",
                table: "opening_stock_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_AuthorizationRoleAssignmentId",
                schema: "advance",
                table: "opening_stocks",
                column: "AuthorizationRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_AuthorizedByEmployeeId",
                schema: "advance",
                table: "opening_stocks",
                column: "AuthorizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId",
                schema: "advance",
                table: "opening_stocks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_AuthorizationIdempotencyKey",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "AuthorizationIdempotencyKey" },
                unique: true,
                filter: "\"AuthorizationIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_CountIdempotencyKey",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "CountIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_ImportBatchId",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "ImportBatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_PeriodStart_PeriodEnd",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_StockPostingBatchId",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "StockPostingBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CompanyId_ValueIdempotencyKey",
                schema: "advance",
                table: "opening_stocks",
                columns: new[] { "CompanyId", "ValueIdempotencyKey" },
                unique: true,
                filter: "\"ValueIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CountedByEmployeeId",
                schema: "advance",
                table: "opening_stocks",
                column: "CountedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_CountRoleAssignmentId",
                schema: "advance",
                table: "opening_stocks",
                column: "CountRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_ImportBatchId",
                schema: "advance",
                table: "opening_stocks",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_ValuedByEmployeeId",
                schema: "advance",
                table: "opening_stocks",
                column: "ValuedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_opening_stocks_ValueRoleAssignmentId",
                schema: "advance",
                table: "opening_stocks",
                column: "ValueRoleAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_fifo_inventory_cost_layers_opening_stock_lines_CompanyId_Op~",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "OpeningStockLineId" },
                principalSchema: "advance",
                principalTable: "opening_stock_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_opening_stock_lines_CompanyId_OpeningStockL~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OpeningStockLineId" },
                principalSchema: "advance",
                principalTable: "opening_stock_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_opening_stocks_CompanyId_OpeningStock~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "OpeningStockId" },
                principalSchema: "advance",
                principalTable: "opening_stocks",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(OpeningStockSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(OpeningStockSql.Down);

            migrationBuilder.DropForeignKey(
                name: "FK_fifo_inventory_cost_layers_opening_stock_lines_CompanyId_Op~",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_opening_stock_lines_CompanyId_OpeningStockL~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_opening_stocks_CompanyId_OpeningStock~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropTable(
                name: "opening_stock_events",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "opening_stock_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "opening_stock_import_staging_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "opening_stocks",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_OpeningStockLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_OpeningStockLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.DropIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_OpeningStockLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_fifo_cost_layer",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.DropColumn(
                name: "OpeningStockId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "OpeningStockLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "OpeningStockLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers");

            migrationBuilder.AlterColumn<Guid>(
                name: "VendorId",
                schema: "advance",
                table: "inventory_lots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fifo_inventory_cost_layers_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_fifo_cost_layer",
                schema: "advance",
                table: "fifo_inventory_cost_layers",
                sql: "\"QuantityReceived\">0 AND \"UnitCost\">=0 AND \"LayerValue\">=0 AND \"CostBasis\"='PO_PROVISIONAL_IDENTICAL'");
        }
    }
}
