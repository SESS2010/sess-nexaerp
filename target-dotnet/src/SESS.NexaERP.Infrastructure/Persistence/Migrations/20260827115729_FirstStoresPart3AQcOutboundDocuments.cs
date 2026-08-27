using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FirstStoresPart3AQcOutboundDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart3ASql.PreUp);
            migrationBuilder.AddColumn<Guid>(
                name: "DeliveryChallanId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobOrderId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialIssueRequestId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QcInspectionRevisionId",
                schema: "advance",
                table: "stores_document_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_orders",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MachineModel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MachineSerial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JobOrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedCompletionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InstallationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_job_orders", x => x.Id);
                    table.UniqueConstraint("AK_job_orders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_job_orders_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_issue_requests",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DestinationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovalRouteSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_material_issue_requests", x => x.Id);
                    table.UniqueConstraint("AK_material_issue_requests_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_issue_requests_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_departments_DestinationDepartmentId",
                        column: x => x.DestinationDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_requests_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_challans",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DcNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentDeliveryChallanId = table.Column<Guid>(type: "uuid", nullable: true),
                    DcType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ExternalReferenceNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DispatchEvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ApprovalRouteSnapshotJson = table.Column<string>(type: "jsonb", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HandledByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_delivery_challans", x => x.Id);
                    table.UniqueConstraint("AK_delivery_challans_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_delivery_challans_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_delivery_challans_CompanyId_ParentDeliver~",
                        columns: x => new { x.CompanyId, x.ParentDeliveryChallanId },
                        principalSchema: "advance",
                        principalTable: "delivery_challans",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_employees_HandledByEmployeeId",
                        column: x => x.HandledByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_material_issue_requests_CompanyId_Materia~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestId },
                        principalSchema: "advance",
                        principalTable: "material_issue_requests",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challans_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_issue_request_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_issue_request_lines", x => x.Id);
                    table.UniqueConstraint("AK_material_issue_request_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_material_issue_request_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_issue_request_lines_material_issue_requests_Compan~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestId },
                        principalSchema: "advance",
                        principalTable: "material_issue_requests",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stores_approval_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryChallanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalCycle = table.Column<int>(type: "integer", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ResolvedEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SnapshotIdentity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stores_approval_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stores_approval_history_delivery_challans_CompanyId_Deliver~",
                        columns: x => new { x.CompanyId, x.DeliveryChallanId },
                        principalSchema: "advance",
                        principalTable: "delivery_challans",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stores_approval_history_employees_ResolvedEmployeeId",
                        column: x => x.ResolvedEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stores_approval_history_material_issue_requests_CompanyId_M~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestId },
                        principalSchema: "advance",
                        principalTable: "material_issue_requests",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_challan_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ParentDeliveryChallanLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialIssueRequestLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    WeightUomId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispatchedWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    ReturnedWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    CalculatedScrapWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    VendorWeightExplanation = table.Column<string>(type: "text", nullable: true),
                    RequiresQcSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    ReplacementGoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_challan_lines", x => x.Id);
                    table.UniqueConstraint("AK_delivery_challan_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_delivery_challan_lines_CompanyId_Par~",
                        columns: x => new { x.CompanyId, x.ParentDeliveryChallanLineId },
                        principalSchema: "advance",
                        principalTable: "delivery_challan_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_delivery_challans_CompanyId_Delivery~",
                        columns: x => new { x.CompanyId, x.DeliveryChallanId },
                        principalSchema: "advance",
                        principalTable: "delivery_challans",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_goods_receipt_lines_CompanyId_GoodsR~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_goods_receipt_lines_CompanyId_Replac~",
                        columns: x => new { x.CompanyId, x.ReplacementGoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_inventory_serials_CompanyId_Inventor~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_material_issue_request_lines_Company~",
                        columns: x => new { x.CompanyId, x.MaterialIssueRequestLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_request_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_challan_lines_uoms_WeightUomId",
                        column: x => x.WeightUomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspections",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryChallanLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspections", x => x.Id);
                    table.UniqueConstraint("AK_qc_inspections_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_qc_inspections_delivery_challan_lines_CompanyId_DeliveryCha~",
                        columns: x => new { x.CompanyId, x.DeliveryChallanLineId },
                        principalSchema: "advance",
                        principalTable: "delivery_challan_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspections_goods_receipt_lines_CompanyId_GoodsReceiptLi~",
                        columns: x => new { x.CompanyId, x.GoodsReceiptLineId },
                        principalSchema: "advance",
                        principalTable: "goods_receipt_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisionKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RevisesRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectorBasis = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FallbackReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InspectionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InspectionCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AcceptedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    InspectionShortfallRejectedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AcceptedConditionLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    QcHoldConditionLocationIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingReturnConditionLocationIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_qc_inspection_revisions", x => x.Id);
                    table.UniqueConstraint("AK_qc_inspection_revisions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_employees_FinalizedByEmployeeId",
                        column: x => x.FinalizedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_employees_InspectorEmployeeId",
                        column: x => x.InspectorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_qc_inspection_revisions_CompanyId_R~",
                        columns: x => new { x.CompanyId, x.RevisesRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_qc_inspections_CompanyId_QcInspecti~",
                        columns: x => new { x.CompanyId, x.QcInspectionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_warehouse_condition_locations_Compa~",
                        columns: x => new { x.CompanyId, x.AcceptedConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_warehouse_condition_locations_Comp~1",
                        columns: x => new { x.CompanyId, x.PendingReturnConditionLocationIdSnapshot },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_revisions_warehouse_condition_locations_Comp~2",
                        columns: x => new { x.CompanyId, x.QcHoldConditionLocationIdSnapshot },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_parameter_results",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionPolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MeasurementUomIdSnapshot = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementUomCodeSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LowerLimitSnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    UpperLimitSnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    InspectionMethodSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequiredSampleSizeSnapshot = table.Column<int>(type: "integer", nullable: false),
                    SampleOrdinal = table.Column<int>(type: "integer", nullable: false),
                    ObservedNumericValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    ObservedTextValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObservedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspection_parameter_results", x => x.Id);
                    table.UniqueConstraint("AK_qc_inspection_parameter_results_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_qc_inspection_parameter_results_employees_ObservedByEmploye~",
                        column: x => x.ObservedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_parameter_results_qc_inspection_policies_QcIn~",
                        column: x => x.QcInspectionPolicyId,
                        principalSchema: "advance",
                        principalTable: "qc_inspection_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_parameter_results_qc_inspection_revisions_Com~",
                        columns: x => new { x.CompanyId, x.QcInspectionRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_parameter_results_uoms_MeasurementUomIdSnapsh~",
                        column: x => x.MeasurementUomIdSnapshot,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_serial_dispositions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcInspectionRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySerialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Disposition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspection_serial_dispositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_qc_inspection_serial_dispositions_inventory_serials_Company~",
                        columns: x => new { x.CompanyId, x.InventorySerialId },
                        principalSchema: "advance",
                        principalTable: "inventory_serials",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_serial_dispositions_qc_inspection_revisions_C~",
                        columns: x => new { x.CompanyId, x.QcInspectionRevisionId },
                        principalSchema: "advance",
                        principalTable: "qc_inspection_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_DeliveryChallanId",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "DeliveryChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_JobOrderId",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "JobOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_MaterialIssueReque~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_QcInspectionRevisi~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_DeliveryChallanId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "DeliveryChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_InventorySerialId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_MaterialIssueRequestLineId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "MaterialIssueRequestLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_ParentDeliveryChallanLineId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "ParentDeliveryChallanLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_QcInspectionRevisionId",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_CompanyId_ReplacementGoodsReceiptLin~",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "ReplacementGoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_DeliveryChallanId_LineNumber",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "DeliveryChallanId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_ItemId",
                schema: "advance",
                table: "delivery_challan_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challan_lines_WeightUomId",
                schema: "advance",
                table: "delivery_challan_lines",
                column: "WeightUomId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId",
                schema: "advance",
                table: "delivery_challans",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_DcNumber",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "DcNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_JobOrderId",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "JobOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_MaterialIssueRequestId",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_ParentDeliveryChallanId",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "ParentDeliveryChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CompanyId_Status_ExpectedReturnDate",
                schema: "advance",
                table: "delivery_challans",
                columns: new[] { "CompanyId", "Status", "ExpectedReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_CustomerId",
                schema: "advance",
                table: "delivery_challans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_HandledByEmployeeId",
                schema: "advance",
                table: "delivery_challans",
                column: "HandledByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_JobOrderId",
                schema: "advance",
                table: "delivery_challans",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_MaterialIssueRequestId",
                schema: "advance",
                table: "delivery_challans",
                column: "MaterialIssueRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_ParentDeliveryChallanId",
                schema: "advance",
                table: "delivery_challans",
                column: "ParentDeliveryChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_challans_VendorId",
                schema: "advance",
                table: "delivery_challans",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId",
                schema: "advance",
                table: "job_orders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_JobOrderNumber",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "JobOrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_MachineSerial",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "MachineSerial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_Status_JobOrderDate",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "Status", "JobOrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_CompanyId_ItemId",
                schema: "advance",
                table: "material_issue_request_lines",
                columns: new[] { "CompanyId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_CompanyId_MaterialIssueRequest~",
                schema: "advance",
                table: "material_issue_request_lines",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_ItemId",
                schema: "advance",
                table: "material_issue_request_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_request_lines_MaterialIssueRequestId_LineNum~",
                schema: "advance",
                table: "material_issue_request_lines",
                columns: new[] { "MaterialIssueRequestId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_ApprovedByEmployeeId",
                schema: "advance",
                table: "material_issue_requests",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CompanyId",
                schema: "advance",
                table: "material_issue_requests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "material_issue_requests",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CompanyId_JobOrderId",
                schema: "advance",
                table: "material_issue_requests",
                columns: new[] { "CompanyId", "JobOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CompanyId_RequestNumber",
                schema: "advance",
                table: "material_issue_requests",
                columns: new[] { "CompanyId", "RequestNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CompanyId_Status_RequiredDate",
                schema: "advance",
                table: "material_issue_requests",
                columns: new[] { "CompanyId", "Status", "RequiredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_CustomerId",
                schema: "advance",
                table: "material_issue_requests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_DestinationDepartmentId",
                schema: "advance",
                table: "material_issue_requests",
                column: "DestinationDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_JobOrderId",
                schema: "advance",
                table: "material_issue_requests",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_RequestedByEmployeeId",
                schema: "advance",
                table: "material_issue_requests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_RequestingDepartmentId",
                schema: "advance",
                table: "material_issue_requests",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_material_issue_requests_VendorId",
                schema: "advance",
                table: "material_issue_requests",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_CompanyId_ParameterCodeSnap~",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                columns: new[] { "CompanyId", "ParameterCodeSnapshot", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_CompanyId_QcInspectionRevis~",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_MeasurementUomIdSnapshot",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                column: "MeasurementUomIdSnapshot");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_ObservedByEmployeeId",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                column: "ObservedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_QcInspectionPolicyId",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                column: "QcInspectionPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_parameter_results_QcInspectionRevisionId_QcIn~",
                schema: "advance",
                table: "qc_inspection_parameter_results",
                columns: new[] { "QcInspectionRevisionId", "QcInspectionPolicyId", "SampleOrdinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId",
                schema: "advance",
                table: "qc_inspection_revisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_AcceptedConditionLocation~",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "AcceptedConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_PendingReturnConditionLoc~",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "PendingReturnConditionLocationIdSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_QcHoldConditionLocationId~",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "QcHoldConditionLocationIdSnapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_QcInspectionId",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "QcInspectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_RevisesRevisionId",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "RevisesRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_CompanyId_Status_InspectionStartedAt",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "CompanyId", "Status", "InspectionStartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_FinalizedByEmployeeId",
                schema: "advance",
                table: "qc_inspection_revisions",
                column: "FinalizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_InspectorEmployeeId",
                schema: "advance",
                table: "qc_inspection_revisions",
                column: "InspectorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_QcInspectionId_RevisionNumber",
                schema: "advance",
                table: "qc_inspection_revisions",
                columns: new[] { "QcInspectionId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_revisions_RevisesRevisionId",
                schema: "advance",
                table: "qc_inspection_revisions",
                column: "RevisesRevisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_serial_dispositions_CompanyId_InventorySerial~",
                schema: "advance",
                table: "qc_inspection_serial_dispositions",
                columns: new[] { "CompanyId", "InventorySerialId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_serial_dispositions_CompanyId_QcInspectionRev~",
                schema: "advance",
                table: "qc_inspection_serial_dispositions",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_serial_dispositions_QcInspectionRevisionId_In~",
                schema: "advance",
                table: "qc_inspection_serial_dispositions",
                columns: new[] { "QcInspectionRevisionId", "InventorySerialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_CompanyId_DeliveryChallanLineId",
                schema: "advance",
                table: "qc_inspections",
                columns: new[] { "CompanyId", "DeliveryChallanLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_CompanyId_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections",
                columns: new[] { "CompanyId", "GoodsReceiptLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_CompanyId_InspectionNumber",
                schema: "advance",
                table: "qc_inspections",
                columns: new[] { "CompanyId", "InspectionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_DeliveryChallanLineId",
                schema: "advance",
                table: "qc_inspections",
                column: "DeliveryChallanLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections",
                column: "GoodsReceiptLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_CompanyId_DeliveryChallanId",
                schema: "advance",
                table: "stores_approval_history",
                columns: new[] { "CompanyId", "DeliveryChallanId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_CompanyId_MaterialIssueRequestId",
                schema: "advance",
                table: "stores_approval_history",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_CorrelationId",
                schema: "advance",
                table: "stores_approval_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_DeliveryChallanId_ApprovalCycle_Ste~",
                schema: "advance",
                table: "stores_approval_history",
                columns: new[] { "DeliveryChallanId", "ApprovalCycle", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_MaterialIssueRequestId_ApprovalCycl~",
                schema: "advance",
                table: "stores_approval_history",
                columns: new[] { "MaterialIssueRequestId", "ApprovalCycle", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stores_approval_history_ResolvedEmployeeId_OccurredAt",
                schema: "advance",
                table: "stores_approval_history",
                columns: new[] { "ResolvedEmployeeId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_stores_document_status_history_delivery_challans_CompanyId_~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "DeliveryChallanId" },
                principalSchema: "advance",
                principalTable: "delivery_challans",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stores_document_status_history_job_orders_CompanyId_JobOrde~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "JobOrderId" },
                principalSchema: "advance",
                principalTable: "job_orders",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stores_document_status_history_material_issue_requests_Comp~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "MaterialIssueRequestId" },
                principalSchema: "advance",
                principalTable: "material_issue_requests",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stores_document_status_history_qc_inspection_revisions_Comp~",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" },
                principalSchema: "advance",
                principalTable: "qc_inspection_revisions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_challan_lines_qc_inspection_revisions_CompanyId_Qc~",
                schema: "advance",
                table: "delivery_challan_lines",
                columns: new[] { "CompanyId", "QcInspectionRevisionId" },
                principalSchema: "advance",
                principalTable: "qc_inspection_revisions",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(FirstStoresPart3ASql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart3ASql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_stores_document_status_history_delivery_challans_CompanyId_~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_stores_document_status_history_job_orders_CompanyId_JobOrde~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_stores_document_status_history_material_issue_requests_Comp~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_stores_document_status_history_qc_inspection_revisions_Comp~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_delivery_challan_lines_delivery_challans_CompanyId_Delivery~",
                schema: "advance",
                table: "delivery_challan_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_delivery_challan_lines_material_issue_request_lines_Company~",
                schema: "advance",
                table: "delivery_challan_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_delivery_challan_lines_qc_inspection_revisions_CompanyId_Qc~",
                schema: "advance",
                table: "delivery_challan_lines");

            migrationBuilder.DropTable(
                name: "qc_inspection_parameter_results",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "qc_inspection_serial_dispositions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stores_approval_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "delivery_challans",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_issue_request_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_issue_requests",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "job_orders",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "qc_inspection_revisions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "qc_inspections",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "delivery_challan_lines",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_CompanyId_DeliveryChallanId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_CompanyId_JobOrderId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_CompanyId_MaterialIssueReque~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropIndex(
                name: "IX_stores_document_status_history_CompanyId_QcInspectionRevisi~",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropColumn(
                name: "DeliveryChallanId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropColumn(
                name: "JobOrderId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropColumn(
                name: "MaterialIssueRequestId",
                schema: "advance",
                table: "stores_document_status_history");

            migrationBuilder.DropColumn(
                name: "QcInspectionRevisionId",
                schema: "advance",
                table: "stores_document_status_history");
        }
    }
}
