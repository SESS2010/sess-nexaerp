using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class FoundationSeedData
{
    public static readonly Role[] Roles =
    [
        RoleSeed("10000000-0000-0000-0000-000000000001", "ADMIN", "Administrator", true),
        RoleSeed("10000000-0000-0000-0000-000000000002", "MD", "Managing Director / CFO", true),
        RoleSeed("10000000-0000-0000-0000-000000000003", "ACCOUNTS_HEAD", "Accounts Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000004", "PURCHASE_HEAD", "Purchase Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000005", "STORE_HEAD", "Store Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000006", "PRODUCTION_HEAD", "Production Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000007", "QC_HEAD", "QC Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000008", "DESIGN_HEAD", "Design Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000009", "SERVICE_HEAD", "Service Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000010", "SALES_HEAD", "Sales Head", true),
        RoleSeed("10000000-0000-0000-0000-000000000011", "SERVICE_COORDINATOR", "Service Coordinator", false),
        RoleSeed("10000000-0000-0000-0000-000000000012", "SERVICE_ENGINEER", "Service Engineer", false),
        RoleSeed("10000000-0000-0000-0000-000000000013", "SALES_ENGINEER", "Sales Engineer", false),
        RoleSeed("10000000-0000-0000-0000-000000000014", "IT_MANAGER", "IT Manager", true),
        RoleSeed("10000000-0000-0000-0000-000000000015", "CUSTOMER", "Customer Portal User", false),
        RoleSeed("10000000-0000-0000-0000-000000000016", "VENDOR", "Vendor Portal User", false),
        RoleSeed("10000000-0000-0000-0000-000000000017", "DOCUMENT_CONTROLLER", "Document Controller", false),
        RoleSeed("10000000-0000-0000-0000-000000000018", "DCC", "DCC / Document Controller", false),
        RoleSeed("10000000-0000-0000-0000-000000000019", "BRANCH_MANAGER", "Branch Manager", true),
        RoleSeed("10000000-0000-0000-0000-000000000020", "OPS_ADMIN_NO_HR", "Operational Admin without HR", true)
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
        PageSeed("20000000-0000-0000-0000-000000000018", "employees.audit-history", "Employees", "Employee Audit History", "/employees/audit-history"),
        PageSeed("20000000-0000-0000-0000-000000000019", "masters.items", "Masters", "Item Master", "/masters/items"),
        PageSeed("20000000-0000-0000-0000-000000000020", "masters.warehouses", "Masters", "Warehouse/Store Master", "/masters/warehouses"),
        PageSeed("20000000-0000-0000-0000-000000000021", "masters.rack-bins", "Masters", "Rack/Bin Location Master", "/masters/rack-bins"),
        PageSeed("20000000-0000-0000-0000-000000000022", "purchase.requisitions", "Purchase", "Purchase Requisitions", "/purchase/requisitions"),
        PageSeed("20000000-0000-0000-0000-000000000023", "purchase.requisition-approvals", "Purchase", "PR Approvals", "/purchase/requisition-approvals"),
        PageSeed("20000000-0000-0000-0000-000000000024", "stores.stock-check", "Stores", "Stock Availability Check", "/stores/stock-check"),
        PageSeed("20000000-0000-0000-0000-000000000025", "stores.reservations", "Stores", "Stock Reservations", "/stores/reservations"),
        PageSeed("20000000-0000-0000-0000-000000000026", "purchase.requirement-handoff", "Purchase", "Purchase Requirement Handoff", "/purchase/requirement-handoff")
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
