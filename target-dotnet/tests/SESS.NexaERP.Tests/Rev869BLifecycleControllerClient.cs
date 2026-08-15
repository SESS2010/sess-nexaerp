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

        var before = await ObserveAsync(contract.Plan.Before, preparation, ct);
        var action = await ActAsync(contract, subcase, preparation, runId, descriptorSha256, ct);
        var after = await ObserveAsync(contract.Plan.After, preparation, ct);
        var durable = await ObserveAsync(contract.Plan.Durable, preparation, ct);
        var independentAudit = await ObserveAsync(contract.Plan.Audit, preparation, ct);

        var cleanupRequestId = Guid.NewGuid();
        await RequestCleanupAsync(preparation.LeaseId, cleanupRequestId, ct);
        var cleanup = await ObserveAsync(contract.Plan.Cleanup, preparation, ct);

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
                !Exact("preparationId", subcase.PreparationId.ToString()) ||
                !Exact("attemptId", subcase.AttemptId.ToString()) ||
                !Exact("evidenceId", subcase.EvidenceId.ToString()) ||
                !Exact("expectedResultId", subcase.ExpectedResultId.ToString()) ||
                !Exact("expectedOutcome", subcase.ExpectedResult) ||
                !Exact("provenance", "authoritative-local-reader"))
                failures.Add("envelope:" + stage);
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
            if (!root.TryGetProperty("selectorReaders", out var readers) || readers.ValueKind != JsonValueKind.Object ||
                expected.Any(selector => !readers.TryGetProperty(selector.SelectorName, out var reader) ||
                    reader.ValueKind != JsonValueKind.String || reader.GetString() != selector.ReaderId))
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
    internal static EvidenceBundle BuildOracleEvidence(AcceptanceContract contract, SubcaseRequirement subcase)
    {
        ValidateContract(contract);
        if (contract.RequiredSubcases.All(x => x.SubcaseId != subcase.SubcaseId))
            throw new ArgumentException("Subcase is not part of the frozen scenario.", nameof(subcase));
        var instanceSha256 = ExactContractSha256(new { subcase.SubcaseId, kind = "target-instance" });
        var roots = Enum.GetValues<EvidenceStage>().ToDictionary(stage => stage,
            stage => new JsonObject
            {
                ["observationStage"] = stage.ToString(),
                ["oracleVersion"] = Rev869BCorrection26FrozenOracle.Version,
                ["oracleSha256"] = Rev869BCorrection26FrozenOracle.ExpectedSha256,
                ["scenarioId"] = contract.ScenarioId,
                ["subcaseId"] = subcase.SubcaseId,
                ["preparationId"] = subcase.PreparationId,
                ["attemptId"] = subcase.AttemptId,
                ["evidenceId"] = subcase.EvidenceId,
                ["expectedResultId"] = subcase.ExpectedResultId,
                ["expectedOutcome"] = subcase.ExpectedResult,
                ["targetInstanceSha256"] = instanceSha256,
                ["leaseBindingId"] = subcase.PreparationId,
                ["asOfSequence"] = 1,
                ["duplicateEvidenceCount"] = 0,
                ["provenance"] = stage == EvidenceStage.Audit ? "controller-supplementary" : "authoritative-local-reader"
            });

        foreach (var selector in Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId))
        {
            var assertion = new EvidenceAssertion(selector.ComponentId, Enum.Parse<EvidenceStage>(selector.Stage),
                "selectors." + selector.SelectorName, Enum.Parse<EvidenceOperator>(selector.Operator), selector.Expected);
            if (assertion.Operator == EvidenceOperator.ExactlyOneTrue)
            {
                var references = SplitReferences(assertion.Expected);
                for (var index = 0; index < references.Length; index++)
                {
                    var reference = ParseReference(references[index]);
                    SetPath(roots[reference.Stage], reference.Path, JsonValue.Create(index == 0));
                }
                continue;
            }

            if (assertion.Operator is EvidenceOperator.SameCanonicalSha256AsBefore or EvidenceOperator.DifferentCanonicalSha256FromBefore)
                continue;

            JsonNode? value = assertion.Operator switch
            {
                EvidenceOperator.Exists => JsonValue.Create("present"),
                EvidenceOperator.Absent => null,
                EvidenceOperator.EqualsLiteral => JsonValue.Create(assertion.Expected),
                EvidenceOperator.NotEqualsLiteral => JsonValue.Create("different:" + assertion.Expected),
                EvidenceOperator.GreaterThanZero => JsonValue.Create(1L),
                EvidenceOperator.Zero => JsonValue.Create(0L),
                EvidenceOperator.ExactSha256 => JsonValue.Create(new string('a', 64)),
                EvidenceOperator.AtMostOne => JsonValue.Create(1L),
                EvidenceOperator.EqualsObservationPath => JsonValue.Create("same-value"),
                EvidenceOperator.NotEqualsObservationPath => JsonValue.Create("left-value"),
                _ => throw new ArgumentOutOfRangeException(nameof(assertion.Operator))
            };

            if (assertion.Operator != EvidenceOperator.Absent)
                SetPath(roots[assertion.Stage], assertion.JsonPath, value);

            if (assertion.Operator is EvidenceOperator.EqualsObservationPath or EvidenceOperator.NotEqualsObservationPath)
            {
                var reference = ParseReference(assertion.Expected);
                var referenced = GetPath(roots[reference.Stage], reference.Path);
                if (referenced is null)
                {
                    SetPath(roots[reference.Stage], reference.Path, JsonValue.Create("right-value"));
                    referenced = GetPath(roots[reference.Stage], reference.Path);
                }
                SetPath(roots[assertion.Stage], assertion.JsonPath,
                    assertion.Operator == EvidenceOperator.EqualsObservationPath
                        ? referenced?.DeepClone()
                        : JsonValue.Create("left-value"));
            }
        }

        SetPath(roots[EvidenceStage.Action], "actionReached", JsonValue.Create(true));
        SetPath(roots[EvidenceStage.Action], "terminalState", JsonValue.Create(subcase.ExpectedResult));
        var scenario = Rev869BCorrection26FrozenOracle.Scenario(contract.ScenarioId);
        if (scenario.SqlState.Length != 0) SetPath(roots[EvidenceStage.Action], "sqlState", JsonValue.Create(scenario.SqlState));
        if (scenario.ErrorCode.Length != 0) SetPath(roots[EvidenceStage.Action], "errorCode", JsonValue.Create(scenario.ErrorCode));
        if (scenario.DatabaseObject.Length != 0) SetPath(roots[EvidenceStage.Action], "databaseObject", JsonValue.Create(scenario.DatabaseObject));
        SetPath(roots[EvidenceStage.Cleanup], "lease", new JsonObject { ["leaseBindingId"] = subcase.PreparationId });
        foreach (var stage in new[] { EvidenceStage.Before, EvidenceStage.After, EvidenceStage.Durable })
        {
            var selectorReaders = new JsonObject();
            foreach (var selector in RequiredSelectorReaders(contract.ScenarioId, stage))
                selectorReaders[selector.SelectorName] = selector.ReaderId;
            if (selectorReaders.Count != 0) roots[stage]["selectorReaders"] = selectorReaders;
        }

        EvidenceObservation Observation(EvidenceStage stage)
        {
            using var document = JsonDocument.Parse(roots[stage].ToJsonString());
            return CanonicalObservation("synthetic:" + contract.ScenarioId + ":" + stage, document.RootElement);
        }

        var bundle = new EvidenceBundle(Observation(EvidenceStage.Before), Observation(EvidenceStage.After),
            Observation(EvidenceStage.Durable), Observation(EvidenceStage.Audit), Observation(EvidenceStage.Cleanup),
            Observation(EvidenceStage.Action));
        foreach (var selector in Rev869BCorrection26FrozenOracle.SelectorsFor(contract.ScenarioId))
        {
            var assertion = new EvidenceAssertion(selector.ComponentId, Enum.Parse<EvidenceStage>(selector.Stage),
                "selectors." + selector.SelectorName, Enum.Parse<EvidenceOperator>(selector.Operator), selector.Expected);
            if (assertion.Operator == EvidenceOperator.SameCanonicalSha256AsBefore)
                bundle = ReplaceStage(bundle, assertion.Stage, bundle.For(assertion.Stage) with { CanonicalSha256 = bundle.Before.CanonicalSha256 });
            else if (assertion.Operator == EvidenceOperator.DifferentCanonicalSha256FromBefore &&
                     bundle.For(assertion.Stage).CanonicalSha256 == bundle.Before.CanonicalSha256)
                bundle = ReplaceStage(bundle, assertion.Stage, bundle.For(assertion.Stage) with { CanonicalSha256 = new string('b', 64) });
        }
        return bundle;
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
            if (selector.Stage == stage.ToString() && selector.Operator != "ExactlyOneTrue")
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
        await using var command = BuildReadCommand(read, connection, preparation);
        var scalar = await command.ExecuteScalarAsync(ct) as string
            ?? throw new InvalidOperationException("Independent verifier query returned no evidence.");
        using var document = JsonDocument.Parse(scalar);
        return CanonicalObservation(read.ReadId, document.RootElement);
    }

    private static NpgsqlCommand BuildReadCommand(EvidenceRead read, NpgsqlConnection connection, ScenarioPreparation p)
    {
        var sql = read.Surface switch
        {
            EvidenceSurface.ControlLifecycle => "SELECT nexa.rev869b_read_lifecycle_evidence_v2(@instance_sha256_text,@lease_id,@scenario_id,@subcase_id,@attempt_id,@request_id,@decision_id,@lease_version)::text",
            EvidenceSurface.ControlAcl => "SELECT nexa.rev869b_read_control_plane_acl_evidence_v2(@oracle_version,@observation_stage)::text",
            EvidenceSurface.TargetCommand => "SELECT nexa.rev869b_read_command_evidence_v2(@instance_sha256,@lease_id,@scenario_id,@subcase_id,@command_id,@attempt_id)::text",
            EvidenceSurface.TargetPurge => "SELECT nexa.rev869b_read_purge_evidence_v2(@instance_sha256,@lease_id,@scenario_id,@subcase_id,@authorization_id,@root_authorization_id,@batch_id,@attempt_id)::text",
            EvidenceSurface.TargetExport => "SELECT nexa.rev869b_read_export_evidence_v2(@instance_sha256,@lease_id,@scenario_id,@subcase_id,@authorization_id,@batch_id,@release_id,@as_of)::text",
            EvidenceSurface.TargetAcl => "SELECT nexa.rev869b_read_target_acl_evidence_v2(@instance_sha256,@lease_id,@scenario_id,@subcase_id,@observation_principal,@observation_object,@observation_operation,@observation_stage)::text",
            _ => throw new InvalidOperationException("Unsupported database evidence surface.")
        };
        var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("lease_id", p.LeaseId);
        command.Parameters.AddWithValue("lease_version", p.LeaseVersion);
        command.Parameters.AddWithValue("attempt_id", p.AttemptId);
        command.Parameters.AddWithValue("scenario_id", p.ScenarioId);
        command.Parameters.AddWithValue("subcase_id", p.SubcaseId);
        command.Parameters.AddWithValue("instance_sha256_text", p.TargetInstanceSha256);
        command.Parameters.Add("instance_sha256", NpgsqlDbType.Bytea).Value = Convert.FromHexString(p.TargetInstanceSha256);
        command.Parameters.AddWithValue("oracle_version", Rev869BCorrection26FrozenOracle.Version);
        command.Parameters.AddWithValue("observation_stage", ReadStage(read.ReadId));
        command.Parameters.AddWithValue("request_id", p.RegistrationRequestId);
        command.Parameters.Add("decision_id", NpgsqlDbType.Uuid).Value = (object?)p.DecisionId ?? DBNull.Value;
        command.Parameters.AddWithValue("command_id", p.CommandId);
        command.Parameters.AddWithValue("authorization_id", p.AuthorizationId);
        command.Parameters.AddWithValue("root_authorization_id", p.RootAuthorizationId);
        command.Parameters.AddWithValue("batch_id", p.BatchId);
        command.Parameters.AddWithValue("as_of", p.AsOf);
        command.Parameters.AddWithValue("observation_principal", p.ObservationPrincipal);
        command.Parameters.AddWithValue("observation_object", p.ObservationObject);
        command.Parameters.AddWithValue("observation_operation", p.ObservationOperation);
        command.Parameters.Add("release_id", NpgsqlDbType.Uuid).Value = (object?)p.ReleaseId ?? DBNull.Value;
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
            p.RootAuthorizationId == Guid.Empty || p.LeaseVersion < 1 ||
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
        Guid EvidenceId, Guid ExpectedResultId, string ExpectedOutcome, Guid LeaseId, long LeaseVersion, Guid FixtureId,
        Guid CommandId, Guid AuthorizationId, Guid AttemptId, Guid RegistrationRequestId, Guid? DecisionId,
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
