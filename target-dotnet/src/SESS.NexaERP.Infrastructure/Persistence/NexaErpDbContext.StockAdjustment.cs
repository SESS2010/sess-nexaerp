using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentLine> StockAdjustmentLines => Set<StockAdjustmentLine>();
    public DbSet<StockAdjustmentDecision> StockAdjustmentDecisions => Set<StockAdjustmentDecision>();

    // Keys, constraints and foreign keys live in 20260920210000_StockAdjustmentPosting; the model
    // maps the columns only, so the snapshot stays a plain column list.
    private static void ConfigureStockAdjustments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockAdjustment>(e =>
        {
            e.ToTable("stock_adjustments"); e.HasKey(x => x.Id);
            e.Property(x => x.AdjustmentNumber).HasMaxLength(40).IsRequired();
            e.Property(x => x.ReasonKind).HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
            e.Property(x => x.CounterEmployeeIdsJson).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.BackdateReason).HasMaxLength(1000);
            e.Property(x => x.ApprovalSnapshotJson).HasColumnType("jsonb");
            e.Property(x => x.PostingIdempotencyKey).HasMaxLength(100);
            e.Property(x => x.PostingRequestFingerprint).HasColumnType("character(64)");
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(160).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(160);
        });
        modelBuilder.Entity<StockAdjustmentLine>(e =>
        {
            e.ToTable("stock_adjustment_lines"); e.HasKey(x => x.Id);
            e.Property(x => x.LotNumber).HasMaxLength(160);
            e.Property(x => x.SerialNumber).HasMaxLength(300);
            e.Property(x => x.QuantityChange).HasPrecision(24, 6);
            e.Property(x => x.UnitValue).HasPrecision(24, 6);
            e.Property(x => x.AcceptedLineValue).HasPrecision(24, 6);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.Property(x => x.CreatedBy).HasMaxLength(160).IsRequired();
        });
        modelBuilder.Entity<StockAdjustmentDecision>(e =>
        {
            e.ToTable("stock_adjustment_decisions"); e.HasKey(x => x.Id);
            e.Property(x => x.Decision).HasMaxLength(20).IsRequired();
            e.Property(x => x.RoleCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(160).IsRequired();
        });
    }
}
