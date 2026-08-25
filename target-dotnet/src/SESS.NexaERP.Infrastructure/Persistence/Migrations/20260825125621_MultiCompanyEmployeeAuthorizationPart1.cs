using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiCompanyEmployeeAuthorizationPart1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(MultiCompanyEmployeeAuthorizationPart1Sql.PreUp);

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_IsActive",
                schema: "advance",
                table: "purchase_approval_workflow_steps");

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_StepNumber_Effec~",
                schema: "advance",
                table: "purchase_approval_workflow_steps");

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_route_settings_RouteCode",
                schema: "advance",
                table: "purchase_approval_route_settings");

            migrationBuilder.DropIndex(
                name: "IX_employee_role_assignments_EmployeeId_RoleId_EffectiveFrom",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_employee_identity_mappings_CompanyId_OrganizationId",
                schema: "advance",
                table: "employee_identity_mappings");

            migrationBuilder.DropIndex(
                name: "IX_employee_identity_mappings_Issuer_Subject_IsActive",
                schema: "advance",
                table: "employee_identity_mappings");

            migrationBuilder.DropIndex(
                name: "IX_employee_identity_mappings_OrganizationId_EmployeeId_Identi~",
                schema: "advance",
                table: "employee_identity_mappings");

            migrationBuilder.DropIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod~1",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.DropIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCode~",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                columns: new[] { "CanApprove", "CanDeactivate" },
                values: new object[] { false, false });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "roles",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("83000000-0000-0000-0000-000000000001"), "PRODUCTION_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multi-company-employee-authorization-part1", true, true, "Production Manager", null, null, 0L },
                    { new Guid("83000000-0000-0000-0000-000000000002"), "ACCOUNTS_MANAGER", new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-multi-company-employee-authorization-part1", true, true, "Accounts Manager", null, null, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_CompanyId_RouteCode_IsActi~",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "CompanyId", "RouteCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_CompanyId_RouteCode_StepNu~",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "CompanyId", "RouteCode", "StepNumber", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_route_settings_CompanyId_RouteCode",
                schema: "advance",
                table: "purchase_approval_route_settings",
                columns: new[] { "CompanyId", "RouteCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_CompanyId_EmployeeId_RoleId_Effec~",
                schema: "advance",
                table: "employee_role_assignments",
                columns: new[] { "CompanyId", "EmployeeId", "RoleId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_EmployeeId",
                schema: "advance",
                table: "employee_role_assignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_CompanyId_Issuer_Subject_IsActive",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "CompanyId", "Issuer", "Subject", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_CompanyId_OrganizationId_Employe~",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "CompanyId", "OrganizationId", "EmployeeId", "IdentityType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IdentityType\" = 'HUMAN'");

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_CompanyId_DepartmentId_Approv~1",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "CompanyId", "DepartmentId", "ApprovalRouteCode", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_CompanyId_DepartmentId_Approva~",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "CompanyId", "DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_DepartmentId",
                schema: "advance",
                table: "department_approval_mappings",
                column: "DepartmentId");

            migrationBuilder.Sql(MultiCompanyEmployeeAuthorizationPart1Sql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(MultiCompanyEmployeeAuthorizationPart1Sql.Down);

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_workflow_steps_CompanyId_RouteCode_IsActi~",
                schema: "advance",
                table: "purchase_approval_workflow_steps");

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_workflow_steps_CompanyId_RouteCode_StepNu~",
                schema: "advance",
                table: "purchase_approval_workflow_steps");

            migrationBuilder.DropIndex(
                name: "IX_purchase_approval_route_settings_CompanyId_RouteCode",
                schema: "advance",
                table: "purchase_approval_route_settings");

            migrationBuilder.DropIndex(
                name: "IX_employee_role_assignments_CompanyId_EmployeeId_RoleId_Effec~",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_employee_role_assignments_EmployeeId",
                schema: "advance",
                table: "employee_role_assignments");

            migrationBuilder.DropIndex(
                name: "IX_employee_identity_mappings_CompanyId_Issuer_Subject_IsActive",
                schema: "advance",
                table: "employee_identity_mappings");

            migrationBuilder.DropIndex(
                name: "IX_employee_identity_mappings_CompanyId_OrganizationId_Employe~",
                schema: "advance",
                table: "employee_identity_mappings");

            migrationBuilder.DropIndex(
                name: "IX_department_approval_mappings_CompanyId_DepartmentId_Approv~1",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.DropIndex(
                name: "IX_department_approval_mappings_CompanyId_DepartmentId_Approva~",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.DropIndex(
                name: "IX_department_approval_mappings_DepartmentId",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("83000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("83000000-0000-0000-0000-000000000002"));

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                columns: new[] { "CanApprove", "CanDeactivate" },
                values: new object[] { true, true });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_IsActive",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "RouteCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_workflow_steps_RouteCode_StepNumber_Effec~",
                schema: "advance",
                table: "purchase_approval_workflow_steps",
                columns: new[] { "RouteCode", "StepNumber", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_route_settings_RouteCode",
                schema: "advance",
                table: "purchase_approval_route_settings",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_role_assignments_EmployeeId_RoleId_EffectiveFrom",
                schema: "advance",
                table: "employee_role_assignments",
                columns: new[] { "EmployeeId", "RoleId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_CompanyId_OrganizationId",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "CompanyId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_Issuer_Subject_IsActive",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "Issuer", "Subject", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_employee_identity_mappings_OrganizationId_EmployeeId_Identi~",
                schema: "advance",
                table: "employee_identity_mappings",
                columns: new[] { "OrganizationId", "EmployeeId", "IdentityType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IdentityType\" = 'HUMAN'");

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCod~1",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_department_approval_mappings_DepartmentId_ApprovalRouteCode~",
                schema: "advance",
                table: "department_approval_mappings",
                columns: new[] { "DepartmentId", "ApprovalRouteCode", "Scope", "EffectiveFrom" },
                unique: true);
        }
    }
}
