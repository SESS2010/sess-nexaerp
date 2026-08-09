namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev868C3EmployeeWorkbookData
{
    public static readonly DateOnly EffectiveFrom = new(2026, 8, 9);
    public const string SourceWorkbook = "SESS_NexaERP_Final_Employee_Master_2026-08-09.xlsx";
    public const string PendingApproverMappingStatus = "PendingApproverMapping";

    public static IReadOnlyList<DepartmentDecision> Departments =>
    [
        new("MANAGEMENT", "Management"),
        new("PURCHASE", "Purchase"),
        new("STORES", "Stores"),
        new("ACCOUNTS_FINANCE", "Accounts / Finance"),
        new("HR_ADMIN", "HR / Admin"),
        new("PRODUCTION_FABRICATION", "Production / Fabrication"),
        new("DESIGN", "Design"),
        new("ELECTRICAL_PLC_INSTRUMENTATION", "Electrical / PLC / Instrumentation"),
        new("REFRIGERATION_MECHANICAL", "Refrigeration / Mechanical"),
        new("SERVICE_TECHNICAL_SUPPORT", "Service / Technical Support"),
        new("SOFTWARE_IT", "Software / IT"),
        new("QUALITY_QC", "Quality / QC")
    ];

    public static IReadOnlyList<ActiveEmployeeDecision> ActiveEmployees =>
    [
        Employee("SESS-001", "1001", "A. PARAMANANTHAM", "Male", "TO_CONFIRM", null, null, "Missing", "Permanent", "Executive", "Technical Director", "Technical Director / CEO; top company authority", "MANAGEMENT", "CHENNAI", "SESS-002", "SESS-001", "ALL", "General"),
        Employee("SESS-002", "1002", "P. ALAGUEASWARI", "Female", "TO_CONFIRM", null, null, "Missing", "Permanent", "Executive", "Managing Director", "Managing Director; first management approval", "MANAGEMENT", "CHENNAI", "SESS-002", "SESS-001", "ALL", "General"),
        Employee("SESS-003", "1004", "SATHISHKUMAR M", "Male", "Degree", new(1992, 12, 12), new(2018, 6, 16), "Source date", "Permanent", "Executive", "Refrigeration Engineer", "Second-level Service Manager; Refrigeration lead", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration"),
        Employee("SESS-004", "1003", "DINESH T", "Male", "Diploma", new(1989, 5, 7), new(2022, 1, 16), "Source date", "Permanent", "Executive", "Sr. Service Engineer", "Technical Support / Chennai Service Manager", "SERVICE_TECHNICAL_SUPPORT", "CHENNAI", "SESS-004", "SESS-003", "CHENNAI", "Refrigeration"),
        Employee("SESS-005", "1007", "WASEEM S", "Male", "ITI", new(1988, 6, 1), new(2023, 2, 2), "Source date", "Permanent", "Executive", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-006", "1005", "NANTHAKUMAR S", "Male", "Degree", new(2000, 4, 12), new(2022, 2, 1), "Source date", "Permanent", "Executive", "Electrical Engineer", "Pune Branch Incharge", "ELECTRICAL_PLC_INSTRUMENTATION", "PUNE", "SESS-038", "SESS-001", "ALL", "Electrical"),
        Employee("SESS-007", "1018", "ALFATHIMA PARVEEN A", "Female", "Degree", new(2003, 3, 7), new(2022, 12, 2), "Source date", "Permanent", "Executive", "Jr. Accountant", "Accounts Manager", "ACCOUNTS_FINANCE", "CHENNAI", "SESS-007", "SESS-002", "ALL", "Admin"),
        Employee("SESS-008", "1016", "SURANTHER P", "Male", "Degree", new(1992, 5, 20), new(2024, 7, 5), "Source date", "Permanent", "Executive", "Software Developer", "IT Manager and Software Developer", "SOFTWARE_IT", "CHENNAI", "SESS-008", "SESS-049", "ALL", "IT"),
        Employee("SESS-009", "1010", "MANIKANDAN.S", "Male", "ITI", new(2004, 4, 19), new(2024, 1, 2), "Source date", "Permanent", "Executive", "Service Technician", "Junior QC support; QC alternate only during approved delegation", "SERVICE_TECHNICAL_SUPPORT", "CHENNAI", "SESS-004", "SESS-003", "CHENNAI", "Refrigeration"),
        Employee("SESS-010", "1013", "RAJESH KUMAR V", "Male", "ITI", new(1997, 11, 14), new(2024, 1, 29), "Source date", "Permanent", "Executive", "Electrical Engineer", "Electrical Engineer", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Electrical"),
        Employee("SESS-011", "1015", "YESWANTH KUMAR N", "Male", "ITI", new(1998, 9, 28), new(2024, 6, 20), "Source date", "Permanent", "Executive", "Jr Engineer", "Bangalore Service Incharge; PR manager up to INR 50,000 for Bangalore Service", "SERVICE_TECHNICAL_SUPPORT", "BANGALORE", "SESS-011", "SESS-004", "BANGALORE", "Refrigeration"),
        Employee("SESS-012", "1019", "PRIYA E", "Female", "Degree", new(1989, 1, 29), new(2024, 10, 21), "Source date", "Permanent", "Executive", "Purchase Incharge", "Purchase Manager; Purchase primary approver", "PURCHASE", "CHENNAI", "SESS-012", "SESS-014", "ALL", "Admin"),
        Employee("SESS-013", "1006", "LALU", "Male", "ITI", new(1995, 4, 1), new(2022, 2, 1), "Source date", "Permanent", "Executive", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-014", "1020", "KAMALI SRINIVASAN", "Female", "Degree", new(1996, 6, 3), new(2024, 12, 4), "Source date", "Permanent", "Executive", "Store Assistant", "Stores Manager; Stores primary approver", "STORES", "CHENNAI", "SESS-014", "SESS-012", "ALL", "Admin"),
        Employee("SESS-015", "1021", "RANJITH E", "Male", "Diploma", new(2001, 7, 28), new(2024, 12, 9), "Source date", "Permanent", "Executive", "Design Engineer", "Regular Product Design Manager", "DESIGN", "CHENNAI", "SESS-015", "SESS-019", "REGULAR_PRODUCT", "Design"),
        Employee("SESS-017", "1025", "MOHD ASHIQ", "Male", "Degree", new(2000, 9, 14), new(2024, 12, 19), "Source date", "Permanent", "Executive", "Jr Engineer", "Service Engineer; no normal approval authority", "SERVICE_TECHNICAL_SUPPORT", "CHENNAI", "SESS-004", "SESS-003", "CHENNAI", "Electrical"),
        Employee("SESS-019", "1027", "RANJITH R", "Male", "Degree", new(1999, 4, 27), new(2025, 1, 2), "Source date", "Permanent", "Executive", "Design Engineer", "Project Design Manager", "DESIGN", "CHENNAI", "SESS-019", "SESS-015", "PROJECT", "Design"),
        Employee("SESS-020", "1035", "RANJEETH B", "Male", "Degree", new(1997, 8, 9), new(2025, 4, 10), "Source date", "Permanent", "Executive", "HR Executive", "HR/Admin Manager", "HR_ADMIN", "CHENNAI", "SESS-020", "SESS-002", "ALL", "Admin"),
        Employee("SESS-021", "NA", "KRISHNAVENI", "Female", "TO_CONFIRM", new(1980, 2, 20), new(2024, 12, 25), "Source date", "Permanent", "Executive", "Housekeeping", "Housekeeping", "HR_ADMIN", "CHENNAI", "SESS-020", "SESS-002", "ALL", "Admin"),
        Employee("SESS-023", "1039", "SARATH BABU K", "Male", "Degree", new(1993, 8, 30), new(2025, 5, 3), "Source date", "Permanent", "Executive", "Production Coordinator", "Production Manager / Incharge", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Production"),
        Employee("SESS-024", "1034", "PRAKASAM B", "Male", "Diploma", new(1976, 1, 3), new(2025, 4, 10), "Source date", "Permanent", "Executive", "Electrical Engineer", "Electrical Engineer", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Electrical"),
        Employee("SESS-025", "1036", "KARTHIKEYAN M.K", "Male", "Degree", new(1992, 6, 5), new(2025, 4, 21), "Source date", "Permanent", "Executive", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-026", "1037", "SRINIVASAN V", "Male", "ITI", new(1992, 1, 22), new(2025, 4, 30), "Source date", "Permanent", "Executive", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-029", "1048", "SRINIVASAN C", "Male", "ITI", new(1979, 3, 29), new(2025, 7, 5), "Source date", "Permanent", "Executive", "Refrigeration Engineer", "Refrigeration Engineer", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration"),
        Employee("SESS-030", "1050", "MANIKANDAN SOKKALINGAM", "Male", "Degree", new(2004, 4, 19), new(2025, 9, 1), "Source date", "Permanent", "Executive", "Electrical Engineer", "Electrical Engineer", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Electrical"),
        Employee("SESS-031", "1053", "VENKAT RAV", "Male", "Degree", new(2004, 4, 11), new(2025, 10, 6), "Source date", "Permanent", "Executive", "Junior Accountant", "Junior Accounts and Service Coordinator", "ACCOUNTS_FINANCE", "CHENNAI", "SESS-007", "SESS-002", "ALL", "Admin"),
        Employee("SESS-033", "1054", "BLESSON PAUL", "Male", "Degree", new(2003, 5, 16), new(2025, 10, 13), "Source date", "Permanent", "Executive", "Junior Engineer", "Service Engineer; no normal approval authority", "SERVICE_TECHNICAL_SUPPORT", "CHENNAI", "SESS-004", "SESS-003", "CHENNAI", "Electrical"),
        Employee("SESS-034", "1062", "MADHAN KUMAR J", "Male", "ITI", new(1992, 5, 10), new(2026, 1, 12), "Source date", "Permanent", "Executive", "Refrigeration Technician", "Refrigeration Technician", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration"),
        Employee("SESS-035", "1038", "VINAYAGAM P", "Male", "ITI", new(1971, 6, 3), new(2025, 5, 2), "Source date", "Permanent", "Executive", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-038", "1058", "SYED IJAZUDDIN Z", "Male", "Degree", new(1994, 5, 7), new(2025, 12, 17), "Source date", "Permanent", "Executive", "PLC Programmer", "Electrical / PLC / Instrumentation Manager", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Programming"),
        Employee("SESS-040", "1065", "NARREN VALENTINO", "Male", "Degree", new(1994, 12, 2), new(2026, 2, 1), "Management confirmed exact date", "Permanent", "Senior Engineer", "Production & Quality Incharge", "Quality/QC Incharge and Primary Manager", "QUALITY_QC", "CHENNAI FACTORY", "SESS-040", "SESS-009", "ALL", "Quality"),
        Employee("SESS-041", "1017", "PARAMESHWARAN S", "Male", "ITI", new(1966, 4, 4), new(2024, 3, 18), "Source date", "Permanent", "TO_CONFIRM", "Fabrication Incharge", "Fabrication Incharge", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-042", "1064", "ILAMPARUTHI D", "Male", "Degree", new(2001, 12, 2), new(2026, 3, 9), "Source date", "Permanent", "TO_CONFIRM", "Jr. Software Developer", "JR. Software Developer", "SOFTWARE_IT", "CHENNAI", "SESS-008", "SESS-049", "ALL", "IT"),
        Employee("SESS-043", "1066", "BHUVANESH M", "Male", "Degree", new(2005, 9, 5), new(2026, 5, 6), "Source date", "Permanent", "TO_CONFIRM", "Refrigeration Technician", "Refrigeration Technician", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration"),
        Employee("SESS-044", "1067", "SUDALAI K", "Male", "Degree", new(1999, 10, 26), new(2026, 5, 7), "Source date", "Permanent", "TO_CONFIRM", "Store Executive", "Store Executive", "STORES", "CHENNAI", "SESS-014", "SESS-012", "ALL", "Purchase & Stores"),
        Employee("SESS-045", "1068", "MOHAMED ASICK", "Male", "Degree", new(2004, 7, 23), new(2026, 5, 7), "Source date", "Permanent", "TO_CONFIRM", "Junior Engineer", "Junior Engineer", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Electrical"),
        Employee("SESS-046", "1069", "BARATH KUMAR D.S", "Male", "Degree", new(1999, 10, 15), new(2026, 5, 11), "Source date", "Permanent", "TO_CONFIRM", "PLC Programmer", "PLC Programmer", "ELECTRICAL_PLC_INSTRUMENTATION", "CHENNAI", "SESS-038", "SESS-001", "ALL", "Programming"),
        Employee("SESS-047", "1070", "PANBARASU G", "Male", "ITI", new(1992, 5, 1), new(2026, 5, 15), "Source date", "Permanent", "TO_CONFIRM", "Refrigeration Engineer", "Refrigeration Engineer", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration"),
        Employee("SESS-048", "1071", "SRINIVASAN R", "Male", "Degree", new(1982, 2, 24), new(2026, 5, 26), "Source date", "Permanent", "TO_CONFIRM", "Fabricator", "Fabricator", "PRODUCTION_FABRICATION", "CHENNAI", "SESS-023", "SESS-040", "ALL", "Fabrication"),
        Employee("SESS-049", "1072", "MAGESHWARI K", "Female", "Degree", new(2002, 4, 21), new(2026, 6, 8), "Source date", "Permanent", "TO_CONFIRM", "Jr. Software Developer", "Software/IT Alternate Manager during approved delegation", "SOFTWARE_IT", "CHENNAI", "SESS-008", "SESS-049", "ALL", "IT"),
        Employee("SESS-050", "1073", "KARTHICK E", "Male", "Degree", new(1996, 3, 26), new(2026, 6, 10), "Source date", "Permanent", "TO_CONFIRM", "Sr. Accountant", "Sr. Accountant", "ACCOUNTS_FINANCE", "CHENNAI", "SESS-007", "SESS-002", "ALL", "Admin"),
        Employee("SESS-051", "1074", "PUSHPARAJ P", "Male", "ITI", new(1985, 5, 24), new(2026, 6, 10), "Source date", "Permanent", "TO_CONFIRM", "Refrigeration Engineer", "Refrigeration Engineer", "REFRIGERATION_MECHANICAL", "CHENNAI", "SESS-003", "SESS-004", "ALL", "Refrigeration")
    ];

    public static IReadOnlyList<DepartmentManagerMappingDecision> ManagerMappings =>
    [
        Mapping("MAP-01", "PURCHASE", "ALL", "SESS-012", "SESS-014", "Confirmed by management"),
        Mapping("MAP-02", "STORES", "ALL", "SESS-014", "SESS-012", "Confirmed by management"),
        Mapping("MAP-03", "ACCOUNTS_FINANCE", "ALL", "SESS-007", "SESS-002", "MD alternate; duplicate-person approval prohibited"),
        Mapping("MAP-04", "HR_ADMIN", "ALL", "SESS-020", "SESS-002", "MD alternate; duplicate-person approval prohibited"),
        Mapping("MAP-05", "PRODUCTION_FABRICATION", "ALL", "SESS-023", "SESS-040", "Confirmed by management"),
        Mapping("MAP-06", "DESIGN", "REGULAR_PRODUCT", "SESS-015", "SESS-019", "Scope-based mapping"),
        Mapping("MAP-07", "DESIGN", "PROJECT", "SESS-019", "SESS-015", "Scope-based mapping"),
        Mapping("MAP-08", "ELECTRICAL_PLC_INSTRUMENTATION", "ALL", "SESS-038", "SESS-001", "TD alternate; count once at highest applicable stage"),
        Mapping("MAP-09", "REFRIGERATION_MECHANICAL", "ALL", "SESS-003", "SESS-004", "Confirmed by management"),
        Mapping("MAP-10", "SERVICE_TECHNICAL_SUPPORT", "CHENNAI", "SESS-004", "SESS-003", "Location-scope mapping"),
        Mapping("MAP-11", "SERVICE_TECHNICAL_SUPPORT", "BANGALORE", "SESS-011", "SESS-004", "Location-scope mapping"),
        Mapping("MAP-12", "SOFTWARE_IT", "ALL", "SESS-008", "SESS-049", "Mageshwari mapped from updated employee list"),
        Mapping("MAP-13", "QUALITY_QC", "ALL", "SESS-040", "SESS-009", "Alternate active only during approved delegation"),
        Mapping("MAP-14", "MANAGEMENT", "ALL", "SESS-002", "SESS-001", "Special management route; self-approval prohibited")
    ];

    public static IReadOnlyList<ApprovalWorkflowDecision> ApprovalWorkflow =>
    [
        new("MANAGER_ONLY", 0m, 50000m, "Department Manager", null, null),
        new("MANAGER_MD", 50000.01m, 500000m, "Department Manager", "SESS-002", null),
        new("MANAGER_MD_TD", 500000.01m, null, "Department Manager", "SESS-002", "SESS-001")
    ];

    public static IReadOnlyList<RelievedEmployeeDecision> RelievedEmployees =>
    [
        Relieved("SESS-016", "KALIDOSS"),
        Relieved("SESS-018", "A. VINAYA SAGAR ARKATI"),
        Relieved("SESS-022", "KARTHICK.B"),
        Relieved("SESS-027", "SANJAY SARAVANAN"),
        Relieved("SESS-028", "PRAVEEN KUMAR.M"),
        Relieved("SESS-032", "PRASANNA.G"),
        Relieved("SESS-036", "FRANCIS XAVIER"),
        Relieved("SESS-037", "DEVANAND B"),
        Relieved("SESS-039", "THIRUNAVUKKARASU")
    ];

    public static IReadOnlyList<DataQualityDecision> DataQualityItems =>
    [
        new("DQ-001", "SESS-021", "Payroll Employee ID", "Source workbook contains NA", "Assign unique payroll ID through HR; ERP code remains SESS-021", "OPEN", "HR"),
        new("DQ-002", "SESS-009 / SESS-030", "Identity", "Two Manikandan records share the same DOB in source", "Distinguished using Payroll IDs 1010 and 1050; verify legal name/DOB privately", "OPEN", "HR"),
        new("DQ-003", "SESS-015 / SESS-019", "Confidential statutory identifier", "Source workbook contains a duplicate confidential statutory identifier across two employees", "Correct in confidential HR source; sensitive identifier is excluded from this ERP-ready workbook", "OPEN", "HR"),
        new("DQ-004", "SESS-040", "Joining Date", "Management confirmed exact joining date", "DOJ set to 2026-02-01 and approximate-date flag must remain false", "RESOLVED", "Management"),
        new("DQ-005", "SESS-040", "Employee Name", "Management confirmed NARREN VALENTINO", "NARREN VALENTINO retained as approved display name", "RESOLVED", "Management"),
        new("DQ-006", "SESS-049", "Gender", "Source workbook marked Mageshwari as Male", "Corrected to Female by management", "RESOLVED", "Management"),
        new("DQ-007", "SESS-001 / SESS-002", "DOB / DOJ", "Source workbook has blank DOB and DOJ", "Complete through confidential HR verification", "OPEN", "HR"),
        new("DQ-008", "SESS-041 to SESS-051", "Grade", "Grade not provided in source", "Set to TO_CONFIRM; do not infer from designation", "OPEN", "HR"),
        new("DQ-009", "Most employees", "Work Location", "Only Pune, Bangalore and Narren Chennai Factory were explicitly confirmed", "CHENNAI used as operational default; verify exceptions", "OPEN", "Management")
    ];

    private static ActiveEmployeeDecision Employee(string code, string payrollEmployeeId, string name, string gender, string qualification, DateOnly? dob, DateOnly? doj, string dojAccuracy, string employmentType, string grade, string hrDesignation, string responsibility, string departmentCode, string workLocation, string primaryApprover, string alternateApprover, string managerScope, string legacyDepartment) =>
        new(code, payrollEmployeeId, name, gender, qualification, dob, doj, dojAccuracy, employmentType, grade, hrDesignation, responsibility, departmentCode, workLocation, "ACTIVE", primaryApprover, alternateApprover, managerScope, legacyDepartment, SourceWorkbook);

    private static DepartmentManagerMappingDecision Mapping(string code, string departmentCode, string scope, string primary, string alternate, string note) =>
        new(code, departmentCode, scope, primary, alternate, 50000m, EffectiveFrom, true, note);

    private static RelievedEmployeeDecision Relieved(string code, string name) =>
        new(code, name, "LEFT / RESIGNED", null, "Not present in updated Employee Master; management confirmed no longer working", "Retain history; do not hard-delete");
}

public sealed record DepartmentDecision(string Code, string Name, bool IsActive = true, string ChangeRule = "No overwrite; retain department transfer history");

public sealed record ActiveEmployeeDecision(
    string EmployeeCode,
    string PayrollEmployeeId,
    string EmployeeName,
    string Gender,
    string Qualification,
    DateOnly? DateOfBirth,
    DateOnly? DateOfJoining,
    string DateOfJoiningAccuracy,
    string EmploymentType,
    string Grade,
    string HrDesignation,
    string FunctionalResponsibility,
    string FinalDepartmentCode,
    string WorkLocation,
    string EmployeeStatus,
    string PrPrimaryApproverCode,
    string PrAlternateApproverCode,
    string ManagerScope,
    string LegacyDepartment,
    string SourceAuditNote);

public sealed record DepartmentManagerMappingDecision(
    string MappingCode,
    string DepartmentCode,
    string Scope,
    string PrimaryManagerCode,
    string AlternateManagerCode,
    decimal ManagerLimit,
    DateOnly EffectiveFrom,
    bool IsActive,
    string ControlNote);

public sealed record ApprovalWorkflowDecision(
    string Route,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    string Step1,
    string? Step2,
    string? Step3,
    string SelfApproval = "BLOCK",
    string SameUserTwice = "BLOCK",
    string MissingMappingStatus = Rev868C3EmployeeWorkbookData.PendingApproverMappingStatus,
    string AuditHistory = "Required");

public sealed record RelievedEmployeeDecision(
    string EmployeeCode,
    string EmployeeName,
    string Status,
    DateOnly? EffectiveDate,
    string Basis,
    string RetentionRule);

public sealed record DataQualityDecision(
    string IssueId,
    string EmployeeCodes,
    string Field,
    string Finding,
    string ActionOrDecision,
    string Status,
    string Owner);
