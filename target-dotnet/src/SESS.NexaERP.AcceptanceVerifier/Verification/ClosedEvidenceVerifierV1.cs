using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SESS.NexaERP.AcceptanceVerifier.Configuration;
using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.AcceptanceVerifier.Verification;

internal interface IEvidenceSignatureVerifier
{
    SigningKeyDescriptor? FindKey(string keyId);
    bool Verify(string keyId, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature);
}

internal interface IClosedOracleV1
{
    string OracleId { get; }
    bool Evaluate(CanonicalEvidenceEnvelopeV1 evidence);
}

internal interface IClosedOracleCatalogV1
{
    IClosedOracleV1? Find(string oracleId);
}

internal interface IVerificationAuditSinkV1
{
    string Append(VerificationAuditEventV1 auditEvent);
}

internal interface IOracleManifestRegistry
{
    OracleManifestV2? Resolve(string oracleId);
}

internal interface IAuthoritativeEvidenceReader
{
    ValueTask<AuthoritativeEvidenceFactsV2> ReadFactsAsync(
        EvidenceReaderDescriptorV2 reader,
        ResourceBindingV2 request,
        CancellationToken cancellationToken = default);
}

internal interface IEvidenceReaderRegistry
{
    EvidenceReaderDescriptorV2? Resolve(string readerId, string version);
}

internal interface IClosedOracleV2
{
    string OracleId { get; }
    string Version { get; }
    (VerificationDisposition Disposition, IReadOnlyList<TrustFailureCodeV2> Reasons) Evaluate(
        CanonicalEvidenceEnvelopeV2 evidence,
        OracleManifestV2 manifest);
}

internal interface IVerificationAuditSinkV2
{
    ValueTask<DurableAuditAppendReceiptV2?> AppendAsync(
        VerificationAuditEventV2 auditEvent,
        CancellationToken cancellationToken = default);
}

internal interface ITrustReadinessProbe
{
    ValueTask<ReadinessResultV2> CheckAsync(CancellationToken cancellationToken = default);
}

internal sealed class ExternalPrerequisiteVerifierReadinessProbeV2(TimeProvider timeProvider) : ITrustReadinessProbe
{
    private static readonly string[] MissingDependencies =
    [
        "CONFIG_MISSING_OR_INVALID",
        "ISSUER_REGISTRY_UNAVAILABLE",
        "KEY_REGISTRY_UNAVAILABLE",
        "ORACLE_NOT_PINNED",
        "READER_SET_INCOMPLETE",
        "IDEMPOTENCY_UNAVAILABLE",
        "AUDIT_WRITER_UNAVAILABLE",
        "ACL_IDENTITY_INVALID"
    ];

    public ValueTask<ReadinessResultV2> CheckAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new ReadinessResultV2(
            ReadinessStateV2.NOT_READY,
            MissingDependencies,
            timeProvider.GetUtcNow()));
}

internal static class StrictEvidenceJsonV2
{
    private static readonly HashSet<string> ProhibitedSemanticFields = new(
        ["pass", "fail", "verdict", "disposition", "expected", "formula"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static CanonicalEvidenceEnvelopeV2 Deserialize(ReadOnlySpan<byte> json)
    {
        try
        {
            using var document = JsonDocument.Parse(json.ToArray());
            RejectProhibitedFields(document.RootElement);
            return JsonSerializer.Deserialize<CanonicalEvidenceEnvelopeV2>(json, Options)
                ?? throw new TrustFailureExceptionV2(
                    TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD,
                    "Evidence JSON did not contain an envelope.");
        }
        catch (TrustFailureExceptionV2)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new TrustFailureExceptionV2(
                TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD,
                "Evidence JSON is malformed or contains unmapped fields.");
        }
    }

    private static void RejectProhibitedFields(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (ProhibitedSemanticFields.Contains(property.Name))
                {
                    throw new TrustFailureExceptionV2(
                        TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD,
                        "Caller-supplied verdict, expectation, or formula fields are forbidden.");
                }
                RejectProhibitedFields(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectProhibitedFields(item);
            }
        }
    }
}

