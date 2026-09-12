using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Identity;

namespace SESS.NexaERP.Tests;

public sealed class OidcAccessTokenTests
{
    [Theory]
    [InlineData("Cognito", "valid", true)]
    [InlineData("Cognito", "id-token", false)]
    [InlineData("Cognito", "wrong-client", false)]
    [InlineData("Cognito", "wrong-issuer", false)]
    [InlineData("Cognito", "expired", false)]
    [InlineData("Cognito", "wrong-scope", false)]
    [InlineData("Cognito", "wrong-key", false)]
    [InlineData("Oidc", "valid", true)]
    [InlineData("Oidc", "id-token", false)]
    [InlineData("Oidc", "wrong-audience", false)]
    [InlineData("Oidc", "wrong-scope", false)]
    [InlineData("Oidc", "wrong-issuer", false)]
    public async Task Signed_access_tokens_enforce_provider_boundary(string provider, string scenario, bool expected)
    {
        var options = Configure(provider);
        using var rsa = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test-key" };
        options.TokenValidationParameters.IssuerSigningKey = key;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = "immutable-employee-subject",
            ["scope"] = scenario == "wrong-scope" ? "openid" : "openid nexaerp/access",
            ["client_id"] = scenario == "wrong-client" ? "different-client" : "employee-client",
            ["token_use"] = scenario == "id-token" ? "id" : "access"
        };
        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = scenario == "wrong-issuer" ? "https://attacker.example" : "https://identity.example/realm",
            Audience = provider == "Cognito" ? null : scenario == "wrong-audience" ? "other-api" : "nexaerp",
            Claims = claims,
            IssuedAt = DateTime.UtcNow.AddMinutes(-30),
            NotBefore = DateTime.UtcNow.AddMinutes(-30),
            Expires = scenario == "expired" ? DateTime.UtcNow.AddMinutes(-2) : DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                scenario == "wrong-key" ? new RsaSecurityKey(other) { KeyId = "test-key" } : key,
                SecurityAlgorithms.RsaSha256)
        };
        var result = await handler.ValidateTokenAsync(handler.CreateToken(descriptor), options.TokenValidationParameters);
        var accepted = result.IsValid;
        if (accepted)
        {
            var context = new TokenValidatedContext(new DefaultHttpContext(),
                new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), options)
            {
                Principal = new ClaimsPrincipal(result.ClaimsIdentity),
                SecurityToken = result.SecurityToken
            };
            await options.Events.TokenValidated(context);
            accepted = context.Result?.Failure is null;
        }
        Assert.Equal(expected, accepted);
    }

    [Theory]
    [InlineData("Authentication:Authority", "http://identity.example")]
    [InlineData("Authentication:Authority", "https://user:password@identity.example")]
    [InlineData("Authentication:RequiredScope", "")]
    [InlineData("Authentication:RequiredScope", "one two")]
    [InlineData("Authentication:AudienceMode", "LocalPassword")]
    [InlineData("Authentication:Audience", "")]
    [InlineData("Authentication:ClientId", "")]
    [InlineData("Authentication:JwksAddress", "http://identity.example/keys")]
    [InlineData("Authentication:OrganizationSelectionMode", "Unverified")]
    public void Invalid_production_configuration_refuses(string name, string value)
    {
        var settings = Settings("Oidc");
        settings[name] = value;
        Assert.Throws<InvalidOperationException>(() =>
            OidcAccessTokenConfiguration.Configure(new JwtBearerOptions(),
                new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
    }

    [Fact]
    public async Task Valid_unmapped_identity_gets_administrator_action_envelope_and_never_reaches_command()
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("iss", "https://identity.example"), new Claim("sub", "unknown"),
             new Claim("organization_id", "SESS_PVT_LTD"),
             new Claim("nexaerp_development_employee_code", "SESS-12")], "Bearer"));
        var reached = false;
        var resolver = new RefusingResolver();
        var identity = new EmployeeIdentityResolutionMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var envelope = new StandardErrorEnvelopeMiddleware(
            http => identity.InvokeAsync(http, resolver),
            Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));
        await envelope.InvokeAsync(context);
        Assert.False(reached);
        Assert.True(resolver.OidcCalled);
        Assert.Equal(403, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("EMPLOYEE_ACCESS_NOT_CONFIGURED", json.RootElement.GetProperty("code").GetString());
        Assert.Contains("administrator", json.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("TECHNICAL_DIRECTOR", false, 403)]
    [InlineData("MANAGING_DIRECTOR", false, 403)]
    [InlineData("ACCOUNTS_MANAGER", false, 403)]
    [InlineData("TECHNICAL_DIRECTOR", true, 200)]
    [InlineData("MANAGING_DIRECTOR", true, 200)]
    [InlineData("ACCOUNTS_MANAGER", true, 200)]
    [InlineData("STORES_MANAGER", false, 200)]
    [InlineData("STORES_MANAGER", true, 200)]
    public async Task Mfa_is_mandatory_for_the_three_current_ERP_roles_only(string role, bool assured, int expected)
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "employee")], "Bearer"));
        context.Items[OidcAccessTokenConfiguration.ValidatedIdentityKey] =
            new ValidatedOidcIdentity("https://identity.example", "employee", "SESS_PVT_LTD", assured);
        var reached = false;
        var identity = new EmployeeIdentityResolutionMiddleware(_ => { reached = true; return Task.CompletedTask; });
        var envelope = new StandardErrorEnvelopeMiddleware(
            http => identity.InvokeAsync(http, new RoleResolver(role)),
            Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));
        await envelope.InvokeAsync(context);
        Assert.Equal(expected, context.Response.StatusCode);
        Assert.Equal(expected == 200, reached);
        if (expected == 403)
        {
            context.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal("MFA_REQUIRED", json.RootElement.GetProperty("code").GetString());
        }
    }

    private sealed class RoleResolver(string role) : IEmployeeIdentityResolver
    {
        public Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organization,
            DateOnly date, CancellationToken ct) => Task.FromResult(
                new ResolvedEmployeeIdentity(true, Guid.NewGuid(), Guid.NewGuid(), organization, "TEST", [role], "Resolved"));
