using Microsoft.EntityFrameworkCore.Migrations;

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class PostgreSqlClusterGuard
{
    private const string Provider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    internal static void Require(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        if (!string.Equals(migrationBuilder.ActiveProvider, Provider, StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"Employee master rebuild is PostgreSQL-only; active provider was '{migrationBuilder.ActiveProvider}'.");
        }
    }
}
