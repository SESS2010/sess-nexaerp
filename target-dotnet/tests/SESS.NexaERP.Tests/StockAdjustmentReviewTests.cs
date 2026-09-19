using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Tests;

public sealed class StockAdjustmentReviewTests
{
    private readonly Guid recorder = Guid.NewGuid();
    private readonly Guid counter = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 5, 0, 0, TimeSpan.Zero);

    private StockAdjustmentReview Review(decimal value = 100m, bool writeOff = false, bool backdated = false)
        => StockAdjustmentReview.Begin(Guid.NewGuid(), StockAdjustmentApprovalSnapshot.Capture(
            [new(-1m, value, false)], recorder, [counter], writeOff, requiresBackdateApproval: backdated));

    private static StockAdjustmentReview Approve(StockAdjustmentReview review, string role, Guid? employee = null)
        => review.Approve(review.RevisionId, employee ?? Guid.NewGuid(), role, Guid.NewGuid(), Now, "Verified retained revision");

    [Fact]
    public void LowValueReviewRequiresStoresAndRetainsDecisionEvidence()
    {
        var initial = Review();
        Assert.False(initial.IsApproved);
        var accepted = Approve(initial, "STORES_MANAGER");
        accepted.RequireApprovedRevision(accepted.RevisionId);
        Assert.Empty(initial.Decisions);
        var decision = Assert.Single(accepted.Decisions);
        Assert.Equal(accepted.RevisionId, decision.RevisionId);
        Assert.NotEqual(Guid.Empty, decision.RoleAssignmentId);
        Assert.Equal(Now, decision.DecidedAt);
        Assert.Throws<NotSupportedException>(() => ((IList<StockAdjustmentReviewDecision>)accepted.Decisions).Clear());
    }

    [Theory]
    [InlineData(5000, "TECHNICAL_DIRECTOR")]
    [InlineData(100000, "TECHNICAL_DIRECTOR")]
    [InlineData(100000.01, "MANAGING_DIRECTOR")]
    public void ValueBandDecisionMustUseTheRequiredRole(decimal value, string role)
    {
        var review = Review(value);
        Assert.Throws<InvalidOperationException>(() => Approve(review, "STORES_MANAGER"));
        Assert.True(Approve(review, role).IsApproved);
    }

    [Fact]
    public void WriteOffWaitsForBothIndependentDecisionsInEitherOrder()
    {
        foreach (var roles in new[] { new[] { "TECHNICAL_DIRECTOR", "ACCOUNTS_MANAGER" },
            new[] { "ACCOUNTS_MANAGER", "TECHNICAL_DIRECTOR" } })
        {
            var review = Approve(Review(writeOff: true), roles[0]);
            Assert.False(review.IsApproved);
            Assert.Throws<InvalidOperationException>(() => review.RequireApprovedRevision(review.RevisionId));
            Assert.Throws<InvalidOperationException>(() => Approve(review, roles[1], review.Decisions[0].EmployeeId));
            Assert.True(Approve(review, roles[1]).IsApproved);
        }
    }

    [Fact]
    public void RecorderAndCountersCannotDecideEvenWithTheRequiredRole()
    {
        var review = Review();
        Assert.Throws<InvalidOperationException>(() => Approve(review, "STORES_MANAGER", recorder));
        Assert.Throws<InvalidOperationException>(() => Approve(review, "STORES_MANAGER", counter));
    }

    [Fact]
    public void ChangedRevisionCannotUseOldApprovalOrReceiveStaleDecision()
    {
        var old = Approve(Review(), "STORES_MANAGER");
        var revised = Review();
        Assert.Throws<InvalidOperationException>(() => old.RequireApprovedRevision(revised.RevisionId));
        Assert.Throws<InvalidOperationException>(() => revised.Approve(old.RevisionId, Guid.NewGuid(),
            "STORES_MANAGER", Guid.NewGuid(), Now, "Stale view"));
        Assert.False(revised.IsApproved);
        Assert.Empty(revised.Decisions);
    }

    [Fact]
    public void RoleDecisionCannotBeDuplicated()
    {
        var review = Approve(Review(writeOff: true), "TECHNICAL_DIRECTOR");
        Assert.Throws<InvalidOperationException>(() => Approve(review, "TECHNICAL_DIRECTOR"));
    }

    [Fact]
    public void BackdatedHighValueRequiresBothMdAndTd()
    {
        var review = Approve(Review(100000.01m, backdated: true), "MANAGING_DIRECTOR");
        Assert.Equal("TECHNICAL_DIRECTOR", Assert.Single(review.OutstandingRoleCodes));
        Assert.True(Approve(review, "TECHNICAL_DIRECTOR").IsApproved);
    }

    [Fact]
    public void DecisionCannotOmitRetainedAssignmentTimeOrReason()
    {
        var review = Review();
        Assert.Throws<ArgumentException>(() => review.Approve(review.RevisionId, Guid.NewGuid(),
            "STORES_MANAGER", Guid.Empty, Now, "Checked"));
        Assert.Throws<ArgumentException>(() => review.Approve(review.RevisionId, Guid.NewGuid(),
            "STORES_MANAGER", Guid.NewGuid(), default, "Checked"));
        Assert.Throws<ArgumentException>(() => review.Approve(review.RevisionId, Guid.NewGuid(),
            "STORES_MANAGER", Guid.NewGuid(), Now, " "));
    }
}
