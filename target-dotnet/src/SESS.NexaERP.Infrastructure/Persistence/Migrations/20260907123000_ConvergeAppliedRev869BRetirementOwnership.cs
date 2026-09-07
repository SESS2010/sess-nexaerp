using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

[DbContext(typeof(NexaErpDbContext))]
[Migration("20260907123000_ConvergeAppliedRev869BRetirementOwnership")]
public sealed class ConvergeAppliedRev869BRetirementOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(RetireRev869BOrdinaryDeploymentSql.RepairAlreadyAppliedOwnership);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        // A security correction is monotonic: rollback must not recreate REV-owned objects.
        migrationBuilder.Sql(RetireRev869BOrdinaryDeploymentSql.RepairAlreadyAppliedOwnership);
    }
}
