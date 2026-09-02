using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Foundation2InventoryOwnershipCustodyTests
{
    private static readonly string[] Tables =
    [
        "inventory_external_parties",
        "inventory_account_holders",
        "inventory_ownership_accounts",
        "inventory_custody_accounts",
        "inventory_custody_cases",
        "inventory_custody_case_lines",
        "inventory_custody_case_gate_entry_links",
        "inventory_custody_case_goods_receipt_links",
        "inventory_custody_case_delivery_challan_links",
        "inventory_custody_case_purchase_order_links",
        "inventory_custody_case_customer_purchase_order_links",
        "inventory_custody_case_job_order_links",
        "inventory_custody_assignments",
        "inventory_custody_handoffs",
        "inventory_custody_handoff_lines",
        "inventory_ownership_transfers",
        "inventory_ownership_transfer_lines",
        "inventory_memo_liability_events"
    ];

    [Fact]
    public void ModelContainsTheCompleteFoundation2SetWithoutChangingStockMovements()
    {
        using var db = Context();
        var model = db.GetService<IDesignTimeModel>().Model;
        foreach (var table in Tables)
            Assert.Contains(model.GetEntityTypes(), entity => entity.GetTableName() == table);

        var movement = model.FindEntityType(typeof(StockMovement))!;
        Assert.Null(movement.FindProperty("OwnershipAccountId"));
        Assert.Null(movement.FindProperty("CustodyAssignmentId"));
        Assert.Null(movement.FindProperty("InventoryProvenanceLayerId"));
        Assert.Null(movement.FindProperty("CustodyCaseLineId"));
    }

    [Fact]
    public void MigrationGuardsBothDirectionsAndCreatesOnlyFoundation2Structures()
    {
        var root = FindRoot();
        var migration = File.ReadAllText(Directory.GetFiles(
            Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations"),
            "*Foundation2InventoryOwnershipAndCustody.cs").Single());
        var sql = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure",
            "Persistence", "Migrations", "Foundation2InventoryOwnershipCustodySql.cs"));

        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.DoesNotContain("stock_movements", migration, StringComparison.Ordinal);
        foreach (var table in Tables)
            Assert.Contains($"name: \"{table}\"", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE OR REPLACE VIEW advance.inventory_custody_case_source_links", sql);
        Assert.Contains("append-only; record a REVERSAL event instead", sql);
        Assert.Contains("LOAN_CLOSED_AGAINST_PO_GRN", migration);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void MigrationClusterGuardRejectsNonPostgreSqlInBothDirections(string methodName)
    {
        var migration = new SESS.NexaERP.Infrastructure.Persistence.Migrations.Foundation2InventoryOwnershipAndCustody();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(
            () => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    [Fact]
    public void CustomerPropertyCasesFailClosedAndBuybackRequiresEvidence()
    {
        var root = FindRoot();
        var mapping = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure",
            "Persistence", "NexaErpDbContext.InventoryOwnershipCustody.cs"));

        Assert.Contains("RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION", mapping);
        Assert.Contains("CK_inventory_custody_cases_other_brand_chargeable", mapping);
        Assert.Contains("CK_inventory_custody_cases_work_authorization", mapping);
        Assert.Contains("CK_inventory_ownership_transfers_buyback", mapping);
        Assert.Contains("AgreementReference", mapping);
        Assert.Equal("CUSTOMER_OTHER_BRAND_MODIFICATION", InventoryCustodyCaseTypes.CustomerOtherBrandModification);
        Assert.Equal("CUSTOMER_SESS_MACHINE_WARRANTY", InventoryCustodyCaseTypes.CustomerSessMachineWarranty);
        Assert.Equal("CUSTOMER_SESS_SPARE_WARRANTY", InventoryCustodyCaseTypes.CustomerSessSpareWarranty);
    }

    [Fact]
    public void TrialDataHasOneDedicatedCustomerPropertyRackPerCompanyAndScannerFirstGuidance()
    {
        var root = FindRoot();
        var trial = File.ReadAllText(Path.Combine(root, "database", "postgresql", "trial-master-data-apply.sql"));
        var design = File.ReadAllText(Path.Combine(root, "outputs", "first_stores_module_schema_design.md"));

        Assert.Contains("ARRAY[6,6,4,5,15,20,2,24,26,12,0]", trial);
        Assert.Contains("'TRIAL-C01-CUSTOMER-PROPERTY'", trial);
        Assert.Contains("'TRIAL-C02-CUSTOMER-PROPERTY'", trial);
        Assert.Contains("'CUSTOMER_PROPERTY','CUSTOMER_PROPERTY'", trial);
        Assert.Contains("scanner's Enter or Tab terminator", design);
        Assert.Contains("Manual typing remains an accessible fallback", design);
    }

    private static NexaErpDbContext Context() =>
        new(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private static int Count(string text, string value)
    {
        var count = 0;
        for (var offset = 0; (offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0; offset += value.Length)
            count++;
        return count;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }
}
