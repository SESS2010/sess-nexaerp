using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Domain.Inventory;

public sealed class QcInspectionPolicy : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public Guid? ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }
    public string ParameterCode { get; set; } = string.Empty;
    public Guid MeasurementUomId { get; set; }
    public Uom? MeasurementUom { get; set; }
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public string InspectionMethod { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string ApprovalStatus { get; set; } = MasterApprovalStatuses.Draft;
    public bool IsActive { get; set; } = true;

    public static string ResolveMissingPolicyCondition(QcInspectionPolicy? policy, DateOnly onDate) =>
        policy is not null && policy.IsActive && policy.ApprovalStatus == MasterApprovalStatuses.Approved && policy.EffectiveFrom <= onDate && (!policy.EffectiveTo.HasValue || policy.EffectiveTo.Value >= onDate)
            ? InventoryConditionCodes.Available
            : InventoryConditionCodes.QcHold;
}
