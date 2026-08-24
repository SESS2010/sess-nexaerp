using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace SESS.NexaERP.SecurityMigrations;

public sealed class Rev869BSecurityDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<Rev869BSecurityDbContext>
{
    public const string MigrationId = "20260824120000_Rev869BSecurityPackage";

    public Rev869BSecurityDbContext CreateDbContext(string[] args)
    {
        var requestedMigration = Environment.GetEnvironmentVariable("NexaErp__Rev869BSecurityMigrationTarget");
        if (!string.Equals(requestedMigration, MigrationId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Set NexaErp__Rev869BSecurityMigrationTarget to '{MigrationId}' for an explicitly authorized security-package operation.");
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp")
            ?? throw new InvalidOperationException(
                "Design-time connection string 'ConnectionStrings__NexaErp' must be supplied by a secure environment variable.");
        var expectedDatabase = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase")
            ?? throw new InvalidOperationException(
                "Design-time expected database 'NexaErp__ExpectedDatabase' must be supplied by a secure environment variable.");
        var actualDatabase = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (!string.Equals(actualDatabase, expectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Design-time connection database does not match the approved expected database.");
        }

        var options = new DbContextOptionsBuilder<Rev869BSecurityDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(Rev869BSecurityDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Rev869BSecurity", "advance");
                })
            .Options;

        return new Rev869BSecurityDbContext(options);
    }
}
