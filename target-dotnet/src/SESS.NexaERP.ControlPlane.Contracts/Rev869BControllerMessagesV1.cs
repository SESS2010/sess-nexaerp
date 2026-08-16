using System.Text;
using System.Text.Json;

namespace SESS.NexaERP.ControlPlane.Contracts;

public enum ControllerRole
{
    ControlPlaneRuntime,
    AcceptanceVerifier,
    AuditWriter,
    RegistryWriter,
    ProvisioningExecutor,
    MigrationExecutor,
    RecoveryExecutor,
    PurgeAuthorizer,
    PurgeExecutor,
    ExportReader,
    MonitoringReader,
    Operator,
    RecoveryApprover
}

public sealed class PhaseAReadinessAuthority(
    string policyVersion,
    IEnumerable<IReadinessDependencyProvider> providers,
    TimeProvider timeProvider) : IReadinessAuthorityV3
{
    private readonly IReadOnlyList<IReadinessDependencyProvider> _providers = providers.ToArray();

    public async ValueTask<ReadinessSnapshotV3> CheckAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DependencyReadinessV3>();
        var providerGroups = _providers
            .GroupBy(static provider => provider.Dependency)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        foreach (var dependency in Enum.GetValues<PhaseADependencyV3>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!providerGroups.TryGetValue(dependency, out var matches) || matches.Length != 1)
            {
                results.Add(new(
                    dependency,
                    matches is null
                        ? ReadinessDependencyStateV3.NOT_CONFIGURED
                        : ReadinessDependencyStateV3.POLICY_MISMATCH,
                    policyVersion,
                    null,
                    matches is null ? "DEPENDENCY_NOT_CONFIGURED" : "DUPLICATE_DEPENDENCY_OWNER"));
                continue;
            }

            var result = await matches[0].CheckAsync(cancellationToken);
            results.Add(result.Dependency != dependency
                ? new(
                    dependency,
                    ReadinessDependencyStateV3.IDENTITY_MISMATCH,
                    policyVersion,
                    result.ObservedVersion,
                    "DEPENDENCY_IDENTITY_MISMATCH")
                : string.IsNullOrWhiteSpace(result.RequiredVersion) ||
                  result.State == ReadinessDependencyStateV3.READY &&
                  !string.Equals(result.RequiredVersion, result.ObservedVersion, StringComparison.Ordinal)
                    ? new(
                        dependency,
                        ReadinessDependencyStateV3.VERSION_MISMATCH,
                        result.RequiredVersion,
                        result.ObservedVersion,
                        "DEPENDENCY_VERSION_MISMATCH")
                    : result);
        }

        return new(policyVersion, results, timeProvider.GetUtcNow());
    }
}

public sealed class NotConfiguredDependencyProvider(PhaseADependencyV3 dependency, string requiredVersion) :
    IReadinessDependencyProvider
{
    public PhaseADependencyV3 Dependency { get; } = dependency;

    public ValueTask<DependencyReadinessV3> CheckAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new DependencyReadinessV3(
            Dependency,
            ReadinessDependencyStateV3.NOT_CONFIGURED,
            requiredVersion,
            null,
            "DEPENDENCY_NOT_CONFIGURED"));
}

public enum ControllerLifecycleState
{
    Registered,
    Preflight,
    Provisioning,
    Ready,
    MigrationAuthorized,
    Migrating,
    VerificationPending,
    Accepted,
    Failed,
    Quarantined,
    RecoveryAuthorized,
    Recovering,
    DropAuthorized,
    Dropped,
    PurgeAuthorized,
    Purging,
    Purged
}

public enum ControllerCommandKind
{
    Register,
    BeginPreflight,
    BeginProvisioning,
    MarkReady,
    AuthorizeMigration,
    BeginMigration,
    RequestVerification,
    RecordAcceptance,
    RecordFailure,
    Quarantine,
    AuthorizeRecovery,
    BeginRecovery,
    AuthorizeDrop,
    RecordDropped,
    AuthorizePurge,
    BeginPurge,
    RecordPurged,
    ExportEvidence
}

public enum TrustRejectionCode
{
    None,
    UnsupportedContractVersion,
    UnsupportedEvidenceVersion,
    UnsupportedCanonicalizationVersion,
    UnsupportedSignatureAlgorithm,
    UnknownKey,
    RevokedKey,
    ExpiredKey,
    InvalidSignature,
    PayloadHashMismatch,
    StaleEnvelope,
    ReplayDetected,
    WrongCompany,
    WrongTargetInstance,
    WrongLease,
    WrongExecution,
    WrongScenario,
    WrongSubcase,
    WrongOracle,
    IllegalTransition,
    UnauthorizedRole,
    MissingObservationStage,
    DuplicateObservation,
    InvalidProvenance,
    InvalidSelector,
    IncompleteActionResult,
    PayloadLimitExceeded,
    CallerSuppliedVerdict
}

public sealed class TrustRejectionException(TrustRejectionCode code, string message) : InvalidOperationException(message)
{
    public TrustRejectionCode Code { get; } = code;
}

public sealed record ControlPlaneInstanceIdentity(string Value);
public sealed record TargetErpInstanceIdentity(string Value, string EnvironmentName, string DatabaseIdentity);
public readonly record struct ScenarioIdentityV1(string Value);
public readonly record struct SubcaseIdentityV1(string Value);
public readonly record struct ObservationIdentityV1(string Value);
public readonly record struct EvidenceEnvelopeIdentityV1(string Value);
public sealed record OracleIdentityV1(string OracleId, string Version, string Sha256);
public sealed record CompanyScope(string CompanyId, bool IsNotApplicable = false)
{
    public static CompanyScope ControlPlaneNotApplicable { get; } = new("N/A", true);
}

public sealed record Rev869BExecutionBinding(
    CompanyScope Company,
    ControlPlaneInstanceIdentity ControlPlane,
    TargetErpInstanceIdentity Target,
    string LeaseId,
    long LeaseVersion,
    string OperationId,
    string PreparationId,
    string AttemptId,
    string ExecutionId,
    string ScenarioId,
    string SubcaseId,
    string OracleId,
    string ActionId);

public sealed record LeaseExpectation(string LeaseId, long LeaseVersion);
public sealed record IdempotencyReplayKey(string Value, DateTimeOffset ExpiresAtUtc);
public sealed record CommandAuthorization(string SubjectId, IReadOnlyList<ControllerRole> Roles, DateTimeOffset AuthorizedAtUtc);
public sealed record LifecycleCommandV1(
    string CommandId,
    ControllerCommandKind Kind,
    Rev869BExecutionBinding Binding,
    ControllerLifecycleState ExpectedState,
    ControllerLifecycleState RequestedState,
    LeaseExpectation Lease,
    IdempotencyReplayKey Replay,
    CommandAuthorization Authorization,
    DateTimeOffset IssuedAtUtc);

