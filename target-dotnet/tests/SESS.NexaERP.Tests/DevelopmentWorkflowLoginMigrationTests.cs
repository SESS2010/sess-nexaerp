using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string WorkflowLoginTarget = "20260907114500_EnableWorkflowDevelopmentLogins";

#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Workflow_development_logins_enable_exactly_eight_and_round_trip_on_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, WorkflowLoginTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("workflow-logins-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("workflow-logins-up.sql", migrator.GenerateScript(predecessor, WorkflowLoginTarget) + EnabledAssertion);
        server.Execute("workflow-logins-down.sql", migrator.GenerateScript(WorkflowLoginTarget, predecessor) + DisabledAssertion);
        server.Execute("workflow-logins-reapply.sql", migrator.GenerateScript(predecessor, WorkflowLoginTarget) + EnabledAssertion);
    }
#endif

    private const string EnabledAssertion = """

        DO $assert$
        BEGIN
          IF (SELECT count(*) FROM advance.employees
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-02','SESS-14','SESS-15','SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']::text[])
                AND "LoginEnabled")<>8 THEN
            RAISE EXCEPTION 'Expected exactly eight workflow development logins enabled.';
          END IF;
        END $assert$;
        """;

    private const string DisabledAssertion = """

        DO $assert$
        BEGIN
          IF EXISTS (SELECT 1 FROM advance.employees
              WHERE "EmployeeCode"=ANY(ARRAY['SESS-02','SESS-14','SESS-15','SESS-16','SESS-25','SESS-33','SESS-35','SESS-41']::text[])
                AND "LoginEnabled") THEN
            RAISE EXCEPTION 'Workflow development logins survived migration Down.';
          END IF;
        END $assert$;
        """;
}
