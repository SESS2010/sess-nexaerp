using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C3EmployeeWorkbookDataTests
{
    [Fact]
    public void Rev868c3_workbook_counts_match_management_approved_summary()
    {
        Assert.Equal(42, Rev868C3EmployeeWorkbookData.ActiveEmployees.Count);
        Assert.Equal(12, Rev868C3EmployeeWorkbookData.Departments.Count);
        Assert.Equal(14, Rev868C3EmployeeWorkbookData.ManagerMappings.Count);
        Assert.Equal(9, Rev868C3EmployeeWorkbookData.RelievedEmployees.Count);
        Assert.Equal(9, Rev868C3EmployeeWorkbookData.DataQualityItems.Count);
        Assert.Equal(new DateOnly(2026, 8, 9), Rev868C3EmployeeWorkbookData.EffectiveFrom);
    }

    [Fact]
    public void Rev868c3_clean_departments_replace_legacy_mixed_import_categories()
    {
        var departmentCodes = Rev868C3EmployeeWorkbookData.Departments.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ENGINEER_TECHNICAL", departmentCodes);
        Assert.DoesNotContain("JUNIOR_ASSISTANT", departmentCodes);
        Assert.DoesNotContain("ADMIN_ACCOUNTS_STORES", departmentCodes);
        Assert.DoesNotContain("MANAGER", departmentCodes);
        Assert.Contains("QUALITY_QC", departmentCodes);

        var invalidEmployeeDepartments = Rev868C3EmployeeWorkbookData.ActiveEmployees
            .Where(x => !departmentCodes.Contains(x.FinalDepartmentCode))
            .Select(x => $"{x.EmployeeCode}:{x.FinalDepartmentCode}")
            .ToList();

        Assert.Empty(invalidEmployeeDepartments);
    }

    [Fact]
    public void Rev868c3_manager_mappings_reference_active_employees_and_clean_departments()
    {
        var activeCodes = Rev868C3EmployeeWorkbookData.ActiveEmployees.Select(x => x.EmployeeCode).ToHashSet(StringComparer.Ordinal);
        var departmentCodes = Rev868C3EmployeeWorkbookData.Departments.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);

        foreach (var mapping in Rev868C3EmployeeWorkbookData.ManagerMappings)
        {
            Assert.Contains(mapping.DepartmentCode, departmentCodes);
            Assert.Contains(mapping.PrimaryManagerCode, activeCodes);
            Assert.Contains(mapping.AlternateManagerCode, activeCodes);
            Assert.Equal(50000m, mapping.ManagerLimit);
            Assert.True(mapping.IsActive);
        }

        Assert.Contains(Rev868C3EmployeeWorkbookData.ManagerMappings, x => x.DepartmentCode == "DESIGN" && x.Scope == "REGULAR_PRODUCT");
        Assert.Contains(Rev868C3EmployeeWorkbookData.ManagerMappings, x => x.DepartmentCode == "DESIGN" && x.Scope == "PROJECT");
        Assert.Contains(Rev868C3EmployeeWorkbookData.ManagerMappings, x => x.DepartmentCode == "SERVICE_TECHNICAL_SUPPORT" && x.Scope == "CHENNAI");
        Assert.Contains(Rev868C3EmployeeWorkbookData.ManagerMappings, x => x.DepartmentCode == "SERVICE_TECHNICAL_SUPPORT" && x.Scope == "BANGALORE");
    }

    [Fact]
    public void Rev868c3_relieved_employees_are_not_in_active_working_list()
    {
        var activeCodes = Rev868C3EmployeeWorkbookData.ActiveEmployees.Select(x => x.EmployeeCode).ToHashSet(StringComparer.Ordinal);

        foreach (var relieved in Rev868C3EmployeeWorkbookData.RelievedEmployees)
        {
            Assert.DoesNotContain(relieved.EmployeeCode, activeCodes);
            Assert.Equal("LEFT / RESIGNED", relieved.Status);
            Assert.Contains("do not hard-delete", relieved.RetentionRule, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Rev868c3_approval_workflow_matches_manager_md_td_chain()
    {
        var routes = Rev868C3EmployeeWorkbookData.ApprovalWorkflow;

        Assert.Collection(
            routes,
            managerOnly =>
            {
                Assert.Equal("MANAGER_ONLY", managerOnly.Route);
                Assert.Equal(0m, managerOnly.MinimumAmount);
                Assert.Equal(50000m, managerOnly.MaximumAmount);
                Assert.Equal("Department Manager", managerOnly.Step1);
                Assert.Null(managerOnly.Step2);
                Assert.Null(managerOnly.Step3);
            },
            managerMd =>
            {
                Assert.Equal("MANAGER_MD", managerMd.Route);
                Assert.Equal(50000.01m, managerMd.MinimumAmount);
                Assert.Equal(500000m, managerMd.MaximumAmount);
                Assert.Equal("Department Manager", managerMd.Step1);
                Assert.Equal("SESS-002", managerMd.Step2);
                Assert.Null(managerMd.Step3);
            },
            managerMdTd =>
            {
                Assert.Equal("MANAGER_MD_TD", managerMdTd.Route);
                Assert.Equal(500000.01m, managerMdTd.MinimumAmount);
                Assert.Null(managerMdTd.MaximumAmount);
                Assert.Equal("Department Manager", managerMdTd.Step1);
                Assert.Equal("SESS-002", managerMdTd.Step2);
                Assert.Equal("SESS-001", managerMdTd.Step3);
            });

        Assert.All(routes, route =>
        {
            Assert.Equal("BLOCK", route.SelfApproval);
            Assert.Equal("BLOCK", route.SameUserTwice);
            Assert.Equal("PendingApproverMapping", route.MissingMappingStatus);
            Assert.Equal("Required", route.AuditHistory);
        });
    }

    [Fact]
    public void Rev868c3_preserves_confidential_data_boundaries()
    {
        Assert.All(Rev868C3EmployeeWorkbookData.ActiveEmployees, employee =>
        {
            Assert.DoesNotContain("PAN", employee.SourceAuditNote, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AADHAAR", employee.SourceAuditNote, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BANK", employee.SourceAuditNote, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Contains(Rev868C3EmployeeWorkbookData.DataQualityItems, x => x.IssueId == "DQ-003" && x.ActionOrDecision.Contains("PAN is excluded", StringComparison.OrdinalIgnoreCase));
    }
}
