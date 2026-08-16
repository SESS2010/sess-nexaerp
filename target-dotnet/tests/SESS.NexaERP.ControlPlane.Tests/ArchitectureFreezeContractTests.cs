using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.AcceptanceVerifier.Configuration;
using SESS.NexaERP.AcceptanceVerifier.Verification;
using SESS.NexaERP.ControlPlane.Contracts;
using SESS.NexaERP.ControlPlane.Domain;
using SESS.NexaERP.ControlPlane.Security;
using Xunit;

namespace SESS.NexaERP.ControlPlane.Tests;

public sealed class ArchitectureFreezeContractTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly string Hash = new('a', 64);

    private static readonly string[] FrozenConcepts =
    [
        "prepare-authorize", "prepare-start", "prepare-complete", "prepare-fail",
        "execute-authorize", "execute-start", "execute-complete", "execute-fail",
        "verify-accept", "verify-reject", "quarantine",
        "recover-authorize", "recover-start", "recover-complete", "recover-fail",
        "drop-authorize", "drop", "purge-authorize", "purge-start", "purge-complete",
        "purge-fail", "export-authorize", "export-start", "export-complete",
        "authorization-cancel", "authorization-expire"
    ];

    [Fact]
    public void A1_PublicProtectedSurfaceHasOnlyTwoRawAuthorities()
    {
        Assert.Equal(
            ["AcceptRawCommandAsync"],
            typeof(IControlPlaneAuthority).GetMethods().Select(static method => method.Name));
        Assert.Equal(
            ["VerifyRawAsync"],
            typeof(IAcceptanceVerifierAuthority).GetMethods().Select(static method => method.Name));

        var controlAssembly = typeof(PhaseAControlPlaneAuthority).Assembly;
        var verifierAssembly = typeof(PhaseAAcceptanceVerifierAuthority).Assembly;
        foreach (var forbidden in new[]
                 {
                     "SignedEnvelopeService", "SignedEnvelopeVerificationService", "SignedCommandServiceV2",
                     "ClosedEvidenceVerifierV1", "ClosedEvidenceVerifierV2", "StrictEvidenceJsonV2"
                 })
        {
            var type = controlAssembly.GetTypes().Concat(verifierAssembly.GetTypes())
                .Single(candidate => candidate.Name == forbidden);
            Assert.False(type.IsPublic);
        }

        Assert.All(
            typeof(IControlPlaneAuthority).GetMethods().Single().GetParameters().Take(3),
            parameter => Assert.Contains(
                parameter.ParameterType,
                new[] { typeof(ReadOnlyMemory<byte>), typeof(AuthenticatedWorkloadIdentityV3) }));
        Assert.Equal(
            typeof(ReadOnlyMemory<byte>),
            typeof(IAcceptanceVerifierAuthority).GetMethods().Single().GetParameters()[0].ParameterType);
    }

    [Fact]
    public async Task A1_RawCommandCodecRejectsEveryNonCanonicalMutationBeforeAuthority()
    {
        var fixture = CommandFixture.Create();
        var invalid = fixture.HeaderBytes.Concat([(byte)0x20]).ToArray();
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.AcceptRawCommandAsync(
                invalid,
                fixture.PayloadBytes,
                fixture.Signature,
                fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, failure.Code);
        Assert.Equal(0, fixture.Controller.CallCount);
    }

    [Fact]
    public async Task A1_RawEvidenceCodecRejectsEveryNonCanonicalMutationBeforeReaderOrOracle()
    {
        var fixture = EvidenceFixture.Create();
        var json = Encoding.UTF8.GetString(fixture.CanonicalEvidence);
        var invalid = Encoding.UTF8.GetBytes(json.Insert(1, "\"Unknown\":1,"));
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(invalid, fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD, failure.Code);
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
        Assert.Equal(0, fixture.Audit.AppendCount);
    }

    [Fact]
    public void A1_ExactlyFourteenAuthoritativeOwnersAndNoParallelPublicAuthority()
    {
        var expected = new Dictionary<ProductionResponsibilityV3, Type>
        {
            [ProductionResponsibilityV3.NexaErpBusinessRuntime] = typeof(INexaErpBusinessRuntime),
            [ProductionResponsibilityV3.ControlPlane] = typeof(IControlPlaneAuthority),
            [ProductionResponsibilityV3.AcceptanceVerifier] = typeof(IAcceptanceVerifierAuthority),
            [ProductionResponsibilityV3.DurableControlPlanePersistence] = typeof(IDurableControlPlanePersistenceProvider),
            [ProductionResponsibilityV3.TrustedIssuerKeyRegistry] = typeof(ITrustedIssuerKeyRegistryProvider),
            [ProductionResponsibilityV3.KmsHsmSigning] = typeof(IKmsHsmSigningProvider),
            [ProductionResponsibilityV3.AuthoritativeEvidenceReader] = typeof(IAuthoritativeEvidenceReaderProvider),
            [ProductionResponsibilityV3.ImmutableAuditEvidence] = typeof(IImmutableAuditEvidenceProvider),
            [ProductionResponsibilityV3.LifecycleController] = typeof(ILifecycleControllerAuthority),
            [ProductionResponsibilityV3.BackupRecoveryAuthority] = typeof(IBackupRecoveryAuthority),
            [ProductionResponsibilityV3.PurgeAuthorizer] = typeof(IPurgeAuthorizer),
            [ProductionResponsibilityV3.PurgeExecutor] = typeof(IPurgeExecutor),
            [ProductionResponsibilityV3.ExportAuthorizer] = typeof(IExportAuthorizer),
            [ProductionResponsibilityV3.ExportDeliveryExecutor] = typeof(IExportDeliveryExecutor)
        };
        Assert.Equal(expected, PhaseAOwnershipCatalog.All);
        Assert.Equal(14, expected.Values.Distinct().Count());

        var exportedNames = typeof(PhaseAControlPlaneAuthority).Assembly.ExportedTypes
            .Concat(typeof(PhaseAAcceptanceVerifierAuthority).Assembly.ExportedTypes)
            .Select(static type => type.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("ILeaseFenceStore", exportedNames);
        Assert.DoesNotContain("IIdempotencyStore", exportedNames);
        Assert.DoesNotContain("ILifecycleStateStore", exportedNames);
        Assert.DoesNotContain("ITrustedIssuerRegistry", exportedNames);
        Assert.DoesNotContain("IEnvelopeSigner", exportedNames);
        Assert.DoesNotContain("IAuthoritativeEvidenceReader", exportedNames);
        Assert.DoesNotContain("IVerificationAuditSinkV2", exportedNames);
    }

    [Fact]
    public async Task A1_TrustedGrantPolicyLeaseAndReaderFactsCannotBeSynthesizedFromRequest()
    {
        var fixture = CommandFixture.Create(headerMutation: header => header with { AuthorizedRole = "PurgeExecutor" });
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.InvokeAsync().AsTask());
        Assert.Equal(TrustFailureCodeV2.REQUEST_ROLE_FORBIDDEN, failure.Code);
        Assert.Equal(0, fixture.Controller.CallCount);
    }

    [Fact]
    public void A1_FrozenLifecycleMatrixHasExactTwentySixConceptualRows()
    {
        var actual = Rev869BControllerStateMachine.PhaseARuleSnapshot
            .Select(Concept)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(FrozenConcepts.OrderBy(static value => value, StringComparer.Ordinal), actual);
    }

    [Fact]
    public void A1_EveryUnlistedLifecycleCombinationIsIllegal()
    {
        var machine = new Rev869BControllerStateMachine();
        var legal = FrozenLegalPairs();
        foreach (var state in Enum.GetValues<ControllerLifecycleState>())
        foreach (var operation in Enum.GetValues<ControllerOperationV2>())
        {
            if (operation is ControllerOperationV2.QUARANTINE or ControllerOperationV2.CANCEL or ControllerOperationV2.EXPIRE ||
                legal.Contains((state, operation)))
            {
                continue;
            }
            var failure = Assert.Throws<TrustFailureExceptionV2>(
                () => machine.RequirePhaseACommand(
                    Command(state, operation, "Operator", [], false),
                    Now));
            Assert.Equal(TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL, failure.Code);
        }
    }

    [Fact]
    public void A1_EveryLifecycleBindingMutationFailsWithoutStateChange()
    {
        var machine = new Rev869BControllerStateMachine();
        var valid = Command(
            ControllerLifecycleState.Preflight,
            ControllerOperationV2.PREPARE,
            "ProvisioningExecutor",
            ["preflight"],
            true);
        Assert.Equal(ControllerLifecycleState.Provisioning, machine.RequirePhaseACommand(valid, Now).NextState);

        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with { CurrentVersion = valid.CurrentVersion + 1 },
                Now),
            TrustFailureCodeV2.RESOURCE_VERSION_STALE);
        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with
                {
                    Authorization = valid.Authorization with
                    {
                        Authorization = valid.Authorization.Authorization with { TrustedRole = "Operator" }
                    }
                },
                Now),
            TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        AssertCode(
            () => machine.RequirePhaseACommand(valid with { RequiredEvidence = [] }, Now),
            TrustFailureCodeV2.READER_MISSING);
    }

    [Fact]
    public void A1_ExportSubstatesCannotBeSkippedOrReused()
    {
        var machine = new Rev869BControllerStateMachine();
        var authorize = Command(
            ControllerLifecycleState.Accepted,
            ControllerOperationV2.AUTHORIZE_EXPORT,
            "ExportAuthorizer",
            ["minimized-batch-root", "privacy-approval"],
            false,
            exportState: ExportAuthorizationSubstateV3.NONE);
        Assert.Equal(
            ExportAuthorizationSubstateV3.AUTHORIZED,
            machine.RequirePhaseACommand(authorize, Now).NextExportState);

        var start = Command(
            ControllerLifecycleState.Accepted,
            ControllerOperationV2.EXPORT,
            "ExportExecutor",
            ["export-authorization"],
            true,
            exportState: ExportAuthorizationSubstateV3.AUTHORIZED);
        Assert.Equal(
            ExportAuthorizationSubstateV3.DELIVERING,
            machine.RequirePhaseACommand(start, Now).NextExportState);

        AssertCode(
            () => machine.RequirePhaseACommand(
                start with { CurrentExportState = ExportAuthorizationSubstateV3.NONE },
                Now),
            TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
    }

    [Fact]
    public void A1_CancelAndExpireChangeOnlyAuthorizationSubstate()
    {
        var machine = new Rev869BControllerStateMachine();
        var cancel = Command(
            ControllerLifecycleState.Ready,
            ControllerOperationV2.CANCEL,
            "OriginalAuthorizer",
            ["cancellation-reason"],
            false);
        Assert.Equal(
            AuthorizationGrantStateV3.CANCELLED,
            machine.RequirePhaseACommand(cancel, Now).NextAuthorizationState);

        var expire = Command(
            ControllerLifecycleState.Ready,
            ControllerOperationV2.EXPIRE,
            "ControlPlaneRuntime",
            ["server-time"],
            false);
        expire = expire with
        {
            Authorization = expire.Authorization with
            {
                Authorization = expire.Authorization.Authorization with { ExpiresAt = Now.AddMinutes(-1) }
            }
        };
        Assert.Equal(
            AuthorizationGrantStateV3.EXPIRED,
            machine.RequirePhaseACommand(expire, Now).NextAuthorizationState);
    }

    [Fact]
    public void A1_QuarantineIsControllerOwnedAndPurgedIsTerminal()
    {
        var machine = new Rev869BControllerStateMachine();
        var quarantine = Command(
            ControllerLifecycleState.Ready,
            ControllerOperationV2.QUARANTINE,
            "ControlPlaneRuntime",
            ["inconsistency-facts"],
            false);
        Assert.Equal(
            ControllerLifecycleState.Quarantined,
            machine.RequirePhaseACommand(quarantine, Now).NextState);
        AssertCode(
            () => machine.RequirePhaseACommand(
                quarantine with { CurrentState = ControllerLifecycleState.Purged },
                Now),
            TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
    }

    [Fact]
    public void A1_LeaseBindsResourceHolderEpochFenceAndExpiry()
    {
        var machine = new Rev869BControllerStateMachine();
        var valid = Command(
            ControllerLifecycleState.Preflight,
            ControllerOperationV2.PREPARE,
            "ProvisioningExecutor",
            ["preflight"],
            true);
        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with { Lease = valid.Lease! with { ResourceId = "other" } },
                Now),
            TrustFailureCodeV2.LEASE_FENCE_STALE);
        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with { Lease = valid.Lease! with { ControllerEpoch = 0 } },
                Now),
            TrustFailureCodeV2.LEASE_FENCE_STALE);
        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with { Lease = valid.Lease! with { ExpiresAt = Now.AddSeconds(-1) } },
                Now),
            TrustFailureCodeV2.LEASE_EXPIRED);
    }

    [Fact]
    public void A1_AuthorizationIsOneTimeAndOperationBound()
    {
        var machine = new Rev869BControllerStateMachine();
        var command = Command(
            ControllerLifecycleState.MigrationAuthorized,
            ControllerOperationV2.EXECUTE,
            "MigrationExecutor",
            ["preflight"],
            true);
        Assert.Equal(
            AuthorizationGrantStateV3.CONSUMED,
            machine.RequirePhaseACommand(command, Now).NextAuthorizationState);
        AssertCode(
            () => machine.RequirePhaseACommand(
                command with { CurrentAuthorizationState = AuthorizationGrantStateV3.CONSUMED },
                Now),
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
    }

    [Fact]
    public async Task A1_ReaderBundleSignatureHashAndScopeAreRecomputed()
    {
        var fixture = EvidenceFixture.Create();
        var verdict = await fixture.Authority.VerifyRawAsync(
            fixture.CanonicalEvidence,
            fixture.Transport);
        Assert.Equal(VerificationDisposition.Passed, verdict.Calculation.Disposition);
        Assert.Equal(1, fixture.Oracle.EvaluationCount);

        var changed = fixture.Bundle with
        {
            Facts = [new("status", SelectorValueKind.String, "failed")]
        };
        var tampered = fixture.WithBundle(changed);
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(tampered, fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_TAMPERED, failure.Code);
    }

    [Fact]
    public async Task A1_CallerFactsActionReceiptExpectedValuesAndVerdictNeverReachOracle()
    {
        var fixture = EvidenceFixture.Create();
        var json = Encoding.UTF8.GetString(fixture.CanonicalEvidence);
        foreach (var name in new[] { "Verdict", "Expected", "ActionReceipt" })
        {
            var invalid = Encoding.UTF8.GetBytes(json.Insert(1, $"\"{name}\":\"PASS\","));
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.Authority.VerifyRawAsync(invalid, fixture.Transport).AsTask());
            Assert.Equal(TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD, failure.Code);
        }
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
    }

    [Fact]
    public async Task A1_ReaderAndGlobalLimitsUseStricterServerOwnedBound()
    {
        var fixture = EvidenceFixture.Create(maximumFacts: 1);
        var extra = fixture.Bundle with
        {
            Facts =
            [
                new("status", SelectorValueKind.String, "complete"),
                new("count", SelectorValueKind.Integer, "1")
            ]
        };
        extra = SignBundle(extra, fixture.Kms);
        var invalid = fixture.WithBundle(extra);
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(invalid, fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.READER_UNAUTHORIZED, failure.Code);
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
    }

    [Fact]
    public async Task A1_OracleIsPinnedMutationSensitiveAndCannotAcceptReaderVerdict()
    {
        var passing = EvidenceFixture.Create();
        var pass = await passing.Authority.VerifyRawAsync(
            passing.CanonicalEvidence,
            passing.Transport);
        Assert.Equal(VerificationDisposition.Passed, pass.Calculation.Disposition);

        var failingBundle = passing.Bundle with
        {
            Facts = [new("status", SelectorValueKind.String, "failed")]
        };
        failingBundle = SignBundle(failingBundle, passing.Kms);
        var failBytes = passing.WithBundle(failingBundle);
        var fail = await passing.Authority.VerifyRawAsync(failBytes, passing.Transport);
        Assert.Equal(VerificationDisposition.Failed, fail.Calculation.Disposition);
    }

    [Fact]
    public async Task A1_ReadinessFailsClosedForMissingDuplicateExceptionTimeoutStaleAndMismatch()
    {
        var dependencies = Enum.GetValues<PhaseADependencyV3>();
        var literalDependencies = new[]
        {
            PhaseADependencyV3.Configuration, PhaseADependencyV3.WorkloadIdentity,
            PhaseADependencyV3.IssuerRegistry, PhaseADependencyV3.AudiencePolicy,
            PhaseADependencyV3.SubjectRoleScopeResolver, PhaseADependencyV3.KeyRegistry,
            PhaseADependencyV3.AlgorithmVersionPolicy, PhaseADependencyV3.TrustedClock,
            PhaseADependencyV3.DurableControlPlane, PhaseADependencyV3.KmsHsm,
            PhaseADependencyV3.LifecycleController, PhaseADependencyV3.OracleRegistry,
            PhaseADependencyV3.EvidenceReaderRegistry, PhaseADependencyV3.ImmutableAuditEvidence,
            PhaseADependencyV3.TargetIdentityAndAcl
        };
        Assert.Equal(literalDependencies, dependencies);

        var providers = literalDependencies
            .Select(dependency => (IReadinessDependencyProvider)new ReadyProvider(dependency, Now))
            .ToList();
        var ready = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            providers,
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.True(ready.CanExecuteProtectedOperation);

        var stale = providers.ToList();
        stale[0] = new ReadyProvider(literalDependencies[0], Now.AddMinutes(-2));
        var staleSnapshot = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            stale,
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.False(staleSnapshot.CanExecuteProtectedOperation);
        Assert.Equal(ReadinessDependencyStateV3.UNAVAILABLE, staleSnapshot.Dependencies[0].State);

        var throwing = providers.ToList();
        throwing[0] = new ThrowingProvider(literalDependencies[0]);
        var unavailable = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            throwing,
            new FixedTimeProvider(Now)).CheckAsync();
        Assert.Equal(ReadinessDependencyStateV3.UNAVAILABLE, unavailable.Dependencies[0].State);
    }

    [Fact]
    public void A1_ImmutableAuditEventBindsExactStateGrantLeaseAttemptTransactionAndKey()
    {
        var valid = new ImmutableAuditEventV3(
            "event", AuditEventKindV3.LIFECYCLE_COMMITTED, "correlation", "actor", "C1", "instance",
            "resource", "EXECUTE", Hash, "policy", "COMMITTED", Hash, Now,
            ControllerLifecycleState.MigrationAuthorized, 7, ControllerLifecycleState.Migrating, 8,
            "attempt", Hash, "lease", 4, 9, "transaction", "key", "v1");
        PhaseAContractValidator.RequireValid(valid);
        AssertCode(
            () => PhaseAContractValidator.RequireValid(valid with { AttemptId = string.Empty }),
            TrustFailureCodeV2.AUDIT_APPEND_FAILED);
    }

    [Fact]
    public async Task A1_AuditAppendFailureCannotReturnProtectedSuccessOrVerdict()
    {
        var fixture = EvidenceFixture.Create(auditSucceeds: false);
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(
                fixture.CanonicalEvidence,
                fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.AUDIT_APPEND_FAILED, failure.Code);
        Assert.Equal(1, fixture.Oracle.EvaluationCount);
    }

    [Fact]
    public async Task A1_ConcurrentDuplicateThroughRawAuthorityHasOneOwnerAndOneDelegate()
    {
        var controller = new CoordinatedController();
        var fixture = CommandFixture.Create(controller: controller);
        var calls = Enumerable.Range(0, 8).Select(_ => fixture.InvokeAsync().AsTask()).ToArray();
        var results = await Task.WhenAll(calls);
        Assert.Equal(1, results.Count(result => result.TransactionOutcome == ControlTransactionOutcomeV3.FIRST_OWNER));
        Assert.Equal(7, results.Count(result => result.TransactionOutcome == ControlTransactionOutcomeV3.IN_PROGRESS));
        Assert.Equal(1, controller.BusinessExecutionCount);
    }

    [Fact]
    public async Task A1_ChangedPayloadIdempotencyCollisionNeverReusesResult()
    {
        var controller = new DigestBindingController();
        var first = CommandFixture.Create(controller: controller);
        var initial = await first.InvokeAsync();
        Assert.Equal(ControlTransactionOutcomeV3.FIRST_OWNER, initial.TransactionOutcome);

        var changed = CommandFixture.Create(
            controller: controller,
            payloadMutation: payload => payload with { ActionId = "different-action" });
        var collision = await changed.InvokeAsync();
        Assert.Equal(TrustFailureCodeV2.IDEMPOTENCY_PAYLOAD_MISMATCH, collision.FailureCode);
        Assert.Equal(1, controller.BusinessExecutionCount);
    }

    [Fact]
    public void A1_OpaquePageTokenBindsScopeSnapshotPriorDigestExpiryAndLimit()
    {
        var scope = new CompanyDatabaseScopeV3(
            "C1", "cluster", "instance", MasterScopeKindV3.COMPANY_LEDGER);
        var expected = new OpaquePageTokenBindingV3(
            "issuer", "subject", scope, "resource", "query-v1", "snapshot", 1_000,
            Now.AddMinutes(5), Hash);
        PhaseAContractValidator.RequireValid(expected, expected, Now);
        AssertCode(
            () => PhaseAContractValidator.RequireValid(
                expected with { Scope = scope with { OrganizationId = "C2" } },
                expected,
                Now),
            TrustFailureCodeV2.PAGINATION_TOKEN_INVALID);
        AssertCode(
            () => PhaseAContractValidator.RequireValid(
                expected with { PageSize = PhaseAContractLimits.MaximumPageSize + 1 },
                expected,
                Now),
            TrustFailureCodeV2.PAGINATION_TOKEN_INVALID);
    }

    [Fact]
    public async Task A1_AllForbiddenEvidenceAuditAndReadinessFieldsAreRejectedAndSanitized()
    {
        var fixture = EvidenceFixture.Create(sensitiveField: "password");
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(
                fixture.CanonicalEvidence,
                fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_SENSITIVE_FIELD, failure.Code);
        Assert.DoesNotContain("secret-value", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, fixture.Audit.AppendCount);
    }

    [Fact]
    public async Task A1_DecisiveSecurityMutationManifestHasZeroSurvivors()
    {
        var killed = new List<string>();

        var command = CommandFixture.Create(headerMutation: header => header with { AuthorizedScope = "ORG:C2" });
        var commandFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => command.InvokeAsync().AsTask());
        if (commandFailure.Code == TrustFailureCodeV2.SCOPE_MISMATCH) killed.Add("command-scope");

        var evidence = EvidenceFixture.Create();
        var tampered = evidence.Bundle with { FactsSha256 = new string('f', 64) };
        var evidenceFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => evidence.Authority.VerifyRawAsync(evidence.WithBundle(tampered), evidence.Transport).AsTask());
        if (evidenceFailure.Code == TrustFailureCodeV2.EVIDENCE_TAMPERED) killed.Add("evidence-hash");

        var state = Command(
            ControllerLifecycleState.Accepted,
            ControllerOperationV2.COMPLETE_EXPORT,
            "ExportExecutor",
            ["delivery-receipt"],
            true,
            exportState: ExportAuthorizationSubstateV3.AUTHORIZED,
            authorizationState: AuthorizationGrantStateV3.CONSUMED);
        try
        {
            new Rev869BControllerStateMachine().RequirePhaseACommand(state, Now);
        }
        catch (TrustFailureExceptionV2 exception) when (exception.Code == TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL)
        {
            killed.Add("export-skip");
        }

        var providers = Enum.GetValues<PhaseADependencyV3>()
            .Select(dependency => (IReadinessDependencyProvider)new ReadyProvider(dependency, Now))
            .Skip(1);
        var snapshot = await new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            providers,
            new FixedTimeProvider(Now)).CheckAsync();
        if (!snapshot.CanExecuteProtectedOperation) killed.Add("readiness-missing");

        Assert.Equal(
            ["command-scope", "evidence-hash", "export-skip", "readiness-missing"],
            killed);
    }

    [Fact]
    public void A1_ReviewedPhaseARangeHasNoWhitespaceOrConflictMarkerError()
    {
        var root = FindRoot();
        var allowlist = new[]
        {
            "src/SESS.NexaERP.ControlPlane.Contracts/Rev869BControllerMessagesV1.cs",
            "src/SESS.NexaERP.ControlPlane.Contracts/Rev869BCompatibilityManifestV1.cs",
            "src/SESS.NexaERP.ControlPlane/Domain/Rev869BExecutionBinding.cs",
            "src/SESS.NexaERP.ControlPlane/Domain/Rev869BControllerStateMachine.cs",
            "src/SESS.NexaERP.ControlPlane/Security/SignedEnvelopeService.cs",
            "src/SESS.NexaERP.ControlPlane/Configuration/ControlPlaneOptions.cs",
            "src/SESS.NexaERP.ControlPlane/Program.cs",
            "src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs",
            "src/SESS.NexaERP.AcceptanceVerifier/Configuration/AcceptanceVerifierOptions.cs",
            "src/SESS.NexaERP.AcceptanceVerifier/Program.cs",
            "src/SESS.NexaERP.AcceptanceVerifier/Verification/ClosedEvidenceVerifierV1.cs",
            "tests/SESS.NexaERP.ControlPlane.Tests/ArchitectureFreezeContractTests.cs",
            "outputs/rev869b_external_controller_phase_a_checkpoint.md"
        };
        Assert.Equal(13, allowlist.Length);
        foreach (var relative in allowlist)
        {
            var lines = File.ReadAllLines(Path.Combine(root, relative));
            Assert.DoesNotContain(lines, static line => line.EndsWith(' ') || line.EndsWith('\t'));
            Assert.DoesNotContain(lines, static line =>
                line.StartsWith("<<<<<<<", StringComparison.Ordinal) ||
                line.StartsWith("=======", StringComparison.Ordinal) ||
                line.StartsWith(">>>>>>>", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void A1_PhaseBImplementationDoesNotLeakIntoCorrection()
    {
        var references = typeof(PhaseAControlPlaneAuthority).Assembly.GetReferencedAssemblies()
            .Select(static assembly => assembly.Name)
            .ToArray();
        Assert.DoesNotContain("Npgsql", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
        Assert.DoesNotContain(
            typeof(PhaseAControlPlaneAuthority).Assembly.ExportedTypes,
            static type => type.Name.Contains("DbContext", StringComparison.Ordinal) ||
                           type.Name.Contains("Migration", StringComparison.Ordinal));
    }

    private static string Concept(LifecycleRuleV3 rule) => rule.RuleId switch
    {
        "registered-authorize-prepare" => "prepare-authorize",
        "preflight-prepare" => "prepare-start",
        "provisioning-complete" => "prepare-complete",
        "provisioning-fail" => "prepare-fail",
        "ready-authorize-execute" => "execute-authorize",
        "authorized-execute" => "execute-start",
        "migrating-complete" => "execute-complete",
        "migrating-fail" => "execute-fail",
        "verification-accept" => "verify-accept",
        "verification-reject" => "verify-reject",
        "any-quarantine" => "quarantine",
        "quarantined-authorize-recover" => "recover-authorize",
        "authorized-recover" => "recover-start",
        "recovering-complete" => "recover-complete",
        "recovering-fail" => "recover-fail",
        "accepted-authorize-drop" or "failed-authorize-drop" or "quarantined-authorize-drop" => "drop-authorize",
        "authorized-drop" => "drop",
        "dropped-authorize-purge" => "purge-authorize",
        "authorized-purge" => "purge-start",
        "purging-complete" => "purge-complete",
        "purging-fail" => "purge-fail",
        "accepted-authorize-export" or "accepted-reauthorize-expired-export" or
            "accepted-reauthorize-failed-export" => "export-authorize",
        "accepted-export" => "export-start",
        "accepted-complete-export" => "export-complete",
        "any-cancel-active-authorization" => "authorization-cancel",
        "any-expire-active-authorization" => "authorization-expire",
        _ => throw new InvalidOperationException($"Unknown frozen rule {rule.RuleId}.")
    };

    private static HashSet<(ControllerLifecycleState, ControllerOperationV2)> FrozenLegalPairs() =>
    [
        (ControllerLifecycleState.Registered, ControllerOperationV2.AUTHORIZE_PREPARE),
        (ControllerLifecycleState.Preflight, ControllerOperationV2.PREPARE),
        (ControllerLifecycleState.Provisioning, ControllerOperationV2.COMPLETE_PREPARE),
        (ControllerLifecycleState.Provisioning, ControllerOperationV2.FAIL),
        (ControllerLifecycleState.Ready, ControllerOperationV2.AUTHORIZE_EXECUTE),
        (ControllerLifecycleState.MigrationAuthorized, ControllerOperationV2.EXECUTE),
        (ControllerLifecycleState.Migrating, ControllerOperationV2.COMPLETE_EXECUTE),
        (ControllerLifecycleState.Migrating, ControllerOperationV2.FAIL),
        (ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_ACCEPT),
        (ControllerLifecycleState.VerificationPending, ControllerOperationV2.VERIFY_REJECT),
        (ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_RECOVER),
        (ControllerLifecycleState.RecoveryAuthorized, ControllerOperationV2.RECOVER),
        (ControllerLifecycleState.Recovering, ControllerOperationV2.COMPLETE_RECOVER),
        (ControllerLifecycleState.Recovering, ControllerOperationV2.FAIL),
        (ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_DROP),
        (ControllerLifecycleState.Failed, ControllerOperationV2.AUTHORIZE_DROP),
        (ControllerLifecycleState.Quarantined, ControllerOperationV2.AUTHORIZE_DROP),
        (ControllerLifecycleState.DropAuthorized, ControllerOperationV2.DROP),
        (ControllerLifecycleState.Dropped, ControllerOperationV2.AUTHORIZE_PURGE),
        (ControllerLifecycleState.PurgeAuthorized, ControllerOperationV2.PURGE),
        (ControllerLifecycleState.Purging, ControllerOperationV2.COMPLETE_PURGE),
        (ControllerLifecycleState.Purging, ControllerOperationV2.FAIL),
        (ControllerLifecycleState.Accepted, ControllerOperationV2.AUTHORIZE_EXPORT),
        (ControllerLifecycleState.Accepted, ControllerOperationV2.EXPORT),
        (ControllerLifecycleState.Accepted, ControllerOperationV2.COMPLETE_EXPORT)
    ];

    private static VerifiedLifecycleCommandV3 Command(
        ControllerLifecycleState state,
        ControllerOperationV2 operation,
        string role,
        IReadOnlyList<string> evidenceIds,
        bool requiresLease,
        ExportAuthorizationSubstateV3 exportState = ExportAuthorizationSubstateV3.NONE,
        AuthorizationGrantStateV3 authorizationState = AuthorizationGrantStateV3.ACTIVE)
    {
        var scope = new CompanyDatabaseScopeV3(
            "C1", "cluster", "instance", MasterScopeKindV3.COMPANY_LEDGER);
        var resolved = new ResolvedAuthorizationV3(
            "authorization", "issuer", "subject", "workload", "audience",
            operation.ToString(), role, "ORG:C1", "policy-v1", "row-v1", Hash,
            Now.AddHours(-1), Now.AddHours(1));
        var lease = requiresLease
            ? new LeaseFenceExpectationV3(
                "lease", 1, 9, Now.AddMinutes(5), "subject", "resource")
            : null;
        var evidence = evidenceIds.Select(id => new EvidenceRequirementV3(
            id,
            id,
            "reader-v1",
            EvidenceStageV3.DURABLE,
            Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
            10,
            1_024)).ToArray();
        var binding = new AuthorizationBindingV3(
            resolved,
            scope,
            "REV869B_TARGET",
            "resource",
            7,
            operation.ToString(),
            Hash,
            Hash,
            lease?.LeaseId ?? "lease-none",
            lease?.FencingToken ?? 0,
            AuthorizationGrantStateV3.ACTIVE);
        return new(
            "command",
            operation,
            state,
            7,
            binding,
            lease,
            evidence,
            new("issuer", "C1", "instance", operation.ToString(), "request", "idempotency", Hash),
            new("issuer", "nonce", Now.AddHours(1), Hash),
            "audit",
            Hash,
            authorizationState,
            exportState,
            "attempt");
    }

    private static void AssertCode(Action action, TrustFailureCodeV2 expected)
    {
        var exception = Assert.Throws<TrustFailureExceptionV2>(action);
        Assert.Equal(expected, exception.Code);
    }

    private sealed class CommandFixture
    {
        private CommandFixture(
            PhaseAControlPlaneAuthority authority,
            CanonicalSignedHeaderV2 header,
            byte[] headerBytes,
            byte[] payloadBytes,
            byte[] signature,
            AuthenticatedWorkloadIdentityV3 transport,
            CountingController controller)
        {
            Authority = authority;
            Header = header;
            HeaderBytes = headerBytes;
            PayloadBytes = payloadBytes;
            Signature = signature;
            Transport = transport;
            Controller = controller;
        }

        public PhaseAControlPlaneAuthority Authority { get; }
        public CanonicalSignedHeaderV2 Header { get; }
        public byte[] HeaderBytes { get; }
        public byte[] PayloadBytes { get; }
        public byte[] Signature { get; }
        public AuthenticatedWorkloadIdentityV3 Transport { get; }
        public CountingController Controller { get; }

        public ValueTask<LifecycleTransitionResultV3> InvokeAsync() =>
            Authority.AcceptRawCommandAsync(HeaderBytes, PayloadBytes, Signature, Transport);

        public static CommandFixture Create(
            Func<CanonicalSignedHeaderV2, CanonicalSignedHeaderV2>? headerMutation = null,
            Func<CanonicalCommandPayloadV2, CanonicalCommandPayloadV2>? payloadMutation = null,
            CountingController? controller = null)
        {
            controller ??= new CountingController();
            var payload = new CanonicalCommandPayloadV2(
                ControllerOperationV2.AUTHORIZE_PREPARE,
                ControllerLifecycleState.Registered,
                ControllerLifecycleState.Preflight,
                new("scenario"),
                new("subcase"),
                "action",
                new Dictionary<string, string>(StringComparer.Ordinal),
                ["target-registration"]);
            payload = payloadMutation?.Invoke(payload) ?? payload;
            var payloadBytes = CanonicalJsonV1.Serialize(payload);
            var header = new CanonicalSignedHeaderV2(
                Rev869BCompatibilityManifestV2.ContractVersion,
                Rev869BCompatibilityManifestV2.CanonicalizationVersion,
                Rev869BCompatibilityManifestV2.SignatureAlgorithm,
                "key",
                "issuer",
                "audience",
                "subject",
                "Operator",
                "ORG:C1",
                "C1",
                "cluster",
                "instance",
                payload.Operation.ToString(),
                "resource",
                7,
                "lease-none",
                0,
                "request",
                "idempotency",
                "AAAAAAAAAAAAAAAAAAAAAA",
                Now.AddMinutes(-1),
                Now.AddMinutes(-1),
                Now.AddMinutes(5),
                Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant(),
                payloadBytes.Length);
            header = headerMutation?.Invoke(header) ?? header;
            var headerBytes = CanonicalSignedHeaderCodecV2.Serialize(header);
            var kms = new FakeKms();
            var authority = new PhaseAControlPlaneAuthority(
                new FakeKeyRegistry(),
                new FixedAudiencePolicy(),
                new FixedAuthorizationResolver(),
                new FixedAlgorithmPolicy(),
                new FixedClockPolicy(),
                kms,
                new FixedLeaseAuthority(),
                new FakeReaderRegistry(),
                controller,
                new FixedTimeProvider(Now));
            return new(
                authority,
                header,
                headerBytes,
                payloadBytes,
                SHA256.HashData(headerBytes),
                new("issuer", "subject", "workload", "audience", Hash),
                controller);
        }
    }

    private class CountingController : ILifecycleControllerAuthority
    {
        private readonly Rev869BControllerStateMachine _machine = new();
        private int _callCount;

        public int CallCount => _callCount;

        public virtual ValueTask<LifecycleTransitionResultV3> TransitionAsync(
            VerifiedLifecycleCommandV3 command,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            var rule = _machine.RequirePhaseACommand(command, Now);
            return ValueTask.FromResult(new LifecycleTransitionResultV3(
                ControlTransactionOutcomeV3.FIRST_OWNER,
                rule.NextState,
                command.CurrentVersion + 1,
                1,
                command.CanonicalEnvelopeSha256,
                command.AuditCorrelationId,
                TrustFailureCodeV2.NONE,
                rule.NextAuthorizationState,
                rule.NextExportState,
                rule.LifecycleAuditEvent));
        }
    }

    private sealed class CoordinatedController : CountingController
    {
        private int _owner;
        private int _businessExecutionCount;

        public int BusinessExecutionCount => _businessExecutionCount;

        public override async ValueTask<LifecycleTransitionResultV3> TransitionAsync(
            VerifiedLifecycleCommandV3 command,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (Interlocked.CompareExchange(ref _owner, 1, 0) == 0)
            {
                Interlocked.Increment(ref _businessExecutionCount);
                var first = await base.TransitionAsync(command, cancellationToken);
                return first with { TransactionOutcome = ControlTransactionOutcomeV3.FIRST_OWNER };
            }
            return new(
                ControlTransactionOutcomeV3.IN_PROGRESS,
                command.CurrentState,
                command.CurrentVersion,
                1,
                command.CanonicalEnvelopeSha256,
                command.AuditCorrelationId,
                TrustFailureCodeV2.IDEMPOTENCY_IN_PROGRESS);
        }
    }

    private sealed class DigestBindingController : CountingController
    {
        private string? _digest;
        private int _businessExecutionCount;

        public int BusinessExecutionCount => _businessExecutionCount;

        public override async ValueTask<LifecycleTransitionResultV3> TransitionAsync(
            VerifiedLifecycleCommandV3 command,
            CancellationToken cancellationToken = default)
        {
            if (_digest is null)
            {
                _digest = command.Idempotency.CanonicalRequestSha256;
                Interlocked.Increment(ref _businessExecutionCount);
                var first = await base.TransitionAsync(command, cancellationToken);
                return first with { TransactionOutcome = ControlTransactionOutcomeV3.FIRST_OWNER };
            }
            if (_digest != command.Idempotency.CanonicalRequestSha256)
            {
                return new(
                    ControlTransactionOutcomeV3.CONFLICT,
                    command.CurrentState,
                    command.CurrentVersion,
                    1,
                    command.CanonicalEnvelopeSha256,
                    command.AuditCorrelationId,
                    TrustFailureCodeV2.IDEMPOTENCY_PAYLOAD_MISMATCH);
            }
            return new(
                ControlTransactionOutcomeV3.COMPLETED_REPLAY,
                command.CurrentState,
                command.CurrentVersion,
                1,
                command.CanonicalEnvelopeSha256,
                command.AuditCorrelationId,
                TrustFailureCodeV2.NONE);
        }
    }

    private sealed class FakeKms : IKmsHsmSigningProvider
    {
        public ValueTask<ReadOnlyMemory<byte>> SignAsync(
            TrustedSigningContextV3 trustedContext,
            ReadOnlyMemory<byte> canonicalBytes,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ReadOnlyMemory<byte>>(SHA256.HashData(canonicalBytes.Span));

        public ValueTask<bool> VerifyAsync(
            SigningKeyMetadataV3 key,
            ReadOnlyMemory<byte> canonicalBytes,
            ReadOnlyMemory<byte> signature,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(canonicalBytes.Span),
                signature.Span));
    }

    private sealed class FakeKeyRegistry : ITrustedIssuerKeyRegistryProvider
    {
        public ValueTask<IssuerTrustPolicyV3?> ResolveIssuerAsync(
            string issuerId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IssuerTrustPolicyV3?>(new(
                issuerId,
                "policy-v1",
                Hash,
                new HashSet<string>(["audience"], StringComparer.Ordinal),
                new HashSet<string>(Enum.GetNames<ControllerOperationV2>(), StringComparer.Ordinal),
                new HashSet<string>([Rev869BCompatibilityManifestV2.SignatureAlgorithm], StringComparer.Ordinal),
                new HashSet<string>([Rev869BCompatibilityManifestV2.ContractVersion], StringComparer.Ordinal),
                Now.AddDays(-1),
                Now.AddDays(1),
                null));

        public ValueTask<SigningKeyMetadataV3?> ResolveKeyAsync(
            string issuerId,
            string keyId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<SigningKeyMetadataV3?>(new(
                issuerId,
                keyId,
                "TEST",
                Rev869BPhaseACompatibilityManifest.SignatureAlgorithm,
                "v1",
                Hash,
                Now.AddDays(-1),
                Now.AddDays(1),
                null));
    }

    private sealed class FixedAudiencePolicy : IAudiencePolicyProvider
    {
        public ValueTask<IReadOnlyList<AudienceOperationPolicyV3>> ResolveAsync(
            string audience,
            string operation,
            string subjectClass,
            CompanyDatabaseScopeV3 scope,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<AudienceOperationPolicyV3>>(
            [
                new(
                    "row-v1",
                    "policy-v1",
                    audience,
                    operation,
                    subjectClass,
                    "Operator",
                    "ORG:C1",
                    "REV869B_TARGET",
                    MasterScopeKindV3.COMPANY_LEDGER)
            ]);
    }

    private sealed class FixedAuthorizationResolver : ITrustedSubjectRoleScopeResolver
    {
        public ValueTask<ResolvedAuthorizationV3> ResolveAsync(
            AuthenticatedWorkloadIdentityV3 identity,
            UntrustedBusinessIntentV3 intent,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ResolvedAuthorizationV3(
                "authorization",
                identity.IssuerId,
                identity.SubjectId,
                identity.WorkloadIdentity,
                identity.TransportAudience,
                intent.Operation,
                "Operator",
                "ORG:C1",
                "policy-v1",
                "row-v1",
                Hash,
                Now.AddMinutes(-5),
                Now.AddMinutes(5)));
    }

    private sealed class FixedAlgorithmPolicy : IAlgorithmVersionPolicyProvider
    {
        public bool IsAllowed(
            string contractVersion,
            string canonicalizationVersion,
            string algorithm,
            string purpose) =>
            contractVersion == Rev869BCompatibilityManifestV2.ContractVersion &&
            canonicalizationVersion == Rev869BCompatibilityManifestV2.CanonicalizationVersion &&
            algorithm == Rev869BCompatibilityManifestV2.SignatureAlgorithm &&
            purpose == "CONTROL_COMMAND";
    }

    private sealed class FixedClockPolicy : IClockFreshnessPolicyProvider
    {
        public ClockFreshnessPolicyV3 Policy { get; } = new(
            "clock-v1",
            TimeSpan.FromMinutes(10),
            TimeSpan.FromSeconds(30),
            2);

        public TrustFailureCodeV2 Validate(
            DateTimeOffset issuedAt,
            DateTimeOffset notBefore,
            DateTimeOffset expiresAt,
            DateTimeOffset serverNow)
        {
            if (notBefore > serverNow + Policy.AllowedClockSkew) return TrustFailureCodeV2.NOT_YET_VALID;
            if (expiresAt < serverNow - Policy.AllowedClockSkew) return TrustFailureCodeV2.ENVELOPE_EXPIRED;
            if (expiresAt - issuedAt > Policy.MaximumEnvelopeLifetime) return TrustFailureCodeV2.ENVELOPE_EXPIRED;
            return TrustFailureCodeV2.NONE;
        }
    }

    private sealed class FixedLeaseAuthority : ILeaseFenceAuthority
    {
        public ValueTask<LeaseFenceExpectationV3?> ReadCurrentAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LeaseFenceExpectationV3?>(new(
                "lease", 1, 9, Now.AddMinutes(5), "subject", resourceId));

        public ValueTask<LeaseFenceExpectationV3> AcquireAsync(
            string resourceId,
            string holderSubject,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new LeaseFenceExpectationV3(
                "lease", 1, 9, expiresAt, holderSubject, resourceId));

        public ValueTask<LeaseFenceExpectationV3> RenewAsync(
            LeaseFenceExpectationV3 current,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(current with
            {
                FencingToken = current.FencingToken + 1,
                ExpiresAt = expiresAt
            });

        public ValueTask<TrustFailureCodeV2> ExpireAsync(
            LeaseFenceExpectationV3 expected,
            DateTimeOffset serverNow,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(
                expected.ExpiresAt < serverNow
                    ? TrustFailureCodeV2.NONE
                    : TrustFailureCodeV2.NOT_YET_VALID);
    }

    private sealed class FakeReaderRegistry(int maximumFacts = 10) : IAuthoritativeEvidenceReaderProvider
    {
        public ValueTask<AuthoritativeReaderDescriptorV3?> ResolveAsync(
            string readerId,
            string readerVersion,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AuthoritativeReaderDescriptorV3?>(new(
                readerId,
                readerVersion,
                Hash,
                $"reader:{readerId}",
                "reader-role",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                new HashSet<string>(["C1"], StringComparer.Ordinal),
                new HashSet<string>(["REV869B_TARGET"], StringComparer.Ordinal),
                new HashSet<string>(["status", "count", "password"], StringComparer.Ordinal),
                maximumFacts,
                32_768,
                EvidenceStageV3.DURABLE));

        public ValueTask<AuthoritativeFactBundleV3> ReadAsync(
            AuthoritativeReaderDescriptorV3 reader,
            EvidenceScopeTemporalBindingV3 binding,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Raw Phase-A evidence already contains the signed reader bundle.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    private sealed class EvidenceFixture
    {
        private EvidenceFixture(
            PhaseAAcceptanceVerifierAuthority authority,
            AuthoritativeFactBundleV3 bundle,
            byte[] canonicalEvidence,
            AuthenticatedWorkloadIdentityV3 transport,
            FakeKms kms,
            MutationSensitiveOracle oracle,
            CapturingAudit audit,
            CanonicalEvidenceEnvelopeV3 envelope)
        {
            Authority = authority;
            Bundle = bundle;
            CanonicalEvidence = canonicalEvidence;
            Transport = transport;
            Kms = kms;
            Oracle = oracle;
            Audit = audit;
            Envelope = envelope;
        }

        public PhaseAAcceptanceVerifierAuthority Authority { get; }
        public AuthoritativeFactBundleV3 Bundle { get; }
        public byte[] CanonicalEvidence { get; }
        public AuthenticatedWorkloadIdentityV3 Transport { get; }
        public FakeKms Kms { get; }
        public MutationSensitiveOracle Oracle { get; }
        public CapturingAudit Audit { get; }
        public CanonicalEvidenceEnvelopeV3 Envelope { get; }

        public byte[] WithBundle(AuthoritativeFactBundleV3 bundle)
        {
            var envelope = Envelope with
            {
                AuthoritativeBundles = [bundle],
                CanonicalEnvelopeSha256 = string.Empty
            };
            envelope = envelope with
            {
                CanonicalEnvelopeSha256 = PhaseAEvidenceCanonicalizer.EnvelopeSha256(envelope)
            };
            return CanonicalJsonV1.Serialize(envelope);
        }

        public static EvidenceFixture Create(
            int maximumFacts = 10,
            bool auditSucceeds = true,
            string? sensitiveField = null)
        {
            var options = new AcceptanceVerifierOptions
            {
                ServiceIdentity = "acceptance-verifier",
                IssuerId = "verifier-issuer",
                Audience = "verifier-audience",
                KeyId = "verifier-key",
                ContractVersion = Rev869BPhaseACompatibilityManifest.ContractVersion,
                EvidenceVersion = Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                CanonicalizationVersion = Rev869BPhaseACompatibilityManifest.CanonicalEnvelopeVersion,
                OwnershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
                ReadinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                EvidenceSchemaVersion = Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                OracleId = "oracle-1",
                OracleVersion = "1.0.0",
                OracleArtifactSha256 = Hash,
                AllowedCallerWorkloadIdentities = ["controller-workload"],
                RequiredReaderIds = ["reader-1"],
                AllowedClusterIds = ["cluster"],
                AllowedInstanceIds = ["instance"],
                AllowedFactFields = ["status", "count"],
                SensitiveFieldNames = ["password", "token", "private_key", "pan", "bank", "payroll"],
                MaximumEnvelopeBytes = PhaseAContractLimits.MaximumEvidenceEnvelopeBytes,
                MaximumObservations = 10,
                MaximumSelectors = 10,
                MaximumFactsPerObservation = 10,
                MaximumStringBytes = 1_024,
                MaximumCumulativeFactBytes = 32_768,
                MaximumClockSkewSeconds = 30,
                MaximumObservationWindowSeconds = 600
            };
            var binding = new EvidenceScopeTemporalBindingV3(
                new("C1", "cluster", "instance", MasterScopeKindV3.COMPANY_LEDGER),
                "VERIFY_ACCEPT",
                "request",
                "REV869B_TARGET",
                "resource",
                7,
                "attempt",
                "lease",
                9,
                EvidenceStageV3.DURABLE,
                "observation",
                Now.AddMinutes(-1),
                "snapshot");
            IReadOnlyList<RawEvidenceFactV3> facts = sensitiveField is null
                ? [new("status", SelectorValueKind.String, "complete")]
                : [new(sensitiveField, SelectorValueKind.String, "secret-value")];
            var bundle = new AuthoritativeFactBundleV3(
                "reader-1",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                Hash,
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                binding,
                facts,
                string.Empty,
                "reader-key",
                Rev869BPhaseACompatibilityManifest.SignatureAlgorithm,
                ReadOnlyMemory<byte>.Empty);
            var kms = new FakeKms();
            bundle = SignBundle(bundle, kms);
            var envelope = new CanonicalEvidenceEnvelopeV3(
                "evidence",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                options.OracleId,
                options.OracleVersion,
                options.OracleArtifactSha256,
                [bundle],
                string.Empty);
            envelope = envelope with
            {
                CanonicalEnvelopeSha256 = PhaseAEvidenceCanonicalizer.EnvelopeSha256(envelope)
            };
            var providers = Enum.GetValues<PhaseADependencyV3>()
                .Select(dependency => (IReadinessDependencyProvider)new ReadyProvider(dependency, Now))
                .ToArray();
            var oracle = new MutationSensitiveOracle(options);
            var audit = new CapturingAudit(auditSucceeds);
            var authority = new PhaseAAcceptanceVerifierAuthority(
                options,
                new PhaseAReadinessAuthority(
                    options.ReadinessPolicyVersion,
                    providers,
                    new FixedTimeProvider(Now)),
                oracle,
                new FakeReaderRegistry(maximumFacts),
                new FakeKeyRegistry(),
                kms,
                audit,
                new FixedTimeProvider(Now));
            return new(
                authority,
                bundle,
                CanonicalJsonV1.Serialize(envelope),
                new("controller-issuer", "controller", "controller-workload", options.Audience, Hash),
                kms,
                oracle,
                audit,
                envelope);
        }
    }

    private static AuthoritativeFactBundleV3 SignBundle(
        AuthoritativeFactBundleV3 bundle,
        FakeKms kms)
    {
        bundle = bundle with
        {
            FactsSha256 = PhaseAEvidenceCanonicalizer.FactPayloadSha256(bundle),
            Signature = ReadOnlyMemory<byte>.Empty
        };
        return bundle with
        {
            Signature = SHA256.HashData(PhaseAEvidenceCanonicalizer.FactPayload(bundle))
        };
    }

    private sealed class MutationSensitiveOracle(AcceptanceVerifierOptions options) : IOracleRegistryProvider
    {
        public int EvaluationCount { get; private set; }

        public ValueTask<OracleDescriptorV3?> ResolveAsync(
            string oracleId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<OracleDescriptorV3?>(new(
                options.OracleId,
                options.OracleVersion,
                options.OracleArtifactSha256,
                options.EvidenceSchemaVersion,
                options.ServiceIdentity,
                Now.AddDays(-1),
                null));

        public ValueTask<OracleEvaluationResultV3> EvaluateAsync(
            OracleDescriptorV3 oracle,
            IReadOnlyList<AuthoritativeFactBundleV3> authoritativeFacts,
            CancellationToken cancellationToken = default)
        {
            EvaluationCount++;
            var passed = authoritativeFacts
                .SelectMany(static bundle => bundle.Facts)
                .Any(static fact =>
                    fact.FieldId == "status" &&
                    fact.CanonicalValue == "complete");
            return ValueTask.FromResult(new OracleEvaluationResultV3(
                passed ? VerificationDisposition.Passed : VerificationDisposition.Failed,
                passed ? [] : [TrustFailureCodeV2.EVIDENCE_TAMPERED]));
        }
    }

    private sealed class CapturingAudit(bool succeeds) : IImmutableAuditEvidenceProvider
    {
        public int AppendCount { get; private set; }
        public ImmutableAuditEventV3? Event { get; private set; }

        public ValueTask<string> ReadCurrentChainHeadSha256Async(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Hash);

        public ValueTask<DurableAuditAppendReceiptV2> AppendAuditAsync(
            ImmutableAuditEventV3 auditEvent,
            CancellationToken cancellationToken = default)
        {
            AppendCount++;
            Event = auditEvent;
            return ValueTask.FromResult(
                succeeds
                    ? new DurableAuditAppendReceiptV2(auditEvent.EventId, "audit", Hash, Now)
                    : null!);
        }

        public ValueTask<DurableAuditAppendReceiptV2> AppendEvidenceAsync(
            string evidenceId,
            string sha256,
            ReadOnlyMemory<byte> canonicalEvidence,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(
                succeeds
                    ? new DurableAuditAppendReceiptV2(evidenceId, "evidence", Hash, Now)
                    : null!);
    }

    private sealed class ReadyProvider(
        PhaseADependencyV3 dependency,
        DateTimeOffset checkedAt) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new DependencyReadinessV3(
                Dependency,
                ReadinessDependencyStateV3.READY,
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                "READY",
                checkedAt,
                checkedAt.AddMinutes(1),
                Dependency.ToString(),
                Dependency.ToString()));
    }

    private sealed class ThrowingProvider(PhaseADependencyV3 dependency) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("unavailable");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("target-dotnet root not found.");
    }
}
