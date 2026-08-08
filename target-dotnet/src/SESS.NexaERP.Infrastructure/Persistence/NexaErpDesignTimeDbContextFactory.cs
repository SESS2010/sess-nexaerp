using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed class NexaErpDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NexaErpDbContext>
{
    public NexaErpDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=localhost;Database=sess_nexaerp;Username=postgres")
            .Options;

        return new NexaErpDbContext(options);
    }
}
