using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string AppliedOwnershipTarget = "20260907123000_ConvergeAppliedRev869BRetirementOwnership";

    [Fact]
    public void Applied_retirement_ownership_converges_absent_complete_and_partial_states_on_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, AppliedOwnershipTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        var up = migrator.GenerateScript(predecessor, AppliedOwnershipTarget);
        var down = migrator.GenerateScript(AppliedOwnershipTarget, predecessor);

        using (var absent = DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            absent.Execute("applied-ownership-absent-pre.sql", migrator.GenerateScript("0", predecessor));
            absent.Execute("applied-ownership-absent-up.sql", up);
            absent.Execute("applied-ownership-absent-down.sql", down);
            absent.Execute("applied-ownership-absent-reapply.sql", up);
        }

        using (var partial = DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            partial.Execute("applied-ownership-partial-pre.sql", migrator.GenerateScript("0", predecessor));
            partial.Execute("applied-ownership-partial-role.sql", "CREATE ROLE nexa_rev869b_security_owner NOLOGIN;");
            partial.AssertRejected("applied-ownership-partial-up.sql", up, "partial retired state");
        }

        const string foundation = "20260824150742_CalibrationPurchasePairItemTypeCorrections";
        using var complete = DisposablePostgreSql.Start(FindPostgreSqlBin());
        complete.Execute("applied-ownership-complete-foundation.sql", migrator.GenerateScript("0", foundation));
        complete.Execute("applied-ownership-complete-roles.sql", ExternalRolePrerequisites + Environment.NewLine + BootstrapRolePrerequisites);
        complete.Execute("applied-ownership-complete-security.sql",
            Rev869BSecurityPackageSql.InstallCommandContext + Environment.NewLine + Rev869BSecurityPackageSql.InstallControlledMutation);
        complete.Execute("applied-ownership-complete-tail.sql", migrator.GenerateScript(foundation, predecessor));
        complete.Execute("applied-ownership-recreate-defect.sql", """
            ALTER SCHEMA advance OWNER TO nexa_rev869b_security_owner;
            CREATE TABLE advance.rev869b_applied_ownership_probe(id integer NOT NULL);
            ALTER TABLE advance.rev869b_applied_ownership_probe OWNER TO nexa_rev869b_security_owner;
            """);
        complete.Execute("applied-ownership-complete-up.sql", up + CompleteOwnershipAssertion);
        complete.Execute("applied-ownership-complete-down.sql", down + CompleteOwnershipAssertion);
        complete.Execute("applied-ownership-complete-reapply.sql", up + CompleteOwnershipAssertion);
    }

    private const string CompleteOwnershipAssertion = """

        DO $ownership$
        BEGIN
          IF EXISTS (
            SELECT 1 FROM pg_namespace n JOIN pg_roles r ON r.oid=n.nspowner
            WHERE n.nspname='advance' AND r.rolname LIKE 'nexa_rev869b_%'
            UNION ALL
            SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_roles r ON r.oid=c.relowner
            WHERE n.nspname='advance' AND r.rolname LIKE 'nexa_rev869b_%'
            UNION ALL
            SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace JOIN pg_roles r ON r.oid=p.proowner
            WHERE n.nspname='advance' AND r.rolname LIKE 'nexa_rev869b_%'
          ) THEN RAISE EXCEPTION 'Applied retirement ownership did not converge.'; END IF;
        END $ownership$;
        SET ROLE nexa_erp_owner;
        INSERT INTO advance.rev869b_applied_ownership_probe(id)
        SELECT COALESCE(max(id),0)+1 FROM advance.rev869b_applied_ownership_probe;
        RESET ROLE;
        """;
}
