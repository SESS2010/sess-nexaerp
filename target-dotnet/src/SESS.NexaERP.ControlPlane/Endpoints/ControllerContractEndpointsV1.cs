using SESS.NexaERP.ControlPlane.Contracts;

namespace SESS.NexaERP.ControlPlane.Endpoints;

public static class ControllerContractEndpointsV1
{
    public static IEndpointRouteBuilder MapControllerContractEndpointsV1(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
        endpoints.MapGet("/health/ready", static () => Results.Ok(new
        {
            status = "contract-only",
            externalPrerequisitesBlocking = true,
            operationsImplemented = false
        }));
        endpoints.MapGet("/version", static () => Results.Ok(new
        {
            contractVersion = Rev869BCompatibilityManifestV1.ContractVersion,
            evidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
            canonicalizationVersion = Rev869BCompatibilityManifestV1.CanonicalizationVersion,
            controlPlaneProductionOwnership = "SESS_OWNED",
            deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
        }));
        return endpoints;
    }
}