public sealed record SignatureMetadataV1(
    string KeyId,
    string Algorithm,
    string CanonicalizationVersion,
    string PayloadSha256,
    string SignatureBase64,
    DateTimeOffset SignedAtUtc);

public sealed record SignedCommandEnvelopeV1(
    string ContractVersion,
    LifecycleCommandV1 Command,
    SignatureMetadataV1 Signature);

public sealed record CommandAttemptV1(
    string AttemptId,
    string CommandId,
    Rev869BExecutionBinding Binding,
    int AttemptNumber,
    ControllerLifecycleState State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string DurableAuditReference);

public sealed record ControllerStateTransitionV1(
    string TransitionId,
    Rev869BExecutionBinding Binding,
    ControllerLifecycleState From,
    ControllerLifecycleState To,
    DateTimeOffset OccurredAtUtc,
    string ActorSubjectId,
    string DurableAuditReference);

public sealed record TerminalOutcomeV1(
    Rev869BExecutionBinding Binding,
    ControllerLifecycleState State,
    string OutcomeCode,
    DateTimeOffset RecordedAtUtc,
    string DurableAuditReference);

public sealed record QuarantineAuthorizationV1(Rev869BExecutionBinding Binding, string ReasonCode, CommandAuthorization Authorization);
public sealed record RecoveryAuthorizationV1(Rev869BExecutionBinding Binding, string RecoveryPlanId, CommandAuthorization Authorization);
public sealed record DropAuthorizationV1(Rev869BExecutionBinding Binding, string DisposalTicket, CommandAuthorization Authorization);
public sealed record PurgeAuthorizationV1(Rev869BExecutionBinding Binding, string RetentionApproval, CommandAuthorization Authorization);
public sealed record EvidenceExportAuthorizationV1(Rev869BExecutionBinding Binding, string ExportPurpose, CommandAuthorization Authorization);
public sealed record DurableAuditReferenceV1(string EventId, string LedgerReference, DateTimeOffset RecordedAtUtc);

public enum ObservationStage
{
    Before,
    After,
    Durable
}

public enum ObservationSourceKind
{
    TargetDatabase,
    TargetApplication,
    ControllerLedger,
    OperatingSystem
}

public enum SelectorValueKind
{
    String,
    Integer,
    Decimal,
    Boolean,
    Null
}

public sealed record TypedSelectorValueV1(SelectorValueKind Kind, string? CanonicalValue);
public sealed record SelectorReaderProvenanceV1(string ReaderId, string ReaderContractVersion, ObservationSourceKind SourceKind);
public sealed record EvidenceSelectorV1(
    string Field,
    string Operator,
    TypedSelectorValueV1 Expected,
    SelectorReaderProvenanceV1? Reader = null);
public sealed record ObservationProvenanceV1(ObservationSourceKind SourceKind, string SourceIdentity, DateTimeOffset ObservedAtUtc);
public sealed record FactOnlyObservationV1(
    string ObservationId,
    ObservationStage Stage,
    ObservationProvenanceV1 Provenance,
    IReadOnlyDictionary<string, TypedSelectorValueV1> Facts);
public sealed record BeforeStateV1(FactOnlyObservationV1 Observation);
public sealed record AfterStateV1(FactOnlyObservationV1 Observation);
public sealed record DurableHistoryStateV1(FactOnlyObservationV1 Observation);

public sealed record ActionResultV1(
    bool TargetReached,
    int AffectedRows,
    string? SqlState,
    string? ErrorCode,
    string? ObjectIdentity,
    string ResultState,
    int? HttpStatus,
    string EvidenceReference);

public sealed record CanonicalEvidenceEnvelopeV1(
    string EvidenceVersion,
    string ContractVersion,
    Rev869BExecutionBinding Binding,
    IReadOnlyList<EvidenceSelectorV1> Selectors,
    IReadOnlyList<FactOnlyObservationV1> Observations,
    ActionResultV1 ActionResult,
    SignatureMetadataV1 Signature,
    string EvidenceEnvelopeId = "",
    string OracleVersion = "",
    string OracleSha256 = "");

public sealed record EvidenceVerificationRequestV1(
    CanonicalEvidenceEnvelopeV1 Evidence,
    Rev869BExecutionBinding ExpectedBinding,
    int MaxObservations,
    int MaxFactsPerObservation);
public sealed record EvidenceVerificationResponseV1(string EvidenceEnvelopeId, VerificationResultV1 Result);

public enum VerificationDisposition
{
    Passed,
    Failed
}

public sealed record VerificationResultV1(
    VerificationDisposition Disposition,
    string OracleId,
    IReadOnlyList<TrustRejectionCode> Rejections,
    string VerificationAuditReference,
    DateTimeOffset VerifiedAtUtc);

public sealed record VerificationAuditEventV1(
    string EventId,
    Rev869BExecutionBinding Binding,
    VerificationDisposition Disposition,
    IReadOnlyList<TrustRejectionCode> Rejections,
    DateTimeOffset OccurredAtUtc);

public sealed record KeyRotationCommandV1(string CurrentKeyId, string NextKeyId, DateTimeOffset ActivateAtUtc, CommandAuthorization Authorization);
public sealed record KeyRevocationCommandV1(string KeyId, string ReasonCode, DateTimeOffset RevokeAtUtc, CommandAuthorization Authorization);
public sealed record ProductionServiceIdentityV1(string SubjectId, ControllerRole Role, string WorkloadAudience);
public sealed record EnterpriseDataScopeV1(
    CompanyScope Company,
    bool SharedGlobalMasters,
    bool SeparateFinancialLedger,
    bool SeparateStockLedger,
    long MaximumEntityRows,
    int PageSize,
    string? ContinuationToken);
public sealed record PageRequestV1(int Offset, int Limit);
public sealed record PageResultV1<T>(IReadOnlyList<T> Items, int Offset, int Limit, bool HasMore);

public static class CanonicalJsonV1
{
    public static byte[] Serialize<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(value));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            WriteElement(writer, document.RootElement);
        }

        return stream.ToArray();
    }

    public static string SerializeToString<T>(T value) => Encoding.UTF8.GetString(Serialize(value));

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(static item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}

