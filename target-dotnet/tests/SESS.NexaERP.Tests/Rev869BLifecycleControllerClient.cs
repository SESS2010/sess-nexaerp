using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace SESS.NexaERP.Tests;

/// <summary>Typed acceptance client. It never receives lifecycle-admin credentials or executes CREATE/DROP SQL.</summary>
internal sealed class Rev869BLifecycleControllerClient : IAsyncDisposable
{
    internal const string OptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private readonly HttpClient http;
    private readonly AcceptancePins pins;

    private Rev869BLifecycleControllerClient(HttpClient http, AcceptancePins pins)
    {
        this.http = http;
        this.pins = pins;
    }

    internal static Rev869BLifecycleControllerClient Create()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), OptIn, StringComparison.Ordinal))
            throw new InvalidOperationException("Explicit isolated REV869B PostgreSQL opt-in is required.");
        var endpoint = Required("REV869B_LIFECYCLE_CONTROLLER_URL");
        var expectedOrigin = Required("REV869B_EXPECTED_CONTROLLER_ORIGIN");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            !string.Equals(uri.GetLeftPart(UriPartial.Authority), expectedOrigin.TrimEnd('/'), StringComparison.Ordinal))
            throw new InvalidOperationException("The exact pinned HTTPS lifecycle-controller origin is required.");
        var pins = new AcceptancePins(
            Required("REV869B_EXPECTED_SOURCE_COMMIT"), Required("REV869B_EXPECTED_MANIFEST_SHA256").ToLowerInvariant(),
            Required("REV869B_EXPECTED_TLS_SPKI_SHA256").ToLowerInvariant(), Required("REV869B_EXPECTED_CLUSTER_SYSTEM_IDENTIFIER"),
            Required("REV869B_CONTROLLER_SIGNING_PUBLIC_KEY_PEM"), Required("REV869B_CONTROLLER_SIGNING_PUBLIC_KEY_SHA256").ToLowerInvariant());
        if (pins.SourceCommit.Length != 40 || pins.SourceCommit.Any(c => !Uri.IsHexDigit(c)) ||
            !ExactSha256(pins.ManifestSha256) || !ExactSha256(pins.TlsSpkiSha256) ||
            pins.ClusterSystemIdentifier.Length is < 10 or > 20 || pins.ClusterSystemIdentifier.Any(c => !char.IsDigit(c)) ||
            !ExactSha256(pins.SigningPublicKeySha256) ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(pins.SigningPublicKeyPem)), Convert.FromHexString(pins.SigningPublicKeySha256)))
            throw new InvalidOperationException("Complete exact lifecycle-controller pins are required.");
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            errors == SslPolicyErrors.None && certificate is not null && CertificateSpkiSha256(certificate) == pins.TlsSpkiSha256;
        return new(new HttpClient(handler) { BaseAddress = uri, Timeout = TimeSpan.FromMinutes(5) }, pins);
    }

    internal async Task<LeaseAllocation> AllocateAsync(string scenario, string family, CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid();
        var contractSha256 = ExactContractSha256(new { requestId, scenario, family, pins.SourceCommit, pins.ManifestSha256, pins.ClusterSystemIdentifier });
        using var response = await http.PostAsJsonAsync("v1/rev869b/test-leases", new { requestId, scenario, family, contractSha256 }, ct);
        response.EnsureSuccessStatusCode();
        var lease = await ReadSignedAsync<LeaseAllocation>(response, ct);
        RequireAllocation(lease, requestId, contractSha256);
        return lease;
    }

    internal async Task<AcceptanceEvidence> RunAcceptanceScenarioAsync(AcceptanceContract contract, CancellationToken ct = default)
    {
        var runId = Guid.NewGuid();
        var contractSha256 = ExactContractSha256(contract);
        using var response = await http.PostAsJsonAsync($"v1/rev869b/acceptance/{contract.ScenarioId}", new
        {
            runId,
            deterministicFailpoint = contract.ScenarioId,
            contractSha256,
            contract,
            pins.SourceCommit,
            pins.ManifestSha256,
            pins.TlsSpkiSha256,
            pins.ClusterSystemIdentifier,
            pins.SigningPublicKeySha256
        }, ct);
        response.EnsureSuccessStatusCode();
        var evidence = await ReadSignedAsync<AcceptanceEvidence>(response, ct);
        RequireAcceptanceEvidence(contract, contractSha256, runId, evidence);
        return evidence;
    }

    private void RequireAcceptanceEvidence(AcceptanceContract contract, string contractSha256, Guid runId, AcceptanceEvidence evidence)
    {
        if (!string.Equals(evidence.ScenarioId, contract.ScenarioId, StringComparison.Ordinal) || evidence.RunId != runId ||
            !string.Equals(evidence.ContractSha256, contractSha256, StringComparison.Ordinal) ||
            evidence.LeaseId == Guid.Empty || evidence.CommandId == Guid.Empty || evidence.AuthorizationId == Guid.Empty || evidence.AttemptId == Guid.Empty || evidence.FixtureId == Guid.Empty ||
            evidence.DurableEvidenceId == Guid.Empty || evidence.CleanupEvidenceId == Guid.Empty ||
            (contract.RequiresDecision && evidence.DecisionId.GetValueOrDefault() == Guid.Empty) ||
            !string.Equals(evidence.Setup, contract.Setup, StringComparison.Ordinal) ||
            !string.Equals(evidence.Action, contract.Action, StringComparison.Ordinal) || !string.Equals(evidence.ActionPerformed, contract.Action, StringComparison.Ordinal) || !evidence.SetupCompleted || !evidence.ActionReached ||
            !string.Equals(evidence.InitialState, contract.ExpectedInitialState, StringComparison.Ordinal) ||
            !string.Equals(evidence.FinalState, contract.ExpectedFinalState, StringComparison.Ordinal) ||
            !string.Equals(evidence.TerminalOutcome, contract.ExpectedTerminalOutcome, StringComparison.Ordinal) ||
            !string.Equals(evidence.CleanupOutcome, contract.ExpectedCleanupOutcome, StringComparison.Ordinal) ||
            !string.Equals(evidence.DatabaseName, contract.ExpectedDatabaseName, StringComparison.Ordinal) ||
            !string.Equals(evidence.TargetInstanceSha256, contract.ExpectedTargetInstanceSha256, StringComparison.Ordinal) ||
            evidence.FixtureId != contract.ExpectedFixtureId || evidence.CommandId != contract.ExpectedCommandId ||
            evidence.AuthorizationId != contract.ExpectedAuthorizationId || evidence.AttemptId != contract.ExpectedAttemptId ||
            evidence.DecisionId != contract.ExpectedDecisionId || evidence.DurableEvidenceId != contract.ExpectedDurableEvidenceId ||
            evidence.CleanupEvidenceId != contract.ExpectedCleanupEvidenceId ||
            !string.Equals(evidence.DatabaseIdentity.Schema, contract.ExpectedIdentity.Schema, StringComparison.Ordinal) ||
            !string.Equals(evidence.DatabaseIdentity.Table, contract.ExpectedIdentity.Table, StringComparison.Ordinal) ||
            !string.Equals(evidence.DatabaseIdentity.Constraint, contract.ExpectedIdentity.Constraint, StringComparison.Ordinal) ||
            !string.Equals(evidence.DatabaseIdentity.Function, contract.ExpectedIdentity.Function, StringComparison.Ordinal) ||
            !string.Equals(evidence.DatabaseIdentity.Trigger, contract.ExpectedIdentity.Trigger, StringComparison.Ordinal) ||
            evidence.BeforeCount != contract.ExpectedBeforeCount || evidence.AfterCount != contract.ExpectedAfterCount ||
            !string.Equals(evidence.BeforeSha256, contract.ExpectedBeforeSha256, StringComparison.Ordinal) ||
            !string.Equals(evidence.AfterSha256, contract.ExpectedAfterSha256, StringComparison.Ordinal) ||
            !ExactSha256(evidence.InitialStateSha256) || !ExactSha256(evidence.FinalStateSha256) ||
            !string.Equals(evidence.FixtureSha256, contract.ExpectedFixtureSha256, StringComparison.Ordinal) ||
            !string.Equals(evidence.DurableEvidenceSha256, contract.ExpectedDurableEvidenceSha256, StringComparison.Ordinal) ||
            !string.Equals(evidence.CleanupEvidenceSha256, contract.ExpectedCleanupEvidenceSha256, StringComparison.Ordinal) ||
            !evidence.SubcaseEvidenceKeys.SequenceEqual(contract.ExpectedSubcaseEvidenceKeys, StringComparer.Ordinal) ||
            !string.Equals(evidence.SourceCommit, pins.SourceCommit, StringComparison.Ordinal) ||
            !string.Equals(evidence.ManifestSha256, pins.ManifestSha256, StringComparison.Ordinal) ||
            !string.Equals(evidence.TlsSpkiSha256, pins.TlsSpkiSha256, StringComparison.Ordinal) ||
            !string.Equals(evidence.ClusterSystemIdentifier, pins.ClusterSystemIdentifier, StringComparison.Ordinal) ||
            !string.Equals(evidence.SigningPublicKeySha256, pins.SigningPublicKeySha256, StringComparison.Ordinal) ||
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

    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException(name + " is required for pinned acceptance evidence.");

    private static string CertificateSpkiSha256(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null) return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
        using var ecdsa = certificate.GetECDsaPublicKey();
        if (ecdsa is not null) return Convert.ToHexString(SHA256.HashData(ecdsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
        return string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static bool TryPositiveMetric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.TryGetInt32(out var count) && count > 0;

    private static bool TryNonNegativeMetric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.TryGetInt32(out var count) && count >= 0;

    private static bool TrySha256Metric(JsonElement metrics, string name) =>
        metrics.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
        value.GetString() is { } text && ExactSha256(text);

    internal async Task ReleaseAsync(Guid leaseId, Guid requestId, CancellationToken ct = default)
    {
        var contractSha256 = ExactContractSha256(new { leaseId, requestId, outcome = "Finalized", targetAbsent = true, rolesAbsent = true });
        using var response = await http.PostAsJsonAsync($"v1/rev869b/test-leases/{leaseId}/release", new { requestId, contractSha256 }, ct);
        response.EnsureSuccessStatusCode();
        var result = await ReadSignedAsync<ReleaseEvidence>(response, ct);
        if (result.LeaseId != leaseId || result.EvidenceId == Guid.Empty || !ExactSha256(result.EvidenceSha256) ||
            result.ContractSha256 != contractSha256 || result.SigningPublicKeySha256 != pins.SigningPublicKeySha256 ||
            result.State != "Finalized" || !result.TargetAbsent || !result.RolesAbsent)
            throw new InvalidOperationException("Lifecycle cleanup was not authoritatively finalized.");
    }

    private void RequireAllocation(LeaseAllocation lease, Guid requestId, string contractSha256)
    {
        if (lease.RequestId != requestId || lease.LeaseId == Guid.Empty || lease.Version < 1 || lease.State != "InUse" ||
            !lease.FixturePrepared || lease.FixtureSha256.Length != 64 || lease.FixtureSha256.Any(c => !Uri.IsHexDigit(c)) ||
            !string.Equals(lease.ContractSha256, contractSha256, StringComparison.Ordinal) ||
            !string.Equals(lease.SigningPublicKeySha256, pins.SigningPublicKeySha256, StringComparison.Ordinal) ||
            !string.Equals(lease.SourceCommit, pins.SourceCommit, StringComparison.Ordinal) ||
            !string.Equals(lease.ManifestSha256, pins.ManifestSha256, StringComparison.Ordinal) ||
            !string.Equals(lease.TlsSpkiSha256, pins.TlsSpkiSha256, StringComparison.Ordinal) ||
            !string.Equals(lease.ClusterSystemIdentifier, pins.ClusterSystemIdentifier, StringComparison.Ordinal) ||
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

    internal static string ExactContractSha256<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions))).ToLowerInvariant();

    private async Task<T> ReadSignedAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var envelope = await response.Content.ReadFromJsonAsync<SignedAcceptanceEnvelope>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Lifecycle controller returned no signed evidence envelope.");
        var payload = Convert.FromBase64String(envelope.PayloadBase64);
        var signature = Convert.FromBase64String(envelope.SignatureBase64);
        using var verifier = ECDsa.Create();
        verifier.ImportFromPem(pins.SigningPublicKeyPem);
        if (!verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256))
            throw new InvalidOperationException("Lifecycle controller evidence signature was invalid.");
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Signed lifecycle evidence payload was absent.");
    }

    internal sealed record LeaseAllocation(Guid RequestId, Guid LeaseId, long Version, string State, string DatabaseName,
        string RuntimeConnectionString, string VerifierConnectionString, string RunId, string OwnershipNonceSha256,
        string MarkerSha256, string ClusterSystemIdentifier, string TlsSpkiSha256, string SourceCommit, string ManifestSha256,
        bool FixturePrepared, string FixtureSha256, string ContractSha256, string SigningPublicKeySha256);
    internal sealed record ReleaseEvidence(Guid LeaseId, Guid EvidenceId, string State, bool TargetAbsent, bool RolesAbsent, string EvidenceSha256, string ContractSha256, string SigningPublicKeySha256);
    internal sealed record DatabaseObjectIdentity(string Schema, string Table, string Constraint, string Function, string Trigger);
    internal sealed record AcceptanceContract(string ScenarioId, string Setup, string Action,
        string ExpectedInitialState, string ExpectedFinalState, string? ExpectedSqlState,
        string? ExpectedDatabaseObject, int ExpectedAffectedRows, bool RequiresDenial,
        bool AllowsZeroRowsTerminal, bool RequiresDecision, DatabaseObjectIdentity ExpectedIdentity,
        int ExpectedBeforeCount, int ExpectedAfterCount, string ExpectedTerminalOutcome, string ExpectedCleanupOutcome,
        string ExpectedDatabaseName, Guid ExpectedFixtureId, Guid ExpectedCommandId, Guid ExpectedAuthorizationId,
        Guid ExpectedAttemptId, Guid? ExpectedDecisionId, string ExpectedTargetInstanceSha256,
        string ExpectedBeforeSha256, string ExpectedAfterSha256, Guid ExpectedDurableEvidenceId,
        string ExpectedDurableEvidenceSha256, Guid ExpectedCleanupEvidenceId, string ExpectedCleanupEvidenceSha256,
        IReadOnlyList<string> ExpectedSubcaseEvidenceKeys)
    {
        public string ExpectedFixtureSha256 => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"REV869B-C22|{ScenarioId}|fixture"))).ToLowerInvariant();
    }
    internal sealed record SignedAcceptanceEnvelope(string PayloadBase64, string SignatureBase64);
    private sealed record AcceptancePins(string SourceCommit, string ManifestSha256, string TlsSpkiSha256,
        string ClusterSystemIdentifier, string SigningPublicKeyPem, string SigningPublicKeySha256);
    internal sealed record AcceptanceEvidence(string ScenarioId, string ContractSha256, Guid RunId, Guid LeaseId,
        Guid CommandId, Guid AuthorizationId, Guid AttemptId,
        Guid? DecisionId, Guid FixtureId, string FixtureSha256, string DatabaseName, string TargetInstanceSha256, string ClusterSystemIdentifier,
        string TlsSpkiSha256, string SourceCommit, string ManifestSha256, string SigningPublicKeySha256, string Setup, string Action, string ActionPerformed,
        bool SetupCompleted, bool ActionReached, int AffectedRows, int DurableEvidenceCount,
        Guid DurableEvidenceId, string DurableEvidenceSha256, Guid CleanupEvidenceId, string CleanupEvidenceSha256, IReadOnlyList<string> SubcaseEvidenceKeys, int UnrelatedMutationCount, bool CleanupFinalized,
        bool TargetAbsent, bool RolesAbsent, string? SqlState, string? DatabaseObject,
        string InitialState, string InitialStateSha256, string FinalState, string FinalStateSha256,
        int BeforeCount, string BeforeSha256, int AfterCount, string AfterSha256,
        DatabaseObjectIdentity DatabaseIdentity, string TerminalOutcome, string CleanupOutcome, JsonElement Metrics);
}
