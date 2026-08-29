using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StrictPanCaseInsensitiveUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendors_PanNumber_LegalVendorName",
                schema: "advance",
                table: "vendors");

            migrationBuilder.DropIndex(
                name: "IX_customers_PanNumber_LegalCustomerName",
                schema: "advance",
                table: "customers");

            // Deactivate rows that violate case-insensitive (PAN, name)
            // uniqueness before creating the stricter index: within each
            // duplicate group only the lowest business code stays active.
            // (Known instance: SESS-V-0028, the Fluidoze re-entry whose GSTIN
            // carries an I/1 typo.)
            migrationBuilder.Sql("""
                UPDATE advance.vendors v SET "IsActive" = false, "VendorStatus" = 'Inactive',
                       "UpdatedAt" = now(), "UpdatedBy" = 'STRICT_PAN_UNIQUENESS_MIGRATION'
                WHERE v."PanNumber" IS NOT NULL AND v."IsActive" AND EXISTS (
                    SELECT 1 FROM advance.vendors keep
                    WHERE keep."PanNumber" = v."PanNumber"
                      AND upper(keep."LegalVendorName") = upper(v."LegalVendorName")
                      AND keep."IsActive" AND keep."VendorCode" < v."VendorCode");
                """);
            migrationBuilder.Sql("""
                UPDATE advance.customers c SET "IsActive" = false, "Status" = 'Inactive',
                       "UpdatedAt" = now(), "UpdatedBy" = 'STRICT_PAN_UNIQUENESS_MIGRATION'
                WHERE c."PanNumber" IS NOT NULL AND c."IsActive" AND EXISTS (
                    SELECT 1 FROM advance.customers keep
                    WHERE keep."PanNumber" = c."PanNumber"
                      AND upper(keep."LegalCustomerName") = upper(c."LegalCustomerName")
                      AND keep."IsActive" AND keep."CustomerCode" < c."CustomerCode");
                """);

            // Case-insensitive uniqueness among active rows: one PAN may cover
            // multiple state branches only when their legal names differ.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_vendors_Pan_LegalNameUpper"
                ON advance.vendors ("PanNumber", upper("LegalVendorName"))
                WHERE "PanNumber" IS NOT NULL AND "IsActive";
                """);
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_customers_Pan_LegalNameUpper"
                ON advance.customers ("PanNumber", upper("LegalCustomerName"))
                WHERE "PanNumber" IS NOT NULL AND "IsActive";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS advance."UX_vendors_Pan_LegalNameUpper";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS advance."UX_customers_Pan_LegalNameUpper";""");

            migrationBuilder.CreateIndex(
                name: "IX_vendors_PanNumber_LegalVendorName",
                schema: "advance",
                table: "vendors",
                columns: new[] { "PanNumber", "LegalVendorName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_PanNumber_LegalCustomerName",
                schema: "advance",
                table: "customers",
                columns: new[] { "PanNumber", "LegalCustomerName" },
                unique: true,
                filter: "\"PanNumber\" IS NOT NULL");
        }
    }
}
