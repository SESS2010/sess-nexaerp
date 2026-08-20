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
    public string OwnershipContractVersion { get; init; } = string.Empty;
    public string ReadinessPolicyVersion { get; init; } = string.Empty;
    public string DurableProviderContractVersion { get; init; } = string.Empty;
    public string DurableProviderIdentity { get; init; } = string.Empty;
    public string DurableProviderArtifactSha256 { get; init; } = string.Empty;
    public string LifecycleControllerIdentity { get; init; } = string.Empty;
    public string LifecycleControllerContractVersion { get; init; } = string.Empty;
    public string LifecycleControllerArtifactSha256 { get; init; } = string.Empty;
    public string AuthorizationPolicyArtifactSha256 { get; init; } = string.Empty;
    public string ManagementAuthorizerIdentity { get; init; } = "phase-a-management-authorizer";
    public string LeaseIssuerIdentity { get; init; } = "phase-a-lease-issuer";
    public string ExecutorWorkloadClass { get; init; } = "phase-a-plan-executor";
    public string TargetExecutionProviderIdentity { get; init; } = "phase-a-target-execution-provider";
    public string ReconciliationAuthorityIdentity { get; init; } = "phase-a-reconciliation-authority";
    public string A4BoundaryContractVersion { get; init; } = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion;
    public string TargetExecutionProviderArtifactSha256 { get; init; } = new('a', 64);
    public string ReconciliationAuthorityArtifactSha256 { get; init; } = new('a', 64);
    public string CanonicalEnvelopeVersion { get; init; } = string.Empty;
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
            value.OwnershipContractVersion != Rev869BPhaseACompatibilityManifest.OwnershipContractVersion ||
            value.ReadinessPolicyVersion != Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion ||
            value.DurableProviderContractVersion != Rev869BPhaseACompatibilityManifest.DurableProviderContractVersion ||
            string.IsNullOrWhiteSpace(value.DurableProviderIdentity) ||
            !IsLowerSha256(value.DurableProviderArtifactSha256) ||
            string.IsNullOrWhiteSpace(value.LifecycleControllerIdentity) ||
            value.LifecycleControllerContractVersion != Rev869BPhaseACompatibilityManifest.OwnershipContractVersion ||
            !IsLowerSha256(value.LifecycleControllerArtifactSha256) ||
            !IsLowerSha256(value.AuthorizationPolicyArtifactSha256) ||
            value.A4BoundaryContractVersion != Rev869BPhaseACompatibilityManifest.OwnershipContractVersion ||
            !IsLowerSha256(value.TargetExecutionProviderArtifactSha256) ||
            !IsLowerSha256(value.ReconciliationAuthorityArtifactSha256) ||
            value.CanonicalEnvelopeVersion != Rev869BPhaseACompatibilityManifest.CanonicalEnvelopeVersion ||
            value.MaximumEvidenceObservations is < 3 or > PhaseAContractLimits.MaximumObservations ||
            value.MaximumFactsPerObservation is < 1 or > PhaseAContractLimits.MaximumFactsPerObservation ||
            value.MaximumReplayWindowSeconds is < 1 or > 86_400 ||
            value.MaximumLeaseSeconds is < 1 or > 86_400 ||
            value.MaximumClockSkewSeconds is < 0 or > 300 ||
            value.MaximumEnvelopeBytes is < 1_024 or > PhaseAContractLimits.MaximumCommandEnvelopeBytes ||
            value.MaximumSelectors is < 1 or > PhaseAContractLimits.MaximumSelectors ||
            value.MaximumStringBytes is < 1 or > PhaseAContractLimits.MaximumStringBytes ||
            value.MaximumCumulativeFacts is < 3 or > PhaseAContractLimits.MaximumCumulativeFactBytes ||
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

        var separatedIdentities = new[]
        {
            value.ManagementAuthorizerIdentity,
            value.LeaseIssuerIdentity,
            value.ExecutorWorkloadClass,
            value.TargetExecutionProviderIdentity,
            value.ReconciliationAuthorityIdentity
        };
        if (separatedIdentities.Any(string.IsNullOrWhiteSpace) ||
            separatedIdentities.Distinct(StringComparer.Ordinal).Count() != separatedIdentities.Length ||
            separatedIdentities.Contains(value.ServiceIdentity, StringComparer.Ordinal) ||
            separatedIdentities.Contains(value.LifecycleControllerIdentity, StringComparer.Ordinal))
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

    public TrustedComponentDescriptorV3 DurableProviderDescriptor() => new(
        DurableProviderIdentity,
        DurableProviderContractVersion,
        DurableProviderArtifactSha256,
        ReadinessPolicyVersion);

    public TrustedComponentDescriptorV3 LifecycleControllerDescriptor() => new(
        LifecycleControllerIdentity,
        LifecycleControllerContractVersion,
        LifecycleControllerArtifactSha256,
        ReadinessPolicyVersion);

    public TrustedComponentDescriptorV3 TargetExecutionProviderDescriptor() => new(
        TargetExecutionProviderIdentity,
        A4BoundaryContractVersion,
        TargetExecutionProviderArtifactSha256,
        ReadinessPolicyVersion);

    public TrustedComponentDescriptorV3 ReconciliationAuthorityDescriptor() => new(
        ReconciliationAuthorityIdentity,
        A4BoundaryContractVersion,
        ReconciliationAuthorityArtifactSha256,
        ReadinessPolicyVersion);

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

    private static bool IsLowerSha256(string value) =>
        value.Length == 64 &&
        value.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool IsPlaceholderHttpsEndpoint(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var endpoint) &&
        endpoint.Scheme == Uri.UriSchemeHttps &&
        endpoint.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase);
}
