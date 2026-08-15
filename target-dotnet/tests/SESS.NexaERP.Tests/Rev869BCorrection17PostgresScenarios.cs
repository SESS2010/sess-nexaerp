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
    [Fact] public Task L01_ReservedInterruptionResumesOrUsesApprovedCleanup() => RunAsync(Rev869BAcceptanceScenarioInventory.L01);
    [Fact] public Task L02_InterruptedCreateIsRecovered() => RunAsync(Rev869BAcceptanceScenarioInventory.L02);
    [Fact] public Task L03_ConcurrentNormalCleanupProducesOneDrop() => RunAsync(Rev869BAcceptanceScenarioInventory.L03);
    [Fact] public Task L04_EveryCleanupBoundaryReconcilesOnce() => RunAsync(Rev869BAcceptanceScenarioInventory.L04);
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
    public void T03_EveryScenarioActionIsMutationSensitive()
    {
        foreach (var canonical in Rev869BAcceptanceScenarioInventory.All)
        {
            Assert.NotEmpty(canonical.RequiredSubcases);
            var first = canonical.RequiredSubcases[0];
            var mutations = new[]
            {
                canonical with { Setup = string.Empty },
                canonical with { Action = string.Empty },
                canonical with { ExpectedInitialState = string.Empty },
                canonical with { ExpectedFinalState = string.Empty },
                canonical with { ExpectedTerminalOutcome = string.Empty },
                canonical with { ExpectedCleanupOutcome = string.Empty },
                canonical with { Fixture = canonical.Fixture with { FixtureOperationId = string.Empty } },
                canonical with { Fixture = canonical.Fixture with { ActionOperationId = string.Empty } },
                canonical with { Fixture = canonical.Fixture with { EvidenceQuery = string.Empty } },
                canonical with { Fixture = canonical.Fixture with { CleanupOperationId = string.Empty } },
                canonical with { RequiredSubcases = canonical.RequiredSubcases.Select((x, index) => index == 0 ? x with { RequiredAction = string.Empty } : x).ToArray() },
                canonical with { RequiredSubcases = canonical.RequiredSubcases.Select((x, index) => index == 0 ? x with { EvidenceSource = string.Empty } : x).ToArray() },
                canonical with { RequiredSubcases = canonical.RequiredSubcases.Select((x, index) => index == 0 ? x with { ExpectedTerminalOutcome = string.Empty } : x).ToArray() }
            };
            if (canonical.Fixture.FixtureDdl.Count > 0)
            {
                var fixtureDdlRemoved = canonical with { Fixture = canonical.Fixture with { FixtureDdl = canonical.Fixture.FixtureDdl.Skip(1).ToArray() } };
                Assert.NotEqual(Rev869BLifecycleControllerClient.ExactContractSha256(canonical),
                    Rev869BLifecycleControllerClient.ExactContractSha256(fixtureDdlRemoved));
                Assert.Throws<ArgumentException>(() => Rev869BLifecycleControllerClient.ValidateContract(fixtureDdlRemoved));
            }
            Assert.All(mutations, mutation =>
            {
                Assert.NotEqual(Rev869BLifecycleControllerClient.ExactContractSha256(canonical),
                    Rev869BLifecycleControllerClient.ExactContractSha256(mutation));
                Assert.Throws<ArgumentException>(() => Rev869BLifecycleControllerClient.ValidateContract(mutation));
            });
            Assert.StartsWith(canonical.ScenarioId + ":", first.SubcaseId, StringComparison.Ordinal);
        }
    }
    private static async Task RunAsync(Rev869BLifecycleControllerClient.AcceptanceContract contract)
    {
        await using var controller = Rev869BLifecycleControllerClient.Create();
        var evidence = await controller.RunAcceptanceScenarioAsync(contract);
        Assert.Equal(contract.ScenarioId, evidence.ScenarioId);
        Assert.Equal(contract.Setup, evidence.Setup);
        Assert.Equal(contract.Fixture.FixtureOperationId, evidence.FixtureOperationId);
        Assert.Equal(contract.Fixture.ActionOperationId, evidence.ActionOperationId);
        Assert.Equal(contract.Fixture.EvidenceQuery, evidence.EvidenceQuery);
        Assert.Equal(contract.Fixture.CleanupOperationId, evidence.CleanupOperationId);
        Assert.Equal(contract.Action, evidence.Action);
        Assert.Equal(contract.ExpectedInitialState, evidence.InitialState);
        Assert.Equal(contract.ExpectedFinalState, evidence.FinalState);
        Assert.Equal(contract.ExpectedAffectedRows, evidence.AffectedRows);
        Assert.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, evidence.DatabaseName, StringComparison.Ordinal);
        Assert.All(new[] { evidence.TargetInstanceSha256, evidence.FixtureSha256, evidence.BeforeSha256,
            evidence.AfterSha256, evidence.DurableEvidenceSha256, evidence.CleanupEvidenceSha256 },
            value => Assert.Matches("^[0-9a-f]{64}$", value));
        Assert.Equal(7, new[] { evidence.LeaseId, evidence.FixtureId, evidence.CommandId, evidence.AuthorizationId,
            evidence.AttemptId, evidence.DurableEvidenceId, evidence.CleanupEvidenceId }.Distinct().Count());
        Assert.Equal(contract.RequiredSubcases.Count, evidence.Subcases.Count);
        Assert.All(evidence.Subcases, subcase =>
        {
            var required = Assert.Single(contract.RequiredSubcases, x => x.SubcaseId == subcase.SubcaseId);
            Assert.Equal(required.RequiredAction, subcase.ActionPerformed);
            Assert.Equal(required.EvidenceSource, subcase.EvidenceSource);
            Assert.Equal(required.ExpectedInitialState, subcase.PreState);
            Assert.Equal(required.ExpectedFinalState, subcase.PostState);
            Assert.Equal(required.ExpectedBeforeCount, subcase.PreCount);
            Assert.Equal(required.ExpectedAfterCount, subcase.PostCount);
            Assert.Equal(required.ExpectedSqlState, subcase.SqlState);
            Assert.Equal(required.ExpectedDatabaseObject, subcase.DatabaseObject);
            Assert.Equal(required.ExpectedAffectedRows, subcase.AffectedRows);
            Assert.Equal(required.ExpectedTerminalOutcome, subcase.TerminalOutcome);
            Assert.NotEqual(Guid.Empty, subcase.EvidenceId);
            Assert.True(subcase.ActionReached);
            Assert.True(subcase.Durable);
        });
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
