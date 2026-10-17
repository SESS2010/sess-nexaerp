namespace SESS.NexaERP.Application.Common;

/// <summary>Business calendar only; persisted audit/token instants remain UTC.</summary>
public static class BusinessCalendar
{
    private static readonly TimeZoneInfo Kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    public static DateOnly DateAt(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Kolkata).DateTime);
}

/// <summary>One trusted business date per DI operation scope, including midnight-crossing requests.</summary>
public sealed class OperationBusinessDate(IDateTimeProvider clock)
{
    private DateOnly? captured;
    public DateOnly Today => captured ??= BusinessCalendar.DateAt(clock.UtcNow);
}