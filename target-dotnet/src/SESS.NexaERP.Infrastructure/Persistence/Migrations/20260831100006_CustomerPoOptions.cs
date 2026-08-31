using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPoOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_po_options",
                schema: "advance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Value = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_po_options", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "customer_po_options",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "IsActive", "Kind", "UpdatedAt", "UpdatedBy", "Value", "Version" },
                values: new object[,]
                {
                    { new Guid("44000000-0000-0000-0002-000000000001"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SERVICE_MODE", null, null, "NON AMC", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000002"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SERVICE_MODE", null, null, "Under AMC", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000003"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SERVICE_MODE", null, null, "Dispatch Machine", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000004"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "Spares", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000005"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "Service Charges", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000006"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "Machine", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000007"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "AMC Charges", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000008"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "Spares & Service", 0L },
                    { new Guid("44000000-0000-0000-0002-000000000009"), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", true, "SALES_TYPE", null, null, "Calibration Charges", 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_po_options_Kind_Value",
                schema: "advance",
                table: "customer_po_options",
                columns: new[] { "Kind", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_po_options",
                schema: "advance");
        }
    }
}
