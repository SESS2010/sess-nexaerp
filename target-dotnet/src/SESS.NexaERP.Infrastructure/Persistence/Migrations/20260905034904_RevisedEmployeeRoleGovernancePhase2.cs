using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RevisedEmployeeRoleGovernancePhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                schema: "advance",
                table: "employee_role_assignments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                schema: "advance",
                table: "employee_role_assignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                schema: "advance",
                table: "employee_role_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndedBy",
                schema: "advance",
                table: "employee_role_assignments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorRoleCode",
                schema: "advance",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedRoleAssignmentId",
                schema: "advance",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRoleAssignmentType",
                schema: "advance",
                table: "audit_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_role_assignment_events",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Operation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FromAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PreviousEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    PreviousEffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    NewEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    NewEffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorLoginId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_role_assignment_events", x => x.Id);
                    table.UniqueConstraint("AK_employee_role_assignment_events_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_employee_role_assignment_events_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_role_assignment_events_employee_role_assignments_A~",
                        column: x => x.AssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_role_assignment_events_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_role_assignment_events_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("11744032-08e9-f364-d36f-c12caeff0b02"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("2a23e241-204c-4810-46cd-5f1b0f513434"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("2e2eb9a5-7caa-e157-2099-e3f06e85fbad"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("51a38ab8-5943-e4f6-6140-76dea2057e8b"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("bf16025e-df11-ac0e-785b-4873e1a14af3"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("bf6ef4ae-fe3a-2861-28d4-88f7708aba51"),
                columns: new[] { "ActorRoleCode", "ResolvedRoleAssignmentId", "ResolvedRoleAssignmentType" },
                values: new object[] { "", null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("02702296-3863-8644-c306-ddc2f49e5cca"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("068427ee-6fc5-8182-b61c-24b2b3187867"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("157d94ff-a39e-3fa4-3a54-f6f8d05cab62"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("18da9f7c-3049-52e3-b76c-c4238cedb213"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("1b5c6764-7dcd-6f19-0097-61b87603b5eb"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("205cd7e9-b79c-4600-f9c9-561e15e2be9f"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("25c10527-28a2-e600-82d2-3b1b767af269"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("261e0ee9-c1a4-6f18-a3fc-461add06916b"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("270a811f-0564-a4b0-8f4f-0b47118d3134"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("2e2b854a-f965-2a71-21c3-96738e3cb840"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("30e7eac7-1101-ffde-70c0-6edd20ed4c01"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("3b51f513-0e8e-7677-b138-19bc0d9c4150"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("3b6fe413-e8d3-3c0e-52a0-2425db151f48"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("4a1b90a5-9797-0fd0-0e6d-58785e981854"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("53f3f0b9-de8b-4119-3668-01c751a3d52a"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("5554a0f5-85f0-d477-ea7b-f3a6cd1ed121"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("67461916-89e1-fe39-e460-39d2d341d242"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("6c56b8eb-3f8a-4940-df22-5e8002b262da"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("6d4b74b6-5611-c8f5-0ba5-48be51fd6996"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("87dd003b-f6f7-fb19-9f89-c395683c8fa0"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("8b4828cc-bbf0-05df-0f27-a3d789052b82"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("8b8c5e6b-cc4d-4386-50a3-32fb3d776860"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("8c3e4b9b-6be9-9fa3-9c81-fa47f23b5818"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("8c7733c4-1a45-970b-a81b-dbf5aa781ef0"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("8ee5108f-6a19-af67-0562-ee708ebd6a05"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("98804443-54b0-2474-7acb-ffc54410e33e"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("9ac81cf0-423b-97a8-08e7-d3797a7410c7"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("9e1e368d-3c82-60cf-f522-7758004d3e88"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("a260b451-c377-907d-ba80-fb03af55ebc0"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("a2bc7e87-56b4-0478-d29d-c329f7eb060a"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("a7552ac8-23f1-9ed4-6de8-669d08054e0a"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("a79e4f09-112d-57e5-4f17-00066b3e6d22"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("ad9892ac-7d0f-89fc-8aec-be5f65860079"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("ae3c6d06-5d8c-fa88-ae24-4dcf2ddbfacb"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("babde2dc-2cd6-83b4-eea4-84c5886b436e"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("c3aa8842-31de-0d93-71b8-ba5e8895a534"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("d278c271-c2e2-00a7-a70b-ca058dc2af0e"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("e6cf6f13-4f3a-56c8-dbed-608f3b596b6e"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("ec95b2c0-4bb6-9b59-3e5e-6fd16ce97ba3"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_role_assignments",
                keyColumn: "Id",
                keyValue: new Guid("f03cb56e-0797-3443-b51a-d28205fcdfa7"),
                columns: new[] { "AssignmentType", "EndReason", "EndedAt", "EndedBy" },
                values: new object[] { "FULL", null, null, null });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "roles",
                columns: new[] { "Id", "Audience", "BusinessArea", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsEmployeeAssignable", "IsPrivileged", "Name", "ReplacementRoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("99000000-0000-0000-0000-000000000005"), "INTERNAL_EMPLOYEE", "ADMINISTRATION", "HR_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, true, "HR Manager", null, null, null, 0L },
                    { new Guid("99000000-0000-0000-0000-000000000006"), "INTERNAL_EMPLOYEE", "ADMINISTRATION", "HOUSEKEEPING_ASSISTANT", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", true, true, false, "Housekeeping Assistant", null, null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "company_role_activations",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedBy", "EffectiveFrom", "EffectiveTo", "IsEnabled", "Remarks", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("309e1bd0-3004-54f5-18d3-9ae547241fc3"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("6df52711-626c-db3a-b9ec-3d3d4f6a7208"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("baafc608-12b4-7c34-c610-4fd7922ff0a0"), new Guid("70000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e1b594f1-a72f-a0b3-15f0-6adc277a4457"), new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-governance-foundation", new DateOnly(2026, 9, 4), null, true, "Initial company role catalogue.", new Guid("99000000-0000-0000-0000-000000000006"), null, null, 0L }
                });

            migrationBuilder.Sql(RevisedEmployeeRoleGovernancePhase2Sql.Prepare);

            migrationBuilder.AddCheckConstraint(
                name: "CK_employee_role_assignment_dates",
                schema: "advance",
                table: "employee_role_assignments",
                sql: "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_employee_role_assignment_end_metadata",
                schema: "advance",
                table: "employee_role_assignments",
                sql: "\"EffectiveTo\" IS NULL OR \"AssignmentType\" = 'TEMPORARY' OR (\"EndReason\" IS NOT NULL AND length(btrim(\"EndReason\")) > 0 AND \"EndedAt\" IS NOT NULL AND \"EndedBy\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_employee_role_assignment_temporary_end",
                schema: "advance",
                table: "employee_role_assignments",
                sql: "\"AssignmentType\" <> 'TEMPORARY' OR \"EffectiveTo\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_employee_role_assignment_type",
                schema: "advance",
                table: "employee_role_assignments",
                sql: "\"AssignmentType\" IN ('FULL','SUPPORT','TEMPORARY')");

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignment_events_ActorEmployeeId",
                schema: "advance",
                table: "employee_role_assignment_events",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignment_events_AssignmentId",
                schema: "advance",
                table: "employee_role_assignment_events",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignment_events_CompanyId",
                schema: "advance",
                table: "employee_role_assignment_events",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignment_events_CompanyId_EmployeeId_Create~",
                schema: "advance",
                table: "employee_role_assignment_events",
                columns: new[] { "CompanyId", "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignment_events_EmployeeId",
                schema: "advance",
                table: "employee_role_assignment_events",
                column: "EmployeeId");

            migrationBuilder.Sql(RevisedEmployeeRoleGovernancePhase2Sql.Up);
            migrationBuilder.Sql(Rev869BControlledMutationSql.ReconcileHistoryAuthority);
            migrationBuilder.Sql(RevisedEmployeeRoleGovernancePhase2Sql.AuthenticationBootstrapCompatibility);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.Sql(RevisedEmployeeRoleGovernancePhase2Sql.Down);

            migrationBuilder.DropTable(
                name: "employee_role_assignment_events",
                schema: "advance");

            migrationBuilder.DropCheckConstraint(
                name: "CK_employee_role_assignment_dates",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_employee_role_assignment_end_metadata",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_employee_role_assignment_temporary_end",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_employee_role_assignment_type",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "company_role_activations",
                keyColumn: "Id",
                keyValue: new Guid("309e1bd0-3004-54f5-18d3-9ae547241fc3"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "company_role_activations",
                keyColumn: "Id",
                keyValue: new Guid("6df52711-626c-db3a-b9ec-3d3d4f6a7208"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "company_role_activations",
                keyColumn: "Id",
                keyValue: new Guid("baafc608-12b4-7c34-c610-4fd7922ff0a0"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "company_role_activations",
                keyColumn: "Id",
                keyValue: new Guid("e1b594f1-a72f-a0b3-15f0-6adc277a4457"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("99000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropColumn(
                name: "EndReason",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropColumn(
                name: "EndedBy",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropColumn(
                name: "ActorRoleCode",
                schema: "advance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ResolvedRoleAssignmentId",
                schema: "advance",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ResolvedRoleAssignmentType",
                schema: "advance",
                table: "audit_logs");
        }
    }
}
