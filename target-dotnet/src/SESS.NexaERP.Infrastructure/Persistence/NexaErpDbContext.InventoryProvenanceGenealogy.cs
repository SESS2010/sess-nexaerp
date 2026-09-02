using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<InventoryLotAttributeRevision> InventoryLotAttributeRevisions => Set<InventoryLotAttributeRevision>();
    public DbSet<InventoryProvenanceLayer> InventoryProvenanceLayers => Set<InventoryProvenanceLayer>();
    public DbSet<InventoryTransformation> InventoryTransformations => Set<InventoryTransformation>();
    public DbSet<InventoryTransformationInput> InventoryTransformationInputs => Set<InventoryTransformationInput>();
    public DbSet<InventoryTransformationOutput> InventoryTransformationOutputs => Set<InventoryTransformationOutput>();
    public DbSet<InventoryProvenanceEdge> InventoryProvenanceEdges => Set<InventoryProvenanceEdge>();
    public DbSet<InventorySerialIdentityRevision> InventorySerialIdentityRevisions => Set<InventorySerialIdentityRevision>();
    public DbSet<InventorySerialGenealogyEvent> InventorySerialGenealogyEvents => Set<InventorySerialGenealogyEvent>();
    public DbSet<InventorySerialGenealogyLink> InventorySerialGenealogyLinks => Set<InventorySerialGenealogyLink>();
    public DbSet<QcInspectionLotDisposition> QcInspectionLotDispositions => Set<QcInspectionLotDisposition>();
    public DbSet<InventoryConcession> InventoryConcessions => Set<InventoryConcession>();
    public DbSet<InventoryConcessionAllocation> InventoryConcessionAllocations => Set<InventoryConcessionAllocation>();
    public DbSet<InventoryConcessionAllocationSerial> InventoryConcessionAllocationSerials => Set<InventoryConcessionAllocationSerial>();
    public DbSet<InventoryProvenanceAnnotation> InventoryProvenanceAnnotations => Set<InventoryProvenanceAnnotation>();

    private static void ConfigureInventoryProvenanceGenealogy(ModelBuilder modelBuilder)
    {
        ConfigureLotAttributeRevisions(modelBuilder);
        ConfigureProvenanceLayers(modelBuilder);
        ConfigureTransformations(modelBuilder);
        ConfigureSerialGenealogy(modelBuilder);
        ConfigureQcLotDispositions(modelBuilder);
        ConfigureConcessions(modelBuilder);
        ConfigureProvenanceOrigins(modelBuilder);
        ConfigureProvenanceAnnotations(modelBuilder);
    }

    private static void ConfigureLotAttributeRevisions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryLotAttributeRevision>(entity =>
        {
            entity.ToTable("inventory_lot_attribute_revisions", table =>
            {
                table.HasCheckConstraint("CK_inventory_lot_attribute_revisions_revision", @"""RevisionNumber"" > 0");
                table.HasCheckConstraint("CK_inventory_lot_attribute_revisions_period", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" > ""EffectiveFrom""");
                table.HasCheckConstraint("CK_inventory_lot_attribute_revisions_json", @"jsonb_typeof(""AttributesJson"") = 'object'");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryLotId, x.RevisionNumber }).IsUnique();
            entity.HasIndex(x => x.SupersedesRevisionId).IsUnique().HasFilter(@"""SupersedesRevisionId"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.InventoryLotId }).IsUnique().HasFilter(@"""EffectiveTo"" IS NULL");
            entity.Property(x => x.AttributesJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SupersedesRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.SupersedesRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RecordedByEmployee).WithMany().HasForeignKey(x => x.RecordedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProvenanceLayers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryProvenanceLayer>(entity =>
        {
            entity.ToTable("inventory_provenance_layers", table =>
            {
                table.HasCheckConstraint("CK_inventory_provenance_layers_type", @"""LayerType"" IN ('RECEIPT','QC_ACCEPTED','QC_REJECTED','CONCESSION_ACCEPTED','CUSTODY','TRANSFORMATION_OUTPUT','RETURN','ADJUSTMENT')");
                table.HasCheckConstraint("CK_inventory_provenance_layers_quantity", @"""QuantityCreated"" > 0");
                table.HasCheckConstraint("CK_inventory_provenance_layers_status", @"""Status"" IN ('ACTIVE','REVERSED')");
                table.HasCheckConstraint("CK_inventory_provenance_layers_hash", @"""IdentityHash"" ~ '^[0-9a-f]{64}$'");
            });
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.IdentityHash }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.ItemId, x.InventoryLotId, x.InventorySerialId });
            entity.Property(x => x.LayerType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.QuantityCreated).HasPrecision(24, 6);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.IdentityHash).HasColumnType("character(64)").IsRequired();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTransformations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryTransformation>(entity =>
        {
            entity.ToTable("inventory_transformations", table =>
            {
                table.HasCheckConstraint("CK_inventory_transformations_type", @"""TransformationType"" IN ('KIT_ASSEMBLY','KIT_DISASSEMBLY','REPACK','UOM_CONVERSION','SUBASSEMBLY')");
                table.HasCheckConstraint("CK_inventory_transformations_status", @"""Status"" IN ('DRAFT','POSTED','REVERSED')");
                table.HasCheckConstraint("CK_inventory_transformations_posting", @"(""Status""='DRAFT' AND ""PostedAt"" IS NULL AND ""PostedByEmployeeId"" IS NULL) OR (""Status""<>'DRAFT' AND ""PostedAt"" IS NOT NULL AND ""PostedByEmployeeId"" IS NOT NULL)");
                table.HasCheckConstraint("CK_inventory_transformations_fingerprint", @"""RequestFingerprint"" ~ '^[0-9a-fA-F]{64}$'");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.TransformationNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.ReversesTransformationId).IsUnique().HasFilter(@"""ReversesTransformationId"" IS NOT NULL");
            entity.Property(x => x.TransformationNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.TransformationType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PostedByEmployee).WithMany().HasForeignKey(x => x.PostedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesTransformation).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesTransformationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryTransformationInput>(entity =>
        {
            entity.ToTable("inventory_transformation_inputs", table => table.HasCheckConstraint("CK_inventory_transformation_inputs_quantity", @"""Quantity"" > 0"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryTransformationId, x.LineNumber }).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(24, 6);
            entity.HasOne(x => x.InventoryTransformation).WithMany(x => x.Inputs).HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryTransformationOutput>(entity =>
        {
            entity.ToTable("inventory_transformation_outputs", table => table.HasCheckConstraint("CK_inventory_transformation_outputs_quantity", @"""Quantity"" > 0"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryTransformationId, x.LineNumber }).IsUnique();
            entity.HasIndex(x => x.OutputProvenanceLayerId).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(24, 6);
            entity.HasOne(x => x.InventoryTransformation).WithMany(x => x.Outputs).HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OutputProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.OutputProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryProvenanceEdge>(entity =>
        {
            entity.ToTable("inventory_provenance_edges", table =>
            {
                table.HasCheckConstraint("CK_inventory_provenance_edges_quantity", @"""Quantity"" > 0");
                table.HasCheckConstraint("CK_inventory_provenance_edges_distinct", @"""FromProvenanceLayerId"" <> ""ToProvenanceLayerId""");
            });
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.FromProvenanceLayerId, x.ToProvenanceLayerId, x.EdgeType }).IsUnique();
            entity.Property(x => x.EdgeType).HasMaxLength(40).IsRequired(); entity.Property(x => x.Quantity).HasPrecision(24, 6); entity.Property(x => x.AllocationBasis).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.InventoryTransformation).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FromProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.FromProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.ToProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSerialGenealogy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventorySerialIdentityRevision>(entity =>
        {
            entity.ToTable("inventory_serial_identity_revisions", table =>
            {
                table.HasCheckConstraint("CK_inventory_serial_identity_revisions_revision", @"""RevisionNumber"" > 0");
                table.HasCheckConstraint("CK_inventory_serial_identity_revisions_period", @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" > ""EffectiveFrom""");
                table.HasCheckConstraint("CK_inventory_serial_identity_revisions_normalized", @"length(btrim(""NormalizedSerialNumberSnapshot"")) > 0");
            });
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventorySerialId, x.RevisionNumber }).IsUnique();
            entity.HasIndex(x => x.SupersedesRevisionId).IsUnique().HasFilter(@"""SupersedesRevisionId"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.InventorySerialId }).IsUnique().HasFilter(@"""EffectiveTo"" IS NULL");
            entity.Property(x => x.StoredSerialNumberSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NormalizedSerialNumberSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SupersedesRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.SupersedesRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RecordedByEmployee).WithMany().HasForeignKey(x => x.RecordedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventorySerialGenealogyEvent>(entity =>
        {
            entity.ToTable("inventory_serial_genealogy_events", table =>
            {
                table.HasCheckConstraint("CK_inventory_serial_genealogy_events_type", @"""EventType"" IN ('CREATED','FITTED','REMOVED','REPLACED','TRANSFORMED','CORRECTED','CONCESSION_ACCEPTED','REVERSAL')");
                table.HasCheckConstraint("CK_inventory_serial_genealogy_events_reversal", @"(""EventType""='REVERSAL' AND ""ReversesEventId"" IS NOT NULL) OR (""EventType""<>'REVERSAL' AND ""ReversesEventId"" IS NULL)");
            });
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.CorrelationId }).IsUnique();
            entity.HasIndex(x => x.ReversesEventId).IsUnique().HasFilter(@"""ReversesEventId"" IS NOT NULL");
            entity.Property(x => x.EventType).HasMaxLength(40).IsRequired(); entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(64).IsRequired(); entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesEvent).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesEventId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventorySerialGenealogyLink>(entity =>
        {
            entity.ToTable("inventory_serial_genealogy_links", table =>
            {
                table.HasCheckConstraint("CK_inventory_serial_genealogy_links_identity", @"""FromInventorySerialId"" IS NOT NULL OR ""ToInventorySerialId"" IS NOT NULL OR ""FromProvenanceLayerId"" IS NOT NULL OR ""ToProvenanceLayerId"" IS NOT NULL");
                table.HasCheckConstraint("CK_inventory_serial_genealogy_links_serial_distinct", @"""FromInventorySerialId"" IS NULL OR ""ToInventorySerialId"" IS NULL OR ""FromInventorySerialId"" <> ""ToInventorySerialId""");
                table.HasCheckConstraint("CK_inventory_serial_genealogy_links_layer_distinct", @"""FromProvenanceLayerId"" IS NULL OR ""ToProvenanceLayerId"" IS NULL OR ""FromProvenanceLayerId"" <> ""ToProvenanceLayerId""");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.InventorySerialGenealogyEventId, x.RelationType });
            entity.Property(x => x.RelationType).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.InventorySerialGenealogyEvent).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialGenealogyEventId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.FromInventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.FromInventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToInventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.ToInventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FromProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.FromProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.ToProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureQcLotDispositions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QcInspectionLotDisposition>(entity =>
        {
            entity.ToTable("qc_inspection_lot_dispositions", table =>
            {
                table.HasCheckConstraint("CK_qc_inspection_lot_dispositions_quantities", @"""InspectedQuantity"" > 0 AND ""AcceptedQuantity"" >= 0 AND ""RejectedQuantity"" >= 0 AND ""DiscrepancyPendingQuantity"" >= 0 AND ""AcceptedQuantity"" + ""RejectedQuantity"" + ""DiscrepancyPendingQuantity"" = ""InspectedQuantity""");
                table.HasCheckConstraint("CK_qc_inspection_lot_dispositions_decision", @"(""Disposition""='ACCEPTED' AND ""AcceptedQuantity"">0 AND ""RejectedQuantity""=0 AND ""DiscrepancyPendingQuantity""=0) OR (""Disposition""='REJECTED' AND ""RejectedQuantity"">0 AND ""AcceptedQuantity""=0 AND ""DiscrepancyPendingQuantity""=0) OR (""Disposition""='DISCREPANCY_PENDING' AND ""DiscrepancyPendingQuantity"">0 AND ""AcceptedQuantity""=0 AND ""RejectedQuantity""=0)");
            });
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.QcInspectionRevisionId, x.GoodsReceiptLineLotAllocationId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.InventoryLotId, x.Disposition });
            foreach (var property in new[] { nameof(QcInspectionLotDisposition.InspectedQuantity), nameof(QcInspectionLotDisposition.AcceptedQuantity), nameof(QcInspectionLotDisposition.RejectedQuantity), nameof(QcInspectionLotDisposition.DiscrepancyPendingQuantity) })
                entity.Property<decimal>(property).HasPrecision(24, 6);
            entity.Property(x => x.Disposition).HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DestinationConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.DestinationConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureConcessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryConcession>(entity =>
        {
            entity.ToTable("inventory_concessions", table =>
            {
                table.HasCheckConstraint("CK_inventory_concessions_quantity", @"""RequestedQuantity"" > 0");
                table.HasCheckConstraint("CK_inventory_concessions_status", @"""Status"" IN ('DRAFT','APPROVED','REJECTED','REVERSED')");
                table.HasCheckConstraint("CK_inventory_concessions_decision", @"(""Status""='DRAFT' AND ""DecidedByEmployeeId"" IS NULL AND ""DecidedRoleCode"" IS NULL AND ""DecidedAt"" IS NULL AND ""DecisionReason"" IS NULL) OR (""Status""<>'DRAFT' AND ""DecidedByEmployeeId"" IS NOT NULL AND ""DecidedRoleCode""='TECHNICAL_DIRECTOR' AND ""DecidedAt"" IS NOT NULL AND length(btrim(""DecisionReason""))>0 AND ""DecidedByEmployeeId""<>""CreatedByEmployeeId"")");
                table.HasCheckConstraint("CK_inventory_concessions_reversal", @"(""Status""='REVERSED' AND ""ReversesConcessionId"" IS NOT NULL) OR (""Status""<>'REVERSED' AND ""ReversesConcessionId"" IS NULL)");
                table.HasCheckConstraint("CK_inventory_concessions_fingerprint", @"""RequestFingerprint"" ~ '^[0-9a-fA-F]{64}$'");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.ConcessionNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => x.ReversesConcessionId).IsUnique().HasFilter(@"""ReversesConcessionId"" IS NOT NULL");
            entity.Property(x => x.ConcessionNumber).HasMaxLength(60).IsRequired(); entity.Property(x => x.RequestedQuantity).HasPrecision(24, 6);
            entity.Property(x => x.FailedParameterSnapshot).HasMaxLength(200).IsRequired(); entity.Property(x => x.MeasuredValueSnapshot).HasMaxLength(500).IsRequired();
            entity.Property(x => x.TechnicalAcceptanceReason).HasMaxLength(2000).IsRequired(); entity.Property(x => x.IntendedUse).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired(); entity.Property(x => x.DecidedRoleCode).HasMaxLength(64); entity.Property(x => x.DecisionReason).HasMaxLength(1000);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired(); entity.Property(x => x.RequestFingerprint).HasColumnType("character(64)").IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionLotDisposition).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionLotDispositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QcInspectionParameterResult).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionParameterResultId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedByEmployee).WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DecidedByEmployee).WithMany().HasForeignKey(x => x.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesConcession).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReversesConcessionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryConcessionAllocation>(entity =>
        {
            entity.ToTable("inventory_concession_allocations", table => table.HasCheckConstraint("CK_inventory_concession_allocations_quantity", @"""Quantity"" > 0"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryConcessionId, x.GoodsReceiptLineLotAllocationId }).IsUnique();
            entity.HasIndex(x => x.AcceptedProvenanceLayerId).IsUnique().HasFilter(@"""AcceptedProvenanceLayerId"" IS NOT NULL");
            entity.Property(x => x.Quantity).HasPrecision(24, 6);
            entity.HasOne(x => x.InventoryConcession).WithMany(x => x.Allocations).HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryLot).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryLotId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RejectedProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.RejectedProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcceptedProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.AcceptedProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryConcessionAllocationSerial>(entity =>
        {
            entity.ToTable("inventory_concession_allocation_serials"); entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.InventoryConcessionAllocationId, x.InventorySerialId }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.InventorySerialId }).IsUnique();
            entity.HasOne(x => x.InventoryConcessionAllocation).WithMany(x => x.Serials).HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureProvenanceOrigins(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryProvenanceOrigin>(entity =>
        {
            entity.UseTpcMappingStrategy();
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryProvenanceLayerId, x.OriginRole }).IsUnique();
            entity.Property(x => x.OriginRole).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        });
        ConfigureOrigin<InventoryProvenanceGoodsReceiptLotOrigin>(modelBuilder, "inventory_provenance_goods_receipt_lot_origins");
        modelBuilder.Entity<InventoryProvenanceGoodsReceiptLotOrigin>().HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ConfigureOrigin<InventoryProvenanceCustodyCaseLineOrigin>(modelBuilder, "inventory_provenance_custody_case_line_origins");
        modelBuilder.Entity<InventoryProvenanceCustodyCaseLineOrigin>().HasOne(x => x.CustodyCaseLine).WithMany().HasForeignKey(x => x.CustodyCaseLineId).OnDelete(DeleteBehavior.Restrict);
        ConfigureOrigin<InventoryProvenanceTransformationOutputOrigin>(modelBuilder, "inventory_provenance_transformation_output_origins");
        modelBuilder.Entity<InventoryProvenanceTransformationOutputOrigin>().HasOne(x => x.InventoryTransformationOutput).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryTransformationOutputId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ConfigureOrigin<InventoryProvenanceQcDispositionOrigin>(modelBuilder, "inventory_provenance_qc_disposition_origins");
        modelBuilder.Entity<InventoryProvenanceQcDispositionOrigin>().HasOne(x => x.QcInspectionLotDisposition).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionLotDispositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        ConfigureOrigin<InventoryProvenanceConcessionAllocationOrigin>(modelBuilder, "inventory_provenance_concession_allocation_origins");
        modelBuilder.Entity<InventoryProvenanceConcessionAllocationOrigin>().HasOne(x => x.InventoryConcessionAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureOrigin<T>(ModelBuilder modelBuilder, string tableName) where T : InventoryProvenanceOrigin
    {
        modelBuilder.Entity<T>(entity =>
        {
            entity.ToTable(tableName);
        });
    }

    private static void ConfigureProvenanceAnnotations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryProvenanceAnnotation>(entity =>
        {
            entity.ToTable("inventory_provenance_annotations", table => table.HasCheckConstraint("CK_inventory_provenance_annotations_json", @"jsonb_typeof(""DetailsJson"")='object'"));
            entity.HasKey(x => x.Id); entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.InventoryProvenanceLayerId, x.AnnotationType, x.AnnotationCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.InventoryConcessionId });
            entity.Property(x => x.AnnotationType).HasMaxLength(40).IsRequired(); entity.Property(x => x.AnnotationCode).HasMaxLength(120).IsRequired(); entity.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasOne(x => x.InventoryProvenanceLayer).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryProvenanceLayerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InventoryConcession).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventoryConcessionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.InheritedFromAnnotation).WithMany().HasForeignKey(x => new { x.CompanyId, x.InheritedFromAnnotationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
