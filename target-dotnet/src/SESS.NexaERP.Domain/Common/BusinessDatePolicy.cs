using System.Text.Json;

namespace SESS.NexaERP.Domain.Common;

/// <summary>Retained event-date interpretation; missing policy means the pre-upgrade UTC contract.</summary>
public static class BusinessDatePolicy
{
    public const string KolkataV1 = "ASIA_KOLKATA_V1";
    private static readonly TimeZoneInfo Kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    public static DateOnly KolkataDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Kolkata).DateTime);
    /// <summary>Inclusive UTC instant at the start of a Kolkata business date, for current-date filters.</summary>
    public static DateTimeOffset KolkataStartOfDay(DateOnly date) =>
        new(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Kolkata));
    public static DateOnly EventDate(DateTimeOffset instant, string? basis) => basis switch
    {
        null => DateOnly.FromDateTime(instant.UtcDateTime),
        KolkataV1 => KolkataDate(instant),
        _ => throw new InvalidOperationException("The retained business-date policy is unknown.")
    };
    public static string? ReadBasis(string snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Business-date snapshot must be an object.");
        return document.RootElement.TryGetProperty("businessDateBasis", out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() : null;
    }
}