using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<InventorySerial> InventorySerials => Set<InventorySerial>();
    public DbSet<GoodsReceiptLineSerial> GoodsReceiptLineSerials => Set<GoodsReceiptLineSerial>();

    private static void ConfigureStoresPart2(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GateEntryLine>()
            .HasAlternateKey(x => new { x.CompanyId, x.Id })
            .HasName("AK_gate_entry_lines_CompanyId_Id");

        ConfigureGoodsReceipts(modelBuilder);
        ConfigureGoodsReceiptLines(modelBuilder);
        ConfigureInventorySerials(modelBuilder);
        ConfigureGoodsReceiptLineSerials(modelBuilder);

        modelBuilder.Entity<StoresDocumentStatusHistory>()
            .HasOne(x => x.GoodsReceipt)
            .WithMany()
            .HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureGoodsReceipts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts", table =>
            {
                table.HasCheckConstraint("CK_goods_receipt_document_kind", "\"DocumentKind\" IN ('NORMAL','REVERSAL')");
                table.HasCheckConstraint("CK_goods_receipt_reversal", "(\"DocumentKind\"='NORMAL' AND \"ReversesGoodsReceiptId\" IS NULL AND \"ReversalReason\" IS NULL) OR (\"DocumentKind\"='REVERSAL' AND \"ReversesGoodsReceiptId\" IS NOT NULL AND length(trim(coalesce(\"ReversalReason\",'')))>0)");
                table.HasCheckConstraint("CK_goods_receipt_status", "\"Status\" IN ('DRAFT','FINALIZED')");
                table.HasCheckConstraint("CK_goods_receipt_finalization", "(\"Status\"='DRAFT' AND \"FinalizedAt\" IS NULL AND \"FinalizedByEmployeeId\" IS NULL) OR (\"Status\"='FINALIZED' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedByEmployeeId\" IS NOT NULL)");
                table.HasCheckConstraint("CK_goods_receipt_bill", "length(trim(\"VendorBillNumber\"))>0 AND \"VendorBillDate\" IS NOT NULL");
                table.HasCheckConstraint("CK_goods_receipt_iso_json", "jsonb_typeof(\"IsoReceiptVerificationJson\")='object'");
                table.HasCheckConstraint("CK_goods_receipt_configuration_json", "jsonb_typeof(\"ConfigurationSnapshotJson\")='object'");
                table.HasCheckConstraint("CK_goods_receipt_configuration_hash", "\"ConfigurationSnapshotHash\" ~ '^[0-9a-f]{64}$'");
                table.HasCheckConstraint("CK_goods_receipt_request_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
                table.HasCheckConstraint("CK_goods_receipt_qc_days", "\"QcCompletionDaysSnapshot\" > 0");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasAlternateKey(x => new { x.GateEntryId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.GrnNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.ReversesGoodsReceiptId).IsUnique()
                .HasFilter("\"DocumentKind\"='REVERSAL' AND \"Status\"='FINALIZED'");
            entity.HasIndex(x => new { x.CompanyId, x.PurchaseOrderId, x.ReceivedAt }).IsDescending(false, false, true);
            entity.HasIndex(x => new { x.CompanyId, x.VendorBillNumber });
            entity.HasIndex(x => new { x.CompanyId, x.QcDueAt });
            entity.HasIndex(x => x.GateEntryId);
            entity.Property(x => x.GrnNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DocumentKind).HasMaxLength(20).HasDefaultValue("NORMAL").IsRequired();
            entity.Property(x => x.ReversalReason).HasMaxLength(1000);
            entity.Property(x => x.VendorNameSnapshot).HasMaxLength(240).IsRequired();
            entity.Property(x => x.VendorBillNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.VendorDcNumberSnapshot).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ModeOfTransportSnapshot).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsoReceiptVerificationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ConfigurationSnapshotJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ConfigurationSnapshotHash).HasColumnType("character(64)").IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT").IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.ReversesGoodsReceipt).WithMany().HasForeignKey(x => x.ReversesGoodsReceiptId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GateEntry).WithMany().HasForeignKey(x => new { x.CompanyId, x.GateEntryId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReceivedByEmployee).WithMany().HasForeignKey(x => x.ReceivedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinalizedByEmployee).WithMany().HasForeignKey(x => x.FinalizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcCompletionDaysConfigVersion).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcCompletionDaysConfigVersionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGoodsReceiptLines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.ToTable("goods_receipt_lines", table =>
            {
                table.HasCheckConstraint("CK_goods_receipt_line_quantities", "\"PoOrderedQuantitySnapshot\">=0 AND \"PriorEffectiveReceivedQuantitySnapshot\">=0 AND \"RemainingPoQuantitySnapshot\">=0 AND \"DeliveredQuantitySnapshot\">0 AND \"ReceivedQuantity\">0 AND \"ExcessRejectedQuantity\">=0");
                table.HasCheckConstraint("CK_goods_receipt_line_reconciliation", "\"ReceivedQuantity\"+\"ExcessRejectedQuantity\"=\"DeliveredQuantitySnapshot\" AND \"ReceivedQuantity\"<=\"RemainingPoQuantitySnapshot\" AND \"RemainingPoQuantitySnapshot\"=\"PoOrderedQuantitySnapshot\"-\"PriorEffectiveReceivedQuantitySnapshot\"");
                table.HasCheckConstraint("CK_goods_receipt_line_excess", "(\"ExcessRejectedQuantity\"=0 AND \"ExcessDisposition\" IS NULL) OR (\"ExcessRejectedQuantity\">0 AND \"ExcessDisposition\"='PENDING_RETURNABLE_DC')");
                table.HasCheckConstraint("CK_goods_receipt_line_gst", "\"GstPercentageSnapshot\" BETWEEN 0 AND 100");
                table.HasCheckConstraint("CK_goods_receipt_line_serial_mode", "\"SerialCaptureModeSnapshot\" IN ('REQUIRED','OPTIONAL')");
                table.HasCheckConstraint("CK_goods_receipt_line_rate", "\"LineValueSnapshot\">=0 AND \"UnitRateSnapshot\">=0 AND \"UnitRateSnapshot\"=\"LineValueSnapshot\"/\"ReceivedQuantity\"");
                table.HasCheckConstraint("CK_goods_receipt_line_warranty", "\"InitialWarrantyExpiryDate\"=\"BillWarrantyLimitDate\"");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.GoodsReceiptId, x.LineNumber }).IsUnique();
            entity.HasIndex(x => new { x.GoodsReceiptId, x.GateEntryLineId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.PurchaseOrderLineId });
            entity.HasIndex(x => new { x.CompanyId, x.ItemId });
            entity.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ItemNameSnapshot).HasMaxLength(240).IsRequired();
            entity.Property(x => x.ItemCategoryCodeSnapshot).HasMaxLength(20).IsRequired();
            entity.Property(x => x.HsnSacCodeSnapshot).HasMaxLength(30).IsRequired();
            entity.Property(x => x.GstPercentageSnapshot).HasPrecision(8, 4);
            entity.Property(x => x.ModelSnapshot).HasMaxLength(160);
            entity.Property(x => x.ManufacturerPartNumberSnapshot).HasMaxLength(160);
            entity.Property(x => x.ManufacturerMakeSnapshot).HasMaxLength(160);
            entity.Property(x => x.UomSnapshot).HasMaxLength(32).IsRequired();
            foreach (var name in new[] { nameof(GoodsReceiptLine.PoOrderedQuantitySnapshot), nameof(GoodsReceiptLine.PriorEffectiveReceivedQuantitySnapshot), nameof(GoodsReceiptLine.RemainingPoQuantitySnapshot), nameof(GoodsReceiptLine.DeliveredQuantitySnapshot), nameof(GoodsReceiptLine.ReceivedQuantity), nameof(GoodsReceiptLine.ExcessRejectedQuantity), nameof(GoodsReceiptLine.LineValueSnapshot), nameof(GoodsReceiptLine.UnitRateSnapshot), nameof(GoodsReceiptLine.SerialThresholdValueSnapshot) })
                entity.Property<decimal>(name).HasPrecision(24, 6);
            entity.Property(x => x.ExcessRejectedQuantity).HasDefaultValue(0m);
            entity.Property(x => x.ExcessDisposition).HasMaxLength(40);
            entity.Property(x => x.SerialCaptureModeSnapshot).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.HasOne(x => x.GoodsReceipt).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GateEntryLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GateEntryLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ItemCategorySnapshot).WithMany().HasForeignKey(x => x.ItemCategoryIdSnapshot).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SerialThresholdConfigVersion).WithMany().HasForeignKey(x => new { x.CompanyId, x.SerialThresholdConfigVersionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SerialOverrideSetting).WithMany().HasForeignKey(x => new { x.CompanyId, x.SerialOverrideSettingId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcRouteSnapshot).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcRouteIdSnapshot }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcHoldConditionLocationSnapshot).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcHoldConditionLocationIdSnapshot }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventorySerials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventorySerial>(entity =>
        {
            entity.ToTable("inventory_serials", table =>
                table.HasCheckConstraint("CK_inventory_serial_values", "length(trim(\"StoredSerialNumber\"))>0 AND length(trim(\"NormalizedStoredSerialNumber\"))>0"));
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.NormalizedStoredSerialNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.ItemId, x.StoredSerialNumber });
            entity.Property(x => x.StoredSerialNumber).HasMaxLength(300).IsRequired();
            entity.Property(x => x.NormalizedStoredSerialNumber).HasMaxLength(300).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FirstCapturedByEmployee).WithMany().HasForeignKey(x => x.FirstCapturedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGoodsReceiptLineSerials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GoodsReceiptLineSerial>(entity =>
        {
            entity.ToTable("goods_receipt_line_serials", table =>
            {
                table.HasCheckConstraint("CK_goods_receipt_line_serial_ordinal", "\"SerialOrdinal\">0");
                table.HasCheckConstraint("CK_goods_receipt_line_serial_values", "length(trim(\"EnteredSerialNumber\"))>0 AND length(trim(\"StoredSerialNumberSnapshot\"))>0");
                table.HasCheckConstraint("CK_goods_receipt_line_serial_disposition", "\"ReceiptDisposition\" IN ('QC_INSPECTION','EXCESS_PENDING_RETURN')");
                table.HasCheckConstraint("CK_goods_receipt_line_serial_disambiguation", "(\"DisambiguationApplied\" AND \"DuplicateWarningAcknowledged\" AND length(trim(coalesce(\"DisambiguationReason\",'')))>0) OR (NOT \"DisambiguationApplied\" AND \"DisambiguationReason\" IS NULL)");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.GoodsReceiptLineId, x.SerialOrdinal }).IsUnique();
            entity.HasIndex(x => new { x.GoodsReceiptLineId, x.InventorySerialId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.ItemId, x.StoredSerialNumberSnapshot });
            entity.HasIndex(x => x.InventorySerialId);
            entity.Property(x => x.EnteredSerialNumber).HasMaxLength(200).IsRequired();
            entity.Property(x => x.StoredSerialNumberSnapshot).HasMaxLength(300).IsRequired();
            entity.Property(x => x.ReceiptDisposition).HasMaxLength(30).IsRequired();
            entity.Property(x => x.DisambiguationApplied).HasDefaultValue(false);
            entity.Property(x => x.DuplicateWarningAcknowledged).HasDefaultValue(false);
            entity.Property(x => x.DisambiguationReason).HasMaxLength(500);
            entity.HasOne(x => x.GoodsReceiptLine).WithMany(x => x.Serials).HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CapturedByEmployee).WithMany().HasForeignKey(x => x.CapturedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
