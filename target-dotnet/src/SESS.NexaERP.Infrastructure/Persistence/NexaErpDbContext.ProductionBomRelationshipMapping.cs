using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureProductionBomRevisionRelationships(EntityTypeBuilder<ProductionBomRevision> e)
    {
        e.HasOne(x => x.ProductionBom).WithMany(x => x.Revisions)
            .HasForeignKey(x => new { x.CompanyId, x.ProductionBomId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.SourceEstimatedBomRevision).WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.SourceEstimatedBomRevisionId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.SupersedesRevision).WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.SupersedesRevisionId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.PreparedByEmployee).WithMany()
            .HasForeignKey(x => x.PreparedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ApprovedByEmployee).WithMany()
            .HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
