using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureEngineeringDocuments(ModelBuilder m)
    {
        m.Entity<EngineeringDocument>(e => {
            e.ToTable("engineering_documents");
            e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.DocumentNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.JobOrderId, x.DocumentType });
            e.Property(x => x.DocumentNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.DocumentType).HasMaxLength(10).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.JobOrder).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.JobOrderId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CurrentRevision).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CurrentRevisionId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        ConfigureEngineeringDocumentRevision(m);
    }
}
