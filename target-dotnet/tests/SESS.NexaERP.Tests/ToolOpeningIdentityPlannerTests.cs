using SESS.NexaERP.Domain.Stores;
namespace SESS.NexaERP.Tests;

public sealed class ToolOpeningIdentityPlannerTests
{
    private static readonly Guid Company=Guid.Parse("cc000000-0000-0000-0000-000000000001");
    private static readonly Guid ItemA=Guid.Parse("aa000000-0000-0000-0000-000000000001");
    private static readonly Guid ItemB=Guid.Parse("aa000000-0000-0000-0000-000000000002");
    private static readonly Guid Holder=Guid.Parse("ee000000-0000-0000-0000-000000000001");
    private static readonly ToolOpeningTypeRow[] Types=[new("Types!2","A",4,3,1),new("Types!3","B",2,0,2)];
    private static readonly ToolOpeningCustodyRow[] Custody=[new("Custody!2","A","E1",2),new("Custody!3","A","E1",1)];
    private static readonly ToolOpeningItemMapping[] Items=[new("A",ItemA),new("B",ItemB)];
    private static readonly ToolOpeningHolderMapping[] Holders=[new("E1",Holder)];
    private static readonly ToolOpeningRegisterTotals Totals=new(2,6,3,3,2,1);
    private static ToolOpeningIdentityPlan Plan() => ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),Types,Custody,Items,Holders,Totals);

    [Fact]
    public void QuantityTwoBecomesTwoDistinctIdentitiesWithTheSameSourceRow()
    {
        var plan=Plan(); Assert.Equal(6,plan.Individuals.Count);
        Assert.Equal(6,plan.Individuals.Select(x=>x.AssetId).Distinct().Count());
        var first=plan.Individuals.Where(x=>x.CustodySourceRow=="Custody!2").ToArray();
        Assert.Equal(2,first.Length); Assert.Equal(new int?[]{1,2},first.Select(x=>x.OrdinalWithinCustodyRow));
        Assert.All(first,x=>Assert.Equal(Holder,x.HolderEmployeeId));
        Assert.Equal(3,plan.Individuals.Count(x=>x.HolderEmployeeId is null));
        Assert.All(plan.Individuals,x=>Assert.StartsWith("TOOL-",x.InternalAssetCode));
    }
    [Fact]
    public void ReorderedRowsAndMappingsProduceTheSameIdentitiesAndReviewFingerprint()
    {
        var first=Plan();
        var reordered=ToolOpeningIdentityPlanner.Prepare(Company,new('A',64),Types.Reverse(),Custody.Reverse(),Items.Reverse(),Holders,Totals);
        Assert.Equal(first.Individuals,reordered.Individuals); Assert.Equal(first.ReviewFingerprint,reordered.ReviewFingerprint);
    }
    [Fact]
    public void AChangedHolderMappingChangesReviewFingerprintWithoutPretendingItIsANewSource()
    {
        var first=Plan();
        var changed=ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),Types,Custody,Items,[new("E1",Guid.NewGuid())],Totals);
        Assert.Equal(first.Individuals.Select(x=>x.AssetId),changed.Individuals.Select(x=>x.AssetId));
        Assert.NotEqual(first.ReviewFingerprint,changed.ReviewFingerprint);
    }
    [Fact]
    public void DifferentCompaniesCannotShareImportedAssetIdentities()
    {
        var first=Plan();
        var second=ToolOpeningIdentityPlanner.Prepare(Guid.NewGuid(),new('a',64),Types,Custody,Items,Holders,Totals);
        Assert.Empty(first.Individuals.Select(x=>x.AssetId).Intersect(second.Individuals.Select(x=>x.AssetId)));
    }
    [Fact]
    public void MissingOrCollapsingItemMappingsRefuseThePlan()
    {
        Assert.Throws<ArgumentException>(()=>ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),Types,Custody,[Items[0]],Holders,Totals));
        Assert.Throws<ArgumentException>(()=>ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),Types,Custody,[Items[0],new("B",ItemA)],Holders,Totals));
    }
    [Fact]
    public void UnknownEmployeeMappingRefusesThePlan()
        => Assert.Throws<ArgumentException>(()=>ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),Types,Custody,Items,[],Totals));
    [Fact]
    public void TotalsAloneCannotSubstituteForSourceRows()
        => Assert.Throws<ArgumentException>(()=>ToolOpeningIdentityPlanner.Prepare(Company,new('a',64),[],[],[],[],ToolOpeningRegisterReconciliation.SessExpected));
    [Fact]
    public void ActualWorkbookIdentityIsRequired()
        => Assert.Throws<ArgumentException>(()=>ToolOpeningIdentityPlanner.Prepare(Company,"not-a-hash",Types,Custody,Items,Holders,Totals));
}
