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
    SERVICE_NOT_READY
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
    EvidenceReaderReceiptV2 Receipt);

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
