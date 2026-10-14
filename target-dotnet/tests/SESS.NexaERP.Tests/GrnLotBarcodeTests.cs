using SESS.NexaERP.Application.Stores;
namespace SESS.NexaERP.Tests;
public sealed class GrnLotBarcodeTests
{
    [Fact] public void CompactIdentityKeepsReceiptSuffixAndExactLineLot()
    {
        var code = GrnLotBarcode.Create("SESS_PVT_LTD", "GRN-26-27-000011", 1, 1);
        Assert.Equal("LP:2627000011001001", code);
        Assert.True(GrnLotBarcode.TryParse(code, out var company, out var number, out var line, out var lot));
        Assert.Equal("SESS_PVT_LTD", company);
        Assert.NotEqual(code, GrnLotBarcode.Create("SESS_PROPRIETORSHIP", number, line, lot));
        Assert.Equal("GRN-26-27-000011", number); Assert.Equal(1, line); Assert.Equal(1, lot);
        Assert.NotEqual(code, GrnLotBarcode.Create("SESS_PVT_LTD", number, line, 2));
    }
    [Theory][InlineData("GRN-26-27-000011",1000,1)][InlineData("GRN-26-27-1000001",1,1000)]
    public void LongFutureNumbersHaveLosslessA4Fallback(string number, int line, int lot)
    {
        var code = GrnLotBarcode.Create("SESS_PVT_LTD", number, line, lot);
        Assert.True(GrnLotBarcode.TryParse(code, out var company, out var parsed, out var parsedLine, out var parsedLot));
        Assert.Equal("SESS_PVT_LTD", company);
        Assert.Equal(number, parsed); Assert.Equal(line, parsedLine); Assert.Equal(lot, parsedLot);
    }
    [Theory][InlineData("LP:2627000011000001")][InlineData("LP:2627000011001000")][InlineData("LP:2627000011001ABC")][InlineData("SESS_PVT_LTD/GRN-X/0/1")]
    public void InvalidOrZeroIdentifiersAreRefused(string code) => Assert.False(GrnLotBarcode.TryParse(code, out _, out _, out _, out _));
}