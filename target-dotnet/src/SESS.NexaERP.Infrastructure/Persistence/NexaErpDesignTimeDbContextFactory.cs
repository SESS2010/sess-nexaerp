using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed class NexaErpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NexaErpDbContext>
{
    public NexaErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp")
            ?? throw new InvalidOperationException("Design-time connection string 'ConnectionStrings__NexaErp' must be supplied by a secure environment variable.");
        var expectedDatabase = Environment.GetEnvironmentVariable("NexaErp__ExpectedDatabase")
            ?? throw new InvalidOperationException("Design-time expected database 'NexaErp__ExpectedDatabase' must be supplied by a secure environment variable.");
        var actualDatabase = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (!string.Equals(actualDatabase, expectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Design-time connection database does not match the approved expected database.");
        }

        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DatabaseSchemas.Advance))
            .Options;

        return new NexaErpDbContext(options);
    }
}
