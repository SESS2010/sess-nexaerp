using System.Text.Json;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class InputTaxCreditEligibilityTests
{
    [Theory]
    [InlineData("PARTIALLY_RECOVERABLE", null)]
    [InlineData("PARTIALLY_RECOVERABLE", 0)]
    [InlineData("PARTIALLY_RECOVERABLE", 100)]
    [InlineData("FULLY_RECOVERABLE", 50)]
    [InlineData("BLOCKED", 100)]
    [InlineData("UNKNOWN", null)]
    public void Invalid_or_ambiguous_recovery_is_rejected(string state, int? percent) =>
        Assert.Throws<InvalidOperationException>(() => InputTaxCreditEligibility.RecoveryPercent(state, percent));

    [Fact]
    public void Captured_eligibility_survives_serialization_and_legacy_snapshots_default_to_full_credit()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var snapshot = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>("{}", options)!;
        Assert.Equal(InputTaxCreditEligibility.FullyRecoverable, snapshot.ItcEligibility);
        Assert.Equal(100m, InputTaxCreditEligibility.RecoveryPercent(snapshot.ItcEligibility, snapshot.RecoverableTaxPercent));
        var captured = snapshot with { ItcEligibility = InputTaxCreditEligibility.PartiallyRecoverable, RecoverableTaxPercent = 37.5m };
        var restored = JsonSerializer.Deserialize<Rev869BTaxRuleSnapshot>(JsonSerializer.Serialize(captured, options), options)!;
        Assert.Equal(captured, restored);
        Assert.Equal(37.5m, InputTaxCreditEligibility.RecoveryPercent(restored.ItcEligibility, restored.RecoverableTaxPercent));
    }
}
