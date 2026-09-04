using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MultiCompanyPrAndStoresStockCheckCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_purchase_requisitions_PrNumber",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0d663ce6-3756-5828-aae7-321d6f53031d"),
                column: "CanView",
                value: true);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("325d5475-24b6-69b3-ed22-4e7e66199841"),
                columns: new[] { "CanDownload", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"),
                column: "HasFullControl",
                value: false);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_CompanyId_PrNumber",
                schema: "advance",
                table: "purchase_requisitions",
                columns: new[] { "CompanyId", "PrNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_purchase_requisitions_CompanyId_PrNumber",
                schema: "advance",
                table: "purchase_requisitions");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0d663ce6-3756-5828-aae7-321d6f53031d"),
                column: "CanView",
                value: false);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("325d5475-24b6-69b3-ed22-4e7e66199841"),
                columns: new[] { "CanDownload", "CanPrint", "CanVerify", "CanView" },
                values: new object[] { false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"),
                column: "HasFullControl",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requisitions_PrNumber",
                schema: "advance",
                table: "purchase_requisitions",
                column: "PrNumber",
                unique: true);
        }
    }
}
