using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SESS.NexaERP.ControlPlane.Contracts;
using SESS.NexaERP.ControlPlane.Domain;

namespace SESS.NexaERP.ControlPlane.Security;

public interface IEnvelopeSigner
{
    string KeyId { get; }
    byte[] Sign(ReadOnlySpan<byte> canonicalPayload);
    byte[] Sign(string keyId, ReadOnlySpan<byte> canonicalPayload) =>
        keyId == KeyId
            ? Sign(canonicalPayload)
            : throw new TrustFailureExceptionV2(TrustFailureCodeV2.KEY_UNKNOWN, "Signing key is unavailable.");
}

public interface IEnvelopeSignatureVerifier
{
    bool Verify(string keyId, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature);
    bool Verify(SigningKeyDescriptor key, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature) =>
        Verify(key.KeyId, canonicalPayload, signature);
}

public interface ISigningKeyRegistry
{
    SigningKeyDescriptor? Find(string keyId);
}

public interface IReplayGuard
{
    bool TryAccept(string replayKey, DateTimeOffset expiresAtUtc);
}

public interface ITrustedIssuerRegistry
{
    TrustedIssuerDescriptorV2? Resolve(string issuerId, string keyId);
}

public interface IAuthorizationResolver
{
    AuthenticatedSubjectV2? Resolve(AuthenticatedSubjectV2 authenticatedSubject, ResourceBindingV2 resource);
}

public interface INonceReplayStore
{
    ValueTask<bool> TryReserveAsync(string issuer, string nonce, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}

public interface ITrustAuditSinkV2
{
    ValueTask<DurableAuditAppendReceiptV2?> AppendAcceptedAttemptAsync(
        CanonicalSignedHeaderV2 header,
        CancellationToken cancellationToken = default);
}

public sealed record VerifiedCommandV2(
    AuthenticatedSubjectV2 Subject,
    ResourceBindingV2 Resource,
    LeaseFenceV2 Lease,
    IdempotencyOutcomeV2 Idempotency);

public static partial class CanonicalSignedHeaderCodecV2
{
    private const string Prefix = "SESS-REV869B-COMMAND-V2\n";
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    private static readonly Regex IdentifierPattern = IdentifierRegex();

    public static byte[] Serialize(CanonicalSignedHeaderV2 value)
    {
        Validate(value);
        var fields = new (string Name, string Value)[]
        {
            ("contract_version", value.ContractVersion),
            ("canonicalization_version", value.CanonicalizationVersion),
            ("algorithm", value.Algorithm),
            ("key_id", value.KeyId),
            ("issuer", value.Issuer),
            ("audience", value.Audience),
            ("subject", value.Subject),
            ("authorized_role", value.AuthorizedRole),
            ("authorized_scope", value.AuthorizedScope),
            ("organization_id", value.OrganizationId),
            ("database_cluster_id", value.DatabaseClusterId),
            ("database_instance_id", value.DatabaseInstanceId),
            ("operation", value.Operation),
            ("resource_id", value.ResourceId),
            ("resource_version", value.ResourceVersion.ToString(CultureInfo.InvariantCulture)),
            ("lease_id", value.LeaseId),
            ("fencing_token", value.FencingToken.ToString(CultureInfo.InvariantCulture)),
            ("request_id", value.RequestId),
            ("idempotency_key", value.IdempotencyKey),
            ("nonce", value.Nonce),
            ("issued_at", value.IssuedAt.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture)),
            ("not_before", value.NotBefore.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture)),
            ("expires_at", value.ExpiresAt.UtcDateTime.ToString(UtcFormat, CultureInfo.InvariantCulture)),
            ("canonical_payload_sha256", value.CanonicalPayloadSha256),
            ("canonical_payload_length", value.CanonicalPayloadLength.ToString(CultureInfo.InvariantCulture))
        };

        var builder = new StringBuilder(Prefix);
        foreach (var (name, fieldValue) in fields)
        {
            builder.Append(name)
                .Append('=')
                .Append(Encoding.UTF8.GetByteCount(fieldValue).ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(fieldValue)
                .Append('\n');
        }

        return new UTF8Encoding(false, true).GetBytes(builder.ToString());
    }

    public static CanonicalSignedHeaderV2 Parse(ReadOnlySpan<byte> canonicalBytes)
    {
        if (canonicalBytes.Length is 0 or > 98_304)
        {
            throw Failure("The canonical header exceeds its byte limit.");
        }

        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(canonicalBytes);
        }
        catch (DecoderFallbackException)
        {
            throw Failure("The canonical header is not strict UTF-8.");
        }

