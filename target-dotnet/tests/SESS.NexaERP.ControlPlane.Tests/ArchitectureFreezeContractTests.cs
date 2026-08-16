using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using SESS.NexaERP.AcceptanceVerifier.Configuration;
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

    [Fact]
    public void CanonicalV2GoldenVectorIsByteExact()
    {
        var fixture = V2Fixture.Create();
        var bytes = CanonicalSignedHeaderCodecV2.Serialize(fixture.Envelope.Header);
        var text = Encoding.UTF8.GetString(bytes);

        Assert.StartsWith("SESS-REV869B-COMMAND-V2\ncontract_version=21:rev869b-controller-v2\n", text);
        Assert.EndsWith($"canonical_payload_length={fixture.Envelope.Header.CanonicalPayloadLength.ToString().Length}:{fixture.Envelope.Header.CanonicalPayloadLength}\n", text);
        Assert.Equal(25, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1);
        var goldenHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Assert.Equal("834b85f94e3e1c4ff11ed943f4428259e1a7fac753f79e3145d27d6e4a3ea4a6", goldenHash);
        Assert.Equal(bytes, CanonicalSignedHeaderCodecV2.Serialize(fixture.Envelope.Header));
        Assert.Equal(fixture.Envelope.Header, CanonicalSignedHeaderCodecV2.Parse(bytes));
    }

    [Fact]
    public async Task EveryProtectedHeaderMutationIsRejected()
    {
        var baseFixture = V2Fixture.Create();
        var mutations = new (Func<CanonicalSignedHeaderV2, CanonicalSignedHeaderV2> Mutate, TrustFailureCodeV2 Code)[]
        {
            (h => h with { ContractVersion = "rev869b-controller-v3" }, TrustFailureCodeV2.CONTRACT_UNSUPPORTED),
            (h => h with { CanonicalizationVersion = "rev869b-command-header-v3" }, TrustFailureCodeV2.CANONICALIZATION_UNSUPPORTED),
            (h => h with { Algorithm = "ECDSA-P384-SHA384" }, TrustFailureCodeV2.ALGORITHM_UNSUPPORTED),
            (h => h with { KeyId = "unknown-key" }, TrustFailureCodeV2.KEY_UNKNOWN),
            (h => h with { Issuer = "unknown-issuer" }, TrustFailureCodeV2.ISSUER_UNKNOWN),
            (h => h with { Audience = "other-audience" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { Subject = "other-subject" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { AuthorizedRole = "MonitoringReader" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { AuthorizedScope = "ORG:C2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { OrganizationId = "C2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { DatabaseClusterId = "cluster-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { DatabaseInstanceId = "instance-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { Operation = "AUTHORIZE_EXECUTE" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { ResourceId = "resource-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { ResourceVersion = 2 }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { LeaseId = "lease-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { FencingToken = 2 }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { RequestId = "request-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { IdempotencyKey = "idempotency-2" }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { Nonce = V2Fixture.SecondNonce }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { IssuedAt = h.IssuedAt.AddSeconds(-1) }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { NotBefore = h.NotBefore.AddSeconds(1) }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { ExpiresAt = h.ExpiresAt.AddSeconds(1) }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { CanonicalPayloadSha256 = new string('a', 64) }, TrustFailureCodeV2.SIGNATURE_INVALID),
            (h => h with { CanonicalPayloadLength = h.CanonicalPayloadLength + 1 }, TrustFailureCodeV2.SIGNATURE_INVALID)
        };

        foreach (var (mutate, code) in mutations)
        {
            var fixture = V2Fixture.Create();
            var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
                await fixture.VerifyAsync(
                    fixture.Envelope with { Header = mutate(baseFixture.Envelope.Header) },
                    fixture.Subject,
                    fixture.Resource));
            Assert.Equal(code, rejection.Code);
            Assert.Equal(0, fixture.Audit.AcceptedCount);
        }
    }

    [Fact]
    public async Task EveryPayloadFieldMutationBreaksHash()
    {
        var mutations = new Func<CanonicalCommandPayloadV2, CanonicalCommandPayloadV2>[]
        {
            p => p with { Operation = ControllerOperationV2.AUTHORIZE_EXECUTE },
            p => p with { ExpectedState = ControllerLifecycleState.Ready },
            p => p with { RequestedState = ControllerLifecycleState.Quarantined },
            p => p with { Scenario = new("scenario-2") },
            p => p with { Subcase = new("subcase-2") },
            p => p with { ActionId = "action-2" },
            p => p with { ApprovedParameters = new Dictionary<string, string> { ["manifest"] = "other" } },
            p => p with { EvidenceRequirements = ["other-evidence"] }
        };

        foreach (var mutate in mutations)
        {
            var fixture = V2Fixture.Create();
            var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
                await fixture.VerifyAsync(
                    fixture.Envelope with { Payload = mutate(fixture.Envelope.Payload) },
                    fixture.Subject,
                    fixture.Resource));
            Assert.Equal(TrustFailureCodeV2.PAYLOAD_HASH_MISMATCH, rejection.Code);
            Assert.Equal(0, fixture.Nonces.ReservationCount);
        }
    }

    [Fact]
    public async Task UnknownIssuerKeyAlgorithmVersionFailClosed()
    {
        await AssertResignedHeaderFailure(h => h with { Issuer = "unknown-issuer" }, TrustFailureCodeV2.ISSUER_UNKNOWN);
        await AssertResignedHeaderFailure(h => h with { KeyId = "unknown-key" }, TrustFailureCodeV2.KEY_UNKNOWN);
        await AssertResignedHeaderFailure(h => h with { Algorithm = "ECDSA-P384-SHA384" }, TrustFailureCodeV2.ALGORITHM_UNSUPPORTED);
        await AssertResignedHeaderFailure(h => h with { ContractVersion = "rev869b-controller-v3" }, TrustFailureCodeV2.CONTRACT_UNSUPPORTED);
    }

    [Fact]
    public async Task RequestRoleCannotGrantAuthority()
    {
        var fixture = V2Fixture.Create(
            trustedRoles: new HashSet<string>(["MonitoringReader"], StringComparer.Ordinal));
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.VerifyAsync(fixture.Envelope, fixture.Subject, fixture.Resource));
        Assert.Equal(TrustFailureCodeV2.REQUEST_ROLE_FORBIDDEN, rejection.Code);
    }

    [Fact]
    public async Task AudienceSubjectAndScopeAreExact()
    {
        await AssertResignedHeaderFailure(h => h with { Audience = "other-audience" }, TrustFailureCodeV2.AUDIENCE_MISMATCH);
        await AssertResignedHeaderFailure(h => h with { Subject = "other-subject" }, TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        await AssertResignedHeaderFailure(h => h with { AuthorizedScope = "ORG:C2" }, TrustFailureCodeV2.SCOPE_MISMATCH);
    }

    [Fact]
    public async Task ClusterInstanceOperationAndVersionSubstitutionReject()
    {
        await AssertResignedHeaderFailure(h => h with { DatabaseClusterId = "cluster-2" }, TrustFailureCodeV2.CLUSTER_MISMATCH);
        await AssertResignedHeaderFailure(h => h with { DatabaseInstanceId = "instance-2" }, TrustFailureCodeV2.INSTANCE_MISMATCH);
        await AssertResignedHeaderFailure(h => h with { Operation = "AUTHORIZE_EXECUTE" }, TrustFailureCodeV2.OPERATION_MISMATCH);
        await AssertResignedHeaderFailure(h => h with { ResourceVersion = 2 }, TrustFailureCodeV2.RESOURCE_VERSION_STALE);
    }

    [Fact]
    public async Task TemporalWindowIsServerOwned()
    {
        await AssertResignedHeaderFailure(h => h with
        {
            IssuedAt = Now.AddMinutes(1),
            NotBefore = Now.AddMinutes(1),
            ExpiresAt = Now.AddMinutes(2)
        }, TrustFailureCodeV2.NOT_YET_VALID);
        await AssertResignedHeaderFailure(h => h with
        {
            IssuedAt = Now.AddMinutes(-2),
            NotBefore = Now.AddMinutes(-2),
            ExpiresAt = Now.AddMinutes(-1)
        }, TrustFailureCodeV2.ENVELOPE_EXPIRED);
        await AssertResignedHeaderFailure(h => h with { ExpiresAt = Now.AddMinutes(6) }, TrustFailureCodeV2.ENVELOPE_EXPIRED);
    }

    [Fact]
    public async Task NonceReplayIsIndependentOfIdempotency()
    {
        var fixture = V2Fixture.Create();
        await fixture.VerifyAsync(fixture.Envelope, fixture.Subject, fixture.Resource);
        Assert.Equal(IdempotencyReservationStateV2.COMPLETED, fixture.Idempotency.LastOutcome?.ReservationState);
        var replayHeader = fixture.Envelope.Header with { IdempotencyKey = "idempotency-2", RequestId = "request-2" };
        var replay = fixture.Sign(replayHeader, fixture.Envelope.Payload);
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.VerifyAsync(replay, fixture.Subject, fixture.Resource));
        Assert.Equal(TrustFailureCodeV2.NONCE_REPLAY, rejection.Code);
        Assert.Equal(1, fixture.Idempotency.ReservationCount);
    }

    [Fact]
    public async Task LeaseAcquireRenewExpireAndFenceAreMonotonic()
    {
        var store = new FakeLeaseStore(Now);
        var first = await store.AcquireAsync("resource-1", "worker-1", Now.AddMinutes(1));
        var second = await store.RenewAsync(first, Now.AddMinutes(2));
        Assert.True(second.FencingToken > first.FencingToken);
        Assert.False(await store.ConsumeFenceAsync(first));
        Assert.True(await store.ConsumeFenceAsync(second));
    }

    [Fact]
    public void EveryUnlistedStateOperationPairIsIllegal()
    {
        var machine = new Rev869BControllerStateMachine();
        foreach (var state in Enum.GetValues<ControllerLifecycleState>())
        {
            foreach (var operation in Enum.GetValues<ControllerOperationV2>())
            {
                if (machine.ListedOperations.Contains((state, operation)) || operation == ControllerOperationV2.QUARANTINE)
                {
                    continue;
                }

                var rejection = Assert.Throws<TrustFailureExceptionV2>(() =>
                    machine.RequireOperation(operation, state, state, "Operator", true, null));
                Assert.Equal(TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL, rejection.Code);
            }
        }
    }

    [Fact]
    public void EveryListedTransitionHasExactRoleEvidenceAndFence()
    {
        var machine = new Rev869BControllerStateMachine();
        foreach (var rule in machine.ListedOperationRules)
        {
            Assert.Equal(rule.Next,
                machine.RequireOperation(
                    rule.Operation,
                    rule.State,
                    rule.Next,
                    rule.Role,
                    true,
                    rule.RequiresLease ? V2Fixture.Lease() : null));
            var deniedRole = Assert.Throws<TrustFailureExceptionV2>(() =>
                machine.RequireOperation(
                    rule.Operation,
                    rule.State,
                    rule.Next,
                    "UnauthorizedRole",
                    true,
                    rule.RequiresLease ? V2Fixture.Lease() : null));
            Assert.Equal(TrustFailureCodeV2.SUBJECT_UNAUTHORIZED, deniedRole.Code);
        }

        var noEvidence = Assert.Throws<TrustFailureExceptionV2>(() =>
            machine.RequireOperation(ControllerOperationV2.PREPARE, ControllerLifecycleState.Preflight,
                ControllerLifecycleState.Provisioning, "ProvisioningExecutor", false, null));
        Assert.Equal(TrustFailureCodeV2.READER_MISSING, noEvidence.Code);
        var noLease = Assert.Throws<TrustFailureExceptionV2>(() =>
            machine.RequireOperation(ControllerOperationV2.PREPARE, ControllerLifecycleState.Preflight,
                ControllerLifecycleState.Provisioning, "ProvisioningExecutor", true, null));
        Assert.Equal(TrustFailureCodeV2.LEASE_REQUIRED, noLease.Code);
        var wrongRole = Assert.Throws<TrustFailureExceptionV2>(() =>
            machine.RequireOperation(ControllerOperationV2.PREPARE, ControllerLifecycleState.Preflight,
                ControllerLifecycleState.Provisioning, "Operator", true, V2Fixture.Lease()));
        Assert.Equal(TrustFailureCodeV2.SUBJECT_UNAUTHORIZED, wrongRole.Code);
        Assert.Equal(ControllerLifecycleState.Provisioning,
            machine.RequireOperation(ControllerOperationV2.PREPARE, ControllerLifecycleState.Preflight,
                ControllerLifecycleState.Provisioning, "ProvisioningExecutor", true, V2Fixture.Lease()));

        var active = new LifecycleResourceStateV2(
            "resource-1", 3, ControllerLifecycleState.Preflight, "audit-1",
            ControllerAuthorizationStatusV2.ACTIVE,
            ControllerOperationV2.AUTHORIZE_PREPARE.ToString(),
            "Operator",
            Now.AddMinutes(1));
        var cancelled = machine.CreateReplacement(
            active, ControllerOperationV2.CANCEL, active.State, "Operator", true, null,
            Now.AddMinutes(1), Now, "audit-cancel");
        Assert.Equal(ControllerAuthorizationStatusV2.CANCELLED, cancelled.AuthorizationStatus);
        var expired = machine.CreateReplacement(
            active with { AuthorizationExpiresAt = Now.AddMinutes(-1) },
            ControllerOperationV2.EXPIRE, active.State, "ControlPlaneRuntime", true, null,
            Now.AddMinutes(-1), Now, "audit-expire");
        Assert.Equal(ControllerAuthorizationStatusV2.EXPIRED, expired.AuthorizationStatus);

        var accepted = new LifecycleResourceStateV2(
            "resource-1", 10, ControllerLifecycleState.Accepted, "audit-accepted");
        var exportAuthorized = machine.CreateReplacement(
            accepted, ControllerOperationV2.AUTHORIZE_EXPORT, accepted.State, "ExportAuthorizer", true, null,
            Now.AddMinutes(5), Now, "audit-export-authorized");
        Assert.Equal(ExportLifecycleStateV2.AUTHORIZED, exportAuthorized.ExportState);
        var delivering = machine.CreateReplacement(
            exportAuthorized, ControllerOperationV2.EXPORT, accepted.State, "ExportExecutor", true,
            V2Fixture.Lease(), Now.AddMinutes(5), Now, "audit-export-delivering");
        Assert.Equal(ExportLifecycleStateV2.DELIVERING, delivering.ExportState);
        var delivered = machine.CreateReplacement(
            delivering, ControllerOperationV2.COMPLETE_EXPORT, accepted.State, "ExportExecutor", true,
            V2Fixture.Lease(), Now.AddMinutes(5), Now, "audit-export-delivered");
        Assert.Equal(ExportLifecycleStateV2.DELIVERED, delivered.ExportState);
    }

    [Fact]
    public void IdempotencyDecisionTableIsExact()
    {
        var binding = new IdempotencyBindingV2("issuer", "C1", "instance-1", "PREPARE", "request-1", "key-1", new string('a', 64));
        var completed = new IdempotencyOutcomeV2(IdempotencyReservationStateV2.COMPLETED, 1, false, null, "response", "audit", Now);
        Assert.Same(completed, IdempotencyDecisionV2.RequireReusable(binding, binding, completed));
        var collision = Assert.Throws<TrustFailureExceptionV2>(() =>
            IdempotencyDecisionV2.RequireReusable(binding with { CanonicalRequestDigest = new string('b', 64) }, binding, completed));
        Assert.Equal(TrustFailureCodeV2.IDEMPOTENCY_PAYLOAD_MISMATCH, collision.Code);
        var nonretryable = Assert.Throws<TrustFailureExceptionV2>(() =>
            IdempotencyDecisionV2.RequireReusable(binding, binding,
                completed with { ReservationState = IdempotencyReservationStateV2.NONRETRYABLE_FAILURE }));
        Assert.Equal(TrustFailureCodeV2.IDEMPOTENCY_NONRETRYABLE, nonretryable.Code);
    }

    [Fact]
    public async Task ConcurrentDuplicateHasOneAuthoritativeWinner()
    {
        var store = new FakeIdempotencyStore();
        var binding = new IdempotencyBindingV2("issuer", "C1", "instance-1", "PREPARE", "request-1", "key-1", new string('a', 64));
        var winner = await store.ReserveAsync(binding);
        var duplicate = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await store.ReserveAsync(binding));
        Assert.Equal(1, store.AuthoritativeRows);
        Assert.Equal(1, winner.AttemptNumber);
        Assert.Equal(TrustFailureCodeV2.IDEMPOTENCY_IN_PROGRESS, duplicate.Code);
    }

    [Fact]
    public async Task OracleManifestAndReadersAreServerPinned()
    {
        var fixture = EvidenceFixture.Create();
        var wrongOracle = fixture.WithEvidence(
            fixture.Request.Evidence with { OracleVersion = "9.9.9" });
        await AssertEvidenceFailure(wrongOracle, TrustFailureCodeV2.ORACLE_MISMATCH);

        var missingReader = fixture.WithEvidence(
            EvidenceFixture.Rehash(fixture.Request.Evidence with { ReaderReceipts = [] }));
        await AssertEvidenceFailure(missingReader, TrustFailureCodeV2.READER_MISSING);

        var substituted = fixture.Request.Evidence.ReaderReceipts[0] with { ReaderArtifactSha256 = new string('d', 64) };
        var wrongReader = fixture.WithEvidence(EvidenceFixture.Rehash(
            fixture.Request.Evidence with { ReaderReceipts = [substituted] }));
        await AssertEvidenceFailure(wrongReader, TrustFailureCodeV2.READER_UNAUTHORIZED);
    }

    [Fact]
    public void CallerVerdictAndExpectedValuesAreUnmapped()
    {
        foreach (var field in new[] { "pass", "fail", "verdict", "disposition", "expected", "formula" })
        {
            var json = Encoding.UTF8.GetBytes($"{{\"{field}\":true}}");
            var rejection = Assert.Throws<TrustFailureExceptionV2>(() => StrictEvidenceJsonV2.Deserialize(json));
            Assert.Equal(TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD, rejection.Code);
        }
    }

    [Fact]
    public async Task TemporalEvidenceBindingIsExact()
    {
        var fixture = EvidenceFixture.Create(authoritativeMutation: observations =>
        {
            var future = observations[2] with
            {
                Provenance = observations[2].Provenance with { ObservedAtUtc = Now.AddMinutes(1) }
            };
            return [.. observations.Take(2), future];
        });
        await AssertEvidenceFailure(fixture, TrustFailureCodeV2.READER_UNAUTHORIZED);

        var wrongLease = fixture.WithEvidence(EvidenceFixture.Rehash(
            fixture.Request.Evidence with
            {
                Lease = fixture.Request.Evidence.Lease with { FencingToken = 2 }
            }));
        await AssertEvidenceFailure(wrongLease, TrustFailureCodeV2.LEASE_FENCE_STALE);
    }

    [Fact]
    public async Task AllEvidenceDimensionsAreServerBounded()
    {
        var oversized = EvidenceFixture.Create(authoritativeMutation: observations =>
        {
            var oversizedFact = observations[0] with
            {
                Facts = new Dictionary<string, TypedSelectorValueV1>
                {
                    ["status"] = new(SelectorValueKind.String, new string('x', 33))
                }
            };
            return [oversizedFact, .. observations.Skip(1)];
        });
        await AssertEvidenceFailure(oversized, TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
    }

    [Fact]
    public async Task CallerFactsCannotReplaceAuthoritativeReaderFacts()
    {
        var fixture = EvidenceFixture.Create();
        var callerClaim = fixture.Request.Evidence.RawFacts[0] with
        {
            Facts = new Dictionary<string, TypedSelectorValueV1>
            {
                ["status"] = new(SelectorValueKind.String, "caller-controlled")
            }
        };
        var changed = fixture.WithEvidence(EvidenceFixture.Rehash(
            fixture.Request.Evidence with { RawFacts = [callerClaim, .. fixture.Request.Evidence.RawFacts.Skip(1)] }));
        var result = await changed.Verifier.VerifyAsync(changed.Request);
        Assert.Equal(VerificationDisposition.Passed, result.Disposition);
        Assert.NotEqual(changed.Request.Evidence.PayloadSha256, changed.Audit.Event!.EvidenceEnvelopeSha256);
    }

    [Fact]
    public async Task SensitiveFactsNeverSerializeOrLog()
    {
        const string sentinel = "secret-value-must-not-leak";
        var fixture = EvidenceFixture.Create(authoritativeMutation: observations =>
        {
            var sensitive = observations[0] with
            {
                Facts = new Dictionary<string, TypedSelectorValueV1>
                {
                    ["password"] = new(SelectorValueKind.String, sentinel)
                }
            };
            return [sensitive, .. observations.Skip(1)];
        });
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.Verifier.VerifyAsync(fixture.Request));
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_SENSITIVE_FIELD, rejection.Code);
        Assert.DoesNotContain(sentinel, rejection.Message, StringComparison.Ordinal);
        Assert.Null(fixture.Audit.Event);
    }

    [Fact]
    public async Task MissingVerifierDependencyReturnsNotReady()
    {
        var fixture = EvidenceFixture.Create(ready: false);
        var readiness = await fixture.Readiness.CheckAsync();
        Assert.Equal(ReadinessStateV2.NOT_READY, readiness.State);
        Assert.Contains("ORACLE_NOT_PINNED", readiness.DependencyCodes);
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.Verifier.VerifyAsync(fixture.Request));
        Assert.Equal(TrustFailureCodeV2.SERVICE_NOT_READY, rejection.Code);
    }

    [Fact]
    public async Task RuntimeIdentityCannotEscalateAcrossRoles()
    {
        foreach (var role in new[]
        {
            "ErpRuntime", "ControlPlaneRuntime", "CommandSigner", "RegistryWriter", "ProvisioningExecutor",
            "MigrationExecutor", "AcceptanceVerifier", "AuditWriter", "RecoveryApprover", "RecoveryExecutor",
            "DropAuthorizer", "DropExecutor", "PurgeAuthorizer", "PurgeExecutor", "ExportAuthorizer",
            "ExportExecutor", "MonitoringReader"
        })
        {
            var fixture = V2Fixture.Create(
                new HashSet<string>([role], StringComparer.Ordinal));
            var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
                await fixture.VerifyAsync(fixture.Envelope, fixture.Subject, fixture.Resource));
            Assert.Equal(TrustFailureCodeV2.REQUEST_ROLE_FORBIDDEN, rejection.Code);
            Assert.Equal(0, fixture.Audit.AcceptedCount);
        }
    }

    [Fact]
    public async Task AuditAppendFailurePreventsVerdictCommit()
    {
        var fixture = EvidenceFixture.Create(auditSucceeds: false);
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.Verifier.VerifyAsync(fixture.Request));
        Assert.Equal(TrustFailureCodeV2.AUDIT_APPEND_FAILED, rejection.Code);
        Assert.Equal(1, fixture.Audit.AppendAttempts);
    }

    [Fact]
    public void MalformedCanonicalInputHasTypedFailure()
    {
        var fixture = V2Fixture.Create();
        var validBytes = CanonicalSignedHeaderCodecV2.Serialize(fixture.Envelope.Header);
        var validText = Encoding.UTF8.GetString(validBytes);
        var malformedHash = fixture.Envelope.Header with { CanonicalPayloadSha256 = new string('A', 64) };
        var invalidHash = Assert.Throws<TrustFailureExceptionV2>(() =>
            CanonicalSignedHeaderCodecV2.Serialize(malformedHash));
        Assert.Equal(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, invalidHash.Code);

        var malformedNonce = fixture.Envelope.Header with { Nonce = "not-base64url" };
        var invalidNonce = Assert.Throws<TrustFailureExceptionV2>(() =>
            CanonicalSignedHeaderCodecV2.Serialize(malformedNonce));
        Assert.Equal(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, invalidNonce.Code);

        foreach (var malformed in new[]
        {
            new byte[] { 0xff, 0xfe },
            Encoding.UTF8.GetBytes(validText.Replace("\n", "\r\n", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(validText + "contract_version=21:rev869b-controller-v2\n"),
            Encoding.UTF8.GetBytes(validText.Replace(
                "contract_version=21:rev869b-controller-v2",
                "contract_version=20:rev869b-controller-v2",
                StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(validText.Replace(
                "resource_version=1:1",
                "resource_version=2:01",
                StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(validText.Replace(
                "issued_at=28:2026-08-15T12:00:00.0000000Z",
                "issued_at=28:2026-08-15T99:00:00.0000000Z",
                StringComparison.Ordinal))
        })
        {
            var rawFailure = Assert.Throws<TrustFailureExceptionV2>(() =>
                CanonicalSignedHeaderCodecV2.Parse(malformed));
            Assert.Equal(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, rawFailure.Code);
        }

        var unmapped = Assert.Throws<TrustFailureExceptionV2>(() =>
            StrictEvidenceJsonV2.Deserialize(Encoding.UTF8.GetBytes("{\"unknown\":1}")));
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD, unmapped.Code);
    }

    [Fact]
    public async Task TenMillionMasterContractUsesPagingOnly()
    {
        var reader = new FakePagedMasterReader(10_000_000, 1_000);
        var pages = new List<PageResultV1<int>>();
        await foreach (var page in reader.ReadAsync(3))
        {
            pages.Add(page);
        }

        Assert.Equal(3, pages.Count);
        Assert.All(pages, page => Assert.InRange(page.Items.Count, 1, 1_000));
        Assert.Equal(3, reader.ReadCount);
        Assert.True(pages[^1].HasMore);
    }

    [Fact]
    public void PhaseACompatibilityManifestIsExactAndClosed()
    {
        Assert.True(Rev869BPhaseACompatibilityManifest.IsCompatible(
            "rev869b-phase-a-v1",
            "rev869b-command-envelope-v3",
            "rev869b-authoritative-evidence-v3",
            "REV869B-READINESS-v1"));
        Assert.Equal("rev869b-control-transaction-v1",
            Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion);
        Assert.Equal("rev869b-production-ownership-v1",
            Rev869BPhaseACompatibilityManifest.OwnershipContractVersion);
        Assert.False(Rev869BPhaseACompatibilityManifest.IsCompatible(
            "rev869b-phase-a-v2",
            Rev869BPhaseACompatibilityManifest.CanonicalEnvelopeVersion,
            Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion));
    }

    [Fact]
    public void EveryPhaseAProductionResponsibilityHasOneDistinctOwnerContract()
    {
        var responsibilities = Enum.GetValues<ProductionResponsibilityV3>();
        Assert.Equal(14, responsibilities.Length);
        Assert.Equal(responsibilities.Length, PhaseAOwnershipCatalog.All.Count);
        Assert.Equal(responsibilities.Length, PhaseAOwnershipCatalog.All.Values.Distinct().Count());
        Assert.All(responsibilities, responsibility =>
        {
            var owner = Assert.Contains(responsibility, PhaseAOwnershipCatalog.All);
            Assert.True(owner.IsInterface);
        });
        PhaseAOwnershipValidator.RequireComplete();
    }

    [Fact]
    public void UntrustedIntentCannotCarryRoleScopeOrPermissionAuthority()
    {
        var propertyNames = typeof(UntrustedBusinessIntentV3)
            .GetProperties()
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Role", propertyNames);
        Assert.DoesNotContain("Roles", propertyNames);
        Assert.DoesNotContain("Permission", propertyNames);
        Assert.DoesNotContain("Permissions", propertyNames);
        Assert.DoesNotContain("AuthorizedScope", propertyNames);
        Assert.True(typeof(ITrustedSubjectRoleScopeResolver).IsInterface);
        Assert.NotEqual(typeof(INexaErpBusinessRuntime), typeof(IControlPlaneAuthority));
        Assert.NotEqual(typeof(IControlPlaneAuthority), typeof(IAcceptanceVerifierAuthority));
    }

    [Fact]
    public void ProtectedCommandSurfaceAcceptsRawCanonicalBytesAndDelegatesOnlyToController()
    {
        var publicMethods = typeof(SignedCommandServiceV2)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(publicMethods, method => method.Name == "Sign");
        var verify = Assert.Single(publicMethods, method => method.Name == "VerifyAsync");
        var parameters = verify.GetParameters().Select(static parameter => parameter.ParameterType).ToArray();
        Assert.Equal(typeof(ReadOnlyMemory<byte>), parameters[0]);
        Assert.Equal(typeof(ReadOnlyMemory<byte>), parameters[1]);
        Assert.Equal(typeof(byte[]), parameters[2]);
        Assert.DoesNotContain(typeof(SignedCommandEnvelopeV2), parameters);

        var constructorParameters = Assert.Single(typeof(SignedCommandServiceV2).GetConstructors())
            .GetParameters().Select(static parameter => parameter.ParameterType).ToArray();
        Assert.Contains(typeof(ILifecycleControllerAuthority), constructorParameters);
        Assert.DoesNotContain(typeof(INonceReplayStore), constructorParameters);
        Assert.DoesNotContain(typeof(IIdempotencyStore), constructorParameters);
        Assert.DoesNotContain(typeof(ILeaseFenceStore), constructorParameters);
        Assert.DoesNotContain(typeof(ILifecycleStateStore), constructorParameters);
        Assert.DoesNotContain(typeof(ITrustAuditSinkV2), constructorParameters);
    }

    [Fact]
    public async Task MissingPhaseADependenciesAreEnumeratedAndFailClosed()
    {
        var authority = new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            [],
            new FixedTimeProvider(Now));
        var snapshot = await authority.CheckAsync();
        Assert.False(snapshot.CanExecuteProtectedOperation);
        Assert.Equal(Enum.GetValues<PhaseADependencyV3>().Length, snapshot.Dependencies.Count);
        Assert.All(snapshot.Dependencies, item =>
        {
            Assert.Equal(ReadinessDependencyStateV3.NOT_CONFIGURED, item.State);
            Assert.Equal("DEPENDENCY_NOT_CONFIGURED", item.DiagnosticCode);
        });
        Assert.Equal(ReadinessDependencyStateV3.NOT_CONFIGURED,
            Assert.Single(snapshot.Dependencies, item => item.Dependency == PhaseADependencyV3.DurableControlPlane).State);
        Assert.Equal(ReadinessDependencyStateV3.NOT_CONFIGURED,
            Assert.Single(snapshot.Dependencies, item => item.Dependency == PhaseADependencyV3.KmsHsm).State);
        Assert.Equal(ReadinessDependencyStateV3.NOT_CONFIGURED,
            Assert.Single(snapshot.Dependencies, item => item.Dependency == PhaseADependencyV3.OracleRegistry).State);
        Assert.Equal(ReadinessDependencyStateV3.NOT_CONFIGURED,
            Assert.Single(snapshot.Dependencies, item => item.Dependency == PhaseADependencyV3.ImmutableAuditEvidence).State);
    }

    [Fact]
    public async Task OnlyOneReadyProviderPerDependencyCanEnableProtectedOperations()
    {
        var providers = Enum.GetValues<PhaseADependencyV3>()
            .Select(dependency => (IReadinessDependencyProvider)new FakeDependencyProvider(
                dependency,
                ReadinessDependencyStateV3.READY))
            .ToArray();
        var ready = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            providers,
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.True(ready.CanExecuteProtectedOperation);

        var duplicate = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            [.. providers, new FakeDependencyProvider(PhaseADependencyV3.KmsHsm, ReadinessDependencyStateV3.READY)],
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.False(duplicate.CanExecuteProtectedOperation);
        Assert.Equal(ReadinessDependencyStateV3.POLICY_MISMATCH,
            Assert.Single(duplicate.Dependencies,
                item => item.Dependency == PhaseADependencyV3.KmsHsm).State);

        var falseReady = providers
            .Where(provider => provider.Dependency != PhaseADependencyV3.TrustedClock)
            .Append(new FixedDependencyProvider(new(
                PhaseADependencyV3.TrustedClock,
                ReadinessDependencyStateV3.READY,
                "clock-v2",
                "clock-v1",
                "READY")))
            .ToArray();
        var versionMismatch = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            falseReady,
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.False(versionMismatch.CanExecuteProtectedOperation);
        Assert.Equal(ReadinessDependencyStateV3.VERSION_MISMATCH,
            Assert.Single(versionMismatch.Dependencies,
                item => item.Dependency == PhaseADependencyV3.TrustedClock).State);
    }

    [Fact]
    public void EveryReadinessStateHasAnExactTypedFailureCode()
    {
        var expected = new Dictionary<ReadinessDependencyStateV3, TrustFailureCodeV2>
        {
            [ReadinessDependencyStateV3.READY] = TrustFailureCodeV2.NONE,
            [ReadinessDependencyStateV3.NOT_CONFIGURED] = TrustFailureCodeV2.DEPENDENCY_NOT_CONFIGURED,
            [ReadinessDependencyStateV3.UNAVAILABLE] = TrustFailureCodeV2.DEPENDENCY_UNAVAILABLE,
            [ReadinessDependencyStateV3.VERSION_MISMATCH] = TrustFailureCodeV2.DEPENDENCY_VERSION_MISMATCH,
            [ReadinessDependencyStateV3.IDENTITY_MISMATCH] = TrustFailureCodeV2.DEPENDENCY_IDENTITY_MISMATCH,
            [ReadinessDependencyStateV3.POLICY_MISMATCH] = TrustFailureCodeV2.DEPENDENCY_POLICY_MISMATCH,
            [ReadinessDependencyStateV3.DEGRADED_NOT_SAFE] = TrustFailureCodeV2.DEPENDENCY_DEGRADED_UNSAFE
        };
        Assert.Equal(Enum.GetValues<ReadinessDependencyStateV3>().Length, expected.Count);
        Assert.All(expected, pair => Assert.Equal(pair.Value, PhaseAContractValidator.FailureFor(pair.Key)));
        Assert.Equal(Enum.GetValues<TrustFailureCodeV2>().Length,
            Enum.GetValues<TrustFailureCodeV2>().Distinct().Count());
    }

    [Fact]
    public void PhaseAEvidenceAndAuditSurfacesContainNoCallerVerdictOrSecretMaterial()
    {
        var evidenceProperties = typeof(CanonicalEvidenceEnvelopeV3)
            .GetProperties().Select(static property => property.Name).ToArray();
        Assert.DoesNotContain(evidenceProperties,
            name => name.Contains("Disposition", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Verdict", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Expected", StringComparison.OrdinalIgnoreCase));

        foreach (var type in new[]
                 {
                     typeof(CanonicalEvidenceEnvelopeV3),
                     typeof(ImmutableAuditEventV3),
                     typeof(ReadinessSnapshotV3),
                     typeof(DeploymentIdentityDescriptorV3)
                 })
        {
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Token", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void PhaseAContractBoundsAreFiniteAndEnforced()
    {
        Assert.Equal(98_304, PhaseAContractLimits.MaximumCommandEnvelopeBytes);
        Assert.Equal(4_194_304, PhaseAContractLimits.MaximumEvidenceEnvelopeBytes);
        Assert.Equal(512, PhaseAContractLimits.MaximumObservations);
        Assert.Equal(1_000, PhaseAContractLimits.MaximumPageSize);
        Assert.Equal(3, PhaseAContractLimits.MaximumTransientRetries);

        var intent = new UntrustedBusinessIntentV3(
            "request-1",
            "idempotency-1",
            "AUTHORIZE_PREPARE",
            new("C1", "cluster-1", "instance-1", MasterScopeKindV3.COMPANY_LEDGER),
            "TARGET",
            "resource-1",
            1,
            Now,
            Now.AddMinutes(1),
            new Dictionary<string, string> { ["oversized"] = new string('x', PhaseAContractLimits.MaximumStringBytes + 1) });
        var rejection = Assert.Throws<TrustFailureExceptionV2>(() => PhaseAContractValidator.RequireValid(intent));
        Assert.Equal(TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED, rejection.Code);
    }

    private static async Task AssertEvidenceFailure(EvidenceFixture fixture, TrustFailureCodeV2 expected)
    {
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.Verifier.VerifyAsync(fixture.Request));
        Assert.Equal(expected, rejection.Code);
    }

    private static async Task AssertResignedHeaderFailure(
        Func<CanonicalSignedHeaderV2, CanonicalSignedHeaderV2> mutate,
        TrustFailureCodeV2 expected)
    {
        var fixture = V2Fixture.Create();
        var changedHeader = mutate(fixture.Envelope.Header);
        var resigned = expected == TrustFailureCodeV2.KEY_UNKNOWN
            ? fixture.Envelope with { Header = changedHeader }
            : fixture.Sign(changedHeader, fixture.Envelope.Payload);
        var rejection = await Assert.ThrowsAsync<TrustFailureExceptionV2>(async () =>
            await fixture.VerifyAsync(resigned, fixture.Subject, fixture.Resource));
        Assert.Equal(expected, rejection.Code);
    }

    private static ControlPlaneOptions Options(string database, string[] patterns) => new()
    {
        ServiceIdentity = "sess-control-plane",
        IssuerId = "controller-issuer",
        Audience = "control-plane",
        ControlPlaneDatabaseIdentity = database,
        CommandSigningKeyId = "command-key",
        EvidenceVerificationKeyId = "evidence-key",
        ContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
        EvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
        CanonicalizationVersion = Rev869BCompatibilityManifestV2.CanonicalizationVersion,
        OwnershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
        ReadinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
        DurableProviderContractVersion = Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion,
        CanonicalEnvelopeVersion = Rev869BPhaseACompatibilityManifest.CanonicalEnvelopeVersion,
        RetentionPolicyReference = "retention-1",
        MaximumEvidenceObservations = 100,
        MaximumFactsPerObservation = 100,
        MaximumReplayWindowSeconds = 300,
        MaximumLeaseSeconds = 300,
        MaximumClockSkewSeconds = 30,
        MaximumEnvelopeBytes = 98_304,
        MaximumSelectors = 128,
        MaximumStringBytes = 4_096,
        MaximumCumulativeFacts = 10_000,
        ControlPlaneEndpoint = "https://control-plane.invalid",
        AcceptanceVerifierEndpoint = "https://acceptance-verifier.invalid",
        AllowedTargetEnvironments = ["test"],
        AllowedDatabaseIdentityPatterns = patterns,
        AllowedIssuerIds = ["controller-issuer"],
        AllowedAudiences = ["control-plane"],
        AllowedRoles = ["Operator"],
        AllowedScopes = ["ORG:C1"],
        AllowedOperations = ["AUTHORIZE_PREPARE"]
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

    private sealed class V2Fixture
    {
        public const string Nonce = "AAECAwQFBgcICQoLDA0ODw";
        public const string SecondNonce = "AQIDBAUGBwgJCgsMDQ4PEA";

        private V2Fixture(
            SignedCommandServiceV2 service,
            SignedCommandEnvelopeV2 envelope,
            AuthenticatedSubjectV2 subject,
            ResourceBindingV2 resource,
            FakeNonceStore nonces,
            FakeIdempotencyStore idempotency,
            FakeTrustAuditSink audit,
            FakeCrypto crypto)
        {
            Service = service;
            Envelope = envelope;
            Subject = subject;
            Resource = resource;
            Nonces = nonces;
            Idempotency = idempotency;
            Audit = audit;
            Crypto = crypto;
        }

        public SignedCommandServiceV2 Service { get; }
        public SignedCommandEnvelopeV2 Envelope { get; }
        public AuthenticatedSubjectV2 Subject { get; }
        public ResourceBindingV2 Resource { get; }
        public FakeNonceStore Nonces { get; }
        public FakeIdempotencyStore Idempotency { get; }
        public FakeTrustAuditSink Audit { get; }
        private FakeCrypto Crypto { get; }

        public static V2Fixture Create(IReadOnlySet<string>? trustedRoles = null)
        {
            trustedRoles ??= new HashSet<string>(["Operator"], StringComparer.Ordinal);
            var crypto = new FakeCrypto("command-key");
            var key = new SigningKeyDescriptor(
                crypto.KeyId,
                Rev869BCompatibilityManifestV2.SignatureAlgorithm,
                SigningKeyState.Active,
                Now.AddDays(-1),
                Now.AddDays(1),
                null);
            var issuer = new TrustedIssuerDescriptorV2(
                "controller-issuer",
                new HashSet<string>(["control-plane"], StringComparer.Ordinal),
                new HashSet<string>([Rev869BCompatibilityManifestV2.ContractVersion], StringComparer.Ordinal),
                new HashSet<string>([Rev869BCompatibilityManifestV2.SignatureAlgorithm], StringComparer.Ordinal),
                new Dictionary<string, SigningKeyDescriptor>(StringComparer.Ordinal) { [key.KeyId] = key },
                new HashSet<string>(["operator-1"], StringComparer.Ordinal),
                new HashSet<string>(trustedRoles, StringComparer.Ordinal),
                new HashSet<string>(["ORG:C1"], StringComparer.Ordinal),
                Enum.GetNames<ControllerOperationV2>().ToHashSet(StringComparer.Ordinal),
                Now.AddDays(-1),
                null);
            var subject = new AuthenticatedSubjectV2(
                issuer.IssuerId,
                "operator-1",
                "workload-1",
                "control-plane",
                new HashSet<string>(trustedRoles, StringComparer.Ordinal),
                new HashSet<string>(["ORG:C1"], StringComparer.Ordinal));
            var resource = new ResourceBindingV2(
                "C1", "cluster-1", "instance-1", "TARGET", "resource-1", 1, "AUTHORIZE_PREPARE");
            var nonces = new FakeNonceStore();
            var idempotency = new FakeIdempotencyStore();
            var audit = new FakeTrustAuditSink();
            var controller = new FakeLifecycleController(nonces, idempotency, audit);
            var service = new SignedCommandServiceV2(
                crypto,
                new FakeIssuerRegistry(issuer),
                new FakeAuthorizationResolver(subject),
                controller,
                new FixedTimeProvider(Now),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(30));
            var payload = new CanonicalCommandPayloadV2(
                ControllerOperationV2.AUTHORIZE_PREPARE,
                ControllerLifecycleState.Registered,
                ControllerLifecycleState.Preflight,
                new ScenarioIdentityV1("scenario-1"),
                new SubcaseIdentityV1("subcase-1"),
                "action-1",
                new Dictionary<string, string>(StringComparer.Ordinal) { ["manifest"] = "sha256:abc" },
                ["target-registration"]);
            var emptyHeader = new CanonicalSignedHeaderV2(
                Rev869BCompatibilityManifestV2.ContractVersion,
                Rev869BCompatibilityManifestV2.CanonicalizationVersion,
                Rev869BCompatibilityManifestV2.SignatureAlgorithm,
                crypto.KeyId,
                issuer.IssuerId,
                "control-plane",
                subject.SubjectId,
                "Operator",
                "ORG:C1",
                resource.OrganizationId,
                resource.DatabaseClusterId,
                resource.DatabaseInstanceId,
                resource.Operation,
                resource.ResourceId,
                resource.ExpectedResourceVersion,
                "lease-none",
                1,
                "request-1",
                "idempotency-1",
                Nonce,
                Now,
                Now,
                Now.AddMinutes(2),
                new string('0', 64),
                0);
            var fixture = new V2Fixture(
                service,
                new SignedCommandEnvelopeV2(emptyHeader, payload, []),
                subject,
                resource,
                nonces,
                idempotency,
                audit,
                crypto);
            return fixture.WithEnvelope(fixture.Sign(emptyHeader, payload));
        }

        private V2Fixture WithEnvelope(SignedCommandEnvelopeV2 envelope) =>
            new(Service, envelope, Subject, Resource, Nonces, Idempotency, Audit, Crypto);

        public SignedCommandEnvelopeV2 Sign(
            CanonicalSignedHeaderV2 header,
            CanonicalCommandPayloadV2 payload)
        {
            var payloadBytes = CanonicalJsonV1.Serialize(payload);
            var completed = header with
            {
                CanonicalPayloadSha256 = Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant(),
                CanonicalPayloadLength = payloadBytes.Length
            };
            return new(completed, payload, Crypto.Sign(CanonicalSignedHeaderCodecV2.Serialize(completed)));
        }

        public ValueTask<VerifiedCommandV2> VerifyAsync(
            SignedCommandEnvelopeV2 envelope,
            AuthenticatedSubjectV2 subject,
            ResourceBindingV2 resource) =>
            Service.VerifyAsync(
                CanonicalSignedHeaderCodecV2.Serialize(envelope.Header),
                CanonicalJsonV1.Serialize(envelope.Payload),
                envelope.Signature,
                subject,
                resource);

        public static LeaseFenceV2 Lease() =>
            new("lease-1", "resource-1", 1, Now.AddMinutes(-1), Now, Now.AddMinutes(1), "worker-1");
    }

    private sealed class FakeIssuerRegistry(TrustedIssuerDescriptorV2 issuer) : ITrustedIssuerRegistry
    {
        public TrustedIssuerDescriptorV2? Resolve(string issuerId, string keyId) =>
            issuerId == issuer.IssuerId ? issuer : null;
    }

    private sealed class FakeAuthorizationResolver(AuthenticatedSubjectV2 resolved) : IAuthorizationResolver
    {
        public AuthenticatedSubjectV2? Resolve(AuthenticatedSubjectV2 authenticatedSubject, ResourceBindingV2 resource) =>
            authenticatedSubject.Issuer == resolved.Issuer &&
            authenticatedSubject.SubjectId == resolved.SubjectId
                ? resolved
                : null;
    }

    private sealed class FakeNonceStore : INonceReplayStore
    {
        private readonly HashSet<string> _nonces = new(StringComparer.Ordinal);
        public int ReservationCount => _nonces.Count;

        public ValueTask<bool> TryReserveAsync(
            string issuer,
            string nonce,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_nonces.Add($"{issuer}:{nonce}"));
    }

    private sealed class FakeLeaseStore(DateTimeOffset now) : ILeaseFenceStore
    {
        private LeaseFenceV2? _current;
        private long _fence;

        public ValueTask<LeaseFenceV2> AcquireAsync(
            string resourceId,
            string holderSubject,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            _current = new(
                $"lease-{++_fence}", resourceId, _fence, now, now, expiresAt, holderSubject);
            return ValueTask.FromResult(_current);
        }

        public ValueTask<LeaseFenceV2> RenewAsync(
            LeaseFenceV2 current,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            if (_current != current)
            {
                throw new TrustFailureExceptionV2(TrustFailureCodeV2.LEASE_FENCE_STALE, "Stale lease.");
            }

            _current = current with { FencingToken = ++_fence, RenewedAt = now, ExpiresAt = expiresAt };
            return ValueTask.FromResult(_current);
        }

        public ValueTask<LeaseFenceV2?> ReadCurrentAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_current?.ResourceId == resourceId ? _current : null);

        public ValueTask<bool> ConsumeFenceAsync(
            LeaseFenceV2 expected,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_current == expected && expected.ExpiresAt >= now);
    }

    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, (IdempotencyBindingV2 Binding, IdempotencyOutcomeV2 Outcome)> _rows =
            new(StringComparer.Ordinal);

        public int ReservationCount { get; private set; }
        public int AuthoritativeRows => _rows.Count;
        public IdempotencyOutcomeV2? LastOutcome => _rows.Values.LastOrDefault().Outcome;

        public ValueTask<IdempotencyOutcomeV2> ReserveAsync(
            IdempotencyBindingV2 binding,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var key = Key(binding);
                if (_rows.TryGetValue(key, out var existing))
                {
                    return ValueTask.FromResult(
                        IdempotencyDecisionV2.RequireReusable(binding, existing.Binding, existing.Outcome));
                }

                var outcome = new IdempotencyOutcomeV2(
                    IdempotencyReservationStateV2.RESERVED, 1, true, null, null, null, null);
                _rows.Add(key, (binding, outcome));
                ReservationCount++;
                return ValueTask.FromResult(outcome);
            }
        }

        public ValueTask<IdempotencyOutcomeV2?> ReadAsync(
            IdempotencyBindingV2 binding,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var row = _rows.Values.SingleOrDefault(item => item.Binding == binding);
                return ValueTask.FromResult<IdempotencyOutcomeV2?>(row.Outcome);
            }
        }

        public ValueTask CompleteAsync(
            IdempotencyBindingV2 binding,
            string responseDigest,
            string auditReference,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var key = Key(binding);
                var existing = _rows[key];
                _rows[key] = (existing.Binding, existing.Outcome with
                {
                    ReservationState = IdempotencyReservationStateV2.COMPLETED,
                    Retryable = false,
                    ResponseDigest = responseDigest,
                    AuditReference = auditReference,
                    CompletedAt = completedAt
                });
                return ValueTask.CompletedTask;
            }
        }

        public ValueTask RecordFailureAsync(
            IdempotencyBindingV2 binding,
            TrustFailureCodeV2 code,
            bool retryable,
            string auditReference,
            DateTimeOffset failedAt,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        private static string Key(IdempotencyBindingV2 binding) =>
            $"{binding.Issuer}:{binding.OrganizationId}:{binding.DatabaseInstanceId}:{binding.Operation}:{binding.IdempotencyKey}";
    }

    private sealed class FakeLifecycleController(
        FakeNonceStore nonces,
        FakeIdempotencyStore idempotency,
        FakeTrustAuditSink audit) : ILifecycleControllerAuthority
    {
        private readonly Rev869BControllerStateMachine _stateMachine = new();

        public async ValueTask<LifecycleTransitionResultV3> TransitionAsync(
            VerifiedLifecycleCommandV3 command,
            CancellationToken cancellationToken = default)
        {
            var rule = _stateMachine.RequirePhaseACommand(command, Now);
            if (!await nonces.TryReserveAsync(
                    command.Nonce.IssuerId,
                    command.Nonce.Nonce,
                    command.Nonce.ExpiresAt,
                    cancellationToken))
            {
                throw new TrustFailureExceptionV2(TrustFailureCodeV2.NONCE_REPLAY, "Nonce already registered.");
            }

            var identity = command.Idempotency;
            var binding = new IdempotencyBindingV2(
                identity.IssuerId,
                identity.OrganizationId,
                identity.DatabaseInstanceId,
                identity.Operation,
                identity.RequestId,
                identity.IdempotencyKey,
                identity.CanonicalRequestSha256);
            var reserved = await idempotency.ReserveAsync(binding, cancellationToken);
            audit.RecordAccepted();
            await idempotency.CompleteAsync(
                binding,
                identity.CanonicalRequestSha256,
                command.AuditCorrelationId,
                Now,
                cancellationToken);
            return new(
                ControlTransactionOutcomeV3.COMMITTED,
                rule.NextState,
                command.CurrentVersion + 1,
                reserved.AttemptNumber,
                identity.CanonicalRequestSha256,
                command.AuditCorrelationId,
                TrustFailureCodeV2.NONE);
        }
    }

    private sealed class FakeLifecycleStore(LifecycleResourceStateV2 initial) : ILifecycleStateStore
    {
        private LifecycleResourceStateV2 _current = initial;

        public ValueTask<LifecycleResourceStateV2> ReadAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_current);

        public ValueTask<bool> CompareExchangeAsync(
            LifecycleResourceStateV2 expected,
            LifecycleResourceStateV2 replacement,
            CancellationToken cancellationToken = default)
        {
            if (_current != expected)
            {
                return ValueTask.FromResult(false);
            }

            _current = replacement;
            return ValueTask.FromResult(true);
        }
    }

    private sealed class FakeTrustAuditSink : ITrustAuditSinkV2
    {
        public int AcceptedCount { get; private set; }

        public void RecordAccepted() => AcceptedCount++;

        public ValueTask<DurableAuditAppendReceiptV2?> AppendAcceptedAttemptAsync(
            CanonicalSignedHeaderV2 header,
            CancellationToken cancellationToken = default)
        {
            RecordAccepted();
            return ValueTask.FromResult<DurableAuditAppendReceiptV2?>(
                new("event-1", "audit-1", new string('a', 64), Now));
        }
    }

    private sealed class EvidenceFixture
    {
        private EvidenceFixture(
            AcceptanceVerifierOptions options,
            FakeReadinessProbe readiness,
            FakeVerificationAuditSinkV2 audit,
            ClosedEvidenceVerifierV2 verifier,
            EvidenceVerificationRequestV2 request)
        {
            Options = options;
            Readiness = readiness;
            Audit = audit;
            Verifier = verifier;
            Request = request;
        }

        public AcceptanceVerifierOptions Options { get; }
        public FakeReadinessProbe Readiness { get; }
        public FakeVerificationAuditSinkV2 Audit { get; }
        public ClosedEvidenceVerifierV2 Verifier { get; }
        public EvidenceVerificationRequestV2 Request { get; }

        public static EvidenceFixture Create(
            bool ready = true,
            bool auditSucceeds = true,
            Func<IReadOnlyList<FactOnlyObservationV1>, IReadOnlyList<FactOnlyObservationV1>>? authoritativeMutation = null)
        {
            var options = new AcceptanceVerifierOptions
            {
                ServiceIdentity = "acceptance-verifier",
                IssuerId = "verifier-issuer",
                Audience = "acceptance-verifier",
                KeyId = "verifier-key",
                ContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
                EvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
                CanonicalizationVersion = Rev869BCompatibilityManifestV2.CanonicalizationVersion,
                OwnershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
                ReadinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                EvidenceSchemaVersion = Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                OracleId = "oracle-1",
                OracleVersion = "2.0.0",
                OracleArtifactSha256 = new string('a', 64),
                RequiredReaderIds = ["reader-1"],
                AllowedClusterIds = ["cluster-1"],
                AllowedInstanceIds = ["instance-1"],
                AllowedFactFields = ["count", "status"],
                SensitiveFieldNames = ["password", "token", "private_key", "pan", "bank", "payroll"],
                MaximumEnvelopeBytes = 4_194_304,
                MaximumObservations = 512,
                MaximumSelectors = 128,
                MaximumFactsPerObservation = 256,
                MaximumStringBytes = 32,
                MaximumCumulativeFactBytes = 2_097_152,
                MaximumClockSkewSeconds = 30,
                MaximumObservationWindowSeconds = 600
            };
            var resource = new ResourceBindingV2(
                "C1", "cluster-1", "instance-1", "TARGET", "resource-1", 7, "VERIFY_ACCEPT");
            var lease = new LeaseFenceV2(
                "lease-7", resource.ResourceId, 7, Now.AddMinutes(-5), Now.AddMinutes(-1),
                Now.AddMinutes(5), "acceptance-verifier");
            var receipt = new EvidenceReaderReceiptV2(
                "reader-1", "facts-v2", new string('b', 64),
                new string('c', 64), new string('d', 64), Now.AddMinutes(-1));
            var facts = new Dictionary<string, TypedSelectorValueV1>(StringComparer.Ordinal)
            {
                ["count"] = new(SelectorValueKind.Integer, "1"),
                ["status"] = new(SelectorValueKind.String, "complete")
            };
            var evidence = new CanonicalEvidenceEnvelopeV2(
                "evidence-1",
                resource,
                "request-1",
                lease,
                Now.AddMinutes(-4),
                Now,
                Now.AddMinutes(-2.5),
                [
                    new("before-1", ObservationStage.Before,
                        new(ObservationSourceKind.TargetDatabase, "db-reader", Now.AddMinutes(-3)), facts),
                    new("after-1", ObservationStage.After,
                        new(ObservationSourceKind.TargetDatabase, "db-reader", Now.AddMinutes(-2)), facts),
                    new("durable-1", ObservationStage.Durable,
                        new(ObservationSourceKind.ControllerLedger, "ledger-reader", Now.AddMinutes(-1)), facts)
                ],
                [receipt],
                new ActionResultV1(true, 1, null, null, "resource-1", "complete", 200, "action-receipt-1"),
                string.Empty,
                options.OracleId,
                options.OracleVersion,
                options.OracleArtifactSha256);
            evidence = Rehash(evidence);
            var authoritativeObservations = authoritativeMutation is null
                ? evidence.RawFacts
                : authoritativeMutation(evidence.RawFacts);
            var manifest = new OracleManifestV2(
                options.OracleId,
                options.OracleVersion,
                options.OracleArtifactSha256,
                options.EvidenceVersion,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["reader-1"] = "facts-v2" },
                Now.AddDays(-1),
                null);
            var descriptor = new EvidenceReaderDescriptorV2(
                "reader-1", "facts-v2", new string('b', 64), ObservationSourceKind.TargetDatabase,
                new HashSet<string>(["C1"], StringComparer.Ordinal),
                new HashSet<string>(["resource-1"], StringComparer.Ordinal),
                new HashSet<string>(["count", "status"], StringComparer.Ordinal),
                256,
                1_048_576);
            var readiness = new FakeReadinessProbe(ready);
            var audit = new FakeVerificationAuditSinkV2(auditSucceeds);
            var verifier = new ClosedEvidenceVerifierV2(
                options,
                readiness,
                new FakeOracleManifestRegistry(manifest),
                new FakeEvidenceReaderRegistry(descriptor),
                new FakeAuthoritativeReader(new AuthoritativeEvidenceFactsV2(facts, receipt, authoritativeObservations)),
                new FakeClosedOracleV2(options.OracleId, options.OracleVersion),
                audit,
                new FixedTimeProvider(Now));
            var context = new EvidenceVerificationContextV2(
                "verifier-issuer", "acceptance-verifier", "verifier-key", "request-1", resource, lease);
            return new(options, readiness, audit, verifier, new(evidence, context));
        }

        public EvidenceFixture WithEvidence(CanonicalEvidenceEnvelopeV2 evidence) =>
            new(Options, Readiness, Audit, Verifier, Request with { Evidence = evidence });

        public static CanonicalEvidenceEnvelopeV2 Rehash(CanonicalEvidenceEnvelopeV2 evidence)
        {
            var unsigned = evidence with { PayloadSha256 = string.Empty };
            return evidence with
            {
                PayloadSha256 = Convert.ToHexString(
                    SHA256.HashData(CanonicalJsonV1.Serialize(unsigned))).ToLowerInvariant()
            };
        }
    }

    private sealed class FakeReadinessProbe(bool ready) : ITrustReadinessProbe
    {
        public ValueTask<ReadinessResultV2> CheckAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ReadinessResultV2(
                ready ? ReadinessStateV2.READY : ReadinessStateV2.NOT_READY,
                ready ? [] : ["ORACLE_NOT_PINNED"],
                Now));
    }

    private sealed class FakeDependencyProvider(
        PhaseADependencyV3 dependency,
        ReadinessDependencyStateV3 state) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new DependencyReadinessV3(
                Dependency,
                state,
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                state == ReadinessDependencyStateV3.READY
                    ? Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion
                    : null,
                state.ToString()));
    }

    private sealed class FixedDependencyProvider(DependencyReadinessV3 result) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency => result.Dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) => ValueTask.FromResult(result);
    }

    private sealed class FakeOracleManifestRegistry(OracleManifestV2 manifest) : IOracleManifestRegistry
    {
        public OracleManifestV2? Resolve(string oracleId) => oracleId == manifest.OracleId ? manifest : null;
    }

    private sealed class FakeEvidenceReaderRegistry(EvidenceReaderDescriptorV2 descriptor) : IEvidenceReaderRegistry
    {
        public EvidenceReaderDescriptorV2? Resolve(string readerId, string version) =>
            readerId == descriptor.ReaderId && version == descriptor.Version ? descriptor : null;
    }

    private sealed class FakeAuthoritativeReader(AuthoritativeEvidenceFactsV2 result) : IAuthoritativeEvidenceReader
    {
        public ValueTask<AuthoritativeEvidenceFactsV2> ReadFactsAsync(
            EvidenceReaderDescriptorV2 reader,
            ResourceBindingV2 request,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(result);
    }

    private sealed class FakeClosedOracleV2(string oracleId, string version) : IClosedOracleV2
    {
        public string OracleId { get; } = oracleId;
        public string Version { get; } = version;

        public (VerificationDisposition Disposition, IReadOnlyList<TrustFailureCodeV2> Reasons) Evaluate(
            CanonicalEvidenceEnvelopeV2 evidence,
            OracleManifestV2 manifest) =>
            (VerificationDisposition.Passed, Array.Empty<TrustFailureCodeV2>());
    }

    private sealed class FakeVerificationAuditSinkV2(bool succeeds) : IVerificationAuditSinkV2
    {
        public VerificationAuditEventV2? Event { get; private set; }
        public int AppendAttempts { get; private set; }

        public ValueTask<DurableAuditAppendReceiptV2?> AppendAsync(
            VerificationAuditEventV2 auditEvent,
            CancellationToken cancellationToken = default)
        {
            AppendAttempts++;
            Event = auditEvent;
            return ValueTask.FromResult<DurableAuditAppendReceiptV2?>(
                succeeds
                    ? new(auditEvent.EventId, "durable-audit-1", new string('e', 64), Now)
                    : null);
        }
    }

    private sealed class FakePagedMasterReader(int totalRows, int pageSize)
    {
        public int ReadCount { get; private set; }

        public async IAsyncEnumerable<PageResultV1<int>> ReadAsync(int maximumPages)
        {
            var offset = 0;
            for (var pageNumber = 0; pageNumber < maximumPages && offset < totalRows; pageNumber++)
            {
                await Task.Yield();
                var count = Math.Min(pageSize, totalRows - offset);
                ReadCount++;
                yield return new PageResultV1<int>(
                    Enumerable.Range(offset, count).ToArray(),
                    offset,
                    count,
                    offset + count < totalRows);
                offset += count;
            }
        }
    }
}

internal static class TestObjectExtensions
{
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> function) => function(value);
}
