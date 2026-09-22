namespace SESS.NexaERP.Domain.Purchase;

public sealed record VendorRatingDimensionScores(decimal Quality, decimal Delivery,
    decimal Warranty, decimal Commercial, decimal Documents, decimal Technical,
    decimal Response, decimal Overall)
{
    public decimal Total => Quality + Delivery + Warranty + Commercial + Documents + Technical + Response + Overall;
    public void Validate()
    {
        var values = new[] { Quality, Delivery, Warranty, Commercial, Documents, Technical, Response, Overall };
        var maxima = new[] { 25m, 20m, 10m, 10m, 10m, 15m, 5m, 5m };
        if (values.Where((value, index) => value < 0 || value > maxima[index]).Any())
            throw new ArgumentException("A retained dimension score is outside its approved range.");
    }
}

/// <summary>Typed historical marks, never evidence of automatic measurement.</summary>
public sealed class LegacyVendorRating
{
    private LegacyVendorRating(string sourceSha256, string sheet, int row, Guid companyId,
        Guid vendorId, string billNumber, DateOnly billDate, VendorRatingDimensionScores scores,
        decimal? originalTotal)
    {
        SourceSha256 = sourceSha256; Sheet = sheet; Row = row; CompanyId = companyId;
        VendorId = vendorId; BillNumber = billNumber; BillDate = billDate; Scores = scores;
        OriginalTotal = originalTotal;
    }
    public string Provenance => "LEGACY";
    public bool IsMeasured => false;
    public string SourceSha256 { get; }
    public string Sheet { get; }
    public int Row { get; }
    public Guid CompanyId { get; }
    public Guid VendorId { get; }
    public string BillNumber { get; }
    public DateOnly BillDate { get; }
    public VendorRatingDimensionScores Scores { get; }
    public decimal? OriginalTotal { get; }
    // Preserve discrepancies for reconciliation rather than rewriting the source total.
    public bool HasTotalDiscrepancy => OriginalTotal is not null && OriginalTotal != Scores.Total;

    public static LegacyVendorRating Capture(string sourceSha256, string sheet, int row,
        Guid companyId, Guid vendorId, string billNumber, DateOnly billDate,
        VendorRatingDimensionScores scores, decimal? originalTotal)
    {
        if (string.IsNullOrWhiteSpace(sourceSha256) || sourceSha256.Length != 64
            || !sourceSha256.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(sheet) || row < 1)
            throw new ArgumentException("The original workbook hash, sheet and row are required.");
        if (companyId == Guid.Empty || vendorId == Guid.Empty || string.IsNullOrWhiteSpace(billNumber)
            || billDate == default)
            throw new ArgumentException("The source bill must retain its company, mapped vendor, number and date.");
        ArgumentNullException.ThrowIfNull(scores); scores.Validate();
        if (originalTotal is < 0 or > 100)
            throw new ArgumentException("The original total must be within zero and 100.");
        return new(sourceSha256.ToLowerInvariant(), sheet, row, companyId, vendorId,
            billNumber, billDate, scores, originalTotal);
    }
}
