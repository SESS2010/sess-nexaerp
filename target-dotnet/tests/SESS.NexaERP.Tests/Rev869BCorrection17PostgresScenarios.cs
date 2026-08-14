using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace SESS.NexaERP.Tests;

internal static class Rev869BCorrection17PostgresScenarios
{
    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    internal static async Task LifecycleTraceAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-lifecycle");
        var states = await Rev869BControlPlaneRegistry.ReadTransitionStatesAsync(lease.ControlPlaneLease);
        Assert.Equal(new[] { "PreCreate", "Created", "Provisioned", "Executing" }, states);
        Assert.Equal(states.Length, states.Distinct(StringComparer.Ordinal).Count());
        var snapshot = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "Executing");
        Assert.Equal(lease.MarkerFingerprint, snapshot.MarkerFingerprint);
    }

    internal static async Task FilesystemOnlyRejectedAsync(string scenario)
    {
        var prior = Environment.GetEnvironmentVariable("REV869B_CONTROL_PLANE");
        try
        {
            Environment.SetEnvironmentVariable("REV869B_CONTROL_PLANE", null);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-control-plane"));
            Assert.Contains("REV869B_CONTROL_PLANE", error.Message, StringComparison.Ordinal);
        }
        finally { Environment.SetEnvironmentVariable("REV869B_CONTROL_PLANE", prior); }
    }

    internal static async Task MutatedLeaseRejectedAsync(string scenario, bool mutateRun)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-lifecycle");
        var changed = mutateRun
            ? lease.ControlPlaneLease with { RunId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant() }
            : lease.ControlPlaneLease with { MigrationFingerprint = new string('0', 64) };
        await Assert.ThrowsAnyAsync<Exception>(() =>
            Rev869BControlPlaneRegistry.ReadExactLeaseAsync(changed, "Executing"));
        var exact = await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "Executing");
        Assert.Equal(lease.DatabaseName, exact.DatabaseName);
    }

    private static Rev869BControlPlaneRegistry.RecoveryApproval Approval(
        Rev869BTestDatabaseLease lease, Guid? authorizationId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new(authorizationId ?? Guid.NewGuid(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            "REV869B_QUARANTINE_DROP_V1", lease.ControlPlaneLease.RequestIssuer,
            lease.ControlPlaneLease.IssuerAuthority, "Executing", "Dropped",
            "TEST-RECOVERY-" + Guid.NewGuid().ToString("N"), "Exact controlled recovery test",
            "nexa_rev869b_recovery_administrator", now, now.AddMinutes(5));
    }

    internal static async Task RecoveryDenialAsync(string scenario, string mutation)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-recovery");
        var approval = Approval(lease);
        approval = mutation switch
        {
            "issuer" => approval with { ApprovalIssuer = "substituted-issuer" },
            "state" => approval with { ExpectedPreState = "Dropped" },
            "expiry" => approval with { ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1) },
            "post-state" => approval with { AuthorizedPostState = "Executing" },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        await Assert.ThrowsAnyAsync<Exception>(() => Rev869BControlPlaneRegistry.ConsumeRecoveryBeforeMutationAsync(
            lease.ControlPlaneLease, approval, Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
        Assert.Equal("Executing",
            (await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "Executing")).State);
    }

    internal static async Task RecoveryReplayAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-recovery");
        var approval = Approval(lease);
        var target = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            Rev869BControlPlaneRegistry.ConsumeRecoveryBeforeMutationAsync(lease.ControlPlaneLease, approval, target));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            Rev869BControlPlaneRegistry.ConsumeRecoveryBeforeMutationAsync(lease.ControlPlaneLease, approval, target));
        Assert.Equal("Executing",
            (await Rev869BControlPlaneRegistry.ReadExactLeaseAsync(lease.ControlPlaneLease, "Executing")).State);
    }

    private static async Task<(Rev869BTestDatabaseLease Lease, Guid Execution, byte[] Organization, byte[] Nonce)>
        AuthorizedPurgeAsync(string scenario)
    {
        var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-purge");
        try
        {
            var execution = Guid.NewGuid();
            var organization = Hash(scenario + ":organization");
            var nonce = Hash(scenario + ":nonce");
            await Rev869BPurgeCoordinator.RegisterAsync(lease.DatabaseName, execution, organization,
                Hash(scenario + ":approval"), nonce, DateTimeOffset.UtcNow, 25, 25);
            return (lease, execution, organization, nonce);
        }
        catch { await lease.DisposeAsync(); throw; }
    }

    internal static async Task MissingPurgeApprovalAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-purge");
        var result = await Rev869BPurgeCoordinator.BeginAsync(lease.DatabaseName, Guid.NewGuid(), Hash("missing"));
        Assert.Equal("Rejected", result.Phase);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        var count = Convert.ToInt64(await new NpgsqlCommand(
            "SELECT count(*) FROM nexa.rev869b_purge_rejection_audits WHERE \"ReasonCategory\"='MissingAuthorization'", verifier).ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    internal static async Task WrongPurgeBindingsAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-purge");
        await using var authorizer = await Rev869BPurgeCoordinator.OpenExactRoleAsync(
            "REV869B_PURGE_AUTHORIZER_CONNECTION", "nexa_rev869b_purge_authorizer", lease.DatabaseName);
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_register_purge_authorization(
              @execution,'invalid',@approval,@organization,clock_timestamp()-interval '89 days',0,0,
              ARRAY['Expired']::text[],clock_timestamp(),clock_timestamp()+interval '16 minutes',@nonce,
              'invalid','wrong.destination')
            """, authorizer);
        command.Parameters.AddWithValue("execution", Guid.NewGuid());
        command.Parameters.AddWithValue("approval", Hash("approval"));
        command.Parameters.AddWithValue("organization", Hash("organization"));
        command.Parameters.AddWithValue("nonce", Hash("nonce"));
        var denial = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("42501", denial.SqlState);
        Assert.Equal("rev869b_fresh_exact_purge_approval_required", denial.ConstraintName);
    }

    internal static async Task ConcurrentPurgeAsync(string scenario)
    {
        var state = await AuthorizedPurgeAsync(scenario);
        await using var lease = state.Lease;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Rev869BPurgeCoordinator.PhaseResult> Competitor()
        {
            await gate.Task;
            return await Rev869BPurgeCoordinator.BeginAsync(lease.DatabaseName, state.Execution, state.Nonce);
        }
        var first = Competitor();
        var second = Competitor();
        gate.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results, x => x.Phase != "Rejected");
        Assert.Single(results, x => x.Phase == "Rejected");
    }

    internal static async Task ZeroRowPurgeAsync(string scenario, bool prohibitFalseSuccess)
    {
        var state = await AuthorizedPurgeAsync(scenario);
        await using var lease = state.Lease;
        var result = await Rev869BPurgeCoordinator.BeginAsync(lease.DatabaseName, state.Execution, state.Nonce);
        Assert.Equal("ZeroRows", result.Phase);
        Assert.Equal(0, result.Value);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        await using var evidence = new NpgsqlCommand(
            "SELECT \"AcceptanceLabel\" FROM nexa.rev869b_purge_attempt_audits WHERE \"ExecutionId\"=@id AND \"Outcome\"='ZeroRows'", verifier);
        evidence.Parameters.AddWithValue("id", state.Execution);
        Assert.Equal("REV869B_PURGE_ZERO_ROWS", Convert.ToString(await evidence.ExecuteScalarAsync()));
        if (prohibitFalseSuccess)
        {
            await using var falsePositive = new NpgsqlCommand(
                "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits WHERE \"ExecutionId\"=@id AND \"Outcome\"='Succeeded'", verifier);
            falsePositive.Parameters.AddWithValue("id", state.Execution);
            Assert.Equal(0, Convert.ToInt64(await falsePositive.ExecuteScalarAsync()));
        }
    }

    private static async Task<Guid> InsertExpiredGrantFixtureAsync(
        Rev869BTestDatabaseLease lease, byte[] organization, string terminalEvent)
    {
        await using var owner = new NpgsqlConnection(lease.OwnerConnectionString);
        await owner.OpenAsync();
        var grant = Guid.NewGuid();
        await using var fixture = new NpgsqlCommand("""
            INSERT INTO nexa.rev869b_command_grants(
              "GrantId","IssuerPrincipal","RuntimePrincipal","TargetBackendPid","TargetTransactionId",
              "OrganizationFingerprint","ActorFingerprint","IdentityFingerprint","RoleFingerprint",
              "SlotFingerprints","SlotCount","ClaimSequence","ClaimSequenceStart","IssuedAt","ExpiresAt","ReservedAt")
            VALUES(@grant,current_user,current_user,1,1,@organization,@hash,@hash,@hash,
              '[{"slot":"fixture","semantic":"fixture","ordinal":1}]'::jsonb,1,'rev869b_claim_seq_001',1,
              clock_timestamp()-interval '92 days',clock_timestamp()-interval '92 days'+interval '10 seconds',
              clock_timestamp()-interval '92 days');
            INSERT INTO nexa.rev869b_command_security_audits(
              "AuditId","GrantId","EventId","EventType","CommandFingerprint","OrganizationFingerprint",
              "ActorFingerprint","IssuerPrincipal","Operation","EntityType","EntityId","ExpectedVersion",
              "CorrelationFingerprint","OccurredAt","Outcome","PolicyVersion")
            VALUES(gen_random_uuid(),@grant,gen_random_uuid(),@event,repeat('a',64),@organization,@hash,
              current_user,'Fixture','Fixture',gen_random_uuid(),1,@hash,clock_timestamp()-interval '91 days',
              'Deterministic retention fixture','MGMT-REV869B-SECURITY-LEDGER-20260813-001');
            """, owner);
        fixture.Parameters.AddWithValue("grant", grant);
        fixture.Parameters.AddWithValue("organization", organization);
        fixture.Parameters.AddWithValue("hash", Hash("fixture"));
        fixture.Parameters.AddWithValue("event", terminalEvent);
        Assert.Equal(2, await fixture.ExecuteNonQueryAsync());
        return grant;
    }

    internal static async Task PurgeFailureAsync(string scenario, bool drift)
    {
        var state = await AuthorizedPurgeAsync(scenario);
        await using var lease = state.Lease;
        var grant = await InsertExpiredGrantFixtureAsync(lease, state.Organization, "Committed");
        var started = await Rev869BPurgeCoordinator.BeginAsync(lease.DatabaseName, state.Execution, state.Nonce);
        Assert.Equal("Started", started.Phase);
        await using (var owner = new NpgsqlConnection(lease.OwnerConnectionString))
        {
            await owner.OpenAsync();
            var faultSql = drift
                ? "UPDATE nexa.rev869b_command_grants SET \"ReservedAt\"=\"ReservedAt\"-interval '1 second' WHERE \"GrantId\"=@grant"
                : """
                  CREATE FUNCTION nexa.rev869b_test_reject_grant_delete() RETURNS trigger LANGUAGE plpgsql AS
                  'BEGIN RAISE EXCEPTION USING ERRCODE=''P0001'',CONSTRAINT=''rev869b_test_delete_fault''; END';
                  CREATE TRIGGER "TR_rev869b_test_delete_fault" BEFORE DELETE ON nexa.rev869b_command_grants
                  FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_test_reject_grant_delete();
                  """;
            await using var fault = new NpgsqlCommand(faultSql, owner);
            fault.Parameters.AddWithValue("grant", grant);
            Assert.True(await fault.ExecuteNonQueryAsync() >= 0);
        }
        var terminal = await Rev869BPurgeCoordinator.ExecuteAsync(lease.DatabaseName, state.Execution);
        Assert.Equal("FailedOrPartialFailure", terminal.Phase);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        await using var preserved = new NpgsqlCommand(
            "SELECT count(*) FROM nexa.rev869b_command_grants WHERE \"GrantId\"=@grant", verifier);
        preserved.Parameters.AddWithValue("grant", grant);
        Assert.Equal(1, Convert.ToInt64(await preserved.ExecuteScalarAsync()));
        await using var terminalEvidence = new NpgsqlCommand(
            "SELECT count(*) FROM nexa.rev869b_purge_attempt_audits WHERE \"ExecutionId\"=@id AND \"Outcome\" IN ('Failed','PartialFailure')", verifier);
        terminalEvidence.Parameters.AddWithValue("id", state.Execution);
        Assert.Equal(1, Convert.ToInt64(await terminalEvidence.ExecuteScalarAsync()));
    }

    internal static async Task PurgeDirectDmlDeniedAsync(string scenario, bool auditTable)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-acl");
        await using var executor = await Rev869BPurgeCoordinator.OpenExactRoleAsync(
            "REV869B_PURGE_EXECUTOR_CONNECTION", "nexa_rev869b_purge_executor", lease.DatabaseName);
        var table = auditTable ? "rev869b_purge_attempt_audits" : "rev869b_command_grants";
        var sql = auditTable
            ? "INSERT INTO nexa.rev869b_purge_attempt_audits DEFAULT VALUES"
            : "DELETE FROM nexa.rev869b_command_grants";
        var denial = await Assert.ThrowsAsync<PostgresException>(() => new NpgsqlCommand(sql, executor).ExecuteNonQueryAsync());
        Assert.Equal("42501", denial.SqlState);
        Assert.Equal(table, denial.TableName);
    }

    internal static async Task PurgePreservesDurableAsync(string scenario)
    {
        var state = await AuthorizedPurgeAsync(scenario);
        await using var lease = state.Lease;
        var grant = await InsertExpiredGrantFixtureAsync(lease, state.Organization, "Committed");
        Assert.Equal("Started",
            (await Rev869BPurgeCoordinator.BeginAsync(lease.DatabaseName, state.Execution, state.Nonce)).Phase);
        Assert.Equal("Succeeded",
            (await Rev869BPurgeCoordinator.ExecuteAsync(lease.DatabaseName, state.Execution)).Phase);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        await using var counts = new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM nexa.rev869b_command_grants WHERE "GrantId"=@grant),
              (SELECT count(*) FROM nexa.rev869b_command_security_audits WHERE "GrantId"=@grant)
            """, verifier);
        counts.Parameters.AddWithValue("grant", grant);
        await using var reader = await counts.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0, reader.GetInt64(0));
        Assert.True(reader.GetInt64(1) >= 1);
        Assert.False(await reader.ReadAsync());
    }

    internal static async Task RuntimeLedgerDeniedAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-acl");
        await using var runtime = await lease.OpenVerifiedConnectionAsync();
        foreach (var table in new[] { "rev869b_command_security_audits",
                     "rev869b_command_consumption_attempt_audits", "rev869b_command_attempt_outcomes" })
        {
            var denial = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await using var command = new NpgsqlCommand($"SELECT * FROM nexa.{table}", runtime);
                await using var reader = await command.ExecuteReaderAsync();
            });
            Assert.Equal("42501", denial.SqlState);
            Assert.Equal(table, denial.TableName);
        }
        var authorization = Guid.NewGuid();
        var nonce = Hash(scenario + ":export-nonce");
        var organization = Hash(scenario + ":export-organization");
        await using (var authorizer = await Rev869BPurgeCoordinator.OpenExactRoleAsync(
            "REV869B_EXPORT_AUTHORIZER_CONNECTION", "nexa_rev869b_security_export_authorizer", lease.DatabaseName))
        {
            await using var approve = new NpgsqlCommand("""
                SELECT nexa.rev869b_register_security_export_authorization(
                  @authorization,@organization,@purpose,
                  ARRAY['AuditId','EventType','CommandFingerprint','Operation','EntityType','EntityId',
                    'ExpectedVersion','OccurredAt','Outcome','FailureCategory']::text[],
                  25,@issued,@expires,@nonce)
                """, authorizer);
            var now = DateTimeOffset.UtcNow;
            approve.Parameters.AddWithValue("authorization", authorization);
            approve.Parameters.AddWithValue("organization", organization);
            approve.Parameters.AddWithValue("purpose", "Independent security investigation");
            approve.Parameters.AddWithValue("issued", now);
            approve.Parameters.AddWithValue("expires", now.AddMinutes(5));
            approve.Parameters.AddWithValue("nonce", nonce);
            await approve.ExecuteNonQueryAsync();
        }
        await using (var exportReader = await Rev869BPurgeCoordinator.OpenExactRoleAsync(
            "REV869B_EXPORT_READER_CONNECTION", "nexa_rev869b_security_export_reader", lease.DatabaseName))
        {
            await using var export = new NpgsqlCommand(
                "SELECT * FROM nexa.rev869b_export_minimized_security_ledger(@authorization,@nonce)", exportReader);
            export.Parameters.AddWithValue("authorization", authorization);
            export.Parameters.AddWithValue("nonce", nonce);
            await using var rows = await export.ExecuteReaderAsync();
            while (await rows.ReadAsync()) Assert.Equal(10, rows.FieldCount);
        }
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        await using var audit = new NpgsqlCommand(
            "SELECT count(*) FROM nexa.rev869b_security_export_audits WHERE \"AuthorizationId\"=@authorization", verifier);
        audit.Parameters.AddWithValue("authorization", authorization);
        Assert.Equal(1, Convert.ToInt64(await audit.ExecuteScalarAsync()));
    }

    internal static async Task AuditFailureBlocksAsync(string scenario)
    {
        await using var owned = await Rev869BOwnedPostgresDatabase.CreateAsync(scenario);
        Guid actor;
        await using (var owner = new NpgsqlConnection(owned.OwnerConnectionString))
        {
            await owner.OpenAsync();
            await new NpgsqlCommand("""
                CREATE FUNCTION nexa.rev869b_test_reject_audit_insert() RETURNS trigger LANGUAGE plpgsql AS
                'BEGIN RAISE EXCEPTION USING ERRCODE=''P0001'',CONSTRAINT=''rev869b_test_audit_insert_fault''; END';
                CREATE TRIGGER "TR_rev869b_test_audit_insert_fault" BEFORE INSERT ON nexa.rev869b_command_security_audits
                FOR EACH ROW EXECUTE FUNCTION nexa.rev869b_test_reject_audit_insert();
                """, owner).ExecuteNonQueryAsync();
            actor = (Guid)(await new NpgsqlCommand("""
                SELECT m."EmployeeId" FROM nexa.employee_identity_mappings m
                WHERE m."Issuer"=@issuer AND m."Subject"=@subject AND m."OrganizationId"=@organization
                  AND m."IsActive" LIMIT 1
                """, owner)
            {
                Parameters =
                {
                    new("issuer", Rev869BOwnedPostgresDatabase.Issuer),
                    new("subject", Rev869BOwnedPostgresDatabase.Login),
                    new("organization", Rev869BOwnedPostgresDatabase.Organization)
                }
            }.ExecuteScalarAsync() ?? throw new InvalidOperationException("Seeded actor was not found."));
        }
        await using var runtime = await owned.OpenConnectionAsync();
        await using var transaction = await runtime.BeginTransactionAsync();
        var denial = await Assert.ThrowsAsync<PostgresException>(() =>
            Rev869BOwnedPostgresDatabase.SetCommandContextAsync(runtime, transaction, actor, "MANAGER",
                new Rev869BOwnedPostgresDatabase.ExactSlot("purchase_transaction_status_history", Guid.NewGuid(),
                    "RFQ", Guid.NewGuid(), "Submit", 1, "Draft", "PendingApproval",
                    "REV869B|" + Guid.NewGuid().ToString("N"), "Exact audit failure fixture")));
        Assert.Equal("P0001", denial.SqlState);
        Assert.Equal("rev869b_test_audit_insert_fault", denial.ConstraintName);
        await transaction.RollbackAsync();
    }

    internal static async Task ImmutableTriggerAsync(string scenario)
    {
        var state = await AuthorizedPurgeAsync(scenario);
        await using var lease = state.Lease;
        await InsertExpiredGrantFixtureAsync(lease, state.Organization, "Committed");
        await using var owner = new NpgsqlConnection(lease.OwnerConnectionString);
        await owner.OpenAsync();
        var denial = await Assert.ThrowsAsync<PostgresException>(() =>
            new NpgsqlCommand("DELETE FROM nexa.rev869b_command_security_audits", owner).ExecuteNonQueryAsync());
        Assert.Equal("42501", denial.SqlState);
        Assert.Equal("rev869b_ten_year_append_only_security_audit", denial.ConstraintName);
    }

    internal static async Task IndependentBackendsAsync(string scenario)
    {
        await using var lease = await Rev869BTestDatabaseLease.CreateAsync(scenario, "correction17-concurrency");
        await using var actor = await Rev869BPurgeCoordinator.OpenExactRoleAsync(
            "REV869B_PURGE_EXECUTOR_CONNECTION", "nexa_rev869b_purge_executor", lease.DatabaseName);
        await using var verifier = new NpgsqlConnection(lease.OwnerConnectionString);
        await verifier.OpenAsync();
        Assert.NotEqual(actor.ProcessID, verifier.ProcessID);
        var actorIdentity = Convert.ToString(await new NpgsqlCommand(
            "SELECT session_user||':'||current_database()||':'||pg_backend_pid()", actor).ExecuteScalarAsync());
        var verifierIdentity = Convert.ToString(await new NpgsqlCommand(
            "SELECT session_user||':'||current_database()||':'||pg_backend_pid()", verifier).ExecuteScalarAsync());
        Assert.NotEqual(actorIdentity, verifierIdentity);
        Assert.Contains(lease.DatabaseName, actorIdentity, StringComparison.Ordinal);
        Assert.Contains(lease.DatabaseName, verifierIdentity, StringComparison.Ordinal);
    }
}
