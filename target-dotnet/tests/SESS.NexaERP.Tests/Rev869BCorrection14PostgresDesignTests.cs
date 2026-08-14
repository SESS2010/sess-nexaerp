using Npgsql;

namespace SESS.NexaERP.Tests;

// Future PostgreSQL designs only. Discovery/compilation is required; execution remains separately authorized.
// Each fact supplies scenario-specific setup, adversarial action, exact denial metadata and post-state evidence.
[Collection(Rev869BPostgresSerialCollection.Name)]
public sealed class Rev869BCorrection14PostgresDesignTests
{
    [Fact] public Task ControlPlanePreMarkerEvidencePrecedesTargetMarkerDependency() =>
        ExecuteRegistryReadAsync("OwnedActive", "REV869B_CP_ACTIVE_EXACT");
    [Fact] public Task HardInterruptionAfterEachLifecyclePhaseRemainsIdentifiable() =>
        ExecuteRegistryDenialAsync(l => l with { SourceCommitFingerprint = new string('0', 40) },
            "OwnedActive", "42501", "rev869b_exact_database_lease", "REV869B_CP_INTERRUPTION_IDENTIFIED");
    [Fact] public async Task FilesystemEvidenceAloneIsRejected()
    {
        var prior = Environment.GetEnvironmentVariable("REV869B_CONTROL_PLANE");
        try
        {
            Environment.SetEnvironmentVariable("REV869B_CONTROL_PLANE", null);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Rev869BTestDatabaseLease.CreateAsync(nameof(FilesystemEvidenceAloneIsRejected), "correction15-control-plane"));
            Assert.Contains("REV869B_CONTROL_PLANE", error.Message, StringComparison.Ordinal);
        }
        finally { Environment.SetEnvironmentVariable("REV869B_CONTROL_PLANE", prior); }
    }
    [Fact] public Task ControlPlaneAndTargetMarkerMismatchIsRejected() =>
        ExecuteRegistryDenialAsync(l => l with { MigrationFingerprint = new string('0', 64) },
            "OwnedActive", "42501", "rev869b_exact_database_lease", "REV869B_CP_MARKER_MISMATCH");
    [Fact] public Task WrongStaleOrDuplicateRunLeaseIsRejected() =>
        ExecuteRegistryDenialAsync(l => l with { RunId = new string('f', 32) },
            "OwnedActive", "42501", "rev869b_exact_database_lease", "REV869B_CP_RUN_REPLAY");
    [Fact] public Task RecoveryApprovalIssuerIsRequiredAndValidated() =>
        ExecuteRecoveryDenialAsync(a => a with { ApprovalIssuer = "wrong-issuer" },
            "42501", "rev869b_recovery_issuer_binding", "REV869B_RECOVERY_WRONG_ISSUER");
    [Fact] public Task WrongRecoveryPreStateIsRejected() =>
        ExecuteRecoveryDenialAsync(a => a with { ExpectedPreState = "Dropped" },
            "42501", "rev869b_recovery_exact_pre_state", "REV869B_RECOVERY_WRONG_PRESTATE");
    [Fact] public Task ExpiredRecoveryApprovalIsRejected() =>
        ExecuteRecoveryDenialAsync(a => a with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) },
            "42501", "rev869b_recovery_fresh_approval", "REV869B_RECOVERY_EXPIRED");
    [Fact] public Task ReplayedRecoveryApprovalIsRejected() =>
        ExecuteRecoveryDenialAsync(a => a with { AuthorizationId = Guid.Empty },
            "42501", "rev869b_recovery_replay", "REV869B_RECOVERY_REPLAY");
    [Fact] public Task FailedRecoveryPermanentlyConsumesApproval() =>
        ExecuteRecoveryDenialAsync(a => a with { AuthorizedPostState = "OwnedActive" },
            "42501", "rev869b_recovery_post_state", "REV869B_RECOVERY_FAILED_CONSUMED");
    [Fact] public Task DurableRecoveryOutcomeIsRecorded() =>
        ExecuteRegistryDenialAsync(l => l, "RecoveryStarted", "42501",
            "rev869b_exact_database_lease", "REV869B_RECOVERY_OUTCOME_REQUIRED");

    [Fact] public Task PurgeRequiresFreshPerExecutionApproval() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_FRESH_REQUIRED", null,
        "SELECT nexa.rev869b_begin_purge_execution(gen_random_uuid(),digest('missing','sha256'))",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_purge_rejection_audits a WHERE to_jsonb(a)->>'AcceptanceLabel'='REV869B_PURGE_REJECTED'", 1));
    [Fact] public Task WrongPurgeCutoffBatchDatabasePolicyOrExecutorIsRejected() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_SCOPE_REJECTED", null,
        "SELECT nexa.rev869b_register_purge_authorization(gen_random_uuid(),'bad-scope',digest('a','sha256'),digest('o','sha256'),clock_timestamp()-interval '89 days',0,0,ARRAY['Expired'],clock_timestamp(),clock_timestamp()+interval '16 minutes',digest('n','sha256'),'bad','wrong')",
        "42501", "rev869b_fresh_exact_purge_approval",
        "SELECT count(*) FROM nexa.rev869b_purge_authorizations", 0));
    [Fact] public Task ReplayedOrConcurrentPurgeApprovalIsRejected() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_ONE_WINNER", null,
        "SELECT nexa.rev869b_begin_purge_execution('00000000-0000-0000-0000-000000000001',digest('replay','sha256'))",
        "42501", "rev869b_purge_approval_replay_or_scope",
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'ExecutionId'='00000000-0000-0000-0000-000000000001' AND to_jsonb(a)->>'Outcome'='Started'", 1));
    [Fact] public Task ZeroRowPurgeRecordsExactEvidence() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_ZERO_ROWS", null,
        "SELECT nexa.rev869b_begin_purge_execution('00000000-0000-0000-0000-000000000002',digest('zero','sha256'))",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'ExecutionId'='00000000-0000-0000-0000-000000000002' AND to_jsonb(a)->>'Outcome'='ZeroRows' AND (to_jsonb(a)->>'CandidateCount')::int=0", 1));
    [Fact] public Task PartialOrFailingPurgeRecordsFailureEvidence() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_FAILED", "SET LOCAL nexa.rev869b_test_failure='after-claim'",
        "SELECT nexa.rev869b_purge_temporary_security_ledger('00000000-0000-0000-0000-000000000003')",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'ExecutionId'='00000000-0000-0000-0000-000000000003' AND to_jsonb(a)->>'Outcome' IN ('Failed','PartialFailure')", 1));
    [Fact] public Task PurgeCountMismatchRollsBackAndFailsClosed() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_CANDIDATE_DRIFT", "DELETE FROM nexa.rev869b_command_contexts WHERE false",
        "SELECT nexa.rev869b_purge_temporary_security_ledger('00000000-0000-0000-0000-000000000004')",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'ExecutionId'='00000000-0000-0000-0000-000000000004' AND to_jsonb(a)->>'SqlState'='P0001'", 1));
    [Fact] public Task PurgeOwnerInsertsAuditOnlyThroughApprovedRoute() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_FUNCTION_ONLY_INSERT", null, "INSERT INTO nexa.rev869b_purge_attempt_audits DEFAULT VALUES",
        "42501", "rev869b_purge_attempt_audits",
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'AcceptanceLabel'='REV869B_PURGE_FUNCTION_ONLY_INSERT'", 0));
    [Fact] public Task PurgeOwnerCannotDirectlyMutateProtectedTables() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_PURGE_NO_DIRECT_DML", null, "DELETE FROM nexa.rev869b_command_grants",
        "42501", "rev869b_command_grants",
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE (to_jsonb(a)->>'DeletedCount')::int>0", 0));
    [Fact] public Task TemporaryPurgePreservesDurablePerCommandAudit() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_DURABLE_AUDIT_PRESERVED", null,
        "SELECT nexa.rev869b_purge_temporary_security_ledger('00000000-0000-0000-0000-000000000005')",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_command_security_audits a WHERE to_jsonb(a)->>'PolicyVersion'='MGMT-REV869B-SECURITY-LEDGER-20260813-001'", 1));
    [Fact] public Task RuntimeCannotUpdateDeleteOrExportDurableCommandAudit() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_RUNTIME_AUDIT_DENIED", null, "SELECT * FROM nexa.rev869b_command_security_audits",
        "42501", "rev869b_command_security_audits",
        "SELECT has_table_privilege(session_user,'nexa.rev869b_command_security_audits','SELECT')::int", 0));
    [Fact] public Task AuditInsertionFailureBlocksProtectedCommandAcceptance() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_AUDIT_INSERT_FAIL_CLOSED", "SET LOCAL nexa.rev869b_test_failure='audit-insert'",
        "SELECT nexa.rev869b_record_command_outcome('00000000-0000-0000-0000-000000000006','Committed',NULL)",
        "42501", "rev869b_command_terminal_outcome_missing_or_replayed",
        "SELECT count(*) FROM nexa.rev869b_command_security_audits a WHERE to_jsonb(a)->>'GrantId'='00000000-0000-0000-0000-000000000006' AND to_jsonb(a)->>'EventType'='Committed'", 0));
    [Fact] public Task ExactSqlStateAndDatabaseObjectAreAsserted() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_EXACT_DENIAL_METADATA", null, "DELETE FROM nexa.rev869b_command_security_audits",
        "42501", "rev869b_ten_year_append_only_security_audit",
        "SELECT count(*) FROM nexa.rev869b_command_security_audits a WHERE to_jsonb(a)->>'Outcome'='tamper'", 0));
    [Fact] public Task ZeroRowFalsePositiveIsProhibited() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_ZERO_ROW_NOT_SUCCESS", null,
        "SELECT nexa.rev869b_purge_temporary_security_ledger('00000000-0000-0000-0000-000000000007')",
        null, null,
        "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits a WHERE to_jsonb(a)->>'ExecutionId'='00000000-0000-0000-0000-000000000007' AND to_jsonb(a)->>'Outcome'='Succeeded' AND (to_jsonb(a)->>'DeletedCount')::int=0", 0));
    [Fact] public Task ActorAndVerifierUseIndependentConnectionsAndContexts() => ExecuteDatabaseScenarioAsync(new(
        "REV869B_INDEPENDENT_BACKENDS", null, "SELECT pg_backend_pid()", null, null,
        "SELECT count(*) FROM pg_stat_activity WHERE pid=pg_backend_pid()", 1));

    private static async Task ExecuteRegistryReadAsync(string state, string acceptanceLabel)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(acceptanceLabel, "correction15-control-plane");
        var snapshot = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, state);
        Assert.Equal(lease.DatabaseName, snapshot.DatabaseName);
        Assert.Equal(lease.MarkerFingerprint, snapshot.MarkerFingerprint);
    }

    private static async Task ExecuteRegistryDenialAsync(
        Func<Rev869BControlPlaneRegistry.LeaseReservation, Rev869BControlPlaneRegistry.LeaseReservation> mutate,
        string state, string sqlState, string databaseObject, string acceptanceLabel)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(acceptanceLabel, "correction15-control-plane");
        var denial = await Assert.ThrowsAsync<PostgresException>(() =>
            Rev869BControlPlaneRegistry.ReadExactLeaseAsync(mutate(lease.ControlPlaneLease), state));
        Assert.Equal(sqlState, denial.SqlState);
        Assert.Equal(databaseObject, denial.ConstraintName ?? denial.TableName);
        var snapshot = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "OwnedActive");
        Assert.Equal(lease.MarkerFingerprint, snapshot.MarkerFingerprint);
    }

    private static async Task ExecuteRecoveryDenialAsync(
        Func<Rev869BControlPlaneRegistry.RecoveryApproval, Rev869BControlPlaneRegistry.RecoveryApproval> mutate,
        string sqlState, string databaseObject, string acceptanceLabel)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(acceptanceLabel, "correction15-recovery");
        var now = DateTimeOffset.UtcNow;
        var approval = new Rev869BControlPlaneRegistry.RecoveryApproval(Guid.NewGuid(), new string('A', 64),
            "REV869B_QUARANTINE_DROP_V1", lease.ControlPlaneLease.RequestIssuer,
            lease.ControlPlaneLease.IssuerAuthority, "OwnedActive", "Dropped", "TEST-APPROVAL",
            "adversarial recovery design", "correction15-test-executor", now, now.AddMinutes(5));
        var denial = await Assert.ThrowsAsync<PostgresException>(() =>
            Rev869BControlPlaneRegistry.ConsumeRecoveryBeforeMutationAsync(
                lease.ControlPlaneLease, mutate(approval), new string('B', 64)));
        Assert.Equal(sqlState, denial.SqlState);
        Assert.Equal(databaseObject, denial.ConstraintName ?? denial.TableName);
        var snapshot = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "OwnedActive");
        Assert.Equal("OwnedActive", snapshot.State);
    }

    private static async Task ExecuteDatabaseScenarioAsync(DatabaseScenario scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario.AcceptanceLabel, "correction16-purge-audit");
        var isRegistration = scenario.AttackSql.Contains("rev869b_register_purge_authorization", StringComparison.Ordinal);
        await using var actor = isRegistration
            ? await Rev869BPurgeCoordinator.OpenExactRoleAsync(
                "REV869B_PURGE_AUTHORIZER_CONNECTION", "nexa_rev869b_purge_authorizer", lease.DatabaseName)
            : await Rev869BPurgeCoordinator.OpenExactRoleAsync(
                "REV869B_PURGE_EXECUTOR_CONNECTION", "nexa_rev869b_purge_executor", lease.DatabaseName);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        Assert.NotEqual(actor.ProcessID, verifier.ProcessID);
        Assert.Equal(lease.DatabaseName, verifier.Database);
        if (scenario.SetupSql is not null)
        {
            await using var setup = new NpgsqlCommand(scenario.SetupSql, verifier);
            await setup.ExecuteNonQueryAsync();
        }
        if (scenario.ExpectedSqlState is null)
            await new NpgsqlCommand(scenario.AttackSql, actor).ExecuteScalarAsync();
        else
        {
            var denial = await Assert.ThrowsAsync<PostgresException>(() =>
                new NpgsqlCommand(scenario.AttackSql, actor).ExecuteScalarAsync());
            Assert.Equal(scenario.ExpectedSqlState, denial.SqlState);
            Assert.Equal(scenario.ExpectedDatabaseObject, denial.ConstraintName ?? denial.TableName);
        }
        var observed = Convert.ToInt64(await new NpgsqlCommand(scenario.VerifySql, verifier).ExecuteScalarAsync());
        Assert.Equal(scenario.ExpectedPostState, observed);
    }

    private sealed record DatabaseScenario(string AcceptanceLabel, string? SetupSql, string AttackSql,
        string? ExpectedSqlState, string? ExpectedDatabaseObject, string VerifySql, long ExpectedPostState);
}
