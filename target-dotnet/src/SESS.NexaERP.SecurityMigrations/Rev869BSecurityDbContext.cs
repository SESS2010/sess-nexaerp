using Microsoft.EntityFrameworkCore;

namespace SESS.NexaERP.SecurityMigrations;

public sealed class Rev869BSecurityDbContext(DbContextOptions<Rev869BSecurityDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.HasDefaultSchema("advance");
}