        if (!text.StartsWith(Prefix, StringComparison.Ordinal) || text.Contains('\r') || !text.EndsWith('\n'))
        {
            throw Failure("The canonical header framing is invalid.");
        }

        var lines = text[Prefix.Length..].Split('\n');
        if (lines.Length != Rev869BCompatibilityManifestV2.SignedFields.Count + 1 || lines[^1].Length != 0)
        {
            throw Failure("The canonical header field count is invalid.");
        }

        var values = new string[Rev869BCompatibilityManifestV2.SignedFields.Count];
        for (var index = 0; index < values.Length; index++)
        {
            var expectedName = Rev869BCompatibilityManifestV2.SignedFields[index];
            var line = lines[index];
            if (!line.StartsWith(expectedName + "=", StringComparison.Ordinal))
            {
                throw Failure("The canonical header field order is invalid.");
            }

            var lengthStart = expectedName.Length + 1;
            var separator = line.IndexOf(':', lengthStart);
            if (separator < 0)
            {
                throw Failure("The canonical header length framing is invalid.");
            }

            var lengthText = line[lengthStart..separator];
            if (!IsCanonicalUnsignedInteger(lengthText, allowZero: true) ||
                !int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out var declaredLength))
            {
                throw Failure("The canonical header byte length is invalid.");
            }

