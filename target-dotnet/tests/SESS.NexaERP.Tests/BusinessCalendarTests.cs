using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed class BusinessCalendarTests
{
    [Theory]
    [InlineData("2026-10-16T18:29:59Z", "2026-10-16")]
    [InlineData("2026-10-16T18:30:00Z", "2026-10-17")]
    [InlineData("2026-10-16T23:59:59Z", "2026-10-17")]
    [InlineData("2026-10-17T00:00:00Z", "2026-10-17")]
    [InlineData("2026-03-31T18:29:59Z", "2026-03-31")]
    [InlineData("2026-03-31T18:30:00Z", "2026-04-01")]
    public void ConvertsInstantToKolkataBusinessDate(string instant, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), BusinessCalendar.DateAt(DateTimeOffset.Parse(instant)));

    [Theory]
    [InlineData("2026-10-16T18:29:59Z", "20261016", "20261016235959")]
    [InlineData("2026-10-16T18:30:00Z", "20261017", "20261017000000")]
    [InlineData("2026-10-17T00:00:00Z", "20261017", "20261017053000")]
    [InlineData("2026-03-31T18:30:00Z", "20260401", "20260401000000")]
    public void NewDocumentIdentifiersUseKolkataAndInvariantGregorianDigits(
        string instant, string date, string timestamp)
    {
        var parsed = DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture);
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-IN", "th-TH", "ar-SA" })
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
                Assert.Equal(date, BusinessCalendar.IdentifierDate(BusinessCalendar.DateAt(parsed)));
                Assert.Equal(timestamp, BusinessCalendar.IdentifierTimestamp(parsed));
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void OperationCapturesOnceButNextOperationUsesNewDate()
    {
        var clock = new Clock { Now = DateTimeOffset.Parse("2026-10-16T18:29:59Z") };
        var operation = new OperationBusinessDate(clock);
        Assert.Equal(new DateOnly(2026, 10, 16), operation.Today);
        clock.Now = DateTimeOffset.Parse("2026-10-16T18:30:00Z");
        Assert.Equal(new DateOnly(2026, 10, 16), operation.Today);
        Assert.Equal(1, clock.Reads);
        Assert.Equal(new DateOnly(2026, 10, 17), new OperationBusinessDate(clock).Today);
        Assert.Equal(2, clock.Reads);
    }

    [Fact]
    public void RequestAndDatabaseShareTheCapturedDateAcrossMidnightWithoutOpeningDatabase()
    {
        var clock = new Clock { Now = DateTimeOffset.Parse("2026-10-16T18:29:59Z") };
        using var provider = new ServiceCollection().AddSingleton<IDateTimeProvider>(clock)
            .AddScoped<OperationBusinessDate>()
            .AddScoped(sp => new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().Options,
                sp.GetRequiredService<OperationBusinessDate>())).BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            Assert.Equal(new DateOnly(2026, 10, 16), RequestBusinessDate.ForRequest(context));
            clock.Now = DateTimeOffset.Parse("2026-10-16T18:30:00Z");
            Assert.Equal(new DateOnly(2026, 10, 16), scope.ServiceProvider.GetRequiredService<NexaErpDbContext>().BusinessDate);
            Assert.Equal(new DateOnly(2026, 10, 16), BarcodeBusinessDate.ForRequest(context));
            Assert.Equal(1, clock.Reads);
        }
        using var next = provider.CreateScope();
        Assert.Equal(new DateOnly(2026, 10, 17), next.ServiceProvider.GetRequiredService<NexaErpDbContext>().BusinessDate);
        Assert.Equal(2, clock.Reads);
    }
    [Theory]
    [InlineData("2026-10-16T18:29:59Z", "2026-10-16")]
    [InlineData("2026-10-16T18:30:00Z", "2026-10-17")]
    [InlineData("2026-10-16T23:59:59Z", "2026-10-17")]
    [InlineData("2026-10-17T00:00:00Z", "2026-10-17")]
    public async Task IdentityResolverReceivesTheSameTrustedDateAsScopeAndDatabase(string instant, string expected)
    {
        var clock = new Clock { Now = DateTimeOffset.Parse(instant) };
        using var services = new ServiceCollection().AddSingleton<IDateTimeProvider>(clock)
            .AddScoped<OperationBusinessDate>().BuildServiceProvider();
        using var scope = services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
            [new("iss", "https://identity.example"), new("sub", "employee")], "Bearer"));
        context.Request.Headers["X-Business-Date"] = "1900-01-01";
        var resolver = new DateResolver();
        var reached = false;
        await new SESS.NexaERP.Api.Middleware.EmployeeIdentityResolutionMiddleware(_ =>
        { reached = true; return Task.CompletedTask; }).InvokeAsync(context, resolver);
        Assert.True(reached);
        Assert.Equal(DateOnly.Parse(expected), resolver.Seen);
        clock.Now = clock.Now.AddDays(1);
        Assert.Equal(resolver.Seen, RequestBusinessDate.ForRequest(context));
        Assert.Equal(1, clock.Reads);
    }

    private sealed class DateResolver : SESS.NexaERP.Application.Identity.IEmployeeIdentityResolver
    {
        public DateOnly Seen { get; private set; }
        public Task<SESS.NexaERP.Application.Identity.ResolvedEmployeeIdentity> ResolveAsync(
            string issuer, string subject, string? organization, DateOnly date, CancellationToken ct)
        {
            Seen = date;
            return Task.FromResult(new SESS.NexaERP.Application.Identity.ResolvedEmployeeIdentity(
                true, Guid.NewGuid(), Guid.NewGuid(), "SESS_PVT_LTD", "SESS-35", ["STORES_EXECUTIVE"], "Resolved"));
        }
#if DEBUG
        public Task<SESS.NexaERP.Application.Identity.ResolvedEmployeeIdentity> ResolveDevelopmentEmployeeAsync(
            string code, string? organization, DateOnly date, CancellationToken ct) => ResolveAsync("", "", organization, date, ct);
#endif
    }
    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public int Reads { get; private set; }
        public DateTimeOffset UtcNow { get { Reads++; return Now; } }
    }
}