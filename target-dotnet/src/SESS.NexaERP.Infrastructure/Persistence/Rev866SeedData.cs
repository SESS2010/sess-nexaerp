using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Audit;
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
        Row("SESS-002", "ALAGUEASWARI", "Management", "Management", "MANAGING DIRECTOR", "MANAGING_DIRECTOR"),
        Row("SESS-003", "M. SATHISHKUMAR", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-004", "T. DINESH", "Service", "Manager", "TECHNICAL SUPPORT MANAGER", "TECHNICAL_SUPPORT_MANAGER"),
        Row("SESS-005", "WASEEM.S", "Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-006", "S. NANTHAKUMAR", "Electrical", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-007", "A. ALFATHIMA PARVEEN", "Accounts", "Junior/Assistant", "JUNIOR ACCOUNTS", "ACCOUNTS_ASSISTANT"),
        Row("SESS-008", "SURANTHER P", "IT", "Engineer/Technical", "IT MANAGER", "SOFTWARE_DEVELOPER"),
        Row("SESS-009", "MANIKANDAN.S", "Maintenance", "Junior/Assistant", "MAINTENANCE ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-010", "RAJESHKUMAR.V", "Electrical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-011", "YESWANTH KUMAR.N", "Service", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-012", "PRIYA.E", "Purchase", "Admin/Accounts/Stores", "PURCHASE EXECUTIVE", "PURCHASE_EXECUTIVE", "STORES_EXECUTIVE"),
        Row("SESS-013", "LALU", "Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-014", "KAMALI SRINIVASAN", "Stores", "Junior/Assistant", "STORES ASSISTANT", "STORES_ASSISTANT"),
        Row("SESS-015", "RANJITH.E", "Design", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-016", "KALIDOSS", "Design", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-017", "MOHD ASHIQ", "Electrical", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-018", "A. VINAYA SAGAR ARKATI", "Electrical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-019", "RANJITH. R", "Design", "Engineer/Technical", "DESIGN ENGINEER", "DESIGN_ENGINEER"),
        Row("SESS-020", "RANJEETH.B", "HR", "Admin/Accounts/Stores", "HR MANAGER", "HR_EXECUTIVE"),
        Row("SESS-021", "KRISHNAVENI", "HR", "Admin/Accounts/Stores", "HOUSEKEEPING ASSISTANT", "ADMIN_EXECUTIVE"),
        Row("SESS-022", "KARTHICK.B", "Electrical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-023", "SARATH BABU.K", "Fabrication", "Production/Fabrication", "PRODUCTION COORDINATOR", "PRODUCTION_COORDINATOR"),
        Row("SESS-024", "PRAKASAM.B", "Electrical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-025", "KARTHIKEYAN MK", "Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-026", "SRINIVASAN.V", "Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-027", "SANJAY SARAVANAN", "Accounts", "Junior/Assistant", "JUNIOR ACCOUNTS", "ACCOUNTS_ASSISTANT"),
        Row("SESS-028", "PRAVEEN KUMAR.M", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-029", "SRINIVASAN.C", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-030", "MANIKANDAN SOKKALINGAM", "Electrical", "Engineer/Technical", "ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER"),
        Row("SESS-031", "VENKAT RAV.S", "Accounts", "Junior/Assistant", "JUNIOR ACCOUNTS", "ACCOUNTS_ASSISTANT"),
        Row("SESS-032", "PRASANNA.G", "PLC/LabVIEW", "Engineer/Technical", "LABVIEW DEVELOPER", "SOFTWARE_ENGINEER"),
        Row("SESS-033", "BLESSON PAUL", "Electrical", "Junior/Assistant", "JUNIOR ENGINEER", "JUNIOR_ENGINEER"),
        Row("SESS-034", "MADHANKUMAR.J", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-035", "VINAYAGAM", "Fabrication", "Production/Fabrication", "FABRICATOR", "PRODUCTION_OPERATOR"),
        Row("SESS-036", "FRANCIS XAVIER", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-037", "DEVANAND B", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER"),
        Row("SESS-038", "SYED IJAZUDDIN Z", "PLC/LabVIEW", "Engineer/Technical", "PLC ENGINEER", "PLC_ENGINEER"),
        Row("SESS-039", "THIRUNAVUKKARASU", "Refrigeration", "Engineer/Technical", "REFRIGERATION / MECHANICAL ENGINEER", "TECHNICAL_ENGINEER")
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

    private static readonly Department[] CanonicalDepartments =
    [
        DepartmentSeed("Management", "MANAGEMENT", "Management"),
        DepartmentSeed("Sales", "SALES", "Sales"),
        DepartmentSeed("Marketing", "MARKETING", "Marketing"),
        DepartmentSeed("Design", "DESIGN", "Design"),
        DepartmentSeed("Production", "PRODUCTION", "Production"),
        DepartmentSeed("QC", "QC", "QC"),
        DepartmentSeed("R&D", "R_AND_D", "R&D"),
        DepartmentSeed("Stores", "STORES", "Stores"),
        DepartmentSeed("Purchase", "PURCHASE", "Purchase"),
        DepartmentSeed("Service", "SERVICE", "Service"),
        DepartmentSeed("AMC", "AMC", "AMC"),
        DepartmentSeed("CAMC", "CAMC", "CAMC"),
        DepartmentSeed("Accounts", "ACCOUNTS", "Accounts"),
        DepartmentSeed("HR", "HR", "HR"),
        DepartmentSeed("IT", "IT", "IT"),
        DepartmentSeed("Maintenance", "MAINTENANCE", "Maintenance"),
        DepartmentSeed("Calibration", "CALIBRATION", "Calibration"),
        DepartmentSeed("Production/Fabrication", "FABRICATION", "Fabrication", "Production"),
        DepartmentSeed("Refrigeration", "REFRIGERATION", "Refrigeration", "Production"),
        DepartmentSeed("Electrical", "ELECTRICAL", "Electrical", "Production"),
        DepartmentSeed("PLC/LabVIEW", "PLC_LABVIEW", "PLC/LabVIEW", "Production")
    ];

    private static readonly Department[] LegacyDepartments =
    [
        DepartmentSeed("Manager", "LEGACY_MANAGER", "Manager (Legacy)", isActive: false),
        DepartmentSeed("Junior/Assistant", "LEGACY_JUNIOR_ASSISTANT", "Junior/Assistant (Legacy)", isActive: false),
        DepartmentSeed("Admin/Accounts/Stores", "LEGACY_ADMIN_ACCOUNTS_STORES", "Admin/Accounts/Stores (Legacy)", isActive: false),
        DepartmentSeed("Engineer/Technical", "LEGACY_ENGINEER_TECHNICAL", "Engineer/Technical (Legacy)", isActive: false)
    ];

    private static readonly Designation[] CanonicalDesignations =
    [
        DesignationSeed("LABVIEW DEVELOPER", "LABVIEW_DEVELOPER", "LabVIEW Developer"),
        DesignationSeed("TECHNICAL DIRECTOR", "TECHNICAL_DIRECTOR", "Technical Director"),
        DesignationSeed("ELECTRICAL ENGINEER", "ELECTRICAL_ENGINEER", "Electrical Engineer"),
        DesignationSeed("JUNIOR ACCOUNTS", "JUNIOR_ACCOUNTS", "Junior Accounts"),
        DesignationSeed("JUNIOR ENGINEER", "JUNIOR_ENGINEER", "Junior Engineer"),
        DesignationSeed("DESIGN ENGINEER", "DESIGN_ENGINEER", "Design Engineer"),
        DesignationSeed("HR DEPT", "HR_MANAGER", "HR Manager"),
        DesignationSeed("ADMIN MAINTENANCE", "HOUSEKEEPING_ASSISTANT", "Housekeeping Assistant"),
        DesignationSeed("SOFTWARE DEVELOPER", "SOFTWARE_DEVELOPER", "Software Developer"),
        DesignationSeed("STORES ASSISTANT", "STORES_ASSISTANT", "Stores Assistant"),
        DesignationSeed("PURCHASE EXECUTIVE", "PURCHASE_EXECUTIVE", "Purchase Executive"),
        DesignationSeed("PRODUCTION COORDINATOR", "PRODUCTION_COORDINATOR", "Production Coordinator"),
        DesignationSeed("MD", "MANAGING_DIRECTOR", "Managing Director"),
        DesignationSeed("REFRIGERATION / MECHANICAL ENGINEER", "REFRIGERATION_MECHANICAL_ENGINEER", "Refrigeration / Mechanical Engineer"),
        DesignationSeed("PLC ENGINEER", "PLC_ENGINEER", "PLC Engineer"),
        DesignationSeed("TECHNICAL SUPPORT MANAGER", "TECHNICAL_SUPPORT_MANAGER", "Technical Support Manager"),
        DesignationSeed("FABRICATOR", "FABRICATOR", "Fabricator"),
        DesignationSeed("MAINTENANCE ENGINEER", "MAINTENANCE_ENGINEER", "Maintenance Engineer"),
        DesignationSeed("IT MANAGER", "IT_MANAGER", "IT Manager")
    ];

    private static readonly Designation[] LegacyDesignations =
    [
        DesignationSeed("JR. ACCOUNT", "LEGACY_JR_ACCOUNT", "Jr. Account (Legacy)", false),
        DesignationSeed("JR. ELECTRICAL / PLC / INSTRUMENTATION SUPPORT", "LEGACY_JR_ELECTRICAL_PLC_SUPPORT", "Jr. Electrical / PLC / Instrumentation Support (Legacy)", false),
        DesignationSeed("JR. ENGINEER", "LEGACY_JR_ENGINEER", "Jr. Engineer (Legacy)", false),
        DesignationSeed("STORES AND PURCHASE", "LEGACY_STORES_AND_PURCHASE", "Stores and Purchase (Legacy)", false),
        DesignationSeed("PRODUCTION MECHANICAL TEAM", "LEGACY_PRODUCTION_MECHANICAL_TEAM", "Production Mechanical Team (Legacy)", false)
    ];

    public static IReadOnlyList<Department> Departments => CanonicalDepartments.Concat(LegacyDepartments).ToList();

    public static IReadOnlyList<Skill> Skills => EmployeeRows
        .Select(row => row.Skill)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(name => new Skill { Id = Id("skill", name), Code = Code(name), Name = name, CreatedAt = SeedTime, CreatedBy = "migration" })
        .ToList();

    public static IReadOnlyList<Designation> Designations => CanonicalDesignations.Concat(LegacyDesignations).ToList();

    public static IReadOnlyList<Employee> Employees => EmployeeRows
        .Select(row => new Employee
        {
            Id = EmployeeId(row.Code),
            EmployeeCode = row.Code,
            EmployeeName = NormalizeName(row.Name),
            OriginalImportedName = row.Name,
            EmployeeType = row.EmployeeType,
            Grade = row.Grade,
            DepartmentId = DepartmentId(row.Department),
            DesignationId = DesignationId(row.Designation),
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
            CompanyId = MultiCompanyFoundationSeedData.SessPvtLtdId,
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

    public static IReadOnlyList<EmployeeStatusHistory> EmployeeStatusHistories => EmployeeRows
        .Select(row => new EmployeeStatusHistory
        {
            Id = Id("employee-status-initial", row.Code, "REV866C1"),
            EmployeeId = EmployeeId(row.Code),
            OldStatus = "Not Created",
            NewStatus = "Active",
            Reason = "Initial approved employee seed/import | SourceRevision=REV866C1 | Correlation=REV866C1_EMPLOYEE_STATUS_INITIAL",
            CreatedAt = SeedTime,
            CreatedBy = "system-migration-rev866c1"
        })
        .ToList();

    public static IReadOnlyList<AuditLog> CorrectiveAuditLogs =>
    [
        Audit("employee-import", "Employees", "Import", nameof(EmployeeImportHistory), "REV866_EMPLOYEE_SEED_20260808", null, "{\"employeeCount\":39,\"sourceRevision\":\"REV866\"}", "Success"),
        Audit("role-assignment", "Employees", "SeedRoleAssignments", nameof(EmployeeRoleAssignment), "REV866_EMPLOYEE_ROLE_ASSIGNMENTS", null, "{\"assignmentCount\":40,\"sourceRevision\":\"REV866\"}", "Success"),
        Audit("initial-status", "Employees", "SeedInitialStatus", nameof(EmployeeStatusHistory), "REV866C1_EMPLOYEE_STATUS_INITIAL", null, "{\"statusHistoryCount\":39,\"newStatus\":\"Active\"}", "Success"),
        Audit("permission-denial", "Security", "Denied", "employees.master", "view", null, "{\"permission\":\"view\",\"result\":\"denied\",\"sourceRevision\":\"REV866C1\"}", "Failure"),
        Audit("employee-status-change", "Employees", "ApprovalStatusChangeEvidence", nameof(EmployeeApprovalHistory), "REV866C1_EMPLOYEE_APPROVAL_STATUS", "{\"approvalStatus\":\"SeedApproved\"}", "{\"approvalStatus\":\"SeedApproved\",\"evidence\":\"corrective checkpoint\"}", "Success"),
        Audit("role-mapping-change", "Employees", "RoleMappingChangeEvidence", nameof(EmployeeRoleAssignment), "REV866C1_ROLE_MAPPING_CHANGE", "{\"mapping\":\"none\"}", "{\"mapping\":\"seeded approved role mappings preserved\"}", "Success")
    ];

    public static IReadOnlyList<RolePagePermission> RolePagePermissions
    {
        get
        {
            var rows = new List<RolePagePermission>();
            foreach (var role in FoundationSeedData.Roles.Concat(AdditionalEmployeeRoles))
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
        var full = role.Code is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR";
        var audit = full || role.Code is "IT_MANAGER";
        var purchase = page.PageKey.StartsWith("purchase.", StringComparison.OrdinalIgnoreCase);
        var inventory = page.PageKey.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase);
        var master = page.PageKey.StartsWith("masters.", StringComparison.OrdinalIgnoreCase);
        var identity = page.PageKey.StartsWith("identity.", StringComparison.OrdinalIgnoreCase) || page.PageKey.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase);
        var employeePage = page.PageKey.StartsWith("employees.", StringComparison.OrdinalIgnoreCase);
        var receiptPage = page.PageKey.Equals("inventory.grn", StringComparison.OrdinalIgnoreCase);
        var stockCheckPage = page.PageKey.Equals("stores.stock-check", StringComparison.OrdinalIgnoreCase);
        var receiptOperatorRole = role.Code is "STORES_EXECUTIVE" or "STORES_ASSISTANT";
        var stockCheckRole = role.Code is "STORES_EXECUTIVE" or "STORE_HEAD";
        var foundationMatrixRole = FoundationSeedData.Roles.Any(seedRole => seedRole.Code == role.Code);
        var commercial = role.Code is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "ACCOUNTS_HEAD" or "PURCHASE_HEAD";
        var canOperatePurchase = role.Code is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "PURCHASE_HEAD" or "STORE_HEAD" or "PURCHASE_EXECUTIVE";
        var canOperateInventory = role.Code is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "STORE_HEAD" or "PURCHASE_HEAD" or "PRODUCTION_HEAD" or "QC_HEAD" or "STORES_EXECUTIVE" or "STORES_ASSISTANT";
        var canOperateMaster = role.Code is "ADMIN" or "MD" or "TECHNICAL_DIRECTOR" or "MANAGING_DIRECTOR" or "IT_MANAGER" or "PURCHASE_HEAD" or "STORE_HEAD" or "SALES_HEAD";
        var canOperateEmployee = role.Code is "ADMIN" or "MD" or "MANAGING_DIRECTOR" or "IT_MANAGER" or "HR_EXECUTIVE";
        var canAdministerIdentity = role.Code == "IT_MANAGER" && page.PageKey is "identity.roles" or "identity.users";
        var canView = full || audit || stockCheckPage && stockCheckRole || (foundationMatrixRole && !identity && (master || purchase || inventory || page.PageKey == "audit.history"))
            || (purchase && role.Code is "PURCHASE_EXECUTIVE")
            || (inventory && role.Code is "STORES_EXECUTIVE" or "STORES_ASSISTANT")
            || (employeePage && role.Code is "HR_EXECUTIVE");
        var canCreate = receiptPage
            ? receiptOperatorRole
            : full || (master && canOperateMaster) || (purchase && canOperatePurchase) || (inventory && canOperateInventory) || (employeePage && canOperateEmployee);
        var canVerify = full || stockCheckPage && stockCheckRole || (inventory && role.Code is "QC_HEAD") || (purchase && role.Code is "PURCHASE_HEAD");
        var canApprove = full || role.Code is "ACCOUNTS_HEAD" && page.PageKey.Contains("commercial", StringComparison.OrdinalIgnoreCase);

        return new RolePagePermission
        {
            Id = Id("role-page", LegacyPermissionIdentityRoleCode(role), page.PageKey),
            RoleId = role.Id,
            PageDefinitionId = page.Id,
            CanView = canView,
            CanCreate = canCreate || canAdministerIdentity,
            CanUpdate = canCreate,
            CanSubmit = canCreate,
            CanVerify = canVerify,
            CanApprove = canApprove,
            CanReject = stockCheckPage && stockCheckRole ? false : canVerify || canApprove,
            CanRequestClarification = stockCheckPage && stockCheckRole ? false : canVerify || canApprove,
            CanRequestRevision = stockCheckPage && stockCheckRole ? false : canVerify || canApprove,
            CanResubmit = canCreate,
            CanCancel = receiptPage ? receiptOperatorRole : full || canCreate,
            CanDeactivate = full || ((master || employeePage) && role.Code is "IT_MANAGER"),
            CanPrint = canView,
            CanDownload = canView,
            CanExport = canView && foundationMatrixRole && role.Code is not "CUSTOMER" and not "VENDOR",
            CanUploadAttachment = canCreate,
            CanReplaceAttachment = canCreate,
            CanViewCommercialValues = commercial,
            CanViewAuditHistory = audit || role.Code is "ACCOUNTS_HEAD",
            HasFullControl = full,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        };
    }

    // IT_MANAGER is an in-place semantic rename of it_admin. Its 26 seeded
    // permission row IDs must remain byte-for-byte stable across that rename.
    private static string LegacyPermissionIdentityRoleCode(Role role) =>
        role.Code == "IT_MANAGER" ? "it_admin" : role.Code;

    private static EmployeeSeed Row(string code, string name, string department, string skill, string designation, params string[] roles)
    {
        return new EmployeeSeed(code, name, "Permanent", "Executive", department, skill, designation, roles);
    }

    private static AuditLog Audit(string key, string module, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string result)
    {
        return new AuditLog
        {
            Id = Id("audit", "REV866C1", key),
            Module = module,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserLoginId = "system-migration-rev866c1",
            Result = result,
            CorrelationId = "REV866C1_" + key.ToUpperInvariant().Replace("-", "_", StringComparison.Ordinal),
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            CreatedAt = SeedTime,
            CreatedBy = "system-migration-rev866c1"
        };
    }

    private static Role Role(string code, string name, bool isPrivileged)
    {
        return new Role
        {
            Id = RoleId(code),
            Code = code.ToUpperInvariant(),
            Name = name,
            IsPrivileged = isPrivileged,
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration"
        };
    }

    private static Department DepartmentSeed(
        string identityName,
        string code,
        string name,
        string? parentIdentityName = null,
        bool isActive = true) => new()
    {
        Id = Id("department", identityName),
        ParentDepartmentId = parentIdentityName is null ? null : Id("department", parentIdentityName),
        Code = code,
        Name = name,
        IsActive = isActive,
        CreatedAt = SeedTime,
        CreatedBy = "migration"
    };

    private static Designation DesignationSeed(string identityName, string code, string name, bool isActive = true) => new()
    {
        Id = Id("designation", identityName),
        Code = code,
        Name = name,
        IsActive = isActive,
        CreatedAt = SeedTime,
        CreatedBy = "migration"
    };

    private static Guid EmployeeId(string employeeCode) => Id("employee", employeeCode);

    private static Guid DepartmentId(string name) => Id("department", name == "Fabrication" ? "Production/Fabrication" : name);

    private static Guid DesignationId(string name) => Id("designation", name switch
    {
        "MANAGING DIRECTOR" => "MD",
        "HR MANAGER" => "HR DEPT",
        "HOUSEKEEPING ASSISTANT" => "ADMIN MAINTENANCE",
        _ => name
    });

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
