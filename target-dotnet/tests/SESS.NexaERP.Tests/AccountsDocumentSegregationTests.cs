using SESS.NexaERP.Domain.Authorization;

namespace SESS.NexaERP.Tests;

public sealed class AccountsDocumentSegregationTests
{
    private static readonly IndependentValueReviewCandidates People = new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid());
    [Theory]
    [InlineData(AccountsDocumentDuty.Recorded)]
    [InlineData(AccountsDocumentDuty.Counted)]
    [InlineData(AccountsDocumentDuty.StoresDecision)]
    [InlineData(AccountsDocumentDuty.Issued)]
    public void MdReviewsWhenAlfathimaAbsentAndKarthickInvolvedInAnyStoresCapacity(AccountsDocumentDuty duty)
    {
        AccountsDocumentParticipation[] history=[new(People.Karthick,duty)];
        Assert.Equal(People.Md,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,history,[]));
        Assert.Equal(People.Td,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,history,[]));
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,history,[People.Td]));
    }
    [Fact]
    public void PlannedLowBandKarthickDecisionRequiresMdReviewWithoutChangingBand()
    {
        Assert.Equal(People.Md,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,[],[People.Karthick]));
        Assert.Equal(People.Td,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,[],[People.Karthick]));
    }
    [Fact]
    public void OpeningStockTdAuthorizerCannotAlsoBeFallbackValueReviewer()
    {
        AccountsDocumentParticipation[] history=[new(People.Karthick,AccountsDocumentDuty.Counted)];
        Assert.Equal(People.Md,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,history,[People.Td]));
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,history,[People.Td]));
    }
    [Fact]
    public void NormalAndUninvolvedBackupReviewAreRetained()
    {
        Assert.Equal(People.Alfathima,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,false,false,[],[]));
        Assert.Equal(People.Karthick,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,[],[]));
    }
    [Theory]
    [InlineData(AccountsDocumentDuty.Recorded,AccountsDocumentDuty.ValueReviewed)]
    [InlineData(AccountsDocumentDuty.Counted,AccountsDocumentDuty.L1Decision)]
    [InlineData(AccountsDocumentDuty.StoresDecision,AccountsDocumentDuty.DepartmentVerified)]
    [InlineData(AccountsDocumentDuty.Issued,AccountsDocumentDuty.ValueReviewed)]
    public void ConflictingDutiesRefuseInBothOrdersAcrossCanonicalIdentity(AccountsDocumentDuty first,AccountsDocumentDuty second)
    {
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.RequireIndependent(People.Karthick,second,[new(People.Karthick,first)]));
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.RequireIndependent(People.Karthick,first,[new(People.Karthick,second)]));
        AccountsDocumentSegregation.RequireIndependent(People.Md,second,[new(People.Karthick,first)]);
    }
    [Fact]
    public void TdWithAnyOtherRecordedDutyCannotReviewWhenMdUnavailable()
    {
        foreach(var duty in Enum.GetValues<AccountsDocumentDuty>())
            Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,
                [new(People.Karthick,AccountsDocumentDuty.StoresDecision),new(People.Td,duty)],[]));
    }
    [Fact]
    public void MdAlreadyFinalApproverDoesNotFallThroughToTdWhileMdAvailable()
    {
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,[],[People.Karthick,People.Md]));
    }
    [Fact]
    public void AnyRetainedKarthickDutyRequiresIndependentMdThenIndependentTd()
    {
        foreach(var duty in Enum.GetValues<AccountsDocumentDuty>())
        {
            AccountsDocumentParticipation[] facts=[new(People.Karthick,duty)];
            Assert.Equal(People.Md,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,false,facts,[]));
            Assert.Equal(People.Td,AccountsDocumentSegregation.SelectIndependentValueReviewer(People,true,true,facts,[]));
            foreach(var tdDuty in Enum.GetValues<AccountsDocumentDuty>())
                Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(
                    People,true,true,[..facts,new(People.Td,tdDuty)],[]));
        }
    }
    [Fact]
    public void EmptyOrAliasedIdentityCannotSelectReviewer()
    {
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.SelectIndependentValueReviewer(People with {Md=People.Karthick},true,false,[],[]));
        Assert.Throws<InvalidOperationException>(()=>AccountsDocumentSegregation.RequireIndependent(Guid.Empty,AccountsDocumentDuty.ValueReviewed,[]));
    }
}
