using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string VendorBillTarget = "20260908082923_VendorBillAndAcceptedBillCosting";

    [Fact]
    public void Vendor_bill_costing_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, VendorBillTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("vendor-bill-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("vendor-bill-up.sql", migrator.GenerateScript(predecessor, VendorBillTarget) + VendorBillAssertions);
        server.Execute("vendor-bill-down.sql", migrator.GenerateScript(VendorBillTarget, predecessor));
        server.Execute("vendor-bill-reapply.sql", migrator.GenerateScript(predecessor, VendorBillTarget) + VendorBillAssertions);
    }

    private const string VendorBillAssertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.vendor_bills') IS NULL
             OR to_regclass('advance.vendor_bill_lines') IS NULL
             OR to_regclass('advance.vendor_bill_history') IS NULL
             OR to_regclass('advance.vendor_bill_cost_allocations') IS NULL
             OR to_regclass('advance.fifo_inventory_cost_layers') IS NULL
             OR to_regclass('advance.fifo_cost_consumptions') IS NULL THEN
            RAISE EXCEPTION 'Vendor Bill/FIFO tables are missing.';
          END IF;
          IF to_regprocedure('advance.create_vendor_bill(uuid,uuid,text,date,jsonb,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.decide_vendor_bill(uuid,uuid,bigint,boolean,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.reverse_vendor_bill(uuid,uuid,bigint,text,text,text,text,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.create_fifo_layers_for_grn(uuid,uuid,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.consume_fifo_for_issue(uuid,uuid,uuid,text,uuid,text,text)') IS NULL THEN
            RAISE EXCEPTION 'Controlled Vendor Bill/FIFO functions are missing.';
          END IF;
          IF (SELECT count(*) FROM advance.page_definitions WHERE "PageKey"='accounts.vendor-bills')<>1 THEN
            RAISE EXCEPTION 'Expected one Vendor Bill page.';
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='VendorBillAndAcceptedBillCosting')<>2 THEN
            RAISE EXCEPTION 'Expected two Vendor Bill grants.';
          END IF;
        END $assert$;
        """;
}