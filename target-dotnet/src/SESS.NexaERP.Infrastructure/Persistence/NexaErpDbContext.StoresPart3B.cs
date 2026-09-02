using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<StockPostingBatch> StockPostingBatches => Set<StockPostingBatch>();

    private static void ConfigureStoresPart3B(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockPostingBatch>(entity =>
        {
            entity.ToTable("stock_posting_batches");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => x.ReversesPostingBatchId).IsUnique().HasFilter(@"""PostingKind""='REVERSAL'");
            entity.HasIndex(x => x.GoodsReceiptId); entity.HasIndex(x => x.QcInspectionRevisionId);
            entity.HasIndex(x => x.MaterialIssueRequestId); entity.HasIndex(x => x.DeliveryChallanId);
            entity.HasIndex(x => x.InventoryCustodyHandoffId); entity.HasIndex(x => x.InventoryOwnershipTransferId);
            entity.HasIndex(x => x.InventoryTransformationId); entity.HasIndex(x => x.InventoryConcessionId);
            entity.HasIndex(x => new { x.CompanyId, x.PostingDate });
            entity.Property(x => x.PostingKind).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ReferenceType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ReferenceNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.GoodsReceipt).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MaterialIssueRequest).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeliveryChallan).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryCustodyHandoff).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryCustodyHandoffId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryOwnershipTransfer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryOwnershipTransferId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryTransformation).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryConcession).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesPostingBatch).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesPostingBatchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PostedByEmployee).WithMany().HasForeignKey(x => x.PostedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements", table =>
            {
                table.HasCheckConstraint("CK_stock_movements_foundation3_schema", @"""LedgerSchemaVersion"" = 2");
                table.HasCheckConstraint("CK_stock_movements_lot_allocation_identity", @"""GoodsReceiptLineLotAllocationId"" IS NULL OR ""InventoryLotId"" IS NOT NULL");
            });
            entity.Property(x => x.QuantityIn).HasPrecision(24, 6).HasDefaultValue(0m);
            entity.Property(x => x.QuantityOut).HasPrecision(24, 6).HasDefaultValue(0m);
            entity.Property(x => x.LedgerSchemaVersion).HasDefaultValue((short)2);
            entity.Property(x => x.ConditionCode).HasMaxLength(30);
            entity.Property(x => x.MovementLeg).HasMaxLength(30);
            entity.Property(x => x.PostingIdentity).HasMaxLength(200);
            entity.HasIndex(x => new { x.StockPostingBatchId, x.BatchLineOrdinal }).IsUnique().HasFilter(@"""StockPostingBatchId"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.PostingIdentity }).IsUnique().HasFilter(@"""PostingIdentity"" IS NOT NULL");
            entity.HasIndex(x => x.ReversesStockMovementId).IsUnique().HasFilter(@"""ReversesStockMovementId"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.ItemId, x.WarehouseConditionLocationId, x.PostingDate, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventorySerialId, x.PostingDate, x.Id }).HasFilter(@"""InventorySerialId"" IS NOT NULL");
            entity.HasIndex(x => x.GoodsReceiptLineId); entity.HasIndex(x => x.QcInspectionRevisionId);
            entity.HasIndex(x => x.MaterialIssueRequestLineId); entity.HasIndex(x => x.DeliveryChallanLineId);
            entity.HasIndex(x => x.OriginGoodsReceiptLineId); entity.HasIndex(x => x.StockPostingBatchId);
            entity.HasIndex(x => x.GoodsReceiptLineLotAllocationId);
            entity.HasIndex(x => new { x.CompanyId, x.OwnershipAccountId, x.CustodyAssignmentId, x.InventoryProvenanceLayerId, x.InventoryLotId, x.InventorySerialId });
            entity.HasIndex(x => x.CustodyCaseLineId); entity.HasIndex(x => x.InventoryLotId);
            entity.HasIndex(x => x.QcInspectionLotDispositionId); entity.HasIndex(x => x.InventoryCustodyHandoffLineId);
            entity.HasIndex(x => x.InventoryOwnershipTransferLineId); entity.HasIndex(x => x.InventoryTransformationInputId);
            entity.HasIndex(x => x.InventoryTransformationOutputId); entity.HasIndex(x => x.InventoryConcessionAllocationId);
            entity.HasOne(x => x.WarehouseConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.WarehouseConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StockPostingBatch).WithMany().HasForeignKey(x => new { x.CompanyId, x.StockPostingBatchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MaterialIssueRequestLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeliveryChallanLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OriginGoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.OriginGoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OwnershipAccount).WithMany().HasForeignKey(x => new { x.CompanyId, x.OwnershipAccountId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustodyAssignment).WithMany().HasForeignKey(x => new { x.CompanyId, x.CustodyAssignmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustodyCaseLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.CustodyCaseLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionLotDisposition).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionLotDispositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryCustodyHandoffLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryCustodyHandoffLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryOwnershipTransferLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryOwnershipTransferLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryTransformationInput).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationInputId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryTransformationOutput).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationOutputId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryConcessionAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesStockMovement).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesStockMovementId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
