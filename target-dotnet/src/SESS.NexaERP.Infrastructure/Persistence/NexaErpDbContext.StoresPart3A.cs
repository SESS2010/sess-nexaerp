using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private const string job_orders = "job_orders";
    private const string material_issue_requests = "material_issue_requests";
    private const string material_issue_request_lines = "material_issue_request_lines";
    private const string delivery_challans = "delivery_challans";
    private const string delivery_challan_lines = "delivery_challan_lines";
    private const string qc_inspections = "qc_inspections";
    private const string qc_inspection_revisions = "qc_inspection_revisions";
    private const string qc_inspection_parameter_results = "qc_inspection_parameter_results";
    private const string qc_inspection_serial_dispositions = "qc_inspection_serial_dispositions";
    private const string stores_approval_history = "stores_approval_history";
    private const string jsonb = "jsonb";
    private static string character(int length) => $"character({length})";
    public DbSet<QcInspection> QcInspections => Set<QcInspection>();
    public DbSet<QcInspectionRevision> QcInspectionRevisions => Set<QcInspectionRevision>();
    public DbSet<QcInspectionParameterResult> QcInspectionParameterResults => Set<QcInspectionParameterResult>();
    public DbSet<QcInspectionSerialDisposition> QcInspectionSerialDispositions => Set<QcInspectionSerialDisposition>();
    public DbSet<JobOrder> JobOrders => Set<JobOrder>();
    public DbSet<MaterialIssueRequest> MaterialIssueRequests => Set<MaterialIssueRequest>();
    public DbSet<MaterialIssueRequestLine> MaterialIssueRequestLines => Set<MaterialIssueRequestLine>();
    public DbSet<StoresApprovalHistory> StoresApprovalHistories => Set<StoresApprovalHistory>();
    public DbSet<DeliveryChallan> DeliveryChallans => Set<DeliveryChallan>();
    public DbSet<DeliveryChallanLine> DeliveryChallanLines => Set<DeliveryChallanLine>();

    private static void ConfigureStoresPart3A(ModelBuilder modelBuilder)
    {
        ConfigureJobOrders(modelBuilder);
        ConfigureMaterialIssues(modelBuilder);
        ConfigureDeliveryChallans(modelBuilder);
        ConfigureDeliveryChallanLines(modelBuilder);
        ConfigureQc(modelBuilder);
        ConfigureStoresApprovalHistory(modelBuilder);
        ConfigurePart3AStatusHistory(modelBuilder);
    }
    private static void ConfigureJobOrders(ModelBuilder m)
    {
        m.Entity<JobOrder>(e => {
            e.ToTable(job_orders); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.JobOrderNumber }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.MachineSerial }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.Status, x.JobOrderDate });
            e.Property(x => x.JobOrderNumber).HasMaxLength(50).IsRequired(); e.Property(x => x.MachineModel).HasMaxLength(160).IsRequired();
            e.Property(x => x.MachineSerial).HasMaxLength(100).IsRequired(); e.Property(x => x.CustomerName).HasMaxLength(240).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired(); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestFingerprint).HasColumnType(character(64)).IsRequired(); e.Property(x => x.Version).IsConcurrencyToken();
        });
    }
    private static void ConfigureMaterialIssues(ModelBuilder m)
    {
        m.Entity<MaterialIssueRequest>(e => {
            e.ToTable(material_issue_requests); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.RequestNumber }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.Status, x.RequiredDate }); e.HasIndex(x => x.JobOrderId);
            e.Property(x => x.RequestNumber).HasMaxLength(50).IsRequired(); e.Property(x => x.Purpose).HasMaxLength(30).IsRequired(); e.Property(x => x.DestinationType).HasMaxLength(20).IsRequired(); e.Property(x => x.DestinationNameSnapshot).HasMaxLength(240).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.ApprovalRouteSnapshotJson).HasColumnType(jsonb).IsRequired(); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired(); e.Property(x => x.RequestFingerprint).HasColumnType(character(64)).IsRequired(); e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DestinationDepartment).WithMany().HasForeignKey(x => x.DestinationDepartmentId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.RequestingDepartment).WithMany().HasForeignKey(x => x.RequestingDepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RequestedByEmployee).WithMany().HasForeignKey(x => x.RequestedByEmployeeId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.ApprovedByEmployee).WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<MaterialIssueRequestLine>(e => {
            e.ToTable(material_issue_request_lines); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id }); e.HasIndex(x => new { x.MaterialIssueRequestId, x.LineNumber }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.ItemId });
            e.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired(); e.Property(x => x.ItemNameSnapshot).HasMaxLength(240).IsRequired(); e.Property(x => x.UomSnapshot).HasMaxLength(32).IsRequired(); e.Property(x => x.RequestedQuantity).HasPrecision(24, 6); e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasOne(x => x.MaterialIssueRequest).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
    }
    private static void ConfigureDeliveryChallans(ModelBuilder m)
    {
        m.Entity<DeliveryChallan>(e => {
            e.ToTable(delivery_challans); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.DcNumber }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.Status, x.ExpectedReturnDate }); e.HasIndex(x => x.ParentDeliveryChallanId); e.HasIndex(x => x.MaterialIssueRequestId); e.HasIndex(x => x.JobOrderId);
            e.Property(x => x.DcNumber).HasMaxLength(50).IsRequired(); e.Property(x => x.Direction).HasMaxLength(20).IsRequired(); e.Property(x => x.DcType).HasMaxLength(20).IsRequired(); e.Property(x => x.Purpose).HasMaxLength(30).IsRequired(); e.Property(x => x.DestinationNameSnapshot).HasMaxLength(240).IsRequired(); e.Property(x => x.ExternalReferenceNumber).HasMaxLength(120);
            e.Property(x => x.DispatchEvidenceJson).HasColumnType(jsonb).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.Property(x => x.ApprovalRouteSnapshotJson).HasColumnType(jsonb); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired(); e.Property(x => x.RequestFingerprint).HasColumnType(character(64)).IsRequired(); e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.ParentDeliveryChallan).WithMany().HasForeignKey(x => new { x.CompanyId, x.ParentDeliveryChallanId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.MaterialIssueRequest).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.HandledByEmployee).WithMany().HasForeignKey(x => x.HandledByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
    private static void ConfigureDeliveryChallanLines(ModelBuilder m)
    {
        m.Entity<DeliveryChallanLine>(e => {
            e.ToTable(delivery_challan_lines); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.DeliveryChallanId, x.LineNumber }).IsUnique();
            e.Property(x => x.ItemCodeSnapshot).HasMaxLength(80).IsRequired(); e.Property(x => x.UomSnapshot).HasMaxLength(32).IsRequired(); e.Property(x => x.Quantity).HasPrecision(24, 6);
            ConfigureDeliveryChallanLineRelationships(e);
        });
    }
    private static void ConfigureDeliveryChallanLineRelationships(EntityTypeBuilder<DeliveryChallanLine> e)
    {
        e.HasOne(x => x.DeliveryChallan).WithMany(x => x.Lines).HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ParentDeliveryChallanLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.ParentDeliveryChallanLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.MaterialIssueRequestLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.GoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ReplacementGoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.ReplacementGoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.WeightUom).WithMany().HasForeignKey(x => x.WeightUomId).OnDelete(DeleteBehavior.Restrict);
    }
    private static void ConfigureQc(ModelBuilder m)
    {
        m.Entity<QcInspection>(e => {
            e.ToTable(qc_inspections); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id });
            e.HasIndex(x => new { x.CompanyId, x.InspectionNumber }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).IsUnique().HasFilter(@"""GoodsReceiptLineLotAllocationId"" IS NOT NULL"); e.HasIndex(x => x.GoodsReceiptLineId); e.HasIndex(x => x.DeliveryChallanLineId).IsUnique(); e.Property(x => x.InspectionNumber).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.GoodsReceiptLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.GoodsReceiptLineLotAllocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.GoodsReceiptLineLotAllocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.DeliveryChallanLine).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanLineId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<QcInspectionRevision>(e => {
            e.ToTable(qc_inspection_revisions); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id }); e.HasIndex(x => new { x.QcInspectionId, x.RevisionNumber }).IsUnique(); e.HasIndex(x => x.RevisesRevisionId).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.Status, x.InspectionStartedAt });
            e.Property(x => x.RevisionKind).HasMaxLength(20).IsRequired(); e.Property(x => x.CorrectionReason).HasMaxLength(1000); e.Property(x => x.InspectorBasis).HasMaxLength(30).IsRequired(); e.Property(x => x.FallbackReason).HasMaxLength(1000); e.Property(x => x.Decision).HasMaxLength(30).IsRequired(); e.Property(x => x.Status).HasMaxLength(20).IsRequired(); e.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired(); e.Property(x => x.RequestFingerprint).HasColumnType(character(64)).IsRequired(); e.Property(x => x.Version).IsConcurrencyToken();
            foreach(var n in new[]{nameof(QcInspectionRevision.InspectedQuantity),nameof(QcInspectionRevision.AcceptedQuantity),nameof(QcInspectionRevision.RejectedQuantity),nameof(QcInspectionRevision.DiscrepancyPendingQuantity)}) e.Property<decimal>(n).HasPrecision(24,6);
            ConfigureQcRevisionRelationships(e);
        });
        m.Entity<QcInspectionParameterResult>(e => {
            e.ToTable(qc_inspection_parameter_results); e.HasKey(x => x.Id); e.HasAlternateKey(x => new { x.CompanyId, x.Id }); e.HasIndex(x => new { x.QcInspectionRevisionId, x.QcInspectionPolicyId, x.SampleOrdinal }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.ParameterCodeSnapshot, x.Result });
            e.Property(x => x.ParameterCodeSnapshot).HasMaxLength(100).IsRequired(); e.Property(x => x.MeasurementUomCodeSnapshot).HasMaxLength(32).IsRequired(); e.Property(x => x.LowerLimitSnapshot).HasPrecision(24,6); e.Property(x => x.UpperLimitSnapshot).HasPrecision(24,6); e.Property(x => x.InspectionMethodSnapshot).HasMaxLength(200).IsRequired(); e.Property(x => x.ObservedNumericValue).HasPrecision(24,6); e.Property(x => x.ObservedTextValue).HasMaxLength(500); e.Property(x => x.Result).HasMaxLength(20).IsRequired(); e.Property(x => x.Remarks).HasMaxLength(1000);
            e.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.QcInspectionPolicy).WithMany().HasForeignKey(x => x.QcInspectionPolicyId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.MeasurementUomSnapshot).WithMany().HasForeignKey(x => x.MeasurementUomIdSnapshot).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.ObservedByEmployee).WithMany().HasForeignKey(x => x.ObservedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<QcInspectionSerialDisposition>(e => {
            e.ToTable(qc_inspection_serial_dispositions); e.HasKey(x => x.Id); e.HasIndex(x => new { x.QcInspectionRevisionId, x.InventorySerialId }).IsUnique(); e.HasIndex(x => new { x.CompanyId, x.InventorySerialId }); e.Property(x => x.Disposition).HasMaxLength(20).IsRequired(); e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.InventorySerial).WithMany().HasForeignKey(x => new { x.CompanyId, x.InventorySerialId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<DeliveryChallanLine>().HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
    private static void ConfigureQcRevisionRelationships(EntityTypeBuilder<QcInspectionRevision> e)
    {
        e.HasOne(x => x.QcInspection).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.RevisesRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.RevisesRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.InspectorEmployee).WithMany().HasForeignKey(x => x.InspectorEmployeeId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.FinalizedByEmployee).WithMany().HasForeignKey(x => x.FinalizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.AcceptedConditionLocation).WithMany().HasForeignKey(x => new { x.CompanyId, x.AcceptedConditionLocationId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.QcHoldConditionLocationSnapshot).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcHoldConditionLocationIdSnapshot }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.PendingReturnConditionLocationSnapshot).WithMany().HasForeignKey(x => new { x.CompanyId, x.PendingReturnConditionLocationIdSnapshot }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
    private static void ConfigureStoresApprovalHistory(ModelBuilder m)
    {
        m.Entity<StoresApprovalHistory>(e => {
            e.ToTable(stores_approval_history); e.HasKey(x => x.Id); e.HasIndex(x => x.CorrelationId).IsUnique(); e.HasIndex(x => new { x.MaterialIssueRequestId, x.ApprovalCycle, x.StepNumber }).IsUnique(); e.HasIndex(x => new { x.DeliveryChallanId, x.ApprovalCycle, x.StepNumber }).IsUnique(); e.HasIndex(x => new { x.ResolvedEmployeeId, x.OccurredAt });
            e.Property(x => x.Action).HasMaxLength(30).IsRequired(); e.Property(x => x.ResolvedRoleCode).HasMaxLength(100).IsRequired(); e.Property(x => x.SnapshotIdentity).HasMaxLength(100).IsRequired(); e.Property(x => x.Remarks).HasMaxLength(1000).IsRequired(); e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.MaterialIssueRequest).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.DeliveryChallan).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.ResolvedEmployee).WithMany().HasForeignKey(x => x.ResolvedEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePart3AStatusHistory(ModelBuilder m)
    {
        m.Entity<StoresDocumentStatusHistory>(e => {
            e.HasOne(x => x.QcInspectionRevision).WithMany().HasForeignKey(x => new { x.CompanyId, x.QcInspectionRevisionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => new { x.CompanyId, x.JobOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.MaterialIssueRequest).WithMany().HasForeignKey(x => new { x.CompanyId, x.MaterialIssueRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.DeliveryChallan).WithMany().HasForeignKey(x => new { x.CompanyId, x.DeliveryChallanId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
