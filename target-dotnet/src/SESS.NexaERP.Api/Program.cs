using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
#if DEBUG
using Microsoft.IdentityModel.Tokens;
#endif
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureHttpJsonOptions(options => ApiJsonContract.Configure(options.SerializerOptions));
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// Development-only authentication follows the same gate pattern as
// DatabaseSecurity:AllowDevelopmentSuperuser: the setting must be absent in a
// Release build, and it activates only in Debug + Development + explicit opt-in.
var developmentAuthenticationSetting = builder.Configuration["NexaErp:AllowDevelopmentAuthentication"];
#if !DEBUG
if (developmentAuthenticationSetting is not null)
{
    throw new InvalidOperationException("NexaErp:AllowDevelopmentAuthentication must not be present in a Release build.");
}
#endif
var developmentAuthenticationEnabled = false;
#if DEBUG
developmentAuthenticationEnabled = builder.Environment.IsDevelopment()
    && string.Equals(developmentAuthenticationSetting, "true", StringComparison.OrdinalIgnoreCase);
if (developmentAuthenticationEnabled)
{
    var developmentTokens = new SESS.NexaERP.Api.Security.DevelopmentTokenService();
    builder.Services.AddSingleton(developmentTokens);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Issuer varies per employee identity mapping; the per-process
                // random signing key is the validation boundary.
                ValidateIssuer = false,
                ValidAudience = SESS.NexaERP.Api.Security.DevelopmentTokenService.Audience,
                IssuerSigningKey = developmentTokens.SigningKey,
            };
        });
}
#endif
if (!developmentAuthenticationEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.MapInboundClaims = false;
        });
}
builder.Services.AddAuthorization();

var app = builder.Build();

await using (var startupScope = app.Services.CreateAsyncScope())
{
    await startupScope.ServiceProvider
        .GetRequiredService<DatabaseRuntimePrincipalGuard>()
        .ValidateAsync();
}

app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<EmployeeIdentityResolutionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

app.MapGet("/api/v1/system/architecture", () => Results.Ok(new
{
    app = "SESS NexaERP",
    architecture = "ASP.NET Core modular monolith target",
    status = "Phase 1 permanent auth foundation",
    sourceSystem = "REV861 Node.js/single HTML current ERP snapshot",
    database = "PostgreSQL authoritative target",
    note = "Master APIs require authenticated JWT/OIDC claims. No temporary header identity is used."
}));

app.MapGet("/api/v1/system/modules", () => Results.Ok(new
{
    modules = Modules.Boundaries
}));

app.MapGet("/api/v1/purchase-stores/workflow-stages", () => Results.Ok(new
{
    stages = Enum.GetNames<PurchaseStoresStage>()
}));

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/v1/system/database-model", (NexaErpDbContext dbContext) => Results.Ok(new
    {
        schema = DatabaseSchemas.Advance,
        provider = dbContext.Database.ProviderName,
        entities = dbContext.Model.GetEntityTypes()
            .Select(entityType => new { name = entityType.ClrType.Name, table = entityType.GetTableName() })
            .OrderBy(entity => entity.name)
    }));
}

#if DEBUG
if (developmentAuthenticationEnabled)
{
    app.Logger.LogCritical(
        "SECURITY WARNING: Development-only authentication is active. Tokens from /api/v1/dev/token bypass the OIDC provider. This configuration must never be deployed.");
    app.MapDevelopmentAuthEndpoints();
}
#endif

app.MapSessionEndpoints();
app.MapIdentityEndpoints();
app.MapAuthorizationEndpoints();
app.MapMasterEndpoints();
app.MapReferenceMasterEndpoints();
app.MapMasterDataTransferEndpoints();
app.MapInventoryEndpoints();
app.MapItemVendorEndpoints();
app.MapPurchaseRequisitionEndpoints();
app.MapRev869BPurchaseEndpoints();
app.MapRev869AConfigurationEndpoints();
app.MapAuditEndpoints();
app.MapEmployeeEndpoints();

app.Run();

public partial class Program { }
