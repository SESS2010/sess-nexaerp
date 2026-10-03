using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task R1DesignEngineerEstimatedBomGrantReadback()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("r1-design-grants-fresh.sql", model.GetService<IMigrator>()
            .GenerateScript("0", "20261003090000_R1RosterSeedReconciliation"));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        var permissionService = new EfPagePermissionService(db);
        var cutoff = new DateOnly(2026, 10, 10);
        foreach (var companyCode in new[] { "SESS_PVT_LTD", "SESS_PROPRIETORSHIP" })
        {
            var company = await db.Companies.SingleAsync(x => x.Code == companyCode);
            foreach (var employeeCode in new[] { "SESS-17", "SESS-19" })
            {
                var employee = await db.Employees.SingleAsync(x => x.EmployeeCode == employeeCode);
                var assignments = await db.EmployeeRoleAssignments.AsNoTracking().Include(x => x.Role)
                    .Where(x => x.CompanyId == company.Id && x.EmployeeId == employee.Id
                        && (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved")
                        && x.EffectiveFrom <= cutoff && (x.EffectiveTo == null || x.EffectiveTo >= cutoff)
                        && x.Role!.IsActive && db.CompanyRoleActivations.Any(a => a.CompanyId == company.Id
                            && a.RoleId == x.RoleId && a.IsEnabled && a.EffectiveFrom <= cutoff
                            && (a.EffectiveTo == null || a.EffectiveTo >= cutoff))).ToListAsync();
                Assert.Equal(new[] { "DESIGN_ENGINEER", "SERVICE_ENGINEER" }, assignments.Select(x => x.Role!.Code).Order());
                Assert.All(assignments, x => Assert.Equal("FULL", x.AssignmentType));
                var permissions = await permissionService.ResolveEffectivePermissionsAsync(assignments.Select(x =>
                    new EffectiveRoleAssignment(x.Id, x.Role!.Code, x.AssignmentType)).ToArray(), company.Code, employee.Id, default);
                foreach (var action in new[] { "view", "create", "update", "submit", "download" })
                    Assert.Contains($"design.estimated-bom:{action}", permissions);
                Assert.DoesNotContain("design.estimated-bom:approve", permissions);
            }
        }
        Assert.True(await permissionService.HasPermissionAsync(new[] { "TECHNICAL_DIRECTOR" },
            "design.estimated-bom", "Approve", default));
        Assert.False(await permissionService.HasPermissionAsync(new[] { "MANAGING_DIRECTOR" },
            "design.estimated-bom", "Approve", default));
    }
}