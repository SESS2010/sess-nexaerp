using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string CompleteWorkflowLoginTarget =
        "20260907220500_CompleteSeededWorkflowDevelopmentLogins";

    [Fact]
    public void Workflow_login_completion_round_trips_only_rows_changed_by_this_migration()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, CompleteWorkflowLoginTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("complete-workflow-logins-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("complete-workflow-logins-existing.sql", """
            UPDATE advance.employees
            SET "LoginEnabled"=true,"UpdatedBy"='pre-existing-login-enablement'
            WHERE "EmployeeCode"='SESS-04';
            """);
        server.Execute("complete-workflow-logins-up.sql",
            migrator.GenerateScript(predecessor, CompleteWorkflowLoginTarget) + CompleteEnabledAssertion);
        server.Execute("complete-workflow-logins-down.sql",
            migrator.GenerateScript(CompleteWorkflowLoginTarget, predecessor) + CompleteDownAssertion);
        server.Execute("complete-workflow-logins-reapply.sql",
            migrator.GenerateScript(predecessor, CompleteWorkflowLoginTarget) + CompleteEnabledAssertion);
    }

    [Fact]
    public void Workflow_login_completion_does_not_name_non_seeded_sess_101()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "SESS.NexaERP.Infrastructure",
            "Persistence", "Migrations", "20260907220500_CompleteSeededWorkflowDevelopmentLogins.cs"));
        Assert.DoesNotContain("SESS-101", source, StringComparison.Ordinal);
    }

    private const string CompleteEnabledAssertion = """

        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM advance.employees
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-01','SESS-04','SESS-12']::text[])
                AND "LoginEnabled")<>3 THEN
            RAISE EXCEPTION 'Expected all three seeded workflow logins enabled.';
          END IF;
          IF (SELECT count(*) FROM advance.employees
              WHERE "EmployeeCode" IN ('SESS-01','SESS-12')
                AND "UpdatedBy"='migration-complete-workflow-development-logins')<>2 THEN
            RAISE EXCEPTION 'Expected this migration to change only the two disabled fixture rows.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.employees
              WHERE "EmployeeCode"='SESS-04' AND "UpdatedBy"='pre-existing-login-enablement') THEN
            RAISE EXCEPTION 'Up rewrote a login that was already enabled.';
          END IF;
        END $assert$;
        """;

    private const string CompleteDownAssertion = """

        DO $assert$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.employees
              WHERE "EmployeeCode" IN ('SESS-01','SESS-12') AND "LoginEnabled") THEN
            RAISE EXCEPTION 'Rows changed by this migration survived Down.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM advance.employees
              WHERE "EmployeeCode"='SESS-04' AND "LoginEnabled"
                AND "UpdatedBy"='pre-existing-login-enablement') THEN
            RAISE EXCEPTION 'Down disabled a login that predated this migration.';
          END IF;
        END $assert$;
        """;
}
