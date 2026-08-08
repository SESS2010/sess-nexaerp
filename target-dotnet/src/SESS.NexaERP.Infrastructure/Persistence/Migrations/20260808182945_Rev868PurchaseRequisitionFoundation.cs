using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev868PurchaseRequisitionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_approval_route_settings",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ApproverRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_approval_route_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequesterEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequiredByDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PurposeJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CostCentre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProjectReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ServiceReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    WorkOrderReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CustomerReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Status = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EstimatedTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisitions", x => x.Id);
                    table.CheckConstraint("CK_purchase_requisitions_estimated_total_nonnegative", "\"EstimatedTotal\" >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "nexa",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_employees_RequesterEmployeeId",
                        column: x => x.RequesterEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_approval_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisition_approval_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_approval_history_purchase_requisitions~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_attachments",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisition_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_attachments_purchase_requisitions_Purc~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SpecificationSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    EstimatedUnitPriceSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedLineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    MachineReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ServiceReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    OnHandSnapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ActiveReservedSnapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AvailableSnapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InTransitSnapshot = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    StockCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ProcurementHandoffQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LineStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisition_lines", x => x.Id);
                    table.CheckConstraint("CK_pr_lines_amounts_nonnegative", "\"EstimatedUnitPriceSnapshot\" >= 0 AND \"EstimatedLineTotal\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ShortageQuantity\" >= 0 AND \"ProcurementHandoffQuantity\" >= 0");
                    table.CheckConstraint("CK_pr_lines_requested_qty_positive", "\"RequestedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "nexa",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_purchase_requisitions_PurchaseRe~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_warehouses_PreferredWarehouseId",
                        column: x => x.PreferredWarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_status_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requisition_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_status_history_purchase_requisitions_P~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_availability_checks",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CheckedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResultStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_availability_checks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_availability_checks_purchase_requisitions_PurchaseReq~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requirement_handoffs",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandoffQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HandoffNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    HandoffBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    HandoffAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requirement_handoffs", x => x.Id);
                    table.CheckConstraint("CK_purchase_handoffs_qty_positive", "\"HandoffQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_purchase_requirement_handoffs_purchase_requisition_lines_Pu~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requirement_handoffs_purchase_requisitions_Purchas~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReservationNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReservedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ReservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.Id);
                    table.CheckConstraint("CK_stock_reservations_qty_positive", "\"ReservedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_stock_reservations_purchase_requisition_lines_PurchaseRequi~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_purchase_requisitions_PurchaseRequisitio~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_availability_check_lines",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAvailabilityCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ActiveReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InTransitQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    LineResultStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_availability_check_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_purchase_requisition_lines_P~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "nexa",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_stock_availability_checks_St~",
                        column: x => x.StockAvailabilityCheckId,
                        principalSchema: "nexa",
                        principalTable: "stock_availability_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservation_history",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservation_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_reservation_history_stock_reservations_StockReservati~",
                        column: x => x.StockReservationId,
                        principalSchema: "nexa",
                        principalTable: "stock_reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000022"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requisitions", "/purchase/requisitions", "Purchase Requisitions", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000023"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requisition-approvals", "/purchase/requisition-approvals", "PR Approvals", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000024"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Stores", "stores.stock-check", "/stores/stock-check", "Stock Availability Check", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000025"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Stores", "stores.reservations", "/stores/reservations", "Stock Reservations", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000026"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requirement-handoff", "/purchase/requirement-handoff", "Purchase Requirement Handoff", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01034d48-c4aa-7261-e9e4-888832ab13b2"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("01275179-960c-8401-c25c-ff3ea100b465"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("0130c6a9-a282-fe5f-0e87-f85dc76b2051"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("01afd214-f457-6905-469e-95e1ba60771c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("026bea62-c207-632b-d3d7-cafb5c973658"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("02f7e8f7-a3d1-08cc-3d1b-9e439be2cf0d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("0557f009-230c-3043-7634-ef0d1dc3480b"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("066b907a-90f6-f400-31c2-9a8de85f58fa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("083fc830-139f-4006-f637-5f900fd8132e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("08df695d-e9ba-70f9-dd5e-6b8d88551bb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0a99c83f-8f94-ecb7-a877-69267beedd8c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("0d271d3e-4008-1465-6de5-9b660ff60bf7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("0ef31d8a-189c-19fe-4a78-033ae9e70bc6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("1056ab04-f3e9-fb95-b805-4a51a7698c69"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("1059f07f-9ce5-de0f-c16c-01cf02116aed"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("10c78633-b41c-5825-051f-a146d4402aeb"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("12ba4a62-899f-a2c2-6ca1-8c3c1399f8d3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("12e1379b-f17a-7f0a-e522-8dba3b966cf9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("14728dfc-d82e-6bfa-923b-5770ddac7bac"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("14aed82d-d726-2c80-fe1c-6e3c54538789"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("14fbdac8-67bc-e8e6-8b56-54d70160c626"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("161a0fe1-1bd2-aefb-1f3a-a8ff3d72c280"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("169c329d-735d-3ae9-d519-17363643a809"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("18226f75-4c36-6e6c-7db1-ac8b334b418f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("198ead02-e678-7cfa-e082-0da2a9237d0e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("1c9f738e-13da-98ed-8735-c0af87d1bed1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("2175c1e4-246b-dc54-47cb-8607d03c2c4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("239b1345-26aa-1c2a-c562-e48e090ed35a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("247195b9-2d76-5233-5d2a-466fd3bca58e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("24f0f519-6d9d-09f4-4fcf-e468d575687b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("2692e7b2-73fd-8756-8b3d-d437d29081a9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("26c4abc1-da39-2a42-c592-3c708d29b708"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("27433977-1cb9-192e-3610-75d085355b48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("2944f487-7898-b449-64f6-0f254dc905be"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2946a6fe-8394-2003-3b33-b849e05c3fcc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("2954e3a3-1352-68bf-fdff-a61240289f93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("29dbf667-a680-7c0e-41c5-2dc90ee8db4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("2b663021-ceb8-6891-1411-78ef52c7eb8e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("2de773df-4003-1012-79ff-8040024a1b4a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("2f9e7d78-ce42-7ea5-728c-87c48a3a7f91"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("305d84e6-491d-a3b4-e65a-151d9d7103bf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("305efbfd-f002-5fa5-e72b-7743de8a4994"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("30d233d8-06fa-b3ca-b27b-5ddd08860846"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("31f2ed64-5eca-9e6d-8b1f-7fc420dea466"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("325d5475-24b6-69b3-ed22-4e7e66199841"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("32c4a61c-3146-ac2a-4f6e-bdb38740ccb0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("37d60081-868c-812b-2e66-f1f8d246fbac"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("38473eb3-b92b-cbb4-4734-eee0f824ae35"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("38c70de3-dbe7-ad40-8654-1eeba4a5a9f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("395caf08-cf73-f71e-9890-3975df1baac2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("3a9913da-c7c7-d537-03cb-d4a75c8c33fe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3db27053-76d7-050d-bbd7-96ea49d32e93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("3ddc0294-a7ae-c93c-394f-0579b64e7f21"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3eb8ca06-7f3a-2338-51b9-d93f2e710a8b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("3ed2e32a-1764-9ca4-3b76-61010dfeb3c2"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("3f77987f-e1a2-00f1-ae12-c97da302650c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("406f8c4d-3b9e-1e3d-780b-d1a27edfc5e6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("41a7df76-f655-3412-5f37-2cd417f98c82"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("41f7a71a-3efb-c4d3-55bb-c8a9508860d2"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("42a15e72-3c0e-67f1-746d-5ee534d9c502"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("42c3d133-02c4-f84e-ccfc-9369445b0a7b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("43ddaf52-98da-5578-2011-7757d6812123"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("4548ad6b-0d20-1b5b-3808-196248fdf7d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4764ff10-a265-f4fc-9deb-b316154b1cb2"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("47a3ddf7-452f-a656-1973-de76128f4bab"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("49980728-f2c1-9f56-0ad9-b36c5a889719"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("4a323803-1aca-a52a-1836-8b15ee90d398"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("4ccfd0e0-caf1-0b35-a4e9-b4c610d1d518"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("4d26d81d-eebb-e354-305c-20c3de67eaba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4de91ccb-ee76-da93-f6b6-3fc772dfff78"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("4df35c89-1203-a270-b546-40696fe301f1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("4ebbd962-8401-cb16-ce3e-40b63680780f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("5003bcd7-f0ec-79ce-a83a-1798a51795cc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("5086a30f-82f9-c2b2-60e3-57ffc2de96c6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("5173d448-e9cd-0d68-c79f-7f1e0ba4fd9c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("517c3c10-2971-66d1-6a18-38dfda4d4d5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("523cc95a-3d9f-9b75-9d87-7df0a1b34253"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("53c18ca9-f7ae-a720-5854-ef47c72ff7c4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("57b0c075-0523-d6f6-b63e-0b114bc49400"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("58941021-2c3e-dd2f-ecc9-5eb5449171c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("5a414697-e5fc-0555-677e-ea21efcf7bb6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("5e835d64-eedf-47ea-747c-bd6f50092619"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("5fea3ee3-8203-6e8c-8252-3886526f5d80"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("61273037-8a65-61f3-387a-b4ae8d854662"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("616d1a16-c056-3b49-b92c-74d382827474"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("638fb6d9-200c-a3b0-9317-cd900579cbb2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("64cf91c9-25bc-cfde-1954-9d8ab7f291f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("67b3c775-07be-ea99-6655-a657dcaf45a5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("67bf1f4b-87b1-90cd-15c8-409205e9e68f"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("6a4e890b-7586-b5b5-1a17-1e2c8ce592fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("6bdc4ba3-dc86-4023-164b-8530fa624738"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("6c4013cc-9b70-4bab-c658-efd52f1534d1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("6eec19ec-3377-42ce-3428-2faec9cfc5f0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("70b4e227-f7b7-5634-7488-9806821837fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("71fba4e5-513d-78af-09e2-352fb4c8be7e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("727c6e92-30c8-ad23-6f5e-9714d4342f0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("740f4cb3-9687-8846-9429-b8028f3fe929"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("77c824f5-4b8c-67d8-7512-c8be21d7e4e8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("792cbc80-2b16-72eb-d980-7d4e174fee04"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("79b9c190-d7f3-44ad-64d9-39ddf14241d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7a0d54be-e71c-e5b1-a734-fc55346672ac"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("7a38257f-236b-f091-cc2d-6a07d4b3b30d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("7ab64f73-3a84-bcde-1d48-46e0f48e5445"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7d3f5f56-f674-1f72-68b0-3a55813f8dfc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("824f4ce6-98b4-6f74-4d3f-3f5df926c4c0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("85937b3f-f489-d8c8-40bb-5aee61844a4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("85b6bc45-c13c-da25-3198-37ee0d504701"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("86346fed-f259-e15c-771a-61cb0b5e6188"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("863e4f26-ac98-0c5e-546b-449eccb3845e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("88352e79-c410-91c1-e012-de38870124d3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("88ea25c7-3475-b940-a8fc-b72a8c33cc56"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("8a6baa4e-990e-de85-d729-3128dbb4b0a9"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8aaeb6eb-fbde-56da-8033-35821404722f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("8af45e13-d08f-c109-023f-c358663e71b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("8ce78826-3814-28ba-6dc2-9b29134d2f16"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("8e22ffbb-d684-6090-7e6e-3605773f71a0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("8f6a81af-626b-1fe2-7bfd-6a65e20597f8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("9238885e-5891-4759-3a2e-d11a14bf4216"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("93cdf397-66b9-7b69-9567-522ae6d132b3"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("941272bd-659e-dddd-0643-367822a5530b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("95823336-4ed8-1c15-e280-0dcf8334035a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("963f18fc-c8c5-66fb-a59a-f250236ed752"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("96c24743-97ca-fa9b-8204-db4eb256bfb1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("997e03c9-200f-848d-95a5-07a8184fa888"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("9acc891b-a181-b02c-5324-4e3e461e3912"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("9b233dda-aab8-6d62-224e-fd7aef39c60c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("a08d7c05-387e-568a-d8e0-e68bc715a01c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("a0b4a6f5-546e-c0da-f65c-37ef5cea452f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a48ba706-ddd8-7c00-c475-2af8e71c05a9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("a7f2c4cb-37c6-6a11-0e81-01f72d96b9f6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("a83d03e3-12f2-a974-3ed7-6a30b3417b0e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a868cbbe-5698-fb7b-78b7-491135a21161"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("a95c1e81-50e3-7611-596e-138988ff96bc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("aa3d4ca4-8a8f-a580-9f26-a8fbccf8d21a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("ae0a6f84-4a8c-4a0a-2064-901d455061dc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("aed2bfcd-4d44-f086-7fd6-9f9f2b15f48b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("af844032-41d3-fa9f-e2b1-dc574dcb5ebb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("b161cf4a-3880-8da3-7f1a-e3e023e7beb6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("b272dde4-4d09-cbab-f515-58c2d33cbb0e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b4d08c0f-ce35-c067-b48c-1921a6e439a4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("b8caf4b8-d200-7f93-d0de-034135a14a55"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("bace752c-586d-a395-8fa7-572166a4065b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("bb264540-e621-baa8-e366-53d5226521fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("bb857600-32b5-7229-f603-29dc829a4f3e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("bc1ba3aa-0266-e40d-e923-93f9556f811b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("bc2cd481-43cf-34b2-bb18-556cc4610e77"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c0d55070-b641-2405-681d-3d0e4cb48ec9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c3872708-d710-c5f1-21a4-98a0c809bd09"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c4ec61cb-60c3-691d-2694-d6224e81675d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c52f1f6a-2182-52d9-060a-e1cf8b2bf35d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c5f29057-8ee7-a61a-ca1c-63115b20e6b1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("c8e771de-9e68-c6bb-77fa-99a35f151bbc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("caf4e7c0-1e09-3f80-df52-19697ce7d9bd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("cc60ef9d-c06b-db1f-df24-b7bd7d92cff7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("ce1fc874-a8dd-69b0-5336-cbccf0053cbb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("ce8e33eb-25dc-69e5-3883-7c87b5dfbc04"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("d093e505-880e-d176-36de-0e7addfee298"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d10ce882-7553-d9d3-38f8-aaf30dfbeb8a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("d2371ab8-06a9-c9c8-5edd-29494f01b74a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d47355ca-57d6-40ae-29a3-e9b9b51aff04"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("d4a22905-013d-6a11-28f8-5287f5fc79f8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("d4e09e16-891a-34cc-1604-57d62da5f6d8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("d4ffa090-2309-a329-a24b-36be029a5644"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("d5f64cfa-c44c-3deb-2934-367db7cb231b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d8b7eab3-ff6b-807d-abc1-83889a59c6d1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("da339d77-93c1-8b00-1227-6bda34380f10"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("dadda6ba-b432-508a-305a-77b8a391f540"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("db7b3a3a-9184-110b-4f1d-3a7970b42f99"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ddff4cb2-3075-5fb8-5217-97bbc5b7c43d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("df09a412-868d-93ba-12b4-0ce27c1178ed"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("e24a5141-961b-7d1d-a40e-c826a74e2be6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("e392d6b6-7b54-e0c2-1d67-42474353fd00"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("e4ef51b0-080a-a4b5-40cd-76ceae40608a"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("e4fb4bcd-a855-58f8-4858-8a4e825185dd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e668f430-c067-2ef4-2e92-80c3551045c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("e93749b8-6e80-7b68-4915-57d98a6ea489"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("ed1cc2e0-7d4f-2edc-50d7-82c87c911dbe"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ee607d44-6dbd-ece5-9270-9220f465a7f0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("f095917e-cd2f-48df-373e-1ea1e7e2a7b8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("f0bd803c-23d2-440c-ca37-02bb554bffd1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("f25f6ac3-b0dd-64f4-a82d-b459aff22397"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("f279f58e-9c05-e09d-cc7a-86085c8b504c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("f4c531c9-50f4-244d-ded3-acdf840ea285"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("f9b536bb-4190-53d4-ed42-16932e9c5a51"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("fa43bbd4-306d-f557-e8ef-d2cd39d87114"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("fa7ff198-dd2a-2f9f-1f73-6245d877889a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("fd028b07-4ecc-439f-ca45-cbcf249574a9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("fe763e56-8a62-9d7e-8722-77ba4816d949"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("ff2181d4-cb3b-ac61-78cb-474d8b70762d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("ffe73e4a-7346-cf6a-2e58-0fa7cc6e2b96"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_route_settings_RouteCode",
                schema: "nexa",
                table: "purchase_approval_route_settings",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_HandoffNumber",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "HandoffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_PurchaseRequisitionId",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_PurchaseRequisitionLineId_Sta~",
                schema: "nexa",
                table: "purchase_requirement_handoffs",
                columns: new[] { "PurchaseRequisitionLineId", "Status" },
                unique: true,
                filter: "\"Status\" = 'PendingRFQ'");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_approval_history_PurchaseRequisitionId~",
                schema: "nexa",
                table: "purchase_requisition_approval_history",
                columns: new[] { "PurchaseRequisitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_attachments_PurchaseRequisitionId_Stor~",
                schema: "nexa",
                table: "purchase_requisition_attachments",
                columns: new[] { "PurchaseRequisitionId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_ItemId",
                schema: "nexa",
                table: "purchase_requisition_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_PreferredWarehouseId",
                schema: "nexa",
                table: "purchase_requisition_lines",
                column: "PreferredWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_PurchaseRequisitionId_LineNumber",
                schema: "nexa",
                table: "purchase_requisition_lines",
                columns: new[] { "PurchaseRequisitionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_status_history_PurchaseRequisitionId_C~",
                schema: "nexa",
                table: "purchase_requisition_status_history",
                columns: new[] { "PurchaseRequisitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_DeliveryWarehouseId",
                schema: "nexa",
                table: "purchase_requisitions",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_OrganizationId_Status",
                schema: "nexa",
                table: "purchase_requisitions",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_PrNumber",
                schema: "nexa",
                table: "purchase_requisitions",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequesterEmployeeId",
                schema: "nexa",
                table: "purchase_requisitions",
                column: "RequesterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequestingDepartmentId",
                schema: "nexa",
                table: "purchase_requisitions",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequiredByDate",
                schema: "nexa",
                table: "purchase_requisitions",
                column: "RequiredByDate");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId",
                schema: "nexa",
                table: "stock_availability_check_lines",
                column: "PurchaseRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "nexa",
                table: "stock_availability_check_lines",
                columns: new[] { "StockAvailabilityCheckId", "PurchaseRequisitionLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_checks_CheckNumber",
                schema: "nexa",
                table: "stock_availability_checks",
                column: "CheckNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_checks_PurchaseRequisitionId",
                schema: "nexa",
                table: "stock_availability_checks",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservation_history_StockReservationId_CreatedAt",
                schema: "nexa",
                table: "stock_reservation_history",
                columns: new[] { "StockReservationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionId",
                schema: "nexa",
                table: "stock_reservations",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_Status",
                schema: "nexa",
                table: "stock_reservations",
                columns: new[] { "PurchaseRequisitionLineId", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ReservationNumber",
                schema: "nexa",
                table: "stock_reservations",
                column: "ReservationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_approval_route_settings",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requirement_handoffs",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requisition_approval_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requisition_attachments",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requisition_status_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "stock_availability_check_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "stock_reservation_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "stock_availability_checks",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "stock_reservations",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requisition_lines",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "purchase_requisitions",
                schema: "nexa");

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01034d48-c4aa-7261-e9e4-888832ab13b2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01275179-960c-8401-c25c-ff3ea100b465"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0130c6a9-a282-fe5f-0e87-f85dc76b2051"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01afd214-f457-6905-469e-95e1ba60771c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("026bea62-c207-632b-d3d7-cafb5c973658"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("02f7e8f7-a3d1-08cc-3d1b-9e439be2cf0d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0557f009-230c-3043-7634-ef0d1dc3480b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("066b907a-90f6-f400-31c2-9a8de85f58fa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("083fc830-139f-4006-f637-5f900fd8132e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("08df695d-e9ba-70f9-dd5e-6b8d88551bb9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0a99c83f-8f94-ecb7-a877-69267beedd8c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0d271d3e-4008-1465-6de5-9b660ff60bf7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0ef31d8a-189c-19fe-4a78-033ae9e70bc6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1056ab04-f3e9-fb95-b805-4a51a7698c69"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1059f07f-9ce5-de0f-c16c-01cf02116aed"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("10c78633-b41c-5825-051f-a146d4402aeb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12ba4a62-899f-a2c2-6ca1-8c3c1399f8d3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12e1379b-f17a-7f0a-e522-8dba3b966cf9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("14728dfc-d82e-6bfa-923b-5770ddac7bac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("14aed82d-d726-2c80-fe1c-6e3c54538789"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("14fbdac8-67bc-e8e6-8b56-54d70160c626"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("161a0fe1-1bd2-aefb-1f3a-a8ff3d72c280"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("169c329d-735d-3ae9-d519-17363643a809"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18226f75-4c36-6e6c-7db1-ac8b334b418f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("198ead02-e678-7cfa-e082-0da2a9237d0e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1c9f738e-13da-98ed-8735-c0af87d1bed1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2175c1e4-246b-dc54-47cb-8607d03c2c4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("239b1345-26aa-1c2a-c562-e48e090ed35a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("247195b9-2d76-5233-5d2a-466fd3bca58e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("24f0f519-6d9d-09f4-4fcf-e468d575687b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2692e7b2-73fd-8756-8b3d-d437d29081a9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("26c4abc1-da39-2a42-c592-3c708d29b708"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("27433977-1cb9-192e-3610-75d085355b48"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2944f487-7898-b449-64f6-0f254dc905be"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2946a6fe-8394-2003-3b33-b849e05c3fcc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2954e3a3-1352-68bf-fdff-a61240289f93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("29dbf667-a680-7c0e-41c5-2dc90ee8db4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2b663021-ceb8-6891-1411-78ef52c7eb8e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2de773df-4003-1012-79ff-8040024a1b4a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f9e7d78-ce42-7ea5-728c-87c48a3a7f91"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("305d84e6-491d-a3b4-e65a-151d9d7103bf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("305efbfd-f002-5fa5-e72b-7743de8a4994"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("30d233d8-06fa-b3ca-b27b-5ddd08860846"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("31f2ed64-5eca-9e6d-8b1f-7fc420dea466"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("325d5475-24b6-69b3-ed22-4e7e66199841"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32c4a61c-3146-ac2a-4f6e-bdb38740ccb0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("37d60081-868c-812b-2e66-f1f8d246fbac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("38473eb3-b92b-cbb4-4734-eee0f824ae35"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("38c70de3-dbe7-ad40-8654-1eeba4a5a9f7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("395caf08-cf73-f71e-9890-3975df1baac2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a9913da-c7c7-d537-03cb-d4a75c8c33fe"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3db27053-76d7-050d-bbd7-96ea49d32e93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3ddc0294-a7ae-c93c-394f-0579b64e7f21"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3eb8ca06-7f3a-2338-51b9-d93f2e710a8b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3ed2e32a-1764-9ca4-3b76-61010dfeb3c2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f77987f-e1a2-00f1-ae12-c97da302650c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("406f8c4d-3b9e-1e3d-780b-d1a27edfc5e6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("41a7df76-f655-3412-5f37-2cd417f98c82"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("41f7a71a-3efb-c4d3-55bb-c8a9508860d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("42a15e72-3c0e-67f1-746d-5ee534d9c502"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("42c3d133-02c4-f84e-ccfc-9369445b0a7b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("43ddaf52-98da-5578-2011-7757d6812123"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4548ad6b-0d20-1b5b-3808-196248fdf7d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4764ff10-a265-f4fc-9deb-b316154b1cb2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("47a3ddf7-452f-a656-1973-de76128f4bab"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("49980728-f2c1-9f56-0ad9-b36c5a889719"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4a323803-1aca-a52a-1836-8b15ee90d398"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4ccfd0e0-caf1-0b35-a4e9-b4c610d1d518"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4d26d81d-eebb-e354-305c-20c3de67eaba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4de91ccb-ee76-da93-f6b6-3fc772dfff78"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4df35c89-1203-a270-b546-40696fe301f1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4ebbd962-8401-cb16-ce3e-40b63680780f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5003bcd7-f0ec-79ce-a83a-1798a51795cc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5086a30f-82f9-c2b2-60e3-57ffc2de96c6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5173d448-e9cd-0d68-c79f-7f1e0ba4fd9c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("517c3c10-2971-66d1-6a18-38dfda4d4d5d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("523cc95a-3d9f-9b75-9d87-7df0a1b34253"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("53c18ca9-f7ae-a720-5854-ef47c72ff7c4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57b0c075-0523-d6f6-b63e-0b114bc49400"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("58941021-2c3e-dd2f-ecc9-5eb5449171c1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5a414697-e5fc-0555-677e-ea21efcf7bb6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5e835d64-eedf-47ea-747c-bd6f50092619"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5fea3ee3-8203-6e8c-8252-3886526f5d80"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("61273037-8a65-61f3-387a-b4ae8d854662"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("616d1a16-c056-3b49-b92c-74d382827474"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("638fb6d9-200c-a3b0-9317-cd900579cbb2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("64cf91c9-25bc-cfde-1954-9d8ab7f291f1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("67b3c775-07be-ea99-6655-a657dcaf45a5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("67bf1f4b-87b1-90cd-15c8-409205e9e68f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a4e890b-7586-b5b5-1a17-1e2c8ce592fa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6bdc4ba3-dc86-4023-164b-8530fa624738"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6c4013cc-9b70-4bab-c658-efd52f1534d1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6eec19ec-3377-42ce-3428-2faec9cfc5f0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("70b4e227-f7b7-5634-7488-9806821837fa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("71fba4e5-513d-78af-09e2-352fb4c8be7e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("727c6e92-30c8-ad23-6f5e-9714d4342f0d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("740f4cb3-9687-8846-9429-b8028f3fe929"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("77c824f5-4b8c-67d8-7512-c8be21d7e4e8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("792cbc80-2b16-72eb-d980-7d4e174fee04"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("79b9c190-d7f3-44ad-64d9-39ddf14241d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a0d54be-e71c-e5b1-a734-fc55346672ac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a38257f-236b-f091-cc2d-6a07d4b3b30d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7ab64f73-3a84-bcde-1d48-46e0f48e5445"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7d3f5f56-f674-1f72-68b0-3a55813f8dfc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("824f4ce6-98b4-6f74-4d3f-3f5df926c4c0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("85937b3f-f489-d8c8-40bb-5aee61844a4d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("85b6bc45-c13c-da25-3198-37ee0d504701"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("86346fed-f259-e15c-771a-61cb0b5e6188"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("863e4f26-ac98-0c5e-546b-449eccb3845e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("88352e79-c410-91c1-e012-de38870124d3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("88ea25c7-3475-b940-a8fc-b72a8c33cc56"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8a6baa4e-990e-de85-d729-3128dbb4b0a9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8aaeb6eb-fbde-56da-8033-35821404722f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8af45e13-d08f-c109-023f-c358663e71b6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8ce78826-3814-28ba-6dc2-9b29134d2f16"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8e22ffbb-d684-6090-7e6e-3605773f71a0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f6a81af-626b-1fe2-7bfd-6a65e20597f8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9238885e-5891-4759-3a2e-d11a14bf4216"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("93cdf397-66b9-7b69-9567-522ae6d132b3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("941272bd-659e-dddd-0643-367822a5530b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("95823336-4ed8-1c15-e280-0dcf8334035a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("963f18fc-c8c5-66fb-a59a-f250236ed752"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("96c24743-97ca-fa9b-8204-db4eb256bfb1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("997e03c9-200f-848d-95a5-07a8184fa888"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9acc891b-a181-b02c-5324-4e3e461e3912"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9b233dda-aab8-6d62-224e-fd7aef39c60c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a08d7c05-387e-568a-d8e0-e68bc715a01c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a0b4a6f5-546e-c0da-f65c-37ef5cea452f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a48ba706-ddd8-7c00-c475-2af8e71c05a9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a7f2c4cb-37c6-6a11-0e81-01f72d96b9f6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a83d03e3-12f2-a974-3ed7-6a30b3417b0e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a868cbbe-5698-fb7b-78b7-491135a21161"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a95c1e81-50e3-7611-596e-138988ff96bc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aa3d4ca4-8a8f-a580-9f26-a8fbccf8d21a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ae0a6f84-4a8c-4a0a-2064-901d455061dc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aed2bfcd-4d44-f086-7fd6-9f9f2b15f48b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af844032-41d3-fa9f-e2b1-dc574dcb5ebb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b161cf4a-3880-8da3-7f1a-e3e023e7beb6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b272dde4-4d09-cbab-f515-58c2d33cbb0e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b4d08c0f-ce35-c067-b48c-1921a6e439a4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b8caf4b8-d200-7f93-d0de-034135a14a55"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bace752c-586d-a395-8fa7-572166a4065b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bb264540-e621-baa8-e366-53d5226521fa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bb857600-32b5-7229-f603-29dc829a4f3e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc1ba3aa-0266-e40d-e923-93f9556f811b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc2cd481-43cf-34b2-bb18-556cc4610e77"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0d55070-b641-2405-681d-3d0e4cb48ec9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c3872708-d710-c5f1-21a4-98a0c809bd09"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c4ec61cb-60c3-691d-2694-d6224e81675d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c52f1f6a-2182-52d9-060a-e1cf8b2bf35d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c5f29057-8ee7-a61a-ca1c-63115b20e6b1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c8e771de-9e68-c6bb-77fa-99a35f151bbc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("caf4e7c0-1e09-3f80-df52-19697ce7d9bd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cc60ef9d-c06b-db1f-df24-b7bd7d92cff7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ce1fc874-a8dd-69b0-5336-cbccf0053cbb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ce8e33eb-25dc-69e5-3883-7c87b5dfbc04"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d093e505-880e-d176-36de-0e7addfee298"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d10ce882-7553-d9d3-38f8-aaf30dfbeb8a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2371ab8-06a9-c9c8-5edd-29494f01b74a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d47355ca-57d6-40ae-29a3-e9b9b51aff04"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d4a22905-013d-6a11-28f8-5287f5fc79f8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d4e09e16-891a-34cc-1604-57d62da5f6d8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d4ffa090-2309-a329-a24b-36be029a5644"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5f64cfa-c44c-3deb-2934-367db7cb231b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8b7eab3-ff6b-807d-abc1-83889a59c6d1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("da339d77-93c1-8b00-1227-6bda34380f10"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dadda6ba-b432-508a-305a-77b8a391f540"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("db7b3a3a-9184-110b-4f1d-3a7970b42f99"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ddff4cb2-3075-5fb8-5217-97bbc5b7c43d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("df09a412-868d-93ba-12b4-0ce27c1178ed"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e24a5141-961b-7d1d-a40e-c826a74e2be6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e392d6b6-7b54-e0c2-1d67-42474353fd00"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4ef51b0-080a-a4b5-40cd-76ceae40608a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4fb4bcd-a855-58f8-4858-8a4e825185dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e668f430-c067-2ef4-2e92-80c3551045c1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e93749b8-6e80-7b68-4915-57d98a6ea489"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ed1cc2e0-7d4f-2edc-50d7-82c87c911dbe"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ee607d44-6dbd-ece5-9270-9220f465a7f0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f095917e-cd2f-48df-373e-1ea1e7e2a7b8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f0bd803c-23d2-440c-ca37-02bb554bffd1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f25f6ac3-b0dd-64f4-a82d-b459aff22397"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f279f58e-9c05-e09d-cc7a-86085c8b504c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f4c531c9-50f4-244d-ded3-acdf840ea285"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f9b536bb-4190-53d4-ed42-16932e9c5a51"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa43bbd4-306d-f557-e8ef-d2cd39d87114"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa7ff198-dd2a-2f9f-1f73-6245d877889a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd028b07-4ecc-439f-ca45-cbcf249574a9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fe763e56-8a62-9d7e-8722-77ba4816d949"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ff2181d4-cb3b-ac61-78cb-474d8b70762d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ffe73e4a-7346-cf6a-2e58-0fa7cc6e2b96"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000026"));
        }
    }
}
