using SESS.NexaERP.Domain.Common;

namespace SESS.NexaERP.Domain.Inventory;

public static class InventoryConditionCodes
{
    public const string Available = "AVAILABLE";
    public const string QcHold = "QC_HOLD";
    public const string Rejected = "REJECTED";
    public const string Quarantine = "QUARANTINE";
    public const string ReturnToVendor = "RETURN_TO_VENDOR";
    public const string Scrap = "SCRAP";

    public static readonly string[] All = [Available, QcHold, Rejected, Quarantine, ReturnToVendor, Scrap];
    public static bool CanReserveOrIssue(string? conditionCode) => string.Equals(conditionCode, Available, StringComparison.OrdinalIgnoreCase);
}

public sealed class WarehouseConditionLocation : CompanyScopedAuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public string ConditionCode { get; set; } = InventoryConditionCodes.Available;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsEffective(DateOnly onDate) => IsActive && EffectiveFrom <= onDate && (!EffectiveTo.HasValue || EffectiveTo.Value >= onDate);

    public static bool IsValid(WarehouseConditionLocation mapping, RackBin rackBin) =>
        mapping.WarehouseId == rackBin.WarehouseId &&
        string.Equals(mapping.ConditionCode, rackBin.MaterialCondition, StringComparison.OrdinalIgnoreCase) &&
        InventoryConditionCodes.All.Contains(mapping.ConditionCode, StringComparer.OrdinalIgnoreCase);
}

public static class StoreLocationKey
{
    public static string Derive(Guid warehouseId, Guid? rackBinId) =>
        rackBinId.HasValue ? $"W:{warehouseId:N}:B:{rackBinId.Value:N}" : $"W:{warehouseId:N}:B:NONE";
}
