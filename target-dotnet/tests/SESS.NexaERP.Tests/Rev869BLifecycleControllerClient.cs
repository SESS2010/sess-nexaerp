using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        AcceptanceResult? last = null;
        foreach (var subcase in contract.RequiredSubcases)
            last = await RunAcceptanceSubcaseAsync(contract, subcase, ct);
        return last ?? throw new InvalidOperationException("A frozen scenario requires at least one subcase.");
    }

    private async Task<AcceptanceResult> RunAcceptanceSubcaseAsync(AcceptanceContract contract,
        SubcaseRequirement subcase, CancellationToken ct)
    {
        var runId = Guid.NewGuid();
        var descriptorSha256 = ExactContractSha256(contract.Descriptor);
        var preparation = await PrepareAsync(contract, subcase, runId, descriptorSha256, ct);
        RequirePreparation(contract, subcase, runId, descriptorSha256, preparation);

        var before = await ObserveAsync(contract, subcase, EvidenceStage.Before, contract.Plan.Before, preparation, ct);
        var action = await ActAsync(contract, subcase, preparation, runId, descriptorSha256, ct);
        var after = await ObserveAsync(contract, subcase, EvidenceStage.After, contract.Plan.After, preparation, ct);
        var durable = await ObserveAsync(contract, subcase, EvidenceStage.Durable, contract.Plan.Durable, preparation, ct);
        var independentAudit = await ObserveAsync(contract, subcase, EvidenceStage.Audit, contract.Plan.Audit, preparation, ct);

        var cleanupRequestId = Guid.NewGuid();
        await RequestCleanupAsync(preparation.LeaseId, cleanupRequestId, ct);
        var cleanup = await ObserveAsync(contract, subcase, EvidenceStage.Cleanup, contract.Plan.Cleanup, preparation, ct);

        var bundle = new EvidenceBundle(before, after, durable, independentAudit, cleanup, ActionObservation(action));
        var failures = VerifyEvidence(contract, subcase, bundle);
        if (failures.Length != 0)
            throw new InvalidOperationException("Independent acceptance formula failed: " + string.Join(",", failures));

        if (action.RunId != runId || action.ScenarioId != contract.ScenarioId || action.SubcaseId != subcase.SubcaseId ||
            action.PreparationId != subcase.PreparationId || action.ExpectedResultId != subcase.ExpectedResultId ||
            action.LeaseId != preparation.LeaseId || action.FixtureId != preparation.FixtureId ||
            action.CommandId != preparation.CommandId || action.AuthorizationId != preparation.AuthorizationId ||
            action.AttemptId != preparation.AttemptId || action.DecisionId != preparation.DecisionId ||
            action.EvidenceId != subcase.EvidenceId || !ExactSha256(action.EvidenceSha256) ||
            action.TerminalState != subcase.ExpectedResult)
            throw new InvalidOperationException("Signed action correlation did not bind the prepared immutable identities.");

        return new AcceptanceResult(contract.ScenarioId, subcase.SubcaseId, runId, preparation.LeaseId, preparation.FixtureId,
            preparation.CommandId, preparation.AuthorizationId, preparation.AttemptId, preparation.DecisionId,
            before, after, durable, independentAudit, cleanup, action, failures);
    }

    internal static void ValidateContract(AcceptanceContract contract)
    {
        Rev869BCorrection26FrozenOracle.Validate();
        Rev869BCorrection28IndependentEvidenceFixtures.Validate();
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

        var required = contract.Plan.RequiredComponentIds;
        var asserted = contract.Plan.Assertions.Select(x => x.AssertionId).ToArray();
        var components = contract.Plan.FormulaComponents;
        if (required.Count == 0 || required.Distinct(StringComparer.Ordinal).Count() != required.Count ||
            !required.ToHashSet(StringComparer.Ordinal).SetEquals(asserted) ||
            components.Count != Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId).Count ||
            components.Select(x => x.ComponentId).Distinct(StringComparer.Ordinal).Count() != components.Count ||
            components.Any(component => string.IsNullOrWhiteSpace(component.LocalReducer) ||
                !contract.Plan.Assertions.Any(assertion => assertion.AssertionId == component.ComponentId &&
                    assertion.Stage == component.Stage && assertion.JsonPath == component.AuthoritativeSelector &&
                    assertion.Operator == component.Operator && assertion.Expected == component.Expected)))
            throw new ArgumentException("Every immutable formula component must bind bijectively to one executable local reducer and assertion.");

        var oracleComponents = Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId);
        if (!oracleComponents.Select(x => x.ComponentId).ToHashSet(StringComparer.Ordinal)
                .SetEquals(components.Select(x => x.ComponentId)) ||
            components.Any(component => oracleComponents.All(oracle =>
                oracle.ComponentId != component.ComponentId || oracle.ValueType != component.ValueType ||
                oracle.ReaderId != component.ReaderId || oracle.Source != component.Source ||
                oracle.Scope != component.Scope || oracle.Cardinality != component.Cardinality ||
                oracle.NullSemantics != component.NullSemantics)))
            throw new ArgumentException("Formula assertions must match the independently frozen typed-selector oracle.");

        var oracleSubcases = Rev869BCorrection26FrozenOracle.SubcasesFor(contract.ScenarioId);
        if (contract.RequiredSubcases.Count != oracleSubcases.Count ||
            contract.RequiredSubcases.Any(subcase => oracleSubcases.All(oracle => oracle.SubcaseId != subcase.SubcaseId ||
                oracle.PreparationId != subcase.PreparationId || oracle.AttemptId != subcase.AttemptId ||
                oracle.EvidenceId != subcase.EvidenceId || oracle.ExpectedResultId != subcase.ExpectedResultId ||
                oracle.ExpectedOutcome != subcase.ExpectedResult || oracle.ActionId != subcase.ActionId)))
            throw new ArgumentException("Every subcase must retain its frozen preparation, attempt, evidence and expected-result binding.");

        if (contract.Plan.Assertions.Any(x => x.Stage == EvidenceStage.Audit ||
            x.Expected.StartsWith("Audit:", StringComparison.Ordinal) ||
            (x.Operator == EvidenceOperator.EqualsLiteral && string.Equals(x.Expected, "PASS", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Controller audit and self-asserted PASS values are prohibited as decisive evidence.");

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

    internal static string[] VerifyEvidence(AcceptanceContract contract, SubcaseRequirement subcase, EvidenceBundle bundle)
    {
        ValidateContract(contract);
        var failures = new List<string>();
        var independent = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var decisiveStages = new[] { EvidenceStage.Before, EvidenceStage.After, EvidenceStage.Durable, EvidenceStage.Cleanup };
        string? instance = null;
        string? lease = null;
        foreach (var stage in decisiveStages)
        {
            var root = bundle.For(stage).Document.RootElement;
            bool Exact(string name, string expected) => root.TryGetProperty(name, out var value) &&
                string.Equals(Scalar(value), expected, StringComparison.Ordinal);
            if (!Exact("oracleVersion", Rev869BCorrection26FrozenOracle.Version) ||
                !Exact("oracleSha256", Rev869BCorrection26FrozenOracle.ExpectedSha256) ||
                !Exact("scenarioId", contract.ScenarioId) || !Exact("subcaseId", subcase.SubcaseId) ||
                !Exact("preparationId", independent.PreparationIdentity.ToString()) ||
                !Exact("attemptId", independent.AttemptIdentity.ToString()) ||
                !Exact("evidenceId", independent.ObservationIdentity.ToString()) ||
                !Exact("expectedResultId", independent.ExpectedResultIdentity.ToString()) ||
                !Exact("expectedOutcome", subcase.ExpectedResult) ||
                !Exact("provenance", "authoritative-local-reader"))
                failures.Add("envelope:" + stage);
            var expectedEnvelopeId = "env:c28:" + independent.EnvelopeIdentity.ToString("D");
            var expectedObservationId = stage switch
            {
                EvidenceStage.Before => independent.BeforeObservationId,
                EvidenceStage.After => independent.AfterObservationId,
                EvidenceStage.Durable => independent.DurableObservationId,
                EvidenceStage.Cleanup => independent.CleanupObservationId,
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
            if (!Exact("schemaVersion", Rev869BCorrection26FrozenOracle.EvidenceSchemaVersion) ||
                !Exact("adapterVersion", Rev869BCorrection26FrozenOracle.AdapterVersion) ||
                !Exact("formulaVersion", Rev869BCorrection26FrozenOracle.FormulaVersion) ||
                !Exact("observationStage", stage.ToString()) ||
                !Exact("observationId", expectedObservationId) || !Exact("envelopeId", expectedEnvelopeId))
                failures.Add("v4-binding:" + stage);
            if (!root.TryGetProperty("leaseVersion", out var leaseVersion) ||
                !leaseVersion.TryGetInt64(out var version) || version != 7)
                failures.Add("lease-version:" + stage);
            if (!root.TryGetProperty("canonicalEvidenceSha256", out var canonicalDigest) ||
                canonicalDigest.ValueKind != JsonValueKind.String || !ExactSha256(canonicalDigest.GetString()!))
                failures.Add("canonical-digest:" + stage);
            else
            {
                var node = JsonNode.Parse(root.GetRawText())!.AsObject();
                if (!string.Equals(EnvelopeSha256(node), canonicalDigest.GetString(), StringComparison.Ordinal))
                    failures.Add("canonical-digest-mismatch:" + stage);
            }
            if (!root.TryGetProperty("asOfSequence", out var sequence) || !sequence.TryGetInt64(out var n) || n < 0)
                failures.Add("freshness:" + stage);
            if (!root.TryGetProperty("duplicateEvidenceCount", out var duplicates) ||
                !duplicates.TryGetInt64(out var duplicateCount) || duplicateCount != 0)
                failures.Add("duplicates:" + stage);
            if (!root.TryGetProperty("targetInstanceSha256", out var target) ||
                target.ValueKind != JsonValueKind.String || !ExactSha256(target.GetString()!))
                failures.Add("instance:" + stage);
            else if (instance is null) instance = target.GetString();
            else if (!string.Equals(instance, target.GetString(), StringComparison.Ordinal)) failures.Add("cross-instance:" + stage);
            if (!root.TryGetProperty("leaseBindingId", out var leaseValue) ||
                leaseValue.ValueKind != JsonValueKind.String || !Guid.TryParse(leaseValue.GetString(), out _))
                failures.Add("lease:" + stage);
            else if (lease is null) lease = leaseValue.GetString();
            else if (!string.Equals(lease, leaseValue.GetString(), StringComparison.Ordinal)) failures.Add("cross-lease:" + stage);
        }

        foreach (var stage in new[] { EvidenceStage.Before, EvidenceStage.After, EvidenceStage.Durable })
        {
            var expected = RequiredSelectorReaders(contract.ScenarioId, stage);
            if (expected.Length == 0) continue;
            var root = bundle.For(stage).Document.RootElement;
            if (!root.TryGetProperty("selectors", out var selectors) || selectors.ValueKind != JsonValueKind.Object)
                failures.Add("selectors:missing:" + stage);
            else
            {
                var actual = selectors.EnumerateObject().Select(x => x.Name).ToArray();
                if (actual.Length != actual.Distinct(StringComparer.Ordinal).Count() ||
                    !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected.Select(x => x.SelectorName)))
                    failures.Add("selectors:exact-set:" + stage);
            }
            if (!root.TryGetProperty("selectorProvenance", out var readers) || readers.ValueKind != JsonValueKind.Object ||
                expected.Any(selector => !readers.TryGetProperty(selector.SelectorName, out var provenance) ||
                    provenance.ValueKind != JsonValueKind.Object ||
                    !provenance.TryGetProperty("readerId", out var reader) ||
                    reader.ValueKind != JsonValueKind.String || reader.GetString() != selector.ReaderId ||
                    !provenance.TryGetProperty("readerSchemaVersion", out var schema) ||
                    schema.GetString() != Rev869BCorrection26FrozenOracle.ReaderContractVersion ||
                    !provenance.TryGetProperty("sourceRowCount", out var count) ||
                    !count.TryGetInt64(out var sourceCount) || sourceCount != 1 ||
                    !provenance.TryGetProperty("sourceSha256", out var sourceSha) ||
                    sourceSha.ValueKind != JsonValueKind.String || !ExactSha256(sourceSha.GetString()!)))
                failures.Add("selectors:reader-binding:" + stage);
        }

        failures.AddRange(contract.Plan.Assertions.Where(assertion => !Evaluate(assertion, bundle))
            .Select(assertion => assertion.AssertionId));
        return failures.Distinct(StringComparer.Ordinal).ToArray();
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
        var values = SplitReferences(references).Select(reference =>
        {
            var parts = reference.Split(':', 2);
            if (parts.Length != 2 || !Enum.TryParse<EvidenceStage>(parts[0], out var stage)) return false;
            var value = Resolve(bundle.For(stage).Document.RootElement, parts[1]);
            return value is not null && value.Value.ValueKind == JsonValueKind.True;
        });
        return values.Count(value => value) == 1;
    }

    // This repository contains a reference/test adapter; the production controller is externally owned.
    internal const string TrustedAdapterProductionOwnership = "EXTERNAL_PENDING";

    private static readonly RawFixtureSpec[] Correction28RawFactTemplates =
    [
        new("P01","P01:formula-pin-mismatch",EvidenceStage.Before,"CP-A4","pinMismatchCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P01","P01:formula-target-acl-delta",EvidenceStage.After,"TA4","targetAclDeltaCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P01","P01:formula-verify",EvidenceStage.Before,"CP-A4","verificationMismatchCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P02","P02:formula-pin-mismatch",EvidenceStage.Before,"CP-A4","pinMismatchCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("P02","P02:formula-lease-zero",EvidenceStage.Durable,"CP-L4","allocatedLeaseCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P02","P02:formula-action-zero",EvidenceStage.Durable,"CP-L4","lifecycleMutationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P03","P03:formula-seeded-one",EvidenceStage.Before,"CP-A4","seededDeltaCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("P03","P03:formula-reported-delta",EvidenceStage.Before,"CP-A4","reportedDeltaSha256","sha256","723fb28ebe0d9b678ebeaac85215a4f1a2d38b5a8bdf6233c73d0ef42835e64d",EvidenceStage.Before,"seededDeltaSha256","sha256","723fb28ebe0d9b678ebeaac85215a4f1a2d38b5a8bdf6233c73d0ef42835e64d","","",""),
        new("P03","P03:formula-protected-zero",EvidenceStage.Before,"CP-A4","protectedMutationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("P03","P03:formula-cleanup-baseline",EvidenceStage.Before,"CP-A4","cleanupFingerprint","sha256","f0488db27a393857a161b96c8d9132f10c49d7d6b57c6b4bb493753084723f95",EvidenceStage.Before,"baselineFingerprint","sha256","f0488db27a393857a161b96c8d9132f10c49d7d6b57c6b4bb493753084723f95","","",""),
        new("L01","L01:formula-reserved",EvidenceStage.Durable,"CP-L4","reservedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L01","L01:formula-branch-xor",EvidenceStage.Durable,"CP-L4","resumeSameAttempt_xor_authorizedCleanup","bool tuple","true",EvidenceStage.Before,"resumeSameAttempt","bool","true","authorizedCleanup","bool","false"),
        new("L01","L01:formula-duplicates-zero",EvidenceStage.Durable,"CP-L4","duplicateAttemptCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("L02","L02:formula-boundary-count",EvidenceStage.Durable,"CP-L4","boundaryCount","int64","3",EvidenceStage.Action,"","","","","",""),
        new("L02","L02:formula-started-each",EvidenceStage.Durable,"CP-L4","startedAttemptsPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L02","L02:formula-reconciled-each",EvidenceStage.Durable,"CP-L4","reconciledAttemptsPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L02","L02:formula-target-each",EvidenceStage.After,"TA4","targetCountPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L02","L02:formula-roles-each",EvidenceStage.After,"TA4","roleSetCountPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L03","L03:formula-requests",EvidenceStage.Durable,"CP-L4","cleanupRequestCount","int64","2",EvidenceStage.Action,"","","","","",""),
        new("L03","L03:formula-dropstarted",EvidenceStage.Durable,"CP-L4","dropStartedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L03","L03:formula-active",EvidenceStage.Durable,"CP-L4","activeDropAttemptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L03","L03:formula-physical",EvidenceStage.Durable,"CP-L4","normalDropTerminalChainCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L03","L03:formula-authorization-chain",EvidenceStage.Durable,"CP-L4","authorizationRegistrationTransitionCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L04","L04:formula-dropstarted",EvidenceStage.Durable,"CP-L4","dropStartedEventsPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L04","L04:formula-finalized",EvidenceStage.Durable,"CP-L4","finalizedEventsPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L04","L04:formula-physical",EvidenceStage.Durable,"CP-L4","terminalOutcomeCountPerBoundary","int64","1",EvidenceStage.Action,"","","","","",""),
        new("L04","L04:formula-target-zero",EvidenceStage.After,"TA4","targetCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("L04","L04:formula-roles-zero",EvidenceStage.After,"TA4","roleCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("L05","L05:formula-use-zero",EvidenceStage.After,"TA4","useMutationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("L05","L05:formula-drop-zero",EvidenceStage.After,"TA4","dropMutationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("L05","L05:formula-quarantine-one",EvidenceStage.Durable,"CP-L4","quarantineOutcomeCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R01","R01:formula-decision-one",EvidenceStage.Durable,"CP-L4","decisionCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R01","R01:formula-consumed-attempt",EvidenceStage.Durable,"CP-L4","consumedAttemptId","uuid","bdce61e0-cc95-6d85-699a-1e7ef06737bb",EvidenceStage.Before,"attemptId","uuid","bdce61e0-cc95-6d85-699a-1e7ef06737bb","","",""),
        new("R01","R01:formula-action",EvidenceStage.Durable,"CP-L4","authorizedAction","string/enum","reference-authorizedAction",EvidenceStage.Before,"performedAction","string/enum","reference-authorizedAction","","",""),
        new("R01","R01:formula-recovery-one",EvidenceStage.Durable,"CP-L4","recoveryAttemptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R01","R01:formula-finalized-one",EvidenceStage.Durable,"CP-L4","finalizedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R02","R02:formula-attempts-zero",EvidenceStage.Durable,"CP-L4","newAttemptCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("R02","R02:formula-events-zero",EvidenceStage.Durable,"CP-L4","newEventCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("R02","R02:formula-consumed-one",EvidenceStage.Durable,"CP-L4","decisionConsumedCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R03","R03:formula-failure-one",EvidenceStage.Durable,"CP-L4","cleanupFailureCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R03","R03:formula-old-zero",EvidenceStage.Durable,"CP-L4","oldDecisionAcceptedCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("R03","R03:formula-fresh-one",EvidenceStage.Durable,"CP-L4","freshLinkedDecisionCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R03","R03:formula-consumed-one",EvidenceStage.Durable,"CP-L4","freshDecisionConsumedCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("R03","R03:formula-finalized-one",EvidenceStage.Durable,"CP-L4","finalizedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C01","C01:formula-business-delta",EvidenceStage.Durable,"TC4","businessRowDelta","int64","7",EvidenceStage.Before,"expectedBusinessRowDelta","int64","7","","",""),
        new("C01","C01:formula-history-delta",EvidenceStage.Durable,"TC4","historyRowDelta","int64","7",EvidenceStage.Before,"expectedHistoryRowDelta","int64","7","","",""),
        new("C01","C01:formula-receipt-one",EvidenceStage.Durable,"TC4","receiptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C01","C01:formula-outcome-one",EvidenceStage.Durable,"TC4","committedOutcomeCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C01","C01:formula-active-zero",EvidenceStage.Durable,"TC4","activeAttemptCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C02","C02:formula-business-same",EvidenceStage.Durable,"TC4","businessAfter2Sha256","sha256","9c296d41e3e07e4aa9bc2c6ffaec2668f509ba450bd8ccee621bde933df5c56c",EvidenceStage.Before,"businessAfter1Sha256","sha256","9c296d41e3e07e4aa9bc2c6ffaec2668f509ba450bd8ccee621bde933df5c56c","","",""),
        new("C02","C02:formula-history-same",EvidenceStage.Durable,"TC4","historyAfter2Sha256","sha256","988d56e941cc7ab482b2dc536ed541c68e2f77fd916fcd884c2444873f14a2ba",EvidenceStage.Before,"historyAfter1Sha256","sha256","988d56e941cc7ab482b2dc536ed541c68e2f77fd916fcd884c2444873f14a2ba","","",""),
        new("C02","C02:formula-receipt-same",EvidenceStage.Durable,"TC4","receiptId2","uuid","a8d17101-86df-6af4-77b3-d8d72491e002",EvidenceStage.Before,"receiptId1","uuid","a8d17101-86df-6af4-77b3-d8d72491e002","","",""),
        new("C02","C02:formula-response-same",EvidenceStage.Durable,"TC4","responseSha2562","sha256","97ed9066bfc809451ccc8d142eafab7bbb52e262e0059611f973bb70184fd217",EvidenceStage.Before,"responseSha2561","sha256","97ed9066bfc809451ccc8d142eafab7bbb52e262e0059611f973bb70184fd217","","",""),
        new("C02","C02:formula-receipt-one",EvidenceStage.Durable,"TC4","receiptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C03","C03:formula-digest-different",EvidenceStage.Durable,"TC4","changedDigest","sha256","f78c37fa0da07a7de2f67c4032680dbbc9eb9c96ee5bf519593df34c3cf571e7",EvidenceStage.Before,"registeredDigest","sha256","86907cf155d06c04f1510a13a1c68d0efd89c14db13e5930ffd3ad2f7d67722a","","",""),
        new("C03","C03:formula-request-zero",EvidenceStage.Durable,"TC4","requestDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C03","C03:formula-attempt-zero",EvidenceStage.Durable,"TC4","attemptDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C03","C03:formula-business-zero",EvidenceStage.Durable,"TC4","businessHistoryDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C04","C04:formula-business-zero",EvidenceStage.Durable,"TC4","businessRowDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C04","C04:formula-history-zero",EvidenceStage.Durable,"TC4","historyRowDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C04","C04:formula-receipt-zero",EvidenceStage.Durable,"TC4","receiptDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C04","C04:formula-rollback-one",EvidenceStage.Durable,"TC4","rolledBackOutcomeCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C05","C05:formula-business-zero",EvidenceStage.Durable,"TC4","businessHistoryReceiptDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C05","C05:formula-rollback-one",EvidenceStage.Durable,"TC4","rolledBackOutcomeCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C05","C05:formula-opened-attempt",EvidenceStage.Durable,"TC4","openedAttemptId","uuid","55fae98e-bbf5-a573-75aa-e14cccd99af9",EvidenceStage.Before,"attemptId","uuid","55fae98e-bbf5-a573-75aa-e14cccd99af9","","",""),
        new("C06","C06:formula-subcases-four",EvidenceStage.Durable,"TC4","interruptionSubcaseCount","int64","4",EvidenceStage.Action,"","","","","",""),
        new("C06","C06:formula-distinct-evidence",EvidenceStage.Durable,"TC4","distinctEvidenceIdCount","int64","4",EvidenceStage.Action,"","","","","",""),
        new("C06","C06:formula-terminal-each",EvidenceStage.Durable,"TC4","terminalOutcomeCountPerAttempt","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C07","C07:formula-requests-two",EvidenceStage.Durable,"TC4","startRequestCount","int64","2",EvidenceStage.Action,"","","","","",""),
        new("C07","C07:formula-started-one",EvidenceStage.Durable,"TC4","startedAttemptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C07","C07:formula-active-one",EvidenceStage.Durable,"TC4","activeAttemptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("C07","C07:formula-unrelated-zero",EvidenceStage.Durable,"TC4","unrelatedMutationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C08","C08:formula-accepted-zero",EvidenceStage.Durable,"TC4","acceptedSubstitutionCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C08","C08:formula-contexts-zero",EvidenceStage.Durable,"TC4","contextDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C08","C08:formula-receipts-zero",EvidenceStage.Durable,"TC4","receiptDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("C08","C08:formula-business-zero",EvidenceStage.Durable,"TC4","businessHistoryDelta","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G01","G01:formula-attempts-zero",EvidenceStage.Durable,"TP4","startedAttemptCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G01","G01:formula-candidates-zero",EvidenceStage.Durable,"TP4","candidateCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G01","G01:formula-events-zero",EvidenceStage.Durable,"TP4","purgeEventCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G02","G02:formula-eligible-zero",EvidenceStage.Durable,"TP4","eligibleBeforeCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G02","G02:formula-frozen-zero",EvidenceStage.Durable,"TP4","frozenCandidateCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G02","G02:formula-deleted-zero",EvidenceStage.Durable,"TP4","deletedRowCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G02","G02:formula-event-one",EvidenceStage.Durable,"TP4","zeroRowsEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G03","G03:formula-eligible-positive",EvidenceStage.Durable,"TP4","eligibleBeforeCount","int64","3",EvidenceStage.Action,"","","","","",""),
        new("G03","G03:formula-frozen-equals",EvidenceStage.Durable,"TP4","frozenCandidateCount","int64","7",EvidenceStage.Before,"eligibleBeforeCount","int64","7","","",""),
        new("G03","G03:formula-deleted-equals",EvidenceStage.Durable,"TP4","deletedRowCount","int64","7",EvidenceStage.Before,"eligibleBeforeCount","int64","7","","",""),
        new("G03","G03:formula-remaining-zero",EvidenceStage.Durable,"TP4","remainingEligibleCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G03","G03:formula-event-one",EvidenceStage.Durable,"TP4","succeededEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G04","G04:formula-hash-different",EvidenceStage.Durable,"TP4","currentCandidateSha256","sha256","429a87473082fa60c0c508359fe5653666d1f4233e4b43fceb047081e91c48c2",EvidenceStage.Before,"frozenCandidateSha256","sha256","f3a2e20aa77e56b3b08d678cc0cb986d4ee6e4e69c501f7fb4a5c20816ea9169","","",""),
        new("G04","G04:formula-deleted-zero",EvidenceStage.Durable,"TP4","deletedRowCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G04","G04:formula-context-same",EvidenceStage.Durable,"TP4","contextAfterSha256","sha256","ecb8d122e442690f153ca0f0596332ed6a01e8ae0ca8892bf7b7cbbc8f4ca4e9",EvidenceStage.Before,"contextBeforeSha256","sha256","ecb8d122e442690f153ca0f0596332ed6a01e8ae0ca8892bf7b7cbbc8f4ca4e9","","",""),
        new("G04","G04:formula-event-one",EvidenceStage.Durable,"TP4","failedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G05","G05:formula-deleted-zero",EvidenceStage.Durable,"TP4","deletedRowCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("G05","G05:formula-context-same",EvidenceStage.Durable,"TP4","contextAfterSha256","sha256","70fe1b27d97bd28ac1247ca53fbad198813b0ce354888d7627f2f581d1bb1e04",EvidenceStage.Before,"contextBeforeSha256","sha256","70fe1b27d97bd28ac1247ca53fbad198813b0ce354888d7627f2f581d1bb1e04","","",""),
        new("G05","G05:formula-event-one",EvidenceStage.Durable,"TP4","failedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G06","G06:formula-starts-two",EvidenceStage.Durable,"TP4","concurrentStartCount","int64","2",EvidenceStage.Action,"","","","","",""),
        new("G06","G06:formula-consumed-one",EvidenceStage.Durable,"TP4","consumedAuthorizationCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G06","G06:formula-execution-max",EvidenceStage.Durable,"TP4","executionCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G06","G06:formula-child-one",EvidenceStage.Durable,"TP4","activeChildCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("G06","G06:formula-substituted-zero",EvidenceStage.Durable,"TP4","substitutedChildCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("E01","E01:formula-within-max",EvidenceStage.Durable,"TE4","preparedRowCountWithinMaximum","bool","true",EvidenceStage.Action,"","","","","",""),
        new("E01","E01:formula-hash",EvidenceStage.Durable,"TE4","preparedSha256","sha256","8affc33969fe0c2cc30b024f40a0587a6157858d83adb940d17d99d3c728369e",EvidenceStage.Before,"recomputedPreparedSha256","sha256","8affc33969fe0c2cc30b024f40a0587a6157858d83adb940d17d99d3c728369e","","",""),
        new("E01","E01:formula-excluded-zero",EvidenceStage.Durable,"TE4","excludedFieldCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("E01","E01:formula-event-one",EvidenceStage.Durable,"TE4","preparedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("E02","E02:formula-rows-same",EvidenceStage.Durable,"TE4","preparedAfterSha256","sha256","45551c7a61ba4cd5db776270c7b6726b04461a5ef8125ce980c986e41f72531f",EvidenceStage.Before,"preparedBeforeSha256","sha256","45551c7a61ba4cd5db776270c7b6726b04461a5ef8125ce980c986e41f72531f","","",""),
        new("E02","E02:formula-count-same",EvidenceStage.Durable,"TE4","preparedAfterCount","int64","7",EvidenceStage.Before,"preparedBeforeCount","int64","7","","",""),
        new("E02","E02:formula-later-one",EvidenceStage.Durable,"TE4","laterEligibleRowCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("E02","E02:formula-later-batch-zero",EvidenceStage.Durable,"TE4","laterRowInBatchCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("E03","E03:formula-released-zero",EvidenceStage.Durable,"TE4","releasedRowCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("E03","E03:formula-events-zero",EvidenceStage.Durable,"TE4","newReleaseEventCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("E03","E03:formula-batch-same",EvidenceStage.Durable,"TE4","preparedAfterSha256","sha256","09ade8da0f378ba349537dbc2176f44566dbcbbee95ee7e70e1224404fbc7469",EvidenceStage.Before,"preparedBeforeSha256","sha256","09ade8da0f378ba349537dbc2176f44566dbcbbee95ee7e70e1224404fbc7469","","",""),
        new("E04","E04:formula-release-distinct",EvidenceStage.Durable,"TE4","releaseId2","uuid","4fa1af19-5491-c706-a5f8-423306e67f58",EvidenceStage.Before,"releaseId1","uuid","0db9d730-5841-1d1d-719b-ed56058df91b","","",""),
        new("E04","E04:formula-prior-link",EvidenceStage.Durable,"TE4","priorReleaseId2","uuid","0db9d730-5841-1d1d-719b-ed56058df91b",EvidenceStage.Before,"releaseId1","uuid","0db9d730-5841-1d1d-719b-ed56058df91b","","",""),
        new("E04","E04:formula-active-one",EvidenceStage.Durable,"TE4","activeReleaseCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("E04","E04:formula-success-max",EvidenceStage.Durable,"TE4","deliverySuccessCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("E04","E04:formula-batch-same",EvidenceStage.Durable,"TE4","batchAfterSha256","sha256","1243f8310530efb85569efefa426a4564c38d64dd3f21b14288dd64b128a3132",EvidenceStage.Before,"batchBeforeSha256","sha256","1243f8310530efb85569efefa426a4564c38d64dd3f21b14288dd64b128a3132","","",""),
        new("A01","A01:formula-unexpected-zero",EvidenceStage.Before,"CP-A4","controlObservedMinusExpectedCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("A01","A01:formula-missing-zero",EvidenceStage.After,"TA4","targetExpectedMinusObservedCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("A01","A01:formula-dimensions",EvidenceStage.After,"TA4","targetAclDimensionCount","int64","3",EvidenceStage.Action,"","","","","",""),
        new("A02","A02:formula-allowed-zero",EvidenceStage.After,"TA4","allowedProtectedOperationCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("A02","A02:formula-tuple-count",EvidenceStage.After,"TA4","durableDenialCount","int64","7",EvidenceStage.Before,"requiredDenialTupleCount","int64","7","","",""),
        new("A02","A02:formula-fingerprint-same",EvidenceStage.After,"TA4","protectedAfterSha256","sha256","6bd8e3efe54d1bb2f9ad06a17b616f5c8f999e0300422746a5fa560509894500",EvidenceStage.Before,"protectedBeforeSha256","sha256","6bd8e3efe54d1bb2f9ad06a17b616f5c8f999e0300422746a5fa560509894500","","",""),
        new("T01","T01:formula-lease-one",EvidenceStage.Durable,"CP-L4","leaseCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T01","T01:formula-target-one",EvidenceStage.After,"TA4","targetCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T01","T01:formula-admin-zero",EvidenceStage.After,"TA4","administrativeBypassCount","int64","0",EvidenceStage.Action,"","","","","",""),
        new("T01","T01:formula-fixture",EvidenceStage.After,"TA4","fixturePrepared","bool","true",EvidenceStage.Action,"","","","","",""),
        new("T02","T02:formula-instance-different",EvidenceStage.Durable,"CP-L4","survivingAttemptCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T02","T02:formula-attempt-same",EvidenceStage.Durable,"CP-L4","reconciledAttemptId","uuid","1a131109-4a13-2dd0-7d6a-713341cce6d2",EvidenceStage.Before,"survivingAttemptId","uuid","1a131109-4a13-2dd0-7d6a-713341cce6d2","","",""),
        new("T02","T02:formula-dropstarted-one",EvidenceStage.Durable,"CP-L4","dropStartedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T02","T02:formula-finalized-one",EvidenceStage.Durable,"CP-L4","finalizedEventCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T02","T02:formula-cleanup-one",EvidenceStage.Durable,"CP-L4","cleanupEvidenceCount","int64","1",EvidenceStage.Action,"","","","","",""),
        new("T03","T03:formula-killed-equals",EvidenceStage.Durable,"OR3","killedMutants","int64","20",EvidenceStage.Before,"requiredNonEquivalentMutants","int64","20","","",""),
        new("T03","T03:formula-survivors-zero",EvidenceStage.Durable,"OR3","survivingMutants","int64","0",EvidenceStage.Action,"","","","","",""),
    ];

    internal enum PipelineMutationKind
    {
        SelectorChanged, MissingField, AdditionalField, DuplicatedField, WrongType, WrongCount,
        WrongState, FabricatedHistory, CrossCompany, CrossInstance, CrossLease, WrongLeaseVersion,
        StaleOrReplayed, WrongOracleHash, WrongObservationIdentity, WrongEnvelopeIdentity,
        MissingDurableHistory, RawDigestChanged, BroadenedAclOrPurgeScope, RemovedDecisiveAssertion
    }

    internal sealed record RawObservationSet(string CompanyId, Guid LeaseId, long LeaseVersion,
        string TargetInstanceSha256, IReadOnlyDictionary<EvidenceStage, IReadOnlyList<string>> Documents);

    private sealed record RawFixtureSpec(string ScenarioId, string ComponentId, EvidenceStage Stage,
        string ReaderId, string SelectorName, string ValueType, string ActualJson,
        EvidenceStage ReferenceStage, string ReferenceName, string ReferenceValueType, string ReferenceJson,
        string SecondReferenceName, string SecondReferenceValueType, string SecondReferenceJson);

    internal sealed record RawScopeV3(string CompanyId, string TargetInstanceSha256, Guid LeaseId,
        long LeaseVersion, Guid OperationId, Guid ScenarioExecutionId, string SubcaseId, string Stage);

    internal sealed record TypedFactV3(string Kind, string Name, string ValueType, JsonElement Value,
        long SourceRowCount, string SourceSha256);

    internal abstract record TypedObservationV3(string ReaderSchemaVersion, string ReaderId,
        RawScopeV3 Scope, string TransactionBoundary, IReadOnlyList<TypedFactV3> Facts, string RawSha256);
    internal sealed record ControlLifecycleObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record ControlAclObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record TargetCommandObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record TargetPurgeObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record TargetExportObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record TargetAclObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);
    internal sealed record MutationRunObservationV3(string Schema, string Id, RawScopeV3 RawScope,
        string Boundary, IReadOnlyList<TypedFactV3> TypedFacts, string Digest)
        : TypedObservationV3(Schema, Id, RawScope, Boundary, TypedFacts, Digest);

    private static int StageSequence(EvidenceStage stage) => stage switch
    {
        EvidenceStage.Before => 1,
        EvidenceStage.After => 2,
        EvidenceStage.Durable => 3,
        EvidenceStage.Cleanup => 4,
        _ => 0
    };

    private static Guid ExactGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static string ReaderId(EvidenceSurface surface) => surface switch
    {
        EvidenceSurface.ControlLifecycle => "CP-L4",
        EvidenceSurface.ControlAcl => "CP-A4",
        EvidenceSurface.TargetCommand => "TC4",
        EvidenceSurface.TargetPurge => "TP4",
        EvidenceSurface.TargetExport => "TE4",
        EvidenceSurface.TargetAcl => "TA4",
        _ => "OR3"
    };



    private static EvidenceRead ReadForStage(ScenarioEvidencePlan plan, EvidenceStage stage) => stage switch
    {
        EvidenceStage.Before => plan.Before,
        EvidenceStage.After => plan.After,
        EvidenceStage.Durable => plan.Durable,
        EvidenceStage.Cleanup => plan.Cleanup,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
    internal static RawObservationSet BuildDatabaseShapedRawEvidence(
        AcceptanceContract contract, SubcaseRequirement subcase)
    {
        ValidateContract(contract);
        Rev869BCorrection28IndependentEvidenceFixtures.Validate();
        var fixture = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var companyId = fixture.FixtureIdentity.ToString();
        var leaseId = fixture.FixtureIdentity;
        var instanceSha256 = ExactContractSha256(new { subcase.SubcaseId, kind = "target-instance" });
        var documents = new Dictionary<EvidenceStage, IReadOnlyList<string>>();
        foreach (var stage in new[] { EvidenceStage.Before, EvidenceStage.After, EvidenceStage.Durable, EvidenceStage.Cleanup })
        {
            var stageSpecs = Correction28RawFactTemplates.Where(x => x.ScenarioId == contract.ScenarioId &&
                (x.Stage == stage || x.ReferenceStage == stage)).ToArray();
            var readerIds = stageSpecs.Select(x => x.ReaderId).Distinct(StringComparer.Ordinal).ToList();
            if (readerIds.Count == 0)
                readerIds.Add(stage == EvidenceStage.Cleanup ? "CP-L4" : ReaderId(ReadForStage(contract.Plan, stage).Surface));
            documents[stage] = readerIds.Order(StringComparer.Ordinal)
                .Select(readerId => BuildRawDocument(subcase, fixture, companyId, leaseId,
                    instanceSha256, stage, readerId, stageSpecs)).ToArray();
        }
        return new RawObservationSet(companyId, leaseId, 7, instanceSha256, documents);
    }

    private static string BuildRawDocument(SubcaseRequirement subcase, Rev869BCorrection28IndependentEvidenceFixtures.Fixture fixture, string companyId, Guid leaseId,
        string instanceSha256, EvidenceStage stage, string readerId, IReadOnlyList<RawFixtureSpec> specs)
    {
        var facts = new JsonArray();
        var seen = new Dictionary<(string Kind, string Name), string>();
        void Add(string kind, string name, string valueType, string actualJson, string source)
        {
            var key = (kind, name);
            if (seen.TryGetValue(key, out var existing))
            {
                if (existing != actualJson) throw new InvalidOperationException("Independent fixture facts contradicted: " + source + "/" + kind + "/" + name + ":" + existing + " != " + actualJson);
                return;
            }
            seen[key] = actualJson;
            var sourceSha = ExactContractSha256(new { source, subcase.SubcaseId, stage, readerId });
            facts.Add(new JsonObject
            {
                ["kind"] = kind,
                ["name"] = name,
                ["valueType"] = valueType,
                ["value"] = FixtureNode(valueType, actualJson),
                ["sourceRowCount"] = 1,
                ["sourceSha256"] = sourceSha
            });
        }

        foreach (var spec in specs.Where(x => x.ReaderId == readerId))
        {
            if (spec.Stage == stage)
                Add("selector", spec.SelectorName, spec.ValueType, spec.ActualJson, spec.ComponentId);
            if (spec.ReferenceStage == stage && spec.ReferenceName.Length != 0)
            {
                Add("reference", spec.ReferenceName, spec.ReferenceValueType,
                    spec.ReferenceJson, spec.ComponentId + ":reference");
                if (spec.SecondReferenceName.Length != 0)
                    Add("reference", spec.SecondReferenceName, spec.SecondReferenceValueType,
                        spec.SecondReferenceJson, spec.ComponentId + ":reference:2");
            }
        }

        var control = readerId is "CP-L4" or "CP-A4";
        var root = new JsonObject
        {
            ["readerSchemaVersion"] = Rev869BCorrection26FrozenOracle.ReaderContractVersion,
            ["readerId"] = readerId,
            ["scope"] = new JsonObject
            {
                ["companyId"] = control ? "not-applicable-control-plane" : companyId,
                ["targetInstanceSha256"] = instanceSha256,
                ["leaseId"] = leaseId,
                ["leaseVersion"] = 7,
                ["operationId"] = fixture.ActionIdentity,
                ["scenarioExecutionId"] = fixture.PreparationIdentity,
                ["subcaseId"] = subcase.SubcaseId,
                ["stage"] = stage.ToString()
            },
            ["observedAtUtc"] = DateTimeOffset.UnixEpoch.AddSeconds(StageSequence(stage)).ToString("O"),
            ["transactionBoundary"] = "tx:" + fixture.ObservationIdentity.ToString("N") + ":" + stage + ":" + StageSequence(stage),
            ["facts"] = facts,
            ["factCount"] = facts.Count,
            ["rawSha256"] = new string('0', 64)
        };
        root["rawSha256"] = RawDocumentSha256(root);
        return root.ToJsonString();
    }

    private static string RawDocumentSha256(JsonObject root)
    {
        var clone = root.DeepClone().AsObject();
        clone.Remove("rawSha256");
        using var document = JsonDocument.Parse(clone.ToJsonString());
        var canonical = JsonSerializer.Serialize(CanonicalValue(document.RootElement), JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string EnvelopeSha256(JsonObject root)
    {
        var clone = root.DeepClone().AsObject();
        clone.Remove("canonicalEvidenceSha256");
        using var document = JsonDocument.Parse(clone.ToJsonString());
        var canonical = JsonSerializer.Serialize(CanonicalValue(document.RootElement), JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }



    private static TypedObservationV3 ParseTypedObservation(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        RequireExactProperties(root, ["readerSchemaVersion", "readerId", "scope", "observedAtUtc",
            "transactionBoundary", "facts", "factCount", "rawSha256"], "raw observation");
        var readerSchema = RequiredString(root, "readerSchemaVersion");
        var readerId = RequiredString(root, "readerId");
        if (readerSchema != Rev869BCorrection26FrozenOracle.ReaderContractVersion ||
            readerId is not ("CP-L4" or "CP-A4" or "TC4" or "TP4" or "TE4" or "TA4" or "OR3"))
            throw new InvalidOperationException("Unknown reader contract.");

        var scopeElement = root.GetProperty("scope");
        RequireExactProperties(scopeElement, ["companyId", "targetInstanceSha256", "leaseId",
            "leaseVersion", "operationId", "scenarioExecutionId", "subcaseId", "stage"], "raw scope");
        var scope = new RawScopeV3(RequiredString(scopeElement, "companyId"),
            RequiredString(scopeElement, "targetInstanceSha256"),
            RequiredGuid(scopeElement, "leaseId"), RequiredInt64(scopeElement, "leaseVersion"),
            RequiredGuid(scopeElement, "operationId"), RequiredGuid(scopeElement, "scenarioExecutionId"),
            RequiredString(scopeElement, "subcaseId"), RequiredString(scopeElement, "stage"));
        if (!ExactSha256(scope.TargetInstanceSha256) || scope.LeaseVersion < 1)
            throw new InvalidOperationException("Raw scope was incomplete.");

        var factsElement = root.GetProperty("facts");
        if (factsElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Facts must be an array.");
        var facts = new List<TypedFactV3>();
        foreach (var element in factsElement.EnumerateArray())
        {
            RequireExactProperties(element, ["kind", "name", "valueType", "value",
                "sourceRowCount", "sourceSha256"], "raw fact");
            var fact = new TypedFactV3(RequiredString(element, "kind"), RequiredString(element, "name"),
                RequiredString(element, "valueType"), element.GetProperty("value").Clone(),
                RequiredInt64(element, "sourceRowCount"), RequiredString(element, "sourceSha256"));
            if (fact.Kind is not ("selector" or "reference") || fact.SourceRowCount < 0 ||
                !ExactSha256(fact.SourceSha256) || !ValueMatchesType(fact.Value, fact.ValueType))
                throw new InvalidOperationException("Raw fact contract was invalid.");
            facts.Add(fact);
        }
        if (facts.Select(x => (x.Kind, x.Name)).Distinct().Count() != facts.Count ||
            RequiredInt64(root, "factCount") != facts.Count)
            throw new InvalidOperationException("Raw facts were duplicated or miscounted.");

        var rawSha = RequiredString(root, "rawSha256");
        var node = JsonNode.Parse(json)!.AsObject();
        if (!ExactSha256(rawSha) || RawDocumentSha256(node) != rawSha)
            throw new InvalidOperationException("Raw observation digest mismatch.");
        if (!DateTimeOffset.TryParse(RequiredString(root, "observedAtUtc"), out _))
            throw new InvalidOperationException("Raw observation time was invalid.");
        var boundary = RequiredString(root, "transactionBoundary");
        if (!boundary.StartsWith("tx:", StringComparison.Ordinal))
            throw new InvalidOperationException("Raw transaction boundary was invalid.");

        return readerId switch
        {
            "CP-L4" => new ControlLifecycleObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            "CP-A4" => new ControlAclObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            "TC4" => new TargetCommandObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            "TP4" => new TargetPurgeObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            "TE4" => new TargetExportObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            "TA4" => new TargetAclObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha),
            _ => new MutationRunObservationV3(readerSchema, readerId, scope, boundary, facts, rawSha)
        };
    }

    private static void RequireExactProperties(JsonElement element,
        IReadOnlyCollection<string> expected, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException(name + " must be an object.");
        var actual = element.EnumerateObject().Select(x => x.Name).ToArray();
        if (actual.Length != actual.Distinct(StringComparer.Ordinal).Count() ||
            !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new InvalidOperationException(name + " property set was not exact.");
    }

    private static string RequiredString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! :
        throw new InvalidOperationException(name + " must be a nonempty string.");

    private static long RequiredInt64(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result :
        throw new InvalidOperationException(name + " must be int64.");

    private static Guid RequiredGuid(JsonElement element, string name) =>
        Guid.TryParse(RequiredString(element, name), out var result) && result != Guid.Empty ? result :
        throw new InvalidOperationException(name + " must be a nonzero UUID.");

    private static bool ValueMatchesType(JsonElement value, string type) => type switch
    {
        "int64" => value.TryGetInt64(out _),
        "bool" or "bool tuple" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "uuid" => value.ValueKind == JsonValueKind.String &&
            Guid.TryParse(value.GetString(), out var id) && id != Guid.Empty,
        "sha256" => value.ValueKind == JsonValueKind.String && ExactSha256(value.GetString()!),
        "string/enum" => value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString()),
        _ => false
    };

    private static JsonNode FixtureNode(string type, string value) => type switch
    {
        "int64" => JsonValue.Create(long.Parse(value, System.Globalization.CultureInfo.InvariantCulture)),
        "bool" or "bool tuple" => JsonValue.Create(bool.Parse(value)),
        "uuid" or "sha256" or "string/enum" => JsonValue.Create(value),
        _ => throw new InvalidOperationException("Unknown independent fixture value type.")
    };



    internal static EvidenceBundle AdaptAndVerifyDatabaseShapedEvidence(
        AcceptanceContract contract, SubcaseRequirement subcase, RawObservationSet raw)
    {
        var before = AdaptStage(contract, subcase, raw, EvidenceStage.Before);
        var after = AdaptStage(contract, subcase, raw, EvidenceStage.After);
        var durable = AdaptStage(contract, subcase, raw, EvidenceStage.Durable);
        var cleanup = AdaptStage(contract, subcase, raw, EvidenceStage.Cleanup);
        using var auditDocument = JsonDocument.Parse("{\"supplementary\":true,\"decisive\":false}");
        var audit = CanonicalObservation(contract.ScenarioId + ":audit:supplementary", auditDocument.RootElement);
        var independent = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var actionObject = new JsonObject
        {
            ["actionReached"] = independent.Action.Reached,
            ["affectedRows"] = independent.Action.AffectedRows,
            ["terminalState"] = independent.Action.TerminalState,
            ["actionIdentity"] = independent.ActionIdentity,
            ["actionFixtureSha256"] = independent.ActionFixtureSha256
        };
        if (independent.Action.SqlState is not null) actionObject["sqlState"] = independent.Action.SqlState;
        if (independent.Action.ErrorCode is not null) actionObject["errorCode"] = independent.Action.ErrorCode;
        if (independent.Action.DatabaseObject is not null) actionObject["databaseObject"] = independent.Action.DatabaseObject;
        using var actionDocument = JsonDocument.Parse(actionObject.ToJsonString());
        var action = CanonicalObservation(contract.ScenarioId + ":action:independent-fixture", actionDocument.RootElement);
        return new EvidenceBundle(before, after, durable, audit, cleanup, action);
    }

    private static EvidenceObservation AdaptStage(AcceptanceContract contract, SubcaseRequirement subcase,
        RawObservationSet raw, EvidenceStage stage)
    {
        var independent = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var typed = raw.Documents[stage].Select(ParseTypedObservation).ToArray();
        if (typed.Select(x => x.ReaderId).Distinct(StringComparer.Ordinal).Count() != typed.Length)
            throw new InvalidOperationException("A reader may contribute at most one exact stage observation.");

        foreach (var observation in typed)
        {
            var control = observation.ReaderId is "CP-L4" or "CP-A4";
            var expectedCompany = control ? "not-applicable-control-plane" : raw.CompanyId;
            if (observation.Scope.CompanyId != expectedCompany ||
                observation.Scope.TargetInstanceSha256 != raw.TargetInstanceSha256 ||
                observation.Scope.LeaseId != raw.LeaseId ||
                observation.Scope.LeaseVersion != raw.LeaseVersion ||
                observation.Scope.OperationId != independent.ActionIdentity ||
                observation.Scope.ScenarioExecutionId != independent.PreparationIdentity ||
                observation.Scope.Stage != stage.ToString())
                throw new InvalidOperationException("Raw observation scope did not match authenticated preparation.");
        }

        var selectorSpecs = Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId)
            .Where(x => Enum.Parse<EvidenceStage>(x.Stage) == stage).ToArray();
        var fixtureSpecs = Correction28RawFactTemplates.Where(x => x.ScenarioId == contract.ScenarioId).ToArray();
        var referenceSpecs = fixtureSpecs.Where(x => x.ReferenceStage == stage &&
            x.ReferenceName.Length != 0).ToArray();
        var expectedReaders = selectorSpecs.Select(x => x.ReaderId)
            .Concat(referenceSpecs.Select(x => x.ReaderId))
            .Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        if (expectedReaders.Count == 0)
            expectedReaders.Add(stage == EvidenceStage.Cleanup ? "CP-L4" :
                ReaderId(ReadForStage(contract.Plan, stage).Surface));
        if (!expectedReaders.SetEquals(typed.Select(x => x.ReaderId)))
            throw new InvalidOperationException("Raw reader set was not exact for the stage.");

        foreach (var observation in typed)
        {
            var expectedFacts = selectorSpecs.Where(x => x.ReaderId == observation.ReaderId)
                .Select(x => (Kind: "selector", Name: x.SelectorName))
                .Concat(referenceSpecs.Where(x => x.ReaderId == observation.ReaderId)
                    .Select(x => (Kind: "reference", Name: x.ReferenceName)))
                .Concat(referenceSpecs.Where(x => x.ReaderId == observation.ReaderId && x.SecondReferenceName.Length != 0)
                    .Select(x => (Kind: "reference", Name: x.SecondReferenceName)))
                .ToHashSet();
            if (!expectedFacts.SetEquals(observation.Facts.Select(x => (x.Kind, x.Name))) ||
                observation.Facts.Any(x => x.SourceRowCount != 1) ||
                !observation.TransactionBoundary.Contains(":" + stage + ":", StringComparison.Ordinal))
                throw new InvalidOperationException("Raw fact set, cardinality, or transaction boundary was not exact.");
        }

        var selectors = new JsonObject();
        var references = new JsonObject();
        var provenance = new JsonObject();
        foreach (var selector in selectorSpecs)
        {
            var observation = typed.Single(x => x.ReaderId == selector.ReaderId);
            var fact = observation.Facts.SingleOrDefault(x =>
                x.Kind == "selector" && x.Name == selector.SelectorName)
                ?? throw new InvalidOperationException("Required typed selector fact was absent.");
            if (!ValueMatchesType(fact.Value, selector.ValueType))
                throw new InvalidOperationException("Typed selector value did not match the frozen contract.");
            selectors[selector.SelectorName] = JsonNode.Parse(fact.Value.GetRawText());
            provenance[selector.SelectorName] = new JsonObject
            {
                ["readerId"] = observation.ReaderId,
                ["readerSchemaVersion"] = observation.ReaderSchemaVersion,
                ["rawPath"] = selector.RawFactPath,
                ["mappingId"] = selector.MappingId,
                ["reducerId"] = selector.Reducer,
                ["rawSha256"] = observation.RawSha256,
                ["sourceRowCount"] = fact.SourceRowCount,
                ["sourceSha256"] = fact.SourceSha256
            };
        }

        foreach (var fixture in referenceSpecs)
        {
            var observation = typed.Single(x => x.ReaderId == fixture.ReaderId);
            var fact = observation.Facts.SingleOrDefault(x =>
                x.Kind == "reference" && x.Name == fixture.ReferenceName)
                ?? throw new InvalidOperationException("Required reference fact was absent.");
            if (references.ContainsKey(fact.Name) &&
                references[fact.Name]!.ToJsonString() != fact.Value.GetRawText())
                throw new InvalidOperationException("Contradictory reference facts were supplied.");
            references[fact.Name] = JsonNode.Parse(fact.Value.GetRawText());
            if (fixture.SecondReferenceName.Length != 0)
            {
                var second = observation.Facts.SingleOrDefault(x =>
                    x.Kind == "reference" && x.Name == fixture.SecondReferenceName)
                    ?? throw new InvalidOperationException("Required second reference fact was absent.");
                references[second.Name] = JsonNode.Parse(second.Value.GetRawText());
            }
        }



        var sequence = StageSequence(stage);
        var envelopeId = "env:c28:" + independent.EnvelopeIdentity.ToString("D");
        var observationId = stage switch
        {
            EvidenceStage.Before => independent.BeforeObservationId,
            EvidenceStage.After => independent.AfterObservationId,
            EvidenceStage.Durable => independent.DurableObservationId,
            EvidenceStage.Audit => independent.AuditObservationId,
            EvidenceStage.Cleanup => independent.CleanupObservationId,
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
        var root = new JsonObject
        {
            ["schemaVersion"] = Rev869BCorrection26FrozenOracle.EvidenceSchemaVersion,
            ["adapterVersion"] = Rev869BCorrection26FrozenOracle.AdapterVersion,
            ["oracleVersion"] = Rev869BCorrection26FrozenOracle.Version,
            ["oracleSha256"] = Rev869BCorrection26FrozenOracle.ExpectedSha256,
            ["formulaVersion"] = Rev869BCorrection26FrozenOracle.FormulaVersion,
            ["scenarioId"] = contract.ScenarioId,
            ["subcaseId"] = subcase.SubcaseId,
            ["companyId"] = raw.CompanyId,
            ["targetInstanceSha256"] = raw.TargetInstanceSha256,
            ["leaseBindingId"] = raw.LeaseId,
            ["leaseVersion"] = raw.LeaseVersion,
            ["preparationId"] = independent.PreparationIdentity,
            ["attemptId"] = independent.AttemptIdentity,
            ["evidenceId"] = independent.ObservationIdentity,
            ["expectedResultId"] = independent.ExpectedResultIdentity,
            ["expectedOutcome"] = subcase.ExpectedResult,
            ["provenance"] = "authoritative-local-reader",
            ["observationId"] = observationId,
            ["envelopeId"] = envelopeId,
            ["observationStage"] = stage.ToString(),
            ["asOfSequence"] = sequence,
            ["duplicateEvidenceCount"] = 0,
            ["transactionBoundaries"] = new JsonArray(
                typed.Select(x => JsonValue.Create(x.TransactionBoundary)).ToArray()),
            ["rawObservationSha256"] = new JsonArray(
                typed.Select(x => JsonValue.Create(x.RawSha256)).ToArray()),
            ["selectors"] = selectors,
            ["references"] = references,
            ["selectorProvenance"] = provenance,
            ["durableAuditReferences"] = stage == EvidenceStage.Durable
                ? new JsonArray(typed.SelectMany(x => x.Facts.Where(f => f.SourceRowCount > 0)
                    .Select(f => JsonValue.Create(x.ReaderId + ":" + f.Name + ":" +
                        f.SourceSha256))).ToArray())
                : new JsonArray(),
            ["lease"] = new JsonObject
            {
                ["leaseBindingId"] = raw.LeaseId,
                ["leaseVersion"] = raw.LeaseVersion
            }
        };
        root["canonicalEvidenceSha256"] = EnvelopeSha256(root);
        using var document = JsonDocument.Parse(root.ToJsonString());
        return CanonicalObservation(contract.ScenarioId + ":" + stage + ":adapter-v4",
            document.RootElement);
    }



    internal static bool PipelineMutationIsRejected(AcceptanceContract contract,
        SubcaseRequirement subcase, PipelineMutationKind mutation)
    {
        try
        {
            if (mutation == PipelineMutationKind.RemovedDecisiveAssertion)
            {
                var decisive = contract.Plan.Assertions.First(x =>
                    x.AssertionId.Contains(":formula-", StringComparison.Ordinal));
                var changed = contract with
                {
                    Plan = contract.Plan with
                    {
                        Assertions = contract.Plan.Assertions
                            .Where(x => x.AssertionId != decisive.AssertionId).ToArray()
                    }
                };
                ValidateContract(changed);
                return false;
            }

            var raw = BuildDatabaseShapedRawEvidence(contract, subcase);
            if (mutation is PipelineMutationKind.WrongOracleHash or
                PipelineMutationKind.WrongObservationIdentity or
                PipelineMutationKind.WrongEnvelopeIdentity)
            {
                var adapted = AdaptAndVerifyDatabaseShapedEvidence(contract, subcase, raw);
                var changed = mutation switch
                {
                    PipelineMutationKind.WrongOracleHash => ChangePath(adapted,
                        EvidenceStage.Durable, "oracleSha256",
                        JsonValue.Create(new string('f', 64)), remove: false),
                    PipelineMutationKind.WrongObservationIdentity => ChangePath(adapted,
                        EvidenceStage.Durable, "observationId",
                        JsonValue.Create("obs:substituted"), remove: false),
                    _ => ChangePath(adapted, EvidenceStage.Durable, "envelopeId",
                        JsonValue.Create("env:substituted"), remove: false)
                };
                return VerifyEvidence(contract, subcase, changed).Length != 0;
            }

            raw = MutateRawObservation(contract, raw, mutation);
            var bundle = AdaptAndVerifyDatabaseShapedEvidence(contract, subcase, raw);
            return VerifyEvidence(contract, subcase, bundle).Length != 0;
        }
        catch (InvalidOperationException ex) when (IsExpectedAdapterRejection(mutation, ex.Message))
        {
            return true;
        }
        catch (ArgumentException ex) when (mutation == PipelineMutationKind.RemovedDecisiveAssertion &&
            ex.Message.Contains("formula component", StringComparison.Ordinal))
        {
            return true;
        }
    }

    internal sealed record PipelineMutationResult(string MutationId, string ScenarioId, string SubcaseId,
        string TargetBoundary, string TargetComponent, string ExpectedRejectionCode, string ActualRejectionCode,
        string EvaluationStage, bool Killed, bool Survived, string EvidenceSha256);

    internal sealed record MutationRunRecord(string OracleVersion, string OracleSha256, Guid RunId,
        string ScenarioId, string SubcaseId, string MutationId, string TargetComponent,
        string ExpectedRejectionCode, string ActualRejectionCode, bool Survived, string EvidenceSha256);

    internal static PipelineMutationResult EvaluatePipelineMutation(AcceptanceContract contract,
        SubcaseRequirement subcase, PipelineMutationKind mutation)
    {
        var selector = Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId).First();
        var expectedCode = ExpectedMutationRejectionCode(mutation, selector.ComponentId);
        var killed = PipelineMutationIsRejected(contract, subcase, mutation);
        var actualCode = killed ? expectedCode : "MUTATION_SURVIVED";
        var mutationId = $"{contract.ScenarioId}/{subcase.SubcaseId}/{mutation}";
        return new PipelineMutationResult(mutationId, contract.ScenarioId, subcase.SubcaseId,
            MutationBoundary(mutation), selector.ComponentId, expectedCode, actualCode, "ADAPTER_AND_VERIFIER",
            killed, !killed, ExactContractSha256(new { mutationId, expectedCode, actualCode, killed }));
    }

    internal static MutationRunObservationV3 DispatchLocalOr3(IReadOnlyList<MutationRunRecord> records,
        string scenarioId, string subcaseId, Guid preparationId, Guid observationId, Guid envelopeId)
    {
        if (records.Count == 0 || records.Any(x => x.ScenarioId != scenarioId || x.SubcaseId != subcaseId ||
            x.OracleVersion != Rev869BCorrection26FrozenOracle.Version ||
            x.OracleSha256 != Rev869BCorrection26FrozenOracle.ExpectedSha256 ||
            x.ExpectedRejectionCode != x.ActualRejectionCode || !ExactSha256(x.EvidenceSha256)) ||
            records.Select(x => x.MutationId).Distinct(StringComparer.Ordinal).Count() != records.Count)
            throw new InvalidOperationException("OR3_RECORD_EXACT_SET");
        var killed = records.Count(x => !x.Survived);
        var facts = new[]
        {
            LocalOr3Fact("killedMutants", killed, records),
            LocalOr3Fact("survivingMutants", records.Count - killed, records),
            LocalOr3Fact("requiredNonEquivalentMutants", records.Count, records)
        };
        var scope = new RawScopeV3("not-applicable-local", new string('a', 64), envelopeId, 1,
            observationId, preparationId, subcaseId, EvidenceStage.Durable.ToString());
        var digest = ExactContractSha256(new { scenarioId, subcaseId, preparationId, observationId, envelopeId,
            evidence = records.Select(x => x.EvidenceSha256).Order(StringComparer.Ordinal).ToArray() });
        return new MutationRunObservationV3(Rev869BCorrection26FrozenOracle.ReaderContractVersion, "OR3",
            scope, "local-or3:" + observationId.ToString("N"), facts, digest);
    }

    private static TypedFactV3 LocalOr3Fact(string name, int value, IReadOnlyList<MutationRunRecord> records)
    {
        using var document = JsonDocument.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new TypedFactV3(name == "requiredNonEquivalentMutants" ? "reference" : "selector", name,
            "int64", document.RootElement.Clone(), records.Count,
            ExactContractSha256(new { name, value, records = records.Select(x => x.EvidenceSha256).ToArray() }));
    }

    private static bool IsExpectedAdapterRejection(PipelineMutationKind mutation, string message) =>
        mutation != PipelineMutationKind.RemovedDecisiveAssertion &&
        ((mutation == PipelineMutationKind.WrongType && message.Contains("requires an element of type", StringComparison.Ordinal)) ||
         (mutation == PipelineMutationKind.BroadenedAclOrPurgeScope && message == "operationId must be a nonzero UUID.") ||
         message.Contains("Raw", StringComparison.Ordinal) || message.Contains("fact", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("scope", StringComparison.OrdinalIgnoreCase) || message.Contains("evidence", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("digest", StringComparison.OrdinalIgnoreCase) || message.Contains("contract", StringComparison.OrdinalIgnoreCase));

    private static string ExpectedMutationRejectionCode(PipelineMutationKind mutation, string component) => mutation switch
    {
        PipelineMutationKind.MissingField or PipelineMutationKind.AdditionalField or PipelineMutationKind.DuplicatedField => "RAW_EXACT_SET",
        PipelineMutationKind.CrossCompany or PipelineMutationKind.CrossInstance or PipelineMutationKind.CrossLease or
            PipelineMutationKind.WrongLeaseVersion or PipelineMutationKind.BroadenedAclOrPurgeScope => "RAW_SCOPE",
        PipelineMutationKind.RawDigestChanged => "RAW_DIGEST",
        PipelineMutationKind.WrongEnvelopeIdentity => "ENVELOPE_IDENTITY",
        PipelineMutationKind.WrongObservationIdentity => "OBSERVATION_IDENTITY",
        PipelineMutationKind.WrongOracleHash => "ORACLE_IDENTITY",
        PipelineMutationKind.MissingDurableHistory or PipelineMutationKind.FabricatedHistory or
            PipelineMutationKind.StaleOrReplayed => "HISTORY_IDENTITY",
        PipelineMutationKind.RemovedDecisiveAssertion => "ASSERTION_REMOVAL:" + component,
        _ => "ASSERTION_" + component
    };

    private static string MutationBoundary(PipelineMutationKind mutation) => mutation switch
    {
        PipelineMutationKind.WrongEnvelopeIdentity => "ENVELOPE",
        PipelineMutationKind.WrongObservationIdentity => "OBSERVATION",
        PipelineMutationKind.RemovedDecisiveAssertion => "ASSERTION",
        PipelineMutationKind.MissingDurableHistory or PipelineMutationKind.FabricatedHistory or
            PipelineMutationKind.StaleOrReplayed => "HISTORY",
        _ => "RAW_READER"
    };
    private static RawObservationSet MutateRawObservation(AcceptanceContract contract,
        RawObservationSet raw, PipelineMutationKind mutation)
    {
        var decisiveSelector = Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId).First();
        var stage = mutation == PipelineMutationKind.MissingDurableHistory
            ? EvidenceStage.Durable
            : Enum.Parse<EvidenceStage>(decisiveSelector.Stage);
        var docs = raw.Documents.ToDictionary(x => x.Key, x => x.Value.ToList());
        var index = docs[stage].FindIndex(x => x.Contains("facts", StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("Raw mutation target was absent.");
        var root = JsonNode.Parse(docs[stage][index])!.AsObject();
        var facts = root["facts"]!.AsArray();
        var scope = root["scope"]!.AsObject();

        switch (mutation)
        {
            case PipelineMutationKind.SelectorChanged:
            case PipelineMutationKind.WrongState:
            case PipelineMutationKind.WrongCount:
                var decisiveFact = facts.FirstOrDefault(x =>
                    x!["kind"]!.GetValue<string>() == "selector" &&
                    x["name"]!.GetValue<string>() == decisiveSelector.SelectorName)
                    ?? throw new InvalidOperationException("A decisive raw fact is required.");
                decisiveFact["value"] = FailingMutationValue(decisiveSelector);
                break;
            case PipelineMutationKind.MissingField:
            case PipelineMutationKind.MissingDurableHistory:
                if (facts.Count == 0) throw new InvalidOperationException("A decisive raw fact is required.");
                facts.RemoveAt(0);
                break;
            case PipelineMutationKind.AdditionalField:
                facts.Add(new JsonObject
                {
                    ["kind"] = "selector",
                    ["name"] = "unexpectedFact",
                    ["valueType"] = "int64",
                    ["value"] = 1,
                    ["sourceRowCount"] = 1,
                    ["sourceSha256"] = new string('a', 64)
                });
                break;
            case PipelineMutationKind.DuplicatedField:
                if (facts.Count == 0) throw new InvalidOperationException("A decisive raw fact is required.");
                facts.Add(facts[0]!.DeepClone());
                break;
            case PipelineMutationKind.WrongType:
                if (facts.Count == 0) throw new InvalidOperationException("A decisive raw fact is required.");
                facts[0]!["value"] = new JsonObject { ["not"] = "a scalar" };
                break;
            case PipelineMutationKind.FabricatedHistory:
                if (facts.Count == 0) throw new InvalidOperationException("A decisive raw fact is required.");
                facts[0]!["sourceRowCount"] = 99;
                break;
            case PipelineMutationKind.CrossCompany:
                scope["companyId"] = ExactGuid("foreign-company").ToString();
                break;
            case PipelineMutationKind.CrossInstance:
                scope["targetInstanceSha256"] = new string('b', 64);
                break;
            case PipelineMutationKind.CrossLease:
                scope["leaseId"] = ExactGuid("foreign-lease");
                break;
            case PipelineMutationKind.WrongLeaseVersion:
                scope["leaseVersion"] = 999;
                break;
            case PipelineMutationKind.StaleOrReplayed:
                root["transactionBoundary"] = "tx:replayed:0";
                break;
            case PipelineMutationKind.RawDigestChanged:
                root["rawSha256"] = new string('f', 64);
                break;
            case PipelineMutationKind.BroadenedAclOrPurgeScope:
                scope["operationId"] = Guid.Empty;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }

        root["factCount"] = facts.Count;
        if (mutation != PipelineMutationKind.RawDigestChanged)
            root["rawSha256"] = RawDocumentSha256(root);
        docs[stage][index] = root.ToJsonString();
        return raw with
        {
            Documents = docs.ToDictionary(x => x.Key,
                x => (IReadOnlyList<string>)x.Value)
        };
    }


    private static JsonNode FailingMutationValue(Rev869BCorrection26FrozenOracle.SelectorSpec selector)
    {
        var fixture = Correction28RawFactTemplates.Single(x => x.ComponentId == selector.ComponentId);
        return selector.Operator switch
        {
            "GreaterThanZero" => JsonValue.Create(0),
            "Zero" => JsonValue.Create(1),
            "AtMostOne" => JsonValue.Create(2),
            "ExactSha256" => JsonValue.Create("not-a-sha256"),
            "NotEqualsLiteral" => FixtureNode(selector.ValueType, selector.Expected),
            "NotEqualsObservationPath" => FixtureNode(fixture.ReferenceValueType, fixture.ReferenceJson),
            "EqualsLiteral" or "EqualsObservationPath" => fixture.ValueType switch
            {
                "int64" => JsonValue.Create(long.Parse(fixture.ActualJson,
                    System.Globalization.CultureInfo.InvariantCulture) + 1),
                "bool" or "bool tuple" => JsonValue.Create(!bool.Parse(fixture.ActualJson)),
                "uuid" => JsonValue.Create(ExactGuid(fixture.ActualJson + ":mutated")),
                "sha256" => JsonValue.Create(ExactContractSha256(new { fixture.ComponentId, mutated = true })),
                _ => JsonValue.Create(fixture.ActualJson + ":mutated")
            },
            _ => JsonValue.Create("mutated")
        };
    }
    private static SelectorReader[] RequiredSelectorReaders(string scenarioId, EvidenceStage stage)
    {
        var readers = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string name, string reader)
        {
            if (readers.TryGetValue(name, out var prior) && prior != reader)
                throw new InvalidOperationException("One authoritative selector cannot be assigned to two reader meanings.");
            readers[name] = reader;
        }

        foreach (var selector in Rev869BCorrection26FrozenOracle.SelectorsFor(scenarioId))
        {
            if (selector.Stage == stage.ToString())
                Add(selector.SelectorName, selector.ReaderId);
            if (selector.Operator is "EqualsObservationPath" or "NotEqualsObservationPath")
            {
                var reference = ParseReference(selector.Expected);
                if (reference.Stage == stage && reference.Path.StartsWith("selectors.", StringComparison.Ordinal))
                    Add(reference.Path["selectors.".Length..], selector.ReaderId);
            }
            if (selector.Operator == "ExactlyOneTrue")
                foreach (var referenceText in SplitReferences(selector.Expected))
                {
                    var reference = ParseReference(referenceText);
                    if (reference.Stage == stage && reference.Path.StartsWith("selectors.", StringComparison.Ordinal))
                        Add(reference.Path["selectors.".Length..], selector.ReaderId);
                }
        }
        return readers.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new SelectorReader(x.Key, x.Value)).ToArray();
    }

    private sealed record SelectorReader(string SelectorName, string ReaderId);

    internal static EvidenceBundle TamperEvidence(EvidenceBundle pristine, EvidenceAssertion assertion)
    {
        if (assertion.Operator == EvidenceOperator.SameCanonicalSha256AsBefore)
            return ReplaceStage(pristine, assertion.Stage, pristine.For(assertion.Stage) with { CanonicalSha256 = new string('c', 64) });
        if (assertion.Operator == EvidenceOperator.DifferentCanonicalSha256FromBefore)
            return ReplaceStage(pristine, assertion.Stage, pristine.For(assertion.Stage) with { CanonicalSha256 = pristine.Before.CanonicalSha256 });
        if (assertion.Operator == EvidenceOperator.ExactlyOneTrue)
        {
            var changed = pristine;
            foreach (var referenceText in SplitReferences(assertion.Expected))
            {
                var reference = ParseReference(referenceText);
                changed = ChangePath(changed, reference.Stage, reference.Path, JsonValue.Create(true), remove: false);
            }
            return changed;
        }

        JsonNode? replacement = assertion.Operator switch
        {
            EvidenceOperator.Exists => null,
            EvidenceOperator.Absent => JsonValue.Create("fabricated"),
            EvidenceOperator.EqualsLiteral => JsonValue.Create("tampered:" + assertion.Expected),
            EvidenceOperator.NotEqualsLiteral => JsonValue.Create(assertion.Expected),
            EvidenceOperator.GreaterThanZero => JsonValue.Create(0L),
            EvidenceOperator.Zero => JsonValue.Create(1L),
            EvidenceOperator.ExactSha256 => JsonValue.Create("not-a-sha256"),
            EvidenceOperator.AtMostOne => JsonValue.Create(2L),
            EvidenceOperator.EqualsObservationPath => JsonValue.Create("tampered-cross-observation"),
            EvidenceOperator.NotEqualsObservationPath => JsonValue.Create(ReferenceScalar(pristine, assertion.Expected)),
            _ => throw new ArgumentOutOfRangeException(nameof(assertion.Operator))
        };
        return ChangePath(pristine, assertion.Stage, assertion.JsonPath, replacement,
            remove: assertion.Operator == EvidenceOperator.Exists);
    }

    internal static EvidenceBundle MutateEvidence(AcceptanceContract contract, SubcaseRequirement subcase,
        EvidenceBundle pristine, EvidenceMutationKind mutation)
    {
        var formula = contract.Plan.Assertions.First(x => x.AssertionId.Contains(":formula-", StringComparison.Ordinal));
        return mutation switch
        {
            EvidenceMutationKind.Missing => ChangePath(pristine, formula.Stage, formula.JsonPath, null, remove: true),
            EvidenceMutationKind.Additional => ChangePath(pristine, formula.Stage,
                "selectors.unexpectedSelector", JsonValue.Create(1L), remove: false),
            EvidenceMutationKind.Duplicated => ChangePath(pristine, formula.Stage,
                "duplicateEvidenceCount", JsonValue.Create(1L), remove: false),
            EvidenceMutationKind.Altered => TamperEvidence(pristine, formula),
            EvidenceMutationKind.Stale => ChangePath(pristine, EvidenceStage.Before,
                "asOfSequence", JsonValue.Create(-1L), remove: false),
            EvidenceMutationKind.Replayed => ChangePath(pristine, EvidenceStage.Durable,
                "evidenceId", JsonValue.Create(Guid.Empty), remove: false),
            EvidenceMutationKind.Fabricated => ChangePath(pristine, EvidenceStage.Durable,
                "provenance", JsonValue.Create("controller-fabricated"), remove: false),
            EvidenceMutationKind.CrossInstance => ChangePath(pristine, EvidenceStage.After,
                "targetInstanceSha256", JsonValue.Create(new string('f', 64)), remove: false),
            EvidenceMutationKind.CrossLease => ChangePath(pristine, EvidenceStage.Cleanup,
                "leaseBindingId", JsonValue.Create(Guid.Empty), remove: false),
            EvidenceMutationKind.WrongVersion => ChangePath(pristine, EvidenceStage.Durable,
                "oracleVersion", JsonValue.Create("REV869B-C26-ORACLE-wrong"), remove: false),
            EvidenceMutationKind.WrongState => ChangePath(pristine, EvidenceStage.Action,
                "terminalState", JsonValue.Create("FabricatedState"), remove: false),
            EvidenceMutationKind.WrongCount => TamperEvidence(pristine,
                contract.Plan.Assertions.FirstOrDefault(x => x.Operator is EvidenceOperator.Zero or
                    EvidenceOperator.GreaterThanZero or EvidenceOperator.AtMostOne ||
                    x.Operator == EvidenceOperator.EqualsLiteral && long.TryParse(x.Expected, out _)) ?? formula),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
    }

    private static (EvidenceStage Stage, string Path) ParseReference(string reference)
    {
        var parts = reference.Split(':', 2);
        if (parts.Length != 2 || !Enum.TryParse<EvidenceStage>(parts[0], out var stage))
            throw new ArgumentException("Exact observation reference required.", nameof(reference));
        return (stage, parts[1]);
    }

    private static string[] SplitReferences(string references) =>
        references.Replace(" OR ", "|", StringComparison.Ordinal)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ReferenceScalar(EvidenceBundle bundle, string reference)
    {
        var parsed = ParseReference(reference);
        var value = Resolve(bundle.For(parsed.Stage).Document.RootElement, parsed.Path)
            ?? throw new ArgumentException("Referenced synthetic evidence path is missing.", nameof(reference));
        return Scalar(value);
    }

    private static EvidenceBundle ChangePath(EvidenceBundle bundle, EvidenceStage stage, string path, JsonNode? value, bool remove)
    {
        var original = bundle.For(stage);
        var root = JsonNode.Parse(original.Document.RootElement.GetRawText())!.AsObject();
        if (remove) RemovePath(root, path); else SetPath(root, path, value);
        using var document = JsonDocument.Parse(root.ToJsonString());
        return ReplaceStage(bundle, stage, CanonicalObservation(original.ReadId, document.RootElement));
    }

    private static EvidenceBundle ReplaceStage(EvidenceBundle bundle, EvidenceStage stage, EvidenceObservation observation) => stage switch
    {
        EvidenceStage.Before => bundle with { Before = observation },
        EvidenceStage.After => bundle with { After = observation },
        EvidenceStage.Durable => bundle with { Durable = observation },
        EvidenceStage.Audit => bundle with { Audit = observation },
        EvidenceStage.Cleanup => bundle with { Cleanup = observation },
        EvidenceStage.Action => bundle with { Action = observation },
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };

    private static void SetPath(JsonObject root, string path, JsonNode? value)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = root;
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (current[parts[index]] is not JsonObject child)
            {
                child = new JsonObject();
                current[parts[index]] = child;
            }
            current = child;
        }
        current[parts[^1]] = value?.DeepClone();
    }

    private static JsonNode? GetPath(JsonObject root, string path)
    {
        JsonNode? current = root;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(part, out current)) return null;
        }
        return current;
    }

    private static void RemovePath(JsonObject root, string path)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var current = root;
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (current[parts[index]] is not JsonObject child) return;
            current = child;
        }
        current.Remove(parts[^1]);
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
            MutationKind.WeakenAssertion => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Select(x => x.AssertionId == mutation.TargetReadId
                    ? x with { Operator = EvidenceOperator.Exists, Expected = string.Empty } : x).ToArray()
            },
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
            MutationKind.CrossLeaseEvidence => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Append(new(contract.ScenarioId + ":cross-lease",
                    EvidenceStage.Durable, "lease.LeaseId", EvidenceOperator.EqualsLiteral, Guid.Empty.ToString())).ToArray()
            },
            MutationKind.WrongVersionEvidence => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Append(new(contract.ScenarioId + ":wrong-version",
                    EvidenceStage.Durable, "lease.Version", EvidenceOperator.EqualsLiteral, "-1")).ToArray()
            },
            MutationKind.WrongCountEvidence => contract.Plan with
            {
                Assertions = contract.Plan.Assertions.Append(new(contract.ScenarioId + ":wrong-count",
                    EvidenceStage.Durable, "attemptCount", EvidenceOperator.EqualsLiteral, "-1")).ToArray()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        return contract with { Plan = plan };
    }

    private async Task<ScenarioPreparation> PrepareAsync(AcceptanceContract contract, SubcaseRequirement subcase,
        Guid runId, string descriptorSha256, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync("v1/rev869b/acceptance/prepare", new
        {
            runId,
            contract.ScenarioId,
            subcase.SubcaseId,
            subcase.PreparationId,
            subcase.AttemptId,
            subcase.EvidenceId,
            subcase.ExpectedResultId,
            contract.Plan.FixtureOperationId,
            actionOperationId = subcase.ActionId,
            expectedResult = subcase.ExpectedResult,
            oracleVersion = Rev869BCorrection26FrozenOracle.Version,
            oracleSha256 = Rev869BCorrection26FrozenOracle.ExpectedSha256,
            descriptorSha256,
            pins.SourceCommit,
            pins.ManifestSha256,
            pins.ClusterSystemIdentifier
        }, ct);
        response.EnsureSuccessStatusCode();
        return await ReadSignedAsync<ScenarioPreparation>(response, pins.ControllerSigningPublicKeyPem, ct);
    }

    private async Task<ActionReceipt> ActAsync(AcceptanceContract contract, SubcaseRequirement subcase,
        ScenarioPreparation preparation, Guid runId, string descriptorSha256, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync($"v1/rev869b/acceptance/{contract.ScenarioId}/actions", new
        {
            runId,
            subcase.SubcaseId,
            subcase.PreparationId,
            subcase.EvidenceId,
            subcase.ExpectedResultId,
            preparation.LeaseId,
            preparation.FixtureId,
            preparation.CommandId,
            preparation.AuthorizationId,
            preparation.ExecutionId,
            preparation.AttemptId,
            preparation.DecisionId,
            actionOperationId = subcase.ActionId,
            expectedResult = subcase.ExpectedResult,
            oracleVersion = Rev869BCorrection26FrozenOracle.Version,
            oracleSha256 = Rev869BCorrection26FrozenOracle.ExpectedSha256,
            descriptorSha256
        }, ct);
        var receipt = await ReadSignedAsync<ActionReceipt>(response, pins.ControllerSigningPublicKeyPem, ct);
        if (receipt.HttpStatus != (int)response.StatusCode)
            throw new InvalidOperationException("Signed action status did not match the transport status.");
        return receipt;
    }

    private sealed record RequestedRawFact(string Name, string ValueType, string Kind, string ReaderId);

    private async Task<EvidenceObservation> ObserveAsync(AcceptanceContract contract,
        SubcaseRequirement subcase, EvidenceStage stage, EvidenceRead read,
        ScenarioPreparation preparation, CancellationToken ct)
    {
        if (read.Surface == EvidenceSurface.ControllerAudit)
        {
            using var response = await auditHttp.GetAsync($"v1/rev869b/audit/{preparation.RunId}/{read.ReadId}", ct);
            response.EnsureSuccessStatusCode();
            var audit = await ReadSignedAsync<JsonElement>(response, pins.AuditSigningPublicKeyPem, ct);
            return CanonicalObservation(read.ReadId, audit);
        }

        var requested = RequiredRawFacts(contract.ScenarioId, stage);
        var readerIds = requested.Select(x => x.ReaderId).Distinct(StringComparer.Ordinal).ToList();
        if (readerIds.Contains("OR3", StringComparer.Ordinal))
            return ObserveLocalOr3(contract, subcase, stage, read, preparation, readerIds);
        if (readerIds.Count == 0)
            readerIds.Add(stage == EvidenceStage.Cleanup ? "CP-L4" : ReaderId(read.Surface));
        var documents = new List<string>();
        foreach (var readerId in readerIds.Order(StringComparer.Ordinal))
        {
            var connectionString = readerId is "CP-L4" or "CP-A4"
                ? preparation.ControlPlaneVerifierConnectionString
                : preparation.TargetVerifierConnectionString;
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(ct);
            await using var command = BuildReadCommand(readerId, stage,
                requested.Where(x => x.ReaderId == readerId).Select(x => x.Name).ToArray(),
                connection, preparation);
            var scalar = await command.ExecuteScalarAsync(ct) as string
                ?? throw new InvalidOperationException("Independent v4 verifier query returned no raw facts.");
            _ = ParseTypedObservation(scalar);
            documents.Add(scalar);
        }

        var raw = new RawObservationSet(preparation.OrganizationId, preparation.LeaseId,
            preparation.LeaseVersion, preparation.TargetInstanceSha256,
            new Dictionary<EvidenceStage, IReadOnlyList<string>> { [stage] = documents });
        return AdaptStage(contract, subcase, raw, stage);
    }

    internal static void RequireLocalOr3Route(string readerId, EvidenceStage stage, IReadOnlyList<string> readerIds)
    {
        if (readerId != "OR3" || stage != EvidenceStage.Durable || readerIds.Count != 1 || readerIds[0] != "OR3")
            throw new InvalidOperationException("OR3_WRONG_OPERATION");
    }

    private static EvidenceObservation ObserveLocalOr3(AcceptanceContract contract, SubcaseRequirement subcase,
        EvidenceStage stage, EvidenceRead read, ScenarioPreparation preparation, IReadOnlyList<string> readerIds)
    {
        RequireLocalOr3Route("OR3", stage, readerIds);
        var fixture = Rev869BCorrection28IndependentEvidenceFixtures.For(subcase.SubcaseId);
        var results = Enum.GetValues<PipelineMutationKind>()
            .Select(mutation => EvaluatePipelineMutation(contract, subcase, mutation)).ToArray();
        var records = results.Select(result => new MutationRunRecord(Rev869BCorrection26FrozenOracle.Version,
            Rev869BCorrection26FrozenOracle.ExpectedSha256, fixture.ActionIdentity, contract.ScenarioId,
            subcase.SubcaseId, result.MutationId, result.TargetComponent, result.ExpectedRejectionCode,
            result.ActualRejectionCode, result.Survived, result.EvidenceSha256)).ToArray();
        var observation = DispatchLocalOr3(records, contract.ScenarioId, subcase.SubcaseId,
            fixture.PreparationIdentity, fixture.ObservationIdentity, fixture.EnvelopeIdentity);
        var rawJson = SerializeLocalOr3(observation);
        var raw = new RawObservationSet("not-applicable-local", fixture.EnvelopeIdentity, 1,
            new string('a', 64), new Dictionary<EvidenceStage, IReadOnlyList<string>>
            {
                [stage] = new[] { rawJson }
            });
        return AdaptStage(contract, subcase, raw, stage) with { ReadId = read.ReadId };
    }

    private static string SerializeLocalOr3(MutationRunObservationV3 observation)
    {
        var facts = new JsonArray(observation.Facts.Select(fact => (JsonNode)new JsonObject
        {
            ["kind"] = fact.Kind, ["name"] = fact.Name, ["valueType"] = fact.ValueType,
            ["value"] = JsonNode.Parse(fact.Value.GetRawText()), ["sourceRowCount"] = fact.SourceRowCount,
            ["sourceSha256"] = fact.SourceSha256
        }).ToArray());
        var root = new JsonObject
        {
            ["readerSchemaVersion"] = observation.ReaderSchemaVersion,
            ["readerId"] = observation.ReaderId,
            ["scope"] = new JsonObject
            {
                ["companyId"] = observation.Scope.CompanyId,
                ["targetInstanceSha256"] = observation.Scope.TargetInstanceSha256,
                ["leaseId"] = observation.Scope.LeaseId,
                ["leaseVersion"] = observation.Scope.LeaseVersion,
                ["operationId"] = observation.Scope.OperationId,
                ["scenarioExecutionId"] = observation.Scope.ScenarioExecutionId,
                ["stage"] = observation.Scope.Stage
            },
            ["observedAtUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["transactionBoundary"] = observation.TransactionBoundary,
            ["facts"] = facts, ["factCount"] = facts.Count, ["rawSha256"] = new string('0', 64)
        };
        root["rawSha256"] = RawDocumentSha256(root);
        return root.ToJsonString();
    }
    private static RequestedRawFact[] RequiredRawFacts(string scenarioId, EvidenceStage stage)
    {
        var facts = new Dictionary<(string ReaderId, string Kind, string Name), RequestedRawFact>();
        foreach (var selector in Rev869BCorrection26FrozenOracle.SelectorsFor(scenarioId))
        {
            if (Enum.Parse<EvidenceStage>(selector.Stage) == stage)
                facts[(selector.ReaderId, "selector", selector.SelectorName)] =
                    new(selector.SelectorName, selector.ValueType, "selector", selector.ReaderId);
            if (selector.Operator is "EqualsObservationPath" or "NotEqualsObservationPath")
            {
                var reference = ParseReference(selector.Expected);
                if (reference.Stage == stage)
                {
                    var name = reference.Path.StartsWith("references.", StringComparison.Ordinal)
                        ? reference.Path["references.".Length..] : reference.Path;
                    facts[(selector.ReaderId, "reference", name)] =
                        new(name, selector.ValueType, "reference", selector.ReaderId);
                }
            }
            if (selector.Operator == "ExactlyOneTrue")
                foreach (var referenceText in SplitReferences(selector.Expected))
                {
                    var reference = ParseReference(referenceText);
                    if (reference.Stage != stage) continue;
                    var name = reference.Path.StartsWith("references.", StringComparison.Ordinal)
                        ? reference.Path["references.".Length..] : reference.Path;
                    facts[(selector.ReaderId, "reference", name)] =
                        new(name, "bool", "reference", selector.ReaderId);
                }
        }
        return facts.Values.OrderBy(x => x.ReaderId, StringComparer.Ordinal)
            .ThenBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }

    private static NpgsqlCommand BuildReadCommand(string readerId, EvidenceStage stage,
        string[] requestedFacts, NpgsqlConnection connection, ScenarioPreparation p)
    {
        var sql = readerId switch
        {
            "CP-L4" => "SELECT nexa.rev869b_read_lifecycle_facts_v4(@instance_sha256_text,@lease_id,@lease_version,@attempt_id,@request_id,@decision_id,@scenario_execution_id,@observation_stage,@subcase_id,@requested_facts)::text",
            "CP-A4" => "SELECT nexa.rev869b_read_control_acl_facts_v4(@instance_sha256_text,@lease_id,@lease_version,@attempt_id,@scenario_execution_id,@observation_principal,@observation_object,@observation_operation,@observation_stage,@subcase_id,@requested_facts)::text",
            "TC4" => "SELECT nexa.rev869b_read_command_facts_v4(@organization_id,@instance_sha256,@lease_id,@lease_version,@command_id,@attempt_id,@scenario_execution_id,@observation_stage,@subcase_id,@requested_facts)::text",
            "TP4" => "SELECT nexa.rev869b_read_purge_facts_v4(@organization_id,@instance_sha256,@lease_id,@lease_version,@authorization_id,@execution_id,@root_authorization_id,@batch_id,@attempt_id,@scenario_execution_id,@observation_stage,@subcase_id,@requested_facts)::text",
            "TE4" => "SELECT nexa.rev869b_read_export_facts_v4(@organization_id,@instance_sha256,@lease_id,@lease_version,@authorization_id,@batch_id,@release_id,@as_of,@scenario_execution_id,@observation_stage,@attempt_id,@subcase_id,@requested_facts)::text",
            "TA4" => "SELECT nexa.rev869b_read_target_acl_facts_v4(@organization_id,@instance_sha256,@lease_id,@lease_version,@attempt_id,@scenario_execution_id,@observation_principal,@observation_object,@observation_operation,@observation_stage,@subcase_id,@requested_facts)::text",
            _ => throw new InvalidOperationException("Unsupported v4 database evidence reader.")
        };
        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("organization_id", p.OrganizationId);
        command.Parameters.AddWithValue("lease_id", p.LeaseId);
        command.Parameters.AddWithValue("lease_version", p.LeaseVersion);
        command.Parameters.AddWithValue("attempt_id", p.AttemptId);
        command.Parameters.AddWithValue("scenario_execution_id", p.PreparationId);
        command.Parameters.AddWithValue("instance_sha256_text", p.TargetInstanceSha256);
        command.Parameters.Add("instance_sha256", NpgsqlDbType.Bytea).Value = Convert.FromHexString(p.TargetInstanceSha256);
        command.Parameters.AddWithValue("observation_stage", stage.ToString());
        command.Parameters.AddWithValue("subcase_id", p.SubcaseId);
        command.Parameters.AddWithValue("request_id", p.RegistrationRequestId);
        command.Parameters.Add("decision_id", NpgsqlDbType.Uuid).Value = (object?)p.DecisionId ?? DBNull.Value;
        command.Parameters.AddWithValue("command_id", p.CommandId);
        command.Parameters.AddWithValue("authorization_id", p.AuthorizationId);
        command.Parameters.AddWithValue("execution_id", p.ExecutionId);
        command.Parameters.AddWithValue("root_authorization_id", p.RootAuthorizationId);
        command.Parameters.AddWithValue("batch_id", p.BatchId);
        command.Parameters.AddWithValue("as_of", p.AsOf);
        command.Parameters.AddWithValue("observation_principal", p.ObservationPrincipal);
        command.Parameters.AddWithValue("observation_object", p.ObservationObject);
        command.Parameters.AddWithValue("observation_operation", p.ObservationOperation);
        command.Parameters.Add("release_id", NpgsqlDbType.Uuid).Value = (object?)p.ReleaseId ?? DBNull.Value;
        command.Parameters.Add("requested_facts", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = requestedFacts;
        return command;
    }
    private static string ReadStage(string readId) =>
        readId.Contains(":before", StringComparison.Ordinal) ? EvidenceStage.Before.ToString() :
        readId.Contains(":after", StringComparison.Ordinal) ? EvidenceStage.After.ToString() :
        readId.Contains(":durable", StringComparison.Ordinal) ? EvidenceStage.Durable.ToString() :
        readId.Contains(":cleanup", StringComparison.Ordinal) ? EvidenceStage.Cleanup.ToString() :
        throw new InvalidOperationException("Evidence read identifier does not declare an exact observation stage.");

    private async Task RequestCleanupAsync(Guid leaseId, Guid requestId, CancellationToken ct)
    {
        using var response = await actionHttp.PostAsJsonAsync($"v1/rev869b/test-leases/{leaseId}/release", new { leaseId, requestId }, ct);
        response.EnsureSuccessStatusCode();
        var receipt = await ReadSignedAsync<CleanupReceipt>(response, pins.ControllerSigningPublicKeyPem, ct);
        if (receipt.LeaseId != leaseId || receipt.RequestId != requestId || receipt.EvidenceId == Guid.Empty || !ExactSha256(receipt.EvidenceSha256))
            throw new InvalidOperationException("Cleanup receipt correlation failed.");
    }

    private void RequirePreparation(AcceptanceContract contract, SubcaseRequirement subcase,
        Guid runId, string descriptorSha256, ScenarioPreparation p)
    {
        var ids = new[] { p.RunId, p.PreparationId, p.ExpectedResultId, p.LeaseId, p.FixtureId,
            p.CommandId, p.AuthorizationId, p.AttemptId, p.RegistrationRequestId, p.BatchId };
        if (p.RunId != runId || p.ScenarioId != contract.ScenarioId || p.SubcaseId != subcase.SubcaseId ||
            p.PreparationId != subcase.PreparationId || p.EvidenceId != subcase.EvidenceId ||
            p.ExpectedResultId != subcase.ExpectedResultId || p.ExpectedOutcome != subcase.ExpectedResult ||
            p.AttemptId != subcase.AttemptId || p.DescriptorSha256 != descriptorSha256 ||
            ids.Any(x => x == Guid.Empty) || ids.Distinct().Count() != ids.Length ||
            p.RootAuthorizationId == Guid.Empty || p.ExecutionId != p.AttemptId || p.LeaseVersion < 1 || string.IsNullOrWhiteSpace(p.OrganizationId) ||
            p.AsOf == default || string.IsNullOrWhiteSpace(p.ObservationPrincipal) ||
            string.IsNullOrWhiteSpace(p.ObservationObject) || string.IsNullOrWhiteSpace(p.ObservationOperation) ||
            !ExactSha256(p.TargetInstanceSha256) || !ExactSha256(p.FixtureSha256) ||
            p.SourceCommit != pins.SourceCommit || p.ManifestSha256 != pins.ManifestSha256 ||
            p.ClusterSystemIdentifier != pins.ClusterSystemIdentifier ||
            !p.DatabaseName.StartsWith(Rev869BTestDatabaseLease.DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Preparation did not bind exact unique scenario identities and pins.");

        RequireTargetConnection(p.TargetVerifierConnectionString, p.DatabaseName, "nexa_rev869b_target_verifier");
        RequireTargetConnection(p.ControlPlaneVerifierConnectionString, Rev869BControlPlaneProvisioningContract.Database, "nexa_rev869b_control_plane_verifier");
        if ((p.TargetVerifierConnectionString + p.ControlPlaneVerifierConnectionString)
            .Contains("lifecycle_administrator", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Privileged controller credentials must never enter tests.");
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
    internal enum MutationKind { RemoveAction, RemoveRead, RemoveAssertion, WeakenAssertion, FabricateEvidence, DuplicateEvidence, SubstituteIdentity, StaleEvidence, CrossInstanceEvidence, CrossLeaseEvidence, WrongVersionEvidence, WrongCountEvidence }
    internal enum EvidenceMutationKind { Missing, Additional, Duplicated, Altered, Stale, Replayed, Fabricated, CrossInstance, CrossLease, WrongVersion, WrongState, WrongCount }

    internal sealed record EvidenceRead(string ReadId, EvidenceSurface Surface, string Purpose);
    internal sealed record FormulaComponent(string ComponentId, EvidenceStage Stage, string AuthoritativeSelector,
        EvidenceOperator Operator, string Expected, string LocalReducer, string ValueType, string ReaderId,
        string Source, string Scope, string Cardinality, string NullSemantics);
    internal sealed record EvidenceAssertion(string AssertionId, EvidenceStage Stage, string JsonPath, EvidenceOperator Operator, string Expected);
    internal sealed record SemanticMutation(string MutationId, MutationKind Kind, string TargetReadId);
    internal sealed record ScenarioEvidencePlan(string FixtureOperationId, string ActionOperationId, string CleanupOperationId,
        EvidenceRead Before, EvidenceRead After, EvidenceRead Durable, EvidenceRead Audit, EvidenceRead Cleanup,
        string ExactFormula, IReadOnlyList<EvidenceAssertion> Assertions, IReadOnlyList<SemanticMutation> Mutations)
    {
        internal IReadOnlyList<string> RequiredComponentIds { get; init; } = Array.Empty<string>();
        internal IReadOnlyList<FormulaComponent> FormulaComponents { get; init; } = Array.Empty<FormulaComponent>();
    }
    internal sealed record AcceptanceDescriptor(string ScenarioId, string Setup, string Action, string ExpectedResult,
        DatabaseObjectIdentity ExpectedIdentity, ScenarioEvidencePlan Plan, IReadOnlyList<string> Subcases);
    internal sealed record AcceptanceContract(string ScenarioId, string Setup, string Action, string ExpectedResult,
        DatabaseObjectIdentity ExpectedIdentity, ScenarioEvidencePlan Plan, IReadOnlyList<SubcaseRequirement> RequiredSubcases)
    {
        internal AcceptanceDescriptor Descriptor => new(ScenarioId, Setup, Action, ExpectedResult, ExpectedIdentity,
            Plan, RequiredSubcases.Select(x => x.SubcaseId).ToArray());
    }

    internal sealed record SubcaseRequirement(string SubcaseId, string ExpectedResult, Guid PreparationId,
        Guid AttemptId, Guid EvidenceId, Guid ExpectedResultId, string ActionId);
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

    internal sealed record AcceptanceResult(string ScenarioId, string SubcaseId, Guid RunId, Guid LeaseId, Guid FixtureId, Guid CommandId,
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

    internal sealed record ScenarioPreparation(Guid RunId, string ScenarioId, string SubcaseId, Guid PreparationId,
        Guid EvidenceId, Guid ExpectedResultId, string ExpectedOutcome, Guid LeaseId, long LeaseVersion, string OrganizationId, Guid FixtureId,
        Guid CommandId, Guid AuthorizationId, Guid ExecutionId, Guid AttemptId, Guid RegistrationRequestId, Guid? DecisionId,
        Guid RootAuthorizationId, Guid BatchId, Guid? ReleaseId, DateTimeOffset AsOf,
        string ObservationPrincipal, string ObservationObject, string ObservationOperation,
        string DatabaseName, string TargetInstanceSha256, string FixtureSha256,
        string TargetVerifierConnectionString, string ControlPlaneVerifierConnectionString, string SourceCommit,
        string ManifestSha256, string ClusterSystemIdentifier, string DescriptorSha256);

    internal sealed record ActionReceipt(Guid RunId, string ScenarioId, string SubcaseId, Guid PreparationId,
        Guid ExpectedResultId, Guid LeaseId, Guid FixtureId, Guid CommandId,
        Guid AuthorizationId, Guid AttemptId, Guid? DecisionId, bool ActionReached, int AffectedRows,
        string? SqlState, string? ErrorCode, string? DatabaseObject, string TerminalState,
        Guid EvidenceId, string EvidenceSha256, Guid ControllerInstanceId, int HttpStatus);

    internal sealed record CleanupReceipt(Guid LeaseId, Guid RequestId, Guid EvidenceId, string EvidenceSha256);
    internal sealed record SignedAcceptanceEnvelope(string PayloadBase64, string SignatureBase64);
    private sealed record AcceptancePins(string SourceCommit, string ManifestSha256, string TlsSpkiSha256,
        string ClusterSystemIdentifier, string ControllerSigningPublicKeyPem, string ControllerSigningPublicKeySha256,
        string AuditSigningPublicKeyPem, string AuditSigningPublicKeySha256);
}
