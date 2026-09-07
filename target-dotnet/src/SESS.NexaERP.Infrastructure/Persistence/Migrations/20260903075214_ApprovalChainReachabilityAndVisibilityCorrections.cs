using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ApprovalChainReachabilityAndVisibilityCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.InsertData(
                schema: "advance",
                table: "role_page_permissions",
                columns: new[] { "Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version" },
                values: new object[,]
                {
                    { new Guid("84000000-0000-0000-0000-000000000007"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("83000000-0000-0000-0000-000000000001"), null, null, 0L },
                    { new Guid("84000000-0000-0000-0000-000000000008"), true, false, false, false, false, false, false, true, false, false, true, false, false, false, false, true, true, true, true, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "migration-approval-configuration-part2", false, new Guid("20000000-0000-0000-0000-000000000022"), new Guid("83000000-0000-0000-0000-000000000002"), null, null, 0L }
                });

            ApprovalChainReachabilityCorrectionsSql.ApplyUp(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            ApprovalChainReachabilityCorrectionsSql.ApplyDownGuard(migrationBuilder);

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "advance",
                table: "role_page_permissions",
                keyColumn: "Id",
                keyValue: new Guid("84000000-0000-0000-0000-000000000008"));
        }
    }
}
