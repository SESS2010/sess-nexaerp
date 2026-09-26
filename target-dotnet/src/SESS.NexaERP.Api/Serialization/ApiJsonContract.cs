using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace SESS.NexaERP.Api.Serialization;

public static class ApiJsonContract
{
    /// <summary>India Standard Time has had no daylight saving since 1945, so a fixed offset is exact.</summary>
    public static readonly TimeSpan IndiaStandardTime = TimeSpan.FromMinutes(330);

    /// <summary>The API's registration: the contract, with zone-less warnings in the application log.</summary>
    public static IServiceCollection AddApiJsonContract(this IServiceCollection services)
    {
        services.AddOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>().Configure<ILoggerFactory>((options, loggers) =>
            Configure(options.SerializerOptions, loggers.CreateLogger(typeof(ApiJsonContract).FullName!)));
        return services;
    }

    public static void Configure(JsonSerializerOptions options) => Configure(options, NullLogger.Instance);

    public static void Configure(JsonSerializerOptions options, ILogger zoneLessTimestampLog)
    {
        options.PropertyNamingPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.DictionaryKeyPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new UtcDateTimeOffsetConverter(zoneLessTimestampLog, field: null));
        // A converter never sees the property it is reading, so each timestamp property gets its own
        // converter carrying its name. That is what lets the zone-less warning name the field.
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(typeInfo => NameTimestampFields(typeInfo, zoneLessTimestampLog));
    }

    private static void NameTimestampFields(JsonTypeInfo typeInfo, ILogger log)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        foreach (var property in typeInfo.Properties)
        {
            if (property.CustomConverter is not null) continue;
            var field = $"{typeInfo.Type.Name}.{property.Name}";
            if (property.PropertyType == typeof(DateTimeOffset))
                property.CustomConverter = new UtcDateTimeOffsetConverter(log, field);
            else if (property.PropertyType == typeof(DateTimeOffset?))
                property.CustomConverter = new NullableUtcDateTimeOffsetConverter(new UtcDateTimeOffsetConverter(log, field));
        }
    }

    /// <summary>
    /// Finding #31. Every incoming timestamp is converted to UTC as it is read. "+05:30" names one
    /// exact instant and timestamptz stores only the instant, but Npgsql refuses to write any offset
    /// other than 0, which reached the user as a 500. Converting keeps the instant and drops only
    /// the notation.
    ///
    /// A value without a zone is read as India Standard Time, as decided by the Technical Director
    /// on 25 September, and a warning naming the field is logged every time. The server's own time
    /// zone plays no part.
    ///
    /// A safety net, not a substitute: clients must still send UTC. Writing is unchanged, so no
    /// response changes shape.
    /// </summary>
    public sealed class UtcDateTimeOffsetConverter(ILogger log, string? field) : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // The reader returns Kind Unspecified exactly when the text carries neither Z nor an offset.
            var parsed = reader.GetDateTime();
            if (parsed.Kind != DateTimeKind.Unspecified) return reader.GetDateTimeOffset().ToUniversalTime();

            var instant = new DateTimeOffset(parsed, IndiaStandardTime).ToUniversalTime();
            log.LogWarning(
                "Timestamp {Field} arrived without a time zone as {Received}; read as India Standard Time (+05:30), stored as {Utc:O}. The client must send UTC.",
                field ?? "(unnamed value)", reader.GetString(), instant);
            return instant;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    private sealed class NullableUtcDateTimeOffsetConverter(UtcDateTimeOffsetConverter inner) : JsonConverter<DateTimeOffset?>
    {
        // Null is handled by the serializer on both sides: HandleNull stays false for Nullable<T>.
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            inner.Read(ref reader, typeof(DateTimeOffset), options);

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value is { } present) writer.WriteStringValue(present);
            else writer.WriteNullValue();
        }
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