#if DEBUG
        public Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string code, string? organization,
            DateOnly date, CancellationToken ct) => throw new InvalidOperationException("Not the development path.");
#endif
    }

    private static JwtBearerOptions Configure(string provider)
    {
        var options = new JwtBearerOptions();
        OidcAccessTokenConfiguration.Configure(options,
            new ConfigurationBuilder().AddInMemoryCollection(Settings(provider)).Build());
        return options;
    }

    private static Dictionary<string, string?> Settings(string provider) => new()
    {
        ["Authentication:AudienceMode"] = provider == "Cognito" ? "ClientClaim" : "Audience",
        ["Authentication:MetadataAddress"] = "https://identity.example/realm/.well-known/openid-configuration",
        ["Authentication:JwksAddress"] = "https://identity.example/realm/keys",
        ["Authentication:ClientIdClaim"] = "client_id",
        ["Authentication:TokenUseClaim"] = "token_use",
        ["Authentication:AccessTokenUseValue"] = "access",
        ["Authentication:SubjectClaim"] = "sub",
        ["Authentication:OrganizationClaim"] = "organization_id",
        ["Authentication:ScopeClaim"] = "scope",
        ["Authentication:Authority"] = "https://identity.example/realm",
        ["Authentication:ClientId"] = "employee-client",
        ["Authentication:Audience"] = provider == "Cognito" ? null : "nexaerp",
        ["Authentication:RequiredScope"] = "nexaerp/access"
    };

    private sealed class RefusingResolver : IEmployeeIdentityResolver
    {
        public bool OidcCalled { get; private set; }
        public Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organization,
            DateOnly date, CancellationToken ct)
        {
            OidcCalled = true;
            return Task.FromResult(ResolvedEmployeeIdentity.Failed("No active employee identity mapping exists."));
        }
#if DEBUG
        public Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode,
            string? organization, DateOnly date, CancellationToken ct) =>
            throw new InvalidOperationException("An OIDC claim must never activate development identity resolution.");
#endif
    }
}
