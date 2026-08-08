using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev866SeedData
{
    private const string ImportBatch = "REV866_EMPLOYEE_SEED_20260808";
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    private static readonly DateOnly EffectiveFrom = new(2026, 8, 8);

    private sealed record EmployeeSeed(string Code, string Name, string EmployeeType, string Grade, string Department, string Skill, string Designation, string[] Roles);

    private static readonly EmployeeSeed[] EmployeeRows =
    [
        Row("SESS-001", "A. PARAMANANTHAM", "Management", "Management", "TECHNICAL DIRECTOR", "TECHNICAL_DIRECTOR"),
        Row("SESS-002", "ALAGUEASWARI", "Management", "Management", "MD", "MANAGING_DIRECTOR"),
        Row("SESS-003", "M. SATHISHKUMAR", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-004", "T. DINESH", "Manager", "Manager", "TECHNICAL SUPPORT MANAGER", "TECHNICAL_SUPPORT_MANAGER"),
        Row("SESS-005", "WASEEM.S", "Production/Fabrication", "Production/Fabrication", "PRODUCTION MECHANICAL TEAM", "PRODUCTION_OPERATOR"),
        Row("SESS-006", "S. NANTHAKUMAR", "Junior/Assistant", "Junior/Assistant", "JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT", "JUNIOR_ENGINEER"),
        Row("SESS-007", "A. ALFATHIMA PARVEEN", "Junior/Assistant", "Junior/Assistant", "JR. ACCOUNT", "ACCOUNTS_ASSISTANT"),
        Row("SESS-008", "SURANTHER P", "Engineer/Technical", "Engineer/Technical", "SOFTWARE DEVELOPER", "SOFTWARE_DEVELOPER"),
        Row("SESS-009", "MANIKANDAN.S", "Junior/Assistant", "Junior/Assistant", "JR. ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-010", "RAJESHKUMAR.V", "Engineer/Technical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-011", "YESWANTH KUMAR.N", "Junior/Assistant", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-012", "PRIYA.E", "Admin/Accounts/Stores", "Admin/Accounts/Stores", "STORES AND PURCHASE", "PURCHASE_EXECUTIVE", "STORES_EXECUTIVE"),
        Row("SESS-013", "LALU", "Production/Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-014", "KAMALI SRINIVASAN", "Junior/Assistant", "Junior/Assistant", "STORES ASSISTANT", "STORES_ASSISTANT"),
        Row("SESS-015", "RANJITH.E", "Engineer/Technical", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-016", "KALIDOSS", "Engineer/Technical", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-017", "MOHD ASHIQ", "Junior/Assistant", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-018", "A. VINAYA SAGAR ARKATI", "Engineer/Technical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-019", "RANJITH. R", "Engineer/Technical", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-020", "RANJEETH.B", "Admin/Accounts/Stores", "Admin/Accounts/Stores", "HR DEPT", "HR_EXECUTIVE"),
        Row("SESS-021", "KRISHNAVENI", "Admin/Accounts/Stores", "Admin/Accounts/Stores", "ADMIN MAINTENANCE", "ADMIN_EXECUTIVE"),
        Row("SESS-022", "KARTHICK.B", "Engineer/Technical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-023", "SARATH BABU.K", "Production/Fabrication", "Production/Fabrication", "PRODUCTION COORDINATOR", "PRODUCTION_COORDINATOR"),
        Row("SESS-024", "PRAKASAM.B", "Engineer/Technical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-025", "KARTHIKEYAN MK", "Production/Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-026", "SRINIVASAN.V", "Production/Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-027", "SANJAY SARAVANAN", "Junior/Assistant", "Junior/Assistant", "JUNIOR ACCOUNTS", "ACCOUNTS_ASSISTANT"),
        Row("SESS-028", "PRAVEEN KUMAR.M", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-029", "SRINIVASAN.C", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-030", "MANIKANDAN SOKKALINGAM", "Engineer/Technical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-031", "VENKAT RAV.S", "Junior/Assistant", "Junior/Assistant", "JUNIOR ACCOUNTS", "ACCOUNTS_ASSISTANT"),
        Row("SESS-032", "PRASANNA.G", "Engineer/Technical", "Engineer/Technical", "LABVIEW DEVELOPER", "SOFTWARE_ENGINEER"),
        Row("SESS-033", "BLESSON PAUL", "Junior/Assistant", "Junior/Assistant", "JR. ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-034", "MADHANKUMAR.J", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-035", "VINAYAGAM", "Production/Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-036", "FRANCIS XAVIER", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-037", "DEVANAND B", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-038", "SYED IJAZUDDIN Z", "Engineer/Technical", "Engineer/Technical", "PLC ENGINEER", "PLC_ENGINEER"),
        Row("SESS-039", "THIRUNAVUKKARASU", "Engineer/Technical", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER")
    ];

    public static IReadOnlyList<Role> AdditionalEmployeeRoles =>
    [
        Role("TECHNICAL_DIRECTOR", "Technical Director", true),
        Role("MANAGING_DIRECTOR", "Managing Director", true),
        Role("TECHNICAL_SUPPORT_MANAGER", "Technical Support Manager", true),
        Role("ACCOUNTS_ASSISTANT", "Accounts Assistant", false),
        Role("SOFTWARE_DEVELOPER", "Software Developer", false),
        Role("PURCHASE_EXECUTIVE", "Purchase Executive", false),
        Role("STORES_EXECUTIVE", "Stores Executive", false),
        Role("STORES_ASSISTANT", "Stores Assistant", false),
        Role("HR_EXECUTIVE", "HR Executive", false),
        Role("ADMIN_EXECUTIVE", "Admin Executive", false),
        Role("PRODUCTION_COORDINATOR", "Production Coordinator", false),
        Role("TECHNICAL_ENGINEER", "Technical Engineer", false),
        Role("ELECTRICAL_ENGINEER", "Electrical Engineer", false),
        Role("PLC_ENGINEER", "PLC Engineer", false),
        Role("DESIGN_ENGINEER", "Design Engineer", false),
        Role("JUNIOR_ENGINEER", "Junior Engineer", false),
        Role("PRODUCTION_OPERATOR", "Production Operator", false),
        Role("SOFTWARE_ENGINEER", "Software Engineer", false)
    ];

    public static IReadOnlyList<Department> Departments => EmployeeRows
        .Select(row => row.Department)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(name => new Department { Id = Id("department", name), Code = Code(name), Name = name, CreatedAt = SeedTime, CreatedBy = "migration" })
        .ToList();

    public static IReadOnlyList<Skill> Skills => EmployeeRows
        .Select(row => row.Skill)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(name => new Skill { Id = Id("skill", name), Code = Code(name), Name = name, CreatedAt = SeedTime, CreatedBy = "migration" })
        .ToList();

    public static IReadOnlyList<Designation> Designations => EmployeeRows
        .Select(row => row.Designation)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(name => new Designation { Id = Id("designation", name), Code = Code(name), Name = name, CreatedAt = SeedTime, CreatedBy = "migration" })
        .ToList();

    public static IReadOnlyList<Employee> Employees => EmployeeRows
        .Select(row => new Employee
        {
            Id = EmployeeId(row.Code),
            EmployeeCode = row.Code,
            EmployeeName = NormalizeName(row.Name),
            OriginalImportedName = row.Name,
            EmployeeType = row.EmployeeType,
            Grade = row.Grade,
            DepartmentId = Id("department", row.Department),
            DesignationId = Id("designation", row.Designation),
            Status = "Active",
            LoginEnabled = false,
            ApprovalStatus = "SeedApproved",
            IsEmployeeCodeLocked = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        })
        .ToList();

    public static IReadOnlyList<EmployeeSkill> EmployeeSkills => EmployeeRows
        .Select(row => new EmployeeSkill
        {
            Id = Id("employee-skill", row.Code, row.Skill),
            EmployeeId = EmployeeId(row.Code),
            SkillId = Id("skill", row.Skill),
            IsPrimary = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        })
        .ToList();

    public static IReadOnlyList<EmployeeRoleAssignment> EmployeeRoleAssignments => EmployeeRows
        .SelectMany(row => row.Roles.Select(role => new EmployeeRoleAssignment
        {
            Id = Id("employee-role", row.Code, role),
            EmployeeId = EmployeeId(row.Code),
            RoleId = RoleId(role),
            EffectiveFrom = EffectiveFrom,
            ApprovalStatus = "SeedApproved",
            Remarks = "REV866 approved initial mapping",
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        }))
        .ToList();

    public static IReadOnlyList<EmployeeImportHistory> EmployeeImportHistories => EmployeeRows
        .Select(row => new EmployeeImportHistory
        {
            Id = Id("employee-import", row.Code),
            EmployeeId = EmployeeId(row.Code),
            ImportBatch = ImportBatch,
            SourceEmployeeCode = row.Code,
            SourceEmployeeName = row.Name,
            NormalizedEmployeeName = NormalizeName(row.Name),
            SourceJson = JsonSerializer.Serialize(row),
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        })
        .ToList();

    public static IReadOnlyList<RolePagePermission> RolePagePermissions
    {
        get
        {
            var rows = new List<RolePagePermission>();
            foreach (var role in FoundationSeedData.Roles)
            {
                foreach (var page in FoundationSeedData.Pages)
                {
                    rows.Add(Permission(role, page));
                }
            }
            return rows;
        }
    }

    private static RolePagePermission Permission(Role role, PageDefinition page)
    {
        var full = role.Code is "admin" or "md";
        var audit = full || role.Code is "it_admin";
        var purchase = page.PageKey.StartsWith("purchase.", StringComparison.OrdinalIgnoreCase);
        var inventory = page.PageKey.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase);
        var master = page.PageKey.StartsWith("masters.", StringComparison.OrdinalIgnoreCase);
        var identity = page.PageKey.StartsWith("identity.", StringComparison.OrdinalIgnoreCase) || page.PageKey.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase);
        var commercial = role.Code is "admin" or "md" or "accounts_head" or "purchase_head";
        var canOperatePurchase = role.Code is "admin" or "md" or "purchase_head" or "store_head";
        var canOperateInventory = role.Code is "admin" or "md" or "store_head" or "purchase_head" or "production_head" or "qc_head";
        var canOperateMaster = role.Code is "admin" or "md" or "it_admin" or "purchase_head" or "store_head" or "sales_head";
        var canView = full || audit || (!identity && (master || purchase || inventory || page.PageKey == "audit.history"));
        var canCreate = full || (master && canOperateMaster) || (purchase && canOperatePurchase) || (inventory && canOperateInventory);
        var canVerify = full || (inventory && role.Code is "qc_head") || (purchase && role.Code is "purchase_head");
        var canApprove = full || role.Code is "accounts_head" && page.PageKey.Contains("commercial", StringComparison.OrdinalIgnoreCase);

        return new RolePagePermission
        {
            Id = Id("role-page", role.Code, page.PageKey),
            RoleId = role.Id,
            PageDefinitionId = page.Id,
            CanView = canView,
            CanCreate = canCreate,
            CanUpdate = canCreate,
            CanSubmit = canCreate,
            CanVerify = canVerify,
            CanApprove = canApprove,
            CanReject = canVerify || canApprove,
            CanRequestClarification = canVerify || canApprove,
            CanRequestRevision = canVerify || canApprove,
            CanResubmit = canCreate,
            CanCancel = full || canCreate,
            CanDeactivate = full || (master && role.Code is "it_admin"),
            CanPrint = canView,
            CanDownload = canView,
            CanExport = canView && role.Code is not "customer" and not "vendor",
            CanUploadAttachment = canCreate,
            CanReplaceAttachment = canCreate,
            CanViewCommercialValues = commercial,
            CanViewAuditHistory = audit || role.Code is "accounts_head",
            HasFullControl = full,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        };
    }

    private static EmployeeSeed Row(string code, string name, string department, string skill, string designation, params string[] roles)
    {
        return new EmployeeSeed(code, name, "Permanent", "Executive", department, skill, designation, roles);
    }

    private static Role Role(string code, string name, bool isPrivileged)
    {
        return new Role
        {
            Id = RoleId(code),
            Code = code.ToLowerInvariant(),
            Name = name,
            IsPrivileged = isPrivileged,
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        };
    }

    private static Guid EmployeeId(string employeeCode) => Id("employee", employeeCode);

    private static Guid RoleId(string roleCode) => Id("role", roleCode.ToLowerInvariant());

    private static Guid Id(params string[] parts)
    {
        var input = string.Join("|", parts).ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes[..16]);
    }

    private static string Code(string value)
    {
        return value.ToUpperInvariant()
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal);
    }

    private static string NormalizeName(string value)
    {
        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
