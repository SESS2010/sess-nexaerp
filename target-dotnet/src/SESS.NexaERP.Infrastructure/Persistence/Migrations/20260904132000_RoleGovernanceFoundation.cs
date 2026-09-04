using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoleGovernanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.AddColumn<string>(
                name: "Audience",
                schema: "advance",
                table: "roles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                schema: "advance",
                table: "roles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEmployeeAssignable",
                schema: "advance",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReplacementRoleId",
                schema: "advance",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "company_role_activations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_role_activations", x => x.Id);
                    table.UniqueConstraint("AK_company_role_activations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_company_role_activation_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_company_role_activations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_company_role_activations_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "advance",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "company_role_activations",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsEnabled", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("0053a1de-6583-1c72-b511-73b01767defd"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("07578821-63aa-57ee-d602-0c0576959546"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("0d08f567-4d7b-2b1d-49af-9380c7e6d443"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("115ca643-312c-7cc0-d06c-6aecd3bf34a1"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("11a6df95-da5a-564d-5923-ed0c24b89bb0"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("1a10766b-439d-76b3-5b2c-608014fff634"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("1d2ea40d-765a-6d44-e3f7-0d4b04b87178"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("1f28ad7c-e576-33f7-7bf2-d8eea9464100"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("20d9d088-2922-8700-add8-1249a9379c85"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "System-security role is not available for employee assignment.", new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("259fbb60-a827-d0ad-29f5-59c5ba6e85d0"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("27f94d9e-3a2a-aa6a-aeb4-2813b096da3a"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("289ca9c3-1591-b1db-51e6-841c896778cc"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("28b232b2-c7e7-b6ef-144a-dc14b3501cb6"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("29d81764-482d-0df7-3b1f-46537b636c34"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("2cc4a72e-1e6e-9eb7-3da5-201d57d5354b"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("382bf734-289c-f155-303b-6d093e06b93d"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("3899757d-fabc-c0b3-4cc4-a2440b0c06d8"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("38d3cf9c-c757-dc52-317a-87050a1bdf99"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3929fa42-c107-4af2-746d-b89d83e7110d"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("3a487f6a-2191-8464-135f-07a457bf22f6"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("3c7819b4-b023-fbf8-3c40-365b01486873"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("3ef5c967-70b5-bd81-7f2b-4453da944078"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("42b4a61e-e6a5-b6bc-c325-8361240f8e12"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("47d44a53-71f9-5cee-9700-d3f35a513d45"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4aac9eac-1f44-c94e-4d75-e83decb6cd38"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4d522616-d391-d521-eddf-dff7ef2084f8"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("50263011-50ed-5065-6dc9-d8ef3ba76751"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("51969676-7bf9-6717-6d3c-2bb7f06ff6b9"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("5442cb40-3a0a-943e-9781-7186eea78069"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("54d3bf4b-f403-ba08-2998-ba288a41b409"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("59ed8ca7-1fa4-6950-566b-6138dddcdb93"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("621a646c-ff91-4b6e-1c91-f983361d003e"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("6408e272-342a-536c-6b22-0b20c00d83e7"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("642a7c9d-7aad-ba44-6b4e-2d0f3237cf6f"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("66488b2e-e3ef-ecce-4052-7ef559d345c8"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("6724d5cc-e60c-fab5-d692-b59ff6c2e028"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("6a3aefd6-a739-9b14-f883-1ee3fc5ac7e7"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7045bf21-800e-3260-a187-90de51c23bac"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("72eea07b-41b9-5f92-92b3-80fe2ab1c871"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("774e3f95-2df7-7f0b-ac4a-78854227787d"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("7b07feaf-fdf7-8ea0-61b9-c6354bc3b778"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("82c9fb0f-b875-e273-110e-92dd788b0e8e"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("82e27f5a-bb17-fff6-c5ac-e9ea1921f8c4"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("892ef128-6898-e8d7-178d-55ba1e888ec9"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("8bac6e66-9ec1-54b6-6a97-9efe48a1db82"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("93c2da5a-bb31-93ab-224a-014acd125ea4"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("9b47993d-39b4-9524-d610-ba513be233c8"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("9b4e11b1-2452-525a-be47-7303d5520269"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("9bfde4b6-c12f-59de-ee52-8c726d623914"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("9ca7b4b1-5f38-11aa-bf20-3fa70faf095a"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("9cf30cb4-6f4d-430a-2d7e-a4ebc1e282ca"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9d23c112-2a6d-d5da-d6b1-cce1bdc206bb"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("9dcf93a1-93f6-fd45-c00a-72e54f822e81"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("a1005141-868e-731f-d6bf-222477b4965e"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("a5d3d568-72f7-1ea6-2c30-7b34e8ba2308"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("a75c71cf-840d-fa83-5876-a76a3182d80b"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("a8ccc26e-6ef5-04c9-de7b-e980eddb0954"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("a8e8793d-65bd-e2c3-2902-cf2095055699"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("b0d6e0e5-f154-8700-7417-f8bbd4768a42"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("b6ccdd44-04e5-75c4-b939-712939daa9f3"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("b9997f8e-9bc0-88b1-4887-37b4461b419a"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("bb328758-9b25-f075-510c-4a0f1db5439d"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("be53cf8d-de89-dbd4-3e65-22c84219d39e"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c673462d-93d0-c8d0-0fb0-664bd11c1b30"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c6f8f0fa-6c73-92f5-f328-c54734caefe3"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c7a4df1d-7879-773f-f1ff-64d412245e62"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("ca2f2621-e633-1823-f952-521139c18257"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("ca2fa898-2c8a-e53d-2a48-434dd026ef61"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("cbc48396-c7b8-6db3-4942-1aad83eae561"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("cbd983a2-64ad-d343-1274-841a69ed5a7e"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d050273a-e669-bf1a-6da4-c5f2b7121c06"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d0849ed6-0db5-75ec-3ee0-3778a014e191"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("d428d57d-75fa-463b-eb38-9fba26368a83"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d6b6850b-2e79-cc91-af6c-63f1edbeebfc"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("d9ffc4cb-8ab1-0b82-df33-5955337e2955"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("dd35f006-270c-7357-6a38-d665e9207171"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("e0526661-0024-40a9-83a6-557092d4b999"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("e1f11928-a860-468b-df5b-932a0e6638d8"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "System-security role is not available for employee assignment.", new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e60237ff-eb63-43e4-c065-1196309a2a1b"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e71ae68b-6273-fe45-6d54-bc3a3d2a380b"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e8e2cf43-a6b9-da3b-ec0c-0dba503e3710"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("e966f370-7c12-826b-ff0d-25a4d4de9e1b"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("e9795083-9407-bbbb-18ea-6ebdd091f888"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("eea735d7-f5f8-1d10-b1c2-2887d95e3bc9"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("eef7b770-9976-c811-8f74-1ab290b838a3"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("efda872e-15db-95a4-5684-36b9f9642111"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("effb1576-1688-c529-25c6-dbcd0b4c8a69"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, false, "Legacy alias retained for history; use the replacement role.", new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("f1023862-ba80-52e5-80a8-f0acc682e550"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("fa209def-18ee-27ea-3cc0-4b1d43a98987"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("facd5f31-1ade-1b88-0e9c-52a7a613147c"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L }
                });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("003197d6-a07b-a658-1014-0d84c68d2355"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ACCOUNTS", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "MANAGEMENT", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("07d53aa2-c266-4802-4786-9723d800e29d"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SERVICE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("0a769058-1bab-5087-26b9-d33415b000e5"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ADMINISTRATION", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "SYSTEM_SECURITY", "SECURITY", false, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "MANAGEMENT", false, new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "ACCOUNTS", false, new Guid("83000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "PURCHASE", false, new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "STORES", false, new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "PRODUCTION", false, new Guid("83000000-0000-0000-0000-000000000001") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "QUALITY", false, new Guid("30000000-0000-0000-0000-000000000003") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "DESIGN", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SERVICE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SALES", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SERVICE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SERVICE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "SALES", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "IT", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "EXTERNAL_PORTAL", "EXTERNAL", false, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "EXTERNAL_PORTAL", "EXTERNAL", false, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000017"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "DOCUMENT_CONTROL", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000018"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "DOCUMENT_CONTROL", false, new Guid("10000000-0000-0000-0000-000000000017") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000019"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "GENERAL", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "GENERAL", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ENGINEERING", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "STORES", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "PURCHASE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "STORES", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000003"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "QUALITY", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000004"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "QUALITY", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000005"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "GENERAL", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "IT", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("45eb9032-3689-8526-caee-41db0e7e2644"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "MANAGEMENT", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "PURCHASE", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ENGINEERING", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "LEGACY_ALIAS", "IT", false, new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ENGINEERING", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ADMINISTRATION", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("83000000-0000-0000-0000-000000000001"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "PRODUCTION", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("83000000-0000-0000-0000-000000000002"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ACCOUNTS", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "STORES", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "PRODUCTION", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("c4133420-c386-9452-93a7-484e18105372"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "ENGINEERING", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "DESIGN", true, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"),
                columns: new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleId" },
                values: new object[] { "INTERNAL_EMPLOYEE", "PRODUCTION", true, null });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "roles",
                columns: new[] { "Id", "Audience", "BusinessArea", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsEmployeeAssignable", "IsPrivileged", "Name", "ReplacementRoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("99000000-0000-0000-0000-000000000001"), "INTERNAL_EMPLOYEE", "PROJECTS", "PROJECT_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, true, "Project Manager", null, null, null, 0L },
                    { new Guid("99000000-0000-0000-0000-000000000002"), "INTERNAL_EMPLOYEE", "PROJECTS", "SITE_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, false, "Site Engineer", null, null, null, 0L },
                    { new Guid("99000000-0000-0000-0000-000000000003"), "INTERNAL_EMPLOYEE", "LOGISTICS", "DISPATCH_COORDINATOR", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, false, "Dispatch Coordinator", null, null, null, 0L },
                    { new Guid("99000000-0000-0000-0000-000000000004"), "INTERNAL_EMPLOYEE", "MAINTENANCE", "MAINTENANCE_ENGINEER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, false, "Maintenance Engineer", null, null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "company_role_activations",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsEnabled", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("4d8738ff-cb9e-6366-4bff-2f72bdc7fd15"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("67edbd9a-8979-1562-c9e2-072b5027482d"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("76ad8a4d-e0a9-e3b5-18ee-3b4adb5a3936"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("85ef1056-23f6-ec5a-d6af-4ab55e8c7bfc"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("991176b7-16a7-cebf-cb6a-fda68151eab2"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("bffacf8f-5c3a-8af3-254c-32118c3f17c7"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e7d283fa-a993-ef89-d923-671db0278dec"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("fced8dec-6d7f-4355-a271-56c9a58073ba"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000002"), null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_roles_ReplacementRoleId",
                schema: "advance",
                table: "roles",
                column: "ReplacementRoleId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roles_assignable_audience",
                schema: "advance",
                table: "roles",
                sql: "\"IsEmployeeAssignable\" = FALSE OR \"Audience\" = 'INTERNAL_EMPLOYEE'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roles_audience",
                schema: "advance",
                table: "roles",
                sql: "\"Audience\" IN ('INTERNAL_EMPLOYEE','EXTERNAL_PORTAL','LEGACY_ALIAS','SYSTEM_SECURITY')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roles_business_area_canonical",
                schema: "advance",
                table: "roles",
                sql: "\"BusinessArea\" = upper(btrim(\"BusinessArea\"))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_roles_replacement",
                schema: "advance",
                table: "roles",
                sql: "(\"Audience\" = 'LEGACY_ALIAS' AND \"ReplacementRoleId\" IS NOT NULL) OR (\"Audience\" <> 'LEGACY_ALIAS' AND \"ReplacementRoleId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_company_role_activations_CompanyId",
                schema: "advance",
                table: "company_role_activations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_company_role_activations_CompanyId_IsEnabled",
                schema: "advance",
                table: "company_role_activations",
                columns: new[] { "CompanyId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_company_role_activations_CompanyId_RoleId_EffectiveFrom",
                schema: "advance",
                table: "company_role_activations",
                columns: new[] { "CompanyId", "RoleId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_role_activations_RoleId",
                schema: "advance",
                table: "company_role_activations",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_roles_roles_ReplacementRoleId",
                schema: "advance",
                table: "roles",
                column: "ReplacementRoleId",
                principalSchema: "advance",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_roles_roles_ReplacementRoleId",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropTable(
                name: "company_role_activations",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_roles_ReplacementRoleId",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roles_assignable_audience",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roles_audience",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roles_business_area_canonical",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_roles_replacement",
                schema: "advance",
                table: "roles");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000004"));

            migrationBuilder.DropColumn(
                name: "Audience",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "BusinessArea",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "IsEmployeeAssignable",
                schema: "advance",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "ReplacementRoleId",
                schema: "advance",
                table: "roles");
        }
    }
}
