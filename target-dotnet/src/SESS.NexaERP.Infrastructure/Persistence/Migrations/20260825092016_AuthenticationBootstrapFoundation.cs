using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticationBootstrapFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(AuthenticationBootstrapFoundationSql.PreUp);

            migrationBuilder.CreateTable(
                name: "authentication_bootstrap_state",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssuerSha256 = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                    SubjectSha256 = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authentication_bootstrap_state", x => x.Id);
                    table.CheckConstraint("CK_authentication_bootstrap_completion", "(\"Status\"='PENDING' AND \"EmployeeId\" IS NULL AND \"CompanyId\" IS NULL AND \"OrganizationId\" IS NULL AND \"IssuerSha256\" IS NULL AND \"SubjectSha256\" IS NULL AND \"CompletedAt\" IS NULL AND \"CompletedBy\" IS NULL) OR (\"Status\"='COMPLETED' AND \"EmployeeId\" IS NOT NULL AND \"CompanyId\" IS NOT NULL AND length(trim(\"OrganizationId\"))>0 AND octet_length(\"IssuerSha256\")=32 AND octet_length(\"SubjectSha256\")=32 AND \"CompletedAt\" IS NOT NULL AND length(trim(\"CompletedBy\"))>0)");
                    table.CheckConstraint("CK_authentication_bootstrap_singleton", "\"Id\" = '81000000-0000-0000-0000-000000000001'::uuid");
                    table.CheckConstraint("CK_authentication_bootstrap_status", "\"Status\" IN ('PENDING','COMPLETED')");
                    table.ForeignKey(
                        name: "FK_authentication_bootstrap_state_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_authentication_bootstrap_state_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "authentication_bootstrap_state",
                columns: new[] { "Id", "CompanyId", "CompletedAt", "CompletedBy", "CreatedAt", "CreatedBy", "EmployeeId", "IssuerSha256", "OrganizationId", "Status", "SubjectSha256", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("81000000-0000-0000-0000-000000000001"), null, null, null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-authentication-bootstrap", null, null, null, "PENDING", null, null, null, 0L });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("34545002-3f06-d2bb-8275-f3fbb141a710"),
                column: "CanCreate",
                value: true);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d0bdd816-1a8f-0ade-28b2-d4c90a283ad0"),
                column: "CanCreate",
                value: true);

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("82000000-0000-0000-0000-000000000001"), false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-authentication-bootstrap", false, new Guid("40000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("82000000-0000-0000-0000-000000000002"), false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-authentication-bootstrap", false, new Guid("40000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L }
                });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("003197d6-a07b-a658-1014-0d84c68d2355"),
                column: "Code",
                value: "ACCOUNTS_ASSISTANT");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"),
                column: "Code",
                value: "MANAGING_DIRECTOR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("07d53aa2-c266-4802-4786-9723d800e29d"),
                column: "Code",
                value: "TECHNICAL_SUPPORT_MANAGER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("0a769058-1bab-5087-26b9-d33415b000e5"),
                column: "Code",
                value: "HR_EXECUTIVE");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Code",
                value: "ADMIN");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Code",
                value: "MD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Code",
                value: "ACCOUNTS_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Code",
                value: "PURCHASE_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Code",
                value: "STORE_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Code",
                value: "PRODUCTION_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Code",
                value: "QC_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Code",
                value: "DESIGN_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Code",
                value: "SERVICE_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                column: "Code",
                value: "SALES_HEAD");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                column: "Code",
                value: "SERVICE_COORDINATOR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                column: "Code",
                value: "SERVICE_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"),
                column: "Code",
                value: "SALES_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "IT_MANAGER", "IT Manager" });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"),
                column: "Code",
                value: "CUSTOMER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"),
                column: "Code",
                value: "VENDOR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000017"),
                column: "Code",
                value: "DOCUMENT_CONTROLLER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000018"),
                column: "Code",
                value: "DCC");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000019"),
                column: "Code",
                value: "BRANCH_MANAGER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                column: "Code",
                value: "OPS_ADMIN_NO_HR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"),
                column: "Code",
                value: "ELECTRICAL_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"),
                column: "Code",
                value: "STORES_ASSISTANT");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"),
                column: "Code",
                value: "SOFTWARE_DEVELOPER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("45eb9032-3689-8526-caee-41db0e7e2644"),
                column: "Code",
                value: "TECHNICAL_DIRECTOR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"),
                column: "Code",
                value: "PURCHASE_EXECUTIVE");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"),
                column: "Code",
                value: "PLC_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"),
                column: "Code",
                value: "SOFTWARE_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"),
                column: "Code",
                value: "TECHNICAL_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"),
                column: "Code",
                value: "ADMIN_EXECUTIVE");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"),
                column: "Code",
                value: "STORES_EXECUTIVE");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"),
                column: "Code",
                value: "PRODUCTION_OPERATOR");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("c4133420-c386-9452-93a7-484e18105372"),
                column: "Code",
                value: "JUNIOR_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"),
                column: "Code",
                value: "DESIGN_ENGINEER");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"),
                column: "Code",
                value: "PRODUCTION_COORDINATOR");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roles_code_canonical",
                schema: "advance",
                table: "roles",
                sql: "\"Code\" = upper(btrim(\"Code\"))");

            migrationBuilder.CreateIndex(
                name: "IX_authentication_bootstrap_state_CompanyId",
                schema: "advance",
                table: "authentication_bootstrap_state",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_authentication_bootstrap_state_EmployeeId",
                schema: "advance",
                table: "authentication_bootstrap_state",
                column: "EmployeeId");

            migrationBuilder.Sql(AuthenticationBootstrapFoundationSql.PostUp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(AuthenticationBootstrapFoundationSql.PreDown);

            migrationBuilder.DropTable(
                name: "authentication_bootstrap_state",
                schema: "advance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roles_code_canonical",
                schema: "advance",
                table: "roles");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("82000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("82000000-0000-0000-0000-000000000002"));

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("34545002-3f06-d2bb-8275-f3fbb141a710"),
                column: "CanCreate",
                value: false);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d0bdd816-1a8f-0ade-28b2-d4c90a283ad0"),
                column: "CanCreate",
                value: false);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("003197d6-a07b-a658-1014-0d84c68d2355"),
                column: "Code",
                value: "accounts_assistant");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"),
                column: "Code",
                value: "managing_director");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("07d53aa2-c266-4802-4786-9723d800e29d"),
                column: "Code",
                value: "technical_support_manager");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("0a769058-1bab-5087-26b9-d33415b000e5"),
                column: "Code",
                value: "hr_executive");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Code",
                value: "admin");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Code",
                value: "md");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Code",
                value: "accounts_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Code",
                value: "purchase_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Code",
                value: "store_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Code",
                value: "production_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Code",
                value: "qc_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Code",
                value: "design_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Code",
                value: "service_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                column: "Code",
                value: "sales_head");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                column: "Code",
                value: "service_coordinator");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                column: "Code",
                value: "service_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"),
                column: "Code",
                value: "sales_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "it_admin", "IT Admin" });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"),
                column: "Code",
                value: "customer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"),
                column: "Code",
                value: "vendor");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000017"),
                column: "Code",
                value: "document_controller");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000018"),
                column: "Code",
                value: "dcc");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000019"),
                column: "Code",
                value: "branch_manager");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                column: "Code",
                value: "ops_admin_no_hr");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"),
                column: "Code",
                value: "electrical_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"),
                column: "Code",
                value: "stores_assistant");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"),
                column: "Code",
                value: "software_developer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("45eb9032-3689-8526-caee-41db0e7e2644"),
                column: "Code",
                value: "technical_director");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"),
                column: "Code",
                value: "purchase_executive");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"),
                column: "Code",
                value: "plc_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"),
                column: "Code",
                value: "software_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"),
                column: "Code",
                value: "technical_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"),
                column: "Code",
                value: "admin_executive");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"),
                column: "Code",
                value: "stores_executive");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"),
                column: "Code",
                value: "production_operator");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("c4133420-c386-9452-93a7-484e18105372"),
                column: "Code",
                value: "junior_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"),
                column: "Code",
                value: "design_engineer");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"),
                column: "Code",
                value: "production_coordinator");

            migrationBuilder.Sql(AuthenticationBootstrapFoundationSql.PostDown);
        }
    }
}
