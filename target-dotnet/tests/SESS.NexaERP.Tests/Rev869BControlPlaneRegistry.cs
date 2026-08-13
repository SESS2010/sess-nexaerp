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
              @migrationFingerprint,@owner,@requested,@leaseExpires,@runtime,@issuer,@requestIssuer,@issuerAuthority,@policy)
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
              @database,@run,@tokenHash,@exactPreState,@exactPostState,@markerFingerprint,
              @outcome,@failureCategory,@occurredAt,@policy)
            """, connection);
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        command.Parameters.AddWithValue("tokenHash", lease.OwnershipTokenHash);
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

    internal static async Task<Guid> ConsumeRecoveryBeforeMutationAsync(
        LeaseReservation lease, RecoveryApproval approval, string targetFingerprint)
    {
        await using var connection = await OpenVerifiedAsync();
        await using var command = new NpgsqlCommand("""
            SELECT nexa.rev869b_consume_recovery_approval(
              @authorization,@database,@run,@tokenHash,@purpose,@approvalIssuer,@issuerAuthority,
              @expectedPreState,@authorizedPostState,@approvalReference,@reason,@executor,@issued,@expires,
              @nonceHash,@targetFingerprint,@consumedAt,@policy)
            """, connection);
        command.Parameters.AddWithValue("authorization", approval.AuthorizationId);
        command.Parameters.AddWithValue("database", lease.DatabaseName);
        command.Parameters.AddWithValue("run", lease.RunId);
        command.Parameters.AddWithValue("tokenHash", lease.OwnershipTokenHash);
        command.Parameters.AddWithValue("purpose", approval.Purpose);
        command.Parameters.AddWithValue("approvalIssuer", approval.ApprovalIssuer);
        command.Parameters.AddWithValue("issuerAuthority", approval.IssuerAuthority);
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

    internal static async Task RecordRecoveryOutcomeAsync(
        Guid attemptId, string exactPreState, string observedPostState, string? markerFingerprint,
        string outcome, string? failureCategory)
    {
        await using var connection = await OpenVerifiedAsync();
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
    }

    private static async Task<NpgsqlConnection> OpenVerifiedAsync()
    {
        var raw = Environment.GetEnvironmentVariable("REV869B_CONTROL_PLANE")
            ?? throw new InvalidOperationException("REV869B_CONTROL_PLANE is required; filesystem state cannot authorize provisioning or recovery.");
        var builder = new NpgsqlConnectionStringBuilder(raw) { Pooling = false };
        var database = builder.Database ?? string.Empty;
        if (!string.Equals(database, ExactDatabase, StringComparison.Ordinal) ||
            string.Equals(builder.Database, Rev869BTestDatabaseLease.ExactSourceDatabase, StringComparison.Ordinal) ||
            database.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("The separately provisioned exact REV869B control-plane database is required.");
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var proof = new NpgsqlCommand("""
            SELECT count(*) FROM pg_database d
            WHERE d.datname=current_database() AND d.datname=@database
              AND pg_get_userbyid(d.datdba)='nexa_rev869b_control_plane_owner'
              AND to_regprocedure('nexa.rev869b_reserve_database_lease(text,text,text,text,text,text,text,text,text,text,text,timestamp with time zone,timestamp with time zone,text,text,text,text,text)') IS NOT NULL
              AND to_regprocedure('nexa.rev869b_consume_recovery_approval(uuid,text,text,text,text,text,text,text,text,text,text,text,timestamp with time zone,timestamp with time zone,text,text,timestamp with time zone,text)') IS NOT NULL
              AND NOT has_table_privilege(session_user,'nexa.rev869b_database_leases','SELECT,INSERT,UPDATE,DELETE')
            """, connection);
        proof.Parameters.AddWithValue("database", ExactDatabase);
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
        command.Parameters.AddWithValue("issuerAuthority", lease.IssuerAuthority);
    }
}
