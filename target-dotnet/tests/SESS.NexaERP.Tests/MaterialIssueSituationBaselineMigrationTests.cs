using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void CorrectMaterialIssueSituationBaselinesRunsUpDownAndReapplyOnDisposablePostgreSql()
    {
        const string predecessor = "20260910094618_InAppNotificationDelivery";
        const string target = "20260910115815_CorrectMaterialIssueSituationBaselines";
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var model = new NexaErpDbContext(options);
        var migrator = model.GetService<IMigrator>();
        Assert.Contains(target, model.Database.GetMigrations());
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("mir-situation-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("mir-situation-up.sql", migrator.GenerateScript(predecessor, target) + AssertConstraint(true));
        server.Execute("mir-situation-down.sql", migrator.GenerateScript(target, predecessor) + AssertConstraint(false));
        server.Execute("mir-situation-reapply.sql", migrator.GenerateScript(predecessor, target) + AssertConstraint(true));
    }

#endif
    private static string AssertConstraint(bool includesSpare) => $"""
        DO $assert$
        DECLARE definition text; table_count integer;
        BEGIN
          SELECT pg_get_constraintdef(oid) INTO definition FROM pg_constraint
            WHERE conrelid='advance.material_issue_requests'::regclass AND conname='CK_mir_situation';
          IF definition IS NULL OR (position('SPARE_SALE' in definition)>0) IS DISTINCT FROM {includesSpare.ToString().ToLowerInvariant()} THEN
            RAISE EXCEPTION 'Unexpected material issue situation constraint: %',definition;
          END IF;
          SELECT count(*) INTO table_count FROM pg_catalog.pg_tables WHERE schemaname='advance';
          IF table_count<>211 THEN
            RAISE EXCEPTION 'Expected 211 advance base tables, found %.',table_count;
          END IF;
        END $assert$;
        """;
}