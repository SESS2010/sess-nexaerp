using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanyRelationshipExternalCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.AddColumn<string>(
                name: "VendorAssignedCustomerCode",
                schema: "advance",
                table: "vendor_company_relationships",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAssignedSupplierCode",
                schema: "advance",
                table: "customer_company_relationships",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_company_relationships_VendorAssignedCustomerCode",
                schema: "advance",
                table: "vendor_company_relationships",
                column: "VendorAssignedCustomerCode",
                filter: "\"VendorAssignedCustomerCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_company_relationships_CustomerAssignedSupplierCode",
                schema: "advance",
                table: "customer_company_relationships",
                column: "CustomerAssignedSupplierCode",
                filter: "\"CustomerAssignedSupplierCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_vendor_company_relationships_VendorAssignedCustomerCode",
                schema: "advance",
                table: "vendor_company_relationships");

            migrationBuilder.DropIndex(
                name: "IX_customer_company_relationships_CustomerAssignedSupplierCode",
                schema: "advance",
                table: "customer_company_relationships");

            migrationBuilder.DropColumn(
                name: "VendorAssignedCustomerCode",
                schema: "advance",
                table: "vendor_company_relationships");

            migrationBuilder.DropColumn(
                name: "CustomerAssignedSupplierCode",
                schema: "advance",
                table: "customer_company_relationships");
        }
    }
}
