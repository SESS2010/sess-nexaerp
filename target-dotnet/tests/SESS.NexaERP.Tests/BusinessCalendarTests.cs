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
    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public int Reads { get; private set; }
        public DateTimeOffset UtcNow { get { Reads++; return Now; } }
    }
}