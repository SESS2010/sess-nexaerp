using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<RequestForQuotation> RequestForQuotations => Set<RequestForQuotation>();
    public DbSet<RequestForQuotationLine> RequestForQuotationLines => Set<RequestForQuotationLine>();
    public DbSet<RfqVendorInvitation> RfqVendorInvitations => Set<RfqVendorInvitation>();
    public DbSet<VendorQuotation> VendorQuotations => Set<VendorQuotation>();
    public DbSet<VendorQuotationLine> VendorQuotationLines => Set<VendorQuotationLine>();
    public DbSet<QuotationTechnicalVerification> QuotationTechnicalVerifications => Set<QuotationTechnicalVerification>();
    public DbSet<CommercialComparison> CommercialComparisons => Set<CommercialComparison>();
    public DbSet<CommercialComparisonLine> CommercialComparisonLines => Set<CommercialComparisonLine>();
    public DbSet<PurchaseTransactionApprovalHistory> PurchaseTransactionApprovalHistories => Set<PurchaseTransactionApprovalHistory>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseOrderHistory> PurchaseOrderHistories => Set<PurchaseOrderHistory>();
    public DbSet<MaterialFollowUpHandoff> MaterialFollowUpHandoffs => Set<MaterialFollowUpHandoff>();
    public DbSet<PurchaseTransactionStatusHistory> PurchaseTransactionStatusHistories => Set<PurchaseTransactionStatusHistory>();
    public DbSet<PurchaseTransactionApprovalPolicy> PurchaseTransactionApprovalPolicies => Set<PurchaseTransactionApprovalPolicy>();

    private static void ConfigureRev869B(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequestForQuotation>(entity =>
        {
            entity.ToTable("request_for_quotations", table =>
            {
                table.HasCheckConstraint("CK_rfqs_sequence_positive", "\"SequenceNumber\" > 0");
                table.HasCheckConstraint("CK_rfqs_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                table.HasCheckConstraint("CK_rfqs_status", "\"Status\" IN ('Draft','Issued','Closed','Cancelled')");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OrganizationId, x.RfqNumber }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.FinancialYear, x.SequenceNumber }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.PurchaseRequisitionId, x.Status });
            Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.RfqNumber), 64); Text(entity.Property(x => x.FinancialYear), 12);
            Text(entity.Property(x => x.CurrencyCode), 3); Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.IdempotencyKey), 200);
            entity.Property(x => x.SingleSourceJustification).HasMaxLength(2000); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestingDepartment).WithMany().HasForeignKey(x => x.RequestingDepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DeliveryWarehouse).WithMany().HasForeignKey(x => x.DeliveryWarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OwnerEmployee).WithMany().HasForeignKey(x => x.OwnerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RequestForQuotationLine>(entity =>
        {
            entity.ToTable("request_for_quotation_lines", table => table.HasCheckConstraint("CK_rfq_lines_quantities", "\"ApprovedQuantitySnapshot\" > 0 AND \"AlreadyOrderedQuantitySnapshot\" >= 0 AND \"OutstandingQuantitySnapshot\" >= 0 AND \"RfqQuantity\" > 0 AND \"RfqQuantity\" <= \"OutstandingQuantitySnapshot"));
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.RequestForQuotationId, x.LineNumber }).IsUnique(); entity.HasIndex(x => new { x.RequestForQuotationId, x.PurchaseRequirementHandoffId }).IsUnique();
            Text(entity.Property(x => x.PrNumberSnapshot), 64); Text(entity.Property(x => x.ItemCodeSnapshot), 100); Text(entity.Property(x => x.ItemNameSnapshot), 300); Text(entity.Property(x => x.UomSnapshot), 30); entity.Property(x => x.SpecificationSnapshot).HasMaxLength(2000);
            Money(entity.Property(x => x.ApprovedQuantitySnapshot)); Money(entity.Property(x => x.AlreadyOrderedQuantitySnapshot)); Money(entity.Property(x => x.OutstandingQuantitySnapshot)); Money(entity.Property(x => x.RfqQuantity)); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.RequestForQuotation).WithMany(x => x.Lines).HasForeignKey(x => x.RequestForQuotationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseRequirementHandoff).WithMany().HasForeignKey(x => x.PurchaseRequirementHandoffId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseRequisitionLine).WithMany().HasForeignKey(x => x.PurchaseRequisitionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RfqVendorInvitation>(entity =>
        {
            entity.ToTable("rfq_vendor_invitations", table => table.HasCheckConstraint("CK_rfq_invitation_status", "\"Status\" IN ('Issued','Submitted','Withdrawn','Cancelled')"));
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.RequestForQuotationId, x.VendorId }).IsUnique(); entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.IdempotencyKey), 200); entity.Property(x => x.VendorQualificationSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.RequestForQuotation).WithMany(x => x.Invitations).HasForeignKey(x => x.RequestForQuotationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorQuotation>(entity =>
        {
            entity.ToTable("vendor_quotations", table =>
            {
                table.HasCheckConstraint("CK_vendor_quotation_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                table.HasCheckConstraint("CK_vendor_quotation_late_authorization", "NOT \"IsLateSubmission\" OR (\"LateAuthorizedByEmployeeId\" IS NOT NULL AND length(trim(coalesce(\"LateAuthorizationRemarks\", ''))) > 0)");
                table.HasCheckConstraint("CK_vendor_quotation_total", "\"TotalPayableValue\" >= 0");
                table.HasCheckConstraint("CK_vendor_quotation_status", "\"Status\" IN ('Submitted','PendingTechnicalVerification','Superseded','Withdrawn','Rejected')");
            });
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.OrganizationId, x.QuotationNumber }).IsUnique(); entity.HasIndex(x => new { x.RfqVendorInvitationId, x.RevisionNumber }).IsUnique();
            entity.HasIndex(x => new { x.RootQuotationId, x.IsCurrentRevision }).IsUnique().HasFilter("\"IsCurrentRevision\" = TRUE"); entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
            Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.QuotationNumber), 64); Text(entity.Property(x => x.FinancialYear), 12); Text(entity.Property(x => x.VendorQuoteReference), 120); Text(entity.Property(x => x.CurrencyCode), 3); Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.IdempotencyKey), 200);
            entity.Property(x => x.LateAuthorizationRemarks).HasMaxLength(2000); entity.Property(x => x.PaymentTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.DeliveryTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.WarrantyTermsSnapshot).HasMaxLength(2000).IsRequired(); Money(entity.Property(x => x.TotalPayableValue)); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.RfqVendorInvitation).WithMany().HasForeignKey(x => x.RfqVendorInvitationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.PreviousRevision).WithMany().HasForeignKey(x => x.PreviousRevisionId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.LateAuthorizedByEmployee).WithMany().HasForeignKey(x => x.LateAuthorizedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VendorQuotationLine>(entity =>
        {
            entity.ToTable("vendor_quotation_lines", table =>
            {
                table.HasCheckConstraint("CK_vendor_quotation_line_quantity", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_vendor_quotation_line_values", "\"UnitRate\" >= 0 AND \"DiscountValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TaxableValue\" >= 0 AND \"CgstValue\" >= 0 AND \"SgstValue\" >= 0 AND \"IgstValue\" >= 0 AND \"CessValue\" >= 0 AND \"TotalPayableValue\" >= 0");
            });
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.VendorQuotationId, x.LineNumber }).IsUnique(); entity.HasIndex(x => new { x.VendorQuotationId, x.RequestForQuotationLineId }).IsUnique();
            foreach (var property in new[] { entity.Property(x => x.Quantity), entity.Property(x => x.UnitRate), entity.Property(x => x.DiscountValue), entity.Property(x => x.PackingForwarding), entity.Property(x => x.Freight), entity.Property(x => x.Insurance), entity.Property(x => x.OtherCharges), entity.Property(x => x.TaxableValue), entity.Property(x => x.CgstValue), entity.Property(x => x.SgstValue), entity.Property(x => x.IgstValue), entity.Property(x => x.CessValue), entity.Property(x => x.RoundOff), entity.Property(x => x.TotalPayableValue) }) Money(property);
            entity.Property(x => x.TaxRuleSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.VendorQuotation).WithMany(x => x.Lines).HasForeignKey(x => x.VendorQuotationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RequestForQuotationLine).WithMany().HasForeignKey(x => x.RequestForQuotationLineId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.TaxGstSetting).WithMany().HasForeignKey(x => x.TaxGstSettingId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuotationTechnicalVerification>(entity =>
        {
            entity.ToTable("quotation_technical_verifications", table => table.HasCheckConstraint("CK_quote_technical_status", "\"ComplianceStatus\" IN ('TechnicallyCompliant','TechnicallyRejected') AND length(trim(\"Remarks\")) > 0"));
            entity.HasKey(x => x.Id); entity.HasIndex(x => x.VendorQuotationLineId).IsUnique(); Text(entity.Property(x => x.ComplianceStatus), 40); entity.Property(x => x.ComplianceSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.Remarks).HasMaxLength(2000).IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.VendorQuotationLine).WithMany().HasForeignKey(x => x.VendorQuotationLineId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.VerifierEmployee).WithMany().HasForeignKey(x => x.VerifierEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommercialComparison>(entity =>
        {
            entity.ToTable("commercial_comparisons", table =>
            {
                table.HasCheckConstraint("CK_comparison_sequence_total", "\"SequenceNumber\" > 0 AND \"TotalPayableValue\" >= 0");
                table.HasCheckConstraint("CK_comparison_single_source_reason", "NOT \"IsSingleSource\" OR length(trim(coalesce(\"SingleSourceJustification\", ''))) > 0");
                table.HasCheckConstraint("CK_comparison_status", "\"Status\" IN ('Draft','Recommended','PendingApproval','Approved','Rejected','RevisionRequested','Cancelled')");
            });
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.OrganizationId, x.ComparisonNumber }).IsUnique(); entity.HasIndex(x => x.RequestForQuotationId).IsUnique(); entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
            Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.ComparisonNumber), 64); Text(entity.Property(x => x.FinancialYear), 12); Text(entity.Property(x => x.CurrencyCode), 3); Text(entity.Property(x => x.ApprovalRoute), 40); Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.IdempotencyKey), 200); entity.Property(x => x.SingleSourceJustification).HasMaxLength(2000); entity.Property(x => x.RecommendationRemarks).HasMaxLength(2000); Money(entity.Property(x => x.TotalPayableValue)); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.RequestForQuotation).WithMany().HasForeignKey(x => x.RequestForQuotationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RecommendedVendorQuotation).WithMany().HasForeignKey(x => x.RecommendedVendorQuotationId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.SelectedVendor).WithMany().HasForeignKey(x => x.SelectedVendorId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.OwnerEmployee).WithMany().HasForeignKey(x => x.OwnerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommercialComparisonLine>(entity =>
        {
            entity.ToTable("commercial_comparison_lines", table => table.HasCheckConstraint("CK_comparison_line_total", "\"TotalPayableValue\" >= 0 AND (NOT \"IsRecommended\" OR length(trim(coalesce(\"RecommendationReason\", ''))) > 0)"));
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.CommercialComparisonId, x.VendorQuotationLineId }).IsUnique(); Text(entity.Property(x => x.TechnicalComplianceSnapshot), 40); entity.Property(x => x.CommercialSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.DeliverySnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.WarrantySnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.PaymentTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.RecommendationReason).HasMaxLength(2000); Money(entity.Property(x => x.TotalPayableValue)); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.CommercialComparison).WithMany(x => x.Lines).HasForeignKey(x => x.CommercialComparisonId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.VendorQuotationLine).WithMany().HasForeignKey(x => x.VendorQuotationLineId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseTransactionApprovalHistory>(entity =>
        {
            entity.ToTable("purchase_transaction_approval_history", table => table.HasCheckConstraint("CK_purchase_approval_history_remarks", "length(trim(\"Remarks\")) > 0")); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.CommercialComparisonId, x.CorrelationId }).IsUnique(); entity.HasIndex(x => new { x.CommercialComparisonId, x.CreatedAt });
            Text(entity.Property(x => x.Action), 50); Text(entity.Property(x => x.FromStatus), 40); Text(entity.Property(x => x.ToStatus), 40); Text(entity.Property(x => x.ApprovalRoute), 40); Text(entity.Property(x => x.ActorLoginId), 256); Text(entity.Property(x => x.ActorRoleCode), 100); Text(entity.Property(x => x.CorrelationId), 200); entity.Property(x => x.Remarks).HasMaxLength(2000).IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.CommercialComparison).WithMany().HasForeignKey(x => x.CommercialComparisonId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders", table =>
            {
                table.HasCheckConstraint("CK_purchase_order_revision", "\"RevisionNumber\" > 0 AND \"SequenceNumber\" > 0");
                table.HasCheckConstraint("CK_purchase_order_values", "\"TaxableValue\" >= 0 AND \"DiscountValue\" >= 0 AND \"TaxValue\" >= 0 AND \"PackingForwarding\" >= 0 AND \"Freight\" >= 0 AND \"Insurance\" >= 0 AND \"OtherCharges\" >= 0 AND \"TotalPayableValue\" >= 0");
                table.HasCheckConstraint("CK_purchase_order_cancel_reason", "\"Status\" <> 'Cancelled' OR (\"CancelledAt\" IS NOT NULL AND length(trim(coalesce(\"CancellationReason\", ''))) > 0)");
                table.HasCheckConstraint("CK_purchase_order_status", "\"Status\" IN ('Draft','PendingReapproval','Approved','Issued','Superseded','Cancelled')");
            });
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.OrganizationId, x.PoNumber, x.RevisionNumber }).IsUnique(); entity.HasIndex(x => new { x.RootPurchaseOrderId, x.RevisionNumber }).IsUnique(); entity.HasIndex(x => new { x.RootPurchaseOrderId, x.IsCurrentVersion }).IsUnique().HasFilter("\"IsCurrentVersion\" = TRUE"); entity.HasIndex(x => new { x.CommercialComparisonId, x.RevisionNumber }).IsUnique(); entity.HasIndex(x => new { x.OrganizationId, x.IdempotencyKey }).IsUnique();
            Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.PoNumber), 64); Text(entity.Property(x => x.FinancialYear), 12); Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.CurrencyCode), 3); Text(entity.Property(x => x.IdempotencyKey), 200); entity.Property(x => x.PaymentTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.DeliveryTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.WarrantyTermsSnapshot).HasMaxLength(2000).IsRequired(); entity.Property(x => x.AmendmentReason).HasMaxLength(2000); entity.Property(x => x.CancellationReason).HasMaxLength(2000);
            foreach (var property in new[] { entity.Property(x => x.TaxableValue), entity.Property(x => x.DiscountValue), entity.Property(x => x.TaxValue), entity.Property(x => x.PackingForwarding), entity.Property(x => x.Freight), entity.Property(x => x.Insurance), entity.Property(x => x.OtherCharges), entity.Property(x => x.RoundOff), entity.Property(x => x.TotalPayableValue) }) Money(property); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PreviousVersion).WithMany().HasForeignKey(x => x.PreviousVersionId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.CommercialComparison).WithMany().HasForeignKey(x => x.CommercialComparisonId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RequestingDepartment).WithMany().HasForeignKey(x => x.RequestingDepartmentId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.DeliveryWarehouse).WithMany().HasForeignKey(x => x.DeliveryWarehouseId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.OwnerEmployee).WithMany().HasForeignKey(x => x.OwnerEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines", table => table.HasCheckConstraint("CK_purchase_order_line_quantity", "\"OrderedQuantity\" > 0 AND \"ApprovedOutstandingQuantitySnapshot\" > 0 AND \"OrderedQuantity\" <= \"ApprovedOutstandingQuantitySnapshot\" AND \"UnitRate\" >= 0 AND \"TotalPayableValue\" >= 0")); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.PurchaseOrderId, x.LineNumber }).IsUnique(); entity.HasIndex(x => new { x.PurchaseOrderId, x.CommercialComparisonLineId }).IsUnique(); entity.HasIndex(x => new { x.PurchaseRequisitionLineId, x.PurchaseOrderId });
            Text(entity.Property(x => x.ItemCodeSnapshot), 100); Text(entity.Property(x => x.ItemNameSnapshot), 300); Text(entity.Property(x => x.UomSnapshot), 30); Money(entity.Property(x => x.OrderedQuantity)); Money(entity.Property(x => x.ApprovedOutstandingQuantitySnapshot)); Money(entity.Property(x => x.UnitRate)); Money(entity.Property(x => x.TotalPayableValue)); entity.Property(x => x.CommercialSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.TaxRuleSnapshotJson).HasColumnType("jsonb").IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.PurchaseOrder).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.CommercialComparisonLine).WithMany().HasForeignKey(x => x.CommercialComparisonLineId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.PurchaseRequisitionLine).WithMany().HasForeignKey(x => x.PurchaseRequisitionLineId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.PurchaseRequirementHandoff).WithMany().HasForeignKey(x => x.PurchaseRequirementHandoffId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderHistory>(entity =>
        {
            entity.ToTable("purchase_order_history", table => table.HasCheckConstraint("CK_purchase_order_history_reason", "length(trim(\"Reason\")) > 0")); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.PurchaseOrderId, x.CorrelationId }).IsUnique(); entity.HasIndex(x => new { x.PurchaseOrderId, x.CreatedAt }); Text(entity.Property(x => x.Action), 50); Text(entity.Property(x => x.FromStatus), 40); Text(entity.Property(x => x.ToStatus), 40); Text(entity.Property(x => x.ActorLoginId), 256); Text(entity.Property(x => x.ActorRoleCode), 100); Text(entity.Property(x => x.CorrelationId), 200); entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken(); entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaterialFollowUpHandoff>(entity =>
        {
            entity.ToTable("material_followup_handoffs", table => table.HasCheckConstraint("CK_material_followup_quantity", "\"OrderedQuantitySnapshot\" > 0 AND \"Status\" IN ('PendingFollowUp','Closed','Cancelled')")); entity.HasKey(x => x.Id); entity.HasIndex(x => x.PurchaseOrderLineId).IsUnique(); entity.HasIndex(x => x.HandoffNumber).IsUnique(); Text(entity.Property(x => x.HandoffNumber), 80); Text(entity.Property(x => x.Status), 40); Text(entity.Property(x => x.CorrelationId), 200); Money(entity.Property(x => x.OrderedQuantitySnapshot)); entity.Property(x => x.Version).IsConcurrencyToken(); entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => x.PurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseTransactionStatusHistory>(entity =>
        {
            entity.ToTable("purchase_transaction_status_history", table => table.HasCheckConstraint("CK_purchase_transaction_history_remarks", "length(trim(\"Remarks\")) > 0")); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CorrelationId }).IsUnique(); entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt }); Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.EntityType), 80); Text(entity.Property(x => x.DocumentNumber), 80); Text(entity.Property(x => x.Action), 50); entity.Property(x => x.FromStatus).HasMaxLength(40); Text(entity.Property(x => x.ToStatus), 40); Text(entity.Property(x => x.ActorLoginId), 256); Text(entity.Property(x => x.ActorRoleCode), 100); Text(entity.Property(x => x.CorrelationId), 200); entity.Property(x => x.Remarks).HasMaxLength(2000).IsRequired(); entity.Property(x => x.Version).IsConcurrencyToken(); entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseTransactionApprovalPolicy>(entity =>
        {
            entity.ToTable("purchase_transaction_approval_policies", table =>
            {
                table.HasCheckConstraint("CK_purchase_transaction_policy_amounts", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" >= \"MinimumAmount\")");
                table.HasCheckConstraint("CK_purchase_transaction_policy_dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom");
            });
            entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.OrganizationId, x.RouteCode, x.EffectiveFrom, x.EffectiveTo }).IsUnique().AreNullsDistinct(false); Text(entity.Property(x => x.OrganizationId), 100); Text(entity.Property(x => x.RouteCode), 40); Text(entity.Property(x => x.ApproverRoleCode), 100); Money(entity.Property(x => x.MinimumAmount)); Money(entity.Property(x => x.MaximumAmount)); entity.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<PageDefinition>().HasData(Rev869BSeedData.Pages);
        modelBuilder.Entity<RolePagePermission>().HasData(Rev869BSeedData.RolePagePermissions);
        modelBuilder.Entity<PurchaseTransactionApprovalPolicy>().HasData(Rev869BSeedData.ApprovalPolicies);
    }

    private static void Text(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<string> property, int maxLength) => property.HasMaxLength(maxLength).IsRequired();
    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) => property.HasPrecision(24, 6);
    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal?> property) => property.HasPrecision(24, 6);
}
