using System.Reflection;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class PurchaseRegisterBusinessDateTests
{
    [Fact]
    public void RegisterDateRangeUsesKolkataMidnightForEveryPurchaseRegister()
    {
        Check<RequestForQuotation>(); Check<VendorQuotation>(); Check<CommercialComparison>(); Check<PurchaseOrder>();
    }

    private static void Check<T>() where T : AuditableEntity, new()
    {
        var start = DateTimeOffset.Parse("2026-10-17T18:30:00Z");
        var end = DateTimeOffset.Parse("2026-10-18T18:30:00Z");
        var instants = new[] { start.AddTicks(-1), start, start.AddHours(1),
            DateTimeOffset.Parse("2026-10-18T00:00:00Z").AddTicks(-1),
            DateTimeOffset.Parse("2026-10-18T00:00:00Z"), end.AddTicks(-1), end };
        var rows = instants.Select(instant => new T { CreatedAt = instant }).AsQueryable();
        var method = typeof(Rev869BPurchaseEndpoints).GetMethod("DateRange", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(T));
        var date = new DateOnly(2026, 10, 18);
        IQueryable<T> Filter(DateOnly? from, DateOnly? to) =>
            (IQueryable<T>)method.Invoke(null, new object?[] { rows, from, to })!;
        Assert.Equal(instants.Skip(1).Take(5), Filter(date, date).Select(x => x.CreatedAt));
        Assert.Equal(instants.Skip(1), Filter(date, null).Select(x => x.CreatedAt));
        Assert.Equal(instants.Take(6), Filter(null, date).Select(x => x.CreatedAt));
        Assert.Equal(instants, Filter(null, null).Select(x => x.CreatedAt));
    }

    [Fact]
    public void GateEntryArrivalDateFilterUsesTheSameInclusiveExclusiveKolkataBounds()
    {
        var start = DateTimeOffset.Parse("2026-10-17T18:30:00Z");
        var end = DateTimeOffset.Parse("2026-10-18T18:30:00Z");
        var instants = new[] { start.AddTicks(-1), start, start.AddHours(1), end.AddTicks(-1), end };
        var rows = instants.Select(value => new SESS.NexaERP.Domain.Stores.GateEntry { ArrivedAt = value }).AsQueryable();
        var method = typeof(SESS.NexaERP.Infrastructure.Stores.EfGateEntryService)
            .GetMethod("FilterArrivalDates", BindingFlags.Static | BindingFlags.NonPublic)!;
        var date = new DateOnly(2026, 10, 18);
        var result = (IQueryable<SESS.NexaERP.Domain.Stores.GateEntry>)method.Invoke(null, new object?[] { rows, date, date })!;
        Assert.Equal(instants.Skip(1).Take(3), result.Select(x => x.ArrivedAt));
    }

    [Theory]
    [InlineData("2026-10-18T00:00:00+05:30")]
    [InlineData("2026-10-18T05:29:59+05:30")]
    [InlineData("2026-10-18T05:30:00+05:30")]
    public void StartOfDateAndCurrentDateShareTheSameCalendar(string timestamp)
    {
        var instant = DateTimeOffset.Parse(timestamp);
        var date = BusinessCalendar.DateAt(instant);
        var start = BusinessCalendar.StartOfDate(date);
        Assert.Equal(DateTimeOffset.Parse("2026-10-17T18:30:00Z"), start);
        Assert.InRange(instant, start, BusinessCalendar.StartOfDate(date.AddDays(1)).AddTicks(-1));
        Assert.Equal(DateOnly.FromDateTime(instant.UtcDateTime), BusinessDatePolicy.EventDate(instant, null));
    }
}
