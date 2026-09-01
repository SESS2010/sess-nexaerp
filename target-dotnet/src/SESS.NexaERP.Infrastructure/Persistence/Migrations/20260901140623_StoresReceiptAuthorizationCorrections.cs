using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoresReceiptAuthorizationCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0df3abd0-c90c-3d36-91bb-68b49e0f2605"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2d5616c6-3914-444d-5b0a-4d6267c96956"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("39e6443c-1cab-ba55-d870-4b7e9c6cb059"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7051007b-907a-2932-86e1-c51029df6df8"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a8dbe752-78d7-fc8d-38b7-3661c16754ac"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c211bad6-b7c8-1d79-1f8a-633a2eee8cce"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d839158c-cc1e-3121-ce72-eabcb8bea70a"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f494380c-48f3-5f83-45cc-3a15c9cc28dd"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("0df3abd0-c90c-3d36-91bb-68b49e0f2605"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("2d5616c6-3914-444d-5b0a-4d6267c96956"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("39e6443c-1cab-ba55-d870-4b7e9c6cb059"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("7051007b-907a-2932-86e1-c51029df6df8"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("a8dbe752-78d7-fc8d-38b7-3661c16754ac"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c211bad6-b7c8-1d79-1f8a-633a2eee8cce"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("d839158c-cc1e-3121-ce72-eabcb8bea70a"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("f494380c-48f3-5f83-45cc-3a15c9cc28dd"),
                columns: new[] { "CanCancel", "CanCreate", "CanReplaceAttachment", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true, true });
        }
    }
}
