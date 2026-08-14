namespace SESS.NexaERP.Tests;

// Future PostgreSQL execution only. Each scenario delegates to a complete owned-fixture implementation
// in Rev869BCorrection17PostgresScenarios. Discovery/compilation never opens PostgreSQL.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BCorrection14PostgresDesignTests
{
    [Fact] public Task ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency() => Rev869BCorrection17PostgresScenarios.LifecycleTraceAsync(nameof(ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency));
    [Fact] public Task HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable() => Rev869BCorrection17PostgresScenarios.LifecycleTraceAsync(nameof(HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable));
    [Fact] public Task FilesystemEvidenceAloneIsRejected() => Rev869BCorrection17PostgresScenarios.FilesystemOnlyRejectedAsync(nameof(FilesystemEvidenceAloneIsRejected));
    [Fact] public Task ControlPlaneAndTargetMarkerMismatchIsRejected() => Rev869BCorrection17PostgresScenarios.MutatedLeaseRejectedAsync(nameof(ControlPlaneAndTargetMarkerMismatchIsRejected), false);
    [Fact] public Task WrongStaleOrDuplicateRunLeaseIsRejected() => Rev869BCorrection17PostgresScenarios.MutatedLeaseRejectedAsync(nameof(WrongStaleOrDuplicateRunLeaseIsRejected), true);
    [Fact] public Task RecoveryApprovalIssuerIsRequiredAndValidated() => Rev869BCorrection17PostgresScenarios.RecoveryDenialAsync(nameof(RecoveryApprovalIssuerIsRequiredAndValidated), "issuer");
    [Fact] public Task WrongRecoveryPreStateIsRejected() => Rev869BCorrection17PostgresScenarios.RecoveryDenialAsync(nameof(WrongRecoveryPreStateIsRejected), "state");
    [Fact] public Task ExpiredRecoveryApprovalIsRejected() => Rev869BCorrection17PostgresScenarios.RecoveryDenialAsync(nameof(ExpiredRecoveryApprovalIsRejected), "expiry");
    [Fact] public Task ReplayedRecoveryApprovalIsRejected() => Rev869BCorrection17PostgresScenarios.RecoveryReplayAsync(nameof(ReplayedRecoveryApprovalIsRejected));
    [Fact] public Task FailedRecoveryPermanentlyConsumesApproval() => Rev869BCorrection17PostgresScenarios.RecoveryReplayAsync(nameof(FailedRecoveryPermanentlyConsumesApproval));
    [Fact] public Task DurableRecoveryOutcomeIsRecorded() => Rev869BCorrection17PostgresScenarios.RecoveryDenialAsync(nameof(DurableRecoveryOutcomeIsRecorded), "post-state");
    [Fact] public Task PurgeRequiresFreshPerExecutionApproval() => Rev869BCorrection17PostgresScenarios.MissingPurgeApprovalAsync(nameof(PurgeRequiresFreshPerExecutionApproval));
    [Fact] public Task WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected() => Rev869BCorrection17PostgresScenarios.WrongPurgeBindingsAsync(nameof(WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected));
    [Fact] public Task ReplayedOrConcurrentPurgeApprovalIsRejected() => Rev869BCorrection17PostgresScenarios.ConcurrentPurgeAsync(nameof(ReplayedOrConcurrentPurgeApprovalIsRejected));
    [Fact] public Task ZeroRowPurgeRecordsExactEvidence() => Rev869BCorrection17PostgresScenarios.ZeroRowPurgeAsync(nameof(ZeroRowPurgeRecordsExactEvidence), false);
    [Fact] public Task PartialOrFailingPurgeRecordsFailureEvidence() => Rev869BCorrection17PostgresScenarios.PurgeFailureAsync(nameof(PartialOrFailingPurgeRecordsFailureEvidence), false);
    [Fact] public Task PurgeCountMismatchRollsBackAndFailsClosed() => Rev869BCorrection17PostgresScenarios.PurgeFailureAsync(nameof(PurgeCountMismatchRollsBackAndFailsClosed), true);
    [Fact] public Task PurgeOwnerInsertsAuditOnlyThroughApprovedRoute() => Rev869BCorrection17PostgresScenarios.PurgeDirectDmlDeniedAsync(nameof(PurgeOwnerInsertsAuditOnlyThroughApprovedRoute), true);
    [Fact] public Task PurgeOwnerCannotDirectlyMutateProtectedTables() => Rev869BCorrection17PostgresScenarios.PurgeDirectDmlDeniedAsync(nameof(PurgeOwnerCannotDirectlyMutateProtectedTables), false);
    [Fact] public Task TemporaryPurgePreservesDurablePerCommandAudit() => Rev869BCorrection17PostgresScenarios.PurgePreservesDurableAsync(nameof(TemporaryPurgePreservesDurablePerCommandAudit));
    [Fact] public Task RuntimeCannotUpdateDeleteOrExportDurableCommandAudit() => Rev869BCorrection17PostgresScenarios.RuntimeLedgerDeniedAsync(nameof(RuntimeCannotUpdateDeleteOrExportDurableCommandAudit));
    [Fact] public Task AuditInsertionFailureBlocksProtectedCommandAcceptance() => Rev869BCorrection17PostgresScenarios.AuditFailureBlocksAsync(nameof(AuditInsertionFailureBlocksProtectedCommandAcceptance));
    [Fact] public Task ExactSqlStateAndDatabaseObjectAreAsserted() => Rev869BCorrection17PostgresScenarios.ImmutableTriggerAsync(nameof(ExactSqlStateAndDatabaseObjectAreAsserted));
    [Fact] public Task ZeroRowFalsePositiveIsProhibited() => Rev869BCorrection17PostgresScenarios.ZeroRowPurgeAsync(nameof(ZeroRowFalsePositiveIsProhibited), true);
    [Fact] public Task ActorAndVerifierUseIndependentConnectionsAndContexts() => Rev869BCorrection17PostgresScenarios.IndependentBackendsAsync(nameof(ActorAndVerifierUseIndependentConnectionsAndContexts));
}
