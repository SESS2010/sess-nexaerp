using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Endpoints;

public static class ControllerContractEndpointsV1
{
    public static IEndpointRouteBuilder MapControllerContractEndpointsV1(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
        endpoints.MapGet("/health/ready", static async (
            IReadinessAuthorityV3 readinessAuthority,
            CancellationToken cancellationToken) =>
        {
            var readiness = await readinessAuthority.CheckAsync(cancellationToken);
            return readiness.CanExecuteProtectedOperation
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
            phaseAContractVersion = Rev869BPhaseACompatibilityManifest.ContractVersion,
            phaseAEvidenceSchemaVersion = Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
            readinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
            ownershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
            v1ProtectedOperations = Rev869BCompatibilityManifestV2.ProtectedOperationV1State,
            controlPlaneProductionOwnership = "SESS_OWNED",
            deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
        }));
        return endpoints;
    }
}
