using System.Text.Json;
using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class BusinessDateSnapshotTests
{
    [Theory]
    [InlineData("2026-10-16T18:29:59Z", "2026-10-16", "2026-10-16")]
    [InlineData("2026-10-16T18:30:00Z", "2026-10-16", "2026-10-17")]
    [InlineData("2026-10-16T23:59:59Z", "2026-10-16", "2026-10-17")]
    [InlineData("2026-10-17T00:00:00Z", "2026-10-17", "2026-10-17")]
    public void LegacyAndNewEventDatesStayExplicitAndDeterministic(string instant, string legacy, string current)
    {
        var at = DateTimeOffset.Parse(instant);
        Assert.Equal(DateOnly.Parse(legacy), BusinessDatePolicy.EventDate(at, null));
        Assert.Equal(DateOnly.Parse(current), BusinessDatePolicy.EventDate(at, BusinessDatePolicy.KolkataV1));
        Assert.Throws<InvalidOperationException>(() => BusinessDatePolicy.EventDate(at, "unknown"));
    }

    [Fact]
    public void LegacySnapshotDoesNotGainAFieldOrRetaxWhileNewSnapshotUsesItsOwnEffectiveDay()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var legacy = new Rev869BTaxRuleSnapshot(Guid.NewGuid(), "SESS_PVT_LTD", TaxJurisdictions.IndiaGst,
            "85362030", "INTRASTATE", "33", "33", "REGULAR", 18, 9, 9, 0, 0, false, false,
            "INR", 2, new DateOnly(2026, 10, 16), new DateOnly(2026, 10, 16), "Approved", true);
        var json = JsonSerializer.Serialize(legacy, options);
        using var document = JsonDocument.Parse(json);
        Assert.False(document.RootElement.TryGetProperty("quotationDateBasis", out _));
        var restored = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(json, options)!;
        Assert.Null(restored.QuotationDateBasis);
        var receivedAt = DateTimeOffset.Parse("2026-10-16T20:00:00Z");
        var input = new Rev869BCommercialInput(10, 1500, 0, 0, 0, 0, 0, 9, 9, 0, 0, 0, 2);
        var stored = Rev869BCommercialCalculator.Calculate(input);
        Assert.Equal(stored, Rev869BCommercialCalculator.Reconcile(input, stored, restored,
            BusinessDatePolicy.EventDate(receivedAt, restored.QuotationDateBasis)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Reconcile(input, stored, restored,
            BusinessDatePolicy.EventDate(receivedAt, BusinessDatePolicy.KolkataV1)));
        var current = legacy with { EffectiveFrom = new(2026, 10, 17), EffectiveTo = new(2026, 10, 17), QuotationDateBasis = BusinessDatePolicy.KolkataV1 };
        Assert.Equal(stored, Rev869BCommercialCalculator.Reconcile(input, stored, current,
            BusinessDatePolicy.EventDate(receivedAt, current.QuotationDateBasis)));
        Assert.Equal(15000m, stored.TaxableValue);
        Assert.Equal(17700m, stored.TotalPayableValue);
        Assert.Equal(BusinessDatePolicy.KolkataV1,
            JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(JsonSerializer.Serialize(current, options), options)!.QuotationDateBasis);
    }

    [Fact]
    public void InvitationAndReceiptSnapshotsKeepMissingOrNullPolicyLegacyAndRejectUnknownPolicy()
    {
        Assert.Null(BusinessDatePolicy.ReadBasis("{}"));
        Assert.Null(BusinessDatePolicy.ReadBasis("{\"businessDateBasis\":null}"));
        Assert.Equal(BusinessDatePolicy.KolkataV1, BusinessDatePolicy.ReadBasis("{\"businessDateBasis\":\"ASIA_KOLKATA_V1\"}"));
        Assert.Throws<InvalidOperationException>(() => BusinessDatePolicy.ReadBasis("[]"));
        Assert.Throws<InvalidOperationException>(() => BusinessDatePolicy.EventDate(DateTimeOffset.UtcNow,
            BusinessDatePolicy.ReadBasis("{\"businessDateBasis\":\"UTC_OVERRIDE\"}")));
    }
}