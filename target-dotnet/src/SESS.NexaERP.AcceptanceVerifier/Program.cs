using SESS.NexaERP.AcceptanceVerifier.Verification;
using SESS.NexaERP.ControlPlane.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ClosedEvidenceVerifierV1>();
var app = builder.Build();

app.MapGet("/health/live", static () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", static () => Results.Ok(new
{
    status = "contract-only",
    externalPrerequisitesBlocking = true,
    verificationOperationsImplemented = false
}));
app.MapGet("/version", static () => Results.Ok(new
{
    evidenceVersion = Rev869BCompatibilityManifestV1.EvidenceVersion,
    acceptanceVerifierProductionOwnership = "SESS_OWNED",
    deploymentSeparation = "DESIGNED_NOT_DEPLOYED"
}));

app.Run();

public partial class Program;
