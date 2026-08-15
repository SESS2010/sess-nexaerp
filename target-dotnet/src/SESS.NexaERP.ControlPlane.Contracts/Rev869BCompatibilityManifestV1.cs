namespace SESS.NexaERP.ControlPlane.Contracts;

public static class Rev869BCompatibilityManifestV1
{
    public const string ContractVersion = "rev869b-controller-v1";
    public const string EvidenceVersion = "rev869b-evidence-v1";
    public const string CanonicalizationVersion = "rev869b-json-v1";
    public const string SignatureAlgorithm = "ECDSA-P256-SHA256";

    public static bool IsCompatible(string contractVersion, string evidenceVersion, string canonicalizationVersion) =>
        contractVersion == ContractVersion &&
        evidenceVersion == EvidenceVersion &&
        canonicalizationVersion == CanonicalizationVersion;
}

public enum SigningKeyState
{
    Active,
    Retired,
    Revoked
}

public sealed record SigningKeyDescriptor(
    string KeyId,
    string Algorithm,
    SigningKeyState State,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset? NotAfterUtc,
    DateTimeOffset? RevokedAtUtc);
