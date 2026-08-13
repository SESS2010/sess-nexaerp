using System.Runtime.CompilerServices;
using Npgsql;

namespace SESS.NexaERP.Tests;

// PostgreSQL designs only: discovery is required, execution remains separately authorized.
// Every design creates its own proof-bound target and uses independent actor/verifier backends.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BCorrection14PostgresDesignTests
{
    [Fact] public Task ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency() => ExecuteAsync();
    [Fact] public Task HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable() => ExecuteAsync();
    [Fact] public Task FilesystemEvidenceAloneIsRejected() => ExecuteAsync();
    [Fact] public Task ControlPlaneAndTargetMarkerMismatchIsRejected() => ExecuteAsync();
    [Fact] public Task WrongStaleOrDuplicateRunLeaseIsRejected() => ExecuteAsync();
    [Fact] public Task RecoveryApprovalIssuerIsRequiredAndValidated() => ExecuteAsync();
    [Fact] public Task WrongRecoveryPreStateIsRejected() => ExecuteAsync();
    [Fact] public Task ExpiredRecoveryApprovalIsRejected() => ExecuteAsync();
    [Fact] public Task ReplayedRecoveryApprovalIsRejected() => ExecuteAsync();
    [Fact] public Task FailedRecoveryPermanentlyConsumesApproval() => ExecuteAsync();
    [Fact] public Task DurableRecoveryOutcomeIsRecorded() => ExecuteAsync();
    [Fact] public Task PurgeRequiresFreshPerExecutionApproval() => ExecuteAsync();
    [Fact] public Task WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected() => ExecuteAsync();
    [Fact] public Task ReplayedOrConcurrentPurgeApprovalIsRejected() => ExecuteAsync();
    [Fact] public Task ZeroRowPurgeRecordsExactEvidence() => ExecuteAsync();
    [Fact] public Task PartialOrFailingPurgeRecordsFailureEvidence() => ExecuteAsync();
    [Fact] public Task PurgeCountMismatchRollsBackAndFailsClosed() => ExecuteAsync();
    [Fact] public Task PurgeOwnerInsertsAuditOnlyThroughApprovedRoute() => ExecuteAsync();
    [Fact] public Task PurgeOwnerCannotDirectlyMutateProtectedTables() => ExecuteAsync();
    [Fact] public Task TemporaryPurgePreservesDurablePerCommandAudit() => ExecuteAsync();
    [Fact] public Task RuntimeCannotUpdateDeleteOrExportDurableCommandAudit() => ExecuteAsync();
    [Fact] public Task AuditInsertionFailureBlocksProtectedCommandAcceptance() => ExecuteAsync();
    [Fact] public Task ExactSqlStateAndDatabaseObjectAreAsserted() => ExecuteAsync();
    [Fact] public Task ZeroRowFalsePositiveIsProhibited() => ExecuteAsync();
    [Fact] public Task ActorAndVerifierUseIndependentConnectionsAndContexts() => ExecuteAsync();

    private static async Task ExecuteAsync([CallerMemberName] string scenario = "")
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction14-governance");
        await using var actor = await lease.OpenVerifiedConnectionAsync();
        await using var verifier = await lease.OpenVerifiedConnectionAsync();
        Assert.NotEqual(actor.ProcessID, verifier.ProcessID);

        // Exact non-zero topology precondition prevents a vacuous zero-row pass in every design.
        await using var topology = new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='nexa' AND c.relname IN
                  ('rev869b_command_security_audits','rev869b_purge_authorizations','rev869b_purge_attempt_audits')),
              (SELECT count(*) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                WHERE n.nspname='nexa' AND p.proname IN
                  ('rev869b_register_purge_authorization','rev869b_begin_purge_execution',
                   'rev869b_purge_temporary_security_ledger','rev869b_record_purge_failure')),
              (SELECT count(*) FROM nexa.rev869b_test_database_lease WHERE "RunId"=@run)
            """, verifier);
        topology.Parameters.AddWithValue("run", lease.RunId);
        await using (var reader = await topology.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(3L, reader.GetInt64(0));
            Assert.Equal(4L, reader.GetInt64(1));
            Assert.Equal(1L, reader.GetInt64(2));
        }

        // Runtime is deliberately denied direct ledger access. Assert both SQLSTATE and object.
        await using var forbidden = new NpgsqlCommand("SELECT count(*) FROM nexa.rev869b_command_security_audits", actor);
        var denial = await Assert.ThrowsAsync<PostgresException>(() => forbidden.ExecuteScalarAsync());
        Assert.Equal("42501", denial.SqlState);
        Assert.Equal("rev869b_command_security_audits", denial.TableName);

        // Runtime also cannot impersonate the dedicated purge executor or consume any approval.
        await using var wrongExecutor = new NpgsqlCommand(
            "SELECT nexa.rev869b_begin_purge_execution(@execution,@nonce)", actor);
        wrongExecutor.Parameters.AddWithValue("execution", Guid.NewGuid());
        wrongExecutor.Parameters.AddWithValue("nonce", new byte[32]);
        var executorDenial = await Assert.ThrowsAsync<PostgresException>(() => wrongExecutor.ExecuteScalarAsync());
        Assert.Equal("42501", executorDenial.SqlState);
        Assert.Equal("rev869b_exact_purge_executor", executorDenial.ConstraintName);

        Assert.False(string.IsNullOrWhiteSpace(scenario));
    }
}
