using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Endpoints;

public interface IControllerReadinessProbeV2
{
    ValueTask<ReadinessResultV2> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed class ExternalPrerequisiteReadinessProbeV2(TimeProvider timeProvider) : IControllerReadinessProbeV2
{
    private static readonly string[] MissingDependencies =
    [
        "ISSUER_REGISTRY_UNAVAILABLE",
        "KEY_REGISTRY_UNAVAILABLE",
        "IDEMPOTENCY_UNAVAILABLE",
        "LEASE_STORE_UNAVAILABLE",
        "LIFECYCLE_STORE_UNAVAILABLE",
        "AUDIT_WRITER_UNAVAILABLE"
    ];

    public ValueTask<ReadinessResultV2> CheckAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new ReadinessResultV2(
            ReadinessStateV2.NOT_READY,
            MissingDependencies,
            timeProvider.GetUtcNow()));
}

public static class ControllerContractEndpointsV1
{
    public static IEndpointRouteBuilder MapControllerContractEndpointsV1(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
        endpoints.MapGet("/health/ready", static async (
            IControllerReadinessProbeV2 readinessProbe,
            CancellationToken cancellationToken) =>
        {
            var readiness = await readinessProbe.CheckAsync(cancellationToken);
            return readiness.State == ReadinessStateV2.READY
                ? Results.Ok(readiness)
                : Results.Json(readiness, statusCode: StatusCodes.Status503ServiceUnavailable);
        });
        endpoints.MapGet("/version", static () => Results.Ok(new
        {
            contractVersion = Rev869BCompatibilityManifestV1.ContractVersion,
            evidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
            canonicalizationVersion = Rev869BCompatibilityManifestV1.CanonicalizationVersion,
            protectedContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
            protectedEvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
            protectedCanonicalizationVersion = Rev869BCompatibilityManifestV2.CanonicalizationVersion,
            v1ProtectedOperations = Rev869BCompatibilityManifestV2.ProtectedOperationV1State,
            controlPlaneProductionOwnership = "SESS_OWNED",
            deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
        }));
        return endpoints;
    }
}
