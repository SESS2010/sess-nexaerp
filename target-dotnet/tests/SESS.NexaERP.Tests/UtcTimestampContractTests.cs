using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Finding #31. Npgsql writes timestamptz only at offset 0, so a request carrying "+05:30"
/// reached the user as a 500. The API's JSON contract now converts every incoming timestamp to
/// UTC. It keeps the instant and drops only the notation. This is a safety net: the frontend
/// must still send UTC on every one of these fields.
///
/// The 13 request fields are the ones listed under #31 in the status document. The real-request
/// witness for row 1 is the LOW band of the complete purchase flow.
/// </summary>
public sealed class UtcTimestampContractTests
{
    private const string Local = "2026-09-30T18:00:00+05:30";
    private static readonly DateTimeOffset Instant = new(2026, 9, 30, 12, 30, 0, TimeSpan.Zero);

    private static JsonSerializerOptions ApiOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonContract.Configure(options);
        return options;
    }

    public static TheoryData<Type, string> ThirteenRequestFields() => new()
    {
        { typeof(Rev869BCreateRfqRequest), nameof(Rev869BCreateRfqRequest.QuoteDueAt) },
        { typeof(Rev869BSubmitQuotationRequest), nameof(Rev869BSubmitQuotationRequest.ReceivedAt) },
        { typeof(CreateGateEntryRequest), nameof(CreateGateEntryRequest.ArrivedAt) },
        { typeof(UpdateGateEntryRequest), nameof(UpdateGateEntryRequest.ArrivedAt) },
        { typeof(CreateGoodsReceiptRequest), nameof(CreateGoodsReceiptRequest.ReceivedAt) },
        { typeof(UpdateGoodsReceiptRequest), nameof(UpdateGoodsReceiptRequest.ReceivedAt) },
        { typeof(FinalizeQcInspectionRequest), nameof(FinalizeQcInspectionRequest.InspectionStartedAt) },
        { typeof(CorrectQcInspectionRequest), nameof(CorrectQcInspectionRequest.InspectionStartedAt) },
        { typeof(CreateMaterialIssue), nameof(CreateMaterialIssue.IssuedAt) },
        { typeof(CreateMaterialReturn), nameof(CreateMaterialReturn.DeclaredAt) },
        { typeof(AcceptMaterialReturn), nameof(AcceptMaterialReturn.AcceptedAt) },
        { typeof(ConfirmComponentFitmentRequest), nameof(ConfirmComponentFitmentRequest.FittedAt) },
        { typeof(SignMachineDeliveryRequest), nameof(SignMachineDeliveryRequest.DeliveredAt) },
    };

    [Theory]
    [MemberData(nameof(ThirteenRequestFields))]
    public void A_local_offset_arrives_as_the_same_instant_in_utc(Type requestType, string field)
    {
        var request = JsonSerializer.Deserialize($"{{\"{field}\":\"{Local}\"}}", requestType, ApiOptions())!;
        var value = (DateTimeOffset)requestType.GetProperty(field)!.GetValue(request)!;

        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(Instant, value);
    }

    [Theory]
    [MemberData(nameof(ThirteenRequestFields))]
    public void A_utc_value_is_unchanged(Type requestType, string field)
    {
        var request = JsonSerializer.Deserialize($"{{\"{field}\":\"2026-09-30T12:30:00Z\"}}", requestType, ApiOptions())!;
        var value = (DateTimeOffset)requestType.GetProperty(field)!.GetValue(request)!;

        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(Instant, value);
    }

    [Fact]
    public void The_same_instant_in_either_notation_fingerprints_identically()
    {
        // Services fingerprint the bound request for idempotency. Resending the same instant with
        // a different offset must replay, not conflict.
        var fingerprintOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var local = JsonSerializer.Deserialize<Rev869BCreateRfqRequest>($"{{\"QuoteDueAt\":\"{Local}\"}}", ApiOptions())!;
        var utc = JsonSerializer.Deserialize<Rev869BCreateRfqRequest>("{\"QuoteDueAt\":\"2026-09-30T12:30:00Z\"}", ApiOptions())!;

        Assert.Equal(JsonSerializer.Serialize(utc, fingerprintOptions), JsonSerializer.Serialize(local, fingerprintOptions));
    }

    private sealed record OptionalTimestamp(DateTimeOffset? At);

    [Fact]
    public void A_nullable_timestamp_is_converted_and_null_stays_null()
    {
        Assert.Equal(TimeSpan.Zero, JsonSerializer.Deserialize<OptionalTimestamp>($"{{\"At\":\"{Local}\"}}", ApiOptions())!.At!.Value.Offset);
        Assert.Null(JsonSerializer.Deserialize<OptionalTimestamp>("{\"At\":null}", ApiOptions())!.At);
    }

    [Fact]
    public void Responses_are_written_exactly_as_before()
    {
        // Nothing the frontend reads changes: writing is the default writer's output.
        var value = new DateTimeOffset(2026, 9, 30, 18, 0, 0, TimeSpan.FromHours(5.5));
        Assert.Equal(
            JsonSerializer.Serialize(new OptionalTimestamp(value), new JsonSerializerOptions(JsonSerializerDefaults.Web)).Replace("\"at\"", "\"At\""),
            JsonSerializer.Serialize(new OptionalTimestamp(value), ApiOptions()));
    }

    [Fact]
    public void An_invalid_timestamp_is_still_a_json_error()
    {
        Assert.ThrowsAny<JsonException>(() =>
            JsonSerializer.Deserialize<Rev869BCreateRfqRequest>("{\"QuoteDueAt\":\"not a date\"}", ApiOptions()));
    }

    // Zone-less values: decided by the Technical Director on 25 September. Read as India Standard
    // Time, never as the server's zone, and a warning naming the field is logged every time.
    // This laptop runs in India Standard Time, so these tests pin the arithmetic and the warning;
    // the converter takes the offset from a constant and never consults TimeZoneInfo.Local.
    private const string ZoneLess = "2026-09-30T18:00:00";

    private static (JsonSerializerOptions Options, CapturingLogger Log) LoggedApiOptions()
    {
        var log = new CapturingLogger();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonContract.Configure(options, log);
        return (options, log);
    }

    [Theory]
    [MemberData(nameof(ThirteenRequestFields))]
    public void A_zone_less_value_is_read_as_india_standard_time_and_warned_by_field(Type requestType, string field)
    {
        var (options, log) = LoggedApiOptions();
        var request = JsonSerializer.Deserialize($"{{\"{field}\":\"{ZoneLess}\"}}", requestType, options)!;
        var value = (DateTimeOffset)requestType.GetProperty(field)!.GetValue(request)!;

        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(Instant, value);
        var warning = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal($"{requestType.Name}.{field}", warning.Field);
        Assert.Contains(ZoneLess, warning.Message);
    }

    [Theory]
    [InlineData(Local)]
    [InlineData("2026-09-30T12:30:00Z")]
    public void A_value_with_a_zone_logs_nothing(string text)
    {
        var (options, log) = LoggedApiOptions();
        var request = JsonSerializer.Deserialize<Rev869BCreateRfqRequest>($"{{\"QuoteDueAt\":\"{text}\"}}", options)!;

        Assert.Equal(Instant, request.QuoteDueAt);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public void Each_zone_less_value_is_warned_every_time()
    {
        var (options, log) = LoggedApiOptions();
        for (var i = 0; i < 3; i++)
            JsonSerializer.Deserialize<Rev869BCreateRfqRequest>($"{{\"QuoteDueAt\":\"{ZoneLess}\"}}", options);

        Assert.Equal(3, log.Entries.Count);
    }

    [Fact]
    public void A_zone_less_date_alone_is_midnight_in_india()
    {
        var (options, log) = LoggedApiOptions();
        var request = JsonSerializer.Deserialize<Rev869BCreateRfqRequest>("{\"QuoteDueAt\":\"2026-09-30\"}", options)!;

        Assert.Equal(new DateTimeOffset(2026, 9, 29, 18, 30, 0, TimeSpan.Zero), request.QuoteDueAt);
        Assert.Equal("Rev869BCreateRfqRequest.QuoteDueAt", Assert.Single(log.Entries).Field);
    }

    [Fact]
    public void A_zone_less_nullable_value_is_converted_and_named()
    {
        var (options, log) = LoggedApiOptions();

        Assert.Equal(Instant, JsonSerializer.Deserialize<OptionalTimestamp>($"{{\"At\":\"{ZoneLess}\"}}", options)!.At);
        Assert.Null(JsonSerializer.Deserialize<OptionalTimestamp>("{\"At\":null}", options)!.At);
        Assert.Equal("OptionalTimestamp.At", Assert.Single(log.Entries).Field);
    }

    [Fact]
    public void A_zone_less_value_outside_a_property_is_still_converted_and_warned()
    {
        var (options, log) = LoggedApiOptions();

        Assert.Equal(Instant, Assert.Single(JsonSerializer.Deserialize<List<DateTimeOffset>>($"[\"{ZoneLess}\"]", options)!));
        Assert.Equal("(unnamed value)", Assert.Single(log.Entries).Field);
    }

    [Fact]
    public async Task The_api_registration_writes_the_warning_to_the_application_log()
    {
        // Proves the registration Program.cs uses, not a copy of it: a real host, a real request.
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var log = new CapturingLogger();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Test" });
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders().AddProvider(new CapturingLoggerProvider(log));
        builder.Services.AddApiJsonContract();
        await using var app = builder.Build();
        app.MapPost("/rfq", (Rev869BCreateRfqRequest request) => Results.Ok(new { request.QuoteDueAt }));
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        using var response = await client.PostAsync("/rfq",
            new StringContent($"{{\"QuoteDueAt\":\"{ZoneLess}\"}}", System.Text.Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        await app.StopAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Equal(Instant, JsonDocument.Parse(body).RootElement.GetProperty("QuoteDueAt").GetDateTimeOffset());
        var warning = Assert.Single(log.Entries, entry => entry.Category == typeof(ApiJsonContract).FullName);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal("Rev869BCreateRfqRequest.QuoteDueAt", warning.Field);
    }

    private sealed record LogEntry(string Category, LogLevel Level, string? Field, string Message);

    private sealed class CapturingLogger(string category = "") : ILogger
    {
        private readonly List<LogEntry> _entries = [];
        private readonly CapturingLogger? _root;

        private CapturingLogger(CapturingLogger root, string category) : this(category) => _root = root;

        public IReadOnlyList<LogEntry> Entries { get { lock (Sink._entries) return Sink._entries.ToArray(); } }
        private CapturingLogger Sink => _root ?? this;

        public ILogger For(string name) => new CapturingLogger(Sink, name);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var field = (state as IEnumerable<KeyValuePair<string, object?>>)?
                .FirstOrDefault(pair => pair.Key == "Field").Value as string;
            lock (Sink._entries) Sink._entries.Add(new LogEntry(category, logLevel, field, formatter(state, exception)));
        }
    }

    private sealed class CapturingLoggerProvider(CapturingLogger log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => log.For(categoryName);
        public void Dispose() { }
    }
}
