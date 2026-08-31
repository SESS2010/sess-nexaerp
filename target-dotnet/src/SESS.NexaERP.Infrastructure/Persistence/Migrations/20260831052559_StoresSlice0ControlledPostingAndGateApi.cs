using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoresSlice0ControlledPostingAndGateApi : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StoresControlledPostingSql.Up);
        }

        /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PostgreSqlClusterGuard.Require(migrationBuilder);
        migrationBuilder.Sql(StoresControlledPostingSql.Down);
    }
    }
}
