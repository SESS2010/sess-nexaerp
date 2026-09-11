using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string VendorAdvancePaymentTarget =
        "20260911180000_VendorAdvanceAndPaymentEvidence";

    [Fact]
    public void Vendor_advance_and_payment_apply_revert_and_reapply_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(
            new DbContextOptionsBuilder<NexaErpDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
                .Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, VendorAdvancePaymentTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("vendor-financial-predecessor.sql",
            migrator.GenerateScript("0", predecessor));
        server.Execute("vendor-financial-up.sql",
            migrator.GenerateScript(predecessor, VendorAdvancePaymentTarget) + VendorFinancialAssertions);
        server.Execute("vendor-financial-down.sql",
            migrator.GenerateScript(VendorAdvancePaymentTarget, predecessor));
        server.Execute("vendor-financial-reapply.sql",
            migrator.GenerateScript(predecessor, VendorAdvancePaymentTarget) + VendorFinancialAssertions);
    }

    private const string VendorFinancialAssertions = """
DO $assert$
BEGIN
 IF (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
     WHERE n.nspname='advance' AND c.relname IN(
      'vendor_advances','vendor_advance_reversals','vendor_advance_adjustments',
      'vendor_advance_adjustment_restorations','vendor_payments',
      'vendor_payment_allocations'))<>6 THEN
  RAISE EXCEPTION 'Expected all six vendor financial evidence tables.';
 END IF;
 IF to_regprocedure(
   'advance.record_vendor_advance(uuid,uuid,date,numeric,text,text,text,text,text,uuid,text,uuid,text,text)')
    IS NULL
  OR to_regprocedure(
   'advance.reverse_vendor_advance(uuid,uuid,text,text,text,uuid,text,uuid,text,text)')
    IS NULL
  OR to_regprocedure(
   'advance.record_vendor_payment(uuid,uuid,date,numeric,text,text,text,jsonb,text,text,uuid,text,uuid,text,text)')
    IS NULL
  OR to_regprocedure('advance.list_vendor_advance_purchase_orders(uuid,uuid)') IS NULL THEN
  RAISE EXCEPTION 'Controlled vendor financial mutation functions are missing.';
 END IF;
 IF (SELECT count(*) FROM advance.page_definitions
     WHERE "PageKey"='accounts.vendor-financial-evidence')<>1 THEN
  RAISE EXCEPTION 'Expected one vendor financial evidence page.';
 END IF;
 IF (SELECT count(*) FROM advance.role_page_permissions
     WHERE "CreatedBy"='VendorAdvanceAndPaymentEvidence')<>1 THEN
  RAISE EXCEPTION 'Expected exactly one Accounts Manager page grant.';
 END IF;
END $assert$;
""";
}