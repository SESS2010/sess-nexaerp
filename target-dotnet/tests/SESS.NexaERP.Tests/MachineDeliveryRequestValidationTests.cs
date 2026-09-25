using System.Text;
using System.Text.Json;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

/// <summary>
/// Findings #34 and #35. A machine DC request is judged on its own fields first: each refusal is a
/// 400 naming the field. Only rules that need the database stay 409. The real-database witness,
/// including the 500 this replaces, is in <c>MachineDeliveryWitnessTests</c>.
/// </summary>
public sealed class MachineDeliveryRequestValidationTests
{
    // 30 Sep 2026 12:00 IST.
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 6, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 30);
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nsigned\n%%EOF");

    private static DispatchMachineRequest Returnable() =>
        new(Guid.NewGuid(), "MDC-001", "RETURNABLE", "DEMO", Today, Today.AddDays(7), "Customer gate", "dispatch-1");

    private static DispatchMachineRequest NonReturnable() =>
        new(Guid.NewGuid(), "MDC-002", "NON_RETURNABLE", "CUSTOMER_PO_BASED", Today, null, "Customer site", "dispatch-2");

    private static SignMachineDeliveryRequest Signed() =>
        new(Now.AddMinutes(-5), "Customer representative", new SupplierInvoiceEvidenceInput("signed.pdf", "application/pdf", Pdf), "sign-1");

    private static IReadOnlyDictionary<string, string[]> Refusal(Action action) =>
        Assert.Throws<StoresValidationException>(action).Errors!;

    [Fact]
    public void Valid_requests_pass()
    {
        MachineDeliveryRequestValidation.Dispatch(Returnable(), Now);
        MachineDeliveryRequestValidation.Dispatch(NonReturnable(), Now);
        Assert.Equal("signed.pdf", MachineDeliveryRequestValidation.Sign(Signed(), Now, out var type));
        Assert.Equal("application/pdf", type);
    }

    [Fact]
    public void An_empty_dispatch_body_names_every_required_field_instead_of_a_500()
    {
        // #34: this is what an omitted field looks like after binding. It used to reach a NOT NULL column.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonContract.Configure(options);
        var request = JsonSerializer.Deserialize<DispatchMachineRequest>("{}", options)!;

        var errors = Refusal(() => MachineDeliveryRequestValidation.Dispatch(request, Now));

        Assert.Equal(
            new[] { "DcNumber", "Destination", "DispatchDate", "IdempotencyKey", "JobOrderId", "Nature", "Purpose" },
            errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void An_empty_signature_body_names_every_required_field_instead_of_a_500()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApiJsonContract.Configure(options);
        var request = JsonSerializer.Deserialize<SignMachineDeliveryRequest>("{}", options)!;

        var errors = Refusal(() => MachineDeliveryRequestValidation.Sign(request, Now, out _));

        Assert.Equal(new[] { "CustomerSignatory", "DeliveredAt", "Evidence", "IdempotencyKey" }, errors.Keys.Order(StringComparer.Ordinal));
    }

    public static TheoryData<string, DispatchMachineRequest> DispatchRefusals()
    {
        var r = Returnable();
        var n = NonReturnable();
        return new()
        {
            { "DcNumber", r with { DcNumber = "   " } },
            { "DcNumber", r with { DcNumber = new string('D', 101) } },
            { "Destination", r with { Destination = "" } },
            { "Destination", r with { Destination = new string('X', 501) } },
            { "Nature", r with { Nature = "LOAN" } },
            { "Nature", r with { Nature = "returnable" } },
            { "Purpose", r with { Purpose = "CUSTOMER_PO_BASED" } },
            { "Purpose", n with { Purpose = "DEMO" } },
            { "DispatchDate", r with { DispatchDate = Today.AddDays(1), ExpectedReturnDate = Today.AddDays(8) } },
            { "ExpectedReturnDate", r with { ExpectedReturnDate = null } },
            { "ExpectedReturnDate", r with { ExpectedReturnDate = Today.AddDays(-1) } },
            { "ExpectedReturnDate", n with { ExpectedReturnDate = Today.AddDays(7) } },
            { "IdempotencyKey", r with { IdempotencyKey = new string('k', 101) } },
            { "JobOrderId", r with { JobOrderId = Guid.Empty } },
        };
    }

    [Theory]
    [MemberData(nameof(DispatchRefusals))]
    public void A_dispatch_field_rule_is_a_400_naming_that_field_alone(string field, DispatchMachineRequest request)
    {
        var errors = Refusal(() => MachineDeliveryRequestValidation.Dispatch(request, Now));

        Assert.Equal(field, Assert.Single(errors).Key);
    }

    [Fact]
    public void Text_limits_count_trimmed_characters_as_the_database_does()
    {
        MachineDeliveryRequestValidation.Dispatch(Returnable() with { DcNumber = "  " + new string('D', 100) + "  " }, Now);
    }

    [Fact]
    public void Today_is_the_india_business_day_not_utc()
    {
        // 1 Oct 01:00 IST is still 30 Sep in UTC. A dispatch dated 1 Oct is today, not the future.
        var justAfterMidnightInIndia = new DateTimeOffset(2026, 9, 30, 19, 30, 0, TimeSpan.Zero);
        MachineDeliveryRequestValidation.Dispatch(NonReturnable() with { DispatchDate = new DateOnly(2026, 10, 1) }, justAfterMidnightInIndia);
    }

    public static TheoryData<string, SignMachineDeliveryRequest> SignRefusals()
    {
        var s = Signed();
        return new()
        {
            { "DeliveredAt", s with { DeliveredAt = Now.AddMinutes(1) } },
            { "CustomerSignatory", s with { CustomerSignatory = " " } },
            { "CustomerSignatory", s with { CustomerSignatory = new string('C', 201) } },
            { "Evidence", s with { Evidence = s.Evidence with { Content = [] } } },
            { "Evidence", s with { Evidence = s.Evidence with { Content = new byte[MachineDeliveryRequestValidation.MaxSignatureBytes + 1] } } },
            { "Evidence.ContentType", s with { Evidence = s.Evidence with { ContentType = "image/png" } } },
            { "Evidence.ContentType", s with { Evidence = s.Evidence with { Content = Encoding.ASCII.GetBytes("not a pdf") } } },
            { "Evidence.FileName", s with { Evidence = s.Evidence with { FileName = null! } } },
            { "Evidence.FileName", s with { Evidence = s.Evidence with { FileName = "bad\u0001name.pdf" } } },
            { "IdempotencyKey", s with { IdempotencyKey = "" } },
        };
    }

    [Theory]
    [MemberData(nameof(SignRefusals))]
    public void A_signature_field_rule_is_a_400_naming_that_field_alone(string field, SignMachineDeliveryRequest request)
    {
        var errors = Refusal(() => MachineDeliveryRequestValidation.Sign(request, Now, out _));

        Assert.Equal(field, Assert.Single(errors).Key);
    }

    [Fact]
    public void The_stored_file_name_is_the_base_name()
    {
        var request = Signed() with { Evidence = Signed().Evidence with { FileName = @"C:\scans\dc\signed.pdf" } };
        Assert.Equal("signed.pdf", MachineDeliveryRequestValidation.Sign(request, Now, out _));
    }

    [Fact]
    public void The_detail_carries_every_message_so_a_client_without_errors_still_reads_the_reason()
    {
        var failure = Assert.Throws<StoresValidationException>(() =>
            MachineDeliveryRequestValidation.Dispatch(Returnable() with { DcNumber = "", Destination = "" }, Now));

        Assert.Contains("DcNumber is required.", failure.Message);
        Assert.Contains("Destination is required.", failure.Message);
    }
}
