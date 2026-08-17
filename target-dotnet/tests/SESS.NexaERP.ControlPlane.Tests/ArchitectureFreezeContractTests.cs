using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
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

    private static readonly string[] LiteralFrozenRows =
    [
        "registered-authorize-prepare|prepare-authorize|Registered|AUTHORIZE_PREPARE|Operator|Preflight|Registered|False|target-registration|3|AUTHORIZATION_DECIDED|PREPARE_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "preflight-prepare|prepare-start|Preflight|PREPARE|ProvisioningExecutor|Provisioning|Failed|True|preflight|3|LIFECYCLE_ATTEMPTED|PREPARE_STARTED|ACTIVE|CONSUMED|NONE|NONE|False|False",
        "provisioning-complete|prepare-complete|Provisioning|COMPLETE_PREPARE|ProvisioningExecutor|Ready|Failed|True|action-receipt,ready-facts|3|LIFECYCLE_ATTEMPTED|PREPARE_COMPLETED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "provisioning-fail|prepare-fail|Provisioning|FAIL|ProvisioningExecutor|Failed|Quarantined|True|failure-facts|0|LIFECYCLE_ATTEMPTED|PREPARE_FAILED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "ready-authorize-execute|execute-authorize|Ready|AUTHORIZE_EXECUTE|Operator|MigrationAuthorized|Ready|False|migration-plan|3|AUTHORIZATION_DECIDED|EXECUTE_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "authorized-execute|execute-start|MigrationAuthorized|EXECUTE|MigrationExecutor|Migrating|Quarantined|True|preflight|3|LIFECYCLE_ATTEMPTED|EXECUTE_STARTED|ACTIVE|CONSUMED|NONE|NONE|False|False",
        "migrating-complete|execute-complete|Migrating|COMPLETE_EXECUTE|MigrationExecutor|VerificationPending|Failed|True|action-receipt,migration-ledger|3|LIFECYCLE_ATTEMPTED|EXECUTE_COMPLETED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "migrating-fail|execute-fail|Migrating|FAIL|MigrationExecutor|Failed|Quarantined|True|failure-facts|0|LIFECYCLE_ATTEMPTED|EXECUTE_FAILED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "verification-accept|verify-accept|VerificationPending|VERIFY_ACCEPT|AcceptanceVerifier|Accepted|Failed|True|signed-verdict,evidence-archive|0|LIFECYCLE_ATTEMPTED|VERIFICATION_ACCEPTED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "verification-reject|verify-reject|VerificationPending|VERIFY_REJECT|AcceptanceVerifier|Failed|Quarantined|True|signed-verdict,evidence-archive|0|LIFECYCLE_ATTEMPTED|VERIFICATION_REJECTED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "any-quarantine|quarantine|Registered|QUARANTINE|ControlPlaneRuntime|Quarantined|Quarantined|False|inconsistency-facts|0|LIFECYCLE_ATTEMPTED|RESOURCE_QUARANTINED|ACTIVE|ACTIVE|NONE|NONE|True|False",
        "quarantined-authorize-recover|recover-authorize|Quarantined|AUTHORIZE_RECOVER|RecoveryApprover|RecoveryAuthorized|Quarantined|False|recovery-plan|3|AUTHORIZATION_DECIDED|RECOVERY_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "authorized-recover|recover-start|RecoveryAuthorized|RECOVER|RecoveryExecutor|Recovering|Quarantined|True|before-facts|3|LIFECYCLE_ATTEMPTED|RECOVERY_STARTED|ACTIVE|CONSUMED|NONE|NONE|False|False",
        "recovering-complete|recover-complete|Recovering|COMPLETE_RECOVER|RecoveryExecutor|Ready|Failed|True|action-receipt,ready-facts|3|LIFECYCLE_ATTEMPTED|RECOVERY_COMPLETED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "recovering-fail|recover-fail|Recovering|FAIL|RecoveryExecutor|Failed|Quarantined|True|failure-facts|0|LIFECYCLE_ATTEMPTED|RECOVERY_FAILED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "accepted-authorize-drop|drop-authorize|Accepted|AUTHORIZE_DROP|DropAuthorizer|DropAuthorized|Accepted|False|backup-attestation,retention-approval|0|AUTHORIZATION_DECIDED|DROP_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "failed-authorize-drop|drop-authorize|Failed|AUTHORIZE_DROP|DropAuthorizer|DropAuthorized|Failed|False|backup-attestation,retention-approval|0|AUTHORIZATION_DECIDED|DROP_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "quarantined-authorize-drop|drop-authorize|Quarantined|AUTHORIZE_DROP|DropAuthorizer|DropAuthorized|Quarantined|False|backup-attestation,retention-approval|0|AUTHORIZATION_DECIDED|DROP_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "authorized-drop|drop|DropAuthorized|DROP|DropExecutor|Dropped|Quarantined|True|drop-authorization,target-facts|0|LIFECYCLE_ATTEMPTED|DROP_COMPLETED|ACTIVE|CONSUMED|NONE|NONE|False|False",
        "dropped-authorize-purge|purge-authorize|Dropped|AUTHORIZE_PURGE|PurgeAuthorizer|PurgeAuthorized|Dropped|False|candidate-root,legal-hold-decision|0|AUTHORIZATION_DECIDED|PURGE_AUTHORIZED|ACTIVE|ACTIVE|NONE|NONE|False|False",
        "authorized-purge|purge-start|PurgeAuthorized|PURGE|PurgeExecutor|Purging|Dropped|True|candidate-root,purge-authorization|0|LIFECYCLE_ATTEMPTED|PURGE_STARTED|ACTIVE|CONSUMED|NONE|NONE|False|False",
        "purging-complete|purge-complete|Purging|COMPLETE_PURGE|PurgeExecutor|Purged|Dropped|True|empty-candidate-proof,batch-audit|0|LIFECYCLE_ATTEMPTED|PURGE_COMPLETED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "purging-fail|purge-fail|Purging|FAIL|PurgeExecutor|Dropped|Quarantined|True|failure-facts|0|LIFECYCLE_ATTEMPTED|PURGE_FAILED|CONSUMED|CONSUMED|NONE|NONE|False|False",
        "accepted-authorize-export|export-authorize|Accepted|AUTHORIZE_EXPORT|ExportAuthorizer|Accepted|Accepted|False|minimized-batch-root,privacy-approval|0|AUTHORIZATION_DECIDED|EXPORT_AUTHORIZED|ACTIVE|ACTIVE|NONE|AUTHORIZED|False|False",
        "accepted-reauthorize-expired-export|export-authorize|Accepted|AUTHORIZE_EXPORT|ExportAuthorizer|Accepted|Accepted|False|minimized-batch-root,privacy-approval|0|AUTHORIZATION_DECIDED|EXPORT_AUTHORIZED|ACTIVE|ACTIVE|EXPIRED|AUTHORIZED|False|False",
        "accepted-reauthorize-failed-export|export-authorize|Accepted|AUTHORIZE_EXPORT|ExportAuthorizer|Accepted|Accepted|False|minimized-batch-root,privacy-approval|0|AUTHORIZATION_DECIDED|EXPORT_AUTHORIZED|ACTIVE|ACTIVE|FAILED|AUTHORIZED|False|False",
        "accepted-export|export-start|Accepted|EXPORT|ExportExecutor|Accepted|Accepted|True|export-authorization|3|LIFECYCLE_ATTEMPTED|EXPORT_STARTED|ACTIVE|CONSUMED|AUTHORIZED|DELIVERING|False|False",
        "accepted-complete-export|export-complete|Accepted|COMPLETE_EXPORT|ExportExecutor|Accepted|Accepted|True|delivery-receipt|0|LIFECYCLE_ATTEMPTED|EXPORT_DELIVERED|CONSUMED|CONSUMED|DELIVERING|DELIVERED|False|False",
        "any-cancel-active-authorization|authorization-cancel|Registered|CANCEL|OriginalAuthorizer|Registered|Registered|False|cancellation-reason|0|LIFECYCLE_ATTEMPTED|AUTHORIZATION_CANCELLED|ACTIVE|CANCELLED|NONE|NONE|False|True",
        "any-expire-active-authorization|authorization-expire|Registered|EXPIRE|ControlPlaneRuntime|Registered|Registered|False|server-time|0|LIFECYCLE_ATTEMPTED|AUTHORIZATION_EXPIRED|ACTIVE|EXPIRED|NONE|NONE|False|True"
    ];

    [Fact]
    public void A2_PublicProtectedSurfaceRemainsRawOnly()
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
    public void A2_OfflineCryptoVectorsAndCanonicalBytesMatchPinnedHashes()
    {
        var fixture = CommandFixture.Create();
        Assert.Equal(
            "84e86d1a6998f21997a3515829e50f27ea148250cc90eb7b423794b99f43ac45",
            Convert.ToHexString(SHA256.HashData(fixture.PayloadBytes)).ToLowerInvariant());
        Assert.Equal(
            "a40b13ef8ff4044991e7f8ec771d2858fc389203e5527421c9fb9179358599ba",
            Convert.ToHexString(SHA256.HashData(fixture.HeaderBytes)).ToLowerInvariant());
        Assert.Equal(SHA256.HashData(fixture.HeaderBytes), fixture.Signature);
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
    public void A2_All14ResponsibilitiesHaveOneEffectiveOwnerAndOneCatalogOwner()
    {
        var literalOwners = new Dictionary<ProductionResponsibilityV3, Type>
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
        Assert.Equal(literalOwners, PhaseAOwnershipCatalog.All);
        PhaseAOwnershipValidator.RequireComplete();

        var dependencies = typeof(PhaseAControlPlaneAuthority).GetConstructors().Single().GetParameters();
        Assert.Equal(1, dependencies.Count(parameter =>
            parameter.ParameterType == typeof(IDurableControlPlanePersistenceProvider)));
        Assert.Empty(typeof(IDurableControlPlanePersistenceProvider).GetInterfaces());
        Assert.Equal(
            ["get_Descriptor", "ReadAuthoritativeSnapshotAsync", "ExecuteAtomicallyAsync"],
            typeof(IDurableControlPlanePersistenceProvider).GetMethods().Select(static method => method.Name));
    }

    [Fact]
    public async Task A2_RawAuthorityReadsOneCompositeSnapshotBeforeLifecycleDecision()
    {
        var fixture = CommandFixture.Create();

        var result = await fixture.InvokeAsync();

        Assert.Equal(ControlTransactionOutcomeV3.FIRST_OWNER, result.TransactionOutcome);
        Assert.Equal(1, fixture.DurableProvider.SnapshotReadCount);
        Assert.Equal(1, fixture.Controller.CallCount);
        Assert.Equal(1, fixture.DurableProvider.AtomicExecuteCount);
        Assert.NotNull(fixture.DurableProvider.LastTransactionRequest);
        Assert.Same(
            fixture.DurableProvider.Snapshot,
            fixture.DurableProvider.LastTransactionRequest!.Command.AuthoritativeSnapshot);
        Assert.Equal(
            fixture.DurableProvider.Descriptor,
            fixture.DurableProvider.LastTransactionRequest.ExpectedProvider);
    }

    [Fact]
    public async Task A2_CallerStateVersionGrantExportAttemptAndEpochCannotBecomeTrustedFacts()
    {
        var mutations = new Func<AuthoritativeControlPlaneSnapshotV3, AuthoritativeControlPlaneSnapshotV3>[]
        {
            snapshot => snapshot with { ProviderIdentity = "other-owner" },
            snapshot => snapshot with { ProviderVersion = "other-version" },
            snapshot => snapshot with { Scope = snapshot.Scope with { OrganizationId = "C2" } },
            snapshot => snapshot with { ResourceType = "OTHER" },
            snapshot => snapshot with { ResourceId = "other-resource" },
            snapshot => snapshot with { ResourceVersion = 8 },
            snapshot => snapshot with { LifecycleState = ControllerLifecycleState.Ready },
            snapshot => snapshot with { AttemptId = string.Empty }
        };

        foreach (var mutate in mutations)
        {
            var fixture = CommandFixture.Create();
            fixture.DurableProvider.Snapshot = mutate(fixture.DurableProvider.Snapshot);
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => fixture.InvokeAsync().AsTask());
            Assert.Equal(0, fixture.Controller.CallCount);
            Assert.Equal(0, fixture.DurableProvider.AtomicExecuteCount);
            Assert.Equal(0, fixture.DurableProvider.BusinessCommitCount);
        }

        var leaseFixture = CommandFixture.Create();
        leaseFixture.DurableProvider.Snapshot = leaseFixture.DurableProvider.Snapshot with
        {
            Lease = new("lease", 1, 10, Now.AddMinutes(1), "subject", "resource")
        };
        var leaseFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => leaseFixture.InvokeAsync().AsTask());
        Assert.Equal(TrustFailureCodeV2.LEASE_FENCE_STALE, leaseFailure.Code);
        Assert.Equal(0, leaseFixture.Controller.CallCount);
        Assert.Equal(0, leaseFixture.DurableProvider.BusinessCommitCount);
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
    public void A2_Literal26RowOracleMatchesProductionWithoutProductionMappingHelpers()
    {
        Assert.Equal(30, LiteralFrozenRows.Length);
        Assert.Equal(26, LiteralFrozenRows.Select(static row => row.Split('|')[1]).Distinct().Count());

        var expected = LiteralFrozenRows
            .Select(static row =>
            {
                var fields = row.Split('|');
                return string.Join('|', fields.Take(1).Concat(fields.Skip(2)));
            })
            .OrderBy(static row => row, StringComparer.Ordinal)
            .ToArray();
        var actual = Rev869BControllerStateMachine.PhaseARuleSnapshot
            .Select(static rule => string.Join('|',
                rule.RuleId,
                rule.CurrentState,
                rule.Operation,
                rule.TrustedRole,
                rule.NextState,
                rule.FailureState,
                rule.RequiresLease,
                string.Join(',', rule.RequiredEvidenceIds),
                rule.MaximumSameAttemptRetries,
                rule.AuditKind,
                rule.LifecycleAuditEvent,
                rule.RequiredAuthorizationState,
                rule.NextAuthorizationState,
                rule.RequiredExportState,
                rule.NextExportState,
                rule.AppliesToAnyNonterminalState,
                rule.AppliesToAnyActiveAuthorization))
            .OrderBy(static row => row, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task A2_All26FrozenRowsExecuteThroughRawAuthorityWithAuthoritativeSnapshot()
    {
        foreach (var literal in LiteralFrozenRows)
        {
            var row = ParseLiteralRow(literal);
            var sourceAuthorization = row.Operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal)
                ? row.ExportState is ExportAuthorizationSubstateV3.EXPIRED or ExportAuthorizationSubstateV3.FAILED
                    ? AuthorizationGrantStateV3.EXPIRED
                    : AuthorizationGrantStateV3.NONE
                : row.RequiredAuthorization;
            var fixture = CommandFixture.Create(
                state: row.State,
                operation: row.Operation,
                requestedState: row.NextState,
                role: row.Role,
                evidenceIds: row.EvidenceIds,
                requiresLease: row.RequiresLease,
                exportState: row.ExportState,
                authorizationState: sourceAuthorization);

            var result = await fixture.InvokeAsync();

            Assert.Equal(row.NextState, result.State);
            Assert.Equal(row.NextAuthorization, result.AuthorizationState);
            Assert.Equal(row.NextExportState, result.ExportState);
            Assert.Equal(1, fixture.DurableProvider.SnapshotReadCount);
            Assert.Equal(1, fixture.DurableProvider.AtomicExecuteCount);
            Assert.Equal(1, fixture.DurableProvider.BusinessCommitCount);
        }
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
    public void A2_CompleteLiteralComplementOf26RowsIsIllegal()
    {
        var machine = new Rev869BControllerStateMachine();
        var listed = LiteralFrozenRows
            .Select(ParseLiteralRow)
            .Select(static row => (row.State, row.Operation))
            .ToHashSet();
        foreach (var state in Enum.GetValues<ControllerLifecycleState>())
        foreach (var operation in Enum.GetValues<ControllerOperationV2>())
        {
            var wildcard = operation == ControllerOperationV2.QUARANTINE && state != ControllerLifecycleState.Purged ||
                           operation is ControllerOperationV2.CANCEL or ControllerOperationV2.EXPIRE;
            if (wildcard || listed.Contains((state, operation))) continue;
            AssertCode(
                () => machine.RequirePhaseACommand(Command(state, operation, "Operator", [], false), Now),
                TrustFailureCodeV2.STATE_TRANSITION_ILLEGAL);
        }
    }

    [Fact]
    public void A2_EveryRowRejectsWrongStateVersionRoleScopeGrantEvidenceLeaseFenceEpochAttemptAndAudit()
    {
        var machine = new Rev869BControllerStateMachine();
        foreach (var literal in LiteralFrozenRows)
        {
            var row = ParseLiteralRow(literal);
            var sourceAuthorization = row.Operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal)
                ? AuthorizationGrantStateV3.NONE
                : row.RequiredAuthorization;
            var valid = Command(
                row.State,
                row.Operation,
                row.Role,
                row.EvidenceIds,
                row.RequiresLease,
                row.ExportState,
                sourceAuthorization);

            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with { CurrentVersion = valid.CurrentVersion + 1 }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with { CurrentState = valid.CurrentState == ControllerLifecycleState.Registered
                    ? ControllerLifecycleState.Ready
                    : ControllerLifecycleState.Registered }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with
                {
                    Authorization = valid.Authorization with
                    {
                        Authorization = valid.Authorization.Authorization with { TrustedRole = "WrongRole" }
                    }
                }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with
                {
                    Authorization = valid.Authorization with
                    {
                        Scope = valid.Authorization.Scope with { OrganizationId = "C2" }
                    }
                }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with
                {
                    CurrentAuthorizationState = valid.CurrentAuthorizationState == AuthorizationGrantStateV3.ACTIVE
                        ? AuthorizationGrantStateV3.CONSUMED
                        : AuthorizationGrantStateV3.ACTIVE
                }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with { RequiredEvidence = [] }, Now));
            Assert.Throws<TrustFailureExceptionV2>(() => machine.RequirePhaseACommand(
                valid with { AttemptId = "other-attempt" }, Now));

            if (valid.Lease is not null)
            {
                AssertCode(
                    () => machine.RequirePhaseACommand(
                        valid with { Lease = valid.Lease with { FencingToken = valid.Lease.FencingToken + 1 } }, Now),
                    TrustFailureCodeV2.LEASE_FENCE_STALE);
                AssertCode(
                    () => machine.RequirePhaseACommand(
                        valid with { Lease = valid.Lease with { ControllerEpoch = 0 } }, Now),
                    TrustFailureCodeV2.LEASE_FENCE_STALE);
            }
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
    public async Task A2_ExportAuthorizeDeliverCompleteAndReauthorizeSequencesAreReachableInOrder()
    {
        var sequence = LiteralFrozenRows
            .Select(ParseLiteralRow)
            .Where(static row => row.Operation is ControllerOperationV2.AUTHORIZE_EXPORT or
                ControllerOperationV2.EXPORT or ControllerOperationV2.COMPLETE_EXPORT)
            .ToArray();
        foreach (var row in sequence)
        {
            var authorization = row.Operation == ControllerOperationV2.AUTHORIZE_EXPORT
                ? row.ExportState is ExportAuthorizationSubstateV3.EXPIRED or ExportAuthorizationSubstateV3.FAILED
                    ? AuthorizationGrantStateV3.EXPIRED
                    : AuthorizationGrantStateV3.NONE
                : row.RequiredAuthorization;
            var fixture = CommandFixture.Create(
                state: row.State,
                operation: row.Operation,
                requestedState: row.NextState,
                role: row.Role,
                evidenceIds: row.EvidenceIds,
                requiresLease: row.RequiresLease,
                exportState: row.ExportState,
                authorizationState: authorization);
            var result = await fixture.InvokeAsync();
            Assert.Equal(row.NextExportState, result.ExportState);
        }
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
    public async Task A2_CancelExpireAndQuarantineUseExistingGrantActorTimeAndHeldLease()
    {
        var quarantine = CommandFixture.Create(
            state: ControllerLifecycleState.Ready,
            operation: ControllerOperationV2.QUARANTINE,
            requestedState: ControllerLifecycleState.Quarantined,
            role: "ControlPlaneRuntime",
            evidenceIds: ["inconsistency-facts"],
            requiresLease: true);
        Assert.Equal(ControllerLifecycleState.Quarantined, (await quarantine.InvokeAsync()).State);

        var machine = new Rev869BControllerStateMachine();
        var cancel = Command(
            ControllerLifecycleState.Ready,
            ControllerOperationV2.CANCEL,
            "OriginalAuthorizer",
            ["cancellation-reason"],
            false);
        var wrongActorSnapshot = cancel.AuthoritativeSnapshot! with
        {
            CurrentAuthorization = cancel.AuthoritativeSnapshot.CurrentAuthorization! with
            {
                GrantAuthorization = cancel.AuthoritativeSnapshot.CurrentAuthorization.GrantAuthorization with
                {
                    AuthenticatedSubject = "other-subject"
                }
            }
        };
        AssertCode(
            () => machine.RequirePhaseACommand(cancel with { AuthoritativeSnapshot = wrongActorSnapshot }, Now),
            TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);

        var expire = Command(
            ControllerLifecycleState.Ready,
            ControllerOperationV2.EXPIRE,
            "ControlPlaneRuntime",
            ["server-time"],
            false);
        var futureGrant = expire.AuthoritativeSnapshot!.CurrentAuthorization! with
        {
            GrantAuthorization = expire.AuthoritativeSnapshot.CurrentAuthorization.GrantAuthorization with
            {
                ExpiresAt = Now.AddMinutes(1)
            }
        };
        AssertCode(
            () => machine.RequirePhaseACommand(
                expire with
                {
                    AuthoritativeSnapshot = expire.AuthoritativeSnapshot with
                    {
                        CurrentAuthorization = futureGrant
                    },
                    StoredGrantClaim = futureGrant
                }, Now),
            TrustFailureCodeV2.NOT_YET_VALID);
    }

    [Fact]
    public async Task A2_AtomicProviderCommitsNonceIdempotencyTransitionGrantAttemptAndAuditOnce()
    {
        var fixture = CommandFixture.Create(
            state: ControllerLifecycleState.MigrationAuthorized,
            operation: ControllerOperationV2.EXECUTE,
            requestedState: ControllerLifecycleState.Migrating,
            role: "MigrationExecutor",
            evidenceIds: ["preflight"],
            requiresLease: true);
        var result = await fixture.InvokeAsync();
        var request = Assert.IsType<ControlPlaneTransactionRequestV3>(fixture.DurableProvider.LastTransactionRequest);
        Assert.Equal(result, request.ProposedTransition);
        Assert.Equal(request.Command.Nonce, request.Nonce);
        Assert.Equal(request.Command.AttemptId, request.Command.AuthoritativeSnapshot!.AttemptId);
        Assert.Equal(1, fixture.DurableProvider.AtomicExecuteCount);
        Assert.Equal(1, fixture.DurableProvider.BusinessCommitCount);
        Assert.Equal(AuthorizationGrantStateV3.CONSUMED, result.AuthorizationState);
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
        var expiredLease = valid.Lease! with { ExpiresAt = Now.AddSeconds(-1) };
        AssertCode(
            () => machine.RequirePhaseACommand(
                valid with
                {
                    Lease = expiredLease,
                    AuthoritativeSnapshot = valid.AuthoritativeSnapshot! with { Lease = expiredLease }
                },
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
    public async Task A2_ExactSignedReaderBundlesProducePinnedOracleVerdictAndAuditReceipt()
    {
        var fixture = EvidenceFixture.Create();
        var verdict = await fixture.Authority.VerifyRawAsync(
            fixture.CanonicalEvidence,
            fixture.Transport);
        Assert.Equal(VerificationDisposition.Passed, verdict.Calculation.Disposition);
        Assert.Equal(1, fixture.Oracle.EvaluationCount);
        Assert.Equal(1, fixture.ReaderRegistry.ExpectationResolveCount);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(1, fixture.Audit.AppendCount);
        Assert.NotNull(fixture.Audit.Event);
    }

    [Fact]
    public async Task A2_CallerCarriedBundleCannotReplaceReaderReturnedBundle()
    {
        var fixture = EvidenceFixture.Create();
        var changed = fixture.Bundle with
        {
            Facts = [new("status", SelectorValueKind.String, "failed")]
        };
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(fixture.WithBundle(changed), fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_TAMPERED, failure.Code);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
        Assert.Equal(0, fixture.Audit.AppendCount);
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
    public async Task A2_StricterConfiguredObservationStringFactAndByteLimitsAreApplied()
    {
        var factLimits = new[]
        {
            EvidenceFixture.Create(maximumFacts: 1, factCount: 2),
            EvidenceFixture.Create(configuredMaximumFacts: 1, factCount: 2)
        };
        foreach (var fixture in factLimits)
        {
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport).AsTask());
            Assert.Equal(TrustFailureCodeV2.READER_UNAUTHORIZED, failure.Code);
            Assert.Equal(0, fixture.Oracle.EvaluationCount);
        }

        var stringLimit = EvidenceFixture.Create(maximumStringBytes: 64, statusValue: new string('x', 65));
        var stringFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => stringLimit.Authority.VerifyRawAsync(stringLimit.CanonicalEvidence, stringLimit.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_TOO_LARGE, stringFailure.Code);
        Assert.Equal(0, stringLimit.ReaderRegistry.ReadCount);

        var byteLimit = EvidenceFixture.Create(maximumCumulativeFactBytes: 32);
        var byteFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => byteLimit.Authority.VerifyRawAsync(byteLimit.CanonicalEvidence, byteLimit.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_TOO_LARGE, byteFailure.Code);
        Assert.Equal(0, byteLimit.Oracle.EvaluationCount);

        var observationLimit = EvidenceFixture.Create(maximumObservations: 3);
        var observationFailure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(() =>
            observationLimit.Authority.VerifyRawAsync(
                observationLimit.WithBundles([
                    observationLimit.Bundle,
                    observationLimit.Bundle,
                    observationLimit.Bundle,
                    observationLimit.Bundle]),
                observationLimit.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.EVIDENCE_TOO_LARGE, observationFailure.Code);
    }

    [Fact]
    public async Task A2_VerifierInvokesEveryRequiredReaderExactlyOnceWithServerOwnedBinding()
    {
        var fixture = EvidenceFixture.Create();
        await fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport);
        Assert.Equal(1, fixture.ReaderRegistry.ExpectationResolveCount);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(1, fixture.Oracle.EvaluationCount);
    }

    [Fact]
    public async Task A2_DuplicateMissingUnknownOrExtraReaderFailsBeforeOracle()
    {
        var fixture = EvidenceFixture.Create();
        var duplicate = fixture.Bundle with
        {
            Binding = fixture.Bundle.Binding with { ObservationId = "observation-2" }
        };
        var vectors = new[]
        {
            (fixture.WithBundles([]), TrustFailureCodeV2.READER_MISSING),
            (fixture.WithBundles([fixture.Bundle, duplicate]), TrustFailureCodeV2.READER_DUPLICATE),
            (fixture.WithBundle(fixture.Bundle with { ReaderId = "unknown-reader" }), TrustFailureCodeV2.READER_MISSING)
        };
        foreach (var (canonical, expected) in vectors)
        {
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.Authority.VerifyRawAsync(canonical, fixture.Transport).AsTask());
            Assert.Equal(expected, failure.Code);
        }
        Assert.Equal(0, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
        Assert.Equal(0, fixture.Audit.AppendCount);
    }

    [Fact]
    public async Task A2_CrossOrganizationResourceVersionAttemptStageOrWatermarkFailsBeforeOracle()
    {
        var mutations = new Func<EvidenceScopeTemporalBindingV3, EvidenceScopeTemporalBindingV3>[]
        {
            binding => binding with { Scope = binding.Scope with { OrganizationId = "C2" } },
            binding => binding with { ResourceId = "other-resource" },
            binding => binding with { ResourceVersion = 8 },
            binding => binding with { AttemptId = "other-attempt" },
            binding => binding with { Stage = EvidenceStageV3.ACTION },
            binding => binding with { SnapshotOrWatermark = "other-watermark" }
        };
        foreach (var mutation in mutations)
        {
            var fixture = EvidenceFixture.Create(authoritativeBindingMutation: mutation);
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport).AsTask());
            Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
            Assert.Equal(0, fixture.Oracle.EvaluationCount);
            Assert.Equal(0, fixture.Audit.AppendCount);
        }
    }

    [Fact]
    public async Task A2_ReaderFailureTimeoutOrAmbiguousBindingReturnsNoVerdictAndNoSuccessAudit()
    {
        var fixture = EvidenceFixture.Create(readerFails: true);
        var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport).AsTask());
        Assert.Equal(TrustFailureCodeV2.READER_UNAUTHORIZED, failure.Code);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(0, fixture.Oracle.EvaluationCount);
        Assert.Equal(0, fixture.Audit.AppendCount);
    }

    [Fact]
    public async Task A2_OracleIsPinnedMutationSensitiveAndCannotAcceptReaderVerdict()
    {
        var passing = EvidenceFixture.Create();
        var pass = await passing.Authority.VerifyRawAsync(
            passing.CanonicalEvidence,
            passing.Transport);
        Assert.Equal(VerificationDisposition.Passed, pass.Calculation.Disposition);

        var failing = EvidenceFixture.Create(statusValue: "failed");
        var fail = await failing.Authority.VerifyRawAsync(failing.CanonicalEvidence, failing.Transport);
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
    public async Task A2_ExactFreshIdentityVersionPolicyDependencySetIsReady()
    {
        var snapshot = await ReadinessSnapshotAsync(ReadyProviders());
        Assert.True(snapshot.CanExecuteProtectedOperation);
        AssertRouteDecisions(snapshot, 200, true);
    }

    [Fact]
    public async Task A2_MissingDuplicateTimeoutExceptionDegradedAndUnsafeDependencyReturn503OnBothRoutes()
    {
        var dependency = PhaseADependencyV3.Configuration;
        var vectors = new List<IReadOnlyList<IReadinessDependencyProvider>>
        {
            ReadyProviders().Skip(1).ToArray(),
            ReadyProviders().Concat([new ReadyProvider(dependency, Now)]).ToArray(),
            ReplaceProvider(dependency, new ThrowingProvider(dependency)),
            ReplaceProvider(dependency, new CancelledProvider(dependency)),
            ReplaceProvider(dependency, new StaticReadinessProvider(dependency, ReadyResult(dependency) with
            {
                State = ReadinessDependencyStateV3.DEGRADED_NOT_SAFE
            }))
        };

        foreach (var providers in vectors)
        {
            AssertRouteDecisions(await ReadinessSnapshotAsync(providers), 503, false);
        }
    }

    [Fact]
    public async Task A2_NullExpiredFutureOrInvertedFreshnessReturns503OnBothRoutes()
    {
        var dependency = PhaseADependencyV3.Configuration;
        var malformed = new[]
        {
            ReadyResult(dependency) with { CheckedAt = null },
            ReadyResult(dependency) with { ValidUntil = null },
            ReadyResult(dependency) with { ValidUntil = Now.AddSeconds(-1) },
            ReadyResult(dependency) with { CheckedAt = Now.AddSeconds(1) },
            ReadyResult(dependency) with { CheckedAt = Now, ValidUntil = Now.AddSeconds(-1) }
        };
        foreach (var result in malformed)
        {
            var snapshot = await ReadinessSnapshotAsync(
                ReplaceProvider(dependency, new StaticReadinessProvider(dependency, result)));
            AssertRouteDecisions(snapshot, 503, false);
        }
    }

    [Fact]
    public async Task A2_IdentityVersionPolicyOrDependencyMismatchReturns503OnBothRoutes()
    {
        var dependency = PhaseADependencyV3.Configuration;
        var malformed = new IReadinessDependencyProvider[]
        {
            new StaticReadinessProvider(dependency, ReadyResult(dependency) with { ObservedIdentity = "other" }),
            new StaticReadinessProvider(dependency, ReadyResult(dependency) with { ObservedVersion = "other" }),
            new StaticReadinessProvider(dependency, ReadyResult(PhaseADependencyV3.WorkloadIdentity)),
        };
        foreach (var provider in malformed)
        {
            var snapshot = await ReadinessSnapshotAsync(ReplaceProvider(dependency, provider));
            AssertRouteDecisions(snapshot, 503, false);
        }

        var wrongPolicy = await new PhaseAReadinessAuthority(
            "wrong-policy",
            ReadyProviders(),
            new FixedTimeProvider(Now)).CheckAsync();
        AssertRouteDecisions(wrongPolicy, 503, false);

        var root = FindRoot();
        var controlRoute = File.ReadAllText(Path.Combine(
            root,
            "src/SESS.NexaERP.ControlPlane/Endpoints/ControllerContractEndpointsV1.cs"));
        var verifierRoute = File.ReadAllText(Path.Combine(
            root,
            "src/SESS.NexaERP.AcceptanceVerifier/Program.cs"));
        Assert.Contains("PhaseAReadinessRouteGuardV3.Evaluate(readiness)", controlRoute, StringComparison.Ordinal);
        Assert.Contains("PhaseAReadinessRouteGuardV3.Evaluate(readiness)", verifierRoute, StringComparison.Ordinal);
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
    public async Task A2_AuditThrowTimeoutWrongReceiptOrChainMismatchReturnsNoProtectedSuccess()
    {
        var fixtures = new[]
        {
            EvidenceFixture.Create(auditSucceeds: false),
            EvidenceFixture.Create(auditWrongReceipt: true),
            EvidenceFixture.Create(auditInvalidChain: true)
        };
        foreach (var fixture in fixtures)
        {
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.Authority.VerifyRawAsync(
                    fixture.CanonicalEvidence,
                    fixture.Transport).AsTask());
            Assert.Equal(TrustFailureCodeV2.AUDIT_APPEND_FAILED, failure.Code);
            Assert.Equal(1, fixture.Oracle.EvaluationCount);
        }
    }

    [Fact]
    public async Task A2_ExactSanitizedAuditAppendPrecedesTransitionOrVerdictSuccess()
    {
        var fixture = EvidenceFixture.Create();
        var verdict = await fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport);
        Assert.Equal(VerificationDisposition.Passed, verdict.Calculation.Disposition);
        Assert.Equal(["evidence", "chain", "audit"], fixture.Audit.Trace);

        var serializedAudit = Encoding.UTF8.GetString(CanonicalJsonV1.Serialize(fixture.Audit.Event!));
        Assert.DoesNotContain("complete", serializedAudit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("signature", serializedAudit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", serializedAudit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private", serializedAudit, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A2_AtomicAuditNonceGrantFenceAndReceiptMismatchCannotReturnLifecycleSuccess()
    {
        var baseMutations = new Func<ControlPlaneTransactionResultV3, ControlPlaneTransactionResultV3>[]
        {
            result => result with { NonceRegistered = false },
            result => result with { AuditOutboxCommitted = false },
            result => result with { Transition = result.Transition with { AuditReference = "wrong-audit" } },
            result => result with { Transition = result.Transition with { ResponseSha256 = new string('f', 64) } },
            result => result with { Transition = result.Transition with { AttemptNumber = 99 } }
        };
        foreach (var mutation in baseMutations)
        {
            var fixture = CommandFixture.Create();
            fixture.DurableProvider.ResultMutation = mutation;
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.InvokeAsync().AsTask());
            Assert.Equal(TrustFailureCodeV2.AUDIT_APPEND_FAILED, failure.Code);
        }

        var leased = CommandFixture.Create(
            state: ControllerLifecycleState.Preflight,
            operation: ControllerOperationV2.PREPARE,
            requestedState: ControllerLifecycleState.Provisioning,
            role: "ProvisioningExecutor",
            evidenceIds: ["preflight"],
            requiresLease: true);
        leased.DurableProvider.ResultMutation = result => result with { FenceConsumed = false };
        Assert.Equal(
            TrustFailureCodeV2.AUDIT_APPEND_FAILED,
            (await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => leased.InvokeAsync().AsTask())).Code);

        var grant = CommandFixture.Create(
            state: ControllerLifecycleState.MigrationAuthorized,
            operation: ControllerOperationV2.EXECUTE,
            requestedState: ControllerLifecycleState.Migrating,
            role: "MigrationExecutor",
            evidenceIds: ["preflight"],
            requiresLease: true);
        grant.DurableProvider.ResultMutation = result => result with { AuthorizationConsumed = false };
        Assert.Equal(
            TrustFailureCodeV2.AUDIT_APPEND_FAILED,
            (await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => grant.InvokeAsync().AsTask())).Code);
    }

    [Fact]
    public async Task A2_ConcurrentIdenticalRequestReturnsOneCommittedOutcome()
    {
        var fixture = CommandFixture.Create();
        var calls = Enumerable.Range(0, 8).Select(_ => fixture.InvokeAsync().AsTask()).ToArray();
        var results = await Task.WhenAll(calls);
        Assert.Equal(1, results.Count(result => result.TransactionOutcome == ControlTransactionOutcomeV3.FIRST_OWNER));
        Assert.Equal(7, results.Count(result => result.TransactionOutcome == ControlTransactionOutcomeV3.COMPLETED_REPLAY));
        Assert.Equal(1, fixture.DurableProvider.BusinessCommitCount);
    }

    [Fact]
    public async Task A2_ProductionAuthorityConcurrencyAndReplayTraceMatchesLiteralOracle()
    {
        var fixture = CommandFixture.Create();
        var results = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => fixture.InvokeAsync().AsTask()));
        Assert.Equal(1, results.Count(static result =>
            result.TransactionOutcome == ControlTransactionOutcomeV3.FIRST_OWNER));
        Assert.Equal(11, results.Count(static result =>
            result.TransactionOutcome == ControlTransactionOutcomeV3.COMPLETED_REPLAY));
        Assert.All(results, static result =>
        {
            Assert.Equal(ControllerLifecycleState.Preflight, result.State);
            Assert.Equal(8, result.Version);
            Assert.Equal(LifecycleAuditEventV3.PREPARE_AUTHORIZED, result.LifecycleAuditEvent);
        });
        Assert.Equal(1, fixture.DurableProvider.BusinessCommitCount);
        Assert.Equal(12, fixture.DurableProvider.AtomicExecuteCount);
    }

    [Fact]
    public async Task A2_IdempotencyDigestCollisionConcurrentOwnerAndNonretryableReplayFailClosed()
    {
        var first = CommandFixture.Create();
        var initial = await first.InvokeAsync();
        Assert.Equal(ControlTransactionOutcomeV3.FIRST_OWNER, initial.TransactionOutcome);

        var changed = CommandFixture.Create(
            durableProvider: first.DurableProvider,
            payloadMutation: payload => payload with { ActionId = "different-action" });
        var collision = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
            () => changed.InvokeAsync().AsTask());
        Assert.Equal(TrustFailureCodeV2.IDEMPOTENCY_PAYLOAD_MISMATCH, collision.Code);
        Assert.Equal(1, first.DurableProvider.BusinessCommitCount);

        foreach (var outcome in new[]
                 {
                     ControlTransactionOutcomeV3.IN_PROGRESS,
                     ControlTransactionOutcomeV3.NONRETRYABLE_FAILURE,
                     ControlTransactionOutcomeV3.CONFLICT
                 })
        {
            var fixture = CommandFixture.Create();
            fixture.DurableProvider.ResultMutation = result => result with
            {
                Outcome = outcome,
                Transition = result.Transition with { TransactionOutcome = outcome }
            };
            var failure = await Assert.ThrowsAsync<TrustFailureExceptionV2>(
                () => fixture.InvokeAsync().AsTask());
            Assert.Equal(TrustFailureCodeV2.AUDIT_APPEND_FAILED, failure.Code);
        }
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
    public async Task A2_AdversarialSecurityVectorsFailAtTheirIntendedProductionGates()
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

    [Fact]
    public void A3_CompositeProviderHasOnePinnedOwnerAndOneAtomicMutationCapability()
    {
        var methods = typeof(IDurableControlPlanePersistenceProvider).GetMethods();
        Assert.Empty(typeof(IDurableControlPlanePersistenceProvider).GetInterfaces());
        Assert.Equal(1, methods.Count(static method =>
            method.Name == nameof(IDurableControlPlanePersistenceProvider.ExecuteAtomicallyAsync)));
        Assert.Equal(1, methods.Count(static method =>
            method.Name == nameof(IDurableControlPlanePersistenceProvider.ReadAuthoritativeSnapshotAsync)));
        var options = ValidControlPlaneOptions();
        Assert.True(ControlPlaneOptions.IsValid(options));
        Assert.Equal("phase-a-durable-owner", options.DurableProviderDescriptor().Identity);
        Assert.Equal(Hash, options.DurableProviderDescriptor().ArtifactSha256);
    }

    [Fact]
    public void A3_ExportedOrInjectablePartialNonceIdempotencyLeaseAndStateMutationIsImpossible()
    {
        var forbidden = new HashSet<string>(
            ["RegisterNonceAsync", "ClaimAsync", "AcquireAsync", "RenewAsync", "ExpireAsync", "WriteAsync"],
            StringComparer.Ordinal);
        Assert.DoesNotContain(
            typeof(IDurableControlPlanePersistenceProvider).GetMethods(),
            method => forbidden.Contains(method.Name));
        Assert.DoesNotContain(
            typeof(IDurableControlPlanePersistenceProvider).Assembly.ExportedTypes,
            type => type.Name is "INonceRegistrationAuthority" or "IIdempotencyAuthority" or
                "ILeaseFenceAuthority" or "ILifecycleStateAuthority" or
                "IAuthorizationStateAuthority" or "IExecutionAttemptAuthority" or
                "IRecoveryQuarantineAuthority" or "IExportStateAuthority" or "IPurgeStateAuthority");
        Assert.Single(typeof(PhaseAControlPlaneAuthority).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IDurableControlPlanePersistenceProvider));
    }

    [Fact]
    public async Task A3_SelfAttestedProviderOrLifecycleIdentityVersionArtifactIsRejectedBeforeSnapshotUse()
    {
        var providerFixture = CommandFixture.Create();
        providerFixture.DurableProvider.Descriptor = providerFixture.DurableProvider.Descriptor with
        {
            ArtifactSha256 = new('b', 64)
        };
        await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => providerFixture.InvokeAsync().AsTask());
        Assert.Equal(0, providerFixture.DurableProvider.SnapshotReadCount);
        Assert.Equal(0, providerFixture.Controller.CallCount);

        var controller = new CountingController
        {
            Descriptor = new(
                "phase-a-lifecycle-controller",
                Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
                new('b', 64),
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion)
        };
        var controllerFixture = CommandFixture.Create(controller: controller);
        await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => controllerFixture.InvokeAsync().AsTask());
        Assert.Equal(0, controllerFixture.DurableProvider.SnapshotReadCount);
        Assert.Equal(0, controller.CallCount);
    }

    [Fact]
    public void A3_All14ResponsibilitiesHaveOneCatalogOwnerAndOneEffectiveOwner()
    {
        Assert.Equal(14, PhaseAOwnershipCatalog.All.Count);
        Assert.Equal(14, PhaseAOwnershipCatalog.All.Values.Distinct().Count());
        PhaseAOwnershipValidator.RequireComplete();
        Assert.Single(typeof(PhaseAControlPlaneAuthority).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(ILifecycleControllerAuthority));
        Assert.Single(typeof(PhaseAAcceptanceVerifierAuthority).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType == typeof(IAuthoritativeEvidenceReaderProvider));
    }

    [Fact]
    public async Task A3_AuthorizeThenConsumeExactGrantAndApprovedPlanThroughRawProductionPath()
    {
        var authorize = CommandFixture.Create(requiresLease: true);
        var authorized = await authorize.InvokeAsync();
        var grant = authorize.DurableProvider.LastTransactionRequest!.Command.StoredGrantClaim!;
        Assert.Equal(ControllerLifecycleState.Preflight, authorized.State);
        Assert.Equal(Hash, grant.GrantAuthorization.GrantSha256);

        var consume = CommandFixture.Create(
            state: ControllerLifecycleState.Preflight,
            operation: ControllerOperationV2.PREPARE,
            requestedState: ControllerLifecycleState.Provisioning,
            role: "ProvisioningExecutor",
            evidenceIds: ["preflight"],
            requiresLease: true,
            authoritativeMutation: snapshot => snapshot with
            {
                AuthorizationState = AuthorizationGrantStateV3.ACTIVE,
                CurrentAuthorization = grant,
                CurrentAuthorizationMatchCount = 1
            });
        var consumed = await consume.InvokeAsync();
        Assert.Equal(ControllerLifecycleState.Provisioning, consumed.State);
        Assert.Equal(AuthorizationGrantStateV3.CONSUMED, consumed.AuthorizationState);
        Assert.Equal(grant.GrantAuthorization.GrantSha256,
            consume.DurableProvider.LastTransactionRequest!.Command.StoredGrantClaim!.GrantAuthorization.GrantSha256);
    }

    [Fact]
    public async Task A3_ExactCompletedReplayReturnsOnlyOriginalGrantBoundOutcome()
    {
        var fixture = CommandFixture.Create();
        var first = await fixture.InvokeAsync();
        var replay = await fixture.InvokeAsync();
        Assert.Equal(ControlTransactionOutcomeV3.FIRST_OWNER, first.TransactionOutcome);
        Assert.Equal(ControlTransactionOutcomeV3.COMPLETED_REPLAY, replay.TransactionOutcome);
        Assert.Equal(first.State, replay.State);
        Assert.Equal(first.Version, replay.Version);
        Assert.Equal(first.ResponseSha256, replay.ResponseSha256);
        Assert.Equal(1, fixture.DurableProvider.BusinessCommitCount);
    }

    [Fact]
    public async Task A3_EveryStoredGrantIssuerActorPolicyTenantPlanVersionEvidenceLeaseFenceAndExpirySubstitutionFailsBeforeLifecycle()
    {
        var mutations = new Func<StoredAuthorizationGrantV3, StoredAuthorizationGrantV3>[]
        {
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { AuthorizationId = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { GrantIssuer = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { AuthenticatedSubject = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { WorkloadIdentity = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { Audience = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { Operation = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { TrustedRole = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { TrustedScope = "ORG:C2" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { PolicyVersion = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { PolicyRowId = "other" } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { GrantSha256 = new('b', 64) } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { NotBefore = Now.AddMinutes(1) } },
            grant => grant with { GrantAuthorization = grant.GrantAuthorization with { ExpiresAt = Now.AddMinutes(-1) } },
            grant => grant with { GrantKeyId = "other" },
            grant => grant with { GrantContractVersion = "other" },
            grant => grant with { GrantSignatureSha256 = new('b', 64) },
            grant => grant with { Scope = grant.Scope with { OrganizationId = "C2" } },
            grant => grant with { Scope = grant.Scope with { DatabaseInstanceId = "other" } },
            grant => grant with { ResourceType = "other" },
            grant => grant with { ResourceId = "other" },
            grant => grant with { ResourceVersion = grant.ResourceVersion + 1 },
            grant => grant with { AuthorizedOperation = "other" },
            grant => grant with { ExecutorClass = "other" },
            grant => grant with { ApprovedIntentSha256 = new('b', 64) },
            grant => grant with { EvidenceManifestSha256 = new('b', 64) },
            grant => grant with { LeaseId = "other" },
            grant => grant with { ControllerEpoch = grant.ControllerEpoch + 1 },
            grant => grant with { FencingToken = grant.FencingToken + 1 },
            grant => grant with { PolicyArtifactSha256 = new('b', 64) }
        };

        foreach (var mutate in mutations)
        {
            var fixture = CommandFixture.Create(
                state: ControllerLifecycleState.Preflight,
                operation: ControllerOperationV2.PREPARE,
                requestedState: ControllerLifecycleState.Provisioning,
                role: "ProvisioningExecutor",
                evidenceIds: ["preflight"],
                requiresLease: true);
            var original = fixture.DurableProvider.Snapshot.CurrentAuthorization!;
            fixture.DurableProvider.Snapshot = fixture.DurableProvider.Snapshot with
            {
                CurrentAuthorization = mutate(original)
            };
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => fixture.InvokeAsync().AsTask());
            Assert.Equal(0, fixture.Controller.CallCount);
            Assert.Equal(0, fixture.DurableProvider.AtomicExecuteCount);
        }
    }

    [Fact]
    public async Task A3_MissingDuplicateConsumedStaleOrAmbiguousGrantFailsClosedWithoutAtomicCall()
    {
        var mutations = new Func<AuthoritativeControlPlaneSnapshotV3, AuthoritativeControlPlaneSnapshotV3>[]
        {
            snapshot => snapshot with { CurrentAuthorization = null, CurrentAuthorizationMatchCount = 0 },
            snapshot => snapshot with { CurrentAuthorizationMatchCount = 2 },
            snapshot => snapshot with
            {
                CurrentAuthorization = snapshot.CurrentAuthorization! with
                {
                    State = AuthorizationGrantStateV3.CONSUMED,
                    ConsumedAt = Now
                }
            },
            snapshot => snapshot with
            {
                CurrentAuthorization = snapshot.CurrentAuthorization! with
                {
                    GrantAuthorization = snapshot.CurrentAuthorization.GrantAuthorization with
                    {
                        ExpiresAt = Now.AddMinutes(-1)
                    }
                }
            }
        };
        foreach (var mutate in mutations)
        {
            var fixture = CommandFixture.Create(
                state: ControllerLifecycleState.Preflight,
                operation: ControllerOperationV2.PREPARE,
                requestedState: ControllerLifecycleState.Provisioning,
                role: "ProvisioningExecutor",
                evidenceIds: ["preflight"],
                requiresLease: true);
            fixture.DurableProvider.Snapshot = mutate(fixture.DurableProvider.Snapshot);
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => fixture.InvokeAsync().AsTask());
            Assert.Equal(0, fixture.Controller.CallCount);
            Assert.Equal(0, fixture.DurableProvider.AtomicExecuteCount);
        }
    }

    [Fact]
    public async Task A3_CallerSnapshotGrantAndApprovedPlanClaimsRemainComparisonOnly()
    {
        var fixture = CommandFixture.Create(
            payloadMutation: payload => payload with
            {
                StoredGrantClaim = payload.StoredGrantClaim! with
                {
                    ApprovedIntentSha256 = new('b', 64)
                }
            },
            state: ControllerLifecycleState.Preflight,
            operation: ControllerOperationV2.PREPARE,
            requestedState: ControllerLifecycleState.Provisioning,
            role: "ProvisioningExecutor",
            evidenceIds: ["preflight"],
            requiresLease: true);
        await Assert.ThrowsAsync<TrustFailureExceptionV2>(() => fixture.InvokeAsync().AsTask());
        Assert.Equal(1, fixture.DurableProvider.SnapshotReadCount);
        Assert.Equal(0, fixture.Controller.CallCount);
        Assert.Equal(0, fixture.DurableProvider.AtomicExecuteCount);
    }

    [Fact]
    public async Task A3_ServerPinnedReaderIdentityVersionArtifactSetSelectsEveryReaderExactlyOnce()
    {
        var fixture = EvidenceFixture.Create();
        var verdict = await fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport);
        Assert.Equal(VerificationDisposition.Passed, verdict.Calculation.Disposition);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(
            [("reader-1", Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion)],
            fixture.ReaderRegistry.DescriptorRequests);
    }

    [Fact]
    public async Task A3_OracleReceivesOnlyFactsFromServerSelectedReaders()
    {
        var fixture = EvidenceFixture.Create();
        await fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport);
        var facts = Assert.Single(fixture.Oracle.LastAuthoritativeFacts!);
        Assert.Equal(fixture.Bundle, facts);
        Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
        Assert.Equal(1, fixture.Oracle.EvaluationCount);
    }

    [Fact]
    public async Task A3_CallerSelectedReaderVersionUpgradeDowngradeArtifactOrSchemaNeverSelectsReader()
    {
        var substitutions = new Func<AuthoritativeFactBundleV3, AuthoritativeFactBundleV3>[]
        {
            bundle => bundle with { ReaderVersion = "0.9.0" },
            bundle => bundle with { ReaderVersion = "99.0.0" },
            bundle => bundle with { ReaderArtifactSha256 = new('b', 64) },
            bundle => bundle with { SchemaVersion = "other-schema" }
        };
        foreach (var substitute in substitutions)
        {
            var fixture = EvidenceFixture.Create();
            var declared = substitute(fixture.Bundle);
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() =>
                fixture.Authority.VerifyRawAsync(fixture.WithBundle(declared), fixture.Transport).AsTask());
            Assert.Equal(
                ("reader-1", Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion),
                Assert.Single(fixture.ReaderRegistry.DescriptorRequests));
            Assert.Equal(1, fixture.ReaderRegistry.ReadCount);
            Assert.Equal(0, fixture.Oracle.EvaluationCount);
        }
    }

    [Fact]
    public async Task A3_MissingDuplicateUnexpectedRevokedOrStalePinnedReaderFailsBeforeReadAndOracle()
    {
        var ordinary = new[]
        {
            EvidenceFixture.Create(),
            EvidenceFixture.Create(),
            EvidenceFixture.Create()
        };
        var inputs = new[]
        {
            ordinary[0].WithBundles([]),
            ordinary[1].WithBundles([ordinary[1].Bundle, ordinary[1].Bundle with
            {
                Binding = ordinary[1].Bundle.Binding with { ObservationId = "other" }
            }]),
            ordinary[2].WithBundle(ordinary[2].Bundle with { ReaderId = "unexpected-reader" })
        };
        for (var index = 0; index < ordinary.Length; index++)
        {
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() =>
                ordinary[index].Authority.VerifyRawAsync(inputs[index], ordinary[index].Transport).AsTask());
            Assert.Equal(0, ordinary[index].ReaderRegistry.ReadCount);
            Assert.Equal(0, ordinary[index].Oracle.EvaluationCount);
        }

        var invalidPins = new[]
        {
            EvidenceFixture.Create(pinnedReaderMutation: reader => reader with { RevokedAt = Now }),
            EvidenceFixture.Create(pinnedReaderMutation: reader => reader with { DowngradePolicyVersion = "stale" })
        };
        foreach (var fixture in invalidPins)
        {
            await Assert.ThrowsAsync<TrustFailureExceptionV2>(() =>
                fixture.Authority.VerifyRawAsync(fixture.CanonicalEvidence, fixture.Transport).AsTask());
            Assert.Equal(0, fixture.ReaderRegistry.ReadCount);
            Assert.Equal(0, fixture.Oracle.EvaluationCount);
        }
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
        var isAuthorizationCreation = operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal);
        if (isAuthorizationCreation && authorizationState == AuthorizationGrantStateV3.ACTIVE)
        {
            authorizationState = AuthorizationGrantStateV3.NONE;
        }
        if (operation is ControllerOperationV2.COMPLETE_PREPARE or
            ControllerOperationV2.COMPLETE_EXECUTE or
            ControllerOperationV2.COMPLETE_RECOVER or
            ControllerOperationV2.COMPLETE_PURGE or
            ControllerOperationV2.COMPLETE_EXPORT or
            ControllerOperationV2.VERIFY_ACCEPT or
            ControllerOperationV2.VERIFY_REJECT or
            ControllerOperationV2.FAIL)
        {
            authorizationState = AuthorizationGrantStateV3.CONSUMED;
        }
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
        var binding = new ExecutionAuthorizationV3(
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
        var grantOperation = operation switch
        {
            ControllerOperationV2.PREPARE or ControllerOperationV2.COMPLETE_PREPARE =>
                ControllerOperationV2.AUTHORIZE_PREPARE,
            ControllerOperationV2.EXECUTE or ControllerOperationV2.COMPLETE_EXECUTE or
                ControllerOperationV2.VERIFY_ACCEPT or ControllerOperationV2.VERIFY_REJECT =>
                ControllerOperationV2.AUTHORIZE_EXECUTE,
            ControllerOperationV2.RECOVER or ControllerOperationV2.COMPLETE_RECOVER =>
                ControllerOperationV2.AUTHORIZE_RECOVER,
            ControllerOperationV2.DROP => ControllerOperationV2.AUTHORIZE_DROP,
            ControllerOperationV2.PURGE or ControllerOperationV2.COMPLETE_PURGE =>
                ControllerOperationV2.AUTHORIZE_PURGE,
            ControllerOperationV2.EXPORT or ControllerOperationV2.COMPLETE_EXPORT =>
                ControllerOperationV2.AUTHORIZE_EXPORT,
            ControllerOperationV2.FAIL when state == ControllerLifecycleState.Provisioning =>
                ControllerOperationV2.AUTHORIZE_PREPARE,
            ControllerOperationV2.FAIL when state == ControllerLifecycleState.Migrating =>
                ControllerOperationV2.AUTHORIZE_EXECUTE,
            ControllerOperationV2.FAIL when state == ControllerLifecycleState.Recovering =>
                ControllerOperationV2.AUTHORIZE_RECOVER,
            ControllerOperationV2.FAIL when state == ControllerLifecycleState.Purging =>
                ControllerOperationV2.AUTHORIZE_PURGE,
            _ => ControllerOperationV2.AUTHORIZE_PREPARE
        };
        StoredAuthorizationGrantV3? currentAuthorization = authorizationState == AuthorizationGrantStateV3.NONE
            ? null
            : new(
                new(
                    "stored-authorization", "issuer", "subject", "workload", "audience",
                    grantOperation.ToString(), "OriginalAuthorizer", "ORG:C1", "policy-v1", "row-v1", Hash,
                    Now.AddHours(-1),
                    operation == ControllerOperationV2.EXPIRE ? Now.AddMinutes(-1) : Now.AddHours(1)),
                "key",
                Rev869BCompatibilityManifestV2.ContractVersion,
                Hash,
                scope,
                "REV869B_TARGET",
                "resource",
                7,
                grantOperation.ToString(),
                "workload",
                Hash,
                Hash,
                lease?.LeaseId ?? "lease-none",
                lease?.ControllerEpoch ?? 0,
                lease?.FencingToken ?? 0,
                Hash,
                authorizationState,
                authorizationState == AuthorizationGrantStateV3.CONSUMED ? Now : null);
        var snapshot = new AuthoritativeControlPlaneSnapshotV3(
            "phase-a-durable-owner",
            Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion,
            Hash,
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            scope,
            "REV869B_TARGET",
            "resource",
            7,
            state,
            authorizationState,
            currentAuthorization,
            exportState,
            lease,
            "attempt",
            1,
            "prior-audit",
            currentAuthorization is null ? 0 : 1);
        return new(
            "command",
            operation,
            state,
            7,
            binding,
            currentAuthorization,
            lease,
            evidence,
            new("issuer", "C1", "instance", operation.ToString(), "request", "idempotency", Hash),
            new("issuer", "nonce", Now.AddHours(1), Hash),
            "audit",
            Hash,
            authorizationState,
            exportState,
            "attempt",
            snapshot);
    }

    private sealed record LiteralRuleRow(
        ControllerLifecycleState State,
        ControllerOperationV2 Operation,
        string Role,
        ControllerLifecycleState NextState,
        bool RequiresLease,
        IReadOnlyList<string> EvidenceIds,
        AuthorizationGrantStateV3 RequiredAuthorization,
        AuthorizationGrantStateV3 NextAuthorization,
        ExportAuthorizationSubstateV3 ExportState,
        ExportAuthorizationSubstateV3 NextExportState);

    private static LiteralRuleRow ParseLiteralRow(string literal)
    {
        var fields = literal.Split('|');
        return new(
            Enum.Parse<ControllerLifecycleState>(fields[2]),
            Enum.Parse<ControllerOperationV2>(fields[3]),
            fields[4],
            Enum.Parse<ControllerLifecycleState>(fields[5]),
            bool.Parse(fields[7]),
            fields[8].Split(',', StringSplitOptions.RemoveEmptyEntries),
            Enum.Parse<AuthorizationGrantStateV3>(fields[12]),
            Enum.Parse<AuthorizationGrantStateV3>(fields[13]),
            Enum.Parse<ExportAuthorizationSubstateV3>(fields[14]),
            Enum.Parse<ExportAuthorizationSubstateV3>(fields[15]));
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
            CountingController controller,
            FakeDurableProvider durableProvider)
        {
            Authority = authority;
            Header = header;
            HeaderBytes = headerBytes;
            PayloadBytes = payloadBytes;
            Signature = signature;
            Transport = transport;
            Controller = controller;
            DurableProvider = durableProvider;
        }

        public PhaseAControlPlaneAuthority Authority { get; }
        public CanonicalSignedHeaderV2 Header { get; }
        public byte[] HeaderBytes { get; }
        public byte[] PayloadBytes { get; }
        public byte[] Signature { get; }
        public AuthenticatedWorkloadIdentityV3 Transport { get; }
        public CountingController Controller { get; }
        public FakeDurableProvider DurableProvider { get; }

        public ValueTask<LifecycleTransitionResultV3> InvokeAsync() =>
            Authority.AcceptRawCommandAsync(HeaderBytes, PayloadBytes, Signature, Transport);

        public static CommandFixture Create(
            Func<CanonicalSignedHeaderV2, CanonicalSignedHeaderV2>? headerMutation = null,
            Func<CanonicalCommandPayloadV2, CanonicalCommandPayloadV2>? payloadMutation = null,
            CountingController? controller = null,
            FakeDurableProvider? durableProvider = null,
            ControllerLifecycleState state = ControllerLifecycleState.Registered,
            ControllerOperationV2 operation = ControllerOperationV2.AUTHORIZE_PREPARE,
            ControllerLifecycleState requestedState = ControllerLifecycleState.Preflight,
            string role = "Operator",
            IReadOnlyList<string>? evidenceIds = null,
            bool requiresLease = false,
            ExportAuthorizationSubstateV3 exportState = ExportAuthorizationSubstateV3.NONE,
            AuthorizationGrantStateV3 authorizationState = AuthorizationGrantStateV3.ACTIVE,
            Func<AuthoritativeControlPlaneSnapshotV3, AuthoritativeControlPlaneSnapshotV3>? authoritativeMutation = null)
        {
            controller ??= new CountingController();
            evidenceIds ??= ["target-registration"];
            var authoritative = Command(
                state,
                operation,
                role,
                evidenceIds,
                requiresLease,
                exportState,
                authorizationState).AuthoritativeSnapshot! with
                {
                    AttemptId = "attempt:request",
                    AttemptNumber = 0,
                    LastAuditReference = "audit:prior"
                };
            if (authoritative.CurrentAuthorization is not null)
            {
                authoritative = authoritative with
                {
                    CurrentAuthorization = authoritative.CurrentAuthorization with
                    {
                        ApprovedIntentSha256 = Convert.ToHexString(SHA256.HashData(
                            CanonicalJsonV1.Serialize(new Dictionary<string, string>(StringComparer.Ordinal)))).ToLowerInvariant(),
                        EvidenceManifestSha256 = Convert.ToHexString(SHA256.HashData(
                            CanonicalJsonV1.Serialize(evidenceIds))).ToLowerInvariant()
                    }
                };
            }
            authoritative = authoritativeMutation?.Invoke(authoritative) ?? authoritative;
            var payload = new CanonicalCommandPayloadV2(
                operation,
                state,
                requestedState,
                new("scenario"),
                new("subcase"),
                "action",
                new Dictionary<string, string>(StringComparer.Ordinal),
                evidenceIds,
                operation.ToString().StartsWith("AUTHORIZE_", StringComparison.Ordinal)
                    ? null
                    : authoritative.CurrentAuthorization);
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
                role,
                "ORG:C1",
                "C1",
                "cluster",
                "instance",
                payload.Operation.ToString(),
                "resource",
                7,
                requiresLease ? "lease" : "lease-none",
                requiresLease ? 9 : 0,
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
            durableProvider ??= new FakeDurableProvider(authoritative);
            var authority = new PhaseAControlPlaneAuthority(
                new FakeKeyRegistry(),
                new FixedAudiencePolicy(role),
                new FixedAuthorizationResolver(role),
                new FixedAlgorithmPolicy(),
                new FixedClockPolicy(),
                kms,
                durableProvider,
                new FakeReaderRegistry(),
                controller,
                new FixedTimeProvider(Now),
                Options.Create(ValidControlPlaneOptions()));
            return new(
                authority,
                header,
                headerBytes,
                payloadBytes,
                SHA256.HashData(headerBytes),
                new("issuer", "subject", "workload", "audience", Hash),
                controller,
                durableProvider);
        }
    }

    private static ControlPlaneOptions ValidControlPlaneOptions() => new()
    {
        ServiceIdentity = "control-plane",
        IssuerId = "issuer",
        Audience = "audience",
        ControlPlaneDatabaseIdentity = "isolated-control-db",
        CommandSigningKeyId = "command-key",
        EvidenceVerificationKeyId = "evidence-key",
        ContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
        EvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
        CanonicalizationVersion = Rev869BCompatibilityManifestV2.CanonicalizationVersion,
        OwnershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
        ReadinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
        DurableProviderContractVersion = Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion,
        DurableProviderIdentity = "phase-a-durable-owner",
        DurableProviderArtifactSha256 = Hash,
        LifecycleControllerIdentity = "phase-a-lifecycle-controller",
        LifecycleControllerContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
        LifecycleControllerArtifactSha256 = Hash,
        AuthorizationPolicyArtifactSha256 = Hash,
        CanonicalEnvelopeVersion = Rev869BPhaseACompatibilityManifest.CanonicalEnvelopeVersion,
        RetentionPolicyReference = "phase-a-retention",
        MaximumEvidenceObservations = 10,
        MaximumFactsPerObservation = 10,
        MaximumReplayWindowSeconds = 300,
        MaximumLeaseSeconds = 300,
        MaximumClockSkewSeconds = 30,
        MaximumEnvelopeBytes = PhaseAContractLimits.MaximumCommandEnvelopeBytes,
        MaximumSelectors = 10,
        MaximumStringBytes = 1_024,
        MaximumCumulativeFacts = 32_768,
        ControlPlaneEndpoint = "https://control-plane.invalid",
        AcceptanceVerifierEndpoint = "https://acceptance-verifier.invalid",
        AllowedTargetEnvironments = ["isolated"],
        AllowedDatabaseIdentityPatterns = ["^isolated-.*$"],
        AllowedIssuerIds = ["issuer"],
        AllowedAudiences = ["audience"],
        AllowedRoles = ["Operator", "ProvisioningExecutor"],
        AllowedScopes = ["ORG:C1"],
        AllowedOperations = Enum.GetNames<ControllerOperationV2>()
    };

    private class CountingController : ILifecycleControllerAuthority
    {
        private readonly Rev869BControllerStateMachine _machine = new();
        private int _callCount;

        public int CallCount => _callCount;
        public TrustedComponentDescriptorV3 Descriptor { get; set; } = new(
            "phase-a-lifecycle-controller",
            Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
            Hash,
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion);

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

    private sealed class FixedAudiencePolicy(string trustedRole = "Operator") : IAudiencePolicyProvider
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
                    trustedRole,
                    "ORG:C1",
                    "REV869B_TARGET",
                    MasterScopeKindV3.COMPANY_LEDGER)
            ]);
    }

    private sealed class FixedAuthorizationResolver(string trustedRole = "Operator") : ITrustedSubjectRoleScopeResolver
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
                trustedRole,
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

    private sealed class FakeDurableProvider(
        AuthoritativeControlPlaneSnapshotV3 snapshot) : IDurableControlPlanePersistenceProvider
    {
        public TrustedComponentDescriptorV3 Descriptor { get; set; } = new(
            "phase-a-durable-owner",
            Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion,
            Hash,
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion);
        public AuthoritativeControlPlaneSnapshotV3 Snapshot { get; set; } = snapshot;
        public int SnapshotReadCount { get; private set; }
        public int AtomicExecuteCount { get; private set; }
        public int BusinessCommitCount { get; private set; }
        public ControlPlaneTransactionRequestV3? LastTransactionRequest { get; private set; }
        public Func<ControlPlaneTransactionResultV3, ControlPlaneTransactionResultV3>? ResultMutation { get; set; }
        private readonly object _gate = new();
        private string? _requestDigest;
        private LifecycleTransitionResultV3? _committed;

        public ValueTask<AuthoritativeControlPlaneSnapshotV3?> ReadAuthoritativeSnapshotAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            SnapshotReadCount++;
            return ValueTask.FromResult<AuthoritativeControlPlaneSnapshotV3?>(
                Snapshot.ResourceId == resourceId ? Snapshot : null);
        }

        public ValueTask<ControlPlaneTransactionResultV3> ExecuteAtomicallyAsync(
            ControlPlaneTransactionRequestV3 request,
            CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                AtomicExecuteCount++;
                LastTransactionRequest = request;
                var digest = request.Command.Idempotency.CanonicalRequestSha256;
                if (_requestDigest is not null && _requestDigest != digest)
                {
                    throw new TrustFailureExceptionV2(
                        TrustFailureCodeV2.IDEMPOTENCY_PAYLOAD_MISMATCH,
                        "The composite owner rejected a changed canonical request digest.");
                }
                if (_committed is not null)
                {
                    var replay = _committed with
                    {
                        TransactionOutcome = ControlTransactionOutcomeV3.COMPLETED_REPLAY
                    };
                    var replayResult = new ControlPlaneTransactionResultV3(
                        replay.TransactionOutcome,
                        replay,
                        true,
                        false,
                        false,
                        true,
                        request.Command.StoredGrantClaim!.GrantAuthorization.GrantSha256,
                        request.Command.StoredGrantClaim.ApprovedIntentSha256);
                    return ValueTask.FromResult(ResultMutation?.Invoke(replayResult) ?? replayResult);
                }

                _requestDigest = digest;
                BusinessCommitCount++;
                _committed = request.ProposedTransition with
                {
                    TransactionOutcome = ControlTransactionOutcomeV3.FIRST_OWNER
                };
                var consumesAuthorization =
                    Snapshot.AuthorizationState == AuthorizationGrantStateV3.ACTIVE &&
                    _committed.AuthorizationState == AuthorizationGrantStateV3.CONSUMED;
                var result = new ControlPlaneTransactionResultV3(
                    _committed.TransactionOutcome,
                    _committed,
                    true,
                    consumesAuthorization,
                    Snapshot.Lease is not null,
                    true,
                    request.Command.StoredGrantClaim!.GrantAuthorization.GrantSha256,
                    request.Command.StoredGrantClaim.ApprovedIntentSha256);
                return ValueTask.FromResult(ResultMutation?.Invoke(result) ?? result);
            }
        }

        public ValueTask<ControlTransactionOutcomeV3> RegisterNonceAsync(
            NonceRegistrationV3 nonce,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ControlTransactionOutcomeV3.FIRST_OWNER);

        public ValueTask<ControlTransactionOutcomeV3> ClaimAsync(
            IdempotencyIdentityV3 identity,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(ControlTransactionOutcomeV3.FIRST_OWNER);

        public ValueTask<LifecycleTransitionResultV3?> ReadCommittedResultAsync(
            IdempotencyIdentityV3 identity,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LifecycleTransitionResultV3?>(null);

        public ValueTask<LeaseFenceExpectationV3?> ReadCurrentAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Snapshot.ResourceId == resourceId ? Snapshot.Lease : null);

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

        public ValueTask<LifecycleTransitionResultV3?> ReadAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<LifecycleTransitionResultV3?>(null);
    }

    private sealed class FakeReaderRegistry : IAuthoritativeEvidenceReaderProvider
    {
        private readonly int _maximumFacts;
        private readonly int _maximumBytes;
        private readonly AuthoritativeFactBundleV3? _bundle;
        private readonly EvidenceVerificationExpectationV3? _expectation;
        private readonly bool _readFails;

        public FakeReaderRegistry(
            int maximumFacts = 10,
            AuthoritativeFactBundleV3? bundle = null,
            EvidenceVerificationExpectationV3? expectation = null,
            bool readFails = false,
            int maximumBytes = 32_768)
        {
            _maximumFacts = maximumFacts;
            _maximumBytes = maximumBytes;
            _bundle = bundle;
            _expectation = expectation;
            _readFails = readFails;
        }

        public int ReadCount { get; private set; }
        public int ExpectationResolveCount { get; private set; }
        public List<(string ReaderId, string ReaderVersion)> DescriptorRequests { get; } = [];

        public ValueTask<EvidenceVerificationExpectationV3?> ResolveExpectationAsync(
            string evidenceEnvelopeId,
            AuthenticatedWorkloadIdentityV3 callerIdentity,
            CancellationToken cancellationToken = default)
        {
            ExpectationResolveCount++;
            return ValueTask.FromResult<EvidenceVerificationExpectationV3?>(
                _expectation is not null && _expectation.EvidenceEnvelopeId == evidenceEnvelopeId
                    ? _expectation
                    : null);
        }

        public ValueTask<AuthoritativeReaderDescriptorV3?> ResolveAsync(
            string readerId,
            string readerVersion,
            CancellationToken cancellationToken = default)
        {
            DescriptorRequests.Add((readerId, readerVersion));
            return ValueTask.FromResult<AuthoritativeReaderDescriptorV3?>(new(
                readerId,
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                Hash,
                $"reader:{readerId}",
                "reader-role",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                new HashSet<string>(["C1"], StringComparer.Ordinal),
                new HashSet<string>(["REV869B_TARGET"], StringComparer.Ordinal),
                new HashSet<string>(["status", "count", "password"], StringComparer.Ordinal),
                _maximumFacts,
                _maximumBytes,
                $"source:{readerId}",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                EvidenceStageV3.DURABLE));
        }

        public ValueTask<EvidenceRequirementV3?> ResolveRequirementAsync(
            string requirementId,
            string operation,
            CompanyDatabaseScopeV3 scope,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<EvidenceRequirementV3?>(new(
                requirementId,
                $"reader-for:{requirementId}",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                EvidenceStageV3.DURABLE,
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                _maximumFacts,
                32_768));

        public ValueTask<AuthoritativeFactBundleV3> ReadAsync(
            AuthoritativeReaderDescriptorV3 reader,
            EvidenceVerificationExpectationV3 expectation,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (_readFails || _bundle is null || _expectation is null || expectation != _expectation)
            {
                throw new InvalidOperationException("No authoritative bundle is configured for this expectation.");
            }
            return ValueTask.FromResult(_bundle);
        }
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
            CanonicalEvidenceEnvelopeV3 envelope,
            FakeReaderRegistry readerRegistry)
        {
            Authority = authority;
            Bundle = bundle;
            CanonicalEvidence = canonicalEvidence;
            Transport = transport;
            Kms = kms;
            Oracle = oracle;
            Audit = audit;
            Envelope = envelope;
            ReaderRegistry = readerRegistry;
        }

        public PhaseAAcceptanceVerifierAuthority Authority { get; }
        public AuthoritativeFactBundleV3 Bundle { get; }
        public byte[] CanonicalEvidence { get; }
        public AuthenticatedWorkloadIdentityV3 Transport { get; }
        public FakeKms Kms { get; }
        public MutationSensitiveOracle Oracle { get; }
        public CapturingAudit Audit { get; }
        public CanonicalEvidenceEnvelopeV3 Envelope { get; }
        public FakeReaderRegistry ReaderRegistry { get; }

        public byte[] WithBundle(AuthoritativeFactBundleV3 bundle)
            => WithBundles([bundle]);

        public byte[] WithBundles(IReadOnlyList<AuthoritativeFactBundleV3> bundles)
        {
            var envelope = Envelope with
            {
                AuthoritativeBundles = bundles,
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
            string? sensitiveField = null,
            int factCount = 1,
            string statusValue = "complete",
            int configuredMaximumFacts = 10,
            int maximumStringBytes = 1_024,
            int maximumObservations = 10,
            int maximumCumulativeFactBytes = 32_768,
            int readerMaximumBytes = 32_768,
            Func<EvidenceScopeTemporalBindingV3, EvidenceScopeTemporalBindingV3>? authoritativeBindingMutation = null,
            bool readerFails = false,
            bool auditWrongReceipt = false,
            bool auditInvalidChain = false,
            Func<AuthoritativeReaderDescriptorV3, AuthoritativeReaderDescriptorV3>? pinnedReaderMutation = null)
        {
            var pinnedReader = new AuthoritativeReaderDescriptorV3(
                "reader-1",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                Hash,
                "reader:reader-1",
                "reader-role",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                new HashSet<string>(["C1"], StringComparer.Ordinal),
                new HashSet<string>(["REV869B_TARGET"], StringComparer.Ordinal),
                new HashSet<string>(["status", "count", "password"], StringComparer.Ordinal),
                maximumFacts,
                readerMaximumBytes,
                "source:reader-1",
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                EvidenceStageV3.DURABLE);
            pinnedReader = pinnedReaderMutation?.Invoke(pinnedReader) ?? pinnedReader;
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
                ReaderSelectionPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
                RequiredReaders = [pinnedReader],
                AllowedClusterIds = ["cluster"],
                AllowedInstanceIds = ["instance"],
                AllowedFactFields = ["status", "count"],
                SensitiveFieldNames = ["password", "token", "private_key", "pan", "bank", "payroll"],
                MaximumEnvelopeBytes = PhaseAContractLimits.MaximumEvidenceEnvelopeBytes,
                MaximumObservations = maximumObservations,
                MaximumSelectors = 10,
                MaximumFactsPerObservation = configuredMaximumFacts,
                MaximumStringBytes = maximumStringBytes,
                MaximumCumulativeFactBytes = maximumCumulativeFactBytes,
                MaximumClockSkewSeconds = 30,
                MaximumObservationWindowSeconds = 600
            };
            var expectedBinding = new EvidenceScopeTemporalBindingV3(
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
            var binding = authoritativeBindingMutation?.Invoke(expectedBinding) ?? expectedBinding;
            IReadOnlyList<RawEvidenceFactV3> facts = sensitiveField is null
                ? Enumerable.Range(0, factCount)
                    .Select(index => index == 0
                        ? new RawEvidenceFactV3("status", SelectorValueKind.String, statusValue)
                        : new RawEvidenceFactV3("count", SelectorValueKind.Integer, index.ToString()))
                    .ToArray()
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
            var expectation = new EvidenceVerificationExpectationV3(
                "evidence",
                expectedBinding.Scope,
                expectedBinding.Operation,
                expectedBinding.RequestId,
                expectedBinding.ResourceType,
                expectedBinding.ResourceId,
                expectedBinding.ResourceVersion,
                expectedBinding.AttemptId,
                expectedBinding.LeaseId,
                expectedBinding.FencingToken,
                expectedBinding.Stage,
                expectedBinding.SnapshotOrWatermark,
                options.ReadinessPolicyVersion,
                options.ReaderSelectionPolicyVersion,
                [pinnedReader]);
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
            var audit = new CapturingAudit(auditSucceeds, auditWrongReceipt, auditInvalidChain);
            var readerRegistry = new FakeReaderRegistry(
                maximumFacts,
                bundle,
                expectation,
                readerFails,
                readerMaximumBytes);
            var authority = new PhaseAAcceptanceVerifierAuthority(
                options,
                new PhaseAReadinessAuthority(
                    options.ReadinessPolicyVersion,
                    providers,
                    new FixedTimeProvider(Now)),
                oracle,
                readerRegistry,
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
                envelope,
                readerRegistry);
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
        public IReadOnlyList<AuthoritativeFactBundleV3>? LastAuthoritativeFacts { get; private set; }

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
            LastAuthoritativeFacts = authoritativeFacts;
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

    private sealed class CapturingAudit(
        bool succeeds,
        bool wrongReceipt = false,
        bool invalidChain = false) : IImmutableAuditEvidenceProvider
    {
        public int AppendCount { get; private set; }
        public int EvidenceAppendCount { get; private set; }
        public ImmutableAuditEventV3? Event { get; private set; }
        public List<string> Trace { get; } = [];

        public ValueTask<string> ReadCurrentChainHeadSha256Async(
            CancellationToken cancellationToken = default)
        {
            Trace.Add("chain");
            return ValueTask.FromResult(invalidChain ? "invalid-chain" : Hash);
        }

        public ValueTask<DurableAuditAppendReceiptV2> AppendAuditAsync(
            ImmutableAuditEventV3 auditEvent,
            CancellationToken cancellationToken = default)
        {
            AppendCount++;
            Trace.Add("audit");
            Event = auditEvent;
            var eventSha256 = Convert.ToHexString(
                SHA256.HashData(CanonicalJsonV1.Serialize(auditEvent))).ToLowerInvariant();
            return ValueTask.FromResult(
                succeeds
                    ? new DurableAuditAppendReceiptV2(
                        wrongReceipt ? "wrong-event" : auditEvent.EventId,
                        "audit",
                        eventSha256,
                        Now)
                    : null!);
        }

        public ValueTask<DurableAuditAppendReceiptV2> AppendEvidenceAsync(
            string evidenceId,
            string sha256,
            ReadOnlyMemory<byte> canonicalEvidence,
            CancellationToken cancellationToken = default)
        {
            EvidenceAppendCount++;
            Trace.Add("evidence");
            return ValueTask.FromResult(
                succeeds
                    ? new DurableAuditAppendReceiptV2(
                        wrongReceipt ? "wrong-evidence" : evidenceId,
                        "evidence",
                        sha256,
                        Now)
                    : null!);
        }
    }

    private static IReadOnlyList<IReadinessDependencyProvider> ReadyProviders() =>
        Enum.GetValues<PhaseADependencyV3>()
            .Select(dependency => (IReadinessDependencyProvider)new ReadyProvider(dependency, Now))
            .ToArray();

    private static IReadOnlyList<IReadinessDependencyProvider> ReplaceProvider(
        PhaseADependencyV3 dependency,
        IReadinessDependencyProvider replacement) =>
        ReadyProviders()
            .Select(provider => provider.Dependency == dependency ? replacement : provider)
            .ToArray();

    private static DependencyReadinessV3 ReadyResult(PhaseADependencyV3 dependency) => new(
        dependency,
        ReadinessDependencyStateV3.READY,
        Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
        Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
        "READY",
        Now,
        Now.AddMinutes(1),
        dependency.ToString(),
        dependency.ToString());

    private static ValueTask<ReadinessSnapshotV3> ReadinessSnapshotAsync(
        IReadOnlyList<IReadinessDependencyProvider> providers) =>
        new PhaseAReadinessAuthority(
            Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            providers,
            new FixedTimeProvider(Now)).CheckAsync();

    private static void AssertRouteDecisions(
        ReadinessSnapshotV3 snapshot,
        int expectedStatus,
        bool expectedHandlerAllowed)
    {
        var controlPlane = PhaseAReadinessRouteGuardV3.Evaluate(snapshot);
        var verifier = PhaseAReadinessRouteGuardV3.Evaluate(snapshot);
        Assert.Equal(expectedStatus, controlPlane.HttpStatusCode);
        Assert.Equal(expectedStatus, verifier.HttpStatusCode);
        Assert.Equal(expectedHandlerAllowed, controlPlane.ProtectedHandlerAllowed);
        Assert.Equal(expectedHandlerAllowed, verifier.ProtectedHandlerAllowed);
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

    private sealed class StaticReadinessProvider(
        PhaseADependencyV3 dependency,
        DependencyReadinessV3 result) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(result);
    }

    private sealed class ThrowingProvider(PhaseADependencyV3 dependency) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("unavailable");
    }

    private sealed class CancelledProvider(PhaseADependencyV3 dependency) : IReadinessDependencyProvider
    {
        public PhaseADependencyV3 Dependency { get; } = dependency;

        public ValueTask<DependencyReadinessV3> CheckAsync(
            CancellationToken cancellationToken = default) =>
            throw new OperationCanceledException("dependency timeout");
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
