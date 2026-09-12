using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SESS.NexaERP.Api.Security;

public sealed record ValidatedOidcIdentity(string Issuer, string Subject, string? Organization, bool MfaAssured);

/// <summary>Deployment endpoints, claims and trust profiles are configuration, not provider-specific code.</summary>
public static class OidcAccessTokenConfiguration
{
    public const string ValidatedIdentityKey = "SESS.Authentication.ValidatedOidcIdentity";
    private const string RejectScheme = "UntrustedOidcIssuer";
    private static readonly HttpClient MetadataClient = new();

    public static void Register(IServiceCollection services, IConfiguration configuration)
    {
        var providers = configuration.GetSection("Authentication:Providers").GetChildren().ToArray();
        if (providers.Length == 0)
        {
            // Retains the single-provider configuration contract.
            var settings = configuration.GetSection("Authentication");
            ConfigureProvider(new JwtBearerOptions(), settings); // fail before accepting requests
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => ConfigureProvider(options, settings));
            return;
        }
        var schemes = new Dictionary<string, string>(StringComparer.Ordinal);
        var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        foreach (var provider in providers)
        {
            var probe = new JwtBearerOptions();
            ConfigureProvider(probe, provider);
            var scheme = "Oidc:" + provider.Key;
            if (!schemes.TryAdd(probe.Authority!, scheme))
                throw new InvalidOperationException("Each configured OIDC issuer must be unique.");
            authentication.AddJwtBearer(scheme, options => ConfigureProvider(options, provider));
        }
        authentication.AddJwtBearer(RejectScheme, options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Fail("The bearer token issuer is not configured.");
                    return Task.CompletedTask;
                }
            };
        });
        authentication.AddPolicyScheme(JwtBearerDefaults.AuthenticationScheme, null, options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return RejectScheme;
                var token = authorization[7..].Trim();
                var handler = new JsonWebTokenHandler();
                if (!handler.CanReadToken(token)) return RejectScheme;
                try
                {
                    // Untrusted issuer selects a validator only. That validator verifies every token.
                    var issuer = handler.ReadJsonWebToken(token).Issuer;
                    return schemes.GetValueOrDefault(issuer, RejectScheme);
                }
                catch (ArgumentException) { return RejectScheme; }
            };
        });
    }

    public static void Configure(JwtBearerOptions options, IConfiguration configuration) =>
        ConfigureProvider(options, configuration.GetSection("Authentication"));

    private static void ConfigureProvider(JwtBearerOptions options, IConfiguration settings)
    {
        var authority = RequireHttps(settings["Authority"], "Authority");
        var metadata = RequireHttps(settings["MetadataAddress"], "MetadataAddress");
        var jwks = RequireHttps(settings["JwksAddress"], "JwksAddress");
        var mode = settings["AudienceMode"] ?? "Audience";
        if (mode is not ("Audience" or "ClientClaim"))
            throw new InvalidOperationException("Authentication:AudienceMode must be Audience or ClientClaim.");
        var audience = settings["Audience"]?.Trim();
        var clientId = settings["ClientId"]?.Trim();
        var clientClaim = settings["ClientIdClaim"]?.Trim();
        var useClaim = Required(settings, "TokenUseClaim");
        var useValue = Required(settings, "AccessTokenUseValue");
        var subjectClaim = Required(settings, "SubjectClaim");
        var organizationMode = settings["OrganizationSelectionMode"] ?? "Claim";
        if (organizationMode is not ("Claim" or "MappedHeader"))
            throw new InvalidOperationException("OrganizationSelectionMode must be Claim or MappedHeader.");
        var organizationClaim = organizationMode == "Claim" ? Required(settings, "OrganizationClaim") : null;
        var organizationHeader = organizationMode == "MappedHeader" ? Required(settings, "OrganizationHeader") : null;
        if (organizationHeader is not null && organizationHeader.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new InvalidOperationException("OrganizationHeader must be a valid HTTP field name.");
        var scopeClaim = Required(settings, "ScopeClaim");
        var scope = Required(settings, "RequiredScope");
        var mfaGuaranteed = bool.TryParse(settings["MfaGuaranteedByProvider"], out var guaranteed) && guaranteed;
        var mfaClaim = settings["MfaClaim"];
        var mfaValue = settings["MfaValue"];
        if (!string.IsNullOrWhiteSpace(mfaClaim) && string.IsNullOrWhiteSpace(mfaValue))
            throw new InvalidOperationException("An MFA claim requires its accepted value.");
        if (scope.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("Authentication:RequiredScope must contain one API scope.");
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientClaim))
            throw new InvalidOperationException("ClientClaim validation requires ClientId and ClientIdClaim.");
        if (mode == "Audience" && string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Authentication:Audience is required.");
        if (!string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(clientClaim))
            throw new InvalidOperationException("Authentication:ClientIdClaim is required when ClientId is set.");

        options.Authority = authority;
        options.MetadataAddress = metadata;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadata, new ConfiguredOidcDocumentRetriever(authority, jwks),
            new HttpDocumentRetriever(options.Backchannel ?? MetadataClient) { RequireHttps = true });
        // Discovery must advertise the exact issuer and JWKS URL supplied in configuration.
        options.RequireHttpsMetadata = true;
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384,
                SecurityAlgorithms.RsaSha512, SecurityAlgorithms.EcdsaSha256,
                SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512]
        };
        if (mode == "ClientClaim")
        {
            options.TokenValidationParameters.AudienceValidator = (_, token, _) =>
                string.Equals(ReadClaim(token, clientClaim!), clientId, StringComparison.Ordinal) &&
                string.Equals(ReadClaim(token, useClaim), useValue, StringComparison.Ordinal) &&
                (string.IsNullOrWhiteSpace(audience) ||
                    string.Equals(ReadClaim(token, "aud"), audience, StringComparison.Ordinal));
        }
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal!;
                var subject = principal.FindFirstValue(subjectClaim);
                var scopes = (principal.FindFirstValue(scopeClaim) ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (string.IsNullOrWhiteSpace(subject) ||
                    !string.Equals(principal.FindFirstValue(useClaim), useValue, StringComparison.Ordinal) ||
                    (!string.IsNullOrWhiteSpace(clientId) &&
                        !string.Equals(principal.FindFirstValue(clientClaim!), clientId, StringComparison.Ordinal)) ||
                    !scopes.Contains(scope, StringComparer.Ordinal))
                {
                    context.Fail("An employee access token with the configured client, token use and API scope is required.");
                    return Task.CompletedTask;
                }
                var mfaAssured = mfaGuaranteed || (!string.IsNullOrWhiteSpace(mfaClaim) &&
                    principal.FindAll(mfaClaim).Any(claim => claim.Value == mfaValue));
                // The requested company is not authority: resolution must verify the signed
                // issuer/subject's active company mapping before any ERP endpoint executes.
                var requestedCompany = organizationClaim is not null ? principal.FindFirstValue(organizationClaim)
                    : context.Request.Headers[organizationHeader!].Count == 1
                        ? context.Request.Headers[organizationHeader!][0] : null;
                context.HttpContext.Items[ValidatedIdentityKey] = new ValidatedOidcIdentity(
                    context.SecurityToken.Issuer, subject, requestedCompany, mfaAssured);
                return Task.CompletedTask;
            }
        };
    }

    private static string Required(IConfiguration settings, string key) =>
        !string.IsNullOrWhiteSpace(settings[key]) ? settings[key]!.Trim()
            : throw new InvalidOperationException($"Authentication:{key} is required.");

    private static string RequireHttps(string? value, string name)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException($"Authentication:{name} must be an absolute HTTPS URL.");
        return value!.Trim();
    }

    private static string? ReadClaim(SecurityToken token, string name) => token switch
    {
        JsonWebToken jwt => jwt.TryGetClaim(name, out var claim) ? claim.Value : null,
        JwtSecurityToken jwt => jwt.Claims.FirstOrDefault(claim => claim.Type == name)?.Value,
        _ => null
    };
}
