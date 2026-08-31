using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPoLinesAndDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountInWords",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstAmount",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstPercent",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTerms",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstAmount",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IgstPercent",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherReferences",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PoFileId",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoFileName",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundOff",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstAmount",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstPercent",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableValue",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_po_files",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_po_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_purchase_order_lines",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerPurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlNo = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    Uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_purchase_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_purchase_order_lines_customer_purchase_orders_Cust~",
                        column: x => x.CustomerPurchaseOrderId,
                        principalSchema: "advance",
                        principalTable: "customer_purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_purchase_order_lines_CustomerPurchaseOrderId_SlNo",
                schema: "advance",
                table: "customer_purchase_order_lines",
                columns: new[] { "CustomerPurchaseOrderId", "SlNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_po_files",
                schema: "advance");

            migrationBuilder.DropTable(
                name: "customer_purchase_order_lines",
                schema: "advance");

            migrationBuilder.DropColumn(
                name: "AmountInWords",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "CgstAmount",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "CgstPercent",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTerms",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "Destination",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "IgstAmount",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "IgstPercent",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "OtherReferences",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "PoFileId",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "PoFileName",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "RoundOff",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "SgstAmount",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "SgstPercent",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "TaxableValue",
                schema: "advance",
                table: "customer_purchase_orders");
        }
    }
}
