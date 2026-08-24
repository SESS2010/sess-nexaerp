using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev866EmployeePermissionMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCancel",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDeactivate",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanDownload",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanPrint",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanReject",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanReplaceAttachment",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanRequestClarification",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanRequestRevision",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanResubmit",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanSubmit",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanUploadAttachment",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanVerify",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewAuditHistory",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewCommercialValues",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasFullControl",
                schema: "nexa",
                table: "role_page_permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "departments",
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
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "designations",
                schema: "nexa",
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
                name: "skills",
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
                    table.PrimaryKey("PK_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "nexa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginalImportedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Grade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DesignationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: true),
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
                        principalSchema: "nexa",
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_designations_DesignationId",
                        column: x => x.DesignationId,
                        principalSchema: "nexa",
                        principalTable: "designations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_approval_history",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_import_history",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_role_assignments",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_role_assignments_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "nexa",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_skills",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_skills_skills_SkillId",
                        column: x => x.SkillId,
                        principalSchema: "nexa",
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_status_history",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reporting_relationships",
                schema: "nexa",
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
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reporting_relationships_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reporting_relationships_employees_ReportingManagerEmployeeId",
                        column: x => x.ReportingManagerEmployeeId,
                        principalSchema: "nexa",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "nexa",
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
                schema: "nexa",
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
                schema: "nexa",
                table: "page_definitions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000016"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.master", "/employees", "Employee Master", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000017"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.role-mapping", "/employees/roles", "Employee Role Mapping", null, null, 0L },
                    { new Guid("20000000-0000-0000-0000-000000000018"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Employees", "employees.audit-history", "/employees/audit-history", "Employee Audit History", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("00cbfa57-17fb-9bc9-ebc2-d82593db20c0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("01a8ed83-0e17-63e1-ff4c-cdc4dadcd776"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("01ba57da-bf38-37ff-1b4d-7a89bba40f68"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("03043ba5-389c-3233-01eb-fc5a0b52e88f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("032618b3-6ddd-dbb6-c6a7-9fa81b357f37"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("036447eb-18a7-241a-c0e7-6c84b3fd572a"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("03c74b25-7022-9594-cca0-2ded65991f10"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("03ebd08f-a093-d3cf-8f87-46300c8d1dba"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("03f83275-7beb-0b99-204e-b232181c659f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("04d5edd1-bd0c-fa30-5694-836b6f46cc46"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("07e0a0a9-0c56-ff51-7a64-df05cb4d8641"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("08286f2c-f6f7-fac1-de8c-a4736570cc51"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("08307be7-8234-e259-ae74-f9392ed2a1fb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("0967d616-a202-f778-22f4-5c0c5606efd3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("0b3c3f4a-2d9a-ac8f-d9ae-9ff61418f67b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0b6178f3-935b-5f40-be62-1209ebaee582"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("0b7594fe-4132-dd48-944a-6107faae95f2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("0b97b7a0-d2ac-a4da-0930-5296011b4496"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("0c3e02e8-2bcd-4d05-a3c8-312d7d66ba22"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("0df3abd0-c90c-3d36-91bb-68b49e0f2605"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("0ef21be8-e408-8a8e-e2c6-3e789e64302b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("0f4187ee-9df8-cf46-4ace-4f8349bdbf37"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("0fcdc8eb-2a3f-7ea7-b022-c3396d868d56"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("0fdc8bf6-3644-6d7c-913e-c5d93ecebda4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("1067e842-f711-c5f8-c54f-605d218e3e9b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("10a312ff-3606-ee0c-b384-03bf891c5d8f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("11b71479-d821-6aa9-75d9-307f56d90621"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("1216c9a3-31eb-e2bb-3238-9c3b6dde5daf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("12694a69-1d2c-4e6b-a81f-65ce1582f29f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("13ca2fe4-4115-8977-0feb-782fe436d5eb"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("13e130d5-1294-8526-2109-72829c861c16"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("14ffdc95-b241-a3c5-9968-ef467797859b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("16bcfa5d-19d9-48d6-8c65-8fe9b00ad2f2"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("1737277b-1cfa-ad32-67a2-49fd84c7b8dc"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("173f2300-ec29-19df-e9e3-1370ab9c8ad9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("175418b4-d466-1033-67bf-185f2dda3fe1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("175aef9a-6f31-588c-9e0c-cbf21dfef7ac"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("17f30c40-8f89-f202-f1da-648eb7c00612"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("18f0cf47-9b69-ae81-4ca6-c669be41d7d0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("18fec771-1b4b-ebbf-bf98-f4747886f977"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("19cd5147-1ca2-70e2-8ef8-33ceb788c475"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("1d7d16c0-8e5c-3264-41fe-eedd38702c06"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("1e76f7c6-0594-5c28-7832-f4bc37ca9daa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("1e7ae789-44e0-cb9f-be17-5eaad290a8d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("1eaeb950-7c69-f801-20ce-03703c14aaed"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("1ed09b6b-9e2f-5689-c92d-37fa81cd429a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("20362439-ea6e-017b-307e-766fb7088540"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("20cd5bd9-af1d-f904-28a8-13249e3ca0b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("21a710c7-4273-ee01-12c4-61303f20ea47"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("2217a7b5-f36e-855b-5b50-4f98715465b5"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("2274b082-d44b-fb16-5a0b-9e7729e9c9d9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("2315442d-921d-6442-1e1b-143e5c4acfb1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("233d6b2d-7eb7-e571-78fb-ff25933a5e48"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("246095fe-bc58-8e7d-d062-fb7f4f5c1a34"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("26dbfb50-8443-c634-2459-3ba1e8429e33"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("27a09995-798f-9a09-ec9e-51bcadea8a79"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("2839ba98-7359-453d-7968-5e5a22aa489d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("28b621bd-b402-9bec-6717-9a957209f5b4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("29257d90-af79-fe7d-82d6-160a25556b29"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("293b30ad-309e-8464-d5c3-837ed16b4c41"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("2a6db5b8-5435-d8c0-22a6-88f577cec4b2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("2ab9cd5a-1606-e54f-ec1c-6dbc407d1bb2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("2afb089d-1f11-e510-46b9-564fbda0ee6d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("2bd2493e-80ef-7ef3-f048-4f5826939ba3"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("2cbfda68-1e6a-929b-a6e4-795789e53e71"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("2d9bb0d0-0f85-e269-8cb9-cea7687c742f"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("2e76c3fd-70d4-ab42-f62f-bda8a16c88d2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("2efcabb0-eabb-859a-508d-ad96495f9d36"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("30ac549b-cf0e-c6ca-9838-b92ac677daee"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("310dc945-7894-7776-9ab9-9071254b5c9c"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("31454649-cc19-b624-8661-3c4e342209d1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("3151527f-891a-560e-508c-26fca6b35bb4"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("31948518-8d84-4d18-de4c-7d303b6dd21c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("320c9c09-5bfe-dcb2-dc7a-50f74bf98804"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("32fbab8f-8022-26f7-af56-98b45eb2cf25"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("33482c08-bf8b-3427-9733-e3f85def2a8f"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("335bcbec-6b9b-2035-e881-ddb219d6a889"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("343bb085-d954-5380-263b-d1d74a9d9ae6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("344fb35c-86d4-a015-745f-98dddc95a13f"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("34545002-3f06-d2bb-8275-f3fbb141a710"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("35201627-24fe-6abb-0dc9-9eeecc5e415b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("35ef158d-6125-9adc-e7ff-74704aab6f44"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("366d6ddb-79e6-6d7e-2948-ed012149ee4a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("37079265-cf1b-157b-0c44-8fd278dc6664"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("37ab5b42-3b7b-e4b5-34d5-83b4d3894073"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("390c816f-29ff-c417-ba72-e1ad9b249a3f"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("39e6443c-1cab-ba55-d870-4b7e9c6cb059"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("3a074d30-3401-99af-48bc-f3553ae95899"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("3c1b91c1-9093-2729-ce6f-de1903123924"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("3d97d051-a73c-2db6-64b9-7a5ed1c267a6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("3db66f31-7496-a50d-fd09-189c6d86a635"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("3dc74640-7587-f7cb-87bf-847feeb760a2"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("3f47dc2b-9f9a-68e1-3bcf-fb4fc442f638"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("406e39ee-9bf0-72c3-d671-75a37a6c6816"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("4091008e-5890-810d-f307-9b419f743026"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("41496500-2f79-9184-b5d3-18f7246eed85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("415daeac-f621-1206-ee3c-b9b43aff6984"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("42243842-8c8c-7642-37ca-9ed5ee13225e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("4446230b-493d-9f12-ce7c-71b2add3e74e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("4642b120-04d3-2ddf-51ff-bc6bcf260f07"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("466753a8-43f4-6c6c-3f8f-ab11d750a794"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("467972a2-cd85-1f32-6e68-f41409e32d91"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("479c596f-8198-41ca-34f4-e066cc121cd2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("48cac800-a59d-0e09-6041-87174034c019"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("49ebb447-9fa7-38eb-aa2e-b97617549c12"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("4a73d4d8-7ab1-2945-568f-9cb8aeeaed82"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("4d2ada08-b246-dda1-ff81-dbaac36cb406"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("4debe1f5-0d5e-90c7-935e-684a0484d7ed"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("4e77844a-0f09-6009-86cf-eec6c8bbcc42"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("50f67d75-9555-756d-5fc5-fc92a88da34c"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("5157b67b-7a2f-4887-4093-de4bd6cc8e2d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("5163ce51-bb0d-d8a2-6ca3-b515a19e8df2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("51d1c1f7-16ab-d29d-dd50-2f6f33aa3073"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("51dab92d-f7d5-d870-a13c-ca7c37f498e6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("51f714f5-c507-34a7-bf0c-3380d85db6a0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("535fb558-59b4-c3a8-01bb-503c161a7505"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("53e325a6-d795-1a83-eeb8-34c6bcec636f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("543cae26-dbd2-59d8-b366-3a8f1aeddc20"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("56c49867-b079-a83b-38c8-b170f4ee32cf"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("57064a0d-0927-ee99-2332-c8fd07790e73"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("580a98f6-3f04-60c8-b75b-78f7fa7f6cd1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("5c5deb8d-b053-8d30-6f27-307f31576ea0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("5d5ca2ed-f113-4ce6-c77a-8955d3db135c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("5df893f5-2499-20e9-1666-29f0a9f88b96"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("6164a7ec-aadb-0213-7bae-5a1d8178422b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("646ad30b-1811-3855-e608-cafca7c51a07"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("653654f5-c2ae-45b3-ecb4-507add141ea8"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("679be5fe-4b9a-cca7-b8bf-59917f04c9e0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("688fee8c-dde0-dbe9-b0d8-7750152d37b9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("68e9ad66-3620-3330-e529-e8d686874e1a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("6987c49f-5d17-db47-280b-8298904ad323"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("6a9dda69-8fb3-850f-53e1-3e7b8855e0ff"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("6cf58043-56d1-1766-ebb3-ce7a8dc63e06"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("6dd34819-c7e2-144e-b68c-8856ee32b294"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("6ed32a35-9080-f312-ade8-0e69bd7103b0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("6fda8dd2-0c61-5790-b407-7afdcacd8285"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("6ffe2a73-a5c4-8ac6-75ad-4f1caef90079"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("7051007b-907a-2932-86e1-c51029df6df8"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("72958664-8652-a076-774f-448a29ce3132"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("73e837a0-5fa2-0b32-34af-b35fc6965ce3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("7452f73a-e4ba-d894-b9d4-8662bebdff2c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("74ccb801-117c-54ac-69e9-8a85ef6c26bd"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("758f5f0e-4e31-3f07-a0e3-95efb66d4bce"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("76a58254-1768-5ebb-92fb-9158aa5b74f1"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("76a964de-f10c-d288-3c77-53a491fefbfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("78089eb5-2ba7-8728-d4a1-773e35d4bbd2"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("7837f34e-c923-a137-2d29-c3bcec1b633a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("783eb3b3-cfec-491c-1554-ddd6d4c913b3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("7a015dd6-122d-7189-9adf-bbaf3368ddcf"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("7ad9e9a7-de8a-a095-1eff-e23ed13ed6d7"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("7c31a078-ed4f-934c-4de3-ee871afb8a93"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("7cec2010-8e12-c7c2-c2b2-fc128289aa87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("7dfd413b-b85a-e5c3-5f43-c5d5034a325c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7e59287c-c2e8-c960-55d3-d79c7a4d5744"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("7eac574a-4169-9263-2370-294c82b9bdda"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("7f0635e0-2361-0a15-a3d9-1d2ab6966569"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("8050cab6-1a94-4b5a-e70f-472814d20b29"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("80512a06-6272-dca2-3240-4c0613c289b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("80783f7b-ad52-b148-b256-b3210e0cdce6"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("8078e022-f68b-0de9-0827-bb2c0e717988"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("816472b6-e790-ba36-9ce5-2b570bc74c71"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("834731e2-dc9b-4d19-3c60-f9a87a2277c9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("84788d82-b9a6-a84d-6607-a741309c0667"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("848a2cbb-8de6-9e6a-1ac2-17cfea210829"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("84f7ba2d-12b9-e26e-8114-f068e9228b85"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("85db0fab-805f-2b78-b892-1bcb767dc36b"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("8664414e-4844-5e77-4bab-51570ae83b8f"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("86bafef0-c08d-5821-7115-4e894d64898b"), false, true, true, true, true, true, true, false, true, false, false, true, true, true, true, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("887cf291-a252-5fca-089a-d530ae89931d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("88950e1f-8468-de1a-04d9-9132b4f50fec"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("89154351-336b-9edd-3afc-a59cda8ec176"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("8980ce4d-9776-2f90-aa8a-ccd9d5d8b3c4"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("89e94086-5957-5132-3379-39eb9cc0ce13"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("8b0726b3-eab8-a66f-8b89-49aabad070b9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("8b6ab424-c665-7869-a989-ef30c5e0da59"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("8caf812d-02db-7b2b-41d0-ce8732edbdaa"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("8cba2d0f-9d5a-4fde-40f8-bab9f24711ca"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("8ec664b5-5209-fe92-34b1-bf0807a69603"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("8f743dfe-db9b-cfa3-2aad-27ec4235b35d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("8fe72ca6-8048-a5ab-89fb-ccce984f22f4"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("905a2158-fcef-c876-e660-4bc3edcd70b0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("9116b47c-8dce-d6cf-e4b4-da5a96a357e2"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("9173e973-5956-7659-a264-980ad79264dd"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("91cee2f6-39f9-75e6-8955-27e8fe3399cd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("933f8f5a-9ea0-f90f-ca17-4ea9effe4ea3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("93533f88-5b3c-f8ba-a6dd-3beb60fa3339"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("93b17b74-0dc2-054f-cdb0-ec9469eaf98c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("9463a4c9-beaf-a4bd-4280-21d7f30a0411"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("95bb74fc-40e0-7f2d-e951-fb217f2a82ad"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("966573f4-19e6-4fee-aa26-ac9239cbe9ff"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("99031e5c-a87c-ebe0-0d04-46399309434c"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("99916786-6eca-5a2b-15f2-2d99abdc60db"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("9a4a2183-07c1-dc79-c13a-5e7a697c540e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("9ac4268c-afa2-1bbe-9e53-6b9da81b06b3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("9caedf98-3378-10a6-0485-8fd863db1f98"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("9df59a47-31be-11b2-62a0-31ef27c3dee8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("a134d8a3-0c2c-1c2a-81c0-87fb5871f301"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("a500f606-1326-47ca-dc72-1d576a511c24"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("a53e363e-3653-2268-f61f-7525e3efbb5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a6872f66-f497-a913-97a0-db9acaea6280"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("a6972069-e73c-ba6f-85ec-3e633fba2c3c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a6a71795-7c1c-f251-d780-881a223728c7"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("a6d2f300-6a1e-1b7d-75b1-14a45c421417"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a8586e94-7271-dc6c-3b5e-b9ca6fa73fda"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("a88e7db3-69d5-c5b9-11d4-bb8d5a64a242"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("a9ece6d5-4ec4-11c8-a213-9866de879500"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("aa301d6f-e785-0359-78ba-c5929c129bbc"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("aaabdea5-7257-0d14-d3bb-915b6f38e613"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("abb41a21-274b-cc54-b107-5ff6c3fed133"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("abe1f298-763d-4a72-0ef9-1d4768d0b868"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("aea3c8f9-28b6-c972-2018-ef06520902c9"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("af3953db-ab3e-0163-1923-86b3d5de15ce"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("af74dec7-de0a-5011-9457-ad44d4dbda2a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("afe97513-5c8d-7b5c-84ff-040359ef958e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b101b956-391d-3a36-a26f-deb25d940c27"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("b1d21a8c-1e82-9322-9373-ba5caef23929"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("b2d7a8e6-9f9e-f711-86bd-7568fc36b2e4"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("b3d794dc-643e-be3a-988a-860bc10d9876"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b3daa5c8-10a7-5ccb-5352-930c50bf4cfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b422385b-18f6-d391-c3de-19d2b46a0623"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b54c3509-d862-535d-0c12-3a2b414529f4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("b5b8ecad-3314-d7f6-3ae5-fd4b0fe8e835"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("b8dd4f0e-d18a-95a3-5a0f-4f4bcbc106fb"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("b98d0310-833d-757d-89d9-92393b4288e5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("b9bbc143-2b09-6d89-ddc1-2a04a32f7730"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("ba12232c-04c4-dd19-162f-5847aa40064e"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("bc40be65-ac6a-ecb8-d457-9c902e4a0eae"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("bc59d5c2-7918-f3dc-a7ba-fb1e3c7310db"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("bc9c23e8-3467-5c09-0b09-4fa378a603c8"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("bc9e6d05-dd39-a901-e0d2-de52da908a5d"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("bcbf203f-2abd-e2e3-548e-f31e72f266a6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("bd32d05f-02cc-fa5c-772c-b820a7e682ab"), false, true, true, false, true, true, true, true, true, true, true, true, true, true, true, true, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("bdaa538a-c42b-bef9-ca05-998f045ea6c0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("bf4661f3-be5f-1a48-63fb-f521d84e8473"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("c010c311-7647-0ec5-8561-5408df855e87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c0a10a61-9d82-1bb4-c3ce-ecb96d912bfb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c0f21c42-f15b-42e5-4f34-f63bd0a6f3d6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c1e852b9-b8bb-ca69-3cd0-f69dc625ab0b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("c211bad6-b7c8-1d79-1f8a-633a2eee8cce"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("c311d9e3-58a4-52a4-a274-57b54ad63183"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("c53896eb-717f-1c93-8a39-a9f9ed0d863e"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c74fb6b2-68b6-592c-055a-c124792cecea"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("c7927b56-2770-1f6a-2767-3657b60403bf"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("c8e913ee-f3ec-7aef-4b7b-2279b9d7a5e0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("cc858ba8-ae20-06d5-36b3-4f5f6ec53848"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("ccc0ebdb-4a57-d380-0da5-19c4a5c0fcc1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("cd24b498-bd2c-66dd-e337-d37871401b75"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("cd98145c-6609-7a61-33fd-dc963e6afc58"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("cfa23ee0-48b3-b14c-7faf-57f6b3ccc05a"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("d0955c56-00bb-33ef-426e-4ebc8a14b877"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("d0bdd816-1a8f-0ade-28b2-d4c90a283ad0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d1f0c525-8e61-dc6d-4913-693414b73a39"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d26c9577-f465-6471-2e17-ec70530f55c6"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("d34fc336-021c-6115-03f2-141c4250f45a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("d36413a5-4ea3-f92c-6801-59e9b7114af0"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("d4939435-a2bd-60a3-ea95-f7226b490dd8"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("d50095d1-ca12-9179-754d-8214572570cb"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("d6e57f01-4362-8b01-f906-949c7421b743"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("d77dcfb5-1484-279b-6b48-b377c46bf620"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("d839158c-cc1e-3121-ce72-eabcb8bea70a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("d8b21d1d-8917-a583-e510-4ac212a9b982"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000003"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("db8223ce-69ce-248f-33aa-ad143b52f80f"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("dd52d2aa-6437-9b50-397b-2951834c1096"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("dd5a0314-f5fc-dcbf-5b15-71ab8422fca2"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("ddb482ac-79dc-2b3e-15bb-ea3ca9464bb2"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("de26e77c-eb61-d353-a66e-4ce6bee14e87"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("de46e36f-893b-fee8-9f12-cfc4d4502a2e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000004"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("de59f25c-9cb4-9779-af25-037162187e40"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("de79f848-28ed-cead-1365-5243b3e4f6d8"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("ded69009-7eeb-3d2a-fe39-c79302dbf6f0"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e42e00a2-9b4d-461a-03d9-76410b89b78a"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("e4eefed5-8eb7-4a8c-13f1-0b458fee2f5b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("e6bd45b9-ee07-41cd-c471-38fabd17d936"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000014"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e884a85a-8f0d-cefb-be5b-5e1abfa6d613"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("e8af7e32-11ba-0e9c-b2d2-1a80aa526f8a"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("e960745e-ed0b-5320-67e3-7168d5a87bfa"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("e9658c44-a678-1843-a98f-8d83f14374c5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("e9be6bd5-e764-1b65-7835-f8bcae254ad3"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("ebc01bbf-4ea6-27ea-cdaa-30b0af73f042"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("ebde7074-a532-bee8-2f7d-e9fca2a6b8b1"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ebf3a1c7-3715-56c1-0137-8168db2caef4"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("ebf9c7e7-5ae8-37f8-971c-d62e9173effe"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000002"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("ed25bf2a-d39b-e5c5-a120-0fb61cea719b"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("ef20211e-7093-b3cf-e56b-7860cbcc3f71"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("f12cb910-2441-39d8-654b-4aa279923689"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("f1878558-833b-96ee-d597-77b29c8df47c"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000006"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f3468670-5e3d-d8ac-35a4-28507f06e96b"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("f3cb0d4c-4540-8941-69dd-eea73b8824e3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("f494380c-48f3-5f83-45cc-3a15c9cc28dd"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("f709009e-b968-82f2-a97f-bbde8548dc39"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("f7524e14-8509-4f20-57df-2de1e6f5b835"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("f782cee5-72e9-d8a2-0b6b-89b556c03f11"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000013"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("f86c37cf-123f-3c8e-10e4-b795ad8d23ce"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("f89a93ab-d59a-4a7a-2633-d405fbe6a350"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000008"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("fa2a4fd6-6b37-8577-a52d-bfbffc2d3998"), false, true, true, false, true, true, true, false, true, false, false, true, true, true, true, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000007"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("fb8f908c-0c79-8a2f-3941-b414ceff52c9"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000005"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("fbb625b2-1fc6-6b06-9197-0788c71746f1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("fbe1b1b4-ab46-124c-67e9-b8e699871fb1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000010"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("fccd7ce8-5d33-8d12-0808-c81350de2b93"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("fdca9135-c0c0-46b4-33e3-5d051f433ad1"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000015"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("003197d6-a07b-a658-1014-0d84c68d2355"), "accounts_assistant", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Accounts Assistant", null, null, 0L },
                    { new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"), "managing_director", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Managing Director", null, null, 0L },
                    { new Guid("07d53aa2-c266-4802-4786-9723d800e29d"), "technical_support_manager", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, true, "Technical Support Manager", null, null, 0L },
                    { new Guid("0a769058-1bab-5087-26b9-d33415b000e5"), "hr_executive", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "HR Executive", null, null, 0L },
                    { new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"), "electrical_engineer", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Electrical Engineer", null, null, 0L },
                    { new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"), "stores_assistant", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, false, "Stores Assistant", null, null, 0L },
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
                schema: "nexa",
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
                schema: "nexa",
                table: "employees",
                columns: new[] { "Id", "ApprovalStatus", "CreatedAt", "CreatedBy", "DateOfJoining", "DepartmentId", "DesignationId", "EmployeeCode", "EmployeeName", "EmployeeType", "Grade", "IsEmployeeCodeLocked", "LoginEnabled", "MobileNumber", "OfficialEmail", "OriginalImportedName", "Status", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("04a820d0-3213-a6c2-9ea1-9a5180efcf37"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("a2ed4710-4cec-d8dd-097e-e8c7353a66a6"), "SESS-009", "MANIKANDAN.S", "Permanent", "Executive", true, false, null, null, "MANIKANDAN.S", "Active", null, null, 0L },
                    { new Guid("131cf31d-0cc0-9b70-da2e-89463c49619e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-013", "LALU", "Permanent", "Executive", true, false, null, null, "LALU", "Active", null, null, 0L },
                    { new Guid("1577c211-a6ed-b6ee-d206-5461ad52c428"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-019", "RANJITH. R", "Permanent", "Executive", true, false, null, null, "RANJITH. R", "Active", null, null, 0L },
                    { new Guid("20f22ccf-a178-a29e-0a35-7671ff2a2bab"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-018", "A. VINAYA SAGAR ARKATI", "Permanent", "Executive", true, false, null, null, "A. VINAYA SAGAR ARKATI", "Active", null, null, 0L },
                    { new Guid("22a9f52a-db35-3ab5-0115-5e399bfbf4b2"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("90c527f8-3ea8-dc72-7283-c80e73a71f5d"), "SESS-008", "SURANTHER P", "Permanent", "Executive", true, false, null, null, "SURANTHER P", "Active", null, null, 0L },
                    { new Guid("26c37705-e799-8708-119b-1227908d5e0f"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-024", "PRAKASAM.B", "Permanent", "Executive", true, false, null, null, "PRAKASAM.B", "Active", null, null, 0L },
                    { new Guid("277fd621-865d-2823-1b5c-e13a9c36eb2a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b9de3ebc-27c8-8fd6-85b2-d8bc916e6dfa"), "SESS-038", "SYED IJAZUDDIN Z", "Permanent", "Executive", true, false, null, null, "SYED IJAZUDDIN Z", "Active", null, null, 0L },
                    { new Guid("294a2d76-6b76-66d0-76ce-e8d12c02f0c7"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-030", "MANIKANDAN SOKKALINGAM", "Permanent", "Executive", true, false, null, null, "MANIKANDAN SOKKALINGAM", "Active", null, null, 0L },
                    { new Guid("2cee437e-777d-514a-0fe0-4299dee7df7d"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("96908ceb-4e96-b670-db7e-59b2237f1dec"), "SESS-023", "SARATH BABU.K", "Permanent", "Executive", true, false, null, null, "SARATH BABU.K", "Active", null, null, 0L },
                    { new Guid("348345c2-1342-5b69-a85a-28d878cd75c6"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-028", "PRAVEEN KUMAR.M", "Permanent", "Executive", true, false, null, null, "PRAVEEN KUMAR.M", "Active", null, null, 0L },
                    { new Guid("3543a705-924a-6599-23be-fb9730a93f06"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("0057b580-1cb1-afa2-8328-5afb1162e77e"), new Guid("086ab1d4-3404-12b7-c35a-4b77737eb97b"), "SESS-001", "A. PARAMANANTHAM", "Permanent", "Executive", true, false, null, null, "A. PARAMANANTHAM", "Active", null, null, 0L },
                    { new Guid("3c926af7-c052-2a69-5cad-b961650d230b"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-035", "VINAYAGAM", "Permanent", "Executive", true, false, null, null, "VINAYAGAM", "Active", null, null, 0L },
                    { new Guid("3edaa7c0-f393-cb3e-fb1e-e2071cbf2178"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-016", "KALIDOSS", "Permanent", "Executive", true, false, null, null, "KALIDOSS", "Active", null, null, 0L },
                    { new Guid("41ff2ffb-081e-4600-7680-eef1ef81c01e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("075fb64f-355a-ee74-517b-6b9c6da0f8db"), "SESS-032", "PRASANNA.G", "Permanent", "Executive", true, false, null, null, "PRASANNA.G", "Active", null, null, 0L },
                    { new Guid("45f0c876-d996-210a-67b3-993b7502d3e5"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-010", "RAJESHKUMAR.V", "Permanent", "Executive", true, false, null, null, "RAJESHKUMAR.V", "Active", null, null, 0L },
                    { new Guid("48f8c731-7101-d7ff-6605-6b8f283718b1"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("2f148a82-10ab-5801-9ff1-9f510611e5fd"), "SESS-022", "KARTHICK.B", "Permanent", "Executive", true, false, null, null, "KARTHICK.B", "Active", null, null, 0L },
                    { new Guid("4f2518af-f9b1-98ce-fa4b-125f1034e56e"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-026", "SRINIVASAN.V", "Permanent", "Executive", true, false, null, null, "SRINIVASAN.V", "Active", null, null, 0L },
                    { new Guid("50a5b3a3-aa3a-8269-a283-149d2a69cf8a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-029", "SRINIVASAN.C", "Permanent", "Executive", true, false, null, null, "SRINIVASAN.C", "Active", null, null, 0L },
                    { new Guid("5fdedc5a-1740-164c-04e9-3c6f2db5417c"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("37ae1390-d60b-28aa-f5f8-43b5549936c8"), "SESS-007", "A. ALFATHIMA PARVEEN", "Permanent", "Executive", true, false, null, null, "A. ALFATHIMA PARVEEN", "Active", null, null, 0L },
                    { new Guid("64382325-5125-141e-057e-7ee3f30b2bd3"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("f38530d3-549c-8fe3-3f75-331795d92bd3"), "SESS-005", "WASEEM.S", "Permanent", "Executive", true, false, null, null, "WASEEM.S", "Active", null, null, 0L },
                    { new Guid("73a13e4d-73b6-b86f-738b-71261ad69e71"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("0057b580-1cb1-afa2-8328-5afb1162e77e"), new Guid("a653c7ab-0b15-c0fc-bdcb-8cb6c64bd830"), "SESS-002", "ALAGUEASWARI", "Permanent", "Executive", true, false, null, null, "ALAGUEASWARI", "Active", null, null, 0L },
                    { new Guid("85b9da5c-cf3b-6217-593f-4b8e206bfa7a"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-037", "DEVANAND B", "Permanent", "Executive", true, false, null, null, "DEVANAND B", "Active", null, null, 0L },
                    { new Guid("889f9bdc-f246-e914-410d-7102ad10e31d"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("6ea3e733-e5e0-9b55-e7de-db94afda2b09"), new Guid("c7775052-f0a9-27e3-f259-746120a113a6"), "SESS-004", "T. DINESH", "Permanent", "Executive", true, false, null, null, "T. DINESH", "Active", null, null, 0L },
                    { new Guid("93216afd-a239-3124-c23e-32d1ff8a8cee"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6"), "SESS-014", "KAMALI SRINIVASAN", "Permanent", "Executive", true, false, null, null, "KAMALI SRINIVASAN", "Active", null, null, 0L },
                    { new Guid("9cb99d4e-f1a7-7c9b-62e4-dd838db62c91"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("4c22a815-6a44-3d0b-9bd2-45743fc0a9aa"), "SESS-011", "YESWANTH KUMAR.N", "Permanent", "Executive", true, false, null, null, "YESWANTH KUMAR.N", "Active", null, null, 0L },
                    { new Guid("a8ffe255-91ff-3c05-8f9f-dfa21826f2d5"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("a2ed4710-4cec-d8dd-097e-e8c7353a66a6"), "SESS-033", "BLESSON PAUL", "Permanent", "Executive", true, false, null, null, "BLESSON PAUL", "Active", null, null, 0L },
                    { new Guid("b04acf39-5c81-d23c-89e6-9266d39b0be6"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-034", "MADHANKUMAR.J", "Permanent", "Executive", true, false, null, null, "MADHANKUMAR.J", "Active", null, null, 0L },
                    { new Guid("b1d0d0fb-27b8-e8db-1b03-023c32c74dc9"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-039", "THIRUNAVUKKARASU", "Permanent", "Executive", true, false, null, null, "THIRUNAVUKKARASU", "Active", null, null, 0L },
                    { new Guid("b42a0911-dc25-c491-e26f-b87a7512a0ed"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("35936fb3-4fc0-4757-268f-c467720e39fa"), "SESS-031", "VENKAT RAV.S", "Permanent", "Executive", true, false, null, null, "VENKAT RAV.S", "Active", null, null, 0L },
                    { new Guid("b6292258-84c4-225f-2571-dc1bc204edb7"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("51d81035-b83e-5452-c5c8-be69b5d3b1b3"), new Guid("e4bec48d-a248-c13d-a71a-00a2dd40e35e"), "SESS-025", "KARTHIKEYAN MK", "Permanent", "Executive", true, false, null, null, "KARTHIKEYAN MK", "Active", null, null, 0L },
                    { new Guid("bc8570aa-774c-9c38-9b42-ddf8599758f0"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-003", "M. SATHISHKUMAR", "Permanent", "Executive", true, false, null, null, "M. SATHISHKUMAR", "Active", null, null, 0L },
                    { new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("940ac030-8dcf-1575-6545-fea0f75f18f8"), "SESS-012", "PRIYA.E", "Permanent", "Executive", true, false, null, null, "PRIYA.E", "Active", null, null, 0L },
                    { new Guid("c175d954-417c-1d34-435c-8a5dce05ac78"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("8e377677-95bb-f0fe-4207-2efaf2b89208"), "SESS-021", "KRISHNAVENI", "Permanent", "Executive", true, false, null, null, "KRISHNAVENI", "Active", null, null, 0L },
                    { new Guid("e2815b6b-d417-6f86-177b-fb4fc46a6045"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("4c9baa15-c3d4-6b41-d040-f354c5cff307"), "SESS-015", "RANJITH.E", "Permanent", "Executive", true, false, null, null, "RANJITH.E", "Active", null, null, 0L },
                    { new Guid("e7bd4851-c9ba-68e9-a21f-e8583cb82642"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("35936fb3-4fc0-4757-268f-c467720e39fa"), "SESS-027", "SANJAY SARAVANAN", "Permanent", "Executive", true, false, null, null, "SANJAY SARAVANAN", "Active", null, null, 0L },
                    { new Guid("eca6a631-ef87-cb26-dbd3-5535a950d37f"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("ee1a1fa1-17d8-623b-d173-ce1efbb11cd4"), new Guid("b5b051ca-7d0d-c78a-0e14-9794651490db"), "SESS-036", "FRANCIS XAVIER", "Permanent", "Executive", true, false, null, null, "FRANCIS XAVIER", "Active", null, null, 0L },
                    { new Guid("f1dbc4aa-d567-616d-5e5c-63fd8f049e68"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("4c22a815-6a44-3d0b-9bd2-45743fc0a9aa"), "SESS-017", "MOHD ASHIQ", "Permanent", "Executive", true, false, null, null, "MOHD ASHIQ", "Active", null, null, 0L },
                    { new Guid("fa72ea80-86c0-5f25-f12c-721e76c1daac"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("bf9f94c3-17cf-7ef5-1dfc-0e1bfcca0f8e"), new Guid("39f842c4-5688-20a6-2a81-dc0fed68aa0f"), "SESS-006", "S. NANTHAKUMAR", "Permanent", "Executive", true, false, null, null, "S. NANTHAKUMAR", "Active", null, null, 0L },
                    { new Guid("ff338b63-0eab-59d7-56b1-525e1bedfffd"), "SeedApproved", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", null, new Guid("d30a9101-4e01-b19c-bc7c-926feb98e889"), new Guid("82783939-c768-2002-5b0e-17db5261eab9"), "SESS-020", "RANJEETH.B", "Permanent", "Executive", true, false, null, null, "RANJEETH.B", "Active", null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("004fb496-d229-d6cc-5c2e-d6ea2b193b4a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("0a848105-61e9-9489-6047-4c2bb6182dd7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("0ed63eb1-6b6e-2fbd-fa25-321db2a61672"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("14054c28-010a-9856-0bb2-7e22d562edff"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("1a407980-d77d-00b9-50f6-9ddad4e3e449"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("28bae41c-47a1-7bd1-12aa-aab213ad92cc"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("33273c8a-2387-bde1-dc0e-86cbb56f7369"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("36111c95-41df-e868-a40d-4ed262ab47d7"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("36494c2b-ae2e-8bbb-b6f3-f4decf561852"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("3be62adc-bee6-6ae9-55d1-ae4209ae72ee"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("469c689f-9d22-6a95-b27f-107487beccbf"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("487274b8-da1f-82e0-5fd6-c6dac5a61f57"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("4e0257d1-8365-9663-2bc8-106f80ac988d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("4fddc34f-732f-2e7a-34cd-52509bab4617"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("5018280b-0d63-2061-42af-459b8ab01588"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("50419c41-f6ec-5073-27a9-eaf624598b7b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("53229a36-fc1e-0ae2-cbb4-fc35ebcbb195"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("5473b883-7efa-504e-2b94-3046e3c3e53a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("553a4767-4479-5fd3-9b6a-8606fb8c12f3"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("5a712165-7d3b-799d-2f89-f59245caff4c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("5c6fbf58-cf3a-3b9e-fc41-5bf8ff8a25bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("61205d41-2d0f-7d63-9277-22f655f23023"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("61ec7fe6-0d34-a48b-5c41-787c818b387b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("71ba55c7-5376-b477-82b0-6738e974588d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000012"), null, null, 0L },
                    { new Guid("73b035d4-6148-2a05-72ce-a6a8d8e78238"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("750ac23a-5f8c-c25e-20d6-a6833c11feb1"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("79fd5580-dd3e-1acb-f533-46ce79e2b7e9"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("8c6fbd52-fd6c-4139-054a-d4849982957a"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000018"), null, null, 0L },
                    { new Guid("927a433e-d64c-2338-422a-caebdefa33dc"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("996887d3-395b-41d3-d284-faea17cc8617"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000009"), null, null, 0L },
                    { new Guid("9b4f26db-a193-5cb2-36fe-cc0398a1f7a5"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("9efb66f9-223c-3547-03f4-db2430ee631c"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("a03ff5b1-354f-99a3-03a8-e967477fbe4d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("a12caa38-5452-cebf-5393-a0c34815de08"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000005"), null, null, 0L },
                    { new Guid("a2f45cb4-93a1-b607-3085-0c8d6452e7b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("a585f941-c6a3-cc7c-0f6d-3700f585eb09"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("affa057c-1b27-237f-be02-ade42d92c483"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("b0a5b838-c938-5c82-0711-952129055538"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000011"), null, null, 0L },
                    { new Guid("b247f1ec-c003-863a-2cd2-ff7d8ad3b099"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000010"), null, null, 0L },
                    { new Guid("b4001b8f-7ec3-8fc5-771e-eabd8bf11f5d"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000004"), null, null, 0L },
                    { new Guid("b539c16a-c83e-75f7-9212-9d8b32bb287e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("b9ffb4de-d78e-866f-735b-2a41baf4ee15"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("ba48d59e-57f7-5826-37a1-fd1c57dc602e"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("c0bcba0f-580b-c780-a1f9-a6330bebaa80"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000007"), null, null, 0L },
                    { new Guid("c64305dc-2309-6840-d2ea-465fcf301537"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("c74d68b0-8370-c01f-696f-b39c622156b3"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000008"), null, null, 0L },
                    { new Guid("cda29931-d84e-aef3-6ec7-d5ee1bcae6de"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000006"), null, null, 0L },
                    { new Guid("d6501fe3-7d9e-95e3-c284-104c86cc5915"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000003"), null, null, 0L },
                    { new Guid("d9c2d3e1-b0f2-708e-2026-8c389dc7f737"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000013"), null, null, 0L },
                    { new Guid("e4ff94bd-1d8f-e2f0-8393-feeac2d7d415"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("e5876085-5ead-4b22-9d5e-3f400c8018bb"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("ed3d364e-a0c0-3d1b-2a06-2b3bc8fe244b"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L },
                    { new Guid("eefe49de-1012-2179-7c75-aba9078db5ca"), true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("f2fe61f8-7339-f3bf-e5c0-d14e1f24ab55"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000015"), null, null, 0L },
                    { new Guid("f7d23fd6-2d22-262a-7d9e-d9247a8021f5"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000014"), null, null, 0L },
                    { new Guid("f85c5b04-4506-9156-b01b-badc19a6ed6e"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000017"), null, null, 0L },
                    { new Guid("fa0e5df4-74cf-d3f5-3f58-43bb465f3a11"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("fb1c6e1a-c6ad-bff4-b8df-e19b219d5a92"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000016"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L },
                    { new Guid("fbe3075e-ebb0-37eb-69d3-d7ab1dcdf0b6"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000017"), new Guid("10000000-0000-0000-0000-000000000020"), null, null, 0L },
                    { new Guid("fd354f3f-68ac-66d8-0639-1c32f09fa0d0"), false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", false, new Guid("20000000-0000-0000-0000-000000000018"), new Guid("10000000-0000-0000-0000-000000000016"), null, null, 0L }
                });

            migrationBuilder.InsertData(
                schema: "nexa",
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
                schema: "nexa",
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
                schema: "nexa",
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

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                schema: "nexa",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_designations_Code",
                schema: "nexa",
                table: "designations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_approval_history_EmployeeId_CreatedAt",
                schema: "nexa",
                table: "employee_approval_history",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_import_history_EmployeeId",
                schema: "nexa",
                table: "employee_import_history",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_import_history_ImportBatch_SourceEmployeeCode",
                schema: "nexa",
                table: "employee_import_history",
                columns: new[] { "ImportBatch", "SourceEmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_EmployeeId_RoleId_EffectiveFrom",
                schema: "nexa",
                table: "employee_role_assignments",
                columns: new[] { "EmployeeId", "RoleId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_RoleId",
                schema: "nexa",
                table: "employee_role_assignments",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_EmployeeId_SkillId",
                schema: "nexa",
                table: "employee_skills",
                columns: new[] { "EmployeeId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_SkillId",
                schema: "nexa",
                table: "employee_skills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_status_history_EmployeeId_CreatedAt",
                schema: "nexa",
                table: "employee_status_history",
                columns: new[] { "EmployeeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_DepartmentId",
                schema: "nexa",
                table: "employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_DesignationId",
                schema: "nexa",
                table: "employees",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeCode",
                schema: "nexa",
                table: "employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_DepartmentHeadEmployeeId",
                schema: "nexa",
                table: "reporting_relationships",
                column: "DepartmentHeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_EmployeeId_EffectiveFrom",
                schema: "nexa",
                table: "reporting_relationships",
                columns: new[] { "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reporting_relationships_ReportingManagerEmployeeId",
                schema: "nexa",
                table: "reporting_relationships",
                column: "ReportingManagerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_skills_Code",
                schema: "nexa",
                table: "skills",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_approval_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employee_import_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employee_role_assignments",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employee_skills",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employee_status_history",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "reporting_relationships",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "nexa");

            migrationBuilder.DropTable(
                name: "designations",
                schema: "nexa");

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("004fb496-d229-d6cc-5c2e-d6ea2b193b4a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00cbfa57-17fb-9bc9-ebc2-d82593db20c0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01a8ed83-0e17-63e1-ff4c-cdc4dadcd776"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01ba57da-bf38-37ff-1b4d-7a89bba40f68"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("03043ba5-389c-3233-01eb-fc5a0b52e88f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("032618b3-6ddd-dbb6-c6a7-9fa81b357f37"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("036447eb-18a7-241a-c0e7-6c84b3fd572a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("03c74b25-7022-9594-cca0-2ded65991f10"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("03ebd08f-a093-d3cf-8f87-46300c8d1dba"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("03f83275-7beb-0b99-204e-b232181c659f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("04d5edd1-bd0c-fa30-5694-836b6f46cc46"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("07e0a0a9-0c56-ff51-7a64-df05cb4d8641"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("08286f2c-f6f7-fac1-de8c-a4736570cc51"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("08307be7-8234-e259-ae74-f9392ed2a1fb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0967d616-a202-f778-22f4-5c0c5606efd3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0a848105-61e9-9489-6047-4c2bb6182dd7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0b3c3f4a-2d9a-ac8f-d9ae-9ff61418f67b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0b6178f3-935b-5f40-be62-1209ebaee582"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0b7594fe-4132-dd48-944a-6107faae95f2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0b97b7a0-d2ac-a4da-0930-5296011b4496"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0c3e02e8-2bcd-4d05-a3c8-312d7d66ba22"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0df3abd0-c90c-3d36-91bb-68b49e0f2605"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0ed63eb1-6b6e-2fbd-fa25-321db2a61672"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0ef21be8-e408-8a8e-e2c6-3e789e64302b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0f4187ee-9df8-cf46-4ace-4f8349bdbf37"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0fcdc8eb-2a3f-7ea7-b022-c3396d868d56"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0fdc8bf6-3644-6d7c-913e-c5d93ecebda4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1067e842-f711-c5f8-c54f-605d218e3e9b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("10a312ff-3606-ee0c-b384-03bf891c5d8f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("11b71479-d821-6aa9-75d9-307f56d90621"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1216c9a3-31eb-e2bb-3238-9c3b6dde5daf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("12694a69-1d2c-4e6b-a81f-65ce1582f29f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("13ca2fe4-4115-8977-0feb-782fe436d5eb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("13e130d5-1294-8526-2109-72829c861c16"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("14054c28-010a-9856-0bb2-7e22d562edff"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("14ffdc95-b241-a3c5-9968-ef467797859b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("16bcfa5d-19d9-48d6-8c65-8fe9b00ad2f2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1737277b-1cfa-ad32-67a2-49fd84c7b8dc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("173f2300-ec29-19df-e9e3-1370ab9c8ad9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("175418b4-d466-1033-67bf-185f2dda3fe1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("175aef9a-6f31-588c-9e0c-cbf21dfef7ac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("17f30c40-8f89-f202-f1da-648eb7c00612"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18f0cf47-9b69-ae81-4ca6-c669be41d7d0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("18fec771-1b4b-ebbf-bf98-f4747886f977"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("19cd5147-1ca2-70e2-8ef8-33ceb788c475"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1a407980-d77d-00b9-50f6-9ddad4e3e449"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1d7d16c0-8e5c-3264-41fe-eedd38702c06"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1e76f7c6-0594-5c28-7832-f4bc37ca9daa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1e7ae789-44e0-cb9f-be17-5eaad290a8d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1eaeb950-7c69-f801-20ce-03703c14aaed"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("1ed09b6b-9e2f-5689-c92d-37fa81cd429a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("20362439-ea6e-017b-307e-766fb7088540"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("20cd5bd9-af1d-f904-28a8-13249e3ca0b9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("21a710c7-4273-ee01-12c4-61303f20ea47"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2217a7b5-f36e-855b-5b50-4f98715465b5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2274b082-d44b-fb16-5a0b-9e7729e9c9d9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2315442d-921d-6442-1e1b-143e5c4acfb1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("233d6b2d-7eb7-e571-78fb-ff25933a5e48"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("246095fe-bc58-8e7d-d062-fb7f4f5c1a34"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("26dbfb50-8443-c634-2459-3ba1e8429e33"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("27a09995-798f-9a09-ec9e-51bcadea8a79"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2839ba98-7359-453d-7968-5e5a22aa489d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("28b621bd-b402-9bec-6717-9a957209f5b4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("28bae41c-47a1-7bd1-12aa-aab213ad92cc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("29257d90-af79-fe7d-82d6-160a25556b29"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("293b30ad-309e-8464-d5c3-837ed16b4c41"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2a6db5b8-5435-d8c0-22a6-88f577cec4b2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2ab9cd5a-1606-e54f-ec1c-6dbc407d1bb2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2afb089d-1f11-e510-46b9-564fbda0ee6d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2bd2493e-80ef-7ef3-f048-4f5826939ba3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2cbfda68-1e6a-929b-a6e4-795789e53e71"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2d9bb0d0-0f85-e269-8cb9-cea7687c742f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2e76c3fd-70d4-ab42-f62f-bda8a16c88d2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2efcabb0-eabb-859a-508d-ad96495f9d36"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("30ac549b-cf0e-c6ca-9838-b92ac677daee"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("310dc945-7894-7776-9ab9-9071254b5c9c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("31454649-cc19-b624-8661-3c4e342209d1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3151527f-891a-560e-508c-26fca6b35bb4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("31948518-8d84-4d18-de4c-7d303b6dd21c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("320c9c09-5bfe-dcb2-dc7a-50f74bf98804"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("32fbab8f-8022-26f7-af56-98b45eb2cf25"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("33273c8a-2387-bde1-dc0e-86cbb56f7369"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("33482c08-bf8b-3427-9733-e3f85def2a8f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("335bcbec-6b9b-2035-e881-ddb219d6a889"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("343bb085-d954-5380-263b-d1d74a9d9ae6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("344fb35c-86d4-a015-745f-98dddc95a13f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("34545002-3f06-d2bb-8275-f3fbb141a710"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("35201627-24fe-6abb-0dc9-9eeecc5e415b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("35ef158d-6125-9adc-e7ff-74704aab6f44"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("36111c95-41df-e868-a40d-4ed262ab47d7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("36494c2b-ae2e-8bbb-b6f3-f4decf561852"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("366d6ddb-79e6-6d7e-2948-ed012149ee4a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("37079265-cf1b-157b-0c44-8fd278dc6664"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("37ab5b42-3b7b-e4b5-34d5-83b4d3894073"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("390c816f-29ff-c417-ba72-e1ad9b249a3f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("39e6443c-1cab-ba55-d870-4b7e9c6cb059"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a074d30-3401-99af-48bc-f3553ae95899"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3be62adc-bee6-6ae9-55d1-ae4209ae72ee"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c1b91c1-9093-2729-ce6f-de1903123924"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3d97d051-a73c-2db6-64b9-7a5ed1c267a6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3db66f31-7496-a50d-fd09-189c6d86a635"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3dc74640-7587-f7cb-87bf-847feeb760a2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f47dc2b-9f9a-68e1-3bcf-fb4fc442f638"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("406e39ee-9bf0-72c3-d671-75a37a6c6816"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4091008e-5890-810d-f307-9b419f743026"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("41496500-2f79-9184-b5d3-18f7246eed85"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("415daeac-f621-1206-ee3c-b9b43aff6984"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("42243842-8c8c-7642-37ca-9ed5ee13225e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4446230b-493d-9f12-ce7c-71b2add3e74e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4642b120-04d3-2ddf-51ff-bc6bcf260f07"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("466753a8-43f4-6c6c-3f8f-ab11d750a794"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("467972a2-cd85-1f32-6e68-f41409e32d91"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("469c689f-9d22-6a95-b27f-107487beccbf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("479c596f-8198-41ca-34f4-e066cc121cd2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("487274b8-da1f-82e0-5fd6-c6dac5a61f57"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("48cac800-a59d-0e09-6041-87174034c019"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("49ebb447-9fa7-38eb-aa2e-b97617549c12"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4a73d4d8-7ab1-2945-568f-9cb8aeeaed82"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4d2ada08-b246-dda1-ff81-dbaac36cb406"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4debe1f5-0d5e-90c7-935e-684a0484d7ed"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4e0257d1-8365-9663-2bc8-106f80ac988d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4e77844a-0f09-6009-86cf-eec6c8bbcc42"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4fddc34f-732f-2e7a-34cd-52509bab4617"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5018280b-0d63-2061-42af-459b8ab01588"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("50419c41-f6ec-5073-27a9-eaf624598b7b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("50f67d75-9555-756d-5fc5-fc92a88da34c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5157b67b-7a2f-4887-4093-de4bd6cc8e2d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5163ce51-bb0d-d8a2-6ca3-b515a19e8df2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("51d1c1f7-16ab-d29d-dd50-2f6f33aa3073"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("51dab92d-f7d5-d870-a13c-ca7c37f498e6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("51f714f5-c507-34a7-bf0c-3380d85db6a0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("53229a36-fc1e-0ae2-cbb4-fc35ebcbb195"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("535fb558-59b4-c3a8-01bb-503c161a7505"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("53e325a6-d795-1a83-eeb8-34c6bcec636f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("543cae26-dbd2-59d8-b366-3a8f1aeddc20"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5473b883-7efa-504e-2b94-3046e3c3e53a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("553a4767-4479-5fd3-9b6a-8606fb8c12f3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("56c49867-b079-a83b-38c8-b170f4ee32cf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("57064a0d-0927-ee99-2332-c8fd07790e73"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("580a98f6-3f04-60c8-b75b-78f7fa7f6cd1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5a712165-7d3b-799d-2f89-f59245caff4c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5c5deb8d-b053-8d30-6f27-307f31576ea0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5c6fbf58-cf3a-3b9e-fc41-5bf8ff8a25bb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5d5ca2ed-f113-4ce6-c77a-8955d3db135c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("5df893f5-2499-20e9-1666-29f0a9f88b96"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("61205d41-2d0f-7d63-9277-22f655f23023"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6164a7ec-aadb-0213-7bae-5a1d8178422b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("61ec7fe6-0d34-a48b-5c41-787c818b387b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("646ad30b-1811-3855-e608-cafca7c51a07"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("653654f5-c2ae-45b3-ecb4-507add141ea8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("679be5fe-4b9a-cca7-b8bf-59917f04c9e0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("688fee8c-dde0-dbe9-b0d8-7750152d37b9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("68e9ad66-3620-3330-e529-e8d686874e1a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6987c49f-5d17-db47-280b-8298904ad323"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a9dda69-8fb3-850f-53e1-3e7b8855e0ff"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6cf58043-56d1-1766-ebb3-ce7a8dc63e06"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6dd34819-c7e2-144e-b68c-8856ee32b294"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ed32a35-9080-f312-ade8-0e69bd7103b0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6fda8dd2-0c61-5790-b407-7afdcacd8285"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ffe2a73-a5c4-8ac6-75ad-4f1caef90079"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7051007b-907a-2932-86e1-c51029df6df8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("71ba55c7-5376-b477-82b0-6738e974588d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("72958664-8652-a076-774f-448a29ce3132"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("73b035d4-6148-2a05-72ce-a6a8d8e78238"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("73e837a0-5fa2-0b32-34af-b35fc6965ce3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7452f73a-e4ba-d894-b9d4-8662bebdff2c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("74ccb801-117c-54ac-69e9-8a85ef6c26bd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("750ac23a-5f8c-c25e-20d6-a6833c11feb1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("758f5f0e-4e31-3f07-a0e3-95efb66d4bce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("76a58254-1768-5ebb-92fb-9158aa5b74f1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("76a964de-f10c-d288-3c77-53a491fefbfa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("78089eb5-2ba7-8728-d4a1-773e35d4bbd2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7837f34e-c923-a137-2d29-c3bcec1b633a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("783eb3b3-cfec-491c-1554-ddd6d4c913b3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("79fd5580-dd3e-1acb-f533-46ce79e2b7e9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7a015dd6-122d-7189-9adf-bbaf3368ddcf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7ad9e9a7-de8a-a095-1eff-e23ed13ed6d7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7c31a078-ed4f-934c-4de3-ee871afb8a93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7cec2010-8e12-c7c2-c2b2-fc128289aa87"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7dfd413b-b85a-e5c3-5f43-c5d5034a325c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7e59287c-c2e8-c960-55d3-d79c7a4d5744"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7eac574a-4169-9263-2370-294c82b9bdda"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7f0635e0-2361-0a15-a3d9-1d2ab6966569"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8050cab6-1a94-4b5a-e70f-472814d20b29"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("80512a06-6272-dca2-3240-4c0613c289b9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("80783f7b-ad52-b148-b256-b3210e0cdce6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8078e022-f68b-0de9-0827-bb2c0e717988"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("816472b6-e790-ba36-9ce5-2b570bc74c71"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("834731e2-dc9b-4d19-3c60-f9a87a2277c9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84788d82-b9a6-a84d-6607-a741309c0667"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("848a2cbb-8de6-9e6a-1ac2-17cfea210829"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84f7ba2d-12b9-e26e-8114-f068e9228b85"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("85db0fab-805f-2b78-b892-1bcb767dc36b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8664414e-4844-5e77-4bab-51570ae83b8f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("86bafef0-c08d-5821-7115-4e894d64898b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("887cf291-a252-5fca-089a-d530ae89931d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("88950e1f-8468-de1a-04d9-9132b4f50fec"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("89154351-336b-9edd-3afc-a59cda8ec176"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8980ce4d-9776-2f90-aa8a-ccd9d5d8b3c4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("89e94086-5957-5132-3379-39eb9cc0ce13"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b0726b3-eab8-a66f-8b89-49aabad070b9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b6ab424-c665-7869-a989-ef30c5e0da59"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c6fbd52-fd6c-4139-054a-d4849982957a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8caf812d-02db-7b2b-41d0-ce8732edbdaa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8cba2d0f-9d5a-4fde-40f8-bab9f24711ca"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8ec664b5-5209-fe92-34b1-bf0807a69603"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8f743dfe-db9b-cfa3-2aad-27ec4235b35d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8fe72ca6-8048-a5ab-89fb-ccce984f22f4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("905a2158-fcef-c876-e660-4bc3edcd70b0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9116b47c-8dce-d6cf-e4b4-da5a96a357e2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9173e973-5956-7659-a264-980ad79264dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("91cee2f6-39f9-75e6-8955-27e8fe3399cd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("927a433e-d64c-2338-422a-caebdefa33dc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("933f8f5a-9ea0-f90f-ca17-4ea9effe4ea3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("93533f88-5b3c-f8ba-a6dd-3beb60fa3339"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("93b17b74-0dc2-054f-cdb0-ec9469eaf98c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9463a4c9-beaf-a4bd-4280-21d7f30a0411"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("95bb74fc-40e0-7f2d-e951-fb217f2a82ad"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("966573f4-19e6-4fee-aa26-ac9239cbe9ff"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("99031e5c-a87c-ebe0-0d04-46399309434c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("996887d3-395b-41d3-d284-faea17cc8617"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("99916786-6eca-5a2b-15f2-2d99abdc60db"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9a4a2183-07c1-dc79-c13a-5e7a697c540e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9ac4268c-afa2-1bbe-9e53-6b9da81b06b3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9b4f26db-a193-5cb2-36fe-cc0398a1f7a5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9caedf98-3378-10a6-0485-8fd863db1f98"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9df59a47-31be-11b2-62a0-31ef27c3dee8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("9efb66f9-223c-3547-03f4-db2430ee631c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a03ff5b1-354f-99a3-03a8-e967477fbe4d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a12caa38-5452-cebf-5393-a0c34815de08"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a134d8a3-0c2c-1c2a-81c0-87fb5871f301"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a2f45cb4-93a1-b607-3085-0c8d6452e7b6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a500f606-1326-47ca-dc72-1d576a511c24"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a53e363e-3653-2268-f61f-7525e3efbb5d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a585f941-c6a3-cc7c-0f6d-3700f585eb09"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6872f66-f497-a913-97a0-db9acaea6280"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6972069-e73c-ba6f-85ec-3e633fba2c3c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6a71795-7c1c-f251-d780-881a223728c7"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6d2f300-6a1e-1b7d-75b1-14a45c421417"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a8586e94-7271-dc6c-3b5e-b9ca6fa73fda"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a88e7db3-69d5-c5b9-11d4-bb8d5a64a242"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a9ece6d5-4ec4-11c8-a213-9866de879500"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aa301d6f-e785-0359-78ba-c5929c129bbc"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aaabdea5-7257-0d14-d3bb-915b6f38e613"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("abb41a21-274b-cc54-b107-5ff6c3fed133"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("abe1f298-763d-4a72-0ef9-1d4768d0b868"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("aea3c8f9-28b6-c972-2018-ef06520902c9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af3953db-ab3e-0163-1923-86b3d5de15ce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("af74dec7-de0a-5011-9457-ad44d4dbda2a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("afe97513-5c8d-7b5c-84ff-040359ef958e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("affa057c-1b27-237f-be02-ade42d92c483"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b0a5b838-c938-5c82-0711-952129055538"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b101b956-391d-3a36-a26f-deb25d940c27"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b1d21a8c-1e82-9322-9373-ba5caef23929"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b247f1ec-c003-863a-2cd2-ff7d8ad3b099"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b2d7a8e6-9f9e-f711-86bd-7568fc36b2e4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b3d794dc-643e-be3a-988a-860bc10d9876"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b3daa5c8-10a7-5ccb-5352-930c50bf4cfa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b4001b8f-7ec3-8fc5-771e-eabd8bf11f5d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b422385b-18f6-d391-c3de-19d2b46a0623"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b539c16a-c83e-75f7-9212-9d8b32bb287e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b54c3509-d862-535d-0c12-3a2b414529f4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b5b8ecad-3314-d7f6-3ae5-fd4b0fe8e835"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b8dd4f0e-d18a-95a3-5a0f-4f4bcbc106fb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b98d0310-833d-757d-89d9-92393b4288e5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b9bbc143-2b09-6d89-ddc1-2a04a32f7730"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b9ffb4de-d78e-866f-735b-2a41baf4ee15"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ba12232c-04c4-dd19-162f-5847aa40064e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ba48d59e-57f7-5826-37a1-fd1c57dc602e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc40be65-ac6a-ecb8-d457-9c902e4a0eae"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc59d5c2-7918-f3dc-a7ba-fb1e3c7310db"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc9c23e8-3467-5c09-0b09-4fa378a603c8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bc9e6d05-dd39-a901-e0d2-de52da908a5d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bcbf203f-2abd-e2e3-548e-f31e72f266a6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bd32d05f-02cc-fa5c-772c-b820a7e682ab"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bdaa538a-c42b-bef9-ca05-998f045ea6c0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("bf4661f3-be5f-1a48-63fb-f521d84e8473"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c010c311-7647-0ec5-8561-5408df855e87"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0a10a61-9d82-1bb4-c3ce-ecb96d912bfb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0bcba0f-580b-c780-a1f9-a6330bebaa80"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c0f21c42-f15b-42e5-4f34-f63bd0a6f3d6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1e852b9-b8bb-ca69-3cd0-f69dc625ab0b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c211bad6-b7c8-1d79-1f8a-633a2eee8cce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c311d9e3-58a4-52a4-a274-57b54ad63183"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c53896eb-717f-1c93-8a39-a9f9ed0d863e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c64305dc-2309-6840-d2ea-465fcf301537"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c74d68b0-8370-c01f-696f-b39c622156b3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c74fb6b2-68b6-592c-055a-c124792cecea"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c7927b56-2770-1f6a-2767-3657b60403bf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c8e913ee-f3ec-7aef-4b7b-2279b9d7a5e0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cc858ba8-ae20-06d5-36b3-4f5f6ec53848"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ccc0ebdb-4a57-d380-0da5-19c4a5c0fcc1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cd24b498-bd2c-66dd-e337-d37871401b75"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cd98145c-6609-7a61-33fd-dc963e6afc58"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cda29931-d84e-aef3-6ec7-d5ee1bcae6de"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("cfa23ee0-48b3-b14c-7faf-57f6b3ccc05a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d0955c56-00bb-33ef-426e-4ebc8a14b877"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d0bdd816-1a8f-0ade-28b2-d4c90a283ad0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1f0c525-8e61-dc6d-4913-693414b73a39"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d26c9577-f465-6471-2e17-ec70530f55c6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d34fc336-021c-6115-03f2-141c4250f45a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d36413a5-4ea3-f92c-6801-59e9b7114af0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d4939435-a2bd-60a3-ea95-f7226b490dd8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d50095d1-ca12-9179-754d-8214572570cb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d6501fe3-7d9e-95e3-c284-104c86cc5915"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d6e57f01-4362-8b01-f906-949c7421b743"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d77dcfb5-1484-279b-6b48-b377c46bf620"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d839158c-cc1e-3121-ce72-eabcb8bea70a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8b21d1d-8917-a583-e510-4ac212a9b982"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d9c2d3e1-b0f2-708e-2026-8c389dc7f737"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("db8223ce-69ce-248f-33aa-ad143b52f80f"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dd52d2aa-6437-9b50-397b-2951834c1096"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("dd5a0314-f5fc-dcbf-5b15-71ab8422fca2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ddb482ac-79dc-2b3e-15bb-ea3ca9464bb2"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("de26e77c-eb61-d353-a66e-4ce6bee14e87"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("de46e36f-893b-fee8-9f12-cfc4d4502a2e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("de59f25c-9cb4-9779-af25-037162187e40"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("de79f848-28ed-cead-1365-5243b3e4f6d8"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ded69009-7eeb-3d2a-fe39-c79302dbf6f0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e42e00a2-9b4d-461a-03d9-76410b89b78a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4eefed5-8eb7-4a8c-13f1-0b458fee2f5b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e4ff94bd-1d8f-e2f0-8393-feeac2d7d415"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e5876085-5ead-4b22-9d5e-3f400c8018bb"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e6bd45b9-ee07-41cd-c471-38fabd17d936"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e884a85a-8f0d-cefb-be5b-5e1abfa6d613"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e8af7e32-11ba-0e9c-b2d2-1a80aa526f8a"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e960745e-ed0b-5320-67e3-7168d5a87bfa"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e9658c44-a678-1843-a98f-8d83f14374c5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e9be6bd5-e764-1b65-7835-f8bcae254ad3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebc01bbf-4ea6-27ea-cdaa-30b0af73f042"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebde7074-a532-bee8-2f7d-e9fca2a6b8b1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebf3a1c7-3715-56c1-0137-8168db2caef4"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ebf9c7e7-5ae8-37f8-971c-d62e9173effe"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ed25bf2a-d39b-e5c5-a120-0fb61cea719b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ed3d364e-a0c0-3d1b-2a06-2b3bc8fe244b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("eefe49de-1012-2179-7c75-aba9078db5ca"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("ef20211e-7093-b3cf-e56b-7860cbcc3f71"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f12cb910-2441-39d8-654b-4aa279923689"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f1878558-833b-96ee-d597-77b29c8df47c"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f2fe61f8-7339-f3bf-e5c0-d14e1f24ab55"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3468670-5e3d-d8ac-35a4-28507f06e96b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f3cb0d4c-4540-8941-69dd-eea73b8824e3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f494380c-48f3-5f83-45cc-3a15c9cc28dd"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f709009e-b968-82f2-a97f-bbde8548dc39"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f7524e14-8509-4f20-57df-2de1e6f5b835"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f782cee5-72e9-d8a2-0b6b-89b556c03f11"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f7d23fd6-2d22-262a-7d9e-d9247a8021f5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f85c5b04-4506-9156-b01b-badc19a6ed6e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f86c37cf-123f-3c8e-10e4-b795ad8d23ce"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f89a93ab-d59a-4a7a-2633-d405fbe6a350"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa0e5df4-74cf-d3f5-3f58-43bb465f3a11"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa2a4fd6-6b37-8577-a52d-bfbffc2d3998"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb1c6e1a-c6ad-bff4-b8df-e19b219d5a92"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fb8f908c-0c79-8a2f-3941-b414ceff52c9"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fbb625b2-1fc6-6b06-9197-0788c71746f1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fbe1b1b4-ab46-124c-67e9-b8e699871fb1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fbe3075e-ebb0-37eb-69d3-d7ab1dcdf0b6"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fccd7ce8-5d33-8d12-0808-c81350de2b93"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd354f3f-68ac-66d8-0639-1c32f09fa0d0"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fdca9135-c0c0-46b4-33e3-5d051f433ad1"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("003197d6-a07b-a658-1014-0d84c68d2355"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("03325f4f-c6d4-b3f3-f4b3-11b728c275da"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("07d53aa2-c266-4802-4786-9723d800e29d"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("0a769058-1bab-5087-26b9-d33415b000e5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("23e39915-e02a-82aa-18f9-10ea329fad00"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("327c54ec-84f0-0eca-2123-cb9068b2c13b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("45eb9032-3689-8526-caee-41db0e7e2644"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("46899b83-f5d7-793d-f008-5b15bcf06b17"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("4dd5b229-c6a0-e45e-dd6c-ef6529087d05"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("5108e629-77d1-c7f2-90ee-cca43777210e"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("80c408fe-3f95-ba8a-54b2-d0eee2374adf"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("81701251-a033-5850-5bb4-f4bf1b16920b"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("8481d263-cb63-6bc1-76ac-b4c2a56fc1c5"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("97cf9b49-ae40-a8a5-e20b-acc199601716"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("c4133420-c386-9452-93a7-484e18105372"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("d52152b0-05b4-18f9-4201-1f7066af4c76"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("e177df2e-c5f3-adb4-fbc9-11973c0d68ac"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "nexa",
                table: "page_definitions",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000018"));

            migrationBuilder.DropColumn(
                name: "CanCancel",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanDeactivate",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanDownload",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanPrint",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanReject",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanReplaceAttachment",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanRequestClarification",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanRequestRevision",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanResubmit",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanSubmit",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanUploadAttachment",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanVerify",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanViewAuditHistory",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "CanViewCommercialValues",
                schema: "nexa",
                table: "role_page_permissions");

            migrationBuilder.DropColumn(
                name: "HasFullControl",
                schema: "nexa",
                table: "role_page_permissions");
        }
    }
}
