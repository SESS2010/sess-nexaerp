using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed class StoresListSortingTests
{
    public static TheoryData<string,Func<GateEntry,string>> GateEntryKeys => new()
    {
        { "gateentrynumber", x => x.GateEntryNumber },
        { "purchaseordernumber", x => x.PurchaseOrder!.PoNumber },
        { "vendorname", x => x.VendorNameSnapshot },
        { "arrivedat", x => x.ArrivedAt.ToString("O") },
        { "status", x => x.Status }
    };

    public static TheoryData<string,Func<GoodsReceipt,string>> GoodsReceiptKeys => new()
    {
        { "grnnumber", x => x.GrnNumber },
        { "gateentrynumber", x => x.GateEntry!.GateEntryNumber },
        { "purchaseordernumber", x => x.PurchaseOrder!.PoNumber },
        { "vendorname", x => x.VendorNameSnapshot },
        { "vendorbilldate", x => x.VendorBillDate.ToString("O") },
        { "receivedat", x => x.ReceivedAt.ToString("O") },
        { "status", x => x.Status }
    };

    [Theory]
    [MemberData(nameof(GateEntryKeys))]
    public void Gate_entry_supports_each_client_sort_key_in_both_directions(string key,Func<GateEntry,string> value)
    {
        var rows=GateEntries();
        Assert.Equal(rows.Select(value).Order(),EfGateEntryService.Sort(rows.AsQueryable(),key,"asc").Select(value));
        Assert.Equal(rows.Select(value).OrderDescending(),EfGateEntryService.Sort(rows.AsQueryable(),key,"desc").Select(value));
    }

    [Theory]
    [MemberData(nameof(GoodsReceiptKeys))]
    public void Goods_receipt_supports_each_client_sort_key_in_both_directions(string key,Func<GoodsReceipt,string> value)
    {
        var rows=GoodsReceipts();
        Assert.Equal(rows.Select(value).Order(),EfGoodsReceiptService.Sort(rows.AsQueryable(),key,"asc").Select(value));
        Assert.Equal(rows.Select(value).OrderDescending(),EfGoodsReceiptService.Sort(rows.AsQueryable(),key,"desc").Select(value));
    }

    [Fact]
    public void Missing_or_unknown_sort_preserves_newest_first_default()
    {
        Assert.Equal(new[]{"GE-2","GE-1"},EfGateEntryService.Sort(GateEntries().AsQueryable(),null,null).Select(x=>x.GateEntryNumber));
        Assert.Equal(new[]{"GRN-2","GRN-1"},EfGoodsReceiptService.Sort(GoodsReceipts().AsQueryable(),"unknown","asc").Select(x=>x.GrnNumber));
    }

    private static GateEntry[] GateEntries()=>
    [
        new(){Id=Guid.Parse("00000000-0000-0000-0000-000000000002"),GateEntryNumber="GE-2",PurchaseOrder=new PurchaseOrder{PoNumber="PO-1"},VendorNameSnapshot="A Vendor",ArrivedAt=new DateTimeOffset(2026,2,1,0,0,0,TimeSpan.Zero),Status="DRAFT"},
        new(){Id=Guid.Parse("00000000-0000-0000-0000-000000000001"),GateEntryNumber="GE-1",PurchaseOrder=new PurchaseOrder{PoNumber="PO-2"},VendorNameSnapshot="B Vendor",ArrivedAt=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero),Status="FINALIZED"}
    ];

    private static GoodsReceipt[] GoodsReceipts()=>
    [
        new(){Id=Guid.Parse("00000000-0000-0000-0000-000000000002"),GrnNumber="GRN-2",GateEntry=new GateEntry{GateEntryNumber="GE-1"},PurchaseOrder=new PurchaseOrder{PoNumber="PO-1"},VendorNameSnapshot="A Vendor",VendorBillDate=new DateOnly(2026,2,1),ReceivedAt=new DateTimeOffset(2026,2,1,0,0,0,TimeSpan.Zero),Status="DRAFT"},
        new(){Id=Guid.Parse("00000000-0000-0000-0000-000000000001"),GrnNumber="GRN-1",GateEntry=new GateEntry{GateEntryNumber="GE-2"},PurchaseOrder=new PurchaseOrder{PoNumber="PO-2"},VendorNameSnapshot="B Vendor",VendorBillDate=new DateOnly(2026,1,1),ReceivedAt=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero),Status="FINALIZED"}
    ];
}
