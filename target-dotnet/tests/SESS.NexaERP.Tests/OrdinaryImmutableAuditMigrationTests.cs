using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string OrdinaryAuditTarget = "20260906163834_OrdinaryImmutableAuditGuard";

    [Fact]
    public void Ordinary_immutable_audit_migration_is_additive_and_guards_both_directions()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", $"{OrdinaryAuditTarget}.cs"));
        var sql = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", "OrdinaryImmutableAuditSql.cs"));

        Assert.Equal(2, migration.Split("PostgreSqlClusterGuard.Require(migrationBuilder);", StringSplitOptions.None).Length - 1);
        Assert.Contains(@"CREATE TRIGGER ""TR_audit_logs_immutable""", sql, StringComparison.Ordinal);
        Assert.Contains("BEFORE UPDATE OR DELETE", sql, StringComparison.Ordinal);
        Assert.Contains("partially installed or bound incorrectly", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO advance.audit_logs", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE advance.audit_logs", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM advance.audit_logs", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ordinary_immutable_audit_guard_handles_absent_complete_and_partial_rev_states_on_postgresql()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var migrator = db.GetService<IMigrator>();
        var migrations = db.Database.GetMigrations().ToArray();
        var targetIndex = Array.IndexOf(migrations, OrdinaryAuditTarget);
        Assert.True(targetIndex > 0);
        var predecessor = migrations[targetIndex - 1];
        var prerequisite = migrator.GenerateScript("0", predecessor);
        var up = migrator.GenerateScript(predecessor, OrdinaryAuditTarget);
        var down = migrator.GenerateScript(OrdinaryAuditTarget, predecessor);

        VerifyAbsentUpDownAndReapply(prerequisite, up, down);
        VerifyCompleteRevCoexists(prerequisite, up);
        VerifyPartialRevIsRefused(prerequisite, up);
    }

    private static void VerifyAbsentUpDownAndReapply(string prerequisite, string up, string down)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("ordinary-audit-absent-pre.sql", prerequisite);
        server.Execute("ordinary-audit-absent-up.sql", up + GuardInstalledAssertion);
        server.AssertRejected("ordinary-audit-update-rejected.sql",
            @"UPDATE advance.audit_logs SET ""Result""=""Result"" WHERE ""Id""=(SELECT ""Id"" FROM advance.audit_logs LIMIT 1);",
            "Durable audit evidence is immutable");
        server.AssertRejected("ordinary-audit-delete-rejected.sql",
            @"DELETE FROM advance.audit_logs WHERE ""Id""=(SELECT ""Id"" FROM advance.audit_logs LIMIT 1);",
            "Durable audit evidence is immutable");
        server.Execute("ordinary-audit-down.sql", down + GuardInstalledAssertion);
        server.AssertRejected("ordinary-audit-update-after-down-rejected.sql",
            @"UPDATE advance.audit_logs SET ""Result""=""Result"" WHERE ""Id""=(SELECT ""Id"" FROM advance.audit_logs LIMIT 1);",
            "Durable audit evidence is immutable");
        server.Execute("ordinary-audit-reapply.sql", up + GuardInstalledAssertion);
    }

    private static void VerifyCompleteRevCoexists(string prerequisite, string up)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("ordinary-audit-complete-pre.sql", prerequisite);
        server.Execute("ordinary-audit-complete-rev.sql", CompleteRevGuard);
        server.Execute("ordinary-audit-complete-up.sql", up + GuardInstalledAssertion);
        server.AssertRejected("ordinary-audit-complete-update-rejected.sql",
            @"UPDATE advance.audit_logs SET ""Result""=""Result"" WHERE ""Id""=(SELECT ""Id"" FROM advance.audit_logs LIMIT 1);",
            "Durable audit evidence is immutable");
    }

    private static void VerifyPartialRevIsRefused(string prerequisite, string up)
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("ordinary-audit-partial-pre.sql", prerequisite);
        server.Execute("ordinary-audit-partial-rev.sql", RevFunctionOnly);
        server.AssertRejected("ordinary-audit-partial-up-rejected.sql", up, "partially installed or bound incorrectly");
    }

    private const string RevFunctionOnly = """
        CREATE OR REPLACE FUNCTION advance.rev869b_guard_durable_audit_retention()
        RETURNS trigger LANGUAGE plpgsql SET search_path=pg_catalog,advance AS $rev869b$
        BEGIN
          RAISE EXCEPTION USING ERRCODE='42501', MESSAGE='Durable audit evidence is immutable.';
        END $rev869b$;
        """;

    private const string CompleteRevGuard = RevFunctionOnly + """

        CREATE TRIGGER trg_rev869b_durable_audit_retention
          BEFORE UPDATE OR DELETE ON advance.audit_logs
          FOR EACH ROW EXECUTE FUNCTION advance.rev869b_guard_durable_audit_retention();
        """;

    private const string GuardInstalledAssertion = """

        DO $assert$
        BEGIN
          IF to_regprocedure('advance.guard_audit_logs_immutable()') IS NULL OR NOT EXISTS (
            SELECT 1 FROM pg_trigger t
            JOIN pg_class c ON c.oid=t.tgrelid
            JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='advance' AND c.relname='audit_logs'
              AND t.tgname='TR_audit_logs_immutable' AND NOT t.tgisinternal
              AND t.tgenabled<>'D'
              AND t.tgfoid=to_regprocedure('advance.guard_audit_logs_immutable()'))
          THEN RAISE EXCEPTION 'ordinary immutable-audit guard missing or misbound'; END IF;
        END $assert$;
        """;
}
