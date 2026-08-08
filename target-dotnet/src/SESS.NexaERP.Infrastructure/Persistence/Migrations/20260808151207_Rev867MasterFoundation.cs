using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev867MasterFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Location",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                schema: "nexa",
                table: "warehouses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultAcceptedLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultQcHoldLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultReceivingLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultRejectedLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultRepairableLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultScrapLocationId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWarehouseCodeLocked",
                schema: "nexa",
                table: "warehouses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WarehouseType",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "vendors",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                schema: "nexa",
                table: "vendors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "nexa",
                table: "vendors",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedMakes",
                schema: "nexa",
                table: "vendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentMetadataJson",
                schema: "nexa",
                table: "vendors",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankMetadataJson",
                schema: "nexa",
                table: "vendors",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                schema: "nexa",
                table: "vendors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                schema: "nexa",
                table: "vendors",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "nexa",
                table: "vendors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreditPeriodDays",
                schema: "nexa",
                table: "vendors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTerms",
                schema: "nexa",
                table: "vendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "nexa",
                table: "vendors",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVendorCodeLocked",
                schema: "nexa",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalVendorName",
                schema: "nexa",
                table: "vendors",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaterialServiceCategories",
                schema: "nexa",
                table: "vendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MsmeNumber",
                schema: "nexa",
                table: "vendors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MsmeStatus",
                schema: "nexa",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                schema: "nexa",
                table: "vendors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "nexa",
                table: "vendors",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "nexa",
                table: "vendors",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "nexa",
                table: "vendors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                schema: "nexa",
                table: "vendors",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                schema: "nexa",
                table: "vendors",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorStatus",
                schema: "nexa",
                table: "vendors",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VendorType",
                schema: "nexa",
                table: "vendors",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                schema: "nexa",
                table: "rack_bins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BinNameNumber",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CapacityQuantity",
                schema: "nexa",
                table: "rack_bins",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CapacityUom",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationType",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaterialCondition",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RackName",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                schema: "nexa",
                table: "rack_bins",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "items",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                schema: "nexa",
                table: "items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "nexa",
                table: "items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarcodeSymbology",
                schema: "nexa",
                table: "items",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BatchTracking",
                schema: "nexa",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailedDescription",
                schema: "nexa",
                table: "items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DrawingDocumentReference",
                schema: "nexa",
                table: "items",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GstPercentage",
                schema: "nexa",
                table: "items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HsnSacCode",
                schema: "nexa",
                table: "items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                schema: "nexa",
                table: "items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                schema: "nexa",
                table: "items",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsItemCodeLocked",
                schema: "nexa",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManufacturerMake",
                schema: "nexa",
                table: "items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                schema: "nexa",
                table: "items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumStock",
                schema: "nexa",
                table: "items",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                schema: "nexa",
                table: "items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartNumber",
                schema: "nexa",
                table: "items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredVendorId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "QcRequired",
                schema: "nexa",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderLevel",
                schema: "nexa",
                table: "items",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SerialNumberTracking",
                schema: "nexa",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShelfLifeTracking",
                schema: "nexa",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardEstimatedPrice",
                schema: "nexa",
                table: "items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "nexa",
                table: "items",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SubcategoryId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalSpecification",
                schema: "nexa",
                table: "items",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UomId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "customers",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                schema: "nexa",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                schema: "nexa",
                table: "customers",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                schema: "nexa",
                table: "customers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                schema: "nexa",
                table: "customers",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "nexa",
                table: "customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                schema: "nexa",
                table: "customers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditPeriodDays",
                schema: "nexa",
                table: "customers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                schema: "nexa",
                table: "customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "nexa",
                table: "customers",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                schema: "nexa",
                table: "customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomerCodeLocked",
                schema: "nexa",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalCustomerName",
                schema: "nexa",
                table: "customers",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                schema: "nexa",
                table: "customers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "nexa",
                table: "customers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalOrganizationId",
                schema: "nexa",
                table: "customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "nexa",
                table: "customers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "nexa",
                table: "customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                schema: "nexa",
                table: "customers",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "nexa",
                table: "customers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TradeName",
                schema: "nexa",
                table: "customers",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_contacts",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_categories",
                schema: "nexa",
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
                schema: "nexa",
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
                schema: "nexa",
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
                schema: "nexa",
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
                schema: "nexa",
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
                name: "uoms",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                name: "vendor_addresses",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vendor_categories",
                schema: "nexa",
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
                name: "vendor_contacts",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_subcategories",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000019"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.items", "/masters/items", "Item Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.warehouses", "/masters/warehouses", "Warehouse/Store Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Masters", "masters.rack-bins", "/masters/rack-bins", "Rack/Bin Location Master", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01ddbcb6-21be-e7ce-a93e-6fb7bcc0dc53"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("0427700a-1559-bcd0-9af2-ef7a1afd7c50"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("09119528-8023-d53b-94dd-5a4e94862289"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("09cc0de9-fcac-63a6-1a66-aae0fa144ee7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("0d663ce6-3756-5828-aae7-321d6f53031d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0d855678-6da1-cb30-a345-23fe101560e0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("11e503aa-3973-c88f-2ccb-2882053ecd4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("173a3c08-7d51-8ee8-3a79-be3f114054fe"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("1c07c5ed-49f6-154a-9758-25e8a2b63caa"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("1c8246c6-9ae9-dfb6-ce0b-89ff476ecf5b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("1d1f1ae5-049b-7b9d-a8db-6b57f30fe06e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2104d4b3-1c47-615b-d775-d6ed6d26d6f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("224f017d-4912-db64-8cc7-19dd85240627"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("22b85f68-8b26-74c0-3b15-e3f36f7578f9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("26aa6d9c-d96e-dc3f-6b21-0a17ff28b343"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("26cd7f8a-1db4-14d9-a2d5-c813e94d4fa7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("28177576-ff93-fe25-a79f-efa99761ecdc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("2922dd56-7a8b-d61d-e3c8-a4362fe51f6b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2c4484af-62c5-f940-0473-6eaac232a8da"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2ecaed61-b739-eebc-7868-76b121d814d5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2ef7bd70-86b3-86d2-0400-3e1100f2e1ec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("37812dfa-a30c-3dc5-75ad-f8297af6eda2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("3d7afcc0-41fa-e29f-27de-f593772064e3"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("42d7c80f-feaf-f785-9890-798d4a402c04"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("434b0f73-2414-966c-9085-793eb852a0f9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("4f80fa3e-f5a1-a206-f045-b159f38e7829"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("50b0e7ac-07c0-50cb-baef-759e3cdcbbe1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("55d18c37-aec1-33b9-bf7d-ccd4f2523552"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("5604f2d6-ed5c-69d0-06f2-59e976f3cf30"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("57484dbf-b071-772a-239b-2d6d99f176dc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("57b57863-6a78-462d-d8b2-78ac4b834960"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("57b9b346-1649-2e3a-9380-92ac4a170646"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("57cc58ff-43e9-e4c7-8dac-7b113001bb66"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("57f23a76-a3ae-d1f0-727d-023ec2d3405c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("5858bc94-2848-a8f6-450e-61b2978415f3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("58acf7bb-a03c-9d8e-6d34-ecc6556175d2"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("59b0049f-79d6-e5ad-ff4b-9e9f381680dd"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("5eb29b02-1be6-f40e-39c8-d8bcadb1c47f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("662a625c-2eec-fe6b-6fdf-f532f9296bb4"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("676f980f-4d6f-e435-aef6-0c87dae4e732"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("6a5e4f85-3fb4-2abb-06c6-400f9eb2d1a7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("6c0b5039-c28f-eca9-bd5c-e5d22ea7e4f0"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("7047e7ed-4bf6-ff5d-a169-15c158798b53"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("744ea3db-88eb-9785-159e-5451b60d5867"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("7529a244-b0c1-d54c-0927-20ee8cd5103f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("7579fd23-0432-fad2-27a7-68cf4b4de9d5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("7903d6b2-ebd5-6618-4419-00a95e4038fc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("7a4fdd8f-31f7-74c4-f2c1-2b809ea6b560"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("7a7b5c71-785d-ba9b-202b-70bef66186a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("7ab6b792-c208-a589-d1b6-e53cae958501"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("7c977b1a-efa8-9f8e-ea4c-b9f5f49cd501"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("7d2fd80d-69aa-38a2-a4cd-5485445c1c57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("806fbb91-1a9c-6ead-f554-0f4067475507"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("86e3a97a-e881-d39b-73eb-ca20c5269ad9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("893e4f6a-c815-bf16-68b0-10ab980658ba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("895d196a-bd9f-772e-b1d7-bf1d6597f8fd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("8b925bc6-c4df-58c7-b80a-91cfc1f9eb57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("8d1325d9-fab1-0ca4-e18b-910b17c9c6e9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("93bb9d5f-2437-7632-81a0-10c147c3ab6b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("97303be4-30aa-2f7a-5671-93dea675bfe2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9cbd157f-60ff-5944-c775-cf75d19df967"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("9e31d353-1832-55c2-cac9-5d4c59b737fc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("a0134759-e6fc-c8f9-c2a3-a6be519c09b2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("a146ffc4-f735-32f7-f26e-58e1811fdba9"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a2a7ccfd-40d7-23ec-1639-1c56a75e65bd"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("a7f0df0c-acbd-17c4-e155-2d6edde94407"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a9c208a9-da86-9948-f87a-205b717e7b44"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("ab3976ac-00d8-41a4-dea2-0ab6fbe4d665"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ab45313d-9bf4-7077-cf2f-1705713378ea"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("ac9d4a3e-5653-900f-bb7f-25e3a77ec854"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ad74412e-c99a-6021-70ea-015ea7e30a1a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ad935742-0a03-120f-58a7-8fa3f25ef45b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("ae1c2833-99c6-0573-538f-548f63f9dd40"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("afc91ac3-6ddb-e930-00d0-3fee04d9b282"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b74b64b2-b794-9c7e-3735-62602c9ab52a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("b77d61a1-a408-7d53-fbfa-7980154414d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("ba28500b-fd12-606f-725c-5a51f7e02b83"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("bb59b2f8-0de6-fa7d-ec97-9a3346ebb6dd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("bdfdf9dd-3718-cd00-0c8b-7fd51a40a37d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("bf01b0ed-7161-1a84-feba-a460b92acc03"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("bf084386-2e2b-1be9-0594-da89a7b8b2c2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("c2358983-d1a1-5201-8190-b89074e78dba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("c40b0ab4-db07-30ab-75c6-a536129f1e42"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("c446d1f1-0a95-f1df-55f1-9863aa2b7cf9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c4db49dd-b10b-6b4d-e275-cb271ba08596"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c883528c-d9d8-8aff-6f8c-5f7db5965311"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("c8f80905-5eb2-45ad-5c10-c82dd3c55a60"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("c9aa1c7c-10fa-d283-94b8-b22438b46889"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("cadbaf89-189c-cf4d-ed63-c74c0248fcbc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("cbf9aef0-76bd-f95d-16b6-105ae20f5e7a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("cbfc7fcf-65b2-e113-7387-39d168be58a1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("d08f0a07-fcc7-9142-f5e2-bc12298b391e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("d098e311-bddb-41a7-6e51-4b8dacecba54"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d2021f2e-6e95-1a51-5a7b-0e09ee34d0ef"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("d2c65f64-d1d1-551b-266b-9ada62ece036"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d5036085-0b51-c015-e3cc-020a497306c5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d7180a5c-bc97-fdb6-a2e2-1b98dd103231"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("dde38067-157b-9e03-a3cd-d782f294ea4c"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("e01fc194-c8a5-1678-23b2-39e1af347b2b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("e074d56d-394c-076d-baeb-d765172ed9b8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("e113bce8-cea3-ca48-8fdb-2bfcf0c3b7e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("e22cdbb3-b206-edf9-39c7-4c5fba7d2e59"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("e3d01b8e-3ddd-b43e-3ca1-1a1accf5d48a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("e3e74f28-4eb0-bae9-2648-3731de092f4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ecb616be-7722-4c49-faeb-040aa1982c54"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("ef05e0b9-c4f6-3f54-337f-ce59ae194851"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("f3722c30-f24c-70a8-9b5f-d1bb217b1a6c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("f3a54bbe-51a4-b36f-11cc-81fbf1c263e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("f4322806-498f-c746-da45-48a867b7e7a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("f590c363-4b5c-ef51-b90c-f0231c1c0b1d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000021"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("f6833c5e-20d7-0207-8c20-1c0be53d39e1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("f6bb9a99-8562-5a79-1e09-e39b78c55e4f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000019"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f93bfe0b-1a54-9e7c-0a9f-70a4b60b7e95"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f9bcd5c7-6f01-2f0a-5b36-f66860c6b37a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000020"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_DepartmentId",
                schema: "nexa",
                table: "warehouses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_IsActive",
                schema: "nexa",
                table: "warehouses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses",
                column: "ResponsibleEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_Status",
                schema: "nexa",
                table: "warehouses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_IsActive",
                schema: "nexa",
                table: "vendors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PanNumber_LegalVendorName",
                schema: "nexa",
                table: "vendors",
                columns: new[] { "PanNumber", "LegalVendorName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_VendorStatus",
                schema: "nexa",
                table: "vendors",
                column: "VendorStatus");

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_IsActive",
                schema: "nexa",
                table: "rack_bins",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_rack_bins_Status",
                schema: "nexa",
                table: "rack_bins",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_items_CategoryId",
                schema: "nexa",
                table: "items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_items_IsActive",
                schema: "nexa",
                table: "items",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_items_ManufacturerId",
                schema: "nexa",
                table: "items",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_items_Name_ManufacturerMake_Model_PartNumber",
                schema: "nexa",
                table: "items",
                columns: new[] { "Name", "ManufacturerMake", "Model", "PartNumber" },
                unique: true,
                filter: "\"PartNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_items_PreferredVendorId",
                schema: "nexa",
                table: "items",
                column: "PreferredVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_items_Status",
                schema: "nexa",
                table: "items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_items_SubcategoryId",
                schema: "nexa",
                table: "items",
                column: "SubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_items_UomId",
                schema: "nexa",
                table: "items",
                column: "UomId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_gst_valid",
                schema: "nexa",
                table: "items",
                sql: "\"GstPercentage\" >= 0 AND \"GstPercentage\" <= 28");

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_maximum_stock_valid",
                schema: "nexa",
                table: "items",
                sql: "\"MaximumStock\" >= \"MinimumStock\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_minimum_stock_nonnegative",
                schema: "nexa",
                table: "items",
                sql: "\"MinimumStock\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_reorder_level_valid",
                schema: "nexa",
                table: "items",
                sql: "\"ReorderLevel\" >= 0 AND \"ReorderLevel\" <= \"MaximumStock\"");

            migrationBuilder.CreateIndex(
                name: "IX_customers_IsActive",
                schema: "nexa",
                table: "customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PanNumber_LegalCustomerName",
                schema: "nexa",
                table: "customers",
                columns: new[] { "PanNumber", "LegalCustomerName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PortalOrganizationId",
                schema: "nexa",
                table: "customers",
                column: "PortalOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Status",
                schema: "nexa",
                table: "customers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_CustomerId_AddressType_SiteName",
                schema: "nexa",
                table: "customer_addresses",
                columns: new[] { "CustomerId", "AddressType", "SiteName" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_contacts_CustomerId_Email",
                schema: "nexa",
                table: "customer_contacts",
                columns: new[] { "CustomerId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_item_categories_Code",
                schema: "nexa",
                table: "item_categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_subcategories_CategoryId_Code",
                schema: "nexa",
                table: "item_subcategories",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_Code",
                schema: "nexa",
                table: "manufacturers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_approval_history_MasterType_MasterId_CreatedAt",
                schema: "nexa",
                table: "master_approval_history",
                columns: new[] { "MasterType", "MasterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_master_attachment_metadata_MasterType_MasterId",
                schema: "nexa",
                table: "master_attachment_metadata",
                columns: new[] { "MasterType", "MasterId" });

            migrationBuilder.CreateIndex(
                name: "IX_master_status_history_MasterType_MasterId_CreatedAt",
                schema: "nexa",
                table: "master_status_history",
                columns: new[] { "MasterType", "MasterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_uoms_Code",
                schema: "nexa",
                table: "uoms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_addresses_VendorId_AddressType",
                schema: "nexa",
                table: "vendor_addresses",
                columns: new[] { "VendorId", "AddressType" });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_categories_Code",
                schema: "nexa",
                table: "vendor_categories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_contacts_VendorId_Email",
                schema: "nexa",
                table: "vendor_contacts",
                columns: new[] { "VendorId", "Email" });

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_categories_CategoryId",
                schema: "nexa",
                table: "items",
                column: "CategoryId",
                principalSchema: "nexa",
                principalTable: "item_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_item_subcategories_SubcategoryId",
                schema: "nexa",
                table: "items",
                column: "SubcategoryId",
                principalSchema: "nexa",
                principalTable: "item_subcategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_manufacturers_ManufacturerId",
                schema: "nexa",
                table: "items",
                column: "ManufacturerId",
                principalSchema: "nexa",
                principalTable: "manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_uoms_UomId",
                schema: "nexa",
                table: "items",
                column: "UomId",
                principalSchema: "nexa",
                principalTable: "uoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_items_vendors_PreferredVendorId",
                schema: "nexa",
                table: "items",
                column: "PreferredVendorId",
                principalSchema: "nexa",
                principalTable: "vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouses_departments_DepartmentId",
                schema: "nexa",
                table: "warehouses",
                column: "DepartmentId",
                principalSchema: "nexa",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouses_employees_ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses",
                column: "ResponsibleEmployeeId",
                principalSchema: "nexa",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_item_categories_CategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_item_subcategories_SubcategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_manufacturers_ManufacturerId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_uoms_UomId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_items_vendors_PreferredVendorId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouses_departments_DepartmentId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouses_employees_ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropTable(
                name: "customer_addresses",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "customer_contacts",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "item_subcategories",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "master_approval_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "master_attachment_metadata",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "master_status_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "uoms",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_addresses",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_categories",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_contacts",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "item_categories",
                schema: "nexa");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_DepartmentId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_IsActive",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_Status",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_vendors_IsActive",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_PanNumber_LegalVendorName",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_vendors_VendorStatus",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_rack_bins_IsActive",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropIndex(
                name: "IX_rack_bins_Status",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropIndex(
                name: "IX_items_CategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_IsActive",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_ManufacturerId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_Name_ManufacturerMake_Model_PartNumber",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_PreferredVendorId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_Status",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_SubcategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_UomId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_items_gst_valid",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_items_maximum_stock_valid",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_items_minimum_stock_nonnegative",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_items_reorder_level_valid",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_customers_IsActive",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_PanNumber_LegalCustomerName",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_PortalOrganizationId",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_Status",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01ddbcb6-21be-e7ce-a93e-6fb7bcc0dc53"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0427700a-1559-bcd0-9af2-ef7a1afd7c50"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("09119528-8023-d53b-94dd-5a4e94862289"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("09cc0de9-fcac-63a6-1a66-aae0fa144ee7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0d663ce6-3756-5828-aae7-321d6f53031d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0d855678-6da1-cb30-a345-23fe101560e0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("11e503aa-3973-c88f-2ccb-2882053ecd4d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("173a3c08-7d51-8ee8-3a79-be3f114054fe"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1c07c5ed-49f6-154a-9758-25e8a2b63caa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1c8246c6-9ae9-dfb6-ce0b-89ff476ecf5b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1d1f1ae5-049b-7b9d-a8db-6b57f30fe06e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2104d4b3-1c47-615b-d775-d6ed6d26d6f1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("224f017d-4912-db64-8cc7-19dd85240627"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("22b85f68-8b26-74c0-3b15-e3f36f7578f9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("26aa6d9c-d96e-dc3f-6b21-0a17ff28b343"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("26cd7f8a-1db4-14d9-a2d5-c813e94d4fa7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("28177576-ff93-fe25-a79f-efa99761ecdc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2922dd56-7a8b-d61d-e3c8-a4362fe51f6b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2c4484af-62c5-f940-0473-6eaac232a8da"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ecaed61-b739-eebc-7868-76b121d814d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ef7bd70-86b3-86d2-0400-3e1100f2e1ec"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("37812dfa-a30c-3dc5-75ad-f8297af6eda2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3d7afcc0-41fa-e29f-27de-f593772064e3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("42d7c80f-feaf-f785-9890-798d4a402c04"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("434b0f73-2414-966c-9085-793eb852a0f9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4f80fa3e-f5a1-a206-f045-b159f38e7829"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("50b0e7ac-07c0-50cb-baef-759e3cdcbbe1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("55d18c37-aec1-33b9-bf7d-ccd4f2523552"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5604f2d6-ed5c-69d0-06f2-59e976f3cf30"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57484dbf-b071-772a-239b-2d6d99f176dc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57b57863-6a78-462d-d8b2-78ac4b834960"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57b9b346-1649-2e3a-9380-92ac4a170646"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57cc58ff-43e9-e4c7-8dac-7b113001bb66"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57f23a76-a3ae-d1f0-727d-023ec2d3405c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5858bc94-2848-a8f6-450e-61b2978415f3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("58acf7bb-a03c-9d8e-6d34-ecc6556175d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("59b0049f-79d6-e5ad-ff4b-9e9f381680dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5eb29b02-1be6-f40e-39c8-d8bcadb1c47f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("662a625c-2eec-fe6b-6fdf-f532f9296bb4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("676f980f-4d6f-e435-aef6-0c87dae4e732"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a5e4f85-3fb4-2abb-06c6-400f9eb2d1a7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6c0b5039-c28f-eca9-bd5c-e5d22ea7e4f0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7047e7ed-4bf6-ff5d-a169-15c158798b53"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("744ea3db-88eb-9785-159e-5451b60d5867"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7529a244-b0c1-d54c-0927-20ee8cd5103f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7579fd23-0432-fad2-27a7-68cf4b4de9d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7903d6b2-ebd5-6618-4419-00a95e4038fc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a4fdd8f-31f7-74c4-f2c1-2b809ea6b560"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a7b5c71-785d-ba9b-202b-70bef66186a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7ab6b792-c208-a589-d1b6-e53cae958501"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7c977b1a-efa8-9f8e-ea4c-b9f5f49cd501"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7d2fd80d-69aa-38a2-a4cd-5485445c1c57"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("806fbb91-1a9c-6ead-f554-0f4067475507"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("86e3a97a-e881-d39b-73eb-ca20c5269ad9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("893e4f6a-c815-bf16-68b0-10ab980658ba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("895d196a-bd9f-772e-b1d7-bf1d6597f8fd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b925bc6-c4df-58c7-b80a-91cfc1f9eb57"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8d1325d9-fab1-0ca4-e18b-910b17c9c6e9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("93bb9d5f-2437-7632-81a0-10c147c3ab6b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("97303be4-30aa-2f7a-5671-93dea675bfe2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9cbd157f-60ff-5944-c775-cf75d19df967"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9e31d353-1832-55c2-cac9-5d4c59b737fc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a0134759-e6fc-c8f9-c2a3-a6be519c09b2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a146ffc4-f735-32f7-f26e-58e1811fdba9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a2a7ccfd-40d7-23ec-1639-1c56a75e65bd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a7f0df0c-acbd-17c4-e155-2d6edde94407"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a9c208a9-da86-9948-f87a-205b717e7b44"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab3976ac-00d8-41a4-dea2-0ab6fbe4d665"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab45313d-9bf4-7077-cf2f-1705713378ea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ac9d4a3e-5653-900f-bb7f-25e3a77ec854"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ad74412e-c99a-6021-70ea-015ea7e30a1a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ad935742-0a03-120f-58a7-8fa3f25ef45b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ae1c2833-99c6-0573-538f-548f63f9dd40"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("afc91ac3-6ddb-e930-00d0-3fee04d9b282"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b74b64b2-b794-9c7e-3735-62602c9ab52a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b77d61a1-a408-7d53-fbfa-7980154414d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ba28500b-fd12-606f-725c-5a51f7e02b83"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bb59b2f8-0de6-fa7d-ec97-9a3346ebb6dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bdfdf9dd-3718-cd00-0c8b-7fd51a40a37d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bf01b0ed-7161-1a84-feba-a460b92acc03"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bf084386-2e2b-1be9-0594-da89a7b8b2c2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c2358983-d1a1-5201-8190-b89074e78dba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c40b0ab4-db07-30ab-75c6-a536129f1e42"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c446d1f1-0a95-f1df-55f1-9863aa2b7cf9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c4db49dd-b10b-6b4d-e275-cb271ba08596"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c883528c-d9d8-8aff-6f8c-5f7db5965311"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c8f80905-5eb2-45ad-5c10-c82dd3c55a60"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c9aa1c7c-10fa-d283-94b8-b22438b46889"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cadbaf89-189c-cf4d-ed63-c74c0248fcbc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cbf9aef0-76bd-f95d-16b6-105ae20f5e7a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cbfc7fcf-65b2-e113-7387-39d168be58a1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d08f0a07-fcc7-9142-f5e2-bc12298b391e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d098e311-bddb-41a7-6e51-4b8dacecba54"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2021f2e-6e95-1a51-5a7b-0e09ee34d0ef"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2c65f64-d1d1-551b-266b-9ada62ece036"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5036085-0b51-c015-e3cc-020a497306c5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d7180a5c-bc97-fdb6-a2e2-1b98dd103231"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dde38067-157b-9e03-a3cd-d782f294ea4c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e01fc194-c8a5-1678-23b2-39e1af347b2b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e074d56d-394c-076d-baeb-d765172ed9b8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e113bce8-cea3-ca48-8fdb-2bfcf0c3b7e4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e22cdbb3-b206-edf9-39c7-4c5fba7d2e59"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e3d01b8e-3ddd-b43e-3ca1-1a1accf5d48a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e3e74f28-4eb0-bae9-2648-3731de092f4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ecb616be-7722-4c49-faeb-040aa1982c54"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ef05e0b9-c4f6-3f54-337f-ce59ae194851"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3722c30-f24c-70a8-9b5f-d1bb217b1a6c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3a54bbe-51a4-b36f-11cc-81fbf1c263e4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f4322806-498f-c746-da45-48a867b7e7a7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f590c363-4b5c-ef51-b90c-f0231c1c0b1d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f6833c5e-20d7-0207-8c20-1c0be53d39e1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f6bb9a99-8562-5a79-1e09-e39b78c55e4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f93bfe0b-1a54-9e7c-0a9f-70a4b60b7e95"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f9bcd5c7-6f01-2f0a-5b36-f66860c6b37a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000021"));

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultAcceptedLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultQcHoldLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultReceivingLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultRejectedLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultRepairableLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DefaultScrapLocationId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "IsWarehouseCodeLocked",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ResponsibleEmployeeId",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "WarehouseType",
                schema: "nexa",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "ApprovedMakes",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "AttachmentMetadataJson",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "BankMetadataJson",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "CreditPeriodDays",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "DeliveryTerms",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "IsVendorCodeLocked",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "LegalVendorName",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MaterialServiceCategories",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MsmeNumber",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MsmeStatus",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "StateCode",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "TradeName",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "VendorStatus",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "VendorType",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "Barcode",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "BinNameNumber",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "CapacityQuantity",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "CapacityUom",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "LocationType",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "MaterialCondition",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "RackName",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "Zone",
                schema: "nexa",
                table: "rack_bins");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "BarcodeSymbology",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "BatchTracking",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "DetailedDescription",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "DrawingDocumentReference",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "GstPercentage",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "HsnSacCode",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "IsItemCodeLocked",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ManufacturerMake",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "MaximumStock",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "Model",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "PartNumber",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "PreferredVendorId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "QcRequired",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "SerialNumberTracking",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ShelfLifeTracking",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "StandardEstimatedPrice",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "SubcategoryId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "TechnicalSpecification",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "UomId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreditPeriodDays",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Industry",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "IsCustomerCodeLocked",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "LegalCustomerName",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "PortalOrganizationId",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "StateCode",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "nexa",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "TradeName",
                schema: "nexa",
                table: "customers");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                schema: "nexa",
                table: "warehouses",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                schema: "nexa",
                table: "vendors",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);
        }
    }
}
