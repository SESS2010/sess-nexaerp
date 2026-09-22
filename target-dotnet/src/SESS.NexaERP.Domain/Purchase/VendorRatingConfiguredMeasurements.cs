namespace SESS.NexaERP.Domain.Purchase;

public sealed record VendorWarrantyScoreBand(int MinimumMonths, decimal Points);
public sealed record VendorRequiredAttachment(string Kind, VendorRatingSource Source, string Sha256, long Bytes);
public sealed record VendorWarrantyMeasurement(VendorRatingSource Rule, VendorRatingSource WarrantySource,
    int Months, decimal Points);
public sealed record VendorCommercialMeasurement(VendorRatingSource Rule, VendorRatingSource PurchaseOrder,
    string RetainedPaymentTerms, decimal Points);
public sealed record VendorDocumentMeasurement(VendorRatingSource Rule, VendorRatingSource Receipt,
    IReadOnlyList<string> RequiredKinds, IReadOnlyList<VendorRequiredAttachment> Attachments,
    IReadOnlyList<string> MissingKinds, decimal Points);

/// <summary>
/// Explicit governed rules, with no default months, credit-day inference or document marks.
/// The application must load the approved definition and retained source facts; constructing
/// this value object does not establish approval or permit clients to submit automatic scores.
/// </summary>
public sealed class VendorRatingConfiguredMeasurements
{
    private readonly VendorWarrantyScoreBand[] warrantyBands;
    private readonly Dictionary<string, decimal> paymentTerms;
    private readonly string[] requiredDocuments;
    private readonly Dictionary<int, decimal> documentScores;

    public VendorRatingConfiguredMeasurements(VendorRatingSource approvedRule,
        IEnumerable<VendorWarrantyScoreBand> warrantyBands,
        IReadOnlyDictionary<string, decimal> paymentTerms,
        IEnumerable<string> requiredDocuments, IReadOnlyDictionary<int, decimal> documentScoresByMissingCount)
    {
        ArgumentNullException.ThrowIfNull(approvedRule); approvedRule.Validate();
        ArgumentNullException.ThrowIfNull(warrantyBands); ArgumentNullException.ThrowIfNull(paymentTerms);
        ArgumentNullException.ThrowIfNull(requiredDocuments); ArgumentNullException.ThrowIfNull(documentScoresByMissingCount);
        Rule = approvedRule;
        this.warrantyBands = warrantyBands.OrderBy(x => x.MinimumMonths).ToArray();
        if (this.warrantyBands.Length == 0 || this.warrantyBands[0].MinimumMonths != 0
            || this.warrantyBands.Any(x => x.MinimumMonths < 0 || x.Points is < 0 or > 10)
            || this.warrantyBands.Select(x => x.MinimumMonths).Distinct().Count() != this.warrantyBands.Length)
            throw new ArgumentException("An explicit warranty scale must cover zero months and have unique non-negative thresholds and marks within ten.");
        this.paymentTerms = new(paymentTerms, StringComparer.Ordinal);
        if (this.paymentTerms.Count == 0 || this.paymentTerms.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Value is < 0 or > 10))
            throw new ArgumentException("An explicit mapping of retained PO payment terms to marks within ten is required.");
        this.requiredDocuments = requiredDocuments.ToArray();
        if (this.requiredDocuments.Length == 0 || this.requiredDocuments.Any(string.IsNullOrWhiteSpace)
            || this.requiredDocuments.Distinct(StringComparer.Ordinal).Count() != this.requiredDocuments.Length)
            throw new ArgumentException("The applicable policy must identify its distinct required document kinds.");
        documentScores = new(documentScoresByMissingCount);
        if (documentScores.Count != this.requiredDocuments.Length + 1
            || Enumerable.Range(0, this.requiredDocuments.Length + 1).Any(x => !documentScores.ContainsKey(x))
            || documentScores.Values.Any(x => x is < 0 or > 10))
            throw new ArgumentException("An explicit document score is required for every possible missing-document count.");
    }

    public VendorRatingSource Rule { get; }

    public VendorWarrantyMeasurement Warranty(VendorRatingSource retainedWarrantySource, int? supplierMonths)
    {
        ArgumentNullException.ThrowIfNull(retainedWarrantySource); retainedWarrantySource.Validate();
        if (supplierMonths is null or < 0)
            throw new ArgumentException("Actual retained supplier warranty months are required; generated expiry is not a substitute.");
        var points = warrantyBands.Last(x => x.MinimumMonths <= supplierMonths).Points;
        return new(Rule, retainedWarrantySource, supplierMonths.Value, points);
    }

    public VendorCommercialMeasurement Commercial(VendorRatingSource purchaseOrder, string retainedPaymentTerms)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder); purchaseOrder.Validate();
        if (string.IsNullOrWhiteSpace(retainedPaymentTerms) || !paymentTerms.TryGetValue(retainedPaymentTerms, out var points))
            throw new ArgumentException("The retained PO terms have no approved score mapping; no commercial score can be inferred.");
        return new(Rule, purchaseOrder, retainedPaymentTerms, points);
    }

    public VendorDocumentMeasurement Documents(VendorRatingSource receipt, IEnumerable<VendorRequiredAttachment> retainedAttachments)
    {
        ArgumentNullException.ThrowIfNull(receipt); receipt.Validate();
        ArgumentNullException.ThrowIfNull(retainedAttachments);
        var attachments = retainedAttachments.ToArray();
        foreach (var attachment in attachments)
        {
            ArgumentNullException.ThrowIfNull(attachment.Source); attachment.Source.Validate();
            if (string.IsNullOrWhiteSpace(attachment.Kind) || attachment.Bytes <= 0
                || attachment.Sha256 is not { Length: 64 } || !attachment.Sha256.All(Uri.IsHexDigit))
                throw new ArgumentException("Document evidence requires an actual retained file identity, hash and positive length.");
        }
        if (attachments.Select(x => (x.Kind, x.Source.RecordId, x.Source.Revision)).Distinct().Count() != attachments.Length)
            throw new ArgumentException("An attachment evidence row cannot be counted twice.");
        var present = attachments.Select(x => x.Kind).ToHashSet(StringComparer.Ordinal);
        var missing = requiredDocuments.Where(x => !present.Contains(x)).ToArray();
        return new(Rule, receipt, Array.AsReadOnly(requiredDocuments), Array.AsReadOnly(attachments),
            Array.AsReadOnly(missing), documentScores[missing.Length]);
    }
}
