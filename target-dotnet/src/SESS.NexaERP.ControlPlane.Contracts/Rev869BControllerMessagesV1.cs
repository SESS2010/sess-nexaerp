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
