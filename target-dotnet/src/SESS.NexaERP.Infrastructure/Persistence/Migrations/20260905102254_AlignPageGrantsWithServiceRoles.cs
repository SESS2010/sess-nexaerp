using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPageGrantsWithServiceRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4be63323-a734-943b-8d03-b7d80fd58683"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("90b24916-a7da-926c-85db-d40df0bb5cb5"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "HasFullControl" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("baff3f8c-6e8c-e814-86d6-9431df1251d1"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c80614d6-27dc-22a0-b1b9-0a9f5b1536ea"),
                columns: new[] { "CanVerify", "HasFullControl" },
                values: new object[] { false, false });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[] { new Guid("84000000-0000-0000-0000-000000000009"), false, false, true, false, true, false, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-role-service-permission-alignment", false, new Guid("40000000-0000-0000-0000-000000000005"), new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000009"));

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("4be63323-a734-943b-8d03-b7d80fd58683"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("90b24916-a7da-926c-85db-d40df0bb5cb5"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "HasFullControl" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("baff3f8c-6e8c-e814-86d6-9431df1251d1"),
                columns: new[] { "CanCancel", "CanCreate", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment" },
                values: new object[] { true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("c80614d6-27dc-22a0-b1b9-0a9f5b1536ea"),
                columns: new[] { "CanVerify", "HasFullControl" },
                values: new object[] { true, true });
        }
    }
}
