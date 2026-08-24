using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Rev867C1Corrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalOrganizationId",
                schema: "nexa",
                table: "vendors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PortalOrganizationId",
                schema: "nexa",
                table: "vendors",
                column: "PortalOrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendors_PortalOrganizationId",
                schema: "nexa",
                table: "vendors");

            migrationBuilder.DropColumn(
                name: "PortalOrganizationId",
                schema: "nexa",
                table: "vendors");
        }
    }
}
