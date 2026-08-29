using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;

namespace SESS.NexaERP.Domain.Masters;

public static class TaxJurisdictions
{
    public const string IndiaGst = "IN_GST";
}

public sealed class TaxGstSetting : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public string JurisdictionCode { get; set; } = TaxJurisdictions.IndiaGst;
    public string HsnSacCode { get; set; } = string.Empty;
    public string SupplyType { get; set; } = string.Empty;
    public string SupplierStateCode { get; set; } = string.Empty;
    public string PlaceOfSupplyStateCode { get; set; } = string.Empty;
    public string VendorRegistrationType { get; set; } = string.Empty;
    public decimal GstRate { get; set; }
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal IgstRate { get; set; }
    public decimal CessRate { get; set; }
    public bool IsExempt { get; set; }
    public bool IsReverseCharge { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public int RoundingScale { get; set; } = 2;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public Guid CreatorEmployeeId { get; set; }
    public Employee? CreatorEmployee { get; set; }
    public Guid? DecisionEmployeeId { get; set; }
    public Employee? DecisionEmployee { get; set; }
    public string? DecisionRoleCode { get; set; }
    public DateTimeOffset? DecisionAt { get; set; }
    public string? DecisionRemarks { get; set; }
    public Guid? SupersedesTaxGstSettingId { get; set; }
    public TaxGstSetting? SupersedesTaxGstSetting { get; set; }
    public bool IsActive { get; set; } = true;

    public static bool IsValidRange(DateOnly from, DateOnly? to) => !to.HasValue || to.Value >= from;
    public static bool IsValidRate(decimal rate) => rate is >= 0 and <= 100;
    public static string ResolveSupplyType(string supplierStateCode, string placeOfSupplyStateCode)
    {
        if (string.IsNullOrWhiteSpace(supplierStateCode) || string.IsNullOrWhiteSpace(placeOfSupplyStateCode)) throw new InvalidOperationException("Supplier and place-of-supply state codes are required.");
        return string.Equals(supplierStateCode.Trim(), placeOfSupplyStateCode.Trim(), StringComparison.OrdinalIgnoreCase) ? "INTRASTATE" : "INTERSTATE";
    }

    public bool HasValidIndiaComponentSplit()
    {
        if (!string.Equals(JurisdictionCode, TaxJurisdictions.IndiaGst, StringComparison.OrdinalIgnoreCase)) return true;
        return SupplyType == "INTRASTATE"
            ? IgstRate == 0 && CgstRate + SgstRate == GstRate
            : SupplyType == "INTERSTATE" && CgstRate == 0 && SgstRate == 0 && IgstRate == GstRate;
    }
}

public sealed record CommercialValueSnapshot(
    string CurrencyCode,
    decimal TaxableValue,
    decimal TaxValue,
    decimal FreightAndOtherCharges,
    decimal DiscountValue,
    decimal TotalPayableValue,
    int RoundingScale)
{
    public static CommercialValueSnapshot Calculate(string currencyCode, decimal taxableValue, decimal taxValue, decimal charges, decimal discount, int roundingScale = 2)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || taxableValue < 0 || taxValue < 0 || charges < 0 || discount < 0 || roundingScale is < 0 or > 6)
            throw new InvalidOperationException("Commercial values, ISO currency code and rounding scale are invalid.");
        var total = decimal.Round(taxableValue + taxValue + charges - discount, roundingScale, MidpointRounding.AwayFromZero);
        if (total < 0) throw new InvalidOperationException("Total payable value cannot be negative.");
        return new(currencyCode.Trim().ToUpperInvariant(), taxableValue, taxValue, charges, discount, total, roundingScale);
    }
}
