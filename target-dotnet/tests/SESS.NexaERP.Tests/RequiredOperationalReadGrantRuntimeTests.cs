using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private static async Task ProveRequiredReadGrantMigration(DisposablePostgreSql server,
        DbContextOptions<NexaErpDbContext> options, IMigrator migrator, string[] migrations)
    {
        const string target = "20260918103000_RequiredOperationalReadGrants";
        var index = Array.IndexOf(migrations, target);
        Assert.True(index > 0);
        var down = migrator.GenerateScript(target, migrations[index - 1]);
        var up = migrator.GenerateScript(migrations[index - 1], target);
        await using var db = new NexaErpDbContext(options);
        var role = await db.Roles.Where(x => x.Code == "TECHNICAL_DIRECTOR").Select(x => x.Id).SingleAsync();
        var page = await db.PageDefinitions.Where(x => x.PageKey == "production.job-orders").Select(x => x.Id).SingleAsync();
        var permission = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.RoleId == role && x.PageDefinitionId == page);
        Assert.True(permission.CanView);
        Assert.False(permission.CanCreate || permission.CanUpdate || permission.CanApprove || permission.CanViewCommercialValues);
        server.Execute("required-read-grant-drift.sql", $"UPDATE advance.role_page_permissions SET \"CanPrint\"=NOT \"CanPrint\" WHERE \"Id\"='{permission.Id:D}';");
        server.AssertRejected("required-read-grant-refuse-drift.sql", down, "rollback refuses a changed or missing permission");
        server.Execute("required-read-grant-restore-drift.sql", $"UPDATE advance.role_page_permissions SET \"CanPrint\"=NOT \"CanPrint\" WHERE \"Id\"='{permission.Id:D}';");
        server.Execute("required-read-grant-down.sql", down);
        server.Execute("required-read-grant-up.sql", up);
        server.Execute("required-read-grant-down-for-site-grant.sql", down);
        permission.CanPrint = true;
        permission.CreatedBy = "SITE_PERMISSION_WITNESS";
        var siteJson = JsonSerializer.Serialize(permission).Replace("'", "''", StringComparison.Ordinal);
        server.Execute("required-read-site-grant.sql", "INSERT INTO advance.role_page_permissions SELECT (jsonb_populate_record(NULL::advance.role_page_permissions, '" + siteJson + "'::jsonb)).*;");
        server.Execute("required-read-preserve-site-up.sql", up);
        server.Execute("required-read-preserve-site-down.sql", down);
        var preserved = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.Id == permission.Id);
        Assert.True(preserved.CanView && preserved.CanPrint);
        Assert.Equal("SITE_PERMISSION_WITNESS", preserved.CreatedBy);
        server.Execute("required-read-remove-site-fixture.sql", $"DELETE FROM advance.role_page_permissions WHERE \"Id\"='{permission.Id:D}' AND \"CreatedBy\"='SITE_PERMISSION_WITNESS';");
        server.Execute("required-read-final-up.sql", up);
    }

    private static async Task ProveRequiredActorLookups(HttpClient client, DbContextOptions<NexaErpDbContext> options, TaxWorkflowUser user, Guid purchaseId, Guid tdId)
    {
        foreach (var role in new[] { "PURCHASE_EXECUTIVE", "PURCHASE_MANAGER" })
        {
            user.Set(purchaseId, "SESS-15", role);
            Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
            await AssertResolvedSeedRole(options, user, role);
            var vendors = await Get<PagedResponse<VendorSummary>>(client, "/api/v1/masters/vendors?search=TRIAL-VEN-001");
            Assert.Contains(vendors.Items, x => x.VendorCode == "TRIAL-VEN-001" && x.Id != Guid.Empty);
            if (role == "PURCHASE_EXECUTIVE") Assert.All(vendors.Items, x => Assert.Null(x.BankMetadata));
        }
        user.Set(tdId, "SESS-01", "TECHNICAL_DIRECTOR");
        Assert.NotEqual(Guid.Empty, Assert.Single(user.EffectiveRoleAssignments).AssignmentId);
        await AssertResolvedSeedRole(options, user, "TECHNICAL_DIRECTOR");
        var jobs = await Get<PagedResponse<JobOrderSummary>>(client, "/api/v1/production/job-orders?search=WITNESS-MACHINE-001");
        var job = Assert.Single(jobs.Items);
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(job.Id, (await Get<JobOrderView>(client, "/api/v1/production/job-orders/" + job.Id)).Id);
    }
}
