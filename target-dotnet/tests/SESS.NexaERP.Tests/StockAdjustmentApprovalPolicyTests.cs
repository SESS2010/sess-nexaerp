using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Tests;

public sealed class StockAdjustmentApprovalPolicyTests
{
    [Theory]
    [InlineData(0, "STORES_MANAGER")]
    [InlineData(4999.99, "STORES_MANAGER")]
    [InlineData(5000, "TECHNICAL_DIRECTOR")]
    [InlineData(100000, "TECHNICAL_DIRECTOR")]
    [InlineData(100000.01, "MANAGING_DIRECTOR")]
    public void MonetaryBoundariesUseTheApprovedBands(decimal value, string role)
    {
        var requirement = StockAdjustmentApprovalPolicy.Resolve(value, false, false);
        Assert.Equal(role, requirement.ApproverRoleCode);
        Assert.False(requirement.AccountsConcurrenceRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4999.99)]
    [InlineData(5000)]
    [InlineData(100000.01)]
    public void SerializedIdentityAlwaysRequiresTechnicalDirector(decimal value)
    {
        var requirement = StockAdjustmentApprovalPolicy.Resolve(value, true, false);
        Assert.Equal("TECHNICAL_DIRECTOR", requirement.ApproverRoleCode);
        Assert.False(requirement.AccountsConcurrenceRequired);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4999.99, true)]
    [InlineData(5000, false)]
    [InlineData(100000.01, true)]
    public void WriteOffRequiresTechnicalDirectorAndAccounts(decimal value, bool serial)
    {
        var requirement = StockAdjustmentApprovalPolicy.Resolve(value, serial, true);
        Assert.Equal("TECHNICAL_DIRECTOR", requirement.ApproverRoleCode);
        Assert.True(requirement.AccountsConcurrenceRequired);
    }

    [Fact]
    public void SignedValueCannotSilentlySelectTheLowestBand() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => StockAdjustmentApprovalPolicy.Resolve(-100001m, false, false));
}
