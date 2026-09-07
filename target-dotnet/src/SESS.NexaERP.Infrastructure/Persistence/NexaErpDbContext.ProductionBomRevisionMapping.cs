using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureProductionBomRevision(ModelBuilder m)
    {
        m.Entity<ProductionBomRevision>(e => {
            e.ToTable("production_bom_revisions");
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.ProductionBomId, x.RevisionNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.RevisionReason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ApprovalReason).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContentFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            ConfigureProductionBomRevisionRelationships(e);
        });
    }
}
