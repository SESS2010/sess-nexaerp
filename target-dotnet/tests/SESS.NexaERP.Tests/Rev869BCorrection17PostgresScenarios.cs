namespace SESS.NexaERP.Tests;

/// <summary>Exactly 34 PostgreSQL acceptance facts. Offline validation discovers/compiles but never executes them.</summary>
public sealed class Rev869BCorrection17PostgresScenarios
{
    [Fact] public Task P01_ExternalProvisioningManifestIsVerified()=>RunAsync(Rev869BAcceptanceScenarioInventory.P01);
    [Fact] public Task P02_MismatchedExternalManifestIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.P02);
    [Fact] public Task P03_CatalogueOrAclDriftIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.P03);
    [Fact] public Task L01_ReservedInterruptionResumesOrUsesApprovedCleanup()=>RunAsync(Rev869BAcceptanceScenarioInventory.L01);
    [Fact] public Task L02_InterruptedCreateIsRecovered()=>RunAsync(Rev869BAcceptanceScenarioInventory.L02);
    [Fact] public Task L03_ConcurrentNormalCleanupProducesOneDrop()=>RunAsync(Rev869BAcceptanceScenarioInventory.L03);
    [Fact] public Task L04_EveryCleanupBoundaryReconcilesOnce()=>RunAsync(Rev869BAcceptanceScenarioInventory.L04);
    [Fact] public Task L05_IdentityMismatchIsQuarantined()=>RunAsync(Rev869BAcceptanceScenarioInventory.L05);
    [Fact] public Task R01_ExactRecoveryDecisionIsConsumed()=>RunAsync(Rev869BAcceptanceScenarioInventory.R01);
    [Fact] public Task R02_RecoveryDecisionReplayIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.R02);
    [Fact] public Task R03_CleanupFailureRequiresFreshRecovery()=>RunAsync(Rev869BAcceptanceScenarioInventory.R03);
    [Fact] public Task C01_CommandCommitPersistsReceiptAndOutcome()=>RunAsync(Rev869BAcceptanceScenarioInventory.C01);
    [Fact] public Task C02_LostResponseReplayReadsReceipt()=>RunAsync(Rev869BAcceptanceScenarioInventory.C02);
    [Fact] public Task C03_ChangedRequestReplayIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.C03);
    [Fact] public Task C04_ReceiptFaultRollsBackBusinessMutation()=>RunAsync(Rev869BAcceptanceScenarioInventory.C04);
    [Fact] public Task C05_RollbackRecordsExactTerminalOutcome()=>RunAsync(Rev869BAcceptanceScenarioInventory.C05);
    [Fact] public Task C06_InterruptedAttemptIsReconciledAfterRestart()=>RunAsync(Rev869BAcceptanceScenarioInventory.C06);
    [Fact] public Task C07_ConcurrentCommandAttemptIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.C07);
    [Fact] public Task C08_SubstitutedCommandBindingIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.C08);
    [Fact] public Task G01_InvalidPurgeAuthorizationIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.G01);
    [Fact] public Task G02_VerifiedEmptyPurgeTerminatesZeroRows()=>RunAsync(Rev869BAcceptanceScenarioInventory.G02);
    [Fact] public Task G03_FrozenPurgeCandidatesAreDeletedAndAudited()=>RunAsync(Rev869BAcceptanceScenarioInventory.G03);
    [Fact] public Task G04_PurgeCandidateDriftIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.G04);
    [Fact] public Task G05_DeleteFaultRollsBackAndIsIndependentlyRecorded()=>RunAsync(Rev869BAcceptanceScenarioInventory.G05);
    [Fact] public Task G06_PurgeConcurrencyAndRetryBindingAreEnforced()=>RunAsync(Rev869BAcceptanceScenarioInventory.G06);
    [Fact] public Task E01_MinimizedExportBatchIsPrepared()=>RunAsync(Rev869BAcceptanceScenarioInventory.E01);
    [Fact] public Task E02_PreparedExportBatchIsImmutable()=>RunAsync(Rev869BAcceptanceScenarioInventory.E02);
    [Fact] public Task E03_InvalidExportReleaseIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.E03);
    [Fact] public Task E04_InterruptedDeliveryRequiresNewRelease()=>RunAsync(Rev869BAcceptanceScenarioInventory.E04);
    [Fact] public Task A01_EffectivePrivilegeInventoryMatchesExactly()=>RunAsync(Rev869BAcceptanceScenarioInventory.A01);
    [Fact] public Task A02_ProtectedDirectAccessIsDenied()=>RunAsync(Rev869BAcceptanceScenarioInventory.A02);
    [Fact] public Task T01_ControllerOwnsFixtureAllocation()=>RunAsync(Rev869BAcceptanceScenarioInventory.T01);
    [Fact] public Task T02_FailedScenarioCleanupSurvivesRestart()=>RunAsync(Rev869BAcceptanceScenarioInventory.T02);

    [Fact]
    public void T03_EveryScenarioActionQueryAssertionAndCleanupIsMutationSensitive()
    {
        Rev869BCorrection26FrozenOracle.Validate();
        Assert.Equal(34, Rev869BAcceptanceScenarioInventory.All.Count);
        Assert.Equal(108, Rev869BCorrection26FrozenOracle.Subcases.Length);
        Assert.Equal(133, Rev869BCorrection26FrozenOracle.Selectors.Length);
        foreach (var canonical in Rev869BAcceptanceScenarioInventory.All)
        {
            Rev869BLifecycleControllerClient.ValidateContract(canonical);
            Assert.Equal(canonical.Plan.Assertions.Count * 2 + 6, canonical.Plan.Mutations.Count);
            foreach (var subcase in canonical.RequiredSubcases)
            {
                var pristine = Rev869BLifecycleControllerClient.BuildOracleEvidence(canonical, subcase);
                Assert.Empty(Rev869BLifecycleControllerClient.VerifyEvidence(canonical, subcase, pristine));
                foreach (var mutation in Enum.GetValues<Rev869BLifecycleControllerClient.EvidenceMutationKind>())
                {
                    var tampered = Rev869BLifecycleControllerClient.MutateEvidence(canonical, subcase, pristine, mutation);
                    Assert.NotEmpty(Rev869BLifecycleControllerClient.VerifyEvidence(canonical, subcase, tampered));
                }
                foreach (var assertion in canonical.Plan.Assertions)
                {
                    var tampered = Rev869BLifecycleControllerClient.TamperEvidence(pristine, assertion);
                    Assert.False(Rev869BLifecycleControllerClient.Evaluate(assertion, tampered), assertion.AssertionId);
                }
            }
            foreach (var mutation in canonical.Plan.Mutations)
            {
                var changed = Rev869BLifecycleControllerClient.ApplyMutation(canonical, mutation);
                Assert.NotEqual(
                    Rev869BLifecycleControllerClient.ExactContractSha256(canonical.Descriptor),
                    Rev869BLifecycleControllerClient.ExactContractSha256(changed.Descriptor));

                Assert.Throws<ArgumentException>(() => Rev869BLifecycleControllerClient.ValidateContract(changed));
            }
        }
    }

    private static async Task RunAsync(Rev869BLifecycleControllerClient.AcceptanceContract contract)
    {
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var result = await controller.RunAcceptanceScenarioAsync(contract);
        Assert.Equal(contract.ScenarioId, result.ScenarioId);
        Assert.Empty(result.FailedAssertions);
        Assert.True(result.Action.ActionReached);
        Assert.Equal(contract.Plan.ActionOperationId, "rev869b/" + result.ScenarioId + "/action/v2");
        Assert.All(new[] { result.Before.CanonicalSha256, result.After.CanonicalSha256,
            result.Durable.CanonicalSha256, result.Audit.CanonicalSha256, result.Cleanup.CanonicalSha256 },
            value => Assert.Matches("^[0-9a-f]{64}$", value));
        Assert.Equal(7, new[] { result.LeaseId, result.FixtureId, result.CommandId, result.AuthorizationId,
            result.AttemptId, result.Action.EvidenceId, result.RunId }.Distinct().Count());
        Assert.True(result.Before.FactCount >= 0);
        Assert.True(result.After.FactCount >= 0);
        Assert.True(result.Durable.FactCount > 0);
        Assert.True(result.Audit.FactCount > 0);
        Assert.True(result.Cleanup.FactCount > 0);
    }
}
