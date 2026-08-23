using System.Globalization;
using System.Security.Cryptography;
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

var cultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("de-DE"),
    new CultureInfo("fr-FR"),
    new CultureInfo("ar-SA"),
    new CultureInfo("tr-TR"),
    new CultureInfo("hi-IN"),
    CultureInfo.InvariantCulture
};
var originalCulture = CultureInfo.CurrentCulture;
var originalUiCulture = CultureInfo.CurrentUICulture;
var outputs = new List<byte[]>();

try
{
    foreach (var culture in cultures)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        var bytes = A5PurchaseCanonicalSerializer.Serialize(
            A5PurchaseActionId.QUOTATION_REVISION_SUBMIT,
            parameters);
        outputs.Add(bytes);
        var name = culture.Equals(CultureInfo.InvariantCulture) ? "InvariantCulture" : culture.Name;
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Console.WriteLine($"{name} length={bytes.Length} sha256={hash}");
    }
}
finally
{
    CultureInfo.CurrentCulture = originalCulture;
    CultureInfo.CurrentUICulture = originalUiCulture;
}

var identical = outputs.Skip(1).All(bytes => bytes.SequenceEqual(outputs[0]));
Console.WriteLine($"ALL_CULTURES_BYTE_IDENTICAL={identical.ToString().ToLowerInvariant()}");
if (!identical) Environment.ExitCode = 1;
