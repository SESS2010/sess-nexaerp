using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Foundation2InventoryOwnershipAndCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_warehouses_Id_CompanyId",
                schema: "advance",
                table: "warehouses",
                columns: new[] { "Id", "CompanyId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_rack_bins_Id_CompanyId",
                schema: "advance",
                table: "rack_bins",
                columns: new[] { "Id", "CompanyId" });

            migrationBuilder.CreateTable(
                name: "inventory_external_parties",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PartyNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_external_parties", x => x.Id);
                    table.UniqueConstraint("AK_inventory_external_parties_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_external_parties_identity", "(\"PartyType\" = 'CUSTOMER' AND \"CustomerId\" IS NOT NULL AND \"VendorId\" IS NULL)\n                   OR (\"PartyType\" = 'VENDOR' AND \"VendorId\" IS NOT NULL AND \"CustomerId\" IS NULL)\n                   OR (\"PartyType\" = 'OTHER' AND \"CustomerId\" IS NULL AND \"VendorId\" IS NULL)");
                    table.CheckConstraint("CK_inventory_external_parties_type", "\"PartyType\" IN ('CUSTOMER','VENDOR','OTHER')");
                    table.ForeignKey(
                        name: "FK_inventory_external_parties_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_external_parties_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "advance",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_external_parties_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "advance",
                        principalTable: "vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_account_holders",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HolderType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HolderCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalPartyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    HolderCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    HolderNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_account_holders", x => x.Id);
                    table.UniqueConstraint("AK_inventory_account_holders_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_account_holders_identity", "(\"HolderType\" = 'COMPANY' AND \"HolderCompanyId\" IS NOT NULL AND \"ExternalPartyId\" IS NULL AND \"EmployeeId\" IS NULL)\n                   OR (\"HolderType\" = 'EXTERNAL_PARTY' AND \"HolderCompanyId\" IS NULL AND \"ExternalPartyId\" IS NOT NULL AND \"EmployeeId\" IS NULL)\n                   OR (\"HolderType\" = 'EMPLOYEE' AND \"HolderCompanyId\" IS NULL AND \"ExternalPartyId\" IS NULL AND \"EmployeeId\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_account_holders_type", "\"HolderType\" IN ('COMPANY','EXTERNAL_PARTY','EMPLOYEE')");
                    table.ForeignKey(
                        name: "FK_inventory_account_holders_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_account_holders_companies_HolderCompanyId",
                        column: x => x.HolderCompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_account_holders_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_account_holders_inventory_external_parties_Compan~",
                        columns: x => new { x.CompanyId, x.ExternalPartyId },
                        principalSchema: "advance",
                        principalTable: "inventory_external_parties",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_accounts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountHolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CustodyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SiteReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_accounts", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_accounts_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_accounts_location", "(\"RackBinId\" IS NULL OR \"WarehouseId\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_custody_accounts_type", "\"CustodyType\" IN ('WAREHOUSE','EMPLOYEE','VEHICLE','SITE','VENDOR','CUSTOMER','OTHER')");
                    table.ForeignKey(
                        name: "FK_inventory_custody_accounts_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_accounts_inventory_account_holders_Compan~",
                        columns: x => new { x.CompanyId, x.AccountHolderId },
                        principalSchema: "advance",
                        principalTable: "inventory_account_holders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_accounts_rack_bins_RackBinId_CompanyId",
                        columns: x => new { x.RackBinId, x.CompanyId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_accounts_warehouses_WarehouseId_CompanyId",
                        columns: x => new { x.WarehouseId, x.CompanyId },
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ownership_accounts",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountHolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OwnershipType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    InventoryValuationBasis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ownership_accounts", x => x.Id);
                    table.UniqueConstraint("AK_inventory_ownership_accounts_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_ownership_accounts_type", "\"OwnershipType\" IN ('SESS_INVENTORY','CUSTOMER_PROPERTY','SUPPLIER_LOAN','DEMO_CUSTODY')");
                    table.CheckConstraint("CK_inventory_ownership_accounts_valuation", "(\"OwnershipType\" = 'SESS_INVENTORY' AND \"InventoryValuationBasis\" = 'FIFO')\n                   OR (\"OwnershipType\" <> 'SESS_INVENTORY' AND \"InventoryValuationBasis\" = 'ZERO_MEMO')");
                    table.ForeignKey(
                        name: "FK_inventory_ownership_accounts_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_accounts_inventory_account_holders_Comp~",
                        columns: x => new { x.CompanyId, x.AccountHolderId },
                        principalSchema: "advance",
                        principalTable: "inventory_account_holders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_handoffs",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HandoffNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FromCustodyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToCustodyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HandedOverAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    HandedOverByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_handoffs", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_handoffs_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_handoffs_accounts", "\"FromCustodyAccountId\" <> \"ToCustodyAccountId\"");
                    table.CheckConstraint("CK_inventory_custody_handoffs_completion", "(\"Status\" = 'DRAFT' AND \"HandedOverAt\" IS NULL AND \"HandedOverByEmployeeId\" IS NULL)\n                   OR (\"Status\" <> 'DRAFT' AND \"HandedOverAt\" IS NOT NULL AND \"HandedOverByEmployeeId\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_custody_handoffs_status", "\"Status\" IN ('DRAFT','COMPLETED','REVERSED')");
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoffs_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoffs_employees_HandedOverByEmployeeId",
                        column: x => x.HandedOverByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoffs_employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoffs_inventory_custody_accounts_Compa~",
                        columns: x => new { x.CompanyId, x.FromCustodyAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoffs_inventory_custody_accounts_Comp~1",
                        columns: x => new { x.CompanyId, x.ToCustodyAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_cases",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CaseType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CommercialAuthorizationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalPartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InboundReturnableDcNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InboundReturnableDcDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OfferReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CustomerPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DueDateSetByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueDateSetAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomerInstructionReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClosureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_cases", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_cases_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_cases_commercial_status", "\"CommercialAuthorizationStatus\" IN ('NOT_REQUIRED','AWAITING_OFFER','AWAITING_CUSTOMER_PO','AUTHORIZED')");
                    table.CheckConstraint("CK_inventory_custody_cases_due_date_evidence", "(\"DueDate\" IS NULL AND \"DueDateSetByEmployeeId\" IS NULL AND \"DueDateSetAt\" IS NULL)\n                   OR (\"DueDate\" IS NOT NULL AND \"DueDateSetByEmployeeId\" IS NOT NULL AND \"DueDateSetAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_custody_cases_other_brand_chargeable", "\"CaseType\" <> 'CUSTOMER_OTHER_BRAND_MODIFICATION' OR \"CommercialAuthorizationStatus\" <> 'NOT_REQUIRED'");
                    table.CheckConstraint("CK_inventory_custody_cases_status", "\"Status\" IN ('RECEIVED','RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION','AUTHORIZED_FOR_WORK','IN_WORK','READY_FOR_RETURN','RETURNED','CLOSED')");
                    table.CheckConstraint("CK_inventory_custody_cases_type", "\"CaseType\" IN ('CUSTOMER_OTHER_BRAND_MODIFICATION','CUSTOMER_SESS_MACHINE_WARRANTY','CUSTOMER_SESS_SPARE_WARRANTY','CUSTOMER_REMOVED_PART','SUPPLIER_LOAN','DEMO_CUSTODY')");
                    table.CheckConstraint("CK_inventory_custody_cases_work_authorization", "\"Status\" IN ('RECEIVED','RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION')\n                   OR \"CommercialAuthorizationStatus\" IN ('NOT_REQUIRED','AUTHORIZED')");
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_customer_purchase_orders_CustomerPu~",
                        column: x => x.CustomerPurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "customer_purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_employees_DueDateSetByEmployeeId",
                        column: x => x.DueDateSetByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_inventory_custody_accounts_CompanyI~",
                        columns: x => new { x.CompanyId, x.CustodyAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_inventory_external_parties_CompanyI~",
                        columns: x => new { x.CompanyId, x.ExternalPartyId },
                        principalSchema: "advance",
                        principalTable: "inventory_external_parties",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_cases_inventory_ownership_accounts_Compan~",
                        columns: x => new { x.CompanyId, x.OwnershipAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ownership_transfers",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    TransferType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromOwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToOwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AgreementReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ownership_transfers", x => x.Id);
                    table.UniqueConstraint("AK_inventory_ownership_transfers_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_ownership_transfers_accounts", "\"FromOwnershipAccountId\" <> \"ToOwnershipAccountId\"");
                    table.CheckConstraint("CK_inventory_ownership_transfers_approval", "(\"Status\" = 'DRAFT' AND \"ApprovedByEmployeeId\" IS NULL AND \"ApprovedAt\" IS NULL AND \"ApprovedRoleCode\" IS NULL)\n                   OR (\"Status\" <> 'DRAFT' AND \"ApprovedByEmployeeId\" IS NOT NULL AND \"ApprovedAt\" IS NOT NULL AND \"ApprovedRoleCode\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_ownership_transfers_buyback", "\"TransferType\" <> 'CUSTOMER_BUYBACK' OR NULLIF(btrim(\"AgreementReference\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_inventory_ownership_transfers_status", "\"Status\" IN ('DRAFT','APPROVED','POSTED','REVERSED')");
                    table.CheckConstraint("CK_inventory_ownership_transfers_type", "\"TransferType\" IN ('CUSTOMER_BUYBACK','CUSTOMER_INSTRUCTION','INTERCOMPANY_ACCEPTANCE','SUPPLIER_LOAN_CONVERSION','CAPITALIZATION')");
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfers_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfers_employees_ApprovedByEmployeeId",
                        column: x => x.ApprovedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfers_inventory_ownership_accounts_~",
                        columns: x => new { x.CompanyId, x.FromOwnershipAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfers_inventory_ownership_accounts~1",
                        columns: x => new { x.CompanyId, x.ToOwnershipAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    DescriptionSnapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalAssetIdentifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    SerialNumberSnapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UomId = table.Column<Guid>(type: "uuid", nullable: false),
                    UomCodeSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommercialScopeStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerPurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    OfferReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    ScopeDecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_lines", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_lines_CompanyId_CustodyCaseId_Id", x => new { x.CompanyId, x.CustodyCaseId, x.Id });
                    table.UniqueConstraint("AK_inventory_custody_case_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_case_lines_identity", "\"ItemId\" IS NOT NULL OR NULLIF(btrim(\"ExternalAssetIdentifier\"), '') IS NOT NULL");
                    table.CheckConstraint("CK_inventory_custody_case_lines_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_inventory_custody_case_lines_scope", "\"CommercialScopeStatus\" IN ('NOT_REQUIRED','AWAITING_AUTHORIZATION','AUTHORIZED','OUT_OF_SCOPE')");
                    table.CheckConstraint("CK_inventory_custody_case_lines_scope_evidence", "\"CommercialScopeStatus\" <> 'AUTHORIZED' OR \"CustomerPurchaseOrderLineId\" IS NOT NULL OR NULLIF(btrim(\"OfferReference\"), '') IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_customer_purchase_order_lines_~",
                        column: x => x.CustomerPurchaseOrderLineId,
                        principalSchema: "advance",
                        principalTable: "customer_purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_inventory_custody_cases_Compan~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_inventory_ownership_accounts_C~",
                        columns: x => new { x.CompanyId, x.OwnershipAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_items_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_lines_uoms_UomId",
                        column: x => x.UomId,
                        principalSchema: "advance",
                        principalTable: "uoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_assignments",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RackBinId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedQuantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    AssignmentReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_assignments", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_assignments_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_assignments_current", "(\"IsCurrent\" AND \"EffectiveTo\" IS NULL) OR (NOT \"IsCurrent\" AND \"EffectiveTo\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_custody_assignments_location", "\"RackBinId\" IS NULL OR \"WarehouseId\" IS NOT NULL");
                    table.CheckConstraint("CK_inventory_custody_assignments_period", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_inventory_custody_assignments_quantity", "\"AssignedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_custody_assignments_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_assignments_inventory_custody_accounts_Co~",
                        columns: x => new { x.CompanyId, x.CustodyAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_assignments_inventory_custody_case_lines_~",
                        columns: x => new { x.CompanyId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_assignments_rack_bins_RackBinId_CompanyId",
                        columns: x => new { x.RackBinId, x.CompanyId },
                        principalSchema: "advance",
                        principalTable: "rack_bins",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_assignments_warehouses_WarehouseId_Compan~",
                        columns: x => new { x.WarehouseId, x.CompanyId },
                        principalSchema: "advance",
                        principalTable: "warehouses",
                        principalColumns: new[] { "Id", "CompanyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_customer_purchase_order_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_customer_purchase_order_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_customer_purchase_order_links_Compan~", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_compan~",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_custom~",
                        column: x => x.CustomerPurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "customer_purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_invent~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_delivery_challan_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DeliveryChallanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_delivery_challan_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_delivery_challan_links_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_delivery_challan_links_companies_Com~",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_delivery_challan_links_delivery_chal~",
                        column: x => x.DeliveryChallanId,
                        principalSchema: "advance",
                        principalTable: "delivery_challans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_delivery_challan_links_inventory_cus~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_gate_entry_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_gate_entry_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_gate_entry_links_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_gate_entry_links_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_gate_entry_links_gate_entries_GateEn~",
                        column: x => x.GateEntryId,
                        principalSchema: "advance",
                        principalTable: "gate_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_gate_entry_links_inventory_custody_c~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_goods_receipt_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_goods_receipt_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_goods_receipt_links_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_goods_receipt_links_companies_Compan~",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_goods_receipt_links_goods_receipts_G~",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_goods_receipt_links_inventory_custod~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_job_order_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_job_order_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_job_order_links_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_job_order_links_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_job_order_links_inventory_custody_ca~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_job_order_links_job_orders_JobOrderId",
                        column: x => x.JobOrderId,
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_case_purchase_order_links",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkRole = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_case_purchase_order_links", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_case_purchase_order_links_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_customer_purchase_order_links_inven~1",
                        columns: x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumns: new[] { "CompanyId", "CustodyCaseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_purchase_order_links_companies_Compa~",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_purchase_order_links_inventory_custo~",
                        columns: x => new { x.CompanyId, x.CustodyCaseId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_cases",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_custody_case_purchase_order_links_purchase_orders~",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_memo_liability_events",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    MemoValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversesEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_memo_liability_events", x => x.Id);
                    table.UniqueConstraint("AK_inventory_memo_liability_events_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_memo_liability_events_close", "\"EventType\" <> 'LOAN_CLOSED_AGAINST_PO_GRN' OR (\"PurchaseOrderId\" IS NOT NULL AND \"GoodsReceiptId\" IS NOT NULL)");
                    table.CheckConstraint("CK_inventory_memo_liability_events_quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_inventory_memo_liability_events_reversal", "(\"EventType\" = 'REVERSAL' AND \"ReversesEventId\" IS NOT NULL)\n                   OR (\"EventType\" <> 'REVERSAL' AND \"ReversesEventId\" IS NULL)");
                    table.CheckConstraint("CK_inventory_memo_liability_events_type", "\"EventType\" IN ('LOAN_RECEIVED','LOAN_CONSUMED_PENDING_PROCUREMENT','LOAN_CLOSED_AGAINST_PO_GRN','REVERSAL')");
                    table.CheckConstraint("CK_inventory_memo_liability_events_value", "\"MemoValue\" >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_goods_receipts_GoodsReceipt~",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "advance",
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_inventory_custody_case_line~",
                        column: x => x.CustodyCaseLineId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_inventory_memo_liability_ev~",
                        column: x => x.ReversesEventId,
                        principalSchema: "advance",
                        principalTable: "inventory_memo_liability_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_inventory_ownership_account~",
                        columns: x => new { x.CompanyId, x.OwnershipAccountId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_accounts",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_memo_liability_events_purchase_orders_PurchaseOrd~",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ownership_transfer_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnershipTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ownership_transfer_lines", x => x.Id);
                    table.UniqueConstraint("AK_inventory_ownership_transfer_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_ownership_transfer_lines_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfer_lines_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfer_lines_inventory_custody_case_l~",
                        column: x => x.CustodyCaseLineId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_ownership_transfer_lines_inventory_ownership_tran~",
                        columns: x => new { x.CompanyId, x.OwnershipTransferId },
                        principalSchema: "advance",
                        principalTable: "inventory_ownership_transfers",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_custody_handoff_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodyHandoffId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    CustodyCaseLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCustodyAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToCustodyAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_custody_handoff_lines", x => x.Id);
                    table.UniqueConstraint("AK_inventory_custody_handoff_lines_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_inventory_custody_handoff_lines_quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoff_lines_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoff_lines_inventory_custody_assignmen~",
                        column: x => x.FromCustodyAssignmentId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoff_lines_inventory_custody_assignme~1",
                        column: x => x.ToCustodyAssignmentId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoff_lines_inventory_custody_case_line~",
                        column: x => x.CustodyCaseLineId,
                        principalSchema: "advance",
                        principalTable: "inventory_custody_case_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_custody_handoff_lines_inventory_custody_handoffs_~",
                        columns: x => new { x.CompanyId, x.CustodyHandoffId },
                        principalSchema: "advance",
                        principalTable: "inventory_custody_handoffs",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_account_holders_CompanyId",
                schema: "advance",
                table: "inventory_account_holders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_account_holders_CompanyId_ExternalPartyId",
                schema: "advance",
                table: "inventory_account_holders",
                columns: new[] { "CompanyId", "ExternalPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_account_holders_CompanyId_HolderCode",
                schema: "advance",
                table: "inventory_account_holders",
                columns: new[] { "CompanyId", "HolderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_account_holders_EmployeeId",
                schema: "advance",
                table: "inventory_account_holders",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_account_holders_HolderCompanyId",
                schema: "advance",
                table: "inventory_account_holders",
                column: "HolderCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_accounts_CompanyId",
                schema: "advance",
                table: "inventory_custody_accounts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_accounts_CompanyId_AccountCode",
                schema: "advance",
                table: "inventory_custody_accounts",
                columns: new[] { "CompanyId", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_accounts_CompanyId_AccountHolderId",
                schema: "advance",
                table: "inventory_custody_accounts",
                columns: new[] { "CompanyId", "AccountHolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_accounts_RackBinId_CompanyId",
                schema: "advance",
                table: "inventory_custody_accounts",
                columns: new[] { "RackBinId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_accounts_WarehouseId_CompanyId",
                schema: "advance",
                table: "inventory_custody_accounts",
                columns: new[] { "WarehouseId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_assignments_CompanyId",
                schema: "advance",
                table: "inventory_custody_assignments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_assignments_CompanyId_CustodyAccountId",
                schema: "advance",
                table: "inventory_custody_assignments",
                columns: new[] { "CompanyId", "CustodyAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_assignments_CompanyId_CustodyCaseLineId",
                schema: "advance",
                table: "inventory_custody_assignments",
                columns: new[] { "CompanyId", "CustodyCaseLineId" },
                unique: true,
                filter: "\"IsCurrent\"");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_assignments_RackBinId_CompanyId",
                schema: "advance",
                table: "inventory_custody_assignments",
                columns: new[] { "RackBinId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_assignments_WarehouseId_CompanyId",
                schema: "advance",
                table: "inventory_custody_assignments",
                columns: new[] { "WarehouseId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_customer_purchase_order_links_Compan~",
                schema: "advance",
                table: "inventory_custody_case_customer_purchase_order_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_customer_purchase_order_links_Custom~",
                schema: "advance",
                table: "inventory_custody_case_customer_purchase_order_links",
                column: "CustomerPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_delivery_challan_links_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_delivery_challan_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_delivery_challan_links_CompanyId_Cus~",
                schema: "advance",
                table: "inventory_custody_case_delivery_challan_links",
                columns: new[] { "CompanyId", "CustodyCaseId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_delivery_challan_links_DeliveryChall~",
                schema: "advance",
                table: "inventory_custody_case_delivery_challan_links",
                column: "DeliveryChallanId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_gate_entry_links_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_gate_entry_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_gate_entry_links_CompanyId_CustodyCa~",
                schema: "advance",
                table: "inventory_custody_case_gate_entry_links",
                columns: new[] { "CompanyId", "CustodyCaseId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_gate_entry_links_GateEntryId",
                schema: "advance",
                table: "inventory_custody_case_gate_entry_links",
                column: "GateEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_goods_receipt_links_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_goods_receipt_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_goods_receipt_links_CompanyId_Custod~",
                schema: "advance",
                table: "inventory_custody_case_goods_receipt_links",
                columns: new[] { "CompanyId", "CustodyCaseId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_goods_receipt_links_GoodsReceiptId",
                schema: "advance",
                table: "inventory_custody_case_goods_receipt_links",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_job_order_links_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_job_order_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_job_order_links_CompanyId_CustodyCas~",
                schema: "advance",
                table: "inventory_custody_case_job_order_links",
                columns: new[] { "CompanyId", "CustodyCaseId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_job_order_links_JobOrderId",
                schema: "advance",
                table: "inventory_custody_case_job_order_links",
                column: "JobOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_lines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_CompanyId_CustodyCaseId_LineNu~",
                schema: "advance",
                table: "inventory_custody_case_lines",
                columns: new[] { "CompanyId", "CustodyCaseId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_CompanyId_OwnershipAccountId",
                schema: "advance",
                table: "inventory_custody_case_lines",
                columns: new[] { "CompanyId", "OwnershipAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "inventory_custody_case_lines",
                column: "CustomerPurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_ItemId",
                schema: "advance",
                table: "inventory_custody_case_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_lines_UomId",
                schema: "advance",
                table: "inventory_custody_case_lines",
                column: "UomId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_purchase_order_links_CompanyId",
                schema: "advance",
                table: "inventory_custody_case_purchase_order_links",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_purchase_order_links_CompanyId_Custo~",
                schema: "advance",
                table: "inventory_custody_case_purchase_order_links",
                columns: new[] { "CompanyId", "CustodyCaseId", "CustodyCaseLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_case_purchase_order_links_PurchaseOrderId",
                schema: "advance",
                table: "inventory_custody_case_purchase_order_links",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId",
                schema: "advance",
                table: "inventory_custody_cases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId_CaseNumber",
                schema: "advance",
                table: "inventory_custody_cases",
                columns: new[] { "CompanyId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId_CustodyAccountId",
                schema: "advance",
                table: "inventory_custody_cases",
                columns: new[] { "CompanyId", "CustodyAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId_ExternalPartyId",
                schema: "advance",
                table: "inventory_custody_cases",
                columns: new[] { "CompanyId", "ExternalPartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId_OwnershipAccountId",
                schema: "advance",
                table: "inventory_custody_cases",
                columns: new[] { "CompanyId", "OwnershipAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CompanyId_Status_DueDate",
                schema: "advance",
                table: "inventory_custody_cases",
                columns: new[] { "CompanyId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_CustomerPurchaseOrderId",
                schema: "advance",
                table: "inventory_custody_cases",
                column: "CustomerPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_cases_DueDateSetByEmployeeId",
                schema: "advance",
                table: "inventory_custody_cases",
                column: "DueDateSetByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoff_lines_CompanyId",
                schema: "advance",
                table: "inventory_custody_handoff_lines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoff_lines_CompanyId_CustodyHandoffId_~",
                schema: "advance",
                table: "inventory_custody_handoff_lines",
                columns: new[] { "CompanyId", "CustodyHandoffId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoff_lines_CustodyCaseLineId",
                schema: "advance",
                table: "inventory_custody_handoff_lines",
                column: "CustodyCaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoff_lines_FromCustodyAssignmentId",
                schema: "advance",
                table: "inventory_custody_handoff_lines",
                column: "FromCustodyAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoff_lines_ToCustodyAssignmentId",
                schema: "advance",
                table: "inventory_custody_handoff_lines",
                column: "ToCustodyAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_CompanyId",
                schema: "advance",
                table: "inventory_custody_handoffs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_CompanyId_FromCustodyAccountId",
                schema: "advance",
                table: "inventory_custody_handoffs",
                columns: new[] { "CompanyId", "FromCustodyAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_CompanyId_HandoffNumber",
                schema: "advance",
                table: "inventory_custody_handoffs",
                columns: new[] { "CompanyId", "HandoffNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "inventory_custody_handoffs",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_CompanyId_ToCustodyAccountId",
                schema: "advance",
                table: "inventory_custody_handoffs",
                columns: new[] { "CompanyId", "ToCustodyAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_HandedOverByEmployeeId",
                schema: "advance",
                table: "inventory_custody_handoffs",
                column: "HandedOverByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_custody_handoffs_ReceivedByEmployeeId",
                schema: "advance",
                table: "inventory_custody_handoffs",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_CompanyId",
                schema: "advance",
                table: "inventory_external_parties",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_CompanyId_CustomerId",
                schema: "advance",
                table: "inventory_external_parties",
                columns: new[] { "CompanyId", "CustomerId" },
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_CompanyId_PartyCode",
                schema: "advance",
                table: "inventory_external_parties",
                columns: new[] { "CompanyId", "PartyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_CompanyId_VendorId",
                schema: "advance",
                table: "inventory_external_parties",
                columns: new[] { "CompanyId", "VendorId" },
                unique: true,
                filter: "\"VendorId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_CustomerId",
                schema: "advance",
                table: "inventory_external_parties",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_external_parties_VendorId",
                schema: "advance",
                table: "inventory_external_parties",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_ActorEmployeeId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_CompanyId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_CompanyId_CorrelationId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                columns: new[] { "CompanyId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_CompanyId_OwnershipAccountI~",
                schema: "advance",
                table: "inventory_memo_liability_events",
                columns: new[] { "CompanyId", "OwnershipAccountId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_CustodyCaseLineId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "CustodyCaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_GoodsReceiptId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_PurchaseOrderId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_memo_liability_events_ReversesEventId",
                schema: "advance",
                table: "inventory_memo_liability_events",
                column: "ReversesEventId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_accounts_CompanyId",
                schema: "advance",
                table: "inventory_ownership_accounts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_accounts_CompanyId_AccountCode",
                schema: "advance",
                table: "inventory_ownership_accounts",
                columns: new[] { "CompanyId", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_accounts_CompanyId_AccountHolderId",
                schema: "advance",
                table: "inventory_ownership_accounts",
                columns: new[] { "CompanyId", "AccountHolderId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfer_lines_CompanyId",
                schema: "advance",
                table: "inventory_ownership_transfer_lines",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfer_lines_CompanyId_OwnershipTrans~",
                schema: "advance",
                table: "inventory_ownership_transfer_lines",
                columns: new[] { "CompanyId", "OwnershipTransferId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfer_lines_CustodyCaseLineId",
                schema: "advance",
                table: "inventory_ownership_transfer_lines",
                column: "CustodyCaseLineId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_ApprovedByEmployeeId",
                schema: "advance",
                table: "inventory_ownership_transfers",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_CompanyId",
                schema: "advance",
                table: "inventory_ownership_transfers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_CompanyId_FromOwnershipAccoun~",
                schema: "advance",
                table: "inventory_ownership_transfers",
                columns: new[] { "CompanyId", "FromOwnershipAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "inventory_ownership_transfers",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_CompanyId_ToOwnershipAccountId",
                schema: "advance",
                table: "inventory_ownership_transfers",
                columns: new[] { "CompanyId", "ToOwnershipAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ownership_transfers_CompanyId_TransferNumber",
                schema: "advance",
                table: "inventory_ownership_transfers",
                columns: new[] { "CompanyId", "TransferNumber" },
                unique: true);

            migrationBuilder.Sql(Foundation2InventoryOwnershipCustodySql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(Foundation2InventoryOwnershipCustodySql.Down);
            migrationBuilder.DropTable(
                name: "inventory_custody_case_customer_purchase_order_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_delivery_challan_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_gate_entry_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_goods_receipt_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_job_order_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_purchase_order_links",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_handoff_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_memo_liability_events",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_ownership_transfer_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_assignments",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_handoffs",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_ownership_transfers",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_case_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_cases",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_custody_accounts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_ownership_accounts",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_account_holders",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "inventory_external_parties",
                schema: "advance");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_warehouses_Id_CompanyId",
                schema: "advance",
                table: "warehouses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_rack_bins_Id_CompanyId",
                schema: "advance",
                table: "rack_bins");
        }
    }
}
