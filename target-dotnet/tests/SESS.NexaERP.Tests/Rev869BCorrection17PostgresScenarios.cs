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
    [Fact] public Task T01_ControllerOwnsFixtureAllocation() => RunAsync(Rev869BAcceptanceScenarioInventory.T01);
    [Fact] public Task T02_FailedScenarioCleanupSurvivesRestart() => RunAsync(Rev869BAcceptanceScenarioInventory.T02);
    [Fact] public Task T03_ConcurrentFixturesRemainIsolated() => RunAsync(Rev869BAcceptanceScenarioInventory.T03);

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
        Assert.True(evidence.SetupCompleted);
        Assert.True(evidence.ActionReached);
        Assert.True(evidence.CleanupFinalized);
        Assert.True(evidence.TargetAbsent);
        Assert.True(evidence.RolesAbsent);
    }
}
