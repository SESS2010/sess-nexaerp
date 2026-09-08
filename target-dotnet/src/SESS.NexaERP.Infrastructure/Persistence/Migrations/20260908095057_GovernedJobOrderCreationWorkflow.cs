using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GovernedJobOrderCreationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(JobOrderGovernanceSql.Preflight);
            migrationBuilder.AddColumn<string>(
                name: "AccountsConfirmationActorRoleCode",
                schema: "advance",
                table: "job_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountsConfirmationReason",
                schema: "advance",
                table: "job_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountsConfirmationRoleAssignmentId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountsConfirmationRoleAssignmentType",
                schema: "advance",
                table: "job_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccountsConfirmedAt",
                schema: "advance",
                table: "job_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationIdempotencyKey",
                schema: "advance",
                table: "job_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationRequestFingerprint",
                schema: "advance",
                table: "job_orders",
                type: "character(64)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatedActorRoleCode",
                schema: "advance",
                table: "job_orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatedRoleAssignmentId",
                schema: "advance",
                table: "job_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatedRoleAssignmentType",
                schema: "advance",
                table: "job_orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MachineOrdinal",
                schema: "advance",
                table: "job_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_order_history",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRoleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResolvedRoleAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedRoleAssignmentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_order_history", x => x.Id);
                    table.UniqueConstraint("AK_job_order_history_CompanyId_Id", x => new { x.CompanyId, x.Id });
                    table.ForeignKey(
                        name: "FK_job_order_history_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "advance",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_history_employee_role_assignments_ResolvedRoleAss~",
                        column: x => x.ResolvedRoleAssignmentId,
                        principalSchema: "advance",
                        principalTable: "employee_role_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_history_employees_ActorEmployeeId",
                        column: x => x.ActorEmployeeId,
                        principalSchema: "advance",
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_order_history_job_orders_CompanyId_JobOrderId",
                        columns: x => new { x.CompanyId, x.JobOrderId },
                        principalSchema: "advance",
                        principalTable: "job_orders",
                        principalColumns: new[] { "CompanyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_AccountsConfirmationRoleAssignmentId",
                schema: "advance",
                table: "job_orders",
                column: "AccountsConfirmationRoleAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "AccountsConfirmedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_ConfirmationIdempotencyKey",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "ConfirmationIdempotencyKey" },
                unique: true,
                filter: "\"ConfirmationIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CompanyId_CustomerPurchaseOrderLineId_MachineOrd~",
                schema: "advance",
                table: "job_orders",
                columns: new[] { "CompanyId", "CustomerPurchaseOrderLineId", "MachineOrdinal" },
                unique: true,
                filter: "\"CustomerPurchaseOrderLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders",
                column: "CustomerPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "job_orders",
                column: "CustomerPurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "InitiatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_orders_InitiatedRoleAssignmentId",
                schema: "advance",
                table: "job_orders",
                column: "InitiatedRoleAssignmentId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_job_order_joint_governance",
                schema: "advance",
                table: "job_orders",
                sql: "(\"CustomerPurchaseOrderId\" IS NULL AND \"CustomerPurchaseOrderLineId\" IS NULL AND \"MachineOrdinal\" IS NULL AND \"InitiatedByEmployeeId\" IS NULL AND \"InitiatedRoleAssignmentId\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL) OR (\"CustomerPurchaseOrderId\" IS NOT NULL AND \"CustomerPurchaseOrderLineId\" IS NOT NULL AND \"MachineOrdinal\">0 AND \"InitiatedByEmployeeId\" IS NOT NULL AND \"InitiatedActorRoleCode\" IS NOT NULL AND \"InitiatedRoleAssignmentId\" IS NOT NULL AND \"InitiatedRoleAssignmentType\" IS NOT NULL AND ((\"Status\"='PENDING_ACCOUNTS' AND \"AccountsConfirmedAt\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NULL) OR (\"Status\"='OPEN' AND \"AccountsConfirmedAt\" IS NOT NULL AND \"AccountsConfirmedByEmployeeId\" IS NOT NULL AND \"AccountsConfirmationActorRoleCode\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentType\" IS NOT NULL AND NULLIF(btrim(\"AccountsConfirmationReason\"),'') IS NOT NULL)))");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_history_ActorEmployeeId",
                schema: "advance",
                table: "job_order_history",
                column: "ActorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_history_CompanyId",
                schema: "advance",
                table: "job_order_history",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_job_order_history_CompanyId_JobOrderId_CreatedAt",
                schema: "advance",
                table: "job_order_history",
                columns: new[] { "CompanyId", "JobOrderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_job_order_history_CorrelationId",
                schema: "advance",
                table: "job_order_history",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_order_history_ResolvedRoleAssignmentId",
                schema: "advance",
                table: "job_order_history",
                column: "ResolvedRoleAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_customer_purchase_order_lines_CustomerPurchaseOr~",
                schema: "advance",
                table: "job_orders",
                column: "CustomerPurchaseOrderLineId",
                principalSchema: "advance",
                principalTable: "customer_purchase_order_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_customer_purchase_orders_CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders",
                column: "CustomerPurchaseOrderId",
                principalSchema: "advance",
                principalTable: "customer_purchase_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_employee_role_assignments_AccountsConfirmationRo~",
                schema: "advance",
                table: "job_orders",
                column: "AccountsConfirmationRoleAssignmentId",
                principalSchema: "advance",
                principalTable: "employee_role_assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_employee_role_assignments_InitiatedRoleAssignmen~",
                schema: "advance",
                table: "job_orders",
                column: "InitiatedRoleAssignmentId",
                principalSchema: "advance",
                principalTable: "employee_role_assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_employees_AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "AccountsConfirmedByEmployeeId",
                principalSchema: "advance",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_job_orders_employees_InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders",
                column: "InitiatedByEmployeeId",
                principalSchema: "advance",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(JobOrderGovernanceSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(JobOrderGovernanceSql.Down);
            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_customer_purchase_order_lines_CustomerPurchaseOr~",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_customer_purchase_orders_CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_employee_role_assignments_AccountsConfirmationRo~",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_employee_role_assignments_InitiatedRoleAssignmen~",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_employees_AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_job_orders_employees_InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropTable(
                name: "job_order_history",
                schema: "advance");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_AccountsConfirmationRoleAssignmentId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_CompanyId_ConfirmationIdempotencyKey",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_CompanyId_CustomerPurchaseOrderLineId_MachineOrd~",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropIndex(
                name: "IX_job_orders_InitiatedRoleAssignmentId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_job_order_joint_governance",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmationActorRoleCode",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmationReason",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmationRoleAssignmentId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmationRoleAssignmentType",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmedAt",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "AccountsConfirmedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "ConfirmationIdempotencyKey",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "ConfirmationRequestFingerprint",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "CustomerPurchaseOrderId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "CustomerPurchaseOrderLineId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "InitiatedActorRoleCode",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "InitiatedByEmployeeId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "InitiatedRoleAssignmentId",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "InitiatedRoleAssignmentType",
                schema: "advance",
                table: "job_orders");

            migrationBuilder.DropColumn(
                name: "MachineOrdinal",
                schema: "advance",
                table: "job_orders");
        }
    }
}
