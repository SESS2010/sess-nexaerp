#if KEYCLOAK_WITNESS
using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Npgsql;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Infrastructure;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    [Trait("Witness", "KeycloakContainer")]
    public async Task LocalKeycloakContainerAuthenticatesThroughProductionOidcAndRealEmployeeMappings()
    {
        var origin = Environment.GetEnvironmentVariable("SESS_KEYCLOAK_WITNESS_URL")?.TrimEnd('/')
            ?? throw new InvalidOperationException("Start the local Keycloak container and set SESS_KEYCLOAK_WITNESS_URL.");
        var pin = Environment.GetEnvironmentVariable("SESS_KEYCLOAK_CERT_SHA256")
            ?? throw new InvalidOperationException("Set the isolated Keycloak TLS certificate SHA256.");
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != "https" || pin.Length != 64)
            throw new InvalidOperationException("The provider witness requires local HTTPS and its exact certificate pin.");
        using var backchannel = KeycloakHttpClient(pin);
        var staff = await KeycloakPkceLogin(origin, "sess-staff", "SESS-04", false, pin);
        var wrongPool = await KeycloakPkceLogin(origin, "sess-staff", "SESS-01", false, pin);
        var unmapped = await KeycloakPkceLogin(origin, "sess-staff", "UNMAPPED", false, pin);
        var approver = await KeycloakPkceLogin(origin, "sess-approvers", "SESS-01", true, pin);
        using var scriptDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("keycloak-schema.sql", scriptDb.GetService<IMigrator>()
            .GenerateScript("0", scriptDb.Database.GetMigrations().Last()));
        await KeycloakMapEmployee(server.ConnectionString, "SESS-04", origin + "/realms/sess-staff", staff.Subject);
        await KeycloakMapEmployee(server.ConnectionString, "SESS-01", origin + "/realms/sess-staff", wrongPool.Subject);
        using (var environment = new EnvironmentVariables(
            ("ConnectionStrings__NexaErpInstaller", server.ConnectionString),
            ("NexaErp__ExpectedDatabase", "advance_parser"),
            ("NEXAERP_MIGRATION_PASSWORD", "Isolated-Item16-Migration-Only!"),
            ("NEXAERP_BOOTSTRAP_PASSWORD", "Isolated-Item16-Bootstrap-Only!"),
            ("NEXAERP_RUNTIME_PASSWORD", "Isolated-Item16-Runtime-Only!")))
        {
            Assert.Equal(0, await InstallerCommand.RunAsync(["database-principals", "provision"]));
            Assert.Equal(0, await InstallerCommand.RunAsync(["database-principals", "status"]));
        }
        var runtime = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Username = "nexa_erp_runtime" }.ConnectionString;
        var settings = new Dictionary<string, string?> { ["ConnectionStrings:NexaErp"] = runtime, ["NexaErp:ExpectedDatabase"] = "advance_parser" };
        foreach (var realm in new[] { "sess-staff", "sess-approvers" })
        {
            var prefix = "Authentication:Providers:" + realm + ":";
            var issuer = origin + "/realms/" + realm;
            foreach (var pair in new Dictionary<string, string?>
            {
                ["Authority"] = issuer, ["MetadataAddress"] = issuer + "/.well-known/openid-configuration",
                ["JwksAddress"] = issuer + "/protocol/openid-connect/certs",
                ["AudienceMode"] = "Audience", ["Audience"] = "nexaerp",
                ["ClientId"] = "nexaerp-witness", ["ClientIdClaim"] = "azp",
                ["TokenUseClaim"] = "typ", ["AccessTokenUseValue"] = "Bearer",
                ["SubjectClaim"] = "sub", ["OrganizationSelectionMode"] = "MappedHeader",
                ["OrganizationHeader"] = "X-NexaERP-Company",
                ["ScopeClaim"] = "scope", ["RequiredScope"] = "nexaerp/access",
                ["MfaGuaranteedByProvider"] = (realm == "sess-approvers").ToString()
            }) settings[prefix + pair.Key] = pair.Value;
        }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "KeycloakWitness" });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureHttpJsonOptions(options => ApiJsonContract.Configure(options.SerializerOptions));
        builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
        builder.Services.AddInfrastructure(builder.Configuration);
        // A pinned, self-signed certificate is accepted only in this isolated test client.
        builder.Services.ConfigureAll<JwtBearerOptions>(options => options.Backchannel = backchannel);
        OidcAccessTokenConfiguration.Register(builder.Services, builder.Configuration);
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<DatabaseRuntimePrincipalGuard>().ValidateAsync();
        app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseMiddleware<EmployeeIdentityResolutionMiddleware>();
        app.UseAuthorization();
        app.MapSessionEndpoints();
        await app.StartAsync();
        using var api = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        async Task<HttpResponseMessage> Me(string token, string? company = "SESS_PVT_LTD")
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (company is not null) request.Headers.Add("X-NexaERP-Company", company);
            return await api.SendAsync(request);
        }

        using (var response = await Me(staff.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var session = await response.Content.ReadFromJsonAsync<SessionMe>();
            Assert.Equal("SESS-04", session!.EmployeeCode);
            Assert.Equal(origin + "/realms/sess-staff", session.IdentityIssuer);
        }
        using (var response = await Me(staff.AccessToken, null))
            await AssertKeycloakError(response, "EMPLOYEE_ACCESS_NOT_CONFIGURED");
        using (var response = await Me(staff.AccessToken, "UNMAPPED_COMPANY"))
            await AssertKeycloakError(response, "EMPLOYEE_ACCESS_NOT_CONFIGURED");
        using (var response = await Me(wrongPool.AccessToken))
            await AssertKeycloakError(response, "MFA_REQUIRED");
        using (var response = await Me(unmapped.AccessToken))
            await AssertKeycloakError(response, "EMPLOYEE_ACCESS_NOT_CONFIGURED");
        using (var response = await Me(staff.IdToken))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var parts = staff.AccessToken.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        using (var response = await Me(string.Join('.', parts)))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await KeycloakCloseMapping(server.ConnectionString, origin + "/realms/sess-staff", wrongPool.Subject);
        await KeycloakMapEmployee(server.ConnectionString, "SESS-01", origin + "/realms/sess-approvers", approver.Subject);
        using (var response = await Me(approver.AccessToken))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var session = await response.Content.ReadFromJsonAsync<SessionMe>();
            Assert.Equal("SESS-01", session!.EmployeeCode);
            Assert.Contains(session.RoleCodes, role => role is "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR");
        }
        await KeycloakCloseMapping(server.ConnectionString, origin + "/realms/sess-staff", staff.Subject);
        using (var response = await Me(staff.AccessToken))
            await AssertKeycloakError(response, "EMPLOYEE_ACCESS_NOT_CONFIGURED");
        await app.StopAsync();
        Console.WriteLine("KEYCLOAK_CONTAINER_WITNESS: PKCE staff login, password+TOTP approver login, real runtime principal/mappings, MFA_REQUIRED, unmapped/revoked identity, ID-token and tampered-signature refusal passed.");
    }

    private static async Task AssertKeycloakError(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.EnumerateObject()
            .Single(property => property.Name.Equals("code", StringComparison.OrdinalIgnoreCase)).Value.GetString());
    }

    private static HttpClient KeycloakHttpClient(string pin) => new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        CookieContainer = new CookieContainer(),
        ServerCertificateCustomValidationCallback = (request, certificate, _, _) =>
            request.RequestUri!.IsLoopback && certificate is not null &&
            certificate.GetCertHashString(HashAlgorithmName.SHA256).Equals(pin, StringComparison.OrdinalIgnoreCase)
    }) { Timeout = TimeSpan.FromMinutes(2) };

    private sealed record KeycloakTokens(string AccessToken, string IdToken, string Subject);

    private static async Task<KeycloakTokens> KeycloakPkceLogin(string origin, string realm, string user, bool otp, string pin)
    {
        using var client = KeycloakHttpClient(pin);
        using var discovery = JsonDocument.Parse(await client.GetStringAsync(origin + "/realms/" + realm + "/.well-known/openid-configuration"));
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var state = Guid.NewGuid().ToString("N");
        var callback = origin + "/witness/callback";
        var authorize = QueryHelpers.AddQueryString(discovery.RootElement.GetProperty("authorization_endpoint").GetString()!,
            new Dictionary<string, string?> {
                ["client_id"] = "nexaerp-witness", ["response_type"] = "code", ["scope"] = "openid nexaerp/access",
                ["redirect_uri"] = callback, ["state"] = state, ["code_challenge_method"] = "S256",
                ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            });
        using var page = await client.GetAsync(authorize);
        page.EnsureSuccessStatusCode();
        var loginAction = KeycloakFormAction(await page.Content.ReadAsStringAsync());
        using var passwordResponse = await client.PostAsync(loginAction, new FormUrlEncodedContent(
            new Dictionary<string,string> { ["username"] = user, ["password"] = "Item16-Witness-Only!26", ["credentialId"] = "" }));
        HttpResponseMessage response = passwordResponse;
        HttpResponseMessage? otpResponse = null;
        try
        {
            if (otp)
            {
                Assert.Equal(HttpStatusCode.OK, passwordResponse.StatusCode);
                var html = await passwordResponse.Content.ReadAsStringAsync();
                Assert.Contains("otp", html, StringComparison.OrdinalIgnoreCase);
                var selectedCredential = Regex.Match(html, @"name=""selectedCredentialId""\s+value=""([^""]+)""").Groups[1].Value;
                Assert.False(string.IsNullOrWhiteSpace(selectedCredential));
                var counter = new byte[8];
                BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
                var digest = HMACSHA1.HashData(Encoding.UTF8.GetBytes("JBSWY3DPEHPK3PXP"), counter);
                var offset = digest[^1] & 15;
                var value = (BinaryPrimitives.ReadInt32BigEndian(digest.AsSpan(offset, 4)) & int.MaxValue) % 1_000_000;
                otpResponse = await client.PostAsync(KeycloakFormAction(html), new FormUrlEncodedContent(
                    new Dictionary<string,string> { ["otp"] = value.ToString("D6", System.Globalization.CultureInfo.InvariantCulture), ["selectedCredentialId"] = WebUtility.HtmlDecode(selectedCredential) }));
                response = otpResponse;
            }
            Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
                "Provider login did not redirect after the expected authentication factors: " + await response.Content.ReadAsStringAsync());
            var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
            Assert.Equal(state, query["state"].ToString());
            using var tokens = await client.PostAsync(discovery.RootElement.GetProperty("token_endpoint").GetString(),
                new FormUrlEncodedContent(new Dictionary<string,string> {
                    ["grant_type"] = "authorization_code", ["client_id"] = "nexaerp-witness", ["redirect_uri"] = callback,
                    ["code"] = query["code"].ToString(), ["code_verifier"] = verifier
                }));
            tokens.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await tokens.Content.ReadAsStringAsync());
            Assert.InRange(document.RootElement.GetProperty("expires_in").GetInt32(), 890, 900);
            var access = document.RootElement.GetProperty("access_token").GetString()!;
            return new KeycloakTokens(access, document.RootElement.GetProperty("id_token").GetString()!,
                new JsonWebTokenHandler().ReadJsonWebToken(access).Subject);
        }
        finally { otpResponse?.Dispose(); }
    }

    private static string KeycloakFormAction(string html)
    {
        var match = Regex.Match(html, @"<form\b[^>]*\baction=""([^""]+)""", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Keycloak login form action was absent.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task KeycloakMapEmployee(string connectionString, string employeeCode, string issuer, string subject)
    {
        Assert.False(string.IsNullOrWhiteSpace(subject), "Keycloak must include its stable subject in access tokens.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),c."Id",c."Code",@issuer,@subject,e."Id",'HUMAN',
              current_date,true,clock_timestamp(),'KEYCLOAK_ISOLATED_FIXTURE',0
            FROM advance.companies c CROSS JOIN advance.employees e
            WHERE c."Code"='SESS_PVT_LTD' AND e."EmployeeCode"=@employee;
            """, connection);
        command.Parameters.AddWithValue("issuer", issuer);
        command.Parameters.AddWithValue("subject", subject);
        command.Parameters.AddWithValue("employee", employeeCode);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }

    private static async Task KeycloakCloseMapping(string connectionString, string issuer, string subject)
    {
        Assert.False(string.IsNullOrWhiteSpace(subject), "Keycloak must include its stable subject in access tokens.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            UPDATE advance.employee_identity_mappings SET "IsActive"=false,"EffectiveTo"=current_date,
              "Version"="Version"+1,"UpdatedAt"=clock_timestamp(),"UpdatedBy"='KEYCLOAK_ISOLATED_FIXTURE'
            WHERE "Issuer"=@issuer AND "Subject"=@subject AND "IsActive";
            """, connection);
        command.Parameters.AddWithValue("issuer", issuer);
        command.Parameters.AddWithValue("subject", subject);
        Assert.Equal(1, await command.ExecuteNonQueryAsync());
    }
}
#endif
