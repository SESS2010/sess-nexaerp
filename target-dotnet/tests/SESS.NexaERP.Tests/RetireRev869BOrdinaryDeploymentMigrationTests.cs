using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string Rev869BRetirementTarget = "20260906203000_RetireRev869BOrdinaryDeployment";

    [Fact]
    public void Rev869B_retirement_applies_and_reverts_when_package_is_absent()
    {
        using var model = MigrationModel();
        var migrator=model.GetService<IMigrator>();var migrations=model.Database.GetMigrations().ToArray();
        var index=Array.IndexOf(migrations,Rev869BRetirementTarget);Assert.True(index>0);var predecessor=migrations[index-1];
        using var server=DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("retire-rev869b-absent-prerequisite.sql",migrator.GenerateScript("0",predecessor));
        server.Execute("retire-rev869b-absent-up.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
        server.Execute("retire-rev869b-absent-down.sql",migrator.GenerateScript(Rev869BRetirementTarget,predecessor));
        server.Execute("retire-rev869b-absent-reapply.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
    }

    [Fact]
    public void Rev869B_retirement_refuses_partial_and_round_trips_complete_package()
    {
        using var model=MigrationModel();var migrator=model.GetService<IMigrator>();var migrations=model.Database.GetMigrations().ToArray();
        var index=Array.IndexOf(migrations,Rev869BRetirementTarget);Assert.True(index>0);var predecessor=migrations[index-1];
        using(var partial=DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            partial.Execute("retire-rev869b-partial-prerequisite.sql",migrator.GenerateScript("0",predecessor));
            partial.Execute("retire-rev869b-partial-marker.sql","CREATE TABLE advance.rev869b_command_requests(id integer);");
            partial.AssertRejected("retire-rev869b-partial-up.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
        }

        const string foundation="20260824150742_CalibrationPurchasePairItemTypeCorrections";
        using var complete=DisposablePostgreSql.Start(FindPostgreSqlBin());
        complete.Execute("retire-rev869b-complete-foundation.sql",migrator.GenerateScript("0",foundation));
        complete.Execute("retire-rev869b-complete-roles.sql",ExternalRolePrerequisites+Environment.NewLine+BootstrapRolePrerequisites);
        complete.Execute("retire-rev869b-complete-security.sql",
            Rev869BSecurityPackageSql.InstallCommandContext+Environment.NewLine+Rev869BSecurityPackageSql.InstallControlledMutation);
        complete.Execute("retire-rev869b-complete-business-tail.sql",migrator.GenerateScript(foundation,predecessor));
        complete.Execute("retire-rev869b-complete-ownership.sql",RealRev869BOwnership);
        complete.Execute("retire-rev869b-complete-up.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
        complete.Execute("retire-rev869b-complete-archive.sql","SELECT 1 FROM advance.rev869b_command_requests; SELECT 1 FROM advance.command_requests;");
        complete.Execute("retire-rev869b-complete-owner-write.sql",OwnerWriteAfterRetirement);
        complete.AssertRejected("retire-rev869b-complete-function-retired.sql",
            "SELECT advance.rev869b_register_command_request('x','x',decode(repeat('00',32),'hex'),decode(repeat('00',32),'hex'),gen_random_uuid(),'x','x','x',gen_random_uuid());");
        complete.Execute("retire-rev869b-complete-down.sql",migrator.GenerateScript(Rev869BRetirementTarget,predecessor));
        complete.Execute("retire-rev869b-complete-function-restored.sql",
            "SELECT to_regprocedure('advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL;");
        complete.Execute("retire-rev869b-complete-reapply.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
        complete.Execute("retire-rev869b-complete-owner-write-after-reapply.sql",OwnerWriteAfterRetirement);
    }

    private static NexaErpDbContext MigrationModel() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);

    private const string RealRev869BOwnership = """
        ALTER SCHEMA advance OWNER TO nexa_rev869b_security_owner;
        CREATE TABLE advance.rev869b_ownership_probe(id integer NOT NULL);
        ALTER TABLE advance.rev869b_ownership_probe OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE advance.employees OWNER TO nexa_rev869b_security_owner;
        """;

    private const string OwnerWriteAfterRetirement = """
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
          ) THEN
            RAISE EXCEPTION 'REV869B ownership survived retirement.';
          END IF;
        END $ownership$;
        SET ROLE nexa_erp_owner;
        INSERT INTO advance.rev869b_ownership_probe(id)
        SELECT COALESCE(max(id),0)+1 FROM advance.rev869b_ownership_probe;
        RESET ROLE;
        """;
}
