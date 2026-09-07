using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class RoleGovernanceFoundationTests
{
    [Fact]
    public void Catalogue_has_51_classified_roles()
    {
        RoleGovernanceSeedData.ApplyToKnownRoles();
        var roles = RoleGovernanceSeedData.KnownRoles.ToDictionary(role => role.Code, StringComparer.Ordinal);
        Assert.Equal(51, roles.Count);
        Assert.Equal(40, roles.Values.Count(role => role.Audience == RoleAudiences.InternalEmployee));
        Assert.Equal(8, roles.Values.Count(role => role.Audience == RoleAudiences.LegacyAlias));
        Assert.Equal(2, roles.Values.Count(role => role.Audience == RoleAudiences.ExternalPortal));
        Assert.Single(roles.Values, role => role.Audience == RoleAudiences.SystemSecurity);
    }

    [Fact]
    public void Legacy_aliases_have_exact_canonical_replacements()
    {
        RoleGovernanceSeedData.ApplyToKnownRoles();
        var roles = RoleGovernanceSeedData.KnownRoles.ToDictionary(role => role.Code, StringComparer.Ordinal);
        var replacements = roles.Values.Where(role => role.ReplacementRoleId.HasValue)
            .ToDictionary(role => role.Code, role => roles.Values.Single(candidate => candidate.Id == role.ReplacementRoleId).Code);
        Assert.Equal(new Dictionary<string, string>
        {
            ["ACCOUNTS_HEAD"] = "ACCOUNTS_MANAGER",
            ["DCC"] = "DOCUMENT_CONTROLLER",
            ["MD"] = "MANAGING_DIRECTOR",
            ["PRODUCTION_HEAD"] = "PRODUCTION_MANAGER",
            ["PURCHASE_HEAD"] = "PURCHASE_MANAGER",
            ["QC_HEAD"] = "QC_MANAGER",
            ["SOFTWARE_ENGINEER"] = "SOFTWARE_DEVELOPER",
            ["STORE_HEAD"] = "STORES_MANAGER"
        }, replacements);
    }

    [Fact]
    public void Both_companies_receive_51_rows_while_new_roles_receive_no_authority()
    {
        RoleGovernanceSeedData.ApplyToKnownRoles();
        var rows = RoleGovernanceSeedData.CompanyRoleActivations;
        Assert.Equal(102, rows.Count);
        var companies = rows.GroupBy(row => row.CompanyId).ToArray();
        Assert.Equal(2, companies.Length);
        Assert.All(companies, company => Assert.Equal(51, company.Count()));
        Assert.All(companies, company => Assert.Equal(42, company.Count(row => row.IsEnabled)));

        var newRoleIds = RoleGovernanceSeedData.AdditionalRoles.Select(role => role.Id).ToHashSet();
        Assert.Equal(["DISPATCH_COORDINATOR", "HOUSEKEEPING_ASSISTANT", "HR_MANAGER", "MAINTENANCE_ENGINEER", "PROJECT_MANAGER", "SITE_ENGINEER"],
            RoleGovernanceSeedData.AdditionalRoles.Select(role => role.Code).Order().ToArray());
        Assert.DoesNotContain(AdvanceSeedData.RolePagePermissions, row => newRoleIds.Contains(row.RoleId));
        Assert.DoesNotContain(Rev866SeedData.EmployeeRoleAssignments, row => newRoleIds.Contains(row.RoleId));
    }

    [Fact]
    public void Model_enforces_company_role_uniqueness_and_role_governance_checks()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var activation = db.Model.FindEntityType(typeof(CompanyRoleActivation))!;
        Assert.Contains(activation.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(["CompanyId", "RoleId", "EffectiveFrom"]));
        var role = db.Model.FindEntityType(typeof(Role))!;
        Assert.Contains(role.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Role) &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual(["ReplacementRoleId"]));
    }

    [Fact]
    public void Update_contracts_have_readable_version_and_governance_inputs()
    {
        var roleFields = typeof(RoleSummary).GetProperties().Select(property => property.Name).ToHashSet();
        foreach (var field in new[] { "Audience", "BusinessArea", "IsEmployeeAssignable", "ReplacementRoleCode", "Version" })
            Assert.Contains(field, roleFields);
        var activationFields = typeof(CompanyRoleActivationSummary).GetProperties().Select(property => property.Name).ToHashSet();
        foreach (var field in new[] { "RoleCode", "IsEnabled", "EffectiveFrom", "EffectiveTo", "Remarks", "Version" })
            Assert.Contains(field, activationFields);
    }
}