public enum TrustFailureCodeV2
{
    NONE,
    CONTRACT_UNSUPPORTED,
    CANONICALIZATION_UNSUPPORTED,
    ALGORITHM_UNSUPPORTED,
    KEY_UNKNOWN,
    KEY_REVOKED,
    ISSUER_UNKNOWN,
    ISSUER_KEY_MISMATCH,
    AUDIENCE_MISMATCH,
    SUBJECT_UNAUTHORIZED,
    REQUEST_ROLE_FORBIDDEN,
    SIGNATURE_INVALID,
    PAYLOAD_HASH_MISMATCH,
    NOT_YET_VALID,
    ENVELOPE_EXPIRED,
    NONCE_REPLAY,
    ORGANIZATION_MISMATCH,
    CLUSTER_MISMATCH,
    INSTANCE_MISMATCH,
    OPERATION_MISMATCH,
    RESOURCE_VERSION_STALE,
    LEASE_REQUIRED,
    LEASE_EXPIRED,
    LEASE_FENCE_STALE,
    STATE_TRANSITION_ILLEGAL,
    IDEMPOTENCY_PAYLOAD_MISMATCH,
    IDEMPOTENCY_NONRETRYABLE,
    ORACLE_MISMATCH,
    READER_MISSING,
    READER_UNAUTHORIZED,
    EVIDENCE_TOO_LARGE,
    EVIDENCE_SENSITIVE_FIELD,
    AUDIT_APPEND_FAILED,
    SERVICE_NOT_READY,
    CANONICAL_HEADER_MALFORMED,
    SCOPE_MISMATCH,
    AUTHORIZATION_BINDING_MISMATCH,
    IDEMPOTENCY_IN_PROGRESS,
    IDEMPOTENCY_RETRY_LIMIT,
    READER_DUPLICATE,
    EVIDENCE_TAMPERED,
    EVIDENCE_UNMAPPED_FIELD,
    PAGINATION_TOKEN_INVALID,
    DEPENDENCY_NOT_CONFIGURED,
    DEPENDENCY_UNAVAILABLE,
    DEPENDENCY_VERSION_MISMATCH,
    DEPENDENCY_IDENTITY_MISMATCH,
    DEPENDENCY_POLICY_MISMATCH,
    DEPENDENCY_DEGRADED_UNSAFE,
    CONTRACT_LIMIT_EXCEEDED,
    DURABLE_TRANSACTION_CONFLICT,
    AUDIT_DATA_FORBIDDEN
}

public sealed class TrustFailureExceptionV2(TrustFailureCodeV2 code, string message) : InvalidOperationException(message)
{
    public TrustFailureCodeV2 Code { get; } = code;
}

public enum IdempotencyReservationStateV2
{
    RESERVED,
    IN_PROGRESS,
    COMPLETED,
    RETRYABLE_FAILURE,
    NONRETRYABLE_FAILURE
}

public enum ReadinessStateV2
{
    READY,
    NOT_READY
}

public enum ControllerOperationV2
{
    AUTHORIZE_PREPARE,
    PREPARE,
    COMPLETE_PREPARE,
    FAIL,
    AUTHORIZE_EXECUTE,
    EXECUTE,
    COMPLETE_EXECUTE,
    VERIFY_ACCEPT,
    VERIFY_REJECT,
    QUARANTINE,
    AUTHORIZE_RECOVER,
    RECOVER,
    COMPLETE_RECOVER,
    AUTHORIZE_DROP,
    DROP,
    AUTHORIZE_PURGE,
    PURGE,
    COMPLETE_PURGE,
    AUTHORIZE_EXPORT,
    EXPORT,
    COMPLETE_EXPORT,
    CANCEL,
    EXPIRE
}

public enum ControllerAuthorizationStatusV2
{
    NONE,
    ACTIVE,
    CONSUMED,
    CANCELLED,
    EXPIRED
}

public enum ExportLifecycleStateV2
{
    NONE,
    AUTHORIZED,
    DELIVERING,
    DELIVERED,
    EXPIRED,
    FAILED
}

public sealed record CanonicalSignedHeaderV2(
    string ContractVersion,
    string CanonicalizationVersion,
    string Algorithm,
    string KeyId,
    string Issuer,
    string Audience,
    string Subject,
    string AuthorizedRole,
    string AuthorizedScope,
    string OrganizationId,
    string DatabaseClusterId,
    string DatabaseInstanceId,
    string Operation,
    string ResourceId,
    long ResourceVersion,
    string LeaseId,
    long FencingToken,
    string RequestId,
    string IdempotencyKey,
    string Nonce,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt,
    string CanonicalPayloadSha256,
    int CanonicalPayloadLength);

public sealed record CanonicalCommandPayloadV2(
    ControllerOperationV2 Operation,
    ControllerLifecycleState ExpectedState,
    ControllerLifecycleState RequestedState,
    ScenarioIdentityV1 Scenario,
    SubcaseIdentityV1 Subcase,
    string ActionId,
    IReadOnlyDictionary<string, string> ApprovedParameters,
    IReadOnlyList<string> EvidenceRequirements);

public sealed record SignedCommandEnvelopeV2(
    CanonicalSignedHeaderV2 Header,
    CanonicalCommandPayloadV2 Payload,
    byte[] Signature);

public sealed record TrustedIssuerDescriptorV2(
    string IssuerId,
    IReadOnlySet<string> AllowedAudiences,
    IReadOnlySet<string> ContractVersions,
    IReadOnlySet<string> Algorithms,
    IReadOnlyDictionary<string, SigningKeyDescriptor> Keys,
    IReadOnlySet<string> SubjectPatterns,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<string> Operations,
    DateTimeOffset ActiveFrom,
    DateTimeOffset? RevokedAt);

public sealed record AuthenticatedSubjectV2(
    string Issuer,
    string SubjectId,
    string WorkloadIdentity,
    string Audience,
    IReadOnlySet<string> TrustedRoles,
    IReadOnlySet<string> TrustedScopes);

public sealed record ResourceBindingV2(
    string OrganizationId,
    string DatabaseClusterId,
    string DatabaseInstanceId,
    string ResourceType,
    string ResourceId,
    long ExpectedResourceVersion,
    string Operation);

public sealed record LeaseFenceV2(
    string LeaseId,
    string ResourceId,
    long FencingToken,
    DateTimeOffset AcquiredAt,
    DateTimeOffset RenewedAt,
    DateTimeOffset ExpiresAt,
    string HolderSubject);

public sealed record TemporalAuthorizationV2(
    string Nonce,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt);

public sealed record IdempotencyBindingV2(
    string Issuer,
    string OrganizationId,
    string DatabaseInstanceId,
    string Operation,
    string RequestId,
    string IdempotencyKey,
    string CanonicalRequestDigest);

public sealed record IdempotencyOutcomeV2(
    IdempotencyReservationStateV2 ReservationState,
    int AttemptNumber,
    bool Retryable,
    TrustFailureCodeV2? TerminalFailureCode,
    string? ResponseDigest,
    string? AuditReference,
    DateTimeOffset? CompletedAt);

