using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FirstStoresPart1FoundationInboundNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart1Sql.PreUp);

            migrationBuilder.DropCheckConstraint(
                name: "CK_warehouse_condition_code",
                schema: "advance",
                table: "warehouse_condition_locations");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_purchase_order_lines_PurchaseOrderId_Id",
                schema: "advance",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "Id" });

            migrationBuilder.CreateTable(
                name: "business_rule_configuration_versions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ValueType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OldValueJson = table.Column<string>(type: "jsonb", nullable: true),
                    NewValueJson = table.Column<string>(type: "jsonb", nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_rule_configuration_versions", x => x.Id);
                    table.UniqueConstraint("AK_business_rule_configuration_versions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_business_rule_configuration_first_version", "(\"VersionNumber\" = 1 AND \"PreviousVersionId\" IS NULL AND \"OldValueJson\" IS NULL) OR (\"VersionNumber\" > 1 AND \"PreviousVersionId\" IS NOT NULL AND \"OldValueJson\" IS NOT NULL)");
                    table.CheckConstraint("CK_business_rule_configuration_json", "jsonb_typeof(\"NewValueJson\") IN ('number','boolean','string') AND (\"OldValueJson\" IS NULL OR jsonb_typeof(\"OldValueJson\") IN ('number','boolean','string'))");
                    table.CheckConstraint("CK_business_rule_configuration_role", "\"ChangedByRoleCode\" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','IT_MANAGER')");
                    table.CheckConstraint("CK_business_rule_configuration_value_type", "\"ValueType\" IN ('INTEGER','DECIMAL','BOOLEAN','TEXT')");
                    table.CheckConstraint("CK_business_rule_configuration_version_number", "\"VersionNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_business_rule_configuration_versions_business_rule_configur~",
                        column: x => x.PreviousVersionId,
                        principalSchema: "advance",
                        principalTable: "business_rule_configuration_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_rule_configuration_versions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_business_rule_configuration_versions_employees_ChangedByEmp~",
                        column: x => x.ChangedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gate_entries",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GateEntryNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DocumentKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NORMAL"),
                    ReversesGateEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorNameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    VendorDcNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ModeOfTransport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ArrivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoReceiptVerificationJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_gate_entries", x => x.Id);
                    table.UniqueConstraint("AK_gate_entries_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.UniqueConstraint("AK_gate_entries_PurchaseOrderId_Id", x => new { x.PurchaseOrderId, x.Id });
                    table.CheckConstraint("CK_gate_entry_document_kind", "\"DocumentKind\" IN ('NORMAL','REVERSAL')");
                    table.CheckConstraint("CK_gate_entry_finalization", "(\"Status\"='DRAFT' AND \"FinalizedAt\" IS NULL AND \"FinalizedByEmployeeId\" IS NULL) OR (\"Status\"='FINALIZED' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedByEmployeeId\" IS NOT NULL)");
                    table.CheckConstraint("CK_gate_entry_iso_json", "jsonb_typeof(\"IsoReceiptVerificationJson\")='object'");
                    table.CheckConstraint("CK_gate_entry_request_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
                    table.CheckConstraint("CK_gate_entry_reversal", "(\"DocumentKind\"='NORMAL' AND \"ReversesGateEntryId\" IS NULL AND \"ReversalReason\" IS NULL) OR (\"DocumentKind\"='REVERSAL' AND \"ReversesGateEntryId\" IS NOT NULL AND length(trim(coalesce(\"ReversalReason\",''))) > 0)");
                    table.CheckConstraint("CK_gate_entry_status", "\"Status\" IN ('DRAFT','FINALIZED')");
                    table.ForeignKey(
                        name: "FK_gate_entries_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entries_employees_FinalizedByEmployeeId",
                        column: x => x.FinalizedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entries_employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entries_gate_entries_ReversesGateEntryId",
                        column: x => x.ReversesGateEntryId,
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entries_purchase_orders_CompanyId_PurchaseOrderId",
                        columns: x => new { x.CompanyId, x.PurchaseOrderId },
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entries_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_company_inventory_settings",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ErpBarcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BarcodeCategoryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BarcodeSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    BarcodeSymbology = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "CODE128"),
                    SerialCaptureMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "INHERIT"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_company_inventory_settings", x => x.Id);
                    table.UniqueConstraint("AK_item_company_inventory_settings_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_item_company_inventory_category", "\"BarcodeCategoryCode\" IN ('ELE','REF','FAS','PLC','FAB','MEC')");
                    table.CheckConstraint("CK_item_company_inventory_sequence", "\"BarcodeSequenceNumber\" > 0");
                    table.CheckConstraint("CK_item_company_inventory_serial_mode", "\"SerialCaptureMode\" IN ('INHERIT','REQUIRED','OPTIONAL')");
                    table.ForeignKey(
                        name: "FK_item_company_inventory_settings_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_company_inventory_settings_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_events",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SourceEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceReferenceSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RecipientRoleCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    TitleSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BodySnapshot = table.Column<string>(type: "text", nullable: false),
                    DeepLinkSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    NotBeforeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CancellationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_events", x => x.Id);
                    table.UniqueConstraint("AK_notification_events_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_notification_event_payload", "jsonb_typeof(\"PayloadJson\")='object'");
                    table.CheckConstraint("CK_notification_event_roles", "cardinality(\"RecipientRoleCodes\") > 0");
                    table.CheckConstraint("CK_notification_event_status", "\"Status\" IN ('SCHEDULED','READY','ACTIVE','COMPLETED','CANCELLED','RECIPIENT_BLOCKED')");
                    table.CheckConstraint("CK_notification_event_text", "length(trim(\"EventType\"))>0 AND length(trim(\"SourceEntityType\"))>0 AND length(trim(\"SourceReferenceSnapshot\"))>0 AND length(trim(\"TitleSnapshot\"))>0 AND length(trim(\"BodySnapshot\"))>0 AND length(trim(\"DeepLinkSnapshot\"))>0");
                    table.CheckConstraint("CK_notification_event_timestamps", "(\"Status\" IN ('SCHEDULED','READY') AND \"ActivatedAt\" IS NULL AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\" IN ('ACTIVE','RECIPIENT_BLOCKED') AND \"ActivatedAt\" IS NOT NULL AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\"='COMPLETED' AND \"ActivatedAt\" IS NOT NULL AND \"CompletedAt\" IS NOT NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\"='CANCELLED' AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NOT NULL AND length(trim(coalesce(\"CancelledBy\",'')))>0 AND length(trim(coalesce(\"CancellationReason\",'')))>0)");
                    table.ForeignKey(
                        name: "FK_notification_events_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "store_category_routes",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    QcHoldConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingReturnConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultAcceptedConditionLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_category_routes", x => x.Id);
                    table.UniqueConstraint("AK_store_category_routes_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_store_category_route_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_store_category_routes_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_store_category_routes_item_categories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_store_category_routes_warehouse_condition_locations_Company~",
                        columns: x => new { x.CompanyId, x.DefaultAcceptedConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_store_category_routes_warehouse_condition_locations_Compan~1",
                        columns: x => new { x.CompanyId, x.PendingReturnConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_store_category_routes_warehouse_condition_locations_Compan~2",
                        columns: x => new { x.CompanyId, x.QcHoldConditionLocationId },
                        principalSchema: "advance",
                        principalTable: "warehouse_condition_locations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gate_entry_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gate_entry_lines", x => x.Id);
                    table.UniqueConstraint("AK_gate_entry_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_gate_entry_line_values", "\"LineNumber\" > 0 AND \"DeliveredQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_gate_entry_lines_gate_entries_CompanyId_GateEntryId",
                        columns: x => new { x.CompanyId, x.GateEntryId },
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entry_lines_gate_entries_PurchaseOrderId_GateEntryId",
                        columns: x => new { x.PurchaseOrderId, x.GateEntryId },
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumns: new[] { "PurchaseOrderId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entry_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entry_lines_purchase_order_lines_CompanyId_PurchaseOrd~",
                        columns: x => new { x.CompanyId, x.PurchaseOrderLineId },
                        principalSchema: "advance",
                        principalTable: "purchase_order_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gate_entry_lines_purchase_order_lines_PurchaseOrderId_Purch~",
                        columns: x => new { x.PurchaseOrderId, x.PurchaseOrderLineId },
                        principalSchema: "advance",
                        principalTable: "purchase_order_lines",
                        principalColumns: new[] { "PurchaseOrderId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stores_document_status_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stores_document_status_history", x => x.Id);
                    table.CheckConstraint("CK_stores_document_status_action", "\"Action\" IN ('CREATED','FINALIZED','REVERSED')");
                    table.CheckConstraint("CK_stores_document_status_transition", "(\"Action\"='CREATED' AND \"FromStatus\" IS NULL AND \"ToStatus\"='DRAFT') OR (\"Action\"='FINALIZED' AND \"FromStatus\"='DRAFT' AND \"ToStatus\"='FINALIZED') OR (\"Action\"='REVERSED' AND \"FromStatus\"='FINALIZED' AND \"ToStatus\"='REVERSED')");
                    table.ForeignKey(
                        name: "FK_stores_document_status_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stores_document_status_history_gate_entries_CompanyId_GateE~",
                        columns: x => new { x.CompanyId, x.GateEntryId },
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_recipients",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InAppAvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReadByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReadCorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_recipients", x => x.Id);
                    table.UniqueConstraint("AK_notification_recipients_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_notification_recipient_read", "(\"ReadAt\" IS NULL AND \"ReadByEmployeeId\" IS NULL AND \"ReadCorrelationId\" IS NULL) OR (\"ReadAt\" IS NOT NULL AND \"ReadByEmployeeId\"=\"RecipientEmployeeId\" AND length(trim(coalesce(\"ReadCorrelationId\",'')))>0)");
                    table.CheckConstraint("CK_notification_recipient_roles", "cardinality(\"ResolvedRoleCodes\") > 0");
                    table.ForeignKey(
                        name: "FK_notification_recipients_employees_ReadByEmployeeId",
                        column: x => x.ReadByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_recipients_employees_RecipientEmployeeId",
                        column: x => x.RecipientEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notification_recipients_notification_events_CompanyId_Notif~",
                        columns: x => new { x.CompanyId, x.NotificationEventId },
                        principalSchema: "advance",
                        principalTable: "notification_events",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery_attempts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationRecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorDetail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_delivery_attempts", x => x.Id);
                    table.CheckConstraint("CK_notification_delivery_attempt_number", "\"AttemptNumber\" > 0");
                    table.CheckConstraint("CK_notification_delivery_channel", "\"Channel\" IN ('IN_APP','EMAIL')");
                    table.CheckConstraint("CK_notification_delivery_result", "(\"Status\"='SENT' AND \"DeliveredAt\" IS NOT NULL AND \"ErrorCode\" IS NULL AND \"ErrorDetail\" IS NULL) OR (\"Status\"='FAILED' AND \"DeliveredAt\" IS NULL AND length(trim(coalesce(\"ErrorCode\",'')))>0 AND length(trim(coalesce(\"ErrorDetail\",'')))>0)");
                    table.CheckConstraint("CK_notification_delivery_status", "\"Status\" IN ('SENT','FAILED')");
                    table.ForeignKey(
                        name: "FK_notification_delivery_attempts_notification_recipients_Comp~",
                        columns: x => new { x.CompanyId, x.NotificationRecipientId },
                        principalSchema: "advance",
                        principalTable: "notification_recipients",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "business_rule_configuration_versions",
                columns: new[] { "Id", "ChangeReason", "ChangedAt", "ChangedByEmployeeId", "ChangedByRoleCode", "CompanyId", "CorrelationId", "EffectiveFrom", "NewValueJson", "OldValueJson", "PreviousVersionId", "RuleKey", "UnitCode", "ValueType", "VersionNumber" },
                values: new object[,]
                {
                    { new Guid("0150c86c-11db-068d-8b65-25006f8f79b5"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-SERIAL_CAPTURE_THRESHOLD", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "SERIAL_CAPTURE_THRESHOLD", "INR", "DECIMAL", 1 },
                    { new Guid("041a9155-c219-c207-0eb4-8fce2ec4543f"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EXPENSE_LODGING_SINGLE_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "800", null, null, "EXPENSE_LODGING_SINGLE_PER_DAY", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("1274cef4-21c8-a54b-6185-233f9325a872"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EXPENSE_LODGING_SINGLE_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "800", null, null, "EXPENSE_LODGING_SINGLE_PER_DAY", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("12eefb66-b24f-1531-4ba7-5f360fbb4c56"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EXPENSE_FOOD_PER_PERSON_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "300", null, null, "EXPENSE_FOOD_PER_PERSON_PER_DAY", "INR_PERSON_DAY", "DECIMAL", 1 },
                    { new Guid("1575c6f0-ed69-5a35-ee6f-5a2ba43a8385"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EXPENSE_DAILY_APPROVAL_CAP", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "EXPENSE_DAILY_APPROVAL_CAP", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("18dfc4f3-f8ba-1af3-a60d-ab0634c970bd"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EMERGENCY_PURCHASE_COUNT_PER_MONTH", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "10", null, null, "EMERGENCY_PURCHASE_COUNT_PER_MONTH", "COUNT", "INTEGER", 1 },
                    { new Guid("2f4c0cbb-6836-5bc2-f15f-e98372322d9b"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EXPENSE_FOOD_PER_PERSON_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "300", null, null, "EXPENSE_FOOD_PER_PERSON_PER_DAY", "INR_PERSON_DAY", "DECIMAL", 1 },
                    { new Guid("3366bcea-787c-bb32-f345-402621cd4133"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-QC_COMPLETION_DAYS", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "2", null, null, "QC_COMPLETION_DAYS", "DAYS", "INTEGER", 1 },
                    { new Guid("4c8237d1-8c83-7514-6779-ad9e71690ceb"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EMERGENCY_PURCHASE_VALUE_LIMIT", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "EMERGENCY_PURCHASE_VALUE_LIMIT", "INR", "DECIMAL", 1 },
                    { new Guid("506b8e67-c6cd-79db-d964-800f07a230cf"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EMERGENCY_PURCHASE_VALUE_LIMIT", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "EMERGENCY_PURCHASE_VALUE_LIMIT", "INR", "DECIMAL", 1 },
                    { new Guid("5cfd5c14-b959-831b-8345-cdbfd58eabd1"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "100", null, null, "EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM", "KM", "DECIMAL", 1 },
                    { new Guid("676487c9-94f3-2a13-e8aa-8e72b3ce3eae"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EMERGENCY_PURCHASE_COUNT_PER_MONTH", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "10", null, null, "EMERGENCY_PURCHASE_COUNT_PER_MONTH", "COUNT", "INTEGER", 1 },
                    { new Guid("850723c0-be64-6af6-dc81-bb3e32ac9d7e"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EXPENSE_DAILY_APPROVAL_CAP", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "EXPENSE_DAILY_APPROVAL_CAP", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("90ecc2b4-4042-00a7-dff3-5b6b7d83fa06"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-QC_COMPLETION_DAYS", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "2", null, null, "QC_COMPLETION_DAYS", "DAYS", "INTEGER", 1 },
                    { new Guid("aa9bacae-af61-9741-cfc0-a7b12a8e8da6"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "100", null, null, "EXPENSE_TRAVEL_DISTANCE_THRESHOLD_KM", "KM", "DECIMAL", 1 },
                    { new Guid("c2f948d0-610f-e649-b84d-2a425cb851c1"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000002"), "STORES-P1-PROP-EXPENSE_LODGING_DOUBLE_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "1200", null, null, "EXPENSE_LODGING_DOUBLE_PER_DAY", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("c495a577-34cf-ae2b-b8dc-ed712fd4cc6b"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-EXPENSE_LODGING_DOUBLE_PER_DAY", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "1200", null, null, "EXPENSE_LODGING_DOUBLE_PER_DAY", "INR_DAY", "DECIMAL", 1 },
                    { new Guid("e2015c98-43ba-7f5e-13ec-6eb1914beffb"), "INITIAL_STORES_CONFIGURATION", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "TECHNICAL_DIRECTOR", new Guid("70000000-0000-0000-0000-000000000001"), "STORES-P1-PVT-SERIAL_CAPTURE_THRESHOLD", new DateTimeOffset(new DateTime(2026, 8, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "5000", null, null, "SERIAL_CAPTURE_THRESHOLD", "INR", "DECIMAL", 1 }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_warehouse_condition_code",
                schema: "advance",
                table: "warehouse_condition_locations",
                sql: "\"ConditionCode\" IN ('AVAILABLE','QC_HOLD','REJECTED','QUARANTINE','RETURN_TO_VENDOR','PENDING_RETURNABLE_DC','SCRAP')");

            migrationBuilder.CreateIndex(
                name: "IX_business_rule_configuration_versions_ChangedByEmployeeId",
                schema: "advance",
                table: "business_rule_configuration_versions",
                column: "ChangedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_business_rule_configuration_versions_CompanyId_RuleKey_Effe~",
                schema: "advance",
                table: "business_rule_configuration_versions",
                columns: new[] { "CompanyId", "RuleKey", "EffectiveFrom" },
                unique: true,
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_business_rule_configuration_versions_CompanyId_RuleKey_Vers~",
                schema: "advance",
                table: "business_rule_configuration_versions",
                columns: new[] { "CompanyId", "RuleKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_rule_configuration_versions_CorrelationId",
                schema: "advance",
                table: "business_rule_configuration_versions",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_rule_configuration_versions_PreviousVersionId",
                schema: "advance",
                table: "business_rule_configuration_versions",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_CompanyId",
                schema: "advance",
                table: "gate_entries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_CompanyId_GateEntryNumber",
                schema: "advance",
                table: "gate_entries",
                columns: new[] { "CompanyId", "GateEntryNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "gate_entries",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_CompanyId_PurchaseOrderId_ArrivedAt",
                schema: "advance",
                table: "gate_entries",
                columns: new[] { "CompanyId", "PurchaseOrderId", "ArrivedAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_CompanyId_VendorId_VendorDcNumber",
                schema: "advance",
                table: "gate_entries",
                columns: new[] { "CompanyId", "VendorId", "VendorDcNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_FinalizedByEmployeeId",
                schema: "advance",
                table: "gate_entries",
                column: "FinalizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_ReceivedByEmployeeId",
                schema: "advance",
                table: "gate_entries",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_ReversesGateEntryId",
                schema: "advance",
                table: "gate_entries",
                column: "ReversesGateEntryId",
                unique: true,
                filter: "\"DocumentKind\"='REVERSAL' AND \"Status\"='FINALIZED'");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entries_VendorId",
                schema: "advance",
                table: "gate_entries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_CompanyId_GateEntryId",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "CompanyId", "GateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_CompanyId_PurchaseOrderLineId",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "CompanyId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_GateEntryId_LineNumber",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "GateEntryId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_GateEntryId_PurchaseOrderLineId",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "GateEntryId", "PurchaseOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_ItemId",
                schema: "advance",
                table: "gate_entry_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_PurchaseOrderId_GateEntryId",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "PurchaseOrderId", "GateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_gate_entry_lines_PurchaseOrderId_PurchaseOrderLineId",
                schema: "advance",
                table: "gate_entry_lines",
                columns: new[] { "PurchaseOrderId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_item_company_inventory_settings_CompanyId",
                schema: "advance",
                table: "item_company_inventory_settings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_item_company_inventory_settings_CompanyId_BarcodeSequenceNu~",
                schema: "advance",
                table: "item_company_inventory_settings",
                columns: new[] { "CompanyId", "BarcodeSequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_company_inventory_settings_CompanyId_ErpBarcode",
                schema: "advance",
                table: "item_company_inventory_settings",
                columns: new[] { "CompanyId", "ErpBarcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_company_inventory_settings_CompanyId_ItemId",
                schema: "advance",
                table: "item_company_inventory_settings",
                columns: new[] { "CompanyId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_company_inventory_settings_ItemId_IsActive",
                schema: "advance",
                table: "item_company_inventory_settings",
                columns: new[] { "ItemId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_CompanyId_Channel_Status_Att~",
                schema: "advance",
                table: "notification_delivery_attempts",
                columns: new[] { "CompanyId", "Channel", "Status", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_CompanyId_NotificationRecipi~",
                schema: "advance",
                table: "notification_delivery_attempts",
                columns: new[] { "CompanyId", "NotificationRecipientId" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_CorrelationId",
                schema: "advance",
                table: "notification_delivery_attempts",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_NotificationRecipientId",
                schema: "advance",
                table: "notification_delivery_attempts",
                column: "NotificationRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_delivery_attempts_NotificationRecipientId_Chan~",
                schema: "advance",
                table: "notification_delivery_attempts",
                columns: new[] { "NotificationRecipientId", "Channel", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_CompanyId_CancellationKey",
                schema: "advance",
                table: "notification_events",
                columns: new[] { "CompanyId", "CancellationKey" },
                unique: true,
                filter: "\"CancellationKey\" IS NOT NULL AND \"Status\" IN ('SCHEDULED','READY','ACTIVE','RECIPIENT_BLOCKED')");

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "notification_events",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_CompanyId_SourceEntityType_SourceEntity~",
                schema: "advance",
                table: "notification_events",
                columns: new[] { "CompanyId", "SourceEntityType", "SourceEntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_events_Status_NotBeforeAt_Id",
                schema: "advance",
                table: "notification_events",
                columns: new[] { "Status", "NotBeforeAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_CompanyId_NotificationEventId",
                schema: "advance",
                table: "notification_recipients",
                columns: new[] { "CompanyId", "NotificationEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_CompanyId_RecipientEmployeeId_ReadA~",
                schema: "advance",
                table: "notification_recipients",
                columns: new[] { "CompanyId", "RecipientEmployeeId", "ReadAt", "InAppAvailableAt" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_NotificationEventId",
                schema: "advance",
                table: "notification_recipients",
                column: "NotificationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_NotificationEventId_RecipientEmploy~",
                schema: "advance",
                table: "notification_recipients",
                columns: new[] { "NotificationEventId", "RecipientEmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_ReadByEmployeeId",
                schema: "advance",
                table: "notification_recipients",
                column: "ReadByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_recipients_RecipientEmployeeId",
                schema: "advance",
                table: "notification_recipients",
                column: "RecipientEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_CompanyId",
                schema: "advance",
                table: "store_category_routes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_CompanyId_DefaultAcceptedConditionLoc~",
                schema: "advance",
                table: "store_category_routes",
                columns: new[] { "CompanyId", "DefaultAcceptedConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_CompanyId_ItemCategoryId_EffectiveFrom",
                schema: "advance",
                table: "store_category_routes",
                columns: new[] { "CompanyId", "ItemCategoryId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_CompanyId_PendingReturnConditionLocat~",
                schema: "advance",
                table: "store_category_routes",
                columns: new[] { "CompanyId", "PendingReturnConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_CompanyId_QcHoldConditionLocationId",
                schema: "advance",
                table: "store_category_routes",
                columns: new[] { "CompanyId", "QcHoldConditionLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_DefaultAcceptedConditionLocationId",
                schema: "advance",
                table: "store_category_routes",
                column: "DefaultAcceptedConditionLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_ItemCategoryId",
                schema: "advance",
                table: "store_category_routes",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_PendingReturnConditionLocationId",
                schema: "advance",
                table: "store_category_routes",
                column: "PendingReturnConditionLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_store_category_routes_QcHoldConditionLocationId",
                schema: "advance",
                table: "store_category_routes",
                column: "QcHoldConditionLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_ActorEmployeeId",
                schema: "advance",
                table: "stores_document_status_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CompanyId_GateEntryId",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "CompanyId", "GateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_CorrelationId",
                schema: "advance",
                table: "stores_document_status_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stores_document_status_history_GateEntryId_OccurredAt",
                schema: "advance",
                table: "stores_document_status_history",
                columns: new[] { "GateEntryId", "OccurredAt" });

            migrationBuilder.Sql(FirstStoresPart1Sql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(FirstStoresPart1Sql.Down);

            migrationBuilder.DropTable(
                name: "business_rule_configuration_versions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "gate_entry_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "item_company_inventory_settings",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "notification_delivery_attempts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "store_category_routes",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stores_document_status_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "notification_recipients",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "gate_entries",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "notification_events",
                schema: "advance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_warehouse_condition_code",
                schema: "advance",
                table: "warehouse_condition_locations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_purchase_order_lines_PurchaseOrderId_Id",
                schema: "advance",
                table: "purchase_order_lines");

            migrationBuilder.AddCheckConstraint(
                name: "CK_warehouse_condition_code",
                schema: "advance",
                table: "warehouse_condition_locations",
                sql: "\"ConditionCode\" IN ('AVAILABLE','QC_HOLD','REJECTED','QUARANTINE','RETURN_TO_VENDOR','SCRAP')");
        }
    }
}
