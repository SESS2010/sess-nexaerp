namespace SESS.NexaERP.Tests;

/// <summary>
/// The 34 executable PostgreSQL acceptance bodies authorized by the Correction 20 reconciliation.
/// Offline validation discovers and compiles these tests but must never execute them.
/// </summary>
public sealed class Rev869BCorrection17PostgresScenarios
{
    [Fact] public Task P01_ExternalProvisioningManifestIsVerified() => RunAsync(Rev869BAcceptanceScenarioInventory.P01);
    [Fact] public Task P02_MismatchedExternalManifestIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.P02);
    [Fact] public Task P03_CatalogueOrAclDriftIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.P03);
    [Fact] public Task L01_ReservedLeaseBecomesReady() => RunAsync(Rev869BAcceptanceScenarioInventory.L01);
    [Fact] public Task L02_InterruptedCreateIsRecovered() => RunAsync(Rev869BAcceptanceScenarioInventory.L02);
    [Fact] public Task L03_ConcurrentLifecycleAttemptIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.L03);
    [Fact] public Task L04_DropAuthorizedLeaseIsFinalized() => RunAsync(Rev869BAcceptanceScenarioInventory.L04);
    [Fact] public Task L05_IdentityMismatchIsQuarantined() => RunAsync(Rev869BAcceptanceScenarioInventory.L05);
    [Fact] public Task R01_ExactRecoveryDecisionIsConsumed() => RunAsync(Rev869BAcceptanceScenarioInventory.R01);
    [Fact] public Task R02_RecoveryDecisionReplayIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.R02);
    [Fact] public Task R03_CleanupFailureRequiresFreshRecovery() => RunAsync(Rev869BAcceptanceScenarioInventory.R03);
    [Fact] public Task C01_CommandCommitPersistsReceiptAndOutcome() => RunAsync(Rev869BAcceptanceScenarioInventory.C01);
    [Fact] public Task C02_LostResponseReplayReadsReceipt() => RunAsync(Rev869BAcceptanceScenarioInventory.C02);
    [Fact] public Task C03_ChangedRequestReplayIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.C03);
    [Fact] public Task C04_ReceiptFaultRollsBackBusinessMutation() => RunAsync(Rev869BAcceptanceScenarioInventory.C04);
    [Fact] public Task C05_RollbackRecordsExactTerminalOutcome() => RunAsync(Rev869BAcceptanceScenarioInventory.C05);
    [Fact] public Task C06_InterruptedAttemptIsReconciledAfterRestart() => RunAsync(Rev869BAcceptanceScenarioInventory.C06);
    [Fact] public Task C07_ConcurrentCommandAttemptIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.C07);
    [Fact] public Task C08_SubstitutedCommandBindingIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.C08);
    [Fact] public Task G01_InvalidPurgeAuthorizationIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.G01);
    [Fact] public Task G02_VerifiedEmptyPurgeTerminatesZeroRows() => RunAsync(Rev869BAcceptanceScenarioInventory.G02);
    [Fact] public Task G03_FrozenPurgeCandidatesAreDeletedAndAudited() => RunAsync(Rev869BAcceptanceScenarioInventory.G03);
    [Fact] public Task G04_PurgeCandidateDriftIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.G04);
    [Fact] public Task G05_DeleteFaultRollsBackAndIsIndependentlyRecorded() => RunAsync(Rev869BAcceptanceScenarioInventory.G05);
    [Fact] public Task G06_PurgeConcurrencyAndRetryBindingAreEnforced() => RunAsync(Rev869BAcceptanceScenarioInventory.G06);
    [Fact] public Task E01_MinimizedExportBatchIsPrepared() => RunAsync(Rev869BAcceptanceScenarioInventory.E01);
    [Fact] public Task E02_PreparedExportBatchIsImmutable() => RunAsync(Rev869BAcceptanceScenarioInventory.E02);
    [Fact] public Task E03_InvalidExportReleaseIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.E03);
    [Fact] public Task E04_InterruptedDeliveryRequiresNewRelease() => RunAsync(Rev869BAcceptanceScenarioInventory.E04);
    [Fact] public Task A01_EffectivePrivilegeInventoryMatchesExactly() => RunAsync(Rev869BAcceptanceScenarioInventory.A01);
    [Fact] public Task A02_ProtectedDirectAccessIsDenied() => RunAsync(Rev869BAcceptanceScenarioInventory.A02);
    [Fact]
    public async Task T01_ControllerOwnsFixtureAllocation()
    {
        await RunAsync(Rev869BAcceptanceScenarioInventory.T01);
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var lease = await controller.AllocateAsync("T01", "ControllerOwnedFixture");
        Assert.Equal("InUse", lease.State);
        Assert.True(lease.FixturePrepared);
        Assert.NotEqual(Guid.Empty, lease.LeaseId);
        Assert.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, lease.DatabaseName, StringComparison.Ordinal);
        await controller.ReleaseAsync(lease.LeaseId, Guid.NewGuid());
    }
    [Fact] public Task T02_FailedScenarioCleanupSurvivesRestart() => RunAsync(Rev869BAcceptanceScenarioInventory.T02);
    [Fact]
    public async Task T03_ConcurrentFixturesRemainIsolated()
    {
        var canonical = Rev869BAcceptanceScenarioInventory.T03;
        var actionRemoved = canonical with { Action = string.Empty };
        Assert.NotEqual(Rev869BLifecycleControllerClient.ExactContractSha256(canonical),
            Rev869BLifecycleControllerClient.ExactContractSha256(actionRemoved));
        await RunAsync(Rev869BAcceptanceScenarioInventory.T03);
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var allocations = await Task.WhenAll(
            controller.AllocateAsync("T03-A", "ConcurrentFixtureIsolation"),
            controller.AllocateAsync("T03-B", "ConcurrentFixtureIsolation"));
        Assert.NotEqual(allocations[0].LeaseId, allocations[1].LeaseId);
        Assert.NotEqual(allocations[0].DatabaseName, allocations[1].DatabaseName);
        Assert.NotEqual(allocations[0].FixtureSha256, allocations[1].FixtureSha256);
        await Task.WhenAll(
            controller.ReleaseAsync(allocations[0].LeaseId, Guid.NewGuid()),
            controller.ReleaseAsync(allocations[1].LeaseId, Guid.NewGuid()));
    }

    private static async Task RunAsync(Rev869BLifecycleControllerClient.AcceptanceContract contract)
    {
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var evidence = await controller.RunAcceptanceScenarioAsync(contract);
        Assert.Equal(contract.ScenarioId, evidence.ScenarioId);
        Assert.Equal(contract.Setup, evidence.Setup);
        Assert.Equal(contract.Action, evidence.Action);
        Assert.Equal(contract.ExpectedInitialState, evidence.InitialState);
        Assert.Equal(contract.ExpectedFinalState, evidence.FinalState);
        Assert.Equal(contract.ExpectedAffectedRows, evidence.AffectedRows);
        Assert.Equal(contract.ExpectedDatabaseName, evidence.DatabaseName);
        Assert.Equal(contract.ExpectedTargetInstanceSha256, evidence.TargetInstanceSha256);
        Assert.Equal(contract.ExpectedFixtureId, evidence.FixtureId);
        Assert.Equal(contract.ExpectedCommandId, evidence.CommandId);
        Assert.Equal(contract.ExpectedAuthorizationId, evidence.AuthorizationId);
        Assert.Equal(contract.ExpectedAttemptId, evidence.AttemptId);
        Assert.Equal(contract.ExpectedDecisionId, evidence.DecisionId);
        Assert.Equal(contract.ExpectedDurableEvidenceId, evidence.DurableEvidenceId);
        Assert.Equal(contract.ExpectedCleanupEvidenceId, evidence.CleanupEvidenceId);
        Assert.Equal(contract.ExpectedFixtureSha256, evidence.FixtureSha256);
        Assert.Equal(contract.ExpectedBeforeSha256, evidence.BeforeSha256);
        Assert.Equal(contract.ExpectedAfterSha256, evidence.AfterSha256);
        Assert.Equal(contract.ExpectedDurableEvidenceSha256, evidence.DurableEvidenceSha256);
        Assert.Equal(contract.ExpectedCleanupEvidenceSha256, evidence.CleanupEvidenceSha256);
        Assert.Equal(contract.ExpectedSubcaseEvidenceKeys, evidence.SubcaseEvidenceKeys);
        Assert.Equal(contract.ExpectedBeforeCount, evidence.BeforeCount);
        Assert.Equal(contract.ExpectedAfterCount, evidence.AfterCount);
        Assert.Equal(contract.ExpectedIdentity, evidence.DatabaseIdentity);
        Assert.Equal(contract.ExpectedTerminalOutcome, evidence.TerminalOutcome);
        Assert.Equal(contract.ExpectedCleanupOutcome, evidence.CleanupOutcome);
        Assert.True(evidence.SetupCompleted);
        Assert.True(evidence.ActionReached);
        Assert.True(evidence.CleanupFinalized);
        Assert.True(evidence.TargetAbsent);
        Assert.True(evidence.RolesAbsent);
    }
}
