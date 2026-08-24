using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Masters;

public sealed class UomConversion : AuditableEntity
{
    // Legacy API compatibility only; UOM conversions are shared foundation data.
    public string OrganizationId { get; set; } = string.Empty;
    public Guid FromUomId { get; set; }
    public Uom? FromUom { get; set; }
    public Guid ToUomId { get; set; }
    public Uom? ToUom { get; set; }
    public string MeasurementDimension { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public int QuantityPrecision { get; set; } = 6;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? FirstUsedAt { get; set; }

    public bool IsUsed => FirstUsedAt.HasValue;

    public static bool IsValid(decimal factor, int precision, Guid fromUomId, Guid toUomId, string? dimension) =>
        factor > 0 && precision == 6 && fromUomId != toUomId && !string.IsNullOrWhiteSpace(dimension);

    public static bool CanEdit(UomConversion conversion) => !conversion.IsUsed;
}
