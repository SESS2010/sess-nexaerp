using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<ComponentFitment> ComponentFitments => Set<ComponentFitment>();
    public DbSet<ComponentFitmentReversal> ComponentFitmentReversals => Set<ComponentFitmentReversal>();
    public DbSet<ActualBom> ActualBoms => Set<ActualBom>();
    public DbSet<ActualBomEntry> ActualBomEntries => Set<ActualBomEntry>();

    private static void ConfigureFitmentActualBom(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComponentFitment>(e =>
        {
            e.ToTable("component_fitments", t => t.HasCheckConstraint("CK_component_fitment_quantity", "\"QuantityBase\">0 AND \"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"ConfirmationNote\"))>0"));
            e.HasKey(x => x.Id); e.HasIndex(x => new { x.CompanyId, x.FitmentNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.JobOrderId, x.MaterialIssueLineId });
            e.HasIndex(x => new { x.CompanyId, x.ReverifiesFitmentId }).IsUnique().HasFilter("\"ReverifiesFitmentId\" IS NOT NULL");
            e.Property(x => x.FitmentNumber).HasMaxLength(50).IsRequired(); e.Property(x => x.QuantityBase).HasPrecision(24, 6);
            e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired(); e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.ConfirmationNote).HasMaxLength(1000).IsRequired(); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestFingerprint).HasColumnType("character(64)");
            e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaterialIssueLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReverifiesFitment).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReverifiesFitmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ConfirmedByEmployee).WithMany().HasForeignKey(x => x.ConfirmedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ComponentFitmentReversal>(e =>
        {
            e.ToTable("component_fitment_reversals", t => t.HasCheckConstraint("CK_component_fitment_reversal", "\"ResolvedRoleAssignmentType\"='FULL' AND length(btrim(\"Reason\"))>0"));
            e.HasKey(x => x.Id); e.HasIndex(x => new { x.CompanyId, x.ComponentFitmentId }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired(); e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired(); e.Property(x => x.RequestFingerprint).HasColumnType("character(64)");
            e.HasOne(x => x.ComponentFitment).WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentFitmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReversedByEmployee).WithMany().HasForeignKey(x => x.ReversedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ActualBom>(e =>
        {
            e.ToTable("actual_boms"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CompanyId, x.JobOrderId }).IsUnique();
            e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ActualBomEntry>(e =>
        {
            e.ToTable("actual_bom_entries", t => t.HasCheckConstraint("CK_actual_bom_entry_source", "num_nonnulls(\"ComponentFitmentId\",\"ComponentFitmentReversalId\")=1 AND ((\"EntryKind\"='FITMENT' AND \"QuantityBase\">0 AND \"AcceptedMaterialValue\">=0 AND \"AllocatedChargeValue\">=0 AND \"TotalAcceptedValue\">=0) OR (\"EntryKind\"='REVERSAL' AND \"QuantityBase\"<0 AND \"AcceptedMaterialValue\"<=0 AND \"AllocatedChargeValue\"<=0 AND \"TotalAcceptedValue\"<=0))"));
            e.HasKey(x => x.Id); e.HasIndex(x => new { x.CompanyId, x.ActualBomId, x.OccurredAt });
            e.HasIndex(x => new { x.CompanyId, x.ComponentFitmentId }).IsUnique().HasFilter("\"ComponentFitmentId\" IS NOT NULL");
            e.HasIndex(x => new { x.CompanyId, x.ComponentFitmentReversalId }).IsUnique().HasFilter("\"ComponentFitmentReversalId\" IS NOT NULL");
            e.Property(x => x.EntryKind).HasMaxLength(20).IsRequired(); e.Property(x => x.QuantityBase).HasPrecision(24, 6);
            e.Property(x => x.GrnNumberSnapshot).HasMaxLength(80); e.Property(x => x.VendorBillNumberSnapshot).HasMaxLength(120);
            e.Property(x => x.AcceptedMaterialValue).HasPrecision(24, 6); e.Property(x => x.AllocatedChargeValue).HasPrecision(24, 6); e.Property(x => x.TotalAcceptedValue).HasPrecision(24, 6);
            e.HasOne(x => x.ActualBom).WithMany(x => x.Entries).HasForeignKey(x => new { x.CompanyId, x.ActualBomId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ComponentFitment).WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentFitmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ComponentFitmentReversal).WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentFitmentReversalId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaterialIssueLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.VendorBillLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.VendorBillLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockPostingBatch>(e => e.HasOne(x => x.ComponentFitment).WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentFitmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict));
        modelBuilder.Entity<StockMovement>(e => e.HasOne(x => x.ComponentFitment).WithMany().HasForeignKey(x => new { x.CompanyId, x.ComponentFitmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict));
        modelBuilder.Entity<VendorBillCostAllocation>(e => e.Property(x => x.AllocatedChargeValue).HasPrecision(24, 6));
    }
}