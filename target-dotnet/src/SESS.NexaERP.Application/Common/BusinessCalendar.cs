namespace SESS.NexaERP.Application.Common;

/// <summary>Business calendar only; persisted audit/token instants remain UTC.</summary>
public static class BusinessCalendar
{
    public static DateOnly DateAt(DateTimeOffset instant) =>
        SESS.NexaERP.Domain.Common.BusinessDatePolicy.KolkataDate(instant);
}

/// <summary>One trusted business date per DI operation scope, including midnight-crossing requests.</summary>
public sealed class OperationBusinessDate(IDateTimeProvider clock)
{
    private DateOnly? captured;
    public DateOnly Today => captured ??= BusinessCalendar.DateAt(clock.UtcNow);
}