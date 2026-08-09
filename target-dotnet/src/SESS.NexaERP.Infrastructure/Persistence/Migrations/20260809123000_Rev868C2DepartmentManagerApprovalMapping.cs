using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[Migration("20260809123000_Rev868C2DepartmentManagerApprovalMapping")]
public partial class Rev868C2DepartmentManagerApprovalMapping : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "department_approval_mappings",
            schema: "nexa",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                ApprovalRouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                PrimaryApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                AlternateApproverEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedBy = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_department_approval_mappings", x => x.Id);
                table.CheckConstraint("CK_department_approval_mapping_effective_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                table.CheckConstraint("CK_department_approval_mapping_manager_route", "\"ApprovalRouteCode\" = 'MANAGER'");
                table.ForeignKey("FK_department_approval_mappings_departments_DepartmentId", x => x.DepartmentId, "nexa", "departments", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_department_approval_mappings_employees_AlternateApproverEmployeeId", x => x.AlternateApproverEmployeeId, "nexa", "employees", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_department_approval_mappings_employees_PrimaryApproverEmployeeId", x => x.PrimaryApproverEmployeeId, "nexa", "employees", "Id", onDelete: ReferentialAction.Restrict);
            });

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

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_PrimaryApproverEmployeeId",
            schema: "nexa",
            table: "department_approval_mappings",
            column: "PrimaryApproverEmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_department_approval_mappings_AlternateApproverEmployeeId",
            schema: "nexa",
            table: "department_approval_mappings",
            column: "AlternateApproverEmployeeId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "department_approval_mappings", schema: "nexa");
    }
}
