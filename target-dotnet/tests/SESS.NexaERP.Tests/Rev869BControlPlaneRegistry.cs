using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Fail-closed source contract for the separately provisioned REV869B control plane. The registry
/// is never created, migrated, repaired, or hosted in the REV868C3/REV869B source or disposable
/// target by this test assembly. Every call disables pooling and invokes an owner-installed,
/// security-definer API; callers receive no table privilege. Filesystem evidence is supplemental.
/// </summary>
internal static class Rev869BControlPlaneRegistry
{
    internal const string ExactDatabase = "sess_nexaerp_rev869b_control_plane";
    internal const string Policy = "MGMT-REV869B-CONTROL-PLANE-20260813-001";
    internal const string SecurityOwner = "nexa_rev869b_control_plane_owner";

    internal sealed record LeaseSnapshot(
        string DatabaseName, string RunId, string OwnershipTokenHash, string FixtureFamily,
        string ScenarioHash, string SourceDatabase, string SourceFingerprint, string SourceCommitFingerprint,
        string MigrationId, string MigrationFingerprint, string ExpectedOwner, DateTimeOffset RequestedAt,
        DateTimeOffset LeaseExpiresAt, string RuntimeRole, string IssuerRole, string State,
        string? MarkerFingerprint);

    internal sealed record LeaseReservation(
        string DatabaseName, string RunId, string OwnershipTokenHash, string FixtureFamily,
        string ScenarioHash, string SourceDatabase, string SourceFingerprint, string SourceCommitFingerprint,
        string MigrationId, string MigrationFingerprint, string ExpectedOwner, DateTimeOffset RequestedAt,
        DateTimeOffset LeaseExpiresAt,
        string RuntimeRole, string IssuerRole, string RequestIssuer, string IssuerAuthority);

    internal sealed record RecoveryApproval(
        Guid AuthorizationId, string NonceHash, string Purpose, string ApprovalIssuer,
        string IssuerAuthority, string ExpectedPreState, string AuthorizedPostState,
        string ApprovalReference, string Reason, string ExecutorIdentity,
        DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

    internal static async Task ReserveBeforeCreateAsync(LeaseReservation lease)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_reserve_database_lease(
              @database,@run,@tokenHash,@family,@scenario,@source,@sourceFingerprint,@sourceCommit,@migration,
              @migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,@requestAuthority,@policy)
            """, connection);
        AddLease(command, lease);
        command.Parameters.AddWithValue("policy", Policy);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The control plane did not durably reserve the exact pre-marker lease.");
    }

    internal static async Task BindMarkerAndOutcomeAsync(
        LeaseReservation lease, string exactPreState, string exactPostState,
        string? markerFingerprint, string outcome, string? failureCategory)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_complete_database_lease(
              @database,@run,@tokenHash,@family,@scenario,@source,@sourceFingerprint,@sourceCommit,@migration,
              @migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,@requestAuthority,
              @exactPreState,@exactPostState,@markerFingerprint,
              @outcome,@failureCategory,@occurredAt,@policy)
            """, connection);
        AddLease(command, lease);
        command.Parameters.AddWithValue("exactPreState", exactPreState);
        command.Parameters.AddWithValue("exactPostState", exactPostState);
        command.Parameters.AddWithValue("markerFingerprint", (object?)markerFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("outcome", outcome);
        command.Parameters.AddWithValue("failureCategory", (object?)failureCategory ?? DBNull.Value);
        command.Parameters.AddWithValue("occurredAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The control plane rejected the exact lease state/outcome transition.");
    }

