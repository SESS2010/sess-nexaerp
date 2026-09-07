using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<EstimatedBom> EstimatedBoms => Set<EstimatedBom>();
    public DbSet<EstimatedBomRevision> EstimatedBomRevisions => Set<EstimatedBomRevision>();
    public DbSet<EstimatedBomLine> EstimatedBomLines => Set<EstimatedBomLine>();

    private static void ConfigureEstimatedBom(ModelBuilder m)
    {
        m.Entity<EstimatedBom>(e => {
            e.ToTable("estimated_boms");
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.BomNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.JobOrderId }).IsUnique();
            e.Property(x => x.BomNumber).HasMaxLength(60).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<EstimatedBomRevision>(e => {
            e.ToTable("estimated_bom_revisions");
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.EstimatedBomId, x.RevisionNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.RevisionReason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ApprovalReason).HasMaxLength(1000);
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContentFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.EstimatedBom).WithMany(x => x.Revisions).HasForeignKey(x => new { x.CompanyId, x.EstimatedBomId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PreparedByEmployee).WithMany().HasForeignKey(x => x.PreparedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedByEmployee).WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<EstimatedBomLine>(e => {
            e.ToTable("estimated_bom_lines");
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.EstimatedBomRevisionId, x.LineNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.ItemId });
            e.Property(x => x.Quantity).HasPrecision(24, 6);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasOne(x => x.EstimatedBomRevision).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.EstimatedBomRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
