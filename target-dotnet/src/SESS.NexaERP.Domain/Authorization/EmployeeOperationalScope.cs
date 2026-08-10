using SESS.NexaERP.Domain.Common;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;

namespace SESS.NexaERP.Domain.Authorization;

public sealed class EmployeeOperationalScope : AuditableEntity
{
    public string OrganizationId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Guid? RackBinId { get; set; }
    public RackBin? RackBin { get; set; }
    public bool AllowsPrivilegedCrossScope { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string Remarks { get; set; } = string.Empty;

    public bool Matches(Guid? departmentId, Guid? warehouseId, Guid? rackBinId, DateOnly onDate)
    {
        if (!IsActive || EffectiveFrom > onDate || (EffectiveTo.HasValue && EffectiveTo.Value < onDate)) return false;
        if (DepartmentId.HasValue && DepartmentId != departmentId) return false;
        if (WarehouseId.HasValue && WarehouseId != warehouseId) return false;
        if (RackBinId.HasValue && RackBinId != rackBinId) return false;
        return true;
    }
}

public static class Rev869ARoleCodes
{
    public const string PurchaseManager = "PURCHASE_MANAGER";
    public const string PurchaseExecutive = "PURCHASE_EXECUTIVE";
    public const string StoresManager = "STORES_MANAGER";
    public const string StoresExecutive = "STORES_EXECUTIVE";
    public const string QcManager = "QC_MANAGER";
    public const string QcInspector = "QC_INSPECTOR";
    public const string DepartmentManager = "DEPARTMENT_MANAGER";
    public const string TechnicalDirector = "TECHNICAL_DIRECTOR";
    public const string ManagingDirector = "MANAGING_DIRECTOR";

    public static readonly string[] All =
    [
        PurchaseManager, PurchaseExecutive, StoresManager, StoresExecutive, QcManager,
        QcInspector, DepartmentManager, TechnicalDirector, ManagingDirector
    ];

    public static bool IsExplicitCrossScopeRole(string? roleCode) => Normalize(roleCode) is TechnicalDirector or ManagingDirector;
    public static string Normalize(string? roleCode) => roleCode?.Trim().ToUpperInvariant() ?? string.Empty;
}
