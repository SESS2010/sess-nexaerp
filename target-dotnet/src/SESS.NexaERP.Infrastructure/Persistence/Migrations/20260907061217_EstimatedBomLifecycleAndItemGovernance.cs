using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstimatedBomLifecycleAndItemGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF current_setting('server_version_num')::integer < 170000 OR current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Estimated BOM lifecycle migration cluster guard refused this database.'; END IF; END $$;");
            migrationBuilder.AddColumn<Guid>(
                name: "CommercialBaselineRevisionId",
                schema: "advance",
                table: "estimated_boms",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_page_permissions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PageDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanView = table.Column<bool>(type: "boolean", nullable: false),
                    CanCreate = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    CanSubmit = table.Column<bool>(type: "boolean", nullable: false),
                    CanDownload = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewAuditHistory = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_page_permissions", x => x.Id);
                    table.UniqueConstraint("AK_employee_page_permissions_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_employee_page_permissions_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_page_permissions_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_page_permissions_page_definitions_PageDefinitionId",
                        column: x => x.PageDefinitionId,
                        principalSchema: "advance",
                        principalTable: "page_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estimated_bom_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedBomId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedBomRevisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estimated_bom_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estimated_bom_history_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_history_employee_role_assignments_ResolvedRol~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_history_estimated_bom_revisions_CompanyId_Est~",
                        columns: x => new { x.CompanyId, x.EstimatedBomRevisionId },
                        principalSchema: "advance",
                        principalTable: "estimated_bom_revisions",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estimated_bom_history_estimated_boms_CompanyId_EstimatedBom~",
                        columns: x => new { x.CompanyId, x.EstimatedBomId },
                        principalSchema: "advance",
                        principalTable: "estimated_boms",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_merge_aliases",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurvivorItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_merge_aliases", x => x.Id);
                    table.UniqueConstraint("AK_item_merge_aliases_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_item_merge_aliases_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_merge_aliases_employee_role_assignments_ResolvedRoleAs~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_merge_aliases_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_merge_aliases_items_SourceItemId",
                        column: x => x.SourceItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_merge_aliases_items_SurvivorItemId",
                        column: x => x.SurvivorItemId,
                        principalSchema: "advance",
                        principalTable: "items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_page_permissions_CompanyId",
                schema: "advance",
                table: "employee_page_permissions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_page_permissions_CompanyId_EmployeeId_PageDefiniti~",
                schema: "advance",
                table: "employee_page_permissions",
                columns: new[] { "CompanyId", "EmployeeId", "PageDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_page_permissions_EmployeeId",
                schema: "advance",
                table: "employee_page_permissions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_page_permissions_PageDefinitionId",
                schema: "advance",
                table: "employee_page_permissions",
                column: "PageDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_history_ActorEmployeeId",
                schema: "advance",
                table: "estimated_bom_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_history_CompanyId_CorrelationId",
                schema: "advance",
                table: "estimated_bom_history",
                columns: new[] { "CompanyId", "CorrelationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_history_CompanyId_EstimatedBomId_CreatedAt",
                schema: "advance",
                table: "estimated_bom_history",
                columns: new[] { "CompanyId", "EstimatedBomId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_history_CompanyId_EstimatedBomRevisionId",
                schema: "advance",
                table: "estimated_bom_history",
                columns: new[] { "CompanyId", "EstimatedBomRevisionId" });

            migrationBuilder.CreateIndex(
                name: "IX_estimated_bom_history_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "estimated_bom_history",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_item_merge_aliases_ActorEmployeeId",
                schema: "advance",
                table: "item_merge_aliases",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_item_merge_aliases_CompanyId",
                schema: "advance",
                table: "item_merge_aliases",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_item_merge_aliases_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "item_merge_aliases",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_item_merge_aliases_SourceItemId",
                schema: "advance",
                table: "item_merge_aliases",
                column: "SourceItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_merge_aliases_SurvivorItemId",
                schema: "advance",
                table: "item_merge_aliases",
                column: "SurvivorItemId");

            migrationBuilder.Sql(EstimatedBomLifecycleSql.Up);
            migrationBuilder.Sql(EstimatedBomLifecycleSql.Grants);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM advance.estimated_bom_history)
                     OR EXISTS (SELECT 1 FROM advance.item_merge_aliases)
                     OR EXISTS (SELECT 1 FROM advance.estimated_boms WHERE "CommercialBaselineRevisionId" IS NOT NULL)
                  THEN RAISE EXCEPTION 'Estimated BOM lifecycle rollback refused persisted governance or business evidence.';
                  END IF;
                END $$;
                """);
            migrationBuilder.Sql(EstimatedBomLifecycleSql.Down);
            migrationBuilder.DropTable(
                name: "employee_page_permissions",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "estimated_bom_history",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "item_merge_aliases",
                schema: "advance");

            migrationBuilder.DropColumn(
                name: "CommercialBaselineRevisionId",
                schema: "advance",
                table: "estimated_boms");
        }
    }
}
