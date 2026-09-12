using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260912130000_GovernedAuthenticationRuntime")]
public sealed class GovernedAuthenticationRuntime : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(GovernedAuthenticationRuntimeSql.Up);
        migrationBuilder.Sql(RetainedIdentityHistorySql.Up);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(GovernedAuthenticationRuntimeSql.Down);
        migrationBuilder.Sql(RetainedIdentityHistorySql.Down);
    }
}
