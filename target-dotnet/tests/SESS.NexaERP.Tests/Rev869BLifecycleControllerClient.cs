using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>Typed acceptance client. It never receives lifecycle-admin credentials or executes CREATE/DROP SQL.</summary>
internal sealed class Rev869BLifecycleControllerClient : IAsyncDisposable
{
    internal const string OptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private readonly HttpClient http;

    private Rev869BLifecycleControllerClient(HttpClient http) => this.http = http;

    internal static Rev869BLifecycleControllerClient Create()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), OptIn, StringComparison.Ordinal))
            throw new InvalidOperationException("Explicit isolated REV869B PostgreSQL opt-in is required.");
        var endpoint = Environment.GetEnvironmentVariable("REV869B_LIFECYCLE_CONTROLLER_URL");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("An HTTPS lifecycle-controller endpoint is required.");
        return new(new HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromMinutes(5) });
    }

    internal async Task<LeaseAllocation> AllocateAsync(string scenario, string family, CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid();
        using var response = await http.PostAsJsonAsync("v1/rev869b/test-leases", new { requestId, scenario, family }, ct);
        response.EnsureSuccessStatusCode();
        var lease = await response.Content.ReadFromJsonAsync<LeaseAllocation>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Lifecycle controller returned no lease allocation.");
        RequireAllocation(lease, requestId);
        return lease;
    }

    internal async Task<AcceptanceEvidence> RunAcceptanceScenarioAsync(AcceptanceContract contract, CancellationToken ct = default)
    {
        var runId = Guid.NewGuid();
        using var response = await http.PostAsJsonAsync($"v1/rev869b/acceptance/{contract.ScenarioId}", new
        {
            runId,
            deterministicFailpoint = contract.ScenarioId,
            contract.Setup,
            contract.Action,
            contract.ExpectedInitialState,
            contract.ExpectedFinalState,
            contract.ExpectedSqlState,
            contract.ExpectedDatabaseObject,
            contract.ExpectedAffectedRows,
            contract.RequiresDenial,
            contract.AllowsZeroRowsTerminal,
            contract.RequiresDecision
        }, ct);
        response.EnsureSuccessStatusCode();
        var evidence = await response.Content.ReadFromJsonAsync<AcceptanceEvidence>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Acceptance controller returned no evidence.");
        RequireAcceptanceEvidence(contract, runId, evidence);
        return evidence;
    }

    private static void RequireAcceptanceEvidence(AcceptanceContract contract, Guid runId, AcceptanceEvidence evidence)
    {
        if (!string.Equals(evidence.ScenarioId, contract.ScenarioId, StringComparison.Ordinal) || evidence.RunId != runId ||
            evidence.LeaseId == Guid.Empty || evidence.AttemptId == Guid.Empty || evidence.FixtureId == Guid.Empty ||
            (contract.RequiresDecision && evidence.DecisionId.GetValueOrDefault() == Guid.Empty) ||
            !string.Equals(evidence.Setup, contract.Setup, StringComparison.Ordinal) ||
            !string.Equals(evidence.Action, contract.Action, StringComparison.Ordinal) || !evidence.SetupCompleted || !evidence.ActionReached ||
            !string.Equals(evidence.InitialState, contract.ExpectedInitialState, StringComparison.Ordinal) ||
            !string.Equals(evidence.FinalState, contract.ExpectedFinalState, StringComparison.Ordinal) ||
            !ExactSha256(evidence.InitialStateSha256) || !ExactSha256(evidence.FinalStateSha256) ||
            !ExactSha256(evidence.FixtureSha256) || !ExactSha256(evidence.DurableEvidenceSha256) ||
            evidence.SourceCommit.Length != 40 || evidence.SourceCommit.Any(c => !Uri.IsHexDigit(c)) ||
            !ExactSha256(evidence.ManifestSha256) || !ExactSha256(evidence.TlsSpkiSha256) ||
            evidence.ClusterSystemIdentifier.Length is < 10 or > 20 || evidence.ClusterSystemIdentifier.Any(c => !char.IsDigit(c)) ||
            evidence.DurableEvidenceCount < 1 || evidence.UnrelatedMutationCount != 0 || !evidence.CleanupFinalized ||
            !evidence.TargetAbsent || !evidence.RolesAbsent || evidence.AffectedRows != contract.ExpectedAffectedRows ||
            evidence.Metrics.ValueKind != JsonValueKind.Object ||
            !TryPositiveMetric(evidence.Metrics, "fixtureRowCount") ||
            !TryNonNegativeMetric(evidence.Metrics, "beforeRowCount") ||
            !TryNonNegativeMetric(evidence.Metrics, "afterRowCount") ||
            !TrySha256Metric(evidence.Metrics, "actionEvidenceSha256") ||
            !TrySha256Metric(evidence.Metrics, "cleanupEvidenceSha256"))
            throw new InvalidOperationException("Acceptance evidence did not prove the exact fixture, action, state, durability, isolation and cleanup contract.");
        if (contract.RequiresDenial)
        {
            if (!string.Equals(evidence.SqlState, contract.ExpectedSqlState, StringComparison.Ordinal) ||
                !string.Equals(evidence.DatabaseObject, contract.ExpectedDatabaseObject, StringComparison.Ordinal))
                throw new InvalidOperationException("Acceptance denial did not carry the exact SQLSTATE and database object.");
        }
        else if (evidence.SqlState is not null || evidence.DatabaseObject is not null ||
                 (contract.ExpectedAffectedRows <= 0 && !contract.AllowsZeroRowsTerminal))
            throw new InvalidOperationException("A successful acceptance action must affect and prove at least one authoritative fact.");
    }

    private static bool ExactSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool TryPositiveMetric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.TryGetInt32(out var count) && count > 0;

    private static bool TryNonNegativeMetric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.TryGetInt32(out var count) && count >= 0;

    private static bool TrySha256Metric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
        value.GetString() is { } text && ExactSha256(text);

    internal async Task ReleaseAsync(Guid leaseId, Guid requestId, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync($"v1/rev869b/test-leases/{leaseId}/release", new { requestId }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReleaseEvidence>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Lifecycle controller returned no cleanup evidence.");
        if (result.LeaseId != leaseId || result.State != "Finalized" || !result.TargetAbsent || !result.RolesAbsent)
            throw new InvalidOperationException("Lifecycle cleanup was not authoritatively finalized.");
    }

    private static void RequireAllocation(LeaseAllocation lease, Guid requestId)
    {
        if (lease.RequestId != requestId || lease.LeaseId == Guid.Empty || lease.Version < 1 || lease.State != "InUse" ||
            !lease.FixturePrepared || lease.FixtureSha256.Length != 64 || lease.FixtureSha256.Any(c => !Uri.IsHexDigit(c)) ||
            !lease.DatabaseName.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal) ||
            lease.DatabaseName.Length != Rev869BTestDatabaseLease.DatabasePrefix.Length + 24 ||
            lease.DatabaseName[Rev869BTestDatabaseLease.DatabasePrefix.Length..].Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("Lifecycle controller returned an unsafe or mismatched allocation.");
        RequireTargetConnection(lease.RuntimeConnectionString, lease.DatabaseName, "nexa_rev869b_app_runtime");
        RequireTargetConnection(lease.VerifierConnectionString, lease.DatabaseName, "nexa_rev869b_target_verifier");
        if (lease.RuntimeConnectionString.Contains("lifecycle_administrator", StringComparison.OrdinalIgnoreCase) ||
            lease.VerifierConnectionString.Contains("lifecycle_administrator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Lifecycle administrator credentials must never enter a test process.");
    }

    private static void RequireTargetConnection(string value, string database, string role)
    {
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (!string.Equals(builder.Database, database, StringComparison.Ordinal) || !string.Equals(builder.Username, role, StringComparison.Ordinal) || builder.Pooling)
            throw new InvalidOperationException("Controller returned a pooled, wrong-database or wrong-role target connection.");
    }

    public ValueTask DisposeAsync() { http.Dispose(); return ValueTask.CompletedTask; }

    internal sealed record LeaseAllocation(Guid RequestId, Guid LeaseId, long Version, string State, string DatabaseName,
        string RuntimeConnectionString, string VerifierConnectionString, string RunId, string OwnershipNonceSha256,
        string MarkerSha256, string ClusterSystemIdentifier, string TlsSpkiSha256, string SourceCommit, string ManifestSha256,
        bool FixturePrepared, string FixtureSha256);
    internal sealed record ReleaseEvidence(Guid LeaseId, string State, bool TargetAbsent, bool RolesAbsent, string EvidenceSha256);
    internal sealed record AcceptanceContract(string ScenarioId, string Setup, string Action,
        string ExpectedInitialState, string ExpectedFinalState, string? ExpectedSqlState,
        string? ExpectedDatabaseObject, int ExpectedAffectedRows, bool RequiresDenial,
        bool AllowsZeroRowsTerminal = false, bool RequiresDecision = false);
    internal sealed record AcceptanceEvidence(string ScenarioId, Guid RunId, Guid LeaseId, Guid AttemptId,
        Guid? DecisionId, Guid FixtureId, string FixtureSha256, string ClusterSystemIdentifier,
        string TlsSpkiSha256, string SourceCommit, string ManifestSha256, string Setup, string Action,
        bool SetupCompleted, bool ActionReached, int AffectedRows, int DurableEvidenceCount,
        string DurableEvidenceSha256, int UnrelatedMutationCount, bool CleanupFinalized,
        bool TargetAbsent, bool RolesAbsent, string? SqlState, string? DatabaseObject,
        string InitialState, string InitialStateSha256, string FinalState, string FinalStateSha256,
        JsonElement Metrics);
}
