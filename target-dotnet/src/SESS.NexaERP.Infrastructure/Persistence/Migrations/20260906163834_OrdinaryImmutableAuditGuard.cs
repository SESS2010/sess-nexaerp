using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrdinaryImmutableAuditGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(OrdinaryImmutableAuditSql.Ensure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(OrdinaryImmutableAuditSql.PreserveOnDown);
        }
    }
}
