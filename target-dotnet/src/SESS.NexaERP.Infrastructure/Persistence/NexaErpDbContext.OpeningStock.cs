using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<OpeningStockImportStagingLine> OpeningStockImportStagingLines => Set<OpeningStockImportStagingLine>();
    public DbSet<OpeningStock> OpeningStocks => Set<OpeningStock>();
    public DbSet<OpeningStockLine> OpeningStockLines => Set<OpeningStockLine>();
    public DbSet<OpeningStockEvent> OpeningStockEvents => Set<OpeningStockEvent>();

    private static void ConfigureOpeningStock(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OpeningStockImportStagingLine>(e =>
        {
            e.ToTable("opening_stock_import_staging_lines", t =>
            {
                t.HasCheckConstraint("CK_opening_stock_staging_quantity", @"""Quantity"">0");
                t.HasCheckConstraint("CK_opening_stock_staging_rate", @"""UnitRate"">=0");
            });
            e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.Property(x => x.LineReference).HasMaxLength(160).IsRequired();
            e.Property(x => x.LotNumber).HasMaxLength(160); e.Property(x => x.SerialNumber).HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(24, 6); e.Property(x => x.UnitRate).HasPrecision(24, 6);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => new { x.CompanyId, x.WarehouseId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => new { x.CompanyId, x.RackBinId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OpeningStock>(e =>
        {
            e.ToTable("opening_stocks", t =>
            {
                t.HasCheckConstraint("CK_opening_stock_period", @"""PeriodStart""<=""PeriodEnd""");
                t.HasCheckConstraint("CK_opening_stock_status", @"""Status"" IN ('COUNTED','VALUED','POSTED')");
                t.HasCheckConstraint("CK_opening_stock_separate_actors", @"""ValuedByEmployeeId"" IS NULL OR (""CountedByEmployeeId""<>""ValuedByEmployeeId"" AND (""AuthorizedByEmployeeId"" IS NULL OR (""AuthorizedByEmployeeId""<>""CountedByEmployeeId"" AND ""AuthorizedByEmployeeId""<>""ValuedByEmployeeId"")))");
            });
            e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.PeriodStart, x.PeriodEnd }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.ImportBatchId }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.CountIdempotencyKey }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.ValueIdempotencyKey }).IsUnique().HasFilter(@"""ValueIdempotencyKey"" IS NOT NULL");
            e.HasIndex(x => new { x.CompanyId, x.AuthorizationIdempotencyKey }).IsUnique().HasFilter(@"""AuthorizationIdempotencyKey"" IS NOT NULL");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.CountActorRoleCode).HasMaxLength(100).IsRequired(); e.Property(x => x.CountRoleAssignmentType).HasMaxLength(20).IsRequired();
            e.Property(x => x.CountReason).HasMaxLength(1000).IsRequired(); e.Property(x => x.CountIdempotencyKey).HasMaxLength(100).IsRequired(); e.Property(x => x.CountRequestFingerprint).HasColumnType("character(64)").IsRequired();
            e.Property(x => x.ValueActorRoleCode).HasMaxLength(100); e.Property(x => x.ValueRoleAssignmentType).HasMaxLength(20); e.Property(x => x.ValueReason).HasMaxLength(1000); e.Property(x => x.ValueIdempotencyKey).HasMaxLength(100); e.Property(x => x.ValueRequestFingerprint).HasColumnType("character(64)");
            e.Property(x => x.AuthorizationActorRoleCode).HasMaxLength(100); e.Property(x => x.AuthorizationRoleAssignmentType).HasMaxLength(20); e.Property(x => x.AuthorizationReason).HasMaxLength(1000); e.Property(x => x.AuthorizationIdempotencyKey).HasMaxLength(100); e.Property(x => x.AuthorizationRequestFingerprint).HasColumnType("character(64)");
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.ImportBatch).WithMany().HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CountedByEmployee).WithMany().HasForeignKey(x => x.CountedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CountRoleAssignment).WithMany().HasForeignKey(x => x.CountRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ValuedByEmployee).WithMany().HasForeignKey(x => x.ValuedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ValueRoleAssignment).WithMany().HasForeignKey(x => x.ValueRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AuthorizedByEmployee).WithMany().HasForeignKey(x => x.AuthorizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AuthorizationRoleAssignment).WithMany().HasForeignKey(x => x.AuthorizationRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockPostingBatch).WithMany().HasForeignKey(x => new { x.CompanyId, x.StockPostingBatchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OpeningStockLine>(e =>
        {
            e.ToTable("opening_stock_lines", t =>
            {
                t.HasCheckConstraint("CK_opening_stock_line_values", @"""LineNumber"">0 AND ""Quantity"">0 AND ""UnitRate"">=0 AND ""LineValue""=""Quantity""*""UnitRate""");
                t.HasCheckConstraint("CK_opening_stock_line_posted_identity", @"num_nonnulls(""InventoryProvenanceLayerId"",""FifoInventoryCostLayerId"") IN (0,2)");
            });
            e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.OpeningStockId, x.LineNumber }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.OpeningStockId, x.LineReference }).IsUnique();
            e.HasIndex(x => x.ImportStagingLineId).IsUnique();
            e.Property(x => x.LineReference).HasMaxLength(160).IsRequired(); e.Property(x => x.LotNumber).HasMaxLength(160); e.Property(x => x.SerialNumber).HasMaxLength(300);
            e.Property(x => x.Quantity).HasPrecision(24, 6); e.Property(x => x.UnitRate).HasPrecision(24, 6); e.Property(x => x.LineValue).HasPrecision(24, 6);
            e.HasOne(x => x.OpeningStock).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.OpeningStockId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ImportStagingLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.ImportStagingLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => new { x.CompanyId, x.WarehouseId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RackBin).WithMany().HasForeignKey(x => new { x.CompanyId, x.RackBinId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.WarehouseConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.WarehouseConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FifoInventoryCostLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.FifoInventoryCostLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OpeningStockEvent>(e =>
        {
            e.ToTable("opening_stock_events"); e.HasKey(x => x.Id); e.HasIndex(x => x.CorrelationId).IsUnique();
            e.Property(x => x.Action).HasMaxLength(30).IsRequired(); e.Property(x => x.FromStatus).HasMaxLength(20); e.Property(x => x.ToStatus).HasMaxLength(20).IsRequired(); e.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired(); e.Property(x => x.ResolvedRoleAssignmentType).HasMaxLength(20).IsRequired(); e.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.OpeningStock).WithMany().HasForeignKey(x => new { x.CompanyId, x.OpeningStockId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ResolvedRoleAssignment).WithMany().HasForeignKey(x => x.ResolvedRoleAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
