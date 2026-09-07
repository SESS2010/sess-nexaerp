using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class AuthenticationBootstrapSteps7To12Tests
{
    private static readonly string TestIssuer = "https://issuer.example.test/tenant/v2.0";
    private static readonly string TestSubject = "5d80fd62-63af-4d89-a5e6-44d22f866001";

    [Fact]
    public void Installer_accepts_provider_neutral_test_issuer_and_subject_without_provider_access()
    {
        Assert.True(AuthenticationBootstrapCommand.TryParse(["--issuer", TestIssuer, "--subject", TestSubject], out var issuer, out var subject, out var error), error);
        Assert.Equal(TestIssuer, issuer);
        Assert.Equal(TestSubject, subject);
        Assert.False(AuthenticationBootstrapCommand.TryParse(["--issuer", "http://insecure.test", "--subject", TestSubject], out _, out _, out _));
        Assert.False(AuthenticationBootstrapCommand.TryParse(["--issuer", TestIssuer, "--subject", ""], out _, out _, out _));
    }

    [Fact]
    public void Bootstrap_sql_is_one_time_two_company_and_least_privilege()
    {
        var sql = CeremonySql("Up");
        Assert.Contains("session_user<>'nexa_erp_bootstrap'", sql);
        Assert.Contains("FOR UPDATE", sql);
        Assert.Contains("has already been completed and cannot be replayed", sql);
        Assert.Contains("v_company_count<>2", sql);
        Assert.Contains("'SESS-12'", sql);
        Assert.Contains("'SURANTHER P'", sql);
        Assert.Contains("'IT_MANAGER'", sql);
        Assert.Contains("employee_identity_mappings", sql);
        Assert.Contains("employee_role_assignments", sql);
        Assert.Contains("operational scopes are incomplete or use the wrong CompanyId", sql);
        Assert.Contains("CompanySetSha256", sql);
        Assert.Contains("REVOKE ALL ON FUNCTION", sql);
        Assert.Contains("GRANT EXECUTE", sql);
        Assert.Contains("missing managed roles", sql);
        Assert.Contains("v_existing_count NOT IN (0,4)", sql);
        Assert.Contains("FROM PUBLIC;", sql);
        Assert.DoesNotContain("SESS-01", sql);
        Assert.DoesNotContain("SESS-02", sql);
    }

    [Fact]
    public void Bootstrap_migration_runs_cluster_guard_in_both_directions_and_refuses_consumed_down()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260826065344_AuthenticationBootstrapCeremonySteps7To12.cs");
        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("AuthenticationBootstrapCeremonySql.PreUp", migration);
        Assert.Contains("AuthenticationBootstrapCeremonySql.Up", migration);
        Assert.Contains("AuthenticationBootstrapCeremonySql.Down", migration);
        Assert.Contains("rollback refuses a consumed bootstrap", CeremonySql("Down"));
    }

    [Fact]
    public void Company_specific_identity_resolution_uses_explicit_organization_switch_and_no_provider_roles()
    {
        var resolver = Read("src", "SESS.NexaERP.Infrastructure", "Identity", "EfEmployeeIdentityResolver.cs");
        var middleware = Read("src", "SESS.NexaERP.Api", "Middleware", "EmployeeIdentityResolutionMiddleware.cs");
        Assert.Contains("x.OrganizationId == normalizedOrganization", resolver);
        Assert.Contains("x.CompanyId == companyId", resolver);
        Assert.Contains("organization_id", middleware);
        Assert.Contains("org_id", middleware);
        Assert.DoesNotContain("FindFirstValue(\"role\")", middleware, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-SESS-Acting-Role", middleware, StringComparison.Ordinal);
        Assert.DoesNotContain("group", middleware, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Identity_and_scope_creation_bind_request_to_current_company_and_company_warehouse()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs");
        Assert.Equal(2, Count(source, "if (!string.Equals(organization, user.OrganizationId, StringComparison.Ordinal)) return Results.Forbid();"));
        Assert.Contains("x.CompanyId == company.Id && x.WarehouseCode", source);
        Assert.Contains("x.CompanyId == company.Id && x.EmployeeId == employee.Id", source);
    }

    [Fact]
    public void Database_model_is_development_only_and_audit_history_is_permission_and_company_scoped()
    {
        var program = Read("src", "SESS.NexaERP.Api", "Program.cs");
        var audit = Read("src", "SESS.NexaERP.Api", "Endpoints", "AuditEndpoints.cs");
        var auditService = Read("src", "SESS.NexaERP.Infrastructure", "Audit", "EfAuditHistoryService.cs");
        var developmentBlock = program[program.IndexOf("if (app.Environment.IsDevelopment())", StringComparison.Ordinal)..program.IndexOf("app.MapSessionEndpoints();", StringComparison.Ordinal)];
        Assert.Contains("/api/v1/system/database-model", developmentBlock);
        Assert.Contains("RequirePagePermission(\"audit.history\", PagePermissionActions.ViewAuditHistory)", audit);
        Assert.Contains("x.CompanyId == companyId && x.Scope == \"COMPANY\"", auditService);
    }

    [Fact]
    public async Task Session_me_requires_authentication_and_returns_resolved_company_session()
    {
        await using var host = await EndpointHost.StartAsync(allowAudit: false);
        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/session/me")).StatusCode);
        Authenticate(client);
        var session = await client.GetFromJsonAsync<SessionMe>("/api/v1/session/me");
        Assert.NotNull(session);
        Assert.Equal("SESS-12", session.EmployeeCode);
        Assert.Equal("SESS_PVT_LTD", session.OrganizationId);
        Assert.Equal(["IT_MANAGER"], session.RoleCodes);
        Assert.Equal(["employees.master:view"], session.Permissions);
        Assert.Equal(TestIssuer, session.IdentityIssuer);
        Assert.Equal(TestSubject, session.IdentitySubject);
    }

    [Fact]
    public async Task Audit_history_requires_authentication_and_view_audit_history_permission()
    {
        await using var host = await EndpointHost.StartAsync(allowAudit: false);
        using var client = new HttpClient { BaseAddress = host.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/audit/history")).StatusCode);
        Authenticate(client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/audit/history")).StatusCode);
    }

    private static void Authenticate(HttpClient client)
    {
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.UserHeader, "SESS-12");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.RoleHeader, "IT_MANAGER");
        client.DefaultRequestHeaders.Add(TestOnlyAuthenticationHandler.OrganizationHeader, "SESS_PVT_LTD");
    }

    private static string CeremonySql(string property)
    {
        var type = typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.AuthenticationBootstrapCeremonySql", true)!;
        return (string)type.GetProperty(property, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    }
    private static int Count(string source, string value) => (source.Length - source.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;
    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. parts]));
    }

    private sealed class EndpointHost(WebApplication app, Uri baseAddress) : IAsyncDisposable
    {
        public Uri BaseAddress { get; } = baseAddress;
        public static async Task<EndpointHost> StartAsync(bool allowAudit)
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Test" });
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.AddRouting();
            builder.Services.AddAuthentication(TestOnlyAuthenticationHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestOnlyAuthenticationHandler>(TestOnlyAuthenticationHandler.SchemeName, _ => { });
            builder.Services.AddAuthorization(); builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<NexaErpDbContext>(options => options.UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect"));
            builder.Services.AddSingleton<ISessionService>(new FakeSessionService());
            builder.Services.AddScoped<ICurrentUser, HeaderCurrentUser>();
            builder.Services.AddSingleton<IRecordScopeAuthorizer, AllowScope>();
            builder.Services.AddSingleton<IPagePermissionService>(new AuditPermission(allowAudit));
            builder.Services.AddSingleton<IAuditWriter, NoopAudit>();
            builder.Services.AddSingleton<IAuditHistoryService, EmptyAuditHistory>();
            var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapSessionEndpoints(); app.MapAuditEndpoints();
            await app.StartAsync(); return new EndpointHost(app, new Uri($"http://127.0.0.1:{port}"));
        }
        public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
    }
    private sealed class FakeSessionService : ISessionService
    {
        public Task<SessionMe> GetCurrentAsync(CancellationToken ct) => Task.FromResult(new SessionMe(Guid.Parse("90000000-0000-0000-0000-000000000012"), "SESS-12", "SURANTHER P", Guid.Parse("70000000-0000-0000-0000-000000000001"), "SESS_PVT_LTD", Guid.Parse("50000000-0000-0000-0000-000000000001"), "IT", ["IT_MANAGER"], ["employees.master:view"], TestIssuer, TestSubject, ["IT_MANAGER"]));
    }
    private sealed class HeaderCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new();
        public string LoginId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unauthenticated";
        public string RoleCode => "IT_MANAGER"; public IReadOnlyList<string> RoleCodes => ["IT_MANAGER"];
        public string? OrganizationId => Principal.FindFirstValue("organization_id"); public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
        public Guid? EmployeeId => IsAuthenticated ? Guid.Parse("90000000-0000-0000-0000-000000000012") : null;
    }
    private sealed class AllowScope : IRecordScopeAuthorizer
    {
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid e, string r, string o, DateOnly d, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "test"));
        public Task<RecordScopeDecision> AuthorizeAsync(Guid e, string r, RecordScopeTarget t, DateOnly d, CancellationToken ct) => Task.FromResult(new RecordScopeDecision(true, "test"));
    }
    private sealed class AuditPermission(bool allowed) : IPagePermissionService
    {
        public Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roles, string page, string permission, CancellationToken ct) => Task.FromResult(allowed);
    }
    private sealed class NoopAudit : IAuditWriter
    {
        public Task WriteAsync(string m, string a, string n, string i, object? b, object? after, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class EmptyAuditHistory : IAuditHistoryService
    {
        public Task<PagedResponse<AuditLogSummary>> GetCompanyHistoryAsync(string? module, int page, int pageSize, CancellationToken ct) =>
            Task.FromResult(new PagedResponse<AuditLogSummary>(0, page, pageSize, []));
    }
}
