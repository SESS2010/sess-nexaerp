using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Tests;

public sealed class AccountsBackupMfaBoundaryTests
{
    [Theory]
    [InlineData("SESS_PVT_LTD", false)]
    [InlineData("SESS_PVT_LTD", true)]
    [InlineData("SESS_PROPRIETORSHIP", false)]
    [InlineData("SESS_PROPRIETORSHIP", true)]
    public async Task AccountsBackupRequiresValidatedMfaInBothCompanies(string company, bool assured)
    {
        using var services = new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context = Context(services, company);
        context.Items[OidcAccessTokenConfiguration.ValidatedIdentityKey] =
            new ValidatedOidcIdentity("https://accounts.test/realms/approvers", "named-accounts-subject", company, assured);
        var resolver = new Resolver(company);
        var reached = false;
        await new EmployeeIdentityResolutionMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context, resolver);
        Assert.Equal(assured, reached);
        Assert.Equal(assured ? 200 : 403, context.Response.StatusCode);
        Assert.Equal(new DateOnly(2026, 10, 17), resolver.SeenDate);
        if (!assured) Assert.Equal("MFA_REQUIRED", context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey]);
        else {
            var resolved = Assert.IsType<ResolvedEmployeeIdentity>(context.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey]);
            Assert.Equal(AccountsBackupIdentityPolicy.MappingType, resolved.IdentityAuthorityType);
            Assert.Equal("SESS-41", resolved.EmployeeCode);
            Assert.Equal(new[] { "ACCOUNTS_MANAGER" }, resolved.RoleCodes);
            Assert.Equal(company, resolved.OrganizationId);
        }
    }

    [Fact]
    public async Task ForgedOtpClaimAndEmployeeHeadersCannotReplaceValidatedMfa()
    {
        using var services = new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context = Context(services, "SESS_PVT_LTD");
        var reached = false;
        await new EmployeeIdentityResolutionMiddleware(_ => { reached = true; return Task.CompletedTask; })
            .InvokeAsync(context, new Resolver("SESS_PVT_LTD"));
        Assert.False(reached);
        Assert.Equal(403, context.Response.StatusCode);
        Assert.Equal("MFA_REQUIRED", context.Items[StandardErrorEnvelopeMiddleware.AuthenticationFailureKey]);
    }

    [Fact]
    public async Task OrdinaryStaffStoresIdentityStillWorksWithoutAccountsMfa()
    {
        using var services = new ServiceCollection().AddSingleton<IDateTimeProvider>(new Clock()).BuildServiceProvider();
        var context = Context(services, "SESS_PVT_LTD");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim("iss", "https://accounts.test/realms/staff"), new Claim("sub", "stores-subject"),
            new Claim("organization_id", "SESS_PVT_LTD"), new Claim(ClaimTypes.Role, "ACCOUNTS_MANAGER")
        }, "Fixture"));
        var reached = false;
        await new EmployeeIdentityResolutionMiddleware(_ => { reached = true; return Task.CompletedTask; })
            .InvokeAsync(context, new Resolver("SESS_PVT_LTD", false));
        Assert.True(reached);
        Assert.Equal(200, context.Response.StatusCode);
        var resolved = Assert.IsType<ResolvedEmployeeIdentity>(context.Items[EmployeeIdentityResolutionMiddleware.ResolutionItemKey]);
        Assert.Equal("HUMAN", resolved.IdentityAuthorityType);
        Assert.Equal(new[] { "STORES_MANAGER" }, resolved.RoleCodes);
        Assert.DoesNotContain("ACCOUNTS_MANAGER", resolved.RoleCodes);
    }

    private static DefaultHttpContext Context(IServiceProvider services, string company)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/v1/purchase/requisitions/example/approve";
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim("iss", "https://accounts.test/realms/approvers"), new Claim("sub", "named-accounts-subject"),
            new Claim("organization_id", company), new Claim("amr", "otp"), new Claim(ClaimTypes.Role, "MANAGING_DIRECTOR")
        }, "Fixture"));
        context.Request.Headers["X-Employee-Id"] = Guid.NewGuid().ToString();
        context.Request.Headers["X-Role"] = "MANAGING_DIRECTOR";
        return context;
    }
    private sealed class Clock : IDateTimeProvider { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-10-16T20:00:00Z"); }
    private sealed class Resolver(string company, bool backup = true) : IEmployeeIdentityResolver
    {
        public DateOnly SeenDate { get; private set; }
        public Task<ResolvedEmployeeIdentity> ResolveAsync(string issuer, string subject, string? organizationId, DateOnly date, CancellationToken ct)
        {
            SeenDate = date;
            Assert.Equal(company, organizationId);
            var role = backup ? "ACCOUNTS_MANAGER" : "STORES_MANAGER";
            return Task.FromResult(new ResolvedEmployeeIdentity(true, Guid.NewGuid(), Guid.NewGuid(), company, "SESS-41",
                new[] { role }, "Resolved", new[] { role },
                new[] { new EffectiveRoleAssignment(Guid.NewGuid(), role, "FULL") }, backup ? AccountsBackupIdentityPolicy.MappingType : "HUMAN"));
        }
#if DEBUG
        public Task<ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(string employeeCode, string? organizationId, DateOnly date, CancellationToken ct) =>
            ResolveAsync("", "", organizationId, date, ct);
#endif
    }
}
