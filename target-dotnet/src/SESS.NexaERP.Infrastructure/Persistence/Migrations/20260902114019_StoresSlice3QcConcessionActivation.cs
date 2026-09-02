using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoresSlice3QcConcessionActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.PreUp);

            migrationBuilder.DropIndex(
                name: "IX_qc_inspections_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_qc_inspection_lot_dispositions_decision",
                schema: "advance",
                table: "qc_inspection_lot_dispositions");

            migrationBuilder.RenameColumn(
                name: "InspectionShortfallRejectedQuantity",
                schema: "advance",
                table: "qc_inspection_revisions",
                newName: "DiscrepancyPendingQuantity");

            migrationBuilder.AddColumn<Guid>(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "qc_inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcceptedProvenanceLayerId",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RejectedProvenanceLayerId",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_CompanyId_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "qc_inspections",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" },
                unique: true,
                filter: "\"GoodsReceiptLineLotAllocationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections",
                column: "GoodsReceiptLineId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_qc_inspection_lot_dispositions_decision",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                sql: "(\"Disposition\"='ACCEPTED' AND \"AcceptedQuantity\">0 AND \"RejectedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='REJECTED' AND \"RejectedQuantity\">0 AND \"AcceptedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='PARTIAL_ACCEPTED' AND \"AcceptedQuantity\">0 AND \"RejectedQuantity\">0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='DISCREPANCY_PENDING' AND \"DiscrepancyPendingQuantity\">0)");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocation_serials_AcceptedProvenanceL~",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                column: "AcceptedProvenanceLayerId",
                unique: true,
                filter: "\"AcceptedProvenanceLayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_AcceptedP~",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "AcceptedProvenanceLayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_RejectedP~",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "RejectedProvenanceLayerId" });

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_concession_allocation_serials_inventory_provenanc~",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "AcceptedProvenanceLayerId" },
                principalSchema: "advance",
                principalTable: "inventory_provenance_layers",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_inventory_concession_allocation_serials_inventory_provenan~1",
                schema: "advance",
                table: "inventory_concession_allocation_serials",
                columns: new[] { "CompanyId", "RejectedProvenanceLayerId" },
                principalSchema: "advance",
                principalTable: "inventory_provenance_layers",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_qc_inspections_goods_receipt_line_lot_allocations_CompanyId~",
                schema: "advance",
                table: "qc_inspections",
                columns: new[] { "CompanyId", "GoodsReceiptLineLotAllocationId" },
                principalSchema: "advance",
                principalTable: "goods_receipt_line_lot_allocations",
                principalColumns: new[] { "CompanyId", "Id" },
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.PostUp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.PreDown);
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.BeforeDown);

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_concession_allocation_serials_inventory_provenanc~",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropForeignKey(
                name: "FK_inventory_concession_allocation_serials_inventory_provenan~1",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropForeignKey(
                name: "FK_qc_inspections_goods_receipt_line_lot_allocations_CompanyId~",
                schema: "advance",
                table: "qc_inspections");

            migrationBuilder.DropIndex(
                name: "IX_qc_inspections_CompanyId_GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "qc_inspections");

            migrationBuilder.DropIndex(
                name: "IX_qc_inspections_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_qc_inspection_lot_dispositions_decision",
                schema: "advance",
                table: "qc_inspection_lot_dispositions");

            migrationBuilder.DropIndex(
                name: "IX_inventory_concession_allocation_serials_AcceptedProvenanceL~",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_AcceptedP~",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropIndex(
                name: "IX_inventory_concession_allocation_serials_CompanyId_RejectedP~",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropColumn(
                name: "GoodsReceiptLineLotAllocationId",
                schema: "advance",
                table: "qc_inspections");

            migrationBuilder.DropColumn(
                name: "AcceptedProvenanceLayerId",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.DropColumn(
                name: "RejectedProvenanceLayerId",
                schema: "advance",
                table: "inventory_concession_allocation_serials");

            migrationBuilder.RenameColumn(
                name: "DiscrepancyPendingQuantity",
                schema: "advance",
                table: "qc_inspection_revisions",
                newName: "InspectionShortfallRejectedQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_GoodsReceiptLineId",
                schema: "advance",
                table: "qc_inspections",
                column: "GoodsReceiptLineId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_qc_inspection_lot_dispositions_decision",
                schema: "advance",
                table: "qc_inspection_lot_dispositions",
                sql: "(\"Disposition\"='ACCEPTED' AND \"AcceptedQuantity\">0 AND \"RejectedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='REJECTED' AND \"RejectedQuantity\">0 AND \"AcceptedQuantity\"=0 AND \"DiscrepancyPendingQuantity\"=0) OR (\"Disposition\"='DISCREPANCY_PENDING' AND \"DiscrepancyPendingQuantity\">0 AND \"AcceptedQuantity\"=0 AND \"RejectedQuantity\"=0)");
            migrationBuilder.Sql(StoresSlice3QcConcessionSql.PostDown);
        }
    }
}
