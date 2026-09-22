using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<EstimatedBom> EstimatedBoms => Set<EstimatedBom>();
    public DbSet<EstimatedBomRevision> EstimatedBomRevisions => Set<EstimatedBomRevision>();
    public DbSet<EstimatedBomLine> EstimatedBomLines => Set<EstimatedBomLine>();
    public DbSet<EstimatedBomHistory> EstimatedBomHistories => Set<EstimatedBomHistory>();
    public DbSet<SESS.NexaERP.Domain.Inventory.ItemMergeAlias> ItemMergeAliases => Set<SESS.NexaERP.Domain.Inventory.ItemMergeAlias>();

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
            e.Property(x => x.Quantity).HasPrecision(24, 6); e.Property(x => x.ValueSource).HasMaxLength(30);
            e.Property(x => x.EstimatedUnitValue).HasPrecision(20, 6);
            e.ToTable(t => t.HasCheckConstraint("CK_estimated_bom_line_value", "\"EstimatedUnitValue\" IS NULL OR \"EstimatedUnitValue\">=0"));
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasOne(x => x.EstimatedBomRevision).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.EstimatedBomRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<EstimatedBomHistory>(e => {
            e.ToTable("estimated_bom_history"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompanyId, x.EstimatedBomId, x.CreatedAt });
            e.HasIndex(x => new { x.CompanyId, x.CorrelationId }).IsUnique();
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();
            e.Property(x => x.FromStatus).HasMaxLength(20); e.Property(x => x.ToStatus).HasMaxLength(20).IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(16).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            e.HasOne<SESS.NexaERP.Domain.Foundation.Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EstimatedBom>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EstimatedBomId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EstimatedBomRevision>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EstimatedBomRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SESS.NexaERP.Domain.Employees.Employee>().WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SESS.NexaERP.Domain.Employees.EmployeeRoleAssignment>().WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<SESS.NexaERP.Domain.Inventory.ItemMergeAlias>(e => {
            e.ToTable("item_merge_aliases"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.SourceItemId).IsUnique(); e.HasIndex(x => x.SurvivorItemId);
            e.Property(x => x.ActorRoleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(16).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<SESS.NexaERP.Domain.Foundation.Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SourceItem).WithMany().HasForeignKey(x => x.SourceItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SurvivorItem).WithMany().HasForeignKey(x => x.SurvivorItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SESS.NexaERP.Domain.Employees.Employee>().WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SESS.NexaERP.Domain.Employees.EmployeeRoleAssignment>().WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
