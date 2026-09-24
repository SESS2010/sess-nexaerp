using System.Text.Json;
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
}
