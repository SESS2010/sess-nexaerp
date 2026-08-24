using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CalibrationPurchasePairItemTypeCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "advance",
                table: "employee_department_assignments",
                keyColumn: "Id",
                keyValue: new Guid("3d29039c-643b-094e-b88a-669004ced668"));

            migrationBuilder.AddColumn<bool>(
                name: "IsReturnable",
                schema: "advance",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                schema: "advance",
                table: "items",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false);

            migrationBuilder.InsertData(
                schema: "advance",
                table: "departments",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("5ede5f5d-44e1-9e6e-dc59-4dac61325d56"), "CALIBRATION", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Calibration", null, null, null, 0L });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "designations",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("17bf8998-2e25-dea9-98f2-c1dfe5ce18d1"), "PURCHASE_EXECUTIVE", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration", true, "Purchase Executive", null, null, 0L });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_department_assignments",
                columns: new[] { "Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("6de32dc7-f94b-3059-d577-1e3f976a3bed"), "SECONDARY", new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multicompany-foundation", new Guid("dd6ab604-a58e-4884-7df9-2ceb7456df64"), new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6"), new DateOnly(2026, 8, 24), null, new Guid("7a419fa7-2d02-d433-5df8-ec0b793043fa"), true, false, "ACTIVE", null, null, 0L });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_import_history",
                keyColumn: "Id",
                keyValue: new Guid("0f56a17e-c040-acb4-6736-1cc168a81c46"),
                column: "SourceJson",
                value: "{\"Code\":\"SESS-012\",\"Name\":\"PRIYA.E\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Purchase\",\"Skill\":\"Admin/Accounts/Stores\",\"Designation\":\"PURCHASE EXECUTIVE\",\"Roles\":[\"PURCHASE_EXECUTIVE\",\"STORES_EXECUTIVE\"]}");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"),
                columns: new[] { "DepartmentId", "DesignationId" },
                values: new object[] { new Guid("dd6ab604-a58e-4884-7df9-2ceb7456df64"), new Guid("17bf8998-2e25-dea9-98f2-c1dfe5ce18d1") });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_department_assignments",
                columns: new[] { "Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("ca76c583-3291-64a4-93cd-5edf7711f2db"), "SECONDARY", new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multicompany-foundation", new Guid("97353e2b-c03c-03ad-dad5-07e697b6429f"), new Guid("17bf8998-2e25-dea9-98f2-c1dfe5ce18d1"), new DateOnly(2026, 8, 24), null, new Guid("3e672d84-b803-74f4-c977-9738a8552abd"), true, false, "ACTIVE", null, null, 0L },
                    { new Guid("ee9f3a7f-5148-2691-3164-4ea513f2c517"), "PRIMARY", new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multicompany-foundation", new Guid("dd6ab604-a58e-4884-7df9-2ceb7456df64"), new Guid("17bf8998-2e25-dea9-98f2-c1dfe5ce18d1"), new DateOnly(2026, 8, 24), null, new Guid("3e672d84-b803-74f4-c977-9738a8552abd"), true, true, "ACTIVE", null, null, 0L }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_item_type",
                schema: "advance",
                table: "items",
                sql: "\"ItemType\" IN ('RAW_MATERIAL','COMPONENT','CONSUMABLE','SPARE','FINISHED_MACHINE','TOOL','SERVICE_ITEM','NON_STOCK')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_items_returnable_tool",
                schema: "advance",
                table: "items",
                sql: "(\"ItemType\" = 'TOOL' AND \"IsReturnable\") OR (\"ItemType\" <> 'TOOL' AND NOT \"IsReturnable\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_items_item_type",
                schema: "advance",
                table: "items");

            migrationBuilder.DropCheckConstraint(
                name: "CK_items_returnable_tool",
                schema: "advance",
                table: "items");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("5ede5f5d-44e1-9e6e-dc59-4dac61325d56"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "employee_department_assignments",
                keyColumn: "Id",
                keyValue: new Guid("6de32dc7-f94b-3059-d577-1e3f976a3bed"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "employee_department_assignments",
                keyColumn: "Id",
                keyValue: new Guid("ca76c583-3291-64a4-93cd-5edf7711f2db"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "employee_department_assignments",
                keyColumn: "Id",
                keyValue: new Guid("ee9f3a7f-5148-2691-3164-4ea513f2c517"));

            migrationBuilder.DropColumn(
                name: "IsReturnable",
                schema: "advance",
                table: "items");

            migrationBuilder.DropColumn(
                name: "ItemType",
                schema: "advance",
                table: "items");

            migrationBuilder.InsertData(
                schema: "advance",
                table: "employee_department_assignments",
                columns: new[] { "Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("3d29039c-643b-094e-b88a-669004ced668"), "PRIMARY", new Guid("70000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multicompany-foundation", new Guid("97353e2b-c03c-03ad-dad5-07e697b6429f"), new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6"), new DateOnly(2026, 8, 24), null, new Guid("3e672d84-b803-74f4-c977-9738a8552abd"), true, true, "ACTIVE", null, null, 0L });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employee_import_history",
                keyColumn: "Id",
                keyValue: new Guid("0f56a17e-c040-acb4-6736-1cc168a81c46"),
                column: "SourceJson",
                value: "{\"Code\":\"SESS-012\",\"Name\":\"PRIYA.E\",\"EmployeeType\":\"Permanent\",\"Grade\":\"Executive\",\"Department\":\"Stores\",\"Skill\":\"Admin/Accounts/Stores\",\"Designation\":\"STORES ASSISTANT\",\"Roles\":[\"PURCHASE_EXECUTIVE\",\"STORES_EXECUTIVE\"]}");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("be7613f2-52e8-5537-06b2-3e25de92c230"),
                columns: new[] { "DepartmentId", "DesignationId" },
                values: new object[] { new Guid("97353e2b-c03c-03ad-dad5-07e697b6429f"), new Guid("f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6") });

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "designations",
                keyColumn: "Id",
                keyValue: new Guid("17bf8998-2e25-dea9-98f2-c1dfe5ce18d1"));
        }
    }
}
