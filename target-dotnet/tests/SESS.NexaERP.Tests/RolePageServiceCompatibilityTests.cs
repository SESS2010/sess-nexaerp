using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class RolePageServiceCompatibilityTests
{
    [Fact]
    public void Page_grants_never_advertise_a_role_the_service_will_refuse()
    {
        var failures = new[]
        {
            RejectedGrantMessage("settings.tax-gst", p => p.CanCreate, OperationRoleContracts.TaxGstCreate),
            RejectedGrantMessage("purchase.technical-verification", p => p.CanVerify, OperationRoleContracts.TechnicalVerification)
        }.Where(x => x is not null);
        Assert.True(!failures.Any(), string.Join("; ", failures));
    }

    [Fact]
    public void Tax_creation_has_a_permission_compatible_service_role()
    {
        var creators = GrantedRoles("settings.tax-gst", p => p.CanCreate);
        Assert.Contains("ACCOUNTS_MANAGER", creators);
    }

    private static string? RejectedGrantMessage(
        string pageKey,
        Func<RolePagePermission, bool> actionGrant,
        IReadOnlyCollection<string> serviceRoles)
    {
        var rejected = GrantedRoles(pageKey, actionGrant)
            .Except(serviceRoles, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return rejected.Length == 0 ? null :
            $"{pageKey} advertises the action to roles the service refuses: {string.Join(", ", rejected)}";
    }

    private static string[] GrantedRoles(string pageKey, Func<RolePagePermission, bool> actionGrant)
    {
        var page = FoundationSeedData.Pages.Concat(Rev869ASeedData.Pages).Concat(Rev869BSeedData.Pages)
            .Single(x => x.PageKey == pageKey);
        var roles = FoundationSeedData.Roles
            .Concat(Rev866SeedData.AdditionalEmployeeRoles)
            .Concat(Rev869ASeedData.Roles)
            .Concat(MultiCompanyEmployeeAuthorizationPart1SeedData.Roles)
            .Append(AdvanceSeedData.DepartmentManagerRole)
            .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => Rev869ARoleCodes.Normalize(x.First().Code));
        return AdvanceSeedData.RolePagePermissions
            .Where(x => x.PageDefinitionId == page.Id && (actionGrant(x) || x.HasFullControl))
            .Select(x => roles[x.RoleId])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}