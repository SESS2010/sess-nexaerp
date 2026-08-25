using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class ApprovalConfigurationPart2Tests
{
    private const string MigrationId = "20260825135023_ApprovalConfigurationAndPermissionsPart2";

    [Fact]
    public void MigrationIsGuardedReversibleAndContainsTheSettledEffectiveDatedManifest()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationId + ".cs");
        var sql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "ApprovalConfigurationAndPermissionsPart2Sql.cs");

        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Contains("nullable: true", migration);
        Assert.Contains("name: \"ApproverRoleCode\"", migration);
        Assert.Contains("4999.999999", sql);
        Assert.Contains("5000.000000", sql);
        Assert.Contains("100000.000001", sql);
        Assert.Contains("4999.99", sql);
        Assert.Contains("5000.00", sql);
        Assert.Contains("100000.01", sql);
        Assert.Contains("('DEPARTMENT_ONLY',0.00::numeric,4999.99::numeric,1,'DEPARTMENT_MAPPING'", sql);
        Assert.Contains("('DEPARTMENT_THEN_TD',5000.00::numeric,100000.00::numeric,2,'CONFIGURED_ROLE','SESS-01','TECHNICAL_DIRECTOR')", sql);
        Assert.Contains("('DEPARTMENT_THEN_MD',100000.01::numeric,NULL::numeric,2,'CONFIGURED_ROLE','SESS-02','MANAGING_DIRECTOR')", sql);
        Assert.Contains("IF (SELECT count(*) FROM __advance_schema__.department_approval_mappings WHERE \"CreatedBy\"='APPROVAL_CONFIGURATION_PART2' AND \"IsActive\")<>42", sql);
        Assert.Contains("\"EffectiveTo\"=DATE '2026-08-26'", sql);
        Assert.Contains("DELETE FROM __advance_schema__.department_approval_mappings WHERE \"CreatedBy\"='APPROVAL_CONFIGURATION_PART2'", sql);
    }

    [Fact]
    public void ManagerPermissionsAreNarrowAndPurchaseManagerCanNeverApprove()
    {
        var permissions = AdvanceSeedData.RolePagePermissions;
        var pages = FoundationSeedData.Pages.Concat(Rev869BSeedData.Pages).ToDictionary(x => x.Id, x => x.PageKey);
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
            .ToDictionary(x => x.Id, x => x.Code);
        roles[Guid.Parse("83000000-0000-0000-0000-000000000001")] = "PRODUCTION_MANAGER";
        roles[Guid.Parse("83000000-0000-0000-0000-000000000002")] = "ACCOUNTS_MANAGER";
        roles[AdvanceSeedData.DepartmentManagerRole.Id] = AdvanceSeedData.DepartmentManagerRole.Code;

        foreach (var manager in new[] { "PRODUCTION_MANAGER", "ACCOUNTS_MANAGER" })
        {
            var rows = permissions.Where(x => roles.TryGetValue(x.RoleId, out var code) && code == manager).ToArray();
            Assert.Equal(new[] { "purchase.commercial-comparisons", "purchase.po", "purchase.requisition-approvals" },
                rows.Select(x => pages[x.PageDefinitionId]).OrderBy(x => x).ToArray());
            Assert.All(rows, row =>
            {
                Assert.True(row.CanView && row.CanApprove && row.CanReject && row.CanRequestRevision &&
                            row.CanViewCommercialValues && row.CanViewAuditHistory);
                Assert.False(row.CanCreate || row.CanUpdate || row.CanSubmit || row.CanIssue || row.CanVerify ||
                             row.CanRequestClarification || row.CanResubmit || row.CanCancel || row.CanDeactivate ||
                             row.CanPrint || row.CanDownload || row.CanExport || row.CanUploadAttachment ||
                             row.CanReplaceAttachment || row.HasFullControl);
            });
        }

        var purchaseManagerRows = permissions.Where(x => roles.TryGetValue(x.RoleId, out var code) &&
                                                          code == "PURCHASE_MANAGER" &&
                                                          pages.ContainsKey(x.PageDefinitionId)).ToArray();
        Assert.Equal(2, purchaseManagerRows.Length);
        Assert.All(purchaseManagerRows, row =>
            Assert.False(row.CanApprove || row.CanReject || row.CanRequestRevision || row.CanCancel ||
                         row.CanVerify || row.HasFullControl));
        var comparison = purchaseManagerRows.Single(x => pages[x.PageDefinitionId] == "purchase.commercial-comparisons");
        Assert.True(comparison.CanView && comparison.CanCreate && comparison.CanSubmit && comparison.CanResubmit &&
                    comparison.CanViewCommercialValues);
        Assert.False(comparison.CanUpdate || comparison.CanIssue);
        var po = purchaseManagerRows.Single(x => pages[x.PageDefinitionId] == "purchase.po");
        Assert.True(po.CanView && po.CanCreate && po.CanUpdate && po.CanSubmit && po.CanIssue && po.CanViewCommercialValues);
        Assert.False(po.CanResubmit);
    }

    [Fact]
    public void DatabaseAndApplicationAuthoritySourcesCannotResurrectPurchaseManagerApproval()
    {
        var controlled = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BControlledMutationSql.cs");
        controlled = controlled[controlled.IndexOf("private const string InstallTemplate", StringComparison.Ordinal)..];
        var permissionService = Read("src", "SESS.NexaERP.Infrastructure", "Authorization", "EfPagePermissionService.cs");
        var service = Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs");

        Assert.DoesNotContain("IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER') AND EXISTS", controlled);
        Assert.DoesNotContain("\"ApprovalRoute\"='MANAGER' AND NEW.\"ActorRoleCode\" IN ('PURCHASE_MANAGER','DEPARTMENT_MANAGER')", controlled);
        Assert.DoesNotContain("r.\"Code\" IN ('PURCHASE_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR')", controlled);
        Assert.Contains("IN ('PRODUCTION_MANAGER','ACCOUNTS_MANAGER')", controlled);
        Assert.Contains("\"purchase.requisition-approvals\"", permissionService);
        Assert.Contains("mappings[0].ApproverRoleCode", service);
    }

    [Fact]
    public void QcManagerHasNoApprovalGrant()
    {
        var role = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
            .Single(x => x.Code == "QC_MANAGER");
        var rows = AdvanceSeedData.RolePagePermissions.Where(x => x.RoleId == role.Id).ToArray();
        Assert.All(rows, row => Assert.False(row.CanApprove || row.CanReject || row.CanRequestRevision || row.HasFullControl));
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { FindRoot() }.Concat(parts).ToArray()));
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
