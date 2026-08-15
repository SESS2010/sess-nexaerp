using System.Security.Cryptography;
using System.Text.Json;
using SESS.NexaERP.AcceptanceVerifier.Verification;
using SESS.NexaERP.ControlPlane.Configuration;
using SESS.NexaERP.ControlPlane.Contracts;
using SESS.NexaERP.ControlPlane.Domain;
using SESS.NexaERP.ControlPlane.Security;
using Xunit;

namespace SESS.NexaERP.ControlPlane.Tests;

public sealed class ArchitectureFreezeContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compatibility_manifest_is_closed_and_versioned()
    {
        Assert.True(Rev869BCompatibilityManifestV1.IsCompatible(
            "rev869b-controller-v1", "rev869b-evidence-v1", "rev869b-json-v1"));
        Assert.False(Rev869BCompatibilityManifestV1.IsCompatible(
            "rev869b-controller-v2", "rev869b-evidence-v1", "rev869b-json-v1"));
        Assert.Equal("ECDSA-P256-SHA256", Rev869BCompatibilityManifestV1.SignatureAlgorithm);
    }

    [Fact]
    public void Canonical_json_is_deterministic_and_sorts_object_keys()
    {
        var first = CanonicalJsonV1.SerializeToString(new Dictionary<string, int> { ["z"] = 1, ["a"] = 2 });
        var second = CanonicalJsonV1.SerializeToString(new Dictionary<string, int> { ["a"] = 2, ["z"] = 1 });
        Assert.Equal(first, second);
        Assert.Equal("{\"a\":2,\"z\":1}", first);
    }

    [Fact]
    public void State_machine_rejects_skip_and_allows_frozen_path()
    {
        var machine = new Rev869BControllerStateMachine();
        Assert.True(machine.CanTransition(ControllerLifecycleState.Registered, ControllerLifecycleState.Preflight));
        var rejection = Assert.Throws<TrustRejectionException>(() =>
            machine.RequireTransition(ControllerLifecycleState.Registered, ControllerLifecycleState.Accepted));
        Assert.Equal(TrustRejectionCode.IllegalTransition, rejection.Code);
    }

    [Fact]
    public void Binding_comparison_fails_closed_on_company_instance_lease_and_subcase()
    {
        var expected = Binding();
        AssertCode(TrustRejectionCode.WrongCompany, expected with { Company = new CompanyScope("C2") });
        AssertCode(TrustRejectionCode.WrongTargetInstance, expected with { Target = expected.Target with { Value = "erp-2" } });
        AssertCode(TrustRejectionCode.WrongLease, expected with { LeaseVersion = 2 });
        AssertCode(TrustRejectionCode.WrongSubcase, expected with { SubcaseId = "subcase-2" });

        void AssertCode(TrustRejectionCode code, Rev869BExecutionBinding actual)
        {
            var rejection = Assert.Throws<TrustRejectionException>(() => Rev869BExecutionBindingValidator.RequireExact(actual, expected));
            Assert.Equal(code, rejection.Code);
        }
    }

    [Fact]
    public void Command_signing_and_verification_rejects_replay_and_revocation()
    {
        var crypto = new FakeCrypto("command-key");
        var clock = new FixedTimeProvider(Now);
        var signed = new SignedEnvelopeService(crypto, clock).Sign(Command());
        var replay = new ReplayGuard();
        var registry = new KeyRegistry(new SigningKeyDescriptor("command-key", Rev869BCompatibilityManifestV1.SignatureAlgorithm,
            SigningKeyState.Active, Now.AddDays(-1), Now.AddDays(1), null));
        var verifier = new SignedEnvelopeVerificationService(crypto, registry, replay, clock);

        verifier.Verify(signed, TimeSpan.FromMinutes(5));
        var repeated = Assert.Throws<TrustRejectionException>(() => verifier.Verify(signed, TimeSpan.FromMinutes(5)));
        Assert.Equal(TrustRejectionCode.ReplayDetected, repeated.Code);

        var revokedRegistry = new KeyRegistry(registry.Key with { State = SigningKeyState.Revoked, RevokedAtUtc = Now });
        var revoked = Assert.Throws<TrustRejectionException>(() =>
            new SignedEnvelopeVerificationService(crypto, revokedRegistry, new ReplayGuard(), clock)
                .Verify(signed, TimeSpan.FromMinutes(5)));
        Assert.Equal(TrustRejectionCode.RevokedKey, revoked.Code);
    }

    [Fact]
    public void Command_verification_fails_closed_for_version_algorithm_key_tamper_and_staleness()
    {
        var crypto = new FakeCrypto("command-key");
        var signed = new SignedEnvelopeService(crypto, new FixedTimeProvider(Now)).Sign(Command());

        AssertVerificationCode(signed with { ContractVersion = "unknown" }, TrustRejectionCode.UnsupportedContractVersion);
        AssertVerificationCode(signed with { Signature = signed.Signature with { Algorithm = "unknown" } },
            TrustRejectionCode.UnsupportedSignatureAlgorithm);
        AssertVerificationCode(signed with { Signature = signed.Signature with { KeyId = "wrong-key" } },
            TrustRejectionCode.UnknownKey);
        AssertVerificationCode(signed with { Command = signed.Command with { CommandId = "tampered" } },
            TrustRejectionCode.PayloadHashMismatch);
        AssertVerificationCode(signed with { Signature = signed.Signature with { SignedAtUtc = Now.AddHours(-1) } },
            TrustRejectionCode.StaleEnvelope);

        void AssertVerificationCode(SignedCommandEnvelopeV1 envelope, TrustRejectionCode expected)
        {
            var registry = new KeyRegistry(new SigningKeyDescriptor("command-key", Rev869BCompatibilityManifestV1.SignatureAlgorithm,
                SigningKeyState.Active, Now.AddDays(-1), Now.AddDays(1), null));
            var verifier = new SignedEnvelopeVerificationService(crypto, registry, new ReplayGuard(), new FixedTimeProvider(Now));
            var rejection = Assert.Throws<TrustRejectionException>(() => verifier.Verify(envelope, TimeSpan.FromMinutes(5)));
            Assert.Equal(expected, rejection.Code);
        }
    }

    [Fact]
    public void Command_policy_rejects_stale_lease_and_cross_role_authorization()
    {
        var command = Command();
        var stale = command with { Lease = command.Lease with { LeaseVersion = 2 } };
        var staleRejection = Assert.Throws<TrustRejectionException>(() => ControllerAuthorizationPolicyV1.RequireAuthorized(stale));
        Assert.Equal(TrustRejectionCode.WrongLease, staleRejection.Code);

        var wrongRole = command with
        {
            Authorization = command.Authorization with { Roles = [ControllerRole.MonitoringReader] }
        };
        var roleRejection = Assert.Throws<TrustRejectionException>(() => ControllerAuthorizationPolicyV1.RequireAuthorized(wrongRole));
        Assert.Equal(TrustRejectionCode.UnauthorizedRole, roleRejection.Code);
    }

    [Fact]
    public void Evidence_contract_has_no_caller_supplied_verdict()
    {
        var json = JsonSerializer.Serialize(SignedEvidence(new FakeCrypto("evidence-key")));
        Assert.DoesNotContain("verdict", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disposition", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passed", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_verifier_calculates_pass_and_writes_durable_audit_reference()
    {
        var crypto = new FakeCrypto("evidence-key");
        var evidence = SignedEvidence(crypto);
        var audit = new AuditSink();
        var verifier = new ClosedEvidenceVerifierV1(
            crypto,
            new OracleCatalog(new PassingOracle("oracle-1")),
            audit,
            new FixedTimeProvider(Now));

        var result = verifier.Verify(new EvidenceVerificationRequestV1(evidence, Binding(), 10, 10));

        Assert.Equal(VerificationDisposition.Passed, result.Disposition);
        Assert.Equal("audit-1", result.VerificationAuditReference);
        Assert.NotNull(audit.Event);
    }

    [Fact]
    public void Closed_verifier_rejects_missing_durable_stage_before_oracle_runs()
    {
        var crypto = new FakeCrypto("evidence-key");
        var original = SignedEvidence(crypto);
        var unsigned = original with
        {
            Observations = original.Observations.Where(item => item.Stage != ObservationStage.Durable).ToArray(),
            Signature = EmptySignature("evidence-key")
        };
        var malformed = SignEvidence(unsigned, crypto);
        var verifier = new ClosedEvidenceVerifierV1(
            crypto,
            new OracleCatalog(new PassingOracle("oracle-1")),
            new AuditSink(),
            new FixedTimeProvider(Now));

        var rejection = Assert.Throws<TrustRejectionException>(() =>
            verifier.Verify(new EvidenceVerificationRequestV1(malformed, Binding(), 10, 10)));
        Assert.Equal(TrustRejectionCode.MissingObservationStage, rejection.Code);
    }

    [Fact]
    public void Closed_verifier_enforces_bounded_payload_rules()
    {
        var crypto = new FakeCrypto("evidence-key");
        var verifier = new ClosedEvidenceVerifierV1(
            crypto,
            new OracleCatalog(new PassingOracle("oracle-1")),
            new AuditSink(),
            new FixedTimeProvider(Now));

        var rejection = Assert.Throws<TrustRejectionException>(() =>
            verifier.Verify(new EvidenceVerificationRequestV1(SignedEvidence(crypto), Binding(), 2, 10)));
        Assert.Equal(TrustRejectionCode.PayloadLimitExceeded, rejection.Code);
    }

    [Fact]
    public void Options_reject_production_identity_and_accept_bounded_nonproduction_pattern()
    {
        var valid = Options("rev869b-control", ["^rev869b_[a-z0-9_]+$"]);
        Assert.True(ControlPlaneOptions.IsValid(valid));
        Assert.True(valid.IsTargetAllowed(new TargetErpInstanceIdentity("erp-1", "test", "rev869b_case_01")));
        Assert.False(Options("production", ["^rev869b_[a-z0-9_]+$"]).Pipe(ControlPlaneOptions.IsValid));
        Assert.False(valid.IsTargetAllowed(new TargetErpInstanceIdentity("erp-1", "test", "prod")));
    }

    private static ControlPlaneOptions Options(string database, string[] patterns) => new()
    {
        ServiceIdentity = "sess-control-plane",
        ControlPlaneDatabaseIdentity = database,
        CommandSigningKeyId = "command-key",
        EvidenceVerificationKeyId = "evidence-key",
        ContractVersion = Rev869BCompatibilityManifestV1.ContractVersion,
        EvidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
        CanonicalizationVersion = Rev869BCompatibilityManifestV1.CanonicalizationVersion,
        RetentionPolicyReference = "retention-1",
        MaximumEvidenceObservations = 100,
        MaximumFactsPerObservation = 100,
        MaximumReplayWindowSeconds = 300,
        MaximumLeaseSeconds = 300,
        ControlPlaneEndpoint = "https://control-plane.invalid",
        AcceptanceVerifierEndpoint = "https://acceptance-verifier.invalid",
        AllowedTargetEnvironments = ["test"],
        AllowedDatabaseIdentityPatterns = patterns
    };

    private static LifecycleCommandV1 Command() => new(
        "command-1", ControllerCommandKind.BeginPreflight, Binding(), ControllerLifecycleState.Registered,
        ControllerLifecycleState.Preflight, new LeaseExpectation("lease-1", 1),
        new IdempotencyReplayKey("replay-1", Now.AddMinutes(2)),
        new CommandAuthorization("operator-1", [ControllerRole.Operator], Now), Now);

    private static Rev869BExecutionBinding Binding() => new(
        new CompanyScope("C1"), new ControlPlaneInstanceIdentity("cp-1"),
        new TargetErpInstanceIdentity("erp-1", "test", "rev869b_case_01"),
        "lease-1", 1, "operation-1", "preparation-1", "attempt-1", "execution-1",
        "scenario-1", "subcase-1", "oracle-1", "action-1");

    private static CanonicalEvidenceEnvelopeV1 SignedEvidence(FakeCrypto crypto)
    {
        var facts = new Dictionary<string, TypedSelectorValueV1>
        {
            ["count"] = new(SelectorValueKind.Integer, "1")
        };
        var unsigned = new CanonicalEvidenceEnvelopeV1(
            Rev869BCompatibilityManifestV1.EvidenceVersion,
            Rev869BCompatibilityManifestV1.ContractVersion,
            Binding(),
            [new EvidenceSelectorV1("count", "eq", new TypedSelectorValueV1(SelectorValueKind.Integer, "1"),
                new SelectorReaderProvenanceV1("reader-1", "facts-v1", ObservationSourceKind.TargetDatabase))],
            [
                new FactOnlyObservationV1("before-1", ObservationStage.Before, new(ObservationSourceKind.TargetDatabase, "db", Now), facts),
                new FactOnlyObservationV1("after-1", ObservationStage.After, new(ObservationSourceKind.TargetDatabase, "db", Now), facts),
                new FactOnlyObservationV1("durable-1", ObservationStage.Durable, new(ObservationSourceKind.ControllerLedger, "ledger", Now), facts)
            ],
            new ActionResultV1(true, 1, null, null, "object-1", "complete", 200, "evidence-1"),
            EmptySignature(crypto.KeyId),
            "evidence-envelope-1",
            "oracle-v1",
            new string('A', 64));
        return SignEvidence(unsigned, crypto);
    }

    private static SignatureMetadataV1 EmptySignature(string keyId) => new(
        keyId, Rev869BCompatibilityManifestV1.SignatureAlgorithm,
        Rev869BCompatibilityManifestV1.CanonicalizationVersion, string.Empty, string.Empty, Now);

    private static CanonicalEvidenceEnvelopeV1 SignEvidence(CanonicalEvidenceEnvelopeV1 unsigned, FakeCrypto crypto)
    {
        var canonical = CanonicalJsonV1.Serialize(unsigned);
        return unsigned with
        {
            Signature = unsigned.Signature with
            {
                PayloadSha256 = Convert.ToHexString(SHA256.HashData(canonical)),
                SignatureBase64 = Convert.ToBase64String(crypto.Sign(canonical))
            }
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCrypto(string keyId) : IEnvelopeSigner, IEnvelopeSignatureVerifier, IEvidenceSignatureVerifier
    {
        public string KeyId { get; } = keyId;
        public byte[] Sign(ReadOnlySpan<byte> canonicalPayload) => SHA256.HashData(canonicalPayload);
        public bool Verify(string requestedKeyId, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature) =>
            requestedKeyId == KeyId && CryptographicOperations.FixedTimeEquals(Sign(canonicalPayload), signature);
        public SigningKeyDescriptor? FindKey(string requestedKeyId) => requestedKeyId == KeyId
            ? new(KeyId, Rev869BCompatibilityManifestV1.SignatureAlgorithm, SigningKeyState.Active, Now.AddDays(-1), Now.AddDays(1), null)
            : null;
    }

    private sealed class KeyRegistry(SigningKeyDescriptor key) : ISigningKeyRegistry
    {
        public SigningKeyDescriptor Key { get; } = key;
        public SigningKeyDescriptor? Find(string keyId) => keyId == Key.KeyId ? Key : null;
    }

    private sealed class ReplayGuard : IReplayGuard
    {
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        public bool TryAccept(string replayKey, DateTimeOffset expiresAtUtc) => _seen.Add(replayKey);
    }

    private sealed class PassingOracle(string oracleId) : IClosedOracleV1
    {
        public string OracleId { get; } = oracleId;
        public bool Evaluate(CanonicalEvidenceEnvelopeV1 evidence) => true;
    }

    private sealed class OracleCatalog(IClosedOracleV1 oracle) : IClosedOracleCatalogV1
    {
        public IClosedOracleV1? Find(string oracleId) => oracleId == oracle.OracleId ? oracle : null;
    }

    private sealed class AuditSink : IVerificationAuditSinkV1
    {
        public VerificationAuditEventV1? Event { get; private set; }
        public string Append(VerificationAuditEventV1 auditEvent)
        {
            Event = auditEvent;
            return "audit-1";
        }
    }
}

internal static class TestObjectExtensions
{
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> function) => function(value);
}