    internal static async Task<LeaseSnapshot> ReadExactLeaseAsync(LeaseReservation lease, string requiredState)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand("""
            SELECT "DatabaseName","RunId","OwnershipTokenHash","FixtureFamily","ScenarioHash",
              "SourceDatabase","SourceFingerprint","SourceCommitFingerprint","MigrationId","MigrationFingerprint",
              "ExpectedOwner","RequestedAt","LeaseExpiresAt","RuntimeRole","IssuerRole","State","MarkerFingerprint"
            FROM nexa.rev869b_read_exact_database_lease(
              @database,@run,@tokenHash,@family,@scenario,@source,@sourceFingerprint,@sourceCommit,
              @migration,@migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,
              @requestAuthority,@requiredState,@policy)
            """, connection);
        AddLease(command, lease);
        command.Parameters.AddWithValue("requiredState", requiredState);
        command.Parameters.AddWithValue("policy", Policy);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new InvalidOperationException("The control plane did not return exactly one fully bound lease.");
        var snapshot = new LeaseSnapshot(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8),
            reader.GetString(9), reader.GetString(10), reader.GetFieldValue<DateTimeOffset>(11),
            reader.GetFieldValue<DateTimeOffset>(12), reader.GetString(13), reader.GetString(14), reader.GetString(15),
            reader.IsDBNull(16) ? null : reader.GetString(16));
        if (await reader.ReadAsync())
            throw new InvalidOperationException("The control plane returned a duplicate lease.");
        return snapshot;
    }

    internal static async Task<string[]> ReadTransitionStatesAsync(LeaseReservation lease)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand(
            "SELECT nexa.rev869b_read_database_lease_transition_states(@database,@run)", connection);
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        return (string[]?)await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Authoritative lifecycle transition evidence is missing.");
    }

    internal static async Task<Guid> BeginLeaseDropAsync(
        LeaseReservation lease, string exactPreState, string markerFingerprint, string requestedPostState)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_begin_database_drop(
              @database,@run,@tokenHash,@family,@scenario,@source,@sourceFingerprint,@sourceCommit,@migration,
              @migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,@requestAuthority,@exactPreState,
              @markerFingerprint,@requestedPostState,@occurredAt,@policy)
            """, connection);
        AddLease(command, lease);
        command.Parameters.AddWithValue("exactPreState", exactPreState);
        command.Parameters.AddWithValue("markerFingerprint", markerFingerprint);
        command.Parameters.AddWithValue("requestedPostState", requestedPostState);
        command.Parameters.AddWithValue("occurredAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        var result = await command.ExecuteScalarAsync();
        if (result is not Guid attemptId || attemptId == Guid.Empty)
            throw new InvalidOperationException("The control plane did not durably authorize the exact drop transition.");
        return attemptId;
    }

    internal static async Task RecordLeaseDropOutcomeAsync(
        Guid attemptId, LeaseReservation lease, string exactPreState, string observedPostState,
        string markerFingerprint, string outcome, string? failureCategory)
    {
        await using var connection = await OpenVerifiedAsync(
            "REV869B_CONTROL_PLANE_AUDIT_WRITER", "nexa_rev869b_control_plane_audit_writer");
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_record_database_drop_outcome(
              @attempt,@database,@run,@tokenHash,@exactPreState,@observedPostState,@markerFingerprint,
              @outcome,@failureCategory,@occurredAt,@policy)
            """, connection);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        command.Parameters.AddWithValue("tokenHash", lease.OwnershipTokenHash);
        command.Parameters.AddWithValue("exactPreState", exactPreState);
        command.Parameters.AddWithValue("observedPostState", observedPostState);
        command.Parameters.AddWithValue("markerFingerprint", markerFingerprint);
        command.Parameters.AddWithValue("outcome", outcome);
        command.Parameters.AddWithValue("failureCategory", (object?)failureCategory ?? DBNull.Value);
        command.Parameters.AddWithValue("occurredAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The control plane did not durably append the exact drop outcome.");
        if (observedPostState == "Dropped" && outcome == "Succeeded")
            await FinalizeLeaseAsync(connection, attemptId, lease, markerFingerprint);
    }

    internal static async Task<Guid> ConsumeRecoveryBeforeMutationAsync(
        LeaseReservation lease, RecoveryApproval approval, string targetFingerprint)
    {
        await using var connection = await OpenVerifiedAsync(
            "REV869B_CONTROL_PLANE_RECOVERY", "nexa_rev869b_recovery_administrator");
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_consume_recovery_approval(
              @authorization,@database,@run,@tokenHash,@family,@scenario,@source,@sourceFingerprint,@sourceCommit,
              @migration,@migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,@requestAuthority,
              @purpose,@approvalIssuer,@approvalAuthority,
              @expectedPreState,@authorizedPostState,@approvalReference,@reason,@executor,@issued,@expires,
              @nonceHash,@targetFingerprint,@consumedAt,@policy)
            """, connection);
        command.Parameters.AddWithValue("authorization", approval.AuthorizationId);
        AddLease(command, lease);
        command.Parameters.AddWithValue("purpose", approval.Purpose);
        command.Parameters.AddWithValue("approvalIssuer", approval.ApprovalIssuer);
        command.Parameters.AddWithValue("approvalAuthority", approval.IssuerAuthority);
        command.Parameters.AddWithValue("expectedPreState", approval.ExpectedPreState);
        command.Parameters.AddWithValue("authorizedPostState", approval.AuthorizedPostState);
        command.Parameters.AddWithValue("approvalReference", approval.ApprovalReference);
        command.Parameters.AddWithValue("reason", approval.Reason);
        command.Parameters.AddWithValue("executor", approval.ExecutorIdentity);
        command.Parameters.AddWithValue("issued", approval.IssuedAt);
        command.Parameters.AddWithValue("expires", approval.ExpiresAt);
        command.Parameters.AddWithValue("nonceHash", approval.NonceHash);
        command.Parameters.AddWithValue("targetFingerprint", targetFingerprint);
        command.Parameters.AddWithValue("consumedAt", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        var result = await command.ExecuteScalarAsync();
        if (result is not Guid attemptId || attemptId == Guid.Empty)
            throw new InvalidOperationException("Recovery approval was not durably consumed before target mutation.");
        return attemptId;
    }

    private static async Task FinalizeLeaseAsync(
        NpgsqlConnection auditWriter, Guid attemptId, LeaseReservation lease, string? markerFingerprint)
    {
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_finalize_database_lease(
              @attempt,@database,@run,@marker,@finalized,@policy)
            """, auditWriter);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        command.Parameters.AddWithValue("marker", (object?)markerFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("finalized", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("The exact dropped lease did not reach Finalized.");
    }

    internal static async Task RecordRecoveryOutcomeAsync(
        Guid attemptId, LeaseReservation lease, string exactPreState, string observedPostState, string? markerFingerprint,
        string outcome, string? failureCategory)
    {
        await using var connection = await OpenVerifiedAsync(
            "REV869B_CONTROL_PLANE_RECOVERY", "nexa_rev869b_recovery_administrator");
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_record_recovery_outcome(
              @attempt,@pre,@post,@marker,@outcome,@failure,@finished,@policy)
            """, connection);
        command.Parameters.AddWithValue("attempt", attemptId);
        command.Parameters.AddWithValue("pre", exactPreState);
        command.Parameters.AddWithValue("post", observedPostState);
        command.Parameters.AddWithValue("marker", (object?)markerFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("outcome", outcome);
        command.Parameters.AddWithValue("failure", (object?)failureCategory ?? DBNull.Value);
        command.Parameters.AddWithValue("finished", DateTimeOffset.UtcNow);
        command.Parameters.AddWithValue("policy", Policy);
        if (Convert.ToInt64(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("Recovery outcome was not durably appended to the control plane.");
        if (observedPostState == "Dropped" && outcome == "Succeeded")
        {
            await using var auditWriter = await OpenVerifiedAsync(
                "REV869B_CONTROL_PLANE_AUDIT_WRITER", "nexa_rev869b_control_plane_audit_writer");
            await FinalizeLeaseAsync(auditWriter, attemptId, lease, markerFingerprint);
        }
    }

    private static Task<NpgsqlConnection> OpenVerifiedAsync() =>
        OpenVerifiedAsync("REV869B_CONTROL_PLANE", ApiRoleName);

    private const string ApiRoleName = "nexa_rev869b_control_plane_api";

    private static async Task<NpgsqlConnection> OpenVerifiedAsync(string environmentName, string expectedRole)
    {
        var raw = Environment.GetEnvironmentVariable(environmentName)
            ?? throw new InvalidOperationException(environmentName + " is required; filesystem state cannot authorize lifecycle or recovery.");
        var builder = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        var database = builder.Database ?? string.Empty;
        Rev869BControlPlaneProvisioningContract.RequireSafeTarget(database);
        if (!string.Equals(database, ExactDatabase, StringComparison.Ordinal) ||
            string.Equals(builder.Database, Rev869BTestDatabaseLease.ExactSourceDatabase, StringComparison.Ordinal) ||
            database.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("The separately provisioned exact REV869B control-plane database is required.");
        if (!string.Equals(builder.Username, expectedRole, StringComparison.Ordinal))
            throw new InvalidOperationException("The exact least-privilege control-plane principal is required.");
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var proof = new NpgsqlCommand(
            Rev869BControlPlaneProvisioningContract.ExactReadinessSql, connection);
        proof.Parameters.AddWithValue("database", ExactDatabase);
        proof.Parameters.AddWithValue("owner", SecurityOwner);
        if (Convert.ToInt64(await proof.ExecuteScalarAsync()) != 1)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Control-plane identity, owner, API, or no-direct-table-access proof failed.");
        }
        return connection;
    }

    private static void AddLease(NpgsqlCommand command, LeaseReservation lease)
    {
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        command.Parameters.AddWithValue("tokenHash", lease.OwnershipTokenHash);
        command.Parameters.AddWithValue("family", lease.FixtureFamily);
        command.Parameters.AddWithValue("scenario", lease.ScenarioHash);
        command.Parameters.AddWithValue("source", lease.SourceDatabase);
        command.Parameters.AddWithValue("sourceFingerprint", lease.SourceFingerprint);
        command.Parameters.AddWithValue("sourceCommit", lease.SourceCommitFingerprint);
        command.Parameters.AddWithValue("migration", lease.MigrationId);
        command.Parameters.AddWithValue("migrationFingerprint", lease.MigrationFingerprint);
        command.Parameters.AddWithValue("owner", lease.ExpectedOwner);
        command.Parameters.AddWithValue("requested", lease.RequestedAt);
        command.Parameters.AddWithValue("leaseExpires", lease.LeaseExpiresAt);
        command.Parameters.AddWithValue("runtime", lease.RuntimeRole);
        command.Parameters.AddWithValue("issuer", lease.IssuerRole);
        command.Parameters.AddWithValue("requestIssuer", lease.RequestIssuer);
        command.Parameters.AddWithValue("requestAuthority", lease.IssuerAuthority);
    }
}
