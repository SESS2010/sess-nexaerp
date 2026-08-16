using SESS.NexaERP.AcceptanceVerifier.Configuration;
using SESS.NexaERP.AcceptanceVerifier.Verification;
using SESS.NexaERP.ControlPlane.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<AcceptanceVerifierOptions>()
    .Bind(builder.Configuration.GetSection(AcceptanceVerifierOptions.SectionName))
    .Validate(AcceptanceVerifierOptions.IsValid, "Acceptance-verifier trust configuration is missing or invalid.");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITrustReadinessProbe, ExternalPrerequisiteVerifierReadinessProbeV2>();
var app = builder.Build();

app.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", static async (
    ITrustReadinessProbe readinessProbe,
    CancellationToken cancellationToken) =>
{
    var readiness = await readinessProbe.CheckAsync(cancellationToken);
    return readiness.State == ReadinessStateV2.READY
        ? Results.Ok(readiness)
        : Results.Json(readiness, statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/version", static () => Results.Ok(new
{
    evidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
    protectedEvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
    protectedContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
    v1ProtectedOperations = Rev869BCompatibilityManifestV2.ProtectedOperationV1State,
    acceptanceVerifierProductionOwnership = "SESS_OWNED",
    deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
}));

app.Run();

public partial class Program;
