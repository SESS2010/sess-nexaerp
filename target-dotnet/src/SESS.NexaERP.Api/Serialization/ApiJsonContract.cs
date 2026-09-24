using System.Text.Json;

namespace SESS.NexaERP.Api.Serialization;

public static class ApiJsonContract
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.DictionaryKeyPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(UtcDateTimeOffsetConverter.Instance);
    }

    /// <summary>
    /// Finding #31. Every incoming timestamp is converted to UTC as it is read. "+05:30" names one
    /// exact instant and timestamptz stores only the instant, but Npgsql refuses to write any offset
    /// other than 0, which reached the user as a 500. Converting keeps the instant and drops only
    /// the notation. A value without a zone is read as the server's local time, exactly as the
    /// default reader already did, and then converted.
    ///
    /// A safety net, not a substitute: clients must still send UTC. Writing is unchanged, so no
    /// response changes shape.
    /// </summary>
    public sealed class UtcDateTimeOffsetConverter : System.Text.Json.Serialization.JsonConverter<DateTimeOffset>
    {
        public static UtcDateTimeOffsetConverter Instance { get; } = new();

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.GetDateTimeOffset().ToUniversalTime();

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    private sealed class PascalCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public static PascalCaseJsonNamingPolicy Instance { get; } = new();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsLower(name[0])) return name;
            return char.ToUpperInvariant(name[0]) + name[1..];
        }
    }
}
