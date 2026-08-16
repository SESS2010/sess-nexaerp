using SESS.NexaERP.ControlPlane.Configuration;
using SESS.NexaERP.ControlPlane.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<ControlPlaneOptions>()
    .Bind(builder.Configuration.GetSection(ControlPlaneOptions.SectionName))
    .Validate(ControlPlaneOptions.IsValid, "Control-plane configuration is incomplete or violates the frozen trust boundary.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IControllerReadinessProbeV2, ExternalPrerequisiteReadinessProbeV2>();

var app = builder.Build();
app.MapControllerContractEndpointsV1();
app.Run();

public partial class Program;
