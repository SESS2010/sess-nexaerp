using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Tests;

public sealed class StockAdjustmentApprovalSnapshotTests
{
    [Fact]
    public void OffsettingChangesCannotHideTheValueRequiringDirectorApproval()
    {
        var snapshot = StockAdjustmentApprovalSnapshot.Capture(
            [new(1m, 60000m, false), new(-1m, 60000m, false)], Guid.NewGuid(), [], false);
        Assert.Equal(120000m, snapshot.AbsoluteValue);
        Assert.Equal(["MANAGING_DIRECTOR"], snapshot.RequiredRoleCodes);
    }

    [Fact]
    public void AuthorityUsesAcceptedLineValueWithoutRecomputingAnAverageUnitRate()
    {
        var snapshot = StockAdjustmentApprovalSnapshot.Capture(
            [new(-3m, 100000.000001m, false)], Guid.NewGuid(), [], false);
        Assert.Equal(100000.000001m, snapshot.AbsoluteValue);
        Assert.Equal(["MANAGING_DIRECTOR"], snapshot.RequiredRoleCodes);
    }

    [Fact]
    public void EveryCounterAndRecorderRemainExcludedEvenIfTheirRoleCanApprove()
    {
        var recorder = Guid.NewGuid(); var first = Guid.NewGuid(); var recount = Guid.NewGuid();
        var snapshot = StockAdjustmentApprovalSnapshot.Capture([new(-1m, 100m, false)],
            recorder, [first, recount, first], false);
        foreach (var employee in new[] { recorder, first, recount })
            Assert.Throws<InvalidOperationException>(() => snapshot.ValidateIndependentDecision(employee, "STORES_MANAGER"));
        snapshot.ValidateIndependentDecision(Guid.NewGuid(), "STORES_MANAGER");
    }

    [Fact]
    public void ZeroValueSerialCorrectionStillNeedsTechnicalDirector()
    {
        var snapshot = StockAdjustmentApprovalSnapshot.Capture([new(0m, 0m, true)],
            Guid.NewGuid(), [], false);
        Assert.Equal(["TECHNICAL_DIRECTOR"], snapshot.RequiredRoleCodes);
        Assert.Throws<InvalidOperationException>(() => snapshot.ValidateIndependentDecision(Guid.NewGuid(), "STORES_MANAGER"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void WriteOffCannotAuthorizeAnAdditionOrIdentityOnlyChange(decimal quantity) =>
        Assert.Throws<ArgumentException>(() => StockAdjustmentApprovalSnapshot.Capture(
            [new(quantity, 100m, true)], Guid.NewGuid(), [], true));

    [Fact]
    public void ReversalDoesNotDropOriginalWriteOffConcurrence()
    {
        var original = StockAdjustmentApprovalSnapshot.Capture([new(-1m, 100m, false)],
            Guid.NewGuid(), [], true);
        var reversal = StockAdjustmentApprovalSnapshot.Capture([new(1m, 100m, false)],
            Guid.NewGuid(), [], false, original);
        Assert.Equal(["ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR"], reversal.RequiredRoleCodes);
    }
}
