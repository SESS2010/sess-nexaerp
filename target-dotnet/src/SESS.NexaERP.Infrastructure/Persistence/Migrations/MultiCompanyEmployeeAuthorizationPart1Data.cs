namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class MultiCompanyEmployeeAuthorizationPart1Data
{
    internal sealed record EmployeeRole(string EmployeeCode, string RoleCode);

    internal static readonly EmployeeRole[] EmployeeRoles =
    [
        R("SESS-01", "TECHNICAL_DIRECTOR"),
        R("SESS-02", "MANAGING_DIRECTOR"),
        R("SESS-03", "ADMIN_EXECUTIVE"),
        R("SESS-04", "TECHNICAL_SUPPORT_MANAGER"),
        R("SESS-05", "TECHNICAL_ENGINEER"),
        R("SESS-06", "JUNIOR_ENGINEER"),
        R("SESS-07", "PRODUCTION_OPERATOR"),
        R("SESS-08", "PRODUCTION_OPERATOR"),
        R("SESS-09", "JUNIOR_ENGINEER"),
        R("SESS-10", "ELECTRICAL_ENGINEER"),
        R("SESS-11", "JUNIOR_ENGINEER"),
        R("SESS-12", "SOFTWARE_DEVELOPER"),
        R("SESS-13", "PRODUCTION_OPERATOR"),
        R("SESS-14", "ACCOUNTS_MANAGER"),
        R("SESS-15", "PURCHASE_EXECUTIVE"),
        R("SESS-15", "STORES_EXECUTIVE"),
        R("SESS-15", "PURCHASE_MANAGER"),
        R("SESS-16", "STORES_ASSISTANT"),
        R("SESS-17", "DESIGN_ENGINEER"),
        R("SESS-18", "JUNIOR_ENGINEER"),
        R("SESS-19", "DESIGN_ENGINEER"),
        R("SESS-20", "ELECTRICAL_ENGINEER"),
        R("SESS-21", "HR_EXECUTIVE"),
        R("SESS-22", "PRODUCTION_OPERATOR"),
        R("SESS-23", "PRODUCTION_OPERATOR"),
        R("SESS-24", "PRODUCTION_OPERATOR"),
        R("SESS-25", "PRODUCTION_MANAGER"),
        R("SESS-26", "TECHNICAL_ENGINEER"),
        R("SESS-27", "ELECTRICAL_ENGINEER"),
        R("SESS-28", "ACCOUNTS_ASSISTANT"),
        R("SESS-29", "JUNIOR_ENGINEER"),
        R("SESS-30", "PLC_ENGINEER"),
        R("SESS-31", "TECHNICAL_ENGINEER"),
        R("SESS-32", "SOFTWARE_DEVELOPER"),
        R("SESS-33", "QC_MANAGER"),
        R("SESS-34", "TECHNICAL_ENGINEER"),
        R("SESS-35", "STORES_EXECUTIVE"),
        R("SESS-36", "ELECTRICAL_ENGINEER"),
        R("SESS-37", "ELECTRICAL_ENGINEER"),
        R("SESS-38", "TECHNICAL_ENGINEER"),
        R("SESS-39", "PRODUCTION_OPERATOR"),
        R("SESS-40", "SOFTWARE_DEVELOPER"),
        R("SESS-41", "ACCOUNTS_ASSISTANT"),
        R("SESS-42", "TECHNICAL_ENGINEER")
    ];

    internal static string EmployeeRoleSql => string.Join(",\n", EmployeeRoles.Select(x => $"('{x.EmployeeCode}','{x.RoleCode}')"));

    private static EmployeeRole R(string employeeCode, string roleCode) => new(employeeCode, roleCode);
}
