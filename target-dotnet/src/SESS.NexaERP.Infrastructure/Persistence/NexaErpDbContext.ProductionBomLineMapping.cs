using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureProductionBomLine(ModelBuilder m)
    {
        m.Entity<ProductionBomLine>(e => {
            e.ToTable("production_bom_lines");
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.ProductionBomRevisionId, x.LineNumber }).IsUnique();
            e.Property(x => x.Quantity).HasPrecision(24, 6);
            e.Property(x => x.PlannedUnitValue).HasPrecision(20, 6);
            e.ToTable(t => t.HasCheckConstraint("CK_production_bom_line_value", "\"PlannedUnitValue\" IS NULL OR \"PlannedUnitValue\">=0"));
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasOne(x => x.ProductionBomRevision).WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.CompanyId, x.ProductionBomRevisionId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