public sealed record OracleManifestV2(
    string OracleId,
    string SemanticVersion,
    string ArtifactSha256,
    string EvidenceSchemaVersion,
    IReadOnlyDictionary<string, string> AllowedReaderVersions,
    DateTimeOffset ActiveFrom,
    DateTimeOffset? RevokedAt);

public sealed record EvidenceReaderDescriptorV2(
    string ReaderId,
    string Version,
    string ArtifactSha256,
    ObservationSourceKind SourceType,
    IReadOnlySet<string> AllowedOrganizations,
    IReadOnlySet<string> AllowedResources,
    IReadOnlySet<string> AllowedFields,
    int MaximumResponseFacts,
    int MaximumResponseBytes);

public sealed record EvidenceReaderReceiptV2(
    string ReaderId,
    string ReaderVersion,
    string ReaderArtifactSha256,
    string RequestDigest,
    string ResponseDigest,
    DateTimeOffset ObservedAt);

public sealed record AuthoritativeEvidenceFactsV2(
    IReadOnlyDictionary<string, TypedSelectorValueV1> Facts,
    EvidenceReaderReceiptV2 Receipt,
    IReadOnlyList<FactOnlyObservationV1>? Observations = null);

public sealed record CanonicalEvidenceEnvelopeV2(
    string EvidenceEnvelopeId,
    ResourceBindingV2 Binding,
    string RequestId,
    LeaseFenceV2 Lease,
    DateTimeOffset ObservationWindowStart,
    DateTimeOffset ObservationWindowEnd,
    DateTimeOffset ActionOccurredAt,
    IReadOnlyList<FactOnlyObservationV1> RawFacts,
    IReadOnlyList<EvidenceReaderReceiptV2> ReaderReceipts,
    ActionResultV1 ActionReceipt,
    string PayloadSha256,
    string OracleId,
    string OracleVersion,
    string OracleArtifactSha256);

public sealed record VerificationAuditEventV2(
    string EventId,
    string Issuer,
    string Subject,
    string KeyId,
    string RequestId,
    string EvidenceEnvelopeId,
    string EvidenceEnvelopeSha256,
    ResourceBindingV2 Binding,
    LeaseFenceV2 Lease,
    string OracleId,
    string OracleVersion,
    string OracleArtifactSha256,
    IReadOnlyList<string> ReaderReceiptDigests,
    VerificationDisposition CalculatedDisposition,
    IReadOnlyList<TrustFailureCodeV2> ReasonCodes,
    DateTimeOffset OccurredAt);

public sealed record DurableAuditAppendReceiptV2(
    string EventId,
    string DurableReference,
    string EventSha256,
    DateTimeOffset AppendedAt);

public sealed record ReadinessResultV2(
    ReadinessStateV2 State,
    IReadOnlyList<string> DependencyCodes,
    DateTimeOffset CheckedAt);

public sealed record EvidenceVerificationContextV2(
    string Issuer,
    string Subject,
    string KeyId,
    string RequestId,
    ResourceBindingV2 ExpectedBinding,
    LeaseFenceV2 ExpectedLease);

public sealed record EvidenceVerificationRequestV2(
    CanonicalEvidenceEnvelopeV2 Evidence,
    EvidenceVerificationContextV2 Context);

public sealed record VerificationResultV2(
    VerificationDisposition Disposition,
    string OracleId,
    string OracleVersion,
    IReadOnlyList<TrustFailureCodeV2> ReasonCodes,
    DurableAuditAppendReceiptV2 AuditReceipt,
    DateTimeOffset VerifiedAt);

public enum ProductionResponsibilityV3
{
    NexaErpBusinessRuntime,
    ControlPlane,
    AcceptanceVerifier,
    DurableControlPlanePersistence,
    TrustedIssuerKeyRegistry,
    KmsHsmSigning,
    AuthoritativeEvidenceReader,
    ImmutableAuditEvidence,
    LifecycleController,
    BackupRecoveryAuthority,
    PurgeAuthorizer,
    PurgeExecutor,
    ExportAuthorizer,
    ExportDeliveryExecutor
}

public enum PhaseADependencyV3
{
    Configuration,
    WorkloadIdentity,
    IssuerRegistry,
    AudiencePolicy,
    SubjectRoleScopeResolver,
    KeyRegistry,
    AlgorithmVersionPolicy,
    TrustedClock,
    DurableControlPlane,
    KmsHsm,
    LifecycleController,
    OracleRegistry,
    EvidenceReaderRegistry,
    ImmutableAuditEvidence,
    TargetIdentityAndAcl
}

public enum ReadinessDependencyStateV3
{
    READY,
    NOT_CONFIGURED,
    UNAVAILABLE,
    VERSION_MISMATCH,
    IDENTITY_MISMATCH,
    POLICY_MISMATCH,
    DEGRADED_NOT_SAFE
}

public enum ControlTransactionOutcomeV3
{
    FIRST_OWNER,
    COMPLETED_REPLAY,
    IN_PROGRESS,
    RETRYABLE_TAKEOVER,
    NONRETRYABLE_FAILURE,
    CONFLICT,
    COMMITTED
}

public enum AuthorizationGrantStateV3
{
    ACTIVE,
    CONSUMED,
    CANCELLED,
    EXPIRED
}

public enum AuditEventKindV3
{
    REQUEST_RECEIVED,
    AUTHENTICATION_DECIDED,
    AUTHORIZATION_DECIDED,
    IDEMPOTENCY_DECIDED,
    LEASE_FENCE_DECIDED,
    LIFECYCLE_ATTEMPTED,
    LIFECYCLE_COMMITTED,
    VERIFIER_CALCULATED,
    DENIED,
    FAILED,
    RECOVERY_DECIDED,
    DROP_DECIDED,
    PURGE_DECIDED,
    EXPORT_DECIDED
}

public enum MasterScopeKindV3
{
    COMPANY_LEDGER,
    SHARED_APPROVED_MASTER,
    CONTROL_PLANE_NOT_APPLICABLE
}

public enum EvidenceStageV3
{
    BEFORE,
    ACTION,
    AFTER,
    DURABLE,
    CLEANUP
}

public sealed record CompanyDatabaseScopeV3(
    string OrganizationId,
    string DatabaseClusterId,
    string DatabaseInstanceId,
    MasterScopeKindV3 ScopeKind,
    string? SharedMasterClass = null);

public sealed record UntrustedBusinessIntentV3(
    string RequestId,
    string IdempotencyKey,
    string Operation,
    CompanyDatabaseScopeV3 Scope,
    string ResourceType,
    string ResourceId,
    long ExpectedResourceVersion,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> Parameters);

