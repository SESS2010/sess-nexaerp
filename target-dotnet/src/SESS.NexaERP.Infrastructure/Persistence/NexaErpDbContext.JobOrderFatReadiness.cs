using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<JobOrderFatCustodyExplanation> JobOrderFatCustodyExplanations => Set<JobOrderFatCustodyExplanation>();
    public DbSet<JobOrderFatReconciliation> JobOrderFatReconciliations => Set<JobOrderFatReconciliation>();
    public DbSet<JobOrderFatReconciliationLine> JobOrderFatReconciliationLines => Set<JobOrderFatReconciliationLine>();

    private static void ConfigureJobOrderFatReadiness(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobOrder>(entity =>
        {
            entity.ToTable("job_orders", table => table.HasCheckConstraint("CK_job_order_fat_readiness",
                "\"FatReadinessStatus\" IN ('NOT_RECONCILED','BLOCKED','READY') AND ((\"FatReadinessStatus\"='NOT_RECONCILED' AND \"FatReconciledAt\" IS NULL AND \"FatReconciledByEmployeeId\" IS NULL AND \"LatestFatReconciliationId\" IS NULL) OR (\"FatReadinessStatus\"<>'NOT_RECONCILED' AND \"FatReconciledAt\" IS NOT NULL AND \"FatReconciledByEmployeeId\" IS NOT NULL AND \"LatestFatReconciliationId\" IS NOT NULL))"));
            entity.Property(x => x.FatReadinessStatus).HasMaxLength(20).HasDefaultValue("NOT_RECONCILED").IsRequired();
            entity.HasOne(x => x.FatReconciledByEmployee).WithMany().HasForeignKey(x => x.FatReconciledByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobOrderFatCustodyExplanation>(entity =>
        {
            entity.ToTable("job_order_fat_custody_explanations", table => table.HasCheckConstraint(
                "CK_fat_custody_explanation", "\"QuantityBase\">0 AND \"Disposition\" IN ('LOST','SCRAPPED') AND \"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"Reason\"))>0"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.JobOrderId, x.MaterialIssueLineId });
            entity.Property(x => x.QuantityBase).HasPrecision(24, 6); entity.Property(x => x.Disposition).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired(); entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MaterialIssueLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExplainedByEmployee).WithMany().HasForeignKey(x => x.ExplainedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobOrderFatReconciliation>(entity =>
        {
            entity.ToTable("job_order_fat_reconciliations", table => table.HasCheckConstraint(
                "CK_fat_reconciliation", "\"AttemptNumber\">0 AND \"Result\" IN ('BLOCKED','READY') AND \"IssuedQuantityBase\">=0 AND \"FittedQuantityBase\">=0 AND \"ReturnedQuantityBase\">=0 AND \"ExplainedQuantityBase\">=0 AND \"UnexplainedQuantityBase\">=0 AND ((\"Result\"='READY' AND \"UnexplainedQuantityBase\"=0) OR (\"Result\"='BLOCKED' AND \"UnexplainedQuantityBase\">0)) AND \"ResolvedRoleAssignmentType\"='FULL'"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.JobOrderId, x.AttemptNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            foreach (var name in new[] { nameof(JobOrderFatReconciliation.IssuedQuantityBase), nameof(JobOrderFatReconciliation.FittedQuantityBase), nameof(JobOrderFatReconciliation.ReturnedQuantityBase), nameof(JobOrderFatReconciliation.ExplainedQuantityBase), nameof(JobOrderFatReconciliation.UnexplainedQuantityBase) }) entity.Property<decimal>(name).HasPrecision(24, 6);
            entity.Property(x => x.Result).HasMaxLength(20).IsRequired(); entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired(); entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired(); entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReconciledByEmployee).WithMany().HasForeignKey(x => x.ReconciledByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobOrderFatReconciliationLine>(entity =>
        {
            entity.ToTable("job_order_fat_reconciliation_lines", table => table.HasCheckConstraint("CK_fat_reconciliation_line",
                "\"IssuedQuantityBase\">0 AND \"FittedQuantityBase\">=0 AND \"ReturnedQuantityBase\">=0 AND \"ReturnedLateQuantityBase\">=0 AND \"ExplainedLostQuantityBase\">=0 AND \"ExplainedScrappedQuantityBase\">=0 AND \"UnexplainedQuantityBase\">=0 AND \"ReturnedLateQuantityBase\"<=\"ReturnedQuantityBase\" AND \"Classification\" IN ('FITTED','RETURNED','RETURNED_LATE','EXPLAINED','MIXED','UNEXPLAINED')"));
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.JobOrderFatReconciliationId, x.MaterialIssueLineId }).IsUnique();
            foreach (var name in new[] { nameof(JobOrderFatReconciliationLine.IssuedQuantityBase), nameof(JobOrderFatReconciliationLine.FittedQuantityBase), nameof(JobOrderFatReconciliationLine.ReturnedQuantityBase), nameof(JobOrderFatReconciliationLine.ReturnedLateQuantityBase), nameof(JobOrderFatReconciliationLine.ExplainedLostQuantityBase), nameof(JobOrderFatReconciliationLine.ExplainedScrappedQuantityBase), nameof(JobOrderFatReconciliationLine.UnexplainedQuantityBase) }) entity.Property<decimal>(name).HasPrecision(24, 6);
            entity.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired(); entity.Property(x => x.CustodianEmployeeCodeSnapshot).HasMaxLength(40).IsRequired(); entity.Property(x => x.Classification).HasMaxLength(20).IsRequired();
            entity.HasOne(x => x.JobOrderFatReconciliation).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.JobOrderFatReconciliationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MaterialIssueLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}