using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Masters;

namespace SESS.NexaERP.Tests;

/// <summary>R6/R7 print support: Indian amount in words, and the optional DC dispatch details.</summary>
public sealed class PrintContractTests
{
    [Theory]
    [InlineData("0", "Rupees Zero Only")]
    [InlineData("5", "Rupees Five Only")]
    [InlineData("100", "Rupees One Hundred Only")]
    [InlineData("1234.50", "Rupees One Thousand Two Hundred Thirty Four and Fifty Paise Only")]
    [InlineData("120050", "Rupees One Lakh Twenty Thousand Fifty Only")]
    [InlineData("12345678.99", "Rupees One Crore Twenty Three Lakh Forty Five Thousand Six Hundred Seventy Eight and Ninety Nine Paise Only")]
    [InlineData("0.995", "Rupees One Only")]
    public void Totals_print_in_Indian_words(string amount, string words) =>
        Assert.Equal(words, AmountInWords.Rupees(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

    private static readonly DateOnly Today = new(2026, 9, 26);
    private static DispatchMachineRequest Dispatch(string? vehicle = null, string? transporter = null, string? eway = null, DateOnly? ewayDate = null) =>
        new(Guid.NewGuid(), "MDC-1", "NON_RETURNABLE", "CUSTOMER_PO_BASED", Today, null, "Customer site", "key-1", vehicle, transporter, eway, ewayDate);

    [Fact]
    public void Dispatch_details_are_optional_and_accepted_when_well_formed()
    {
        var now = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(5.5));
        MachineDeliveryRequestValidation.Dispatch(Dispatch(), now);
        MachineDeliveryRequestValidation.Dispatch(Dispatch("TN 01 AB 1234", "Own vehicle", "123456789012", Today.AddDays(-1)), now);
    }

    [Theory]
    [InlineData("EwayBillNo", "12345", "2026-09-26")]
    [InlineData("EwayBillNo", "12345678901A", "2026-09-26")]
    [InlineData("EwayBillDate", "123456789012", null)]
    [InlineData("EwayBillDate", null, "2026-09-26")]
    [InlineData("EwayBillDate", "123456789012", "2026-09-27")]
    public void A_malformed_e_way_bill_names_its_field(string field, string? number, string? date)
    {
        var failure = Assert.Throws<StoresValidationException>(() => MachineDeliveryRequestValidation.Dispatch(
            Dispatch(eway: number, ewayDate: date is null ? null : DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture)),
            new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(5.5))));
        Assert.Contains(field, failure.Errors!.Keys);
    }

    [Fact]
    public void Over_long_vehicle_and_transporter_are_refused()
    {
        var failure = Assert.Throws<StoresValidationException>(() => MachineDeliveryRequestValidation.Dispatch(
            Dispatch(new string('V', 31), new string('T', 201)), new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(5.5))));
        Assert.Equal(["Transporter", "VehicleNo"], failure.Errors!.Keys.Order().ToArray());
    }
}
