using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260929143000_VendorImportSourceApproval")]
public sealed class VendorImportSourceApproval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            ALTER TABLE advance.vendors
              ADD COLUMN "LegacyApprovalStatus" varchar(8) NULL,
              ADD COLUMN "LegacyApprovedDate" date NULL,
              ADD CONSTRAINT ck_vendor_legacy_approval_status
                CHECK ("LegacyApprovalStatus" IS NULL OR "LegacyApprovalStatus" IN ('Approved','Pending'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            LOCK TABLE advance.vendors IN ACCESS EXCLUSIVE MODE;
            DO $guard$ BEGIN
              IF EXISTS (SELECT 1 FROM advance.vendors
                WHERE "LegacyApprovalStatus" IS NOT NULL OR "LegacyApprovedDate" IS NOT NULL) THEN
                RAISE EXCEPTION 'Vendor source approval rollback refuses to discard imported approval facts.';
              END IF;
            END $guard$;
            ALTER TABLE advance.vendors DROP CONSTRAINT ck_vendor_legacy_approval_status,
              DROP COLUMN "LegacyApprovalStatus", DROP COLUMN "LegacyApprovedDate";
            """);
    }
}
