using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class FirstStoresPart2MigrationTests
{
    private static NexaErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        return new NexaErpDbContext(options);
    }

    [Fact]
    public void Part2ContainsExactlyTheFourApprovedTables()
    {
        using var db = CreateContext();
        Type[] types =
        [
            typeof(GoodsReceipt), typeof(GoodsReceiptLine),
            typeof(InventorySerial), typeof(GoodsReceiptLineSerial)
        ];
        var actual = types.Select(type => db.Model.FindEntityType(type)!.GetTableName())
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        string[] expected =
        [
            "goods_receipt_line_serials", "goods_receipt_lines",
            "goods_receipts", "inventory_serials"
        ];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Part2PrecedesPart3AAndPart3B()
    {
        using var db = CreateContext();
        var migrations = db.Database.GetMigrations().ToArray();
        Assert.True(Array.IndexOf(migrations, "20260827110550_FirstStoresPart2GrnAndSerials")
                    < Array.IndexOf(migrations, "20260827115729_FirstStoresPart3AQcOutboundDocuments"));
        Assert.True(Array.IndexOf(migrations, "20260827115729_FirstStoresPart3AQcOutboundDocuments")
                    < Array.IndexOf(migrations, "20260827132947_FirstStoresPart3BLedgerActivation"));
        var tables = db.Model.GetEntityTypes().Select(x => x.GetTableName()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("qc_inspections", tables);
        Assert.Contains("delivery_challans", tables);
        Assert.Contains("stock_posting_batches", tables);
    }

    [Fact]
    public void GrnLineUsesSettledQuantityPrecisionAndWarrantyAndSerialSnapshots()
    {
        using var db = CreateContext();
        var line = db.Model.FindEntityType(typeof(GoodsReceiptLine))!;
        foreach (var property in new[]
        {
            nameof(GoodsReceiptLine.PoOrderedQuantitySnapshot),
            nameof(GoodsReceiptLine.PriorEffectiveReceivedQuantitySnapshot),
            nameof(GoodsReceiptLine.RemainingPoQuantitySnapshot),
            nameof(GoodsReceiptLine.DeliveredQuantitySnapshot),
            nameof(GoodsReceiptLine.ReceivedQuantity),
            nameof(GoodsReceiptLine.ExcessRejectedQuantity),
            nameof(GoodsReceiptLine.LineValueSnapshot),
            nameof(GoodsReceiptLine.UnitRateSnapshot),
            nameof(GoodsReceiptLine.SerialThresholdValueSnapshot)
        })
        {
            Assert.Equal(24, line.FindProperty(property)!.GetPrecision());
            Assert.Equal(6, line.FindProperty(property)!.GetScale());
        }
        Assert.NotNull(line.FindProperty(nameof(GoodsReceiptLine.BillWarrantyLimitDate)));
        Assert.NotNull(line.FindProperty(nameof(GoodsReceiptLine.InitialWarrantyExpiryDate)));
        Assert.NotNull(line.FindProperty(nameof(GoodsReceiptLine.SerialThresholdConfigVersionId)));
    }

    [Fact]
    public void StatusHistoryHasExactlyOneNullablePart2SourcePair()
    {
        using var db = CreateContext();
        var history = db.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(StoresDocumentStatusHistory))!;
        Assert.True(history.FindProperty(nameof(StoresDocumentStatusHistory.GateEntryId))!.IsNullable);
        Assert.True(history.FindProperty(nameof(StoresDocumentStatusHistory.GoodsReceiptId))!.IsNullable);
        Assert.Contains(history.GetCheckConstraints(), x => x.Name == "CK_stores_document_status_part2_source");
    }
}
