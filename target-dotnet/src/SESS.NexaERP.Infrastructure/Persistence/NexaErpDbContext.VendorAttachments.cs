using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<VendorAttachment> VendorAttachments => Set<VendorAttachment>();

    private static void ConfigureVendorAttachments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VendorAttachment>(entity =>
        {
            entity.ToTable("vendor_attachments");
            entity.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => x.Kind);
        });
    }
}
