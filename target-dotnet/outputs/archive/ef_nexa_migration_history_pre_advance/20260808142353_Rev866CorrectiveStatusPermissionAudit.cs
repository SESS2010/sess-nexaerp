using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev866CorrectiveStatusPermissionAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "nexa",
                table: "audit_logs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "REV866C1_SCHEMA_BACKFILL");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                schema: "nexa",
                table: "audit_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Success");

            migrationBuilder.InsertData(
                schema: "nexa",
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
                schema: "nexa",
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

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3be62adc-bee6-6ae9-55d1-ae4209ae72ee"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("553a4767-4479-5fd3-9b6a-8606fb8c12f3"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f7d23fd6-2d22-262a-7d9e-d9247a8021f5"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true, true });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("010a0c72-51e7-5832-2267-3788f0e50446"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0296b8ac-3ef8-319e-2bbd-52fc1434991a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("031586f9-bb24-8506-db97-f5714fa795ec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("03992abd-35a5-cdf5-c20d-6febeefceb22"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("0465f200-8bf3-8526-6ff2-7cabe33dc321"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("049a5c74-1f4e-5f6b-ada7-6ee9e078f31b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("058ec479-426d-9ccb-79ab-06ba7768ccd5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("07627316-54e4-db4d-77ef-0f161f685487"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("082c708f-c0ee-a4b7-547a-b5547cee5a48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("08db82bf-3428-9428-5102-888698acbaaa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("09b4df68-6e86-6dd3-a000-2e81cbfda172"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("09e6faf2-aa84-4975-dd32-33617250adb0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("0a41b95f-94b3-3462-7529-66fe94b49291"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("0f11ca79-fd0e-dfad-030c-865843cb8512"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("0f6fe7be-2afc-f4e7-2245-a5366599dfa9"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("12a9d791-55b7-7c0d-fb6d-99780a741e5b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("12fcb308-740b-14aa-cc1f-0197ce4c2448"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("144a76c1-f002-aee5-6f2d-3beb9a95aec5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("15ebeb75-142b-95a5-0c8f-f76f67e2cb93"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("16244354-0b37-2d19-cb30-f5f42725e630"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("1668624a-f2ad-0829-d2dd-0d4ea7ed0de4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("16ae3bf1-2b26-07e6-ed2a-6778ac80d373"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("17579bb5-969a-3378-52b7-76e4f6cabfc6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("17df4e3d-7834-3baf-449f-432487209c99"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("17e0a6d4-754a-5b66-dd33-9cf605995071"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("18203f3c-be44-3b65-acce-669dfcc2f9d1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("18374130-7861-27c5-cea8-0dc5824ada09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("187b9f2e-0af9-56bf-865c-2e5e656737c4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("18919a69-5da5-457f-b7e9-414d3df60136"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("1a376031-e48b-5c6a-79c2-01e348af1cc3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("1a9955f2-be51-7afc-9626-52c09c992beb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("1c37266c-c716-12eb-c9c5-a7c9c1031fb8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("1da21651-8f3a-3aa3-ce70-bcc28303030c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("1e9ef48a-863a-76cd-8e93-9019dbb37814"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("1f3c480c-606c-bfb1-f604-8218c9fb63e3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("227f9917-a233-3837-aadf-523264527624"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2339ab91-b159-f632-2013-3bf1f1d9bd93"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("2471ce82-f75c-f7be-d738-477687b33f82"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("260b7c1e-4743-8986-2c40-ed65cbecb2b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("277593dc-13ae-9384-7d93-964c3d2249e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("29e8e885-ec5b-77d1-548e-1c3717588eec"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("2a596a42-8571-37e5-3bcc-d6ca9da53341"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2a643fb6-252a-b03c-da85-d34692718ad8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("2ad1d150-3b2b-e715-3d30-4c3794b7fae6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("2ae27e32-6e47-856a-5d8a-c390ce208334"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("2b65dc13-1086-a99b-f478-4dd973f00f06"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("2c05b987-114e-af45-ab8e-84ceb61f5f62"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("2cacae30-7781-df69-ec66-a203c3d7b4a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2cef3812-dbb8-0935-18e8-74ce8cbab6a8"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("2cf53de7-abeb-d10b-84dc-9293a7af5ad7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2d5616c6-3914-444d-5b0a-4d6267c96956"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("2db7161b-d98c-3932-b43d-a06699323626"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("2ec28c45-6e02-1965-8849-2aadbec9262a"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("2f172da3-f681-1b9f-1d6c-ece0b9692e1f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("2f984f86-453b-52e8-88fd-6ccfb8ef34c7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("2f9bec8e-895b-c9fb-78a0-85fa7713b999"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("2fdcd65f-cdc7-b1fb-6b9f-544b405f1990"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("3041366d-bcf6-258b-a2be-7a88cf728455"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("30b5b613-ff4a-81cd-005f-1df54c743c77"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("30eeedb1-7d9f-15f5-3bdd-00a5aa01ce1e"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("319239ca-5893-0244-c8e5-2544b8e881de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("31ae89bc-fe23-612c-3a1a-03341c4efde5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("31d5b574-123f-4a5b-abe0-4468da1100c5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("321003ea-d45d-d309-8c38-72194f7b7e2b"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("32e5d033-63ed-ceca-bbcb-522d43909bc7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("32e72d3e-b825-9842-a7fd-4e06bbb085ea"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("3354553c-ad03-69a5-f0f5-282ac7a1d5a6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("337cee21-4cbc-253d-f17e-7dbf11541599"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("359200fe-9d40-55e3-52b6-1821b7438685"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3697228e-09e4-dbab-0d7b-58a43d2dd716"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("372edd02-7a14-f3b3-72d1-d3e027fa42eb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("383d8700-f680-385f-f524-33cf0e4bfb72"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("3a20b73b-99b1-c7bf-6b99-017acd31df5c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("3b62d952-1ca2-ae55-ed90-fe71d9d4848b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("3bd63edd-3894-fdf2-17d1-1e5b699f29bc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3c6fd9cc-7314-1ffb-f4ac-1d57ab3f4aef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("3c922e27-af12-ba97-886e-16e89297a956"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("3cd2fba4-acd2-a1f4-1891-7745cfb42380"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("3d2da141-203b-40ae-0a0c-d243f36348ce"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("3dc5b2af-04c1-eb29-b15e-ecbff0f0fc4f"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("3e52faf6-03b3-0e58-ba25-4ad63d4f92ee"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("3ebe2992-be24-27df-0bc0-dd0c85e53636"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("3eda507a-cf53-2e01-0950-a7a65946108b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("3f6e1541-1464-e68b-3e0b-5b444ad1f72b"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("3fba85da-c214-4d5e-13ee-5a3b66f8c741"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("405252c6-2a82-3522-3c7e-d65f7deae4db"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("422a028d-22c5-19ee-b1df-fdd47b65b20b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("426a0e3f-b280-25e0-b076-03e7c1a88d96"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("43813eec-3db0-3729-c81a-8daadc59f173"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("43b02d72-3431-0a4e-e865-bb1a9e886416"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("4629b82a-981b-9796-f3df-3a8dbc0de44e"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("46d9cc79-47ab-8e6a-83bd-2d375b16131d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("48a389f6-16b0-f540-72c6-e20ba1d40a64"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("48e32245-7534-2f20-96ba-2a31a31dab25"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("4aa10823-0fcf-6599-c0b8-f9ec405ba7ae"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4aa6e836-8bd0-8f15-8002-df67c2d95511"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("4b80730b-2715-d60f-7065-f746030638a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("4bd8f56a-bbe0-27e1-003d-f255a6532758"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("4d2c248c-25fb-392e-6f5a-60208b0a6e48"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("4fb31d9e-c277-7477-89a0-ae6c49db999f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("4fe26f18-ef4e-36ec-e635-a7d7720a6660"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("50d1f5c4-02f5-0771-9ce8-0b1307616b2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("5128619a-d31e-87ca-478a-b50c6791df90"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("5530f0f0-965e-0e93-9b2d-631ae75660bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("56088d7b-2c62-f188-7d63-34c07caaea0d"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("5667e068-872b-cf4e-a06c-584508676d3a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("5932721b-3fc7-7394-05bf-0f3d85ffe6aa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("59b8da95-79b2-0432-41fa-9050269d9d1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("5ac4660f-f3b0-d49c-1c95-c955d9618645"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("5cd81a6a-8e63-3b49-4128-101994edfd04"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("5e27c98e-607c-fed4-b43e-e25e948d485f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("5e7aa9cb-9fc5-d195-8baf-98f3b809a8b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("5ef477ae-5a0a-bb20-f18a-316ac7ade64d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("5fd287af-f845-0376-0a39-6cfa61d58cf2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("618bfa4d-faca-74af-b7c6-5591fef965b2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("6201ed4c-5f4c-e0db-2668-7addc500f9a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("62056203-8c08-c7b2-152e-b327a8f46bea"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("628b3ade-f181-f0c6-82ae-f2d244043090"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("63a78e76-6f15-0b55-a31f-418672cdf720"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("63adf607-2d6d-72a5-ff7f-e856de6aab11"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("64121877-340c-1bdc-325b-d3c412332b65"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("644c6e86-09ef-761c-dbd0-ad51a2f836f3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("64a8087e-3887-cbeb-90ee-2cb95c7909b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("659eca8f-f21e-daa0-cfb1-97f3f2e43e6c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("65ed132e-fc60-0269-7271-5b7a07c31ca2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("66b11bcd-7c1d-d295-cbb8-ada867cb94f4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("6787d9ce-3640-2bae-4260-af3cbed8b782"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("69a7a4a4-8e92-d2e9-1b2f-c213766de3cc"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("6a9c8ad8-b367-1a61-2588-a94b68bf2b52"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("6afce85d-da43-b61c-2824-b162c873c663"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("6bf2f44f-fad0-ea64-6c79-769b069683e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("6e8b516d-d134-51d7-6603-00ee4641d201"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("6e8f5f0e-278a-7be7-e19e-ec988f624ce5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("6ea81887-4081-0f2a-6c6e-575eeb829d02"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("6efbd431-67ef-7c73-749a-505b6e548bef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("6f53141a-cb72-c5b3-8038-b748a76ae530"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("70ee3e0b-e1a8-a0db-5469-a217db6d2bcb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("71e39728-bb8d-2e8b-2a3c-4fd77e4a0a47"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("74453ca1-b8f7-4cf7-f037-be0f7f9a28a4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("7593c831-9460-87ee-883e-a7d08024d65b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("7694cc1d-0abd-005a-7c0b-b89fbcc158b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("77fb98ba-b0e2-ef2c-2b9f-d49a83d0b44b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("78ce88c3-a409-5f0a-7979-17a0f8b041b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("7963abca-474e-b640-c9b1-50a5bfedc78a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("7a7eb399-24f8-6d48-4de1-a7b5b0e39aad"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("7acd0e7e-d083-54ab-e37c-57d9c62551e8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("7c69b181-a5c5-1b76-da6e-35406a69aae1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("7cc56aa3-23fa-11ae-46d1-623948321e63"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("7cd2f17b-5d8b-6487-e05f-e64b8479281d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("7f01070e-be22-9f0f-756f-18bf6b956c33"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("7fc64329-98c7-f899-6921-cd698f033900"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("8089c59d-2aca-4bf5-c271-111945e8a3a7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("82bcbd59-3d5a-f356-e900-e1fc8f4e69e4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("830fe0f0-9c17-f2b9-a0b2-12b9e44b84d6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("846803c7-d263-6baf-82e0-63a56d603dc7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("846b5f6a-608d-fbaf-97ef-ab11d5ceaa0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("84f9cf6d-beeb-15c5-bf9d-c755a86cf430"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("856a3274-c78c-1d6b-015d-03632d335780"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("869467c3-bd8a-a82a-ea6d-f9b19db5bab8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("87dd8ec8-4f5d-c0a3-8327-6cf6a4ad33bd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("8899102b-18ab-fa68-2e81-a99dc2b34fab"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("8990bf6d-2a8f-9671-6de3-7433fbb74836"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8b4e2f42-1331-b78e-de39-5995a0d30f36"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("8b5fae41-94e8-1ab2-0904-91120573f0b7"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("8b808c74-c3d4-b7f4-c8ed-dcac47d15424"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("8c051193-f470-e36d-2672-d11f7a2b0219"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("8c1a07d8-dc4c-c472-6bf5-1df7bf7dbc0d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("8c889da2-6c92-cf0e-845c-2040cfe9ea0e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("8d45be3c-fc01-9cc2-8015-891e6b6d1080"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("8d4d9e66-7bbf-236f-f63f-11c9b4647383"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("8e22a8ae-26e4-cc03-65e7-633b6a517175"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("8eb4e3ac-e6d2-740b-7eb8-0e62a2565e44"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("8f8d56ef-2e89-5fce-08eb-82efc9656cda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("904a9582-0819-5d89-25b1-286508c3d02c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("90745c7b-ea98-a2b4-0791-01644be47a2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("90ab054b-00bb-d9e5-1d88-73f5fcd8c548"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("90fd2936-b4b0-5b9a-01d4-1fa6d4f5f6a2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("926cd756-8fb0-2ccb-fb32-13ea6525870a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("93836533-63cc-35e5-be22-2cb8894b7454"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("966f6af1-e43e-42a8-ce08-728b7d0ab91d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("96a5e312-7794-b575-d6c0-e170f98d5f43"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("96c0cebe-6b54-d437-c401-1c05b2dd103b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("985007e6-6494-dd02-0f9c-cdab27a30d4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("99108e9c-0f3f-1f43-010a-b7ced737ef32"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("9a6279a1-74b1-36e3-e6d8-e6da88cfebf8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9ac00023-06a3-d6d1-8a83-74c5da3c7fea"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("9cdc572d-cb76-7ae4-9a53-eb357a0fb02d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("9d5fe111-8566-dce8-2860-8ac0310dea08"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("9e86b1dd-a7fd-0145-0677-5512f67f1218"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("9ec1412c-af4c-02e3-ac2f-45cfa9149c67"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("9ec4143b-df86-095c-cc5b-3d08e99395f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("9f872e58-e23b-b66e-35a1-3c72d1e69bb7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("9fe26a48-5164-292c-c077-56c475a90799"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("a061307c-2f24-842e-8c0e-0d9b9daeffcb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("a0a1fa8d-81f1-ac55-52e7-faed6fe0b611"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("a0c69eec-9aee-18eb-24bd-cc9b3daa8085"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a11d18a5-b282-4ea6-a8cd-966cfff5d966"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("a1abdcbf-fa38-d16e-03e8-9cf53318f41d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("a205685c-e1da-6d63-fcc2-4e465c002638"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a215b1a2-f495-9021-2fd6-82fd54a31700"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("a2689417-50c9-da49-5777-a90679631d48"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("a3a8df3c-3b0a-7101-526a-fdb4d732dcba"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a4358d5a-cf30-c2c8-5f9b-2cc0e7587c5e"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("a49bee3f-605f-f293-bba8-d203226f43e5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("a523ae85-9c0b-bd09-6229-fd088dc6b093"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("a5b8b6a8-e6f6-d296-a051-97e91756a93a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("a889a40b-3203-30d6-c2bf-816248b0d25a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("a8dbe752-78d7-fc8d-38b7-3661c16754ac"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("a93ccabc-4794-c2f6-3d37-19884ccb3dc9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("aa7c3f6e-ef42-0c7f-954c-07b3c457ed87"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("aade5825-905f-a69d-b974-ef2fde1452de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ab05c07c-6629-6863-148b-b7028dd5521f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("ab64a087-98fd-ce58-c891-b39b05194ced"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("ab6f475a-7d39-f24c-e5c7-be059b872fb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ab9191f2-f567-f45d-5abd-07ffcaa475fe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ac431528-72d7-fda3-b418-59a0d36b0f43"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("adcdd502-eb96-2505-71c9-bae79f2ec76f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("adebecc8-7c41-3e42-94f6-c1d9f97c1863"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("ae777567-07a5-5e98-147b-de7208e72904"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("af1b71b7-5b28-71ff-0b8e-f80f1c625a1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("af72be34-73a1-99e0-cccf-9c9c53afdbda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("af9aa99b-c0ab-7c3f-55ab-170c96fdcbb9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("affb3208-ab19-26dd-2b60-562c5e5cfd27"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("b0d1dd92-26a2-cda9-46b6-ae3a1d485e7a"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b18632d4-ce08-7cf1-80c8-f1ddd53ad048"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("b1bbf629-2618-5dce-44c9-0c75cfa25f95"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b1f19258-42d5-d3d2-b7a9-ec2070e21292"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("b2a9d65b-f747-d20d-0d7e-45572b648bc9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("b4db806a-5e1d-d2aa-979c-d8d19a5792b5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("b51251a4-13f4-53ed-799f-06f3fe8fc0a3"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("b625b6d1-eccc-5fd0-a983-22a6259994cd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("b9a9b404-28bd-f797-cd5b-8e8fd84a8b0a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ba39daea-cc90-39f3-4a49-681faecfc257"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("bab5cb53-156d-c331-b998-3c8e6d83268b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("bdf6a64a-0df0-2785-9f0f-1177de1d50a6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("beef6121-7e65-1fce-e2f3-8f62fe0bb8c5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("bf1dec49-39af-3d46-107f-f8768c7c688c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("bf4b5556-50d1-21d4-ad5c-d731d685a3ef"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("bfbfb40e-b99c-9993-bde9-749bf72206e2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("c0db68e3-2dc5-561a-a8f9-90e856226cf4"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("c0dc5975-0100-a46b-3a68-f08b53370d26"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("c1b8be25-b2c6-460d-9a28-6072291c4297"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("c28dee56-8bec-e33e-3dd1-1b8266fdc579"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("c418ec92-57b8-1bb9-3fb1-66ed13d66dbd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("c4af3501-84d4-0256-5930-6fb1f4386550"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("c531b32c-fbea-1c48-b494-26eb8db51c59"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("c62ec087-8bc1-1319-9aee-42841cc2cb67"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("c664f9a8-aac1-2c66-4950-fe8d0a4c2430"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("c77a51a2-4e3b-e953-40a7-faea97015505"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("c82ead8c-ecb4-cd22-16d5-203fe3aa41e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("c8d1d274-222b-5dd0-2ce3-28d851b28e94"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("cab229bc-99c5-02d6-a8ac-f83ceab8c336"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("cace414d-dc8f-d9ba-c2fa-42aa38e8a9a0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("cb4de670-3717-6aeb-23de-5f1791b26a50"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("cb6061cd-7db3-ac48-b473-9618f7c5024b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("cb69c0d5-aeac-9685-43f8-4d3a505b8718"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("cbb59df7-01e1-34b3-24f4-e3998b3c9fce"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("cbc259c9-6d95-23df-1ec8-4d5967df1169"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("cca246c2-60dc-fefb-9378-9b7dc704231b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("ce43339e-1d79-3c80-6110-c27249e1407d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("cf171090-d8b5-8bbf-1f09-cf39b01953dd"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("cf49ddab-ce4a-e66b-1f7a-7ffd5e7c3779"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("d2a23ff6-4e32-5802-94da-c3cf2bad90a5"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("d2ce877b-87c4-8939-1e0b-fd980179beb6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("d2e88ad7-3e31-6cf4-2033-e634e4c17ceb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d306cb4c-368c-2c9b-1077-89409efcb9f9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("d36151b6-241a-a60e-10c9-002d5022bd58"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("d4b7e9bb-ef93-96a1-cf7a-46fef4a6f6af"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("d5ac7af8-60c7-a10c-7723-8c5766b232a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("d5d678bb-996e-ac73-c99a-2f0605fa7373"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("d6b8f38b-86af-0ac5-7e97-581c08aec115"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("d82abe1c-11f3-1650-0f35-adc43cfd8e45"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("d8d61e3a-c764-d217-764c-b0ddb0d54957"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("d8dd92aa-9d84-e2d0-b79c-c3e381d95318"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("da939ff0-1ad5-618d-c010-f56081d823f7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("dcbc343e-750b-3fe5-2562-ff40af63dccd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), null, null, 0L },
                    { new Guid("dcfead7d-6432-2c14-471a-35bfff6fc6fd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("dd24724e-2f2a-9e4c-65ba-15d944d4fe9f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("de30a7c6-c3bd-02a9-4253-6cf7767b019b"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("df23630f-765d-2320-c2c0-77ee01d88572"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"), null, null, 0L },
                    { new Guid("df313adc-aabf-e303-0836-b0e171f19e09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("e2d18738-f7eb-ddd5-6f4e-7c87a471f435"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("e30f630e-07b1-2fa5-d183-a3774266f6bd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("e3b09e2d-2382-f2e5-4386-685189d312c9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e41627b8-5410-a60f-eafe-c02045b8d6e7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("e42536bf-9ca7-1f32-bfb8-3afb6e930af4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), null, null, 0L },
                    { new Guid("e46d8194-1a7a-0adb-66dd-7de372c49126"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("e5732185-aee6-05ac-5448-66bd800f108a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("e623b2e9-5eb1-cc16-7b0e-d370e1b08929"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("e6e28da6-674c-94df-0579-cac1a27e22bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e7a7015b-3a6c-9956-3c78-560c375e6c4f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), null, null, 0L },
                    { new Guid("e7e7faaf-0d61-f923-14aa-4a83358e27c1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("e89087db-d5c2-1aa8-4e8a-344b9b61650d"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("e8df651c-9a0e-1538-fc93-58eb88bd547b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"), null, null, 0L },
                    { new Guid("e9233f7f-5f43-f2a5-c288-ad2133fc2a85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("e9909612-12de-8e0e-8ee1-ec626ebfee73"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("eabf8ec0-15e8-833d-221b-09216a1274fb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"), null, null, 0L },
                    { new Guid("eaed321e-6705-0242-862f-648df84de291"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("eb22cee7-d0b9-f458-45e8-8c20892f22d5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("ebad443a-b38d-50c0-70b1-16d5cdc7dd27"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L },
                    { new Guid("ec065199-e859-2cdb-b92a-d94d25f7cd41"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("ec0787e5-d6cc-31c9-9de2-e605c3e3f41f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"), null, null, 0L },
                    { new Guid("edf294d4-23a2-7db3-17df-d51fb3242b7e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"), null, null, 0L },
                    { new Guid("ef517f5d-f399-b278-68ce-ce1438f75ef3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("ef704a63-fbd9-c948-86eb-16ee917f26db"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"), null, null, 0L },
                    { new Guid("efca1a41-5b67-3fd1-6e31-30bc93ed28d7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("efebb601-947d-4bcc-990d-874e68519092"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("f0b1e3a9-f280-4200-6d54-82795a4dce05"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"), null, null, 0L },
                    { new Guid("f11df896-6b21-11c7-e76d-19738edaee75"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f281882b-ac49-d6d5-1d06-7321ad65a23c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("f2c2d173-6222-a13a-5670-366c5392c343"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("f3c13c8c-1c8e-9fe8-f7d3-7c03ec9b4395"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("f42b50c3-5a0b-aa73-857d-9458c9bfcceb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f476dd17-da4d-1832-a8f9-38c838af9d1d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("f79f00d4-6cf4-e4ff-a123-1b51ba9a52c8"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("45eb9032-3689-8526-caee-41db0e7e2644"), null, null, 0L },
                    { new Guid("facfe5f2-40e0-709c-9244-1b46cba82981"), false, true, true, false, true, false, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("fb2f67b4-6f92-4982-87fb-a6f19856ef11"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), null, null, 0L },
                    { new Guid("fb3764c9-95da-27af-b6f2-2ea6387262a8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("fb644d21-29f5-811c-79b5-c440415f2ef9"), true, true, true, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), null, null, 0L },
                    { new Guid("fd943b39-60d2-01e0-2978-7313334b7cc0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), null, null, 0L },
                    { new Guid("fdf4faec-0492-3386-311d-4b366d490863"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"), null, null, 0L },
                    { new Guid("fe9aff9c-bcbf-a921-59d0-15536ba33d22"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"), null, null, 0L },
                    { new Guid("fea6b003-57fb-420d-e2de-6fa446866317"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("c4133420-c386-9452-93a7-484e18105372"), null, null, 0L },
                    { new Guid("fea74237-eda4-9622-b2f2-e4a5344fd686"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"), null, null, 0L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("11744032-08e9-f364-d36f-c12caeff0b02"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("2a23e241-204c-4810-46cd-5f1b0f513434"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("2e2eb9a5-7caa-e157-2099-e3f06e85fbad"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("51a38ab8-5943-e4f6-6140-76dea2057e8b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("bf16025e-df11-ac0e-785b-4873e1a14af3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "audit_logs",
                keyColumn: "Id",
                keyValue: new Guid("bf6ef4ae-fe3a-2861-28d4-88f7708aba51"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("1a2a5dec-8b78-3dfc-0bc2-5b6bb336fc01"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("205f6311-eba5-4e1c-98ed-17ca94e92b44"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("23b32cf1-c5a1-6049-4f33-03950ec24ce2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("2a0fb58f-2f46-2566-d05f-6fcd92c66fed"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("2ab8a02f-674f-99d0-6c92-ce3c6dc00663"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("2f900ed8-9a79-65e8-a307-f71aa6314a5a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("3374eb17-8fb6-b10a-6a44-3be3153f170f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("34d2f6f1-8ada-a885-0d8f-f2ad198281f8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("3547cdb4-9ff3-e9d8-aa74-656ba070fef0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("4303b834-9774-a18c-8633-6e1fe106e392"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("4731bc5c-5c63-790b-85b2-f765faedefa4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("48803a06-69f4-567d-532d-ab1b013b72ad"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("4eb6a19d-9df1-cd9b-25bd-c579ed7552c0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("5f8ae880-c6ba-df5d-2c0b-c52bf71a618d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("702445e8-b85d-b073-d05b-84650d3b6a97"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("743e6628-613e-2cad-032a-dff4f833d6f6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("77b778cb-98fd-62e6-9649-0dae69949e4e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("86472fee-4ed8-8ac7-cfc4-e78a4c3cfc3f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("898f649c-3f51-4f3a-fc46-1fc43dfb66a2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("9ffd8d2f-c652-162e-bd22-a9b125e6a8c7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("a7c5f3dc-aa26-101c-4874-8d9f225535b1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("aca4b500-66b1-cdf9-b28b-aa7d8551862e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("b0367265-16b4-52d6-3ac6-ccc4b33d19a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("b4435402-3a74-077a-e1d3-5032a6edcf38"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("c4a093bf-f81d-5b3e-e389-9eb4950c566c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("c9ce6e48-2658-5935-7265-c011ba95289a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("cfa8f7dc-45b5-d440-4b57-c34f58d4a4d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("d91817a8-88ec-93fc-9bc9-6942645adcff"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("dc1eaea8-b142-2f3f-a15c-d1ff5ce8d00a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("dd47ee1a-bb03-d9a5-4b3a-3cd487c7cdfb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("e05f8f93-f979-12d3-ace5-3f69f918ec1c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("e0aab112-f4c5-b21d-6102-9467c0a95550"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("e4f6a991-75a9-d2a2-4216-d74fbd34c58f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("e5064514-d384-8f16-e12e-4c203da968af"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("e788746d-af16-703a-d04b-8bf390e27424"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("ebd5a713-4f24-4650-20a4-abf789301415"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("ed5fd5a7-91de-64c1-0b9a-7236cc964595"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("f2d23aba-aa01-573a-a48c-dc52f57a35ab"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "employee_status_history",
                keyColumn: "Id",
                keyValue: new Guid("fe374d1c-2cf7-702a-70c1-464e2ed31f34"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("010a0c72-51e7-5832-2267-3788f0e50446"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0296b8ac-3ef8-319e-2bbd-52fc1434991a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("031586f9-bb24-8506-db97-f5714fa795ec"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("03992abd-35a5-cdf5-c20d-6febeefceb22"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0465f200-8bf3-8526-6ff2-7cabe33dc321"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("049a5c74-1f4e-5f6b-ada7-6ee9e078f31b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("058ec479-426d-9ccb-79ab-06ba7768ccd5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("07627316-54e4-db4d-77ef-0f161f685487"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("082c708f-c0ee-a4b7-547a-b5547cee5a48"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("08db82bf-3428-9428-5102-888698acbaaa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("09b4df68-6e86-6dd3-a000-2e81cbfda172"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("09e6faf2-aa84-4975-dd32-33617250adb0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0a41b95f-94b3-3462-7529-66fe94b49291"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0f11ca79-fd0e-dfad-030c-865843cb8512"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0f6fe7be-2afc-f4e7-2245-a5366599dfa9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12a9d791-55b7-7c0d-fb6d-99780a741e5b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12fcb308-740b-14aa-cc1f-0197ce4c2448"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("144a76c1-f002-aee5-6f2d-3beb9a95aec5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("15ebeb75-142b-95a5-0c8f-f76f67e2cb93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("16244354-0b37-2d19-cb30-f5f42725e630"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1668624a-f2ad-0829-d2dd-0d4ea7ed0de4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("16ae3bf1-2b26-07e6-ed2a-6778ac80d373"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17579bb5-969a-3378-52b7-76e4f6cabfc6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17df4e3d-7834-3baf-449f-432487209c99"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17e0a6d4-754a-5b66-dd33-9cf605995071"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18203f3c-be44-3b65-acce-669dfcc2f9d1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18374130-7861-27c5-cea8-0dc5824ada09"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("187b9f2e-0af9-56bf-865c-2e5e656737c4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18919a69-5da5-457f-b7e9-414d3df60136"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1a376031-e48b-5c6a-79c2-01e348af1cc3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1a9955f2-be51-7afc-9626-52c09c992beb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1c37266c-c716-12eb-c9c5-a7c9c1031fb8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1da21651-8f3a-3aa3-ce70-bcc28303030c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1e9ef48a-863a-76cd-8e93-9019dbb37814"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1f3c480c-606c-bfb1-f604-8218c9fb63e3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("227f9917-a233-3837-aadf-523264527624"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2339ab91-b159-f632-2013-3bf1f1d9bd93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2471ce82-f75c-f7be-d738-477687b33f82"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("260b7c1e-4743-8986-2c40-ed65cbecb2b0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("277593dc-13ae-9384-7d93-964c3d2249e7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("29e8e885-ec5b-77d1-548e-1c3717588eec"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2a596a42-8571-37e5-3bcc-d6ca9da53341"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2a643fb6-252a-b03c-da85-d34692718ad8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ad1d150-3b2b-e715-3d30-4c3794b7fae6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ae27e32-6e47-856a-5d8a-c390ce208334"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2b65dc13-1086-a99b-f478-4dd973f00f06"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2c05b987-114e-af45-ab8e-84ceb61f5f62"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2cacae30-7781-df69-ec66-a203c3d7b4a7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2cef3812-dbb8-0935-18e8-74ce8cbab6a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2cf53de7-abeb-d10b-84dc-9293a7af5ad7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2d5616c6-3914-444d-5b0a-4d6267c96956"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2db7161b-d98c-3932-b43d-a06699323626"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ec28c45-6e02-1965-8849-2aadbec9262a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f172da3-f681-1b9f-1d6c-ece0b9692e1f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f984f86-453b-52e8-88fd-6ccfb8ef34c7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f9bec8e-895b-c9fb-78a0-85fa7713b999"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2fdcd65f-cdc7-b1fb-6b9f-544b405f1990"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3041366d-bcf6-258b-a2be-7a88cf728455"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("30b5b613-ff4a-81cd-005f-1df54c743c77"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("30eeedb1-7d9f-15f5-3bdd-00a5aa01ce1e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("319239ca-5893-0244-c8e5-2544b8e881de"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("31ae89bc-fe23-612c-3a1a-03341c4efde5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("31d5b574-123f-4a5b-abe0-4468da1100c5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("321003ea-d45d-d309-8c38-72194f7b7e2b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32e5d033-63ed-ceca-bbcb-522d43909bc7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32e72d3e-b825-9842-a7fd-4e06bbb085ea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3354553c-ad03-69a5-f0f5-282ac7a1d5a6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("337cee21-4cbc-253d-f17e-7dbf11541599"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("359200fe-9d40-55e3-52b6-1821b7438685"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3697228e-09e4-dbab-0d7b-58a43d2dd716"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("372edd02-7a14-f3b3-72d1-d3e027fa42eb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("383d8700-f680-385f-f524-33cf0e4bfb72"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a20b73b-99b1-c7bf-6b99-017acd31df5c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3b62d952-1ca2-ae55-ed90-fe71d9d4848b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3bd63edd-3894-fdf2-17d1-1e5b699f29bc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c6fd9cc-7314-1ffb-f4ac-1d57ab3f4aef"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c922e27-af12-ba97-886e-16e89297a956"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3cd2fba4-acd2-a1f4-1891-7745cfb42380"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3d2da141-203b-40ae-0a0c-d243f36348ce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3dc5b2af-04c1-eb29-b15e-ecbff0f0fc4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3e52faf6-03b3-0e58-ba25-4ad63d4f92ee"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3ebe2992-be24-27df-0bc0-dd0c85e53636"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3eda507a-cf53-2e01-0950-a7a65946108b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f6e1541-1464-e68b-3e0b-5b444ad1f72b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3fba85da-c214-4d5e-13ee-5a3b66f8c741"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("405252c6-2a82-3522-3c7e-d65f7deae4db"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("422a028d-22c5-19ee-b1df-fdd47b65b20b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("426a0e3f-b280-25e0-b076-03e7c1a88d96"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("43813eec-3db0-3729-c81a-8daadc59f173"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("43b02d72-3431-0a4e-e865-bb1a9e886416"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4629b82a-981b-9796-f3df-3a8dbc0de44e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("46d9cc79-47ab-8e6a-83bd-2d375b16131d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("48a389f6-16b0-f540-72c6-e20ba1d40a64"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("48e32245-7534-2f20-96ba-2a31a31dab25"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4aa10823-0fcf-6599-c0b8-f9ec405ba7ae"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4aa6e836-8bd0-8f15-8002-df67c2d95511"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4b80730b-2715-d60f-7065-f746030638a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4bd8f56a-bbe0-27e1-003d-f255a6532758"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4d2c248c-25fb-392e-6f5a-60208b0a6e48"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4fb31d9e-c277-7477-89a0-ae6c49db999f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4fe26f18-ef4e-36ec-e635-a7d7720a6660"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("50d1f5c4-02f5-0771-9ce8-0b1307616b2a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5128619a-d31e-87ca-478a-b50c6791df90"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5530f0f0-965e-0e93-9b2d-631ae75660bb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("56088d7b-2c62-f188-7d63-34c07caaea0d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5667e068-872b-cf4e-a06c-584508676d3a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5932721b-3fc7-7394-05bf-0f3d85ffe6aa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("59b8da95-79b2-0432-41fa-9050269d9d1d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5ac4660f-f3b0-d49c-1c95-c955d9618645"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5cd81a6a-8e63-3b49-4128-101994edfd04"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5e27c98e-607c-fed4-b43e-e25e948d485f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5e7aa9cb-9fc5-d195-8baf-98f3b809a8b0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5ef477ae-5a0a-bb20-f18a-316ac7ade64d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5fd287af-f845-0376-0a39-6cfa61d58cf2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("618bfa4d-faca-74af-b7c6-5591fef965b2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6201ed4c-5f4c-e0db-2668-7addc500f9a7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("62056203-8c08-c7b2-152e-b327a8f46bea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("628b3ade-f181-f0c6-82ae-f2d244043090"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("63a78e76-6f15-0b55-a31f-418672cdf720"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("63adf607-2d6d-72a5-ff7f-e856de6aab11"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("64121877-340c-1bdc-325b-d3c412332b65"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("644c6e86-09ef-761c-dbd0-ad51a2f836f3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("64a8087e-3887-cbeb-90ee-2cb95c7909b6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("659eca8f-f21e-daa0-cfb1-97f3f2e43e6c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("65ed132e-fc60-0269-7271-5b7a07c31ca2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("66b11bcd-7c1d-d295-cbb8-ada867cb94f4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6787d9ce-3640-2bae-4260-af3cbed8b782"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("69a7a4a4-8e92-d2e9-1b2f-c213766de3cc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a9c8ad8-b367-1a61-2588-a94b68bf2b52"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6afce85d-da43-b61c-2824-b162c873c663"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6bf2f44f-fad0-ea64-6c79-769b069683e4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6e8b516d-d134-51d7-6603-00ee4641d201"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6e8f5f0e-278a-7be7-e19e-ec988f624ce5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ea81887-4081-0f2a-6c6e-575eeb829d02"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6efbd431-67ef-7c73-749a-505b6e548bef"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6f53141a-cb72-c5b3-8038-b748a76ae530"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("70ee3e0b-e1a8-a0db-5469-a217db6d2bcb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("71e39728-bb8d-2e8b-2a3c-4fd77e4a0a47"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("74453ca1-b8f7-4cf7-f037-be0f7f9a28a4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7593c831-9460-87ee-883e-a7d08024d65b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7694cc1d-0abd-005a-7c0b-b89fbcc158b4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("77fb98ba-b0e2-ef2c-2b9f-d49a83d0b44b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("78ce88c3-a409-5f0a-7979-17a0f8b041b4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7963abca-474e-b640-c9b1-50a5bfedc78a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a7eb399-24f8-6d48-4de1-a7b5b0e39aad"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7acd0e7e-d083-54ab-e37c-57d9c62551e8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7c69b181-a5c5-1b76-da6e-35406a69aae1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7cc56aa3-23fa-11ae-46d1-623948321e63"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7cd2f17b-5d8b-6487-e05f-e64b8479281d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7f01070e-be22-9f0f-756f-18bf6b956c33"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7fc64329-98c7-f899-6921-cd698f033900"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8089c59d-2aca-4bf5-c271-111945e8a3a7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("82bcbd59-3d5a-f356-e900-e1fc8f4e69e4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("830fe0f0-9c17-f2b9-a0b2-12b9e44b84d6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("846803c7-d263-6baf-82e0-63a56d603dc7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("846b5f6a-608d-fbaf-97ef-ab11d5ceaa0d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84f9cf6d-beeb-15c5-bf9d-c755a86cf430"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("856a3274-c78c-1d6b-015d-03632d335780"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("869467c3-bd8a-a82a-ea6d-f9b19db5bab8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("87dd8ec8-4f5d-c0a3-8327-6cf6a4ad33bd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8899102b-18ab-fa68-2e81-a99dc2b34fab"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8990bf6d-2a8f-9671-6de3-7433fbb74836"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b4e2f42-1331-b78e-de39-5995a0d30f36"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b5fae41-94e8-1ab2-0904-91120573f0b7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b808c74-c3d4-b7f4-c8ed-dcac47d15424"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c051193-f470-e36d-2672-d11f7a2b0219"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c1a07d8-dc4c-c472-6bf5-1df7bf7dbc0d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c889da2-6c92-cf0e-845c-2040cfe9ea0e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8d45be3c-fc01-9cc2-8015-891e6b6d1080"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8d4d9e66-7bbf-236f-f63f-11c9b4647383"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8e22a8ae-26e4-cc03-65e7-633b6a517175"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8eb4e3ac-e6d2-740b-7eb8-0e62a2565e44"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f8d56ef-2e89-5fce-08eb-82efc9656cda"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("904a9582-0819-5d89-25b1-286508c3d02c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("90745c7b-ea98-a2b4-0791-01644be47a2a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("90ab054b-00bb-d9e5-1d88-73f5fcd8c548"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("90fd2936-b4b0-5b9a-01d4-1fa6d4f5f6a2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("926cd756-8fb0-2ccb-fb32-13ea6525870a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("93836533-63cc-35e5-be22-2cb8894b7454"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("966f6af1-e43e-42a8-ce08-728b7d0ab91d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("96a5e312-7794-b575-d6c0-e170f98d5f43"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("96c0cebe-6b54-d437-c401-1c05b2dd103b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("985007e6-6494-dd02-0f9c-cdab27a30d4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("99108e9c-0f3f-1f43-010a-b7ced737ef32"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9a6279a1-74b1-36e3-e6d8-e6da88cfebf8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9ac00023-06a3-d6d1-8a83-74c5da3c7fea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9cdc572d-cb76-7ae4-9a53-eb357a0fb02d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9d5fe111-8566-dce8-2860-8ac0310dea08"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9e86b1dd-a7fd-0145-0677-5512f67f1218"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9ec1412c-af4c-02e3-ac2f-45cfa9149c67"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9ec4143b-df86-095c-cc5b-3d08e99395f7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9f872e58-e23b-b66e-35a1-3c72d1e69bb7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9fe26a48-5164-292c-c077-56c475a90799"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a061307c-2f24-842e-8c0e-0d9b9daeffcb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a0a1fa8d-81f1-ac55-52e7-faed6fe0b611"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a0c69eec-9aee-18eb-24bd-cc9b3daa8085"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a11d18a5-b282-4ea6-a8cd-966cfff5d966"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a1abdcbf-fa38-d16e-03e8-9cf53318f41d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a205685c-e1da-6d63-fcc2-4e465c002638"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a215b1a2-f495-9021-2fd6-82fd54a31700"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a2689417-50c9-da49-5777-a90679631d48"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a3a8df3c-3b0a-7101-526a-fdb4d732dcba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a4358d5a-cf30-c2c8-5f9b-2cc0e7587c5e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a49bee3f-605f-f293-bba8-d203226f43e5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a523ae85-9c0b-bd09-6229-fd088dc6b093"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a5b8b6a8-e6f6-d296-a051-97e91756a93a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a889a40b-3203-30d6-c2bf-816248b0d25a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a8dbe752-78d7-fc8d-38b7-3661c16754ac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a93ccabc-4794-c2f6-3d37-19884ccb3dc9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aa7c3f6e-ef42-0c7f-954c-07b3c457ed87"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aade5825-905f-a69d-b974-ef2fde1452de"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab05c07c-6629-6863-148b-b7028dd5521f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab64a087-98fd-ce58-c891-b39b05194ced"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab6f475a-7d39-f24c-e5c7-be059b872fb9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ab9191f2-f567-f45d-5abd-07ffcaa475fe"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ac431528-72d7-fda3-b418-59a0d36b0f43"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("adcdd502-eb96-2505-71c9-bae79f2ec76f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("adebecc8-7c41-3e42-94f6-c1d9f97c1863"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ae777567-07a5-5e98-147b-de7208e72904"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af1b71b7-5b28-71ff-0b8e-f80f1c625a1d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af72be34-73a1-99e0-cccf-9c9c53afdbda"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af9aa99b-c0ab-7c3f-55ab-170c96fdcbb9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("affb3208-ab19-26dd-2b60-562c5e5cfd27"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b0d1dd92-26a2-cda9-46b6-ae3a1d485e7a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b18632d4-ce08-7cf1-80c8-f1ddd53ad048"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b1bbf629-2618-5dce-44c9-0c75cfa25f95"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b1f19258-42d5-d3d2-b7a9-ec2070e21292"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b2a9d65b-f747-d20d-0d7e-45572b648bc9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b4db806a-5e1d-d2aa-979c-d8d19a5792b5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b51251a4-13f4-53ed-799f-06f3fe8fc0a3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b625b6d1-eccc-5fd0-a983-22a6259994cd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b9a9b404-28bd-f797-cd5b-8e8fd84a8b0a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ba39daea-cc90-39f3-4a49-681faecfc257"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bab5cb53-156d-c331-b998-3c8e6d83268b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bdf6a64a-0df0-2785-9f0f-1177de1d50a6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("beef6121-7e65-1fce-e2f3-8f62fe0bb8c5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bf1dec49-39af-3d46-107f-f8768c7c688c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bf4b5556-50d1-21d4-ad5c-d731d685a3ef"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bfbfb40e-b99c-9993-bde9-749bf72206e2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0db68e3-2dc5-561a-a8f9-90e856226cf4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0dc5975-0100-a46b-3a68-f08b53370d26"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1b8be25-b2c6-460d-9a28-6072291c4297"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c28dee56-8bec-e33e-3dd1-1b8266fdc579"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c418ec92-57b8-1bb9-3fb1-66ed13d66dbd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c4af3501-84d4-0256-5930-6fb1f4386550"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c531b32c-fbea-1c48-b494-26eb8db51c59"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c62ec087-8bc1-1319-9aee-42841cc2cb67"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c664f9a8-aac1-2c66-4950-fe8d0a4c2430"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c77a51a2-4e3b-e953-40a7-faea97015505"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c82ead8c-ecb4-cd22-16d5-203fe3aa41e7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c8d1d274-222b-5dd0-2ce3-28d851b28e94"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cab229bc-99c5-02d6-a8ac-f83ceab8c336"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cace414d-dc8f-d9ba-c2fa-42aa38e8a9a0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cb4de670-3717-6aeb-23de-5f1791b26a50"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cb6061cd-7db3-ac48-b473-9618f7c5024b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cb69c0d5-aeac-9685-43f8-4d3a505b8718"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cbb59df7-01e1-34b3-24f4-e3998b3c9fce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cbc259c9-6d95-23df-1ec8-4d5967df1169"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cca246c2-60dc-fefb-9378-9b7dc704231b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ce43339e-1d79-3c80-6110-c27249e1407d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cf171090-d8b5-8bbf-1f09-cf39b01953dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cf49ddab-ce4a-e66b-1f7a-7ffd5e7c3779"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2a23ff6-4e32-5802-94da-c3cf2bad90a5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2ce877b-87c4-8939-1e0b-fd980179beb6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d2e88ad7-3e31-6cf4-2033-e634e4c17ceb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d306cb4c-368c-2c9b-1077-89409efcb9f9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d36151b6-241a-a60e-10c9-002d5022bd58"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d4b7e9bb-ef93-96a1-cf7a-46fef4a6f6af"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5ac7af8-60c7-a10c-7723-8c5766b232a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d5d678bb-996e-ac73-c99a-2f0605fa7373"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d6b8f38b-86af-0ac5-7e97-581c08aec115"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d82abe1c-11f3-1650-0f35-adc43cfd8e45"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8d61e3a-c764-d217-764c-b0ddb0d54957"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8dd92aa-9d84-e2d0-b79c-c3e381d95318"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("da939ff0-1ad5-618d-c010-f56081d823f7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dcbc343e-750b-3fe5-2562-ff40af63dccd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dcfead7d-6432-2c14-471a-35bfff6fc6fd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dd24724e-2f2a-9e4c-65ba-15d944d4fe9f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("de30a7c6-c3bd-02a9-4253-6cf7767b019b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("df23630f-765d-2320-c2c0-77ee01d88572"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("df313adc-aabf-e303-0836-b0e171f19e09"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e2d18738-f7eb-ddd5-6f4e-7c87a471f435"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e30f630e-07b1-2fa5-d183-a3774266f6bd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e3b09e2d-2382-f2e5-4386-685189d312c9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e41627b8-5410-a60f-eafe-c02045b8d6e7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e42536bf-9ca7-1f32-bfb8-3afb6e930af4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e46d8194-1a7a-0adb-66dd-7de372c49126"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e5732185-aee6-05ac-5448-66bd800f108a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e623b2e9-5eb1-cc16-7b0e-d370e1b08929"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e6e28da6-674c-94df-0579-cac1a27e22bb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e7a7015b-3a6c-9956-3c78-560c375e6c4f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e7e7faaf-0d61-f923-14aa-4a83358e27c1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e89087db-d5c2-1aa8-4e8a-344b9b61650d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e8df651c-9a0e-1538-fc93-58eb88bd547b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e9233f7f-5f43-f2a5-c288-ad2133fc2a85"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e9909612-12de-8e0e-8ee1-ec626ebfee73"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("eabf8ec0-15e8-833d-221b-09216a1274fb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("eaed321e-6705-0242-862f-648df84de291"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("eb22cee7-d0b9-f458-45e8-8c20892f22d5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebad443a-b38d-50c0-70b1-16d5cdc7dd27"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ec065199-e859-2cdb-b92a-d94d25f7cd41"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ec0787e5-d6cc-31c9-9de2-e605c3e3f41f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("edf294d4-23a2-7db3-17df-d51fb3242b7e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ef517f5d-f399-b278-68ce-ce1438f75ef3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ef704a63-fbd9-c948-86eb-16ee917f26db"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("efca1a41-5b67-3fd1-6e31-30bc93ed28d7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("efebb601-947d-4bcc-990d-874e68519092"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f0b1e3a9-f280-4200-6d54-82795a4dce05"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f11df896-6b21-11c7-e76d-19738edaee75"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f281882b-ac49-d6d5-1d06-7321ad65a23c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f2c2d173-6222-a13a-5670-366c5392c343"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3c13c8c-1c8e-9fe8-f7d3-7c03ec9b4395"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f42b50c3-5a0b-aa73-857d-9458c9bfcceb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f476dd17-da4d-1832-a8f9-38c838af9d1d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f79f00d4-6cf4-e4ff-a123-1b51ba9a52c8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("facfe5f2-40e0-709c-9244-1b46cba82981"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb2f67b4-6f92-4982-87fb-a6f19856ef11"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb3764c9-95da-27af-b6f2-2ea6387262a8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb644d21-29f5-811c-79b5-c440415f2ef9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd943b39-60d2-01e0-2978-7313334b7cc0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fdf4faec-0492-3386-311d-4b366d490863"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fe9aff9c-bcbf-a921-59d0-15536ba33d22"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fea6b003-57fb-420d-e2de-6fa446866317"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fea74237-eda4-9622-b2f2-e4a5344fd686"));

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "nexa",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "Result",
                schema: "nexa",
                table: "audit_logs");

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3be62adc-bee6-6ae9-55d1-ae4209ae72ee"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("553a4767-4479-5fd3-9b6a-8606fb8c12f3"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f7d23fd6-2d22-262a-7d9e-d9247a8021f5"),
                columns: new[] { "CanCancel", "CanCreate", "CanDeactivate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false, false });
        }
    }
}

