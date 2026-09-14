using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task SupplierInvoiceMigrationKeepsEvidencePrivateAndSupportsEmptyRollback()
    {
        const string target="20260914090000_SupplierInvoiceReceipts";
        using var model=new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator=model.GetService<IMigrator>();
        var migrations=model.Database.GetMigrations().ToArray();
        var predecessor=migrations[Array.IndexOf(migrations,target)-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("supplier-invoice-predecessor.sql",migrator.GenerateScript("0",predecessor));
        using var environment=new OrdinaryPrincipalEnvironment(server.ConnectionString,"supplier-invoice-runtime-123456789");
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("supplier-invoice-up.sql",migrator.GenerateScript(predecessor,target));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
        server.Execute("supplier-invoice-private.sql","""
            DO $assert$ BEGIN
             IF has_table_privilege('nexa_erp_runtime','advance.supplier_invoices','SELECT')
              OR has_table_privilege('nexa_erp_runtime','advance.supplier_invoice_receipt_matches','INSERT')
              OR has_function_privilege('nexa_erp_runtime','advance.reconcile_supplier_invoice_receipts(uuid,uuid,uuid)','EXECUTE')
              OR has_function_privilege('nexa_erp_runtime','advance.supplier_invoice_command_valid(uuid,uuid,text,uuid,text,text,text)','EXECUTE')
              OR NOT has_function_privilege('nexa_erp_runtime','advance.get_supplier_invoice(uuid,uuid)','EXECUTE')
              OR NOT EXISTS(SELECT 1 FROM pg_trigger WHERE tgrelid='advance.goods_receipts'::regclass
               AND tgname='trg_supplier_invoice_receipt_match' AND tgenabled='O') THEN
              RAISE EXCEPTION 'Supplier invoice private authority is invalid.';
             END IF;
            END $assert$;
            """);
        server.Execute("supplier-invoice-down.sql",migrator.GenerateScript(target,predecessor));
        server.Execute("supplier-invoice-reapply.sql",migrator.GenerateScript(predecessor,target));
        server.Execute("fifo-receipt-eligibility-up.sql",migrator.GenerateScript(target,"20260914100000_FifoReversedReceiptEligibility"));
        server.Execute("fifo-receipt-eligibility-down.sql",migrator.GenerateScript("20260914100000_FifoReversedReceiptEligibility",target));
        Assert.Equal(0,await DatabasePrincipalCommand.RunAsync(["database-principals","provision"]));
    }
}
