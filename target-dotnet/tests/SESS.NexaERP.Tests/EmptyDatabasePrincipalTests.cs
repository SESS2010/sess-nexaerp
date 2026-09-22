using Npgsql;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task PrincipalProvisioningSupportsEmptySchemaBeforeFirstMigration()
    {
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("empty-schema.sql", "CREATE SCHEMA advance;");
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "empty-principal-witness-123456789");
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("new-owner-function.sql", "SET ROLE nexa_erp_owner; CREATE FUNCTION advance.private_probe() RETURNS integer LANGUAGE sql AS 'SELECT 1';");
        await using var runtime = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(server.ConnectionString)
        { Username = "nexa_erp_runtime", Password = "empty-principal-witness-123456789", Pooling = false }.ConnectionString);
        await runtime.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE TABLE advance.forbidden(id integer)", runtime);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("42501", exception.SqlState);
        await using var function = new NpgsqlCommand("SELECT advance.private_probe()", runtime);
        var denied = await Assert.ThrowsAsync<PostgresException>(() => function.ExecuteScalarAsync());
        Assert.Equal("42501", denied.SqlState);
    }
}
