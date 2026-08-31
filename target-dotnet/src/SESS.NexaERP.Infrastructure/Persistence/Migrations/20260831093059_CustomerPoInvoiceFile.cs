using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPoInvoiceFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceFileId",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceFileName",
                schema: "advance",
                table: "customer_purchase_orders",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceFileId",
                schema: "advance",
                table: "customer_purchase_orders");

            migrationBuilder.DropColumn(
                name: "InvoiceFileName",
                schema: "advance",
                table: "customer_purchase_orders");
        }
    }
}
