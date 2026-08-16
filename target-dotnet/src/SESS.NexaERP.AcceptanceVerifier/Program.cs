using SESS.NexaERP.AcceptanceVerifier.Configuration;
using SESS.NexaERP.AcceptanceVerifier.Verification;
using SESS.NexaERP.ControlPlane.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<AcceptanceVerifierOptions>()
    .Bind(builder.Configuration.GetSection(AcceptanceVerifierOptions.SectionName))
    .Validate(AcceptanceVerifierOptions.IsValid, "Acceptance-verifier trust configuration is missing or invalid.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IReadinessAuthorityV3>(services =>
    new PhaseAReadinessAuthority(
        Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
        [],
        services.GetRequiredService<TimeProvider>()));
var app = builder.Build();

app.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", static async (
    IReadinessAuthorityV3 readinessAuthority,
    CancellationToken cancellationToken) =>
{
    var readiness = await readinessAuthority.CheckAsync(cancellationToken);
    return readiness.CanExecuteProtectedOperation
        ? Results.Ok(readiness)
        : Results.Json(readiness, statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/version", static () => Results.Ok(new
{
    evidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
    protectedEvidenceVersion = Rev869BCompatibilityManifestV2.EvidenceVersion,
    protectedContractVersion = Rev869BCompatibilityManifestV2.ContractVersion,
    phaseAContractVersion = Rev869BPhaseACompatibilityManifest.ContractVersion,
    phaseAEvidenceSchemaVersion = Rev869BPhaseACompatibilityManifest.EvidenceSchemaVersion,
    readinessPolicyVersion = Rev869BPhaseACompatibilityManifest.ReadinessPolicyVersion,
    ownershipContractVersion = Rev869BPhaseACompatibilityManifest.OwnershipContractVersion,
    v1ProtectedOperations = Rev869BCompatibilityManifestV2.ProtectedOperationV1State,
    acceptanceVerifierProductionOwnership = "SESS_OWNED",
    deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
}));

app.Run();

public partial class Program;
