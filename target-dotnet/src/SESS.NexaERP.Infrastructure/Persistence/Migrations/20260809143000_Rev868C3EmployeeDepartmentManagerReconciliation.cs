using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

public partial class Rev868C3EmployeeDepartmentManagerReconciliation : Migration
{
    private static readonly DateTimeOffset Stamp = new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
    private const string Actor = "REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            create table if not exists nexa.rev868c3_employee_backup
            (
                "EmployeeId" uuid primary key,
                "EmployeeCode" character varying(40) not null,
                "EmployeeName" character varying(200) not null,
                "OriginalImportedName" character varying(200) not null,
                "EmployeeType" character varying(60) not null,
                "Grade" character varying(60) not null,
                "DepartmentId" uuid not null,
                "DesignationId" uuid not null,
                "Status" character varying(40) not null,
                "DateOfJoining" date null,
                "OfficialEmail" character varying(254) null,
                "MobileNumber" character varying(40) null,
                "LoginEnabled" boolean not null,
                "ApprovalStatus" character varying(60) not null,
                "IsEmployeeCodeLocked" boolean not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );

            create table if not exists nexa.rev868c3_department_backup
            (
                "DepartmentId" uuid primary key,
                "Code" character varying(80) not null,
                "Name" character varying(160) not null,
                "IsActive" boolean not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );

            create table if not exists nexa.rev868c3_department_mapping_backup
            (
                "MappingId" uuid primary key,
                "DepartmentId" uuid not null,
                "ApprovalRouteCode" character varying(40) not null,
                "PrimaryApproverEmployeeId" uuid not null,
                "AlternateApproverEmployeeId" uuid null,
                "EffectiveFrom" date not null,
                "EffectiveTo" date null,
                "IsActive" boolean not null,
                "Remarks" character varying(500) not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );


            create table if not exists nexa.rev868c3_role_backup
            (
                "RoleId" uuid primary key,
                "Code" character varying(64) not null,
                "Name" character varying(160) not null,
                "IsPrivileged" boolean not null,
                "IsActive" boolean not null,
                "CreatedAt" timestamp with time zone not null,
                "CreatedBy" text not null,
                "UpdatedAt" timestamp with time zone null,
                "UpdatedBy" text null,
                "Version" bigint not null,
                "CapturedAt" timestamp with time zone not null,
                "CapturedBy" text not null
            );

