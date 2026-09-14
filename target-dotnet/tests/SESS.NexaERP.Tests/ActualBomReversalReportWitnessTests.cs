namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public Task ActualBomReversalNegatesMachineCostWhileIssueRemainsInCustody() =>
        RunCompletePurchaseFlow(mixedRun: _ => Task.CompletedTask);
}
