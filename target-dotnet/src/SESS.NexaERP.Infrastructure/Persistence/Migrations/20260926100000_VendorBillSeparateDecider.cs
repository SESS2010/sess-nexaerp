using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

/// <summary>
/// R3 (approved 26 September 2026): the employee who entered a Vendor Bill cannot also accept or
/// reject it. <c>decide_vendor_bill</c> checked only the Accounts Manager role, so one Accounts
/// Manager could create and accept the same bill. A narrow trigger enforces the separation at the
/// only transition that decides a bill (DRAFT to ACCEPTED or REJECTED), leaving the decision
/// function itself unchanged.
/// </summary>
[DbContext(typeof(NexaErpDbContext))]
[Migration("20260926100000_VendorBillSeparateDecider")]
public sealed class VendorBillSeparateDecider : Migration
{
    internal const string Refusal = "The employee who entered this Vendor Bill cannot also accept or reject it. Another Accounts Manager must decide.";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql($$"""
            CREATE FUNCTION advance.guard_vendor_bill_separate_decider() RETURNS trigger
            LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $guard$
            BEGIN
              IF NEW."DecidedByEmployeeId" IS NOT DISTINCT FROM OLD."CreatedByEmployeeId" THEN
                RAISE EXCEPTION '{{Refusal}}';
              END IF;
              RETURN NEW;
            END $guard$;
            CREATE TRIGGER trg_vendor_bills_separate_decider
              BEFORE UPDATE OF "Status" ON advance.vendor_bills
              FOR EACH ROW
              WHEN (OLD."Status"='DRAFT' AND NEW."Status" IN ('ACCEPTED','REJECTED'))
              EXECUTE FUNCTION advance.guard_vendor_bill_separate_decider();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_vendor_bills_separate_decider ON advance.vendor_bills;
            DROP FUNCTION IF EXISTS advance.guard_vendor_bill_separate_decider();
            """);
    }
}
