using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class FirstStoresPart3BMigrationTests
{
    private static NexaErpDbContext CreateContext() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
    private static string Sql(string property)
    {
        var type=typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.FirstStoresPart3BSql",true)!;
        return (string)type.GetProperty(property,BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
    }

    [Fact]
    public void Part3BDependenciesAndCoreLedgerMappingsRemainIntact()
    {
        using var db=CreateContext();
        var migrations=db.Database.GetMigrations().ToArray();
        Assert.True(Array.IndexOf(migrations,"20260827132947_FirstStoresPart3BLedgerActivation")
            < Array.IndexOf(migrations,"20260828121759_ItemReferenceMasterFrontendReadiness"));
        Assert.True(Array.IndexOf(migrations,"20260829114544_ControlledTaxGstWorkflow")
            < Array.IndexOf(migrations,"20260831052559_StoresSlice0ControlledPostingAndGateApi"));
        var batch=db.Model.FindEntityType(typeof(StockPostingBatch));
        var movement=db.Model.FindEntityType(typeof(StockMovement));
        Assert.NotNull(batch);
        Assert.NotNull(movement);
        Assert.Equal("advance",batch.GetSchema());
        Assert.Equal("stock_posting_batches",batch.GetTableName());
        Assert.Equal("advance",movement.GetSchema());
        Assert.Equal("stock_movements",movement.GetTableName());
        Assert.NotNull(movement.FindProperty(nameof(StockMovement.StockPostingBatchId)));
        Assert.NotNull(movement.FindProperty(nameof(StockMovement.WarehouseConditionLocationId)));
        Assert.NotNull(movement.FindProperty(nameof(StockMovement.ReversesStockMovementId)));
    }

    [Fact]
    public void LedgerModelCarriesTheCompleteVersionTwoContract()
    {
        using var db=CreateContext(); var entity=db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(StockMovement))!;
        Assert.Equal(24,entity.FindProperty(nameof(StockMovement.QuantityIn))!.GetPrecision());
        Assert.Equal(6,entity.FindProperty(nameof(StockMovement.QuantityIn))!.GetScale());
        foreach(var name in new[]{nameof(StockMovement.StockPostingBatchId),nameof(StockMovement.WarehouseConditionLocationId),nameof(StockMovement.GoodsReceiptLineId),nameof(StockMovement.QcInspectionRevisionId),nameof(StockMovement.MaterialIssueRequestLineId),nameof(StockMovement.DeliveryChallanLineId),nameof(StockMovement.OriginGoodsReceiptLineId),nameof(StockMovement.InventorySerialId),nameof(StockMovement.ReversesStockMovementId)})
            Assert.True(entity.FindProperty(name)!.IsNullable);
    }

    [Fact]
    public void UpGuardsLegacyRowsAndActivatesOnlyAfterAtomicGuards()
    {
        var pre=Sql("PreUp"); var up=Sql("Up");
        Assert.Contains("requires PostgreSQL 17",pre);
        Assert.Contains("stock_movements IN ACCESS EXCLUSIVE MODE",pre);
        Assert.Contains("must preserve every pre-existing movement as version 1",up);
        Assert.Contains("must not invent legacy provenance",up);
        Assert.Contains("DEFERRABLE INITIALLY DEFERRED",up);
        Assert.Contains("must contain movements",up);
    }

    [Fact]
    public void LedgerIsTypedAppendOnlyIdempotentAndExactlyReversible()
    {
        var up=Sql("Up");
        Assert.Contains("New stock movements must use ledger schema version 2",up);
        Assert.Contains("Stock movements are append-only; post a reversal",up);
        Assert.Contains("different fingerprint",up);
        Assert.Contains("exactly negate one target movement",up);
        Assert.Contains("Serialized movement origin does not match receipt provenance",up);
    }

    [Fact]
    public void PostingKindsHaveSourceSpecificReconciliation()
    {
        var up=Sql("Up");
        foreach(var text in new[]{"GRN custody batch does not reconcile","QC disposition batch must be a balanced","Material issue batch exceeds or bypasses","Delivery Challan posting does not reconcile","Reversal batch must negate every target movement"}) Assert.Contains(text,up);
    }

    [Fact]
    public void DownIsClusterGuardedAndFailsClosedBeforeRestoringBlockers()
    {
        var down=Sql("Down");
        Assert.Contains("down requires PostgreSQL 17",down);
        Assert.Contains("rollback refuses any posting batch",down);
        Assert.Contains("rollback refuses any version-2 movement",down);
        Assert.Contains("rollback refuses any dependent document",down);
        Assert.Contains("cannot safely narrow retained legacy quantities",down);
        Assert.Contains("TR_qc_revision_part3a_block",down);
    }
}
