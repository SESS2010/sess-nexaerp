using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class RoleGovernanceSeedData
{
    private static readonly DateOnly EffectiveFrom = new(2026, 9, 4);
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    public static readonly Role[] AdditionalRoles =
    [
        NewRole("99000000-0000-0000-0000-000000000001", "PROJECT_MANAGER", "Project Manager", true),
        NewRole("99000000-0000-0000-0000-000000000002", "SITE_ENGINEER", "Site Engineer", false),
        NewRole("99000000-0000-0000-0000-000000000003", "DISPATCH_COORDINATOR", "Dispatch Coordinator", false),
        NewRole("99000000-0000-0000-0000-000000000004", "MAINTENANCE_ENGINEER", "Maintenance Engineer", false),
        NewRole("99000000-0000-0000-0000-000000000005", "HR_MANAGER", "HR Manager", true),
        NewRole("99000000-0000-0000-0000-000000000006", "HOUSEKEEPING_ASSISTANT", "Housekeeping Assistant", false)
    ];
    public static IReadOnlyList<CompanyRoleActivation> CompanyRoleActivations { get; private set; } = [];
    public static IReadOnlyList<Role> KnownRoles { get; private set; } = [];

    public static void ApplyToKnownRoles()
    {
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles)
            .Concat(Rev869ASeedData.Roles).Append(AdvanceSeedData.DepartmentManagerRole)
            .Concat(MultiCompanyEmployeeAuthorizationPart1SeedData.Roles).Concat(AdditionalRoles).ToArray();
        KnownRoles = roles;
        var byCode = roles.ToDictionary(role => role.Code, StringComparer.Ordinal);
        foreach (var role in roles)
        {
            var definition = Definition(role.Code);
            role.Audience = definition.Audience;
            role.BusinessArea = definition.BusinessArea;
            role.IsEmployeeAssignable = definition.IsEmployeeAssignable;
            role.ReplacementRoleId = definition.ReplacementRoleCode is null ? null : byCode[definition.ReplacementRoleCode].Id;
        }

        CompanyRoleActivations = MultiCompanyFoundationSeedData.Companies
            .SelectMany(company => roles.Select(role => Activation(company.Id, company.Code, role)))
            .ToArray();
    }

    private static GovernanceDefinition Definition(string code) => code switch
    {
        "ADMIN" => System("SECURITY"),
        "CUSTOMER" or "VENDOR" => External(),
        "MD" => Legacy("MANAGEMENT", "MANAGING_DIRECTOR"),
        "ACCOUNTS_HEAD" => Legacy("ACCOUNTS", "ACCOUNTS_MANAGER"),
        "PURCHASE_HEAD" => Legacy("PURCHASE", "PURCHASE_MANAGER"),
        "STORE_HEAD" => Legacy("STORES", "STORES_MANAGER"),
        "PRODUCTION_HEAD" => Legacy("PRODUCTION", "PRODUCTION_MANAGER"),
        "QC_HEAD" => Legacy("QUALITY", "QC_MANAGER"),
        "DCC" => Legacy("DOCUMENT_CONTROL", "DOCUMENT_CONTROLLER"),
        "SOFTWARE_ENGINEER" => Legacy("IT", "SOFTWARE_DEVELOPER"),
        "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" => Internal("MANAGEMENT"),
        "ACCOUNTS_MANAGER" or "ACCOUNTS_ASSISTANT" => Internal("ACCOUNTS"),
        "PURCHASE_MANAGER" or "PURCHASE_EXECUTIVE" => Internal("PURCHASE"),
        "STORES_MANAGER" or "STORES_EXECUTIVE" or "STORES_ASSISTANT" => Internal("STORES"),
        "PRODUCTION_MANAGER" or "PRODUCTION_COORDINATOR" or "PRODUCTION_OPERATOR" => Internal("PRODUCTION"),
        "QC_MANAGER" or "QC_INSPECTOR" => Internal("QUALITY"),
        "DESIGN_HEAD" or "DESIGN_ENGINEER" => Internal("DESIGN"),
        "SERVICE_HEAD" or "SERVICE_COORDINATOR" or "SERVICE_ENGINEER" or "TECHNICAL_SUPPORT_MANAGER" => Internal("SERVICE"),
        "SALES_HEAD" or "SALES_ENGINEER" => Internal("SALES"),
        "IT_MANAGER" or "SOFTWARE_DEVELOPER" => Internal("IT"),
        "DOCUMENT_CONTROLLER" => Internal("DOCUMENT_CONTROL"),
        "ADMIN_EXECUTIVE" or "HR_EXECUTIVE" or "HR_MANAGER" or "HOUSEKEEPING_ASSISTANT" => Internal("ADMINISTRATION"),
        "BRANCH_MANAGER" or "OPS_ADMIN_NO_HR" or "DEPARTMENT_MANAGER" => Internal("GENERAL"),
        "TECHNICAL_ENGINEER" or "ELECTRICAL_ENGINEER" or "PLC_ENGINEER" or "JUNIOR_ENGINEER" => Internal("ENGINEERING"),
        "PROJECT_MANAGER" or "SITE_ENGINEER" => Internal("PROJECTS"),
        "DISPATCH_COORDINATOR" => Internal("LOGISTICS"),
        "MAINTENANCE_ENGINEER" => Internal("MAINTENANCE"),
        _ => Internal("GENERAL")
    };

    private static CompanyRoleActivation Activation(Guid companyId, string companyCode, Role role)
        => new()
        {
            Id = StableId("company-role-activation", companyCode, role.Code),
            CompanyId = companyId,
            RoleId = role.Id,
            IsEnabled = role.Audience is not (RoleAudiences.LegacyAlias or RoleAudiences.SystemSecurity),
            EffectiveFrom = EffectiveFrom,
            Remarks = role.Audience == RoleAudiences.LegacyAlias
                ? "Legacy alias retained for history; use the replacement role."
                : role.Audience == RoleAudiences.SystemSecurity
                    ? "System-security role is not available for employee assignment."
                    : "Initial company role catalogue.",
            CreatedAt = SeedTime,
            CreatedBy = "migration-role-governance-foundation"
        };

    private static GovernanceDefinition Internal(string area) => new(RoleAudiences.InternalEmployee, area, true, null);
    private static GovernanceDefinition External() => new(RoleAudiences.ExternalPortal, "EXTERNAL", false, null);
    private static GovernanceDefinition System(string area) => new(RoleAudiences.SystemSecurity, area, false, null);
    private static GovernanceDefinition Legacy(string area, string replacement) => new(RoleAudiences.LegacyAlias, area, false, replacement);

    private static Role NewRole(string id, string code, string name, bool privileged) => new()
    {
        Id = Guid.Parse(id), Code = code, Name = name, IsPrivileged = privileged, IsActive = true,
        CreatedAt = SeedTime, CreatedBy = "migration-role-governance-foundation"
    };

    private static Guid StableId(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts).ToLowerInvariant()));
        return new Guid(bytes[..16]);
    }

    private sealed record GovernanceDefinition(string Audience, string BusinessArea, bool IsEmployeeAssignable, string? ReplacementRoleCode);
}
