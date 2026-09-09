using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<JobOrderHistory> JobOrderHistories => Set<JobOrderHistory>();

    private static void ConfigureJobOrderGovernance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobOrder>(entity =>
        {
            entity.ToTable("job_orders", table => table.HasCheckConstraint(
                "CK_job_order_joint_governance",
                "(\"CustomerPurchaseOrderId\" IS NULL AND \"CustomerPurchaseOrderLineId\" IS NULL AND \"MachineOrdinal\" IS NULL AND \"InitiatedByEmployeeId\" IS NULL AND \"InitiatedRoleAssignmentId\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL) OR (\"CustomerPurchaseOrderId\" IS NOT NULL AND \"CustomerPurchaseOrderLineId\" IS NOT NULL AND \"MachineOrdinal\">0 AND \"InitiatedByEmployeeId\" IS NOT NULL AND \"InitiatedActorRoleCode\" IS NOT NULL AND \"InitiatedRoleAssignmentId\" IS NOT NULL AND \"InitiatedRoleAssignmentType\" IS NOT NULL AND ((\"Status\"='PENDING_ACCOUNTS' AND \"AccountsConfirmedAt\" IS NULL AND \"AccountsConfirmedByEmployeeId\" IS NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NULL) OR (\"Status\"='OPEN' AND \"AccountsConfirmedAt\" IS NOT NULL AND \"AccountsConfirmedByEmployeeId\" IS NOT NULL AND \"AccountsConfirmationActorRoleCode\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentId\" IS NOT NULL AND \"AccountsConfirmationRoleAssignmentType\" IS NOT NULL AND NULLIF(btrim(\"AccountsConfirmationReason\"),'') IS NOT NULL)))"));
            entity.HasIndex(x => new { x.CompanyId, x.CustomerPurchaseOrderLineId, x.MachineOrdinal })
                .IsUnique().HasFilter("\"CustomerPurchaseOrderLineId\" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.ConfirmationIdempotencyKey })
                .IsUnique().HasFilter("\"ConfirmationIdempotencyKey\" IS NOT NULL");
            entity.Property(x => x.InitiatedActorRoleCode).HasMaxLength(100);
            entity.Property(x => x.InitiatedRoleAssignmentType).HasMaxLength(20);
            entity.Property(x => x.AccountsConfirmationActorRoleCode).HasMaxLength(100);
            entity.Property(x => x.AccountsConfirmationRoleAssignmentType).HasMaxLength(20);
            entity.Property(x => x.AccountsConfirmationReason).HasMaxLength(1000);
            entity.Property(x => x.ConfirmationIdempotencyKey).HasMaxLength(100);
            entity.Property(x => x.ConfirmationRequestFingerprint).HasColumnType("character(64)");
            entity.HasOne(x => x.CustomerPurchaseOrder).WithMany().HasForeignKey(x => x.CustomerPurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustomerPurchaseOrderLine).WithMany().HasForeignKey(x => x.CustomerPurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InitiatedByEmployee).WithMany().HasForeignKey(x => x.InitiatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InitiatedRoleAssignment).WithMany().HasForeignKey(x => x.InitiatedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AccountsConfirmedByEmployee).WithMany().HasForeignKey(x => x.AccountsConfirmedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AccountsConfirmationRoleAssignment).WithMany().HasForeignKey(x => x.AccountsConfirmationRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobOrderHistory>(entity =>
        {
            entity.ToTable("job_order_history");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.JobOrderId, x.CreatedAt });
            entity.Property(x => x.Action).HasMaxLength(30).IsRequired();
            entity.Property(x => x.FromStatus).HasMaxLength(30);
            entity.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}