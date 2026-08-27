using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<BusinessRuleConfigurationVersion> BusinessRuleConfigurationVersions => Set<BusinessRuleConfigurationVersion>();
    public DbSet<ItemCompanyInventorySetting> ItemCompanyInventorySettings => Set<ItemCompanyInventorySetting>();
    public DbSet<StoreCategoryRoute> StoreCategoryRoutes => Set<StoreCategoryRoute>();
    public DbSet<GateEntry> GateEntries => Set<GateEntry>();
    public DbSet<GateEntryLine> GateEntryLines => Set<GateEntryLine>();
    public DbSet<StoresDocumentStatusHistory> StoresDocumentStatusHistories => Set<StoresDocumentStatusHistory>();
    public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts => Set<NotificationDeliveryAttempt>();

    private static void ConfigureStoresPart1(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderLine>()
            .HasAlternateKey(x => new { x.PurchaseOrderId, x.Id })
            .HasName("AK_purchase_order_lines_PurchaseOrderId_Id");

        modelBuilder.Entity<WarehouseConditionLocation>()
            .ToTable("warehouse_condition_locations", table =>
                table.HasCheckConstraint(
                    "CK_warehouse_condition_code",
                    "\"ConditionCode\" IN ('AVAILABLE','QC_HOLD','REJECTED','QUARANTINE','RETURN_TO_VENDOR','PENDING_RETURNABLE_DC','SCRAP')"));

        ConfigureStoresConfiguration(modelBuilder);
        ConfigureStoresInventorySettings(modelBuilder);
        ConfigureGateEntries(modelBuilder);
        ConfigureStoresStatusHistory(modelBuilder);
        ConfigureNotifications(modelBuilder);
    }

    private static void ConfigureStoresConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessRuleConfigurationVersion>(entity =>
        {
            entity.ToTable("business_rule_configuration_versions", table =>
            {
                table.HasCheckConstraint("CK_business_rule_configuration_version_number", "\"VersionNumber\" > 0");
                table.HasCheckConstraint("CK_business_rule_configuration_value_type", "\"ValueType\" IN ('INTEGER','DECIMAL','BOOLEAN','TEXT')");
                table.HasCheckConstraint("CK_business_rule_configuration_role", "\"ChangedByRoleCode\" IN ('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR','IT_MANAGER')");
                table.HasCheckConstraint("CK_business_rule_configuration_json", "jsonb_typeof(\"NewValueJson\") IN ('number','boolean','string') AND (\"OldValueJson\" IS NULL OR jsonb_typeof(\"OldValueJson\") IN ('number','boolean','string'))");
                table.HasCheckConstraint("CK_business_rule_configuration_first_version", "(\"VersionNumber\" = 1 AND \"PreviousVersionId\" IS NULL AND \"OldValueJson\" IS NULL) OR (\"VersionNumber\" > 1 AND \"PreviousVersionId\" IS NOT NULL AND \"OldValueJson\" IS NOT NULL)");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.RuleKey, x.VersionNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.RuleKey, x.EffectiveFrom }).IsUnique().IsDescending(false, false, true);
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.Property(x => x.RuleKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ValueType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.OldValueJson).HasColumnType("jsonb");
            entity.Property(x => x.NewValueJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.UnitCode).HasMaxLength(30);
            entity.Property(x => x.ChangedByRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ChangeReason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreviousVersion).WithMany().HasForeignKey(x => x.PreviousVersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ChangedByEmployee).WithMany().HasForeignKey(x => x.ChangedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasData(StoresPart1SeedData.ConfigurationVersions);
        });
    }

    private static void ConfigureStoresInventorySettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemCompanyInventorySetting>(entity =>
        {
            entity.ToTable("item_company_inventory_settings", table =>
            {
                table.HasCheckConstraint("CK_item_company_inventory_category", "\"BarcodeCategoryCode\" IN ('ELE','REF','FAS','PLC','FAB','MEC')");
                table.HasCheckConstraint("CK_item_company_inventory_sequence", "\"BarcodeSequenceNumber\" > 0");
                table.HasCheckConstraint("CK_item_company_inventory_serial_mode", "\"SerialCaptureMode\" IN ('INHERIT','REQUIRED','OPTIONAL')");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.ItemId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.ErpBarcode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.BarcodeSequenceNumber }).IsUnique();
            entity.HasIndex(x => new { x.ItemId, x.IsActive });
            entity.Property(x => x.ErpBarcode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.BarcodeCategoryCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.BarcodeSymbology).HasMaxLength(30).HasDefaultValue("CODE128").IsRequired();
            entity.Property(x => x.SerialCaptureMode).HasMaxLength(20).HasDefaultValue("INHERIT").IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StoreCategoryRoute>(entity =>
        {
            entity.ToTable("store_category_routes", table =>
                table.HasCheckConstraint("CK_store_category_route_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\""));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.ItemCategoryId, x.EffectiveFrom }).IsUnique();
            entity.HasIndex(x => x.QcHoldConditionLocationId);
            entity.HasIndex(x => x.PendingReturnConditionLocationId);
            entity.HasIndex(x => x.DefaultAcceptedConditionLocationId);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.ItemCategory).WithMany().HasForeignKey(x => x.ItemCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcHoldConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcHoldConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PendingReturnConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.PendingReturnConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DefaultAcceptedConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.DefaultAcceptedConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGateEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GateEntry>(entity =>
        {
            entity.ToTable("gate_entries", table =>
            {
                table.HasCheckConstraint("CK_gate_entry_document_kind", "\"DocumentKind\" IN ('NORMAL','REVERSAL')");
                table.HasCheckConstraint("CK_gate_entry_reversal", "(\"DocumentKind\"='NORMAL' AND \"ReversesGateEntryId\" IS NULL AND \"ReversalReason\" IS NULL) OR (\"DocumentKind\"='REVERSAL' AND \"ReversesGateEntryId\" IS NOT NULL AND length(trim(coalesce(\"ReversalReason\",''))) > 0)");
                table.HasCheckConstraint("CK_gate_entry_status", "\"Status\" IN ('DRAFT','FINALIZED')");
                table.HasCheckConstraint("CK_gate_entry_finalization", "(\"Status\"='DRAFT' AND \"FinalizedAt\" IS NULL AND \"FinalizedByEmployeeId\" IS NULL) OR (\"Status\"='FINALIZED' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizedByEmployeeId\" IS NOT NULL)");
                table.HasCheckConstraint("CK_gate_entry_iso_json", "jsonb_typeof(\"IsoReceiptVerificationJson\")='object'");
                table.HasCheckConstraint("CK_gate_entry_request_fingerprint", "\"RequestFingerprint\" ~ '^[0-9a-fA-F]{64}$'");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.PurchaseOrderId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.GateEntryNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.ReversesGateEntryId).IsUnique().HasFilter("\"DocumentKind\"='REVERSAL' AND \"Status\"='FINALIZED'");
            entity.HasIndex(x => new { x.CompanyId, x.PurchaseOrderId, x.ArrivedAt }).IsDescending(false, false, true);
            entity.HasIndex(x => new { x.CompanyId, x.VendorId, x.VendorDcNumber });
            entity.Property(x => x.GateEntryNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DocumentKind).HasMaxLength(20).HasDefaultValue("NORMAL").IsRequired();
            entity.Property(x => x.ReversalReason).HasMaxLength(1000);
            entity.Property(x => x.VendorNameSnapshot).HasMaxLength(240).IsRequired();
            entity.Property(x => x.VendorDcNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.VehicleNumber).HasMaxLength(50);
            entity.Property(x => x.ModeOfTransport).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsoReceiptVerificationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT").IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.ReversesGateEntry).WithMany().HasForeignKey(x => x.ReversesGateEntryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReceivedByEmployee).WithMany().HasForeignKey(x => x.ReceivedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FinalizedByEmployee).WithMany().HasForeignKey(x => x.FinalizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GateEntryLine>(entity =>
        {
            entity.ToTable("gate_entry_lines", table =>
                table.HasCheckConstraint("CK_gate_entry_line_values", "\"LineNumber\" > 0 AND \"DeliveredQuantity\" > 0"));
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.GateEntryId, x.LineNumber }).IsUnique();
            entity.HasIndex(x => new { x.GateEntryId, x.PurchaseOrderLineId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.PurchaseOrderLineId });
            entity.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(x => x.UomSnapshot).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DeliveredQuantity).HasPrecision(24, 6);
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.HasOne(x => x.GateEntry).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.GateEntryId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<GateEntry>().WithMany().HasForeignKey(x => new { x.PurchaseOrderId, x.GateEntryId }).HasPrincipalKey(x => new { x.PurchaseOrderId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.PurchaseOrderLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseOrderLine>().WithMany().HasForeignKey(x => new { x.PurchaseOrderId, x.PurchaseOrderLineId }).HasPrincipalKey(x => new { x.PurchaseOrderId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureStoresStatusHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoresDocumentStatusHistory>(entity =>
        {
            entity.ToTable("stores_document_status_history", table =>
            {
                table.HasCheckConstraint("CK_stores_document_status_action", "\"Action\" IN ('CREATED','FINALIZED','REVERSED')");
                table.HasCheckConstraint("CK_stores_document_status_transition", "(\"Action\"='CREATED' AND \"FromStatus\" IS NULL AND \"ToStatus\"='DRAFT') OR (\"Action\"='FINALIZED' AND \"FromStatus\"='DRAFT' AND \"ToStatus\"='FINALIZED') OR (\"Action\"='REVERSED' AND \"FromStatus\"='FINALIZED' AND \"ToStatus\"='REVERSED')");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.GateEntryId, x.OccurredAt });
            entity.Property(x => x.FromStatus).HasMaxLength(30);
            entity.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.GateEntry).WithMany().HasForeignKey(x => new { x.CompanyId, x.GateEntryId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        ConfigureNotificationEvents(modelBuilder);
        ConfigureNotificationRecipients(modelBuilder);
        ConfigureNotificationDeliveryAttempts(modelBuilder);
    }

    private static void ConfigureNotificationEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEvent>(entity =>
        {
            entity.ToTable("notification_events", table =>
            {
                table.HasCheckConstraint("CK_notification_event_text", "length(trim(\"EventType\"))>0 AND length(trim(\"SourceEntityType\"))>0 AND length(trim(\"SourceReferenceSnapshot\"))>0 AND length(trim(\"TitleSnapshot\"))>0 AND length(trim(\"BodySnapshot\"))>0 AND length(trim(\"DeepLinkSnapshot\"))>0");
                table.HasCheckConstraint("CK_notification_event_roles", "cardinality(\"RecipientRoleCodes\") > 0");
                table.HasCheckConstraint("CK_notification_event_payload", "jsonb_typeof(\"PayloadJson\")='object'");
                table.HasCheckConstraint("CK_notification_event_status", "\"Status\" IN ('SCHEDULED','READY','ACTIVE','COMPLETED','CANCELLED','RECIPIENT_BLOCKED')");
                table.HasCheckConstraint("CK_notification_event_timestamps", "(\"Status\" IN ('SCHEDULED','READY') AND \"ActivatedAt\" IS NULL AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\" IN ('ACTIVE','RECIPIENT_BLOCKED') AND \"ActivatedAt\" IS NOT NULL AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\"='COMPLETED' AND \"ActivatedAt\" IS NOT NULL AND \"CompletedAt\" IS NOT NULL AND \"CancelledAt\" IS NULL AND \"CancelledBy\" IS NULL AND \"CancellationReason\" IS NULL) OR (\"Status\"='CANCELLED' AND \"CompletedAt\" IS NULL AND \"CancelledAt\" IS NOT NULL AND length(trim(coalesce(\"CancelledBy\",'')))>0 AND length(trim(coalesce(\"CancellationReason\",'')))>0)");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.CancellationKey }).IsUnique().HasFilter("\"CancellationKey\" IS NOT NULL AND \"Status\" IN ('SCHEDULED','READY','ACTIVE','RECIPIENT_BLOCKED')");
            entity.HasIndex(x => new { x.Status, x.NotBeforeAt, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.SourceEntityType, x.SourceEntityId, x.CreatedAt });
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SourceEntityType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SourceReferenceSnapshot).HasMaxLength(160).IsRequired();
            entity.Property(x => x.RecipientRoleCodes).HasColumnType("text[]").IsRequired();
            entity.Property(x => x.TitleSnapshot).HasMaxLength(300).IsRequired();
            entity.Property(x => x.BodySnapshot).IsRequired();
            entity.Property(x => x.DeepLinkSnapshot).HasMaxLength(500).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb").HasDefaultValue("{}").IsRequired();
            entity.Property(x => x.CancellationKey).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CreatedBy).IsRequired();
            entity.Property(x => x.CancelledBy).HasMaxLength(200);
            entity.Property(x => x.CancellationReason).HasMaxLength(1000);
            entity.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotificationRecipients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecipient>(entity =>
        {
            entity.ToTable("notification_recipients", table =>
            {
                table.HasCheckConstraint("CK_notification_recipient_roles", "cardinality(\"ResolvedRoleCodes\") > 0");
                table.HasCheckConstraint("CK_notification_recipient_read", "(\"ReadAt\" IS NULL AND \"ReadByEmployeeId\" IS NULL AND \"ReadCorrelationId\" IS NULL) OR (\"ReadAt\" IS NOT NULL AND \"ReadByEmployeeId\"=\"RecipientEmployeeId\" AND length(trim(coalesce(\"ReadCorrelationId\",'')))>0)");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.NotificationEventId, x.RecipientEmployeeId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.RecipientEmployeeId, x.ReadAt, x.InAppAvailableAt }).IsDescending(false, false, false, true);
            entity.HasIndex(x => x.NotificationEventId);
            entity.Property(x => x.ResolvedRoleCodes).HasColumnType("text[]").IsRequired();
            entity.Property(x => x.ReadCorrelationId).HasMaxLength(100);
            entity.HasOne(x => x.NotificationEvent).WithMany(x => x.Recipients).HasForeignKey(x => new { x.CompanyId, x.NotificationEventId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RecipientEmployee).WithMany().HasForeignKey(x => x.RecipientEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReadByEmployee).WithMany().HasForeignKey(x => x.ReadByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotificationDeliveryAttempts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationDeliveryAttempt>(entity =>
        {
            entity.ToTable("notification_delivery_attempts", table =>
            {
                table.HasCheckConstraint("CK_notification_delivery_attempt_number", "\"AttemptNumber\" > 0");
                table.HasCheckConstraint("CK_notification_delivery_channel", "\"Channel\" IN ('IN_APP','EMAIL')");
                table.HasCheckConstraint("CK_notification_delivery_status", "\"Status\" IN ('SENT','FAILED')");
                table.HasCheckConstraint("CK_notification_delivery_result", "(\"Status\"='SENT' AND \"DeliveredAt\" IS NOT NULL AND \"ErrorCode\" IS NULL AND \"ErrorDetail\" IS NULL) OR (\"Status\"='FAILED' AND \"DeliveredAt\" IS NULL AND length(trim(coalesce(\"ErrorCode\",'')))>0 AND length(trim(coalesce(\"ErrorDetail\",'')))>0)");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.NotificationRecipientId, x.Channel, x.AttemptNumber }).IsUnique();
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.Channel, x.Status, x.AttemptedAt });
            entity.HasIndex(x => x.NotificationRecipientId);
            entity.Property(x => x.Channel).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProviderMessageId).HasMaxLength(300);
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorDetail).HasMaxLength(2000);
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasOne(x => x.NotificationRecipient).WithMany(x => x.DeliveryAttempts).HasForeignKey(x => new { x.CompanyId, x.NotificationRecipientId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
