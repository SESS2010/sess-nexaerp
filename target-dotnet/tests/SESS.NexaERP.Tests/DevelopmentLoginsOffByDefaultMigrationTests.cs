using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string LoginsOffTarget = "20260926090000_DevelopmentLoginsOffByDefault";

    /// <summary>
    /// Decided by the Technical Director on 25 September: no employee is login-enabled by default on a
    /// fresh database. The migration turns off only what the two September development migrations
    /// turned on, keeps anyone with a real identity mapping or a deliberate enablement, never touches
    /// Status, and rolls back exactly.
    /// </summary>
    [Fact]
    public void Development_logins_are_off_by_default_and_mapped_or_deliberate_ones_are_kept()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, LoginsOffTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("logins-off-predecessor.sql", migrator.GenerateScript("0", predecessor) + """
            DO $before$ BEGIN
              IF (SELECT count(*) FROM advance.employees WHERE "LoginEnabled")<>11 THEN
                RAISE EXCEPTION 'Expected the 11 development logins before the migration.';
              END IF;
            END $before$;
            -- SESS-15 went through Step 6: a real, active mapping. It must stay enabled.
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),c."Id",c."Code",'https://192.168.68.130:8444/realms/staff','sess-15-subject',e."Id",'HUMAN',
                   current_date,true,clock_timestamp(),'STEP6_WITNESS',0
            FROM advance.employees e CROSS JOIN advance.companies c
            WHERE e."EmployeeCode"='SESS-15' AND c."Code"='SESS_PVT_LTD';
            -- SESS-02 has only a development mapping. That is no reason to keep it enabled.
            INSERT INTO advance.employee_identity_mappings
              ("Id","CompanyId","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
               "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
            SELECT gen_random_uuid(),c."Id",c."Code",'urn:nexaerp:development','SESS-02',e."Id",'HUMAN',
                   current_date,true,clock_timestamp(),'DEVELOPMENT_WORKFLOW_IDENTITIES',0
            FROM advance.employees e CROSS JOIN advance.companies c
            WHERE e."EmployeeCode"='SESS-02' AND c."Code"='SESS_PVT_LTD';
            -- SESS-16 was enabled again deliberately (activate-login writes the operator's login).
            UPDATE advance.employees SET "UpdatedBy"='sess-12-subject' WHERE "EmployeeCode"='SESS-16';
            CREATE TABLE public.logins_off_status_before AS
              SELECT "EmployeeCode","Status" FROM advance.employees;
            """);

        const string After = """
            DO $after$ BEGIN
              IF (SELECT string_agg("EmployeeCode",',' ORDER BY "EmployeeCode") FROM advance.employees WHERE "LoginEnabled")
                 IS DISTINCT FROM 'SESS-15,SESS-16' THEN
                RAISE EXCEPTION 'Expected only the mapped SESS-15 and the deliberately enabled SESS-16 to stay enabled.';
              END IF;
              IF (SELECT count(*) FROM advance.employees
                  WHERE "UpdatedBy" LIKE 'migration-development-logins-off-by-default:%' AND NOT "LoginEnabled")<>9 THEN
                RAISE EXCEPTION 'Expected nine development logins turned off, each carrying its original marker.';
              END IF;
              IF EXISTS (SELECT 1 FROM advance.employees e JOIN public.logins_off_status_before b USING ("EmployeeCode")
                         WHERE e."Status" IS DISTINCT FROM b."Status") THEN
                RAISE EXCEPTION 'The migration must never change an employee Status.';
              END IF;
            END $after$;
            """;
        const string Restored = """
            DO $restored$ BEGIN
              IF (SELECT count(*) FROM advance.employees WHERE "LoginEnabled")<>11
                 OR (SELECT count(*) FROM advance.employees WHERE "UpdatedBy"='migration-workflow-development-logins' AND "LoginEnabled")<>7
                 OR (SELECT count(*) FROM advance.employees WHERE "UpdatedBy"='migration-complete-workflow-development-logins' AND "LoginEnabled")<>3 THEN
                RAISE EXCEPTION 'Down must restore exactly the flags and markers Up changed.';
              END IF;
            END $restored$;
            """;

        server.Execute("logins-off-up.sql", migrator.GenerateScript(predecessor, LoginsOffTarget) + After);
        server.Execute("logins-off-down.sql", migrator.GenerateScript(LoginsOffTarget, predecessor) + Restored);
        server.Execute("logins-off-reapply.sql", migrator.GenerateScript(predecessor, LoginsOffTarget) + After);
    }

    [Fact]
    public void A_fresh_database_has_no_login_enabled_employee()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("logins-off-fresh.sql", migrator.GenerateScript("0", model.Database.GetMigrations().Last()) + """
            DO $fresh$ BEGIN
              IF EXISTS (SELECT 1 FROM advance.employees WHERE "LoginEnabled") THEN
                RAISE EXCEPTION 'A fresh database must have no login-enabled employee.';
              END IF;
              IF (SELECT count(*) FROM advance.employees WHERE "EmployeeCode"='SESS-12' AND upper("Status")='ACTIVE')<>1 THEN
                RAISE EXCEPTION 'SESS-12 must stay ACTIVE for the bootstrap.';
              END IF;
            END $fresh$;
            """);
    }
}
