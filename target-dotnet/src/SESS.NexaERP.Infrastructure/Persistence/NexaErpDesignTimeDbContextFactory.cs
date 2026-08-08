using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed class NexaErpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NexaErpDbContext>
{
    public NexaErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NexaErp")
            ?? throw new InvalidOperationException("Design-time connection string 'ConnectionStrings__NexaErp' must be supplied by a secure environment variable.");

        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new NexaErpDbContext(options);
    }
}
