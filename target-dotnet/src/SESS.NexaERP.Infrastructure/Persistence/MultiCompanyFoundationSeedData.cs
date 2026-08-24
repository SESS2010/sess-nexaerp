using System.Security.Cryptography;
using System.Text;
using SESS.NexaERP.Domain.Foundation;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class MultiCompanyFoundationSeedData
{
    public static readonly Guid SessPvtLtdId = Guid.Parse("70000000-0000-0000-0000-000000000001");
    public static readonly Guid SessProprietorshipId = Guid.Parse("70000000-0000-0000-0000-000000000002");
    public static readonly DateOnly EffectiveFrom = new(2026, 8, 24);
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;

    public static readonly Company[] Companies =
    [
        new()
        {
            Id = SessProprietorshipId,
            Code = "SESS_PROPRIETORSHIP",
            LegalName = "Sri Easwari Scientific Solution",
            EntityType = "PROPRIETORSHIP",
            Status = "ACTIVE",
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration-multicompany-foundation"
        },
        new()
        {
            Id = SessPvtLtdId,
            Code = "SESS_PVT_LTD",
            LegalName = "Sri Easwari Scientific Solution Private Limited",
            EntityType = "PRIVATE_LIMITED",
            Status = "ACTIVE",
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration-multicompany-foundation"
        }
    ];

    public static readonly CompanyGstRegistration[] CompanyGstRegistrations =
    [
        Gst("71000000-0000-0000-0000-000000000001", SessProprietorshipId, "33APRPA5532K1ZU", "Sri Easwari Scientific Solution", "PROPRIETORSHIP"),
        Gst("71000000-0000-0000-0000-000000000002", SessPvtLtdId, "33ABACS5491H1ZA", "Sri Easwari Scientific Solution Private Limited", "PRIVATE_LIMITED")
    ];

    public static readonly Currency[] Currencies =
    [
        new()
        {
            Id = Guid.Parse("72000000-0000-0000-0000-000000000001"),
            Code = "INR",
            Name = "Indian Rupee",
            NumericCode = "356",
            MinorUnitDigits = 2,
            Symbol = "₹",
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration-multicompany-foundation"
        }
    ];

    public static readonly EmployeeCompanyAssignment[] EmployeeCompanyAssignments = Rev866SeedData.Employees
        .Select(employee => new EmployeeCompanyAssignment
        {
            Id = StableId("employee-company-assignment", employee.EmployeeCode, "SESS_PVT_LTD", "PAYROLL"),
            CompanyId = SessPvtLtdId,
            EmployeeId = employee.Id,
            AssignmentType = "PAYROLL",
            EmployeeCode = employee.EmployeeCode,
            PayrollEmployeeId = employee.PayrollEmployeeId,
            EmploymentType = employee.EmployeeType,
            EffectiveFrom = EffectiveFrom,
            Status = "ACTIVE",
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration-multicompany-foundation"
        })
        .ToArray();

    private static readonly HashSet<string> CrossTrainedEmployeeCodes =
    [
        "SESS-003", "SESS-005", "SESS-006", "SESS-009", "SESS-010", "SESS-011", "SESS-013", "SESS-017",
        "SESS-018", "SESS-022", "SESS-024", "SESS-025", "SESS-026", "SESS-028", "SESS-029", "SESS-030",
        "SESS-032", "SESS-033", "SESS-034", "SESS-035", "SESS-036", "SESS-037", "SESS-038", "SESS-039"
    ];

    private static readonly string[] SecondaryDepartmentCodes =
        ["ELECTRICAL", "REFRIGERATION", "PLC_LABVIEW", "FABRICATION", "SERVICE", "AMC", "CAMC"];

    private static readonly IReadOnlyDictionary<string, string[]> OfficeSecondaryDepartmentCodes =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["SESS-012"] = ["STORES"],
            ["SESS-014"] = ["PURCHASE"]
        };

    public static readonly EmployeeDepartmentAssignment[] EmployeeDepartmentAssignments = BuildDepartmentAssignments();

    private static EmployeeDepartmentAssignment[] BuildDepartmentAssignments()
    {
        var departmentById = Rev866SeedData.Departments.ToDictionary(department => department.Id);
        var departmentByCode = Rev866SeedData.Departments.ToDictionary(department => department.Code, StringComparer.Ordinal);
        var rows = new List<EmployeeDepartmentAssignment>();

        foreach (var employee in Rev866SeedData.Employees)
        {
            var companyAssignmentId = StableId("employee-company-assignment", employee.EmployeeCode, "SESS_PVT_LTD", "PAYROLL");
            var primary = departmentById[employee.DepartmentId];
            rows.Add(DepartmentAssignment(employee.EmployeeCode, companyAssignmentId, primary.Id, employee.DesignationId, "PRIMARY", true));

            if (CrossTrainedEmployeeCodes.Contains(employee.EmployeeCode))
            {
                foreach (var departmentCode in SecondaryDepartmentCodes.Where(code => code != primary.Code))
                    rows.Add(DepartmentAssignment(employee.EmployeeCode, companyAssignmentId, departmentByCode[departmentCode].Id, employee.DesignationId, "SECONDARY", false));
            }

            if (OfficeSecondaryDepartmentCodes.TryGetValue(employee.EmployeeCode, out var officeSecondaries))
            {
                foreach (var departmentCode in officeSecondaries)
                    rows.Add(DepartmentAssignment(employee.EmployeeCode, companyAssignmentId, departmentByCode[departmentCode].Id, employee.DesignationId, "SECONDARY", false));
            }
        }

        if (rows.Count != 186 || rows.Count(row => row.IsPrimary) != 39 || rows.Count(row => !row.IsPrimary) != 147)
            throw new InvalidOperationException("Deterministic employee department assignment counts changed.");
        return rows.ToArray();
    }

    private static EmployeeDepartmentAssignment DepartmentAssignment(
        string employeeCode,
        Guid companyAssignmentId,
        Guid departmentId,
        Guid designationId,
        string assignmentType,
        bool isPrimary) => new()
    {
        Id = StableId("employee-department-assignment", employeeCode, departmentId.ToString("D"), assignmentType),
        CompanyId = SessPvtLtdId,
        EmployeeCompanyAssignmentId = companyAssignmentId,
        DepartmentId = departmentId,
        DesignationId = designationId,
        AssignmentType = assignmentType,
        EffectiveFrom = EffectiveFrom,
        IsPrimary = isPrimary,
        Status = "ACTIVE",
        IsActive = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-multicompany-foundation"
    };

    private static CompanyGstRegistration Gst(string id, Guid companyId, string gstin, string legalName, string registrationType) => new()
    {
        Id = Guid.Parse(id),
        CompanyId = companyId,
        Gstin = gstin,
        RegisteredLegalName = legalName,
        StateCode = "33",
        RegistrationType = registrationType,
        EffectiveFrom = EffectiveFrom,
        IsPrimary = true,
        IsActive = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-multicompany-foundation"
    };

    private static Guid StableId(params string[] parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', parts).ToLowerInvariant()));
        return new Guid(bytes[..16]);
    }
}
