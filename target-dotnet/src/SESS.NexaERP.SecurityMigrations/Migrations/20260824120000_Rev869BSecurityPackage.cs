using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.SecurityMigrations.Migrations;

[DbContext(typeof(Rev869BSecurityDbContext))]
[Migration(Rev869BSecurityDesignTimeDbContextFactory.MigrationId)]
public sealed class Rev869BSecurityPackage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(Rev869BSecurityPackageSql.InstallCommandContext);
        migrationBuilder.Sql(Rev869BSecurityPackageSql.InstallControlledMutation);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(Rev869BSecurityPackageSql.RemoveControlledMutation);
        migrationBuilder.Sql(Rev869BSecurityPackageSql.RemoveCommandContext);
    }
}
