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

    internal async Task<AcceptanceEvidence> RunAcceptanceScenarioAsync(string id, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync($"v1/rev869b/acceptance/{id}", new { runId = Guid.NewGuid(), deterministicFailpoint = id }, ct);
        response.EnsureSuccessStatusCode();
        var evidence = await response.Content.ReadFromJsonAsync<AcceptanceEvidence>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Acceptance controller returned no evidence.");
        if (!string.Equals(evidence.ScenarioId, id, StringComparison.Ordinal) || !evidence.ActionReached ||
            evidence.UnrelatedMutationCount != 0 || !evidence.CleanupFinalized || evidence.DurableEvidenceCount < 1)
            throw new InvalidOperationException("Acceptance evidence did not prove action, isolation, durability and cleanup.");
        return evidence;
    }

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
    internal sealed record AcceptanceEvidence(string ScenarioId, bool ActionReached, int DurableEvidenceCount,
        int UnrelatedMutationCount, bool CleanupFinalized, string? SqlState, string? DatabaseObject,
        string InitialState, string FinalState, JsonElement Metrics);
}