internal sealed class ClosedEvidenceVerifierV1(
    IEvidenceSignatureVerifier signatureVerifier,
    IClosedOracleCatalogV1 oracleCatalog,
    IVerificationAuditSinkV1 auditSink,
    TimeProvider timeProvider)
{
    public VerificationResultV1 Verify(EvidenceVerificationRequestV1 request)
    {
        var evidence = request.Evidence;
        Require(evidence.ContractVersion == Rev869BCompatibilityManifestV1.ContractVersion,
            TrustRejectionCode.UnsupportedContractVersion);
        Require(evidence.EvidenceVersion == Rev869BCompatibilityManifestV1.EvidenceVersion,
            TrustRejectionCode.UnsupportedEvidenceVersion);
        Require(evidence.Signature.CanonicalizationVersion == Rev869BCompatibilityManifestV1.CanonicalizationVersion,
            TrustRejectionCode.UnsupportedCanonicalizationVersion);
        Require(evidence.Signature.Algorithm == Rev869BCompatibilityManifestV1.SignatureAlgorithm,
            TrustRejectionCode.UnsupportedSignatureAlgorithm);
        RequireExactBinding(evidence.Binding, request.ExpectedBinding);
        Require(!string.IsNullOrWhiteSpace(evidence.EvidenceEnvelopeId) &&
                !string.IsNullOrWhiteSpace(evidence.OracleVersion) &&
                evidence.OracleSha256.Length == 64 &&
                evidence.OracleSha256.All(Uri.IsHexDigit),
            TrustRejectionCode.WrongOracle);
        Require(request.MaxObservations is >= 3 and <= 10_000 &&
                request.MaxFactsPerObservation is >= 1 and <= 1_000 &&
                evidence.Observations.Count <= request.MaxObservations &&
                evidence.Observations.All(observation => observation.Facts.Count <= request.MaxFactsPerObservation),
            TrustRejectionCode.PayloadLimitExceeded);
        Require(Enum.GetValues<ObservationStage>().All(stage => evidence.Observations.Any(item => item.Stage == stage)),
            TrustRejectionCode.MissingObservationStage);
        Require(evidence.Observations.Select(static item => item.ObservationId).Distinct(StringComparer.Ordinal).Count() == evidence.Observations.Count,
            TrustRejectionCode.DuplicateObservation);
        Require(evidence.Observations.All(static item =>
                !string.IsNullOrWhiteSpace(item.Provenance.SourceIdentity) &&
                item.Provenance.SourceKind is not ObservationSourceKind.ControllerLedger || item.Stage == ObservationStage.Durable),
            TrustRejectionCode.InvalidProvenance);
        Require(evidence.Selectors.Count > 0 && evidence.Selectors.All(static selector =>
                !string.IsNullOrWhiteSpace(selector.Field) &&
                !string.IsNullOrWhiteSpace(selector.Operator) &&
                selector.Reader is not null &&
                !string.IsNullOrWhiteSpace(selector.Reader.ReaderId) &&
                !string.IsNullOrWhiteSpace(selector.Reader.ReaderContractVersion) &&
                (selector.Expected.Kind == SelectorValueKind.Null) == (selector.Expected.CanonicalValue is null)),
            TrustRejectionCode.InvalidSelector);
        Require(!string.IsNullOrWhiteSpace(evidence.ActionResult.ResultState) &&
                !string.IsNullOrWhiteSpace(evidence.ActionResult.EvidenceReference) &&
                evidence.ActionResult.AffectedRows >= 0,
            TrustRejectionCode.IncompleteActionResult);

        var key = signatureVerifier.FindKey(evidence.Signature.KeyId);
        Require(key is not null, TrustRejectionCode.UnknownKey);
        Require(key!.State != SigningKeyState.Revoked, TrustRejectionCode.RevokedKey);
        var now = timeProvider.GetUtcNow();
        Require(key.State == SigningKeyState.Active && key.NotBeforeUtc <= now && (key.NotAfterUtc is null || key.NotAfterUtc >= now),
            TrustRejectionCode.ExpiredKey);

        var unsignedEvidence = evidence with { Signature = evidence.Signature with { PayloadSha256 = string.Empty, SignatureBase64 = string.Empty } };
        var canonicalPayload = CanonicalJsonV1.Serialize(unsignedEvidence);
        byte[] declaredHash;
        byte[] signature;
        try
        {
            declaredHash = Convert.FromHexString(evidence.Signature.PayloadSha256);
            signature = Convert.FromBase64String(evidence.Signature.SignatureBase64);
        }
        catch (FormatException exception)
        {
            throw new TrustRejectionException(TrustRejectionCode.InvalidSignature, exception.Message);
        }

        Require(CryptographicOperations.FixedTimeEquals(declaredHash, SHA256.HashData(canonicalPayload)),
            TrustRejectionCode.PayloadHashMismatch);
        Require(signatureVerifier.Verify(key.KeyId, canonicalPayload, signature), TrustRejectionCode.InvalidSignature);

        var oracle = oracleCatalog.Find(evidence.Binding.OracleId);
        Require(oracle is not null && oracle.OracleId == request.ExpectedBinding.OracleId, TrustRejectionCode.WrongOracle);
        var disposition = oracle!.Evaluate(evidence) ? VerificationDisposition.Passed : VerificationDisposition.Failed;
        var rejectionCodes = disposition == VerificationDisposition.Passed
            ? Array.Empty<TrustRejectionCode>()
            : [TrustRejectionCode.IncompleteActionResult];
        var auditEvent = new VerificationAuditEventV1(
            Guid.NewGuid().ToString("N"), evidence.Binding, disposition, rejectionCodes, now);
        var auditReference = auditSink.Append(auditEvent);
        return new VerificationResultV1(disposition, oracle.OracleId, rejectionCodes, auditReference, now);
    }

    private static void RequireExactBinding(Rev869BExecutionBinding actual, Rev869BExecutionBinding expected)
    {
        Require(actual.Company == expected.Company, TrustRejectionCode.WrongCompany);
        Require(actual.ControlPlane == expected.ControlPlane && actual.Target == expected.Target, TrustRejectionCode.WrongTargetInstance);
        Require(actual.LeaseId == expected.LeaseId && actual.LeaseVersion == expected.LeaseVersion, TrustRejectionCode.WrongLease);
        Require(actual.ExecutionId == expected.ExecutionId && actual.OperationId == expected.OperationId &&
                actual.PreparationId == expected.PreparationId && actual.AttemptId == expected.AttemptId && actual.ActionId == expected.ActionId,
            TrustRejectionCode.WrongExecution);
        Require(actual.ScenarioId == expected.ScenarioId, TrustRejectionCode.WrongScenario);
        Require(actual.SubcaseId == expected.SubcaseId, TrustRejectionCode.WrongSubcase);
        Require(actual.OracleId == expected.OracleId, TrustRejectionCode.WrongOracle);
    }

    private static void Require(bool condition, TrustRejectionCode code)
    {
        if (!condition)
        {
            throw new TrustRejectionException(code, $"Evidence rejected: {code}.");
        }
    }
}

