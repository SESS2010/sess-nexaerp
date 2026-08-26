using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TwoLevelPurchaseApprovalEnginePart3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "purchase_requisitions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "purchase_requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "purchase_requisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_requisitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "purchase_orders",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "purchase_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_order_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalRoute",
                schema: "advance",
                table: "purchase_order_history",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_order_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_order_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_order_history",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_order_history",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_order_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalCycle",
                schema: "advance",
                table: "commercial_comparisons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "commercial_comparisons",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "commercial_comparisons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "commercial_comparisons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "commercial_comparisons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparison~2",
                schema: "advance",
                table: "purchase_transaction_approval_history",
                columns: new[] { "CommercialComparisonId", "ApprovalCycle", "StepNumber" },
                unique: true,
                filter: "\"Action\" = 'Approve' AND \"StepNumber\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_requisition_approval_progress",
                schema: "advance",
                table: "purchase_requisitions",
                sql: "\"ApprovalCycle\" >= 0 AND \"CompletedApprovalStepCount\" >= 0 AND \"CompletedApprovalStepCount\" <= \"RequiredApprovalStepCount\" AND (\"ApprovalCycle\" = 0 OR (\"RequiredApprovalStepCount\" > 0 AND \"CreatorEmployeeId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"ApprovalWorkflowSnapshotJson\" <> '{}'::jsonb))");

            TwoLevelPurchaseApprovalEnginePart3Sql.Up(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisition_approval_history_PurchaseRequisitionI~1",
                schema: "advance",
                table: "purchase_requisition_approval_history",
                columns: new[] { "PurchaseRequisitionId", "ApprovalCycle", "StepNumber" },
                unique: true,
                filter: "\"Action\" = 'Approve' AND \"StepNumber\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_order_approval_progress",
                schema: "advance",
                table: "purchase_orders",
                sql: "\"ApprovalCycle\" >= 0 AND \"CompletedApprovalStepCount\" >= 0 AND \"CompletedApprovalStepCount\" <= \"RequiredApprovalStepCount\" AND (\"ApprovalCycle\" = 0 OR (\"RequiredApprovalStepCount\" > 0 AND \"CreatorEmployeeId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"ApprovalWorkflowSnapshotJson\" <> '{}'::jsonb))");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_ApprovalCycle_StepNu~",
                schema: "advance",
                table: "purchase_order_history",
                columns: new[] { "PurchaseOrderId", "ApprovalCycle", "StepNumber" },
                unique: true,
                filter: "\"Action\" = 'Approve' AND \"StepNumber\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_comparison_approval_progress",
                schema: "advance",
                table: "commercial_comparisons",
                sql: "\"ApprovalCycle\" >= 0 AND \"CompletedApprovalStepCount\" >= 0 AND \"CompletedApprovalStepCount\" <= \"RequiredApprovalStepCount\" AND (\"ApprovalCycle\" = 0 OR (\"RequiredApprovalStepCount\" > 0 AND \"CreatorEmployeeId\" <> '00000000-0000-0000-0000-000000000000'::uuid AND \"ApprovalWorkflowSnapshotJson\" <> '{}'::jsonb))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            TwoLevelPurchaseApprovalEnginePart3Sql.Down(migrationBuilder);
            migrationBuilder.DropIndex(
                name: "IX_purchase_transaction_approval_history_CommercialComparison~2",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_requisition_approval_progress",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropIndex(
                name: "IX_purchase_requisition_approval_history_PurchaseRequisitionI~1",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_order_approval_progress",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_history_PurchaseOrderId_ApprovalCycle_StepNu~",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropCheckConstraint(
                name: "CK_comparison_approval_progress",
                schema: "advance",
                table: "commercial_comparisons");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_transaction_approval_history");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_requisition_approval_history");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "ApprovalRoute",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "ResolvedEmployeeId",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "ResolvedRoleCode",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "SnapshotIdentity",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                schema: "advance",
                table: "purchase_order_history");

            migrationBuilder.DropColumn(
                name: "ApprovalCycle",
                schema: "advance",
                table: "commercial_comparisons");

            migrationBuilder.DropColumn(
                name: "ApprovalWorkflowSnapshotJson",
                schema: "advance",
                table: "commercial_comparisons");

            migrationBuilder.DropColumn(
                name: "CompletedApprovalStepCount",
                schema: "advance",
                table: "commercial_comparisons");

            migrationBuilder.DropColumn(
                name: "CreatorEmployeeId",
                schema: "advance",
                table: "commercial_comparisons");

            migrationBuilder.DropColumn(
                name: "RequiredApprovalStepCount",
                schema: "advance",
                table: "commercial_comparisons");
        }
    }
}
