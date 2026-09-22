using SESS.NexaERP.Domain.Purchase;
namespace SESS.NexaERP.Tests;
public sealed class LegacyVendorRatingTests
{
    private static readonly VendorRatingDimensionScores Scores = new(25,20,10,10,7.5m,15,5,5);
    private static LegacyVendorRating Capture(decimal? total) => LegacyVendorRating.Capture(
        new string('A',64),"Historical bills",2,Guid.NewGuid(),Guid.NewGuid(),"INV/1",new(2025,6,1),Scores,total);
    [Fact]
    public void TypedFullMarksAlwaysRemainLegacy()
    {
        var row=Capture(97.5m);
        Assert.Equal("LEGACY",row.Provenance); Assert.False(row.IsMeasured);
        Assert.Equal(15,row.Scores.Technical); Assert.Equal(7.5m,row.Scores.Documents);
        Assert.Equal(new string('a',64),row.SourceSha256); Assert.False(row.HasTotalDiscrepancy);
    }
    [Fact]
    public void OriginalTotalDiscrepancyIsPreservedForReconciliation()
    {
        var row=Capture(96.9m);
        Assert.Equal(96.9m,row.OriginalTotal); Assert.Equal(97.5m,row.Scores.Total);
        Assert.True(row.HasTotalDiscrepancy);
    }
    [Fact]
    public void MissingOriginalTotalIsNotFabricated()
        => Assert.Null(Capture(null).OriginalTotal);
    [Theory]
    [InlineData(-1)] [InlineData(101)]
    public void InvalidOriginalTotalCannotBeAccepted(decimal total)
        => Assert.Throws<ArgumentException>(()=>Capture(total));
    [Fact]
    public void InvalidDimensionIsNotClampedToFullMarks()
        => Assert.Throws<ArgumentException>(()=>LegacyVendorRating.Capture(new string('a',64),"Sheet",2,
            Guid.NewGuid(),Guid.NewGuid(),"INV",new(2025,6,1),Scores with {Technical=16},100));
    [Fact]
    public void SourceIdentityIsRequired()
        => Assert.Throws<ArgumentException>(()=>LegacyVendorRating.Capture("missing","Sheet",2,
            Guid.NewGuid(),Guid.NewGuid(),"INV",new(2025,6,1),Scores,97.5m));
}
