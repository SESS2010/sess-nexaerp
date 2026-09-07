using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureEngineeringDocumentRevision(ModelBuilder m)
    {
        m.Entity<EngineeringDocumentRevision>(e => {
            e.ToTable("engineering_document_revisions");
            e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.EngineeringDocumentId, x.RevisionNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.RevisionCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.RevisionNote).HasMaxLength(2000).IsRequired();
            e.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(300).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContentFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.EngineeringDocument).WithMany(x => x.Revisions)
                .HasForeignKey(x => new { x.CompanyId, x.EngineeringDocumentId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SupersedesRevision).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.SupersedesRevisionId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DrawnByEmployee).WithMany().HasForeignKey(x => x.DrawnByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CheckedByEmployee).WithMany().HasForeignKey(x => x.CheckedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ApprovedByEmployee).WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
