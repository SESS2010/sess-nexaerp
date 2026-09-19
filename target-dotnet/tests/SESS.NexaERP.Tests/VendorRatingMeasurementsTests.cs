using SESS.NexaERP.Domain.Purchase;
namespace SESS.NexaERP.Tests;
public sealed class VendorRatingMeasurementsTests
{
    private static VendorRatingSource Source() => new(Guid.NewGuid(), 1);
    [Theory]
    [InlineData(-1,20)] [InlineData(0,20)] [InlineData(1,18)] [InlineData(3,18)]
    [InlineData(4,15)] [InlineData(7,15)] [InlineData(8,10)] [InlineData(15,10)] [InlineData(16,0)]
    public void DeliveryUsesAgreedBoundary(int days, decimal points)
    {
        var committed = new DateOnly(2026,9,1);
        var result = VendorRatingMeasurements.Delivery(Source(), Source(), committed, committed.AddDays(days));
        Assert.Equal(points, result.Points);
        Assert.Equal(Math.Max(0,days),result.DaysLate);
    }
    [Fact]
    public void ConcessionIsAcceptedAndRetainedSeparately()
    {
        var receipt=Source(); var qc=Source();
        var result=VendorRatingMeasurements.Quality(receipt,qc,10,7,1);
        Assert.Equal(20m,result.Points); Assert.Equal(1,result.ConcessionAcceptedQuantity);
        Assert.Equal(receipt,result.ReceiptLine); Assert.Equal(qc,result.QcDisposition);
    }
    [Theory]
    [InlineData(0,0,0)] [InlineData(10,-1,0)] [InlineData(10,0,-1)]
    [InlineData(10,10,1)] [InlineData(10,11,0)]
    public void InvalidOrDoubleCountedQualityCannotBecomeAMark(decimal received,decimal accepted,decimal concession)
        => Assert.Throws<ArgumentException>(()=>VendorRatingMeasurements.Quality(Source(),Source(),received,accepted,concession));
    [Fact]
    public void ZeroAcceptedIsZeroNotMissingOrFullMarks()
        => Assert.Equal(0,VendorRatingMeasurements.Quality(Source(),Source(),10,0,0).Points);
    [Fact]
    public void MissingEvidenceCannotBecomeFullMarks()
    {
        Assert.Throws<ArgumentException>(()=>VendorRatingMeasurements.Quality(new(Guid.Empty,1),Source(),10,10,0));
        Assert.Throws<ArgumentException>(()=>VendorRatingMeasurements.Quality(Source(),new(Guid.NewGuid(),-1),10,10,0));
        Assert.Throws<ArgumentException>(()=>VendorRatingMeasurements.Delivery(Source(),Source(),default,new(2026,9,1)));
    }
    [Fact]
    public void OriginalVersionZeroIsRetainedEvidence()
    {
        var receipt=new VendorRatingSource(Guid.NewGuid(),0);
        var qc=new VendorRatingSource(Guid.NewGuid(),0);
        var measurement=VendorRatingMeasurements.Quality(receipt,qc,10,8,0);
        Assert.Equal(20m,measurement.Points);
        Assert.Equal(0,measurement.ReceiptLine.Revision);
        Assert.Equal(0,measurement.QcDisposition.Revision);
    }
    [Fact]
    public void UnlikeLineUnitsRemainSeparateMeasurements()
    {
        var pieces=VendorRatingMeasurements.Quality(Source(),Source(),1,0,0);
        var kilograms=VendorRatingMeasurements.Quality(Source(),Source(),1000,1000,0);
        Assert.Equal(0,pieces.Points); Assert.Equal(25,kilograms.Points);
    }
}
