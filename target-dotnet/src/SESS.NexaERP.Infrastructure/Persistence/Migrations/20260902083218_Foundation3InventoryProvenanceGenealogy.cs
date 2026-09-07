using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Foundation3InventoryProvenanceGenealogy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(Foundation3InventoryProvenanceGenealogySql.PreUp);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "LedgerSchemaVersion",
                schema: "advance",
                table: "stock_movements",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)1);

            migrationBuilder.AddColumn<Guid>(
                name: "CustodyAssignmentId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryLotId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryProvenanceLayerId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnershipAccountId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_lot_attribute_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupersedesRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttributesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_lot_attribute_revisions", x => x.Id);
                    table.UniqueConstraint("AK_inventory_lot_attribute_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_lot_attribute_revisions_json", "jsonb_typeof(\"AttributesJson\") = 'object'");
                    table.CheckConstraint("CK_inventory_lot_attribute_revisions_period", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.CheckConstraint("CK_inventory_lot_attribute_revisions_revision", "\"RevisionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_lot_attribute_revisions_employees_RecordedByEmplo~",
                        column: x => x.RecordedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_lot_attribute_revisions_inventory_lot_attribute_r~",
                        columns: x => new { x.CompanyId, x.SupersedesRevisionId },
                        principalSchema: "advance",
                        principalTable: "inventory_lot_attribute_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_lot_attribute_revisions_inventory_lots_CompanyId_~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_layers",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    LayerType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    QuantityCreated = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdentityHash = table.Column<string>(type: "character(64)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_layers", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_layers_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_provenance_layers_hash", "\"IdentityHash\" ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_inventory_provenance_layers_quantity", "\"QuantityCreated\" > 0");
                    table.CheckConstraint("CK_inventory_provenance_layers_status", "\"Status\" IN ('ACTIVE','REVERSED')");
                    table.CheckConstraint("CK_inventory_provenance_layers_type", "\"LayerType\" IN ('RECEIPT','QC_ACCEPTED','QC_REJECTED','CONCESSION_ACCEPTED','CUSTODY','TRANSFORMATION_OUTPUT','RETURN','ADJUSTMENT')");
                    table.ForeignKey(
                        name: "FK_inventory_provenance_layers_inventory_lots_CompanyId_Invent~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_layers_inventory_serials_CompanyId_Inv~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_layers_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_layers_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_serial_genealogy_events",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversesEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_serial_genealogy_events", x => x.Id);
                    table.UniqueConstraint("AK_inventory_serial_genealogy_events_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_serial_genealogy_events_reversal", "(\"EventType\"='REVERSAL' AND \"ReversesEventId\" IS NOT NULL) OR (\"EventType\"<>'REVERSAL' AND \"ReversesEventId\" IS NULL)");
                    table.CheckConstraint("CK_inventory_serial_genealogy_events_type", "\"EventType\" IN ('CREATED','FITTED','REMOVED','REPLACED','TRANSFORMED','CORRECTED','CONCESSION_ACCEPTED','REVERSAL')");
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_events_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_events_inventory_serial_genealog~",
                        columns: x => new { x.CompanyId, x.ReversesEventId },
                        principalSchema: "advance",
                        principalTable: "inventory_serial_genealogy_events",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_events_job_orders_CompanyId_JobO~",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_serial_identity_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    SupersedesRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoredSerialNumberSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedSerialNumberSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_serial_identity_revisions", x => x.Id);
                    table.UniqueConstraint("AK_inventory_serial_identity_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_serial_identity_revisions_normalized", "length(btrim(\"NormalizedSerialNumberSnapshot\")) > 0");
                    table.CheckConstraint("CK_inventory_serial_identity_revisions_period", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.CheckConstraint("CK_inventory_serial_identity_revisions_revision", "\"RevisionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_serial_identity_revisions_employees_RecordedByEmp~",
                        column: x => x.RecordedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_identity_revisions_inventory_serial_identi~",
                        columns: x => new { x.CompanyId, x.SupersedesRevisionId },
                        principalSchema: "advance",
                        principalTable: "inventory_serial_identity_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_identity_revisions_inventory_serials_Compa~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transformations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransformationNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    TransformationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversesTransformationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_inventory_transformations", x => x.Id);
                    table.UniqueConstraint("AK_inventory_transformations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_transformations_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
                    table.CheckConstraint("CK_inventory_transformations_posting", "(\"Status\"='DRAFT' AND \"PostedAt\" IS NULL AND \"PostedByEmployeeId\" IS NULL) OR (\"Status\"<>'DRAFT' AND \"PostedAt\" IS NOT NULL AND \"PostedByEmployeeId\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_transformations_status", "\"Status\" IN ('DRAFT','POSTED','REVERSED')");
                    table.CheckConstraint("CK_inventory_transformations_type", "\"TransformationType\" IN ('KIT_ASSEMBLY','KIT_DISASSEMBLY','REPACK','UOM_CONVERSION','SUBASSEMBLY')");
                    table.ForeignKey(
                        name: "FK_inventory_transformations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformations_employees_PostedByEmployeeId",
                        column: x => x.PostedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformations_inventory_transformations_Company~",
                        columns: x => new { x.CompanyId, x.ReversesTransformationId },
                        principalSchema: "advance",
                        principalTable: "inventory_transformations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_lot_dispositions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineLotAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DiscrepancyPendingQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Disposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DestinationConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspection_lot_dispositions", x => x.Id);
                    table.UniqueConstraint("AK_qc_inspection_lot_dispositions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_qc_inspection_lot_dispositions_decision", "(\"Disposition\"='ACCEPTED' AND \"AcceptedQuantity\">0 AND \"RejectedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='REJECTED' AND \"RejectedQuantity\">0 AND \"AcceptedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='DISCREPANCY_PENDING' AND \"DiscrepancyPendingQuantity\">0 AND \"AcceptedQuantity\"=0 AND \"RejectedQuantity\"=0)");
                    table.CheckConstraint("CK_qc_inspection_lot_dispositions_quantities", "\"InspectedQuantity\" > 0 AND \"AcceptedQuantity\" >= 0 AND \"RejectedQuantity\" >= 0 AND \"DiscrepancyPendingQuantity\" >= 0 AND \"AcceptedQuantity\" + \"RejectedQuantity\" + \"DiscrepancyPendingQuantity\" = \"InspectedQuantity\"");
                    table.ForeignKey(
                        name: "FK_qc_inspection_lot_dispositions_goods_receipt_line_lot_alloc~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_line_lot_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_lot_dispositions_inventory_lots_CompanyId_Inv~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_lot_dispositions_qc_inspection_revisions_Comp~",
                        columns: x => new { x.CompanyId, x.QcInspectionRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_lot_dispositions_warehouse_condition_location~",
                        columns: x => new { x.CompanyId, x.DestinationConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_custody_case_line_origins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_custody_case_line_origins", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_custody_case_line_origins_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_provenance_custody_case_line_origins_inventory_cu~",
                        column: x => x.CustodyCaseLineId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_custody_case_line_origins_inventory_pr~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_goods_receipt_lot_origins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    GoodsReceiptLineLotAllocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_goods_receipt_lot_origins", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_goods_receipt_lot_origins_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_provenance_goods_receipt_lot_origins_goods_receip~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_line_lot_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_goods_receipt_lot_origins_inventory_pr~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_serial_genealogy_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialGenealogyEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromInventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToInventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_serial_genealogy_links", x => x.Id);
                    table.CheckConstraint("CK_inventory_serial_genealogy_links_identity", "\"FromInventorySerialId\" IS NOT NULL OR \"ToInventorySerialId\" IS NOT NULL OR \"FromProvenanceLayerId\" IS NOT NULL OR \"ToProvenanceLayerId\" IS NOT NULL");
                    table.CheckConstraint("CK_inventory_serial_genealogy_links_layer_distinct", "\"FromProvenanceLayerId\" IS NULL OR \"ToProvenanceLayerId\" IS NULL OR \"FromProvenanceLayerId\" <> \"ToProvenanceLayerId\"");
                    table.CheckConstraint("CK_inventory_serial_genealogy_links_serial_distinct", "\"FromInventorySerialId\" IS NULL OR \"ToInventorySerialId\" IS NULL OR \"FromInventorySerialId\" <> \"ToInventorySerialId\"");
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_links_inventory_provenance_layer~",
                        columns: x => new { x.CompanyId, x.FromProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_links_inventory_provenance_laye~1",
                        columns: x => new { x.CompanyId, x.ToProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_links_inventory_serial_genealogy~",
                        columns: x => new { x.CompanyId, x.InventorySerialGenealogyEventId },
                        principalSchema: "advance",
                        principalTable: "inventory_serial_genealogy_events",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_links_inventory_serials_CompanyI~",
                        columns: x => new { x.CompanyId, x.FromInventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_serial_genealogy_links_inventory_serials_Company~1",
                        columns: x => new { x.CompanyId, x.ToInventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_edges",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryTransformationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdgeType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AllocationBasis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_edges", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_edges_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_provenance_edges_distinct", "\"FromProvenanceLayerId\" <> \"ToProvenanceLayerId\"");
                    table.CheckConstraint("CK_inventory_provenance_edges_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_provenance_edges_inventory_provenance_layers_Comp~",
                        columns: x => new { x.CompanyId, x.FromProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_edges_inventory_provenance_layers_Com~1",
                        columns: x => new { x.CompanyId, x.ToProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_edges_inventory_transformations_Compan~",
                        columns: x => new { x.CompanyId, x.InventoryTransformationId },
                        principalSchema: "advance",
                        principalTable: "inventory_transformations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transformation_inputs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryTransformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transformation_inputs", x => x.Id);
                    table.UniqueConstraint("AK_inventory_transformation_inputs_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_transformation_inputs_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_transformation_inputs_inventory_provenance_layers~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformation_inputs_inventory_transformations_C~",
                        columns: x => new { x.CompanyId, x.InventoryTransformationId },
                        principalSchema: "advance",
                        principalTable: "inventory_transformations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_transformation_outputs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryTransformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_transformation_outputs", x => x.Id);
                    table.UniqueConstraint("AK_inventory_transformation_outputs_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_transformation_outputs_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_transformation_outputs_inventory_lots_CompanyId_I~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformation_outputs_inventory_provenance_layer~",
                        columns: x => new { x.CompanyId, x.OutputProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformation_outputs_inventory_transformations_~",
                        columns: x => new { x.CompanyId, x.InventoryTransformationId },
                        principalSchema: "advance",
                        principalTable: "inventory_transformations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_transformation_outputs_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_transformation_outputs_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_concessions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcessionNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionLotDispositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionParameterResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    FailedParameterSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MeasuredValueSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TechnicalAcceptanceReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IntendedUse = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecidedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReversesConcessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_inventory_concessions", x => x.Id);
                    table.UniqueConstraint("AK_inventory_concessions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_concessions_decision", "(\"Status\"='DRAFT' AND \"DecidedByEmployeeId\" IS NULL AND \"DecidedRoleCode\" IS NULL AND \"DecidedAt\" IS NULL AND \"DecisionReason\" IS NULL) OR (\"Status\"<>'DRAFT' AND \"DecidedByEmployeeId\" IS NOT NULL AND \"DecidedRoleCode\"='TECHNICAL_DIRECTOR' AND \"DecidedAt\" IS NOT NULL AND length(btrim(\"DecisionReason\"))>0 AND \"DecidedByEmployeeId\"<>\"CreatedByEmployeeId\")");
                    table.CheckConstraint("CK_inventory_concessions_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
                    table.CheckConstraint("CK_inventory_concessions_quantity", "\"RequestedQuantity\" > 0");
                    table.CheckConstraint("CK_inventory_concessions_reversal", "(\"Status\"='REVERSED' AND \"ReversesConcessionId\" IS NOT NULL) OR (\"Status\"<>'REVERSED' AND \"ReversesConcessionId\" IS NULL)");
                    table.CheckConstraint("CK_inventory_concessions_status", "\"Status\" IN ('DRAFT','APPROVED','REJECTED','REVERSED')");
                    table.ForeignKey(
                        name: "FK_inventory_concessions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_employees_DecidedByEmployeeId",
                        column: x => x.DecidedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_inventory_concessions_CompanyId_Rever~",
                        columns: x => new { x.CompanyId, x.ReversesConcessionId },
                        principalSchema: "advance",
                        principalTable: "inventory_concessions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_qc_inspection_lot_dispositions_Compan~",
                        columns: x => new { x.CompanyId, x.QcInspectionLotDispositionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_lot_dispositions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_qc_inspection_parameter_results_Compa~",
                        columns: x => new { x.CompanyId, x.QcInspectionParameterResultId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_parameter_results",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concessions_qc_inspection_revisions_CompanyId_QcI~",
                        columns: x => new { x.CompanyId, x.QcInspectionRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_qc_disposition_origins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    QcInspectionLotDispositionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_qc_disposition_origins", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_qc_disposition_origins_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_provenance_qc_disposition_origins_inventory_prove~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_qc_disposition_origins_qc_inspection_l~",
                        columns: x => new { x.CompanyId, x.QcInspectionLotDispositionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_lot_dispositions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_transformation_output_origins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    InventoryTransformationOutputId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_transformation_output_origins", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_transformation_output_origins_CompanyI~", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_provenance_transformation_output_origins_inventor~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_transformation_output_origins_invento~1",
                        columns: x => new { x.CompanyId, x.InventoryTransformationOutputId },
                        principalSchema: "advance",
                        principalTable: "inventory_transformation_outputs",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_concession_allocations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryConcessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineLotAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RejectedProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptedProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_concession_allocations", x => x.Id);
                    table.UniqueConstraint("AK_inventory_concession_allocations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_concession_allocations_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocations_goods_receipt_line_lot_all~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_line_lot_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocations_inventory_concessions_Comp~",
                        columns: x => new { x.CompanyId, x.InventoryConcessionId },
                        principalSchema: "advance",
                        principalTable: "inventory_concessions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocations_inventory_lots_CompanyId_I~",
                        columns: x => new { x.CompanyId, x.InventoryLotId },
                        principalSchema: "advance",
                        principalTable: "inventory_lots",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocations_inventory_provenance_layer~",
                        columns: x => new { x.CompanyId, x.AcceptedProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocations_inventory_provenance_laye~1",
                        columns: x => new { x.CompanyId, x.RejectedProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_annotations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnotationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AnnotationCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    InventoryConcessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InheritedFromAnnotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_annotations", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_annotations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_provenance_annotations_json", "jsonb_typeof(\"DetailsJson\")='object'");
                    table.ForeignKey(
                        name: "FK_inventory_provenance_annotations_inventory_concessions_Comp~",
                        columns: x => new { x.CompanyId, x.InventoryConcessionId },
                        principalSchema: "advance",
                        principalTable: "inventory_concessions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_annotations_inventory_provenance_annot~",
                        columns: x => new { x.CompanyId, x.InheritedFromAnnotationId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_annotations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_annotations_inventory_provenance_layer~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_concession_allocation_serials",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryConcessionAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_concession_allocation_serials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocation_serials_inventory_concessio~",
                        columns: x => new { x.CompanyId, x.InventoryConcessionAllocationId },
                        principalSchema: "advance",
                        principalTable: "inventory_concession_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_concession_allocation_serials_inventory_serials_C~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_provenance_concession_allocation_origins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProvenanceLayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    InventoryConcessionAllocationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_provenance_concession_allocation_origins", x => x.Id);
                    table.UniqueConstraint("AK_inventory_provenance_concession_allocation_origins_CompanyI~", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_provenance_concession_allocation_origins_inventor~",
                        columns: x => new { x.CompanyId, x.InventoryProvenanceLayerId },
                        principalSchema: "advance",
                        principalTable: "inventory_provenance_layers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_provenance_concession_allocation_origins_invento~1",
                        columns: x => new { x.CompanyId, x.InventoryConcessionAllocationId },
                        principalSchema: "advance",
                        principalTable: "inventory_concession_allocations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryConcessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryCustodyHandoffId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryOwnershipTransferId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryTransformationId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "InventoryConcessionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "InventoryCustodyHandoffId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "InventoryOwnershipTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_posting_batches_InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches",
                column: "InventoryTransformationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_CustodyAssignmentId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "CustodyAssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryConcessionAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryCustodyHandoffLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryLotId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryOwnershipTransferLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryProvenanceLayerId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryTransformationInputId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryTransformationOutputId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_OwnershipAccountId_CustodyAssignm~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OwnershipAccountId", "CustodyAssignmentId", "InventoryProvenanceLayerId", "InventoryLotId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "QcInspectionLotDispositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements",
                column: "CustodyCaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryConcessionAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryCustodyHandoffLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryLotId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryOwnershipTransferLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryTransformationInputId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements",
                column: "InventoryTransformationOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements",
                column: "QcInspectionLotDispositionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_stock_movements_foundation3_schema",
                schema: "advance",
                table: "stock_movements",
                sql: "\"LedgerSchemaVersion\" = 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_stock_movements_lot_allocation_identity",
                schema: "advance",
                table: "stock_movements",
                sql: "\"GoodsReceiptLineLotAllocationId\" IS NULL OR \"InventoryLotId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_Inventor~1",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "InventoryConcessionAllocationId", "InventorySerialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_Inventory~",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "InventorySerialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_AcceptedProvenanceLayerId",
                schema: "advance",
                table: "inventory_concession_allocations",
                column: "AcceptedProvenanceLayerId",
                unique: true,
                filter: "\"AcceptedProvenanceLayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_CompanyId_AcceptedProvenan~",
                schema: "advance",
                table: "inventory_concession_allocations",
                columns: new[] { "CompanyId", "AcceptedProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_CompanyId_GoodsReceiptLine~",
                schema: "advance",
                table: "inventory_concession_allocations",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_CompanyId_InventoryConcess~",
                schema: "advance",
                table: "inventory_concession_allocations",
                columns: new[] { "CompanyId", "InventoryConcessionId", "GoodsReceiptLineLotAllocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_CompanyId_InventoryLotId",
                schema: "advance",
                table: "inventory_concession_allocations",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocations_CompanyId_RejectedProvenan~",
                schema: "advance",
                table: "inventory_concession_allocations",
                columns: new[] { "CompanyId", "RejectedProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId",
                schema: "advance",
                table: "inventory_concessions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_ConcessionNumber",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "ConcessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_QcInspectionLotDispositionId",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "QcInspectionLotDispositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_QcInspectionParameterResult~",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "QcInspectionParameterResultId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_QcInspectionRevisionId",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CompanyId_ReversesConcessionId",
                schema: "advance",
                table: "inventory_concessions",
                columns: new[] { "CompanyId", "ReversesConcessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_CreatedByEmployeeId",
                schema: "advance",
                table: "inventory_concessions",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_DecidedByEmployeeId",
                schema: "advance",
                table: "inventory_concessions",
                column: "DecidedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concessions_ReversesConcessionId",
                schema: "advance",
                table: "inventory_concessions",
                column: "ReversesConcessionId",
                unique: true,
                filter: "\"ReversesConcessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_attribute_revisions_CompanyId_InventoryLotId",
                schema: "advance",
                table: "inventory_lot_attribute_revisions",
                columns: new[] { "CompanyId", "InventoryLotId" },
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_attribute_revisions_CompanyId_InventoryLotId_~",
                schema: "advance",
                table: "inventory_lot_attribute_revisions",
                columns: new[] { "CompanyId", "InventoryLotId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_attribute_revisions_CompanyId_SupersedesRevis~",
                schema: "advance",
                table: "inventory_lot_attribute_revisions",
                columns: new[] { "CompanyId", "SupersedesRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_attribute_revisions_RecordedByEmployeeId",
                schema: "advance",
                table: "inventory_lot_attribute_revisions",
                column: "RecordedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_lot_attribute_revisions_SupersedesRevisionId",
                schema: "advance",
                table: "inventory_lot_attribute_revisions",
                column: "SupersedesRevisionId",
                unique: true,
                filter: "\"SupersedesRevisionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_annotations_CompanyId_InheritedFromAnn~",
                schema: "advance",
                table: "inventory_provenance_annotations",
                columns: new[] { "CompanyId", "InheritedFromAnnotationId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_annotations_CompanyId_InventoryConcess~",
                schema: "advance",
                table: "inventory_provenance_annotations",
                columns: new[] { "CompanyId", "InventoryConcessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_annotations_CompanyId_InventoryProvena~",
                schema: "advance",
                table: "inventory_provenance_annotations",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "AnnotationType", "AnnotationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_concession_allocation_origins_CompanyI~",
                schema: "advance",
                table: "inventory_provenance_concession_allocation_origins",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "OriginRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_custody_case_line_origins_CompanyId_In~",
                schema: "advance",
                table: "inventory_provenance_custody_case_line_origins",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "OriginRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_custody_case_line_origins_CustodyCaseL~",
                schema: "advance",
                table: "inventory_provenance_custody_case_line_origins",
                column: "CustodyCaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_edges_CompanyId_FromProvenanceLayerId_~",
                schema: "advance",
                table: "inventory_provenance_edges",
                columns: new[] { "CompanyId", "FromProvenanceLayerId", "ToProvenanceLayerId", "EdgeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_edges_CompanyId_InventoryTransformatio~",
                schema: "advance",
                table: "inventory_provenance_edges",
                columns: new[] { "CompanyId", "InventoryTransformationId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_edges_CompanyId_ToProvenanceLayerId",
                schema: "advance",
                table: "inventory_provenance_edges",
                columns: new[] { "CompanyId", "ToProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_goods_receipt_lot_origins_CompanyId_Go~",
                schema: "advance",
                table: "inventory_provenance_goods_receipt_lot_origins",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_goods_receipt_lot_origins_CompanyId_In~",
                schema: "advance",
                table: "inventory_provenance_goods_receipt_lot_origins",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "OriginRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_CompanyId_IdentityHash",
                schema: "advance",
                table: "inventory_provenance_layers",
                columns: new[] { "CompanyId", "IdentityHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_CompanyId_InventoryLotId",
                schema: "advance",
                table: "inventory_provenance_layers",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_CompanyId_InventorySerialId",
                schema: "advance",
                table: "inventory_provenance_layers",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_CompanyId_ItemId_InventoryLotId~",
                schema: "advance",
                table: "inventory_provenance_layers",
                columns: new[] { "CompanyId", "ItemId", "InventoryLotId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_ItemId",
                schema: "advance",
                table: "inventory_provenance_layers",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_layers_UomId",
                schema: "advance",
                table: "inventory_provenance_layers",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_qc_disposition_origins_CompanyId_Inven~",
                schema: "advance",
                table: "inventory_provenance_qc_disposition_origins",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "OriginRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_qc_disposition_origins_CompanyId_QcIns~",
                schema: "advance",
                table: "inventory_provenance_qc_disposition_origins",
                columns: new[] { "CompanyId", "QcInspectionLotDispositionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_provenance_transformation_output_origins_CompanyI~",
                schema: "advance",
                table: "inventory_provenance_transformation_output_origins",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId", "OriginRole" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_events_ActorEmployeeId",
                schema: "advance",
                table: "inventory_serial_genealogy_events",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_events_CompanyId_CorrelationId",
                schema: "advance",
                table: "inventory_serial_genealogy_events",
                columns: new[] { "CompanyId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_events_CompanyId_JobOrderId",
                schema: "advance",
                table: "inventory_serial_genealogy_events",
                columns: new[] { "CompanyId", "JobOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_events_CompanyId_ReversesEventId",
                schema: "advance",
                table: "inventory_serial_genealogy_events",
                columns: new[] { "CompanyId", "ReversesEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_events_ReversesEventId",
                schema: "advance",
                table: "inventory_serial_genealogy_events",
                column: "ReversesEventId",
                unique: true,
                filter: "\"ReversesEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_links_CompanyId_FromInventorySer~",
                schema: "advance",
                table: "inventory_serial_genealogy_links",
                columns: new[] { "CompanyId", "FromInventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_links_CompanyId_FromProvenanceLa~",
                schema: "advance",
                table: "inventory_serial_genealogy_links",
                columns: new[] { "CompanyId", "FromProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_links_CompanyId_InventorySerialG~",
                schema: "advance",
                table: "inventory_serial_genealogy_links",
                columns: new[] { "CompanyId", "InventorySerialGenealogyEventId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_links_CompanyId_ToInventorySeria~",
                schema: "advance",
                table: "inventory_serial_genealogy_links",
                columns: new[] { "CompanyId", "ToInventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_genealogy_links_CompanyId_ToProvenanceLaye~",
                schema: "advance",
                table: "inventory_serial_genealogy_links",
                columns: new[] { "CompanyId", "ToProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_identity_revisions_CompanyId_InventorySer~1",
                schema: "advance",
                table: "inventory_serial_identity_revisions",
                columns: new[] { "CompanyId", "InventorySerialId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_identity_revisions_CompanyId_InventorySeri~",
                schema: "advance",
                table: "inventory_serial_identity_revisions",
                columns: new[] { "CompanyId", "InventorySerialId" },
                unique: true,
                filter: "\"EffectiveTo\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_identity_revisions_CompanyId_SupersedesRev~",
                schema: "advance",
                table: "inventory_serial_identity_revisions",
                columns: new[] { "CompanyId", "SupersedesRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_identity_revisions_RecordedByEmployeeId",
                schema: "advance",
                table: "inventory_serial_identity_revisions",
                column: "RecordedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_serial_identity_revisions_SupersedesRevisionId",
                schema: "advance",
                table: "inventory_serial_identity_revisions",
                column: "SupersedesRevisionId",
                unique: true,
                filter: "\"SupersedesRevisionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_inputs_CompanyId_InventoryProvenan~",
                schema: "advance",
                table: "inventory_transformation_inputs",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_inputs_CompanyId_InventoryTransfor~",
                schema: "advance",
                table: "inventory_transformation_inputs",
                columns: new[] { "CompanyId", "InventoryTransformationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_CompanyId_InventoryLotId",
                schema: "advance",
                table: "inventory_transformation_outputs",
                columns: new[] { "CompanyId", "InventoryLotId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_CompanyId_InventoryTransfo~",
                schema: "advance",
                table: "inventory_transformation_outputs",
                columns: new[] { "CompanyId", "InventoryTransformationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_CompanyId_OutputProvenance~",
                schema: "advance",
                table: "inventory_transformation_outputs",
                columns: new[] { "CompanyId", "OutputProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_ItemId",
                schema: "advance",
                table: "inventory_transformation_outputs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_OutputProvenanceLayerId",
                schema: "advance",
                table: "inventory_transformation_outputs",
                column: "OutputProvenanceLayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformation_outputs_UomId",
                schema: "advance",
                table: "inventory_transformation_outputs",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_CompanyId",
                schema: "advance",
                table: "inventory_transformations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "inventory_transformations",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_CompanyId_ReversesTransformationId",
                schema: "advance",
                table: "inventory_transformations",
                columns: new[] { "CompanyId", "ReversesTransformationId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_CompanyId_TransformationNumber",
                schema: "advance",
                table: "inventory_transformations",
                columns: new[] { "CompanyId", "TransformationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_PostedByEmployeeId",
                schema: "advance",
                table: "inventory_transformations",
                column: "PostedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_transformations_ReversesTransformationId",
                schema: "advance",
                table: "inventory_transformations",
                column: "ReversesTransformationId",
                unique: true,
                filter: "\"ReversesTransformationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_lot_dispositions_CompanyId_DestinationConditi~",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                columns: new[] { "CompanyId", "DestinationConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_lot_dispositions_CompanyId_GoodsReceiptLineLo~",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_lot_dispositions_CompanyId_InventoryLotId_Dis~",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                columns: new[] { "CompanyId", "InventoryLotId", "Disposition" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_lot_dispositions_CompanyId_QcInspectionRevisi~",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                columns: new[] { "CompanyId", "QcInspectionRevisionId", "GoodsReceiptLineLotAllocationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_concession_allocations_CompanyId_~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryConcessionAllocationId" },
                principalSchema: "advance",
                principalTable: "inventory_concession_allocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_custody_assignments_CompanyId_Cus~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "CustodyAssignmentId" },
                principalSchema: "advance",
                principalTable: "inventory_custody_assignments",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_custody_case_lines_CompanyId_Cust~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "CustodyCaseLineId" },
                principalSchema: "advance",
                principalTable: "inventory_custody_case_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_custody_handoff_lines_CompanyId_I~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryCustodyHandoffLineId" },
                principalSchema: "advance",
                principalTable: "inventory_custody_handoff_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_lots_CompanyId_InventoryLotId",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryLotId" },
                principalSchema: "advance",
                principalTable: "inventory_lots",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_ownership_accounts_CompanyId_Owne~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "OwnershipAccountId" },
                principalSchema: "advance",
                principalTable: "inventory_ownership_accounts",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_ownership_transfer_lines_CompanyI~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryOwnershipTransferLineId" },
                principalSchema: "advance",
                principalTable: "inventory_ownership_transfer_lines",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_provenance_layers_CompanyId_Inven~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryProvenanceLayerId" },
                principalSchema: "advance",
                principalTable: "inventory_provenance_layers",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_transformation_inputs_CompanyId_I~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryTransformationInputId" },
                principalSchema: "advance",
                principalTable: "inventory_transformation_inputs",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_inventory_transformation_outputs_CompanyId_~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "InventoryTransformationOutputId" },
                principalSchema: "advance",
                principalTable: "inventory_transformation_outputs",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_qc_inspection_lot_dispositions_CompanyId_Qc~",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "CompanyId", "QcInspectionLotDispositionId" },
                principalSchema: "advance",
                principalTable: "qc_inspection_lot_dispositions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_inventory_concessions_CompanyId_Inven~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryConcessionId" },
                principalSchema: "advance",
                principalTable: "inventory_concessions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_inventory_custody_handoffs_CompanyId_~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryCustodyHandoffId" },
                principalSchema: "advance",
                principalTable: "inventory_custody_handoffs",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_inventory_ownership_transfers_Company~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryOwnershipTransferId" },
                principalSchema: "advance",
                principalTable: "inventory_ownership_transfers",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_posting_batches_inventory_transformations_CompanyId_I~",
                schema: "advance",
                table: "stock_posting_batches",
                columns: new[] { "CompanyId", "InventoryTransformationId" },
                principalSchema: "advance",
                principalTable: "inventory_transformations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(Foundation3InventoryProvenanceGenealogySql.UpContract);
            migrationBuilder.Sql(Foundation3InventoryProvenanceGenealogySql.ControlledPosting);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(Foundation3InventoryProvenanceGenealogySql.DownContract);
            migrationBuilder.Sql(StoresControlledPostingSql.PostingOnly);

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_concession_allocations_CompanyId_~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_custody_assignments_CompanyId_Cus~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_custody_case_lines_CompanyId_Cust~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_custody_handoff_lines_CompanyId_I~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_lots_CompanyId_InventoryLotId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_ownership_accounts_CompanyId_Owne~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_ownership_transfer_lines_CompanyI~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_provenance_layers_CompanyId_Inven~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_transformation_inputs_CompanyId_I~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_inventory_transformation_outputs_CompanyId_~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_qc_inspection_lot_dispositions_CompanyId_Qc~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_inventory_concessions_CompanyId_Inven~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_inventory_custody_handoffs_CompanyId_~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_inventory_ownership_transfers_Company~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_posting_batches_inventory_transformations_CompanyId_I~",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropTable(
                name: "inventory_concession_allocation_serials",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_lot_attribute_revisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_annotations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_concession_allocation_origins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_custody_case_line_origins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_edges",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_goods_receipt_lot_origins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_qc_disposition_origins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_transformation_output_origins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_serial_genealogy_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_serial_identity_revisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_transformation_inputs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_concession_allocations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_transformation_outputs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_serial_genealogy_events",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_concessions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_provenance_layers",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_transformations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "qc_inspection_lot_dispositions",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_CompanyId_InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_posting_batches_InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_CustodyAssignmentId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryLotId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryProvenanceLayerId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_OwnershipAccountId_CustodyAssignm~",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryLotId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stock_movements_foundation3_schema",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stock_movements_lot_allocation_identity",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryConcessionId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "InventoryCustodyHandoffId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "InventoryOwnershipTransferId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "InventoryTransformationId",
                schema: "advance",
                table: "stock_posting_batches");

            migrationBuilder.DropColumn(
                name: "CustodyAssignmentId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "CustodyCaseLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryConcessionAllocationId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryCustodyHandoffLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryLotId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryOwnershipTransferLineId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryProvenanceLayerId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryTransformationInputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "InventoryTransformationOutputId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "OwnershipAccountId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "QcInspectionLotDispositionId",
                schema: "advance",
                table: "stock_movements");

            migrationBuilder.AlterColumn<short>(
                name: "LedgerSchemaVersion",
                schema: "advance",
                table: "stock_movements",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);
        }
    }
}