internal sealed class ClosedEvidenceVerifierV2(
    AcceptanceVerifierOptions options,
    ITrustReadinessProbe readinessProbe,
    IOracleManifestRegistry oracleManifestRegistry,
    IEvidenceReaderRegistry readerRegistry,
    IAuthoritativeEvidenceReader authoritativeReader,
    IClosedOracleV2 oracle,
    IVerificationAuditSinkV2 auditSink,
    TimeProvider timeProvider)
{
    public async ValueTask<VerificationResultV2> VerifyAsync(
        EvidenceVerificationRequestV2 request,
        CancellationToken cancellationToken = default)
    {
        var readiness = await readinessProbe.CheckAsync(cancellationToken);
        Require(readiness.State == ReadinessStateV2.READY && readiness.DependencyCodes.Count == 0,
            TrustFailureCodeV2.SERVICE_NOT_READY);
        Require(AcceptanceVerifierOptions.IsValid(options), TrustFailureCodeV2.SERVICE_NOT_READY);

        var evidence = request.Evidence;
        var canonicalBytes = CanonicalJsonV1.Serialize(evidence);
        Require(canonicalBytes.Length <= options.MaximumEnvelopeBytes, TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        Require(request.Context.Issuer == options.IssuerId &&
                request.Context.Subject == options.ServiceIdentity &&
                request.Context.KeyId == options.KeyId,
            TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        RequireExact(evidence.Binding, request.Context.ExpectedBinding);
        Require(options.AllowedClusterIds.Contains(evidence.Binding.DatabaseClusterId, StringComparer.Ordinal),
            TrustFailureCodeV2.CLUSTER_MISMATCH);
        Require(options.AllowedInstanceIds.Contains(evidence.Binding.DatabaseInstanceId, StringComparer.Ordinal),
            TrustFailureCodeV2.INSTANCE_MISMATCH);
        Require(evidence.Lease == request.Context.ExpectedLease, TrustFailureCodeV2.LEASE_FENCE_STALE);
        Require(evidence.RequestId == request.Context.RequestId, TrustFailureCodeV2.READER_UNAUTHORIZED);

        var manifest = oracleManifestRegistry.Resolve(options.OracleId);
        Require(manifest is not null &&
                manifest.OracleId == options.OracleId &&
                manifest.SemanticVersion == options.OracleVersion &&
                manifest.ArtifactSha256 == options.OracleArtifactSha256 &&
                manifest.RevokedAt is null,
            TrustFailureCodeV2.ORACLE_MISMATCH);
        Require(evidence.OracleId == manifest!.OracleId &&
                evidence.OracleVersion == manifest.SemanticVersion &&
                evidence.OracleArtifactSha256 == manifest.ArtifactSha256,
            TrustFailureCodeV2.ORACLE_MISMATCH);

        var receiptKeySequence = evidence.ReaderReceipts
            .Select(static receipt => $"{receipt.ReaderId}@{receipt.ReaderVersion}")
            .ToArray();
        Require(receiptKeySequence.Distinct(StringComparer.Ordinal).Count() == receiptKeySequence.Length,
            TrustFailureCodeV2.READER_DUPLICATE);
        var receiptKeys = receiptKeySequence.ToHashSet(StringComparer.Ordinal);
        var requiredKeys = manifest.AllowedReaderVersions
            .Select(static pair => $"{pair.Key}@{pair.Value}")
            .ToHashSet(StringComparer.Ordinal);
        Require(requiredKeys.SetEquals(receiptKeys), TrustFailureCodeV2.READER_MISSING);

        var authoritativeReceipts = new List<EvidenceReaderReceiptV2>();
        var authoritativeObservations = new List<FactOnlyObservationV1>();
        foreach (var receipt in evidence.ReaderReceipts)
        {
            var descriptor = readerRegistry.Resolve(receipt.ReaderId, receipt.ReaderVersion);
            Require(descriptor is not null, TrustFailureCodeV2.READER_MISSING);
            Require(descriptor!.ArtifactSha256 == receipt.ReaderArtifactSha256 &&
                    descriptor.AllowedOrganizations.Contains(evidence.Binding.OrganizationId) &&
                    descriptor.AllowedResources.Contains(evidence.Binding.ResourceId),
                TrustFailureCodeV2.READER_UNAUTHORIZED);
            var facts = await authoritativeReader.ReadFactsAsync(descriptor, evidence.Binding, cancellationToken);
            Require(facts.Receipt.ReaderId == receipt.ReaderId &&
                    facts.Receipt.ReaderVersion == receipt.ReaderVersion &&
                    facts.Receipt.ResponseDigest == receipt.ResponseDigest,
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            Require(facts.Observations is { Count: > 0 }, TrustFailureCodeV2.READER_MISSING);
            authoritativeReceipts.Add(facts.Receipt);
            authoritativeObservations.AddRange(facts.Observations!);
        }

        var unsigned = evidence with { PayloadSha256 = string.Empty };
        var actualHash = Convert.ToHexString(SHA256.HashData(CanonicalJsonV1.Serialize(unsigned))).ToLowerInvariant();
        Require(string.Equals(evidence.PayloadSha256, actualHash, StringComparison.Ordinal),
            TrustFailureCodeV2.PAYLOAD_HASH_MISMATCH);

        var authoritativeEvidence = evidence with
        {
            RawFacts = authoritativeObservations,
            ReaderReceipts = authoritativeReceipts,
            PayloadSha256 = string.Empty
        };
        ValidateEvidenceDimensions(authoritativeEvidence);
        ValidateObservationTimes(authoritativeEvidence);
        var authoritativeHash = Convert.ToHexString(
            SHA256.HashData(CanonicalJsonV1.Serialize(authoritativeEvidence))).ToLowerInvariant();
        authoritativeEvidence = authoritativeEvidence with { PayloadSha256 = authoritativeHash };

        Require(oracle.OracleId == manifest.OracleId && oracle.Version == manifest.SemanticVersion,
            TrustFailureCodeV2.ORACLE_MISMATCH);
        var calculated = oracle.Evaluate(authoritativeEvidence, manifest);
        var now = timeProvider.GetUtcNow();
        var auditEvent = new VerificationAuditEventV2(
            Guid.NewGuid().ToString("N"),
            request.Context.Issuer,
            request.Context.Subject,
            request.Context.KeyId,
            evidence.RequestId,
            evidence.EvidenceEnvelopeId,
            authoritativeHash,
            evidence.Binding,
            evidence.Lease,
            manifest.OracleId,
            manifest.SemanticVersion,
            manifest.ArtifactSha256,
            authoritativeReceipts.Select(static item => item.ResponseDigest).ToArray(),
            calculated.Disposition,
            calculated.Reasons,
            now);
        var auditReceipt = await auditSink.AppendAsync(auditEvent, cancellationToken);
        Require(auditReceipt is not null &&
                auditReceipt.EventId == auditEvent.EventId &&
                !string.IsNullOrWhiteSpace(auditReceipt.DurableReference) &&
                !string.IsNullOrWhiteSpace(auditReceipt.EventSha256),
            TrustFailureCodeV2.AUDIT_APPEND_FAILED);
        return new(calculated.Disposition, manifest.OracleId, manifest.SemanticVersion,
            calculated.Reasons, auditReceipt!, now);
    }

    private void ValidateEvidenceDimensions(CanonicalEvidenceEnvelopeV2 evidence)
    {
        Require(evidence.RawFacts.Count is >= 3 &&
                evidence.RawFacts.Count <= options.MaximumObservations,
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        Require(evidence.RawFacts.Select(static item => item.ObservationId)
                .Distinct(StringComparer.Ordinal).Count() == evidence.RawFacts.Count,
            TrustFailureCodeV2.READER_DUPLICATE);
        Require(evidence.RawFacts.All(observation =>
                observation.Facts.Count <= options.MaximumFactsPerObservation),
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);

        var allFacts = evidence.RawFacts.SelectMany(static observation => observation.Facts).ToArray();
        Require(allFacts.All(item => options.AllowedFactFields.Contains(item.Key, StringComparer.Ordinal)),
            TrustFailureCodeV2.EVIDENCE_SENSITIVE_FIELD);
        Require(allFacts.All(item => !options.SensitiveFieldNames.Contains(item.Key, StringComparer.OrdinalIgnoreCase)),
            TrustFailureCodeV2.EVIDENCE_SENSITIVE_FIELD);
        Require(allFacts.All(item =>
                Encoding.UTF8.GetByteCount(item.Key) <= options.MaximumStringBytes &&
                (item.Value.CanonicalValue is null ||
                 Encoding.UTF8.GetByteCount(item.Value.CanonicalValue) <= options.MaximumStringBytes)),
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        var cumulativeBytes = allFacts.Sum(item =>
            Encoding.UTF8.GetByteCount(item.Key) +
            (item.Value.CanonicalValue is null ? 0 : Encoding.UTF8.GetByteCount(item.Value.CanonicalValue)));
        Require(cumulativeBytes <= options.MaximumCumulativeFactBytes, TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
    }

    private void ValidateObservationTimes(CanonicalEvidenceEnvelopeV2 evidence)
    {
        var now = timeProvider.GetUtcNow();
        var skew = TimeSpan.FromSeconds(options.MaximumClockSkewSeconds);
        Require(evidence.ObservationWindowStart <= evidence.ObservationWindowEnd &&
                evidence.ActionOccurredAt >= evidence.ObservationWindowStart &&
                evidence.ActionOccurredAt <= evidence.ObservationWindowEnd &&
                evidence.ObservationWindowEnd - evidence.ObservationWindowStart <=
                    TimeSpan.FromSeconds(options.MaximumObservationWindowSeconds) &&
                evidence.ObservationWindowEnd <= now + skew,
            TrustFailureCodeV2.NOT_YET_VALID);
        Require(evidence.RawFacts.All(item =>
                item.Provenance.ObservedAtUtc >= evidence.ObservationWindowStart &&
                item.Provenance.ObservedAtUtc <= evidence.ObservationWindowEnd &&
                !string.IsNullOrWhiteSpace(item.Provenance.SourceIdentity) &&
                (item.Provenance.SourceKind != ObservationSourceKind.ControllerLedger ||
                 item.Stage == ObservationStage.Durable)),
            TrustFailureCodeV2.READER_UNAUTHORIZED);
        Require(Enum.GetValues<ObservationStage>().All(stage =>
                evidence.RawFacts.Any(item => item.Stage == stage)),
            TrustFailureCodeV2.READER_MISSING);
        Require(evidence.RawFacts
                .Where(static item => item.Stage == ObservationStage.Before)
                .All(item => item.Provenance.ObservedAtUtc < evidence.ActionOccurredAt) &&
                evidence.RawFacts
                .Where(static item => item.Stage is ObservationStage.After or ObservationStage.Durable)
                .All(item => item.Provenance.ObservedAtUtc > evidence.ActionOccurredAt),
            TrustFailureCodeV2.READER_UNAUTHORIZED);
    }

    private static void RequireExact(ResourceBindingV2 actual, ResourceBindingV2 expected)
    {
        Require(actual.OrganizationId == expected.OrganizationId, TrustFailureCodeV2.ORGANIZATION_MISMATCH);
        Require(actual.DatabaseClusterId == expected.DatabaseClusterId, TrustFailureCodeV2.CLUSTER_MISMATCH);
        Require(actual.DatabaseInstanceId == expected.DatabaseInstanceId, TrustFailureCodeV2.INSTANCE_MISMATCH);
        Require(actual.Operation == expected.Operation, TrustFailureCodeV2.OPERATION_MISMATCH);
        Require(actual.ResourceId == expected.ResourceId, TrustFailureCodeV2.INSTANCE_MISMATCH);
        Require(actual.ExpectedResourceVersion == expected.ExpectedResourceVersion, TrustFailureCodeV2.RESOURCE_VERSION_STALE);
    }

    private static void Require(bool condition, TrustFailureCodeV2 code)
    {
        if (!condition)
        {
            throw new TrustFailureExceptionV2(code, $"Evidence rejected: {code}.");
        }
    }
}

public sealed class PhaseAAcceptanceVerifierAuthority(
    AcceptanceVerifierOptions options,
    IReadinessAuthorityV3 readiness,
    IOracleRegistryProvider oracleRegistry,
    IAuthoritativeEvidenceReaderProvider readerRegistry,
    ITrustedIssuerKeyRegistryProvider keyRegistry,
    IKmsHsmSigningProvider kms,
    IImmutableAuditEvidenceProvider audit,
    TimeProvider timeProvider) : IAcceptanceVerifierAuthority
{
    private static readonly JsonSerializerOptions StrictJson = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 64
    };

    public async ValueTask<SignedVerdictV3> VerifyRawAsync(
        ReadOnlyMemory<byte> canonicalEvidence,
        AuthenticatedWorkloadIdentityV3 transportIdentity,
        CancellationToken cancellationToken = default)
    {
        Require(AcceptanceVerifierOptions.IsValid(options), TrustFailureCodeV2.SERVICE_NOT_READY);
        Require(canonicalEvidence.Length is > 0 &&
                canonicalEvidence.Length <= Math.Min(options.MaximumEnvelopeBytes, PhaseAContractLimits.MaximumEvidenceEnvelopeBytes),
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);

        var readinessSnapshot = await readiness.CheckAsync(cancellationToken);
        Require(readinessSnapshot.PolicyVersion == options.ReadinessPolicyVersion &&
                readinessSnapshot.CanExecuteProtectedOperation,
            TrustFailureCodeV2.SERVICE_NOT_READY);
        Require(transportIdentity.TransportAudience == options.Audience &&
                options.AllowedCallerWorkloadIdentities.Contains(
                    transportIdentity.WorkloadIdentity,
                    StringComparer.Ordinal) &&
                !string.IsNullOrWhiteSpace(transportIdentity.CredentialBindingSha256),
            TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);

        CanonicalEvidenceEnvelopeV3 evidence;
        try
        {
            evidence = JsonSerializer.Deserialize<CanonicalEvidenceEnvelopeV3>(
                canonicalEvidence.Span,
                StrictJson) ?? throw new JsonException("Evidence is empty.");
        }
        catch (JsonException)
        {
            throw Failure(TrustFailureCodeV2.EVIDENCE_UNMAPPED_FIELD);
        }
        var reserialized = CanonicalJsonV1.Serialize(evidence);
        Require(reserialized.Length == canonicalEvidence.Length &&
                CryptographicOperations.FixedTimeEquals(reserialized, canonicalEvidence.Span),
            TrustFailureCodeV2.EVIDENCE_TAMPERED);
        Require(evidence.EvidenceSchemaVersion == options.EvidenceSchemaVersion &&
                evidence.CanonicalEnvelopeSha256 == PhaseAEvidenceCanonicalizer.EnvelopeSha256(evidence),
            TrustFailureCodeV2.EVIDENCE_TAMPERED);
        Require(evidence.AuthoritativeBundles.Count <= options.MaximumObservations &&
                EvidenceStringsAreBounded(evidence, options.MaximumStringBytes),
            TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
        var requiredReaders = options.RequiredReaderIds.ToHashSet(StringComparer.Ordinal);
        var readerGroups = evidence.AuthoritativeBundles
            .GroupBy(static bundle => bundle.ReaderId, StringComparer.Ordinal)
            .ToArray();
        var actualReaders = readerGroups.Select(static group => group.Key).ToHashSet(StringComparer.Ordinal);
        Require(evidence.AuthoritativeBundles.Count == options.RequiredReaderIds.Length &&
                readerGroups.All(static group => group.Count() == 1) &&
                requiredReaders.SetEquals(actualReaders),
            readerGroups.Any(static group => group.Count() != 1)
                ? TrustFailureCodeV2.READER_DUPLICATE
                : TrustFailureCodeV2.READER_MISSING);
        PhaseAContractValidator.RequireValid(evidence);

        var expectation = await readerRegistry.ResolveExpectationAsync(
            evidence.EvidenceEnvelopeId,
            transportIdentity,
            cancellationToken);
        Require(expectation is not null &&
                expectation.EvidenceEnvelopeId == evidence.EvidenceEnvelopeId &&
                expectation.PolicyVersion == options.ReadinessPolicyVersion &&
                expectation.ResourceVersion > 0 &&
                !string.IsNullOrWhiteSpace(expectation.RequestId) &&
                !string.IsNullOrWhiteSpace(expectation.ResourceType) &&
                !string.IsNullOrWhiteSpace(expectation.ResourceId) &&
                !string.IsNullOrWhiteSpace(expectation.AttemptId) &&
                !string.IsNullOrWhiteSpace(expectation.LeaseId) &&
                options.AllowedClusterIds.Contains(expectation.Scope.DatabaseClusterId, StringComparer.Ordinal) &&
                options.AllowedInstanceIds.Contains(expectation.Scope.DatabaseInstanceId, StringComparer.Ordinal),
            TrustFailureCodeV2.AUTHORIZATION_BINDING_MISMATCH);
        var trustedExpectation = expectation!;

        var oracle = await oracleRegistry.ResolveAsync(evidence.OracleId, cancellationToken);
        Require(oracle is not null &&
                oracle.OracleId == options.OracleId &&
                oracle.SemanticVersion == options.OracleVersion &&
                oracle.ArtifactSha256 == options.OracleArtifactSha256 &&
                oracle.EvidenceSchemaVersion == options.EvidenceSchemaVersion &&
                oracle.RevokedAt is null &&
                oracle.ActiveFrom <= timeProvider.GetUtcNow(),
            TrustFailureCodeV2.ORACLE_MISMATCH);

        EvidenceScopeTemporalBindingV3? commonBinding = null;
        var now = timeProvider.GetUtcNow();
        var maximumAge = TimeSpan.FromSeconds(options.MaximumObservationWindowSeconds);
        var clockSkew = TimeSpan.FromSeconds(options.MaximumClockSkewSeconds);
        var cumulativeBytes = 0;
        var verifiedBundles = new List<AuthoritativeFactBundleV3>(evidence.AuthoritativeBundles.Count);
        foreach (var declaredBundle in evidence.AuthoritativeBundles)
        {
            var descriptor = await readerRegistry.ResolveAsync(
                declaredBundle.ReaderId,
                declaredBundle.ReaderVersion,
                cancellationToken);
            Require(descriptor is not null, TrustFailureCodeV2.READER_MISSING);
            AuthoritativeFactBundleV3 bundle;
            try
            {
                bundle = await readerRegistry.ReadAsync(
                    descriptor!,
                    trustedExpectation,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw Failure(TrustFailureCodeV2.READER_UNAUTHORIZED);
            }
            catch (Exception)
            {
                throw Failure(TrustFailureCodeV2.READER_UNAUTHORIZED);
            }
            var declaredBytes = CanonicalJsonV1.Serialize(declaredBundle);
            var authoritativeBytes = CanonicalJsonV1.Serialize(bundle);
            Require(declaredBytes.Length == authoritativeBytes.Length &&
                    CryptographicOperations.FixedTimeEquals(declaredBytes, authoritativeBytes),
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            Require(descriptor!.ReaderId == bundle.ReaderId &&
                    descriptor.ReaderVersion == bundle.ReaderVersion &&
                    descriptor.ArtifactSha256 == bundle.ReaderArtifactSha256 &&
                    descriptor.SchemaVersion == bundle.SchemaVersion &&
                    descriptor.RequiredStage == bundle.Binding.Stage &&
                    BindingMatchesExpectation(bundle.Binding, trustedExpectation) &&
                    descriptor.AllowedOrganizations.Contains(bundle.Binding.Scope.OrganizationId) &&
                    descriptor.AllowedResourceTypes.Contains(bundle.Binding.ResourceType),
                TrustFailureCodeV2.READER_UNAUTHORIZED);
            Require(options.AllowedClusterIds.Contains(bundle.Binding.Scope.DatabaseClusterId, StringComparer.Ordinal),
                TrustFailureCodeV2.CLUSTER_MISMATCH);
            Require(options.AllowedInstanceIds.Contains(bundle.Binding.Scope.DatabaseInstanceId, StringComparer.Ordinal),
                TrustFailureCodeV2.INSTANCE_MISMATCH);
            Require(bundle.Binding.ObservedAt <= now + clockSkew &&
                    now - bundle.Binding.ObservedAt <= maximumAge &&
                    !string.IsNullOrWhiteSpace(bundle.Binding.AttemptId) &&
                    !string.IsNullOrWhiteSpace(bundle.Binding.SnapshotOrWatermark),
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            if (commonBinding is null)
            {
                commonBinding = bundle.Binding;
            }
            else
            {
                Require(SameOperationBinding(commonBinding, bundle.Binding),
                    TrustFailureCodeV2.EVIDENCE_TAMPERED);
            }

            Require(bundle.Facts.All(fact =>
                    !options.SensitiveFieldNames.Contains(fact.FieldId, StringComparer.OrdinalIgnoreCase)),
                TrustFailureCodeV2.EVIDENCE_SENSITIVE_FIELD);
            Require(bundle.Facts.Count <= Math.Min(descriptor.MaximumFacts, options.MaximumFactsPerObservation) &&
                    bundle.Facts.All(fact =>
                        descriptor.AllowedFields.Contains(fact.FieldId) &&
                        options.AllowedFactFields.Contains(fact.FieldId, StringComparer.Ordinal)),
                TrustFailureCodeV2.READER_UNAUTHORIZED);

            var factBytes = PhaseAEvidenceCanonicalizer.FactPayload(bundle);
            cumulativeBytes += factBytes.Length;
            Require(factBytes.Length <= descriptor.MaximumBytes &&
                    cumulativeBytes <= options.MaximumCumulativeFactBytes,
                TrustFailureCodeV2.EVIDENCE_TOO_LARGE);
            Require(bundle.FactsSha256 == PhaseAEvidenceCanonicalizer.FactPayloadSha256(bundle),
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            var readerKey = await keyRegistry.ResolveKeyAsync(
                bundle.ReaderId,
                bundle.KeyId,
                cancellationToken);
            Require(readerKey is not null &&
                    readerKey.IssuerId == bundle.ReaderId &&
                    readerKey.Algorithm == bundle.Algorithm &&
                    readerKey.RevokedAt is null &&
                    readerKey.NotBefore <= now &&
                    (readerKey.NotAfter is null || readerKey.NotAfter >= now),
                TrustFailureCodeV2.READER_UNAUTHORIZED);
            Require(await kms.VerifyAsync(readerKey!, factBytes, bundle.Signature, cancellationToken),
                TrustFailureCodeV2.EVIDENCE_TAMPERED);
            verifiedBundles.Add(bundle);
        }

        var pinnedOracle = oracle!;
        var evaluation = await oracleRegistry.EvaluateAsync(
            pinnedOracle,
            verifiedBundles,
            cancellationToken);
        var authoritativeInputSha = Convert.ToHexString(
            SHA256.HashData(CanonicalJsonV1.Serialize(verifiedBundles))).ToLowerInvariant();
        var calculation = new CalculatedVerificationV3(
            evaluation.Disposition,
            evaluation.ReasonCodes,
            authoritativeInputSha,
            pinnedOracle.ArtifactSha256,
            now);

        var evidenceReceipt = await audit.AppendEvidenceAsync(
            evidence.EvidenceEnvelopeId,
            evidence.CanonicalEnvelopeSha256,
            canonicalEvidence,
            cancellationToken);
        var durableEvidenceReceipt = evidenceReceipt ??
            throw Failure(TrustFailureCodeV2.AUDIT_APPEND_FAILED);
        Require(durableEvidenceReceipt.EventId == evidence.EvidenceEnvelopeId &&
                !string.IsNullOrWhiteSpace(durableEvidenceReceipt.DurableReference) &&
                durableEvidenceReceipt.EventSha256 == evidence.CanonicalEnvelopeSha256 &&
                durableEvidenceReceipt.AppendedAt <= now,
            TrustFailureCodeV2.AUDIT_APPEND_FAILED);
        var prior = await audit.ReadCurrentChainHeadSha256Async(cancellationToken);
        Require(IsSha256(prior), TrustFailureCodeV2.AUDIT_APPEND_FAILED);
        var binding = commonBinding!;
        var signingKey = await keyRegistry.ResolveKeyAsync(options.IssuerId, options.KeyId, cancellationToken);
        Require(signingKey is not null &&
                signingKey.IssuerId == options.IssuerId &&
                signingKey.Algorithm == Rev869BCompatibilityManifestV2.SignatureAlgorithm &&
                signingKey.RevokedAt is null &&
                signingKey.NotBefore <= now &&
                (signingKey.NotAfter is null || signingKey.NotAfter >= now),
            TrustFailureCodeV2.KEY_UNKNOWN);
        var auditEvent = new ImmutableAuditEventV3(
            $"verify:{evidence.EvidenceEnvelopeId}",
            AuditEventKindV3.VERIFIER_CALCULATED,
            binding.RequestId,
            options.ServiceIdentity,
            binding.Scope.OrganizationId,
            binding.Scope.DatabaseInstanceId,
            binding.ResourceId,
            binding.Operation,
            authoritativeInputSha,
            options.ReadinessPolicyVersion,
            evaluation.Disposition.ToString(),
            prior,
            now,
            AttemptId: binding.AttemptId,
            LeaseId: binding.LeaseId,
            FencingToken: binding.FencingToken,
            SourceTransactionId: durableEvidenceReceipt.DurableReference,
            SigningKeyId: signingKey!.KeyId,
            SigningKeyVersion: signingKey.KeyVersion);
        PhaseAContractValidator.RequireValid(auditEvent);
        var auditReceipt = await audit.AppendAuditAsync(auditEvent, cancellationToken);
        var durableAuditReceipt = auditReceipt ??
            throw Failure(TrustFailureCodeV2.AUDIT_APPEND_FAILED);
        var expectedAuditSha256 = Convert.ToHexString(
            SHA256.HashData(CanonicalJsonV1.Serialize(auditEvent))).ToLowerInvariant();
        Require(durableAuditReceipt.EventId == auditEvent.EventId &&
                !string.IsNullOrWhiteSpace(durableAuditReceipt.DurableReference) &&
                durableAuditReceipt.EventSha256 == expectedAuditSha256 &&
                durableAuditReceipt.AppendedAt <= now,
            TrustFailureCodeV2.AUDIT_APPEND_FAILED);

        var serverAuthorization = new ResolvedAuthorizationV3(
            $"verifier:{evidence.EvidenceEnvelopeId}",
            options.IssuerId,
            options.ServiceIdentity,
            options.ServiceIdentity,
            options.Audience,
            binding.Operation,
            "AcceptanceVerifier",
            $"ORG:{binding.Scope.OrganizationId}",
            options.ReadinessPolicyVersion,
            "acceptance-verifier-policy",
            pinnedOracle.ArtifactSha256,
            now - clockSkew,
            now + maximumAge);
        var signingIdentity = new AuthenticatedWorkloadIdentityV3(
            options.IssuerId,
            options.ServiceIdentity,
            options.ServiceIdentity,
            options.Audience,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(options.ServiceIdentity))).ToLowerInvariant());
        var verifierKey = signingKey!;
        var signingContext = new TrustedSigningContextV3(
            serverAuthorization,
            signingIdentity,
            verifierKey,
            binding.Scope,
            binding.ResourceType,
            binding.ResourceId,
            binding.ResourceVersion,
            binding.LeaseId,
            binding.FencingToken,
            binding.RequestId,
            evidence.EvidenceEnvelopeId,
            binding.ObservationId,
            now,
            now - clockSkew,
            now + maximumAge);
        var verdictBytes = CanonicalJsonV1.Serialize(calculation);
        var verdictSignature = await kms.SignAsync(signingContext, verdictBytes, cancellationToken);
        return new(
            $"verdict:{evidence.EvidenceEnvelopeId}",
            evidence.EvidenceEnvelopeId,
            calculation,
            options.ServiceIdentity,
            verifierKey.KeyId,
            verifierKey.Algorithm,
            Convert.ToBase64String(verdictSignature.Span),
            durableAuditReceipt.DurableReference);
    }

    private static bool SameOperationBinding(
        EvidenceScopeTemporalBindingV3 left,
        EvidenceScopeTemporalBindingV3 right) =>
        left.Scope == right.Scope &&
        left.Operation == right.Operation &&
        left.RequestId == right.RequestId &&
        left.ResourceType == right.ResourceType &&
        left.ResourceId == right.ResourceId &&
        left.ResourceVersion == right.ResourceVersion &&
        left.AttemptId == right.AttemptId &&
        left.LeaseId == right.LeaseId &&
        left.FencingToken == right.FencingToken &&
        left.Stage == right.Stage;

    private static bool BindingMatchesExpectation(
        EvidenceScopeTemporalBindingV3 binding,
        EvidenceVerificationExpectationV3 expectation) =>
        binding.Scope == expectation.Scope &&
        binding.Operation == expectation.Operation &&
        binding.RequestId == expectation.RequestId &&
        binding.ResourceType == expectation.ResourceType &&
        binding.ResourceId == expectation.ResourceId &&
        binding.ResourceVersion == expectation.ResourceVersion &&
        binding.AttemptId == expectation.AttemptId &&
        binding.LeaseId == expectation.LeaseId &&
        binding.FencingToken == expectation.FencingToken &&
        binding.Stage == expectation.Stage &&
        binding.SnapshotOrWatermark == expectation.SnapshotOrWatermark;

    private static bool EvidenceStringsAreBounded(
        CanonicalEvidenceEnvelopeV3 evidence,
        int maximumBytes) =>
        Bounded(evidence.EvidenceEnvelopeId, maximumBytes) &&
        Bounded(evidence.EvidenceSchemaVersion, maximumBytes) &&
        Bounded(evidence.OracleId, maximumBytes) &&
        Bounded(evidence.OracleVersion, maximumBytes) &&
        evidence.AuthoritativeBundles.All(bundle =>
            Bounded(bundle.ReaderId, maximumBytes) &&
            Bounded(bundle.ReaderVersion, maximumBytes) &&
            Bounded(bundle.ReaderArtifactSha256, maximumBytes) &&
            Bounded(bundle.SchemaVersion, maximumBytes) &&
            Bounded(bundle.Binding.Operation, maximumBytes) &&
            Bounded(bundle.Binding.RequestId, maximumBytes) &&
            Bounded(bundle.Binding.ResourceType, maximumBytes) &&
            Bounded(bundle.Binding.ResourceId, maximumBytes) &&
            Bounded(bundle.Binding.AttemptId, maximumBytes) &&
            Bounded(bundle.Binding.LeaseId, maximumBytes) &&
            Bounded(bundle.Binding.ObservationId, maximumBytes) &&
            Bounded(bundle.Binding.SnapshotOrWatermark, maximumBytes) &&
            bundle.Facts.All(fact =>
                Bounded(fact.FieldId, maximumBytes) &&
                (fact.CanonicalValue is null || Bounded(fact.CanonicalValue, maximumBytes))));

    private static bool Bounded(string value, int maximumBytes) =>
        !string.IsNullOrWhiteSpace(value) &&
        Encoding.UTF8.GetByteCount(value) <= maximumBytes;

    private static bool IsSha256(string value) =>
        value.Length == 64 &&
        value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void Require(bool condition, TrustFailureCodeV2 code)
    {
        if (!condition)
        {
            throw Failure(code);
        }
    }

    private static TrustFailureExceptionV2 Failure(TrustFailureCodeV2 code) =>
        new(code, $"Evidence rejected: {code}.");
}
