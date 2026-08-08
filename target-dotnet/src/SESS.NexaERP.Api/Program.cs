using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization(AuthorizationPolicies.AddSessPolicies);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
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

app.MapGet("/api/v1/system/database-model", (NexaErpDbContext dbContext) => Results.Ok(new
{
    schema = "nexa",
    provider = dbContext.Database.ProviderName,
    entities = dbContext.Model.GetEntityTypes()
        .Select(entityType => new
        {
            name = entityType.ClrType.Name,
            table = entityType.GetTableName()
        })
        .OrderBy(entity => entity.name)
}));

app.MapIdentityEndpoints();
app.MapAuthorizationEndpoints();
app.MapMasterEndpoints();
app.MapInventoryEndpoints();
app.MapPurchaseRequisitionEndpoints();
app.MapAuditEndpoints();
app.MapEmployeeEndpoints();

app.Run();

public partial class Program { }
