using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed class AuthorizationIntegrationTests
{
    [Fact]
    public async Task Protected_page_without_token_returns_401()
    {
        await using var host = await TestHost.StartAsync((_, _) => true);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        var response = await client.GetAsync("/employee-list");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_without_page_permission_returns_403_and_audits_denial()
    {
        var audit = new CapturingAuditWriter();
        await using var host = await TestHost.StartAsync((_, _) => false, audit);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, "SESS-020");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, "hr_executive");
        var response = await client.GetAsync("/employee-list");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var denial = Assert.Single(audit.Entries);
        Assert.Equal("Security", denial.Module);
        Assert.Equal("Denied", denial.Action);
        Assert.Equal("Failure", denial.Result);
        Assert.False(string.IsNullOrWhiteSpace(denial.CorrelationId));
    }

    [Fact]
    public async Task Authorized_role_succeeds()
    {
        await using var host = await TestHost.StartAsync((_, _) => true);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, "SESS-001");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, "technical_director");
        var response = await client.GetAsync("/employee-list");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/role-assign", "create")]
    [InlineData("/commercial", "view-commercial-values")]
    [InlineData("/export", "export")]
    [InlineData("/approve", "approve")]
    public async Task Unauthorized_controlled_actions_return_403(string path, string expectedPermission)
    {
        var audit = new CapturingAuditWriter();
        await using var host = await TestHost.StartAsync((_, permission) => permission != expectedPermission, audit);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, "SESS-014");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, "stores_assistant");
        var response = await client.PostAsync(path, JsonContent.Create(new { remarks = "test" }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(audit.Entries, entry => entry.Module == "Security" && entry.Action == "Denied" && entry.Result == "Failure");
    }

    [Theory]
    [InlineData("technical_director", "/approve")]
    [InlineData("managing_director", "/approve")]
    public async Task Td_and_md_authorized_actions_succeed_according_to_matrix(string role, string path)
    {
        await using var host = await TestHost.StartAsync((actualRole, permission) =>
            (actualRole is "technical_director" or "managing_director") && permission == PagePermissionActions.Approve);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, role == "technical_director" ? "SESS-001" : "SESS-002");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, role);
        var response = await client.PostAsync(path, JsonContent.Create(new { remarks = "test" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("customer", "CUST-A", "CUST-B")]
    [InlineData("vendor", "VEND-A", "VEND-B")]
    public async Task Portal_cross_organization_access_is_denied(string role, string claimOrg, string requestedOrg)
    {
        await using var host = await TestHost.StartAsync((_, _) => true);

        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, role + "-user");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, role);
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.OrganizationHeader, claimOrg);
        var response = await client.GetAsync($"/portal-record/{requestedOrg}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private TestHost(WebApplication app, Uri baseAddress)
        {
            _app = app;
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public static async Task<TestHost> StartAsync(Func<string, string, bool> permissionDecision, CapturingAuditWriter? audit = null)
        {
            var port = GetFreePort();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = Array.Empty<string>(),
                EnvironmentName = "Test"
            });
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.AddRouting();
            builder.Services.AddAuthentication(TestOnlyAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestOnlyAuthenticationHandler>(TestOnlyAuthenticationHandler.SchemeName, _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<IPagePermissionService>(new DelegatePagePermissionService(permissionDecision));
            builder.Services.AddSingleton<IAuditWriter>(audit ?? new CapturingAuditWriter());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, TestCurrentUser>();
            builder.Services.AddSingleton<IRecordScopeAuthorizer, AllowingRecordScopeAuthorizer>();

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/employee-list", () => Results.Ok(new { ok = true }))
                .RequireAuthorization()
                .RequirePagePermission("employees.master", PagePermissionActions.View);
            app.MapPost("/role-assign", () => Results.Ok(new { ok = true }))
                .RequireAuthorization()
                .RequirePagePermission("employees.role-mapping", PagePermissionActions.Create);
            app.MapPost("/commercial", () => Results.Ok(new { ok = true }))
                .RequireAuthorization()
                .RequirePagePermission("purchase.po", PagePermissionActions.ViewCommercialValues);
            app.MapPost("/export", () => Results.Ok(new { ok = true }))
                .RequireAuthorization()
                .RequirePagePermission("purchase.po", PagePermissionActions.Export);
            app.MapPost("/approve", () => Results.Ok(new { ok = true }))
                .RequireAuthorization()
                .RequirePagePermission("purchase.po", PagePermissionActions.Approve);
            app.MapGet("/portal-record/{organizationId}", (HttpContext context, string organizationId) =>
            {
                var role = context.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                var claimOrg = context.User.FindFirstValue("organization_id") ?? string.Empty;
                return (role is "customer" or "vendor") && !string.Equals(claimOrg, organizationId, StringComparison.OrdinalIgnoreCase)
                    ? Results.Forbid()
                    : Results.Ok(new { organizationId });
            }).RequireAuthorization();

            await app.StartAsync();
            return new TestHost(app, new Uri($"http://127.0.0.1:{port}"));
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }

    private sealed class TestCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();
        public string LoginId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        public string RoleCode => Principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        public string? OrganizationId => Principal.FindFirstValue("organization_id") ?? "SESS";
        public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
        public Guid? EmployeeId => IsAuthenticated ? Guid.Parse("90000000-0000-0000-0000-000000000001") : null;
    }

    private sealed class AllowingRecordScopeAuthorizer : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken cancellationToken) => Task.FromResult(new RecordScopeDecision(true, "Test operational scope."));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken cancellationToken) => Task.FromResult(new RecordScopeDecision(true, "Test record scope."));
    }
    private sealed class DelegatePagePermissionService(Func<string, string, bool> permissionDecision) : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(string roleCode, string pageKey, string permission, CancellationToken cancellationToken)
        {
            return Task.FromResult(permissionDecision(roleCode, permission));
        }
    }

    public sealed class CapturingAuditWriter : IAuditWriter
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken cancellationToken)
        {
            Entries.Add(new AuditEntry(module, action, entityName, entityId, action == "Denied" ? "Failure" : "Success", Guid.NewGuid().ToString("N")));
            return Task.CompletedTask;
        }
    }

    public sealed record AuditEntry(string Module, string Action, string EntityName, string EntityId, string Result, string CorrelationId);
}

public sealed class TestOnlyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Rev866TestOnly";
    public const string UserHeader = "X-Test-User";
    public const string RoleHeader = "X-Test-Role";
    public const string OrganizationHeader = "X-Test-Organization";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userValues) ||
            !Request.Headers.TryGetValue(RoleHeader, out var roleValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userValues.ToString()),
            new(ClaimTypes.Name, userValues.ToString()),
            new(ClaimTypes.Role, roleValues.ToString())
        };

        if (Request.Headers.TryGetValue(OrganizationHeader, out var organizationValues))
        {
            claims.Add(new Claim("organization_id", organizationValues.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}