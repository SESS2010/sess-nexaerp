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
        complete.Execute("retire-rev869b-complete-roles.sql",ExternalRolePrerequisites);
        complete.Execute("retire-rev869b-complete-security.sql",
            Rev869BSecurityPackageSql.InstallCommandContext+Environment.NewLine+Rev869BSecurityPackageSql.InstallControlledMutation);
        complete.Execute("retire-rev869b-complete-business-tail.sql",migrator.GenerateScript(foundation,predecessor));
        complete.Execute("retire-rev869b-complete-up.sql",migrator.GenerateScript(predecessor,Rev869BRetirementTarget));
        complete.Execute("retire-rev869b-complete-archive.sql","SELECT 1 FROM advance.rev869b_command_requests; SELECT 1 FROM advance.command_requests;");
        complete.AssertRejected("retire-rev869b-complete-function-retired.sql",
            "SELECT advance.rev869b_register_command_request('x','x',decode(repeat('00',32),'hex'),decode(repeat('00',32),'hex'),gen_random_uuid(),'x','x','x',gen_random_uuid());");
        complete.Execute("retire-rev869b-complete-down.sql",migrator.GenerateScript(Rev869BRetirementTarget,predecessor));
        complete.Execute("retire-rev869b-complete-function-restored.sql",
            "SELECT to_regprocedure('advance.rev869b_register_command_request(text,text,bytea,bytea,uuid,text,text,text,uuid)') IS NOT NULL;");
    }

    private static NexaErpDbContext MigrationModel() => new(new DbContextOptionsBuilder<NexaErpDbContext>()
        .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
}
