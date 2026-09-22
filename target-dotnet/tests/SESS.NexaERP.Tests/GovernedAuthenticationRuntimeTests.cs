using System.Reflection;
using Npgsql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Identity;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task GovernedAuthenticationCreatesRevokesAndRerunsWithoutDuplicatingAuthority()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var scriptDb = new NexaErpDbContext(options);
        var migrator = scriptDb.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("identity-schema.sql", migrator.GenerateScript("0", scriptDb.Database.GetMigrations().Last()));
        var migrationNames = scriptDb.Database.GetMigrations().ToArray();
        var identityMigrationIndex = Array.IndexOf(migrationNames, "20260912130000_GovernedAuthenticationRuntime");
        server.Execute("identity-down.sql", migrator.GenerateScript(migrationNames[identityMigrationIndex], migrationNames[identityMigrationIndex - 1]));
        server.Execute("identity-reapply.sql", migrator.GenerateScript(migrationNames[identityMigrationIndex - 1], migrationNames[identityMigrationIndex]));
        using (var environment = new EnvironmentVariables(
            ("ConnectionStrings__NexaErpInstaller", server.ConnectionString),
            ("NexaErp__ExpectedDatabase", "advance_parser"),
            ("NEXAERP_MIGRATION_PASSWORD", "Isolated-Item16-Migration-Only!"),
            ("NEXAERP_BOOTSTRAP_PASSWORD", "Isolated-Item16-Bootstrap-Only!"),
            ("NEXAERP_RUNTIME_PASSWORD", "Isolated-Item16-Runtime-Only!")))
        {
            Assert.Equal(0, await InstallerCommand.RunAsync(["database-principals", "provision"]));
            Assert.Equal(0, await InstallerCommand.RunAsync(["database-principals", "status"]));
        }
        server.Execute("identity-bootstrap.sql", """
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),c."Id",c."Code",'urn:nexaerp:development',e."EmployeeCode",e."Id",'HUMAN',
              current_date,true,clock_timestamp(),'ISOLATED_DEVELOPMENT_HISTORY',0
            FROM advance.companies c CROSS JOIN advance.employees e
            WHERE c."Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP')
              AND e."EmployeeCode" IN ('SESS-01','SESS-02','SESS-04','SESS-12','SESS-14','SESS-15',
                'SESS-16','SESS-25','SESS-33','SESS-35','SESS-41');
            GRANT USAGE ON SCHEMA advance TO nexa_erp_bootstrap;
            SET SESSION AUTHORIZATION nexa_erp_bootstrap;
            SELECT advance.govern_authentication_bootstrap('https://identity.example/realm','admin-subject',false,true);
            SELECT advance.govern_authentication_bootstrap('https://identity.example/realm','admin-subject',true,false);
            RESET SESSION AUTHORIZATION;
            DO $assert$
            BEGIN
              IF (SELECT count(*) FROM advance.employee_identity_mappings WHERE "IsActive")<>2 THEN
                RAISE EXCEPTION 'Expected exactly two first-administrator company mappings.';
              END IF;
              IF (SELECT count(*) FROM advance.audit_logs WHERE "Action"='AuthenticationBootstrap')<>2 THEN
                RAISE EXCEPTION 'Rerun duplicated bootstrap audits.';
              END IF;
              IF (SELECT count(*) FROM advance.employee_identity_mappings
                  WHERE "Issuer"='urn:nexaerp:development' AND NOT "IsActive")<>22
                 OR (SELECT count(*) FROM advance.audit_logs WHERE "Action"='DevelopmentIdentityRetired')<>22 THEN
                RAISE EXCEPTION 'Expected all 22 development mappings retained inactive with 22 retirement audits.';
              END IF;
              IF has_function_privilege('nexa_erp_runtime','advance.govern_authentication_bootstrap(text,text,boolean,boolean)','EXECUTE') THEN
                RAISE EXCEPTION 'Runtime can execute bootstrap.';
              END IF;
            END $assert$;
            """);
        server.AssertRejected("identity-replay.sql",
            "SET SESSION AUTHORIZATION nexa_erp_bootstrap; SELECT advance.govern_authentication_bootstrap('https://identity.example/realm','admin-subject',false,true);",
            "explicit --rerun");
        server.AssertRejected("identity-wrong-rerun.sql",
            "SET SESSION AUTHORIZATION nexa_erp_bootstrap; SELECT advance.govern_authentication_bootstrap('https://identity.example/realm','different-subject',true,false);",
            "differs");
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        var employee = await db.Employees.SingleAsync(row => row.EmployeeCode == "SESS-04");
        var admin = await db.Employees.SingleAsync(row => row.EmployeeCode == "SESS-12");
        var user = new IdentityWitnessUser(admin.Id);
        var audit = new EfAuditWriter(db, user);
        var today = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURRENT_DATE AS \"Value\"").SingleAsync();
        var request = new CreateEmployeeIdentityMappingRequest("SESS_PVT_LTD",
            "https://identity.example/realm", "employee-subject", "SESS-04", "HUMAN", today, null, "Witness onboarding");
        var missing = await InvokeIdentity("CreateIdentity", request with { EmployeeCode = "DOES-NOT-EXIST" }, db, user, audit, CancellationToken.None);
        Assert.Equal(409, ((IStatusCodeHttpResult)missing).StatusCode);
        var originalStatus = employee.Status;
        employee.Status = "INACTIVE";
        await db.SaveChangesAsync();
        var inactive = await InvokeIdentity("CreateIdentity", request, db, user, audit, CancellationToken.None);
        Assert.Equal(409, ((IStatusCodeHttpResult)inactive).StatusCode);
        employee.Status = originalStatus;
        await db.SaveChangesAsync();

        var runtimeConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
            { Username = "nexa_erp_runtime" }.ConnectionString;
        await using var runtimeDb = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(runtimeConnection).Options);
        var runtimeAudit = new EfAuditWriter(runtimeDb, user);
        Assert.Equal("nexa_erp_runtime", await runtimeDb.Database
            .SqlQueryRaw<string>("SELECT session_user::text AS \"Value\"").SingleAsync());

        var created = await InvokeIdentity("CreateIdentity", request, runtimeDb, user, runtimeAudit, CancellationToken.None);
        Assert.Equal(201, ((IStatusCodeHttpResult)created).StatusCode);
        var duplicate = await InvokeIdentity("CreateIdentity", request, runtimeDb, user, runtimeAudit, CancellationToken.None);
        Assert.Equal(409, ((IStatusCodeHttpResult)duplicate).StatusCode);
        var mapping = await runtimeDb.EmployeeIdentityMappings.SingleAsync(row => row.Subject == "employee-subject");
        var resolver = new EfEmployeeIdentityResolver(runtimeDb);
        Assert.True((await resolver.ResolveAsync(request.Issuer, request.Subject, request.OrganizationId, today, default)).Success);
        var stale = await InvokeIdentity("RevokeIdentity", mapping.Id, new RevokeEmployeeIdentityRequest(mapping.Version + 1, "Stale witness"),
            runtimeDb, user, runtimeAudit, CancellationToken.None);
        Assert.Equal(409, ((IStatusCodeHttpResult)stale).StatusCode);
        var revoked = await InvokeIdentity("RevokeIdentity", mapping.Id, new RevokeEmployeeIdentityRequest(mapping.Version, "Witness revocation"),
            runtimeDb, user, runtimeAudit, CancellationToken.None);
        Assert.Equal(200, ((IStatusCodeHttpResult)revoked).StatusCode);
        Assert.False((await resolver.ResolveAsync(request.Issuer, request.Subject, request.OrganizationId, today, default)).Success);
        Assert.Equal(1, await runtimeDb.EmployeeIdentityMappings.CountAsync(row => row.Subject == "employee-subject" && !row.IsActive));
        Assert.Equal(1, await runtimeDb.AuditLogs.CountAsync(row => row.Action == "CreateIdentityMapping"));
        Assert.Equal(1, await runtimeDb.AuditLogs.CountAsync(row => row.Action == "RevokeIdentityMapping"));
        Assert.Equal(2, await runtimeDb.ControlledConfigurationHistories.CountAsync(row => row.EntityId == mapping.Id));

        // A failed audit must roll back the mapping and its controlled history.
        runtimeDb.ChangeTracker.Clear();
        var failingRequest = request with { Subject = "must-roll-back" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeIdentity("CreateIdentity",
            failingRequest, runtimeDb, user, new FailingIdentityAudit(), CancellationToken.None));
        runtimeDb.ChangeTracker.Clear();
        Assert.False(await runtimeDb.EmployeeIdentityMappings.AnyAsync(row => row.Subject == "must-roll-back"));
    }

    private static Task<IResult> InvokeIdentity(string method, params object[] arguments) =>
        (Task<IResult>)typeof(Rev869AConfigurationEndpoints)
            .GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments)!;

    private sealed class IdentityWitnessUser(Guid employeeId) : ICurrentUser
    {
        public string LoginId => "admin-subject";
        public string RoleCode => "IT_MANAGER";
        public string? OrganizationId => "SESS_PVT_LTD";
        public bool IsAuthenticated => true;
        public Guid? EmployeeId => employeeId;
    }

    private sealed class FailingIdentityAudit : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entity, string id,
            object? before, object? after, CancellationToken ct) =>
            throw new InvalidOperationException("Injected audit failure");
    }
}