public sealed record AuthenticatedWorkloadIdentityV3(
    string IssuerId,
    string SubjectId,
    string WorkloadIdentity,
    string TransportAudience,
    string CredentialBindingSha256);

public sealed record SigningKeyMetadataV3(
    string IssuerId,
    string KeyId,
    string Purpose,
    string Algorithm,
    string KeyVersion,
    string PublicKeySha256,
    DateTimeOffset NotBefore,
    DateTimeOffset? NotAfter,
    DateTimeOffset? RevokedAt);

public sealed record IssuerTrustPolicyV3(
    string IssuerId,
    string PolicyVersion,
    string PolicyArtifactSha256,
    IReadOnlySet<string> AllowedAudiences,
    IReadOnlySet<string> AllowedOperations,
    IReadOnlySet<string> AllowedAlgorithms,
    IReadOnlySet<string> AllowedContractVersions,
    DateTimeOffset ActiveFrom,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);

public sealed record AudienceOperationPolicyV3(
    string PolicyRowId,
    string PolicyVersion,
    string Audience,
    string Operation,
    string SubjectClass,
    string TrustedRole,
    string TrustedScope,
    string ResourceType,
    MasterScopeKindV3 ScopeKind);

public sealed record ResolvedAuthorizationV3(
    string AuthorizationId,
    string GrantIssuer,
    string AuthenticatedSubject,
    string WorkloadIdentity,
    string Audience,
    string Operation,
    string TrustedRole,
    string TrustedScope,
    string PolicyVersion,
    string PolicyRowId,
    string GrantSha256,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt);

public sealed record ClockFreshnessPolicyV3(
    string PolicyVersion,
    TimeSpan MaximumEnvelopeLifetime,
    TimeSpan AllowedClockSkew,
    int RequiredTimeSources);

public sealed record TrustedSigningContextV3(
    ResolvedAuthorizationV3 Authorization,
    AuthenticatedWorkloadIdentityV3 SignerIdentity,
    SigningKeyMetadataV3 Key,
    CompanyDatabaseScopeV3 Scope,
    string ResourceType,
    string ResourceId,
    long ResourceVersion,
    string LeaseId,
    long FencingToken,
    string RequestId,
    string IdempotencyKey,
    string Nonce,
    DateTimeOffset IssuedAt,
    DateTimeOffset NotBefore,
    DateTimeOffset ExpiresAt);

public sealed record CanonicalProtectedEnvelopeV3(
    string ContractVersion,
    string CanonicalizationVersion,
    string Algorithm,
    string KeyId,
    ReadOnlyMemory<byte> CanonicalHeader,
    ReadOnlyMemory<byte> CanonicalPayload,
    ReadOnlyMemory<byte> Signature);

public sealed record IdempotencyIdentityV3(
    string IssuerId,
    string OrganizationId,
    string DatabaseInstanceId,
    string Operation,
    string RequestId,
    string IdempotencyKey,
    string CanonicalRequestSha256);

public sealed record LeaseFenceExpectationV3(
    string LeaseId,
    long ControllerEpoch,
    long FencingToken,
    DateTimeOffset ExpiresAt,
    string HolderSubject);

public sealed record EvidenceRequirementV3(
    string RequirementId,
    string ReaderId,
    string ReaderVersion,
    EvidenceStageV3 Stage,
    string SchemaVersion,
    int MaximumFacts,
    int MaximumBytes);

public sealed record AuthorizationBindingV3(
    ResolvedAuthorizationV3 Authorization,
    CompanyDatabaseScopeV3 Scope,
    string ResourceType,
    string ResourceId,
    long ResourceVersion,
    string Operation,
    string CanonicalRequestSha256,
    string EvidenceManifestSha256,
    string LeaseId,
    long FencingToken,
    AuthorizationGrantStateV3 State);

public sealed record VerifiedLifecycleCommandV3(
    string CommandId,
    ControllerOperationV2 Operation,
    ControllerLifecycleState CurrentState,
    long CurrentVersion,
    AuthorizationBindingV3 Authorization,
    LeaseFenceExpectationV3? Lease,
    IReadOnlyList<EvidenceRequirementV3> RequiredEvidence,
    IdempotencyIdentityV3 Idempotency,
    NonceRegistrationV3 Nonce,
    string AuditCorrelationId,
    string CanonicalEnvelopeSha256);

public sealed record LifecycleRuleV3(
    string RuleId,
    ControllerLifecycleState CurrentState,
    ControllerOperationV2 Operation,
    string TrustedRole,
    ControllerLifecycleState NextState,
    ControllerLifecycleState FailureState,
    bool RequiresLease,
    IReadOnlyList<string> RequiredEvidenceIds,
    int MaximumSameAttemptRetries,
    AuditEventKindV3 AuditKind);

public sealed record LifecycleTransitionResultV3(
    ControlTransactionOutcomeV3 TransactionOutcome,
    ControllerLifecycleState State,
    long Version,
    int AttemptNumber,
    string ResponseSha256,
    string AuditReference,
    TrustFailureCodeV2 FailureCode);

public sealed record NonceRegistrationV3(
    string IssuerId,
    string Nonce,
    DateTimeOffset ExpiresAt,
    string RequestSha256);

public sealed record ControlPlaneTransactionRequestV3(
    VerifiedLifecycleCommandV3 Command,
    NonceRegistrationV3 Nonce,
    DateTimeOffset ServerNow,
    string ExpectedProviderVersion);

public sealed record ControlPlaneTransactionResultV3(
    ControlTransactionOutcomeV3 Outcome,
    LifecycleTransitionResultV3 Transition,
    bool NonceRegistered,
    bool AuthorizationConsumed,
    bool FenceConsumed,
    bool AuditOutboxCommitted);

public sealed record OracleDescriptorV3(
    string OracleId,
    string SemanticVersion,
    string ArtifactSha256,
    string EvidenceSchemaVersion,
    string SigningIdentity,
    DateTimeOffset ActiveFrom,
    DateTimeOffset? RevokedAt);

public sealed record AuthoritativeReaderDescriptorV3(
    string ReaderId,
    string ReaderVersion,
    string ArtifactSha256,
    string ServiceIdentity,
    string DatabaseRole,
    string SchemaVersion,
    IReadOnlySet<string> AllowedOrganizations,
    IReadOnlySet<string> AllowedResourceTypes,
    IReadOnlySet<string> AllowedFields,
    int MaximumFacts,
    int MaximumBytes);

public sealed record RawEvidenceFactV3(
    string FieldId,
    SelectorValueKind ValueKind,
    string? CanonicalValue);