            values[index] = line[(separator + 1)..];
            if (Encoding.UTF8.GetByteCount(values[index]) != declaredLength)
            {
                throw Failure("The canonical header byte length does not match its value.");
            }
        }

        var header = new CanonicalSignedHeaderV2(
            values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8],
            values[9], values[10], values[11], values[12], values[13],
            ParseLong(values[14], allowZero: false), values[15], ParseLong(values[16], allowZero: false),
            values[17], values[18], values[19], ParseUtc(values[20]), ParseUtc(values[21]), ParseUtc(values[22]),
            values[23], ParseInt(values[24], allowZero: true));
        var regenerated = Serialize(header);
        if (!canonicalBytes.SequenceEqual(regenerated))
        {
            throw Failure("The canonical header is not byte exact.");
        }
        return header;
    }

    public static CanonicalSignedHeaderV2 CreateHeader(
        CanonicalSignedHeaderV2 unsignedHeader,
        CanonicalCommandPayloadV2 payload)
    {
        var payloadBytes = CanonicalJsonV1.Serialize(payload);
        return unsignedHeader with
        {
            CanonicalPayloadSha256 = Convert.ToHexString(SHA256.HashData(payloadBytes)).ToLowerInvariant(),
            CanonicalPayloadLength = payloadBytes.Length
        };
    }

    private static void Validate(CanonicalSignedHeaderV2 value)
    {
        var identifiers = new[]
        {
            value.ContractVersion, value.CanonicalizationVersion, value.Algorithm, value.KeyId, value.Issuer,
            value.Audience, value.Subject, value.AuthorizedRole, value.AuthorizedScope, value.OrganizationId,
            value.DatabaseClusterId, value.DatabaseInstanceId, value.Operation, value.ResourceId, value.LeaseId,
            value.RequestId, value.IdempotencyKey
        };
        var digestIsLowerHex = value.CanonicalPayloadSha256.Length == 64 &&
            value.CanonicalPayloadSha256.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
        if (identifiers.Any(item => !IdentifierPattern.IsMatch(item) || Encoding.UTF8.GetByteCount(item) > 128) ||
            value.ResourceVersion <= 0 || value.FencingToken <= 0 || value.CanonicalPayloadLength < 0 ||
            !digestIsLowerHex || value.IssuedAt.Offset != TimeSpan.Zero || value.NotBefore.Offset != TimeSpan.Zero ||
            value.ExpiresAt.Offset != TimeSpan.Zero || value.IssuedAt > value.NotBefore ||
            value.NotBefore > value.ExpiresAt || !IsNonce(value.Nonce))
        {
            throw new TrustFailureExceptionV2(
                TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED,
                "The protected header is not canonical.");
        }
    }

    private static bool IsNonce(string value)
    {
        if (value.Length != 22 || value.Contains('='))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "==").Length == 16;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static long ParseLong(string value, bool allowZero)
    {
        if (!IsCanonicalUnsignedInteger(value, allowZero) ||
            !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw Failure("The canonical header integer is invalid.");
        }
        return parsed;
    }

    private static int ParseInt(string value, bool allowZero)
    {
        if (!IsCanonicalUnsignedInteger(value, allowZero) ||
            !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw Failure("The canonical header integer is invalid.");
        }
        return parsed;
    }

    private static bool IsCanonicalUnsignedInteger(string value, bool allowZero) =>
        value.Length > 0 &&
        value.All(static character => character is >= '0' and <= '9') &&
        (value.Length == 1 || value[0] != '0') &&
        (allowZero || value != "0");

    private static DateTimeOffset ParseUtc(string value)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                UtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed) ||
            parsed.Offset != TimeSpan.Zero)
        {
            throw Failure("The canonical header UTC timestamp is invalid.");
        }
        return parsed;
    }

    private static TrustFailureExceptionV2 Failure(string message) =>
        new(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, message);

    [GeneratedRegex("^[A-Za-z0-9._:/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}

public static class CanonicalCommandPayloadCodecV2
{
    private const int MaximumPayloadBytes = 65_536;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static CanonicalCommandPayloadV2 Parse(ReadOnlySpan<byte> canonicalBytes)
    {
        if (canonicalBytes.Length is 0 or > MaximumPayloadBytes)
        {
            throw new TrustFailureExceptionV2(
                TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED,
                "The canonical command payload exceeds its byte boundary.");
        }

        try
        {
            var payload = JsonSerializer.Deserialize<CanonicalCommandPayloadV2>(canonicalBytes, Options)
                ?? throw Failure();
            var regenerated = CanonicalJsonV1.Serialize(payload);
            if (!canonicalBytes.SequenceEqual(regenerated))
            {
                throw Failure();
            }
            if (payload.ApprovedParameters.Count > PhaseAContractLimits.MaximumSelectors ||
                payload.EvidenceRequirements.Count > PhaseAContractLimits.MaximumObservations ||
                payload.ApprovedParameters.Any(static pair =>
                    Encoding.UTF8.GetByteCount(pair.Key) > PhaseAContractLimits.MaximumIdentifierBytes ||
                    Encoding.UTF8.GetByteCount(pair.Value) > PhaseAContractLimits.MaximumStringBytes) ||
                payload.EvidenceRequirements.Any(static value =>
                    Encoding.UTF8.GetByteCount(value) > PhaseAContractLimits.MaximumIdentifierBytes))
            {
                throw new TrustFailureExceptionV2(
                    TrustFailureCodeV2.CONTRACT_LIMIT_EXCEEDED,
                    "The canonical command payload contains an unbounded value.");
            }
            return payload;
        }
        catch (TrustFailureExceptionV2)
        {
            throw;
        }
        catch (JsonException)
        {
            throw Failure();
        }
    }

    private static TrustFailureExceptionV2 Failure() =>
        new(TrustFailureCodeV2.CANONICAL_HEADER_MALFORMED, "The command payload is not canonical.");
}

public sealed class SignedEnvelopeService(IEnvelopeSigner signer, TimeProvider timeProvider)
{
    public SignedCommandEnvelopeV1 Sign(LifecycleCommandV1 command)
    {
        var canonicalPayload = CanonicalJsonV1.Serialize(command);
        var signature = signer.Sign(canonicalPayload);
        var metadata = new SignatureMetadataV1(
            signer.KeyId,
            Rev869BCompatibilityManifestV1.SignatureAlgorithm,
            Rev869BCompatibilityManifestV1.CanonicalizationVersion,
            Convert.ToHexString(SHA256.HashData(canonicalPayload)),
            Convert.ToBase64String(signature),
            timeProvider.GetUtcNow());

        return new SignedCommandEnvelopeV1(
            Rev869BCompatibilityManifestV1.ContractVersion,
            command,
            metadata);
    }
}

public sealed class SignedEnvelopeVerificationService(
    IEnvelopeSignatureVerifier verifier,
    ISigningKeyRegistry keyRegistry,
    IReplayGuard replayGuard,
    TimeProvider timeProvider)
{
    public void Verify(SignedCommandEnvelopeV1 envelope, TimeSpan maximumAge)
    {
        Require(envelope.ContractVersion == Rev869BCompatibilityManifestV1.ContractVersion,
            TrustRejectionCode.UnsupportedContractVersion, "Unsupported command contract version.");
        Require(envelope.Signature.CanonicalizationVersion == Rev869BCompatibilityManifestV1.CanonicalizationVersion,
            TrustRejectionCode.UnsupportedCanonicalizationVersion, "Unsupported canonicalization version.");
        Require(envelope.Signature.Algorithm == Rev869BCompatibilityManifestV1.SignatureAlgorithm,
            TrustRejectionCode.UnsupportedSignatureAlgorithm, "Unsupported signature algorithm.");
        ControllerAuthorizationPolicyV1.RequireAuthorized(envelope.Command);

        var now = timeProvider.GetUtcNow();
        var key = keyRegistry.Find(envelope.Signature.KeyId);
        Require(key is not null, TrustRejectionCode.UnknownKey, "Signing key is unknown.");
        Require(key!.State != SigningKeyState.Revoked, TrustRejectionCode.RevokedKey, "Signing key is revoked.");
        Require(key.State == SigningKeyState.Active && key.NotBeforeUtc <= now && (key.NotAfterUtc is null || key.NotAfterUtc >= now),
            TrustRejectionCode.ExpiredKey, "Signing key is not active for this instant.");
        Require(envelope.Signature.SignedAtUtc <= now && now - envelope.Signature.SignedAtUtc <= maximumAge,
            TrustRejectionCode.StaleEnvelope, "Envelope is stale or future-dated.");

        var canonicalPayload = CanonicalJsonV1.Serialize(envelope.Command);
        Require(CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(envelope.Signature.PayloadSha256),
                SHA256.HashData(canonicalPayload)),
            TrustRejectionCode.PayloadHashMismatch, "Payload hash does not match.");

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(envelope.Signature.SignatureBase64);
        }
        catch (FormatException exception)
        {
            throw new TrustRejectionException(TrustRejectionCode.InvalidSignature, exception.Message);
        }

        Require(verifier.Verify(key.KeyId, canonicalPayload, signature),
            TrustRejectionCode.InvalidSignature, "Signature verification failed.");
        Require(envelope.Command.Replay.ExpiresAtUtc >= now &&
                replayGuard.TryAccept(envelope.Command.Replay.Value, envelope.Command.Replay.ExpiresAtUtc),
            TrustRejectionCode.ReplayDetected, "Replay key was already consumed or expired.");
    }

    private static void Require(bool condition, TrustRejectionCode code, string message)
    {
        if (!condition)
        {
            throw new TrustRejectionException(code, message);
        }
    }
}

