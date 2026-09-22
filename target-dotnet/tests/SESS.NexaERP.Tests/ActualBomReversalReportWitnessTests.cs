namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if WORKFLOW_WITNESS
    [Fact]
    public Task ActualBomReversalNegatesMachineCostWhileIssueRemainsInCustody() =>
        RunCompletePurchaseFlow(mixedRun: _ => Task.CompletedTask);
#endif
}
