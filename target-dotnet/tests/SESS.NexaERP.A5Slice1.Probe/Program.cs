using SESS.NexaERP.Application.Purchase.A5;
using SESS.NexaERP.Domain.Masters;

var parameters = new A5QuotationRevisionSubmitParameters(
    Guid.Parse("10000000-0000-0000-0000-000000000001"),
    "VQ-001",
    "INR",
    "NET30",
    "DAP",
    "12M",
    false,
    null,
    A5SubmissionSource.EMAIL_RECEIVED,
    new DateTimeOffset(2026, 8, 23, 7, 8, 9, TimeSpan.FromHours(5.5)),
    "purchase/quotes/vq-001.pdf",
    new string('a', 64),
    "Vendor attestation",
    7,
    null,
    "idem-quotation-001",
    [new A5QuotationLineParameters(
        Guid.Parse("20000000-0000-0000-0000-000000000002"),
        1.50m, 2.500m, 3.000m, 4.040m, 5.0m, 6.600m, 7.7000m,
        new DateOnly(2026, 9, 30),
        "8471", "KA", "TN", VendorRegistrationType.REGULAR, 8.80000m)],
    9.9000m);

var bytes = A5PurchaseCanonicalSerializer.Serialize(
    A5PurchaseActionId.QUOTATION_REVISION_SUBMIT,
    parameters);
Console.Write(Convert.ToBase64String(bytes));