public sealed record EvidenceScopeTemporalBindingV3(
    CompanyDatabaseScopeV3 Scope,
    string Operation,
    string RequestId,
    string ResourceType,
    string ResourceId,
    long ResourceVersion,
    string AttemptId,
    string LeaseId,
    long FencingToken,
    EvidenceStageV3 Stage,
    string ObservationId,
    DateTimeOffset ObservedAt,
    string SnapshotOrWatermark);

public sealed record AuthoritativeFactBundleV3(
    string ReaderId,
    string ReaderVersion,
    string ReaderArtifactSha256,
    string SchemaVersion,
    EvidenceScopeTemporalBindingV3 Binding,
    IReadOnlyList<RawEvidenceFactV3> Facts,
    string FactsSha256,
    string KeyId,
    string Algorithm,
    ReadOnlyMemory<byte> Signature);

public sealed record CanonicalEvidenceEnvelopeV3(
    string EvidenceEnvelopeId,
    string EvidenceSchemaVersion,
    string OracleId,
    string OracleVersion,
    string OracleArtifactSha256,
    IReadOnlyList<AuthoritativeFactBundleV3> AuthoritativeBundles,
    string CanonicalEnvelopeSha256);

public sealed record CalculatedVerificationV3(
    VerificationDisposition Disposition,
    IReadOnlyList<TrustFailureCodeV2> ReasonCodes,
    string AuthoritativeInputSha256,
    string OracleArtifactSha256,
    DateTimeOffset CalculatedAt);

public sealed record SignedVerdictV3(
    string VerdictId,
    string EvidenceEnvelopeId,
    CalculatedVerificationV3 Calculation,
    string VerifierIdentity,
    string KeyId,
    string Algorithm,
    string SignatureBase64,
    string AuditReference);

public sealed record VerificationFailureV3(
    TrustFailureCodeV2 Code,
    string EvidenceEnvelopeId,
    string AuditReference,
    DateTimeOffset FailedAt);

public sealed record ImmutableAuditEventV3(
    string EventId,
    AuditEventKindV3 Kind,
    string CorrelationId,
    string ActorIdentity,
    string OrganizationId,
    string DatabaseInstanceId,
    string ResourceId,
    string Operation,
    string RequestSha256,
    string PolicyVersion,
    string OutcomeCode,
    string PriorEventSha256,
    DateTimeOffset OccurredAt);

public sealed record DependencyReadinessV3(
    PhaseADependencyV3 Dependency,
    ReadinessDependencyStateV3 State,
    string RequiredVersion,
    string? ObservedVersion,
    string DiagnosticCode);

public sealed record ReadinessSnapshotV3(
    string PolicyVersion,
    IReadOnlyList<DependencyReadinessV3> Dependencies,
    DateTimeOffset CheckedAt)
{
    public bool CanExecuteProtectedOperation =>
        Dependencies.Count == Enum.GetValues<PhaseADependencyV3>().Length &&
        Dependencies.Select(static item => item.Dependency).ToHashSet()
            .SetEquals(Enum.GetValues<PhaseADependencyV3>()) &&
        Dependencies.All(static item => item.State == ReadinessDependencyStateV3.READY);
}

public sealed record DeploymentIdentityDescriptorV3(
    ProductionResponsibilityV3 Responsibility,
    string ServiceIdentity,
    string DatabaseRole,
    string NetworkAudience,
    string TrustStoreReference,
    string KeySourceReference,
    IReadOnlySet<string> AllowedOperations,
    IReadOnlySet<string> DeniedOperations,
    string AuditDestination,
    IReadOnlyDictionary<PhaseADependencyV3, string> RequiredDependencyVersions);

public sealed record OpaquePageTokenBindingV3(
    string Issuer,
    string Subject,
    CompanyDatabaseScopeV3 Scope,
    string ResourceId,
    string QueryVersion,
    string SnapshotOrWatermark,
    int PageSize,
    DateTimeOffset ExpiresAt,
    string PriorPageSha256);

public interface INexaErpBusinessRuntime
{
    ValueTask<string> SubmitBusinessIntentAsync(UntrustedBusinessIntentV3 intent, CancellationToken cancellationToken = default);
}

public interface IControlPlaneAuthority
{
    ValueTask<LifecycleTransitionResultV3> AcceptRawCommandAsync(
        ReadOnlyMemory<byte> canonicalHeader,
        ReadOnlyMemory<byte> canonicalPayload,
        ReadOnlyMemory<byte> signature,
        AuthenticatedWorkloadIdentityV3 transportIdentity,
        CancellationToken cancellationToken = default);
}

public interface IAcceptanceVerifierAuthority
{
    ValueTask<SignedVerdictV3> VerifyAsync(CanonicalEvidenceEnvelopeV3 evidence, CancellationToken cancellationToken = default);
}

public interface ITrustedIssuerKeyRegistryProvider
{
    ValueTask<IssuerTrustPolicyV3?> ResolveIssuerAsync(string issuerId, CancellationToken cancellationToken = default);
    ValueTask<SigningKeyMetadataV3?> ResolveKeyAsync(string issuerId, string keyId, CancellationToken cancellationToken = default);
}

public interface IAudiencePolicyProvider
{
    ValueTask<IReadOnlyList<AudienceOperationPolicyV3>> ResolveAsync(
        string audience,
        string operation,
        string subjectClass,
        CompanyDatabaseScopeV3 scope,
        CancellationToken cancellationToken = default);
}

public interface ITrustedSubjectRoleScopeResolver
{
    ValueTask<ResolvedAuthorizationV3> ResolveAsync(
        AuthenticatedWorkloadIdentityV3 identity,
        UntrustedBusinessIntentV3 intent,
        CancellationToken cancellationToken = default);
}

public interface IAlgorithmVersionPolicyProvider
{
    bool IsAllowed(string contractVersion, string canonicalizationVersion, string algorithm, string purpose);
}

public interface IClockFreshnessPolicyProvider
{
    ClockFreshnessPolicyV3 Policy { get; }
    TrustFailureCodeV2 Validate(
        DateTimeOffset issuedAt,
        DateTimeOffset notBefore,
        DateTimeOffset expiresAt,
        DateTimeOffset serverNow);
}

public interface IKmsHsmSigningProvider
{
    ValueTask<ReadOnlyMemory<byte>> SignAsync(
        TrustedSigningContextV3 trustedContext,
        ReadOnlyMemory<byte> canonicalBytes,
        CancellationToken cancellationToken = default);
    ValueTask<bool> VerifyAsync(
        SigningKeyMetadataV3 key,
        ReadOnlyMemory<byte> canonicalBytes,
        ReadOnlyMemory<byte> signature,
        CancellationToken cancellationToken = default);
}

public interface INonceRegistrationAuthority
{
    ValueTask<ControlTransactionOutcomeV3> RegisterNonceAsync(
        NonceRegistrationV3 nonce,
        CancellationToken cancellationToken = default);
}

