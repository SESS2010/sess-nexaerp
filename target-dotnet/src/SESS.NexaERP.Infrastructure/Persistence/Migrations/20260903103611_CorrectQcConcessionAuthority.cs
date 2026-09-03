using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrectQcConcessionAuthority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                column: "CanCancel",
                value: false);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7fa66608-1650-7481-0d97-33b93ff14201"),
                column: "CanApprove",
                value: true);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"),
                columns: new[] { "CanApprove", "CanCancel" },
                values: new object[] { false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                column: "CanCancel",
                value: true);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7fa66608-1650-7481-0d97-33b93ff14201"),
                column: "CanApprove",
                value: false);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("8c9ccb5e-d2ee-b5c2-70b3-26f0805ab6d3"),
                columns: new[] { "CanApprove", "CanCancel" },
                values: new object[] { true, true });
        }
    }
}
