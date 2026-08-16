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

public static class Rev869BCompatibilityManifestV2
{
    public const string ContractVersion = "rev869b-controller-v2";
    public const string EvidenceVersion = "rev869b-evidence-v2";
    public const string CanonicalizationVersion = "rev869b-command-header-v2";
    public const string SignatureAlgorithm = "ECDSA-P256-SHA256";
    public const string ProtectedOperationV1State = "UNSUPPORTED";

    public static IReadOnlyList<string> SignedFields { get; } =
    [
        "contract_version", "canonicalization_version", "algorithm", "key_id", "issuer", "audience",
        "subject", "authorized_role", "authorized_scope", "organization_id", "database_cluster_id",
        "database_instance_id", "operation", "resource_id", "resource_version", "lease_id", "fencing_token",
        "request_id", "idempotency_key", "nonce", "issued_at", "not_before", "expires_at",
        "canonical_payload_sha256", "canonical_payload_length"
    ];

    public static bool IsProtectedCommandCompatible(string contractVersion, string canonicalizationVersion, string algorithm) =>
        contractVersion == ContractVersion &&
        canonicalizationVersion == CanonicalizationVersion &&
        algorithm == SignatureAlgorithm;
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
