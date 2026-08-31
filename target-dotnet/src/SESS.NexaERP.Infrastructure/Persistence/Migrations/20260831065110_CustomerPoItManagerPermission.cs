using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerPoItManagerPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000007"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "RoleId" },
                values: new object[] { true, true, true, true, true, true, new Guid("10000000-0000-0000-0000-000000000014") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000008"),
                columns: new[] { "CanExport", "CanViewCommercialValues", "RoleId" },
                values: new object[] { true, true, new Guid("10000000-0000-0000-0000-000000000003") });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("44000000-0000-0000-0001-000000000009"), false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, false, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-sales-customer-po", false, new Guid("44000000-0000-0000-0000-000000000001"), new Guid("10000000-0000-0000-0000-000000000019"), null, null, 0L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000009"));

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000008"),
                columns: new[] { "CanExport", "CanViewCommercialValues", "RoleId" },
                values: new object[] { false, false, new Guid("10000000-0000-0000-0000-000000000019") });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("44000000-0000-0000-0001-000000000007"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "RoleId" },
                values: new object[] { false, false, false, false, false, false, new Guid("10000000-0000-0000-0000-000000000003") });
        }
    }
}
