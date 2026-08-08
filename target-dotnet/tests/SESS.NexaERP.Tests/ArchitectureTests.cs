using SESS.NexaERP.Domain;

namespace SESS.NexaERP.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Module_boundaries_include_purchase_and_stores_inventory()
    {
        Assert.Contains("Purchase", Modules.Boundaries);
        Assert.Contains("StoresInventory", Modules.Boundaries);
    }

    [Fact]
    public void Purchase_stores_workflow_keeps_grn_qc_and_stock_ledger_order()
    {
        var stages = Enum.GetNames<PurchaseStoresStage>().ToList();

        Assert.True(stages.IndexOf("PurchaseOrder") < stages.IndexOf("GateEntry"));
        Assert.True(stages.IndexOf("GateEntry") < stages.IndexOf("Grn"));
        Assert.True(stages.IndexOf("Grn") < stages.IndexOf("QcVerification"));
        Assert.True(stages.IndexOf("QcVerification") < stages.IndexOf("InventoryUpdate"));
        Assert.True(stages.IndexOf("InventoryUpdate") < stages.IndexOf("StockLedger"));
    }
}
