namespace SESS.NexaERP.Application.Common;

/// <summary>Business calendar only; persisted audit/token instants remain UTC.</summary>
public static class BusinessCalendar
{
    public static DateOnly DateAt(DateTimeOffset instant) =>
        SESS.NexaERP.Domain.Common.BusinessDatePolicy.KolkataDate(instant);

    /// <summary>Culture-independent date in new document numbers; retained numbers stay unchanged.</summary>
    public static string IdentifierDate(DateOnly date) =>
        date.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Kolkata wall-clock suffix for new identifiers; audit instants remain UTC.</summary>
    public static string IdentifierTimestamp(DateTimeOffset instant) =>
        SESS.NexaERP.Domain.Common.BusinessDatePolicy.KolkataTime(instant)
            .ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

    public static DateTimeOffset StartOfDate(DateOnly date) =>
        SESS.NexaERP.Domain.Common.BusinessDatePolicy.KolkataStartOfDay(date);
}

/// <summary>One trusted business date per DI operation scope, including midnight-crossing requests.</summary>
public sealed class OperationBusinessDate(IDateTimeProvider clock)
{
    private DateOnly? captured;
    public DateOnly Today => captured ??= BusinessCalendar.DateAt(clock.UtcNow);
}