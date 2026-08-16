using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.AcceptanceVerifier.Configuration;

public sealed class AcceptanceVerifierOptions
{
    public const string SectionName = "Rev869BAcceptanceVerifier";

    public string ServiceIdentity { get; init; } = string.Empty;
    public string IssuerId { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string KeyId { get; init; } = string.Empty;
    public string ContractVersion { get; init; } = string.Empty;
    public string EvidenceVersion { get; init; } = string.Empty;
    public string CanonicalizationVersion { get; init; } = string.Empty;
    public string OracleId { get; init; } = string.Empty;
    public string OracleVersion { get; init; } = string.Empty;
    public string OracleArtifactSha256 { get; init; } = string.Empty;
    public string[] RequiredReaderIds { get; init; } = [];
    public string[] AllowedClusterIds { get; init; } = [];
    public string[] AllowedInstanceIds { get; init; } = [];
    public string[] AllowedFactFields { get; init; } = [];
    public string[] SensitiveFieldNames { get; init; } = [];
    public int MaximumEnvelopeBytes { get; init; }
    public int MaximumObservations { get; init; }
    public int MaximumSelectors { get; init; }
    public int MaximumFactsPerObservation { get; init; }
    public int MaximumStringBytes { get; init; }
    public int MaximumCumulativeFactBytes { get; init; }
    public int MaximumClockSkewSeconds { get; init; }
    public int MaximumObservationWindowSeconds { get; init; }

    public static bool IsValid(AcceptanceVerifierOptions value) =>
        NonEmpty(value.ServiceIdentity, value.IssuerId, value.Audience, value.KeyId, value.OracleId, value.OracleVersion) &&
        value.ContractVersion == Rev869BCompatibilityManifestV2.ContractVersion &&
        value.EvidenceVersion == Rev869BCompatibilityManifestV2.EvidenceVersion &&
        value.CanonicalizationVersion == Rev869BCompatibilityManifestV2.CanonicalizationVersion &&
        IsLowerSha256(value.OracleArtifactSha256) &&
        UniqueNonEmpty(value.RequiredReaderIds) &&
        UniqueNonEmpty(value.AllowedClusterIds) &&
        UniqueNonEmpty(value.AllowedInstanceIds) &&
        UniqueNonEmpty(value.AllowedFactFields) &&
        UniqueNonEmpty(value.SensitiveFieldNames) &&
        !value.AllowedFactFields.Intersect(value.SensitiveFieldNames, StringComparer.OrdinalIgnoreCase).Any() &&
        value.MaximumEnvelopeBytes is >= 1_024 and <= 4_194_304 &&
        value.MaximumObservations is >= 3 and <= 512 &&
        value.MaximumSelectors is >= 1 and <= 128 &&
        value.MaximumFactsPerObservation is >= 1 and <= 256 &&
        value.MaximumStringBytes is >= 1 and <= 4_096 &&
        value.MaximumCumulativeFactBytes is >= 1 and <= 2_097_152 &&
        value.MaximumClockSkewSeconds is >= 0 and <= 300 &&
        value.MaximumObservationWindowSeconds is >= 1 and <= 86_400;

    private static bool NonEmpty(params string[] values) =>
        values.All(static value => !string.IsNullOrWhiteSpace(value));

    private static bool UniqueNonEmpty(string[] values) =>
        values.Length > 0 &&
        values.All(static value => !string.IsNullOrWhiteSpace(value)) &&
        values.Distinct(StringComparer.Ordinal).Count() == values.Length;

    private static bool IsLowerSha256(string value) =>
        value.Length == 64 &&
        value.All(character =>
            (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));
}
