using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Tests;

public sealed class ActualBomProvenanceTests
{
    [Theory]
    [InlineData("FITMENT")]
    [InlineData("REVERSAL")]
    public void ReceiptProvenanceUsesResolvedAcceptanceWithoutPaymentStatus(string kind)
    {
        var entry = new ActualBomEntry { EntryKind = kind, GrnNumberSnapshot = "GRN-12" };
        Assert.Equal("GRN GRN-12 - bill not yet accepted",
            EfFitmentActualBomService.EntryProvenance(entry, null, null, TimeZoneInfo.Utc));
        Assert.Equal("Bill 6S/INV/0916/001 - accepted, matched",
            EfFitmentActualBomService.EntryProvenance(entry, Guid.NewGuid(), "6S/INV/0916/001", TimeZoneInfo.Utc));
    }

    [Fact]
    public void OpeningDeclarationNeverBecomesVerifiedBillProvenance()
    {
        var entry = new ActualBomEntry
        {
            OpeningStockLineId = Guid.NewGuid(),
            OpeningStockLine = new OpeningStockLine { VendorBillNumber = "LEGACY-1" }
        };
        Assert.Equal("Bill LEGACY-1 - declared at opening stock, not verified in this system",
            EfFitmentActualBomService.EntryProvenance(entry, Guid.NewGuid(), "ACCEPTED-OTHER", TimeZoneInfo.Utc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void OpeningWithoutBillUsesAuthorizationEmployeeAndCalendarDate(string? declaredBill)
    {
        var entry = new ActualBomEntry
        {
            OpeningStockLineId = Guid.NewGuid(),
            OpeningStockLine = new OpeningStockLine
            {
                VendorBillNumber = declaredBill,
                OpeningStock = new OpeningStock
                {
                    AuthorizedAt = new DateTimeOffset(2026, 9, 19, 23, 30, 0, TimeSpan.Zero),
                    AuthorizedByEmployee = new Employee { EmployeeCode = "SESS-12" }
                }
            }
        };
        Assert.Equal("Opening stock, authorised 20 Sep 2026 by SESS-12",
            EfFitmentActualBomService.EntryProvenance(entry, null, null,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")));
    }
}
