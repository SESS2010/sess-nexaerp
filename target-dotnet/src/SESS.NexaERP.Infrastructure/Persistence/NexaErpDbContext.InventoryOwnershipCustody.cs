using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<InventoryExternalParty> InventoryExternalParties => Set<InventoryExternalParty>();
    public DbSet<InventoryAccountHolder> InventoryAccountHolders => Set<InventoryAccountHolder>();
    public DbSet<InventoryOwnershipAccount> InventoryOwnershipAccounts => Set<InventoryOwnershipAccount>();
    public DbSet<InventoryCustodyAccount> InventoryCustodyAccounts => Set<InventoryCustodyAccount>();
    public DbSet<InventoryCustodyCase> InventoryCustodyCases => Set<InventoryCustodyCase>();
    public DbSet<InventoryCustodyCaseLine> InventoryCustodyCaseLines => Set<InventoryCustodyCaseLine>();
    public DbSet<InventoryCustodyAssignment> InventoryCustodyAssignments => Set<InventoryCustodyAssignment>();
    public DbSet<InventoryCustodyHandoff> InventoryCustodyHandoffs => Set<InventoryCustodyHandoff>();
    public DbSet<InventoryCustodyHandoffLine> InventoryCustodyHandoffLines => Set<InventoryCustodyHandoffLine>();
    public DbSet<InventoryOwnershipTransfer> InventoryOwnershipTransfers => Set<InventoryOwnershipTransfer>();
    public DbSet<InventoryOwnershipTransferLine> InventoryOwnershipTransferLines => Set<InventoryOwnershipTransferLine>();
    public DbSet<InventoryMemoLiabilityEvent> InventoryMemoLiabilityEvents => Set<InventoryMemoLiabilityEvent>();

    private static void ConfigureInventoryOwnershipCustody(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryExternalParty>(entity =>
        {
            entity.ToTable("inventory_external_parties", table =>
            {
                table.HasCheckConstraint("CK_inventory_external_parties_type",
                    @"""PartyType"" IN ('CUSTOMER','VENDOR','OTHER')");
                table.HasCheckConstraint("CK_inventory_external_parties_identity",
                    @"(""PartyType"" = 'CUSTOMER' AND ""CustomerId"" IS NOT NULL AND ""VendorId"" IS NULL)
                   OR (""PartyType"" = 'VENDOR' AND ""VendorId"" IS NOT NULL AND ""CustomerId"" IS NULL)
                   OR (""PartyType"" = 'OTHER' AND ""CustomerId"" IS NULL AND ""VendorId"" IS NULL)");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.PartyCode }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.CustomerId }).IsUnique().HasFilter(@"""CustomerId"" IS NOT NULL");
            entity.HasIndex(x => new { x.CompanyId, x.VendorId }).IsUnique().HasFilter(@"""VendorId"" IS NOT NULL");
            entity.Property(x => x.PartyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PartyCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PartyNameSnapshot).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryAccountHolder>(entity =>
        {
            entity.ToTable("inventory_account_holders", table =>
            {
                table.HasCheckConstraint("CK_inventory_account_holders_type",
                    @"""HolderType"" IN ('COMPANY','EXTERNAL_PARTY','EMPLOYEE')");
                table.HasCheckConstraint("CK_inventory_account_holders_identity",
                    @"(""HolderType"" = 'COMPANY' AND ""HolderCompanyId"" IS NOT NULL AND ""ExternalPartyId"" IS NULL AND ""EmployeeId"" IS NULL)
                   OR (""HolderType"" = 'EXTERNAL_PARTY' AND ""HolderCompanyId"" IS NULL AND ""ExternalPartyId"" IS NOT NULL AND ""EmployeeId"" IS NULL)
                   OR (""HolderType"" = 'EMPLOYEE' AND ""HolderCompanyId"" IS NULL AND ""ExternalPartyId"" IS NULL AND ""EmployeeId"" IS NOT NULL)");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.HolderCode }).IsUnique();
            entity.Property(x => x.HolderType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.HolderCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.HolderNameSnapshot).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.HolderCompany).WithMany().HasForeignKey(x => x.HolderCompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExternalParty).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ExternalPartyId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryOwnershipAccount>(entity =>
        {
            entity.ToTable("inventory_ownership_accounts", table =>
            {
                table.HasCheckConstraint("CK_inventory_ownership_accounts_type",
                    @"""OwnershipType"" IN ('SESS_INVENTORY','CUSTOMER_PROPERTY','SUPPLIER_LOAN','DEMO_CUSTODY')");
                table.HasCheckConstraint("CK_inventory_ownership_accounts_valuation",
                    @"(""OwnershipType"" = 'SESS_INVENTORY' AND ""InventoryValuationBasis"" = 'FIFO')
                   OR (""OwnershipType"" <> 'SESS_INVENTORY' AND ""InventoryValuationBasis"" = 'ZERO_MEMO')");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.AccountCode }).IsUnique();
            entity.Property(x => x.AccountCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.OwnershipType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.InventoryValuationBasis).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.HasOne(x => x.AccountHolder).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.AccountHolderId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCustodyAccount>(entity =>
        {
            entity.ToTable("inventory_custody_accounts", table =>
            {
                table.HasCheckConstraint("CK_inventory_custody_accounts_type",
                    @"""CustodyType"" IN ('WAREHOUSE','EMPLOYEE','VEHICLE','SITE','VENDOR','CUSTOMER','OTHER')");
                table.HasCheckConstraint("CK_inventory_custody_accounts_location",
                    @"(""RackBinId"" IS NULL OR ""WarehouseId"" IS NOT NULL)");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.AccountCode }).IsUnique();
            entity.Property(x => x.AccountCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CustodyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.VehicleReference).HasMaxLength(120);
            entity.Property(x => x.SiteReference).HasMaxLength(160);
            entity.HasOne(x => x.AccountHolder).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.AccountHolderId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany()
                .HasForeignKey(x => new { x.RackBinId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCustodyCase>(entity =>
        {
            entity.ToTable("inventory_custody_cases", table =>
            {
                table.HasCheckConstraint("CK_inventory_custody_cases_type",
                    @"""CaseType"" IN ('CUSTOMER_OTHER_BRAND_MODIFICATION','CUSTOMER_SESS_MACHINE_WARRANTY','CUSTOMER_SESS_SPARE_WARRANTY','CUSTOMER_REMOVED_PART','SUPPLIER_LOAN','DEMO_CUSTODY')");
                table.HasCheckConstraint("CK_inventory_custody_cases_status",
                    @"""Status"" IN ('RECEIVED','RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION','AUTHORIZED_FOR_WORK','IN_WORK','READY_FOR_RETURN','RETURNED','CLOSED')");
                table.HasCheckConstraint("CK_inventory_custody_cases_commercial_status",
                    @"""CommercialAuthorizationStatus"" IN ('NOT_REQUIRED','AWAITING_OFFER','AWAITING_CUSTOMER_PO','AUTHORIZED')");
                table.HasCheckConstraint("CK_inventory_custody_cases_due_date_evidence",
                    @"(""DueDate"" IS NULL AND ""DueDateSetByEmployeeId"" IS NULL AND ""DueDateSetAt"" IS NULL)
                   OR (""DueDate"" IS NOT NULL AND ""DueDateSetByEmployeeId"" IS NOT NULL AND ""DueDateSetAt"" IS NOT NULL)");
                table.HasCheckConstraint("CK_inventory_custody_cases_other_brand_chargeable",
                    @"""CaseType"" <> 'CUSTOMER_OTHER_BRAND_MODIFICATION' OR ""CommercialAuthorizationStatus"" <> 'NOT_REQUIRED'");
                table.HasCheckConstraint("CK_inventory_custody_cases_work_authorization",
                    @"""Status"" IN ('RECEIVED','RECEIVED_AWAITING_COMMERCIAL_AUTHORIZATION')
                   OR ""CommercialAuthorizationStatus"" IN ('NOT_REQUIRED','AUTHORIZED')");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.CaseNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.Status, x.DueDate });
            entity.Property(x => x.CaseNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.CaseType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(60).IsRequired();
            entity.Property(x => x.CommercialAuthorizationStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.InboundReturnableDcNumber).HasMaxLength(120).IsRequired();
            entity.Property(x => x.OfferReference).HasMaxLength(160);
            entity.Property(x => x.CustomerInstructionReference).HasMaxLength(200);
            entity.Property(x => x.ClosureReason).HasMaxLength(500);
            entity.HasOne(x => x.ExternalParty).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ExternalPartyId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OwnershipAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.OwnershipAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustodyAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CustodyAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustomerPurchaseOrder).WithMany()
                .HasForeignKey(x => x.CustomerPurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DueDateSetByEmployee).WithMany()
                .HasForeignKey(x => x.DueDateSetByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCustodyCaseLine>(entity =>
        {
            entity.ToTable("inventory_custody_case_lines", table =>
            {
                table.HasCheckConstraint("CK_inventory_custody_case_lines_quantity", @"""Quantity"" > 0");
                table.HasCheckConstraint("CK_inventory_custody_case_lines_identity",
                    @"""ItemId"" IS NOT NULL OR NULLIF(btrim(""ExternalAssetIdentifier""), '') IS NOT NULL");
                table.HasCheckConstraint("CK_inventory_custody_case_lines_scope",
                    @"""CommercialScopeStatus"" IN ('NOT_REQUIRED','AWAITING_AUTHORIZATION','AUTHORIZED','OUT_OF_SCOPE')");
                table.HasCheckConstraint("CK_inventory_custody_case_lines_scope_evidence",
                    @"""CommercialScopeStatus"" <> 'AUTHORIZED' OR ""CustomerPurchaseOrderLineId"" IS NOT NULL OR NULLIF(btrim(""OfferReference""), '') IS NOT NULL");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.CustodyCaseId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.CustodyCaseId, x.LineNumber }).IsUnique();
            entity.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ExternalAssetIdentifier).HasMaxLength(160);
            entity.Property(x => x.SerialNumberSnapshot).HasMaxLength(160);
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.UomCodeSnapshot).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CommercialScopeStatus).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OfferReference).HasMaxLength(160);
            entity.Property(x => x.ScopeDecisionReason).HasMaxLength(500);
            entity.HasOne(x => x.CustodyCase).WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.CompanyId, x.CustodyCaseId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Uom).WithMany().HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OwnershipAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.OwnershipAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustomerPurchaseOrderLine).WithMany()
                .HasForeignKey(x => x.CustomerPurchaseOrderLineId).OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureCustodySourceLinks(modelBuilder);

        modelBuilder.Entity<InventoryCustodyAssignment>(entity =>
        {
            entity.ToTable("inventory_custody_assignments", table =>
            {
                table.HasCheckConstraint("CK_inventory_custody_assignments_quantity", @"""AssignedQuantity"" > 0");
                table.HasCheckConstraint("CK_inventory_custody_assignments_period",
                    @"""EffectiveTo"" IS NULL OR ""EffectiveTo"" >= ""EffectiveFrom""");
                table.HasCheckConstraint("CK_inventory_custody_assignments_current",
                    @"(""IsCurrent"" AND ""EffectiveTo"" IS NULL) OR (NOT ""IsCurrent"" AND ""EffectiveTo"" IS NOT NULL)");
                table.HasCheckConstraint("CK_inventory_custody_assignments_location",
                    @"""RackBinId"" IS NULL OR ""WarehouseId"" IS NOT NULL");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.CustodyCaseLineId }).HasFilter(@"""IsCurrent""").IsUnique();
            entity.Property(x => x.AssignedQuantity).HasPrecision(18, 6);
            entity.Property(x => x.AssignmentReason).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.CustodyAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CustodyAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustodyCaseLine).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CustodyCaseLineId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Warehouse).WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RackBin).WithMany()
                .HasForeignKey(x => new { x.RackBinId, x.CompanyId })
                .HasPrincipalKey(x => new { x.Id, x.CompanyId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCustodyHandoff>(entity =>
        {
            entity.ToTable("inventory_custody_handoffs", table =>
            {
                table.HasCheckConstraint("CK_inventory_custody_handoffs_status",
                    @"""Status"" IN ('DRAFT','COMPLETED','REVERSED')");
                table.HasCheckConstraint("CK_inventory_custody_handoffs_accounts",
                    @"""FromCustodyAccountId"" <> ""ToCustodyAccountId""");
                table.HasCheckConstraint("CK_inventory_custody_handoffs_completion",
                    @"(""Status"" = 'DRAFT' AND ""HandedOverAt"" IS NULL AND ""HandedOverByEmployeeId"" IS NULL)
                   OR (""Status"" <> 'DRAFT' AND ""HandedOverAt"" IS NOT NULL AND ""HandedOverByEmployeeId"" IS NOT NULL)");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.HandoffNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.Property(x => x.HandoffNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.FromCustodyAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.FromCustodyAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToCustodyAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ToCustodyAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.HandedOverByEmployee).WithMany().HasForeignKey(x => x.HandedOverByEmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReceivedByEmployee).WithMany().HasForeignKey(x => x.ReceivedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryCustodyHandoffLine>(entity =>
        {
            entity.ToTable("inventory_custody_handoff_lines", table =>
                table.HasCheckConstraint("CK_inventory_custody_handoff_lines_quantity", @"""Quantity"" > 0"));
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.CustodyHandoffId, x.LineNumber }).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.HasOne(x => x.CustodyHandoff).WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.CompanyId, x.CustodyHandoffId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CustodyCaseLine).WithMany().HasForeignKey(x => x.CustodyCaseLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FromCustodyAssignment).WithMany().HasForeignKey(x => x.FromCustodyAssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToCustodyAssignment).WithMany().HasForeignKey(x => x.ToCustodyAssignmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryOwnershipTransfer>(entity =>
        {
            entity.ToTable("inventory_ownership_transfers", table =>
            {
                table.HasCheckConstraint("CK_inventory_ownership_transfers_type",
                    @"""TransferType"" IN ('CUSTOMER_BUYBACK','CUSTOMER_INSTRUCTION','INTERCOMPANY_ACCEPTANCE','SUPPLIER_LOAN_CONVERSION','CAPITALIZATION')");
                table.HasCheckConstraint("CK_inventory_ownership_transfers_status",
                    @"""Status"" IN ('DRAFT','APPROVED','POSTED','REVERSED')");
                table.HasCheckConstraint("CK_inventory_ownership_transfers_accounts",
                    @"""FromOwnershipAccountId"" <> ""ToOwnershipAccountId""");
                table.HasCheckConstraint("CK_inventory_ownership_transfers_approval",
                    @"(""Status"" = 'DRAFT' AND ""ApprovedByEmployeeId"" IS NULL AND ""ApprovedAt"" IS NULL AND ""ApprovedRoleCode"" IS NULL)
                   OR (""Status"" <> 'DRAFT' AND ""ApprovedByEmployeeId"" IS NOT NULL AND ""ApprovedAt"" IS NOT NULL AND ""ApprovedRoleCode"" IS NOT NULL)");
                table.HasCheckConstraint("CK_inventory_ownership_transfers_buyback",
                    @"""TransferType"" <> 'CUSTOMER_BUYBACK' OR NULLIF(btrim(""AgreementReference""), '') IS NOT NULL");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.TransferNumber }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.IdempotencyKey }).IsUnique();
            entity.Property(x => x.TransferNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.TransferType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AgreementReference).HasMaxLength(200);
            entity.Property(x => x.ApprovedRoleCode).HasMaxLength(64);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.FromOwnershipAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.FromOwnershipAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToOwnershipAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.ToOwnershipAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApprovedByEmployee).WithMany().HasForeignKey(x => x.ApprovedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryOwnershipTransferLine>(entity =>
        {
            entity.ToTable("inventory_ownership_transfer_lines", table =>
                table.HasCheckConstraint("CK_inventory_ownership_transfer_lines_quantity", @"""Quantity"" > 0"));
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.OwnershipTransferId, x.LineNumber }).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.HasOne(x => x.OwnershipTransfer).WithMany(x => x.Lines)
                .HasForeignKey(x => new { x.CompanyId, x.OwnershipTransferId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CustodyCaseLine).WithMany().HasForeignKey(x => x.CustodyCaseLineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryMemoLiabilityEvent>(entity =>
        {
            entity.ToTable("inventory_memo_liability_events", table =>
            {
                table.HasCheckConstraint("CK_inventory_memo_liability_events_type",
                    @"""EventType"" IN ('LOAN_RECEIVED','LOAN_CONSUMED_PENDING_PROCUREMENT','LOAN_CLOSED_AGAINST_PO_GRN','REVERSAL')");
                table.HasCheckConstraint("CK_inventory_memo_liability_events_quantity", @"""Quantity"" > 0");
                table.HasCheckConstraint("CK_inventory_memo_liability_events_value", @"""MemoValue"" >= 0");
                table.HasCheckConstraint("CK_inventory_memo_liability_events_close",
                    @"""EventType"" <> 'LOAN_CLOSED_AGAINST_PO_GRN' OR (""PurchaseOrderId"" IS NOT NULL AND ""GoodsReceiptId"" IS NOT NULL)");
                table.HasCheckConstraint("CK_inventory_memo_liability_events_reversal",
                    @"(""EventType"" = 'REVERSAL' AND ""ReversesEventId"" IS NOT NULL)
                   OR (""EventType"" <> 'REVERSAL' AND ""ReversesEventId"" IS NULL)");
            });
            entity.HasAlternateKey(x => new { x.CompanyId, x.Id });
            entity.HasIndex(x => new { x.CompanyId, x.OwnershipAccountId, x.OccurredAt });
            entity.HasIndex(x => new { x.CompanyId, x.CorrelationId }).IsUnique();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.MemoValue).HasPrecision(18, 2);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ActorRoleCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.OwnershipAccount).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.OwnershipAccountId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CustodyCaseLine).WithMany().HasForeignKey(x => x.CustodyCaseLineId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.GoodsReceipt).WithMany().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReversesEvent).WithMany().HasForeignKey(x => x.ReversesEventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorEmployee).WithMany().HasForeignKey(x => x.ActorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCustodySourceLinks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryCustodyCaseSourceLink>().UseTpcMappingStrategy();

        ConfigureSourceLink<InventoryCustodyCaseGateEntryLink>(modelBuilder, "inventory_custody_case_gate_entry_links");
        modelBuilder.Entity<InventoryCustodyCaseGateEntryLink>()
            .HasOne(x => x.GateEntry).WithMany().HasForeignKey(x => x.GateEntryId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSourceLink<InventoryCustodyCaseGoodsReceiptLink>(modelBuilder, "inventory_custody_case_goods_receipt_links");
        modelBuilder.Entity<InventoryCustodyCaseGoodsReceiptLink>()
            .HasOne(x => x.GoodsReceipt).WithMany().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSourceLink<InventoryCustodyCaseDeliveryChallanLink>(modelBuilder, "inventory_custody_case_delivery_challan_links");
        modelBuilder.Entity<InventoryCustodyCaseDeliveryChallanLink>()
            .HasOne(x => x.DeliveryChallan).WithMany().HasForeignKey(x => x.DeliveryChallanId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSourceLink<InventoryCustodyCasePurchaseOrderLink>(modelBuilder, "inventory_custody_case_purchase_order_links");
        modelBuilder.Entity<InventoryCustodyCasePurchaseOrderLink>()
            .HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSourceLink<InventoryCustodyCaseCustomerPurchaseOrderLink>(modelBuilder, "inventory_custody_case_customer_purchase_order_links");
        modelBuilder.Entity<InventoryCustodyCaseCustomerPurchaseOrderLink>()
            .HasOne(x => x.CustomerPurchaseOrder).WithMany().HasForeignKey(x => x.CustomerPurchaseOrderId).OnDelete(DeleteBehavior.Restrict);

        ConfigureSourceLink<InventoryCustodyCaseJobOrderLink>(modelBuilder, "inventory_custody_case_job_order_links");
        modelBuilder.Entity<InventoryCustodyCaseJobOrderLink>()
            .HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => x.JobOrderId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureSourceLink<T>(ModelBuilder modelBuilder, string tableName)
        where T : InventoryCustodyCaseSourceLink
    {
        modelBuilder.Entity<T>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasIndex(x => new { x.CompanyId, x.CustodyCaseId, x.LinkRole });
            entity.Property(x => x.LinkRole).HasMaxLength(40).IsRequired();
            entity.HasOne(x => x.CustodyCase).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CustodyCaseId })
                .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CustodyCaseLine).WithMany()
                .HasForeignKey(x => new { x.CompanyId, x.CustodyCaseId, x.CustodyCaseLineId })
                .HasPrincipalKey(x => new { x.CompanyId, x.CustodyCaseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
