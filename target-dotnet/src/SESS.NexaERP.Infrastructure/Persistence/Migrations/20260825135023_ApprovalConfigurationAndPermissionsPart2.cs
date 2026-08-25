using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalConfigurationAndPermissionsPart2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("6ae6814b-2691-8ca4-9fcb-a280f5a0abaa"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e68927ed-7502-fd94-30dc-08fcdc435577"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("e818913d-251c-5c1c-8395-3ea116a3c0b2"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("fd4c02e7-4bcf-cf41-bc84-0c4025efae03"));

            migrationBuilder.AlterColumn<string>(
                name: "ApproverRoleCode",
                schema: "advance",
                table: "purchase_transaction_approval_policies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ApproverRoleCode",
                schema: "advance",
                table: "department_approval_mappings",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01a0c648-2bdc-3643-465f-d013309c37be"),
                columns: new[] { "CanCancel", "CanDownload", "CanExport", "CanPrint", "CanResubmit", "CanUploadAttachment", "CanViewAuditHistory" },
                values: new object[] { false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b5a0a82a-8bf9-abdb-c594-8185fa6de8d2"),
                columns: new[] { "CanApprove", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanRequestClarification", "CanRequestRevision", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanViewAuditHistory" },
                values: new object[] { false, false, false, false, false, false, false, false, false, false, false });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                columns: new[] { "CanReject", "CanRequestRevision" },
                values: new object[] { false, false });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("84000000-0000-0000-0000-000000000001"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000002"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000003"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000004"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000023"), new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000005"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("45ee1d9e-ac42-f959-ed98-df00b9c20bb0"), new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000006"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000012"), new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L }
                });

            migrationBuilder.Sql(ApprovalConfigurationAndPermissionsPart2Sql.Up);
            migrationBuilder.Sql(Rev869BControlledMutationSql.ApprovalConfigurationPart2Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(ApprovalConfigurationAndPermissionsPart2Sql.Down);
            migrationBuilder.Sql(Rev869BControlledMutationSql.ApprovalConfigurationPart2Down);
            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000006"));

            migrationBuilder.DropColumn(
                name: "ApproverRoleCode",
                schema: "advance",
                table: "department_approval_mappings");

            migrationBuilder.AlterColumn<string>(
                name: "ApproverRoleCode",
                schema: "advance",
                table: "purchase_transaction_approval_policies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("01a0c648-2bdc-3643-465f-d013309c37be"),
                columns: new[] { "CanCancel", "CanDownload", "CanExport", "CanPrint", "CanResubmit", "CanUploadAttachment", "CanViewAuditHistory" },
                values: new object[] { true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("b5a0a82a-8bf9-abdb-c594-8185fa6de8d2"),
                columns: new[] { "CanApprove", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanRequestClarification", "CanRequestRevision", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanViewAuditHistory" },
                values: new object[] { true, true, true, true, true, true, true, true, true, true, true });

            migrationBuilder.UpdateData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("451ff88f-816b-39fb-0097-18ecd1e752d2"),
                columns: new[] { "CanReject", "CanRequestRevision" },
                values: new object[] { true, true });

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("6ae6814b-2691-8ca4-9fcb-a280f5a0abaa"), false, true, true, false, true, true, true, false, false, false, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("21231666-baa1-d0fb-60c4-aff55813333f"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e68927ed-7502-fd94-30dc-08fcdc435577"), false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("2be06aa9-fb95-c4b6-1f26-c27f71cd58e9"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("e818913d-251c-5c1c-8395-3ea116a3c0b2"), false, false, false, false, true, true, true, false, false, true, false, false, false, false, false, false, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("e6c278a8-9f01-4a07-845a-1aba37ca0e46"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("fd4c02e7-4bcf-cf41-bc84-0c4025efae03"), false, true, true, false, true, true, true, false, false, true, false, false, true, true, true, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-rev869b", false, new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000001"), null, null, 0L }
                });
        }
    }
}
