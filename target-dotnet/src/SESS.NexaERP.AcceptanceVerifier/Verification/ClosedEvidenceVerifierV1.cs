using System.Security.Cryptography;
using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.AcceptanceVerifier.Verification;

public interface IEvidenceSignatureVerifier
{
    SigningKeyDescriptor? FindKey(string keyId);
    bool Verify(string keyId, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature);
}

public interface IClosedOracleV1
{
    string OracleId { get; }
    bool Evaluate(CanonicalEvidenceEnvelopeV1 evidence);
}

public interface IClosedOracleCatalogV1
{
    IClosedOracleV1? Find(string oracleId);
}

public interface IVerificationAuditSinkV1
{
    string Append(VerificationAuditEventV1 auditEvent);
}

public sealed class ClosedEvidenceVerifierV1(
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
