using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<MaterialIssue> MaterialIssues => Set<MaterialIssue>();
    public DbSet<MaterialIssueLine> MaterialIssueLines => Set<MaterialIssueLine>();
    public DbSet<MaterialIssueExcessDecision> MaterialIssueExcessDecisions => Set<MaterialIssueExcessDecision>();
    public DbSet<MaterialIssueHistory> MaterialIssueHistories => Set<MaterialIssueHistory>();

    private static void ConfigureMaterialIssueExecution(ModelBuilder m)
    {
        m.Entity<MaterialIssue>(e => {
            e.ToTable("material_issues"); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.IssueNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.MaterialIssueRequestId });
            e.HasIndex(x => new { x.CompanyId, x.JobOrderId });
            e.Property(x => x.IssueNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.MaterialIssueRequest).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.IssuedToEmployee).WithMany().HasForeignKey(x => x.IssuedToEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.IssuedByEmployee).WithMany().HasForeignKey(x => x.IssuedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockPostingBatch).WithMany().HasForeignKey(x => x.StockPostingBatchId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<MaterialIssueLine>(e => {
            e.ToTable("material_issue_lines"); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.MaterialIssueId, x.LineNumber }).IsUnique();
            e.Property(x => x.QuantityBase).HasPrecision(24, 6);
            e.HasOne(x => x.MaterialIssue).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.MaterialIssueId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaterialIssueRequestLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InventoryCustodyCaseLine>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustodyCaseLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GoodsReceiptLine>().WithMany().HasForeignKey(x => new { x.CompanyId, x.OriginGoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<GoodsReceiptLineLotAllocation>().WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<QcInspectionLotDisposition>().WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionLotDispositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<MaterialIssueExcessDecision>(e => {
            e.ToTable("material_issue_excess_decisions"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.Decision).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.MaterialIssueRequestLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DecidedByEmployee).WithMany().HasForeignKey(x => x.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<MaterialIssueHistory>(e => {
            e.ToTable("material_issue_history"); e.HasKey(x => x.Id);
            e.HasIndex(x => x.CorrelationId).IsUnique();
            e.Property(x => x.Action).HasMaxLength(30).IsRequired();
            e.Property(x => x.FromStatus).HasMaxLength(30);
            e.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
            e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
        });
    }
}
