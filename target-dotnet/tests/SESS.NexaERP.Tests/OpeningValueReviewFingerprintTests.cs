using System.Text.Json;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
namespace SESS.NexaERP.Tests;
public sealed class OpeningValueReviewFingerprintTests
{
    [Fact]
    public void NormalOpeningTransitionRetainsPreUpgradeRequestFingerprint()
    {
        var id=Guid.NewGuid();const string org="SESS_PVT_LTD",operation="OpeningStock.ConfirmValue",key="retained-value-key";
        var legacy=new{id,request=new{Version=2L,Reason="Signed ex-tax valuation",IdempotencyKey=key}};
        var normal=new{id,request=new OpeningStockTransitionRequest(2,"Signed ex-tax valuation",key)};
        var oldHash=Rev869BCommandContextAuthorizer.CommandEnvelope.Create(org,operation,key,legacy).RequestFingerprint;
        Assert.Equal(oldHash,Rev869BCommandContextAuthorizer.CommandEnvelope.Create(org,operation,key,normal).RequestFingerprint);
        var independent=new{id,request=normal.request with{IndependentMdReview=true}};
        Assert.NotEqual(oldHash,Rev869BCommandContextAuthorizer.CommandEnvelope.Create(org,operation,key,independent).RequestFingerprint);
    }
    [Fact]
    public void NewModeIsExplicitWhileAbsentOrFalseUsesLegacyWireShape()
    {
        var normal=new OpeningStockTransitionRequest(1,"Reason","key");
        Assert.DoesNotContain("IndependentMdReview",JsonSerializer.Serialize(normal));
        Assert.Contains("IndependentMdReview",JsonSerializer.Serialize(normal with{IndependentMdReview=true}));
        Assert.False(JsonSerializer.Deserialize<OpeningStockTransitionRequest>("{\"Version\":1,\"Reason\":\"Reason\",\"IdempotencyKey\":\"key\"}")!.IndependentMdReview);
    }
}
