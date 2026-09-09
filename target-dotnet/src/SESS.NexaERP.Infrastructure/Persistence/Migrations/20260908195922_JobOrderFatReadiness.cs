using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class JobOrderFatReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.AddColumn<string>(
                name: "FatReadinessStatus",
                schema: "advance",
                table: "job_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NOT_RECONCILED");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FatReconciledAt",
                schema: "advance",
                table: "job_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LatestFatReconciliationId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_order_fat_custody_explanations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Disposition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExplainedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_order_fat_custody_explanations", x => x.Id);
                    table.UniqueConstraint("AK_job_order_fat_custody_explanations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_fat_custody_explanation", "\"QuantityBase\">0 AND \"Disposition\" IN ('LOST','SCRAPPED') AND \"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"Reason\"))>0");
                    table.ForeignKey(
                        name: "FK_job_order_fat_custody_explanations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_custody_explanations_employee_role_assignment~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_custody_explanations_employees_ExplainedByEmp~",
                        column: x => x.ExplainedByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_custody_explanations_job_orders_CompanyId_Job~",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_custody_explanations_material_issue_lines_Com~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_order_fat_reconciliations",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    FittedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReturnedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExplainedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnexplainedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReconciledByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestFingerprint = table.Column<string>(type: "character(64)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_order_fat_reconciliations", x => x.Id);
                    table.UniqueConstraint("AK_job_order_fat_reconciliations_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.CheckConstraint("CK_fat_reconciliation", "\"AttemptNumber\">0 AND \"Result\" IN ('BLOCKED','READY') AND \"IssuedQuantityBase\">=0 AND \"FittedQuantityBase\">=0 AND \"ReturnedQuantityBase\">=0 AND \"ExplainedQuantityBase\">=0 AND \"UnexplainedQuantityBase\">=0 AND ((\"Result\"='READY' AND \"UnexplainedQuantityBase\"=0) OR (\"Result\"='BLOCKED' AND \"UnexplainedQuantityBase\">0)) AND \"ResolvedRoleAssignmentType\"='FULL'");
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliations_employee_role_assignments_Res~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliations_employees_ReconciledByEmploye~",
                        column: x => x.ReconciledByEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliations_job_orders_CompanyId_JobOrder~",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_order_fat_reconciliation_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderFatReconciliationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCodeSnapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CustodianEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodianEmployeeCodeSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IssuedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    FittedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReturnedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ReturnedLateQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExplainedLostQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    ExplainedScrappedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    UnexplainedQuantityBase = table.Column<decimal>(type: "numeric(24,6)", precision: 24, scale: 6, nullable: false),
                    Classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_order_fat_reconciliation_lines", x => x.Id);
                    table.CheckConstraint("CK_fat_reconciliation_line", "\"IssuedQuantityBase\">0 AND \"FittedQuantityBase\">=0 AND \"ReturnedQuantityBase\">=0 AND \"ReturnedLateQuantityBase\">=0 AND \"ExplainedLostQuantityBase\">=0 AND \"ExplainedScrappedQuantityBase\">=0 AND \"UnexplainedQuantityBase\">=0 AND \"ReturnedLateQuantityBase\"<=\"ReturnedQuantityBase\" AND \"Classification\" IN ('FITTED','RETURNED','RETURNED_LATE','EXPLAINED','MIXED','UNEXPLAINED')");
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliation_lines_job_order_fat_reconcilia~",
                        columns: x => new { x.CompanyId, x.JobOrderFatReconciliationId },
                        principalSchema: "advance",
                        principalTable: "job_order_fat_reconciliations",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_fat_reconciliation_lines_material_issue_lines_Com~",
                        columns: x => new { x.CompanyId, x.MaterialIssueLineId },
                        principalSchema: "advance",
                        principalTable: "material_issue_lines",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "FatReconciledByEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_job_order_fat_readiness",
                schema: "advance",
                table: "job_orders",
                sql: "\"FatReadinessStatus\" IN ('NOT_RECONCILED','BLOCKED','READY') AND ((\"FatReadinessStatus\"='NOT_RECONCILED' AND \"FatReconciledAt\" IS NULL AND \"FatReconciledByEmployeeId\" IS NULL AND \"LatestFatReconciliationId\" IS NULL) OR (\"FatReadinessStatus\"<>'NOT_RECONCILED' AND \"FatReconciledAt\" IS NOT NULL AND \"FatReconciledByEmployeeId\" IS NOT NULL AND \"LatestFatReconciliationId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_CompanyId",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_CompanyId_JobOrderId_Mat~",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                columns: new[] { "CompanyId", "JobOrderId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_CompanyId_MaterialIssueL~",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_ExplainedByEmployeeId",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                column: "ExplainedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_custody_explanations_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "job_order_fat_custody_explanations",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliation_lines_CompanyId_JobOrderFatRec~",
                schema: "advance",
                table: "job_order_fat_reconciliation_lines",
                columns: new[] { "CompanyId", "JobOrderFatReconciliationId" });

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliation_lines_CompanyId_MaterialIssueL~",
                schema: "advance",
                table: "job_order_fat_reconciliation_lines",
                columns: new[] { "CompanyId", "MaterialIssueLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliation_lines_JobOrderFatReconciliatio~",
                schema: "advance",
                table: "job_order_fat_reconciliation_lines",
                columns: new[] { "JobOrderFatReconciliationId", "MaterialIssueLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliations_CompanyId",
                schema: "advance",
                table: "job_order_fat_reconciliations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliations_CompanyId_IdempotencyKey",
                schema: "advance",
                table: "job_order_fat_reconciliations",
                columns: new[] { "CompanyId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliations_CompanyId_JobOrderId_AttemptN~",
                schema: "advance",
                table: "job_order_fat_reconciliations",
                columns: new[] { "CompanyId", "JobOrderId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliations_ReconciledByEmployeeId",
                schema: "advance",
                table: "job_order_fat_reconciliations",
                column: "ReconciledByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_fat_reconciliations_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "job_order_fat_reconciliations",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_employees_FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "FatReconciledByEmployeeId",
                principalSchema: "advance",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(JobOrderFatReadinessSql.Up);
            migrationBuilder.Sql(JobOrderFatReadinessCommandSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(JobOrderFatReadinessCommandSql.Down);
            migrationBuilder.Sql(JobOrderFatReadinessSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_employees_FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropTable(
                name: "job_order_fat_custody_explanations",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "job_order_fat_reconciliation_lines",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "job_order_fat_reconciliations",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_job_order_fat_readiness",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "FatReadinessStatus",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "FatReconciledAt",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "FatReconciledByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "LatestFatReconciliationId",
                schema: "advance",
                table: "job_orders");
        }
    }
}
