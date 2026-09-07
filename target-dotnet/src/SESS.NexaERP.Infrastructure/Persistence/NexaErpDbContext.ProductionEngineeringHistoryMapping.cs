using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureProductionEngineeringHistory(ModelBuilder m)
    {
        m.Entity<ProductionEngineeringHistory>(e => {
            e.ToTable("production_engineering_history");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompanyId, x.CorrelationId }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.OccurredAt });
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();
            e.Property(x => x.FromStatus).HasMaxLength(20);
            e.Property(x => x.ToStatus).HasMaxLength(20).IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(2000).IsRequired();
            e.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProductionBom).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ProductionBomId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProductionBomRevision).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ProductionBomRevisionId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EngineeringDocument).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.EngineeringDocumentId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EngineeringDocumentRevision).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.EngineeringDocumentRevisionId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
