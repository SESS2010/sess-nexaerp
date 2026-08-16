using System.Text.RegularExpressions;
using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Configuration;

public sealed class ControlPlaneOptions
{
    public const string SectionName = "Rev869BControlPlane";

    public string ServiceIdentity { get; init; } = string.Empty;
    public string IssuerId { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string ControlPlaneDatabaseIdentity { get; init; } = string.Empty;
    public string CommandSigningKeyId { get; init; } = string.Empty;
    public string EvidenceVerificationKeyId { get; init; } = string.Empty;
    public string ContractVersion { get; init; } = string.Empty;
    public string EvidenceVersion { get; init; } = string.Empty;
    public string CanonicalizationVersion { get; init; } = string.Empty;
    public string RetentionPolicyReference { get; init; } = string.Empty;
    public int MaximumEvidenceObservations { get; init; }
    public int MaximumFactsPerObservation { get; init; }
    public int MaximumReplayWindowSeconds { get; init; }
    public int MaximumLeaseSeconds { get; init; }
    public int MaximumClockSkewSeconds { get; init; }
    public int MaximumEnvelopeBytes { get; init; }
    public int MaximumSelectors { get; init; }
    public int MaximumStringBytes { get; init; }
    public int MaximumCumulativeFacts { get; init; }
    public string ControlPlaneEndpoint { get; init; } = string.Empty;
    public string AcceptanceVerifierEndpoint { get; init; } = string.Empty;
    public string[] AllowedTargetEnvironments { get; init; } = [];
    public string[] AllowedDatabaseIdentityPatterns { get; init; } = [];
    public string[] AllowedIssuerIds { get; init; } = [];
    public string[] AllowedAudiences { get; init; } = [];
    public string[] AllowedRoles { get; init; } = [];
    public string[] AllowedScopes { get; init; } = [];
    public string[] AllowedOperations { get; init; } = [];

    public static bool IsValid(ControlPlaneOptions value)
    {
        if (string.IsNullOrWhiteSpace(value.ServiceIdentity) ||
            string.IsNullOrWhiteSpace(value.IssuerId) ||
            string.IsNullOrWhiteSpace(value.Audience) ||
            string.IsNullOrWhiteSpace(value.ControlPlaneDatabaseIdentity) ||
            string.IsNullOrWhiteSpace(value.CommandSigningKeyId) ||
            string.IsNullOrWhiteSpace(value.EvidenceVerificationKeyId) ||
            value.CommandSigningKeyId == value.EvidenceVerificationKeyId ||
            string.IsNullOrWhiteSpace(value.RetentionPolicyReference) ||
            value.MaximumEvidenceObservations is < 3 or > 10_000 ||
            value.MaximumFactsPerObservation is < 1 or > 1_000 ||
            value.MaximumReplayWindowSeconds is < 1 or > 86_400 ||
            value.MaximumLeaseSeconds is < 1 or > 86_400 ||
            value.MaximumClockSkewSeconds is < 0 or > 300 ||
            value.MaximumEnvelopeBytes is < 1_024 or > 16_777_216 ||
            value.MaximumSelectors is < 1 or > 1_000 ||
            value.MaximumStringBytes is < 1 or > 65_536 ||
            value.MaximumCumulativeFacts is < 3 or > 100_000 ||
            value.AllowedTargetEnvironments.Length == 0 ||
            value.AllowedDatabaseIdentityPatterns.Length == 0 ||
            value.AllowedIssuerIds.Length == 0 ||
            value.AllowedAudiences.Length == 0 ||
            value.AllowedRoles.Length == 0 ||
            value.AllowedScopes.Length == 0 ||
            value.AllowedOperations.Length == 0 ||
            !value.AllowedIssuerIds.Contains(value.IssuerId, StringComparer.Ordinal) ||
            !value.AllowedAudiences.Contains(value.Audience, StringComparer.Ordinal) ||
            !IsPlaceholderHttpsEndpoint(value.ControlPlaneEndpoint) ||
            !IsPlaceholderHttpsEndpoint(value.AcceptanceVerifierEndpoint) ||
            value.ControlPlaneEndpoint == value.AcceptanceVerifierEndpoint)
        {
            return false;
        }

        if (!Rev869BCompatibilityManifestV2.IsProtectedCommandCompatible(
                value.ContractVersion,
                value.CanonicalizationVersion,
                Rev869BCompatibilityManifestV2.SignatureAlgorithm) ||
            value.EvidenceVersion != Rev869BCompatibilityManifestV2.EvidenceVersion)
        {
            return false;
        }

        if (ContainsProhibitedProductionIdentity(value.ControlPlaneDatabaseIdentity))
        {
            return false;
        }

        try
        {
            return value.AllowedDatabaseIdentityPatterns.All(static pattern => IsSafePattern(pattern));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool IsTargetAllowed(TargetErpInstanceIdentity target)
    {
        if (ContainsProhibitedProductionIdentity(target.DatabaseIdentity) ||
            !AllowedTargetEnvironments.Contains(target.EnvironmentName, StringComparer.Ordinal))
        {
            return false;
        }

        return AllowedDatabaseIdentityPatterns.Any(pattern =>
            Regex.IsMatch(target.DatabaseIdentity, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));
    }

    private static bool ContainsProhibitedProductionIdentity(string value) =>
        Regex.IsMatch(value, "(^|[^a-z0-9])(prod(uction)?|main|rev861)([^a-z0-9]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static bool IsSafePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || ContainsProhibitedProductionIdentity(pattern))
        {
            return false;
        }

        _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return true;
    }

    private static bool IsPlaceholderHttpsEndpoint(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var endpoint) &&
        endpoint.Scheme == Uri.UriSchemeHttps &&
        endpoint.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);
}
