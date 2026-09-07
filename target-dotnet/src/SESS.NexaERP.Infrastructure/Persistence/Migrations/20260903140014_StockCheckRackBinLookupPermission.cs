using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StockCheckRackBinLookupPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c883528c-d9d8-8aff-6f8c-5f7db5965311"),
                column: "CanView",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c883528c-d9d8-8aff-6f8c-5f7db5965311"),
                column: "CanView",
                value: false);
        }
    }
}