public interface IIdempotencyAuthority
{
    ValueTask<ControlTransactionOutcomeV3> ClaimAsync(
        IdempotencyIdentityV3 identity,
        CancellationToken cancellationToken = default);
    ValueTask<LifecycleTransitionResultV3?> ReadCommittedResultAsync(
        IdempotencyIdentityV3 identity,
        CancellationToken cancellationToken = default);
}

public interface ILeaseFenceAuthority
{
    ValueTask<LeaseFenceExpectationV3> AcquireAsync(
        string resourceId,
        string holderSubject,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
    ValueTask<LeaseFenceExpectationV3> RenewAsync(
        LeaseFenceExpectationV3 current,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
    ValueTask<TrustFailureCodeV2> ExpireAsync(
        LeaseFenceExpectationV3 expected,
        DateTimeOffset serverNow,
        CancellationToken cancellationToken = default);
}

public interface ILifecycleStateAuthority
{
    ValueTask<LifecycleTransitionResultV3?> ReadAsync(string resourceId, CancellationToken cancellationToken = default);
}

public interface IAuthorizationStateAuthority { }
public interface IExecutionAttemptAuthority { }
public interface IRecoveryQuarantineAuthority { }
public interface IExportStateAuthority { }
public interface IPurgeStateAuthority { }

public interface IDurableControlPlanePersistenceProvider :
    INonceRegistrationAuthority,
    IIdempotencyAuthority,
    ILeaseFenceAuthority,
    ILifecycleStateAuthority,
    IAuthorizationStateAuthority,
    IExecutionAttemptAuthority,
    IRecoveryQuarantineAuthority,
    IExportStateAuthority,
    IPurgeStateAuthority
{
    string ProviderVersion { get; }
    ValueTask<ControlPlaneTransactionResultV3> ExecuteAtomicallyAsync(
        ControlPlaneTransactionRequestV3 request,
        CancellationToken cancellationToken = default);
}

public interface ILifecycleControllerAuthority
{
    ValueTask<LifecycleTransitionResultV3> TransitionAsync(
        VerifiedLifecycleCommandV3 command,
        CancellationToken cancellationToken = default);
}

public interface IOracleRegistryProvider
{
    ValueTask<OracleDescriptorV3?> ResolveAsync(string oracleId, CancellationToken cancellationToken = default);
}

public interface IAuthoritativeEvidenceReaderProvider
{
    ValueTask<AuthoritativeReaderDescriptorV3?> ResolveAsync(
        string readerId,
        string readerVersion,
        CancellationToken cancellationToken = default);
    ValueTask<AuthoritativeFactBundleV3> ReadAsync(
        AuthoritativeReaderDescriptorV3 reader,
        EvidenceScopeTemporalBindingV3 binding,
        CancellationToken cancellationToken = default);
}

public interface IImmutableAuditEvidenceProvider
{
    ValueTask<DurableAuditAppendReceiptV2> AppendAuditAsync(
        ImmutableAuditEventV3 auditEvent,
        CancellationToken cancellationToken = default);
    ValueTask<DurableAuditAppendReceiptV2> AppendEvidenceAsync(
        string evidenceId,
        string sha256,
        ReadOnlyMemory<byte> canonicalEvidence,
        CancellationToken cancellationToken = default);
}

public interface IBackupRecoveryAuthority
{
    ValueTask<string> RequestRestoreAttestationAsync(
        string databaseInstanceId,
        string backupId,
        CancellationToken cancellationToken = default);
}

public interface IPurgeAuthorizer
{
    ValueTask<ResolvedAuthorizationV3> AuthorizeAsync(
        string candidateRootSha256,
        CompanyDatabaseScopeV3 scope,
        CancellationToken cancellationToken = default);
}

public interface IPurgeExecutor
{
    ValueTask<string> ExecuteAuthorizedBatchAsync(
        ResolvedAuthorizationV3 authorization,
        string batchSha256,
        CancellationToken cancellationToken = default);
}

public interface IExportAuthorizer
{
    ValueTask<ResolvedAuthorizationV3> AuthorizeAsync(
        string minimizedBatchSha256,
        CompanyDatabaseScopeV3 scope,
        CancellationToken cancellationToken = default);
}

public interface IExportDeliveryExecutor
{
    ValueTask<string> DeliverAuthorizedBatchAsync(
        ResolvedAuthorizationV3 authorization,
        string recipientId,
        CancellationToken cancellationToken = default);
}

public interface IReadinessDependencyProvider
{
    PhaseADependencyV3 Dependency { get; }
    ValueTask<DependencyReadinessV3> CheckAsync(CancellationToken cancellationToken = default);
}

public interface IReadinessAuthorityV3
{
    ValueTask<ReadinessSnapshotV3> CheckAsync(CancellationToken cancellationToken = default);
}

public static class PhaseAOwnershipCatalog
{
    private static readonly IReadOnlyDictionary<ProductionResponsibilityV3, Type> Owners =
        new Dictionary<ProductionResponsibilityV3, Type>
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

    public static IReadOnlyDictionary<ProductionResponsibilityV3, Type> All => Owners;
}

public static class PhaseAContractLimits
{
    public const int MaximumIdentifierBytes = 128;
    public const int MaximumCommandEnvelopeBytes = 98_304;
    public const int MaximumEvidenceEnvelopeBytes = 4_194_304;
    public const int MaximumObservations = 512;
    public const int MaximumSelectors = 128;
    public const int MaximumFactsPerObservation = 256;
    public const int MaximumStringBytes = 4_096;
    public const int MaximumCumulativeFactBytes = 2_097_152;
    public const int MaximumPageSize = 1_000;
    public const int MaximumTransientRetries = 3;
}

public static class PhaseAContractValidator
{
    public static void RequireValid(UntrustedBusinessIntentV3 intent)
    {
        RequireIdentifier(intent.RequestId);
        RequireIdentifier(intent.IdempotencyKey);
        RequireIdentifier(intent.Operation);
        RequireIdentifier(intent.ResourceType);
        RequireIdentifier(intent.ResourceId);
        Require(intent.ExpectedResourceVersion > 0, TrustFailureCodeV2.RESOURCE_VERSION_STALE);
        Require(intent.IssuedAt.Offset == TimeSpan.Zero &&
                intent.ExpiresAt.Offset == TimeSpan.Zero &&
                intent.IssuedAt < intent.ExpiresAt,
            TrustFailureCodeV2.ENVELOPE_EXPIRED);
        RequireScope(intent.Scope);
        Require(intent.Parameters.Count <= PhaseAContractLimits.MaximumSelectors,
            TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED);
        Require(intent.Parameters.All(static pair =>
                IsBounded(pair.Key, PhaseAContractLimits.MaximumIdentifierBytes) &&
                IsBounded(pair.Value, PhaseAContractLimits.MaximumStringBytes)),
            TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED);
    }

    public static void RequireValid(AuthorizationBindingV3 binding)
    {
        Require(binding.State == AuthorizationGrantStateV3.ACTIVE,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        RequireScope(binding.Scope);
        RequireIdentifier(binding.ResourceType);
        RequireIdentifier(binding.ResourceId);
        RequireIdentifier(binding.Operation);
        Require(binding.ResourceVersion > 0 &&
                IsLowerSha256(binding.CanonicalRequestSha256) &&
                IsLowerSha256(binding.EvidenceManifestSha256) &&
                binding.Authorization.Operation == binding.Operation &&
                binding.Authorization.NotBefore < binding.Authorization.ExpiresAt,
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        var expectedScope = binding.Scope.ScopeKind == MasterScopeKindV3.COMPANY_LEDGER
            ? $"ORG:{binding.Scope.OrganizationId}"
            : binding.Authorization.TrustedScope;
        Require(binding.Authorization.TrustedScope == expectedScope,
            TrustFailureCodeV2.SCOPE_MISMATCH);
    }

    public static void RequireValid(CanonicalEvidenceEnvelopeV3 evidence)
    {
        RequireIdentifier(evidence.EvidenceEnvelopeId);
        RequireIdentifier(evidence.EvidenceSchemaVersion);
        RequireIdentifier(evidence.OracleId);
        RequireIdentifier(evidence.OracleVersion);
        Require(IsLowerSha256(evidence.OracleArtifactSha256) &&
                IsLowerSha256(evidence.CanonicalEnvelopeSha256),
            TrustFailureCodeV2.EVIDENCE_TAMPERED);
        Require(evidence.AuthoritativeBundles.Count is > 0 and <= PhaseAContractLimits.MaximumObservations,
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        var identities = evidence.AuthoritativeBundles
            .Select(static bundle => $"{bundle.ReaderId}@{bundle.ReaderVersion}:{bundle.Binding.ObservationId}")
            .ToArray();
        Require(identities.Distinct(StringComparer.Ordinal).Count() == identities.Length,
            TrustFailureCodeV2.READER_DUPLICATE);
        foreach (var bundle in evidence.AuthoritativeBundles)
        {
            Require(bundle.Facts.Count <= PhaseAContractLimits.MaximumFactsPerObservation,
                TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
            Require(IsLowerSha256(bundle.ReaderArtifactSha256) && IsLowerSha256(bundle.FactsSha256),
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            RequireScope(bundle.Binding.Scope);
            Require(bundle.Facts.All(static fact =>
                    IsBounded(fact.FieldId, PhaseAContractLimits.MaximumIdentifierBytes) &&
                    (fact.CanonicalValue is null ||
                     IsBounded(fact.CanonicalValue, PhaseAContractLimits.MaximumStringBytes))),
                TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        }
    }

    public static void RequireValid(DeploymentIdentityDescriptorV3 descriptor)
    {
        RequireIdentifier(descriptor.ServiceIdentity);
        RequireIdentifier(descriptor.DatabaseRole);
        RequireIdentifier(descriptor.NetworkAudience);
        RequireIdentifier(descriptor.TrustStoreReference);
        RequireIdentifier(descriptor.KeySourceReference);
        RequireIdentifier(descriptor.AuditDestination);
        Require(descriptor.AllowedOperations.Count > 0 &&
                descriptor.DeniedOperations.Count > 0 &&
                !descriptor.AllowedOperations.Intersect(descriptor.DeniedOperations, StringComparer.Ordinal).Any(),
            TrustFailureCodeV2.DEPENDENCY_POLICY_MISMATCH);
        Require(descriptor.RequiredDependencyVersions.Count > 0 &&
                descriptor.RequiredDependencyVersions.All(static pair =>
                    IsBounded(pair.Value, PhaseAContractLimits.MaximumIdentifierBytes)),
            TrustFailureCodeV2.DEPENDENCY_VERSION_MISMATCH);
    }

    public static TrustFailureCodeV2 FailureFor(ReadinessDependencyStateV3 state) => state switch
    {
        ReadinessDependencyStateV3.READY => TrustFailureCodeV2.NONE,
        ReadinessDependencyStateV3.NOT_CONFIGURED => TrustFailureCodeV2.DEPENDENCY_NOT_CONFIGURED,
        ReadinessDependencyStateV3.UNAVAILABLE => TrustFailureCodeV2.DEPENDENCY_UNAVAILABLE,
        ReadinessDependencyStateV3.VERSION_MISMATCH => TrustFailureCodeV2.DEPENDENCY_VERSION_MISMATCH,
        ReadinessDependencyStateV3.IDENTITY_MISMATCH => TrustFailureCodeV2.DEPENDENCY_IDENTITY_MISMATCH,
        ReadinessDependencyStateV3.POLICY_MISMATCH => TrustFailureCodeV2.DEPENDENCY_POLICY_MISMATCH,
        ReadinessDependencyStateV3.DEGRADED_NOT_SAFE => TrustFailureCodeV2.DEPENDENCY_DEGRADED_UNSAFE,
        _ => TrustFailureCodeV2.SERVICE_NOT_READY
    };

    private static void RequireScope(CompanyDatabaseScopeV3 scope)
    {
        RequireIdentifier(scope.OrganizationId);
        RequireIdentifier(scope.DatabaseClusterId);
        RequireIdentifier(scope.DatabaseInstanceId);
        Require(scope.ScopeKind != MasterScopeKindV3.SHARED_APPROVED_MASTER ||
                !string.IsNullOrWhiteSpace(scope.SharedMasterClass),
            TrustFailureCodeV2.SCOPE_MISMATCH);
        Require(scope.ScopeKind == MasterScopeKindV3.SHARED_APPROVED_MASTER ||
                string.IsNullOrWhiteSpace(scope.SharedMasterClass),
            TrustFailureCodeV2.SCOPE_MISMATCH);
    }

    private static void RequireIdentifier(string value) =>
        Require(IsBounded(value, PhaseAContractLimits.MaximumIdentifierBytes),
            TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED);

    private static bool IsBounded(string value, int maximumBytes) =>
        !string.IsNullOrWhiteSpace(value) &&
        Encoding.UTF8.GetByteCount(value) <= maximumBytes;

    private static bool IsLowerSha256(string value) =>
        value.Length == 64 &&
        value.All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void Require(bool condition, TrustFailureCodeV2 code)
    {
        if (!condition)
        {
            throw new TrustFailureExceptionV2(code, $"Phase A contract rejected: {code}.");
        }
    }
}