public sealed class SignedCommandServiceV2(
    IEnvelopeSignatureVerifier signatureVerifier,
    ITrustedIssuerRegistry issuerRegistry,
    IAuthorizationResolver authorizationResolver,
    ILifecycleControllerAuthority lifecycleController,
    TimeProvider timeProvider,
    TimeSpan maximumLifetime,
    TimeSpan allowedClockSkew)
{
    private async ValueTask<VerifiedCommandV2> VerifyParsedAsync(
        SignedCommandEnvelopeV2 envelope,
        AuthenticatedSubjectV2 transportSubject,
        ResourceBindingV2 expectedResource,
        CancellationToken cancellationToken = default)
    {
        var header = envelope.Header;
        Require(header.ContractVersion == Rev869BCompatibilityManifestV2.ContractVersion, TrustFailureCodeV2.CONTRACT_UNSUPPORTED);
        Require(header.CanonicalizationVersion == Rev869BCompatibilityManifestV2.CanonicalizationVersion,
            TrustFailureCodeV2.CANONICALIZATION_UNSUPPORTED);
        Require(header.Algorithm == Rev869BCompatibilityManifestV2.SignatureAlgorithm, TrustFailureCodeV2.ALGORITHM_UNSUPPORTED);

        var issuer = issuerRegistry.Resolve(header.Issuer, header.KeyId);
        Require(issuer is not null, TrustFailureCodeV2.ISSUER_UNKNOWN);
        Require(issuer!.Keys.TryGetValue(header.KeyId, out var key), TrustFailureCodeV2.KEY_UNKNOWN);
        Require(key!.Algorithm == header.Algorithm && issuer.Algorithms.Contains(header.Algorithm),
            TrustFailureCodeV2.ISSUER_KEY_MISMATCH);
        var now = timeProvider.GetUtcNow();
        Require(issuer.ContractVersions.Contains(header.ContractVersion) &&
                issuer.ActiveFrom <= now &&
                issuer.RevokedAt is null,
            TrustFailureCodeV2.ISSUER_KEY_MISMATCH);
        Require(key.State != SigningKeyState.Revoked, TrustFailureCodeV2.KEY_REVOKED);
        Require(key.State == SigningKeyState.Active && key.NotBeforeUtc <= now &&
                (key.NotAfterUtc is null || key.NotAfterUtc >= now),
            TrustFailureCodeV2.KEY_REVOKED);
        Require(signatureVerifier.Verify(key, CanonicalSignedHeaderCodecV2.Serialize(header), envelope.Signature),
            TrustFailureCodeV2.SIGNATURE_INVALID);

        var payloadBytes = CanonicalJsonV1.Serialize(envelope.Payload);
        Require(payloadBytes.Length == header.CanonicalPayloadLength, TrustFailureCodeV2.PAYLOAD_HASH_MISMATCH);
        byte[] declaredDigest;
        try
        {
            declaredDigest = Convert.FromHexString(header.CanonicalPayloadSha256);
        }
        catch (FormatException)
        {
            throw new TrustFailureExceptionV2(TrustFailureCodeV2.PAYLOAD_HASH_MISMATCH, "Payload digest is malformed.");
        }
        Require(CryptographicOperations.FixedTimeEquals(declaredDigest, SHA256.HashData(payloadBytes)),
            TrustFailureCodeV2.PAYLOAD_HASH_MISMATCH);

        Require(issuer.AllowedAudiences.Contains(header.Audience) && transportSubject.Audience == header.Audience,
            TrustFailureCodeV2.AUDIENCE_MISMATCH);
        Require(transportSubject.Issuer == header.Issuer && transportSubject.SubjectId == header.Subject,
            TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        Require(issuer.SubjectPatterns.Contains(header.Subject), TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        var resolved = authorizationResolver.Resolve(transportSubject, expectedResource);
        Require(resolved is not null, TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        Require(resolved!.TrustedRoles.Count == 1 &&
                resolved.TrustedRoles.Contains(header.AuthorizedRole),
            TrustFailureCodeV2.REQUEST_ROLE_FORBIDDEN);
        Require(issuer.Roles.Contains(header.AuthorizedRole), TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);
        Require(resolved.TrustedScopes.Count == 1 &&
                resolved.TrustedScopes.Contains(header.AuthorizedScope) &&
                issuer.Scopes.Contains(header.AuthorizedScope),
            TrustFailureCodeV2.SCOPE_MISMATCH);
        Require(issuer.Operations.Contains(header.Operation), TrustFailureCodeV2.SUBJECT_UNAUTHORIZED);

        Require(header.OrganizationId == expectedResource.OrganizationId, TrustFailureCodeV2.ORGANIZATION_MISMATCH);
        Require(header.DatabaseClusterId == expectedResource.DatabaseClusterId, TrustFailureCodeV2.CLUSTER_MISMATCH);
        Require(header.DatabaseInstanceId == expectedResource.DatabaseInstanceId, TrustFailureCodeV2.INSTANCE_MISMATCH);
        Require(header.Operation == expectedResource.Operation &&
                envelope.Payload.Operation.ToString().Equals(header.Operation, StringComparison.Ordinal),
            TrustFailureCodeV2.OPERATION_MISMATCH);
        Require(header.ResourceId == expectedResource.ResourceId, TrustFailureCodeV2.INSTANCE_MISMATCH);
        Require(header.ResourceVersion == expectedResource.ExpectedResourceVersion, TrustFailureCodeV2.RESOURCE_VERSION_STALE);
        Require(header.AuthorizedScope == $"ORG:{header.OrganizationId}",
            TrustFailureCodeV2.SCOPE_MISMATCH);

        Require(header.IssuedAt <= now + allowedClockSkew && header.NotBefore <= now + allowedClockSkew,
            TrustFailureCodeV2.NOT_YET_VALID);
        Require(header.ExpiresAt >= now - allowedClockSkew, TrustFailureCodeV2.ENVELOPE_EXPIRED);
        Require(header.ExpiresAt - header.IssuedAt <= maximumLifetime, TrustFailureCodeV2.ENVELOPE_EXPIRED);
        var requestDigest = header.CanonicalPayloadSha256;
        var scope = new CompanyDatabaseScopeV3(
            header.OrganizationId,
            header.DatabaseClusterId,
            header.DatabaseInstanceId,
            MasterScopeKindV3.COMPANY_LEDGER);
        var authorization = new ResolvedAuthorizationV3(
            $"grant:{header.RequestId}",
            header.Issuer,
            resolved.SubjectId,
            resolved.WorkloadIdentity,
            header.Audience,
            header.Operation,
            header.AuthorizedRole,
            header.AuthorizedScope,
            Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
            $"policy:{header.Operation}:{header.AuthorizedRole}",
            requestDigest,
            header.NotBefore,
            header.ExpiresAt);
        var authorizationBinding = new AuthorizationBindingV3(
            authorization,
            scope,
            expectedResource.ResourceType,
            header.ResourceId,
            header.ResourceVersion,
            header.Operation,
            requestDigest,
            HashEvidenceRequirements(envelope.Payload.EvidenceRequirements),
            header.LeaseId,
            header.FencingToken,
            AuthorizationGrantStateV3.ACTIVE);
        var idempotency = new IdempotencyIdentityV3(
            header.Issuer,
            header.OrganizationId,
            header.DatabaseInstanceId,
            header.Operation,
            header.RequestId,
            header.IdempotencyKey,
            requestDigest);
        var nonce = new NonceRegistrationV3(header.Issuer, header.Nonce, header.ExpiresAt, requestDigest);
        var lease = header.LeaseId == "lease-none"
            ? null
            : new LeaseFenceExpectationV3(
                header.LeaseId,
                1,
                header.FencingToken,
                header.ExpiresAt,
                resolved.SubjectId);
        var requirements = envelope.Payload.EvidenceRequirements
            .Select(static requirement => new EvidenceRequirementV3(
                requirement,
                "phase-a-reader",
                "phase-a-v1",
                EvidenceStageV3.DURABLE,
                Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
                PhaseAContractLimits.MaximumFactsPerObservation,
                PhaseAContractLimits.MaximumEvidenceEnvelopeBytes))
            .ToArray();
        var verified = new VerifiedLifecycleCommandV3(
            $"command:{header.RequestId}",
            envelope.Payload.Operation,
            envelope.Payload.ExpectedState,
            header.ResourceVersion,
            authorizationBinding,
            lease,
            requirements,
            idempotency,
            nonce,
            $"audit:{header.RequestId}",
            Convert.ToHexString(SHA256.HashData(CanonicalSignedHeaderCodecV2.Serialize(header))).ToLowerInvariant());
        var transition = await lifecycleController.TransitionAsync(verified, cancellationToken);
        var legacyLease = lease is null
            ? NoLease(header, resolved.SubjectId)
            : new(
                lease.LeaseId,
                header.ResourceId,
                lease.FencingToken,
                header.IssuedAt,
                header.IssuedAt,
                lease.ExpiresAt,
                lease.HolderSubject);
        var outcome = new IdempotencyOutcomeV2(
            transition.TransactionOutcome == ControlTransactionOutcomeV3.COMPLETED_REPLAY
                ? IdempotencyReservationStateV2.COMPLETED
                : transition.FailureCode == TrustFailureCodeV2.NONE
                    ? IdempotencyReservationStateV2.COMPLETED
                    : IdempotencyReservationStateV2.NONRETRYABLE_FAILURE,
            transition.AttemptNumber,
            false,
            transition.FailureCode == TrustFailureCodeV2.NONE ? null : transition.FailureCode,
            transition.ResponseSha256,
            transition.AuditReference,
            now);
        return new(resolved, expectedResource, legacyLease, outcome);
    }

    public ValueTask<VerifiedCommandV2> VerifyAsync(
        ReadOnlyMemory<byte> canonicalHeaderBytes,
        ReadOnlyMemory<byte> canonicalPayloadBytes,
        byte[] signature,
        AuthenticatedSubjectV2 transportSubject,
        ResourceBindingV2 expectedResource,
        CancellationToken cancellationToken = default) =>
        VerifyParsedAsync(
            new SignedCommandEnvelopeV2(
                CanonicalSignedHeaderCodecV2.Parse(canonicalHeaderBytes.Span),
                CanonicalCommandPayloadCodecV2.Parse(canonicalPayloadBytes.Span),
                signature),
            transportSubject,
            expectedResource,
            cancellationToken);

    private static string HashEvidenceRequirements(IReadOnlyList<string> requirements) =>
        Convert.ToHexString(SHA256.HashData(CanonicalJsonV1.Serialize(requirements))).ToLowerInvariant();

    private static LeaseFenceV2 NoLease(CanonicalSignedHeaderV2 header, string subject) =>
        new(header.LeaseId, header.ResourceId, header.FencingToken, header.IssuedAt, header.IssuedAt, header.ExpiresAt, subject);

    private static void Require(bool condition, TrustFailureCodeV2 code)
    {
        if (!condition)
        {
            throw new TrustFailureExceptionV2(code, $"Command rejected: {code}.");
        }
    }
}
