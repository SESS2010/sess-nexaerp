using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdvanceInitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "advance");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UserLoginId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Result = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "controlled_configuration_histories",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controlled_configuration_histories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsCustomerCodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    LegalCustomerName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    CustomerType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GstNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PanNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Industry = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreditPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PortalOrganizationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "designations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_designations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_categories",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_approval_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MasterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_master_approval_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_attachment_metadata",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MasterId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_attachment_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_status_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MasterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceRevision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_status_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_policies",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyValue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_policies", x => x.Id);
                    table.CheckConstraint("CK_organization_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                });

            migrationBuilder.CreateTable(
                name: "page_definitions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Route = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_page_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_approval_route_settings",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ApproverRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ApproverResolutionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
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
                    table.CheckConstraint("CK_purchase_route_limits_valid", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")");
                });

            migrationBuilder.CreateTable(
                name: "purchase_approval_workflow_steps",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    ApproverResolutionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApproverEmployeeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ApproverRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_approval_workflow_steps", x => x.Id);
                    table.CheckConstraint("CK_purchase_workflow_amounts_valid", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\") AND \"StepNumber\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_number_sequences",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastNumber = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_number_sequences", x => x.Id);
                    table.CheckConstraint("CK_purchase_number_sequences_last_number_nonnegative", "\"LastNumber\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_transaction_approval_policies",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    ApproverRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_approval_policies", x => x.Id);
                    table.CheckConstraint("CK_purchase_transaction_policy_amounts", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")");
                    table.CheckConstraint("CK_purchase_transaction_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsPrivileged = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tax_gst_settings",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VendorRegistrationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GstRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CgstRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    SgstRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    IgstRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    CessRate = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    IsExempt = table.Column<bool>(type: "boolean", nullable: false),
                    IsReverseCharge = table.Column<bool>(type: "boolean", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RoundingScale = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_gst_settings", x => x.Id);
                    table.CheckConstraint("CK_tax_gst_component_split", "(\"SupplyType\" = 'INTRASTATE' AND \"IgstRate\" = 0 AND \"CgstRate\" + \"SgstRate\" = \"GstRate\") OR (\"SupplyType\" = 'INTERSTATE' AND \"CgstRate\" = 0 AND \"SgstRate\" = 0 AND \"IgstRate\" = \"GstRate\")");
                    table.CheckConstraint("CK_tax_gst_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_tax_gst_rates", "\"GstRate\" BETWEEN 0 AND 100 AND \"CgstRate\" BETWEEN 0 AND 100 AND \"SgstRate\" BETWEEN 0 AND 100 AND \"IgstRate\" BETWEEN 0 AND 100 AND \"CessRate\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_tax_gst_rounding", "\"RoundingScale\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_tax_gst_state_supply", "(\"SupplierStateCode\" = \"PlaceOfSupplyStateCode\" AND \"SupplyType\" = 'INTRASTATE') OR (\"SupplierStateCode\" <> \"PlaceOfSupplyStateCode\" AND \"SupplyType\" = 'INTERSTATE')");
                    table.CheckConstraint("CK_tax_gst_supply_type", "\"SupplyType\" IN ('INTRASTATE','INTERSTATE')");
                });

            migrationBuilder.CreateTable(
                name: "uoms",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MeasurementDimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantityPrecision = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uoms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vendor_categories",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsVendorCodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    LegalVendorName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    TradeName = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    VendorType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GstNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PanNumber = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    MsmeStatus = table.Column<bool>(type: "boolean", nullable: false),
                    MsmeNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MaterialServiceCategories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedMakes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeliveryTerms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreditPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    BankMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    AttachmentMetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    PortalOrganizationId = table.Column<string>(type: "text", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    VendorStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommercialVerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    CommercialVerifiedBy = table.Column<string>(type: "text", nullable: true),
                    CommercialVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RequiresReverification = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SiteName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_addresses_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_contacts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_contacts_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PayrollEmployeeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalImportedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Gender = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Qualification = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    EmployeeType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Grade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: true),
                    DateOfJoiningAccuracy = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsDateOfJoiningApproximate = table.Column<bool>(type: "boolean", nullable: false),
                    ApproximateDateNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FunctionalResponsibility = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WorkLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ManagerScope = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    LegacyDepartment = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    OfficialEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    MobileNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LoginEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsEmployeeCodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "advance",
                        principalTable: "designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_subcategories",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_subcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_subcategories_item_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_page_permissions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreate = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    CanSubmit = table.Column<bool>(type: "boolean", nullable: false),
                    CanIssue = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CanVerify = table.Column<bool>(type: "boolean", nullable: false),
                    CanApprove = table.Column<bool>(type: "boolean", nullable: false),
                    CanReject = table.Column<bool>(type: "boolean", nullable: false),
                    CanRequestClarification = table.Column<bool>(type: "boolean", nullable: false),
                    CanRequestRevision = table.Column<bool>(type: "boolean", nullable: false),
                    CanResubmit = table.Column<bool>(type: "boolean", nullable: false),
                    CanCancel = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeactivate = table.Column<bool>(type: "boolean", nullable: false),
                    CanPrint = table.Column<bool>(type: "boolean", nullable: false),
                    CanDownload = table.Column<bool>(type: "boolean", nullable: false),
                    CanExport = table.Column<bool>(type: "boolean", nullable: false),
                    CanUploadAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    CanReplaceAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewCommercialValues = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewAuditHistory = table.Column<bool>(type: "boolean", nullable: false),
                    HasFullControl = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_page_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_page_permissions_page_definitions_PageDefinitionId",
                        column: x => x.PageDefinitionId,
                        principalSchema: "advance",
                        principalTable: "page_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_page_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "advance",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UserType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MfaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_accounts_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "advance",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "uom_conversions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FromUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasurementDimension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "numeric(24,12)", precision: 24, scale: 12, nullable: false),
                    QuantityPrecision = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FirstUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uom_conversions", x => x.Id);
                    table.CheckConstraint("CK_uom_conversion_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_uom_conversion_distinct", "\"FromUomId\" <> \"ToUomId\"");
                    table.CheckConstraint("CK_uom_conversion_factor", "\"ConversionFactor\" > 0");
                    table.CheckConstraint("CK_uom_conversion_precision", "\"QuantityPrecision\" = 6");
                    table.ForeignKey(
                        name: "FK_uom_conversions_uoms_FromUomId",
                        column: x => x.FromUomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_uom_conversions_uoms_ToUomId",
                        column: x => x.ToUomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_addresses",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AddressLine = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_addresses_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_contacts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_contacts_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "department_approval_mappings",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Scope = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PrimaryApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlternateApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_approval_mappings", x => x.Id);
                    table.CheckConstraint("CK_department_approval_mapping_effective_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_department_approval_mapping_manager_route", "\"ApprovalRouteCode\" = 'MANAGER'");
                    table.ForeignKey(
                        name: "FK_department_approval_mappings_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_department_approval_mappings_employees_AlternateApproverEmp~",
                        column: x => x.AlternateApproverEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_department_approval_mappings_employees_PrimaryApproverEmplo~",
                        column: x => x.PrimaryApproverEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_approval_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_approval_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_approval_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_department_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceRevision = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_department_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_department_history_departments_NewDepartmentId",
                        column: x => x.NewDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_department_history_departments_PreviousDepartmentId",
                        column: x => x.PreviousDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_department_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_identity_mappings",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_identity_mappings", x => x.Id);
                    table.CheckConstraint("CK_employee_identity_mapping_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_employee_identity_mapping_type", "\"IdentityType\" IN ('HUMAN','SERVICE')");
                    table.ForeignKey(
                        name: "FK_employee_identity_mappings_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_import_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatch = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SourceEmployeeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceEmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedEmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_import_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_import_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_role_assignments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_role_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_role_assignments_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_role_assignments_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "advance",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_skills",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_skills_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "advance",
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_status_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_status_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_transaction_status_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_status_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_transaction_history_remarks", "length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_transaction_status_history_employees_ActorEmployee~",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reporting_relationships",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportingManagerEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentHeadEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporting_relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reporting_relationships_employees_DepartmentHeadEmployeeId",
                        column: x => x.DepartmentHeadEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reporting_relationships_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reporting_relationships_employees_ReportingManagerEmployeeId",
                        column: x => x.ReportingManagerEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_qualifications",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    QualificationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerifiedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_qualifications", x => x.Id);
                    table.CheckConstraint("CK_vendor_qualification_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_employees_VerifiedByEmployeeId",
                        column: x => x.VerifiedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_item_categories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsWarehouseCodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WarehouseType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsibleEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultReceivingLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultAcceptedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultQcHoldLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultRejectedLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultRepairableLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultScrapLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_warehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_warehouses_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouses_employees_ResponsibleEmployeeId",
                        column: x => x.ResponsibleEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsItemCodeLocked = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    DetailedDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubcategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Uom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: true),
                    BaseUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerMake = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PartNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    HsnSacCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GstPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TechnicalSpecification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DrawingDocumentReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    QcRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SerialNumberTracking = table.Column<bool>(type: "boolean", nullable: false),
                    BatchTracking = table.Column<bool>(type: "boolean", nullable: false),
                    ShelfLifeTracking = table.Column<bool>(type: "boolean", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BarcodeSymbology = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ImageStorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ImageFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ImageContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaximumStock = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PreferredVendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardEstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_items", x => x.Id);
                    table.CheckConstraint("CK_items_gst_valid", "\"GstPercentage\" >= 0 AND \"GstPercentage\" <= 28");
                    table.CheckConstraint("CK_items_maximum_stock_valid", "\"MaximumStock\" >= \"MinimumStock\"");
                    table.CheckConstraint("CK_items_minimum_stock_nonnegative", "\"MinimumStock\" >= 0");
                    table.CheckConstraint("CK_items_reorder_level_valid", "\"ReorderLevel\" >= 0 AND \"ReorderLevel\" <= \"MaximumStock\"");
                    table.ForeignKey(
                        name: "FK_items_item_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_item_subcategories_SubcategoryId",
                        column: x => x.SubcategoryId,
                        principalSchema: "advance",
                        principalTable: "item_subcategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalSchema: "advance",
                        principalTable: "manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_uoms_BaseUomId",
                        column: x => x.BaseUomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_items_vendors_PreferredVendorId",
                        column: x => x.PreferredVendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    PrSequence = table.Column<long>(type: "bigint", nullable: false),
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
                    table.CheckConstraint("CK_purchase_requisitions_estimated_total_nonnegative", "\"EstimatedTotal\" >= 0 AND \"PrSequence\" > 0");
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_employees_RequesterEmployeeId",
                        column: x => x.RequesterEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rack_bins",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    BinCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RackName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BinNameNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Zone = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LocationType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MaterialCondition = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CapacityQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    CapacityUom = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_rack_bins", x => x.Id);
                    table.UniqueConstraint("AK_rack_bins_WarehouseId_Id", x => new { x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_rack_bins_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_inspection_policies",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParameterCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MeasurementUomId = table.Column<Guid>(type: "uuid", nullable: false),
                    LowerLimit = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    UpperLimit = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: true),
                    InspectionMethod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspection_policies", x => x.Id);
                    table.CheckConstraint("CK_qc_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_qc_policy_limits", "\"LowerLimit\" IS NULL OR \"UpperLimit\" IS NULL OR \"UpperLimit\" >= \"LowerLimit\"");
                    table.CheckConstraint("CK_qc_policy_owner", "(\"ItemId\" IS NOT NULL) <> (\"ItemCategoryId\" IS NOT NULL)");
                    table.CheckConstraint("CK_qc_policy_sample", "\"SampleSize\" > 0");
                    table.ForeignKey(
                        name: "FK_qc_inspection_policies_item_categories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalSchema: "advance",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_policies_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_policies_uoms_MeasurementUomId",
                        column: x => x.MeasurementUomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_approval_history",
                schema: "advance",
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_attachments",
                schema: "advance",
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                schema: "advance",
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
                    table.CheckConstraint("CK_pr_lines_reconcile_requested", "\"ReservedQuantity\" <= \"RequestedQuantity\" AND \"ShortageQuantity\" = GREATEST(\"RequestedQuantity\" - \"ReservedQuantity\", 0) AND \"ProcurementHandoffQuantity\" = \"ShortageQuantity\"");
                    table.CheckConstraint("CK_pr_lines_requested_qty_positive", "\"RequestedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_purchase_requisitions_PurchaseRe~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_warehouses_PreferredWarehouseId",
                        column: x => x.PreferredWarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_status_history",
                schema: "advance",
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RfqNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsSingleSource = table.Column<bool>(type: "boolean", nullable: false),
                    SingleSourceJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_for_quotations", x => x.Id);
                    table.CheckConstraint("CK_rfqs_sequence_positive", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_rfqs_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                    table.CheckConstraint("CK_rfqs_status", "\"Status\" IN ('Draft','Issued','Closed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_request_for_quotations_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_purchase_requisitions_PurchaseRequis~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotations_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_availability_checks",
                schema: "advance",
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_operational_scopes",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnRecordsOnly = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsPrivilegedCrossScope = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_operational_scopes", x => x.Id);
                    table.CheckConstraint("CK_employee_operational_scope_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_employee_operational_scope_rack_warehouse", "\"RackBinId\" IS NULL OR \"WarehouseId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_rack_bins_WarehouseId_RackBinId",
                        columns: x => new { x.WarehouseId, x.RackBinId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    MovementType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    QuantityIn = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityOut = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_movements_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_rack_bins_RackBinId",
                        column: x => x.RackBinId,
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_movements_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_condition_locations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_condition_locations", x => x.Id);
                    table.CheckConstraint("CK_warehouse_condition_code", "\"ConditionCode\" IN ('AVAILABLE','QC_HOLD','REJECTED','QUARANTINE','RETURN_TO_VENDOR','SCRAP')");
                    table.CheckConstraint("CK_warehouse_condition_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_warehouse_condition_locations_rack_bins_WarehouseId_RackBin~",
                        columns: x => new { x.WarehouseId, x.RackBinId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouse_condition_locations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requirement_handoffs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requirement_handoffs_purchase_requisitions_Purchas~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_requirement_handoffs_rack_bins_RackBinId",
                        column: x => x.RackBinId,
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requirement_handoffs_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                        principalSchema: "advance",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_purchase_requisitions_PurchaseRequisitio~",
                        column: x => x.PurchaseRequisitionId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_reservations_rack_bins_RackBinId",
                        column: x => x.RackBinId,
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rfq_vendor_invitations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QuoteDueAtSnapshot = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VendorQualificationSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_vendor_invitations", x => x.Id);
                    table.CheckConstraint("CK_rfq_invitation_status", "\"Status\" IN ('Issued','Submitted','Withdrawn','Cancelled')");
                    table.ForeignKey(
                        name: "FK_rfq_vendor_invitations_request_for_quotations_RequestForQuo~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "advance",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfq_vendor_invitations_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_availability_check_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockAvailabilityCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ActiveReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    InTransitQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ShortageQuantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.CheckConstraint("CK_stock_check_lines_quantities_valid", "\"RequestedQuantity\" > 0 AND \"OnHandQuantity\" >= 0 AND \"ActiveReservedQuantity\" >= 0 AND \"AvailableQuantity\" >= 0 AND \"InTransitQuantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ShortageQuantity\" >= 0 AND \"ReservedQuantity\" <= \"RequestedQuantity\"");
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_purchase_requisition_lines_P~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_rack_bins_RackBinId",
                        column: x => x.RackBinId,
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_stock_availability_checks_St~",
                        column: x => x.StockAvailabilityCheckId,
                        principalSchema: "advance",
                        principalTable: "stock_availability_checks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_availability_check_lines_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_for_quotation_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequirementHandoffId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    PrNumberSnapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrLineNumberSnapshot = table.Column<int>(type: "integer", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SpecificationSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    AlreadyOrderedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OutstandingQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RfqQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RequiredDateSnapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_for_quotation_lines", x => x.Id);
                    table.CheckConstraint("CK_rfq_lines_quantities", "\"ApprovedQuantitySnapshot\" > 0 AND \"AlreadyOrderedQuantitySnapshot\" >= 0 AND \"OutstandingQuantitySnapshot\" >= 0 AND \"RfqQuantity\" > 0 AND \"RfqQuantity\" <= \"OutstandingQuantitySnapshot\"");
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_purchase_requirement_handoffs_P~",
                        column: x => x.PurchaseRequirementHandoffId,
                        principalSchema: "advance",
                        principalTable: "purchase_requirement_handoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_purchase_requisition_lines_Purc~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_for_quotation_lines_request_for_quotations_RequestF~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "advance",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservation_history",
                schema: "advance",
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
                        principalSchema: "advance",
                        principalTable: "stock_reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RfqVendorInvitationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousRevisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentRevision = table.Column<bool>(type: "boolean", nullable: false),
                    VendorQuoteReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubmissionSource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttachmentObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttachmentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VendorAttestation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLateSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    LateAuthorizedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LateAuthorizationRemarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantyTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_quotations", x => x.Id);
                    table.CheckConstraint("CK_vendor_quotation_late_authorization", "NOT \"IsLateSubmission\" OR (\"LateAuthorizedByEmployeeId\" IS NOT NULL AND length(trim(coalesce(\"LateAuthorizationRemarks\", ''))) > 0)");
                    table.CheckConstraint("CK_vendor_quotation_provenance", "\"SubmissionSource\" IN ('EMAIL_RECEIVED','PHYSICAL_RECEIVED') AND length(trim(\"AttachmentObjectKey\")) > 0 AND \"AttachmentSha256\" ~ '^[0-9A-Fa-f]{64}$' AND length(trim(\"VendorAttestation\")) > 0 AND \"ReceivedAt\" <= \"SubmittedAt\"");
                    table.CheckConstraint("CK_vendor_quotation_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_vendor_quotation_status", "\"Status\" IN ('Draft','Submitted','TechnicallyCompliant','TechnicallyRejected','Superseded','Withdrawn','Rejected')");
                    table.CheckConstraint("CK_vendor_quotation_total", "\"HeaderDiscountValue\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_vendor_quotations_employees_LateAuthorizedByEmployeeId",
                        column: x => x.LateAuthorizedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_rfq_vendor_invitations_RfqVendorInvitatio~",
                        column: x => x.RfqVendorInvitationId,
                        principalSchema: "advance",
                        principalTable: "rfq_vendor_invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_vendor_quotations_PreviousRevisionId",
                        column: x => x.PreviousRevisionId,
                        principalSchema: "advance",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotations_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_comparisons",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ComparisonNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RequestForQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendedVendorQuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelectedVendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsSingleSource = table.Column<bool>(type: "boolean", nullable: false),
                    SingleSourceJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecommendationRemarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_comparisons", x => x.Id);
                    table.CheckConstraint("CK_comparison_sequence_total", "\"SequenceNumber\" > 0 AND \"TotalPayableValue\" >= 0");
                    table.CheckConstraint("CK_comparison_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                    table.CheckConstraint("CK_comparison_status", "\"Status\" IN ('Draft','PendingApproval','Approved','Rejected','RevisionRequested','Cancelled')");
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_request_for_quotations_RequestForQuo~",
                        column: x => x.RequestForQuotationId,
                        principalSchema: "advance",
                        principalTable: "request_for_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_vendor_quotations_RecommendedVendorQ~",
                        column: x => x.RecommendedVendorQuotationId,
                        principalSchema: "advance",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparisons_vendors_SelectedVendorId",
                        column: x => x.SelectedVendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_quotation_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestForQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PackingForwarding = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Freight = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Insurance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxGstSettingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRuleSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VendorRegistrationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    SgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IgstValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CessValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PromisedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_quotation_lines", x => x.Id);
                    table.CheckConstraint("CK_vendor_quotation_line_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_vendor_quotation_line_values", "\"UnitRate\" >= 0 AND \"DiscountValue\" >= 0 AND \"HeaderDiscountValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TaxableValue\" >= 0 AND \"CgstValue\" >= 0 AND \"SgstValue\" >= 0 AND \"IgstValue\" >= 0 AND \"CessValue\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_request_for_quotation_lines_RequestF~",
                        column: x => x.RequestForQuotationLineId,
                        principalSchema: "advance",
                        principalTable: "request_for_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_tax_gst_settings_TaxGstSettingId",
                        column: x => x.TaxGstSettingId,
                        principalSchema: "advance",
                        principalTable: "tax_gst_settings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_quotation_lines_vendor_quotations_VendorQuotationId",
                        column: x => x.VendorQuotationId,
                        principalSchema: "advance",
                        principalTable: "vendor_quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RootPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsCurrentVersion = table.Column<bool>(type: "boolean", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TaxableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    HeaderDiscountValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TaxValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    PackingForwarding = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Freight = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Insurance = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    OtherCharges = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovalPolicySnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DeliveryTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantyTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AmendmentReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TransitionCorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_cancel_reason", "\"Status\" <> 'Cancelled' OR (\"CancelledAt\" IS NOT NULL AND length(trim(coalesce(\"CancellationReason\", ''))) > 0)");
                    table.CheckConstraint("CK_purchase_order_current_lifecycle", "\"Status\" <> 'Superseded' OR NOT \"IsCurrentVersion\"");
                    table.CheckConstraint("CK_purchase_order_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_purchase_order_status", "\"Status\" IN ('Draft','PendingApproval','Approved','Issued','Rejected','RevisionDraft','Resubmitted','Superseded','Cancelled')");
                    table.CheckConstraint("CK_purchase_order_values", "\"TaxableValue\" >= 0 AND \"DiscountValue\" >= 0 AND \"HeaderDiscountValue\" >= 0 AND \"TaxValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TotalPayableValue\" >= 0 AND \"ApprovalPolicySnapshotJson\" <> '{}'::jsonb");
                    table.ForeignKey(
                        name: "FK_purchase_orders_commercial_comparisons_CommercialComparison~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "advance",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_departments_RequestingDepartmentId",
                        column: x => x.RequestingDepartmentId,
                        principalSchema: "advance",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_purchase_orders_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_DeliveryWarehouseId",
                        column: x => x.DeliveryWarehouseId,
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_transaction_approval_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ApprovalRoute = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_transaction_approval_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_approval_history_remarks", "length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_transaction_approval_history_commercial_comparison~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "advance",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_transaction_approval_history_employees_ActorEmploy~",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commercial_comparison_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalComplianceSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CommercialSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    DeliverySnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WarrantySnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PaymentTermsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    IsRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commercial_comparison_lines", x => x.Id);
                    table.CheckConstraint("CK_comparison_line_total", "\"TotalPayableValue\" >= 0 AND (NOT \"IsRecommended\" OR length(trim(coalesce(\"RecommendationReason\", ''))) > 0)");
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_commercial_comparisons_Commerci~",
                        column: x => x.CommercialComparisonId,
                        principalSchema: "advance",
                        principalTable: "commercial_comparisons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_vendor_quotation_lines_VendorQu~",
                        column: x => x.VendorQuotationLineId,
                        principalSchema: "advance",
                        principalTable: "vendor_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commercial_comparison_lines_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_technical_verifications",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorQuotationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifierEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ComplianceSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_technical_verifications", x => x.Id);
                    table.CheckConstraint("CK_quote_technical_status", "\"ComplianceStatus\" IN ('TechnicallyCompliant','TechnicallyRejected') AND length(trim(\"Remarks\")) > 0");
                    table.ForeignKey(
                        name: "FK_quotation_technical_verifications_employees_VerifierEmploye~",
                        column: x => x.VerifierEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_technical_verifications_vendor_quotation_lines_Ve~",
                        column: x => x.VendorQuotationLineId,
                        principalSchema: "advance",
                        principalTable: "vendor_quotation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_history", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_history_reason", "length(trim(\"Reason\")) > 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_history_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialComparisonLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequisitionLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequirementHandoffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UomSnapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ApprovedOutstandingQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CommercialSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TaxRuleSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalPayableValue = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.CheckConstraint("CK_purchase_order_line_quantity", "\"OrderedQuantity\" > 0 AND \"ApprovedOutstandingQuantitySnapshot\" > 0 AND \"OrderedQuantity\" <= \"ApprovedOutstandingQuantitySnapshot\" AND \"UnitRate\" >= 0 AND \"TotalPayableValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_commercial_comparison_lines_Commercial~",
                        column: x => x.CommercialComparisonLineId,
                        principalSchema: "advance",
                        principalTable: "commercial_comparison_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_requirement_handoffs_Purchase~",
                        column: x => x.PurchaseRequirementHandoffId,
                        principalSchema: "advance",
                        principalTable: "purchase_requirement_handoffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_requisition_lines_PurchaseReq~",
                        column: x => x.PurchaseRequisitionLineId,
                        principalSchema: "advance",
                        principalTable: "purchase_requisition_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "material_followup_handoffs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandoffNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OrderedQuantitySnapshot = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HandoffAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_followup_handoffs", x => x.Id);
                    table.CheckConstraint("CK_material_followup_quantity", "\"OrderedQuantitySnapshot\" > 0 AND \"Status\" IN ('PendingFollowUp','InProgress','Completed')");
                    table.ForeignKey(
                        name: "FK_material_followup_handoffs_purchase_order_lines_PurchaseOrd~",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "advance",
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_material_followup_handoffs_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "audit_logs",
                columns: new[] { "Id", "Action", "AfterJson", "BeforeJson", "CorrelationId", "CreatedAt", "CreatedBy", "EntityId", "EntityName", "IpAddress", "Module", "Result", "UpdatedAt", "UpdatedBy", "UserLoginId", "Version" },
                values: new object[,]
                {
                    { new Guid("11744032-08e9-f364-d36f-c12caeff0b02"), "SeedInitialStatus", "{\"statusHistoryCount\":39,\"newStatus\":\"Active\"}", null, "REV866C1_INITIAL_STATUS", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "REV866C1_EMPLOYEE_STATUS_INITIAL", "EmployeeStatusHistory", null, "Employees", "Success", null, null, "system-migration-rev866c1", 0L },
                    { new Guid("2a23e241-204c-4810-46cd-5f1b0f513434"), "Denied", "{\"permission\":\"view\",\"result\":\"denied\",\"sourceRevision\":\"REV866C1\"}", null, "REV866C1_PERMISSION_DENIAL", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "view", "employees.master", null, "Security", "Failure", null, null, "system-migration-rev866c1", 0L },
                    { new Guid("2e2eb9a5-7caa-e157-2099-e3f06e85fbad"), "ApprovalStatusChangeEvidence", "{\"approvalStatus\":\"SeedApproved\",\"evidence\":\"corrective checkpoint\"}", "{\"approvalStatus\":\"SeedApproved\"}", "REV866C1_EMPLOYEE_STATUS_CHANGE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "REV866C1_EMPLOYEE_APPROVAL_STATUS", "EmployeeApprovalHistory", null, "Employees", "Success", null, null, "system-migration-rev866c1", 0L },
                    { new Guid("51a38ab8-5943-e4f6-6140-76dea2057e8b"), "SeedRoleAssignments", "{\"assignmentCount\":40,\"sourceRevision\":\"REV866\"}", null, "REV866C1_ROLE_ASSIGNMENT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "REV866_EMPLOYEE_ROLE_ASSIGNMENTS", "EmployeeRoleAssignment", null, "Employees", "Success", null, null, "system-migration-rev866c1", 0L },
                    { new Guid("bf16025e-df11-ac0e-785b-4873e1a14af3"), "Import", "{\"employeeCount\":39,\"sourceRevision\":\"REV866\"}", null, "REV866C1_EMPLOYEE_IMPORT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "REV866_EMPLOYEE_SEED_20260808", "EmployeeImportHistory", null, "Employees", "Success", null, null, "system-migration-rev866c1", 0L },
                    { new Guid("bf6ef4ae-fe3a-2861-28d4-88f7708aba51"), "RoleMappingChangeEvidence", "{\"mapping\":\"seeded approved role mappings preserved\"}", "{\"mapping\":\"none\"}", "REV866C1_ROLE_MAPPING_CHANGE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", "REV866C1_ROLE_MAPPING_CHANGE", "EmployeeRoleAssignment", null, "Employees", "Success", null, null, "system-migration-rev866c1", 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "departments",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0057b580-1cb1-afa2-8328-5afb1162e77e"), "MANAGEMENT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Management", null, null, 0L },
                    { new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), "PRODUCTION_FABRICATION", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Production/Fabrication", null, null, 0L },
                    { new Guid("6ea3e733-e5e0-9b55-e7de-db94afda2b09"), "MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Manager", null, null, 0L },
                    { new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), "JUNIOR_ASSISTANT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Junior/Assistant", null, null, 0L },
                    { new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), "ADMIN_ACCOUNTS_STORES", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Admin/Accounts/Stores", null, null, 0L },
                    { new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), "ENGINEER_TECHNICAL", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Engineer/Technical", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "designations",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("075fb64f-355a-ee74-517b-6b9c6da0f8db"), "LABVIEW_DEVELOPER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "LABVIEW DEVELOPER", null, null, 0L },
                    { new Guid("086ab1d4-3404-12b7-c35a-4b77737eb97b"), "TECHNICAL_DIRECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "TECHNICAL DIRECTOR", null, null, 0L },
                    { new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "ELECTRICAL_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "ELECTRICAL ENGINEER", null, null, 0L },
                    { new Guid("35936fb3-4fc0-4757-268f-c467720e39fa"), "JUNIOR_ACCOUNTS", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "JUNIOR ACCOUNTS", null, null, 0L },
                    { new Guid("37ae1390-d60b-28aa-f5f8-43b5549936c8"), "JR._ACCOUNT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "JR. ACCOUNT", null, null, 0L },
                    { new Guid("39f842c4-5688-20a6-2a81-dc0fed68aa0f"), "JR._ELECTRICAL___PLC___INSTRUMENTATION_SUPPORT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT", null, null, 0L },
                    { new Guid("4c22a815-6a44-3d0b-9bd2-45743fc0a9aa"), "JUNIOR_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "JUNIOR ENGINEER", null, null, 0L },
                    { new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "DESIGN_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "DESIGN ENGINEER", null, null, 0L },
                    { new Guid("82783939-c768-2002-5b0e-17db5261eab9"), "HR_DEPT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "HR DEPT", null, null, 0L },
                    { new Guid("8e377677-95bb-f0fe-4207-2efaf2b89208"), "ADMIN_MAINTENANCE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "ADMIN MAINTENANCE", null, null, 0L },
                    { new Guid("90c527f8-3ea8-dc72-7283-c80e73a71f5d"), "SOFTWARE_DEVELOPER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "SOFTWARE DEVELOPER", null, null, 0L },
                    { new Guid("940ac030-8dcf-1575-6545-fea0f75f18f8"), "STORES_AND_PURCHASE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "STORES AND PURCHASE", null, null, 0L },
                    { new Guid("96908ceb-4e96-b670-db7e-59b2237f1dec"), "PRODUCTION_COORDINATOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "PRODUCTION COORDINATOR", null, null, 0L },
                    { new Guid("a2ed4710-4cec-d8dd-097e-e8c7353a66a6"), "JR._ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "JR. ENGINEER", null, null, 0L },
                    { new Guid("a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830"), "MD", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "MD", null, null, 0L },
                    { new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "REFRIGERATION___MECHANICAL_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "REFRIGERATION / MECHANICAL ENGINEER", null, null, 0L },
                    { new Guid("b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa"), "PLC_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "PLC ENGINEER", null, null, 0L },
                    { new Guid("c7775052-f0a9-27e3-f259-746120a113a6"), "TECHNICAL_SUPPORT_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "TECHNICAL SUPPORT MANAGER", null, null, 0L },
                    { new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "FABRICATOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "FABRICATOR", null, null, 0L },
                    { new Guid("f38530d3-549c-8fe3-3f75-331795d92bd3"), "PRODUCTION_MECHANICAL_TEAM", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "PRODUCTION MECHANICAL TEAM", null, null, 0L },
                    { new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6"), "STORES_ASSISTANT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "STORES ASSISTANT", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "organization_policies",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsActive", "OrganizationId", "PolicyCode", "PolicyValue", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", new DateOnly(2026, 8, 10), null, true, "SESS", "VENDOR_FINAL_APPROVER", "MANAGING_DIRECTOR", null, null, 0L },
                    { new Guid("50000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", new DateOnly(2026, 8, 10), null, true, "SESS", "INVENTORY_VALUATION_METHOD", "WEIGHTED_AVERAGE", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Identity", "identity.roles", "/identity/roles", "Role Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Identity", "identity.users", "/identity/users", "User Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Admin", "authorization.pages", "/authorization/pages", "Page Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Admin", "authorization.role-pages", "/authorization/role-pages", "Role Page Permissions", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.customers", "/masters/customers", "Customer Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.vendors", "/masters/vendors", "Vendor Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Inventory", "inventory.items", "/inventory/items", "Item Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Inventory", "inventory.warehouses", "/inventory/warehouses", "Warehouse Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Inventory", "inventory.rack-bins", "/inventory/rack-bins", "Rack/Bin Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requests", "/purchase/requests", "Purchase Request", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.rfq", "/purchase/rfq", "RFQ", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.po", "/purchase/purchase-orders", "Purchase Order", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000013"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Inventory", "inventory.grn", "/inventory/grn", "GRN", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000014"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Inventory", "inventory.stock-ledger", "/inventory/stock-ledger", "Stock Ledger", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000015"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Audit", "audit.history", "/audit/history", "Audit History", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000016"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.master", "/employees", "Employee Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000017"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.role-mapping", "/employees/roles", "Employee Role Mapping", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000018"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.audit-history", "/employees/audit-history", "Employee Audit History", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000019"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.items", "/masters/items", "Item Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.warehouses", "/masters/warehouses", "Warehouse/Store Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.rack-bins", "/masters/rack-bins", "Rack/Bin Location Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000022"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requisitions", "/purchase/requisitions", "Purchase Requisitions", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000023"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requisition-approvals", "/purchase/requisition-approvals", "PR Approvals", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000024"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Stores", "stores.stock-check", "/stores/stock-check", "Stock Availability Check", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000025"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Stores", "stores.reservations", "/stores/reservations", "Stock Reservations", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000026"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase", "purchase.requirement-handoff", "/purchase/requirement-handoff", "Purchase Requirement Handoff", null, null, 0L },
                    { new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.vendor-quotations", "/purchase/vendor-quotations", "Vendor Quotations", null, null, 0L },
                    { new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.material-followup", "/purchase/material-followup", "Material Follow-up", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Security", "security.employee-identities", "/security/employee-identities", "Employee Identities", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Security", "security.operational-scopes", "/security/operational-scopes", "Operational Scopes", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.uoms", "/masters/uoms", "UOM Master", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.uom-conversions", "/masters/uom-conversions", "UOM Conversion Master", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Settings", "settings.tax-gst", "/settings/tax-gst", "Tax/GST Settings", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.vendor-qualifications", "/masters/vendor-qualifications", "Vendor Qualifications", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.warehouse-condition-locations", "/masters/warehouse-condition-locations", "Warehouse Condition Locations", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "QC", "qc.inspection-policies", "/qc/inspection-policies", "QC Inspection Policies", null, null, 0L },
                    { new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.commercial-comparisons", "/purchase/commercial-comparisons", "Commercial Comparisons", null, null, 0L },
                    { new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, "Purchase", "purchase.technical-verification", "/purchase/technical-verification", "Technical Verification", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "purchase_transaction_approval_policies",
                columns: new[] { "Id", "ApproverRoleCode", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsActive", "MaximumAmount", "MinimumAmount", "OrganizationId", "RouteCode", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0e6e49ea-95c5-86dd-1c23-18d61c50f4c1"), "MANAGING_DIRECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 999999999999999999.999999m, 500000.000001m, "SESS", "MANAGING_DIRECTOR", null, null, 0L },
                    { new Guid("d7b12d20-a4be-c916-9f5e-de2245510b91"), "PURCHASE_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 50000m, 0m, "SESS", "MANAGER", null, null, 0L },
                    { new Guid("f9505a0c-182b-7627-52f4-1197a29e4c16"), "TECHNICAL_DIRECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", new DateOnly(2026, 8, 11), null, true, 500000m, 50000.000001m, "SESS", "TECHNICAL_DIRECTOR", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), "accounts_assistant", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Accounts Assistant", null, null, 0L },
                    { new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), "managing_director", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Managing Director", null, null, 0L },
                    { new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), "technical_support_manager", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Technical Support Manager", null, null, 0L },
                    { new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), "hr_executive", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "HR Executive", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000001"), "admin", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Administrator", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "md", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Managing Director / CFO", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "accounts_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Accounts Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "purchase_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Purchase Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "store_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Store Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "production_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Production Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "qc_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "QC Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "design_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Design Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "service_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Service Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "sales_head", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Sales Head", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "service_coordinator", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Service Coordinator", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "service_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Service Engineer", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "sales_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Sales Engineer", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "it_admin", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "IT Admin", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "customer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Customer Portal User", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "vendor", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Vendor Portal User", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "document_controller", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Document Controller", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "dcc", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "DCC / Document Controller", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "branch_manager", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Branch Manager", null, null, 0L },
                    { new Guid("10000000-0000-0000-0000-000000000020"), "ops_admin_no_hr", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Operational Admin without HR", null, null, 0L },
                    { new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), "electrical_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Electrical Engineer", null, null, 0L },
                    { new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), "stores_assistant", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Stores Assistant", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000001"), "PURCHASE_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "Purchase Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "STORES_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "Stores Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "QC_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "QC Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "QC_INSPECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, false, "QC Inspector", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000005"), "DEPARTMENT_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "advance-baseline", true, true, "Department Manager", null, null, 0L },
                    { new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), "software_developer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Software Developer", null, null, 0L },
                    { new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), "technical_director", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Technical Director", null, null, 0L },
                    { new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), "purchase_executive", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Purchase Executive", null, null, 0L },
                    { new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), "plc_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "PLC Engineer", null, null, 0L },
                    { new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), "software_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Software Engineer", null, null, 0L },
                    { new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), "technical_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Technical Engineer", null, null, 0L },
                    { new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), "admin_executive", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Admin Executive", null, null, 0L },
                    { new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), "stores_executive", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Stores Executive", null, null, 0L },
                    { new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), "production_operator", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Production Operator", null, null, 0L },
                    { new Guid("c4133420-c386-9452-93a7-484e18105372"), "junior_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Junior Engineer", null, null, 0L },
                    { new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), "design_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Design Engineer", null, null, 0L },
                    { new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), "production_coordinator", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Production Coordinator", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "skills",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("6bb4adb2-ac56-5ebc-abd0-f0eb65cd965a"), "MANAGEMENT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Management", null, null, 0L },
                    { new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), "JUNIOR_ASSISTANT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Junior/Assistant", null, null, 0L },
                    { new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), "PRODUCTION_FABRICATION", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Production/Fabrication", null, null, 0L },
                    { new Guid("972ffd8b-159a-fbe4-9a9a-a3913ce3a623"), "ADMIN_ACCOUNTS_STORES", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Admin/Accounts/Stores", null, null, 0L },
                    { new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), "ENGINEER_TECHNICAL", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Engineer/Technical", null, null, 0L },
                    { new Guid("ffbbe947-c562-fa9e-3962-a4ce411c8004"), "MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Manager", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employees",
                columns: new[] { "Id", "ApprovalStatus", "ApproximateDateNote", "CreatedAt", "CreatedBy", "DateOfBirth", "DateOfJoining", "DateOfJoiningAccuracy", "DepartmentId", "DesignationId", "EmployeeCode", "EmployeeName", "EmployeeType", "FunctionalResponsibility", "Gender", "Grade", "IsDateOfJoiningApproximate", "IsEmployeeCodeLocked", "LegacyDepartment", "LoginEnabled", "ManagerScope", "MobileNumber", "OfficialEmail", "OriginalImportedName", "PayrollEmployeeId", "Qualification", "Status", "UpdatedAt", "UpdatedBy", "Version", "WorkLocation" },
                values: new object[,]
                {
                    { new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("a2ed4710-4cec-d8dd-097e-e8c7353a66a6"), "SESS-009", "MANIKANDAN.S", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "MANIKANDAN.S", null, null, "Active", null, null, 0L, null },
                    { new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-013", "LALU", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "LALU", null, null, "Active", null, null, 0L, null },
                    { new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-019", "RANJITH. R", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "RANJITH. R", null, null, "Active", null, null, 0L, null },
                    { new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-018", "A. VINAYA SAGAR ARKATI", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "A. VINAYA SAGAR ARKATI", null, null, "Active", null, null, 0L, null },
                    { new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("90c527f8-3ea8-dc72-7283-c80e73a71f5d"), "SESS-008", "SURANTHER P", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SURANTHER P", null, null, "Active", null, null, 0L, null },
                    { new Guid("26c37705-e799-8708-119b-1227908d5e0f"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-024", "PRAKASAM.B", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "PRAKASAM.B", null, null, "Active", null, null, 0L, null },
                    { new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa"), "SESS-038", "SYED IJAZUDDIN Z", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SYED IJAZUDDIN Z", null, null, "Active", null, null, 0L, null },
                    { new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-030", "MANIKANDAN SOKKALINGAM", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "MANIKANDAN SOKKALINGAM", null, null, "Active", null, null, 0L, null },
                    { new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("96908ceb-4e96-b670-db7e-59b2237f1dec"), "SESS-023", "SARATH BABU.K", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SARATH BABU.K", null, null, "Active", null, null, 0L, null },
                    { new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-028", "PRAVEEN KUMAR.M", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "PRAVEEN KUMAR.M", null, null, "Active", null, null, 0L, null },
                    { new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("0057b580-1cb1-afa2-8328-5afb1162e77e"), new Guid("086ab1d4-3404-12b7-c35a-4b77737eb97b"), "SESS-001", "A. PARAMANANTHAM", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "A. PARAMANANTHAM", null, null, "Active", null, null, 0L, null },
                    { new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-035", "VINAYAGAM", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "VINAYAGAM", null, null, "Active", null, null, 0L, null },
                    { new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-016", "KALIDOSS", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "KALIDOSS", null, null, "Active", null, null, 0L, null },
                    { new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("075fb64f-355a-ee74-517b-6b9c6da0f8db"), "SESS-032", "PRASANNA.G", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "PRASANNA.G", null, null, "Active", null, null, 0L, null },
                    { new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-010", "RAJESHKUMAR.V", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "RAJESHKUMAR.V", null, null, "Active", null, null, 0L, null },
                    { new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-022", "KARTHICK.B", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "KARTHICK.B", null, null, "Active", null, null, 0L, null },
                    { new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-026", "SRINIVASAN.V", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SRINIVASAN.V", null, null, "Active", null, null, 0L, null },
                    { new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-029", "SRINIVASAN.C", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SRINIVASAN.C", null, null, "Active", null, null, 0L, null },
                    { new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("37ae1390-d60b-28aa-f5f8-43b5549936c8"), "SESS-007", "A. ALFATHIMA PARVEEN", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "A. ALFATHIMA PARVEEN", null, null, "Active", null, null, 0L, null },
                    { new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("f38530d3-549c-8fe3-3f75-331795d92bd3"), "SESS-005", "WASEEM.S", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "WASEEM.S", null, null, "Active", null, null, 0L, null },
                    { new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("0057b580-1cb1-afa2-8328-5afb1162e77e"), new Guid("a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830"), "SESS-002", "ALAGUEASWARI", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "ALAGUEASWARI", null, null, "Active", null, null, 0L, null },
                    { new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-037", "DEVANAND B", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "DEVANAND B", null, null, "Active", null, null, 0L, null },
                    { new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("6ea3e733-e5e0-9b55-e7de-db94afda2b09"), new Guid("c7775052-f0a9-27e3-f259-746120a113a6"), "SESS-004", "T. DINESH", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "T. DINESH", null, null, "Active", null, null, 0L, null },
                    { new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6"), "SESS-014", "KAMALI SRINIVASAN", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "KAMALI SRINIVASAN", null, null, "Active", null, null, 0L, null },
                    { new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("4c22a815-6a44-3d0b-9bd2-45743fc0a9aa"), "SESS-011", "YESWANTH KUMAR.N", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "YESWANTH KUMAR.N", null, null, "Active", null, null, 0L, null },
                    { new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("a2ed4710-4cec-d8dd-097e-e8c7353a66a6"), "SESS-033", "BLESSON PAUL", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "BLESSON PAUL", null, null, "Active", null, null, 0L, null },
                    { new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-034", "MADHANKUMAR.J", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "MADHANKUMAR.J", null, null, "Active", null, null, 0L, null },
                    { new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-039", "THIRUNAVUKKARASU", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "THIRUNAVUKKARASU", null, null, "Active", null, null, 0L, null },
                    { new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("35936fb3-4fc0-4757-268f-c467720e39fa"), "SESS-031", "VENKAT RAV.S", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "VENKAT RAV.S", null, null, "Active", null, null, 0L, null },
                    { new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-025", "KARTHIKEYAN MK", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "KARTHIKEYAN MK", null, null, "Active", null, null, 0L, null },
                    { new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-003", "M. SATHISHKUMAR", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "M. SATHISHKUMAR", null, null, "Active", null, null, 0L, null },
                    { new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("940ac030-8dcf-1575-6545-fea0f75f18f8"), "SESS-012", "PRIYA.E", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "PRIYA.E", null, null, "Active", null, null, 0L, null },
                    { new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("8e377677-95bb-f0fe-4207-2efaf2b89208"), "SESS-021", "KRISHNAVENI", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "KRISHNAVENI", null, null, "Active", null, null, 0L, null },
                    { new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-015", "RANJITH.E", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "RANJITH.E", null, null, "Active", null, null, 0L, null },
                    { new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("35936fb3-4fc0-4757-268f-c467720e39fa"), "SESS-027", "SANJAY SARAVANAN", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "SANJAY SARAVANAN", null, null, "Active", null, null, 0L, null },
                    { new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-036", "FRANCIS XAVIER", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "FRANCIS XAVIER", null, null, "Active", null, null, 0L, null },
                    { new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("4c22a815-6a44-3d0b-9bd2-45743fc0a9aa"), "SESS-017", "MOHD ASHIQ", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "MOHD ASHIQ", null, null, "Active", null, null, 0L, null },
                    { new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("39f842c4-5688-20a6-2a81-dc0fed68aa0f"), "SESS-006", "S. NANTHAKUMAR", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "S. NANTHAKUMAR", null, null, "Active", null, null, 0L, null },
                    { new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), "SeedApproved", null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, null, null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("82783939-c768-2002-5b0e-17db5261eab9"), "SESS-020", "RANJEETH.B", "Permanent", null, null, "Executive", false, true, null, false, null, null, null, "RANJEETH.B", null, null, "Active", null, null, 0L, null }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("004fb496-d229-d6cc-5c2e-d6ea2b193b4a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("00cbfa57-17fb-9bc9-ebc2-d82593db20c0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("01034d48-c4aa-7261-e9e4-888832ab13b2"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("010a0c72-51e7-5832-2267-3788f0e50446"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("01275179-960c-8401-c25c-ff3ea100b465"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("0130c6a9-a282-fe5f-0e87-f85dc76b2051"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanIssue", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("01a0c648-2bdc-3643-465f-d013309c37be"), false, true, true, false, true, true, true, true, false, false, false, false, true, true, true, true, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01a8ed83-0e17-63e1-ff4c-cdc4dadcd776"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("01afd214-f457-6905-469e-95e1ba60771c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("01b635f9-b7c0-6952-aad1-db0a13aabe39"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("01ba57da-bf38-37ff-1b4d-7a89bba40f68"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("01ddbcb6-21be-e7ce-a93e-6fb7bcc0dc53"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("0229a2fa-bdb6-b6b5-4da0-db1e3bc6d395"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("026bea62-c207-632b-d3d7-cafb5c973658"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("0296b8ac-3ef8-319e-2bbd-52fc1434991a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("02f7e8f7-a3d1-08cc-3d1b-9e439be2cf0d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("03043ba5-389c-3233-01eb-fc5a0b52e88f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("031586f9-bb24-8506-db97-f5714fa795ec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("032618b3-6ddd-dbb6-c6a7-9fa81b357f37"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("036447eb-18a7-241a-c0e7-6c84b3fd572a"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("03992abd-35a5-cdf5-c20d-6febeefceb22"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("03c74b25-7022-9594-cca0-2ded65991f10"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("03ebd08f-a093-d3cf-8f87-46300c8d1dba"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("03f83275-7beb-0b99-204e-b232181c659f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("0427700a-1559-bcd0-9af2-ef7a1afd7c50"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("0465f200-8bf3-8526-6ff2-7cabe33dc321"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("049a5c74-1f4e-5f6b-ada7-6ee9e078f31b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("04d5edd1-bd0c-fa30-5694-836b6f46cc46"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("0557f009-230c-3043-7634-ef0d1dc3480b"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("058ec479-426d-9ccb-79ab-06ba7768ccd5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("062c8d00-221a-5347-8c3b-bd87604fc083"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("062fd69d-1356-6930-ecdb-1c13ae5a01d5"), false, false, false, false, true, false, true, false, false, true, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("066b907a-90f6-f400-31c2-9a8de85f58fa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("07627316-54e4-db4d-77ef-0f161f685487"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("07e0a0a9-0c56-ff51-7a64-df05cb4d8641"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("08286f2c-f6f7-fac1-de8c-a4736570cc51"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("082c708f-c0ee-a4b7-547a-b5547cee5a48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("08307be7-8234-e259-ae74-f9392ed2a1fb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("083fc830-139f-4006-f637-5f900fd8132e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("08db82bf-3428-9428-5102-888698acbaaa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("08df695d-e9ba-70f9-dd5e-6b8d88551bb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("09119528-8023-d53b-94dd-5a4e94862289"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("0967d616-a202-f778-22f4-5c0c5606efd3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("09b4df68-6e86-6dd3-a000-2e81cbfda172"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("09cc0de9-fcac-63a6-1a66-aae0fa144ee7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("09e6faf2-aa84-4975-dd32-33617250adb0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("0a41b95f-94b3-3462-7529-66fe94b49291"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("0a848105-61e9-9489-6047-4c2bb6182dd7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("0a99c83f-8f94-ecb7-a877-69267beedd8c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("0b3c3f4a-2d9a-ac8f-d9ae-9ff61418f67b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0b6178f3-935b-5f40-be62-1209ebaee582"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("0b7594fe-4132-dd48-944a-6107faae95f2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("0b97b7a0-d2ac-a4da-0930-5296011b4496"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("0c3e02e8-2bcd-4d05-a3c8-312d7d66ba22"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("0d271d3e-4008-1465-6de5-9b660ff60bf7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("0d663ce6-3756-5828-aae7-321d6f53031d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0d855678-6da1-cb30-a345-23fe101560e0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("0df3abd0-c90c-3d36-91bb-68b49e0f2605"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("0ed63eb1-6b6e-2fbd-fa25-321db2a61672"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("0ef21be8-e408-8a8e-e2c6-3e789e64302b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("0ef31d8a-189c-19fe-4a78-033ae9e70bc6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0f11ca79-fd0e-dfad-030c-865843cb8512"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0f4187ee-9df8-cf46-4ace-4f8349bdbf37"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("0f6fe7be-2afc-f4e7-2245-a5366599dfa9"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("0f9deb7e-4745-0527-9d8e-bb60c8cececa"), false, true, true, false, true, false, true, true, false, true, true, true, true, true, true, true, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("0fcdc8eb-2a3f-7ea7-b022-c3396d868d56"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0fdc8bf6-3644-6d7c-913e-c5d93ecebda4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("1056ab04-f3e9-fb95-b805-4a51a7698c69"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("1059f07f-9ce5-de0f-c16c-01cf02116aed"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("1067e842-f711-c5f8-c54f-605d218e3e9b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("10a312ff-3606-ee0c-b384-03bf891c5d8f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("10c78633-b41c-5825-051f-a146d4402aeb"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("11038140-87fb-6522-7425-da633f209502"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("11b71479-d821-6aa9-75d9-307f56d90621"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("11e503aa-3973-c88f-2ccb-2882053ecd4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("1216c9a3-31eb-e2bb-3238-9c3b6dde5daf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("125a37ad-46bf-b2c4-c02f-d588f0969a84"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("12694a69-1d2c-4e6b-a81f-65ce1582f29f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("12a9d791-55b7-7c0d-fb6d-99780a741e5b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("12ba4a62-899f-a2c2-6ca1-8c3c1399f8d3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("12e1379b-f17a-7f0a-e522-8dba3b966cf9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("12fcb308-740b-14aa-cc1f-0197ce4c2448"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("13ca2fe4-4115-8977-0feb-782fe436d5eb"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("13e130d5-1294-8526-2109-72829c861c16"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("14054c28-010a-9856-0bb2-7e22d562edff"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("144a76c1-f002-aee5-6f2d-3beb9a95aec5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("14728dfc-d82e-6bfa-923b-5770ddac7bac"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("14aed82d-d726-2c80-fe1c-6e3c54538789"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("14fbdac8-67bc-e8e6-8b56-54d70160c626"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("14ffdc95-b241-a3c5-9968-ef467797859b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("15ebeb75-142b-95a5-0c8f-f76f67e2cb93"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("15ee5b19-d532-c28c-b755-de4152769a7a"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("161a0fe1-1bd2-aefb-1f3a-a8ff3d72c280"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("1668624a-f2ad-0829-d2dd-0d4ea7ed0de4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("169c329d-735d-3ae9-d519-17363643a809"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("16ae3bf1-2b26-07e6-ed2a-6778ac80d373"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("16bcfa5d-19d9-48d6-8c65-8fe9b00ad2f2"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("1737277b-1cfa-ad32-67a2-49fd84c7b8dc"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("173a3c08-7d51-8ee8-3a79-be3f114054fe"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("173f2300-ec29-19df-e9e3-1370ab9c8ad9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("175418b4-d466-1033-67bf-185f2dda3fe1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("17579bb5-969a-3378-52b7-76e4f6cabfc6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("175aef9a-6f31-588c-9e0c-cbf21dfef7ac"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("17df4e3d-7834-3baf-449f-432487209c99"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("17e0a6d4-754a-5b66-dd33-9cf605995071"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("17f30c40-8f89-f202-f1da-648eb7c00612"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("18203f3c-be44-3b65-acce-669dfcc2f9d1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("18226f75-4c36-6e6c-7db1-ac8b334b418f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("18374130-7861-27c5-cea8-0dc5824ada09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("187b9f2e-0af9-56bf-865c-2e5e656737c4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("18919a69-5da5-457f-b7e9-414d3df60136"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("18f0cf47-9b69-ae81-4ca6-c669be41d7d0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("18fec771-1b4b-ebbf-bf98-f4747886f977"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("1913267c-7d4c-e241-5011-8cf30bd84137"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("198ead02-e678-7cfa-e082-0da2a9237d0e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("19cd5147-1ca2-70e2-8ef8-33ceb788c475"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("1a376031-e48b-5c6a-79c2-01e348af1cc3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("1a407980-d77d-00b9-50f6-9ddad4e3e449"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("1a9955f2-be51-7afc-9626-52c09c992beb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("1c07c5ed-49f6-154a-9758-25e8a2b63caa"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("1c37266c-c716-12eb-c9c5-a7c9c1031fb8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("1c7a6074-478b-76b5-920b-da17f5147d7c"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("1c8246c6-9ae9-dfb6-ce0b-89ff476ecf5b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("1c9f738e-13da-98ed-8735-c0af87d1bed1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("1d1f1ae5-049b-7b9d-a8db-6b57f30fe06e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("1d7d16c0-8e5c-3264-41fe-eedd38702c06"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("1da21651-8f3a-3aa3-ce70-bcc28303030c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("1e76f7c6-0594-5c28-7832-f4bc37ca9daa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("1e7ae789-44e0-cb9f-be17-5eaad290a8d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("1eaeb950-7c69-f801-20ce-03703c14aaed"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("1ed09b6b-9e2f-5689-c92d-37fa81cd429a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("1f3c480c-606c-bfb1-f604-8218c9fb63e3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("20362439-ea6e-017b-307e-766fb7088540"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("20a822a5-146b-b1ab-b849-5b19b42d053b"), false, true, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("20cd5bd9-af1d-f904-28a8-13249e3ca0b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("2104d4b3-1c47-615b-d775-d6ed6d26d6f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("2139e45b-4437-632d-a851-c87145ba4071"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("2175c1e4-246b-dc54-47cb-8607d03c2c4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("21a710c7-4273-ee01-12c4-61303f20ea47"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("21c63dbc-0985-5d45-72a0-6db78ecf2a39"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2217a7b5-f36e-855b-5b50-4f98715465b5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("224f017d-4912-db64-8cc7-19dd85240627"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("2274b082-d44b-fb16-5a0b-9e7729e9c9d9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("227f9917-a233-3837-aadf-523264527624"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("22b85f68-8b26-74c0-3b15-e3f36f7578f9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("2315442d-921d-6442-1e1b-143e5c4acfb1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("2339ab91-b159-f632-2013-3bf1f1d9bd93"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("233d6b2d-7eb7-e571-78fb-ff25933a5e48"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("239b1345-26aa-1c2a-c562-e48e090ed35a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("246095fe-bc58-8e7d-d062-fb7f4f5c1a34"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("247195b9-2d76-5233-5d2a-466fd3bca58e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("2471ce82-f75c-f7be-d738-477687b33f82"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("24f0f519-6d9d-09f4-4fcf-e468d575687b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("260b7c1e-4743-8986-2c40-ed65cbecb2b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("2692e7b2-73fd-8756-8b3d-d437d29081a9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("26aa6d9c-d96e-dc3f-6b21-0a17ff28b343"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("26c4abc1-da39-2a42-c592-3c708d29b708"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("26cd7f8a-1db4-14d9-a2d5-c813e94d4fa7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("26dbfb50-8443-c634-2459-3ba1e8429e33"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("27433977-1cb9-192e-3610-75d085355b48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("277593dc-13ae-9384-7d93-964c3d2249e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("27a09995-798f-9a09-ec9e-51bcadea8a79"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("28177576-ff93-fe25-a79f-efa99761ecdc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("2839ba98-7359-453d-7968-5e5a22aa489d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("28b621bd-b402-9bec-6717-9a957209f5b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("28bae41c-47a1-7bd1-12aa-aab213ad92cc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("2922dd56-7a8b-d61d-e3c8-a4362fe51f6b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("29257d90-af79-fe7d-82d6-160a25556b29"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("293b30ad-309e-8464-d5c3-837ed16b4c41"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("2944f487-7898-b449-64f6-0f254dc905be"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2946a6fe-8394-2003-3b33-b849e05c3fcc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("2954e3a3-1352-68bf-fdff-a61240289f93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("29dbf667-a680-7c0e-41c5-2dc90ee8db4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("29e8e885-ec5b-77d1-548e-1c3717588eec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("2a596a42-8571-37e5-3bcc-d6ca9da53341"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2a643fb6-252a-b03c-da85-d34692718ad8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("2a6b1d70-e88b-a9f8-3f68-1e4cbfdd8b67"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("2a6db5b8-5435-d8c0-22a6-88f577cec4b2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("2ab9cd5a-1606-e54f-ec1c-6dbc407d1bb2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("2ad1d150-3b2b-e715-3d30-4c3794b7fae6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("2ae27e32-6e47-856a-5d8a-c390ce208334"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("2afb089d-1f11-e510-46b9-564fbda0ee6d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("2b65dc13-1086-a99b-f478-4dd973f00f06"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2b663021-ceb8-6891-1411-78ef52c7eb8e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("2bd2493e-80ef-7ef3-f048-4f5826939ba3"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("2c05b987-114e-af45-ab8e-84ceb61f5f62"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("2c4484af-62c5-f940-0473-6eaac232a8da"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2cacae30-7781-df69-ec66-a203c3d7b4a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2cbfda68-1e6a-929b-a6e4-795789e53e71"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("2cef3812-dbb8-0935-18e8-74ce8cbab6a8"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("2cf53de7-abeb-d10b-84dc-9293a7af5ad7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2d3b700e-1aeb-d373-de93-2c2fa8a3370e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("2d5616c6-3914-444d-5b0a-4d6267c96956"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("2d9bb0d0-0f85-e269-8cb9-cea7687c742f"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2db7161b-d98c-3932-b43d-a06699323626"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("2de773df-4003-1012-79ff-8040024a1b4a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("2e76c3fd-70d4-ab42-f62f-bda8a16c88d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("2ec28c45-6e02-1965-8849-2aadbec9262a"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("2ecaed61-b739-eebc-7868-76b121d814d5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2ef7bd70-86b3-86d2-0400-3e1100f2e1ec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2efcabb0-eabb-859a-508d-ad96495f9d36"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("2f172da3-f681-1b9f-1d6c-ece0b9692e1f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("2f984f86-453b-52e8-88fd-6ccfb8ef34c7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2f9bec8e-895b-c9fb-78a0-85fa7713b999"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("2f9e7d78-ce42-7ea5-728c-87c48a3a7f91"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("2fdcd65f-cdc7-b1fb-6b9f-544b405f1990"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("3041366d-bcf6-258b-a2be-7a88cf728455"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("305d84e6-491d-a3b4-e65a-151d9d7103bf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("305efbfd-f002-5fa5-e72b-7743de8a4994"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("30ac549b-cf0e-c6ca-9838-b92ac677daee"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("30b5b613-ff4a-81cd-005f-1df54c743c77"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("30d233d8-06fa-b3ca-b27b-5ddd08860846"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("30eeedb1-7d9f-15f5-3bdd-00a5aa01ce1e"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("310dc945-7894-7776-9ab9-9071254b5c9c"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("31454649-cc19-b624-8661-3c4e342209d1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("3151527f-891a-560e-508c-26fca6b35bb4"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("319239ca-5893-0244-c8e5-2544b8e881de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("31948518-8d84-4d18-de4c-7d303b6dd21c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("31ae89bc-fe23-612c-3a1a-03341c4efde5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("31d5b574-123f-4a5b-abe0-4468da1100c5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("31f2ed64-5eca-9e6d-8b1f-7fc420dea466"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("320c9c09-5bfe-dcb2-dc7a-50f74bf98804"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("321003ea-d45d-d309-8c38-72194f7b7e2b"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("325d5475-24b6-69b3-ed22-4e7e66199841"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("32c4a61c-3146-ac2a-4f6e-bdb38740ccb0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("32e5d033-63ed-ceca-bbcb-522d43909bc7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("32e72d3e-b825-9842-a7fd-4e06bbb085ea"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("32fbab8f-8022-26f7-af56-98b45eb2cf25"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("33273c8a-2387-bde1-dc0e-86cbb56f7369"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("33482c08-bf8b-3427-9733-e3f85def2a8f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("3354553c-ad03-69a5-f0f5-282ac7a1d5a6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("335bcbec-6b9b-2035-e881-ddb219d6a889"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("337cee21-4cbc-253d-f17e-7dbf11541599"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("338f3857-ff34-b27c-d671-ad42eb33fe3d"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("343bb085-d954-5380-263b-d1d74a9d9ae6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("344fb35c-86d4-a015-745f-98dddc95a13f"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("34545002-3f06-d2bb-8275-f3fbb141a710"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("35201627-24fe-6abb-0dc9-9eeecc5e415b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("35376d76-a0b1-7ee1-b32d-1499b7e24f06"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("35794bca-a9b2-9e06-d997-a7031ebd5a24"), true, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("359200fe-9d40-55e3-52b6-1821b7438685"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("35ef158d-6125-9adc-e7ff-74704aab6f44"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("36111c95-41df-e868-a40d-4ed262ab47d7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("36494c2b-ae2e-8bbb-b6f3-f4decf561852"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("366d6ddb-79e6-6d7e-2948-ed012149ee4a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("3697228e-09e4-dbab-0d7b-58a43d2dd716"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("37079265-cf1b-157b-0c44-8fd278dc6664"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("372edd02-7a14-f3b3-72d1-d3e027fa42eb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("37812dfa-a30c-3dc5-75ad-f8297af6eda2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("37ab5b42-3b7b-e4b5-34d5-83b4d3894073"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("37d60081-868c-812b-2e66-f1f8d246fbac"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("38371df3-5a46-5137-8204-4c5391633180"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("383d8700-f680-385f-f524-33cf0e4bfb72"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("38473eb3-b92b-cbb4-4734-eee0f824ae35"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("38c70de3-dbe7-ad40-8654-1eeba4a5a9f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("390c816f-29ff-c417-ba72-e1ad9b249a3f"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("395caf08-cf73-f71e-9890-3975df1baac2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("39e6443c-1cab-ba55-d870-4b7e9c6cb059"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("3a074d30-3401-99af-48bc-f3553ae95899"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("3a20b73b-99b1-c7bf-6b99-017acd31df5c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("3a9913da-c7c7-d537-03cb-d4a75c8c33fe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3b62d952-1ca2-ae55-ed90-fe71d9d4848b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("3bd63edd-3894-fdf2-17d1-1e5b699f29bc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3be62adc-bee6-6ae9-55d1-ae4209ae72ee"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("3c1b91c1-9093-2729-ce6f-de1903123924"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("3c6fd9cc-7314-1ffb-f4ac-1d57ab3f4aef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("3c922e27-af12-ba97-886e-16e89297a956"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("3cd2fba4-acd2-a1f4-1891-7745cfb42380"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("3d2da141-203b-40ae-0a0c-d243f36348ce"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("3d7afcc0-41fa-e29f-27de-f593772064e3"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("3d97d051-a73c-2db6-64b9-7a5ed1c267a6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("3db27053-76d7-050d-bbd7-96ea49d32e93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("3db66f31-7496-a50d-fd09-189c6d86a635"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("3dc5b2af-04c1-eb29-b15e-ecbff0f0fc4f"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("3dc74640-7587-f7cb-87bf-847feeb760a2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("3ddc0294-a7ae-c93c-394f-0579b64e7f21"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3e52faf6-03b3-0e58-ba25-4ad63d4f92ee"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("3eb8ca06-7f3a-2338-51b9-d93f2e710a8b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("3ebe2992-be24-27df-0bc0-dd0c85e53636"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3ed2e32a-1764-9ca4-3b76-61010dfeb3c2"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("3eda507a-cf53-2e01-0950-a7a65946108b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3f47dc2b-9f9a-68e1-3bcf-fb4fc442f638"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("3f6e1541-1464-e68b-3e0b-5b444ad1f72b"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("3f77987f-e1a2-00f1-ae12-c97da302650c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("3fba85da-c214-4d5e-13ee-5a3b66f8c741"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("405252c6-2a82-3522-3c7e-d65f7deae4db"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("406e39ee-9bf0-72c3-d671-75a37a6c6816"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("406f8c4d-3b9e-1e3d-780b-d1a27edfc5e6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("4091008e-5890-810d-f307-9b419f743026"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("41496500-2f79-9184-b5d3-18f7246eed85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("415daeac-f621-1206-ee3c-b9b43aff6984"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("41a7df76-f655-3412-5f37-2cd417f98c82"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("41f7a71a-3efb-c4d3-55bb-c8a9508860d2"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("42243842-8c8c-7642-37ca-9ed5ee13225e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("422a028d-22c5-19ee-b1df-fdd47b65b20b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("4235c07c-e564-bbf7-e475-eda92f8f8a15"), true, true, false, false, true, true, true, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("426a0e3f-b280-25e0-b076-03e7c1a88d96"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("42a15e72-3c0e-67f1-746d-5ee534d9c502"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("42c3d133-02c4-f84e-ccfc-9369445b0a7b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("42d7c80f-feaf-f785-9890-798d4a402c04"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("42e2a253-d767-6191-caf9-e1f79652c44f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("434b0f73-2414-966c-9085-793eb852a0f9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("43813eec-3db0-3729-c81a-8daadc59f173"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("43b02d72-3431-0a4e-e865-bb1a9e886416"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("43ddaf52-98da-5578-2011-7757d6812123"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("4446230b-493d-9f12-ce7c-71b2add3e74e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"), true, true, true, true, true, false, true, true, false, true, true, true, true, true, true, true, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4527d01a-d157-1d9f-fc8e-77bc2e2fdd00"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("4548ad6b-0d20-1b5b-3808-196248fdf7d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4629b82a-981b-9796-f3df-3a8dbc0de44e"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("4642b120-04d3-2ddf-51ff-bc6bcf260f07"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("466753a8-43f4-6c6c-3f8f-ab11d750a794"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("467972a2-cd85-1f32-6e68-f41409e32d91"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("469c689f-9d22-6a95-b27f-107487beccbf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("46d9cc79-47ab-8e6a-83bd-2d375b16131d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("4764ff10-a265-f4fc-9deb-b316154b1cb2"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("478a8966-7033-50b4-591e-18765aa441bb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("479c596f-8198-41ca-34f4-e066cc121cd2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("47a3ddf7-452f-a656-1973-de76128f4bab"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("47e11a52-579e-038e-8fd3-f4c25dd72bb5"), true, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("487274b8-da1f-82e0-5fd6-c6dac5a61f57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("48a389f6-16b0-f540-72c6-e20ba1d40a64"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("48cac800-a59d-0e09-6041-87174034c019"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("48e32245-7534-2f20-96ba-2a31a31dab25"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("49980728-f2c1-9f56-0ad9-b36c5a889719"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("49ebb447-9fa7-38eb-aa2e-b97617549c12"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("4a045df1-dc0e-e920-6a8e-02afdc1f9f37"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("4a323803-1aca-a52a-1836-8b15ee90d398"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("4a73d4d8-7ab1-2945-568f-9cb8aeeaed82"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("4aa10823-0fcf-6599-c0b8-f9ec405ba7ae"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4aa6e836-8bd0-8f15-8002-df67c2d95511"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("4b80730b-2715-d60f-7065-f746030638a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("4bd8f56a-bbe0-27e1-003d-f255a6532758"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("4be63323-a734-943b-8d03-b7d80fd58683"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("4ccfd0e0-caf1-0b35-a4e9-b4c610d1d518"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("4d26d81d-eebb-e354-305c-20c3de67eaba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4d2ada08-b246-dda1-ff81-dbaac36cb406"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("4d2c248c-25fb-392e-6f5a-60208b0a6e48"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4d6fc2ba-2e1b-bdd4-d132-c87b77102ccb"), false, true, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4de91ccb-ee76-da93-f6b6-3fc772dfff78"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("4debe1f5-0d5e-90c7-935e-684a0484d7ed"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4df35c89-1203-a270-b546-40696fe301f1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("4e0257d1-8365-9663-2bc8-106f80ac988d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("4e10adcf-6248-dceb-cb7c-5ce74abcec69"), true, true, false, false, true, true, true, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4e77844a-0f09-6009-86cf-eec6c8bbcc42"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("4ebbd962-8401-cb16-ce3e-40b63680780f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("4f80fa3e-f5a1-a206-f045-b159f38e7829"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("4fb31d9e-c277-7477-89a0-ae6c49db999f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4fddc34f-732f-2e7a-34cd-52509bab4617"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("4fe26f18-ef4e-36ec-e635-a7d7720a6660"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("5003bcd7-f0ec-79ce-a83a-1798a51795cc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("5018280b-0d63-2061-42af-459b8ab01588"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("50419c41-f6ec-5073-27a9-eaf624598b7b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("5086a30f-82f9-c2b2-60e3-57ffc2de96c6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("50b0e7ac-07c0-50cb-baef-759e3cdcbbe1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("50d1f5c4-02f5-0771-9ce8-0b1307616b2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("50f67d75-9555-756d-5fc5-fc92a88da34c"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("5128619a-d31e-87ca-478a-b50c6791df90"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("5157b67b-7a2f-4887-4093-de4bd6cc8e2d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("5163ce51-bb0d-d8a2-6ca3-b515a19e8df2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("5173d448-e9cd-0d68-c79f-7f1e0ba4fd9c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("517c3c10-2971-66d1-6a18-38dfda4d4d5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("51d1c1f7-16ab-d29d-dd50-2f6f33aa3073"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("51dab92d-f7d5-d870-a13c-ca7c37f498e6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("523cc95a-3d9f-9b75-9d87-7df0a1b34253"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("53229a36-fc1e-0ae2-cbb4-fc35ebcbb195"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("535fb558-59b4-c3a8-01bb-503c161a7505"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("53c18ca9-f7ae-a720-5854-ef47c72ff7c4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("53e325a6-d795-1a83-eeb8-34c6bcec636f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("543cae26-dbd2-59d8-b366-3a8f1aeddc20"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("5473b883-7efa-504e-2b94-3046e3c3e53a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("5530f0f0-965e-0e93-9b2d-631ae75660bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("553a4767-4479-5fd3-9b6a-8606fb8c12f3"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("55d18c37-aec1-33b9-bf7d-ccd4f2523552"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("5604f2d6-ed5c-69d0-06f2-59e976f3cf30"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("56088d7b-2c62-f188-7d63-34c07caaea0d"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("5667e068-872b-cf4e-a06c-584508676d3a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("56c49867-b079-a83b-38c8-b170f4ee32cf"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("57064a0d-0927-ee99-2332-c8fd07790e73"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("57484dbf-b071-772a-239b-2d6d99f176dc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("5794f740-90b1-5a70-413a-d59bbc97ce78"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("57b0c075-0523-d6f6-b63e-0b114bc49400"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("57b57863-6a78-462d-d8b2-78ac4b834960"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("57b9b346-1649-2e3a-9380-92ac4a170646"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("57cc58ff-43e9-e4c7-8dac-7b113001bb66"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("57f23a76-a3ae-d1f0-727d-023ec2d3405c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("580a98f6-3f04-60c8-b75b-78f7fa7f6cd1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("5858bc94-2848-a8f6-450e-61b2978415f3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("58941021-2c3e-dd2f-ecc9-5eb5449171c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("58acf7bb-a03c-9d8e-6d34-ecc6556175d2"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("5932721b-3fc7-7394-05bf-0f3d85ffe6aa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("59b0049f-79d6-e5ad-ff4b-9e9f381680dd"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("59b8da95-79b2-0432-41fa-9050269d9d1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5a414697-e5fc-0555-677e-ea21efcf7bb6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("5a712165-7d3b-799d-2f89-f59245caff4c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("5ac4660f-f3b0-d49c-1c95-c955d9618645"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("5c5deb8d-b053-8d30-6f27-307f31576ea0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("5c6fbf58-cf3a-3b9e-fc41-5bf8ff8a25bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("5cd81a6a-8e63-3b49-4128-101994edfd04"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("5ceb4c02-d702-580c-00ca-75404dada0f7"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5d19be17-57d3-0652-d98b-5a11f62faf19"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("5d5ca2ed-f113-4ce6-c77a-8955d3db135c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("5df893f5-2499-20e9-1666-29f0a9f88b96"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("5e27c98e-607c-fed4-b43e-e25e948d485f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("5e7aa9cb-9fc5-d195-8baf-98f3b809a8b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("5e835d64-eedf-47ea-747c-bd6f50092619"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("5eb29b02-1be6-f40e-39c8-d8bcadb1c47f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("5ef477ae-5a0a-bb20-f18a-316ac7ade64d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("5fd287af-f845-0376-0a39-6cfa61d58cf2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("5fea3ee3-8203-6e8c-8252-3886526f5d80"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("61063a45-9de0-6ada-716f-b308ab881c76"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("61205d41-2d0f-7d63-9277-22f655f23023"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("61273037-8a65-61f3-387a-b4ae8d854662"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("6164a7ec-aadb-0213-7bae-5a1d8178422b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("616d1a16-c056-3b49-b92c-74d382827474"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("618bfa4d-faca-74af-b7c6-5591fef965b2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("61ec7fe6-0d34-a48b-5c41-787c818b387b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("6201ed4c-5f4c-e0db-2668-7addc500f9a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("62056203-8c08-c7b2-152e-b327a8f46bea"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("625a6c21-32f6-45b9-911c-fef812d43657"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("628b3ade-f181-f0c6-82ae-f2d244043090"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("638fb6d9-200c-a3b0-9317-cd900579cbb2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("63a78e76-6f15-0b55-a31f-418672cdf720"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("63adf607-2d6d-72a5-ff7f-e856de6aab11"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("64121877-340c-1bdc-325b-d3c412332b65"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("644c6e86-09ef-761c-dbd0-ad51a2f836f3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("646ad30b-1811-3855-e608-cafca7c51a07"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("64a8087e-3887-cbeb-90ee-2cb95c7909b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("64cf91c9-25bc-cfde-1954-9d8ab7f291f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("653654f5-c2ae-45b3-ecb4-507add141ea8"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("659eca8f-f21e-daa0-cfb1-97f3f2e43e6c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("65ac8a90-8d09-c31b-1285-cf09b38f6c6f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("65ed132e-fc60-0269-7271-5b7a07c31ca2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("662a625c-2eec-fe6b-6fdf-f532f9296bb4"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("66b11bcd-7c1d-d295-cbb8-ada867cb94f4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("676f980f-4d6f-e435-aef6-0c87dae4e732"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("6787d9ce-3640-2bae-4260-af3cbed8b782"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("679be5fe-4b9a-cca7-b8bf-59917f04c9e0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("67b3c775-07be-ea99-6655-a657dcaf45a5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("67bf1f4b-87b1-90cd-15c8-409205e9e68f"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("680f7358-4b7c-0733-be42-f9d52e746d1b"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("688fee8c-dde0-dbe9-b0d8-7750152d37b9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("68b94637-ea35-95bb-731a-68ab0d83b6f5"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("68e9ad66-3620-3330-e529-e8d686874e1a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("6987c49f-5d17-db47-280b-8298904ad323"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("69a7a4a4-8e92-d2e9-1b2f-c213766de3cc"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("6a4e890b-7586-b5b5-1a17-1e2c8ce592fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("6a5e4f85-3fb4-2abb-06c6-400f9eb2d1a7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("6a9c8ad8-b367-1a61-2588-a94b68bf2b52"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("6a9dda69-8fb3-850f-53e1-3e7b8855e0ff"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("6ae6814b-2691-8ca4-9fcb-a280f5a0abaa"), false, true, true, false, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("6afce85d-da43-b61c-2824-b162c873c663"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("6bdc4ba3-dc86-4023-164b-8530fa624738"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("6bf2f44f-fad0-ea64-6c79-769b069683e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("6c0b5039-c28f-eca9-bd5c-e5d22ea7e4f0"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("6c4013cc-9b70-4bab-c658-efd52f1534d1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("6cf58043-56d1-1766-ebb3-ce7a8dc63e06"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("6dd34819-c7e2-144e-b68c-8856ee32b294"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("6e8b516d-d134-51d7-6603-00ee4641d201"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("6e8f5f0e-278a-7be7-e19e-ec988f624ce5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("6ea81887-4081-0f2a-6c6e-575eeb829d02"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("6ecea443-25c0-ccd6-067d-c53b9cb5369b"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("6ed32a35-9080-f312-ade8-0e69bd7103b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("6eec19ec-3377-42ce-3428-2faec9cfc5f0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("6efbd431-67ef-7c73-749a-505b6e548bef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("6f53141a-cb72-c5b3-8038-b748a76ae530"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("6f876ecb-f37c-9c97-c30e-fe992cf56d10"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("6fda8dd2-0c61-5790-b407-7afdcacd8285"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("6ffe2a73-a5c4-8ac6-75ad-4f1caef90079"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("7047e7ed-4bf6-ff5d-a169-15c158798b53"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("7051007b-907a-2932-86e1-c51029df6df8"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("70b4e227-f7b7-5634-7488-9806821837fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("70ee3e0b-e1a8-a0db-5469-a217db6d2bcb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("7170ddae-3c8f-154e-5e20-e51f8d572074"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("71ba55c7-5376-b477-82b0-6738e974588d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("71e39728-bb8d-2e8b-2a3c-4fd77e4a0a47"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("71fba4e5-513d-78af-09e2-352fb4c8be7e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("727c6e92-30c8-ad23-6f5e-9714d4342f0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("72958664-8652-a076-774f-448a29ce3132"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("73b035d4-6148-2a05-72ce-a6a8d8e78238"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("73e837a0-5fa2-0b32-34af-b35fc6965ce3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("740f4cb3-9687-8846-9429-b8028f3fe929"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("74453ca1-b8f7-4cf7-f037-be0f7f9a28a4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("744ea3db-88eb-9785-159e-5451b60d5867"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("7452f73a-e4ba-d894-b9d4-8662bebdff2c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("7492c61c-6d29-ca2a-0f2c-e7fe98b66bc0"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("74ccb801-117c-54ac-69e9-8a85ef6c26bd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("750ac23a-5f8c-c25e-20d6-a6833c11feb1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("7529a244-b0c1-d54c-0927-20ee8cd5103f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("7579fd23-0432-fad2-27a7-68cf4b4de9d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("758f5f0e-4e31-3f07-a0e3-95efb66d4bce"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("7593c831-9460-87ee-883e-a7d08024d65b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("75b705db-ab96-19a0-0fc7-1f5ec2ada945"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("7694cc1d-0abd-005a-7c0b-b89fbcc158b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("76a58254-1768-5ebb-92fb-9158aa5b74f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("76a964de-f10c-d288-3c77-53a491fefbfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("77c824f5-4b8c-67d8-7512-c8be21d7e4e8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("77fb98ba-b0e2-ef2c-2b9f-d49a83d0b44b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("7802b358-eab1-01d0-24b6-2ea8f479222a"), false, false, false, false, true, false, true, false, false, true, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("78089eb5-2ba7-8728-d4a1-773e35d4bbd2"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("7837f34e-c923-a137-2d29-c3bcec1b633a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("783eb3b3-cfec-491c-1554-ddd6d4c913b3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("78ce88c3-a409-5f0a-7979-17a0f8b041b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("7903d6b2-ebd5-6618-4419-00a95e4038fc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("792cbc80-2b16-72eb-d980-7d4e174fee04"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("7963abca-474e-b640-c9b1-50a5bfedc78a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("799bd43d-d80a-0eb5-777d-6ba1afc0717b"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("79b9c190-d7f3-44ad-64d9-39ddf14241d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("79fd5580-dd3e-1acb-f533-46ce79e2b7e9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("7a015dd6-122d-7189-9adf-bbaf3368ddcf"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("7a0d54be-e71c-e5b1-a734-fc55346672ac"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("7a38257f-236b-f091-cc2d-6a07d4b3b30d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("7a4fdd8f-31f7-74c4-f2c1-2b809ea6b560"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("7a7b5c71-785d-ba9b-202b-70bef66186a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("7a7eb399-24f8-6d48-4de1-a7b5b0e39aad"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("7ab64f73-3a84-bcde-1d48-46e0f48e5445"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7ab6b792-c208-a589-d1b6-e53cae958501"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("7acd0e7e-d083-54ab-e37c-57d9c62551e8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("7ad9e9a7-de8a-a095-1eff-e23ed13ed6d7"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("7c31a078-ed4f-934c-4de3-ee871afb8a93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7c69b181-a5c5-1b76-da6e-35406a69aae1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("7c977b1a-efa8-9f8e-ea4c-b9f5f49cd501"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("7cc56aa3-23fa-11ae-46d1-623948321e63"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("7cd2f17b-5d8b-6487-e05f-e64b8479281d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("7cec2010-8e12-c7c2-c2b2-fc128289aa87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("7d2fd80d-69aa-38a2-a4cd-5485445c1c57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("7d3f5f56-f674-1f72-68b0-3a55813f8dfc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("7dfd413b-b85a-e5c3-5f43-c5d5034a325c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7e2b6cda-fef5-6891-4b5a-9dda984ea76c"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("7e59287c-c2e8-c960-55d3-d79c7a4d5744"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("7eac574a-4169-9263-2370-294c82b9bdda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7f01070e-be22-9f0f-756f-18bf6b956c33"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("7f0635e0-2361-0a15-a3d9-1d2ab6966569"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7f52e39c-fb55-8e9b-3865-d29bffaee942"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("7fa66608-1650-7481-0d97-33b93ff14201"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("7fc64329-98c7-f899-6921-cd698f033900"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("8050cab6-1a94-4b5a-e70f-472814d20b29"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("80512a06-6272-dca2-3240-4c0613c289b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("806fbb91-1a9c-6ead-f554-0f4067475507"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("80783f7b-ad52-b148-b256-b3210e0cdce6"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8078e022-f68b-0de9-0827-bb2c0e717988"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("8089c59d-2aca-4bf5-c271-111945e8a3a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("816472b6-e790-ba36-9ce5-2b570bc74c71"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("822a0e7d-4b77-a6f5-bd9b-2117b6675e7e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("824f4ce6-98b4-6f74-4d3f-3f5df926c4c0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("82bcbd59-3d5a-f356-e900-e1fc8f4e69e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("830fe0f0-9c17-f2b9-a0b2-12b9e44b84d6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("834731e2-dc9b-4d19-3c60-f9a87a2277c9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("846803c7-d263-6baf-82e0-63a56d603dc7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("846b5f6a-608d-fbaf-97ef-ab11d5ceaa0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("84788d82-b9a6-a84d-6607-a741309c0667"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("848a2cbb-8de6-9e6a-1ac2-17cfea210829"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("84b35820-207b-51a5-bf29-205365672b1d"), false, true, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("84f7ba2d-12b9-e26e-8114-f068e9228b85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("84f9cf6d-beeb-15c5-bf9d-c755a86cf430"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("856a3274-c78c-1d6b-015d-03632d335780"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("85937b3f-f489-d8c8-40bb-5aee61844a4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("85b6bc45-c13c-da25-3198-37ee0d504701"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("85db0fab-805f-2b78-b892-1bcb767dc36b"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("8626e388-a399-ab33-a557-df27a097aa40"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("86346fed-f259-e15c-771a-61cb0b5e6188"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("863e4f26-ac98-0c5e-546b-449eccb3845e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("8642768b-72de-85b7-3700-52f204bc2412"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("8664414e-4844-5e77-4bab-51570ae83b8f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("869467c3-bd8a-a82a-ea6d-f9b19db5bab8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("86bafef0-c08d-5821-7115-4e894d64898b"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("86e3a97a-e881-d39b-73eb-ca20c5269ad9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("87dd8ec8-4f5d-c0a3-8327-6cf6a4ad33bd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("88352e79-c410-91c1-e012-de38870124d3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("887cf291-a252-5fca-089a-d530ae89931d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("88950e1f-8468-de1a-04d9-9132b4f50fec"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("8899102b-18ab-fa68-2e81-a99dc2b34fab"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("88ea25c7-3475-b940-a8fc-b72a8c33cc56"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("88fe402d-de4f-37cb-8d43-b187d534e57d"), false, false, true, false, true, false, true, false, false, true, false, false, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("89154351-336b-9edd-3afc-a59cda8ec176"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("893e4f6a-c815-bf16-68b0-10ab980658ba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("895d196a-bd9f-772e-b1d7-bf1d6597f8fd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("8980ce4d-9776-2f90-aa8a-ccd9d5d8b3c4"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("899d4333-60f8-7e14-4ee3-52edb1baa52c"), false, false, true, false, true, false, true, false, false, false, false, false, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("89e94086-5957-5132-3379-39eb9cc0ce13"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("8a6baa4e-990e-de85-d729-3128dbb4b0a9"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8aaeb6eb-fbde-56da-8033-35821404722f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("8af45e13-d08f-c109-023f-c358663e71b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("8b0726b3-eab8-a66f-8b89-49aabad070b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("8b4e2f42-1331-b78e-de39-5995a0d30f36"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("8b5fae41-94e8-1ab2-0904-91120573f0b7"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8b6ab424-c665-7869-a989-ef30c5e0da59"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("8b808c74-c3d4-b7f4-c8ed-dcac47d15424"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("8b925bc6-c4df-58c7-b80a-91cfc1f9eb57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("8c051193-f470-e36d-2672-d11f7a2b0219"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("8c1a07d8-dc4c-c472-6bf5-1df7bf7dbc0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("8c6fbd52-fd6c-4139-054a-d4849982957a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("8c889da2-6c92-cf0e-845c-2040cfe9ea0e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8c9dff2e-5ed3-13c3-668b-b11f7602e9d8"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8caf812d-02db-7b2b-41d0-ce8732edbdaa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("8cba2d0f-9d5a-4fde-40f8-bab9f24711ca"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("8ce78826-3814-28ba-6dc2-9b29134d2f16"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("8d1325d9-fab1-0ca4-e18b-910b17c9c6e9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("8d45be3c-fc01-9cc2-8015-891e6b6d1080"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("8d4d9e66-7bbf-236f-f63f-11c9b4647383"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("8db2124b-a20d-c86d-0c55-44f6a9b83dcb"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8e22a8ae-26e4-cc03-65e7-633b6a517175"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("8e22ffbb-d684-6090-7e6e-3605773f71a0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("8eb4e3ac-e6d2-740b-7eb8-0e62a2565e44"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("8ec664b5-5209-fe92-34b1-bf0807a69603"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("8f6a81af-626b-1fe2-7bfd-6a65e20597f8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("8f743dfe-db9b-cfa3-2aad-27ec4235b35d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("8f8d56ef-2e89-5fce-08eb-82efc9656cda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("8fe72ca6-8048-a5ab-89fb-ccce984f22f4"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("8fec4773-1c6b-0cae-e623-f394e71f3901"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("904a9582-0819-5d89-25b1-286508c3d02c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("905a2158-fcef-c876-e660-4bc3edcd70b0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("90745c7b-ea98-a2b4-0791-01644be47a2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("90ab054b-00bb-d9e5-1d88-73f5fcd8c548"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("90b24916-a7da-926c-85db-d40df0bb5cb5"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("90fd2936-b4b0-5b9a-01d4-1fa6d4f5f6a2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("9116b47c-8dce-d6cf-e4b4-da5a96a357e2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("9173e973-5956-7659-a264-980ad79264dd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("918d0634-ff61-c756-98f4-a17290d04110"), false, false, false, false, true, false, true, false, false, true, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("91cee2f6-39f9-75e6-8955-27e8fe3399cd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("9238885e-5891-4759-3a2e-d11a14bf4216"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("926cd756-8fb0-2ccb-fb32-13ea6525870a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("927a433e-d64c-2338-422a-caebdefa33dc"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("933f8f5a-9ea0-f90f-ca17-4ea9effe4ea3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("93533f88-5b3c-f8ba-a6dd-3beb60fa3339"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("93836533-63cc-35e5-be22-2cb8894b7454"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("93b17b74-0dc2-054f-cdb0-ec9469eaf98c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("93bb9d5f-2437-7632-81a0-10c147c3ab6b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("93cdf397-66b9-7b69-9567-522ae6d132b3"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("941272bd-659e-dddd-0643-367822a5530b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("9463a4c9-beaf-a4bd-4280-21d7f30a0411"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("95823336-4ed8-1c15-e280-0dcf8334035a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("95bb74fc-40e0-7f2d-e951-fb217f2a82ad"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("963f18fc-c8c5-66fb-a59a-f250236ed752"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("966573f4-19e6-4fee-aa26-ac9239cbe9ff"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("966f6af1-e43e-42a8-ce08-728b7d0ab91d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("96a5e312-7794-b575-d6c0-e170f98d5f43"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("96c0cebe-6b54-d437-c401-1c05b2dd103b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("96c24743-97ca-fa9b-8204-db4eb256bfb1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("97303be4-30aa-2f7a-5671-93dea675bfe2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("973f3950-0ae0-1df9-ad6f-570f6cd38b89"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("97eb4bbf-2fea-a75a-5226-bbfd8aa0d667"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("985007e6-6494-dd02-0f9c-cdab27a30d4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("99031e5c-a87c-ebe0-0d04-46399309434c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("99108e9c-0f3f-1f43-010a-b7ced737ef32"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("99230bc2-6f8e-6513-4b7a-d6424d3cf345"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("996887d3-395b-41d3-d284-faea17cc8617"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("997e03c9-200f-848d-95a5-07a8184fa888"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("99916786-6eca-5a2b-15f2-2d99abdc60db"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("9a4a2183-07c1-dc79-c13a-5e7a697c540e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("9a6279a1-74b1-36e3-e6d8-e6da88cfebf8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9ac00023-06a3-d6d1-8a83-74c5da3c7fea"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("9ac4268c-afa2-1bbe-9e53-6b9da81b06b3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("9acc891b-a181-b02c-5324-4e3e461e3912"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("9b233dda-aab8-6d62-224e-fd7aef39c60c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("9b4f26db-a193-5cb2-36fe-cc0398a1f7a5"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("9caedf98-3378-10a6-0485-8fd863db1f98"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("9cbd157f-60ff-5944-c775-cf75d19df967"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("9cdc572d-cb76-7ae4-9a53-eb357a0fb02d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9d5fe111-8566-dce8-2860-8ac0310dea08"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("9d745645-4715-debc-7899-f8f307dea12e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("9df59a47-31be-11b2-62a0-31ef27c3dee8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("9e31d353-1832-55c2-cac9-5d4c59b737fc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("9e371f64-5812-c791-9e35-41c95554f945"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("9e86b1dd-a7fd-0145-0677-5512f67f1218"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("9ec1412c-af4c-02e3-ac2f-45cfa9149c67"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("9ec4143b-df86-095c-cc5b-3d08e99395f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("9efb66f9-223c-3547-03f4-db2430ee631c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("9f872e58-e23b-b66e-35a1-3c72d1e69bb7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("9fe26a48-5164-292c-c077-56c475a90799"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("a0134759-e6fc-c8f9-c2a3-a6be519c09b2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("a03ff5b1-354f-99a3-03a8-e967477fbe4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a061307c-2f24-842e-8c0e-0d9b9daeffcb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("a08d7c05-387e-568a-d8e0-e68bc715a01c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("a0a1fa8d-81f1-ac55-52e7-faed6fe0b611"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("a0b4a6f5-546e-c0da-f65c-37ef5cea452f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a0c69eec-9aee-18eb-24bd-cc9b3daa8085"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a103fb11-2d03-59f1-2d58-c823ade55568"), false, true, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a1153b99-f614-049c-8f62-2e6672c1163d"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a11d18a5-b282-4ea6-a8cd-966cfff5d966"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a12caa38-5452-cebf-5393-a0c34815de08"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("a134d8a3-0c2c-1c2a-81c0-87fb5871f301"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("a146ffc4-f735-32f7-f26e-58e1811fdba9"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a1abdcbf-fa38-d16e-03e8-9cf53318f41d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("a205685c-e1da-6d63-fcc2-4e465c002638"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a215b1a2-f495-9021-2fd6-82fd54a31700"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("a2689417-50c9-da49-5777-a90679631d48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("a2a7ccfd-40d7-23ec-1639-1c56a75e65bd"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("a2f45cb4-93a1-b607-3085-0c8d6452e7b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("a3a8df3c-3b0a-7101-526a-fdb4d732dcba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a3c118c5-33e2-98c4-a904-8a7cf3a5a7ad"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a41ce315-d082-63f6-b0cb-5cde4bd4fe03"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a4358d5a-cf30-c2c8-5f9b-2cc0e7587c5e"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a48ba706-ddd8-7c00-c475-2af8e71c05a9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("a49bee3f-605f-f293-bba8-d203226f43e5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("a500f606-1326-47ca-dc72-1d576a511c24"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("a523ae85-9c0b-bd09-6229-fd088dc6b093"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("a53e363e-3653-2268-f61f-7525e3efbb5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a585f941-c6a3-cc7c-0f6d-3700f585eb09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("a5b8b6a8-e6f6-d296-a051-97e91756a93a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("a6872f66-f497-a913-97a0-db9acaea6280"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("a6972069-e73c-ba6f-85ec-3e633fba2c3c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a6a71795-7c1c-f251-d780-881a223728c7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("a6d2f300-6a1e-1b7d-75b1-14a45c421417"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a7f0df0c-acbd-17c4-e155-2d6edde94407"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a7f2c4cb-37c6-6a11-0e81-01f72d96b9f6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("a83d03e3-12f2-a974-3ed7-6a30b3417b0e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a8586e94-7271-dc6c-3b5e-b9ca6fa73fda"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("a868cbbe-5698-fb7b-78b7-491135a21161"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("a889a40b-3203-30d6-c2bf-816248b0d25a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("a88e7db3-69d5-c5b9-11d4-bb8d5a64a242"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a8dbe752-78d7-fc8d-38b7-3661c16754ac"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a915b601-7adc-9bdc-9f57-38f51929fd64"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a93ccabc-4794-c2f6-3d37-19884ccb3dc9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a95c1e81-50e3-7611-596e-138988ff96bc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("a9c208a9-da86-9948-f87a-205b717e7b44"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("a9d6e145-ea26-2b7f-1844-90682dffd78f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a9e03f47-cc6e-9cf1-c48c-6b2a8f14b3e6"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a9ece6d5-4ec4-11c8-a213-9866de879500"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("aa301d6f-e785-0359-78ba-c5929c129bbc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("aa3d4ca4-8a8f-a580-9f26-a8fbccf8d21a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("aa7c3f6e-ef42-0c7f-954c-07b3c457ed87"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("aaabdea5-7257-0d14-d3bb-915b6f38e613"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("aade5825-905f-a69d-b974-ef2fde1452de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ab05c07c-6629-6863-148b-b7028dd5521f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("ab3976ac-00d8-41a4-dea2-0ab6fbe4d665"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ab45313d-9bf4-7077-cf2f-1705713378ea"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("ab64a087-98fd-ce58-c891-b39b05194ced"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("ab6f475a-7d39-f24c-e5c7-be059b872fb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ab9191f2-f567-f45d-5abd-07ffcaa475fe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("abb41a21-274b-cc54-b107-5ff6c3fed133"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("abe1f298-763d-4a72-0ef9-1d4768d0b868"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("ac431528-72d7-fda3-b418-59a0d36b0f43"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ac9d4a3e-5653-900f-bb7f-25e3a77ec854"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("acd3edac-34a5-b17d-13c6-91b098ce03bb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("ad74412e-c99a-6021-70ea-015ea7e30a1a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ad935742-0a03-120f-58a7-8fa3f25ef45b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("adcdd502-eb96-2505-71c9-bae79f2ec76f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("adebecc8-7c41-3e42-94f6-c1d9f97c1863"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("ae0a6f84-4a8c-4a0a-2064-901d455061dc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("ae1c2833-99c6-0573-538f-548f63f9dd40"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("ae777567-07a5-5e98-147b-de7208e72904"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("aea2e8a1-18a6-72d2-a954-6f5513b80eeb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("aea3c8f9-28b6-c972-2018-ef06520902c9"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("aed2bfcd-4d44-f086-7fd6-9f9f2b15f48b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("af1b71b7-5b28-71ff-0b8e-f80f1c625a1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("af3953db-ab3e-0163-1923-86b3d5de15ce"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("af72be34-73a1-99e0-cccf-9c9c53afdbda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("af74dec7-de0a-5011-9457-ad44d4dbda2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("af844032-41d3-fa9f-e2b1-dc574dcb5ebb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("af9aa99b-c0ab-7c3f-55ab-170c96fdcbb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("afc91ac3-6ddb-e930-00d0-3fee04d9b282"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("afe97513-5c8d-7b5c-84ff-040359ef958e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("affa057c-1b27-237f-be02-ade42d92c483"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("affb3208-ab19-26dd-2b60-562c5e5cfd27"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("b0a5b838-c938-5c82-0711-952129055538"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b0d1dd92-26a2-cda9-46b6-ae3a1d485e7a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b101b956-391d-3a36-a26f-deb25d940c27"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("b1302fd0-129b-62d1-3006-293ac6bf6a87"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("b161cf4a-3880-8da3-7f1a-e3e023e7beb6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("b18632d4-ce08-7cf1-80c8-f1ddd53ad048"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("b1d21a8c-1e82-9322-9373-ba5caef23929"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("b1f19258-42d5-d3d2-b7a9-ec2070e21292"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("b247f1ec-c003-863a-2cd2-ff7d8ad3b099"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("b272dde4-4d09-cbab-f515-58c2d33cbb0e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b2a9d65b-f747-d20d-0d7e-45572b648bc9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("b2cfa60a-5fc8-d083-5d04-5a01d70cbc02"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("b2d7a8e6-9f9e-f711-86bd-7568fc36b2e4"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("b3d794dc-643e-be3a-988a-860bc10d9876"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b3daa5c8-10a7-5ccb-5352-930c50bf4cfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b4001b8f-7ec3-8fc5-771e-eabd8bf11f5d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("b422385b-18f6-d391-c3de-19d2b46a0623"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b4d08c0f-ce35-c067-b48c-1921a6e439a4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("b4db806a-5e1d-d2aa-979c-d8d19a5792b5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("b51251a4-13f4-53ed-799f-06f3fe8fc0a3"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b539c16a-c83e-75f7-9212-9d8b32bb287e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("b54c3509-d862-535d-0c12-3a2b414529f4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("b5a0a82a-8bf9-abdb-c594-8185fa6de8d2"), true, false, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("b5b8ecad-3314-d7f6-3ae5-fd4b0fe8e835"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b625b6d1-eccc-5fd0-a983-22a6259994cd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("b74b64b2-b794-9c7e-3735-62602c9ab52a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("b77d61a1-a408-7d53-fbfa-7980154414d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("b8caf4b8-d200-7f93-d0de-034135a14a55"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b8dd4f0e-d18a-95a3-5a0f-4f4bcbc106fb"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b98d0310-833d-757d-89d9-92393b4288e5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("b9a9b404-28bd-f797-cd5b-8e8fd84a8b0a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("b9bbc143-2b09-6d89-ddc1-2a04a32f7730"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("b9ffb4de-d78e-866f-735b-2a41baf4ee15"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("ba12232c-04c4-dd19-162f-5847aa40064e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ba28500b-fd12-606f-725c-5a51f7e02b83"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("ba39daea-cc90-39f3-4a49-681faecfc257"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("ba48d59e-57f7-5826-37a1-fd1c57dc602e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("bab5cb53-156d-c331-b998-3c8e6d83268b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("bace752c-586d-a395-8fa7-572166a4065b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("baff3f8c-6e8c-e814-86d6-9431df1251d1"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("bb264540-e621-baa8-e366-53d5226521fa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("bb59b2f8-0de6-fa7d-ec97-9a3346ebb6dd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("bb857600-32b5-7229-f603-29dc829a4f3e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("bc1ba3aa-0266-e40d-e923-93f9556f811b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("bc2cd481-43cf-34b2-bb18-556cc4610e77"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("bc40be65-ac6a-ecb8-d457-9c902e4a0eae"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("bc59d5c2-7918-f3dc-a7ba-fb1e3c7310db"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("bc9c23e8-3467-5c09-0b09-4fa378a603c8"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("bc9e6d05-dd39-a901-e0d2-de52da908a5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("bcbf203f-2abd-e2e3-548e-f31e72f266a6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("bd32d05f-02cc-fa5c-772c-b820a7e682ab"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("bd4fa4d4-57b6-6b58-3f9d-f5e17b47865e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("bdaa538a-c42b-bef9-ca05-998f045ea6c0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("bdf6a64a-0df0-2785-9f0f-1177de1d50a6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("bdfdf9dd-3718-cd00-0c8b-7fd51a40a37d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("beef6121-7e65-1fce-e2f3-8f62fe0bb8c5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("bf01b0ed-7161-1a84-feba-a460b92acc03"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("bf084386-2e2b-1be9-0594-da89a7b8b2c2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("bf1dec49-39af-3d46-107f-f8768c7c688c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("bf4661f3-be5f-1a48-63fb-f521d84e8473"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("bf4b5556-50d1-21d4-ad5c-d731d685a3ef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("bfbfb40e-b99c-9993-bde9-749bf72206e2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("c010c311-7647-0ec5-8561-5408df855e87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c0a10a61-9d82-1bb4-c3ce-ecb96d912bfb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c0bcba0f-580b-c780-a1f9-a6330bebaa80"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c0d55070-b641-2405-681d-3d0e4cb48ec9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c0db68e3-2dc5-561a-a8f9-90e856226cf4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("c0dc5975-0100-a46b-3a68-f08b53370d26"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("c0f21c42-f15b-42e5-4f34-f63bd0a6f3d6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c1b8be25-b2c6-460d-9a28-6072291c4297"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("c1e852b9-b8bb-ca69-3cd0-f69dc625ab0b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("c211bad6-b7c8-1d79-1f8a-633a2eee8cce"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("c2358983-d1a1-5201-8190-b89074e78dba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("c28dee56-8bec-e33e-3dd1-1b8266fdc579"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("c311d9e3-58a4-52a4-a274-57b54ad63183"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c3872708-d710-c5f1-21a4-98a0c809bd09"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c40b0ab4-db07-30ab-75c6-a536129f1e42"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("c418ec92-57b8-1bb9-3fb1-66ed13d66dbd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("c446d1f1-0a95-f1df-55f1-9863aa2b7cf9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c4af3501-84d4-0256-5930-6fb1f4386550"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c4db49dd-b10b-6b4d-e275-cb271ba08596"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c4ec61cb-60c3-691d-2694-d6224e81675d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c52f1f6a-2182-52d9-060a-e1cf8b2bf35d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c531b32c-fbea-1c48-b494-26eb8db51c59"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("c53896eb-717f-1c93-8a39-a9f9ed0d863e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c5f29057-8ee7-a61a-ca1c-63115b20e6b1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("c62ec087-8bc1-1319-9aee-42841cc2cb67"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("c63323be-cd04-b0e3-eb1c-97442843e6ba"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c64305dc-2309-6840-d2ea-465fcf301537"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c664f9a8-aac1-2c66-4950-fe8d0a4c2430"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("c66d4c06-b1a6-2b18-37b1-56b7d9677643"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("c6c95969-c68e-f1d9-c708-d280df85c29e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("c74d68b0-8370-c01f-696f-b39c622156b3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c74fb6b2-68b6-592c-055a-c124792cecea"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c78eb0ff-8d8e-0082-a51f-f862c75a0ca9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("c7927b56-2770-1f6a-2767-3657b60403bf"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("c80614d6-27dc-22a0-b1b9-0a9f5b1536ea"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", true, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("c81d042a-8e3b-c955-e28d-688c09fa5e55"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c82ead8c-ecb4-cd22-16d5-203fe3aa41e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("c883528c-d9d8-8aff-6f8c-5f7db5965311"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("c8d1d274-222b-5dd0-2ce3-28d851b28e94"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("c8e771de-9e68-c6bb-77fa-99a35f151bbc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c8e913ee-f3ec-7aef-4b7b-2279b9d7a5e0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("c8f80905-5eb2-45ad-5c10-c82dd3c55a60"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("c9aa1c7c-10fa-d283-94b8-b22438b46889"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c9ec48cd-024d-128e-697f-389458e12c97"), true, false, false, false, true, false, true, true, false, true, true, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("cab229bc-99c5-02d6-a8ac-f83ceab8c336"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("cace414d-dc8f-d9ba-c2fa-42aa38e8a9a0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("cadbaf89-189c-cf4d-ed63-c74c0248fcbc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("caf4e7c0-1e09-3f80-df52-19697ce7d9bd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("cb4de670-3717-6aeb-23de-5f1791b26a50"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("cb6061cd-7db3-ac48-b473-9618f7c5024b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("cb69c0d5-aeac-9685-43f8-4d3a505b8718"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("cbb59df7-01e1-34b3-24f4-e3998b3c9fce"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("cbc259c9-6d95-23df-1ec8-4d5967df1169"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("cbf9aef0-76bd-f95d-16b6-105ae20f5e7a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("cbfc7fcf-65b2-e113-7387-39d168be58a1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("cc60ef9d-c06b-db1f-df24-b7bd7d92cff7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("cc858ba8-ae20-06d5-36b3-4f5f6ec53848"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("cca246c2-60dc-fefb-9378-9b7dc704231b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ccc0ebdb-4a57-d380-0da5-19c4a5c0fcc1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("cd24b498-bd2c-66dd-e337-d37871401b75"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("cd98145c-6609-7a61-33fd-dc963e6afc58"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("cda29931-d84e-aef3-6ec7-d5ee1bcae6de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("ce1fc874-a8dd-69b0-5336-cbccf0053cbb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("ce23c00d-3772-21ee-ec50-8e903fe1fc81"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("ce43339e-1d79-3c80-6110-c27249e1407d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ce8e33eb-25dc-69e5-3883-7c87b5dfbc04"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("cecbb5f3-5709-025a-d5f7-807d4151a665"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("cf171090-d8b5-8bbf-1f09-cf39b01953dd"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("cf49ddab-ce4a-e66b-1f7a-7ffd5e7c3779"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("cfa23ee0-48b3-b14c-7faf-57f6b3ccc05a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("d08f0a07-fcc7-9142-f5e2-bc12298b391e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("d093e505-880e-d176-36de-0e7addfee298"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d0955c56-00bb-33ef-426e-4ebc8a14b877"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("d098e311-bddb-41a7-6e51-4b8dacecba54"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d0bdd816-1a8f-0ade-28b2-d4c90a283ad0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d10ce882-7553-d9d3-38f8-aaf30dfbeb8a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("d1224cfb-0e09-3337-c4dc-b5fc728b4450"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d15f336b-f96e-94d4-9ac0-764d82895884"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("d1f0c525-8e61-dc6d-4913-693414b73a39"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d2021f2e-6e95-1a51-5a7b-0e09ee34d0ef"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("d2371ab8-06a9-c9c8-5edd-29494f01b74a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d26c9577-f465-6471-2e17-ec70530f55c6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("d2a23ff6-4e32-5802-94da-c3cf2bad90a5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d2c65f64-d1d1-551b-266b-9ada62ece036"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d2ce877b-87c4-8939-1e0b-fd980179beb6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("d2e88ad7-3e31-6cf4-2033-e634e4c17ceb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d306cb4c-368c-2c9b-1077-89409efcb9f9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("d34fc336-021c-6115-03f2-141c4250f45a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("d36151b6-241a-a60e-10c9-002d5022bd58"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d36413a5-4ea3-f92c-6801-59e9b7114af0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("d47355ca-57d6-40ae-29a3-e9b9b51aff04"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("d4939435-a2bd-60a3-ea95-f7226b490dd8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("d4a22905-013d-6a11-28f8-5287f5fc79f8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("d4b7e9bb-ef93-96a1-cf7a-46fef4a6f6af"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("d4e09e16-891a-34cc-1604-57d62da5f6d8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("d4ffa090-2309-a329-a24b-36be029a5644"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("d50095d1-ca12-9179-754d-8214572570cb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("d5036085-0b51-c015-e3cc-020a497306c5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d5ac7af8-60c7-a10c-7723-8c5766b232a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("d5caa783-5666-ecf5-aa06-6d2b302c30c7"), false, false, true, false, true, false, true, false, false, true, false, false, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d5d678bb-996e-ac73-c99a-2f0605fa7373"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("d5f64cfa-c44c-3deb-2934-367db7cb231b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d6501fe3-7d9e-95e3-c284-104c86cc5915"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d6b8f38b-86af-0ac5-7e97-581c08aec115"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("d6e57f01-4362-8b01-f906-949c7421b743"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d7180a5c-bc97-fdb6-a2e2-1b98dd103231"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("d77dcfb5-1484-279b-6b48-b377c46bf620"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("d82abe1c-11f3-1650-0f35-adc43cfd8e45"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d839158c-cc1e-3121-ce72-eabcb8bea70a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("d8b21d1d-8917-a583-e510-4ac212a9b982"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("d8b7eab3-ff6b-807d-abc1-83889a59c6d1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("d8d1e9af-6bf3-d7f7-cb14-7a9f6d1d13ab"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d8d61e3a-c764-d217-764c-b0ddb0d54957"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("d8dd92aa-9d84-e2d0-b79c-c3e381d95318"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("d9c2d3e1-b0f2-708e-2026-8c389dc7f737"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("da339d77-93c1-8b00-1227-6bda34380f10"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("da939ff0-1ad5-618d-c010-f56081d823f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("dadda6ba-b432-508a-305a-77b8a391f540"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("db7b3a3a-9184-110b-4f1d-3a7970b42f99"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("db8223ce-69ce-248f-33aa-ad143b52f80f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("dc466d18-679e-85aa-a346-e4062dfbeddc"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("dc9b2f94-6506-f50d-5518-46d6e04af43a"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("dcbc343e-750b-3fe5-2562-ff40af63dccd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("dcfead7d-6432-2c14-471a-35bfff6fc6fd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("dd24724e-2f2a-9e4c-65ba-15d944d4fe9f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("dd52d2aa-6437-9b50-397b-2951834c1096"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("dd5a0314-f5fc-dcbf-5b15-71ab8422fca2"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("ddb482ac-79dc-2b3e-15bb-ea3ca9464bb2"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("dde38067-157b-9e03-a3cd-d782f294ea4c"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ddff4cb2-3075-5fb8-5217-97bbc5b7c43d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("de26e77c-eb61-d353-a66e-4ce6bee14e87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("de30a7c6-c3bd-02a9-4253-6cf7767b019b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("de46e36f-893b-fee8-9f12-cfc4d4502a2e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("de59f25c-9cb4-9779-af25-037162187e40"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("de79f848-28ed-cead-1365-5243b3e4f6d8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("ded69009-7eeb-3d2a-fe39-c79302dbf6f0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("df09a412-868d-93ba-12b4-0ce27c1178ed"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("df23630f-765d-2320-c2c0-77ee01d88572"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("df313adc-aabf-e303-0836-b0e171f19e09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("e01fc194-c8a5-1678-23b2-39e1af347b2b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("e074d56d-394c-076d-baeb-d765172ed9b8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("e113bce8-cea3-ca48-8fdb-2bfcf0c3b7e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("e22cdbb3-b206-edf9-39c7-4c5fba7d2e59"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("e24a5141-961b-7d1d-a40e-c826a74e2be6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("e2d18738-f7eb-ddd5-6f4e-7c87a471f435"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("e30f630e-07b1-2fa5-d183-a3774266f6bd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("e392d6b6-7b54-e0c2-1d67-42474353fd00"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("e3b09e2d-2382-f2e5-4386-685189d312c9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e3d01b8e-3ddd-b43e-3ca1-1a1accf5d48a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("e3e74f28-4eb0-bae9-2648-3731de092f4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("e41627b8-5410-a60f-eafe-c02045b8d6e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("e42536bf-9ca7-1f32-bfb8-3afb6e930af4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("e42e00a2-9b4d-461a-03d9-76410b89b78a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("e46d8194-1a7a-0adb-66dd-7de372c49126"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("e4eefed5-8eb7-4a8c-13f1-0b458fee2f5b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("e4ef51b0-080a-a4b5-40cd-76ceae40608a"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("e4fb4bcd-a855-58f8-4858-8a4e825185dd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e4ff94bd-1d8f-e2f0-8393-feeac2d7d415"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("e5732185-aee6-05ac-5448-66bd800f108a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("e5876085-5ead-4b22-9d5e-3f400c8018bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("e623b2e9-5eb1-cc16-7b0e-d370e1b08929"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("e668f430-c067-2ef4-2e92-80c3551045c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("e68927ed-7502-fd94-30dc-08fcdc435577"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e6bd45b9-ee07-41cd-c471-38fabd17d936"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e6e28da6-674c-94df-0579-cac1a27e22bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e7a7015b-3a6c-9956-3c78-560c375e6c4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("e7b9e221-0799-7867-f623-9ec602b64c84"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e7e7faaf-0d61-f923-14aa-4a83358e27c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("e818913d-251c-5c1c-8395-3ea116a3c0b2"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e884a85a-8f0d-cefb-be5b-5e1abfa6d613"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e89087db-d5c2-1aa8-4e8a-344b9b61650d"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("e8af7e32-11ba-0e9c-b2d2-1a80aa526f8a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e8df651c-9a0e-1538-fc93-58eb88bd547b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e9233f7f-5f43-f2a5-c288-ad2133fc2a85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("e93749b8-6e80-7b68-4915-57d98a6ea489"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("e960745e-ed0b-5320-67e3-7168d5a87bfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("e9658c44-a678-1843-a98f-8d83f14374c5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("e9909612-12de-8e0e-8ee1-ec626ebfee73"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("e9be6bd5-e764-1b65-7835-f8bcae254ad3"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("eaa52044-6e94-fcd9-82d8-9a3323450753"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("eabf8ec0-15e8-833d-221b-09216a1274fb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("eaed321e-6705-0242-862f-648df84de291"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("eb22cee7-d0b9-f458-45e8-8c20892f22d5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("ebad443a-b38d-50c0-70b1-16d5cdc7dd27"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ebc01bbf-4ea6-27ea-cdaa-30b0af73f042"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("ebde7074-a532-bee8-2f7d-e9fca2a6b8b1"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ebf3a1c7-3715-56c1-0137-8168db2caef4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ebf9c7e7-5ae8-37f8-971c-d62e9173effe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("ec065199-e859-2cdb-b92a-d94d25f7cd41"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("ec0787e5-d6cc-31c9-9de2-e605c3e3f41f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("ec586367-c47c-fa2c-a3a6-7b652a8bcf03"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("ecb616be-7722-4c49-faeb-040aa1982c54"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("ed1cc2e0-7d4f-2edc-50d7-82c87c911dbe"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ed25bf2a-d39b-e5c5-a120-0fb61cea719b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("ed3d364e-a0c0-3d1b-2a06-2b3bc8fe244b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("edf294d4-23a2-7db3-17df-d51fb3242b7e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ee607d44-6dbd-ece5-9270-9220f465a7f0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("eefe49de-1012-2179-7c75-aba9078db5ca"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("ef05e0b9-c4f6-3f54-337f-ce59ae194851"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("ef20211e-7093-b3cf-e56b-7860cbcc3f71"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("ef517f5d-f399-b278-68ce-ce1438f75ef3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("ef704a63-fbd9-c948-86eb-16ee917f26db"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("efca1a41-5b67-3fd1-6e31-30bc93ed28d7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("efebb601-947d-4bcc-990d-874e68519092"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("f095917e-cd2f-48df-373e-1ea1e7e2a7b8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("f0b1e3a9-f280-4200-6d54-82795a4dce05"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("f0bd803c-23d2-440c-ca37-02bb554bffd1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("f11df896-6b21-11c7-e76d-19738edaee75"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f12cb910-2441-39d8-654b-4aa279923689"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("f1878558-833b-96ee-d597-77b29c8df47c"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f25f6ac3-b0dd-64f4-a82d-b459aff22397"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("f279f58e-9c05-e09d-cc7a-86085c8b504c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("f281882b-ac49-d6d5-1d06-7321ad65a23c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("f2fe61f8-7339-f3bf-e5c0-d14e1f24ab55"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("f3468670-5e3d-d8ac-35a4-28507f06e96b"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("f3722c30-f24c-70a8-9b5f-d1bb217b1a6c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("f3a54bbe-51a4-b36f-11cc-81fbf1c263e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("f3c13c8c-1c8e-9fe8-f7d3-7c03ec9b4395"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("f3cb0d4c-4540-8941-69dd-eea73b8824e3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("f42b50c3-5a0b-aa73-857d-9458c9bfcceb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f4322806-498f-c746-da45-48a867b7e7a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("f476dd17-da4d-1832-a8f9-38c838af9d1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f494380c-48f3-5f83-45cc-3a15c9cc28dd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("f4c531c9-50f4-244d-ded3-acdf840ea285"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("f5240291-ec17-bea1-5a31-eead7c8a0ec9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f590c363-4b5c-ef51-b90c-f0231c1c0b1d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("f6833c5e-20d7-0207-8c20-1c0be53d39e1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("f6bb9a99-8562-5a79-1e09-e39b78c55e4f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f709009e-b968-82f2-a97f-bbde8548dc39"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("f7524e14-8509-4f20-57df-2de1e6f5b835"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f782cee5-72e9-d8a2-0b6b-89b556c03f11"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("f79f00d4-6cf4-e4ff-a123-1b51ba9a52c8"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("f7d23fd6-2d22-262a-7d9e-d9247a8021f5"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("f85c5b04-4506-9156-b01b-badc19a6ed6e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("f86c37cf-123f-3c8e-10e4-b795ad8d23ce"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f89a93ab-d59a-4a7a-2633-d405fbe6a350"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("f8e7d0a6-f056-175a-e604-14c1f9f6ad83"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("f93bfe0b-1a54-9e7c-0a9f-70a4b60b7e95"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f9b536bb-4190-53d4-ed42-16932e9c5a51"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000024"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("f9bcd5c7-6f01-2f0a-5b36-f66860c6b37a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("f9c9f6cc-48b9-8727-4c81-4196e4444b59"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("fa0e5df4-74cf-d3f5-3f58-43bb465f3a11"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("fa2a4fd6-6b37-8577-a52d-bfbffc2d3998"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("fa43bbd4-306d-f557-e8ef-d2cd39d87114"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("fa7ff198-dd2a-2f9f-1f73-6245d877889a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("facfe5f2-40e0-709c-9244-1b46cba82981"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("fb1c6e1a-c6ad-bff4-b8df-e19b219d5a92"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("fb2f67b4-6f92-4982-87fb-a6f19856ef11"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("fb3764c9-95da-27af-b6f2-2ea6387262a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("fb8f908c-0c79-8a2f-3941-b414ceff52c9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("fbb625b2-1fc6-6b06-9197-0788c71746f1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("fbe1b1b4-ab46-124c-67e9-b8e699871fb1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("fbe3075e-ebb0-37eb-69d3-d7ab1dcdf0b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("fccd7ce8-5d33-8d12-0808-c81350de2b93"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("fcf487a0-7345-b5f0-8f88-784ce8f0016a"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("fd028b07-4ecc-439f-ca45-cbcf249574a9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000026"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("fd354f3f-68ac-66d8-0639-1c32f09fa0d0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("fd4c02e7-4bcf-cf41-bc84-0c4025efae03"), false, true, true, false, true, true, true, false, false, true, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("fd943b39-60d2-01e0-2978-7313334b7cc0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("fdca9135-c0c0-46b4-33e3-5d051f433ad1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("fdf4faec-0492-3386-311d-4b366d490863"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("fe763e56-8a62-9d7e-8722-77ba4816d949"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("fe9aff9c-bcbf-a921-59d0-15536ba33d22"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("fea6b003-57fb-420d-e2de-6fa446866317"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("fea74237-eda4-9622-b2f2-e4a5344fd686"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ff2181d4-cb3b-ac61-78cb-474d8b70762d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000025"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("ffe73e4a-7346-cf6a-2e58-0fa7cc6e2b96"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_import_history",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EmployeeId", "ImportBatch", "NormalizedEmployeeName", "SourceEmployeeCode", "SourceEmployeeName", "SourceJson", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0f56a17e-c040-acb4-6736-1cc168a81c46"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "REV866_EMPLOYEE_SEED_20260808", "PRIYA.E", "SESS-012", "PRIYA.E", "{\"Code\":\"SESS-012\",\"Name\":\"PRIYA.E\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Admin/Accounts/Stores\",\"Skill\":\"Admin/Accounts/Stores\",\"Designation\":\"STORES AND PURCHASE\",\"Roles\":[\"PURCHASE_EXECUTIVE\",\"STORES_EXECUTIVE\"]}", null, null, 0L },
                    { new Guid("0f6b42e6-1bab-d372-290a-9057fd7805f6"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), "REV866_EMPLOYEE_SEED_20260808", "M. SATHISHKUMAR", "SESS-003", "M. SATHISHKUMAR", "{\"Code\":\"SESS-003\",\"Name\":\"M. SATHISHKUMAR\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("1963049e-f974-5923-54e3-72af4c92f635"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), "REV866_EMPLOYEE_SEED_20260808", "DEVANAND B", "SESS-037", "DEVANAND B", "{\"Code\":\"SESS-037\",\"Name\":\"DEVANAND B\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("2bc77b77-1d6d-4279-8d9d-8cf854537ea0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), "REV866_EMPLOYEE_SEED_20260808", "PRASANNA.G", "SESS-032", "PRASANNA.G", "{\"Code\":\"SESS-032\",\"Name\":\"PRASANNA.G\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"LABVIEW DEVELOPER\",\"Roles\":[\"SOFTWARE_ENGINEER\"]}", null, null, 0L },
                    { new Guid("2d009327-ea1c-2e86-5f13-bc4df67fd6bc"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), "REV866_EMPLOYEE_SEED_20260808", "ALAGUEASWARI", "SESS-002", "ALAGUEASWARI", "{\"Code\":\"SESS-002\",\"Name\":\"ALAGUEASWARI\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Management\",\"Skill\":\"Management\",\"Designation\":\"MD\",\"Roles\":[\"MANAGING_DIRECTOR\"]}", null, null, 0L },
                    { new Guid("2f40e507-8533-479e-6db2-d696d7cb5807"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), "REV866_EMPLOYEE_SEED_20260808", "A. ALFATHIMA PARVEEN", "SESS-007", "A. ALFATHIMA PARVEEN", "{\"Code\":\"SESS-007\",\"Name\":\"A. ALFATHIMA PARVEEN\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JR. ACCOUNT\",\"Roles\":[\"ACCOUNTS_ASSISTANT\"]}", null, null, 0L },
                    { new Guid("3045b304-1c11-b626-4170-02ed928cfde8"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), "REV866_EMPLOYEE_SEED_20260808", "YESWANTH KUMAR.N", "SESS-011", "YESWANTH KUMAR.N", "{\"Code\":\"SESS-011\",\"Name\":\"YESWANTH KUMAR.N\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JUNIOR ENGINEER\",\"Roles\":[\"JUNIOR_ENGINEER\"]}", null, null, 0L },
                    { new Guid("360cc0c3-8709-66a2-513c-bff91aed60e0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), "REV866_EMPLOYEE_SEED_20260808", "VINAYAGAM", "SESS-035", "VINAYAGAM", "{\"Code\":\"SESS-035\",\"Name\":\"VINAYAGAM\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"FABRICATOR\",\"Roles\":[\"PRODUCTION_OPERATOR\"]}", null, null, 0L },
                    { new Guid("38f02d97-8ea0-c6a0-6132-cf41067a7af3"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), "REV866_EMPLOYEE_SEED_20260808", "S. NANTHAKUMAR", "SESS-006", "S. NANTHAKUMAR", "{\"Code\":\"SESS-006\",\"Name\":\"S. NANTHAKUMAR\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT\",\"Roles\":[\"JUNIOR_ENGINEER\"]}", null, null, 0L },
                    { new Guid("3da0797e-c7ce-8c50-3bcd-a857613a54db"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), "REV866_EMPLOYEE_SEED_20260808", "RAJESHKUMAR.V", "SESS-010", "RAJESHKUMAR.V", "{\"Code\":\"SESS-010\",\"Name\":\"RAJESHKUMAR.V\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"ELECTRICAL ENGINEER\",\"Roles\":[\"ELECTRICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("402f96b9-1b0a-2400-183e-987b2b06f2d6"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), "REV866_EMPLOYEE_SEED_20260808", "RANJEETH.B", "SESS-020", "RANJEETH.B", "{\"Code\":\"SESS-020\",\"Name\":\"RANJEETH.B\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Admin/Accounts/Stores\",\"Skill\":\"Admin/Accounts/Stores\",\"Designation\":\"HR DEPT\",\"Roles\":[\"HR_EXECUTIVE\"]}", null, null, 0L },
                    { new Guid("433b462b-d44e-0ce4-a6ba-a9373b87e605"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), "REV866_EMPLOYEE_SEED_20260808", "SURANTHER P", "SESS-008", "SURANTHER P", "{\"Code\":\"SESS-008\",\"Name\":\"SURANTHER P\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"SOFTWARE DEVELOPER\",\"Roles\":[\"SOFTWARE_DEVELOPER\"]}", null, null, 0L },
                    { new Guid("48023480-faa5-975e-ee67-4ee5854aa96b"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), "REV866_EMPLOYEE_SEED_20260808", "MOHD ASHIQ", "SESS-017", "MOHD ASHIQ", "{\"Code\":\"SESS-017\",\"Name\":\"MOHD ASHIQ\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JUNIOR ENGINEER\",\"Roles\":[\"JUNIOR_ENGINEER\"]}", null, null, 0L },
                    { new Guid("55b979e1-f612-de68-1aa0-d6348dd174cd"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), "REV866_EMPLOYEE_SEED_20260808", "SRINIVASAN.C", "SESS-029", "SRINIVASAN.C", "{\"Code\":\"SESS-029\",\"Name\":\"SRINIVASAN.C\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("59fbbe70-bb14-d466-3bf7-e97a1040c446"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), "REV866_EMPLOYEE_SEED_20260808", "MADHANKUMAR.J", "SESS-034", "MADHANKUMAR.J", "{\"Code\":\"SESS-034\",\"Name\":\"MADHANKUMAR.J\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("5d2880b6-6e40-84c4-b982-e4f16b422dd5"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), "REV866_EMPLOYEE_SEED_20260808", "MANIKANDAN.S", "SESS-009", "MANIKANDAN.S", "{\"Code\":\"SESS-009\",\"Name\":\"MANIKANDAN.S\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JR. ENGINEER\",\"Roles\":[\"JUNIOR_ENGINEER\"]}", null, null, 0L },
                    { new Guid("6695623a-7f5c-4041-00e4-c8d7cde7745e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), "REV866_EMPLOYEE_SEED_20260808", "RANJITH.E", "SESS-015", "RANJITH.E", "{\"Code\":\"SESS-015\",\"Name\":\"RANJITH.E\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"DESIGN ENGINEER\",\"Roles\":[\"DESIGN_ENGINEER\"]}", null, null, 0L },
                    { new Guid("756caab8-cd36-fe0a-4a9b-2cfc2651549e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), "REV866_EMPLOYEE_SEED_20260808", "THIRUNAVUKKARASU", "SESS-039", "THIRUNAVUKKARASU", "{\"Code\":\"SESS-039\",\"Name\":\"THIRUNAVUKKARASU\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("75cd655f-0c24-89ae-9f3b-11fc83651c0e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), "REV866_EMPLOYEE_SEED_20260808", "SARATH BABU.K", "SESS-023", "SARATH BABU.K", "{\"Code\":\"SESS-023\",\"Name\":\"SARATH BABU.K\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"PRODUCTION COORDINATOR\",\"Roles\":[\"PRODUCTION_COORDINATOR\"]}", null, null, 0L },
                    { new Guid("91576a97-ed27-5bf5-5ff3-82bf4912a2da"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), "REV866_EMPLOYEE_SEED_20260808", "SANJAY SARAVANAN", "SESS-027", "SANJAY SARAVANAN", "{\"Code\":\"SESS-027\",\"Name\":\"SANJAY SARAVANAN\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JUNIOR ACCOUNTS\",\"Roles\":[\"ACCOUNTS_ASSISTANT\"]}", null, null, 0L },
                    { new Guid("9a98139e-e3cf-e3a5-efb7-eb276b5b5bf7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("26c37705-e799-8708-119b-1227908d5e0f"), "REV866_EMPLOYEE_SEED_20260808", "PRAKASAM.B", "SESS-024", "PRAKASAM.B", "{\"Code\":\"SESS-024\",\"Name\":\"PRAKASAM.B\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"ELECTRICAL ENGINEER\",\"Roles\":[\"ELECTRICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("9c911b33-3733-9d90-307f-c2221e6586b3"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), "REV866_EMPLOYEE_SEED_20260808", "SYED IJAZUDDIN Z", "SESS-038", "SYED IJAZUDDIN Z", "{\"Code\":\"SESS-038\",\"Name\":\"SYED IJAZUDDIN Z\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"PLC ENGINEER\",\"Roles\":[\"PLC_ENGINEER\"]}", null, null, 0L },
                    { new Guid("a0519833-9d8b-dbd7-42aa-df3fb73ab391"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), "REV866_EMPLOYEE_SEED_20260808", "KAMALI SRINIVASAN", "SESS-014", "KAMALI SRINIVASAN", "{\"Code\":\"SESS-014\",\"Name\":\"KAMALI SRINIVASAN\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"STORES ASSISTANT\",\"Roles\":[\"STORES_ASSISTANT\"]}", null, null, 0L },
                    { new Guid("a16a71a7-1c21-c40b-7fe5-4b76aa13f2d7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), "REV866_EMPLOYEE_SEED_20260808", "T. DINESH", "SESS-004", "T. DINESH", "{\"Code\":\"SESS-004\",\"Name\":\"T. DINESH\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Manager\",\"Skill\":\"Manager\",\"Designation\":\"TECHNICAL SUPPORT MANAGER\",\"Roles\":[\"TECHNICAL_SUPPORT_MANAGER\"]}", null, null, 0L },
                    { new Guid("a9a42d67-1710-9687-2eeb-df48df1adc33"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), "REV866_EMPLOYEE_SEED_20260808", "PRAVEEN KUMAR.M", "SESS-028", "PRAVEEN KUMAR.M", "{\"Code\":\"SESS-028\",\"Name\":\"PRAVEEN KUMAR.M\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("b2e05e24-8e31-871f-a938-4253cfe87be9"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), "REV866_EMPLOYEE_SEED_20260808", "KALIDOSS", "SESS-016", "KALIDOSS", "{\"Code\":\"SESS-016\",\"Name\":\"KALIDOSS\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"DESIGN ENGINEER\",\"Roles\":[\"DESIGN_ENGINEER\"]}", null, null, 0L },
                    { new Guid("b4c08282-5c80-b7a0-5143-fd5a5bb112a1"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), "REV866_EMPLOYEE_SEED_20260808", "MANIKANDAN SOKKALINGAM", "SESS-030", "MANIKANDAN SOKKALINGAM", "{\"Code\":\"SESS-030\",\"Name\":\"MANIKANDAN SOKKALINGAM\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"ELECTRICAL ENGINEER\",\"Roles\":[\"ELECTRICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("b7dea89e-de29-daa2-4608-72c6734e3aa1"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), "REV866_EMPLOYEE_SEED_20260808", "KARTHICK.B", "SESS-022", "KARTHICK.B", "{\"Code\":\"SESS-022\",\"Name\":\"KARTHICK.B\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"ELECTRICAL ENGINEER\",\"Roles\":[\"ELECTRICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("c02926b7-b69c-f94e-4f98-d3e7e8b304a6"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), "REV866_EMPLOYEE_SEED_20260808", "A. VINAYA SAGAR ARKATI", "SESS-018", "A. VINAYA SAGAR ARKATI", "{\"Code\":\"SESS-018\",\"Name\":\"A. VINAYA SAGAR ARKATI\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"ELECTRICAL ENGINEER\",\"Roles\":[\"ELECTRICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("c169fe6d-6b2c-33ec-c820-daaebaf58fef"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), "REV866_EMPLOYEE_SEED_20260808", "VENKAT RAV.S", "SESS-031", "VENKAT RAV.S", "{\"Code\":\"SESS-031\",\"Name\":\"VENKAT RAV.S\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JUNIOR ACCOUNTS\",\"Roles\":[\"ACCOUNTS_ASSISTANT\"]}", null, null, 0L },
                    { new Guid("c4c160a6-38ca-fb45-1596-1acde02fef13"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), "REV866_EMPLOYEE_SEED_20260808", "KRISHNAVENI", "SESS-021", "KRISHNAVENI", "{\"Code\":\"SESS-021\",\"Name\":\"KRISHNAVENI\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Admin/Accounts/Stores\",\"Skill\":\"Admin/Accounts/Stores\",\"Designation\":\"ADMIN MAINTENANCE\",\"Roles\":[\"ADMIN_EXECUTIVE\"]}", null, null, 0L },
                    { new Guid("ca1ac22f-c92b-6f0b-6d00-dd686a27adf0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), "REV866_EMPLOYEE_SEED_20260808", "WASEEM.S", "SESS-005", "WASEEM.S", "{\"Code\":\"SESS-005\",\"Name\":\"WASEEM.S\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"PRODUCTION MECHANICAL TEAM\",\"Roles\":[\"PRODUCTION_OPERATOR\"]}", null, null, 0L },
                    { new Guid("cfdc990d-5afd-1b29-bf52-ab5995b174cf"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), "REV866_EMPLOYEE_SEED_20260808", "FRANCIS XAVIER", "SESS-036", "FRANCIS XAVIER", "{\"Code\":\"SESS-036\",\"Name\":\"FRANCIS XAVIER\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"REFRIGERATION / MECHANICAL ENGINEER\",\"Roles\":[\"TECHNICAL_ENGINEER\"]}", null, null, 0L },
                    { new Guid("d181ade1-290a-8ebe-1f57-47b66b4ecdde"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), "REV866_EMPLOYEE_SEED_20260808", "KARTHIKEYAN MK", "SESS-025", "KARTHIKEYAN MK", "{\"Code\":\"SESS-025\",\"Name\":\"KARTHIKEYAN MK\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"FABRICATOR\",\"Roles\":[\"PRODUCTION_OPERATOR\"]}", null, null, 0L },
                    { new Guid("d4bbc4c9-5036-bb52-53bb-2dd1e420b5ed"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), "REV866_EMPLOYEE_SEED_20260808", "LALU", "SESS-013", "LALU", "{\"Code\":\"SESS-013\",\"Name\":\"LALU\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"FABRICATOR\",\"Roles\":[\"PRODUCTION_OPERATOR\"]}", null, null, 0L },
                    { new Guid("d85900eb-e0a2-9ac2-9298-7bbef29480e7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), "REV866_EMPLOYEE_SEED_20260808", "SRINIVASAN.V", "SESS-026", "SRINIVASAN.V", "{\"Code\":\"SESS-026\",\"Name\":\"SRINIVASAN.V\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Production/Fabrication\",\"Skill\":\"Production/Fabrication\",\"Designation\":\"FABRICATOR\",\"Roles\":[\"PRODUCTION_OPERATOR\"]}", null, null, 0L },
                    { new Guid("e2bb043e-cfe0-c4a1-1a63-53097f1ebea4"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), "REV866_EMPLOYEE_SEED_20260808", "BLESSON PAUL", "SESS-033", "BLESSON PAUL", "{\"Code\":\"SESS-033\",\"Name\":\"BLESSON PAUL\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Junior/Assistant\",\"Skill\":\"Junior/Assistant\",\"Designation\":\"JR. ENGINEER\",\"Roles\":[\"JUNIOR_ENGINEER\"]}", null, null, 0L },
                    { new Guid("f03f9db4-a89a-7d11-960a-43eb702e3439"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), "REV866_EMPLOYEE_SEED_20260808", "RANJITH. R", "SESS-019", "RANJITH. R", "{\"Code\":\"SESS-019\",\"Name\":\"RANJITH. R\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Engineer/Technical\",\"Skill\":\"Engineer/Technical\",\"Designation\":\"DESIGN ENGINEER\",\"Roles\":[\"DESIGN_ENGINEER\"]}", null, null, 0L },
                    { new Guid("fca42fa4-a3cf-3b56-6f79-bc0eeebf551e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "REV866_EMPLOYEE_SEED_20260808", "A. PARAMANANTHAM", "SESS-001", "A. PARAMANANTHAM", "{\"Code\":\"SESS-001\",\"Name\":\"A. PARAMANANTHAM\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Management\",\"Skill\":\"Management\",\"Designation\":\"TECHNICAL DIRECTOR\",\"Roles\":[\"TECHNICAL_DIRECTOR\"]}", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_role_assignments",
                columns: new[] { "Id", "ApprovalStatus", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "EmployeeId", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("02702296-3863-8644-c306-ddc2f49e5cca"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), "REV866 approved initial mapping", new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("068427ee-6fc5-8182-b61c-24b2b3187867"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("157d94ff-a39e-3fa4-3a54-f6f8d05cab62"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), "REV866 approved initial mapping", new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("18da9f7c-3049-52e3-b76c-c4238cedb213"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), "REV866 approved initial mapping", new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("1b5c6764-7dcd-6f19-0097-61b87603b5eb"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), "REV866 approved initial mapping", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("205cd7e9-b79c-4600-f9c9-561e15e2be9f"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), "REV866 approved initial mapping", new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("25c10527-28a2-e600-82d2-3b1b767af269"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), "REV866 approved initial mapping", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("261e0ee9-c1a4-6f18-a3fc-461add06916b"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), "REV866 approved initial mapping", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("270a811f-0564-a4b0-8f4f-0b47118d3134"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), "REV866 approved initial mapping", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("2e2b854a-f965-2a71-21c3-96738e3cb840"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("30e7eac7-1101-ffde-70c0-6edd20ed4c01"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("3b51f513-0e8e-7677-b138-19bc0d9c4150"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("3b6fe413-e8d3-3c0e-52a0-2425db151f48"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), "REV866 approved initial mapping", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("4a1b90a5-9797-0fd0-0e6d-58785e981854"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), "REV866 approved initial mapping", new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("53f3f0b9-de8b-4119-3668-01c751a3d52a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), "REV866 approved initial mapping", new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("5554a0f5-85f0-d477-ea7b-f3a6cd1ed121"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), "REV866 approved initial mapping", new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("67461916-89e1-fe39-e460-39d2d341d242"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), "REV866 approved initial mapping", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("6c56b8eb-3f8a-4940-df22-5e8002b262da"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), "REV866 approved initial mapping", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("6d4b74b6-5611-c8f5-0ba5-48be51fd6996"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), "REV866 approved initial mapping", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("87dd003b-f6f7-fb19-9f89-c395683c8fa0"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), "REV866 approved initial mapping", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("8b4828cc-bbf0-05df-0f27-a3d789052b82"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), "REV866 approved initial mapping", new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("8b8c5e6b-cc4d-4386-50a3-32fb3d776860"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), "REV866 approved initial mapping", new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("8c3e4b9b-6be9-9fa3-9c81-fa47f23b5818"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), "REV866 approved initial mapping", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("8c7733c4-1a45-970b-a81b-dbf5aa781ef0"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("8ee5108f-6a19-af67-0562-ee708ebd6a05"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), "REV866 approved initial mapping", new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("98804443-54b0-2474-7acb-ffc54410e33e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), "REV866 approved initial mapping", new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("9ac81cf0-423b-97a8-08e7-d3797a7410c7"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), "REV866 approved initial mapping", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("9e1e368d-3c82-60cf-f522-7758004d3e88"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "REV866 approved initial mapping", new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a260b451-c377-907d-ba80-fb03af55ebc0"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), "REV866 approved initial mapping", new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a2bc7e87-56b4-0478-d29d-c329f7eb060a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("26c37705-e799-8708-119b-1227908d5e0f"), "REV866 approved initial mapping", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("a7552ac8-23f1-9ed4-6de8-669d08054e0a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "REV866 approved initial mapping", new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a79e4f09-112d-57e5-4f17-00066b3e6d22"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "REV866 approved initial mapping", new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("ad9892ac-7d0f-89fc-8aec-be5f65860079"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), "REV866 approved initial mapping", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("ae3c6d06-5d8c-fa88-ae24-4dcf2ddbfacb"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), "REV866 approved initial mapping", new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("babde2dc-2cd6-83b4-eea4-84c5886b436e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("c3aa8842-31de-0d93-71b8-ba5e8895a534"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), "REV866 approved initial mapping", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("d278c271-c2e2-00a7-a70b-ca058dc2af0e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), "REV866 approved initial mapping", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e6cf6f13-4f3a-56c8-dbed-608f3b596b6e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), "REV866 approved initial mapping", new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("ec95b2c0-4bb6-9b59-3e5e-6fd16ce97ba3"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), "REV866 approved initial mapping", new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("f03cb56e-0797-3443-b51a-d28205fcdfa7"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new DateOnly(2026, 8, 8), null, new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), "REV866 approved initial mapping", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_skills",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EmployeeId", "IsPrimary", "SkillId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01b656b7-1c1b-049d-efd1-8d0b64829a8d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("056007cc-14ad-07ac-37ed-710317986079"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("05a4f01a-b08e-eef9-111a-5c2d80628635"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("064c0e7c-1100-53e4-e61b-e31cde27b926"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("0776cbef-3fce-eae3-93f9-203026c14b0d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("0c2f0c13-7f99-0844-40a4-4176bf879e8e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("0d319655-0dc2-824f-dd37-feca4300c8f5"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("0f95c04d-3bc4-dffe-9303-cdc4beb486f3"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("1527dc30-ed30-3417-6eba-e2e67586e3e5"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("17b1b1bf-53a3-cf1b-4fc7-5045babfc4bd"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("1b52cf9c-cd01-a36c-1f4e-0e4d70b9c62d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("1c4e3af6-7bb3-0435-a4cf-c1f30e9068ff"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("26c37705-e799-8708-119b-1227908d5e0f"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("263389c6-816d-ddf3-d085-308c2658dab2"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("2709331f-90d3-8de8-aa96-fd4d23550dd4"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("32f1d74d-1ccb-bb31-b51c-6b800255b5aa"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("3355ba31-be24-d446-9158-a258f3473fa8"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("35a6a5ec-633b-5c4f-af77-14548af36cb1"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("4bb6ddc1-ab0c-84b4-e3fa-05d27646c634"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("4eed53a2-8959-28e0-dcce-deab88618ae7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), true, new Guid("972ffd8b-159a-fbe4-9a9a-a3913ce3a623"), null, null, 0L },
                    { new Guid("5277f6ec-9fee-9a53-9ccd-e698241f6dfa"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("56375b72-eccc-a97c-290b-764b093de78f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("5c9de5f8-0784-1031-b5be-c849e4018681"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("81ad13d5-7a16-39df-1166-a5daadbbbd89"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("8268ac61-34c0-ac38-3f34-7cba88708059"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("833d1f0d-b59b-50b6-e892-6068d5a0c2f7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), true, new Guid("6bb4adb2-ac56-5ebc-abd0-f0eb65cd965a"), null, null, 0L },
                    { new Guid("8d6d8037-574a-f183-10a7-431bede5bdcb"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("a2df480a-f0c7-7cb4-52f7-fdcd9ee0bd30"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("3543a705-924a-6599-23be-fb9730a93f06"), true, new Guid("6bb4adb2-ac56-5ebc-abd0-f0eb65cd965a"), null, null, 0L },
                    { new Guid("a86542e3-a607-4f29-7685-0d51aeca0fea"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("aa630aa9-d92a-9c88-941f-8e4d002caf52"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("b1e15fe1-c3e8-0e30-cd19-dff7fdb308d2"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("b2ebc5df-0942-03c6-8b66-b6584329509e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("b301cb58-6e97-dbb4-ff52-742484c2a591"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("bfccd85a-7e4f-2158-3351-ab4326af10b7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), true, new Guid("7c29ea14-2ef4-0a51-001b-4c748b86d151"), null, null, 0L },
                    { new Guid("c7a9ebe9-d598-71c9-f070-daabd17af6ea"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), true, new Guid("972ffd8b-159a-fbe4-9a9a-a3913ce3a623"), null, null, 0L },
                    { new Guid("c92b9a24-7d96-4ded-7d7d-0d6b3ff3a4c3"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L },
                    { new Guid("ce47b71b-1e03-b4e7-1f00-d7be97960b9f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), true, new Guid("76356d46-cd2a-c51d-d164-02cf7e3d570e"), null, null, 0L },
                    { new Guid("d365ce57-7aa7-0484-d381-d9acddce8da8"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), true, new Guid("ffbbe947-c562-fa9e-3962-a4ce411c8004"), null, null, 0L },
                    { new Guid("d519ea74-a6f8-5245-7cc9-55b5495d758d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), true, new Guid("972ffd8b-159a-fbe4-9a9a-a3913ce3a623"), null, null, 0L },
                    { new Guid("d98aad28-a681-ba35-fab2-5203598373f7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), true, new Guid("fb71015c-021d-75f2-daf5-dc631d89220b"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_status_history",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EmployeeId", "NewStatus", "OldStatus", "Reason", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("1a2a5dec-8b78-3dfc-0bc2-5b6bb336fc01"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("205f6311-eba5-4e1c-98ed-17ca94e92b44"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("23b32cf1-c5a1-6049-4f33-03950ec24ce2"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("2a0fb58f-2f46-2566-d05f-6fcd92c66fed"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("2ab8a02f-674f-99d0-6c92-ce3c6dc00663"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("2f900ed8-9a79-65e8-a307-f71aa6314a5a"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("3374eb17-8fb6-b10a-6a44-3be3153f170f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("34d2f6f1-8ada-a885-0d8f-f2ad198281f8"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("3547cdb4-9ff3-e9d8-aa74-656ba070fef0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("4303b834-9774-a18c-8633-6e1fe106e392"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("4731bc5c-5c63-790b-85b2-f765faedefa4"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("48803a06-69f4-567d-532d-ab1b013b72ad"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("4eb6a19d-9df1-cd9b-25bd-c579ed7552c0"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("5f8ae880-c6ba-df5d-2c0b-c52bf71a618d"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("702445e8-b85d-b073-d05b-84650d3b6a97"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("743e6628-613e-2cad-032a-dff4f833d6f6"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("77b778cb-98fd-62e6-9649-0dae69949e4e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("86472fee-4ed8-8ac7-cfc4-e78a4c3cfc3f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("898f649c-3f51-4f3a-fc46-1fc43dfb66a2"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("9ffd8d2f-c652-162e-bd22-a9b125e6a8c7"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("a7c5f3dc-aa26-101c-4874-8d9f225535b1"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("aca4b500-66b1-cdf9-b28b-aa7d8551862e"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("b0367265-16b4-52d6-3ac6-ccc4b33d19a8"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("b4435402-3a74-077a-e1d3-5032a6edcf38"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("c4a093bf-f81d-5b3e-e389-9eb4950c566c"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("c9ce6e48-2658-5935-7265-c011ba95289a"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("cfa8f7dc-45b5-d440-4b57-c34f58d4a4d5"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("26c37705-e799-8708-119b-1227908d5e0f"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("d91817a8-88ec-93fc-9bc9-6942645adcff"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("dc1eaea8-b142-2f3f-a15c-d1ff5ce8d00a"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("dd47ee1a-bb03-d9a5-4b3a-3cd487c7cdfb"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("e05f8f93-f979-12d3-ace5-3f69f918ec1c"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("e0aab112-f4c5-b21d-6102-9467c0a95550"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("e4f6a991-75a9-d2a2-4216-d74fbd34c58f"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("e5064514-d384-8f16-e12e-4c203da968af"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("e788746d-af16-703a-d04b-8bf390e27424"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("ebd5a713-4f24-4650-20a4-abf789301415"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("ed5fd5a7-91de-64c1-0b9a-7236cc964595"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("f2d23aba-aa01-573a-a48c-dc52f57a35ab"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L },
                    { new Guid("fe374d1c-2cf7-702a-70c1-464e2ed31f34"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system-migration-rev866c1", new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "Active", "Not Created", "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL", null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CreatedAt",
                schema: "advance",
                table: "audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Module_EntityName_EntityId",
                schema: "advance",
                table: "audit_logs",
                columns: new[] { "Module", "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_CommercialComparisonId_VendorQu~",
                schema: "advance",
                table: "commercial_comparison_lines",
                columns: new[] { "CommercialComparisonId", "VendorQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_VendorId",
                schema: "advance",
                table: "commercial_comparison_lines",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparison_lines_VendorQuotationLineId",
                schema: "advance",
                table: "commercial_comparison_lines",
                column: "VendorQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OrganizationId_ComparisonNumber",
                schema: "advance",
                table: "commercial_comparisons",
                columns: new[] { "OrganizationId", "ComparisonNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OrganizationId_IdempotencyKey",
                schema: "advance",
                table: "commercial_comparisons",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_OwnerEmployeeId",
                schema: "advance",
                table: "commercial_comparisons",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_RecommendedVendorQuotationId",
                schema: "advance",
                table: "commercial_comparisons",
                column: "RecommendedVendorQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_RequestForQuotationId",
                schema: "advance",
                table: "commercial_comparisons",
                column: "RequestForQuotationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commercial_comparisons_SelectedVendorId",
                schema: "advance",
                table: "commercial_comparisons",
                column: "SelectedVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_configuration_histories_CorrelationId",
                schema: "advance",
                table: "controlled_configuration_histories",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_configuration_histories_EntityType_EntityId_Crea~",
                schema: "advance",
                table: "controlled_configuration_histories",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_CustomerId_AddressType_SiteName",
                schema: "advance",
                table: "customer_addresses",
                columns: new[] { "CustomerId", "AddressType", "SiteName" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_contacts_CustomerId_Email",
                schema: "advance",
                table: "customer_contacts",
                columns: new[] { "CustomerId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_CustomerCode",
                schema: "advance",
                table: "customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_GstNumber",
                schema: "advance",
                table: "customers",
                column: "GstNumber",
                unique: true,
                filter: "\"GstNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsActive",
                schema: "advance",
                table: "customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PanNumber_LegalCustomerName",
                schema: "advance",
                table: "customers",
                columns: new[] { "PanNumber", "LegalCustomerName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PortalOrganizationId",
                schema: "advance",
                table: "customers",
                column: "PortalOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Status",
                schema: "advance",
                table: "customers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_AlternateApproverEmployeeId",
                schema: "advance",
                table: "department_approval_mappings",
                column: "AlternateApproverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod~1",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCode~",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_PrimaryApproverEmployeeId",
                schema: "advance",
                table: "department_approval_mappings",
                column: "PrimaryApproverEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                schema: "advance",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_designations_Code",
                schema: "advance",
                table: "designations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_approval_history_EmployeeId_CreatedAt",
                schema: "advance",
                table: "employee_approval_history",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_history_CorrelationId",
                schema: "advance",
                table: "employee_department_history",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_history_EmployeeId_CreatedAt",
                schema: "advance",
                table: "employee_department_history",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_history_NewDepartmentId",
                schema: "advance",
                table: "employee_department_history",
                column: "NewDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_history_PreviousDepartmentId",
                schema: "advance",
                table: "employee_department_history",
                column: "PreviousDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_EmployeeId",
                schema: "advance",
                table: "employee_identity_mappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_Issuer_Subject_IsActive",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "Issuer", "Subject", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_OrganizationId_EmployeeId_Identi~",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "OrganizationId", "EmployeeId", "IdentityType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IdentityType\" = 'HUMAN'");

            migrationBuilder.CreateIndex(
                name: "IX_employee_import_history_EmployeeId",
                schema: "advance",
                table: "employee_import_history",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_import_history_ImportBatch_SourceEmployeeCode",
                schema: "advance",
                table: "employee_import_history",
                columns: new[] { "ImportBatch", "SourceEmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_DepartmentId",
                schema: "advance",
                table: "employee_operational_scopes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_EmployeeId",
                schema: "advance",
                table: "employee_operational_scopes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~",
                schema: "advance",
                table: "employee_operational_scopes",
                columns: new[] { "OrganizationId", "EmployeeId", "DepartmentId", "WarehouseId", "RackBinId", "OwnRecordsOnly", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_WarehouseId_RackBinId",
                schema: "advance",
                table: "employee_operational_scopes",
                columns: new[] { "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_EmployeeId_RoleId_EffectiveFrom",
                schema: "advance",
                table: "employee_role_assignments",
                columns: new[] { "EmployeeId", "RoleId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_RoleId",
                schema: "advance",
                table: "employee_role_assignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_EmployeeId_SkillId",
                schema: "advance",
                table: "employee_skills",
                columns: new[] { "EmployeeId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_SkillId",
                schema: "advance",
                table: "employee_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_status_history_EmployeeId_CreatedAt",
                schema: "advance",
                table: "employee_status_history",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_DepartmentId",
                schema: "advance",
                table: "employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_DesignationId",
                schema: "advance",
                table: "employees",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeCode",
                schema: "advance",
                table: "employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_PayrollEmployeeId",
                schema: "advance",
                table: "employees",
                column: "PayrollEmployeeId",
                unique: true,
                filter: "\"PayrollEmployeeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_Code",
                schema: "advance",
                table: "item_categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_subcategories_CategoryId_Code",
                schema: "advance",
                table: "item_subcategories",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_Barcode",
                schema: "advance",
                table: "items",
                column: "Barcode",
                unique: true,
                filter: "\"Barcode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_items_BaseUomId",
                schema: "advance",
                table: "items",
                column: "BaseUomId");

            migrationBuilder.CreateIndex(
                name: "IX_items_CategoryId",
                schema: "advance",
                table: "items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_items_IsActive",
                schema: "advance",
                table: "items",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_items_ItemCode",
                schema: "advance",
                table: "items",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_ManufacturerId",
                schema: "advance",
                table: "items",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_items_Name_ManufacturerMake_Model_PartNumber",
                schema: "advance",
                table: "items",
                columns: new[] { "Name", "ManufacturerMake", "Model", "PartNumber" },
                unique: true,
                filter: "\"PartNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_items_PreferredVendorId",
                schema: "advance",
                table: "items",
                column: "PreferredVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_items_Status",
                schema: "advance",
                table: "items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_items_SubcategoryId",
                schema: "advance",
                table: "items",
                column: "SubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_items_UomId",
                schema: "advance",
                table: "items",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_Code",
                schema: "advance",
                table: "manufacturers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_approval_history_MasterType_MasterId_CreatedAt",
                schema: "advance",
                table: "master_approval_history",
                columns: new[] { "MasterType", "MasterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_master_attachment_metadata_MasterType_MasterId",
                schema: "advance",
                table: "master_attachment_metadata",
                columns: new[] { "MasterType", "MasterId" });

            migrationBuilder.CreateIndex(
                name: "IX_master_status_history_MasterType_MasterId_CreatedAt",
                schema: "advance",
                table: "master_status_history",
                columns: new[] { "MasterType", "MasterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_HandoffNumber",
                schema: "advance",
                table: "material_followup_handoffs",
                column: "HandoffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_PurchaseOrderId",
                schema: "advance",
                table: "material_followup_handoffs",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_material_followup_handoffs_PurchaseOrderLineId",
                schema: "advance",
                table: "material_followup_handoffs",
                column: "PurchaseOrderLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~",
                schema: "advance",
                table: "organization_policies",
                columns: new[] { "OrganizationId", "PolicyCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_page_definitions_PageKey",
                schema: "advance",
                table: "page_definitions",
                column: "PageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_route_settings_RouteCode",
                schema: "advance",
                table: "purchase_approval_route_settings",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_IsActive",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "RouteCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_StepNumber_Effec~",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "RouteCode", "StepNumber", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_number_sequences_OrganizationId_FinancialYear_Pref~",
                schema: "advance",
                table: "purchase_number_sequences",
                columns: new[] { "OrganizationId", "FinancialYear", "Prefix" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_ActorEmployeeId",
                schema: "advance",
                table: "purchase_order_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_CorrelationId",
                schema: "advance",
                table: "purchase_order_history",
                columns: new[] { "PurchaseOrderId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_CreatedAt",
                schema: "advance",
                table: "purchase_order_history",
                columns: new[] { "PurchaseOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_CommercialComparisonLineId",
                schema: "advance",
                table: "purchase_order_lines",
                column: "CommercialComparisonLineId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_ItemId",
                schema: "advance",
                table: "purchase_order_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_CommercialComparisonLi~",
                schema: "advance",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "CommercialComparisonLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId_LineNumber",
                schema: "advance",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseOrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseRequirementHandoffId",
                schema: "advance",
                table: "purchase_order_lines",
                column: "PurchaseRequirementHandoffId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseRequisitionLineId_PurchaseOrde~",
                schema: "advance",
                table: "purchase_order_lines",
                columns: new[] { "PurchaseRequisitionLineId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CommercialComparisonId_RevisionNumber",
                schema: "advance",
                table: "purchase_orders",
                columns: new[] { "CommercialComparisonId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_DeliveryWarehouseId",
                schema: "advance",
                table: "purchase_orders",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_IdempotencyKey",
                schema: "advance",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OrganizationId_PoNumber_RevisionNumber",
                schema: "advance",
                table: "purchase_orders",
                columns: new[] { "OrganizationId", "PoNumber", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_OwnerEmployeeId",
                schema: "advance",
                table: "purchase_orders",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PreviousVersionId",
                schema: "advance",
                table: "purchase_orders",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RequestingDepartmentId",
                schema: "advance",
                table: "purchase_orders",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RootPurchaseOrderId_IsCurrentVersion",
                schema: "advance",
                table: "purchase_orders",
                columns: new[] { "RootPurchaseOrderId", "IsCurrentVersion" },
                unique: true,
                filter: "\"IsCurrentVersion\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_RootPurchaseOrderId_RevisionNumber",
                schema: "advance",
                table: "purchase_orders",
                columns: new[] { "RootPurchaseOrderId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_VendorId",
                schema: "advance",
                table: "purchase_orders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_HandoffNumber",
                schema: "advance",
                table: "purchase_requirement_handoffs",
                column: "HandoffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_PurchaseRequisitionId",
                schema: "advance",
                table: "purchase_requirement_handoffs",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_PurchaseRequisitionLineId_Sta~",
                schema: "advance",
                table: "purchase_requirement_handoffs",
                columns: new[] { "PurchaseRequisitionLineId", "Status" },
                unique: true,
                filter: "\"Status\" = 'PendingRFQ'");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_RackBinId",
                schema: "advance",
                table: "purchase_requirement_handoffs",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requirement_handoffs_WarehouseId",
                schema: "advance",
                table: "purchase_requirement_handoffs",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_approval_history_PurchaseRequisitionId~",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                columns: new[] { "PurchaseRequisitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_attachments_PurchaseRequisitionId_Stor~",
                schema: "advance",
                table: "purchase_requisition_attachments",
                columns: new[] { "PurchaseRequisitionId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_ItemId",
                schema: "advance",
                table: "purchase_requisition_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_PreferredWarehouseId",
                schema: "advance",
                table: "purchase_requisition_lines",
                column: "PreferredWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_lines_PurchaseRequisitionId_LineNumber",
                schema: "advance",
                table: "purchase_requisition_lines",
                columns: new[] { "PurchaseRequisitionId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_status_history_PurchaseRequisitionId_C~",
                schema: "advance",
                table: "purchase_requisition_status_history",
                columns: new[] { "PurchaseRequisitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_DeliveryWarehouseId",
                schema: "advance",
                table: "purchase_requisitions",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen~",
                schema: "advance",
                table: "purchase_requisitions",
                columns: new[] { "OrganizationId", "FinancialYear", "PrSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_OrganizationId_Status",
                schema: "advance",
                table: "purchase_requisitions",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_PrNumber",
                schema: "advance",
                table: "purchase_requisitions",
                column: "PrNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequesterEmployeeId",
                schema: "advance",
                table: "purchase_requisitions",
                column: "RequesterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequestingDepartmentId",
                schema: "advance",
                table: "purchase_requisitions",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_RequiredByDate",
                schema: "advance",
                table: "purchase_requisitions",
                column: "RequiredByDate");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_ActorEmployeeId",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparison~1",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                columns: new[] { "CommercialComparisonId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparisonI~",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                columns: new[] { "CommercialComparisonId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_policies_OrganizationId_Route~",
                schema: "advance",
                table: "purchase_transaction_approval_policies",
                columns: new[] { "OrganizationId", "RouteCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_ActorEmployeeId",
                schema: "advance",
                table: "purchase_transaction_status_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_EntityType_EntityId_Cor~",
                schema: "advance",
                table: "purchase_transaction_status_history",
                columns: new[] { "EntityType", "EntityId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_status_history_EntityType_EntityId_Cre~",
                schema: "advance",
                table: "purchase_transaction_status_history",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_ItemCategoryId",
                schema: "advance",
                table: "qc_inspection_policies",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_ItemId",
                schema: "advance",
                table: "qc_inspection_policies",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_MeasurementUomId",
                schema: "advance",
                table: "qc_inspection_policies",
                column: "MeasurementUomId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~",
                schema: "advance",
                table: "qc_inspection_policies",
                columns: new[] { "OrganizationId", "ItemId", "ItemCategoryId", "ParameterCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_technical_verifications_VendorQuotationLineId",
                schema: "advance",
                table: "quotation_technical_verifications",
                column: "VendorQuotationLineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotation_technical_verifications_VerifierEmployeeId",
                schema: "advance",
                table: "quotation_technical_verifications",
                column: "VerifierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_IsActive",
                schema: "advance",
                table: "rack_bins",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_Status",
                schema: "advance",
                table: "rack_bins",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_WarehouseId_BinCode",
                schema: "advance",
                table: "rack_bins",
                columns: new[] { "WarehouseId", "BinCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_DepartmentHeadEmployeeId",
                schema: "advance",
                table: "reporting_relationships",
                column: "DepartmentHeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_EmployeeId_EffectiveFrom",
                schema: "advance",
                table: "reporting_relationships",
                columns: new[] { "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_ReportingManagerEmployeeId",
                schema: "advance",
                table: "reporting_relationships",
                column: "ReportingManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_ItemId",
                schema: "advance",
                table: "request_for_quotation_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_PurchaseRequirementHandoffId",
                schema: "advance",
                table: "request_for_quotation_lines",
                column: "PurchaseRequirementHandoffId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_PurchaseRequisitionLineId",
                schema: "advance",
                table: "request_for_quotation_lines",
                column: "PurchaseRequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_RequestForQuotationId_LineNumber",
                schema: "advance",
                table: "request_for_quotation_lines",
                columns: new[] { "RequestForQuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotation_lines_RequestForQuotationId_PurchaseR~",
                schema: "advance",
                table: "request_for_quotation_lines",
                columns: new[] { "RequestForQuotationId", "PurchaseRequirementHandoffId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_DeliveryWarehouseId",
                schema: "advance",
                table: "request_for_quotations",
                column: "DeliveryWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_FinancialYear_Sequenc~",
                schema: "advance",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "FinancialYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_IdempotencyKey",
                schema: "advance",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OrganizationId_RfqNumber",
                schema: "advance",
                table: "request_for_quotations",
                columns: new[] { "OrganizationId", "RfqNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_OwnerEmployeeId",
                schema: "advance",
                table: "request_for_quotations",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_PurchaseRequisitionId_Status",
                schema: "advance",
                table: "request_for_quotations",
                columns: new[] { "PurchaseRequisitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_request_for_quotations_RequestingDepartmentId",
                schema: "advance",
                table: "request_for_quotations",
                column: "RequestingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_RequestForQuotationId_IdempotencyKey",
                schema: "advance",
                table: "rfq_vendor_invitations",
                columns: new[] { "RequestForQuotationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_RequestForQuotationId_VendorId",
                schema: "advance",
                table: "rfq_vendor_invitations",
                columns: new[] { "RequestForQuotationId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_vendor_invitations_VendorId",
                schema: "advance",
                table: "rfq_vendor_invitations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_role_page_permissions_PageDefinitionId",
                schema: "advance",
                table: "role_page_permissions",
                column: "PageDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_page_permissions_RoleId_PageDefinitionId",
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "RoleId", "PageDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                schema: "advance",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skills_Code",
                schema: "advance",
                table: "skills",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_PurchaseRequisitionLineId_Wa~",
                schema: "advance",
                table: "stock_availability_check_lines",
                columns: new[] { "PurchaseRequisitionLineId", "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_RackBinId",
                schema: "advance",
                table: "stock_availability_check_lines",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur~",
                schema: "advance",
                table: "stock_availability_check_lines",
                columns: new[] { "StockAvailabilityCheckId", "PurchaseRequisitionLineId", "LocationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_check_lines_WarehouseId",
                schema: "advance",
                table: "stock_availability_check_lines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_checks_CheckNumber",
                schema: "advance",
                table: "stock_availability_checks",
                column: "CheckNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_availability_checks_PurchaseRequisitionId",
                schema: "advance",
                table: "stock_availability_checks",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ItemId_PostingDate",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "ItemId", "PostingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_RackBinId",
                schema: "advance",
                table: "stock_movements",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ReferenceType_ReferenceNumber",
                schema: "advance",
                table: "stock_movements",
                columns: new[] { "ReferenceType", "ReferenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_WarehouseId",
                schema: "advance",
                table: "stock_movements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservation_history_StockReservationId_CreatedAt",
                schema: "advance",
                table: "stock_reservation_history",
                columns: new[] { "StockReservationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ItemId_WarehouseId_RackBinId_Status",
                schema: "advance",
                table: "stock_reservations",
                columns: new[] { "ItemId", "WarehouseId", "RackBinId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionId",
                schema: "advance",
                table: "stock_reservations",
                column: "PurchaseRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St~",
                schema: "advance",
                table: "stock_reservations",
                columns: new[] { "PurchaseRequisitionLineId", "LocationKey", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_RackBinId",
                schema: "advance",
                table: "stock_reservations",
                column: "RackBinId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_ReservationNumber",
                schema: "advance",
                table: "stock_reservations",
                column: "ReservationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_WarehouseId",
                schema: "advance",
                table: "stock_reservations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~",
                schema: "advance",
                table: "tax_gst_settings",
                columns: new[] { "OrganizationId", "JurisdictionCode", "HsnSacCode", "SupplierStateCode", "PlaceOfSupplyStateCode", "VendorRegistrationType", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_FromUomId",
                schema: "advance",
                table: "uom_conversions",
                column: "FromUomId");

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~",
                schema: "advance",
                table: "uom_conversions",
                columns: new[] { "OrganizationId", "FromUomId", "ToUomId", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_ToUomId",
                schema: "advance",
                table: "uom_conversions",
                column: "ToUomId");

            migrationBuilder.CreateIndex(
                name: "IX_uoms_Code",
                schema: "advance",
                table: "uoms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_Email",
                schema: "advance",
                table: "user_accounts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_LoginId",
                schema: "advance",
                table: "user_accounts",
                column: "LoginId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_RoleId",
                schema: "advance",
                table: "user_accounts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_addresses_VendorId_AddressType",
                schema: "advance",
                table: "vendor_addresses",
                columns: new[] { "VendorId", "AddressType" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_categories_Code",
                schema: "advance",
                table: "vendor_categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_contacts_VendorId_Email",
                schema: "advance",
                table: "vendor_contacts",
                columns: new[] { "VendorId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_ApprovedByEmployeeId",
                schema: "advance",
                table: "vendor_qualifications",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_ItemCategoryId",
                schema: "advance",
                table: "vendor_qualifications",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~",
                schema: "advance",
                table: "vendor_qualifications",
                columns: new[] { "OrganizationId", "VendorId", "ItemCategoryId", "QualificationCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_VendorId",
                schema: "advance",
                table: "vendor_qualifications",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_VerifiedByEmployeeId",
                schema: "advance",
                table: "vendor_qualifications",
                column: "VerifiedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_RequestForQuotationLineId",
                schema: "advance",
                table: "vendor_quotation_lines",
                column: "RequestForQuotationLineId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_TaxGstSettingId",
                schema: "advance",
                table: "vendor_quotation_lines",
                column: "TaxGstSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_VendorQuotationId_LineNumber",
                schema: "advance",
                table: "vendor_quotation_lines",
                columns: new[] { "VendorQuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotation_lines_VendorQuotationId_RequestForQuotatio~",
                schema: "advance",
                table: "vendor_quotation_lines",
                columns: new[] { "VendorQuotationId", "RequestForQuotationLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_LateAuthorizedByEmployeeId",
                schema: "advance",
                table: "vendor_quotations",
                column: "LateAuthorizedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_OrganizationId_IdempotencyKey",
                schema: "advance",
                table: "vendor_quotations",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_OrganizationId_QuotationNumber",
                schema: "advance",
                table: "vendor_quotations",
                columns: new[] { "OrganizationId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_PreviousRevisionId",
                schema: "advance",
                table: "vendor_quotations",
                column: "PreviousRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_RfqVendorInvitationId_RevisionNumber",
                schema: "advance",
                table: "vendor_quotations",
                columns: new[] { "RfqVendorInvitationId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_RootQuotationId_IsCurrentRevision",
                schema: "advance",
                table: "vendor_quotations",
                columns: new[] { "RootQuotationId", "IsCurrentRevision" },
                unique: true,
                filter: "\"IsCurrentRevision\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_quotations_VendorId",
                schema: "advance",
                table: "vendor_quotations",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_GstNumber",
                schema: "advance",
                table: "vendors",
                column: "GstNumber",
                unique: true,
                filter: "\"GstNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_IsActive",
                schema: "advance",
                table: "vendors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PanNumber_LegalVendorName",
                schema: "advance",
                table: "vendors",
                columns: new[] { "PanNumber", "LegalVendorName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PortalOrganizationId",
                schema: "advance",
                table: "vendors",
                column: "PortalOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_VendorCode",
                schema: "advance",
                table: "vendors",
                column: "VendorCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendors_VendorStatus",
                schema: "advance",
                table: "vendors",
                column: "VendorStatus");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~",
                schema: "advance",
                table: "warehouse_condition_locations",
                columns: new[] { "OrganizationId", "WarehouseId", "RackBinId", "ConditionCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_condition_locations_WarehouseId_RackBinId",
                schema: "advance",
                table: "warehouse_condition_locations",
                columns: new[] { "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_DepartmentId",
                schema: "advance",
                table: "warehouses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_IsActive",
                schema: "advance",
                table: "warehouses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_ResponsibleEmployeeId",
                schema: "advance",
                table: "warehouses",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_Status",
                schema: "advance",
                table: "warehouses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_WarehouseCode",
                schema: "advance",
                table: "warehouses",
                column: "WarehouseCode",
                unique: true);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.InstallRev869A);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.InstallRev869B);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.Install);
            migrationBuilder.Sql(Rev869BDatabaseLifecycleSql.Install);
            migrationBuilder.Sql(Rev869BCommandContextSql.Install);
            migrationBuilder.Sql(Rev869BControlledMutationSql.Install);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Rev869BControlledMutationSql.Remove);
            migrationBuilder.Sql(Rev869BCommandContextSql.Remove);
            migrationBuilder.Sql(Rev869BDatabaseLifecycleSql.Remove);
            migrationBuilder.Sql(Rev869BDatabaseSafetySql.Remove);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.RemoveRev869B);
            migrationBuilder.Sql(AdvanceDatabaseContractSql.RemoveRev869A);
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "controlled_configuration_histories",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "customer_addresses",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "customer_contacts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "department_approval_mappings",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_approval_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_department_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_identity_mappings",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_import_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_operational_scopes",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_role_assignments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_skills",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employee_status_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "master_approval_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "master_attachment_metadata",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "master_status_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "material_followup_handoffs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "organization_policies",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_approval_route_settings",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_approval_workflow_steps",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_number_sequences",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_order_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requisition_approval_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requisition_attachments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requisition_status_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_transaction_approval_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_transaction_approval_policies",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_transaction_status_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "qc_inspection_policies",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "quotation_technical_verifications",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "reporting_relationships",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "role_page_permissions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stock_availability_check_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stock_reservation_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "uom_conversions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_addresses",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_categories",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_contacts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_qualifications",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "warehouse_condition_locations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "page_definitions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stock_availability_checks",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "stock_reservations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "commercial_comparison_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_quotation_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "commercial_comparisons",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "request_for_quotation_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "tax_gst_settings",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendor_quotations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requirement_handoffs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "rfq_vendor_invitations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requisition_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "rack_bins",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "request_for_quotations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "items",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "purchase_requisitions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "item_subcategories",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "uoms",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "vendors",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "item_categories",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "designations",
                schema: "advance");
        }
    }
}
