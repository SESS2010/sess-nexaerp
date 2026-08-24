using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev869AIdentityMasterScopeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $rev869a_role_reuse$
                DECLARE
                    department_manager_count integer;
                    department_manager_active_count integer;
                    department_manager_suitable_count integer;
                    new_role_collision_count integer;
                BEGIN
                    SELECT count(*), count(*) FILTER (WHERE "IsActive"), count(*) FILTER (
                        WHERE "Code" = 'DEPARTMENT_MANAGER'
                          AND "IsActive"
                          AND nullif(trim("Name"), '') IS NOT NULL
                          AND nullif(trim("CreatedBy"), '') IS NOT NULL
                          AND "CreatedBy" <> 'migration-rev869a'
                          AND "Id" NOT IN (
                            '30000000-0000-0000-0000-000000000001'::uuid,
                            '30000000-0000-0000-0000-000000000002'::uuid,
                            '30000000-0000-0000-0000-000000000003'::uuid,
                            '30000000-0000-0000-0000-000000000004'::uuid))
                    INTO department_manager_count, department_manager_active_count, department_manager_suitable_count
                    FROM nexa.roles
                    WHERE upper(trim("Code")) = 'DEPARTMENT_MANAGER';

                    SELECT count(*) INTO new_role_collision_count
                    FROM nexa.roles
                    WHERE upper(trim("Code")) IN ('PURCHASE_MANAGER', 'STORES_MANAGER', 'QC_MANAGER', 'QC_INSPECTOR');

                    IF department_manager_count <> 1 OR department_manager_active_count <> 1 OR department_manager_suitable_count <> 1 THEN
                        RAISE EXCEPTION 'REV869A requires exactly one active suitable pre-existing DEPARTMENT_MANAGER role';
                    END IF;
                    IF new_role_collision_count <> 0 THEN
                        RAISE EXCEPTION 'REV869A new-role collision detected';
                    END IF;
                END $rev869a_role_reuse$;
                """);

                        migrationBuilder.Sql("""
                CREATE TABLE nexa.rev869a_items_prechange_backup AS TABLE nexa.items;
                CREATE TABLE nexa.rev869a_uoms_prechange_backup AS TABLE nexa.uoms;
                CREATE TABLE nexa.rev869a_vendors_prechange_backup AS TABLE nexa.vendors;
                """);
migrationBuilder.AddColumn<string>(
                name: "CommercialVerificationStatus",
                schema: "nexa",
                table: "vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CommercialVerifiedAt",
                schema: "nexa",
                table: "vendors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialVerifiedBy",
                schema: "nexa",
                table: "vendors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                schema: "nexa",
                table: "vendors",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                schema: "nexa",
                table: "vendors",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReverification",
                schema: "nexa",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MeasurementDimension",
                schema: "nexa",
                table: "uoms",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QuantityPrecision",
                schema: "nexa",
                table: "uoms",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseUomId",
                schema: "nexa",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                DO $rev869a_uom_plan$
                DECLARE affected_item_count integer;
                BEGIN
                    IF EXISTS (SELECT 1 FROM nexa.uoms WHERE "Id" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) THEN RAISE EXCEPTION 'REV869A approved EA UOM Id collision'; END IF;
                    IF EXISTS (SELECT 1 FROM nexa.uoms WHERE upper(trim("Code")) = 'EA') THEN RAISE EXCEPTION 'REV869A approved EA UOM Code collision'; END IF;
                    IF EXISTS (SELECT 1 FROM nexa.uoms WHERE upper(trim("Name")) = 'EACH') THEN RAISE EXCEPTION 'REV869A approved EA UOM Name collision'; END IF;
                    IF (SELECT count(*) FROM nexa.items) <> 1 OR
                       (SELECT count(*) FROM nexa.items WHERE "Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND "ItemCode" = 'REV868C1-ITEM' AND "Name" = 'REV868C1 Item' AND "MaterialType" = 'Material' AND "Status" = 'Active' AND "IsActive" AND "UomId" IS NULL) <> 1 THEN
                        RAISE EXCEPTION 'REV869A approved Item-to-EA plan does not exactly match current Item source evidence';
                    END IF;
                    IF (SELECT count(*) FROM nexa.rev869a_items_prechange_backup WHERE "Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND "ItemCode" = 'REV868C1-ITEM' AND "Name" = 'REV868C1 Item' AND "MaterialType" = 'Material' AND "Status" = 'Active' AND "UomId" IS NULL) <> 1 THEN
                        RAISE EXCEPTION 'REV869A approved Item original values are not preserved in the prechange backup';
                    END IF;
                    INSERT INTO nexa.uoms ("Id", "Code", "Name", "MeasurementDimension", "QuantityPrecision", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                    VALUES ('f71a4725-bb15-e7bf-e97b-991985e96328'::uuid, 'EA', 'Each', 'COUNT', 0, true, TIMESTAMPTZ '2026-08-10T00:00:00+00:00', 'migration-rev869a', null, null, 0::bigint);
                    UPDATE nexa.items SET "UomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid, "BaseUomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid
                    WHERE "Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND "ItemCode" = 'REV868C1-ITEM' AND "UomId" IS NULL;
                    GET DIAGNOSTICS affected_item_count = ROW_COUNT;
                    IF affected_item_count <> 1 THEN RAISE EXCEPTION 'REV869A approved Item-to-EA update affected % rows instead of 1', affected_item_count; END IF;
                    IF EXISTS (SELECT 1 FROM nexa.items WHERE "UomId" IS NULL OR "BaseUomId" IS NULL) THEN RAISE EXCEPTION 'REV869A approved Item-to-EA backfill is incomplete'; END IF;
                END $rev869a_uom_plan$;
                ALTER TABLE nexa.items ALTER COLUMN "BaseUomId" SET NOT NULL;
                ALTER TABLE nexa.uoms ALTER COLUMN "MeasurementDimension" DROP DEFAULT;
                """);













            migrationBuilder.CreateTable(
                name: "controlled_configuration_histories",
                schema: "nexa",
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


            migrationBuilder.AddUniqueConstraint(
                name: "AK_rack_bins_WarehouseId_Id",
                schema: "nexa",
                table: "rack_bins",
                columns: new[] { "WarehouseId", "Id" });
            migrationBuilder.Sql("""
                INSERT INTO nexa.controlled_configuration_histories
                    ("Id", "OrganizationId", "EntityType", "EntityId", "Action", "BeforeJson", "AfterJson", "ActorLoginId", "ActorRoleCode", "Remarks", "CorrelationId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                VALUES ('0007efa3-4888-a87d-45ef-72cc55f4dd45'::uuid, 'SESS', 'UOM', 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid,
                    'MANAGEMENT_APPROVED_CREATE_AND_ITEM_BACKFILL', null,
                    '{"UomId":"f71a4725-bb15-e7bf-e97b-991985e96328","UomCode":"EA","Name":"Each","MeasurementDimension":"COUNT","QuantityPrecision":0,"IsCanonicalBase":true,"ConversionPolicy":"IDENTITY_ONLY","LifecycleAction":"CREATE","ApprovalStatus":"APPROVED","ManagementApprovalReference":"MGMT-REV869A-UOM-20260810-001","ItemId":"8c428e59-db05-471d-a7e7-4f7dc1c13b54","ItemCode":"REV868C1-ITEM","MappingStatus":"APPROVED","MappingBasis":"MANAGEMENT_APPROVED"}'::jsonb,
                    'management-approved-source-plan', 'MANAGEMENT_APPROVAL', 'Approved EA UOM creation and exact REV868C1-ITEM mapping; MGMT-REV869A-UOM-20260810-001',
                    'MGMT-REV869A-UOM-20260810-001', TIMESTAMPTZ '2026-08-10T00:00:00+00:00', 'migration-rev869a', null, null, 0::bigint);
                """);

            migrationBuilder.CreateTable(
                name: "employee_identity_mappings",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_operational_scopes",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_rack_bins_WarehouseId_RackBinId",
                        columns: x => new { x.WarehouseId, x.RackBinId },
                        principalSchema: "nexa",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_operational_scopes_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_policies",
                schema: "nexa",
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
                name: "qc_inspection_policies",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_policies_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "nexa",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspection_policies_uoms_MeasurementUomId",
                        column: x => x.MeasurementUomId,
                        principalSchema: "nexa",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_gst_settings",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HsnSacCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SupplierStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PlaceOfSupplyStateCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SupplyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.CheckConstraint("CK_tax_gst_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_tax_gst_rates", "\"GstRate\" BETWEEN 0 AND 100 AND \"CgstRate\" BETWEEN 0 AND 100 AND \"SgstRate\" BETWEEN 0 AND 100 AND \"IgstRate\" BETWEEN 0 AND 100 AND \"CessRate\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_tax_gst_rounding", "\"RoundingScale\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_tax_gst_supply_type", "\"SupplyType\" IN ('INTRASTATE','INTERSTATE')");
                    table.CheckConstraint("CK_tax_gst_state_supply", "(\"SupplierStateCode\" = \"PlaceOfSupplyStateCode\" AND \"SupplyType\" = 'INTRASTATE') OR (\"SupplierStateCode\" <> \"PlaceOfSupplyStateCode\" AND \"SupplyType\" = 'INTERSTATE')");
                    table.CheckConstraint("CK_tax_gst_component_split", "(\"SupplyType\" = 'INTRASTATE' AND \"IgstRate\" = 0 AND \"CgstRate\" + \"SgstRate\" = \"GstRate\") OR (\"SupplyType\" = 'INTERSTATE' AND \"CgstRate\" = 0 AND \"SgstRate\" = 0 AND \"IgstRate\" = \"GstRate\")");
                });

            migrationBuilder.CreateTable(
                name: "uom_conversions",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_uom_conversions_uoms_ToUomId",
                        column: x => x.ToUomId,
                        principalSchema: "nexa",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_qualifications",
                schema: "nexa",
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
                    table.PrimaryKey("PK_vendor_qualifications", x => x.Id);
                    table.CheckConstraint("CK_vendor_qualification_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_item_categories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalSchema: "nexa",
                        principalTable: "item_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_qualifications_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "nexa",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_condition_locations",
                schema: "nexa",
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
                        name: "FK_warehouse_condition_locations_rack_bins_WarehouseId_RackBinId",
                        columns: x => new { x.WarehouseId, x.RackBinId },
                        principalSchema: "nexa",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouse_condition_locations_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "nexa",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });








































            migrationBuilder.InsertData(
                schema: "nexa",
                table: "organization_policies",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsActive", "OrganizationId", "PolicyCode", "PolicyValue", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", new DateOnly(2026, 8, 10), null, true, "SESS", "VENDOR_FINAL_APPROVER", "MANAGING_DIRECTOR", null, null, 0L },
                    { new Guid("50000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", new DateOnly(2026, 8, 10), null, true, "SESS", "INVENTORY_VALUATION_METHOD", "WEIGHTED_AVERAGE", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Security", "security.employee-identities", "/security/employee-identities", "Employee Identities", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Security", "security.operational-scopes", "/security/operational-scopes", "Operational Scopes", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.uoms", "/masters/uoms", "UOM Master", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.uom-conversions", "/masters/uom-conversions", "UOM Conversion Master", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Settings", "settings.tax-gst", "/settings/tax-gst", "Tax/GST Settings", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.vendor-qualifications", "/masters/vendor-qualifications", "Vendor Qualifications", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "Masters", "masters.warehouse-condition-locations", "/masters/warehouse-condition-locations", "Warehouse Condition Locations", null, null, 0L },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, "QC", "qc.inspection-policies", "/qc/inspection-policies", "QC Inspection Policies", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), "PURCHASE_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "Purchase Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000002"), "STORES_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "Stores Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000003"), "QC_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, true, "QC Manager", null, null, 0L },
                    { new Guid("30000000-0000-0000-0000-000000000004"), "QC_INSPECTOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, false, "QC Inspector", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("01b635f9-b7c0-6952-aad1-db0a13aabe39"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("0229a2fa-bdb6-b6b5-4da0-db1e3bc6d395"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("062c8d00-221a-5347-8c3b-bd87604fc083"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("0f9deb7e-4745-0527-9d8e-bb60c8cececa"), false, true, true, false, true, false, true, true, false, true, true, true, true, true, true, true, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("11038140-87fb-6522-7425-da633f209502"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("125a37ad-46bf-b2c4-c02f-d588f0969a84"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("1913267c-7d4c-e241-5011-8cf30bd84137"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("1c7a6074-478b-76b5-920b-da17f5147d7c"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2139e45b-4437-632d-a851-c87145ba4071"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("21c63dbc-0985-5d45-72a0-6db78ecf2a39"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2a6b1d70-e88b-a9f8-3f68-1e4cbfdd8b67"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("2d3b700e-1aeb-d373-de93-2c2fa8a3370e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("338f3857-ff34-b27c-d671-ad42eb33fe3d"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("35376d76-a0b1-7ee1-b32d-1499b7e24f06"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"), true, true, true, true, true, false, true, true, false, true, true, true, true, true, true, true, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4a045df1-dc0e-e920-6a8e-02afdc1f9f37"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("4be63323-a734-943b-8d03-b7d80fd58683"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("5ceb4c02-d702-580c-00ca-75404dada0f7"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5d19be17-57d3-0652-d98b-5a11f62faf19"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("61063a45-9de0-6ada-716f-b308ab881c76"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("625a6c21-32f6-45b9-911c-fef812d43657"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("65ac8a90-8d09-c31b-1285-cf09b38f6c6f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("68b94637-ea35-95bb-731a-68ab0d83b6f5"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("6ecea443-25c0-ccd6-067d-c53b9cb5369b"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("6f876ecb-f37c-9c97-c30e-fe992cf56d10"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7170ddae-3c8f-154e-5e20-e51f8d572074"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("7492c61c-6d29-ca2a-0f2c-e7fe98b66bc0"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("75b705db-ab96-19a0-0fc7-1f5ec2ada945"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("799bd43d-d80a-0eb5-777d-6ba1afc0717b"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("7f52e39c-fb55-8e9b-3865-d29bffaee942"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("7fa66608-1650-7481-0d97-33b93ff14201"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("8626e388-a399-ab33-a557-df27a097aa40"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("8642768b-72de-85b7-3700-52f204bc2412"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8c9dff2e-5ed3-13c3-668b-b11f7602e9d8"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8db2124b-a20d-c86d-0c55-44f6a9b83dcb"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8fec4773-1c6b-0cae-e623-f394e71f3901"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("90b24916-a7da-926c-85db-d40df0bb5cb5"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("973f3950-0ae0-1df9-ad6f-570f6cd38b89"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("97eb4bbf-2fea-a75a-5226-bbfd8aa0d667"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("99230bc2-6f8e-6513-4b7a-d6424d3cf345"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("a1153b99-f614-049c-8f62-2e6672c1163d"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a3c118c5-33e2-98c4-a904-8a7cf3a5a7ad"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("a41ce315-d082-63f6-b0cb-5cde4bd4fe03"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a9d6e145-ea26-2b7f-1844-90682dffd78f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("b1302fd0-129b-62d1-3006-293ac6bf6a87"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("b2cfa60a-5fc8-d083-5d04-5a01d70cbc02"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("baff3f8c-6e8c-e814-86d6-9431df1251d1"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("bd4fa4d4-57b6-6b58-3f9d-f5e17b47865e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("c63323be-cd04-b0e3-eb1c-97442843e6ba"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c66d4c06-b1a6-2b18-37b1-56b7d9677643"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("c6c95969-c68e-f1d9-c708-d280df85c29e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("c78eb0ff-8d8e-0082-a51f-f862c75a0ca9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("ce23c00d-3772-21ee-ec50-8e903fe1fc81"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("cecbb5f3-5709-025a-d5f7-807d4151a665"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d1224cfb-0e09-3337-c4dc-b5fc728b4450"), true, true, true, true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", true, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d15f336b-f96e-94d4-9ac0-764d82895884"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000007"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("d8d1e9af-6bf3-d7f7-cb14-7a9f6d1d13ab"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000004"), new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("dc466d18-679e-85aa-a346-e4062dfbeddc"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("dc9b2f94-6506-f50d-5518-46d6e04af43a"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("e7b9e221-0799-7867-f623-9ec602b64c84"), false, true, true, false, true, false, true, false, false, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("eaa52044-6e94-fcd9-82d8-9a3323450753"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("ec586367-c47c-fa2c-a3a6-7b652a8bcf03"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("f5240291-ec17-bea1-5a31-eead7c8a0ec9"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f9c9f6cc-48b9-8727-4c81-4196e4444b59"), false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("fcf487a0-7345-b5f0-8f88-784ce8f0016a"), false, true, true, false, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869a", false, new Guid("40000000-0000-0000-0000-000000000006"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L }
                });

            migrationBuilder.Sql("""
                INSERT INTO nexa.role_page_permissions
                    ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
                SELECT v."Id", false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false,
                       TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-rev869a', false, v."PageDefinitionId", r."Id", null, null, 0::bigint
                FROM (VALUES
                    ('aea2e8a1-18a6-72d2-a954-6f5513b80eeb'::uuid, '40000000-0000-0000-0000-000000000001'::uuid),
                    ('f8e7d0a6-f056-175a-e604-14c1f9f6ad83'::uuid, '40000000-0000-0000-0000-000000000002'::uuid),
                    ('a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12'::uuid, '40000000-0000-0000-0000-000000000003'::uuid),
                    ('15ee5b19-d532-c28c-b755-de4152769a7a'::uuid, '40000000-0000-0000-0000-000000000004'::uuid),
                    ('5794f740-90b1-5a70-413a-d59bbc97ce78'::uuid, '40000000-0000-0000-0000-000000000005'::uuid),
                    ('42e2a253-d767-6191-caf9-e1f79652c44f'::uuid, '40000000-0000-0000-0000-000000000006'::uuid),
                    ('38371df3-5a46-5137-8204-4c5391633180'::uuid, '40000000-0000-0000-0000-000000000007'::uuid),
                    ('680f7358-4b7c-0733-be42-f9d52e746d1b'::uuid, '40000000-0000-0000-0000-000000000008'::uuid)
                ) AS v("Id", "PageDefinitionId")
                CROSS JOIN nexa.roles r
                WHERE r."Code" = 'DEPARTMENT_MANAGER';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_items_BaseUomId",
                schema: "nexa",
                table: "items",
                column: "BaseUomId");




            migrationBuilder.CreateIndex(
                name: "IX_controlled_configuration_histories_CorrelationId",
                schema: "nexa",
                table: "controlled_configuration_histories",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_controlled_configuration_histories_EntityType_EntityId_Crea~",
                schema: "nexa",
                table: "controlled_configuration_histories",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });





            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_EmployeeId",
                schema: "nexa",
                table: "employee_identity_mappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_OrganizationId_EmployeeId_Identi~",
                schema: "nexa",
                table: "employee_identity_mappings",
                columns: new[] { "OrganizationId", "EmployeeId", "IdentityType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IdentityType\" = 'HUMAN'");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_Issuer_Subject_IsActive",
                schema: "nexa",
                table: "employee_identity_mappings",
                columns: new[] { "Issuer", "Subject", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_DepartmentId",
                schema: "nexa",
                table: "employee_operational_scopes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_EmployeeId",
                schema: "nexa",
                table: "employee_operational_scopes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_OrganizationId_EmployeeId_Depar~",
                schema: "nexa",
                table: "employee_operational_scopes",
                columns: new[] { "OrganizationId", "EmployeeId", "DepartmentId", "WarehouseId", "RackBinId", "OwnRecordsOnly", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_WarehouseId_RackBinId",
                schema: "nexa",
                table: "employee_operational_scopes",
                columns: new[] { "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_operational_scopes_WarehouseId",
                schema: "nexa",
                table: "employee_operational_scopes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_policies_OrganizationId_PolicyCode_EffectiveFr~",
                schema: "nexa",
                table: "organization_policies",
                columns: new[] { "OrganizationId", "PolicyCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_ItemCategoryId",
                schema: "nexa",
                table: "qc_inspection_policies",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_ItemId",
                schema: "nexa",
                table: "qc_inspection_policies",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_MeasurementUomId",
                schema: "nexa",
                table: "qc_inspection_policies",
                column: "MeasurementUomId");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_policies_OrganizationId_ItemId_ItemCategoryId~",
                schema: "nexa",
                table: "qc_inspection_policies",
                columns: new[] { "OrganizationId", "ItemId", "ItemCategoryId", "ParameterCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_tax_gst_settings_OrganizationId_JurisdictionCode_HsnSacCode~",
                schema: "nexa",
                table: "tax_gst_settings",
                columns: new[] { "OrganizationId", "JurisdictionCode", "HsnSacCode", "SupplierStateCode", "PlaceOfSupplyStateCode", "VendorRegistrationType", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_FromUomId",
                schema: "nexa",
                table: "uom_conversions",
                column: "FromUomId");

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_OrganizationId_FromUomId_ToUomId_EffectiveF~",
                schema: "nexa",
                table: "uom_conversions",
                columns: new[] { "OrganizationId", "FromUomId", "ToUomId", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_uom_conversions_ToUomId",
                schema: "nexa",
                table: "uom_conversions",
                column: "ToUomId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_ItemCategoryId",
                schema: "nexa",
                table: "vendor_qualifications",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_OrganizationId_VendorId_ItemCategoryI~",
                schema: "nexa",
                table: "vendor_qualifications",
                columns: new[] { "OrganizationId", "VendorId", "ItemCategoryId", "QualificationCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_qualifications_VendorId",
                schema: "nexa",
                table: "vendor_qualifications",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_condition_locations_OrganizationId_WarehouseId_Ra~",
                schema: "nexa",
                table: "warehouse_condition_locations",
                columns: new[] { "OrganizationId", "WarehouseId", "RackBinId", "ConditionCode", "EffectiveFrom", "EffectiveTo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_condition_locations_WarehouseId_RackBinId",
                schema: "nexa",
                table: "warehouse_condition_locations",
                columns: new[] { "WarehouseId", "RackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_condition_locations_WarehouseId",
                schema: "nexa",
                table: "warehouse_condition_locations",
                column: "WarehouseId");




            migrationBuilder.Sql("""
                CREATE FUNCTION nexa.rev869a_block_history_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Controlled configuration history is append-only'; END $$;
                CREATE TRIGGER trg_rev869a_history_append_only BEFORE UPDATE OR DELETE ON nexa.controlled_configuration_histories FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_block_history_mutation();

                CREATE FUNCTION nexa.rev869a_guard_controlled_version() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'Controlled configuration versions cannot be deleted'; END IF;
                    IF (to_jsonb(NEW) - ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version']) <> (to_jsonb(OLD) - ARRAY['EffectiveTo','IsActive','UpdatedAt','UpdatedBy','Version']) THEN
                        RAISE EXCEPTION 'Historical effective records are immutable; close the old version and insert a corrected version';
                    END IF;
                    IF OLD."EffectiveTo" IS NOT NULL OR NEW."EffectiveTo" IS NULL OR NEW."EffectiveTo" < OLD."EffectiveFrom" THEN RAISE EXCEPTION 'Only a valid first close of an open effective version is allowed'; END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER trg_rev869a_identity_version_guard BEFORE UPDATE OR DELETE ON nexa.employee_identity_mappings FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_scope_version_guard BEFORE UPDATE OR DELETE ON nexa.employee_operational_scopes FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_policy_version_guard BEFORE UPDATE OR DELETE ON nexa.organization_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_qc_version_guard BEFORE UPDATE OR DELETE ON nexa.qc_inspection_policies FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_tax_version_guard BEFORE UPDATE OR DELETE ON nexa.tax_gst_settings FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_vendor_qualification_version_guard BEFORE UPDATE OR DELETE ON nexa.vendor_qualifications FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_warehouse_condition_version_guard BEFORE UPDATE OR DELETE ON nexa.warehouse_condition_locations FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();
                CREATE TRIGGER trg_rev869a_uom_conversion_version_guard BEFORE UPDATE OR DELETE ON nexa.uom_conversions FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_controlled_version();

                CREATE FUNCTION nexa.rev869a_guard_used_uom_conversion() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF OLD."FirstUsedAt" IS NOT NULL THEN RAISE EXCEPTION 'A used UOM conversion is immutable'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER trg_rev869a_used_uom_conversion BEFORE UPDATE OR DELETE ON nexa.uom_conversions FOR EACH ROW EXECUTE FUNCTION nexa.rev869a_guard_used_uom_conversion();
                """);
            migrationBuilder.AddForeignKey(
                name: "FK_items_uoms_BaseUomId",
                schema: "nexa",
                table: "items",
                column: "BaseUomId",
                principalSchema: "nexa",
                principalTable: "uoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS nexa.rev869a_guard_used_uom_conversion() CASCADE;
                DROP FUNCTION IF EXISTS nexa.rev869a_guard_controlled_version() CASCADE;
                DROP FUNCTION IF EXISTS nexa.rev869a_block_history_mutation() CASCADE;
                """);
            migrationBuilder.Sql("""
                DO $rev869a_uom_rollback$
                DECLARE restored_item_count integer; deleted_history_count integer; deleted_uom_count integer;
                BEGIN
                    IF (SELECT count(*) FROM nexa.rev869a_items_prechange_backup WHERE "Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND "UomId" IS NULL) <> 1 THEN RAISE EXCEPTION 'REV869A rollback cannot prove the exact original null Item UomId'; END IF;
                    IF (SELECT count(*) FROM nexa.items WHERE "Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND "UomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid AND "BaseUomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid) <> 1 THEN RAISE EXCEPTION 'REV869A rollback target Item is not in the exact approved EA-mapped state'; END IF;
                    IF EXISTS (SELECT 1 FROM nexa.items WHERE "Id" <> '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND ("UomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid OR "BaseUomId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid)) THEN RAISE EXCEPTION 'REV869A rollback refuses to delete EA because an unapproved Item references it'; END IF;
                    UPDATE nexa.items i SET "UomId" = b."UomId", "BaseUomId" = null FROM nexa.rev869a_items_prechange_backup b
                    WHERE i."Id" = b."Id" AND i."Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid;
                    GET DIAGNOSTICS restored_item_count = ROW_COUNT;
                    IF restored_item_count <> 1 OR EXISTS (SELECT 1 FROM nexa.items i JOIN nexa.rev869a_items_prechange_backup b ON b."Id" = i."Id" WHERE i."Id" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND (to_jsonb(i) - 'BaseUomId') IS DISTINCT FROM to_jsonb(b)) THEN RAISE EXCEPTION 'REV869A rollback did not restore the exact backed-up Item values'; END IF;
                    DELETE FROM nexa.controlled_configuration_histories WHERE "Id" = '0007efa3-4888-a87d-45ef-72cc55f4dd45'::uuid AND "EntityId" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid AND "CreatedBy" = 'migration-rev869a' AND "CorrelationId" = 'MGMT-REV869A-UOM-20260810-001';
                    GET DIAGNOSTICS deleted_history_count = ROW_COUNT;
                    IF deleted_history_count <> 1 THEN RAISE EXCEPTION 'REV869A rollback did not delete exactly one owned EA approval-history row'; END IF;
                    DELETE FROM nexa.uoms WHERE "Id" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid AND "Code" = 'EA' AND "Name" = 'Each' AND "MeasurementDimension" = 'COUNT' AND "QuantityPrecision" = 0 AND "CreatedBy" = 'migration-rev869a';
                    GET DIAGNOSTICS deleted_uom_count = ROW_COUNT;
                    IF deleted_uom_count <> 1 THEN RAISE EXCEPTION 'REV869A rollback did not delete exactly one migration-owned EA UOM'; END IF;
                END $rev869a_uom_rollback$;
                """);

            migrationBuilder.DeleteData(schema: "nexa", table: "organization_policies", keyColumn: "Id", keyColumnType: "uuid", keyValue: new Guid("50000000-0000-0000-0000-000000000001"));
            migrationBuilder.DeleteData(schema: "nexa", table: "organization_policies", keyColumn: "Id", keyColumnType: "uuid", keyValue: new Guid("50000000-0000-0000-0000-000000000002"));

            migrationBuilder.Sql("""
                DELETE FROM nexa.role_page_permissions p
                USING nexa.roles r
                WHERE p."RoleId" = r."Id"
                  AND r."Code" = 'DEPARTMENT_MANAGER'
                  AND p."CreatedBy" = 'migration-rev869a'
                  AND p."PageDefinitionId" IN (
                    '40000000-0000-0000-0000-000000000001'::uuid,
                    '40000000-0000-0000-0000-000000000002'::uuid,
                    '40000000-0000-0000-0000-000000000003'::uuid,
                    '40000000-0000-0000-0000-000000000004'::uuid,
                    '40000000-0000-0000-0000-000000000005'::uuid,
                    '40000000-0000-0000-0000-000000000006'::uuid,
                    '40000000-0000-0000-0000-000000000007'::uuid,
                    '40000000-0000-0000-0000-000000000008'::uuid
                  );
                """);




            migrationBuilder.DropForeignKey(
                name: "FK_items_uoms_BaseUomId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropTable(
                name: "controlled_configuration_histories",
                schema: "nexa");


            migrationBuilder.DropTable(
                name: "employee_identity_mappings",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employee_operational_scopes",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "organization_policies",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "qc_inspection_policies",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "tax_gst_settings",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "uom_conversions",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "vendor_qualifications",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "warehouse_condition_locations",
                schema: "nexa");

            migrationBuilder.DropIndex(
                name: "IX_items_BaseUomId",
                schema: "nexa",
                table: "items");




            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("01b635f9-b7c0-6952-aad1-db0a13aabe39"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("0229a2fa-bdb6-b6b5-4da0-db1e3bc6d395"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("062c8d00-221a-5347-8c3b-bd87604fc083"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("0f9deb7e-4745-0527-9d8e-bb60c8cececa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("11038140-87fb-6522-7425-da633f209502"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("125a37ad-46bf-b2c4-c02f-d588f0969a84"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("1913267c-7d4c-e241-5011-8cf30bd84137"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("1c7a6074-478b-76b5-920b-da17f5147d7c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("2139e45b-4437-632d-a851-c87145ba4071"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("21c63dbc-0985-5d45-72a0-6db78ecf2a39"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("2a6b1d70-e88b-a9f8-3f68-1e4cbfdd8b67"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("2d3b700e-1aeb-d373-de93-2c2fa8a3370e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("338f3857-ff34-b27c-d671-ad42eb33fe3d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("35376d76-a0b1-7ee1-b32d-1499b7e24f06"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("4a045df1-dc0e-e920-6a8e-02afdc1f9f37"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("4be63323-a734-943b-8d03-b7d80fd58683"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("5ceb4c02-d702-580c-00ca-75404dada0f7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("5d19be17-57d3-0652-d98b-5a11f62faf19"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("61063a45-9de0-6ada-716f-b308ab881c76"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("625a6c21-32f6-45b9-911c-fef812d43657"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("65ac8a90-8d09-c31b-1285-cf09b38f6c6f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("68b94637-ea35-95bb-731a-68ab0d83b6f5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("6ecea443-25c0-ccd6-067d-c53b9cb5369b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("6f876ecb-f37c-9c97-c30e-fe992cf56d10"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("7170ddae-3c8f-154e-5e20-e51f8d572074"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("7492c61c-6d29-ca2a-0f2c-e7fe98b66bc0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("75b705db-ab96-19a0-0fc7-1f5ec2ada945"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("799bd43d-d80a-0eb5-777d-6ba1afc0717b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("7f52e39c-fb55-8e9b-3865-d29bffaee942"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("7fa66608-1650-7481-0d97-33b93ff14201"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8626e388-a399-ab33-a557-df27a097aa40"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8642768b-72de-85b7-3700-52f204bc2412"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8c9dff2e-5ed3-13c3-668b-b11f7602e9d8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8db2124b-a20d-c86d-0c55-44f6a9b83dcb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("8fec4773-1c6b-0cae-e623-f394e71f3901"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("90b24916-a7da-926c-85db-d40df0bb5cb5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("973f3950-0ae0-1df9-ad6f-570f6cd38b89"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("97eb4bbf-2fea-a75a-5226-bbfd8aa0d667"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("99230bc2-6f8e-6513-4b7a-d6424d3cf345"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("a1153b99-f614-049c-8f62-2e6672c1163d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("a3c118c5-33e2-98c4-a904-8a7cf3a5a7ad"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("a41ce315-d082-63f6-b0cb-5cde4bd4fe03"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("a9d6e145-ea26-2b7f-1844-90682dffd78f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("b1302fd0-129b-62d1-3006-293ac6bf6a87"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("b2cfa60a-5fc8-d083-5d04-5a01d70cbc02"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("baff3f8c-6e8c-e814-86d6-9431df1251d1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("bd4fa4d4-57b6-6b58-3f9d-f5e17b47865e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("c63323be-cd04-b0e3-eb1c-97442843e6ba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("c66d4c06-b1a6-2b18-37b1-56b7d9677643"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("c6c95969-c68e-f1d9-c708-d280df85c29e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("c78eb0ff-8d8e-0082-a51f-f862c75a0ca9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("ce23c00d-3772-21ee-ec50-8e903fe1fc81"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("cecbb5f3-5709-025a-d5f7-807d4151a665"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("d1224cfb-0e09-3337-c4dc-b5fc728b4450"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("d15f336b-f96e-94d4-9ac0-764d82895884"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("d8d1e9af-6bf3-d7f7-cb14-7a9f6d1d13ab"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("dc466d18-679e-85aa-a346-e4062dfbeddc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("dc9b2f94-6506-f50d-5518-46d6e04af43a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("e7b9e221-0799-7867-f623-9ec602b64c84"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("eaa52044-6e94-fcd9-82d8-9a3323450753"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("ec586367-c47c-fa2c-a3a6-7b652a8bcf03"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("f5240291-ec17-bea1-5a31-eead7c8a0ec9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("f9c9f6cc-48b9-8727-4c81-4196e4444b59"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("fcf487a0-7345-b5f0-8f88-784ce8f0016a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("40000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyColumnType: "uuid",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"));


            migrationBuilder.DropColumn(
                name: "CommercialVerificationStatus",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "CommercialVerifiedAt",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "CommercialVerifiedBy",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "RequiresReverification",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "MeasurementDimension",
                schema: "nexa",
                table: "uoms");

            migrationBuilder.DropColumn(
                name: "QuantityPrecision",
                schema: "nexa",
                table: "uoms");

            migrationBuilder.DropColumn(
                name: "BaseUomId",
                schema: "nexa",
                table: "items");

            migrationBuilder.DropUniqueConstraint(name: "AK_rack_bins_WarehouseId_Id", schema: "nexa", table: "rack_bins");

            migrationBuilder.Sql("""
                DROP TABLE nexa.rev869a_vendors_prechange_backup;
                DROP TABLE nexa.rev869a_uoms_prechange_backup;
                DROP TABLE nexa.rev869a_items_prechange_backup;
                """);













        }
    }
}
