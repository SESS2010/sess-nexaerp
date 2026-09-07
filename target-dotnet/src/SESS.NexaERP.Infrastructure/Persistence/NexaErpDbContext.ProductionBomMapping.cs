using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureProductionBoms(ModelBuilder m)
    {
        ConfigureProductionBomHeader(m);
        ConfigureProductionBomRevision(m);
        ConfigureProductionBomLine(m);
    }

    private static void ConfigureProductionBomHeader(ModelBuilder m)
    {
        m.Entity<ProductionBom>(e => {
            e.ToTable("production_boms"); e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.BomNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.JobOrderId }).IsUnique();
            e.Property(x => x.BomNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.JobOrder).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.JobOrderId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<JobOrder>().HasOne(x => x.PinnedProductionBomRevision).WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.PinnedProductionBomRevisionId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
