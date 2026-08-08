using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class FoundationSeedData
{
    public static readonly Role[] Roles =
    [
        RoleSeed("10000000-0000-0000-0000-000000000001", "admin", "Administrator", true),
        RoleSeed("10000000-0000-0000-0000-000000000002", "md", "Managing Director / CFO", true),
        RoleSeed("10000000-0000-0000-0000-000000000003", "accounts_head", "Accounts Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000004", "purchase_head", "Purchase Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000005", "store_head", "Store Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000006", "production_head", "Production Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000007", "qc_head", "QC Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000008", "design_head", "Design Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000009", "service_head", "Service Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000010", "sales_head", "Sales Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000011", "service_coordinator", "Service Coordinator", false),
        RoleSeed("10000000-0000-0000-0000-000000000012", "service_engineer", "Service Engineer", false),
        RoleSeed("10000000-0000-0000-0000-000000000013", "sales_engineer", "Sales Engineer", false),
        RoleSeed("10000000-0000-0000-0000-000000000014", "it_admin", "IT Admin", true),
        RoleSeed("10000000-0000-0000-0000-000000000015", "customer", "Customer Portal User", false),
        RoleSeed("10000000-0000-0000-0000-000000000016", "vendor", "Vendor Portal User", false),
        RoleSeed("10000000-0000-0000-0000-000000000017", "document_controller", "Document Controller", false),
        RoleSeed("10000000-0000-0000-0000-000000000018", "dcc", "DCC / Document Controller", false),
        RoleSeed("10000000-0000-0000-0000-000000000019", "branch_manager", "Branch Manager", true),
        RoleSeed("10000000-0000-0000-0000-000000000020", "ops_admin_no_hr", "Operational Admin without HR", true)
    ];

    public static readonly PageDefinition[] Pages =
    [
        PageSeed("20000000-0000-0000-0000-000000000001", "identity.roles", "Identity", "Role Master", "/identity/roles"),
        PageSeed("20000000-0000-0000-0000-000000000002", "identity.users", "Identity", "User Master", "/identity/users"),
        PageSeed("20000000-0000-0000-0000-000000000003", "authorization.pages", "Admin", "Page Master", "/authorization/pages"),
        PageSeed("20000000-0000-0000-0000-000000000004", "authorization.role-pages", "Admin", "Role Page Permissions", "/authorization/role-pages"),
        PageSeed("20000000-0000-0000-0000-000000000005", "masters.customers", "Masters", "Customer Master", "/masters/customers"),
        PageSeed("20000000-0000-0000-0000-000000000006", "masters.vendors", "Masters", "Vendor Master", "/masters/vendors"),
        PageSeed("20000000-0000-0000-0000-000000000007", "inventory.items", "Inventory", "Item Master", "/inventory/items"),
        PageSeed("20000000-0000-0000-0000-000000000008", "inventory.warehouses", "Inventory", "Warehouse Master", "/inventory/warehouses"),
        PageSeed("20000000-0000-0000-0000-000000000009", "inventory.rack-bins", "Inventory", "Rack/Bin Master", "/inventory/rack-bins"),
        PageSeed("20000000-0000-0000-0000-000000000010", "purchase.requests", "Purchase", "Purchase Request", "/purchase/requests"),
        PageSeed("20000000-0000-0000-0000-000000000011", "purchase.rfq", "Purchase", "RFQ", "/purchase/rfq"),
        PageSeed("20000000-0000-0000-0000-000000000012", "purchase.po", "Purchase", "Purchase Order", "/purchase/purchase-orders"),
        PageSeed("20000000-0000-0000-0000-000000000013", "inventory.grn", "Inventory", "GRN", "/inventory/grn"),
        PageSeed("20000000-0000-0000-0000-000000000014", "inventory.stock-ledger", "Inventory", "Stock Ledger", "/inventory/stock-ledger"),
        PageSeed("20000000-0000-0000-0000-000000000015", "audit.history", "Audit", "Audit History", "/audit/history"),
        PageSeed("20000000-0000-0000-0000-000000000016", "employees.master", "Employees", "Employee Master", "/employees"),
        PageSeed("20000000-0000-0000-0000-000000000017", "employees.role-mapping", "Employees", "Employee Role Mapping", "/employees/roles"),
        PageSeed("20000000-0000-0000-0000-000000000018", "employees.audit-history", "Employees", "Employee Audit History", "/employees/audit-history")
    ];

    private static Role RoleSeed(string id, string code, string name, bool isPrivileged)
    {
        return new Role
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            IsPrivileged = isPrivileged,
            IsActive = true,
            CreatedAt = DateTimeOffset.UnixEpoch,
            CreatedBy = "migration"
        };
    }

    private static PageDefinition PageSeed(string id, string pageKey, string module, string title, string route)
    {
        return new PageDefinition
        {
            Id = Guid.Parse(id),
            PageKey = pageKey,
            Module = module,
            Title = title,
            Route = route,
            IsActive = true,
            CreatedAt = DateTimeOffset.UnixEpoch,
            CreatedBy = "migration"
        };
    }
}


