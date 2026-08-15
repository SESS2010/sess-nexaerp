using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Staged acceptance client. Signed controller messages are correlation inputs only; all verdicts are
/// calculated locally from separately queried verifier/audit observations.
/// </summary>
internal sealed class Rev869BLifecycleControllerClient : IAsyncDisposable
{
    internal const string OptIn = "ISOLATED_REV869B_BEHAVIOR_TESTS";
    private readonly HttpClient actionHttp;
    private readonly HttpClient auditHttp;
    private readonly AcceptancePins pins;

    private Rev869BLifecycleControllerClient(HttpClient actionHttp, HttpClient auditHttp, AcceptancePins pins)
    {
        this.actionHttp = actionHttp;
        this.auditHttp = auditHttp;
        this.pins = pins;
    }

    internal static Rev869BLifecycleControllerClient Create()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("REV869B_POSTGRES_OPT_IN"), OptIn, StringComparison.Ordinal))
            throw new InvalidOperationException("Explicit isolated REV869B PostgreSQL opt-in is required.");

        var actionUri = ExactHttpsOrigin(Required("REV869B_LIFECYCLE_CONTROLLER_URL"), Required("REV869B_EXPECTED_CONTROLLER_ORIGIN"));
        var auditUri = ExactHttpsOrigin(Required("REV869B_CONTROLLER_AUDIT_URL"), Required("REV869B_EXPECTED_CONTROLLER_AUDIT_ORIGIN"));
        if (actionUri.GetLeftPart(UriPartial.Authority) == auditUri.GetLeftPart(UriPartial.Authority))
            throw new InvalidOperationException("Action and independent audit origins must be distinct.");

        var pins = new AcceptancePins(
            Required("REV869B_EXPECTED_SOURCE_COMMIT"),
            Required("REV869B_EXPECTED_MANIFEST_SHA256").ToLowerInvariant(),
            Required("REV869B_EXPECTED_TLS_SPKI_SHA256").ToLowerInvariant(),
            Required("REV869B_EXPECTED_CLUSTER_SYSTEM_IDENTIFIER"),
            Required("REV869B_CONTROLLER_SIGNING_PUBLIC_KEY_PEM"),
            Required("REV869B_CONTROLLER_SIGNING_PUBLIC_KEY_SHA256").ToLowerInvariant(),
            Required("REV869B_AUDIT_SIGNING_PUBLIC_KEY_PEM"),
            Required("REV869B_AUDIT_SIGNING_PUBLIC_KEY_SHA256").ToLowerInvariant());

        if (pins.SourceCommit.Length != 40 || pins.SourceCommit.Any(c => !Uri.IsHexDigit(c)) ||
            !ExactSha256(pins.ManifestSha256) || !ExactSha256(pins.TlsSpkiSha256) ||
            pins.ClusterSystemIdentifier.Length is < 10 or > 20 || pins.ClusterSystemIdentifier.Any(c => !char.IsDigit(c)) ||
            !ExactPinnedKey(pins.ControllerSigningPublicKeyPem, pins.ControllerSigningPublicKeySha256) ||
            !ExactPinnedKey(pins.AuditSigningPublicKeyPem, pins.AuditSigningPublicKeySha256) ||
            CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(pins.ControllerSigningPublicKeySha256),
                Convert.FromHexString(pins.AuditSigningPublicKeySha256)))
            throw new InvalidOperationException("Complete independent controller and audit pins are required.");

        return new(
            NewPinnedClient(actionUri, pins.TlsSpkiSha256),
            NewPinnedClient(auditUri, pins.TlsSpkiSha256),
            pins);
    }

    internal async Task<LeaseAllocation> AllocateAsync(string scenario, string family, CancellationToken ct = default)
    {
        var requestId = Guid.NewGuid();
        var contractSha256 = ExactContractSha256(new { requestId, scenario, family, pins.SourceCommit, pins.ManifestSha256, pins.ClusterSystemIdentifier });
        using var response = await actionHttp.PostAsJsonAsync("v1/rev869b/test-leases",
            new { requestId, scenario, family, contractSha256 }, ct);
        response.EnsureSuccessStatusCode();
        var lease = await ReadSignedAsync<LeaseAllocation>(response, pins.ControllerSigningPublicKeyPem, ct);
        if (lease.RequestId != requestId || lease.LeaseId == Guid.Empty || lease.Version < 1 || lease.State != "InUse" ||
            !lease.FixturePrepared || !ExactSha256(lease.FixtureSha256) || lease.ContractSha256 != contractSha256 ||
            lease.SourceCommit != pins.SourceCommit || lease.ManifestSha256 != pins.ManifestSha256 ||
            lease.ClusterSystemIdentifier != pins.ClusterSystemIdentifier ||
            !lease.DatabaseName.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Lifecycle allocation did not bind the exact request and pins.");
        RequireTargetConnection(lease.RuntimeConnectionString, lease.DatabaseName, "nexa_rev869b_app_runtime");
        RequireTargetConnection(lease.VerifierConnectionString, lease.DatabaseName, "nexa_rev869b_target_verifier");
        return lease;
    }

    internal Task ReleaseAsync(Guid leaseId, Guid requestId, CancellationToken ct = default) =>
        RequestCleanupAsync(leaseId, requestId, ct);
    internal async Task<AcceptanceResult> RunAcceptanceScenarioAsync(AcceptanceContract contract, CancellationToken ct = default)
    {
        ValidateContract(contract);
        var runId = Guid.NewGuid();
        var descriptorSha256 = ExactContractSha256(contract.Descriptor);
        var preparation = await PrepareAsync(contract, runId, descriptorSha256, ct);
        RequirePreparation(contract, runId, descriptorSha256, preparation);

        var before = await ObserveAsync(contract.Plan.Before, preparation, ct);
        var action = await ActAsync(contract, preparation, runId, descriptorSha256, ct);
        var after = await ObserveAsync(contract.Plan.After, preparation, ct);
        var durable = await ObserveAsync(contract.Plan.Durable, preparation, ct);
        var independentAudit = await ObserveAsync(contract.Plan.Audit, preparation, ct);

        var cleanupRequestId = Guid.NewGuid();
        await RequestCleanupAsync(preparation.LeaseId, cleanupRequestId, ct);
        var cleanup = await ObserveAsync(contract.Plan.Cleanup, preparation, ct);

        var bundle = new EvidenceBundle(before, after, durable, independentAudit, cleanup, ActionObservation(action));
        var failures = contract.Plan.Assertions.Where(assertion => !Evaluate(assertion, bundle)).Select(x => x.AssertionId).ToArray();
        if (failures.Length != 0)
            throw new InvalidOperationException("Independent acceptance formula failed: " + string.Join(",", failures));

        if (action.RunId != runId || action.ScenarioId != contract.ScenarioId ||
            action.LeaseId != preparation.LeaseId || action.FixtureId != preparation.FixtureId ||
            action.CommandId != preparation.CommandId || action.AuthorizationId != preparation.AuthorizationId ||
            action.AttemptId != preparation.AttemptId || action.DecisionId != preparation.DecisionId)
            throw new InvalidOperationException("Signed action correlation did not bind the prepared immutable identities.");

        return new AcceptanceResult(contract.ScenarioId, runId, preparation.LeaseId, preparation.FixtureId,
            preparation.CommandId, preparation.AuthorizationId, preparation.AttemptId, preparation.DecisionId,
            before, after, durable, independentAudit, cleanup, action, failures);
    }

    internal static void ValidateContract(AcceptanceContract contract)
    {
        static void Required(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(field + " is required.");
        }

        Required(contract.ScenarioId, nameof(contract.ScenarioId));
        Required(contract.Setup, nameof(contract.Setup));
        Required(contract.Action, nameof(contract.Action));
        Required(contract.Plan.FixtureOperationId, nameof(contract.Plan.FixtureOperationId));
        Required(contract.Plan.ActionOperationId, nameof(contract.Plan.ActionOperationId));
        Required(contract.Plan.CleanupOperationId, nameof(contract.Plan.CleanupOperationId));
        Required(contract.Plan.ExactFormula, nameof(contract.Plan.ExactFormula));

        var reads = new[] { contract.Plan.Before, contract.Plan.After, contract.Plan.Durable, contract.Plan.Audit, contract.Plan.Cleanup };
        if (reads.Select(x => x.ReadId).Distinct(StringComparer.Ordinal).Count() != reads.Length ||
            reads.Any(x => string.IsNullOrWhiteSpace(x.ReadId) || string.IsNullOrWhiteSpace(x.Purpose)) ||
            contract.Plan.Assertions.Count == 0 || contract.Plan.Mutations.Count == 0 ||
            contract.Plan.Assertions.Select(x => x.AssertionId).Distinct(StringComparer.Ordinal).Count() != contract.Plan.Assertions.Count ||
            contract.Plan.Mutations.Select(x => x.MutationId).Distinct(StringComparer.Ordinal).Count() != contract.Plan.Mutations.Count)
            throw new ArgumentException("Every scenario requires unique executable reads, assertions and semantic mutants.");

        if (contract.Plan.Assertions.Any(x => !x.AssertionId.StartsWith(contract.ScenarioId + ":", StringComparison.Ordinal)) ||
            contract.Plan.Mutations.Any(x => !x.MutationId.StartsWith(contract.ScenarioId + ":", StringComparison.Ordinal)) ||
            contract.RequiredSubcases.Count == 0 ||
            contract.RequiredSubcases.Select(x => x.SubcaseId).Distinct(StringComparer.Ordinal).Count() != contract.RequiredSubcases.Count ||
            contract.RequiredSubcases.Any(x => !x.SubcaseId.StartsWith(contract.ScenarioId + ":", StringComparison.Ordinal)))
            throw new ArgumentException("Scenario evidence must be scenario-local and exhaustive.");

        if (contract.Plan.Assertions.Any(x => x.Operator == EvidenceOperator.EqualsLiteral &&
            string.Equals(x.Expected, "PASS", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Self-asserted PASS values are prohibited.");

        if (contract.Plan.Before.ReadId == contract.Plan.After.ReadId ||
            contract.Plan.Durable.ReadId == contract.Plan.Cleanup.ReadId ||
            contract.Plan.Mutations.Any(x => !reads.Any(r => r.ReadId == x.TargetReadId) && x.TargetReadId != contract.Plan.ActionOperationId && !contract.Plan.Assertions.Any(a => a.AssertionId == x.TargetReadId)))
            throw new ArgumentException("Before/after/durable/cleanup and mutation targets must be independently bound.");

        if (contract.ScenarioId is "P02" or "P03")
        {
            var serialized = JsonSerializer.Serialize(contract.Descriptor);
            if (serialized.Contains("22012", StringComparison.Ordinal) ||
                serialized.Contains("int4div", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("P02/P03 sentinel evidence is prohibited.");
        }
    }

    internal static bool Evaluate(EvidenceAssertion assertion, EvidenceBundle bundle)
    {
        var observation = bundle.For(assertion.Stage);
        var value = Resolve(observation.Document.RootElement, assertion.JsonPath);
        return assertion.Operator switch
        {
            EvidenceOperator.Exists => value is not null,
            EvidenceOperator.Absent => value is null || value.Value.ValueKind == JsonValueKind.Null,
            EvidenceOperator.EqualsLiteral => value is not null && string.Equals(Scalar(value.Value), assertion.Expected, StringComparison.Ordinal),
            EvidenceOperator.NotEqualsLiteral => value is not null && !string.Equals(Scalar(value.Value), assertion.Expected, StringComparison.Ordinal),
            EvidenceOperator.GreaterThanZero => value is not null && value.Value.TryGetInt64(out var n) && n > 0,
            EvidenceOperator.Zero => value is not null && value.Value.TryGetInt64(out var n) && n == 0,
            EvidenceOperator.ExactSha256 => value is not null && value.Value.ValueKind == JsonValueKind.String && ExactSha256(value.Value.GetString()!),
            EvidenceOperator.SameCanonicalSha256AsBefore => observation.CanonicalSha256 == bundle.Before.CanonicalSha256,
            EvidenceOperator.DifferentCanonicalSha256FromBefore => observation.CanonicalSha256 != bundle.Before.CanonicalSha256,
            EvidenceOperator.EqualsObservationPath => CompareObservationPath(assertion.Expected, value, bundle, equal: true),
            EvidenceOperator.NotEqualsObservationPath => CompareObservationPath(assertion.Expected, value, bundle, equal: false),
            EvidenceOperator.AtMostOne => value is not null && value.Value.TryGetInt64(out var n) && n <= 1,
            EvidenceOperator.ExactlyOneTrue => ExactlyOneTrue(assertion.Expected, bundle),
            _ => false
        };
    }

    private static bool CompareObservationPath(string reference, JsonElement? actual, EvidenceBundle bundle, bool equal)
    {
        var parts = reference.Split(':', 2);
        if (actual is null || parts.Length != 2 || !Enum.TryParse<EvidenceStage>(parts[0], out var stage)) return false;
        var expected = Resolve(bundle.For(stage).Document.RootElement, parts[1]);
        if (expected is null) return false;
        var same = string.Equals(Scalar(actual.Value), Scalar(expected.Value), StringComparison.Ordinal);
        return equal ? same : !same;
    }

    private static bool ExactlyOneTrue(string references, EvidenceBundle bundle)
    {
        var values = references.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(reference =>
        {
            var parts = reference.Split(':', 2);
            if (parts.Length != 2 || !Enum.TryParse<EvidenceStage>(parts[0], out var stage)) return false;
            var value = Resolve(bundle.For(stage).Document.RootElement, parts[1]);
            return value is not null && value.Value.ValueKind == JsonValueKind.True;
        });
        return values.Count(value => value) == 1;
    }
    internal static AcceptanceContract ApplyMutation(AcceptanceContract contract, SemanticMutation mutation)
    {
        var plan = mutation.Kind switch
        {
            MutationKind.RemoveAction => contract.Plan with { ActionOperationId = string.Empty },
            MutationKind.RemoveRead => contract.Plan with
            {
                Before = contract.Plan.Before.ReadId == mutation.TargetReadId ? contract.Plan.Before with { ReadId = string.Empty } : contract.Plan.Before,
                After = contract.Plan.After.ReadId == mutation.TargetReadId ? contract.Plan.After with { ReadId = string.Empty } : contract.Plan.After,
                Durable = contract.Plan.Durable.ReadId == mutation.TargetReadId ? contract.Plan.Durable with { ReadId = string.Empty } : contract.Plan.Durable,
                Audit = contract.Plan.Audit.ReadId == mutation.TargetReadId ? contract.Plan.Audit with { ReadId = string.Empty } : contract.Plan.Audit,
                Cleanup = contract.Plan.Cleanup.ReadId == mutation.TargetReadId ? contract.Plan.Cleanup with { ReadId = string.Empty } : contract.Plan.Cleanup
            },
            MutationKind.RemoveAssertion => contract.Plan with { Assertions = contract.Plan.Assertions.Where(x => x.AssertionId != mutation.TargetReadId).ToArray() },
            MutationKind.FabricateEvidence => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Select((x, i) => i == 0 ? x with { Expected = "fabricated" } : x).ToArray()
            },
            MutationKind.DuplicateEvidence => contract.Plan with { Durable = contract.Plan.After },
            MutationKind.SubstituteIdentity => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Append(new(contract.ScenarioId + ":substituted-identity",
                    EvidenceStage.Durable, "targetIdentity.instanceId", EvidenceOperator.EqualsLiteral, Guid.Empty.ToString())).ToArray()
            },
            MutationKind.StaleEvidence => contract.Plan with { Before = contract.Plan.After },
            MutationKind.CrossInstanceEvidence => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Append(new(contract.ScenarioId + ":cross-instance",
                    EvidenceStage.After, "targetIdentity.databaseName", EvidenceOperator.EqualsLiteral, "foreign_target")).ToArray()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        return contract with { Plan = plan };
    }

    private async Task<ScenarioPreparation> PrepareAsync(AcceptanceContract contract, Guid runId, string descriptorSha256, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync("v1/rev869b/acceptance/prepare", new
        {
            runId,
            contract.ScenarioId,
            contract.Plan.FixtureOperationId,
            contract.Plan.ActionOperationId,
            descriptorSha256,
            pins.SourceCommit,
            pins.ManifestSha256,
            pins.ClusterSystemIdentifier
        }, ct);
        response.EnsureSuccessStatusCode();
        return await ReadSignedAsync<ScenarioPreparation>(response, pins.ControllerSigningPublicKeyPem, ct);
    }

    private async Task<ActionReceipt> ActAsync(AcceptanceContract contract, ScenarioPreparation preparation, Guid runId, string descriptorSha256, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync($"v1/rev869b/acceptance/{contract.ScenarioId}/actions", new
        {
            runId,
            preparation.LeaseId,
            preparation.FixtureId,
            preparation.CommandId,
            preparation.AuthorizationId,
            preparation.AttemptId,
            preparation.DecisionId,
            actionOperationId = contract.Plan.ActionOperationId,
            descriptorSha256
        }, ct);
        var receipt = await ReadSignedAsync<ActionReceipt>(response, pins.ControllerSigningPublicKeyPem, ct);
        if (receipt.HttpStatus != (int)response.StatusCode)
            throw new InvalidOperationException("Signed action status did not match the transport status.");
        return receipt;
    }

    private async Task<EvidenceObservation> ObserveAsync(EvidenceRead read, ScenarioPreparation preparation, CancellationToken ct)
    {
        if (read.Surface == EvidenceSurface.ControllerAudit)
        {
            using var response = await auditHttp.GetAsync($"v1/rev869b/audit/{preparation.RunId}/{read.ReadId}", ct);
            response.EnsureSuccessStatusCode();
            var audit = await ReadSignedAsync<JsonElement>(response, pins.AuditSigningPublicKeyPem, ct);
            return CanonicalObservation(read.ReadId, audit);
        }

        var connectionString = read.Surface is EvidenceSurface.ControlLifecycle or EvidenceSurface.ControlAcl
            ? preparation.ControlPlaneVerifierConnectionString
            : preparation.TargetVerifierConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = BuildReadCommand(read.Surface, connection, preparation);
        var scalar = await command.ExecuteScalarAsync(ct) as string
            ?? throw new InvalidOperationException("Independent verifier query returned no evidence.");
        using var document = JsonDocument.Parse(scalar);
        return CanonicalObservation(read.ReadId, document.RootElement);
    }

    private static NpgsqlCommand BuildReadCommand(EvidenceSurface surface, NpgsqlConnection connection, ScenarioPreparation p)
    {
        var sql = surface switch
        {
            EvidenceSurface.ControlLifecycle => "SELECT nexa.rev869b_read_lifecycle_evidence(@lease_id,@attempt_id,@request_id,@decision_id)::text",
            EvidenceSurface.ControlAcl => "SELECT nexa.rev869b_read_control_plane_acl_evidence()::text",
            EvidenceSurface.TargetCommand => "SELECT nexa.rev869b_read_command_evidence(@command_id,@attempt_id)::text",
            EvidenceSurface.TargetPurge => "SELECT nexa.rev869b_read_purge_evidence(@authorization_id,@attempt_id)::text",
            EvidenceSurface.TargetExport => "SELECT nexa.rev869b_read_export_evidence(@authorization_id,@batch_id,@release_id)::text",
            EvidenceSurface.TargetAcl => "SELECT nexa.rev869b_read_target_acl_evidence()::text",
            _ => throw new InvalidOperationException("Unsupported database evidence surface.")
        };
        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("lease_id", p.LeaseId);
        command.Parameters.AddWithValue("attempt_id", p.AttemptId);
        command.Parameters.AddWithValue("request_id", p.RegistrationRequestId);
        command.Parameters.Add("decision_id", NpgsqlDbType.Uuid).Value = (object?)p.DecisionId ?? DBNull.Value;
        command.Parameters.AddWithValue("command_id", p.CommandId);
        command.Parameters.AddWithValue("authorization_id", p.AuthorizationId);
        command.Parameters.AddWithValue("batch_id", p.BatchId);
        command.Parameters.Add("release_id", NpgsqlDbType.Uuid).Value = (object?)p.ReleaseId ?? DBNull.Value;
        return command;
    }

    private async Task RequestCleanupAsync(Guid leaseId, Guid requestId, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync($"v1/rev869b/test-leases/{leaseId}/release", new { leaseId, requestId }, ct);
        response.EnsureSuccessStatusCode();
        var receipt = await ReadSignedAsync<CleanupReceipt>(response, pins.ControllerSigningPublicKeyPem, ct);
        if (receipt.LeaseId != leaseId || receipt.RequestId != requestId || receipt.EvidenceId == Guid.Empty || !ExactSha256(receipt.EvidenceSha256))
            throw new InvalidOperationException("Cleanup receipt correlation failed.");
    }

    private void RequirePreparation(AcceptanceContract contract, Guid runId, string descriptorSha256, ScenarioPreparation p)
    {
        var ids = new[] { p.RunId, p.LeaseId, p.FixtureId, p.CommandId, p.AuthorizationId, p.AttemptId, p.RegistrationRequestId, p.BatchId };
        if (p.RunId != runId || p.ScenarioId != contract.ScenarioId || p.DescriptorSha256 != descriptorSha256 ||
            ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length ||
            !ExactSha256(p.TargetInstanceSha256) || !ExactSha256(p.FixtureSha256) ||
            p.SourceCommit != pins.SourceCommit || p.ManifestSha256 != pins.ManifestSha256 ||
            p.ClusterSystemIdentifier != pins.ClusterSystemIdentifier ||
            !p.DatabaseName.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Preparation did not bind exact unique scenario identities and pins.");

        RequireTargetConnection(p.TargetVerifierConnectionString, p.DatabaseName, "nexa_rev869b_target_verifier");
        RequireTargetConnection(p.ControlPlaneVerifierConnectionString, Rev869BControlPlaneProvisioningContract.Database, "nexa_rev869b_control_plane_verifier");
        if ((p.TargetVerifierConnectionString + p.ControlPlaneVerifierConnectionString)
            .Contains("lifecycle_administrator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Lifecycle administrator credentials must never enter tests.");
    }

    private static EvidenceObservation ActionObservation(ActionReceipt action)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(action, JsonOptions));
        return CanonicalObservation("signed-action-correlation", doc.RootElement);
    }

    private static EvidenceObservation CanonicalObservation(string readId, JsonElement element)
    {
        var canonical = JsonSerializer.Serialize(CanonicalValue(element), JsonOptions);
        return new(readId, JsonDocument.Parse(canonical), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), CountFacts(element));
    }

    private static object? CanonicalValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToDictionary(x => x.Name, x => CanonicalValue(x.Value), StringComparer.Ordinal),
        JsonValueKind.Array => element.EnumerateArray().Select(CanonicalValue).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var n) => n,
        JsonValueKind.Number => element.GetDecimal(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static int CountFacts(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Sum(x => CountFacts(x.Value)),
        JsonValueKind.Array => element.EnumerateArray().Sum(CountFacts),
        JsonValueKind.Null or JsonValueKind.Undefined => 0,
        _ => 1
    };

    private static JsonElement? Resolve(JsonElement root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }
        return current;
    }

    private static string Scalar(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString()! : value.GetRawText();

    private static Uri ExactHttpsOrigin(string endpoint, string expectedOrigin)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            !string.Equals(uri.GetLeftPart(UriPartial.Authority), expectedOrigin.TrimEnd('/'), StringComparison.Ordinal))
            throw new InvalidOperationException("The exact pinned HTTPS origin is required.");
        return uri;
    }

    private static HttpClient NewPinnedClient(Uri uri, string tlsSpkiSha256)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            errors == SslPolicyErrors.None && certificate is not null && CertificateSpkiSha256(certificate) == tlsSpkiSha256;
        return new HttpClient(handler) { BaseAddress = uri, Timeout = TimeSpan.FromMinutes(5) };
    }

    private static void RequireTargetConnection(string value, string database, string role)
    {
        var builder = new NpgsqlConnectionStringBuilder(value);
        if (!string.Equals(builder.Database, database, StringComparison.Ordinal) ||
            !string.Equals(builder.Username, role, StringComparison.Ordinal) || builder.Pooling)
            throw new InvalidOperationException("A nonpooled exact verifier connection is required.");
    }

    private static async Task<T> ReadSignedAsync<T>(HttpResponseMessage response, string publicKeyPem, CancellationToken ct)
    {
        var envelope = await response.Content.ReadFromJsonAsync<SignedAcceptanceEnvelope>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Signed evidence envelope was absent.");
        var payload = Convert.FromBase64String(envelope.PayloadBase64);
        using var verifier = ECDsa.Create();
        verifier.ImportFromPem(publicKeyPem);
        if (!verifier.VerifyData(payload, Convert.FromBase64String(envelope.SignatureBase64), HashAlgorithmName.SHA256))
            throw new InvalidOperationException("Signed evidence signature was invalid.");
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Signed evidence payload was absent.");
    }

    internal static string ExactContractSha256<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions))).ToLowerInvariant();

    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException(name + " is required.");

    private static bool ExactPinnedKey(string pem, string sha) => ExactSha256(sha) &&
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(pem)), Convert.FromHexString(sha));

    private static bool ExactSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string CertificateSpkiSha256(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null) return Convert.ToHexString(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
        using var ecdsa = certificate.GetECDsaPublicKey();
        return ecdsa is null ? string.Empty : Convert.ToHexString(SHA256.HashData(ecdsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
    }

    public ValueTask DisposeAsync()
    {
        actionHttp.Dispose();
        auditHttp.Dispose();
        return ValueTask.CompletedTask;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal enum EvidenceSurface { ControlLifecycle, ControlAcl, TargetCommand, TargetPurge, TargetExport, TargetAcl, ControllerAudit }
    internal enum EvidenceStage { Before, After, Durable, Audit, Cleanup, Action }
    internal enum EvidenceOperator { Exists, Absent, EqualsLiteral, NotEqualsLiteral, GreaterThanZero, Zero, ExactSha256, SameCanonicalSha256AsBefore, DifferentCanonicalSha256FromBefore, EqualsObservationPath, NotEqualsObservationPath, AtMostOne, ExactlyOneTrue }
    internal enum MutationKind { RemoveAction, RemoveRead, RemoveAssertion, FabricateEvidence, DuplicateEvidence, SubstituteIdentity, StaleEvidence, CrossInstanceEvidence }

    internal sealed record EvidenceRead(string ReadId, EvidenceSurface Surface, string Purpose);
    internal sealed record EvidenceAssertion(string AssertionId, EvidenceStage Stage, string JsonPath, EvidenceOperator Operator, string Expected);
    internal sealed record SemanticMutation(string MutationId, MutationKind Kind, string TargetReadId);
    internal sealed record ScenarioEvidencePlan(string FixtureOperationId, string ActionOperationId, string CleanupOperationId,
        EvidenceRead Before, EvidenceRead After, EvidenceRead Durable, EvidenceRead Audit, EvidenceRead Cleanup,
        string ExactFormula, IReadOnlyList<EvidenceAssertion> Assertions, IReadOnlyList<SemanticMutation> Mutations);
    internal sealed record AcceptanceDescriptor(string ScenarioId, string Setup, string Action, string ExpectedResult,
        DatabaseObjectIdentity ExpectedIdentity, ScenarioEvidencePlan Plan, IReadOnlyList<string> Subcases);
    internal sealed record AcceptanceContract(string ScenarioId, string Setup, string Action, string ExpectedResult,
        DatabaseObjectIdentity ExpectedIdentity, ScenarioEvidencePlan Plan, IReadOnlyList<SubcaseRequirement> RequiredSubcases)
    {
        internal AcceptanceDescriptor Descriptor => new(ScenarioId, Setup, Action, ExpectedResult, ExpectedIdentity,
            Plan, RequiredSubcases.Select(x => x.SubcaseId).ToArray());
    }

    internal sealed record SubcaseRequirement(string SubcaseId, string ExpectedResult);
    internal sealed record DatabaseObjectIdentity(string Schema, string Table, string Constraint, string Function, string Trigger);
    internal sealed record EvidenceObservation(string ReadId, JsonDocument Document, string CanonicalSha256, int FactCount);
    internal sealed record EvidenceBundle(EvidenceObservation Before, EvidenceObservation After, EvidenceObservation Durable,
        EvidenceObservation Audit, EvidenceObservation Cleanup, EvidenceObservation Action)
    {
        internal EvidenceObservation For(EvidenceStage stage) => stage switch
        {
            EvidenceStage.Before => Before,
            EvidenceStage.After => After,
            EvidenceStage.Durable => Durable,
            EvidenceStage.Audit => Audit,
            EvidenceStage.Cleanup => Cleanup,
            EvidenceStage.Action => Action,
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
    }

    internal sealed record AcceptanceResult(string ScenarioId, Guid RunId, Guid LeaseId, Guid FixtureId, Guid CommandId,
        Guid AuthorizationId, Guid AttemptId, Guid? DecisionId, EvidenceObservation Before, EvidenceObservation After,
        EvidenceObservation Durable, EvidenceObservation Audit, EvidenceObservation Cleanup, ActionReceipt Action,
        IReadOnlyList<string> FailedAssertions)
    {
        internal string FinalState => Action.TerminalState;
    }

    internal sealed record LeaseAllocation(Guid RequestId, Guid LeaseId, long Version, string State, string DatabaseName,
        string RuntimeConnectionString, string VerifierConnectionString, string RunId, string OwnershipNonceSha256,
        string MarkerSha256, string ClusterSystemIdentifier, string TlsSpkiSha256, string SourceCommit, string ManifestSha256,
        bool FixturePrepared, string FixtureSha256, string ContractSha256, string SigningPublicKeySha256);

    internal sealed record ScenarioPreparation(Guid RunId, string ScenarioId, Guid LeaseId, Guid FixtureId,
        Guid CommandId, Guid AuthorizationId, Guid AttemptId, Guid RegistrationRequestId, Guid? DecisionId,
        Guid BatchId, Guid? ReleaseId, string DatabaseName, string TargetInstanceSha256, string FixtureSha256,
        string TargetVerifierConnectionString, string ControlPlaneVerifierConnectionString, string SourceCommit,
        string ManifestSha256, string ClusterSystemIdentifier, string DescriptorSha256);

    internal sealed record ActionReceipt(Guid RunId, string ScenarioId, Guid LeaseId, Guid FixtureId, Guid CommandId,
        Guid AuthorizationId, Guid AttemptId, Guid? DecisionId, bool ActionReached, int AffectedRows,
        string? SqlState, string? ErrorCode, string? DatabaseObject, string TerminalState,
        Guid EvidenceId, string EvidenceSha256, Guid ControllerInstanceId, int HttpStatus);

    internal sealed record CleanupReceipt(Guid LeaseId, Guid RequestId, Guid EvidenceId, string EvidenceSha256);
    internal sealed record SignedAcceptanceEnvelope(string PayloadBase64, string SignatureBase64);
    private sealed record AcceptancePins(string SourceCommit, string ManifestSha256, string TlsSpkiSha256,
        string ClusterSystemIdentifier, string ControllerSigningPublicKeyPem, string ControllerSigningPublicKeySha256,
        string AuditSigningPublicKeyPem, string AuditSigningPublicKeySha256);
}