using System.Security.Cryptography;
using SESS.NexaERP.ControlPlane.Contracts;
using SESS.NexaERP.ControlPlane.Domain;

namespace SESS.NexaERP.ControlPlane.Security;

public interface IEnvelopeSigner
{
    string KeyId { get; }
    byte[] Sign(ReadOnlySpan<byte> canonicalPayload);
}

public interface IEnvelopeSignatureVerifier
{
    bool Verify(string keyId, ReadOnlySpan<byte> canonicalPayload, ReadOnlySpan<byte> signature);
}

public interface ISigningKeyRegistry
{
    SigningKeyDescriptor? Find(string keyId);
}

public interface IReplayGuard
{
    bool TryAccept(string replayKey, DateTimeOffset expiresAtUtc);
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
