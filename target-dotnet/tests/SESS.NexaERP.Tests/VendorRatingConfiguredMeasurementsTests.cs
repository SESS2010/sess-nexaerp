using SESS.NexaERP.Domain.Purchase;
namespace SESS.NexaERP.Tests;
public sealed class VendorRatingConfiguredMeasurementsTests
{
    // Deliberately synthetic scales exercise the mechanism; these are not SESS-approved thresholds.
    private static VendorRatingSource Source() => new(Guid.NewGuid(), 1);
    private static VendorRatingConfiguredMeasurements Rules() => new(Source(),
        [new(0,0),new(6,4),new(12,9)],new Dictionary<string,decimal>{{"NET 30",8}},
        ["INVOICE","CERTIFICATE"],new Dictionary<int,decimal>{{0,10},{1,3},{2,0}});
    private static VendorRequiredAttachment Attachment(string kind)
        => new(kind,Source(),new string('a',64),100);
    [Theory]
    [InlineData(0,0)] [InlineData(5,0)] [InlineData(6,4)] [InlineData(11,4)] [InlineData(12,9)] [InlineData(60,9)]
    public void WarrantyUsesOnlyTheExplicitScale(int months,decimal expected)
        => Assert.Equal(expected,Rules().Warranty(Source(),months).Points);
    [Fact]
    public void MissingWarrantyCannotUseGeneratedExpiry()
        => Assert.Throws<ArgumentException>(()=>Rules().Warranty(Source(),null));
    [Fact]
    public void CommercialUsesTheRetainedExactTermsAndRefusesUnmappedProse()
    {
        var rules=Rules();
        Assert.Equal(8,rules.Commercial(Source(),"NET 30").Points);
        Assert.Throws<ArgumentException>(()=>rules.Commercial(Source(),"NET 30 with additional advance"));
    }
    [Fact]
    public void DocumentMarksUseApprovedScaleNotAssumedEqualWeights()
    {
        var rules=Rules();
        var result=rules.Documents(Source(),[Attachment("INVOICE")]);
        Assert.Equal(3,result.Points); Assert.Equal("CERTIFICATE",Assert.Single(result.MissingKinds));
        Assert.Equal(10,rules.Documents(Source(),[Attachment("INVOICE"),Attachment("CERTIFICATE")]).Points);
        Assert.Equal(0,rules.Documents(Source(),[]).Points);
    }
    [Fact]
    public void ExtraFilesCannotStandInForMissingRequiredKind()
        => Assert.Equal(0,Rules().Documents(Source(),[Attachment("PHOTO"),Attachment("OTHER")]).Points);
    [Fact]
    public void ChecklistOrFilenameWithoutRetainedBytesIsNotEvidence()
        => Assert.Throws<ArgumentException>(()=>Rules().Documents(Source(),[Attachment("INVOICE") with {Bytes=0}]));
    [Fact]
    public void DuplicateEvidenceDoesNotInflatePresence()
    {
        var attachment=Attachment("INVOICE");
        Assert.Throws<ArgumentException>(()=>Rules().Documents(Source(),[attachment,attachment]));
    }
    [Fact]
    public void RuleCopiesPreventLaterDictionaryEditsRewritingItsMeaning()
    {
        var terms=new Dictionary<string,decimal>{{"NET 30",8}};
        var documents=new Dictionary<int,decimal>{{0,10},{1,0}};
        var required=new List<string>{"INVOICE"};
        var rules=new VendorRatingConfiguredMeasurements(Source(),[new(0,0)],terms,required,documents);
        terms["NET 30"]=10; documents[1]=10; required.Clear();
        Assert.Equal(8,rules.Commercial(Source(),"NET 30").Points);
        Assert.Equal(0,rules.Documents(Source(),[]).Points);
    }
    [Fact]
    public void NoImplicitDocumentScaleOrWarrantyScaleIsAllowed()
    {
        Assert.Throws<ArgumentException>(()=>new VendorRatingConfiguredMeasurements(Source(),[],
            new Dictionary<string,decimal>{{"NET 30",8}},["INVOICE"],new Dictionary<int,decimal>{{0,10},{1,0}}));
        Assert.Throws<ArgumentException>(()=>new VendorRatingConfiguredMeasurements(Source(),[new(0,0)],
            new Dictionary<string,decimal>{{"NET 30",8}},["INVOICE"],new Dictionary<int,decimal>{{0,10}}));
    }
}
