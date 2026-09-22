using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task SessionProjectionExactlyMatchesEndpointPolicyForEveryActiveRole()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("session-permission-parity-up.sql", migrator.GenerateScript("0", model.Database.GetMigrations().Last()));

        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        var service = new EfPagePermissionService(db);
        var roles = await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync();
        var grants = await db.RolePagePermissions.AsNoTracking().Include(x => x.Role).Include(x => x.PageDefinition)
            .Where(x => x.Role != null && x.Role.IsActive && x.PageDefinition != null && x.PageDefinition.IsActive)
            .ToListAsync();

        foreach (var role in roles)
        foreach (var assignmentType in new[] { "FULL", "SUPPORT", "TEMPORARY" })
        {
            var assignment = new EffectiveRoleAssignment(Guid.NewGuid(), role.Code, assignmentType);
            var projected = await service.ResolveEffectivePermissionsAsync([assignment], "SESS_PVT_LTD", Guid.Empty, default);
            var expected = new HashSet<string>(RoleAuthorityResolution.UniversalEmployeePermissions, StringComparer.Ordinal);
            foreach (var grant in grants.Where(x => x.RoleId == role.Id))
            foreach (var action in EfPagePermissionService.SupportedActions)
            {
                var permission = $"{grant.PageDefinition!.PageKey}:{action}";
                if (EfPagePermissionService.RoleGrantAllows(grant, grant.PageDefinition.PageKey, action) &&
                    RoleAuthorityResolution.CanAssignmentExercise(assignmentType, permission))
                    expected.Add(permission);
            }
            Assert.Equal(expected.Order(StringComparer.Ordinal), projected);
        }

        var itManager = roles.Single(x => x.Code == "IT_MANAGER");
        var itPermissions = await service.ResolveEffectivePermissionsAsync(
            [new EffectiveRoleAssignment(Guid.NewGuid(), itManager.Code, "FULL")], "SESS_PVT_LTD", Guid.Empty, default);
        Assert.Contains("security.employee-identities:view", itPermissions);
        Assert.Contains("security.employee-identities:create", itPermissions);
    }
}