            insert into nexa.rev868c3_employee_backup
                ("EmployeeId", "EmployeeCode", "EmployeeName", "OriginalImportedName", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "EmployeeCode", "EmployeeName", "OriginalImportedName", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_EMPLOYEE_BACKUP'
            from nexa.employees
            where "EmployeeCode" like 'SESS-%'
            on conflict ("EmployeeId") do nothing;

            insert into nexa.rev868c3_department_backup
                ("DepartmentId", "Code", "Name", "IsActive", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "Code", "Name", "IsActive", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_DEPARTMENT_BACKUP'
            from nexa.departments
            where "Code" in ('MANAGEMENT','PURCHASE','STORES','ACCOUNTS_FINANCE','HR_ADMIN','PRODUCTION_FABRICATION','DESIGN','ELECTRICAL_PLC_INSTRUMENTATION','REFRIGERATION_MECHANICAL','SERVICE_TECHNICAL_SUPPORT','SOFTWARE_IT','QUALITY_QC','ENGINEER_TECHNICAL','MANAGER','JUNIOR_ASSISTANT','ADMIN_ACCOUNTS_STORES')
            on conflict ("DepartmentId") do nothing;

            insert into nexa.rev868c3_department_mapping_backup
                ("MappingId", "DepartmentId", "ApprovalRouteCode", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "DepartmentId", "ApprovalRouteCode", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_MAPPING_BACKUP'
            from nexa.department_approval_mappings
            where "ApprovalRouteCode" = 'MANAGER'
            on conflict ("MappingId") do nothing;


            insert into nexa.rev868c3_role_backup
                ("RoleId", "Code", "Name", "IsPrivileged", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version", "CapturedAt", "CapturedBy")
            select "Id", "Code", "Name", "IsPrivileged", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version", TIMESTAMPTZ '2026-08-09T00:00:00+00:00', 'REV868C3_ROLE_BACKUP'
            from nexa.roles
            where "Code" = 'DEPARTMENT_MANAGER'
            on conflict ("RoleId") do nothing;
        """);

        migrationBuilder.AddColumn<string>(name: "PayrollEmployeeId", schema: "nexa", table: "employees", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Gender", schema: "nexa", table: "employees", type: "character varying(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Qualification", schema: "nexa", table: "employees", type: "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<DateOnly>(name: "DateOfBirth", schema: "nexa", table: "employees", type: "date", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DateOfJoiningAccuracy", schema: "nexa", table: "employees", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsDateOfJoiningApproximate", schema: "nexa", table: "employees", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "ApproximateDateNote", schema: "nexa", table: "employees", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "FunctionalResponsibility", schema: "nexa", table: "employees", type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "WorkLocation", schema: "nexa", table: "employees", type: "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ManagerScope", schema: "nexa", table: "employees", type: "character varying(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>(name: "LegacyDepartment", schema: "nexa", table: "employees", type: "character varying(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Scope", schema: "nexa", table: "department_approval_mappings", type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "ALL");

        migrationBuilder.CreateTable(
            name: "employee_department_history",
            schema: "nexa",
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
                Version = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_department_history", x => x.Id);
                table.ForeignKey("FK_employee_department_history_departments_NewDepartmentId", x => x.NewDepartmentId, principalSchema: "nexa", principalTable: "departments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_employee_department_history_departments_PreviousDepartmentId", x => x.PreviousDepartmentId, principalSchema: "nexa", principalTable: "departments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_employee_department_history_employees_EmployeeId", x => x.EmployeeId, principalSchema: "nexa", principalTable: "employees", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_employee_department_history_CorrelationId", schema: "nexa", table: "employee_department_history", column: "CorrelationId");
        migrationBuilder.CreateIndex(name: "IX_employee_department_history_EmployeeId_CreatedAt", schema: "nexa", table: "employee_department_history", columns: new[] { "EmployeeId", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_employee_department_history_NewDepartmentId", schema: "nexa", table: "employee_department_history", column: "NewDepartmentId");
        migrationBuilder.CreateIndex(name: "IX_employee_department_history_PreviousDepartmentId", schema: "nexa", table: "employee_department_history", column: "PreviousDepartmentId");

        migrationBuilder.CreateTable(
            name: "purchase_approval_workflow_steps",
            schema: "nexa",
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
                Version = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_purchase_approval_workflow_steps", x => x.Id);
                table.CheckConstraint("CK_purchase_workflow_amounts_valid", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\") AND \"StepNumber\" > 0");
            });

        migrationBuilder.CreateIndex(name: "IX_purchase_approval_workflow_steps_RouteCode_IsActive", schema: "nexa", table: "purchase_approval_workflow_steps", columns: new[] { "RouteCode", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_purchase_approval_workflow_steps_RouteCode_StepNumber_EffectiveFrom", schema: "nexa", table: "purchase_approval_workflow_steps", columns: new[] { "RouteCode", "StepNumber", "EffectiveFrom" }, unique: true);


        migrationBuilder.CreateIndex(
            name: "IX_employees_PayrollEmployeeId",
            schema: "nexa",
            table: "employees",
            column: "PayrollEmployeeId",
            unique: true,
            filter: "\"PayrollEmployeeId\" IS NOT NULL");

        migrationBuilder.DropIndex(name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod", schema: "nexa", table: "department_approval_mappings");
        migrationBuilder.DropIndex(name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1", schema: "nexa", table: "department_approval_mappings");

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_Route_Scope_From",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_Route_Scope_Active",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "IsActive" });

        migrationBuilder.Sql(BuildUpsertSql());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_employees_PayrollEmployeeId", schema: "nexa", table: "employees");
        migrationBuilder.DropIndex(name: "IX_department_approval_mappings_DepartmentId_Route_Scope_From", schema: "nexa", table: "department_approval_mappings");
        migrationBuilder.DropIndex(name: "IX_department_approval_mappings_DepartmentId_Route_Scope_Active", schema: "nexa", table: "department_approval_mappings");

        migrationBuilder.Sql("""
            delete from nexa.employee_status_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "Reason" like 'REV868C3 employee workbook reconciliation%';
            delete from nexa.employee_department_history where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION' and "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION';
            delete from nexa.audit_logs where "CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION' and "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';

            delete from nexa.department_approval_mappings m
            where m."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
              and not exists (select 1 from nexa.rev868c3_department_mapping_backup b where b."MappingId" = m."Id");

            update nexa.department_approval_mappings m
            set "DepartmentId" = b."DepartmentId",
                "ApprovalRouteCode" = b."ApprovalRouteCode",
                "PrimaryApproverEmployeeId" = b."PrimaryApproverEmployeeId",
                "AlternateApproverEmployeeId" = b."AlternateApproverEmployeeId",
                "EffectiveFrom" = b."EffectiveFrom",
                "EffectiveTo" = b."EffectiveTo",
                "IsActive" = b."IsActive",
                "Remarks" = b."Remarks",
                "UpdatedAt" = b."UpdatedAt",
                "UpdatedBy" = b."UpdatedBy",
                "Version" = b."Version"
            from nexa.rev868c3_department_mapping_backup b
            where m."Id" = b."MappingId";

            delete from nexa.employee_role_assignments where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
            delete from nexa.role_page_permissions where "CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
            delete from nexa.roles r
            where r."Code" = 'DEPARTMENT_MANAGER'
              and r."CreatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION'
              and not exists (select 1 from nexa.rev868c3_role_backup b where b."RoleId" = r."Id");

            update nexa.roles r
            set "Code" = b."Code",
                "Name" = b."Name",
                "IsPrivileged" = b."IsPrivileged",
                "IsActive" = b."IsActive",
                "CreatedAt" = b."CreatedAt",
                "CreatedBy" = b."CreatedBy",
                "UpdatedAt" = b."UpdatedAt",
                "UpdatedBy" = b."UpdatedBy",
                "Version" = b."Version"
            from nexa.rev868c3_role_backup b
            where r."Id" = b."RoleId";

            delete from nexa.employees e
            where e."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
              and not exists (select 1 from nexa.rev868c3_employee_backup b where b."EmployeeId" = e."Id");

            update nexa.employees e
            set "EmployeeName" = b."EmployeeName",
                "OriginalImportedName" = b."OriginalImportedName",
                "EmployeeType" = b."EmployeeType",
                "Grade" = b."Grade",
                "DepartmentId" = b."DepartmentId",
                "DesignationId" = b."DesignationId",
                "Status" = b."Status",
                "DateOfJoining" = b."DateOfJoining",
                "OfficialEmail" = b."OfficialEmail",
                "MobileNumber" = b."MobileNumber",
                "LoginEnabled" = b."LoginEnabled",
                "ApprovalStatus" = b."ApprovalStatus",
                "IsEmployeeCodeLocked" = b."IsEmployeeCodeLocked",
                "UpdatedAt" = b."UpdatedAt",
                "UpdatedBy" = b."UpdatedBy",
                "Version" = b."Version"
            from nexa.rev868c3_employee_backup b
            where e."Id" = b."EmployeeId";

            update nexa.departments d
            set "Code" = b."Code",
                "Name" = b."Name",
                "IsActive" = b."IsActive",
                "UpdatedAt" = b."UpdatedAt",
                "UpdatedBy" = b."UpdatedBy",
                "Version" = b."Version"
            from nexa.rev868c3_department_backup b
            where d."Id" = b."DepartmentId";

            delete from nexa.departments d
            where d."CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION'
              and not exists (select 1 from nexa.rev868c3_department_backup b where b."DepartmentId" = d."Id");

            delete from nexa.designations where "CreatedBy" = 'REV868C3_EMPLOYEE_DEPARTMENT_MANAGER_RECONCILIATION';

            do $$
            begin
                if exists (select 1 from nexa.employees where "EmployeeCode" is null or length(trim("EmployeeCode")) = 0) then
                    raise exception 'REV868C3 rollback blocked: employee code integrity failure';
                end if;
            end $$;
        """);

        migrationBuilder.DropTable(name: "purchase_approval_workflow_steps", schema: "nexa");

        migrationBuilder.DropTable(name: "employee_department_history", schema: "nexa");

        migrationBuilder.DropColumn(name: "Scope", schema: "nexa", table: "department_approval_mappings");
        migrationBuilder.DropColumn(name: "PayrollEmployeeId", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "Gender", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "Qualification", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "DateOfBirth", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "DateOfJoiningAccuracy", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "IsDateOfJoiningApproximate", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "ApproximateDateNote", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "FunctionalResponsibility", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "WorkLocation", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "ManagerScope", schema: "nexa", table: "employees");
        migrationBuilder.DropColumn(name: "LegacyDepartment", schema: "nexa", table: "employees");

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "EffectiveFrom" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod1",
            schema: "nexa",
            table: "department_approval_mappings",
            columns: new[] { "DepartmentId", "ApprovalRouteCode", "IsActive" });

        migrationBuilder.Sql("""
            drop table if exists nexa.rev868c3_department_mapping_backup;
            drop table if exists nexa.rev868c3_role_backup;
            drop table if exists nexa.rev868c3_department_backup;
            drop table if exists nexa.rev868c3_employee_backup;
        """);
    }

    private static string BuildUpsertSql()
    {
        var sb = new StringBuilder();
        foreach (var department in Rev868C3EmployeeWorkbookData.Departments)
        {
            sb.AppendLine($"""
                insert into nexa.departments ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                values ('{Id("department", department.Code)}', {Sql(department.Code)}, {Sql(department.Name)}, {Bool(department.IsActive)}, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0)
                on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}';
                """);
        }

        var designations = Rev868C3EmployeeWorkbookData.ActiveEmployees.Select(x => x.HrDesignation).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var designation in designations)
        {
            sb.AppendLine($"""
                insert into nexa.designations ("Id", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                values ('{Id("designation", designation)}', {Sql(Code(designation))}, {Sql(designation)}, true, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0)
                on conflict ("Code") do update set "Name" = excluded."Name", "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}';
                """);
        }

        sb.AppendLine($"""
            do $rev868c3_fk_guard$
            begin
                if exists (
                    select 1
                    from (values {Values(Rev868C3EmployeeWorkbookData.Departments.Select(x => x.Code))}) as expected("Code")
                    left join nexa.departments d on d."Code" = expected."Code"
                    where d."Id" is null
                ) then
                    raise exception 'REV868C3 missing department lookup for employee reconciliation';
                end if;

                if exists (
                    select 1
                    from (values {Values(designations.Select(Code))}) as expected("Code")
                    left join nexa.designations d on d."Code" = expected."Code"
                    where d."Id" is null
                ) then
                    raise exception 'REV868C3 missing designation lookup for employee reconciliation';
                end if;
            end
            $rev868c3_fk_guard$;
            """);


        foreach (var employee in Rev868C3EmployeeWorkbookData.ActiveEmployees)
        {
            var approximate = employee.DateOfJoiningAccuracy.StartsWith("Approximate", StringComparison.OrdinalIgnoreCase);
            sb.AppendLine($"""
                insert into nexa.employees ("Id", "EmployeeCode", "PayrollEmployeeId", "EmployeeName", "OriginalImportedName", "Gender", "Qualification", "DateOfBirth", "EmployeeType", "Grade", "DepartmentId", "DesignationId", "Status", "DateOfJoining", "DateOfJoiningAccuracy", "IsDateOfJoiningApproximate", "ApproximateDateNote", "FunctionalResponsibility", "WorkLocation", "ManagerScope", "LegacyDepartment", "OfficialEmail", "MobileNumber", "LoginEnabled", "ApprovalStatus", "IsEmployeeCodeLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                select '{Id("employee", employee.EmployeeCode)}', {Sql(employee.EmployeeCode)}, {Sql(employee.PayrollEmployeeId == "NA" ? null : employee.PayrollEmployeeId)}, {Sql(employee.EmployeeName)}, {Sql(employee.EmployeeName)}, {Sql(employee.Gender)}, {Sql(employee.Qualification)}, {Date(employee.DateOfBirth)}, {Sql(employee.EmploymentType)}, {Sql(employee.Grade)}, d."Id", g."Id", 'Active', {Date(employee.DateOfJoining)}, {Sql(employee.DateOfJoiningAccuracy)}, {Bool(approximate)}, {Sql(approximate ? employee.DateOfJoiningAccuracy : null)}, {Sql(employee.FunctionalResponsibility)}, {Sql(employee.WorkLocation)}, {Sql(employee.ManagerScope)}, {Sql(employee.LegacyDepartment)}, null, null, false, 'SeedApproved', true, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0
                from nexa.departments d
                join nexa.designations g on g."Code" = {Sql(Code(employee.HrDesignation))}
                where d."Code" = {Sql(employee.FinalDepartmentCode)}
                on conflict ("EmployeeCode") do update set
                    "PayrollEmployeeId" = excluded."PayrollEmployeeId", "EmployeeName" = excluded."EmployeeName", "Gender" = excluded."Gender", "Qualification" = excluded."Qualification", "DateOfBirth" = excluded."DateOfBirth", "EmployeeType" = excluded."EmployeeType", "Grade" = excluded."Grade", "DepartmentId" = excluded."DepartmentId", "DesignationId" = excluded."DesignationId", "Status" = 'Active', "DateOfJoining" = excluded."DateOfJoining", "DateOfJoiningAccuracy" = excluded."DateOfJoiningAccuracy", "IsDateOfJoiningApproximate" = excluded."IsDateOfJoiningApproximate", "ApproximateDateNote" = excluded."ApproximateDateNote", "FunctionalResponsibility" = excluded."FunctionalResponsibility", "WorkLocation" = excluded."WorkLocation", "ManagerScope" = excluded."ManagerScope", "LegacyDepartment" = excluded."LegacyDepartment", "IsEmployeeCodeLocked" = true, "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}';
                """);
        }

        foreach (var relieved in Rev868C3EmployeeWorkbookData.RelievedEmployees)
        {
            sb.AppendLine($"""
                update nexa.employees
                set "Status" = 'Left / Resigned', "LoginEnabled" = false, "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}'
                where "EmployeeCode" = {Sql(relieved.EmployeeCode)};
                """);
        }

        sb.AppendLine($"""
            insert into nexa.roles ("Id", "Code", "Name", "IsPrivileged", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            values ('{Id("role", "department_manager")}', 'DEPARTMENT_MANAGER', 'Department Manager', false, true, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0)
            on conflict ("Code") do update set "Name" = excluded."Name", "IsPrivileged" = false, "IsActive" = true, "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
            """);

        sb.AppendLine($"""
            do $rev868c3_role_guard$
            begin
                if not exists (select 1 from nexa.roles where "Code" = 'DEPARTMENT_MANAGER') then
                    raise exception 'REV868C3 missing DEPARTMENT_MANAGER role lookup';
                end if;

                if exists (
                    select 1
                    from (values ('purchase.requisitions'), ('purchase.requisition-approvals')) as expected("PageKey")
                    left join nexa.page_definitions p on p."PageKey" = expected."PageKey"
                    where p."Id" is null
                ) then
                    raise exception 'REV868C3 missing page lookup for department manager permissions';
                end if;
            end
            $rev868c3_role_guard$;


            insert into nexa.role_page_permissions ("Id", "RoleId", "PageDefinitionId", "CanView", "CanCreate", "CanUpdate", "CanSubmit", "CanVerify", "CanApprove", "CanReject", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanCancel", "CanDeactivate", "CanPrint", "CanDownload", "CanExport", "CanUploadAttachment", "CanReplaceAttachment", "CanViewCommercialValues", "CanViewAuditHistory", "HasFullControl", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            select '{Id("rev868c3-department-manager-permission", "purchase-requisitions")}', r."Id", p."Id", true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
            from nexa.roles r join nexa.page_definitions p on p."PageKey" = 'purchase.requisitions'
            where r."Code" = 'DEPARTMENT_MANAGER'
            on conflict ("RoleId", "PageDefinitionId") do update set "CanView" = excluded."CanView", "CanCreate" = excluded."CanCreate", "CanUpdate" = excluded."CanUpdate", "CanSubmit" = excluded."CanSubmit", "CanVerify" = excluded."CanVerify", "CanApprove" = excluded."CanApprove", "CanReject" = excluded."CanReject", "CanRequestClarification" = excluded."CanRequestClarification", "CanRequestRevision" = excluded."CanRequestRevision", "CanResubmit" = excluded."CanResubmit", "CanCancel" = excluded."CanCancel", "CanDeactivate" = excluded."CanDeactivate", "CanPrint" = excluded."CanPrint", "CanDownload" = excluded."CanDownload", "CanExport" = excluded."CanExport", "CanUploadAttachment" = excluded."CanUploadAttachment", "CanReplaceAttachment" = excluded."CanReplaceAttachment", "CanViewCommercialValues" = excluded."CanViewCommercialValues", "CanViewAuditHistory" = excluded."CanViewAuditHistory", "HasFullControl" = excluded."HasFullControl", "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';

            insert into nexa.role_page_permissions ("Id", "RoleId", "PageDefinitionId", "CanView", "CanCreate", "CanUpdate", "CanSubmit", "CanVerify", "CanApprove", "CanReject", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanCancel", "CanDeactivate", "CanPrint", "CanDownload", "CanExport", "CanUploadAttachment", "CanReplaceAttachment", "CanViewCommercialValues", "CanViewAuditHistory", "HasFullControl", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            select '{Id("rev868c3-department-manager-permission", "purchase-requisition-approvals")}', r."Id", p."Id", true, false, false, false, false, true, true, true, true, false, false, false, false, false, false, false, false, false, true, false, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
            from nexa.roles r join nexa.page_definitions p on p."PageKey" = 'purchase.requisition-approvals'
            where r."Code" = 'DEPARTMENT_MANAGER'
            on conflict ("RoleId", "PageDefinitionId") do update set "CanView" = excluded."CanView", "CanCreate" = excluded."CanCreate", "CanUpdate" = excluded."CanUpdate", "CanSubmit" = excluded."CanSubmit", "CanVerify" = excluded."CanVerify", "CanApprove" = excluded."CanApprove", "CanReject" = excluded."CanReject", "CanRequestClarification" = excluded."CanRequestClarification", "CanRequestRevision" = excluded."CanRequestRevision", "CanResubmit" = excluded."CanResubmit", "CanCancel" = excluded."CanCancel", "CanDeactivate" = excluded."CanDeactivate", "CanPrint" = excluded."CanPrint", "CanDownload" = excluded."CanDownload", "CanExport" = excluded."CanExport", "CanUploadAttachment" = excluded."CanUploadAttachment", "CanReplaceAttachment" = excluded."CanReplaceAttachment", "CanViewCommercialValues" = excluded."CanViewCommercialValues", "CanViewAuditHistory" = excluded."CanViewAuditHistory", "HasFullControl" = excluded."HasFullControl", "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = 'REV868C3_DEPARTMENT_MANAGER_PERMISSION';
            """);

        var managerCodes = Rev868C3EmployeeWorkbookData.ManagerMappings.SelectMany(x => new[] { x.PrimaryManagerCode, x.AlternateManagerCode }).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var employeeCode in managerCodes)
        {
            sb.AppendLine($"""
                insert into nexa.employee_role_assignments ("Id", "EmployeeId", "RoleId", "EffectiveFrom", "EffectiveTo", "ApprovalStatus", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                select '{Id("rev868c3-department-manager-role", employeeCode)}', e."Id", r."Id", DATE '2026-08-09', null, 'SeedApproved', 'REV868C3 approved department manager approval permission', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', 'REV868C3_DEPARTMENT_MANAGER_PERMISSION', null, null, 0
                from nexa.employees e
                join nexa.roles r on r."Code" = 'DEPARTMENT_MANAGER'
                where e."EmployeeCode" = {Sql(employeeCode)}
                on conflict ("EmployeeId", "RoleId", "EffectiveFrom") do nothing;
                """);
        }

        foreach (var mapping in Rev868C3EmployeeWorkbookData.ManagerMappings)
        {
            sb.AppendLine($"""
                insert into nexa.department_approval_mappings ("Id", "DepartmentId", "ApprovalRouteCode", "Scope", "PrimaryApproverEmployeeId", "AlternateApproverEmployeeId", "EffectiveFrom", "EffectiveTo", "IsActive", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
                select '{Id("rev868c3-manager-mapping", mapping.MappingCode)}', d."Id", 'MANAGER', {Sql(mapping.Scope)}, p."Id", a."Id", DATE '2026-08-09', null, true, {Sql(mapping.ControlNote)}, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0
                from nexa.departments d
                join nexa.employees p on p."EmployeeCode" = {Sql(mapping.PrimaryManagerCode)}
                join nexa.employees a on a."EmployeeCode" = {Sql(mapping.AlternateManagerCode)}
                where d."Code" = {Sql(mapping.DepartmentCode)}
                on conflict ("DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom") do update set
                    "PrimaryApproverEmployeeId" = excluded."PrimaryApproverEmployeeId", "AlternateApproverEmployeeId" = excluded."AlternateApproverEmployeeId", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}';
                """);
        }

        sb.AppendLine($"""
            insert into nexa.purchase_approval_workflow_steps ("Id", "RouteCode", "MinimumAmount", "MaximumAmount", "StepNumber", "ApproverResolutionType", "ApproverEmployeeCode", "ApproverRoleCode", "IsActive", "EffectiveFrom", "EffectiveTo", "Remarks", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            values
              ('{Id("rev868c3-workflow", "MANAGER_ONLY", "1")}', 'MANAGER_ONLY', 0.00, 50000.00, 1, 'DEPARTMENT_MAPPING', null, 'MANAGER', true, DATE '2026-08-09', null, '0-50000 department manager approval', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0),
              ('{Id("rev868c3-workflow", "MANAGER_MD", "1")}', 'MANAGER_MD', 50000.01, 500000.00, 1, 'DEPARTMENT_MAPPING', null, 'MANAGER', true, DATE '2026-08-09', null, '50000.01-500000 department manager step', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0),
              ('{Id("rev868c3-workflow", "MANAGER_MD", "2")}', 'MANAGER_MD', 50000.01, 500000.00, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR', true, DATE '2026-08-09', null, '50000.01-500000 MD step', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0),
              ('{Id("rev868c3-workflow", "MANAGER_MD_TD", "1")}', 'MANAGER_MD_TD', 500000.01, null, 1, 'DEPARTMENT_MAPPING', null, 'MANAGER', true, DATE '2026-08-09', null, 'above 500000 department manager step', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0),
              ('{Id("rev868c3-workflow", "MANAGER_MD_TD", "2")}', 'MANAGER_MD_TD', 500000.01, null, 2, 'FIXED_EMPLOYEE_ROLE', 'SESS-002', 'MANAGING_DIRECTOR', true, DATE '2026-08-09', null, 'above 500000 MD step', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0),
              ('{Id("rev868c3-workflow", "MANAGER_MD_TD", "3")}', 'MANAGER_MD_TD', 500000.01, null, 3, 'FIXED_EMPLOYEE_ROLE', 'SESS-001', 'TECHNICAL_DIRECTOR', true, DATE '2026-08-09', null, 'above 500000 TD CEO step', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0)
            on conflict ("RouteCode", "StepNumber", "EffectiveFrom") do update set
                "MinimumAmount" = excluded."MinimumAmount", "MaximumAmount" = excluded."MaximumAmount", "ApproverResolutionType" = excluded."ApproverResolutionType", "ApproverEmployeeCode" = excluded."ApproverEmployeeCode", "ApproverRoleCode" = excluded."ApproverRoleCode", "IsActive" = true, "Remarks" = excluded."Remarks", "UpdatedAt" = TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', "UpdatedBy" = '{Actor}';
        """);

        sb.AppendLine($"""
            insert into nexa.employee_status_history ("Id", "EmployeeId", "OldStatus", "NewStatus", "Reason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            select gen_random_uuid(), e."Id", b."Status", e."Status", 'REV868C3 employee workbook reconciliation; SourceWorkbook={Rev868C3EmployeeWorkbookData.SourceWorkbook}', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0
            from nexa.employees e
            left join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id"
            where e."EmployeeCode" like 'SESS-%'
              and b."EmployeeId" is not null and b."Status" is distinct from e."Status"
              and not exists (select 1 from nexa.employee_status_history h where h."EmployeeId" = e."Id" and h."Reason" like 'REV868C3 employee workbook reconciliation%');

            insert into nexa.employee_department_history ("Id", "EmployeeId", "PreviousDepartmentId", "NewDepartmentId", "Reason", "SourceRevision", "CorrelationId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            select gen_random_uuid(), e."Id", b."DepartmentId", e."DepartmentId", 'REV868C3 approved department reconciliation; SourceWorkbook={Rev868C3EmployeeWorkbookData.SourceWorkbook}', 'REV868C3', 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION', TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0
            from nexa.employees e
            left join nexa.rev868c3_employee_backup b on b."EmployeeId" = e."Id"
            where e."EmployeeCode" like 'SESS-%'
              and (b."EmployeeId" is null or b."DepartmentId" is distinct from e."DepartmentId")
              and not exists (select 1 from nexa.employee_department_history h where h."EmployeeId" = e."Id" and h."CorrelationId" = 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION');

            insert into nexa.audit_logs ("Id", "UserLoginId", "UserRole", "Module", "EntityName", "EntityId", "Action", "OldValue", "NewValue", "Reason", "Result", "CorrelationId", "IpAddress", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "Version")
            values (gen_random_uuid(), 'system-migration', 'SYSTEM', 'Employees', 'EmployeeWorkbook', 'REV868C3', 'ReconcileEmployeeDepartmentManagerWorkbook', null, {Sql("{\"activeEmployees\":42,\"relievedEmployees\":9,\"departments\":12,\"managerMappings\":14}")}, 'Approved REV868C3 employee workbook source checkpoint', 'Success', 'REV868C3_EMPLOYEE_WORKBOOK_RECONCILIATION', null, TIMESTAMPTZ '{Stamp:yyyy-MM-ddTHH:mm:sszzz}', '{Actor}', null, null, 0)
            on conflict do nothing;
        """);

        return sb.ToString();
    }

    private static Guid Id(params string[] parts)
    {
        var input = string.Join("|", parts).ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes[..16]);
    }

    private static string Code(string value) => value.ToUpperInvariant().Replace("/", "_", StringComparison.Ordinal).Replace(" ", "_", StringComparison.Ordinal).Replace("-", "_", StringComparison.Ordinal).Replace(".", string.Empty, StringComparison.Ordinal);
    private static string Sql(string value) => value is null ? "null" : "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    private static string Values(IEnumerable<string> values) => string.Join(", ", values.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Select(value => $"({Sql(value)})"));
    private static string Date(DateOnly? value) => value.HasValue ? $"DATE '{value.Value:yyyy-MM-dd}'" : "null";
    private static string Bool(bool value) => value ? "true" : "false";
}
