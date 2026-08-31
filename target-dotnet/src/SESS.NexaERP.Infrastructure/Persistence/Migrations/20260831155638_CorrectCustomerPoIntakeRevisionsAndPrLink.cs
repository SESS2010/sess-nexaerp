using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrectCustomerPoIntakeRevisionsAndPrLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                  IF EXISTS (SELECT 1 FROM advance.customer_purchase_orders WHERE "CustomerId" IS NULL OR "CompanyId" IS NULL) THEN
                    RAISE EXCEPTION 'Customer PO correction requires every existing intake row to have CustomerId and CompanyId; repair unmapped rows before retrying.';
                  END IF;
                  IF EXISTS (
                    SELECT 1 FROM advance.customer_purchase_orders
                    WHERE "InvoiceNumber" IS NOT NULL OR "InvoiceDate" IS NOT NULL OR "FinalInvoiceDate" IS NOT NULL
                       OR "InvoiceFileId" IS NOT NULL OR "InvoiceFileName" IS NOT NULL OR "PaymentStatus" IS NOT NULL
                  ) THEN
                    RAISE EXCEPTION 'Customer PO correction will not discard legacy Accounts data; archive and clear invoice/payment/file fields before retrying.';
                  END IF;
                END $guard$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_customer_purchase_order_lines_CustomerPurchaseOrderId_SlNo",
                schema: "advance",
                table: "customer_purchase_order_lines");

            migrationBuilder.DropColumn(name: "CustomerName", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "FinalInvoiceDate", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "InvoiceDate", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "InvoiceFileId", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "InvoiceFileName", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "InvoiceNumber", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "PaymentStatus", schema: "advance", table: "customer_purchase_orders");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerPurchaseOrderId", schema: "advance", table: "purchase_requisitions",
                type: "uuid", nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId", schema: "advance", table: "customer_purchase_orders",
                type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId", schema: "advance", table: "customer_purchase_orders",
                type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRevisionNumber", schema: "advance", table: "customer_purchase_orders",
                type: "integer", nullable: false, defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber", schema: "advance", table: "customer_purchase_order_lines",
                type: "integer", nullable: false, defaultValue: 1);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_purchase_orders_Id_CompanyId", schema: "advance",
                table: "customer_purchase_orders", columns: new[] { "Id", "CompanyId" });

            migrationBuilder.CreateTable(
                name: "customer_purchase_order_revisions",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_purchase_order_revisions", x => x.Id);
                    table.UniqueConstraint("AK_customer_purchase_order_revisions_CustomerPurchaseOrderId_R~", x => new { x.CustomerPurchaseOrderId, x.RevisionNumber });
                    table.CheckConstraint("CK_customer_purchase_order_revisions_number", "\"RevisionNumber\" >= 1");
                    table.ForeignKey(
                        name: "FK_customer_purchase_order_revisions_customer_purchase_orders_~",
                        column: x => x.CustomerPurchaseOrderId,
                        principalSchema: "advance", principalTable: "customer_purchase_orders", principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO advance.customer_purchase_order_revisions
                  ("Id","CustomerPurchaseOrderId","RevisionNumber","ChangeReason","SnapshotJson","CreatedAt","CreatedBy","Version")
                SELECT gen_random_uuid(), po."Id", 1, 'Migration baseline',
                       to_jsonb(po) || jsonb_build_object('Lines', COALESCE((
                         SELECT jsonb_agg(to_jsonb(line) ORDER BY line."SlNo")
                         FROM advance.customer_purchase_order_lines line
                         WHERE line."CustomerPurchaseOrderId" = po."Id" AND line."RevisionNumber" = 1
                       ), '[]'::jsonb)),
                       po."CreatedAt", po."CreatedBy", 0
                FROM advance.customer_purchase_orders po;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_CustomerPurchaseOrderId_CompanyId",
                schema: "advance", table: "purchase_requisitions",
                columns: new[] { "CustomerPurchaseOrderId", "CompanyId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_customer_purchase_orders_current_revision", schema: "advance",
                table: "customer_purchase_orders", sql: "\"CurrentRevisionNumber\" >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_order_lines_CustomerPurchaseOrderId_Revis~",
                schema: "advance", table: "customer_purchase_order_lines",
                columns: new[] { "CustomerPurchaseOrderId", "RevisionNumber", "SlNo" }, unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_purchase_order_lines_customer_purchase_order_revis~",
                schema: "advance", table: "customer_purchase_order_lines",
                columns: new[] { "CustomerPurchaseOrderId", "RevisionNumber" },
                principalSchema: "advance", principalTable: "customer_purchase_order_revisions",
                principalColumns: new[] { "CustomerPurchaseOrderId", "RevisionNumber" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_requisitions_customer_purchase_orders_CustomerPurc~",
                schema: "advance", table: "purchase_requisitions",
                columns: new[] { "CustomerPurchaseOrderId", "CompanyId" },
                principalSchema: "advance", principalTable: "customer_purchase_orders",
                principalColumns: new[] { "Id", "CompanyId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION advance.guard_customer_po_revision_immutable()
                RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $guard$
                BEGIN
                  RAISE EXCEPTION 'Customer PO revisions and revision lines are append-only; create a new revision.';
                END $guard$;
                CREATE TRIGGER customer_po_revisions_append_only
                  BEFORE UPDATE OR DELETE ON advance.customer_purchase_order_revisions
                  FOR EACH ROW EXECUTE FUNCTION advance.guard_customer_po_revision_immutable();
                CREATE TRIGGER customer_po_revision_lines_append_only
                  BEFORE UPDATE OR DELETE ON advance.customer_purchase_order_lines
                  FOR EACH ROW EXECUTE FUNCTION advance.guard_customer_po_revision_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS customer_po_revision_lines_append_only ON advance.customer_purchase_order_lines;
                DROP TRIGGER IF EXISTS customer_po_revisions_append_only ON advance.customer_purchase_order_revisions;
                DROP FUNCTION IF EXISTS advance.guard_customer_po_revision_immutable();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_customer_purchase_order_lines_customer_purchase_order_revis~",
                schema: "advance", table: "customer_purchase_order_lines");
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_requisitions_customer_purchase_orders_CustomerPurc~",
                schema: "advance", table: "purchase_requisitions");
            migrationBuilder.DropTable(name: "customer_purchase_order_revisions", schema: "advance");
            migrationBuilder.DropIndex(
                name: "IX_purchase_requisitions_CustomerPurchaseOrderId_CompanyId",
                schema: "advance", table: "purchase_requisitions");
            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_purchase_orders_Id_CompanyId",
                schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropCheckConstraint(
                name: "CK_customer_purchase_orders_current_revision",
                schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropIndex(
                name: "IX_customer_purchase_order_lines_CustomerPurchaseOrderId_Revis~",
                schema: "advance", table: "customer_purchase_order_lines");
            migrationBuilder.DropColumn(name: "CustomerPurchaseOrderId", schema: "advance", table: "purchase_requisitions");
            migrationBuilder.DropColumn(name: "CurrentRevisionNumber", schema: "advance", table: "customer_purchase_orders");
            migrationBuilder.DropColumn(name: "RevisionNumber", schema: "advance", table: "customer_purchase_order_lines");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId", schema: "advance", table: "customer_purchase_orders",
                type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");
            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId", schema: "advance", table: "customer_purchase_orders",
                type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");

            migrationBuilder.AddColumn<string>(name: "CustomerName", schema: "advance", table: "customer_purchase_orders", type: "character varying(300)", maxLength: 300, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateOnly>(name: "FinalInvoiceDate", schema: "advance", table: "customer_purchase_orders", type: "date", nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "InvoiceDate", schema: "advance", table: "customer_purchase_orders", type: "date", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "InvoiceFileId", schema: "advance", table: "customer_purchase_orders", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "InvoiceFileName", schema: "advance", table: "customer_purchase_orders", type: "character varying(260)", maxLength: 260, nullable: true);
            migrationBuilder.AddColumn<string>(name: "InvoiceNumber", schema: "advance", table: "customer_purchase_orders", type: "character varying(200)", maxLength: 200, nullable: true);
            migrationBuilder.AddColumn<string>(name: "PaymentStatus", schema: "advance", table: "customer_purchase_orders", type: "character varying(80)", maxLength: 80, nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_order_lines_CustomerPurchaseOrderId_SlNo",
                schema: "advance", table: "customer_purchase_order_lines",
                columns: new[] { "CustomerPurchaseOrderId", "SlNo" });
        }
    }
}