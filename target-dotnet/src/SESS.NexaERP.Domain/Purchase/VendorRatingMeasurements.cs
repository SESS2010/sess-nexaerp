namespace SESS.NexaERP.Domain.Purchase;

public sealed record VendorRatingSource(Guid RecordId, long Revision)
{
    public void Validate()
    {
        if (RecordId == Guid.Empty || Revision < 0)
            throw new ArgumentException("A measured rating requires its retained source identity and effective revision.");
    }
}

public sealed record VendorQualityMeasurement(VendorRatingSource ReceiptLine,
    VendorRatingSource QcDisposition, decimal ReceivedQuantity, decimal AcceptedWithoutConcession,
    decimal ConcessionAcceptedQuantity, decimal Points);

public sealed record VendorDeliveryMeasurement(VendorRatingSource Receipt,
    VendorRatingSource AgreedPoDelivery, DateOnly CommittedDate, DateOnly ReceivedDate,
    int DaysLate, decimal Points);

/// <summary>
/// Fixed owner-approved dimensions. Services must resolve these sources from the same
/// retained receipt and PO/QC chain. This calculator does not attest to caller-supplied IDs.
/// Quality remains per line: adding unlike units across receipt lines is not supported.
/// </summary>
public static class VendorRatingMeasurements
{
    public const decimal QualityMaximumPoints = 25m;
    public const string RuleVersion = "SESS-EIGHT-DIMENSIONS-20260919-V1";

    public static VendorQualityMeasurement Quality(VendorRatingSource receiptLine,
        VendorRatingSource qcDisposition, decimal receivedQuantity,
        decimal acceptedWithoutConcession, decimal concessionAcceptedQuantity)
    {
        ArgumentNullException.ThrowIfNull(receiptLine);
        ArgumentNullException.ThrowIfNull(qcDisposition);
        receiptLine.Validate(); qcDisposition.Validate();
        if (receivedQuantity <= 0 || acceptedWithoutConcession < 0 || concessionAcceptedQuantity < 0
            || acceptedWithoutConcession > receivedQuantity
            || concessionAcceptedQuantity > receivedQuantity - acceptedWithoutConcession)
            throw new ArgumentException("Accepted quantities must be non-overlapping and between zero and the positive receipt quantity.");
        var points = QualityMaximumPoints * ((acceptedWithoutConcession + concessionAcceptedQuantity) / receivedQuantity);
        return new(receiptLine, qcDisposition, receivedQuantity, acceptedWithoutConcession,
            concessionAcceptedQuantity, points);
    }

    public static VendorDeliveryMeasurement Delivery(VendorRatingSource receipt,
        VendorRatingSource agreedPoDelivery, DateOnly committedDate, DateOnly receivedDate)
    {
        ArgumentNullException.ThrowIfNull(receipt); ArgumentNullException.ThrowIfNull(agreedPoDelivery);
        receipt.Validate(); agreedPoDelivery.Validate();
        if (committedDate == default || receivedDate == default)
            throw new ArgumentException("Both the agreed delivery date and actual receipt date are required.");
        var days = Math.Max(0, receivedDate.DayNumber - committedDate.DayNumber);
        var points = days switch { 0 => 20m, <= 3 => 18m, <= 7 => 15m, <= 15 => 10m, _ => 0m };
        return new(receipt, agreedPoDelivery, committedDate, receivedDate, days, points);
    }
}
