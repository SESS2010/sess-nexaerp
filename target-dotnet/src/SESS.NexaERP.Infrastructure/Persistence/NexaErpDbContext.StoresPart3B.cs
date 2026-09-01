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
            entity.HasOne(x => x.ReversesPostingBatch).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesPostingBatchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PostedByEmployee).WithMany().HasForeignKey(x => x.PostedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(x => x.QuantityIn).HasPrecision(24, 6).HasDefaultValue(0m);
            entity.Property(x => x.QuantityOut).HasPrecision(24, 6).HasDefaultValue(0m);
            entity.Property(x => x.LedgerSchemaVersion).HasDefaultValue((short)1);
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
            entity.HasOne(x => x.WarehouseConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.WarehouseConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StockPostingBatch).WithMany().HasForeignKey(x => new { x.CompanyId, x.StockPostingBatchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MaterialIssueRequestLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeliveryChallanLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OriginGoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.OriginGoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesStockMovement).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesStockMovementId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
