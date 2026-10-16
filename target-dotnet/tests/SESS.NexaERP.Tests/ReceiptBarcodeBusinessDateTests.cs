using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Tests;

public sealed class BarcodeBusinessDateTests
{
    [Theory]
    [InlineData("2026-10-15T18:29:59Z", "2026-10-16", null, "2026-10-15", false)]
    [InlineData("2026-10-15T18:30:00Z", "2026-10-16", null, "2026-10-16", true)]
    [InlineData("2026-10-15T22:30:00Z", "2026-10-16", null, "2026-10-16", true)]
    [InlineData("2026-10-15T23:59:59Z", "2026-10-16", null, "2026-10-16", true)]
    [InlineData("2026-10-16T00:00:00Z", "2026-10-16", null, "2026-10-16", true)]
    [InlineData("2026-10-16T18:29:59Z", "2026-10-16", null, "2026-10-16", true)]
    [InlineData("2026-10-15T18:29:59Z", "2026-10-01", "2026-10-15", "2026-10-15", true)]
    [InlineData("2026-10-15T18:30:00Z", "2026-10-01", "2026-10-15", "2026-10-16", false)]
    [InlineData("2026-10-15T22:30:00Z", "2026-10-01", "2026-10-15", "2026-10-16", false)]
    [InlineData("2026-10-15T23:59:59Z", "2026-10-01", "2026-10-15", "2026-10-16", false)]
    [InlineData("2026-10-16T00:00:00Z", "2026-10-01", "2026-10-15", "2026-10-16", false)]
    [InlineData("2026-10-16T18:29:59Z", "2026-10-01", "2026-10-16", "2026-10-16", true)]
    public async Task ReceiptScopeStartsAndEndsOnKolkataDateAcrossMidnightAndUtcBoundary(
        string utc, string from, string? to, string expectedDate, bool allowed)
    {
        var clock = new Clock(DateTimeOffset.Parse(utc));
        var scope = new Scope(DateOnly.Parse(from), to is null ? null : DateOnly.Parse(to));
        using var services = Services(clock, scope);
        var context = Context(services, true);
        context.Request.Headers["X-Business-Date"] = "1900-01-01"; // No client-selected date policy.
        var nextCalled = false;
        var result = await EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope(
            new DefaultEndpointFilterInvocationContext(context), _ => { nextCalled = true; return ValueTask.FromResult<object?>("allowed"); });
        Assert.Equal(allowed, nextCalled);
        Assert.Equal(DateOnly.Parse(expectedDate), scope.SeenDate);
        Assert.Equal("STORES_EXECUTIVE", scope.SeenRole);
        Assert.Equal("SESS_PVT_LTD", scope.SeenCompany);
        Assert.Equal(User.Employee, scope.SeenEmployee);
        if (allowed) Assert.Equal("allowed", result); else Assert.IsType<ForbidHttpResult>(result);
        Assert.Equal(DateOnly.Parse(expectedDate), BarcodeBusinessDate.ForRequest(context));
        Assert.Equal(1, clock.Reads);
    }

    [Fact]
    public async Task DuplicateFiltersAndHandlerShareOneDateEvenIfTheClockCrossesMidnight()
    {
        var clock = new Clock(DateTimeOffset.Parse("2026-10-15T18:29:59Z"));
        var scope = new Scope(new(2026, 1, 1), null);
        using var services = Services(clock, scope);
        var context = Context(services, true);
        await EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope(new DefaultEndpointFilterInvocationContext(context), _ => ValueTask.FromResult<object?>(null));
        clock.Now = DateTimeOffset.Parse("2026-10-15T18:30:00Z");
        await EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope(new DefaultEndpointFilterInvocationContext(context), _ => ValueTask.FromResult<object?>(null));
        Assert.Equal(new DateOnly(2026, 10, 15), scope.SeenDate);
        Assert.Equal(new DateOnly(2026, 10, 15), BarcodeBusinessDate.ForRequest(context));
        Assert.Equal(1, clock.Reads);
        Assert.Equal(new DateOnly(2026, 10, 16), BarcodeBusinessDate.ForRequest(Context(services, true)));
    }

    [Fact]
    public async Task UnmarkedLegacyEndpointsRetainTheirExistingUtcDatePolicy()
    {
        var clock = new Clock(DateTimeOffset.Parse("1999-01-01T00:00:00Z"));
        var scope = new Scope(new(1900, 1, 1), null);
        using var services = Services(clock, scope);
        var before = DateOnly.FromDateTime(DateTime.UtcNow);
        await EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope(new DefaultEndpointFilterInvocationContext(Context(services, false)), _ => ValueTask.FromResult<object?>(null));
        Assert.Contains(scope.SeenDate, new[] { before, DateOnly.FromDateTime(DateTime.UtcNow) });
        Assert.Equal(0, clock.Reads);
    }

    private static ServiceProvider Services(Clock clock, Scope scope) => new ServiceCollection()
        .AddSingleton<IDateTimeProvider>(clock).AddSingleton<ICurrentUser>(new User())
        .AddSingleton<IRecordScopeAuthorizer>(scope).AddSingleton<IAuditWriter>(new Audit()).BuildServiceProvider();
    private static DefaultHttpContext Context(IServiceProvider services, bool marked)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            marked ? new EndpointMetadataCollection(new BarcodeBusinessDate()) : new EndpointMetadataCollection(), "receipt-scope-test"));
        return context;
    }
    private sealed class Clock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public int Reads { get; private set; }
        public DateTimeOffset UtcNow { get { Reads++; return Now; } }
    }
    private sealed class User : ICurrentUser
    {
        internal static readonly Guid Employee = Guid.NewGuid();
        public string LoginId => "scope-date-test";
        public string RoleCode => "STORES_EXECUTIVE";
        public string? OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public Guid? EmployeeId => Employee;
    }
    private sealed class Scope(DateOnly from, DateOnly? to) : IRecordScopeAuthorizer
    {
        public DateOnly SeenDate { get; private set; }
        public string? SeenRole { get; private set; }
        public string? SeenCompany { get; private set; }
        public Guid SeenEmployee { get; private set; }
        public Task<RecordScopeDecision> AuthorizeAnyAsync(Guid employeeId, string roleCode, string organizationId, DateOnly onDate, CancellationToken ct)
        {
            SeenDate = onDate; SeenRole = roleCode; SeenCompany = organizationId; SeenEmployee = employeeId;
            return Task.FromResult(new RecordScopeDecision(from <= onDate && (!to.HasValue || to >= onDate), "effective scope boundary"));
        }
        public Task<RecordScopeDecision> AuthorizeAsync(Guid employeeId, string roleCode, RecordScopeTarget target, DateOnly onDate, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Audit : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entityName, string entityId, object? before, object? after, CancellationToken ct) => Task.CompletedTask;
    }
}