using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Inventory;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<ItemVendor> ItemVendors => Set<ItemVendor>();
    public DbSet<ItemImage> ItemImages => Set<ItemImage>();

    private static void ConfigureItemVendors(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemVendor>(entity =>
        {
            entity.ToTable("item_vendors");
            entity.HasIndex(x => new { x.ItemId, x.VendorId }).IsUnique();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemImage>(entity =>
        {
            entity.ToTable("item_images");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Content).IsRequired();
        });
    }
}
