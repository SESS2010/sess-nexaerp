using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Identity;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<DevelopmentLoginPassword> DevelopmentLoginPasswords => Set<DevelopmentLoginPassword>();

    private static void ConfigureDevelopmentLogin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DevelopmentLoginPassword>(entity =>
        {
            entity.ToTable("development_login_passwords");
            entity.HasIndex(x => x.EmployeeId).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(400).IsRequired();
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
